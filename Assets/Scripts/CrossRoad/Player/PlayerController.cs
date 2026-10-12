using UnityEngine;
using UnityEngine.InputSystem;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// The chicken's hop state machine: one hop per key press (with a one-step buffer
    /// so fast double-taps are never eaten), log riding, landing logic.
    /// Looks are ChickenRig, death animation is PlayerDeathFx, communication is events.
    /// </summary>
    public class PlayerController : MonoBehaviour, IProgressReporter
    {
        public int MaxZ { get; private set; }
        public int CurrentZ => gz;
        public float WorldX => worldPos.x;
        public float WorldZ => worldPos.z;
        public bool Dead => dead;

        private CrossRoadEvents events;
        private LaneManager lanes;

        private int gx, gz;
        private Vector3 worldPos;
        private Vector3 hopFrom, hopTo;
        private float hopT = 1f;
        private MovingObject ridingLog;
        private Lane currentLane;
        private bool dead;

        // one-step input buffer: a key pressed mid-hop fires the instant we land
        private bool hasPending;
        private int pendingDx, pendingDz;
        private bool inited;            // guards against play-exit/recompile races clearing injected refs
        private bool controlsEnabled;  // false in menus — the world runs, the chicken waits

        public void Init(CrossRoadEvents events, LaneManager lanes)
        {
            this.events = events;
            this.lanes = lanes;
            inited = true;
            controlsEnabled = false;
            events.GameStarted.AddListener(() => controlsEnabled = true);
            events.DeathRequested.AddListener(OnDeathRequested);
            events.Restarted.AddListener(ResetPlayer);
            PlayerProgress.Register(this);   // world generation line: max over all players
        }

        private void OnDestroy() => PlayerProgress.Unregister(this);

        private void Start() => ResetPlayer();

        public void ResetPlayer()
        {
            dead = false;
            ridingLog = null;
            currentLane?.ClearPlayer(this);
            currentLane = null;
            gx = 0;
            gz = 0;
            MaxZ = 0;
            hopT = 1f;
            hasPending = false;
            worldPos = new Vector3(0f, CrossRoadConfig.SurfY, 0f);
            transform.position = worldPos;
        }

        private void Update()
        {
            if (!inited || dead || !controlsEnabled) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) TryHop(0, 1);
            else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) TryHop(0, -1);
            else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) TryHop(-1, 0);
            else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) TryHop(1, 0);

            if (ridingLog != null && hopT >= 1f)
            {
                // carried sideways by the log
                worldPos.x = ridingLog.transform.localPosition.x;
                transform.position = worldPos;
                if (Mathf.Abs(worldPos.x) > CrossRoadConfig.RideOffX)
                {
                    events.DeathRequested.Raise(LKey.DeathDrift, DeathKind.Fade);
                    return;
                }
            }
            else
            {
                UpdateHop();
            }
        }

        /// Test hook for editor-driven runs (the old Input Manager cannot be fed by key simulation).
        public void DebugHop(int dx, int dz) => TryHop(dx, dz);

        private void TryHop(int dx, int dz)
        {
            if (dead || !controlsEnabled) return;
            if (hopT < 1f)
            {
                // mid-hop: remember the intent, run it on landing
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
            if (lane == null || lane.IsBlocked(tx)) return;   // a tree stands there

            ridingLog = null;

            hopFrom = worldPos;
            float targetY = lane.Type == LaneType.River ? CrossRoadConfig.LogTopY : CrossRoadConfig.SurfY;
            hopTo = new Vector3(tx, targetY, tz);
            hopT = 0f;

            if (dz > 0) transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            else if (dz < 0) transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            else if (dx > 0) transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            else if (dx < 0) transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            // stage 3: optimistic hop executed locally; report for server ordering (online only)
            NetClient.Instance?.ReportHop(dx, dz);
        }

        private void UpdateHop()
        {
            if (hopT >= 1f)
            {
                transform.position = worldPos;
                return;
            }

            hopT += Time.deltaTime / CrossRoadConfig.HopDuration;
            float t = Mathf.Clamp01(hopT);
            Vector3 p = Vector3.Lerp(hopFrom, hopTo, t);
            p.y += 4f * t * (1f - t) * CrossRoadConfig.HopArcHeight;
            worldPos = p;
            transform.position = p;
            if (t >= 1f) Land();
        }

        private void Land()
        {
            gx = Mathf.RoundToInt(hopTo.x);
            gz = Mathf.RoundToInt(hopTo.z);
            worldPos = hopTo;
            transform.position = worldPos;

            // hazard registration: only the lane we stand on keeps watching us
            if (currentLane != null && currentLane.Z != gz) currentLane.ClearPlayer(this);
            currentLane = lanes.GetLane(gz);
            currentLane?.SetPlayer(this);

            if (gz > MaxZ)
            {
                MaxZ = gz;
                events.ScoreChanged.Raise(MaxZ);
            }

            var lane = currentLane;
            if (lane != null && lane.Type == LaneType.River)
            {
                var log = lane.LogAt(worldPos.x);
                if (log != null) ridingLog = log;
                else
                {
                    events.DeathRequested.Raise(LKey.DeathDrown, DeathKind.Drown);
                    return;
                }
            }

            // buffered input fires now that we're grounded
            if (hasPending)
            {
                hasPending = false;
                TryHop(pendingDx, pendingDz);
            }
        }

        private void OnDeathRequested(string reason, DeathKind kind)
        {
            dead = true;
            hasPending = false;
            ridingLog = null;
            currentLane?.ClearPlayer(this);
            currentLane = null;

            // stage 3: this end judged the death — tell the others (online only)
            NetClient.Instance?.ReportDied(reason, kind);
        }
    }
}
