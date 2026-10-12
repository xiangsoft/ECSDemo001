using System.Collections.Generic;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// 全玩家进度登记表：世界生成/回收线的统一驱动源。
    /// ------------------------------------------------------------------
    /// 单机：只有本地玩家注册 → 进度线 = 本地玩家位置（原相机线等价）。
    /// 联机：本地 + 远程玩家全部注册 → 进度线 = 全员最前 Z。
    /// 确定性关键：hop 流经服务器定序后各端一致 → 各端玩家位置一致 →
    /// 进度线一致 → 车道生成/回收的 RNG 消耗顺序一致 → 世界完全相同。
    /// </summary>
    public static class PlayerProgress
    {
        private static readonly List<IProgressReporter> reporters = new List<IProgressReporter>();

        /// <summary>当前进度线：所有已注册玩家的最大 Z（无人时为 0）。</summary>
        public static int FrontZ
        {
            get
            {
                int front = 0;
                for (int i = 0; i < reporters.Count; i++)
                {
                    int z = reporters[i].CurrentZ;
                    if (z > front) front = z;
                }
                return front;
            }
        }

        public static void Register(IProgressReporter reporter)
        {
            if (!reporters.Contains(reporter)) reporters.Add(reporter);
        }

        public static void Unregister(IProgressReporter reporter) => reporters.Remove(reporter);

        /// <summary>对局结束清空（玩家实体销毁时各自注销，这里兜底）。</summary>
        public static void Clear() => reporters.Clear();
    }

    /// <summary>玩家实体的进度上报接口（本地与远程鸡都实现）。</summary>
    public interface IProgressReporter
    {
        /// <summary>玩家当前所在格 Z（世界生成线取全员的 max）。</summary>
        int CurrentZ { get; }
    }
}
