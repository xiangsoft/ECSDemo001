using FixedMathSharp;
using UnityEngine;
using Xiangsoft.Lib.ECS.Attribute;
using Xiangsoft.Lib.LockStep;

namespace Xiangsoft.Game.Skill
{
    [CreateAssetMenu(fileName = "NewSkillEffect", menuName = "技能/技能效果/近战伤害")]
    public class MeleeDamageEffect : SkillEffect
    {
        public Fixed64 DamageMultiplier = Fixed64.One; // 造成自身攻击力 100% 的伤害

        public override void Execute(SkillContext context)
        {
            if (context.Target == null || context.Target.IsDead)
                return;

            // ★ 新增：获取暴击属性
            Fixed64 critRate = context.Caster.Get(FloatStat.CritRate);
            // 如果策划没配倍率，默认给 2.0 倍
            Fixed64 critMult = context.Caster.Get(FloatStat.CritMultiplier) <= Fixed64.Zero ? Fixed64.Two : context.Caster.Get(FloatStat.CritMultiplier);

            bool isCrit = RandomManager.Instance.Value < critRate;

            // 获取施法者的攻击力
            int casterAttack = context.Caster.Get(IntStat.Attack);
            int finalDamage = ((int)FixedMath.Ceiling(casterAttack * DamageMultiplier * (isCrit ? critMult : Fixed64.One)));

            // 对目标造成伤害
            context.Target.TakeDamage(finalDamage, isCrit);
        }
    }
}