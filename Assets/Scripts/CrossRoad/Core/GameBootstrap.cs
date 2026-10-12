using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Composition root: creates the event channels and wires every system together.
    /// No singleton, no GameObject.Find — the scene camera is a serialized reference.
    /// Everything else is built at runtime (primitives only, no assets).
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        /// <summary>启动即自动连接服务器（阶段 4 默认开；PlayMode 测试置 false 保持假数据模式）。</summary>
        public static bool AutoConnect = true;

        [SerializeField] private Camera sceneCamera;

        private CrossRoadEvents events;

        private void Start()
        {
            LocalizationService.Init();   // language first — every UI build reads L(key)
            events = new CrossRoadEvents();

            // late-bound refs for the listeners below (declared here so the lambdas can capture them)
            LaneManager lanes = null;
            NetClient net = null;

            // seed the sim right away: lanes are generated during Init (menu backdrop),
            // so Rng must never be null; a game start re-seeds for the actual run
            Sim.BeginRandom();

            // deterministic sim: offline games get a random seed; online games are seeded
            // by the server broadcast (NetClient already did Sim.Begin(seed) before raising)
            events.GameStarted.AddListener(() =>
            {
                if (RoomCatalog.OnlineMode && net != null) net.SpawnRemotes(lanes);
                else Sim.BeginRandom();
            });
            events.Restarted.AddListener(() =>
            {
                RemotePlayer.ClearAll();   // local restart clears the ghost players too
                if (!RoomCatalog.OnlineMode) Sim.BeginRandom();
            });

            // state
            var hubGo = new GameObject("GameHub");
            var hub = hubGo.AddComponent<GameHub>();

            // camera (scene object, driven by a component)
            var cam = sceneCamera != null ? sceneCamera : Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
            }
            var cameraControl = cam.gameObject.AddComponent<CreepingCamera>();

            // world
            var lanesGo = new GameObject("Lanes");
            lanes = lanesGo.AddComponent<LaneManager>();

            // player
            var playerGo = new GameObject("Chicken");
            var player = playerGo.AddComponent<PlayerController>();
            playerGo.AddComponent<ChickenRig>().Build();
            var deathFx = playerGo.AddComponent<PlayerDeathFx>();

            // presentation
            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<HudRenderer>();
            var restart = hudGo.AddComponent<RestartOnRKey>();
            var menu = hudGo.AddComponent<MainMenuUi>();

            // net (stage 4: auto-connect on launch — the server assigns names; manual entry
            // stays available through the Online panel for custom addresses)
            var netGo = new GameObject("NetClient");
            net = netGo.AddComponent<NetClient>();
            if (AutoConnect) net.ConnectServer("127.0.0.1", 7777);   // stage 4: auto-connect on launch

            // wiring — order only matters for Init (subscriptions must exist before Raises)
            hub.Init(events);
            cameraControl.Init(cam, player, events);
            lanes.Init(cameraControl, events);
            player.Init(events, lanes);
            deathFx.Init(events);
            hud.Init(events);
            restart.Init(events);
            menu.Init(events);
            net.Init(events);
        }

        private void Update()
        {
            // fixed-timestep heart: the whole world advances in 20Hz logic ticks,
            // frame-rate independent and identical on every client
            Sim.Advance(Time.deltaTime);
        }

        private void OnDestroy()
        {
            // domain-reload-off safety: drop the runtime event SO instances with the bootstrap
            events?.Dispose();
        }
    }
}
