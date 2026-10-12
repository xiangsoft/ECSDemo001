using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Xiangsoft.Game.Ui;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Room lobby: the member list of the room you are in (4 slots), player count, and the
    /// start button — enabled only for the host with 2+ players. Opened by hosting or joining.
    /// </summary>
    public class RoomLobbyUi : MonoBehaviour
    {
        private GameObject root;
        private Text titleText;
        private Text codeText;
        private Text countText;
        private Text minPlayersText;
        private Button startButton;
        private Button backButton;
        private RectTransform listHolder;
        private readonly List<GameObject> memberRows = new List<GameObject>();

        private RoomSession session;
        private CrossRoadEvents events;
        private MainMenuUi menu;

        public void Init(Transform canvas, CrossRoadEvents events, MainMenuUi menu)
        {
            this.events = events;
            this.menu = menu;

            root = MainMenuUi.CreateScreenRoot("RoomLobby", canvas);
            UiPanel.Create("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.25f)).Show();

            var card = UiKit.CreateCard("Card", root.transform, new Vector2(640f, 660f), Vector2.zero,
                new Color(0.45f, 0.71f, 0.30f));

            titleText = UiKit.CreateText("Title", card, "", 48, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 272f),
                FontStyle.Bold);
            codeText = UiKit.CreateText("Code", card, "", 26, new Color(0.95f, 0.85f, 0.40f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 218f));

            // member rows hang under this anchor so Open() can rebuild them
            var holderGo = new GameObject("Members");
            listHolder = holderGo.AddComponent<RectTransform>();
            listHolder.SetParent(card, false);
            listHolder.anchorMin = new Vector2(0.5f, 0.5f);
            listHolder.anchorMax = new Vector2(0.5f, 0.5f);
            listHolder.pivot = new Vector2(0.5f, 0.5f);
            listHolder.anchoredPosition = Vector2.zero;

            countText = UiKit.CreateText("Count", card, "", 24, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -92f));
            minPlayersText = UiKit.CreateText("MinPlayers", card, "", 17, new Color(1f, 1f, 1f, 0.55f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -124f));

            startButton = UiKit.CreateButton("StartButton", card, "", 28, Color.white,
                new Color(0.42f, 0.66f, 0.28f), new Vector2(340f, 64f), new Vector2(0f, -190f),
                StartGame);
            backButton = UiKit.CreateButton("BackButton", card, "", 22, new Color(1f, 1f, 1f, 0.90f),
                new Color(0.18f, 0.19f, 0.23f), new Vector2(200f, 50f), new Vector2(0f, -272f),
                Back);

            LocalizationService.LanguageChanged += OnLanguageChanged;
            RoomCatalog.SessionChanged += OnSessionChanged;   // stage 2: live room updates from the server
            events.GameStarted.AddListener(() => root.SetActive(false));   // stage 3: guests close the lobby on the broadcast (host closes on click)
            root.SetActive(false);   // hidden until hosting or joining opens it
        }

        private void OnDestroy()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            RoomCatalog.SessionChanged -= OnSessionChanged;
        }

        /// <summary>房间数据变化推送（成员加入/离开）：界面开着时整体重渲染。</summary>
        private void OnSessionChanged()
        {
            if (root != null && root.activeSelf && RoomCatalog.CurrentSession != null)
                Open(RoomCatalog.CurrentSession);
        }

        /// <summary>Open with a session (hosted or joined) — rebuilds the member rows.</summary>
        public void Open(RoomSession roomSession)
        {
            session = roomSession;
            RefreshTexts();
            RebuildRows();
            root.SetActive(true);
        }

        public void Show()
        {
            if (session != null) Open(session);
        }

        private void StartGame()
        {
            // guarded by interactability: host with 2+ players
            root.SetActive(false);
            RoomCatalog.RequestStartGame(events);   // online: server validates then broadcasts; offline: raise directly
        }

        private void Back()
        {
            root.SetActive(false);
            RoomCatalog.LeaveOnline();   // online: tell the server we left
            menu.Show();
        }

        private void RebuildRows()
        {
            for (int i = 0; i < memberRows.Count; i++)
                if (memberRows[i] != null) Destroy(memberRows[i]);
            memberRows.Clear();

            // four slots, top to bottom; filled by members, then empty slots
            float[] rowY = { 150f, 92f, 34f, -24f };
            var slotBg = new Color(0.15f, 0.16f, 0.20f, 0.90f);
            var emptyColor = new Color(1f, 1f, 1f, 0.38f);

            for (int i = 0; i < RoomSession.MaxPlayers; i++)
            {
                var rowGo = new GameObject("Member" + i);
                var row = rowGo.AddComponent<RectTransform>();
                row.SetParent(listHolder, false);
                row.anchorMin = new Vector2(0.5f, 0.5f);
                row.anchorMax = new Vector2(0.5f, 0.5f);
                row.pivot = new Vector2(0.5f, 0.5f);
                row.sizeDelta = new Vector2(520f, 52f);
                row.anchoredPosition = new Vector2(0f, rowY[i]);

                var bg = rowGo.AddComponent<Image>();
                bg.color = slotBg;
                bg.raycastTarget = false;

                var member = i < session.Members.Count ? session.Members[i] : null;
                if (member != null)
                {
                    string name = member.Name ?? LocalizationService.L(LKey.LobbyYou);
                    UiKit.CreateText("Name", row, name, 24, Color.white,
                        TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 0f));
                    if (member.IsHost)
                        UiKit.CreateText("Tag", row, LocalizationService.L(LKey.LobbyHostTag), 18,
                            new Color(0.60f, 0.90f, 0.45f),
                            TextAnchor.MiddleRight, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-20f, 0f));
                }
                else
                {
                    UiKit.CreateText("Empty", row, LocalizationService.L(LKey.LobbyWaitingSlot), 20, emptyColor,
                        TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 0f)).fontStyle = FontStyle.Italic;
                }

                memberRows.Add(rowGo);
            }
        }

        private void OnLanguageChanged()
        {
            RefreshTexts();
            if (session != null && root.activeSelf) RebuildRows();
        }

        private void RefreshTexts()
        {
            titleText.text = LocalizationService.L(LKey.LobbyTitle);
            minPlayersText.text = LocalizationService.L(LKey.LobbyMinPlayers);
            backButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.Back);

            if (session != null)
            {
                codeText.text = LocalizationService.L(LKey.LobbyRoomCode, session.Code);
                countText.text = LocalizationService.L(LKey.LobbyCount, session.Members.Count, RoomSession.MaxPlayers);

                bool canStart = session.YouAreHost && session.Members.Count >= 2;
                startButton.interactable = canStart;
                startButton.GetComponentInChildren<Text>().text = session.YouAreHost
                    ? LocalizationService.L(LKey.StartGame)
                    : LocalizationService.L(LKey.LobbyGuestWait);
            }
        }
    }
}
