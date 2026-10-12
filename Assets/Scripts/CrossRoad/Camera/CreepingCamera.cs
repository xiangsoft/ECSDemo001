using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// The creeping camera: follows the chicken, creeps forward on its own,
    /// and raises DeathRequested when the chicken falls behind the frame.
    /// Freezes on death, resets on restart. Camera maths lives here and nowhere else.
    /// </summary>
    public class CreepingCamera : MonoBehaviour
    {
        public float FollowZ { get; private set; }

        private Camera cam;
        private PlayerController player;
        private CrossRoadEvents events;
        private float creepZ;
        private int scoreCache;
        private bool snapped;
        private bool inited;   // guards against play-exit/recompile races clearing injected refs

        public void Init(Camera cam, PlayerController player, CrossRoadEvents events)
        {
            this.cam = cam;
            this.player = player;
            this.events = events;
            inited = true;

            cam.fieldOfView = CrossRoadConfig.CamFov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CrossRoadConfig.Sky;
            cam.transform.position = new Vector3(0f, CrossRoadConfig.CamHeight, -CrossRoadConfig.CamBack);
            cam.transform.rotation = Quaternion.Euler(CrossRoadConfig.CamPitch, 0f, 0f);

            events.ScoreChanged.AddListener(s => scoreCache = s);
            events.DeathRequested.AddListener(OnDeath);
            events.Restarted.AddListener(OnRestarted);
            events.GameStarted.AddListener(OnRestarted);   // leaving menus = fresh start

            enabled = false;   // idle in menus: the world runs, the camera waits
        }

        private void OnDeath(string reason, DeathKind kind) => enabled = false;

        private void OnRestarted()
        {
            creepZ = 0f;
            FollowZ = 0f;
            snapped = false;
            scoreCache = 0;
            enabled = true;
        }

        private void Update()
        {
            if (!inited) return;

            float speed = CrossRoadConfig.BaseCreep
                        + Mathf.Min(CrossRoadConfig.CreepScoreCap, scoreCache * CrossRoadConfig.CreepScoreScale);
            creepZ += speed * Time.deltaTime;

            float target = Mathf.Max(creepZ, player.CurrentZ);
            if (!snapped)
            {
                FollowZ = target;   // first frame: snap, no lerp from zero
                snapped = true;
            }
            else
            {
                FollowZ = Mathf.Lerp(FollowZ, target, 1f - Mathf.Exp(-CrossRoadConfig.CamFollowLerp * Time.deltaTime));
            }

            // Invariant: the player's own lane is never recycled (death line 3.4 < recycle line 13),
            // so reading the player here is always safe.
            if (player.WorldZ < FollowZ - CrossRoadConfig.BehindLine)
                events.DeathRequested.Raise(LKey.DeathBehind, DeathKind.Fade);

            var pos = cam.transform.position;
            pos.z = FollowZ - CrossRoadConfig.CamBack;
            cam.transform.position = pos;
        }
    }
}
