using TrueSync;
using UnityEngine;
using Xiangsoft.Game.Skill;
using Xiangsoft.Lib.ECS.Attribute;
using Xiangsoft.Lib.ECS.Authoring;
using Xiangsoft.Lib.ECS.Component;
using Xiangsoft.Lib.ECS.Pool;

namespace Xiangsoft.Lib.ECS.Skill
{
    [CreateAssetMenu(fileName = "NewSkillEffect", menuName = "技能/技能效果/子弹(ECS)")]
    public class ShootProjectileECSEffect : SkillEffect
    {
        [Header("子弹基础配置")]
        // 注意：这里的预制体请挂载一个简易的组件，用于生成模型。
        // 为了方便演示，假设你有一个对象池 ProjectilePool 来获取 GameObject
        public GameObject projectilePrefab;

        public float projectileSpeed = 10f;
        public float damageMultiplier = 1.0f;
        public float hitRadius = 0.8f;
        public float lifetime = 3.0f;
        public int maxPiercing = 1;
        public bool isBoomerang = false;

        public override void Execute(SkillContext context)
        {
            if (context.Caster == null)
                return;

            TSVector fireDirection;

            if (context.Target != null)
                fireDirection = (context.Target.transform.position - context.Caster.transform.position).normalized.ToTSVector();
            else if (context.TargetPosition != default)
                fireDirection = (context.TargetPosition - context.Caster.transform.position.ToTSVector()).normalized;
            else
                fireDirection = TSVector.zero;

            fireDirection.y = 0; // 保持水平飞行

            int attack = context.Caster.Get(IntStat.Attack);
            float critRate = context.Caster.Get(FloatStat.CritRate);
            float critMult = context.Caster.Get(FloatStat.CritMultiplier) <= 0f ? 2.0f : context.Caster.Get(FloatStat.CritMultiplier);

            bool isCrit = TSRandom.value < critRate;
            int damage = Mathf.CeilToInt(attack * damageMultiplier * (isCrit ? critMult : 1.0f));

            // 1. 获取施法者的 ECS ID (假设你可以通过 Caster 拿到它的 EntityID)
            // 在实际项目中，你可以在 EntityStats 里存一个 public int EntityID;
            int casterID = context.Caster.GetComponent<EntityAuthoring>().GetEntityID();

            // 2. 向 ECS 申请一发子弹的身份
            Entity entity = ECSEngine.Instance.World.CreateEntity();
            if (entity.ID == -1)
                return;

            // 3. 填充纯数据！
            ref ProjectileComponent projComp = ref ECSEngine.Instance.World.Projectiles[entity.ID];
            projComp.IsActive = true;
            projComp.CasterID = casterID;
            projComp.Direction = fireDirection;
            projComp.Speed = projectileSpeed;
            projComp.Damage = damage;
            projComp.IsCrit = isCrit;
            projComp.HitRadius = hitRadius;
            projComp.MaxPiercing = maxPiercing;
            projComp.IsBoomerang = isBoomerang;
            projComp.MaxLifetime = lifetime;
            projComp.LifeTimer = 0f;
            projComp.CurrentHitCount = 0;
            projComp.IsReturning = false;
            ulong comMask = (ulong)ComponentMask.Projectile;

            // 清空这一发子弹的命中记忆
            for (int i = 0; i < projComp.HitHistory.Length; i++)
                projComp.HitHistory[i] = -1;

            // 4. 同步 Transform 数据 (从对象池拿个皮囊给它)
            GameObject projGO = ProjectilePool.Instance.Get(projectilePrefab);
            TransformComponent tComp = ECSEngine.Instance.World.Transforms[entity.ID];
            tComp.Transform = projGO.transform;
            tComp.Position = (context.Caster.transform.position + Vector3.up * 1f).ToTSVector();
            comMask |= (ulong)ComponentMask.Transform;

            // 强制立刻刷新一次皮囊的位置和朝向，防止从池子里刚拿出来时在出生点闪烁一帧
            projGO.transform.position = tComp.Position.ToVector();
            projGO.transform.rotation = Quaternion.LookRotation(fireDirection.ToVector());

            ECSEngine.Instance.World.EntityMasks[entity.ID] = comMask;
        }
    }
}