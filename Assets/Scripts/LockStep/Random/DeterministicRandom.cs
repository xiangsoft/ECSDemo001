namespace Xiangsoft.Lib.LockStep
{
    /// <summary>
    /// 纯数学的确定性随机数生成器 (XorShift算法，极其高效且 0 GC)
    /// </summary>
    public static class DeterministicRandom
    {
        private static uint state = 1; // 随机数种子

        // 服务器在游戏开始时下发统一的种子！
        public static void SetSeed(uint seed)
        {
            if (seed == 0) seed = 1; // XorShift 种子不能为 0
            state = seed;
        }

        // 生成一个 uint 随机数
        private static uint Next()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        // 返回 [0, 1) 的浮点数 (替代 Random.value)
        public static float value
        {
            get { return (Next() & 0xFFFFFF) / (float)0x1000000; }
        }

        // 返回 [min, max) 的浮点数 (替代 Random.Range)
        public static float Range(float min, float max)
        {
            return min + (max - min) * value;
        }

        // 返回 [min, max) 的整数 (替代 Random.Range)
        public static int Range(int min, int max)
        {
            if (min == max) return min;
            return min + (int)(Next() % (max - min));
        }

        // 返回圆内随机点 (替代 Random.insideUnitCircle)
        public static UnityEngine.Vector2 insideUnitCircle
        {
            get
            {
                float angle = value * UnityEngine.Mathf.PI * 2f;
                float r = UnityEngine.Mathf.Sqrt(value); // 取平方根保证在圆内均匀分布
                return new UnityEngine.Vector2(r * UnityEngine.Mathf.Cos(angle), r * UnityEngine.Mathf.Sin(angle));
            }
        }
    }
}