using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChaosCrew
{
    /// <summary>Secret role hand-out, shown to the local player only.</summary>
    public sealed class RoleRevealScreen : ScreenBase
    {
        private Image _card;
        private Text _role;
        private Text _blurb;
        private Text _countdown;
        private Image _avatar;
        private float _anim;

        protected override void Build()
        {
            Image dim = UIKit.NewImage("Dim", Root, TextureLab.Solid(), new Color(0f, 0f, 0f, 0.82f));
            dim.raycastTarget = true;
            UIKit.Stretch(dim.rectTransform);

            _card = UIKit.Panel("Card", Root, Palette.Panel, 34);
            UIKit.Place(_card.rectTransform, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 1000f));

            Text head = UIKit.Label("Head", _card.rectTransform, "DEINE ROLLE", 38, Palette.InkMuted);
            UIKit.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -70f), new Vector2(800f, 50f));

            _avatar = UIKit.NewImage("Avatar", _card.rectTransform, TextureLab.Blob(), Palette.Crew[0]);
            UIKit.Place(_avatar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -300f), new Vector2(280f, 320f));

            _role = UIKit.Label("Role", _card.rectTransform, "CREW", 96, Palette.Good);
            UIKit.Place(_role.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -60f), new Vector2(820f, 120f));

            _blurb = UIKit.Label("Blurb", _card.rectTransform, "", 34, Palette.Ink,
                TextAnchor.UpperCenter, FontStyle.Normal);
            UIKit.Place(_blurb.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -230f), new Vector2(760f, 240f));

            _countdown = UIKit.Label("Countdown", Root, "", 40, Palette.InkMuted);
            UIKit.Place(_countdown.rectTransform, new Vector2(0.5f, 0.13f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(800f, 60f));
        }

        public void Present(PlayerState local)
        {
            _anim = 0f;
            if (local == null) return;

            bool sab = local.IsSaboteur;
            _avatar.color = local.Color;
            _role.text = sab ? "SABOTEUR" : "CREW";
            _role.color = sab ? Palette.Chaos : Palette.Good;
            _card.color = sab ? Palette.Hex("4A1F45") : Palette.Panel;
            _blurb.text = sab
                ? "Du bist allein. Stifte Chaos, halte dein Alibi sauber\nund lass dich nicht filmen."
                : "Erledige deine Aufgaben – und merk dir,\nwer sich wo herumtreibt.";
        }

        public void Refresh(float remaining)
        {
            _countdown.text = "Runde startet in " + Mathf.CeilToInt(Mathf.Max(0f, remaining)) + " …";
            _anim += Time.deltaTime;
            float pop = Mathf.Clamp01(_anim * 3.4f);
            float scale = Mathf.LerpUnclamped(0.7f, 1f, 1f - Mathf.Pow(1f - pop, 3f));
            _card.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _card.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_anim * 2.2f) * 1.4f);
        }
    }

    /// <summary>
    /// Reviews the security footage. Each clip replays the recorded positions inside one
    /// room; jammed cameras play back as static, and a false trail shows the wrong colour.
    /// </summary>
    public sealed class EvidenceScreen : ScreenBase
    {
        private const float ViewW = 860f;
        private const float ViewH = 620f;

        private List<CameraClip> _clips = new List<CameraClip>();
        private int _index;
        private float _playhead;
        private bool _playing = true;

        private RectTransform _viewport;
        private Image _noise;
        private Text _roomName;
        private Text _caption;
        private Text _counter;
        private Text _timestamp;
        private Image _scrubFill;
        private readonly Dictionary<int, RectTransform> _dots = new Dictionary<int, RectTransform>();
        private Text _noSignal;

        protected override void Build()
        {
            Image bg = UIKit.NewImage("Bg", Root, TextureLab.Solid(), Palette.Backdrop);
            UIKit.Stretch(bg.rectTransform);

            Text head = UIKit.Label("Head", Root, "BEWEISPHASE", 58, Palette.Info);
            UIKit.Place(head.rectTransform, new Vector2(0.5f, 0.945f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 80f));

            _counter = UIKit.Label("Counter", Root, "", 32, Palette.InkMuted);
            UIKit.Place(_counter.rectTransform, new Vector2(0.5f, 0.895f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 50f));

            Image frame = UIKit.Panel("Frame", Root, Palette.Hex("11132A"), 26);
            UIKit.Place(frame.rectTransform, new Vector2(0.5f, 0.63f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(ViewW + 40f, ViewH + 130f));

            _roomName = UIKit.Label("RoomName", frame.rectTransform, "", 34, Palette.Info, TextAnchor.MiddleLeft);
            UIKit.Place(_roomName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(34f, -26f), new Vector2(520f, 48f));

            _timestamp = UIKit.Label("Time", frame.rectTransform, "", 30, Palette.WithAlpha(Palette.Bad, 0.9f),
                TextAnchor.MiddleRight);
            UIKit.Place(_timestamp.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-34f, -26f), new Vector2(320f, 48f));

            _viewport = UIKit.NewRect("Viewport", frame.rectTransform);
            UIKit.Place(_viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -22f), new Vector2(ViewW, ViewH));

            Image floor = UIKit.NewImage("Floor", _viewport, TextureLab.RoundedRect(16), Palette.Hex("232746"));
            UIKit.Stretch(floor.rectTransform);

            var mask = _viewport.gameObject.AddComponent<RectMask2D>();
            mask.enabled = true;

            _noise = UIKit.NewImage("Noise", _viewport, TextureLab.Noise(19), Palette.WithAlpha(Color.white, 0.75f));
            UIKit.Stretch(_noise.rectTransform);
            _noise.gameObject.SetActive(false);

            _noSignal = UIKit.Label("NoSignal", _viewport, "SIGNAL GESTÖRT", 54, Palette.Bad);
            UIKit.Stretch(_noSignal.rectTransform);
            _noSignal.gameObject.SetActive(false);

            _caption = UIKit.Label("Caption", Root, "", 34, Palette.Ink, TextAnchor.UpperCenter, FontStyle.Normal);
            UIKit.Place(_caption.rectTransform, new Vector2(0.5f, 0.34f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(880f, 140f));

            _scrubFill = UIKit.Bar("Scrub", Root, Palette.BackdropAlt, Palette.Info, 8);
            UIKit.Place((RectTransform)_scrubFill.transform.parent, new Vector2(0.5f, 0.375f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(860f, 18f));

            UIKit.Place((RectTransform)UIKit.Button("Prev", Root, "<", Palette.PanelSoft, Palette.Ink, 48,
                    () => Step(-1)).transform,
                new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.5f), new Vector2(-320f, 0f), new Vector2(180f, 130f));

            UIKit.Place((RectTransform)UIKit.Button("PlayPause", Root, "PAUSE", Palette.PanelSoft, Palette.Ink, 34,
                    TogglePlay).transform,
                new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 130f));

            UIKit.Place((RectTransform)UIKit.Button("Next", Root, ">", Palette.PanelSoft, Palette.Ink, 48,
                    () => Step(1)).transform,
                new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.5f), new Vector2(320f, 0f), new Vector2(180f, 130f));

            UIKit.Place((RectTransform)UIKit.Button("ToVote", Root, "ZUR ABSTIMMUNG", Palette.Accent, Palette.Backdrop, 46,
                    () => D.BeginVoting()).transform,
                new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 150f));
        }

        public void Present(List<CameraClip> clips)
        {
            _clips = clips ?? new List<CameraClip>();
            _index = 0;
            _playhead = 0f;
            _playing = true;
            EnsureDots();
            LoadClip();
        }

        private void EnsureDots()
        {
            if (D.State == null) return;
            for (int i = 0; i < D.State.Players.Count; i++)
            {
                int id = D.State.Players[i].Id;
                if (_dots.ContainsKey(id)) continue;

                RectTransform dot = UIKit.NewRect("Dot" + id, _viewport);
                dot.anchorMin = dot.anchorMax = new Vector2(0.5f, 0.5f);
                dot.pivot = new Vector2(0.5f, 0.5f);
                dot.sizeDelta = new Vector2(74f, 74f);

                Image body = UIKit.NewImage("Body", dot, TextureLab.Blob(), Color.white);
                UIKit.Stretch(body.rectTransform);

                Text tag = UIKit.Label("Tag", dot, "", 22, Color.white);
                UIKit.Place(tag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 6f), new Vector2(220f, 32f));

                _dots[id] = dot;
            }
        }

        private void LoadClip()
        {
            _playhead = 0f;
            if (_clips.Count == 0) return;

            CameraClip clip = _clips[Mathf.Clamp(_index, 0, _clips.Count - 1)];
            _roomName.text = "KAMERA · " + clip.RoomName.ToUpperInvariant();
            _caption.text = clip.Corrupted ? "Diese Aufnahme wurde gestört." : clip.Caption;
            _counter.text = "Clip " + (_index + 1) + " von " + _clips.Count;
            _noise.gameObject.SetActive(clip.Corrupted);
            _noSignal.gameObject.SetActive(clip.Corrupted);
        }

        private void Step(int delta)
        {
            if (_clips.Count == 0) return;
            _index = (_index + delta + _clips.Count) % _clips.Count;
            D.Audio.Play(Sfx.Tap);
            LoadClip();
        }

        private void TogglePlay()
        {
            _playing = !_playing;
            D.Audio.Play(Sfx.Tap);
        }

        private void Update()
        {
            if (_clips.Count == 0 || D.State == null) return;
            CameraClip clip = _clips[Mathf.Clamp(_index, 0, _clips.Count - 1)];

            if (_playing)
            {
                _playhead += Time.deltaTime;
                if (_playhead > clip.Duration) _playhead = 0f;
            }

            UIKit.SetBar(_scrubFill, _playhead / clip.Duration);
            _timestamp.text = "● REC  " + UIKit.FormatTime(clip.StartTime + _playhead);

            if (clip.Corrupted)
            {
                foreach (var kv in _dots) kv.Value.gameObject.SetActive(false);
                _noise.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                _noise.color = Palette.WithAlpha(Color.white, Random.Range(0.5f, 0.85f));
                return;
            }

            RenderFrame(clip, clip.StartTime + _playhead);
        }

        private void RenderFrame(CameraClip clip, float atTime)
        {
            RoomDef room = D.State.Map.RoomById(clip.RoomId);
            float scale = room == null ? 40f
                : Mathf.Min(ViewW / Mathf.Max(1, room.Bounds.width), ViewH / Mathf.Max(1, room.Bounds.height));

            var visible = new HashSet<int>();

            if (clip.Frames.Count > 0)
            {
                int idx = 0;
                for (int i = 0; i < clip.Frames.Count; i++)
                {
                    if (clip.Frames[i].Time <= atTime) idx = i;
                    else break;
                }
                EvidenceSample a = clip.Frames[idx];
                EvidenceSample b = clip.Frames[Mathf.Min(idx + 1, clip.Frames.Count - 1)];
                float span = Mathf.Max(0.0001f, b.Time - a.Time);
                float t = Mathf.Clamp01((atTime - a.Time) / span);

                for (int i = 0; i < a.Players.Length; i++)
                {
                    PlayerSnapshot pa = a.Players[i];
                    Vector2 pos = pa.Position;
                    for (int j = 0; j < b.Players.Length; j++)
                    {
                        if (b.Players[j].PlayerId != pa.PlayerId) continue;
                        pos = Vector2.Lerp(pa.Position, b.Players[j].Position, t);
                        break;
                    }

                    // The footage shows whoever the clip claims it shows.
                    int shownId = clip.DisplayIdFor(pa.PlayerId);
                    PlayerState shown = D.State.PlayerById(shownId);
                    if (shown == null || !_dots.TryGetValue(pa.PlayerId, out RectTransform dot)) continue;

                    visible.Add(pa.PlayerId);
                    dot.gameObject.SetActive(true);

                    Vector2 local = room == null ? Vector2.zero
                        : (pos - new Vector2(room.Bounds.xMin, room.Bounds.yMin)
                           - new Vector2(room.Bounds.width, room.Bounds.height) * 0.5f) * scale;
                    dot.anchoredPosition = local;

                    Image body = dot.Find("Body").GetComponent<Image>();
                    body.color = shown.Color;
                    Text tag = dot.Find("Tag").GetComponent<Text>();
                    tag.text = shown.DisplayName;
                }
            }

            foreach (var kv in _dots)
                if (!visible.Contains(kv.Key)) kv.Value.gameObject.SetActive(false);
        }
    }

    /// <summary>Accusation screen: one card per suspect plus a skip option.</summary>
    public sealed class VotingScreen : ScreenBase
    {
        private readonly List<Image> _cards = new List<Image>();
        private readonly List<Text> _counts = new List<Text>();
        private readonly List<Button> _buttons = new List<Button>();
        private Text _timer;
        private Text _prompt;

        protected override void Build()
        {
            Image bg = UIKit.NewImage("Bg", Root, TextureLab.Solid(), Palette.Backdrop);
            UIKit.Stretch(bg.rectTransform);

            Text head = UIKit.Label("Head", Root, "WER WAR ES?", 62, Palette.Chaos);
            UIKit.Place(head.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 90f));

            _timer = UIKit.Label("Timer", Root, "", 40, Palette.Accent);
            UIKit.Place(_timer.rectTransform, new Vector2(0.5f, 0.875f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 60f));

            for (int i = 0; i < CCConfig.PlayerCount; i++)
            {
                int seat = i;
                float y = 0.74f - i * 0.135f;

                Image card = UIKit.Panel("Vote" + i, Root, Palette.Panel, 26);
                UIKit.Place(card.rectTransform, new Vector2(0.5f, y), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(900f, 175f));
                _cards.Add(card);

                Image avatar = UIKit.NewImage("Avatar", card.rectTransform, TextureLab.Blob(), Palette.Crew[i]);
                UIKit.Place(avatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(40f, 0f), new Vector2(115f, 130f));
                avatar.name = "Avatar";

                Text name = UIKit.Label("Name", card.rectTransform, "", 44, Palette.Ink, TextAnchor.MiddleLeft);
                UIKit.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(185f, 0f), new Vector2(430f, 60f));
                name.name = "Name";

                Text count = UIKit.Label("Count", card.rectTransform, "", 34, Palette.Accent, TextAnchor.MiddleRight);
                UIKit.Place(count.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-230f, 0f), new Vector2(200f, 50f));
                _counts.Add(count);

                Button b = UIKit.Button("Pick", card.rectTransform, "VERDACHT", Palette.Chaos, Color.white, 30,
                    () => D.SubmitVote(SeatToPlayerId(seat)));
                UIKit.Place((RectTransform)b.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-30f, 0f), new Vector2(190f, 110f));
                _buttons.Add(b);
            }

            _prompt = UIKit.Label("Prompt", Root, "", 32, Palette.InkMuted);
            UIKit.Place(_prompt.rectTransform, new Vector2(0.5f, 0.19f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 60f));

            UIKit.Place((RectTransform)UIKit.Button("Skip", Root, "NIEMAND / ÜBERSPRINGEN",
                    Palette.PanelSoft, Palette.Ink, 36, () => D.SubmitVote(VoteSystem.SkipVote)).transform,
                new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 130f));
        }

        private int SeatToPlayerId(int seat)
        {
            if (D.State == null || seat >= D.State.Players.Count) return VoteSystem.SkipVote;
            return D.State.Players[seat].Id;
        }

        public void Present()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                bool exists = D.State != null && i < D.State.Players.Count;
                _cards[i].gameObject.SetActive(exists);
                if (!exists) continue;

                PlayerState p = D.State.Players[i];
                _cards[i].transform.Find("Avatar").GetComponent<Image>().color = p.Color;
                _cards[i].transform.Find("Name").GetComponent<Text>().text =
                    p.DisplayName + (p.IsLocal ? "  (du)" : "");
                _buttons[i].interactable = !p.IsLocal;
                _counts[i].text = "";
            }
        }

        public void Refresh(float remaining)
        {
            _timer.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)) + " Sekunden";

            bool voted = D.Votes != null && D.Votes.LocalVoteCast;
            _prompt.text = voted ? "Stimme abgegeben – die anderen überlegen noch." : "Tippe auf deinen Verdacht.";

            for (int i = 0; i < _counts.Count; i++)
            {
                if (D.State == null || i >= D.State.Players.Count) continue;
                PlayerState p = D.State.Players[i];
                int n = D.Votes != null ? D.Votes.CountFor(p.Id) : 0;
                _counts[i].text = n > 0 ? new string('•', n) : "";
                if (voted) _buttons[i].interactable = false;
            }
        }
    }

    /// <summary>Outcome, the reveal, and a fast path into another round.</summary>
    public sealed class ResultScreen : ScreenBase
    {
        private Text _headline;
        private Text _sub;
        private Text _detail;
        private Image _banner;
        private readonly List<Text> _rows = new List<Text>();

        protected override void Build()
        {
            Image bg = UIKit.NewImage("Bg", Root, TextureLab.Solid(), Palette.Backdrop);
            UIKit.Stretch(bg.rectTransform);

            _banner = UIKit.Panel("Banner", Root, Palette.Good, 30);
            UIKit.Place(_banner.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(960f, 260f));

            _headline = UIKit.Label("Headline", _banner.rectTransform, "", 74, Palette.Backdrop);
            UIKit.Place(_headline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 32f), new Vector2(900f, 100f));

            _sub = UIKit.Label("Sub", _banner.rectTransform, "", 34, Palette.WithAlpha(Palette.Backdrop, 0.85f),
                TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.Place(_sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -50f), new Vector2(880f, 80f));

            _detail = UIKit.Label("Detail", Root, "", 34, Palette.Ink, TextAnchor.UpperCenter, FontStyle.Normal);
            UIKit.Place(_detail.rectTransform, new Vector2(0.5f, 0.69f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(900f, 180f));

            for (int i = 0; i < CCConfig.PlayerCount; i++)
            {
                Text row = UIKit.Label("Row" + i, Root, "", 34, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
                UIKit.Place(row.rectTransform, new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -i * 72f), new Vector2(860f, 64f));
                _rows.Add(row);
            }

            UIKit.Place((RectTransform)UIKit.Button("Again", Root, "NOCHMAL", Palette.Good, Palette.Backdrop, 52,
                    () => D.PlayAgain()).transform,
                new Vector2(0.5f, 0.17f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 160f));

            UIKit.Place((RectTransform)UIKit.Button("Menu", Root, "HAUPTMENÜ", Palette.PanelSoft, Palette.Ink, 38,
                    () => D.ReturnToMenu()).transform,
                new Vector2(0.5f, 0.075f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 120f));
        }

        public void Present()
        {
            MatchState s = D.State;
            if (s == null) return;

            PlayerState sab = s.Saboteur;
            PlayerState local = s.Local;
            bool crewWon = s.Winner == Role.Crew;
            bool localWon = local != null && local.Role == s.Winner;

            _banner.color = localWon ? Palette.Good : Palette.Chaos;
            _headline.text = localWon ? "GEWONNEN!" : "VERLOREN";
            _headline.color = localWon ? Palette.Backdrop : Color.white;
            _sub.color = Palette.WithAlpha(localWon ? Palette.Backdrop : Color.white, 0.9f);
            _sub.text = crewWon ? "Die Crew hat den Tag gerettet." : "Der Saboteur kommt damit durch.";

            string ejected;
            if (s.VoteWasTied || s.EjectedPlayerId == VoteSystem.SkipVote)
            {
                ejected = "Die Abstimmung endete unentschieden – niemand flog raus.";
            }
            else
            {
                PlayerState e = s.PlayerById(s.EjectedPlayerId);
                bool right = sab != null && e != null && e.Id == sab.Id;
                ejected = (e != null ? e.DisplayName : "Niemand") + " wurde rausgeworfen – " +
                          (right ? "richtig geraten!" : "unschuldig.");
            }

            string reason;
            switch (s.EndReason)
            {
                case RoundEndReason.TasksCompleted: reason = "Alle Aufgaben erledigt."; break;
                case RoundEndReason.ChaosMaxed: reason = "Der Chaos-Balken lief über."; break;
                default: reason = "Die Zeit war um."; break;
            }

            _detail.text = reason + "\n" + ejected + "\n\n<b>Saboteur war: " +
                           (sab != null ? sab.DisplayName : "?") + "</b>";

            for (int i = 0; i < _rows.Count; i++)
            {
                if (i >= s.Players.Count)
                {
                    _rows[i].text = "";
                    continue;
                }
                PlayerState p = s.Players[i];
                string role = p.IsSaboteur ? "Saboteur" : "Crew";
                int done = 0;
                for (int t = 0; t < s.Tasks.Count; t++)
                    if (s.Tasks[t].OwnerId == p.Id && s.Tasks[t].Completed) done++;

                string tasks = p.IsSaboteur ? "nur so getan" : done + "/" + CCConfig.TasksPerCrew + " Aufgaben";
                _rows[i].text = "<b>" + p.DisplayName + "</b>   " + role + "   ·   " + tasks;
                _rows[i].color = p.IsSaboteur ? Palette.Chaos : Palette.Ink;
            }
        }
    }
}
