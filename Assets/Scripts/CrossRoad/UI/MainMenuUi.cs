using UnityEngine;
using UnityEngine.UI;
using Xiangsoft.Game.Ui;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Main menu over the living world: title + host/join/settings/quit.
    /// All text flows through LocalizationService and re-renders on language change.
    /// </summary>
    public class MainMenuUi : MonoBehaviour
    {
        private GameObject root;
        private Text titleText;
        private Text subtitleText;
        private Button onlineButton;
        private Button hostButton;
        private Button joinButton;
        private Button settingsButton;
        private Button quitButton;
        private Text cornerStatusText;   // stage 4: bottom-right server status
        private Text waitHintText;       // stage 4: centre "server not ready" toast
        private float waitHintTimer;

        private RoomLobbyUi lobbyScreen;
        private JoinRoomUi joinScreen;
        private SettingsUi settingsScreen;
        private OnlinePanelUi onlineScreen;

        /// <summary>服务器就绪 = 已握手 + 已拿到分配名（建房/加入的前置条件）。</summary>
        private static bool ServerReady =>
            NetClient.Instance != null && NetClient.Instance.Online
            && NetClient.Instance.Connected && !string.IsNullOrEmpty(NetClient.Instance.AssignedName);

        public void Init(CrossRoadEvents events)
        {
            UiKit.EnsureEventSystem();

            var canvas = UiKit.CreateCanvas("MenuCanvas");
            canvas.sortingOrder = 10;                    // above the HUD canvas
            canvas.transform.SetParent(transform, false);

            root = CreateScreenRoot("MainMenu", canvas.transform);
            UiPanel.Create("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.35f)).Show();

            titleText = UiKit.CreateText("Title", root.transform, "", 84, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 212f),
                FontStyle.Bold);
            subtitleText = UiKit.CreateText("Subtitle", root.transform, "", 22, new Color(1f, 1f, 1f, 0.55f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 148f));

            var dark = new Color(0.13f, 0.13f, 0.17f);
            var plate = new Color(0.97f, 0.97f, 0.95f);

            onlineButton = UiKit.CreateButton("OnlineButton", root.transform, "", 30, dark, plate,
                new Vector2(320f, 68f), new Vector2(0f, 128f),
                () => { root.SetActive(false); onlineScreen.Show(); });   // manual entry: custom address / status
            hostButton = UiKit.CreateButton("HostButton", root.transform, "", 30, dark, plate,
                new Vector2(320f, 68f), new Vector2(0f, 48f),
                () =>
                {
                    if (!ServerReady) { ShowWaitHint(); return; }   // stage 4: gate on server connection
                    root.SetActive(false);
                    lobbyScreen.Open(RoomCatalog.CreateAsHost());
                });
            joinButton = UiKit.CreateButton("JoinButton", root.transform, "", 30, dark, plate,
                new Vector2(320f, 68f), new Vector2(0f, -32f),
                () =>
                {
                    if (!ServerReady) { ShowWaitHint(); return; }
                    root.SetActive(false);
                    joinScreen.Show();
                });
            settingsButton = UiKit.CreateButton("SettingsButton", root.transform, "", 30, dark, plate,
                new Vector2(320f, 68f), new Vector2(0f, -112f),
                () => { root.SetActive(false); settingsScreen.Show(); });
            quitButton = UiKit.CreateButton("QuitButton", root.transform, "", 30, dark, plate,
                new Vector2(320f, 68f), new Vector2(0f, -192f),
                Application.Quit);

            // stage 4: bottom-right server status (live) + centre wait toast (2.5s)
            cornerStatusText = UiKit.CreateText("ServerStatus", root.transform, "", 19, new Color(1f, 1f, 1f, 0.70f),
                TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36f, 34f));
            waitHintText = UiKit.CreateText("WaitHint", root.transform, "", 26, new Color(0.98f, 0.75f, 0.35f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -286f),
                FontStyle.Bold);
            waitHintText.gameObject.SetActive(false);

            // sub-screens live on the same object, wired to this menu
            lobbyScreen = gameObject.AddComponent<RoomLobbyUi>();
            lobbyScreen.Init(canvas.transform, events, this);
            joinScreen = gameObject.AddComponent<JoinRoomUi>();
            joinScreen.Init(canvas.transform, lobbyScreen, this);
            settingsScreen = gameObject.AddComponent<SettingsUi>();
            settingsScreen.Init(canvas.transform, this);
            onlineScreen = gameObject.AddComponent<OnlinePanelUi>();
            onlineScreen.Init(canvas.transform, this);

            events.GameStarted.AddListener(Hide);
            LocalizationService.LanguageChanged += RefreshTexts;
            RefreshTexts();
        }

        private void OnDestroy()
        {
            LocalizationService.LanguageChanged -= RefreshTexts;
        }

        public void Show() => root.SetActive(true);

        public void Hide() => root.SetActive(false);

        private void Update()
        {
            if (root == null || !root.activeSelf) return;

            // bottom-right live status: connecting… / connected · server-assigned name
            var net = NetClient.Instance;
            if (ServerReady)
                cornerStatusText.text = LocalizationService.L(LKey.OnlineConnectedAs, net.AssignedName);
            else
                cornerStatusText.text = LocalizationService.L(LKey.OnlineConnectingServer);

            // centre toast countdown
            if (waitHintText.gameObject.activeSelf)
            {
                waitHintTimer -= UnityEngine.Time.deltaTime;
                if (waitHintTimer <= 0f) waitHintText.gameObject.SetActive(false);
            }
        }

        /// <summary>未连接时点建房/加入：中央提示“服务器尚未连接成功请稍等…”（2.5 秒）。</summary>
        private void ShowWaitHint()
        {
            waitHintText.text = LocalizationService.L(LKey.OnlineWaitServer);
            waitHintText.gameObject.SetActive(true);
            waitHintTimer = 2.5f;
        }

        private void RefreshTexts()
        {
            titleText.text = LocalizationService.L(LKey.Title);
            subtitleText.text = LocalizationService.L(LKey.Subtitle);
            onlineButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.OnlineTitle);
            hostButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.MenuHost);
            joinButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.MenuJoin);
            settingsButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.MenuSettings);
            quitButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.MenuQuit);
        }

        /// <summary>Shared helper: a full-stretch screen root under a canvas.</summary>
        public static GameObject CreateScreenRoot(string screenName, Transform canvas)
        {
            var go = new GameObject(screenName);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(canvas, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }
    }
}
