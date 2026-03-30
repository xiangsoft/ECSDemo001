using TrueSync;

namespace Xiangsoft.Lib.Pathfinding
{
    public struct Cell
    {
        /// <summary>
        /// 世界坐标
        /// </summary>
        public TSVector WorldPosition;

        /// <summary>
        /// 格子坐标
        /// </summary>
        public TSVector2 GridPosition;

        /// <summary>
        /// 代价
        /// </summary>
        public int Cost;

        /// <summary>
        /// 积分场
        /// </summary>
        public uint BestCost;

        /// <summary>
        /// 流场方向
        /// </summary>
        public TSVector2 BestDirection;

        /// <summary>
        /// 允许的邻居方向
        /// </summary>
        public DirectionFlags AvailableDirections;
    }
}
