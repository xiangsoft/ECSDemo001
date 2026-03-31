using System.Collections.Generic;
using FixedMathSharp;
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
        private Fixed64 minDist = new Fixed64(0.0001f);

        public MovementSystem(GameWorld world, FlowFieldGrid flowGrid, SpatialHashECSGrid spatialGrid) : base(world)
        {
            this.flowGrid = flowGrid;
            this.spatialGrid = spatialGrid;

            requireMask = (ulong)(ComponentMask.Transform | ComponentMask.Movement);
        }

        public override void Update(Fixed64 deltaTime)
        {
            Fixed64 time = (Fixed64)Time.time;

            for (int i = 0; i < world.MaxAllocatedID; i++)
            {
                if (!isValidEntity(i))
                    continue;

                Vector3d currentPos = world.Transforms[i].Position;
                MovementComponent moveComp = world.Movements[i];

                if (flowGrid.HasArrived(currentPos))
                    continue;

                Vector3d desiredVelocity = Vector3d.Zero;
                AIState state = world.AIs[i].CurrentState;

                if (state == AIState.Chase)
                {
                    // 追击状态：读取流场方向
                    Vector2d flowDir2D = flowGrid.GetDirectionFromWorldPos(currentPos);
                    desiredVelocity = new Vector3d(flowDir2D.x, Fixed64.Zero, flowDir2D.y);

                    if (desiredVelocity != Vector3d.Zero)
                    {
                        Vector3d rightVector = new Vector3d(-desiredVelocity.z, Fixed64.Zero, desiredVelocity.x);
                        Fixed64 wobble = FixedMath.Sin(time * moveComp.WobbleSpeed + moveComp.RandomPhase) * moveComp.WobbleStrength;
                        desiredVelocity += rightVector * wobble;
                        desiredVelocity.Normalize();
                    }
                }
                else if (state == AIState.Flee)
                {
                    // 逃跑状态：无视流场，直接反向远离主角！
                    Vector3d playerPos = world.Transforms[ECSEngine.Instance.PlayerEntityID].Position;
                    desiredVelocity = (currentPos - playerPos).Normal; // 背对主角
                }
                else if (state == AIState.Attack)
                {
                    // 攻击状态：速度为 0，原地罚站！
                    desiredVelocity = Vector3d.Zero;

                    // 但是要平滑转身面向主角
                    Vector3d playerPos = world.Transforms[ECSEngine.Instance.PlayerEntityID].Position;
                    Vector3d lookDir = (playerPos - currentPos).Normal;
                    lookDir.y = Fixed64.Zero;
                    if (lookDir != Vector3d.Zero)
                    {
                        world.Transforms[i].Rotation = FixedQuaternion.Lerp(world.Transforms[i].Rotation, FixedQuaternion.LookRotation(lookDir), moveComp.RotationSpeed * deltaTime);
                    }
                }

                // 3. 计算排斥力 (需要把 SpatialHashGrid 稍作改造存储 Entity ID，这里假设它返回的是 ID 列表)
                spatialGrid.FindNeighbors(currentPos, neighborBuffer);
                Vector3d separationForce = Vector3d.Zero;

                for (int j = 0; j < neighborBuffer.Count; j++)
                {
                    int neighborID = neighborBuffer[j];

                    if (neighborID == i)
                        continue;

                    Vector3d dirAway = currentPos - world.Transforms[neighborID].Position;
                    dirAway.y = Fixed64.Zero;

                    Fixed64 sqrDist = dirAway.SqrMagnitude;
                    Fixed64 sepRadius = moveComp.SeparationRadius;
                    if (sqrDist < sepRadius * sepRadius && sqrDist > minDist)
                    {
                        Fixed64 dist = FixedMath.Sqrt(sqrDist);
                        Fixed64 force = Fixed64.One - (dist / sepRadius);
                        separationForce += (dirAway / dist) * force;
                    }
                }

                Vector3d finalDirection = (desiredVelocity + separationForce * moveComp.SeparationWeight).Normal;

                // 4. 分轴移动与防穿墙计算
                if (finalDirection == Vector3d.Zero)
                    continue;

                Vector3d moveDelta = finalDirection * moveComp.MoveSpeed * deltaTime;

                Vector3d nextPosX = currentPos + new Vector3d(moveDelta.x, Fixed64.Zero, Fixed64.Zero);
                if (flowGrid.IsWalkable(nextPosX))
                    currentPos.x = nextPosX.x;

                Vector3d nextPosZ = currentPos + new Vector3d(Fixed64.Zero, Fixed64.Zero, moveDelta.z);
                if (flowGrid.IsWalkable(nextPosZ))
                    currentPos.z = nextPosZ.z;

                // 将计算好的结果写回数据
                world.Transforms[i].Position = currentPos;
                world.Transforms[i].Rotation = FixedQuaternion.Lerp(
                    world.Transforms[i].Rotation,
                    FixedQuaternion.LookRotation(finalDirection),
                    moveComp.RotationSpeed * deltaTime
                );
            }
        }
    }
}