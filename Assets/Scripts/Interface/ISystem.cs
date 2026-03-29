namespace Xiangsoft.Lib.Interface
{
    /// <summary>
    /// ECS 系统的基础接口
    /// 继承自 IUpdate，具备每帧轮询的能力
    /// </summary>
    public interface ISystem : IUpdate
    {
        // 目前它是空的，作为一个“标识接口 (Marker Interface)”存在。
        // 未来如果 System 有自己独有的生命周期（比如 void Init(GameWorld world); 或者 void Destroy();），可以写在这里，而不污染通用的 IUpdate。
    }
}