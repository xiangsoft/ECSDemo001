using System.Collections.Generic;
using UnityEngine;
using Xiangsoft.Lib.ECS.Component;
using Xiangsoft.Lib.ECS.Grid;
using Xiangsoft.Lib.ECS.World;
using Xiangsoft.Lib.Pathfinding;

namespace Xiangsoft.Lib.ECS.System
{
    public class MovementSystem : ISystem
    {
        private GameWorld world;
        private FlowFieldGrid flowGrid;
        private SpatialHashECSGrid spatialGrid;

        // 静态缓存池，用于寻找邻居，绝对 0 GC
        private List<int> neighborBuffer = new List<int>(64);

        private readonly ulong requireMask;

        public MovementSystem(GameWorld world, FlowFieldGrid flowGrid, SpatialHashECSGrid spatialGrid)
        {
            this.world = world;
            this.flowGrid = flowGrid;
            this.spatialGrid = spatialGrid;

            requireMask = (ulong)(ComponentMask.Transform | ComponentMask.Movement);
        }

        public void Update(float deltaTime)
        {
            float time = Time.time;

            for (int i = 0; i < world.MaxAllocatedID; i++)
            {
                if ((world.EntityMasks[i] & requireMask) != requireMask)
                    continue;

                Vector3 currentPos = world.Transforms[i].Position;
                MovementComponent moveComp = world.Movements[i];

                if (flowGrid.HasArrived(currentPos))
                    continue;

                Vector3 desiredVelocity = Vector3.zero;
                AIState state = world.AIs[i].CurrentState;

                if (state == AIState.Chase)
                {
                    // 追击状态：读取流场方向
                    Vector2 flowDir2D = flowGrid.GetDirectionFromWorldPos(currentPos);
                    desiredVelocity = new Vector3(flowDir2D.x, 0, flowDir2D.y);

                    if (desiredVelocity != Vector3.zero)
                    {
                        Vector3 rightVector = new Vector3(-desiredVelocity.z, 0, desiredVelocity.x);
                        float wobble = Mathf.Sin(time * moveComp.WobbleSpeed + moveComp.RandomPhase) * moveComp.WobbleStrength;
                        desiredVelocity += rightVector * wobble;
                        desiredVelocity.Normalize();
                    }
                }
                else if (state == AIState.Flee)
                {
                    // 逃跑状态：无视流场，直接反向远离主角！
                    Vector3 playerPos = world.Transforms[ECSEngine.Instance.PlayerEntityID].Position;
                    desiredVelocity = (currentPos - playerPos).normalized; // 背对主角
                }
                else if (state == AIState.Attack)
                {
                    // 攻击状态：速度为 0，原地罚站！
                    desiredVelocity = Vector3.zero;

                    // 但是要平滑转身面向主角
                    Vector3 playerPos = world.Transforms[ECSEngine.Instance.PlayerEntityID].Position;
                    Vector3 lookDir = (playerPos - currentPos).normalized;
                    lookDir.y = 0;
                    if (lookDir != Vector3.zero)
                    {
                        world.Transforms[i].Rotation = Quaternion.Slerp(world.Transforms[i].Rotation, Quaternion.LookRotation(lookDir), moveComp.RotationSpeed * deltaTime);
                    }
                }

                // 3. 计算排斥力 (需要把 SpatialHashGrid 稍作改造存储 Entity ID，这里假设它返回的是 ID 列表)
                spatialGrid.FindNeighbors(currentPos, neighborBuffer);
                Vector3 separationForce = Vector3.zero;

                for (int j = 0; j < neighborBuffer.Count; j++)
                {
                    int neighborID = neighborBuffer[j];

                    if (neighborID == i)
                        continue;

                    Vector3 dirAway = currentPos - world.Transforms[neighborID].Position;
                    dirAway.y = 0;

                    float sqrDist = dirAway.sqrMagnitude;
                    float sepRadius = moveComp.SeparationRadius;
                    if (sqrDist < sepRadius * sepRadius && sqrDist > 0.0001f)
                    {
                        float dist = Mathf.Sqrt(sqrDist);
                        float force = 1f - (dist / sepRadius);
                        separationForce += (dirAway / dist) * force;
                    }
                }

                Vector3 finalDirection = (desiredVelocity + separationForce * moveComp.SeparationWeight).normalized;

                // 4. 分轴移动与防穿墙计算
                if (finalDirection == Vector3.zero)
                    continue;

                Vector3 moveDelta = finalDirection * moveComp.MoveSpeed * deltaTime;

                Vector3 nextPosX = currentPos + new Vector3(moveDelta.x, 0, 0);
                if (flowGrid.IsWalkable(nextPosX))
                    currentPos.x = nextPosX.x;

                Vector3 nextPosZ = currentPos + new Vector3(0, 0, moveDelta.z);
                if (flowGrid.IsWalkable(nextPosZ))
                    currentPos.z = nextPosZ.z;

                // 将计算好的结果写回数据
                world.Transforms[i].Position = currentPos;
                world.Transforms[i].Rotation = Quaternion.Slerp(
                    world.Transforms[i].Rotation,
                    Quaternion.LookRotation(finalDirection),
                    moveComp.RotationSpeed * deltaTime
                );
            }
        }
    }
}