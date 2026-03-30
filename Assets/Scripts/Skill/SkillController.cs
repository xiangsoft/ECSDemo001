using System.Collections.Generic;
using TrueSync;
using UnityEngine;
using Xiangsoft.Lib.ECS.Attribute;

namespace Xiangsoft.Game.Skill
{
    [RequireComponent(typeof(EntityStats))]
    public class SkillController : MonoBehaviour
    {
        [Header("出生的默认技能")]
        // 在 Inspector 里把 ScriptableObject 拖进来
        // 小怪可能只拖一个“普通攻击”，主角可以拖一排技能
        public List<SkillData> initialSkills;

        private List<SkillInstance> skills = new List<SkillInstance>();

        private float currentGCD = 0f;

        private void Awake()
        {
            foreach (SkillData skill in initialSkills)
            {
                skills.Add(new SkillInstance(skill));
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (currentGCD > 0f)
                currentGCD -= deltaTime;

            if (currentGCD < 0f)
                currentGCD = 0f;

            foreach (SkillInstance skill in skills)
            {
                skill.UpdateCD(deltaTime);
            }
        }

        public void ResetSkills()
        {
            currentGCD = 0f;
            foreach (SkillInstance skill in skills)
            {
                skill.CurrentCD = 0f;
            }
        }

        public void TryCastAll(EntityStats target = null, TSVector targetPos = default)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                TryCastSkill(i, target, targetPos);
            }
        }

        public bool TryCastSkill(int skillIndex, EntityStats target = null, TSVector targetPos = default)
        {
            if (skillIndex < 0 || skillIndex >= skills.Count)
                return false;

            SkillInstance skill = skills[skillIndex];

            if (!skill.IsReady)
                return false;

            if (skill.Data.RespectsGCD && currentGCD > 0)
                return false;

            SkillContext skillContext = new SkillContext
            {
                Caster = GetComponent<EntityStats>(),
                Target = target,
                TargetPosition = targetPos == default ? transform.position.ToTSVector() : targetPos
            };

            foreach (SkillEffect effect in skill.Data.Effects)
            {
                if (effect == null)
                    continue;

                effect.Execute(skillContext);
            }

            skill.ResetCD();

            if (skill.Data.TriggersGCD)
                currentGCD = skill.Data.GCDTime;

            return true;
        }

        public SkillInstance GetBasicSkill()
        {
            if (skills.Count > 0)
                return skills[0];
            else
                return null;
        }
    }
}