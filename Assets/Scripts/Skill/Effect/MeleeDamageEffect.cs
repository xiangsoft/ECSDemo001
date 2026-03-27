using UnityEngine;
using Xiangsoft.Lib.ECS.Attribute;

namespace Xiangsoft.Game.Skill
{
    [CreateAssetMenu(fileName = "NewSkillEffect", menuName = "技能/技能效果/近战伤害")]
    public class MeleeDamageEffect : SkillEffect
    {
        public float DamageMultiplier = 1.0f; // 造成自身攻击力 100% 的伤害

        public override void Execute(SkillContext context)
        {
            if (context.Target == null || context.Target.IsDead) 
                return;

            // 获取施法者的攻击力
            int casterAttack = context.Caster.Get(IntStat.Attack);
            int finalDamage = Mathf.CeilToInt(casterAttack * DamageMultiplier);
            
            // 对目标造成伤害
            context.Target.TakeDamage(finalDamage);
        }
    }
}