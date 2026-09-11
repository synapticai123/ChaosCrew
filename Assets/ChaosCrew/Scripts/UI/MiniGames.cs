using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChaosCrew
{
    /// <summary>Click/press relay so individual widgets do not each need their own component.</summary>
    public sealed class Pressable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action OnDown;
        public System.Action OnUp;

        public void OnPointerDown(PointerEventData e) => OnDown?.Invoke();
        public void OnPointerUp(PointerEventData e) => OnUp?.Invoke();

        public static Pressable Attach(GameObject go, System.Action down, System.Action up = null)
        {
            var img = go.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            var p = go.AddComponent<Pressable>();
            p.OnDown = down;
            p.OnUp = up;
            return p;
        }
    }

    /// <summary>
    /// Base for the touch tasks. Opens as a modal over the HUD, reports success or a cancel,
    /// and tears itself down. Adding a task type means one subclass plus one switch entry.
    /// </summary>
    public abstract class MiniGame : MonoBehaviour
    {
        protected RectTransform Content;
        protected SfxSynth Audio;
        protected Text Hint;

        private System.Action<bool> _done;
        private bool _closed;

        public static MiniGame Open(RectTransform parent, StationDef station, SfxSynth sfx,
            System.Action<bool> done)
        {
            RectTransform root = UIKit.NewRect("MiniGame", parent);
            UIKit.Stretch(root);

            Image dim = UIKit.NewImage("Dim", root, TextureLab.Solid(), new Color(0f, 0f, 0f, 0.72f));
            dim.raycastTarget = true;
            UIKit.Stretch(dim.rectTransform);

            Image panel = UIKit.Panel("Panel", root, Palette.Panel, 30);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(940f, 1120f));

            Text title = UIKit.Label("Title", panel.rectTransform, station.DisplayName, 52, Palette.Ink);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -60f), new Vector2(860f, 70f));

            var game = CreateFor(station.MiniGame, root.gameObject);
            game.Audio = sfx;
            game._done = done;

            Text hint = UIKit.Label("Hint", panel.rectTransform, "", 32, Palette.InkMuted);
            UIKit.Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -140f), new Vector2(840f, 60f));
            game.Hint = hint;

            RectTransform content = UIKit.NewRect("Content", panel.rectTransform);
            UIKit.Place(content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(840f, 720f));
            game.Content = content;

            Button cancel = UIKit.Button("Cancel", panel.rectTransform, "Abbrechen",
                Palette.BackdropAlt, Palette.InkMuted, 36, () => game.Close(false));
            UIKit.Place((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(420f, 96f));

            game.Build();
            return game;
        }

        private static MiniGame CreateFor(MiniGameKind kind, GameObject host)
        {
            switch (kind)
            {
                case MiniGameKind.Wires: return host.AddComponent<WiresGame>();
                case MiniGameKind.HoldGauge: return host.AddComponent<HoldGaugeGame>();
                case MiniGameKind.TimingBar: return host.AddComponent<TimingBarGame>();
                case MiniGameKind.Mash: return host.AddComponent<MashGame>();
                case MiniGameKind.Keypad: return host.AddComponent<KeypadGame>();
                default: return host.AddComponent<TapItemsGame>();
            }
        }

        protected abstract void Build();

        protected void Succeed()
        {
            Audio?.Play(Sfx.TaskDone);
            Close(true);
        }

        public void Close(bool success)
        {
            if (_closed) return;
            _closed = true;
            _done?.Invoke(success);
            Destroy(gameObject);
        }

        protected void Beep(Sfx s, float pitch = 1f) => Audio?.Play(s, pitch);
    }

    // ------------------------------------------------------------------ Wires

    /// <summary>Tap a left terminal, then its matching colour on the right.</summary>
    public sealed class WiresGame : MiniGame
    {
        private const int Count = 4;

        private readonly List<Image> _left = new List<Image>();
        private readonly List<Image> _right = new List<Image>();
        private readonly List<int> _rightOrder = new List<int>();
        private readonly HashSet<int> _connected = new HashSet<int>();
        private int _selected = -1;
        private RectTransform _wireLayer;

        private static readonly Color[] WireColors =
        {
            Palette.Bad, Palette.Info, Palette.Good, Palette.Accent
        };

        protected override void Build()
        {
            Hint.text = "Verbinde gleiche Farben";

            _wireLayer = UIKit.NewRect("Wires", Content);
            UIKit.Stretch(_wireLayer);

            for (int i = 0; i < Count; i++) _rightOrder.Add(i);
            var rng = new DeterministicRng(Random.Range(1, 99999));
            rng.Shuffle(_rightOrder);

            float step = 170f;
            float top = 250f;

            for (int i = 0; i < Count; i++)
            {
                int idx = i;
                Image l = MakeNode(-300f, top - i * step, WireColors[i]);
                Pressable.Attach(l.gameObject, () => SelectLeft(idx));
                _left.Add(l);

                Image r = MakeNode(300f, top - i * step, WireColors[_rightOrder[i]]);
                int slot = i;
                Pressable.Attach(r.gameObject, () => SelectRight(slot));
                _right.Add(r);
            }
        }

        private Image MakeNode(float x, float y, Color c)
        {
            Image img = UIKit.NewImage("Node", Content, TextureLab.RoundedRect(18), c);
            UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y), new Vector2(150f, 110f));
            img.raycastTarget = true;
            return img;
        }

        private void SelectLeft(int i)
        {
            if (_connected.Contains(i)) return;
            _selected = i;
            Beep(Sfx.Tap, 1.2f);
            Refresh();
        }

        private void SelectRight(int slot)
        {
            if (_selected < 0) return;
            if (_rightOrder[slot] != _selected)
            {
                Beep(Sfx.Error);
                _selected = -1;
                Refresh();
                return;
            }

            _connected.Add(_selected);
            DrawWire(_left[_selected].rectTransform.anchoredPosition,
                _right[slot].rectTransform.anchoredPosition, WireColors[_selected]);
            _selected = -1;
            Beep(Sfx.Confirm);
            Refresh();

            if (_connected.Count >= Count) Succeed();
        }

        private void DrawWire(Vector2 a, Vector2 b, Color c)
        {
            Vector2 delta = b - a;
            Image wire = UIKit.NewImage("Wire", _wireLayer, TextureLab.RoundedRect(8), c);
            RectTransform rt = wire.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = a;
            rt.sizeDelta = new Vector2(delta.magnitude, 22f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            rt.SetAsFirstSibling();
        }

        private void Refresh()
        {
            for (int i = 0; i < _left.Count; i++)
            {
                float s = _selected == i ? 1.12f : 1f;
                _left[i].rectTransform.localScale = new Vector3(s, s, 1f);
                _left[i].color = _connected.Contains(i) ? Palette.Darken(WireColors[i], 0.45f) : WireColors[i];
            }
        }
    }

    // ------------------------------------------------------------------ Hold gauge

    /// <summary>Hold to fill, let go inside the green band. Overfilling spills.</summary>
    public sealed class HoldGaugeGame : MiniGame
    {
        private Image _fill;
        private RectTransform _zone;
        private float _value;
        private bool _holding;
        private float _zoneMin = 0.58f;
        private float _zoneMax = 0.80f;
        private int _wins;

        protected override void Build()
        {
            Hint.text = "Halten und im grünen Bereich loslassen";

            Image track = UIKit.NewImage("Track", Content, TextureLab.RoundedRect(24), Palette.Backdrop);
            UIKit.Place(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 190f), new Vector2(700f, 110f));

            _zone = UIKit.NewImage("Zone", track.rectTransform, TextureLab.RoundedRect(20),
                Palette.WithAlpha(Palette.Good, 0.35f)).rectTransform;
            _zone.anchorMin = new Vector2(0f, 0f);
            _zone.anchorMax = new Vector2(0f, 1f);
            _zone.pivot = new Vector2(0f, 0.5f);

            _fill = UIKit.NewImage("Fill", track.rectTransform, TextureLab.RoundedRect(20), Palette.Accent);
            _fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _fill.rectTransform.offsetMin = new Vector2(0f, 8f);
            _fill.rectTransform.offsetMax = new Vector2(0f, -8f);

            Image btn = UIKit.NewImage("Hold", Content, TextureLab.Circle(256), Palette.AccentDeep);
            UIKit.Place(btn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -140f), new Vector2(320f, 320f));
            Text lbl = UIKit.Label("Label", btn.rectTransform, "HALTEN", 44, Palette.Backdrop);
            UIKit.Stretch(lbl.rectTransform);
            Pressable.Attach(btn.gameObject, () => _holding = true, Release);

            NewRound();
        }

        private void NewRound()
        {
            _value = 0f;
            float width = Random.Range(0.16f, 0.24f);
            _zoneMin = Random.Range(0.42f, 0.74f);
            _zoneMax = _zoneMin + width;
        }

        private void Update()
        {
            float trackW = 700f;
            _zone.sizeDelta = new Vector2(trackW * (_zoneMax - _zoneMin), -16f);
            _zone.anchoredPosition = new Vector2(trackW * _zoneMin, 0f);

            if (_holding)
            {
                _value += Time.deltaTime * 0.62f;
                if (_value > 1.05f)
                {
                    Beep(Sfx.Error);
                    _holding = false;
                    NewRound();
                }
            }

            _fill.rectTransform.sizeDelta = new Vector2(trackW * Mathf.Clamp01(_value), -16f);
            bool inZone = _value >= _zoneMin && _value <= _zoneMax;
            _fill.color = inZone ? Palette.Good : Palette.Accent;
        }

        private void Release()
        {
            if (!_holding) return;
            _holding = false;

            if (_value >= _zoneMin && _value <= _zoneMax)
            {
                _wins++;
                Beep(Sfx.Confirm, 1f + _wins * 0.12f);
                if (_wins >= 2)
                {
                    Succeed();
                    return;
                }
                Hint.text = "Noch einmal!";
            }
            else
            {
                Beep(Sfx.Error);
            }
            NewRound();
        }
    }

    // ------------------------------------------------------------------ Timing bar

    /// <summary>A marker sweeps back and forth; tap inside the shrinking target three times.</summary>
    public sealed class TimingBarGame : MiniGame
    {
        private RectTransform _marker;
        private RectTransform _target;
        private float _t;
        private float _dir = 1f;
        private float _targetCenter = 0.5f;
        private float _targetHalf = 0.12f;
        private int _hits;
        private Text _counter;

        private const float TrackW = 720f;

        protected override void Build()
        {
            Hint.text = "Tippe, wenn der Zeiger im Feld ist";

            Image track = UIKit.NewImage("Track", Content, TextureLab.RoundedRect(24), Palette.Backdrop);
            UIKit.Place(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 150f), new Vector2(TrackW, 130f));

            _target = UIKit.NewImage("Target", track.rectTransform, TextureLab.RoundedRect(18),
                Palette.WithAlpha(Palette.Good, 0.45f)).rectTransform;
            _target.anchorMin = new Vector2(0f, 0f);
            _target.anchorMax = new Vector2(0f, 1f);
            _target.pivot = new Vector2(0.5f, 0.5f);

            _marker = UIKit.NewImage("Marker", track.rectTransform, TextureLab.RoundedRect(10), Palette.Ink).rectTransform;
            _marker.anchorMin = new Vector2(0f, 0f);
            _marker.anchorMax = new Vector2(0f, 1f);
            _marker.pivot = new Vector2(0.5f, 0.5f);
            _marker.sizeDelta = new Vector2(22f, -18f);

            _counter = UIKit.Label("Counter", Content, "0 / 3", 40, Palette.InkMuted);
            UIKit.Place(_counter.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(400f, 60f));

            Image btn = UIKit.NewImage("Tap", Content, TextureLab.RoundedRect(28), Palette.AccentDeep);
            UIKit.Place(btn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -160f), new Vector2(560f, 190f));
            Text lbl = UIKit.Label("Label", btn.rectTransform, "STOPP", 52, Palette.Backdrop);
            UIKit.Stretch(lbl.rectTransform);
            Pressable.Attach(btn.gameObject, Hit);

            NewRound();
        }

        private void NewRound()
        {
            _targetHalf = Mathf.Lerp(0.13f, 0.07f, _hits / 3f);
            _targetCenter = Random.Range(0.2f, 0.8f);
        }

        private void Update()
        {
            float speed = 0.62f + _hits * 0.22f;
            _t += Time.deltaTime * speed * _dir;
            if (_t > 1f) { _t = 1f; _dir = -1f; }
            if (_t < 0f) { _t = 0f; _dir = 1f; }

            _marker.anchoredPosition = new Vector2(TrackW * _t, 0f);
            _target.sizeDelta = new Vector2(TrackW * _targetHalf * 2f, -20f);
            _target.anchoredPosition = new Vector2(TrackW * _targetCenter, 0f);
        }

        private void Hit()
        {
            if (Mathf.Abs(_t - _targetCenter) <= _targetHalf)
            {
                _hits++;
                _counter.text = _hits + " / 3";
                Beep(Sfx.Confirm, 1f + _hits * 0.15f);
                if (_hits >= 3)
                {
                    Succeed();
                    return;
                }
                NewRound();
            }
            else
            {
                Beep(Sfx.Error);
                _hits = Mathf.Max(0, _hits - 1);
                _counter.text = _hits + " / 3";
            }
        }
    }

    // ------------------------------------------------------------------ Mash

    /// <summary>Tap fast; the bar drains if you stop.</summary>
    public sealed class MashGame : MiniGame
    {
        private Image _fill;
        private float _value;
        private Image _button;

        protected override void Build()
        {
            Hint.text = "Schnell tippen!";

            Image track = UIKit.NewImage("Track", Content, TextureLab.RoundedRect(24), Palette.Backdrop);
            UIKit.Place(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 200f), new Vector2(700f, 100f));

            _fill = UIKit.NewImage("Fill", track.rectTransform, TextureLab.RoundedRect(20), Palette.Chaos);
            _fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _fill.rectTransform.offsetMin = new Vector2(0f, 8f);
            _fill.rectTransform.offsetMax = new Vector2(0f, -8f);

            _button = UIKit.NewImage("Mash", Content, TextureLab.Circle(256), Palette.Chaos);
            UIKit.Place(_button.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -120f), new Vector2(380f, 380f));
            Text lbl = UIKit.Label("Label", _button.rectTransform, "TIPP!", 60, Color.white);
            UIKit.Stretch(lbl.rectTransform);
            Pressable.Attach(_button.gameObject, Tap);
        }

        private void Tap()
        {
            _value = Mathf.Min(1.02f, _value + 0.085f);
            Beep(Sfx.Tap, 0.9f + _value * 0.6f);
            _button.rectTransform.localScale = Vector3.one * 0.9f;
            if (_value >= 1f) Succeed();
        }

        private void Update()
        {
            _value = Mathf.Max(0f, _value - Time.deltaTime * 0.22f);
            _fill.rectTransform.sizeDelta = new Vector2(700f * Mathf.Clamp01(_value), -16f);
            _button.rectTransform.localScale = Vector3.Lerp(_button.rectTransform.localScale, Vector3.one,
                1f - Mathf.Exp(-16f * Time.deltaTime));
        }
    }

    // ------------------------------------------------------------------ Keypad

    /// <summary>Watch a short code light up, then repeat it.</summary>
    public sealed class KeypadGame : MiniGame
    {
        private const int CodeLength = 4;

        private readonly List<Image> _keys = new List<Image>();
        private readonly List<int> _code = new List<int>();
        private int _progress;
        private float _showTimer;
        private int _showIndex = -1;
        private bool _inputEnabled;

        protected override void Build()
        {
            Hint.text = "Code merken …";

            for (int i = 0; i < 9; i++)
            {
                int idx = i;
                int col = i % 3;
                int row = i / 3;
                Image key = UIKit.NewImage("Key" + i, Content, TextureLab.RoundedRect(20), Palette.BackdropAlt);
                UIKit.Place(key.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((col - 1) * 210f, 210f - row * 210f), new Vector2(180f, 180f));
                Text t = UIKit.Label("Num", key.rectTransform, (i + 1).ToString(), 54, Palette.InkMuted);
                UIKit.Stretch(t.rectTransform);
                Pressable.Attach(key.gameObject, () => Press(idx));
                _keys.Add(key);
            }

            for (int i = 0; i < CodeLength; i++) _code.Add(Random.Range(0, 9));
            _showIndex = 0;
            _showTimer = 0.45f;
        }

        private void Update()
        {
            if (_inputEnabled) return;

            _showTimer -= Time.deltaTime;
            for (int i = 0; i < _keys.Count; i++)
                _keys[i].color = Palette.BackdropAlt;

            if (_showIndex < _code.Count)
            {
                if (_showTimer > 0.18f)
                {
                    _keys[_code[_showIndex]].color = Palette.Info;
                }
                if (_showTimer <= 0f)
                {
                    Beep(Sfx.Tap, 1f + _showIndex * 0.1f);
                    _showIndex++;
                    _showTimer = 0.45f;
                }
            }
            else
            {
                _inputEnabled = true;
                Hint.text = "Jetzt eingeben";
            }
        }

        private void Press(int key)
        {
            if (!_inputEnabled) return;

            if (_code[_progress] == key)
            {
                _progress++;
                Beep(Sfx.Confirm, 1f + _progress * 0.14f);
                _keys[key].color = Palette.Good;
                if (_progress >= _code.Count) Succeed();
            }
            else
            {
                Beep(Sfx.Error);
                _progress = 0;
                _inputEnabled = false;
                _showIndex = 0;
                _showTimer = 0.45f;
                Hint.text = "Falsch – nochmal merken";
            }
        }
    }

    // ------------------------------------------------------------------ Tap items

    /// <summary>Clear the mess: tap every wobbling item.</summary>
    public sealed class TapItemsGame : MiniGame
    {
        private const int ItemCount = 7;

        private readonly List<RectTransform> _items = new List<RectTransform>();
        private int _left;
        private Text _counter;

        protected override void Build()
        {
            Hint.text = "Alles wegräumen";
            _left = ItemCount;

            _counter = UIKit.Label("Counter", Content, _left + " übrig", 38, Palette.InkMuted);
            UIKit.Place(_counter.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 10f), new Vector2(400f, 56f));

            for (int i = 0; i < ItemCount; i++)
            {
                Image item = UIKit.NewImage("Item" + i, Content, TextureLab.RoundedRect(16),
                    i % 2 == 0 ? Palette.Accent : Palette.Info);
                float x = Random.Range(-320f, 320f);
                float y = Random.Range(-180f, 280f);
                UIKit.Place(item.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(140f, 140f));
                item.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));

                RectTransform rt = item.rectTransform;
                Pressable.Attach(item.gameObject, () => Collect(rt));
                _items.Add(rt);
            }
        }

        private void Collect(RectTransform rt)
        {
            if (rt == null || !rt.gameObject.activeSelf) return;
            rt.gameObject.SetActive(false);
            _left--;
            _counter.text = _left + " übrig";
            Beep(Sfx.Tap, 1.1f + (ItemCount - _left) * 0.06f);
            if (_left <= 0) Succeed();
        }

        private void Update()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] == null || !_items[i].gameObject.activeSelf) continue;
                float wobble = Mathf.Sin(Time.time * 3f + i * 1.7f) * 6f;
                _items[i].localRotation = Quaternion.Euler(0f, 0f, wobble);
            }
        }
    }
}
