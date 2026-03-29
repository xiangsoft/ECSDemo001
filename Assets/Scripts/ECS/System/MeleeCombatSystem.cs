using UnityEngine;
using Xiangsoft.Game.Skill;
using Xiangsoft.Lib.ECS.Attribute;
using Xiangsoft.Lib.ECS.Component;
using Xiangsoft.Lib.ECS.World;

namespace Xiangsoft.Lib.ECS.System
{
    public class MeleeCombatSystem : BaseSystem
    {
        public MeleeCombatSystem(GameWorld world) : base(world)
        {
            requireMask = (ulong)ComponentMask.AI;
        }

        public override void Update(float deltaTime)
        {
            int playerID = ECSEngine.Instance.PlayerEntityID;

            if (playerID == -1)
                return;

            Vector3 playerPos = world.Transforms[playerID].Position;
            EntityStats playerStats = world.StatsBridge[playerID];

            if (playerStats == null || playerStats.IsDead)
                return;

            for (int i = 0; i < world.MaxAllocatedID; i++)
            {
                if (playerID == i)
                    continue;

                if (!isValidEntity(i))
                    continue;

                if (world.AIs[i].CurrentState != AIState.Attack)
                    continue;

                SkillController skillController = world.SkillBridge[i];
                if (skillController == null)
                    continue;

                SkillInstance basicSkill = skillController.GetBasicSkill();

                if (basicSkill == null || !basicSkill.IsReady)
                    continue;

                skillController.TryCastSkill(0, playerStats, playerPos);
            }
        }
    }
}