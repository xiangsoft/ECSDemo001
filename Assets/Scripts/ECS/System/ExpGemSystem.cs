using UnityEngine;
using Xiangsoft.Lib.ECS.Attribute;
using Xiangsoft.Lib.ECS.Component;
using Xiangsoft.Lib.ECS.Pool;
using Xiangsoft.Lib.ECS.World;

namespace Xiangsoft.Lib.ECS.System
{
    public class ExpGemSystem : BaseSystem
    {
        // 策划配置项 (实际项目中可以提出来放到 ECSEngine 或主角属性里)
        private const float MAGNETIC_RADIUS_SQR = 3.5f * 3.5f; // 磁吸触发半径的平方
        private const float COLLECT_RADIUS_SQR = 0.5f * 0.5f;  // 吃到经验的判定半径平方
        private const float ACCELERATION = 20f;                // 磁吸时的加速度

        public ExpGemSystem(GameWorld world): base(world)
        {
            requireMask = (ulong)(ComponentMask.Transform | ComponentMask.ExpGem);
        }

        public override void Update(float deltaTime)
        {
            int playerID = ECSEngine.Instance.PlayerEntityID;
            if (playerID == -1)
                return;

            Vector3 playerPos = world.Transforms[playerID].Position;
            EntityStats playerStats = world.StatsBridge[playerID];

            for (int i = 0; i < world.MaxAllocatedID; i++)
            {
                if (!isValidEntity(i)) 
                    continue;

                ref ExpGemComponent gem = ref world.ExpGems[i];
                TransformComponent tComp = world.Transforms[i];

                Vector3 gemPos = tComp.Position;
                Vector3 offset = playerPos - gemPos;
                offset.y = 0;
                float sqrDist = offset.sqrMagnitude;

                if (gem.State == ExpGemState.Idle)
                {
                    // 1. 静止状态：检测是否进入磁吸范围
                    if (sqrDist <= MAGNETIC_RADIUS_SQR)
                    {
                        gem.State = ExpGemState.Attracting;
                        gem.CurrentSpeed = 2f; // 给一个微小的初速度
                    }
                }
                else if (gem.State == ExpGemState.Attracting)
                {
                    // 2. 磁吸状态：越来越快地飞向主角！
                    if (sqrDist <= COLLECT_RADIUS_SQR)
                    {
                        // 吃到了！
                        playerStats.Modify(LongStat.TotalExp, gem.ExpValue);
                        // Debug.Log($"吃到经验球，获得 {gem.ExpValue} 经验！当前总经验：{playerStats.Get(LongStat.TotalExp)}");

                        killGem(entityID: i, tComp);
                        continue;
                    }

                    // 加速飞行算法
                    gem.CurrentSpeed += ACCELERATION * deltaTime;
                    Vector3 moveDir = offset.normalized;
                    tComp.Position += moveDir * gem.CurrentSpeed * deltaTime;
                }
            }
        }

        private void killGem(int entityID, TransformComponent tComp)
        {
            // 抹除 ECS 掩码，物理隐形
            world.EntityMasks[entityID] = (ulong)ComponentMask.None;

            // 回收皮囊
            if (tComp.Transform != null)
            {
                // 这里调用我们将要在下一步写的对象池
                ExpGemPool.Instance.Release(tComp.Transform.gameObject);
                tComp.Transform = null;
            }
        }
    }
}