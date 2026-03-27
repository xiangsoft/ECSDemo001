using System;

namespace Xiangsoft.Lib.ECS.Attribute
{
    // 浮点型属性（主要用于带小数的战斗数值、速度等）
    public enum FloatStat
    {
        None,
        MoveSpeed,
        RotationSpeed,
        AttackRange,
        CritRate,
        SeparationRadius,
        SeparationWeight,
        WobbleSpeed,
        WobbleStrength,
    }

    // 整型属性（主要用于等级、阵营、阶段等离散数值）
    public enum IntStat
    {
        None,
        UnitID,
        MaxHealth,
        CurrentHealth,
        Level,
        Attack,
        Defense,
        FactionID,   // 阵营ID（比如 0是玩家，1是怪物）
        DropGold     // 击杀掉落的金币数
    }

    // 长整型属性（主要用于可能超过21亿的巨大数值）
    public enum LongStat
    {
        None,
        TotalExp,    // 总经验值
        TotalDamage  // 累计造成伤害
    }

    // 字符串属性（主要用于显示、名字、预制体路径等）
    public enum StringStat
    {
        None,
        EntityName,
        Description,
        PrefabPath
    }

    [Serializable]
    public struct FloatStatConfig
    {
        public FloatStat Type;
        public float BaseValue;
    }

    [Serializable]
    public struct IntStatConfig
    {
        public IntStat Type;
        public int BaseValue;
    }

    [Serializable]
    public struct LongStatConfig
    {
        public LongStat Type;
        public long BaseValue;
    }

    [Serializable]
    public struct StringStatConfig
    {
        public StringStat Type;
        public string BaseValue;
    }
}