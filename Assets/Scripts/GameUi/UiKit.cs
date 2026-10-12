using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Xiangsoft.Game.Ui
{
    /// <summary>
    /// Runtime UGUI toolkit: overlay canvases, anchored labels, buttons, input fields —
    /// built entirely from code, zero prefabs, zero assets. Chinese glyphs render through
    /// the dynamic font's OS fallback. Intended for quick game jams and prototypes.
    /// </summary>
    public static class UiKit
    {
        private static Font cachedFont;

        /// The built-in dynamic font (LegacyRuntime on modern Unity, Arial on older).
        public static Font DefaultFont
        {
            get
            {
                if (cachedFont == null)
                {
                    cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return cachedFont;
            }
        }

        /// One EventSystem for the whole game (idempotent — safe to call from every screen).
        public static EventSystem EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null) return existing;

            var go = new GameObject("EventSystem");
            var es = go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();   // Input System UI backend (replaces StandaloneInputModule)
            return es;
        }

        /// <summary>
        /// Screen-space overlay canvas with a reference resolution (scales to fit,
        /// 50/50 width-height match).
        /// </summary>
        public static Canvas CreateCanvas(string canvasName, float referenceWidth = 1280f, float referenceHeight = 720f)
        {
            var go = new GameObject(canvasName);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>
        /// Anchored text label. anchorMin/anchorMax pick the anchor point,
        /// offset shifts it from there (anchoredPosition), content overflows freely.
        /// </summary>
        public static Text CreateText(string textName, Transform parent, string content, int fontSize, Color color,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset,
            FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(textName);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.anchoredPosition = offset;

            var text = go.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// A centred opaque card with an optional coloured top bar — the building block
        /// of pop-up screens. Children anchor to the card; the world stays visible around it.
        /// </summary>
        public static RectTransform CreateCard(string cardName, Transform parent, Vector2 size, Vector2 offset,
            Color? topBarColor = null)
        {
            var go = new GameObject(cardName);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.09f, 0.10f, 0.14f, 0.96f);   // deep slate card
            image.raycastTarget = true;                            // swallow clicks behind the card

            if (topBarColor.HasValue)
            {
                var bar = new GameObject(cardName + "TopBar");
                var barRect = bar.AddComponent<RectTransform>();
                barRect.SetParent(rect, false);
                barRect.anchorMin = new Vector2(0f, 1f);
                barRect.anchorMax = new Vector2(1f, 1f);
                barRect.pivot = new Vector2(0.5f, 1f);
                barRect.sizeDelta = new Vector2(0f, 8f);
                var barImage = bar.AddComponent<Image>();
                barImage.color = topBarColor.Value;
                barImage.raycastTarget = false;
            }
            return rect;
        }

        /// <summary>
        /// Centred button: coloured plate + bold label, built-in colour-tint hover/press feedback.
        /// </summary>
        public static Button CreateButton(string buttonName, Transform parent, string label, int fontSize,
            Color textColor, Color background, Vector2 size, Vector2 offset,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(buttonName);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;

            var image = go.AddComponent<Image>();
            image.color = background;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            CreateText("Label", rect, label, fontSize, textColor,
                TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, FontStyle.Bold);

            button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>
        /// Single-line input field: plate + content text + italic placeholder + caret.
        /// </summary>
        public static InputField CreateInputField(string fieldName, Transform parent, string placeholder, int fontSize,
            Color textColor, Color background, Vector2 size, Vector2 offset)
        {
            var go = new GameObject(fieldName);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;

            var image = go.AddComponent<Image>();
            image.color = background;

            var input = go.AddComponent<InputField>();

            var content = CreateText("Text", rect, "", fontSize, textColor,
                TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 0f));
            content.supportRichText = false;

            var hint = CreateText("Placeholder", rect, placeholder, fontSize,
                new Color(textColor.r, textColor.g, textColor.b, 0.40f),
                TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 0f));
            hint.fontStyle = FontStyle.Italic;

            input.textComponent = content;
            input.placeholder = hint;
            input.caretColor = textColor;
            input.selectionColor = new Color(0.30f, 0.50f, 0.90f, 0.55f);
            return input;
        }

        /// <summary>
        /// A dropdown picker, built entirely from code: caption label + expandable template
        /// (viewport/mask/scroll) + item (background/checkmark square/label).
        /// Call AddOptions on the returned Dropdown; item height is fixed at 44px.
        /// </summary>
        public static Dropdown CreateDropdown(string dropName, Transform parent, int fontSize,
            Color textColor, Color background, Vector2 size, Vector2 offset, float listHeight)
        {
            var go = new GameObject(dropName);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;

            var plate = go.AddComponent<Image>();
            plate.color = background;

            var dropdown = go.AddComponent<Dropdown>();

            // caption (selected value)
            dropdown.captionText = CreateText("Label", rect, "", fontSize, textColor,
                TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 0f));

            // little arrow square on the right
            var arrowGo = new GameObject("Arrow");
            var arrowRect = arrowGo.AddComponent<RectTransform>();
            arrowRect.SetParent(rect, false);
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(10f, 10f);
            arrowRect.anchoredPosition = new Vector2(-18f, 0f);
            var arrowImage = arrowGo.AddComponent<Image>();
            arrowImage.color = new Color(textColor.r, textColor.g, textColor.b, 0.45f);
            arrowImage.raycastTarget = false;

            // template (shown while open): plate + viewport(mask) + scroll + content + item
            var templateGo = new GameObject("Template");
            var templateRect = templateGo.AddComponent<RectTransform>();
            templateRect.SetParent(rect, false);
            templateRect.anchorMin = new Vector2(0f, 1f);
            templateRect.anchorMax = new Vector2(1f, 1f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0f, listHeight);
            var templateImage = templateGo.AddComponent<Image>();
            templateImage.color = background;

            var viewportGo = new GameObject("Viewport");
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.SetParent(templateRect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(6f, 6f);
            viewportRect.offsetMax = new Vector2(-6f, -6f);
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = background;
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content");
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, listHeight);

            var itemGo = new GameObject("Item");
            var itemRect = itemGo.AddComponent<RectTransform>();
            itemRect.SetParent(contentRect, false);
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = new Vector2(0f, 44f);

            var itemToggle = itemGo.AddComponent<Toggle>();

            var itemBgGo = new GameObject("Item Background");
            var itemBgRect = itemBgGo.AddComponent<RectTransform>();
            itemBgRect.SetParent(itemRect, false);
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.offsetMin = Vector2.zero;
            itemBgRect.offsetMax = Vector2.zero;
            var itemBg = itemBgGo.AddComponent<Image>();
            itemBg.color = new Color(1f, 1f, 1f, 0.35f);
            itemToggle.targetGraphic = itemBg;

            var checkGo = new GameObject("Item Checkmark");
            var checkRect = checkGo.AddComponent<RectTransform>();
            checkRect.SetParent(itemRect, false);
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(12f, 12f);
            checkRect.anchoredPosition = new Vector2(20f, 0f);
            var checkImage = checkGo.AddComponent<Image>();
            checkImage.color = new Color(0.42f, 0.66f, 0.28f);
            itemToggle.graphic = checkImage;

            dropdown.itemText = CreateText("Item Label", itemRect, "", fontSize, textColor,
                TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(38f, 0f));

            var scroll = templateGo.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;

            dropdown.template = templateRect;
            templateGo.SetActive(false);
            return dropdown;
        }
    }
}
