using UnityEngine;

namespace Xiangsoft.Lib.ECS.Pool
{
    public class PooledProjectile : MonoBehaviour
    {
        [HideInInspector]
        public int PrefabInstanceID; // 记录这个子弹是哪个预制体实例化的，方便对象池管理
    }
}