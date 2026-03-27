using System;

namespace Xiangsoft.Lib.Pathfinding
{
    [Flags]
    public enum DirectionFlags : byte
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
        UpLeft = 16,
        UpRight = 32,
        DownLeft = 64,
        DownRight = 128
    }
}
