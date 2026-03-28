using System.Collections.Generic;
using UnityEngine;
using Xiangsoft.Lib.ECS.AI;
using Xiangsoft.Lib.ECS.Grid;
using Xiangsoft.Lib.ECS.System;
using Xiangsoft.Lib.ECS.World;
using Xiangsoft.Lib.Pathfinding;

namespace Xiangsoft.Lib.ECS
{
    [DefaultExecutionOrder(-50)]
    public class ECSEngine : MonoBehaviour
    {
        public static ECSEngine Instance { get; private set; }

        [Header("网格设置")]
        public int Width = 42;
        public int Height = 26;
        public float CellSize = 1f;

        [Header("基础设施引用")]
        public BaseGrid Grid;
        public FlowFieldGrid FlowGrid;
        public SpatialHashECSGrid SpatialGrid;

        [Header("ECS 设置")]
        public int MaxEntities = 3000; // 微信小游戏同屏 3000 怪的底气！

        public int PlayerEntityID = -1;

        // 暴露给外部 (如 Authoring) 访问的纯数据世界
        public GameWorld World { get; private set; }

        private List<ISystem> systems = null;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            World = new GameWorld(MaxEntities);

            Grid.Init(new Vector2Int(Width, Height), CellSize);
            Grid.CreateGrid();
            SpatialGrid = new SpatialHashECSGrid(Width, Height, CellSize);

            systems = new List<ISystem>
            {
                new MobAISystem(World),
                new MovementSystem(World, FlowGrid, SpatialGrid),
                new TransformSyncSystem(World),
                new ProjectileSystem(World, SpatialGrid),
                new MeleeCombatSystem(World),
                new ExpGemSystem(World)
            };
        }

        private void Update()
        {
            if (World.ActiveEntityCount == 0)
                return;

            float deltaTime = Time.deltaTime;

            rebuildSpatialGrid();

            foreach (ISystem system in systems)
            {
                system.Update(deltaTime);
            }
        }

        private void rebuildSpatialGrid()
        {
            SpatialGrid.Clear();
            for (int i = 0; i < World.MaxAllocatedID; i++)
            {
                Vector3 pos = World.Transforms[i].Position;
                SpatialGrid.Insert(i, pos);
            }
        }
    }
}