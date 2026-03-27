using UnityEngine;
using Xiangsoft.Lib.ECS.Attribute;

namespace Xiangsoft.Game.Skill
{
    [CreateAssetMenu(fileName = "NewSkillEffect", menuName = "技能/技能效果/治疗")]
    public class HealEffect : SkillEffect
    {
        public int HealAmount;
        public bool IsPercentage; // 是否按百分比治疗
        public bool IsTarget; // 是否治疗目标（否则治疗施法者）

        public override void Execute(SkillContext context)
        {
            EntityStats stats = IsTarget ? context.Target : context.Caster;
            if (stats == null || stats.IsDead)
                return;

            int healAmount = HealAmount;
            if (IsPercentage)
                healAmount = Mathf.CeilToInt(stats.Get(IntStat.MaxHealth) * HealAmount / 100f);

            healAmount += stats.Get(IntStat.CurrentHealth);
            stats.Modify(IntStat.CurrentHealth, healAmount);
        }
    }
}