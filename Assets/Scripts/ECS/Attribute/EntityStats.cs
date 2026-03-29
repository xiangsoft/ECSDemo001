using System;
using System.Collections.Generic;
using UnityEngine;
using Xiangsoft.Game.UI;

namespace Xiangsoft.Lib.ECS.Attribute
{
    public class EntityStats : MonoBehaviour
    {
        [Header("初始属性配置 (Inspector)")]
        public List<FloatStatConfig> initialFloatStats = new List<FloatStatConfig>();
        public List<IntStatConfig> initialIntStats = new List<IntStatConfig>();
        public List<LongStatConfig> initialLongStats = new List<LongStatConfig>();
        public List<StringStatConfig> initialStringStats = new List<StringStatConfig>();

        // --- 运行时的 0 GC 高性能字典 ---
        // 浮点型
        private Dictionary<FloatStat, float> baseFloatStats = new Dictionary<FloatStat, float>();
        private Dictionary<FloatStat, float> currentFloatStats = new Dictionary<FloatStat, float>();
        // 整型
        private Dictionary<IntStat, int> baseIntStats = new Dictionary<IntStat, int>();
        private Dictionary<IntStat, int> currentIntStats = new Dictionary<IntStat, int>();
        // 长整型
        private Dictionary<LongStat, long> baseLongStats = new Dictionary<LongStat, long>();
        private Dictionary<LongStat, long> currentLongStats = new Dictionary<LongStat, long>();
        // 字符串
        private Dictionary<StringStat, string> baseStringStats = new Dictionary<StringStat, string>();
        private Dictionary<StringStat, string> currentStringStats = new Dictionary<StringStat, string>();

        public event Action<EntityStats> OnDeath;

        public bool IsDead
        {
            get
            {
                return Get(IntStat.CurrentHealth) <= 0;
            }
        }

        private void Awake()
        {
            initializeStats();
        }

        private void initializeStats()
        {
            foreach (var config in initialFloatStats)
            {
                baseFloatStats[config.Type] = config.BaseValue; currentFloatStats[config.Type] = config.BaseValue;
            }

            foreach (var config in initialIntStats)
            {
                baseIntStats[config.Type] = config.BaseValue; currentIntStats[config.Type] = config.BaseValue;
            }

            foreach (var config in initialLongStats)
            {
                baseLongStats[config.Type] = config.BaseValue; currentLongStats[config.Type] = config.BaseValue;
            }

            foreach (var config in initialStringStats)
            {
                baseStringStats[config.Type] = config.BaseValue; currentStringStats[config.Type] = config.BaseValue;
            }
        }

        public void ResetStats()
        {
            // 复活时，用基础属性重置当前属性
            foreach (var kvp in baseFloatStats)
                currentFloatStats[kvp.Key] = kvp.Value;

            foreach (var kvp in baseIntStats)
                currentIntStats[kvp.Key] = kvp.Value;

            foreach (var kvp in baseLongStats)
                currentLongStats[kvp.Key] = kvp.Value;

            foreach (var kvp in baseStringStats)
                currentStringStats[kvp.Key] = kvp.Value;

            // 特殊处理：回满血
            currentIntStats[IntStat.CurrentHealth] = GetBase(IntStat.MaxHealth);
        }

        public float Get(FloatStat type)
        {
            return currentFloatStats.TryGetValue(type, out float val) ? val : 0f;
        }

        public int Get(IntStat type)
        {
            return currentIntStats.TryGetValue(type, out int val) ? val : 0;
        }

        public long Get(LongStat type)
        {
            return currentLongStats.TryGetValue(type, out long val) ? val : 0;
        }

        public string Get(StringStat type)
        {
            return baseStringStats.TryGetValue(type, out string val) ? val : string.Empty;
        }

        public float GetBase(FloatStat type)
        {
            return baseFloatStats.TryGetValue(type, out float val) ? val : 0f;
        }

        public int GetBase(IntStat type)
        {
            return baseIntStats.TryGetValue(type, out int val) ? val : 0;
        }

        public long GetBase(LongStat type)
        {
            return baseLongStats.TryGetValue(type, out long val) ? val : 0;
        }

        public string GetBase(StringStat type)
        {
            return baseStringStats.TryGetValue(type, out string val) ? val : string.Empty;
        }

        public void Set(FloatStat type, float value)
        {
            currentFloatStats[type] = value;
        }

        public void Set(IntStat type, int value)
        {
            currentIntStats[type] = value;

            if (type == IntStat.CurrentHealth && value <= 0)
            {
                currentIntStats[IntStat.CurrentHealth] = 0;
                die();
            }
        }

        public void Set(LongStat type, long value)
        {
            currentLongStats[type] = value;
        }

        public void Set(StringStat type, string value)
        {
            currentStringStats[type] = value;
        }

        public void Modify(FloatStat type, float delta)
        {
            Set(type, Get(type) + delta);
        }

        public void Modify(IntStat type, int delta)
        {
            Set(type, Get(type) + delta);
        }

        public void Modify(LongStat type, long delta)
        {
            Set(type, Get(type) + delta);
        }

        /// <summary>
        /// 战斗专用扣血接口
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <param name="isCrit"></param>
        public void TakeDamage(int damageAmount, bool isCrit = false)
        {
            if (IsDead)
                return;

            int defense = Get(IntStat.Defense);
            int actualDamage = Mathf.Max(1, damageAmount - defense);
            Modify(IntStat.CurrentHealth, -actualDamage);

            if (DamageTextManager.Instance == null)
                return;

            DamageTextManager.Instance.ShowDamage(actualDamage, transform.position, isCrit);
        }

        private void die()
        {
            OnDeath?.Invoke(this);
        }
    }
}