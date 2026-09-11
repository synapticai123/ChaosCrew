using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ChaosCrew
{
    /// <summary>
    /// Owns the match: phase machine, simulation tick and the wiring between systems.
    /// Screens talk to this and nothing else.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }

        public GamePhase Phase { get; private set; } = GamePhase.Boot;
        public MatchState State { get; private set; }
        public GameSettings Settings { get; private set; }

        public MapRuntime MapRuntime { get; private set; }
        public TaskSystem Tasks { get; private set; }
        public SabotageSystem Sabotage { get; private set; }
        public HazardSystem Hazards { get; private set; }
        public CarrySystem Carry { get; private set; }
        public EvidenceRecorder Evidence { get; private set; }
        public VoteSystem Votes { get; private set; }
        public BotBrain Bots { get; private set; }

        public SfxSynth Audio { get; private set; }
        public UIManager UI { get; private set; }
        public WorldBuilder World { get; private set; }
        public IsoCamera Cam { get; private set; }
        public LocalTransport Transport { get; private set; }

        public List<CameraClip> Clips { get; private set; } = new List<CameraClip>();

        private MiniGame _openMiniGame;
        private Vector2Int _unjamTarget;
        private bool _unjamActive;
        private float _unjamProgress;
        private float _phaseTimer;
        private bool _botVotesCast;
        private readonly Dictionary<string, Transform> _crateViews = new Dictionary<string, Transform>();

        public bool MiniGameOpen => _openMiniGame != null;
        public float UnjamProgress => _unjamActive ? Mathf.Clamp01(_unjamProgress / CCConfig.DoorUnjamSeconds) : 0f;
        public float PhaseTimer => _phaseTimer;
        /// <summary>Fake round-trip time shown in the HUD. Real numbers arrive with real netcode.</summary>
        public int PingMs { get; private set; } = 32;

        // ------------------------------------------------------------ lifecycle

        private void Awake()
        {
            Instance = this;

            Settings = new GameSettings();
            Settings.Load();

            Audio = gameObject.AddComponent<SfxSynth>();
            Audio.Muted = Settings.Muted;
            Audio.Volume = Settings.Volume;

            Transport = new LocalTransport();
            Transport.MatchStarted += OnTransportMatchStarted;

            Cam = gameObject.AddComponent<IsoCamera>();
            Cam.Bind(Camera.main);

            World = gameObject.AddComponent<WorldBuilder>();

            UI = UIManager.Create(this);

            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
        }

        private void Start() => UI.Loading.Begin(GoToTitle);

        private void Update()
        {
            float dt = Time.deltaTime;

            switch (Phase)
            {
                case GamePhase.Lobby:
                    Transport.Tick(dt);
                    UI.Lobby.Refresh();
                    break;

                case GamePhase.RoleReveal:
                    _phaseTimer -= dt;
                    UI.RoleReveal.Refresh(_phaseTimer);
                    World.Tick(dt);
                    FollowCamera(dt);
                    if (_phaseTimer <= 0f) BeginRound();
                    break;

                case GamePhase.Round:
                    TickRound(dt);
                    break;

                case GamePhase.Voting:
                    TickVoting(dt);
                    break;
            }
        }

        // ------------------------------------------------------------ phase changes

        public void GoToTitle()
        {
            Phase = GamePhase.Title;
            ClearWorld();
            UI.Show(GamePhase.Title);
        }

        public void OpenSettings()
        {
            Phase = GamePhase.Settings;
            UI.Show(GamePhase.Settings);
        }

        public void OpenLobby()
        {
            Phase = GamePhase.Lobby;
            Transport.OpenLobby(Settings.PlayerName);
            Transport.SetReady(Transport.LocalSeatId, true);
            UI.Show(GamePhase.Lobby);
            UI.Lobby.Refresh();
            Audio.Play(Sfx.Confirm);
        }

        public void LeaveLobby()
        {
            Transport.Leave();
            GoToTitle();
        }

        public void RequestStart()
        {
            if (Transport.Seats.Count < CCConfig.PlayerCount) return;
            Transport.RequestStart();
        }

        private void OnTransportMatchStarted(int seed) => SetUpMatch(seed);

        /// <summary>Builds a fresh match from the seat list and deals secret roles.</summary>
        private void SetUpMatch(int seed)
        {
            State = new MatchState
            {
                Seed = seed,
                Rng = new DeterministicRng(seed),
                Map = MapCatalog.BuildDefault(),
                LocalPlayerId = Transport.LocalSeatId,
                TimeLeft = CCConfig.RoundSeconds,
                Chaos = 0f
            };

            MapRuntime = new MapRuntime(State.Map);

            for (int i = 0; i < Transport.Seats.Count; i++)
            {
                SeatInfo seat = Transport.Seats[i];
                State.Players.Add(new PlayerState
                {
                    Id = seat.Id,
                    DisplayName = seat.DisplayName,
                    ColorIndex = seat.ColorIndex,
                    IsLocal = seat.IsLocal,
                    IsBot = seat.IsBot,
                    Role = Role.Crew,
                    Position = State.Map.SpawnPoints[i % State.Map.SpawnPoints.Count],
                    Energy = CCConfig.EnergyMax
                });
            }

            AssignRoles();

            Evidence = new EvidenceRecorder();
            Evidence.Begin(State);

            Hazards = new HazardSystem();
            Hazards.Bind(State, Evidence, Audio);

            Tasks = new TaskSystem();
            Tasks.Bind(State, Evidence, Audio);
            Tasks.AssignTasks();

            Carry = new CarrySystem();
            Carry.Bind(State, MapRuntime, Evidence, Audio);

            Sabotage = new SabotageSystem();
            Sabotage.Bind(State, MapRuntime, Hazards, Tasks, Evidence, Audio);
            Sabotage.OnSabotage += OnSabotageFired;

            Bots = new BotBrain();
            Bots.Bind(State, MapRuntime, Tasks, Sabotage, Evidence);

            Hazards.OnPlayerSlipped += (p, h) =>
            {
                if (p.IsLocal) Cam.Shake(0.5f, 0.35f);
                if (p.IsCarrying) Carry.TryDrop(p);
            };

            World.Build(State, MapRuntime, Tasks, Hazards, Cam);
            BuildCrateViews();
            Cam.SnapTo(State.Local != null ? State.Local.Position : State.Map.ElevatorPad);

            UI.Hud.Rebuild();

            Phase = GamePhase.RoleReveal;
            _phaseTimer = CCConfig.RoleRevealSeconds;
            UI.Show(GamePhase.RoleReveal);
            UI.RoleReveal.Present(State.Local);
            Audio.Play(Sfx.Alarm);
        }

        private void AssignRoles()
        {
            var candidates = new List<PlayerState>(State.Players);

            PlayerState chosen;
            if (Settings.RolePreference == RolePreference.AlwaysSaboteur)
            {
                chosen = State.Local;
            }
            else if (Settings.RolePreference == RolePreference.AlwaysCrew)
            {
                candidates.Remove(State.Local);
                chosen = candidates[State.Rng.Range(0, candidates.Count)];
            }
            else
            {
                chosen = candidates[State.Rng.Range(0, candidates.Count)];
            }

            for (int i = 0; i < State.Players.Count; i++) State.Players[i].Role = Role.Crew;
            if (chosen != null) chosen.Role = Role.Saboteur;
        }

        private void BeginRound()
        {
            Phase = GamePhase.Round;
            UI.Show(GamePhase.Round);
            UI.Hud.Refresh();
        }

        // ------------------------------------------------------------ round tick

        private void TickRound(float dt)
        {
            State.TimeLeft -= dt;

            MapRuntime.Tick(dt);
            Sabotage.Tick(dt);

            for (int i = 0; i < State.Players.Count; i++)
            {
                PlayerState p = State.Players[i];
                p.SpeedMultiplier = 1f;
                if (p.StunTimer > 0f) p.StunTimer -= dt;
                if (p.IsCarrying) p.SpeedMultiplier *= CCConfig.CarrySpeedFactor;
            }

            Hazards.Tick(dt);
            TickEnergy(dt);
            MoveLocalPlayer(dt);
            Bots.Tick(dt);
            Carry.Tick(dt);

            for (int i = 0; i < State.Players.Count; i++)
            {
                PlayerState p = State.Players[i];
                p.SlideVelocity = Vector2.Lerp(p.SlideVelocity, Vector2.zero, 1f - Mathf.Exp(-3.2f * dt));
            }

            Evidence.Tick(dt);
            World.Tick(dt);
            SyncCrateViews();
            FollowCamera(dt);
            UI.Hud.Refresh();

            if (State.CompletedTaskCount >= State.TotalTaskCount && State.TotalTaskCount > 0)
                EndRound(RoundEndReason.TasksCompleted);
            else if (State.Chaos >= CCConfig.ChaosMax)
                EndRound(RoundEndReason.ChaosMaxed);
            else if (State.TimeLeft <= 0f)
                EndRound(RoundEndReason.TimeUp);
        }

        private void FollowCamera(float dt)
        {
            PlayerState p = State != null ? State.Local : null;
            if (p == null) return;
            Cam.SetZoom(p.Sprinting ? CCConfig.CameraSizeSprint : CCConfig.CameraSize);
            Cam.Follow(p.Position, dt);
        }

        /// <summary>Sprint burns stamina; running dry forces a breather before you can go again.</summary>
        private void TickEnergy(float dt)
        {
            for (int i = 0; i < State.Players.Count; i++)
            {
                PlayerState p = State.Players[i];

                bool wants = p.SprintHeld && !p.IsStunned && !p.Winded && p.Speed > 0.4f;
                p.Sprinting = wants && p.Energy > 0f;

                if (p.Sprinting)
                {
                    p.Energy -= CCConfig.EnergyDrainPerSecond * dt;
                    if (p.Energy <= 0f)
                    {
                        p.Energy = 0f;
                        p.Winded = true;
                        p.Sprinting = false;
                    }
                }
                else
                {
                    p.Energy = Mathf.Min(CCConfig.EnergyMax, p.Energy + CCConfig.EnergyRegenPerSecond * dt);
                    if (p.Winded && p.Energy >= CCConfig.EnergyResumeThreshold) p.Winded = false;
                }

                if (p.Sprinting) p.SpeedMultiplier *= CCConfig.SprintMultiplier;
            }
        }

        private void MoveLocalPlayer(float dt)
        {
            PlayerState p = State.Local;
            if (p == null) return;

            if (MiniGameOpen || p.IsStunned)
            {
                p.Speed = 0f;
                TickUnjam(dt, true);
                return;
            }

            Vector2 raw = UI.Hud.MoveInput + ReadKeyboard();
            if (raw.sqrMagnitude > 1f) raw.Normalize();

            // The stick is screen-relative; rotate it onto the isometric ground plane.
            Vector2 input = Cam.StickToWorld(raw);

            Vector2 velocity = input * CCConfig.WalkSpeed * p.SpeedMultiplier + p.SlideVelocity;
            Vector2 before = p.Position;
            p.Position = MapRuntime.MoveWithCollision(p.Position, velocity * dt, CCConfig.PlayerRadius);
            p.Speed = Vector2.Distance(before, p.Position) / Mathf.Max(0.0001f, dt);
            if (input.sqrMagnitude > 0.01f) p.Facing = input.normalized;

            TickUnjam(dt, raw.sqrMagnitude > 0.02f);
        }

        private static Vector2 ReadKeyboard()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            Vector2 v = Vector2.zero;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
            return v;
#else
            return Vector2.zero;
#endif
        }

        // ------------------------------------------------------------ input handlers

        public void SetSprint(bool held)
        {
            PlayerState p = State != null ? State.Local : null;
            if (p != null) p.SprintHeld = held;
        }

        /// <summary>Carry button: grab the nearest crate, or set down the one in hand.</summary>
        public void OnCarryButton()
        {
            if (Phase != GamePhase.Round || MiniGameOpen) return;
            PlayerState p = State.Local;
            if (p == null) return;

            if (p.IsCarrying)
            {
                bool delivered = Vector2.Distance(p.Position, Carry.DropZone) <= CCConfig.DropZoneRadius;
                Carry.TryDrop(p);
                UI.Hud.Toast(delivered && !p.IsSaboteur ? "Kiste abgeliefert!" : "Kiste abgestellt");
            }
            else if (!Carry.TryPickUp(p))
            {
                UI.Hud.Toast("Keine Kiste in der Nähe");
            }
        }

        public bool CanCarryAct()
        {
            PlayerState p = State != null ? State.Local : null;
            if (p == null) return false;
            return p.IsCarrying || Carry.NearestFree(p.Position) != null;
        }

        /// <summary>What the interact button should currently do.</summary>
        public bool TryGetInteraction(out string label, out StationDef station, out Vector2Int door)
        {
            label = null;
            station = null;
            door = default;

            PlayerState p = State != null ? State.Local : null;
            if (p == null || p.IsStunned) return false;

            station = Tasks.NearestActionableStation(p, out _);
            if (station != null)
            {
                label = "INTERACT";
                return true;
            }

            float best = CCConfig.InteractRange * 1.4f;
            bool found = false;
            foreach (Vector2Int t in MapRuntime.JammedDoors)
            {
                float d = Vector2.Distance(p.Position, t + new Vector2(0.5f, 0.5f));
                if (d < best)
                {
                    best = d;
                    door = t;
                    found = true;
                }
            }
            if (found)
            {
                label = "TÜR FREI";
                return true;
            }
            return false;
        }

        public void OnInteractDown()
        {
            if (Phase != GamePhase.Round || MiniGameOpen) return;
            if (!TryGetInteraction(out _, out StationDef station, out Vector2Int door)) return;

            if (station != null)
            {
                OpenMiniGame(station);
                return;
            }

            _unjamActive = true;
            _unjamTarget = door;
            _unjamProgress = 0f;
        }

        public void OnInteractUp() => CancelUnjam();

        private void CancelUnjam()
        {
            _unjamActive = false;
            _unjamProgress = 0f;
        }

        private void TickUnjam(float dt, bool moved)
        {
            if (!_unjamActive) return;

            PlayerState p = State.Local;
            float d = Vector2.Distance(p.Position, _unjamTarget + new Vector2(0.5f, 0.5f));
            if (moved || d > CCConfig.InteractRange * 1.6f || !MapRuntime.IsDoorJammed(_unjamTarget))
            {
                CancelUnjam();
                return;
            }

            _unjamProgress += dt;
            if (_unjamProgress >= CCConfig.DoorUnjamSeconds)
            {
                MapRuntime.UnjamDoor(_unjamTarget);
                Audio.Play(Sfx.Confirm);
                UI.Hud.Toast("Tür wieder frei");
                CancelUnjam();
            }
        }

        private void OpenMiniGame(StationDef station)
        {
            PlayerState p = State.Local;
            p.BusyStationId = station.Id;
            UI.Hud.SetControlsActive(false);

            _openMiniGame = MiniGame.Open(UI.OverlayLayer, station, Audio, success =>
            {
                _openMiniGame = null;
                p.BusyStationId = null;
                UI.Hud.SetControlsActive(true);
                if (!success) return;

                Tasks.CompleteTask(p, station.Id);
                UI.Hud.Toast(station.DisplayName + " erledigt");

                // Brewing coffee is also how you get your legs back.
                if (station.Icon == TaskIcon.Coffee)
                {
                    p.Energy = Mathf.Min(CCConfig.EnergyMax, p.Energy + CCConfig.EnergyPerCoffee);
                    p.Winded = false;
                    UI.Hud.Toast("Koffein! Energie aufgefüllt");
                }
            });
        }

        public void TriggerSabotage(SabotageKind kind)
        {
            if (Phase != GamePhase.Round) return;
            PlayerState p = State.Local;
            if (p == null || !p.IsSaboteur) return;
            if (!Sabotage.Trigger(kind, p))
                UI.Hud.Toast("Geht gerade nicht");
        }

        private void OnSabotageFired(SabotageDef def, PlayerState by, string flavour)
        {
            Cam.Shake(def.IsTampering ? 0.15f : 0.4f, 0.3f);

            if (by.IsLocal) UI.Hud.Toast(flavour);
            else if (def.Kind == SabotageKind.LightsOut) UI.Hud.Toast("Das Licht geht aus!");
            else if (def.Kind == SabotageKind.PrinterFrenzy) UI.Hud.Toast("Irgendwo dreht ein Drucker durch");
            else if (def.Kind == SabotageKind.JamDoors) UI.Hud.Toast("Türen klemmen");
            else if (def.Kind == SabotageKind.MoveObject) UI.Hud.Toast("Da hat jemand Möbel gerückt");
            else if (def.Kind == SabotageKind.ElevatorHijack) UI.Hud.Toast("Der Aufzug spinnt");
        }

        // ------------------------------------------------------------ crate visuals

        private void BuildCrateViews()
        {
            _crateViews.Clear();
            Transform host = World.transform;
            for (int i = 0; i < Carry.Items.Count; i++)
            {
                CarryItem it = Carry.Items[i];
                Transform t = PropLibrary.CardboardBox(host, new Vector3(it.Position.x, 0f, it.Position.y), 25f, 1.05f);
                _crateViews[it.Id] = t;
            }

            // Marker for where deliveries belong.
            Transform zone = PropKit.Group(host, "DropZone", new Vector3(Carry.DropZone.x, 0f, Carry.DropZone.y));
            PropLibrary.SelectionRing(zone, CCConfig.DropZoneRadius * 2f, Pal3D.Green);
        }

        private void SyncCrateViews()
        {
            for (int i = 0; i < Carry.Items.Count; i++)
            {
                CarryItem it = Carry.Items[i];
                if (!_crateViews.TryGetValue(it.Id, out Transform view) || view == null) continue;

                if (it.CarriedBy >= 0)
                {
                    // Ride along at chest height in front of whoever picked it up.
                    PlayerState holder = State.PlayerById(it.CarriedBy);
                    if (holder == null) continue;
                    var fwd = new Vector3(holder.Facing.x, 0f, holder.Facing.y).normalized;
                    view.position = new Vector3(holder.Position.x, 0.95f, holder.Position.y) + fwd * 0.45f;
                    view.rotation = Quaternion.LookRotation(fwd, Vector3.up);
                }
                else
                {
                    view.position = new Vector3(it.Position.x, 0f, it.Position.y);
                }
            }
        }

        // ------------------------------------------------------------ end of round

        private void EndRound(RoundEndReason reason)
        {
            State.EndReason = reason;
            CancelUnjam();

            if (_openMiniGame != null)
            {
                _openMiniGame.Close(false);
                _openMiniGame = null;
            }

            Clips = Evidence.BuildClips(CCConfig.MaxClips);

            Phase = GamePhase.Evidence;
            UI.Show(GamePhase.Evidence);
            UI.EvidenceScreen.Present(Clips);
            Audio.Play(Sfx.Alarm);
        }

        public void BeginVoting()
        {
            Votes = new VoteSystem();
            Votes.Begin(State, Evidence.BuildSuspicion(Clips));

            Phase = GamePhase.Voting;
            _phaseTimer = CCConfig.VotingSeconds;
            _botVotesCast = false;
            UI.Show(GamePhase.Voting);
            UI.Voting.Present();
        }

        private void TickVoting(float dt)
        {
            _phaseTimer -= dt;

            if (!_botVotesCast && (Votes.LocalVoteCast || _phaseTimer <= 1.5f))
            {
                Votes.CastBotVotes();
                _botVotesCast = true;
            }

            UI.Voting.Refresh(_phaseTimer);

            if (_phaseTimer <= 0f ||
                (Votes.LocalVoteCast && _botVotesCast && _phaseTimer <= CCConfig.VotingSeconds - 2.5f))
                ResolveVote();
        }

        public void SubmitVote(int targetId)
        {
            if (Phase != GamePhase.Voting || Votes.LocalVoteCast) return;
            Votes.CastLocalVote(targetId);
            Audio.Play(Sfx.Vote);
            UI.Voting.Refresh(_phaseTimer);
        }

        private void ResolveVote()
        {
            if (Phase != GamePhase.Voting) return;

            Votes.CastBotVotes();
            int ejected = Votes.Tally(out bool tied);
            State.EjectedPlayerId = ejected;
            State.VoteWasTied = tied;

            PlayerState saboteur = State.Saboteur;
            bool caught = saboteur != null && ejected == saboteur.Id;
            bool tasksDone = State.EndReason == RoundEndReason.TasksCompleted;

            State.Winner = (caught || tasksDone) ? Role.Crew : Role.Saboteur;

            Phase = GamePhase.Result;
            UI.Show(GamePhase.Result);
            UI.Result.Present();

            bool localWon = State.Local != null && State.Local.Role == State.Winner;
            Audio.Play(localWon ? Sfx.Win : Sfx.Lose);
        }

        // ------------------------------------------------------------ teardown

        public void PlayAgain()
        {
            ClearWorld();
            OpenLobby();
        }

        public void ReturnToMenu()
        {
            Transport.Leave();
            GoToTitle();
        }

        private void ClearWorld()
        {
            if (_openMiniGame != null)
            {
                _openMiniGame.Close(false);
                _openMiniGame = null;
            }
            _crateViews.Clear();
            if (World != null) World.Clear();
            State = null;
        }

        public void ApplyAudioSettings()
        {
            Audio.Muted = Settings.Muted;
            Audio.Volume = Settings.Volume;
            Settings.Save();
        }
    }
}
