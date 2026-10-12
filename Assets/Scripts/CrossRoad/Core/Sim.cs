using System;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// 确定性模拟核心（lockstep 心脏）：固定逻辑 tick + 对局种子随机源。
    /// ------------------------------------------------------------------
    /// · TickRate：逻辑帧率（20Hz）。所有世界推进（车流/火车/浮木/死亡判定）
    ///   只在逻辑 tick 里发生，与渲染帧率无关——各端同 tick 数即同世界。
    /// · 渲染层直接读 tick 后的位置（20Hz 的风格化移动足够平滑；
    ///   需要更顺滑时再加插值，不影响确定性）。
    /// · Rng：对局种子随机源（System.Random，仅客户端间需要一致——
    ///   服务器不跑世界模拟，无跨运行时一致性风险）。
    ///   单机也走同一管道（种子随机生成），行为与联机一致。
    /// · 世界生成/回收的驱动线：单机由相机推进；联机由全玩家最前进度线推进
    ///   （RemotePlayer 落地后切换）——保证各端 RNG 消耗顺序一致。
    /// </summary>
    public static class Sim
    {
        /// <summary>逻辑帧率（每秒 tick 数）。</summary>
        public const int TickRate = 20;

        /// <summary>单逻辑 tick 的固定时长（秒）。</summary>
        public const float TickDelta = 1f / TickRate;

        /// <summary>当前逻辑 tick 计数（确定性时间；对局开始时清零）。</summary>
        public static int Tick { get; private set; }

        /// <summary>对局种子随机源：车道序列/车辆/火车全部从这里取数。null = 未开局（不得使用）。</summary>
        public static Random Rng { get; private set; }

        /// <summary>每逻辑 tick 广播一次（dt = TickDelta）；世界系统订阅此事件推进。</summary>
        public static event Action<float> Stepped;

        private static float accumulator;

        /// <summary>开局/重开：注入种子并清零 tick。单机用随机种子，联机用服务器广播的种子。</summary>
        public static void Begin(int seed)
        {
            Rng = new Random(seed);
            Tick = 0;
            accumulator = 0f;
        }

        /// <summary>单机便捷入口：随机种子开局。</summary>
        public static void BeginRandom() => Begin(Guid.NewGuid().GetHashCode());

        /// <summary>渲染帧驱动：累积真实时间，按固定步长推进整数个逻辑 tick。</summary>
        public static void Advance(float realDelta)
        {
            if (Rng == null) return;   // 未开局：世界静止（菜单态）

            // 防卡帧螺旋：单帧最多补 5 个 tick（丢步优于雪崩）
            if (realDelta > 5f * TickDelta) realDelta = 5f * TickDelta;

            accumulator += realDelta;
            while (accumulator >= TickDelta)
            {
                accumulator -= TickDelta;
                Tick++;
                Stepped?.Invoke(TickDelta);
            }
        }
    }
}
