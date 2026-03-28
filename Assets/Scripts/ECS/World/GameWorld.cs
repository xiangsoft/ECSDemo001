using System;
using System.Collections.Generic;
using UnityEngine;
using Xiangsoft.Game.Skill;
using Xiangsoft.Lib.ECS.Attribute;
using Xiangsoft.Lib.ECS.Component;

namespace Xiangsoft.Lib.ECS.World
{
    public class GameWorld
    {
        public readonly int MaxEntities;

        public ulong[] EntityMasks;

        public TransformComponent[] Transforms;
        public MovementComponent[] Movements;
        public ProjectileComponent[] Projectiles;
        public AIComponent[] AIs;
        public ExpGemComponent[] ExpGems;

        public EntityStats[] StatsBridge;
        public SkillController[] SkillBridge;

        private Queue<int> freeIDs = null;
        public int MaxAllocatedID { get; private set; }
        public int ActiveEntityCount { get; private set; }

        public GameWorld(int maxEntities)
        {
            MaxEntities = maxEntities;
            EntityMasks = new ulong[maxEntities];
            Transforms = new TransformComponent[maxEntities];
            Movements = new MovementComponent[maxEntities];
            Projectiles = new ProjectileComponent[maxEntities];
            AIs = new AIComponent[maxEntities];
            ExpGems = new ExpGemComponent[maxEntities];

            StatsBridge = new EntityStats[maxEntities];
            SkillBridge = new SkillController[maxEntities];

            // 预分配对象与内部数组，彻底消灭 GC
            for (int i = 0; i < maxEntities; i++)
            {
                Transforms[i] = new TransformComponent();
                Projectiles[i].HitHistory = new int[256];
            }

            freeIDs = new Queue<int>(maxEntities);
            MaxAllocatedID = 0;
            ActiveEntityCount = 0;
        }

        public Entity CreateEntity()
        {
            int newID = -1;

            if (freeIDs.Count > 0)
            {
                newID = freeIDs.Dequeue();
            }
            else if (MaxAllocatedID < MaxEntities)
            {
                newID = MaxAllocatedID;
                MaxAllocatedID++;
            }
            else
            {
                Debug.LogWarning("ECS 实体数量已达上限！");
                return new Entity { ID = -1 };
            }

            EntityMasks[newID] = (ulong)ComponentMask.None;

            return new Entity { ID = newID };
        }

        public void DestroyEntity(Entity entity)
        {
            if (entity.ID == -1)
                return;

            //防止同时被多个系统销毁
            if (EntityMasks[entity.ID] == (ulong)ComponentMask.None)
                return;

            EntityMasks[entity.ID] = (ulong)ComponentMask.None;
            if (StatsBridge[entity.ID] != null)
                StatsBridge[entity.ID] = null;

            freeIDs.Enqueue(entity.ID);
            ActiveEntityCount--;
        }
    }
}