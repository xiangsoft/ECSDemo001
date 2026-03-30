using TrueSync;

namespace Xiangsoft.Lib.ECS.Component
{
    public enum ExpGemState : byte
    {
        Idle = 0,       // 掉落在地上静止
        Attracting = 1  // 正在被主角吸过去
    }

    public struct ExpGemComponent
    {
        public ExpGemState State;
        public int ExpValue;        // 蕴含的经验值
        public FP CurrentSpeed;  // 当前飞行速度 (用于实现加速吸附)
    }
}