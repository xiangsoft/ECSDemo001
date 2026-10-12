using UnityEngine;
using UnityEngine.UI;
using Xiangsoft.Game.Ui;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Settings screen: a card with a language dropdown. Switching applies instantly —
    /// every screen re-renders itself through the LanguageChanged event.
    /// </summary>
    public class SettingsUi : MonoBehaviour
    {
        private GameObject root;
        private Text titleText;
        private Text langLabel;
        private Button backButton;
        private Dropdown dropdown;

        public void Init(Transform canvas, MainMenuUi menu)
        {
            root = MainMenuUi.CreateScreenRoot("Settings", canvas);
            UiPanel.Create("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.25f)).Show();

            var card = UiKit.CreateCard("Card", root.transform, new Vector2(640f, 460f), Vector2.zero,
                new Color(0.45f, 0.71f, 0.30f));

            titleText = UiKit.CreateText("Title", card, "", 52, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f),
                FontStyle.Bold);
            langLabel = UiKit.CreateText("LangLabel", card, "", 24, new Color(1f, 1f, 1f, 0.85f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 88f));

            var dark = new Color(0.13f, 0.13f, 0.17f);
            var plate = new Color(0.97f, 0.97f, 0.95f);

            dropdown = UiKit.CreateDropdown("Language", card, 24, dark, plate,
                new Vector2(460f, 56f), new Vector2(0f, 24f), 440f);
            dropdown.onValueChanged.AddListener(index =>
                LocalizationService.SetLanguage((GameLanguage)index));

            backButton = UiKit.CreateButton("BackButton", card, "", 22, new Color(1f, 1f, 1f, 0.90f),
                new Color(0.18f, 0.19f, 0.23f), new Vector2(200f, 50f), new Vector2(0f, -166f),
                () => { root.SetActive(false); menu.Show(); });

            LocalizationService.LanguageChanged += RefreshTexts;
            RefreshTexts();
            root.SetActive(false);   // hidden until the menu opens it
        }

        private void OnDestroy()
        {
            LocalizationService.LanguageChanged -= RefreshTexts;
        }

        public void Show()
        {
            RefreshTexts();
            root.SetActive(true);
        }

        private void RefreshTexts()
        {
            titleText.text = LocalizationService.L(LKey.SettingsTitle);
            langLabel.text = LocalizationService.L(LKey.SettingsLanguage);
            backButton.GetComponentInChildren<Text>().text = LocalizationService.L(LKey.Back);

            // fill/refresh options with native names, keeping the current selection
            int current = (int)LocalizationService.Current;
            if (dropdown.options.Count == 0)
            {
                for (int i = 0; i <= (int)GameLanguage.Vietnamese; i++)
                    dropdown.options.Add(new Dropdown.OptionData(LocalizationService.DisplayName((GameLanguage)i)));
                dropdown.value = current;
                dropdown.RefreshShownValue();
            }
            else if (dropdown.value != current)
            {
                dropdown.value = current;
                dropdown.RefreshShownValue();
            }
        }
    }
}
