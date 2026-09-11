using System;
using UnityEngine;
using UnityEngine.UI;

namespace ChaosCrew
{
    /// <summary>
    /// Thin helpers for assembling uGUI at runtime. Keeping the whole UI in code means the
    /// prototype has no prefab wiring to break and no scene to keep in sync.
    /// </summary>
    public static class UIKit
    {
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                catch { _font = null; }
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                    catch { _font = null; }
                }
                if (_font == null)
                    _font = UnityEngine.Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica" }, 32);
                return _font;
            }
        }

        // ------------------------------------------------------------ construction

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        public static Image Panel(string name, Transform parent, Color color, int radius = 24)
        {
            Image img = NewImage(name, parent, TextureLab.RoundedRect(radius), color);
            img.raycastTarget = true;
            return img;
        }

        public static Image Outline(string name, Transform parent, Color color, int radius = 24, int thickness = 3)
        {
            return NewImage(name, parent, TextureLab.RoundedOutline(radius, thickness), color);
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            RectTransform rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        /// <summary>Big, thumb-friendly button: rounded plate, drop shadow, label.</summary>
        public static Button Button(string name, Transform parent, string label, Color bg, Color fg,
            int fontSize, Action onClick)
        {
            RectTransform rt = NewRect(name, parent);

            Image shadow = NewImage("Shadow", rt, TextureLab.RoundedRect(28), new Color(0f, 0f, 0f, 0.28f));
            Stretch(shadow.rectTransform, 0f, -6f, 0f, -6f);

            Image plate = NewImage("Plate", rt, TextureLab.RoundedRect(28), bg);
            plate.raycastTarget = true;
            Stretch(plate.rectTransform);

            Text text = Label("Label", rt, label, fontSize, fg);
            Stretch(text.rectTransform, 18f, 0f, 18f, 0f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = plate;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            rt.gameObject.AddComponent<ButtonPunch>();
            return btn;
        }

        /// <summary>Horizontal progress bar. Returns the fill image; set fillAmount-style width via SetBar.</summary>
        public static Image Bar(string name, Transform parent, Color back, Color fill, int radius = 12)
        {
            Image bg = NewImage(name, parent, TextureLab.RoundedRect(radius), back);
            Image f = NewImage("Fill", bg.rectTransform, TextureLab.RoundedRect(radius), fill);
            f.rectTransform.anchorMin = new Vector2(0f, 0f);
            f.rectTransform.anchorMax = new Vector2(0f, 1f);
            f.rectTransform.pivot = new Vector2(0f, 0.5f);
            f.rectTransform.offsetMin = new Vector2(0f, 0f);
            f.rectTransform.offsetMax = new Vector2(0f, 0f);
            return f;
        }

        public static void SetBar(Image fill, float t01)
        {
            RectTransform parent = fill.rectTransform.parent as RectTransform;
            float w = parent != null ? parent.rect.width : 100f;
            fill.rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, w * Mathf.Clamp01(t01)), 0f);
        }

        // ------------------------------------------------------------ layout

        public static void Stretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Anchors a rect to a normalised point and sizes it in reference pixels.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform TopBar(RectTransform rt, float height, float inset = 0f)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(inset, -height);
            rt.offsetMax = new Vector2(-inset, 0f);
            return rt;
        }

        public static RectTransform BottomBar(RectTransform rt, float height, float inset = 0f)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(inset, 0f);
            rt.offsetMax = new Vector2(-inset, height);
            return rt;
        }

        public static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return m + ":" + s.ToString("00");
        }
    }

    /// <summary>Tiny squash-and-stretch on press so buttons feel alive without any assets.</summary>
    public sealed class ButtonPunch : MonoBehaviour,
        UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IPointerUpHandler
    {
        private Vector3 _base = Vector3.one;
        private float _t = 1f;

        private void Awake() => _base = transform.localScale;

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) => _t = 0.93f;
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) => _t = 1f;

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _base * _t, 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime));
        }

        private void OnDisable() => transform.localScale = _base;
    }
}
