using FixedMathSharp;
using UnityEngine;
using Xiangsoft.Lib.ECS.Attribute;

namespace Xiangsoft.Game.Skill
{
    public struct SkillContext
    {
        public EntityStats Caster;
        public EntityStats Target;
        public Vector3d TargetPosition;
    }

    public abstract class SkillEffect : ScriptableObject
    {
        public abstract void Execute(SkillContext context);
    }
}