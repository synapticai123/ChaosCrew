using UnityEngine;
using UnityEngine.UI;

namespace ChaosCrew
{
    /// <summary>
    /// Splash shown while the office is warmed up. The artwork is the key art loaded from
    /// Resources, cropped to fill any phone aspect, with a progress bar underneath.
    /// </summary>
    public sealed class LoadingScreen : ScreenBase
    {
        private const string ResourceName = "LoadingScreen";

        private Image _barFill;
        private Text _status;
        private Text _tapHint;
        private RawImage _art;
        private System.Action _onDone;
        private float _t;
        private bool _running;
        private bool _finished;

        private static readonly string[] Tips =
        {
            "Lager, Toiletten und Aufzug haben keine Kamera …",
            "Kaffee füllt deine Energie wieder auf …",
            "Kisten ins Büro bringen beruhigt das Chaos …",
            "Der Saboteur hat dieselbe Aufgabenliste wie du …",
            "Die Kameras merken sich alles. Fast alles.",
        };

        protected override void Build()
        {
            Image bg = UIKit.NewImage("Bg", Root, TextureLab.Solid(), Palette.Hex("0E1524"));
            bg.raycastTarget = true;
            UIKit.Stretch(bg.rectTransform);

            // Art layer: envelope the screen so the key art always fills it edge to edge.
            RectTransform frame = UIKit.NewRect("ArtFrame", Root);
            UIKit.Stretch(frame);
            frame.gameObject.AddComponent<RectMask2D>();

            RectTransform artRt = UIKit.NewRect("Art", frame);
            artRt.anchorMin = artRt.anchorMax = new Vector2(0.5f, 0.5f);
            artRt.pivot = new Vector2(0.5f, 0.5f);
            _art = artRt.gameObject.AddComponent<RawImage>();
            _art.raycastTarget = false;

            var tex = Resources.Load<Texture2D>(ResourceName);
            if (tex != null)
            {
                _art.texture = tex;
                var fitter = artRt.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = tex.width / (float)tex.height;
                // Envelope crops top and bottom evenly; nudge down so the logo stays whole.
                artRt.anchoredPosition = new Vector2(0f, -70f);
            }
            else
            {
                // Missing artwork should degrade quietly rather than show a white rectangle.
                _art.color = Palette.Hex("1A2338");
                UIKit.Stretch(artRt);
            }

            // Gradient scrim so the bar and text stay readable over the art.
            Image scrim = UIKit.NewImage("Scrim", Root, TextureLab.BottomFade(), Color.white);
            scrim.rectTransform.anchorMin = new Vector2(0f, 0f);
            scrim.rectTransform.anchorMax = new Vector2(1f, 0.34f);
            scrim.rectTransform.offsetMin = Vector2.zero;
            scrim.rectTransform.offsetMax = Vector2.zero;
            scrim.raycastTarget = false;

            _status = UIKit.Label("Status", Root, "Büro wird eingerichtet …", 34, Palette.Ink);
            UIKit.Place(_status.rectTransform, new Vector2(0.5f, 0.155f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 60f));

            _barFill = UIKit.Bar("Bar", Root, new Color(1f, 1f, 1f, 0.18f), Palette.Accent, 10);
            UIKit.Place((RectTransform)_barFill.transform.parent, new Vector2(0.5f, 0.1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 22f));

            _tapHint = UIKit.Label("Tap", Root, "", 34, Color.white);
            UIKit.Place(_tapHint.rectTransform, new Vector2(0.5f, 0.055f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 50f));

            Pressable.Attach(bg.gameObject, Finish);
        }

        public void Begin(System.Action onDone)
        {
            _onDone = onDone;
            _t = 0f;
            _running = true;
            _finished = false;
            _status.text = Tips[Random.Range(0, Tips.Length)];
            _tapHint.text = "";
            D.UI.Show(GamePhase.Loading);
        }

        private void Update()
        {
            if (!_running) return;

            _t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(_t / 2.6f);
            UIKit.SetBar(_barFill, p);

            if (p >= 1f && !_finished)
            {
                _tapHint.text = "Tippen zum Starten";
                _tapHint.color = new Color(1f, 1f, 1f, 0.62f + 0.38f * Mathf.Sin(Time.unscaledTime * 3.4f));
            }
        }

        private void Finish()
        {
            if (!_running || _t < 0.6f) return;
            _running = false;
            _finished = true;
            D.Audio.Play(Sfx.Confirm);
            _onDone?.Invoke();
        }
    }
}
