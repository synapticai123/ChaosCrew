using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChaosCrew
{
    /// <summary>
    /// The round D-pad from the concept art: a translucent dish with four arrow hints and a
    /// pale knob. It sits in a fixed spot but accepts a drag started anywhere in the lower
    /// left of the screen, which keeps it usable without looking at your thumb.
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float Diameter = 370f;
        private const float KnobDiameter = 152f;
        private const float Travel = 108f;

        private RectTransform _zone;
        private RectTransform _dish;
        private RectTransform _knob;
        private readonly Image[] _arrows = new Image[4];
        private int _pointerId = -99;
        private Vector2 _origin;

        public Vector2 Value { get; private set; }
        public bool Active => _pointerId != -99;

        private static readonly Vector2[] Dirs =
        {
            Vector2.up, Vector2.right, Vector2.down, Vector2.left
        };

        public static VirtualJoystick Create(RectTransform parent)
        {
            RectTransform zone = UIKit.NewRect("StickZone", parent);
            zone.anchorMin = new Vector2(0f, 0f);
            zone.anchorMax = new Vector2(0.56f, 0.46f);
            zone.offsetMin = Vector2.zero;
            zone.offsetMax = Vector2.zero;

            var hit = zone.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;

            var js = zone.gameObject.AddComponent<VirtualJoystick>();
            js.Build(zone);
            return js;
        }

        private void Build(RectTransform zone)
        {
            _zone = zone;

            _dish = UIKit.NewRect("Dish", zone);
            _dish.anchorMin = _dish.anchorMax = new Vector2(0f, 0f);
            _dish.pivot = new Vector2(0.5f, 0.5f);
            _dish.anchoredPosition = new Vector2(215f, 235f);
            _dish.sizeDelta = new Vector2(Diameter, Diameter);

            Image dish = UIKit.NewImage("Plate", _dish, TextureLab.Circle(256), new Color(0.09f, 0.12f, 0.2f, 0.42f));
            UIKit.Stretch(dish.rectTransform);

            Image rim = UIKit.NewImage("Rim", _dish, TextureLab.Ring(256, 8), new Color(1f, 1f, 1f, 0.28f));
            UIKit.Stretch(rim.rectTransform);

            for (int i = 0; i < 4; i++)
            {
                Image arrow = UIKit.NewImage("Arrow" + i, _dish, IconLab.Get(Icon.ArrowUp),
                    new Color(1f, 1f, 1f, 0.75f));
                UIKit.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Dirs[i] * 143f, new Vector2(54f, 54f));
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f * i);
                _arrows[i] = arrow;
            }

            _knob = UIKit.NewRect("Knob", _dish);
            _knob.anchorMin = _knob.anchorMax = new Vector2(0.5f, 0.5f);
            _knob.pivot = new Vector2(0.5f, 0.5f);
            _knob.sizeDelta = new Vector2(KnobDiameter, KnobDiameter);

            UIKit.NewImage("Shadow", _knob, TextureLab.Circle(192), new Color(0f, 0f, 0f, 0.3f))
                .rectTransform.let(rt => UIKit.Stretch(rt, -6f, -14f, -6f, 2f));
            Image cap = UIKit.NewImage("Cap", _knob, TextureLab.Circle(192), new Color(0.92f, 0.94f, 0.96f, 0.97f));
            UIKit.Stretch(cap.rectTransform);
            Image gloss = UIKit.NewImage("Gloss", _knob, TextureLab.Ring(192, 10), new Color(1f, 1f, 1f, 0.65f));
            UIKit.Stretch(gloss.rectTransform, 8f, 8f, 8f, 8f);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (Active) return;
            _pointerId = e.pointerId;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_zone, e.position, e.pressEventCamera,
                    out Vector2 local))
            {
                // Dragging far from the dish still works: treat the touch point as the centre.
                _origin = Vector2.Distance(local, _dish.anchoredPosition) <= Diameter * 0.55f
                    ? _dish.anchoredPosition
                    : local;
            }
            UpdateFrom(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            UpdateFrom(e);
        }

        private void UpdateFrom(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_zone, e.position, e.pressEventCamera,
                    out Vector2 local))
                return;

            Vector2 delta = Vector2.ClampMagnitude(local - _origin, Travel);
            _knob.anchoredPosition = delta;

            Vector2 raw = delta / Travel;
            Value = raw.magnitude < 0.16f ? Vector2.zero : raw;
            Highlight();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            ResetStick();
        }

        private void Highlight()
        {
            for (int i = 0; i < 4; i++)
            {
                float lit = Value.sqrMagnitude < 0.02f ? 0f : Mathf.Clamp01(Vector2.Dot(Value.normalized, Dirs[i]));
                _arrows[i].color = new Color(1f, 1f, 1f, Mathf.Lerp(0.45f, 1f, lit));
                float s = Mathf.Lerp(1f, 1.18f, lit);
                _arrows[i].rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }

        public void ResetStick()
        {
            _pointerId = -99;
            Value = Vector2.zero;
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
            Highlight();
        }
    }

    internal static class RectChain
    {
        /// <summary>Lets a freshly created rect be positioned inline at its call site.</summary>
        public static void let(this RectTransform rt, System.Action<RectTransform> f) => f(rt);
    }
}
