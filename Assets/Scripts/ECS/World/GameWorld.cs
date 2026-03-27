using System;
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

        public int ActiveEntityCount = 0;

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
        }

        public Entity CreateEntity()
        {
            if (ActiveEntityCount >= MaxEntities)
                throw new Exception("Reached maximum entity limit!");

            Entity entity = new Entity { ID = ActiveEntityCount };
            EntityMasks[entity.ID] = (ulong)ComponentMask.None;
            ActiveEntityCount++;
            return entity;
        }

        public void DestroyEntity(Entity entity)
        {
            if (entity.ID < 0 || entity.ID >= MaxEntities)
                throw new Exception("Invalid entity ID!");

            EntityMasks[entity.ID] = (ulong)ComponentMask.None;
            StatsBridge[entity.ID] = null; // 切断桥梁引用
            SkillBridge[entity.ID] = null;
        }
    }
}