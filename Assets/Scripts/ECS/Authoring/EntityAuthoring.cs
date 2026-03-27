using UnityEngine;
using Xiangsoft.Game.Skill;
using Xiangsoft.Lib.ECS.Attribute;
using Xiangsoft.Lib.ECS.Component;
using Xiangsoft.Lib.ECS.Pool;
using Xiangsoft.Lib.ECS.Spawner;

namespace Xiangsoft.Lib.ECS.Authoring
{
    public class EntityAuthoring : BaseAuthoring
    {
        [HideInInspector]
        public int PrefabInstanceID;

        [Header("实体类型")]
        public bool IsPlayer = false;

        [Header("AI 配置")]
        public bool isRangedMob = false;
        public float attackRange = 1.5f;
        public float fleeRange = 0f; // 如果不是远程怪，就设为 0

        protected override void OnLoad()
        {
            if (IsPlayer)
                Invoke(nameof(Register), 0.1f);

            base.OnLoad();
        }

        private void Update()
        {
            if (IsPlayer && entity.ID != -1 && ECSEngine.Instance != null)
            {
                // 主角在 OOP 里移动了，我们要把坐标同步给 ECS，这样 ECS 里的子弹和怪物才能找到主角
                TransformComponent tComp = ECSEngine.Instance.World.Transforms[entity.ID];
                tComp.Position = transform.position;
                tComp.Rotation = transform.rotation;
            }
        }

        public override void Register()
        {
            // 1. 向全局的 ECS 引擎申请一个身份
            entity = ECSEngine.Instance.World.CreateEntity();

            if (entity.ID == -1)
            {
                base.Register();
                return;
            }

            // 2. 将 Unity 的 Transform 引用和初始坐标填入 ECS 数组
            TransformComponent tComp = ECSEngine.Instance.World.Transforms[entity.ID];
            tComp.Transform = transform;
            tComp.Position = transform.position;
            tComp.Rotation = transform.rotation;
            ulong comMask = (ulong)ComponentMask.Transform;

            if (!IsPlayer)
            {
                // 3. 将 EntityStats 里的数据填入纯数据组件！(这就是数据驱动的魅力)
                // 我们通过 ref 直接修改结构体数组里的值，0 GC
                ref MovementComponent mComp = ref ECSEngine.Instance.World.Movements[entity.ID];
                mComp.MoveSpeed = stats.Get(FloatStat.MoveSpeed);
                mComp.RotationSpeed = stats.Get(FloatStat.RotationSpeed);
                mComp.SeparationRadius = stats.Get(FloatStat.SeparationRadius);
                mComp.SeparationWeight = stats.Get(FloatStat.SeparationWeight);
                mComp.WobbleSpeed = stats.Get(FloatStat.WobbleSpeed);
                mComp.WobbleStrength = stats.Get(FloatStat.WobbleStrength);
                mComp.RandomPhase = Random.Range(0f, 100f);
                comMask |= (ulong)ComponentMask.Movement;

                ref AIComponent ai = ref ECSEngine.Instance.World.AIs[entity.ID];
                ai.IsRanged = isRangedMob;
                ai.AttackRange = attackRange;
                ai.FleeRange = fleeRange;
                ai.CurrentState = AIState.Chase;
                comMask |= (ulong)ComponentMask.AI;
            }

            ECSEngine.Instance.World.StatsBridge[entity.ID] = stats;
            ECSEngine.Instance.World.SkillBridge[entity.ID] = GetComponent<SkillController>();

            if (IsPlayer)
            {
                ECSEngine.Instance.PlayerEntityID = entity.ID;
                UnitECSSpawner.Instance.IsPlayerInitOK = true;
            }

            ECSEngine.Instance.World.EntityMasks[entity.ID] = comMask;

            base.Register();
        }

        protected override void OnDeath(EntityStats stats)
        {
            if (IsPlayer)
            {
                Debug.Log("玩家死亡，隐藏对象");
                gameObject.SetActive(false);
            }
            else
            {
                UnitPool.Instance?.Release(this);
            }

            base.OnDeath(stats);
        }
    }
}