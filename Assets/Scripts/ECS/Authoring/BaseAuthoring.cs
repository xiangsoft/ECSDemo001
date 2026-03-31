using UnityEngine;
using Xiangsoft.Lib.ECS.Attribute;
using Xiangsoft.Lib.ECS.Component;

namespace Xiangsoft.Lib.ECS.Authoring
{
    [RequireComponent(typeof(EntityStats))]
    public class BaseAuthoring : MonoBehaviour
    {
        /// <summary>
        /// 实体
        /// </summary>
        protected Entity entity;

        protected EntityStats stats;

        private void Awake()
        {
            stats = GetComponent<EntityStats>();
            stats.OnDeath += OnDeath;

            OnLoad();
        }

        protected virtual void OnLoad()
        {

        }

        public virtual void Register()
        {

        }

        public virtual void Unregister()
        {
            if (entity.ID == -1 || ECSEngine.Instance == null)
                return;

            // 通知 ECS 世界销毁这个实体数据
            ECSEngine.Instance.World.DestroyEntity(entity);
            entity.ID = -1;
        }

        public int GetEntityID()
        {
            return entity.ID;
        }

        protected virtual void OnDeath(EntityStats stats)
        {

        }
    }
}