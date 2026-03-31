using FixedMathSharp;
using System.Collections.Generic;

namespace Xiangsoft.Lib.ECS.Grid
{
    public class SpatialHashECSGrid
    {
        private float cellSize;
        private int width;
        private int height;
        
        private Fixed64 fixedCellSize;
        private Fixed64 fixedWidthIndex;
        private Fixed64 fixedHeightIndex;

        // 核心修改：一维数组里装的是 List<int>，绝对的纯数据！
        private List<int>[] cells;

        public SpatialHashECSGrid(int width, int height, float cellSize)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;

            fixedCellSize = (Fixed64)cellSize;
            fixedWidthIndex = (Fixed64)(width - 1);
            fixedHeightIndex = (Fixed64)(height - 1);

            int length = width * height;
            cells = new List<int>[length];
            for (int i = 0; i < length; i++)
            {
                // 预分配容量，假设每个格子里最多挤 32 个怪，彻底消灭运行时 GC
                cells[i] = new List<int>(256);
            }
        }

        private int GetIndex(int x, int y)
        {
            return y * width + x;
        }

        // 1. 每帧清空网格 (List<int>.Clear() 极快且 0 GC)
        public void Clear()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].Clear();
            }
        }

        // 2. 核心修改：插入实体时，只传入它的 ID 和坐标
        public void Insert(int entityId, Vector3d pos)
        {
            int x = FixedMath.Clamp(FixedMath.Floor(pos.x / fixedCellSize), Fixed64.Zero, fixedWidthIndex).CeilToInt();
            int y = FixedMath.Clamp(FixedMath.Floor(pos.z / fixedCellSize), Fixed64.Zero, fixedHeightIndex).CeilToInt();

            cells[GetIndex(x, y)].Add(entityId);
        }

        // 3. 核心修改：查找邻居时，返回的也是实体 ID 的列表
        public void FindNeighbors(Vector3d worldPos, List<int> results)
        {
            results.Clear();

            int gridX = FixedMath.Clamp(FixedMath.Floor(worldPos.x / fixedCellSize), Fixed64.Zero, fixedWidthIndex).CeilToInt();
            int gridY = FixedMath.Clamp(FixedMath.Floor(worldPos.z / fixedCellSize), Fixed64.Zero, fixedHeightIndex).CeilToInt();

            // 遍历自己所在的格子，以及周围 8 个格子 (3x3 范围)
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int checkX = gridX + dx;
                    int checkY = gridY + dy;

                    if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                    {
                        List<int> cellEntities = cells[GetIndex(checkX, checkY)];
                        for (int i = 0; i < cellEntities.Count; i++)
                        {
                            if (results.Count >= results.Capacity)
                                break;

                            results.Add(cellEntities[i]);
                        }
                    }
                }
            }
        }
    }
}