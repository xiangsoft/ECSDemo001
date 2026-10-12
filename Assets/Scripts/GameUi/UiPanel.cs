using UnityEngine;
using UnityEngine.UI;

namespace Xiangsoft.Game.Ui
{
    /// <summary>
    /// A full-stretch dim panel with a CanvasGroup that fades in and out.
    /// Children (labels, images) inherit the fade. Non-interactive.
    /// </summary>
    public class UiPanel : MonoBehaviour
    {
        private const float FadeSpeed = 6f;

        private CanvasGroup group;
        private float targetAlpha;

        /// <summary>Create a full-screen panel with the given background colour, hidden by default.</summary>
        public static UiPanel Create(string panelName, Transform parent, Color background)
        {
            var go = new GameObject(panelName);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = background;
            image.raycastTarget = false;

            var panel = go.AddComponent<UiPanel>();
            panel.group = go.AddComponent<CanvasGroup>();
            panel.group.blocksRaycasts = false;
            panel.group.alpha = 0f;
            panel.targetAlpha = 0f;
            return panel;
        }

        public void Show() => targetAlpha = 1f;

        public void Hide() => targetAlpha = 0f;

        private void Update()
        {
            if (group == null) return;
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, FadeSpeed * Time.deltaTime);
        }
    }
}
