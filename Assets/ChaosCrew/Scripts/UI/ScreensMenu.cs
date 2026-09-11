using UnityEngine;
using UnityEngine.UI;

namespace ChaosCrew
{
    /// <summary>Front page: logo, play, settings and a short how-to.</summary>
    public sealed class TitleScreen : ScreenBase
    {
        private RectTransform _howTo;
        private float _t;
        private RectTransform _logo;

        protected override void Build()
        {
            Image bg = UIKit.NewImage("Bg", Root, TextureLab.Solid(), Palette.Backdrop);
            UIKit.Stretch(bg.rectTransform);

            // Decorative confetti so the menu is not a flat colour field.
            var rng = new DeterministicRng(4242);
            for (int i = 0; i < 26; i++)
            {
                Color c = Palette.Crew[i % Palette.Crew.Length];
                Image dot = UIKit.NewImage("Confetti", Root, TextureLab.RoundedRect(6),
                    Palette.WithAlpha(c, 0.16f));
                float size = rng.Range(30f, 90f);
                UIKit.Place(dot.rectTransform, new Vector2(rng.NextFloat(), rng.NextFloat()),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size * 0.6f));
                dot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rng.Range(0f, 360f));
            }

            _logo = UIKit.NewRect("Logo", Root);
            UIKit.Place(_logo, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 320f));

            Text t1 = UIKit.Label("Chaos", _logo, "CHAOS", 150, Palette.Accent);
            UIKit.Place(t1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 78f), new Vector2(960f, 170f));

            Text t2 = UIKit.Label("Crew", _logo, "CREW", 150, Palette.Chaos);
            UIKit.Place(t2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -62f), new Vector2(960f, 170f));

            Text sub = UIKit.Label("Sub", Root, "Vier Kolleg:innen. Einer sabotiert.\nDie Kameras haben alles gesehen.",
                38, Palette.InkMuted);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 140f));

            UIKit.Place((RectTransform)UIKit.Button("Play", Root, "SPIELEN", Palette.Good, Palette.Backdrop, 62,
                    () => D.OpenLobby()).transform,
                new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 180f));

            UIKit.Place((RectTransform)UIKit.Button("Settings", Root, "EINSTELLUNGEN", Palette.PanelSoft, Palette.Ink, 40,
                    () => D.OpenSettings()).transform,
                new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 120f));

            UIKit.Place((RectTransform)UIKit.Button("How", Root, "SO GEHT'S", Palette.Panel, Palette.InkMuted, 36,
                    ToggleHowTo).transform,
                new Vector2(0.5f, 0.13f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 100f));

            BuildHowTo();
        }

        private void BuildHowTo()
        {
            _howTo = UIKit.NewRect("HowTo", Root);
            UIKit.Stretch(_howTo);

            Image dim = UIKit.NewImage("Dim", _howTo, TextureLab.Solid(), new Color(0f, 0f, 0f, 0.75f));
            dim.raycastTarget = true;
            UIKit.Stretch(dim.rectTransform);

            Image panel = UIKit.Panel("Panel", _howTo, Palette.Panel, 30);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(960f, 1250f));

            Text head = UIKit.Label("Head", panel.rectTransform, "SO GEHT'S", 58, Palette.Accent);
            UIKit.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -70f), new Vector2(860f, 80f));

            const string body =
                "<b>Crew</b>\n" +
                "Lauf mit dem Joystick los und erledige deine vier Aufgaben. " +
                "Der Knopf rechts öffnet die Aufgabe, wenn du nah genug dran bist.\n\n" +
                "<b>Saboteur</b>\n" +
                "Du bekommst dieselbe Aufgabenliste – aber sie zählt nicht. " +
                "Nutze das rote Feld, um Chaos zu stiften, ohne gesehen zu werden.\n\n" +
                "<b>Kameras</b>\n" +
                "Sechs Räume werden gefilmt. Lager, Toilette und Aufzug nicht. " +
                "Am Ende seht ihr Ausschnitte – und stimmt ab.\n\n" +
                "<b>Sieg</b>\n" +
                "Crew gewinnt mit allen Aufgaben oder dem richtigen Verdacht. " +
                "Der Saboteur gewinnt mit vollem Chaos-Balken oder unerkannt.";

            Text text = UIKit.Label("Body", panel.rectTransform, body, 34, Palette.Ink,
                TextAnchor.UpperLeft, FontStyle.Normal);
            UIKit.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -180f), new Vector2(840f, 880f));

            UIKit.Place((RectTransform)UIKit.Button("Close", panel.rectTransform, "ALLES KLAR",
                    Palette.Good, Palette.Backdrop, 42, ToggleHowTo).transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(520f, 120f));

            _howTo.gameObject.SetActive(false);
        }

        private void ToggleHowTo()
        {
            _howTo.gameObject.SetActive(!_howTo.gameObject.activeSelf);
            D.Audio.Play(Sfx.Tap);
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_logo != null)
            {
                float s = 1f + Mathf.Sin(_t * 1.7f) * 0.02f;
                _logo.localScale = new Vector3(s, s, 1f);
                _logo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 1.1f) * 1.2f);
            }
        }
    }

    /// <summary>Sound, name and a role override that makes both sides easy to test.</summary>
    public sealed class SettingsScreen : ScreenBase
    {
        private Text _soundLabel;
        private Text _roleLabel;
        private Text _volumeLabel;

        protected override void Build()
        {
            Image bg = UIKit.NewImage("Bg", Root, TextureLab.Solid(), Palette.Backdrop);
            UIKit.Stretch(bg.rectTransform);

            Text head = UIKit.Label("Head", Root, "EINSTELLUNGEN", 62, Palette.Accent);
            UIKit.Place(head.rectTransform, new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 90f));

            _soundLabel = AddRow(0, "Sound", ToggleSound);
            _volumeLabel = AddRow(1, "Lautstärke", CycleVolume);
            _roleLabel = AddRow(2, "Rolle (Test)", CycleRole);

            Text note = UIKit.Label("Note", Root,
                "Die Rollen-Einstellung ist eine Test-Hilfe: damit kannst du gezielt als Crew " +
                "oder als Saboteur starten, statt auf den Zufall zu warten.",
                30, Palette.InkMuted, TextAnchor.UpperCenter, FontStyle.Normal);
            UIKit.Place(note.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(860f, 160f));

            UIKit.Place((RectTransform)UIKit.Button("Back", Root, "ZURÜCK", Palette.PanelSoft, Palette.Ink, 44,
                    () => D.GoToTitle()).transform,
                new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 130f));
        }

        private Text AddRow(int index, string label, System.Action onTap)
        {
            float y = 0.78f - index * 0.11f;

            Image row = UIKit.Panel("Row" + index, Root, Palette.Panel, 24);
            UIKit.Place(row.rectTransform, new Vector2(0.5f, y), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 140f));

            Text name = UIKit.Label("Name", row.rectTransform, label, 40, Palette.Ink, TextAnchor.MiddleLeft);
            UIKit.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(40f, 0f), new Vector2(460f, 60f));

            Button b = UIKit.Button("Value", row.rectTransform, "", Palette.BackdropAlt, Palette.Accent, 36, onTap);
            UIKit.Place((RectTransform)b.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-30f, 0f), new Vector2(340f, 96f));

            return b.GetComponentInChildren<Text>();
        }

        public override void OnShow() => Refresh();

        private void Refresh()
        {
            _soundLabel.text = D.Settings.Muted ? "AUS" : "AN";
            _volumeLabel.text = Mathf.RoundToInt(D.Settings.Volume * 100f) + "%";
            switch (D.Settings.RolePreference)
            {
                case RolePreference.AlwaysCrew: _roleLabel.text = "IMMER CREW"; break;
                case RolePreference.AlwaysSaboteur: _roleLabel.text = "IMMER SABOTEUR"; break;
                default: _roleLabel.text = "ZUFALL"; break;
            }
        }

        private void ToggleSound()
        {
            D.Settings.Muted = !D.Settings.Muted;
            D.ApplyAudioSettings();
            D.Audio.Play(Sfx.Tap);
            Refresh();
        }

        private void CycleVolume()
        {
            D.Settings.Volume = Mathf.Round((D.Settings.Volume + 0.25f) * 100f) / 100f;
            if (D.Settings.Volume > 1.01f) D.Settings.Volume = 0.25f;
            D.ApplyAudioSettings();
            D.Audio.Play(Sfx.Tap);
            Refresh();
        }

        private void CycleRole()
        {
            D.Settings.RolePreference = (RolePreference)(((int)D.Settings.RolePreference + 1) % 3);
            D.Settings.Save();
            D.Audio.Play(Sfx.Tap);
            Refresh();
        }
    }

    /// <summary>Four seats filling up, then start.</summary>
    public sealed class LobbyScreen : ScreenBase
    {
        private readonly Image[] _cards = new Image[CCConfig.PlayerCount];
        private readonly Text[] _names = new Text[CCConfig.PlayerCount];
        private readonly Text[] _states = new Text[CCConfig.PlayerCount];
        private readonly Image[] _avatars = new Image[CCConfig.PlayerCount];
        private Button _start;
        private Text _startLabel;
        private Text _status;

        protected override void Build()
        {
            Image bg = UIKit.NewImage("Bg", Root, TextureLab.Solid(), Palette.Backdrop);
            UIKit.Stretch(bg.rectTransform);

            Text head = UIKit.Label("Head", Root, "LOBBY", 62, Palette.Accent);
            UIKit.Place(head.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 90f));

            _status = UIKit.Label("Status", Root, "", 34, Palette.InkMuted);
            UIKit.Place(_status.rectTransform, new Vector2(0.5f, 0.87f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 60f));

            for (int i = 0; i < CCConfig.PlayerCount; i++)
            {
                float y = 0.76f - i * 0.13f;

                Image card = UIKit.Panel("Seat" + i, Root, Palette.Panel, 26);
                UIKit.Place(card.rectTransform, new Vector2(0.5f, y), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(900f, 170f));
                _cards[i] = card;

                Image avatar = UIKit.NewImage("Avatar", card.rectTransform, TextureLab.Blob(), Palette.Crew[i]);
                UIKit.Place(avatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(40f, 0f), new Vector2(110f, 120f));
                _avatars[i] = avatar;

                _names[i] = UIKit.Label("Name", card.rectTransform, "wartet …", 42, Palette.Ink, TextAnchor.MiddleLeft);
                UIKit.Place(_names[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(180f, 14f), new Vector2(500f, 56f));

                _states[i] = UIKit.Label("State", card.rectTransform, "", 30, Palette.InkMuted, TextAnchor.MiddleLeft);
                UIKit.Place(_states[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(180f, -30f), new Vector2(500f, 44f));
            }

            _start = UIKit.Button("Start", Root, "STARTEN", Palette.Good, Palette.Backdrop, 54, () => D.RequestStart());
            UIKit.Place((RectTransform)_start.transform, new Vector2(0.5f, 0.17f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700f, 160f));
            _startLabel = _start.GetComponentInChildren<Text>();

            UIKit.Place((RectTransform)UIKit.Button("Back", Root, "ZURÜCK", Palette.PanelSoft, Palette.Ink, 38,
                    () => D.LeaveLobby()).transform,
                new Vector2(0.5f, 0.07f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 100f));
        }

        public void Refresh()
        {
            var seats = D.Transport.Seats;
            for (int i = 0; i < CCConfig.PlayerCount; i++)
            {
                bool filled = i < seats.Count;
                _cards[i].color = filled ? Palette.Panel : Palette.WithAlpha(Palette.Panel, 0.35f);
                _avatars[i].color = filled ? Palette.Crew[seats[i].ColorIndex % Palette.Crew.Length]
                    : Palette.WithAlpha(Palette.InkMuted, 0.2f);

                if (!filled)
                {
                    _names[i].text = "Freier Platz";
                    _names[i].color = Palette.WithAlpha(Palette.InkMuted, 0.5f);
                    _states[i].text = "verbindet …";
                    continue;
                }

                SeatInfo s = seats[i];
                _names[i].text = s.DisplayName + (s.IsLocal ? "  (du)" : "");
                _names[i].color = Palette.Ink;
                _states[i].text = s.IsBot ? "Bot · bereit" : (s.Ready ? "bereit" : "tippt herum …");
            }

            bool full = seats.Count >= CCConfig.PlayerCount;
            _status.text = full ? "Alle da – los geht's!" : seats.Count + " / " + CCConfig.PlayerCount + " Spieler";
            _start.interactable = full;
            _startLabel.color = full ? Palette.Backdrop : Palette.WithAlpha(Palette.Backdrop, 0.4f);
        }
    }
}
