using UnityEngine;
using UnityEngine.UI;
using Xiangsoft.Game;
using Xiangsoft.Game.Ui;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// 联机对战入口（阶段 4 真机验证）：服务器地址 + 玩家名 + 连接按钮。
    /// 连接成功后回到主菜单——“建立房间 / 加入房间”自动切换为真实网络流程
    /// （RoomCatalog 双模式）。状态行实时显示连接状态（含断线自愈提示）。
    /// </summary>
    public class OnlinePanelUi : MonoBehaviour
    {
        private GameObject root;
        private Text titleText;
        private Text addressLabel;
        private Text statusText;
        private InputField addressInput;
        private Button connectButton;
        private Button backButton;

        public void Init(Transform canvas, MainMenuUi menu)
        {
            root = MainMenuUi.CreateScreenRoot("OnlinePanel", canvas.transform);
            UiPanel.Create("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.35f)).Show();

            var card = UiKit.CreateCard("Card", root.transform, new Vector2(620f, 620f), Vector2.zero,
                new Color(0.36f, 0.56f, 0.82f));

            titleText = UiKit.CreateText("Title", card, "", 40, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 244f),
                FontStyle.Bold);

            addressLabel = UiKit.CreateText("AddressLabel", card, "", 20, new Color(1f, 1f, 1f, 0.75f),
                TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(120f, 118f));
            addressInput = UiKit.CreateInputField("AddressInput", card, "127.0.0.1:7777", 24,
                Color.white, new Color(0.15f, 0.16f, 0.20f, 0.90f), new Vector2(420f, 52f), new Vector2(0f, 62f));

            statusText = UiKit.CreateText("Status", card, "", 19, new Color(0.60f, 0.90f, 0.45f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -76f));

            connectButton = UiKit.CreateButton("ConnectButton", card, "", 26, Color.white,
                new Color(0.42f, 0.66f, 0.28f), new Vector2(340f, 60f), new Vector2(0f, -160f),
                OnConnect);
            backButton = UiKit.CreateButton("BackButton", card, "", 20, new Color(1f, 1f, 1f, 0.90f),
                new Color(0.18f, 0.19f, 0.23f), new Vector2(200f, 48f), new Vector2(0f, -244f),
                () => { root.SetActive(false); menu.Show(); });

            LocalizationService.LanguageChanged += RefreshTexts;
            RefreshTexts();
            root.SetActive(false);
        }

        private void OnDestroy()
        {
            LocalizationService.LanguageChanged -= RefreshTexts;
        }

        public void Show()
        {
            root.SetActive(true);
            RefreshStatus();
        }

        /// <summary>连接：解析 host[:port]（缺省 7777）→ NetClient.ConnectServer。</summary>
        private void OnConnect()
        {
            var net = NetClient.Instance;
            if (net == null)
            {
                GameLog.Warn("net", "NetClient 不存在（装配异常）");
                return;
            }

            string address = string.IsNullOrEmpty(addressInput.text) ? "127.0.0.1:7777" : addressInput.text.Trim();
            string host = address;
            int port = 7777;
            int colon = address.LastIndexOf(':');
            if (colon > 0 && int.TryParse(address.Substring(colon + 1), out var parsed))
            {
                host = address.Substring(0, colon);
                port = parsed;
            }

            // stage 4: no self-reported name — the server assigns it in the Hello ack
            net.ConnectServer(host, port);
            RefreshStatus();
        }

        private void Update()
        {
            if (root != null && root.activeSelf) RefreshStatus();
        }

        /// <summary>状态行：未连接 / 连接中 / 已连接（联机后建房/加入走真实网络）。</summary>
        private void RefreshStatus()
        {
            var net = NetClient.Instance;
            if (net != null && net.Online && net.Connected)
                statusText.text = LocalizationService.L(LKey.OnlineConnected);
            else if (net != null && net.Online)
                statusText.text = LocalizationService.L(LKey.OnlineConnecting);
            else
                statusText.text = LocalizationService.L(LKey.OnlineOffline);
        }

        private void RefreshTexts()
        {
            titleText.text = LocalizationService.L(LKey.OnlineTitle);
            addressLabel.text = LocalizationService.L(LKey.OnlineAddress);
            connectButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.OnlineConnect);
            backButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.Back);
        }
    }
}
