using UnityEngine;
using UnityEngine.UI;
using Xiangsoft.Game.Ui;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// UGUI HUD: score, controls hint and the death overlay (fades in via UiPanel).
    /// Pure presentation — built from UiKit primitives, listens to events, owns nothing else.
    /// </summary>
    public class HudRenderer : MonoBehaviour
    {
        private Text scoreText;
        private Text hintText;
        private Text deathTitleText;
        private Text deathRetryText;
        private Text reasonText;
        private UiPanel deathPanel;
        private int scoreCache;

        public void Init(CrossRoadEvents events)
        {
            BuildUi();
            events.ScoreChanged.AddListener(OnScoreChanged);
            events.DeathRequested.AddListener(OnDeathRequested);
            events.Restarted.AddListener(OnRestarted);
            events.GameStarted.AddListener(OnGameStarted);
            LocalizationService.LanguageChanged += RefreshTexts;
            RefreshTexts();
        }

        private void OnDestroy()
        {
            LocalizationService.LanguageChanged -= RefreshTexts;
        }

        private void BuildUi()
        {
            var canvas = UiKit.CreateCanvas("HudCanvas");
            canvas.transform.SetParent(transform, false);   // canvas lives and dies with the HUD object

            var dark = new Color(0.13f, 0.13f, 0.17f);
            var grey = new Color(0.20f, 0.20f, 0.25f);

            scoreText = UiKit.CreateText("Score", canvas.transform, "", 40, dark,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -18f),
                FontStyle.Bold);

            hintText = UiKit.CreateText("Hint", canvas.transform, "", 18, grey,
                TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 16f));

            // in menus the game HUD is hidden — it appears with GameStarted
            scoreText.gameObject.SetActive(false);
            hintText.gameObject.SetActive(false);

            deathPanel = UiPanel.Create("DeathPanel", canvas.transform, new Color(0f, 0f, 0f, 0.45f));

            deathTitleText = UiKit.CreateText("Title", deathPanel.transform, "", 56, new Color(0.95f, 0.25f, 0.20f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f),
                FontStyle.Bold);
            reasonText = UiKit.CreateText("Reason", deathPanel.transform, "", 30, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            deathRetryText = UiKit.CreateText("Retry", deathPanel.transform, "", 26, new Color(0.90f, 0.90f, 0.90f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f));
        }

        private void OnScoreChanged(int score)
        {
            scoreCache = score;
            scoreText.text = LocalizationService.L(LKey.Score) + "  " + score;
        }

        private void OnDeathRequested(string reason, DeathKind kind)
        {
            reasonText.text = LocalizationService.L(reason);   // reason is a key — translate at display time
            deathPanel.Show();
        }

        private void OnRestarted()
        {
            deathPanel.Hide();
            OnScoreChanged(0);
        }

        private void OnGameStarted()
        {
            scoreText.gameObject.SetActive(true);
            hintText.gameObject.SetActive(true);
        }

        private void RefreshTexts()
        {
            scoreText.text = LocalizationService.L(LKey.Score) + "  " + scoreCache;
            hintText.text = LocalizationService.L(LKey.Hint);
            deathTitleText.text = LocalizationService.L(LKey.YouDied);
            deathRetryText.text = LocalizationService.L(LKey.PressRestart);
        }
    }
}
