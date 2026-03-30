using TrueSync;

namespace Xiangsoft.Lib.ECS.Component
{
    public struct ProjectileComponent
    {
        public bool IsActive;

        // 基础属性
        public int CasterID;        // 施法者的实体 ID
        public TSVector Direction;   // 飞行方向
        public FP Speed;
        public int Damage;
        public bool IsCrit;
        public FP HitRadius;

        // 高级机制 (穿透与回旋)
        public int MaxPiercing;
        public bool IsBoomerang;
        public FP MaxLifetime;

        // 运行时状态
        public FP LifeTimer;
        public int CurrentHitCount;
        public bool IsReturning;

        // 0 GC 命中记忆：存储被击中过的实体 ID。
        // 因为 C# struct 不能直接包含定长数组，我们存一个引用，
        // 但这个数组会在 GameWorld 初始化时一次性分配好，永不产生 GC！
        public int[] HitHistory;
    }
}