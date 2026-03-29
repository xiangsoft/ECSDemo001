using UnityEngine;
using Xiangsoft.Lib.ECS.Component;
using Xiangsoft.Lib.ECS.System;
using Xiangsoft.Lib.ECS.World;

namespace Xiangsoft.Lib.ECS.AI
{
    public class MobAISystem : BaseSystem
    {
        public MobAISystem(GameWorld world) : base(world)
        {
            requireMask = (ulong)ComponentMask.AI;
        }

        public override void Update(float deltaTime)
        {
            int playerID = ECSEngine.Instance.PlayerEntityID;

            if (playerID == -1)
                return;

            Vector3 playerPos = world.Transforms[playerID].Position;

            for (int i = 0; i < world.MaxAllocatedID; i++)
            {
                if (!isValidEntity(i))
                    continue;

                ref AIComponent ai = ref world.AIs[i];
                Vector3 pos = world.Transforms[i].Position;

                float sqrDist = (playerPos - pos).sqrMagnitude;

                if (ai.IsRanged)
                {
                    // --- 远程怪的聪明逻辑 ---
                    if (sqrDist < ai.FleeRange * ai.FleeRange)
                        ai.CurrentState = AIState.Flee; // 太近了！快跑！
                    else if (sqrDist <= ai.AttackRange * ai.AttackRange)
                        ai.CurrentState = AIState.Attack; // 完美射程，停下攻击！
                    else
                        ai.CurrentState = AIState.Chase; // 太远了，走流场追上去！
                }
                else
                {
                    // --- 近战怪的莽夫逻辑 ---
                    if (sqrDist <= ai.AttackRange * ai.AttackRange)
                        ai.CurrentState = AIState.Attack; // 够得着，直接咬！
                    else
                        ai.CurrentState = AIState.Chase; // 够不着，死命追！
                }
            }
        }
    }
}