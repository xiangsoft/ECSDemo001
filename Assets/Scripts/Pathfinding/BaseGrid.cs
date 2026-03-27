using UnityEngine;

namespace Xiangsoft.Lib.Pathfinding
{
    public class BaseGrid : MonoBehaviour
    {
        public static BaseGrid Instance { get; private set; }

        public FlowFieldGrid FlowGrid { get; private set; }
        public AStarGrid AStarGrid { get; private set; }

        public Cell[] Cells { get; private set; }
        public Vector2Int GridSize { get; private set; }
        public float CellSize { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            if (FlowGrid == null)
                FlowGrid = GetComponent<FlowFieldGrid>();

            if (AStarGrid == null)
                AStarGrid = GetComponent<AStarGrid>();
        }

        public void Init(Vector2Int gridSize, float cellSize)
        {
            GridSize = gridSize;
            CellSize = cellSize;
        }

        public void CreateGrid()
        {
            Cells = new Cell[GridSize.x * GridSize.y];
            Vector3 cellHalfExtents = Vector3.one * CellSize / 2f;
            int terrainMask = LayerMask.GetMask("Impassible", "RoughTerrain");
            for (int y = 0; y < GridSize.y; y++)
            {
                for (int x = 0; x < GridSize.x; x++)
                {
                    int index = GetIndex(x, y);
                    int cost = 1;
                    Vector3 worldPosition = new Vector3(transform.position.x + CellSize / 2f + x * CellSize, 0, transform.position.z + CellSize / 2f + y * CellSize);
                    Collider[] obstacles = Physics.OverlapBox(worldPosition, cellHalfExtents, Quaternion.identity, terrainMask);
                    bool hasIncreasedCost = false;

                    foreach (Collider col in obstacles)
                    {
                        if (col.gameObject.layer == 8)
                        {
                            cost = 255;
                            continue;
                        }
                        else if (!hasIncreasedCost && col.gameObject.layer == 9)
                        {
                            cost += 3;
                            hasIncreasedCost = true;
                        }
                    }

                    Cells[index] = new Cell
                    {
                        WorldPosition = worldPosition,
                        GridPosition = new Vector2Int(x, y),
                        Cost = cost,
                        BestCost = uint.MaxValue,
                        BestDirection = Vector2.zero,
                        AvailableDirections = getNeighborDirections(x, y)
                    };
                }
            }
        }

        public int GetIndex(int x, int y)
        {
            return y * GridSize.x + x;
        }

        public int GetCellIndexFromWorldPos(Vector3 worldPos)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((worldPos.x - transform.position.x) / CellSize), 0, GridSize.x - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt((worldPos.z - transform.position.z) / CellSize), 0, GridSize.y - 1);
            return GetIndex(x, y);
        }

        private DirectionFlags getNeighborDirections(int x, int y)
        {
            DirectionFlags flags = DirectionFlags.None;

            if (x > 0)
                flags |= DirectionFlags.Left;

            if (x + 1 < GridSize.x)
                flags |= DirectionFlags.Right;

            if (y > 0)
                flags |= DirectionFlags.Down;

            if (y + 1 < GridSize.y)
                flags |= DirectionFlags.Up;

            if (x > 0 && y + 1 < GridSize.y)
                flags |= DirectionFlags.UpLeft;

            if (x + 1 < GridSize.x && y + 1 < GridSize.y)
                flags |= DirectionFlags.UpRight;

            if (x > 0 && y > 0)
                flags |= DirectionFlags.DownLeft;

            if (x + 1 < GridSize.x && y > 0)
                flags |= DirectionFlags.DownRight;

            return flags;
        }
    }
}