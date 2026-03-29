using Xiangsoft.Lib.ECS.World;
using Xiangsoft.Lib.Interface;

namespace Xiangsoft.Lib.ECS.System
{
    public abstract class BaseSystem : ISystem
    {
        protected GameWorld world = null;
        protected ulong requireMask = 0;

        protected BaseSystem(GameWorld world)
        {
            this.world = world;
        }

        public abstract void Update(float deltaTime);

        /// <summary>
        /// 判断指定的实体标识符是否与有效实体相对应。
        /// </summary>
        /// <param name="entityID">要验证的实体的标识符。</param>
        /// <returns>如果实体标识符有效，则为真；否则为假。</returns>
        protected bool isValidEntity(int entityID)
        {
            return (world.EntityMasks[entityID] & requireMask) == requireMask;
        }
    }
}