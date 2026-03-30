using System.Collections.Generic;
using TrueSync;
using UnityEngine;
using Xiangsoft.Lib.ECS.Component;
using Xiangsoft.Lib.ECS.Grid;
using Xiangsoft.Lib.ECS.World;
using Xiangsoft.Lib.Pathfinding;

namespace Xiangsoft.Lib.ECS.System
{
    public class MovementSystem : BaseSystem
    {
        private FlowFieldGrid flowGrid;
        private SpatialHashECSGrid spatialGrid;

        // 静态缓存池，用于寻找邻居，绝对 0 GC
        private List<int> neighborBuffer = new List<int>(64);

        public MovementSystem(GameWorld world, FlowFieldGrid flowGrid, SpatialHashECSGrid spatialGrid) : base(world)
        {
            this.flowGrid = flowGrid;
            this.spatialGrid = spatialGrid;

            requireMask = (ulong)(ComponentMask.Transform | ComponentMask.Movement);
        }

        public override void Update(FP deltaTime)
        {
            FP time = Time.time;

            for (int i = 0; i < world.MaxAllocatedID; i++)
            {
                if (!isValidEntity(i))
                    continue;

                TSVector currentPos = world.Transforms[i].Position;
                MovementComponent moveComp = world.Movements[i];

                if (flowGrid.HasArrived(currentPos))
                    continue;

                TSVector desiredVelocity = TSVector.zero;
                AIState state = world.AIs[i].CurrentState;

                if (state == AIState.Chase)
                {
                    // 追击状态：读取流场方向
                    TSVector2 flowDir2D = flowGrid.GetDirectionFromWorldPos(currentPos);
                    desiredVelocity = new TSVector(flowDir2D.x, 0, flowDir2D.y);

                    if (desiredVelocity != TSVector.zero)
                    {
                        TSVector rightVector = new TSVector(-desiredVelocity.z, 0, desiredVelocity.x);
                        FP wobble = TSMath.Sin(time * moveComp.WobbleSpeed + moveComp.RandomPhase) * moveComp.WobbleStrength;
                        desiredVelocity += rightVector * wobble;
                        desiredVelocity.Normalize();
                    }
                }
                else if (state == AIState.Flee)
                {
                    // 逃跑状态：无视流场，直接反向远离主角！
                    TSVector playerPos = world.Transforms[ECSEngine.Instance.PlayerEntityID].Position;
                    desiredVelocity = (currentPos - playerPos).normalized; // 背对主角
                }
                else if (state == AIState.Attack)
                {
                    // 攻击状态：速度为 0，原地罚站！
                    desiredVelocity = TSVector.zero;

                    // 但是要平滑转身面向主角
                    TSVector playerPos = world.Transforms[ECSEngine.Instance.PlayerEntityID].Position;
                    TSVector lookDir = (playerPos - currentPos).normalized;
                    lookDir.y = 0;
                    if (lookDir != TSVector.zero)
                    {
                        world.Transforms[i].Rotation = TSQuaternion.Slerp(world.Transforms[i].Rotation, TSQuaternion.LookRotation(lookDir), moveComp.RotationSpeed * deltaTime);
                    }
                }

                // 3. 计算排斥力 (需要把 SpatialHashGrid 稍作改造存储 Entity ID，这里假设它返回的是 ID 列表)
                spatialGrid.FindNeighbors(currentPos.ToVector(), neighborBuffer);
                TSVector separationForce = TSVector.zero;

                for (int j = 0; j < neighborBuffer.Count; j++)
                {
                    int neighborID = neighborBuffer[j];

                    if (neighborID == i)
                        continue;

                    TSVector dirAway = currentPos - world.Transforms[neighborID].Position;
                    dirAway.y = 0;

                    FP sqrDist = dirAway.sqrMagnitude;
                    FP sepRadius = moveComp.SeparationRadius;
                    if (sqrDist < sepRadius * sepRadius && sqrDist > 0.0001f)
                    {
                        FP dist = TSMath.Sqrt(sqrDist);
                        FP force = 1f - (dist / sepRadius);
                        separationForce += (dirAway / dist) * force;
                    }
                }

                TSVector finalDirection = (desiredVelocity + separationForce * moveComp.SeparationWeight).normalized;

                // 4. 分轴移动与防穿墙计算
                if (finalDirection == TSVector.zero)
                    continue;

                TSVector moveDelta = finalDirection * moveComp.MoveSpeed * deltaTime;

                TSVector nextPosX = currentPos + new TSVector(moveDelta.x, 0, 0);
                if (flowGrid.IsWalkable(nextPosX))
                    currentPos.x = nextPosX.x;

                TSVector nextPosZ = currentPos + new TSVector(0, 0, moveDelta.z);
                if (flowGrid.IsWalkable(nextPosZ))
                    currentPos.z = nextPosZ.z;

                // 将计算好的结果写回数据
                world.Transforms[i].Position = currentPos;
                world.Transforms[i].Rotation = TSQuaternion.Slerp(
                    world.Transforms[i].Rotation,
                    TSQuaternion.LookRotation(finalDirection),
                    moveComp.RotationSpeed * deltaTime
                );
            }
        }
    }
}