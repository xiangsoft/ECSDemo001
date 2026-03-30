using FixedMathSharp;
using Xiangsoft.Lib.ECS.Component;

namespace Xiangsoft.Lib.ECS.System
{
    public class TransformSyncSystem : BaseSystem
    {
        public TransformSyncSystem(World.GameWorld world) : base(world)
        {
            requireMask = (ulong)ComponentMask.Transform;
        }

        public override void Update(Fixed64 deltaTime)
        {
            for (int i = 0; i < world.MaxAllocatedID; i++)
            {
                if (ECSEngine.Instance.PlayerEntityID == i)
                    continue;

                if (!isValidEntity(i))
                    continue;

                TransformComponent tComp = world.Transforms[i];

                if (tComp.Transform == null)
                    continue;

                tComp.Transform.position = tComp.Position.ToVector3();
                tComp.Transform.rotation = tComp.Rotation.ToQuaternion();
            }
        }
    }
}