using FixedMathSharp;

namespace Xiangsoft.Lib.Pathfinding
{
    public struct Cell
    {
        /// <summary>
        /// 世界坐标
        /// </summary>
        public Vector3d WorldPosition;

        /// <summary>
        /// 格子坐标
        /// </summary>
        public Vector2d GridPosition;

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
        public Vector2d BestDirection;

        /// <summary>
        /// 允许的邻居方向
        /// </summary>
        public DirectionFlags AvailableDirections;
    }
}
