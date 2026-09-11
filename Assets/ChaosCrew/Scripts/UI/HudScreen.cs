using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChaosCrew
{
    /// <summary>
    /// The in-round overlay, laid out to the concept art: energy bar and clock across the
    /// top, grouped task checklist top-left, round D-pad bottom-left and the action cluster
    /// bottom-right. The saboteur gets one extra button; everything else is shared.
    /// </summary>
    public sealed class HudScreen : ScreenBase
    {
        private static readonly Color PanelNavy = Palette.Hex("16233F");
        private static readonly Color PanelNavySoft = Palette.Hex("243657");

        private VirtualJoystick _stick;

        // Top bar
        private Image _energyFill;
        private Image _chaosFill;
        private Text _timer;
        private Text _ping;

        // Task panel
        private Text _taskHeader;
        private readonly List<TaskRow> _rows = new List<TaskRow>();

        // Action cluster
        private RectTransform _handButton;
        private Image _handPlate;
        private Image _handIcon;
        private Image _handRing;
        private RectTransform _boxButton;
        private Image _boxPlate;
        private RectTransform _runButton;
        private Image _runPlate;
        private Image _runRing;
        private RectTransform _sabButton;

        // Floating prompt
        private RectTransform _prompt;
        private Text _promptLabel;

        private Text _toast;
        private float _toastTimer;
        private Image _roleBadge;
        private Text _roleText;

        private RectTransform _taskPanelExpanded;
        private RectTransform _sabotagePanel;
        private readonly Dictionary<SabotageKind, Image> _sabCooldowns = new Dictionary<SabotageKind, Image>();
        private readonly Dictionary<SabotageKind, Button> _sabButtons = new Dictionary<SabotageKind, Button>();

        private sealed class TaskRow
        {
            public RectTransform Root;
            public Image Tile;
            public Image Glyph;
            public Image CheckCircle;
            public Text Count;
            public Image BarFill;
        }

        public Vector2 MoveInput =>
            _stick != null && _stick.isActiveAndEnabled ? _stick.Value : Vector2.zero;

        protected override void Build()
        {
            BuildTopBar();
            BuildTaskPanel();
            BuildPrompt();
            BuildControls();
            BuildSabotagePanel();
            BuildExpandedTaskPanel();

            _toast = UIKit.Label("Toast", Root, "", 38, Palette.Accent);
            UIKit.Place(_toast.rectTransform, new Vector2(0.5f, 0.63f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(940f, 90f));
            _toast.color = Palette.WithAlpha(Palette.Accent, 0f);
        }

        // ------------------------------------------------------------------ top bar

        private void BuildTopBar()
        {
            // Energy: bolt glyph then a rounded track, exactly as in the mock-up.
            Image bolt = UIKit.NewImage("Bolt", Root, IconLab.Get(Icon.Bolt), Palette.Hex("FFD233"));
            UIKit.Place(bolt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(-252f, -56f), new Vector2(46f, 46f));

            Image track = UIKit.NewImage("EnergyTrack", Root, TextureLab.RoundedRect(16),
                new Color(0.06f, 0.09f, 0.16f, 0.72f));
            UIKit.Place(track.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(10f, -56f), new Vector2(440f, 34f));

            _energyFill = UIKit.NewImage("EnergyFill", track.rectTransform, TextureLab.RoundedRect(14),
                Palette.Hex("FFD233"));
            _energyFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _energyFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _energyFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _energyFill.rectTransform.offsetMin = new Vector2(4f, 4f);
            _energyFill.rectTransform.offsetMax = new Vector2(4f, -4f);

            // Chaos rides just under it so both meters read as one instrument cluster.
            Image skull = UIKit.NewImage("Skull", Root, IconLab.Get(Icon.Skull), Palette.Hex("F7509B"));
            UIKit.Place(skull.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(-252f, -96f), new Vector2(36f, 36f));

            Image chaosTrack = UIKit.NewImage("ChaosTrack", Root, TextureLab.RoundedRect(12),
                new Color(0.06f, 0.09f, 0.16f, 0.72f));
            UIKit.Place(chaosTrack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(10f, -96f), new Vector2(440f, 22f));

            _chaosFill = UIKit.NewImage("ChaosFill", chaosTrack.rectTransform, TextureLab.RoundedRect(10),
                Palette.Hex("F7509B"));
            _chaosFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _chaosFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _chaosFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _chaosFill.rectTransform.offsetMin = new Vector2(3f, 3f);
            _chaosFill.rectTransform.offsetMax = new Vector2(3f, -3f);

            // Clock: heavy, white, with a hard shadow so it survives any background.
            _timer = UIKit.Label("Timer", Root, "03:42", 92, Color.white);
            UIKit.Place(_timer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(96f, -186f), new Vector2(420f, 120f));
            var outline = _timer.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.04f, 0.07f, 0.14f, 0.95f);
            outline.effectDistance = new Vector2(4f, -4f);
            var shadow = _timer.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -8f);

            // Connection readout and settings, top right.
            // Chip behind the readout so it stays legible over a bright floor.
            Image netChip = UIKit.NewImage("NetChip", Root, TextureLab.RoundedRect(18),
                new Color(0.06f, 0.09f, 0.16f, 0.62f));
            UIKit.Place(netChip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-104f, -58f), new Vector2(212f, 56f));

            Image signal = UIKit.NewImage("Signal", Root, IconLab.Get(Icon.Signal), Palette.Hex("5BE07A"));
            UIKit.Place(signal.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-250f, -58f), new Vector2(40f, 40f));

            _ping = UIKit.Label("Ping", Root, "32ms", 30, Palette.Hex("9FE8B0"), TextAnchor.MiddleLeft);
            UIKit.Place(_ping.rectTransform, new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                new Vector2(-242f, -58f), new Vector2(140f, 40f));

            Button gear = UIKit.Button("Gear", Root, "", PanelNavy, Color.white, 30, OpenPause);
            UIKit.Place((RectTransform)gear.transform, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-42f, -58f), new Vector2(92f, 92f));
            RoundPlate(gear);
            Image gearIcon = UIKit.NewImage("Icon", (RectTransform)gear.transform, IconLab.Get(Icon.Gear), Color.white);
            UIKit.Place(gearIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(50f, 50f));

            _roleBadge = UIKit.Panel("RoleBadge", Root, Palette.Good, 16);
            UIKit.Place(_roleBadge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(28f, -418f), new Vector2(208f, 54f));
            _roleText = UIKit.Label("Text", _roleBadge.rectTransform, "CREW", 32, Palette.Backdrop);
            UIKit.Stretch(_roleText.rectTransform);
        }

        /// <summary>Swaps a button's plate for a circular one.</summary>
        private static void RoundPlate(Button b)
        {
            var plate = b.targetGraphic as Image;
            if (plate != null) plate.sprite = TextureLab.Circle(256);
            Transform shadow = b.transform.Find("Shadow");
            if (shadow != null)
            {
                var img = shadow.GetComponent<Image>();
                if (img != null) img.sprite = TextureLab.Circle(256);
            }
        }

        // ------------------------------------------------------------------ tasks

        private void BuildTaskPanel()
        {
            Image panel = UIKit.Panel("TaskPanel", Root, Palette.WithAlpha(PanelNavy, 0.93f), 24);
            UIKit.Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -104f), new Vector2(396f, 286f));

            Image stroke = UIKit.Outline("Stroke", panel.rectTransform, Palette.Hex("4C6A9E"), 24, 3);
            UIKit.Stretch(stroke.rectTransform);

            _taskHeader = UIKit.Label("Header", panel.rectTransform, "TASKS <color=#FFD233>0/5</color>", 40,
                Color.white, TextAnchor.MiddleLeft);
            UIKit.Place(_taskHeader.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -16f), new Vector2(350f, 50f));

            for (int i = 0; i < 3; i++)
            {
                var row = new TaskRow();
                row.Root = UIKit.NewRect("Row" + i, panel.rectTransform);
                UIKit.Place(row.Root, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(16f, -78f - i * 68f), new Vector2(364f, 62f));

                row.Tile = UIKit.NewImage("Tile", row.Root, TextureLab.RoundedRect(14), PanelNavySoft);
                UIKit.Place(row.Tile.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(6f, 0f), new Vector2(52f, 52f));

                row.Glyph = UIKit.NewImage("Glyph", row.Tile.rectTransform, IconLab.Get(Icon.Document), Color.white);
                UIKit.Place(row.Glyph.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(34f, 34f));

                row.CheckCircle = UIKit.NewImage("Check", row.Root, TextureLab.Circle(128), Palette.Hex("3FCF6A"));
                UIKit.Place(row.CheckCircle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(68f, 0f), new Vector2(38f, 38f));
                Image tick = UIKit.NewImage("Tick", row.CheckCircle.rectTransform, IconLab.Get(Icon.Check), Color.white);
                UIKit.Place(tick.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(23f, 23f));

                row.Count = UIKit.Label("Count", row.Root, "0/1", 30, Palette.Hex("C9D6EE"), TextAnchor.MiddleLeft);
                UIKit.Place(row.Count.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(68f, 0f), new Vector2(64f, 38f));

                Image bar = UIKit.NewImage("Bar", row.Root, TextureLab.RoundedRect(10), Palette.Hex("0E1830"));
                UIKit.Place(bar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(136f, 0f), new Vector2(214f, 18f));

                row.BarFill = UIKit.NewImage("Fill", bar.rectTransform, TextureLab.RoundedRect(10),
                    Palette.Hex("3FCF6A"));
                row.BarFill.rectTransform.anchorMin = new Vector2(0f, 0f);
                row.BarFill.rectTransform.anchorMax = new Vector2(0f, 1f);
                row.BarFill.rectTransform.pivot = new Vector2(0f, 0.5f);
                row.BarFill.rectTransform.offsetMin = Vector2.zero;
                row.BarFill.rectTransform.offsetMax = Vector2.zero;

                _rows.Add(row);
            }

            Pressable.Attach(panel.gameObject, () => TogglePanel(_taskPanelExpanded));
        }

        private void BuildPrompt()
        {
            _prompt = UIKit.NewRect("Prompt", Root);
            UIKit.Place(_prompt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 150f), new Vector2(340f, 110f));

            Image pill = UIKit.NewImage("Pill", _prompt, TextureLab.RoundedRect(22), PanelNavy);
            UIKit.Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(290f, 64f));
            Image pillStroke = UIKit.Outline("Stroke", pill.rectTransform, Palette.Hex("6B8CC4"), 22, 3);
            UIKit.Stretch(pillStroke.rectTransform);

            _promptLabel = UIKit.Label("Label", pill.rectTransform, "INTERACT", 34, Color.white);
            UIKit.Stretch(_promptLabel.rectTransform);

            Image chev = UIKit.NewImage("Chevron", _prompt, IconLab.Get(Icon.ChevronDown), Palette.Hex("8FD8F0"));
            UIKit.Place(chev.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -70f), new Vector2(40f, 30f));

            _prompt.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ controls

        private void BuildControls()
        {
            _stick = VirtualJoystick.Create(Root);

            // Primary action: the big yellow hand.
            Button hand = UIKit.Button("Hand", Root, "", Palette.Hex("FFD233"), Color.white, 30, null);
            _handButton = (RectTransform)hand.transform;
            UIKit.Place(_handButton, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-190f, 200f), new Vector2(248f, 248f));
            RoundPlate(hand);
            _handPlate = hand.targetGraphic as Image;

            _handRing = UIKit.NewImage("Ring", _handButton, TextureLab.Ring(256, 12), Color.white);
            UIKit.Stretch(_handRing.rectTransform, -8f, -8f, -8f, -8f);

            _handIcon = UIKit.NewImage("Icon", _handButton, IconLab.Get(Icon.Hand), Color.white);
            UIKit.Place(_handIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(124f, 124f));

            Pressable.Attach(hand.gameObject, () => D.OnInteractDown(), () => D.OnInteractUp());

            // Carry.
            Button box = UIKit.Button("Carry", Root, "", PanelNavy, Color.white, 28, D.OnCarryButton);
            _boxButton = (RectTransform)box.transform;
            UIKit.Place(_boxButton, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-372f, 352f), new Vector2(152f, 152f));
            RoundPlate(box);
            _boxPlate = box.targetGraphic as Image;
            UIKit.NewImage("Ring", _boxButton, TextureLab.Ring(256, 8), new Color(1f, 1f, 1f, 0.6f))
                .rectTransform.let(rt => UIKit.Stretch(rt, -4f, -4f, -4f, -4f));
            Image boxIcon = UIKit.NewImage("Icon", _boxButton, IconLab.Get(Icon.Box), Color.white);
            UIKit.Place(boxIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(74f, 74f));

            // Sprint.
            Button run = UIKit.Button("Run", Root, "", PanelNavy, Color.white, 28, null);
            _runButton = (RectTransform)run.transform;
            UIKit.Place(_runButton, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-146f, 452f), new Vector2(152f, 152f));
            RoundPlate(run);
            _runPlate = run.targetGraphic as Image;
            _runRing = UIKit.NewImage("Ring", _runButton, TextureLab.Ring(256, 8), new Color(1f, 1f, 1f, 0.6f));
            UIKit.Stretch(_runRing.rectTransform, -4f, -4f, -4f, -4f);
            Image runIcon = UIKit.NewImage("Icon", _runButton, IconLab.Get(Icon.Runner), Color.white);
            UIKit.Place(runIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(80f, 80f));
            Pressable.Attach(run.gameObject, () => D.SetSprint(true), () => D.SetSprint(false));

            // Sabotage, saboteur only.
            Button sab = UIKit.Button("Sabotage", Root, "", Palette.Hex("F7509B"), Color.white, 28,
                () => TogglePanel(_sabotagePanel));
            _sabButton = (RectTransform)sab.transform;
            UIKit.Place(_sabButton, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-372f, 552f), new Vector2(152f, 152f));
            RoundPlate(sab);
            UIKit.NewImage("Ring", _sabButton, TextureLab.Ring(256, 8), new Color(1f, 1f, 1f, 0.7f))
                .rectTransform.let(rt => UIKit.Stretch(rt, -4f, -4f, -4f, -4f));
            Image sabIcon = UIKit.NewImage("Icon", _sabButton, IconLab.Get(Icon.Skull), Color.white);
            UIKit.Place(sabIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(78f, 78f));
        }

        // ------------------------------------------------------------------ panels

        private void BuildExpandedTaskPanel()
        {
            _taskPanelExpanded = UIKit.NewRect("TaskDetail", Root);
            UIKit.Place(_taskPanelExpanded, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 80f), new Vector2(900f, 160f + CCConfig.TasksPerCrew * 86f + 180f));

            Image plate = UIKit.Panel("Plate", _taskPanelExpanded, Palette.WithAlpha(PanelNavy, 0.97f), 28);
            UIKit.Stretch(plate.rectTransform);
            UIKit.Outline("Stroke", plate.rectTransform, Palette.Hex("4C6A9E"), 28, 3)
                .rectTransform.let(rt => UIKit.Stretch(rt));

            Text head = UIKit.Label("Head", _taskPanelExpanded, "DEINE AUFGABEN", 46, Palette.Accent);
            UIKit.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -54f), new Vector2(820f, 60f));

            for (int i = 0; i < CCConfig.TasksPerCrew; i++)
            {
                Text row = UIKit.Label("Task" + i, _taskPanelExpanded, "", 34, Color.white,
                    TextAnchor.MiddleLeft, FontStyle.Normal);
                UIKit.Place(row.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(56f, -130f - i * 86f), new Vector2(790f, 74f));
                row.name = "Detail" + i;
            }

            Text note = UIKit.Label("Note", _taskPanelExpanded, "", 28, Palette.InkMuted,
                TextAnchor.UpperLeft, FontStyle.Italic);
            UIKit.Place(note.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(56f, 130f), new Vector2(790f, 110f));

            UIKit.Place((RectTransform)UIKit.Button("Close", _taskPanelExpanded, "SCHLIESSEN",
                    PanelNavySoft, Color.white, 34, () => TogglePanel(_taskPanelExpanded)).transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(440f, 96f));

            _taskPanelExpanded.gameObject.SetActive(false);
        }

        private void BuildSabotagePanel()
        {
            _sabotagePanel = UIKit.NewRect("SabotagePanel", Root);
            UIKit.Place(_sabotagePanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(960f, 1180f));

            Image plate = UIKit.Panel("Plate", _sabotagePanel, Palette.WithAlpha(PanelNavy, 0.97f), 28);
            UIKit.Stretch(plate.rectTransform);
            UIKit.Outline("Stroke", plate.rectTransform, Palette.Hex("F7509B"), 28, 3)
                .rectTransform.let(rt => UIKit.Stretch(rt));

            Text head = UIKit.Label("Head", _sabotagePanel, "CHAOS STIFTEN", 46, Palette.Hex("F7509B"));
            UIKit.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -50f), new Vector2(860f, 60f));

            var catalog = SabotageSystem.Catalog;
            for (int i = 0; i < catalog.Length; i++)
            {
                SabotageDef def = catalog[i];
                int col = i % 3;
                int row = i / 3;

                RectTransform cell = UIKit.NewRect("Sab_" + def.Kind, _sabotagePanel);
                UIKit.Place(cell, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2((col - 1) * 290f, -220f - row * 250f), new Vector2(270f, 230f));

                Button b = UIKit.Button("Btn", cell, "",
                    def.IsTampering ? PanelNavySoft : Palette.Hex("C93A78"), Color.white, 30,
                    () => Fire(def.Kind));
                UIKit.Stretch((RectTransform)b.transform);
                _sabButtons[def.Kind] = b;

                Text glyph = UIKit.Label("Glyph", cell, def.Glyph, 42, Color.white);
                UIKit.Place(glyph.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -30f), new Vector2(240f, 56f));

                Text name = UIKit.Label("Name", cell, def.DisplayName, 24,
                    Palette.WithAlpha(Color.white, 0.92f), TextAnchor.UpperCenter, FontStyle.Normal);
                UIKit.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -96f), new Vector2(240f, 90f));

                Image cd = UIKit.NewImage("Cooldown", cell, TextureLab.RoundedRect(28),
                    new Color(0f, 0f, 0f, 0.66f));
                cd.rectTransform.anchorMin = new Vector2(0f, 0f);
                cd.rectTransform.anchorMax = new Vector2(1f, 0f);
                cd.rectTransform.pivot = new Vector2(0.5f, 0f);
                _sabCooldowns[def.Kind] = cd;
            }

            Text tip = UIKit.Label("Tip", _sabotagePanel,
                "Lager, Toiletten und Aufzug haben keine Kamera.",
                26, Palette.InkMuted, TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.Place(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 150f), new Vector2(860f, 60f));

            UIKit.Place((RectTransform)UIKit.Button("Close", _sabotagePanel, "SCHLIESSEN",
                    PanelNavySoft, Color.white, 34, () => TogglePanel(_sabotagePanel)).transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(440f, 96f));

            _sabotagePanel.gameObject.SetActive(false);
        }

        private void Fire(SabotageKind kind)
        {
            D.TriggerSabotage(kind);
            TogglePanel(_sabotagePanel);
        }

        private void OpenPause()
        {
            D.Audio.Play(Sfx.Tap);
            D.OpenSettings();
        }

        private void TogglePanel(RectTransform panel)
        {
            bool open = !panel.gameObject.activeSelf;
            _taskPanelExpanded.gameObject.SetActive(false);
            _sabotagePanel.gameObject.SetActive(false);
            panel.gameObject.SetActive(open);
            SetControlsActive(!open);
            D.Audio.Play(Sfx.Tap);
        }

        public void SetControlsActive(bool active)
        {
            if (_stick != null)
            {
                _stick.ResetStick();
                _stick.gameObject.SetActive(active);
            }
            if (_handButton != null) _handButton.gameObject.SetActive(active);
            if (_boxButton != null) _boxButton.gameObject.SetActive(active);
            if (_runButton != null) _runButton.gameObject.SetActive(active);
            if (_sabButton != null)
            {
                PlayerState p = D.State != null ? D.State.Local : null;
                _sabButton.gameObject.SetActive(active && p != null && p.IsSaboteur);
            }
            if (!active) D.SetSprint(false);
        }

        // ------------------------------------------------------------------ refresh

        public void Rebuild()
        {
            PlayerState p = D.State != null ? D.State.Local : null;
            bool saboteur = p != null && p.IsSaboteur;

            _roleBadge.color = saboteur ? Palette.Hex("F7509B") : Palette.Hex("3FCF6A");
            _roleText.text = saboteur ? "SABOTEUR" : "CREW";
            _roleText.color = saboteur ? Color.white : Palette.Backdrop;

            _taskPanelExpanded.gameObject.SetActive(false);
            _sabotagePanel.gameObject.SetActive(false);
            SetControlsActive(true);

            Transform note = _taskPanelExpanded.Find("Note");
            if (note != null)
            {
                var t = note.GetComponent<Text>();
                if (t != null)
                    t.text = saboteur
                        ? "Diese Liste zählt für niemanden. Tu trotzdem so, als würdest du arbeiten."
                        : "Erledige alles, bevor die Zeit abläuft.";
            }
        }

        public void Toast(string message)
        {
            _toast.text = message;
            _toastTimer = 2.6f;
        }

        public void Refresh()
        {
            MatchState s = D.State;
            if (s == null) return;

            _timer.text = UIKit.FormatTime(s.TimeLeft);
            _timer.color = s.TimeLeft < 30f
                ? Color.Lerp(Palette.Hex("FF5A5F"), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.time * 8f))
                : Color.white;

            _ping.text = D.PingMs + "ms";

            PlayerState p = s.Local;
            if (p != null)
            {
                UIKit.SetBar(_energyFill, p.Energy / CCConfig.EnergyMax);
                _energyFill.color = p.Winded ? Palette.Hex("FF8A3D") : Palette.Hex("FFD233");
            }
            UIKit.SetBar(_chaosFill, s.Chaos01);

            RefreshTaskRows();
            RefreshButtons();
            RefreshDetailRows();
            RefreshCooldowns();

            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
                _toast.color = Palette.WithAlpha(Palette.Accent, Mathf.Clamp01(_toastTimer * 1.6f));
            }
        }

        /// <summary>
        /// The checklist groups tasks by pictogram, the way the concept art does: one row per
        /// category with its own little progress bar.
        /// </summary>
        private void RefreshTaskRows()
        {
            PlayerState p = D.State.Local;
            if (p == null) return;

            var order = new List<TaskIcon>();
            var done = new Dictionary<TaskIcon, int>();
            var total = new Dictionary<TaskIcon, int>();

            for (int i = 0; i < p.TaskIds.Count; i++)
            {
                StationDef st = D.State.StationById(p.TaskIds[i]);
                if (st == null) continue;
                if (!total.ContainsKey(st.Icon))
                {
                    order.Add(st.Icon);
                    total[st.Icon] = 0;
                    done[st.Icon] = 0;
                }
                total[st.Icon]++;
                if (p.CompletedTaskIds.Contains(p.TaskIds[i])) done[st.Icon]++;
            }

            int completed = p.CompletedTaskIds.Count;
            _taskHeader.text = "TASKS <color=#FFD233>" + completed + "/" + p.TaskIds.Count + "</color>";

            for (int i = 0; i < _rows.Count; i++)
            {
                TaskRow row = _rows[i];
                bool used = i < order.Count;
                row.Root.gameObject.SetActive(used);
                if (!used) continue;

                TaskIcon icon = order[i];
                int d = done[icon];
                int t = total[icon];
                bool complete = d >= t;

                row.Glyph.sprite = IconLab.Get(IconFor(icon));
                row.CheckCircle.gameObject.SetActive(complete);
                row.Count.gameObject.SetActive(!complete);
                row.Count.text = d + "/" + t;

                UIKit.SetBar(row.BarFill, t == 0 ? 0f : d / (float)t);
                row.BarFill.color = complete ? Palette.Hex("3FCF6A") : Palette.Hex("FFD233");
            }
        }

        private static Icon IconFor(TaskIcon icon)
        {
            switch (icon)
            {
                case TaskIcon.Coffee: return Icon.Coffee;
                case TaskIcon.Box: return Icon.Box;
                default: return Icon.Document;
            }
        }

        private void RefreshButtons()
        {
            bool canInteract = D.TryGetInteraction(out string label, out _, out _);
            _handPlate.color = canInteract ? Palette.Hex("FFD233") : Palette.Hex("9C8A3A");
            _handIcon.color = canInteract ? Color.white : new Color(1f, 1f, 1f, 0.7f);
            _handRing.color = canInteract
                ? Color.Lerp(Color.white, Palette.Hex("FFF2B0"), 0.5f + 0.5f * Mathf.Sin(Time.time * 5f))
                : new Color(1f, 1f, 1f, 0.35f);

            float hold = D.UnjamProgress;
            if (hold > 0f) _handRing.rectTransform.localScale = Vector3.one * (0.9f + hold * 0.16f);
            else _handRing.rectTransform.localScale = Vector3.one;

            // The floating prompt mirrors the action button, right above the character.
            bool showPrompt = canInteract;
            if (_prompt.gameObject.activeSelf != showPrompt) _prompt.gameObject.SetActive(showPrompt);
            if (showPrompt)
            {
                _promptLabel.text = label;
                _prompt.anchoredPosition = new Vector2(0f, 150f + Mathf.Sin(Time.time * 3f) * 8f);
            }

            bool canCarry = D.CanCarryAct();
            _boxPlate.color = canCarry ? PanelNavy : new Color(0.35f, 0.4f, 0.5f, 0.5f);

            PlayerState p = D.State.Local;
            bool canRun = p != null && !p.Winded && p.Energy > 1f;
            _runPlate.color = p != null && p.Sprinting
                ? Palette.Hex("3FCF6A")
                : (canRun ? PanelNavy : new Color(0.35f, 0.4f, 0.5f, 0.5f));
            _runRing.color = p != null && p.Sprinting
                ? new Color(1f, 1f, 1f, 0.95f)
                : new Color(1f, 1f, 1f, 0.55f);
        }

        private void RefreshDetailRows()
        {
            if (!_taskPanelExpanded.gameObject.activeSelf) return;
            PlayerState p = D.State.Local;
            if (p == null) return;

            for (int i = 0; i < CCConfig.TasksPerCrew; i++)
            {
                Transform t = _taskPanelExpanded.Find("Detail" + i);
                if (t == null) continue;
                var label = t.GetComponent<Text>();
                if (label == null) continue;

                if (i >= p.TaskIds.Count)
                {
                    label.text = "";
                    continue;
                }

                string id = p.TaskIds[i];
                StationDef st = D.State.StationById(id);
                RoomDef room = st != null ? D.State.Map.RoomById(st.RoomId) : null;
                bool complete = p.CompletedTaskIds.Contains(id);

                label.text = (complete ? "<b>[x]</b>  " : "[  ]  ")
                             + (st != null ? st.DisplayName : id)
                             + (room != null ? "   <i>" + room.DisplayName + "</i>" : "");
                label.color = complete ? Palette.Hex("3FCF6A") : Color.white;
            }
        }

        private void RefreshCooldowns()
        {
            PlayerState p = D.State.Local;
            if (p == null || !p.IsSaboteur || D.Sabotage == null) return;

            foreach (var kv in _sabCooldowns)
            {
                float f = D.Sabotage.CooldownFraction(kv.Key);
                kv.Value.rectTransform.sizeDelta = new Vector2(0f, 230f * f);
                kv.Value.gameObject.SetActive(f > 0.001f);
                if (_sabButtons.TryGetValue(kv.Key, out Button b)) b.interactable = f <= 0.001f;
            }
        }
    }
}
