using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// 远程玩家的鸡：按服务器定序的 hop 广播执行同样的格子跳跃与骑木逻辑
    /// （确定性世界下本地与远端算出相同结果），但只做视觉——不监听键盘、
    /// 不做死亡判定（该玩家的生死由其所属端判定并上报，广播到达时播放表现）。
    /// 死亡后保持幽灵观战形态（压扁/沉没后静止，其余玩家继续对局）。
    /// 所有远程实例登记在 All（NetClient 按 slot 分发 hop）与 PlayerProgress（世界生成线）。
    /// </summary>
    public class RemotePlayer : MonoBehaviour, IProgressReporter
    {
        /// <summary>槽位 → 远程鸡实例（对局期间有效；重开/离线时清空）。</summary>
        public static readonly Dictionary<int, RemotePlayer> All = new Dictionary<int, RemotePlayer>();

        public int Slot { get; private set; }
        public string PlayerName { get; private set; }
        public int CurrentZ => gz;

        private LaneManager lanes;
        private int gx, gz;
        private Vector3 worldPos;
        private Vector3 hopFrom, hopTo;
        private float hopT = 1f;
        private MovingObject ridingLog;
        private bool dead;
        private bool hasPending;
        private int pendingDx, pendingDz;

        public void Init(int slot, string playerName, LaneManager laneManager)
        {
            Slot = slot;
            PlayerName = playerName;
            lanes = laneManager;
            name = "Remote_" + slot + "_" + playerName;

            gameObject.AddComponent<ChickenRig>().Build();
            ResetRemote();

            All[slot] = this;
            PlayerProgress.Register(this);
        }

        private void OnDestroy()
        {
            All.Remove(Slot);
            PlayerProgress.Unregister(this);
        }

        /// <summary>重开对局：所有远程鸡回起点（由 Bootstrap 的重开流程调用 ClearAll）。</summary>
        public static void ClearAll()
        {
            var go = new List<GameObject>();
            foreach (var remote in All.Values) go.Add(remote.gameObject);
            All.Clear();
            for (int i = 0; i < go.Count; i++) Destroy(go[i]);
        }

        private void ResetRemote()
        {
            dead = false;
            ridingLog = null;
            gx = 0;
            gz = 0;
            worldPos = new Vector3(0f, CrossRoadConfig.SurfY, 0f);
            transform.localPosition = worldPos;
            transform.localScale = Vector3.one;
            hopT = 1f;
            hasPending = false;
        }

        /// <summary>服务器定序的 hop 广播到达：执行一格跳（与本地 TryHop 同款格子规则）。</summary>
        public void ApplyHop(int direction)
        {
            if (dead) return;
            int dx = direction == 2 ? -1 : direction == 3 ? 1 : 0;
            int dz = direction == 0 ? 1 : direction == 1 ? -1 : 0;
            TryHop(dx, dz);
        }

        /// <summary>静态分发：NetClient 收到 hop 广播后按槽位找到对应远程鸡执行。</summary>
        public static void Apply(int slot, int direction)
        {
            if (All.TryGetValue(slot, out var remote)) remote.ApplyHop(direction);
        }

        /// <summary>静态分发：某远程玩家死亡广播（其所属端上报）。</summary>
        public static void ApplyDeathRemote(int slot, DeathKind kind)
        {
            if (All.TryGetValue(slot, out var remote)) remote.ApplyDeath(kind);
        }

        /// <summary>该玩家的死亡广播到达：播放表现并进入幽灵观战（静置）。</summary>
        public void ApplyDeath(DeathKind kind)
        {
            if (dead) return;   // 幂等：本地世界已判到同样结果
            dead = true;
            hasPending = false;
            ridingLog = null;

            switch (kind)
            {
                case DeathKind.Squash:
                    var squash = transform.localPosition;
                    squash.y = 0.06f;
                    transform.localPosition = squash;
                    transform.localScale = new Vector3(1.25f, 0.12f, 1.25f);
                    break;
                case DeathKind.Drown:
                case DeathKind.Fade:
                    var sink = transform.localPosition;
                    sink.y = -0.6f;
                    transform.localPosition = sink;
                    break;
            }
        }

        private void Update()
        {
            if (dead) return;

            if (ridingLog != null && hopT >= 1f)
            {
                worldPos.x = ridingLog.transform.localPosition.x;
                transform.localPosition = worldPos;
                if (Mathf.Abs(worldPos.x) > CrossRoadConfig.RideOffX)
                {
                    // 确定性世界下所属端必然判到同样死亡并上报；本端直接播表现
                    ApplyDeath(DeathKind.Fade);
                }
            }
            else
            {
                UpdateHop();
            }
        }

        private void TryHop(int dx, int dz)
        {
            if (hopT < 1f)
            {
                hasPending = true;
                pendingDx = dx;
                pendingDz = dz;
                return;
            }

            int bx = ridingLog != null ? Mathf.RoundToInt(worldPos.x) : gx;
            int tx = bx + dx;
            int tz = gz + dz;

            if (Mathf.Abs(tx) > CrossRoadConfig.PlayHalfWidth) return;

            var lane = lanes.GetLane(tz);
            if (lane == null || lane.IsBlocked(tx)) return;

            ridingLog = null;

            hopFrom = worldPos;
            float targetY = lane.Type == LaneType.River ? CrossRoadConfig.LogTopY : CrossRoadConfig.SurfY;
            hopTo = new Vector3(tx, targetY, tz);
            hopT = 0f;

            if (dz > 0) transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            else if (dz < 0) transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            else if (dx > 0) transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            else if (dx < 0) transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        }

        private void UpdateHop()
        {
            if (hopT >= 1f)
            {
                transform.localPosition = worldPos;
                return;
            }

            hopT += Time.deltaTime / CrossRoadConfig.HopDuration;
            float t = Mathf.Clamp01(hopT);
            Vector3 p = Vector3.Lerp(hopFrom, hopTo, t);
            p.y += 4f * t * (1f - t) * CrossRoadConfig.HopArcHeight;
            worldPos = p;
            transform.localPosition = p;
            if (t >= 1f) Land();
        }

        private void Land()
        {
            gx = Mathf.RoundToInt(hopTo.x);
            gz = Mathf.RoundToInt(hopTo.z);
            worldPos = hopTo;
            transform.localPosition = worldPos;

            var lane = lanes.GetLane(gz);
            if (lane != null && lane.Type == LaneType.River)
            {
                var log = lane.LogAt(worldPos.x);
                if (log != null) ridingLog = log;
                else ApplyDeath(DeathKind.Drown);   // 所属端同样会判到并上报；这里先播表现
            }

            if (hasPending)
            {
                hasPending = false;
                TryHop(pendingDx, pendingDz);
            }
        }
    }
}
