using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Game.Skill
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "技能/技能数据")]
    public class SkillData : ScriptableObject
    {
        [Header("基础信息")]
        public string SkillID;          // 技能唯一标识符
        public string SkillName;        // 技能名称
        public float CastRange = 2f;    // 施法距离 (近战通常是 1-2，远程可能是 10)

        [Header("冷却设置 (Cooldown)")]
        public float Cooldown = 1.5f;   // 技能自身的冷却时间

        [Header("公共冷却设置 (GCD)")]
        public bool RespectsGCD = true; // 是否受 GCD 限制？(比如保命技能可以无视 GCD 瞬发)
        public bool TriggersGCD = true; // 释放该技能后，是否会触发 GCD？
        public float GCDTime = 1.0f;    // 触发的 GCD 长度 (MMO 标准通常是 1.0s - 1.5s)

        [Header("技能效果")]
        public List<SkillEffect> Effects = new List<SkillEffect>();
    }
}