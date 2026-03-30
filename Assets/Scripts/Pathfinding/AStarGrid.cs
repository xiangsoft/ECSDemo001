using FixedMathSharp;
using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Lib.Pathfinding
{
    public class AStarGrid : MonoBehaviour
    {
        private uint[] gCost;
        private uint[] fCost;
        private int[] parent;
        private bool[] inOpenSet;
        private bool[] inClosedSet;

        private List<int> openSet;

        public void Init()
        {
            int totalCells = BaseGrid.Instance.GridSize.x * BaseGrid.Instance.GridSize.y;

            gCost = new uint[totalCells];
            fCost = new uint[totalCells];
            parent = new int[totalCells];
            inOpenSet = new bool[totalCells];
            inClosedSet = new bool[totalCells];

            openSet = new List<int>(totalCells / 4);
        }

        public bool FindPath(Vector3 startPos, Vector3 targetPos, List<Vector3> path)
        {
            path.Clear(); // 清空旧路径

            if (BaseGrid.Instance == null || BaseGrid.Instance.Cells == null) 
                return false;

            int startIndex = BaseGrid.Instance.GetCellIndexFromWorldPos(startPos);
            int targetIndex = BaseGrid.Instance.GetCellIndexFromWorldPos(targetPos);

            // 如果起点等于终点，或者终点是墙壁，直接不走
            if (startIndex == targetIndex || BaseGrid.Instance.Cells[targetIndex].Cost == 255)
                return false;

            // 初始化所有状态
            for (int i = 0; i < BaseGrid.Instance.Cells.Length; i++)
            {
                gCost[i] = uint.MaxValue;
                fCost[i] = uint.MaxValue;
                inOpenSet[i] = false;
                inClosedSet[i] = false;
            }

            gCost[startIndex] = 0;
            fCost[startIndex] = getHeuristic(startIndex, targetIndex);

            openSet.Clear();
            openSet.Add(startIndex);
            inOpenSet[startIndex] = true;

            while (openSet.Count > 0)
            {
                // 寻找 fCost 最小的节点（简单的线性查找，因为是预分配数组，速度极快）
                int currentIndex = openSet[0];
                int openSetIndex = 0;
                for (int i = 1; i < openSet.Count; i++)
                {
                    int idx = openSet[i];
                    if (fCost[idx] < fCost[currentIndex] || (fCost[idx] == fCost[currentIndex] && gCost[idx] < gCost[currentIndex]))
                    {
                        currentIndex = idx;
                        openSetIndex = i;
                    }
                }

                openSet.RemoveAt(openSetIndex);
                inOpenSet[currentIndex] = false;
                inClosedSet[currentIndex] = true;

                // 找到终点了！回溯生成路径
                if (currentIndex == targetIndex)
                {
                    retracePath(startIndex, targetIndex, path);
                    return true;
                }

                // 检查周围邻居
                checkNeighbors(currentIndex, targetIndex);
            }

            return false; // 找不到路径（比如被墙完全围死了）
        }

        private void checkNeighbors(int currentIndex, int targetIndex)
        {
            Cell cell = BaseGrid.Instance.Cells[currentIndex];
            DirectionFlags flags = cell.AvailableDirections;

            // 为了保持跟流场完全一致的斜角处理，直接复用位掩码逻辑
            processNeighbor(currentIndex, targetIndex, flags, DirectionFlags.Up, BaseGrid.Instance.GridSize.x, 10);
            processNeighbor(currentIndex, targetIndex, flags, DirectionFlags.Down, -BaseGrid.Instance.GridSize.x, 10);
            processNeighbor(currentIndex, targetIndex, flags, DirectionFlags.Left, -1, 10);
            processNeighbor(currentIndex, targetIndex, flags, DirectionFlags.Right, 1, 10);

            processNeighbor(currentIndex, targetIndex, flags, DirectionFlags.UpLeft, BaseGrid.Instance.GridSize.x - 1, 14);
            processNeighbor(currentIndex, targetIndex, flags, DirectionFlags.UpRight, BaseGrid.Instance.GridSize.x + 1, 14);
            processNeighbor(currentIndex, targetIndex, flags, DirectionFlags.DownLeft, -BaseGrid.Instance.GridSize.x - 1, 14);
            processNeighbor(currentIndex, targetIndex, flags, DirectionFlags.DownRight, -BaseGrid.Instance.GridSize.x + 1, 14);
        }

        private void processNeighbor(int currentIndex, int targetIndex, DirectionFlags currentFlags, DirectionFlags checkFlag, int offset, uint moveCost)
        {
            if ((currentFlags & checkFlag) == 0) return;

            int neighborIndex = currentIndex + offset;

            // 对角线防穿墙判定 (复用你流场里的逻辑)
            if (checkFlag == DirectionFlags.UpLeft && BaseGrid.Instance.Cells[currentIndex + BaseGrid.Instance.GridSize.x].Cost == 255 && BaseGrid.Instance.Cells[currentIndex - 1].Cost == 255) 
                return;
            
            if (checkFlag == DirectionFlags.UpRight && BaseGrid.Instance.Cells[currentIndex + BaseGrid.Instance.GridSize.x].Cost == 255 && BaseGrid.Instance.Cells[currentIndex + 1].Cost == 255) 
                return;
            
            if (checkFlag == DirectionFlags.DownLeft && BaseGrid.Instance.Cells[currentIndex - BaseGrid.Instance.GridSize.x].Cost == 255 && BaseGrid.Instance.Cells[currentIndex - 1].Cost == 255) 
                return;
            
            if (checkFlag == DirectionFlags.DownRight && BaseGrid.Instance.Cells[currentIndex - BaseGrid.Instance.GridSize.x].Cost == 255 && BaseGrid.Instance.Cells[currentIndex + 1].Cost == 255) 
                return;

            Cell neighbor = BaseGrid.Instance.Cells[neighborIndex];

            // 如果邻居是墙，或者已经在关闭列表中了，跳过
            if (neighbor.Cost == 255 || inClosedSet[neighborIndex]) 
                return;

            // 叠加移动代价与地形代价 (地形越泥泞，越倾向于绕路)
            uint tentativeGCost = gCost[currentIndex] + moveCost + (uint)(neighbor.Cost * 10);

            if (tentativeGCost < gCost[neighborIndex] || !inOpenSet[neighborIndex])
            {
                gCost[neighborIndex] = tentativeGCost;
                fCost[neighborIndex] = tentativeGCost + getHeuristic(neighborIndex, targetIndex);
                parent[neighborIndex] = currentIndex;

                if (!inOpenSet[neighborIndex])
                {
                    openSet.Add(neighborIndex);
                    inOpenSet[neighborIndex] = true;
                }
            }
        }

        private void retracePath(int startNode, int endNode, List<Vector3> path)
        {
            int currentNode = endNode;
            while (currentNode != startNode)
            {
                path.Add(BaseGrid.Instance.Cells[currentNode].WorldPosition.ToVector3());
                currentNode = parent[currentNode];
            }
            // 因为是从终点往回追溯的，所以需要把列表翻转过来
            path.Reverse();
        }

        // 计算曼哈顿/对角线启发距离
        private uint getHeuristic(int indexA, int indexB)
        {
            int xA = indexA % BaseGrid.Instance.GridSize.x;
            int yA = indexA / BaseGrid.Instance.GridSize.x;
            int xB = indexB % BaseGrid.Instance.GridSize.x;
            int yB = indexB / BaseGrid.Instance.GridSize.x;

            int dstX = Mathf.Abs(xA - xB);
            int dstY = Mathf.Abs(yA - yB);

            // 对角线距离计算：直线走 10，斜线走 14
            if (dstX > dstY)
                return (uint)(14 * dstY + 10 * (dstX - dstY));

            return (uint)(14 * dstX + 10 * (dstY - dstX));
        }
    }
}