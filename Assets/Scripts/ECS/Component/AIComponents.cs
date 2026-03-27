namespace Xiangsoft.Lib.ECS.Component
{
    public enum AIState : byte
    {
        None = 0,
        Chase = 1,
        Attack = 2,
        Flee = 3,
    }

    public struct AIComponent
    {
        public AIState CurrentState;

        /// <summary>
        /// 是否是远程拉扯怪？
        /// </summary>
        public bool IsRanged;

        /// <summary>
        /// 进入攻击状态的距离
        /// </summary>
        public float AttackRange;

        /// <summary>
        /// 触发逃跑的距离 (贴脸距离)
        /// </summary>
        public float FleeRange;
    }
}