using System;
using System.Collections.Generic;
using UnityEngine;
using Xiangsoft.Lib.ECS.Component;
using Xiangsoft.Lib.ECS.Grid;
using Xiangsoft.Lib.ECS.Pool;
using Xiangsoft.Lib.ECS.World;

namespace Xiangsoft.Lib.ECS.System
{
    public class ProjectileSystem : BaseSystem
    {
        private SpatialHashECSGrid spatialGrid;

        // 静态缓冲池，0 GC 接收网格查询结果
        private static List<int> hitBuffer = new List<int>(32);

        public ProjectileSystem(GameWorld world, SpatialHashECSGrid spatialGrid) : base(world)
        {
            this.spatialGrid = spatialGrid;

            requireMask = (ulong)(ComponentMask.Transform | ComponentMask.Projectile);
        }

        public override void Update(float deltaTime)
        {
            // 批量遍历所有实体，寻找激活的子弹
            for (int i = 0; i < world.MaxAllocatedID; i++)
            {
                if (!isValidEntity(i))
                    continue;

                // 使用 ref 获取引用，这样我们修改结构体的数据才会生效！
                ref ProjectileComponent proj = ref world.Projectiles[i];
                if (!proj.IsActive)
                    continue;

                TransformComponent tComp = world.Transforms[i];

                proj.LifeTimer += deltaTime;
                // --- 1. 回旋镖轨迹控制 ---
                if (proj.IsBoomerang && !proj.IsReturning && proj.LifeTimer >= proj.MaxLifetime / 2f)
                {
                    proj.IsReturning = true;
                    // 飞回阶段：清空命中记忆
                    proj.CurrentHitCount = 0;
                    for (int k = 0; k < proj.HitHistory.Length; k++)
                        proj.HitHistory[k] = -1;
                }

                if (proj.IsReturning && world.Transforms[proj.CasterID].Transform != null)
                {
                    Vector3 casterPos = world.Transforms[proj.CasterID].Position;
                    Vector3 returnDir = casterPos - tComp.Position;
                    returnDir.y = 0;

                    if (returnDir.sqrMagnitude < 1f)
                    {
                        killProjectile(ref proj, tComp); // 回到手中，销毁
                        continue;
                    }

                    proj.Direction = Vector3.Slerp(proj.Direction, returnDir.normalized, deltaTime * 5f).normalized;
                }

                if (proj.LifeTimer >= proj.MaxLifetime)
                {
                    killProjectile(ref proj, tComp); // 寿命耗尽，销毁
                    continue;
                }

                // --- 2. 飞行位移计算 ---
                tComp.Position += proj.Direction * proj.Speed * deltaTime;

                if (proj.Direction != Vector3.zero)
                {
                    tComp.Rotation = Quaternion.LookRotation(proj.Direction);
                }

                // --- 3. 空间哈希防抖碰撞检测 (O(1) 极速索敌) ---
                spatialGrid.FindNeighbors(tComp.Position, hitBuffer);
                float sqrHitRadius = proj.HitRadius * proj.HitRadius;

                for (int j = 0; j < hitBuffer.Count; j++)
                {
                    int targetID = hitBuffer[j];

                    // 忽略自己、忽略已经死亡的实体、忽略不是怪物的实体
                    if (targetID == proj.CasterID || world.StatsBridge[targetID] == null)
                        continue;

                    Vector3 offset = tComp.Position - world.Transforms[targetID].Position;
                    offset.y = 0;

                    // 纯数学撞击判定！
                    if (offset.sqrMagnitude <= sqrHitRadius)
                    {
                        // 检查是否已经打过
                        bool alreadyHit = false;

                        for (int k = 0; k < proj.CurrentHitCount; k++)
                        {
                            if (proj.HitHistory[k] == targetID)
                            {
                                alreadyHit = true;
                                break;
                            }
                        }

                        if (alreadyHit)
                            continue;

                        // ==========================================
                        // 命中！通过 Hybrid 桥梁呼叫 OOP 扣血逻辑
                        // ==========================================
                        world.StatsBridge[targetID].TakeDamage(proj.Damage);

                        // 记入历史
                        if (proj.CurrentHitCount < proj.HitHistory.Length)
                        {
                            proj.HitHistory[proj.CurrentHitCount] = targetID;
                        }
                        proj.CurrentHitCount++;

                        if (proj.CurrentHitCount >= proj.MaxPiercing)
                        {
                            killProjectile(ref proj, tComp); // 穿透次数耗尽，销毁
                            break;
                        }
                    }
                }
            }
        }

        // 在 ProjectileSystem 类中新增这个辅助方法：
        private void killProjectile(ref ProjectileComponent proj, TransformComponent tComp)
        {
            proj.IsActive = false; // 逻辑死亡

            // 如果它身上还挂着皮囊，回收皮囊
            if (tComp.Transform != null)
            {
                ProjectilePool.Instance.Release(tComp.Transform.gameObject);
                tComp.Transform = null; // 切断物理世界的羁绊
            }
        }
    }
}