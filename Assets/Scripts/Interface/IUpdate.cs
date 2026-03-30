using TrueSync;

namespace Xiangsoft.Lib.Interface
{
    public interface IUpdate
    {
        /// <summary>
        /// 每帧调用一次，执行对象的更新逻辑。
        /// </summary>
        /// <param name="deltaTime"></param>
        void Update(FP deltaTime);
    }

    public interface ILateUpdate
    {
        /// <summary>
        /// 每帧调用一次，执行Update更新逻辑之后调用。
        /// </summary>
        /// <param name="deltaTime"></param>
        void LateUpdate(FP deltaTime);
    }

    public interface IFixedUpdate
    {
        /// <summary>
        /// 按固定时间间隔执行逻辑，通常用于物理更新。
        /// </summary>
        /// <param name="fixedDeltaTime">自上次修复更新以来经过的时间（秒）。</param>
        void FixedUpdate(FP fixedDeltaTime);
    }
}