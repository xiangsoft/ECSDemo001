using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Xiangsoft.Game.Ui;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Join screen: a 3-per-row grid of rooms fetched from the catalog (the server later).
    /// Clicking a room joins it and opens the lobby. Full rooms are disabled.
    /// </summary>
    public class JoinRoomUi : MonoBehaviour
    {
        private GameObject root;
        private Text titleText;
        private Text descText;
        private Text statusText;
        private RectTransform gridHolder;
        private Button refreshButton;
        private Button backButton;
        private readonly List<GameObject> roomCards = new List<GameObject>();

        private RoomLobbyUi lobby;
        private MainMenuUi menu;
        private bool joining;

        public void Init(Transform canvas, RoomLobbyUi lobby, MainMenuUi menu)
        {
            this.lobby = lobby;
            this.menu = menu;

            root = MainMenuUi.CreateScreenRoot("JoinRoom", canvas);
            UiPanel.Create("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.25f)).Show();

            var card = UiKit.CreateCard("Card", root.transform, new Vector2(760f, 660f), Vector2.zero,
                new Color(0.45f, 0.71f, 0.30f));

            titleText = UiKit.CreateText("Title", card, "", 48, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 272f),
                FontStyle.Bold);
            descText = UiKit.CreateText("Desc", card, "", 21, new Color(1f, 1f, 1f, 0.75f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 222f));

            // 3-per-row room grid
            var gridGo = new GameObject("RoomGrid");
            gridHolder = gridGo.AddComponent<RectTransform>();
            gridHolder.SetParent(card, false);
            gridHolder.anchorMin = new Vector2(0.5f, 0.5f);
            gridHolder.anchorMax = new Vector2(0.5f, 0.5f);
            gridHolder.pivot = new Vector2(0.5f, 0.5f);
            gridHolder.anchoredPosition = new Vector2(0f, 40f);
            gridHolder.sizeDelta = new Vector2(660f, 320f);

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(206f, 140f);
            grid.spacing = new Vector2(16f, 18f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            statusText = UiKit.CreateText("Status", card, "", 24, new Color(0.95f, 0.85f, 0.40f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f));

            refreshButton = UiKit.CreateButton("RefreshButton", card, "", 22, new Color(0.13f, 0.13f, 0.17f),
                new Color(0.97f, 0.97f, 0.95f), new Vector2(220f, 56f), new Vector2(-128f, -218f),
                RebuildGrid);
            backButton = UiKit.CreateButton("BackButton", card, "", 22, new Color(1f, 1f, 1f, 0.90f),
                new Color(0.18f, 0.19f, 0.23f), new Vector2(220f, 56f), new Vector2(128f, -218f),
                Back);

            LocalizationService.LanguageChanged += OnLanguageChanged;
            root.SetActive(false);   // hidden until the menu opens it
        }

        private void OnDestroy()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
        }

        private void Back()
        {
            root.SetActive(false);
            menu.Show();
        }

        public void Show()
        {
            RefreshTexts();
            statusText.text = "";
            RebuildGrid();
            root.SetActive(true);
        }

        private void RebuildGrid()
        {
            for (int i = 0; i < roomCards.Count; i++)
                if (roomCards[i] != null) Destroy(roomCards[i]);
            roomCards.Clear();

            var rooms = RoomCatalog.FetchRooms();   // the server call goes here later
            if (rooms.Count == 0)
            {
                statusText.text = LocalizationService.L(LKey.JoinEmpty);
                return;
            }

            var dark = new Color(0.13f, 0.13f, 0.17f);
            foreach (var room in rooms)
            {
                bool full = room.PlayerCount >= RoomSession.MaxPlayers;

                var cardGo = new GameObject("Room_" + room.Code);
                var cardRect = cardGo.AddComponent<RectTransform>();
                cardRect.SetParent(gridHolder, false);

                var bg = cardGo.AddComponent<Image>();
                bg.color = full ? new Color(0.14f, 0.14f, 0.16f, 0.95f) : new Color(0.17f, 0.18f, 0.22f, 0.95f);

                var button = cardGo.AddComponent<Button>();
                button.targetGraphic = bg;
                button.interactable = !full;

                var codeColor = full ? new Color(0.6f, 0.6f, 0.62f) : new Color(0.95f, 0.85f, 0.40f);
                UiKit.CreateText("Code", cardRect, room.Code, 30, codeColor,
                    TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 38f), FontStyle.Bold);
                UiKit.CreateText("Host", cardRect, room.HostName, 19, new Color(1f, 1f, 1f, 0.85f),
                    TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, -2f));

                string countLine = room.PlayerCount + "/" + RoomSession.MaxPlayers +
                    (full ? "  " + LocalizationService.L(LKey.LobbyFull) : "");
                UiKit.CreateText("Count", cardRect, countLine, 17, new Color(1f, 1f, 1f, 0.55f),
                    TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, -38f));

                var target = room;   // capture
                button.onClick.AddListener(() => { if (!joining) StartCoroutine(JoinFlow(target)); });

                roomCards.Add(cardGo);
            }
        }

        private IEnumerator JoinFlow(RoomInfo room)
        {
            joining = true;
            statusText.text = LocalizationService.L(LKey.JoinJoining, room.Code);

            yield return new WaitForSeconds(0.8f);

            joining = false;
            var session = RoomCatalog.JoinAsGuest(room);   // the server call goes here later
            root.SetActive(false);
            lobby.Open(session);
        }

        private void OnLanguageChanged()
        {
            RefreshTexts();
            if (root.activeSelf) RebuildGrid();
        }

        private void RefreshTexts()
        {
            titleText.text = LocalizationService.L(LKey.JoinTitle);
            descText.text = LocalizationService.L(LKey.JoinDesc);
            refreshButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.JoinRefresh);
            backButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.Back);
        }
    }
}
