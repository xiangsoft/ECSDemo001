using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Xiangsoft.Game.CrossRoad.Tests
{
    /// <summary>
    /// PlayMode regression suite for Cross Road. The composition root lets us wire
    /// the whole game from scratch in an empty scene — no scene dependency, no
    /// singletons to reset. These tests are the safety net for future refactors.
    /// </summary>
    public class CrossRoadPlayModeTests
    {
        private GameObject bootstrapGo;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameBootstrap.AutoConnect = false;   // tests stay offline: fake-data room flows, no server dependency
            bootstrapGo = new GameObject("TestBootstrap");
            bootstrapGo.AddComponent<GameBootstrap>();
            yield return null;   // let Start() wire every system
            yield return null;  // one more frame: lanes built, camera settled
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // destroy the bootstrap (disposes the event channels) and every system it spawned
            if (bootstrapGo != null) Object.Destroy(bootstrapGo);
            DestroyAll<GameHub>();
            DestroyAll<PlayerController>();
            DestroyAll<LaneManager>();
            DestroyAll<CreepingCamera>();
            DestroyAll<HudRenderer>();
            yield return null;   // let the destroys complete before the next test wires a fresh game
        }

        private static void DestroyAll<T>() where T : Component
        {
            foreach (var found in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
                Object.Destroy(found.gameObject);
        }

        private static GameHub Hub => Object.FindFirstObjectByType<GameHub>();
        private static PlayerController Player => Object.FindFirstObjectByType<PlayerController>();
        private static LaneManager Lanes => Object.FindFirstObjectByType<LaneManager>();

        [UnityTest]
        public IEnumerator Bootstrap_WiresAllSystems_WithoutRestart()
        {
            Assert.IsNotNull(Hub, "GameHub should exist after bootstrap");
            Assert.IsNotNull(Player, "PlayerController should exist after bootstrap");
            Assert.IsNotNull(Lanes, "LaneManager should exist after bootstrap");
            Assert.IsNotNull(Object.FindFirstObjectByType<CreepingCamera>(), "CreepingCamera should exist after bootstrap");
            Assert.IsFalse(Hub.IsDead, "a fresh game must be alive");
            yield break;
        }

        [UnityTest]
        public IEnumerator InitialLanes_ExistWithoutRestartEvent()
        {
            // regression: the first generation must not depend on a Restarted raise
            for (int z = 0; z < 6; z++)
                Assert.IsNotNull(Lanes.GetLane(z), "safe-zone lane " + z + " must exist right after bootstrap");
            yield break;
        }

        [UnityTest]
        public IEnumerator Localization_SwitchesTranslatesAndFallsBack()
        {
            Assert.AreEqual("建立房间", LocalizationService.L(LKey.MenuHost), "default language is Simplified Chinese");

            int changeEvents = 0;
            System.Action counter = () => changeEvents++;
            LocalizationService.LanguageChanged += counter;

            LocalizationService.SetLanguage(GameLanguage.English);
            Assert.AreEqual("HOST ROOM", LocalizationService.L(LKey.MenuHost), "English table must apply");
            Assert.AreEqual(1, changeEvents, "LanguageChanged must fire on switch");

            LocalizationService.SetLanguage(GameLanguage.Korean);
            Assert.AreEqual("방 만들기", LocalizationService.L(LKey.MenuHost), "Korean table must apply");

            // restoring must fire too (and leave the clean default for other tests)
            LocalizationService.SetLanguage(GameLanguage.SimplifiedChinese);
            Assert.AreEqual("建立房间", LocalizationService.L(LKey.MenuHost));

            LocalizationService.LanguageChanged -= counter;

            Assert.AreEqual("no_such_key", LocalizationService.L("no_such_key"), "unknown keys fall back to themselves");
            yield break;
        }

        [UnityTest]
        public IEnumerator RoomCatalog_ModelsAreSane()
        {
            var host = RoomCatalog.CreateAsHost();
            Assert.IsTrue(host.YouAreHost, "hosting makes you the host");
            Assert.AreEqual(1, host.Members.Count, "a fresh hosted room has one member");
            Assert.AreEqual(4, RoomSession.MaxPlayers, "rooms hold at most four players");

            var rooms = RoomCatalog.FetchRooms();
            Assert.IsTrue(rooms.Count > 0, "the catalog lists rooms");
            foreach (var room in rooms)
                Assert.IsTrue(room.PlayerCount >= 1 && room.PlayerCount <= RoomSession.MaxPlayers,
                    "room player counts must be within bounds");

            var guest = RoomCatalog.JoinAsGuest(rooms[0]);
            Assert.IsFalse(guest.YouAreHost, "joining makes you a guest");
            Assert.AreEqual(rooms[0].PlayerCount, guest.Members.Count, "guest session mirrors the room count");
            yield break;
        }

        [UnityTest]
        public IEnumerator MenuState_BlocksPlayerAndCamera()
        {
            // fresh boot = menus: the world runs, the chicken and the camera wait
            Player.DebugHop(0, 1);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(0, Player.CurrentZ, "hops must be ignored while menus are up");
            Assert.IsFalse(Hub.IsPlaying, "the hub must not be playing before GameStarted");

            var cam = Object.FindFirstObjectByType<CreepingCamera>();
            Assert.IsNotNull(cam, "camera control should exist");
            Assert.IsFalse(cam.enabled, "the camera must idle while menus are up");
        }

        [UnityTest]
        public IEnumerator GameStarted_EnablesPlayerAndCamera()
        {
            Hub.Events.GameStarted.Raise();
            yield return null;

            Assert.IsTrue(Hub.IsPlaying, "GameStarted must flip the hub to playing");
            var cam = Object.FindFirstObjectByType<CreepingCamera>();
            Assert.IsTrue(cam.enabled, "GameStarted must wake the camera");

            Player.DebugHop(0, 1);
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1, Player.CurrentZ, "the player must hop after the game starts");
            Assert.AreEqual(1, Hub.Score, "score must count after the game starts");
        }

        [UnityTest]
        public IEnumerator Hop_AdvancesPlayerAndRaisesScore()
        {
            Hub.Events.GameStarted.Raise();
            yield return null;

            for (int i = 0; i < 3; i++)
            {
                Player.DebugHop(0, 1);
                yield return new WaitForSeconds(0.25f);
            }
            Assert.AreEqual(3, Player.CurrentZ, "player should be on lane 3");
            Assert.AreEqual(3, Hub.Score, "score should follow the furthest lane reached");
        }

        [UnityTest]
        public IEnumerator BufferedHop_FiresOnLanding()
        {
            Hub.Events.GameStarted.Raise();
            yield return null;

            Player.DebugHop(0, 1);
            Player.DebugHop(0, 1);   // pressed mid-hop -> buffered, must not be eaten
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, Player.CurrentZ, "the buffered hop must fire on landing");
        }

        [UnityTest]
        public IEnumerator DeathEvent_SetsHubStateAndNotifiesListeners()
        {
            int raised = 0;
            Hub.Events.DeathRequested.AddListener((reason, kind) => raised++);

            Hub.Events.DeathRequested.Raise("test death", DeathKind.Squash);

            Assert.AreEqual(1, raised, "death listeners must be notified once");
            Assert.IsTrue(Hub.IsDead, "the hub must be dead after DeathRequested");
            Assert.AreEqual("test death", Hub.DeathReason, "the reason must be carried through the channel");
            yield break;
        }

        [UnityTest]
        public IEnumerator Restarted_ResetsEverySystem()
        {
            Hub.Events.GameStarted.Raise();
            yield return null;

            Player.DebugHop(0, 1);
            yield return new WaitForSeconds(0.25f);
            Hub.Events.DeathRequested.Raise("dead for restart test", DeathKind.Fade);
            Assert.IsTrue(Hub.IsDead, "precondition: dead");

            Hub.Events.Restarted.Raise();
            yield return null;

            Assert.IsFalse(Hub.IsDead, "restart must clear the death state");
            Assert.AreEqual(0, Hub.Score, "restart must clear the score");
            Assert.AreEqual(0, Player.CurrentZ, "the player must be back at the start");

            Player.DebugHop(0, 1);   // alive and kicking again
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1, Player.CurrentZ, "the player must hop after a restart");
            Assert.AreEqual(1, Hub.Score, "score must count again after a restart");
        }
    }
}
