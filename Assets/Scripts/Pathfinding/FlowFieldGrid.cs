using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Lib.Pathfinding
{
    public class FlowFieldGrid : MonoBehaviour
    {
        private Vector2 upLeft = new Vector2(-1, 1).normalized;
        private Vector2 upRight = new Vector2(1, 1).normalized;
        private Vector2 downLeft = new Vector2(-1, -1).normalized;
        private Vector2 downRight = new Vector2(1, -1).normalized;

        private Queue<int> indicesToCheck = null;

        public void GenerateIntegrationField(int targetIndex)
        {
            if (indicesToCheck == null)
                indicesToCheck = new Queue<int>(BaseGrid.Instance.Cells.Length);

            if (BaseGrid.Instance.Cells[targetIndex].Cost == 255)
                return;

            for (int i = 0; i < BaseGrid.Instance.Cells.Length; i++)
            {
                BaseGrid.Instance.Cells[i].BestCost = uint.MaxValue;
            }

            BaseGrid.Instance.Cells[targetIndex].BestCost = 0;

            indicesToCheck.Clear();
            indicesToCheck.Enqueue(targetIndex);

            while (indicesToCheck.Count > 0)
            {
                int currentIndex = indicesToCheck.Dequeue();
                Cell currentCell = BaseGrid.Instance.Cells[currentIndex];

                if ((currentCell.AvailableDirections & DirectionFlags.Up) != 0)
                {
                    int index = currentIndex + BaseGrid.Instance.GridSize.x;
                    Cell neighbor = BaseGrid.Instance.Cells[index];

                    if (neighbor.Cost == 255)
                        continue;

                    uint newCost = (uint)(currentCell.BestCost + neighbor.Cost * 10);

                    if (newCost < neighbor.BestCost)
                    {
                        BaseGrid.Instance.Cells[index].BestCost = newCost;
                        indicesToCheck.Enqueue(index);
                    }
                }

                if ((currentCell.AvailableDirections & DirectionFlags.Down) != 0)
                {
                    int index = currentIndex - BaseGrid.Instance.GridSize.x;
                    Cell neighbor = BaseGrid.Instance.Cells[index];

                    if (neighbor.Cost == 255)
                        continue;

                    uint newCost = (uint)(currentCell.BestCost + neighbor.Cost * 10);

                    if (newCost < neighbor.BestCost)
                    {
                        BaseGrid.Instance.Cells[index].BestCost = newCost;
                        indicesToCheck.Enqueue(index);
                    }
                }

                if ((currentCell.AvailableDirections & DirectionFlags.Left) != 0)
                {
                    int index = currentIndex - 1;
                    Cell neighbor = BaseGrid.Instance.Cells[index];

                    if (neighbor.Cost == 255)
                        continue;

                    uint newCost = (uint)(currentCell.BestCost + neighbor.Cost * 10);

                    if (newCost < neighbor.BestCost)
                    {
                        BaseGrid.Instance.Cells[index].BestCost = newCost;
                        indicesToCheck.Enqueue(index);
                    }
                }

                if ((currentCell.AvailableDirections & DirectionFlags.Right) != 0)
                {
                    int index = currentIndex + 1;
                    Cell neighbor = BaseGrid.Instance.Cells[index];

                    if (neighbor.Cost == 255)
                        continue;

                    uint newCost = (uint)(currentCell.BestCost + neighbor.Cost * 10);

                    if (newCost < neighbor.BestCost)
                    {
                        BaseGrid.Instance.Cells[index].BestCost = newCost;
                        indicesToCheck.Enqueue(index);
                    }
                }

                if ((currentCell.AvailableDirections & DirectionFlags.UpLeft) != 0)
                {
                    int index = currentIndex + BaseGrid.Instance.GridSize.x - 1;
                    Cell neighbor = BaseGrid.Instance.Cells[index];

                    int upIndex = currentIndex + BaseGrid.Instance.GridSize.x;
                    int leftIndex = currentIndex - 1;
                    if (BaseGrid.Instance.Cells[upIndex].Cost == 255 && BaseGrid.Instance.Cells[leftIndex].Cost == 255)
                        continue;

                    if (neighbor.Cost == 255)
                        continue;

                    uint newCost = (uint)(currentCell.BestCost + neighbor.Cost * 14);

                    if (newCost < neighbor.BestCost)
                    {
                        BaseGrid.Instance.Cells[index].BestCost = newCost;
                        indicesToCheck.Enqueue(index);
                    }
                }

                if ((currentCell.AvailableDirections & DirectionFlags.UpRight) != 0)
                {
                    int index = currentIndex + BaseGrid.Instance.GridSize.x + 1;
                    Cell neighbor = BaseGrid.Instance.Cells[index];

                    int upIndex = currentIndex + BaseGrid.Instance.GridSize.x;
                    int rightIndex = currentIndex + 1;
                    if (BaseGrid.Instance.Cells[upIndex].Cost == 255 && BaseGrid.Instance.Cells[rightIndex].Cost == 255)
                        continue;

                    if (neighbor.Cost == 255)
                        continue;

                    uint newCost = (uint)(currentCell.BestCost + neighbor.Cost * 14);

                    if (newCost < neighbor.BestCost)
                    {
                        BaseGrid.Instance.Cells[index].BestCost = newCost;
                        indicesToCheck.Enqueue(index);
                    }
                }

                if ((currentCell.AvailableDirections & DirectionFlags.DownLeft) != 0)
                {
                    int index = currentIndex - BaseGrid.Instance.GridSize.x - 1;
                    Cell neighbor = BaseGrid.Instance.Cells[index];

                    int downIndex = currentIndex - BaseGrid.Instance.GridSize.x;
                    int leftIndex = currentIndex - 1;
                    if (BaseGrid.Instance.Cells[downIndex].Cost == 255 && BaseGrid.Instance.Cells[leftIndex].Cost == 255)
                        continue;

                    if (neighbor.Cost == 255)
                        continue;

                    uint newCost = (uint)(currentCell.BestCost + neighbor.Cost * 14);

                    if (newCost < neighbor.BestCost)
                    {
                        BaseGrid.Instance.Cells[index].BestCost = newCost;
                        indicesToCheck.Enqueue(index);
                    }
                }

                if ((currentCell.AvailableDirections & DirectionFlags.DownRight) != 0)
                {
                    int index = currentIndex - BaseGrid.Instance.GridSize.x + 1;
                    Cell neighbor = BaseGrid.Instance.Cells[index];

                    int downIndex = currentIndex - BaseGrid.Instance.GridSize.x;
                    int rightIndex = currentIndex + 1;
                    if (BaseGrid.Instance.Cells[downIndex].Cost == 255 && BaseGrid.Instance.Cells[rightIndex].Cost == 255)
                        continue;

                    if (neighbor.Cost == 255)
                        continue;

                    uint newCost = (uint)(currentCell.BestCost + neighbor.Cost * 14);

                    if (newCost < neighbor.BestCost)
                    {
                        BaseGrid.Instance.Cells[index].BestCost = newCost;
                        indicesToCheck.Enqueue(index);
                    }
                }
            }
        }

        public void GenerateVectorField()
        {
            for (int i = 0; i < BaseGrid.Instance.Cells.Length; i++)
            {
                Cell cell = BaseGrid.Instance.Cells[i];

                if (cell.Cost == 255 || cell.BestCost == 0)
                    continue;

                uint bestCost = cell.BestCost;
                Vector2 bestDirection = Vector2.zero;

                if ((cell.AvailableDirections & DirectionFlags.Up) != 0)
                {
                    int index = i + BaseGrid.Instance.GridSize.x;
                    if (BaseGrid.Instance.Cells[index].Cost != 255 && BaseGrid.Instance.Cells[index].BestCost < bestCost)
                    {
                        bestCost = BaseGrid.Instance.Cells[index].BestCost;
                        bestDirection = Vector2.up;
                    }
                }

                if ((cell.AvailableDirections & DirectionFlags.Down) != 0)
                {
                    int index = i - BaseGrid.Instance.GridSize.x;
                    if (BaseGrid.Instance.Cells[index].Cost != 255 && BaseGrid.Instance.Cells[index].BestCost < bestCost)
                    {
                        bestCost = BaseGrid.Instance.Cells[index].BestCost;
                        bestDirection = Vector2.down;
                    }
                }

                if ((cell.AvailableDirections & DirectionFlags.Left) != 0)
                {
                    int index = i - 1;
                    if (BaseGrid.Instance.Cells[index].Cost != 255 && BaseGrid.Instance.Cells[index].BestCost < bestCost)
                    {
                        bestCost = BaseGrid.Instance.Cells[index].BestCost;
                        bestDirection = Vector2.left;
                    }
                }

                if ((cell.AvailableDirections & DirectionFlags.Right) != 0)
                {
                    int index = i + 1;
                    if (BaseGrid.Instance.Cells[index].Cost != 255 && BaseGrid.Instance.Cells[index].BestCost < bestCost)
                    {
                        bestCost = BaseGrid.Instance.Cells[index].BestCost;
                        bestDirection = Vector2.right;
                    }
                }

                if ((cell.AvailableDirections & DirectionFlags.UpLeft) != 0)
                {
                    int upIndex = i + BaseGrid.Instance.GridSize.x;
                    int leftIndex = i - 1;
                    if (!(BaseGrid.Instance.Cells[upIndex].Cost == 255 && BaseGrid.Instance.Cells[leftIndex].Cost == 255))
                    {
                        int index = i + BaseGrid.Instance.GridSize.x - 1;
                        if (BaseGrid.Instance.Cells[index].Cost != 255 && BaseGrid.Instance.Cells[index].BestCost < bestCost)
                        {
                            bestCost = BaseGrid.Instance.Cells[index].BestCost;
                            bestDirection = upLeft;
                        }
                    }
                }

                if ((cell.AvailableDirections & DirectionFlags.UpRight) != 0)
                {
                    int upIndex = i + BaseGrid.Instance.GridSize.x;
                    int rightIndex = i + 1;
                    if (!(BaseGrid.Instance.Cells[upIndex].Cost == 255 && BaseGrid.Instance.Cells[rightIndex].Cost == 255))
                    {
                        int index = i + BaseGrid.Instance.GridSize.x + 1;
                        if (BaseGrid.Instance.Cells[index].Cost != 255 && BaseGrid.Instance.Cells[index].BestCost < bestCost)
                        {
                            bestCost = BaseGrid.Instance.Cells[index].BestCost;
                            bestDirection = upRight;
                        }
                    }
                }

                if ((cell.AvailableDirections & DirectionFlags.DownLeft) != 0)
                {
                    int downIndex = i - BaseGrid.Instance.GridSize.x;
                    int leftIndex = i - 1;
                    if (!(BaseGrid.Instance.Cells[downIndex].Cost == 255 && BaseGrid.Instance.Cells[leftIndex].Cost == 255))
                    {
                        int index = i - BaseGrid.Instance.GridSize.x - 1;
                        if (BaseGrid.Instance.Cells[index].Cost != 255 && BaseGrid.Instance.Cells[index].BestCost < bestCost)
                        {
                            bestCost = BaseGrid.Instance.Cells[index].BestCost;
                            bestDirection = downLeft;
                        }
                    }
                }

                if ((cell.AvailableDirections & DirectionFlags.DownRight) != 0)
                {
                    int downIndex = i - BaseGrid.Instance.GridSize.x;
                    int rightIndex = i + 1;
                    if (!(BaseGrid.Instance.Cells[downIndex].Cost == 255 && BaseGrid.Instance.Cells[rightIndex].Cost == 255))
                    {
                        int index = i - BaseGrid.Instance.GridSize.x + 1;
                        if (BaseGrid.Instance.Cells[index].Cost != 255 && BaseGrid.Instance.Cells[index].BestCost < bestCost)
                        {
                            bestCost = BaseGrid.Instance.Cells[index].BestCost;
                            bestDirection = downRight;
                        }
                    }
                }

                BaseGrid.Instance.Cells[i].BestDirection = bestDirection;
            }
        }

        public Vector2 GetDirectionFromWorldPos(Vector3 worldPos)
        {
            if (BaseGrid.Instance.Cells == null || BaseGrid.Instance.Cells.Length == 0)
                return Vector2.zero;

            int index = BaseGrid.Instance.GetCellIndexFromWorldPos(worldPos);

            return BaseGrid.Instance.Cells[index].BestDirection;
        }

        // 判定1：是否真正到达了终点？（积分场的代价值为0才是真到了）
        public bool HasArrived(Vector3 worldPos)
        {
            if (BaseGrid.Instance.Cells == null || BaseGrid.Instance.Cells.Length == 0)
                return false;

            int index = BaseGrid.Instance.GetCellIndexFromWorldPos(worldPos);
            return BaseGrid.Instance.Cells[index].BestCost == 0;
        }

        // 判定2：要去的那个点是不是墙壁？（用于防穿墙）
        public bool IsWalkable(Vector3 worldPos)
        {
            if (BaseGrid.Instance.Cells == null || BaseGrid.Instance.Cells.Length == 0)
                return false;

            int index = BaseGrid.Instance.GetCellIndexFromWorldPos(worldPos);
            return BaseGrid.Instance.Cells[index].Cost != 255;
        }

#if UNITY_EDITOR
        [Header("Debug Visualization")]
        public bool DisplayGizmos = true;
        //public bool DisplayCostText = false;
        //public bool DisplayBestCostText = false;

        private void OnDrawGizmos()
        {
            if (BaseGrid.Instance == null || !DisplayGizmos || BaseGrid.Instance.Cells == null || BaseGrid.Instance.Cells.Length == 0)
                return;

            //GUIStyle textStyle = new GUIStyle();
            //textStyle.normal.textColor = Color.yellow;
            //textStyle.alignment = TextAnchor.MiddleCenter;
            //textStyle.fontSize = 12;
            //textStyle.fontStyle = FontStyle.Bold;

            for (int i = 0; i < BaseGrid.Instance.Cells.Length; i++)
            {
                Cell cell = BaseGrid.Instance.Cells[i];
                Vector3 center = cell.WorldPosition;

                // --- 1. 画格子颜色与方向 (原有逻辑) ---
                if (cell.Cost == 255)
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                    Gizmos.DrawCube(center, new Vector3(BaseGrid.Instance.CellSize, 0.1f, BaseGrid.Instance.CellSize));
                }
                else if (cell.BestCost == 0)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
                    Gizmos.DrawCube(center, new Vector3(BaseGrid.Instance.CellSize, 0.1f, BaseGrid.Instance.CellSize));
                }
                else if (cell.BestDirection != Vector2.zero)
                {
                    Gizmos.color = Color.white;
                    Vector3 dir3D = new Vector3(cell.BestDirection.x, 0, cell.BestDirection.y);
                    Vector3 endPos = center + dir3D * (BaseGrid.Instance.CellSize * 0.4f);
                    Gizmos.DrawLine(center, endPos);
                    Gizmos.DrawSphere(endPos, BaseGrid.Instance.CellSize * 0.1f);
                }
                else
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireCube(center, new Vector3(BaseGrid.Instance.CellSize * 0.8f, 0.1f, BaseGrid.Instance.CellSize * 0.8f));
                }

                //if (DisplayCostText)
                //{
                //    Vector3 textPos = center + Vector3.up * 0.2f;
                //    UnityEditor.Handles.Label(textPos, cell.Cost.ToString(), textStyle);
                //}

                //if (DisplayBestCostText)
                //{
                //    Vector3 textPos = center + Vector3.up * 0.2f;
                //    UnityEditor.Handles.Label(textPos, cell.BestCost.ToString(), textStyle);
                //}
            }
        }
#endif
    }
}