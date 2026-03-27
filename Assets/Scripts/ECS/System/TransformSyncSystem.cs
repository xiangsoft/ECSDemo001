using Xiangsoft.Lib.ECS.Component;

namespace Xiangsoft.Lib.ECS.System
{
    public class TransformSyncSystem : ISystem
    {
        private World.GameWorld world;

        private readonly ulong requireMask;

        public TransformSyncSystem(World.GameWorld world)
        {
            this.world = world;

            requireMask = (ulong)ComponentMask.Transform;
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < world.ActiveEntityCount; i++)
            {
                if (ECSEngine.Instance.PlayerEntityID == i)
                    continue;

                if ((world.EntityMasks[i] & requireMask) != requireMask)
                    continue;

                TransformComponent tComp = world.Transforms[i];

                if (tComp.Transform == null)
                    continue;

                tComp.Transform.position = tComp.Position;
                tComp.Transform.rotation = tComp.Rotation;
            }
        }
    }
}