using FixedMathSharp;
using UnityEngine;

namespace Xiangsoft.Lib.ECS.Component
{
    /// <summary>
    /// 实体ID
    /// </summary>
    public struct Entity
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID;
    }

    /// <summary>
    /// 坐标与变换组件
    /// </summary>
    public class TransformComponent
    {
        // 为什么这里用 class？因为我们需要持有 Unity 的 Transform 引用来更新画面
        // 纯数据逻辑中我们只读写 Position 和 Rotation，最后由专门的系统同步给 Transform
        public Transform Transform;
        public Vector3d Position;
        public FixedQuaternion Rotation;
    }

    /// <summary>
    /// 移动属性组件
    /// </summary>
    public struct MovementComponent
    {
        public Fixed64 MoveSpeed;
        public Fixed64 RotationSpeed;
        public Fixed64 SeparationRadius;
        public Fixed64 SeparationWeight;
        public Fixed64 WobbleSpeed;
        public Fixed64 WobbleStrength;
        public Fixed64 RandomPhase;
    }
}