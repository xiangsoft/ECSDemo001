using FixedMathSharp;
using UnityEngine;
using Xiangsoft.Game.Level;
using Xiangsoft.Game.Skill;
using Xiangsoft.Lib.ECS.Attribute;
using Xiangsoft.Lib.ECS.Component;
using Xiangsoft.Lib.ECS.Pool;
using Xiangsoft.Lib.LockStep;

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
        public Fixed64 attackRange = (Fixed64)1.5;
        public Fixed64 fleeRange = (Fixed64)0; // 如果不是远程怪，就设为 0

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
                tComp.Position = transform.position.ToVector3d();
                tComp.Rotation = transform.rotation.ToFixedQuaternion();
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
            tComp.Position = transform.position.ToVector3d();
            tComp.Rotation = transform.rotation.ToFixedQuaternion();
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
                mComp.RandomPhase = RandomManager.Instance.Range((Fixed64)0, (Fixed64)100);
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
                WaveManager.Instance.StartGame();
            }

            ECSEngine.Instance.World.EntityMasks[entity.ID] = comMask;

            base.Register();
        }

        protected override void OnDeath(EntityStats stats)
        {
            if (!gameObject.activeSelf)
                return;

            if (IsPlayer)
            {
                gameObject.SetActive(false);
            }
            else
            {
                // ==========================================
                // ★ 新增：怪物死亡，爆出经验球！
                // ==========================================
                if (ECSEngine.Instance != null && ExpGemPool.Instance != null)
                {
                    Entity gemEntity = ECSEngine.Instance.World.CreateEntity();
                    if (gemEntity.ID != -1)
                    {
                        // 1. 填入 ECS 纯数据
                        ref ExpGemComponent gemComp = ref ECSEngine.Instance.World.ExpGems[gemEntity.ID];
                        gemComp.State = ExpGemState.Idle;
                        gemComp.ExpValue = 10; // 假设每只怪掉 10 点经验
                        gemComp.CurrentSpeed = Fixed64.Zero;

                        // 2. 绑定皮囊
                        GameObject gemGO = ExpGemPool.Instance.Get();
                        gemGO.transform.position = transform.position; // 原地掉落

                        TransformComponent tComp = ECSEngine.Instance.World.Transforms[gemEntity.ID];
                        tComp.Transform = gemGO.transform;
                        tComp.Position = transform.position.ToVector3d();

                        // 3. 赋予 DNA
                        ulong gemMask = (ulong)(ComponentMask.Transform | ComponentMask.ExpGem);
                        ECSEngine.Instance.World.EntityMasks[gemEntity.ID] = gemMask;
                    }
                }

                UnitPool.Instance?.Release(this);
            }

            base.OnDeath(stats);
        }
    }
}