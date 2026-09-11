using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    public sealed class SabotageDef
    {
        public SabotageKind Kind;
        public string DisplayName;
        public string Description;
        public string Glyph;
        public float Cooldown;
        public float ChaosGain;
        /// <summary>Tampering actions target the evidence phase rather than the round itself.</summary>
        public bool IsTampering;
    }

    /// <summary>
    /// The saboteur's toolbox. Every entry is harmless slapstick — nothing here hurts anyone,
    /// it just wastes the crew's time and muddies the footage.
    /// </summary>
    public sealed class SabotageSystem
    {
        public static readonly SabotageDef[] Catalog =
        {
            new SabotageDef { Kind = SabotageKind.BananaPeel,     DisplayName = "Bananenschale", Glyph = "BAN", Cooldown = 10f, ChaosGain = 4f,  Description = "Legt eine Schale ab. Wer reinläuft, schlittert." },
            new SabotageDef { Kind = SabotageKind.WaterSpill,     DisplayName = "Wasser kippen", Glyph = "H2O", Cooldown = 14f, ChaosGain = 5f,  Description = "Eine Pfütze. Sehr rutschig, sehr unschuldig." },
            new SabotageDef { Kind = SabotageKind.JamDoors,       DisplayName = "Türen blockieren", Glyph = "TÜR", Cooldown = 28f, ChaosGain = 7f, Description = "Klemmt die nächsten Türen. Aufhalten dauert." },
            new SabotageDef { Kind = SabotageKind.MoveObject,     DisplayName = "Sachen verschieben", Glyph = "MOV", Cooldown = 26f, ChaosGain = 6f,  Description = "Ein Arbeitsplatz steht plötzlich woanders." },
            new SabotageDef { Kind = SabotageKind.PrinterFrenzy,  DisplayName = "Drucker-Amok", Glyph = "PRN", Cooldown = 30f, ChaosGain = 8f, Description = "Der Drucker spuckt Papier. Überall Papier." },
            new SabotageDef { Kind = SabotageKind.LightsOut,      DisplayName = "Licht aus", Glyph = "LIC", Cooldown = 38f, ChaosGain = 9f, Description = "Kurzschluss. Für alle wird es dunkel." },
            new SabotageDef { Kind = SabotageKind.ElevatorHijack, DisplayName = "Aufzug umleiten", Glyph = "LFT", Cooldown = 40f, ChaosGain = 9f, Description = "Schickt alle im Aufzug in einen anderen Raum." },
            new SabotageDef { Kind = SabotageKind.CameraGlitch,   DisplayName = "Kamera stören", Glyph = "CAM", Cooldown = 26f, ChaosGain = 2f,  IsTampering = true, Description = "Rauscht die Aufnahme in diesem Raum weg." },
            new SabotageDef { Kind = SabotageKind.FalseTrail,     DisplayName = "Falsche Spur", Glyph = "?!",  Cooldown = 34f, ChaosGain = 2f,  IsTampering = true, Description = "Die nächste Tat erscheint unter fremdem Namen." },
        };

        private readonly Dictionary<SabotageKind, float> _cooldowns = new Dictionary<SabotageKind, float>();

        private MatchState _state;
        private MapRuntime _mapRuntime;
        private HazardSystem _hazards;
        private TaskSystem _tasks;
        private EvidenceRecorder _evidence;
        private SfxSynth _sfx;

        /// <summary>Fired for UI toasts. The bool says whether the local player caused it.</summary>
        public System.Action<SabotageDef, PlayerState, string> OnSabotage;

        public void Bind(MatchState state, MapRuntime mapRuntime, HazardSystem hazards,
            TaskSystem tasks, EvidenceRecorder evidence, SfxSynth sfx)
        {
            _state = state;
            _mapRuntime = mapRuntime;
            _hazards = hazards;
            _tasks = tasks;
            _evidence = evidence;
            _sfx = sfx;

            _cooldowns.Clear();
            for (int i = 0; i < Catalog.Length; i++)
                _cooldowns[Catalog[i].Kind] = 3f; // short opening lockout
        }

        public static SabotageDef Find(SabotageKind kind)
        {
            for (int i = 0; i < Catalog.Length; i++)
                if (Catalog[i].Kind == kind) return Catalog[i];
            return null;
        }

        public float CooldownRemaining(SabotageKind kind) =>
            _cooldowns.TryGetValue(kind, out float t) ? Mathf.Max(0f, t) : 0f;

        public float CooldownFraction(SabotageKind kind)
        {
            SabotageDef def = Find(kind);
            if (def == null || def.Cooldown <= 0f) return 0f;
            return Mathf.Clamp01(CooldownRemaining(kind) / def.Cooldown);
        }

        public bool CanTrigger(SabotageKind kind) => CooldownRemaining(kind) <= 0f;

        public void Tick(float dt)
        {
            var keys = new List<SabotageKind>(_cooldowns.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                float t = _cooldowns[keys[i]];
                if (t > 0f) _cooldowns[keys[i]] = t - dt;
            }

            if (_state.LightsOut)
            {
                _state.LightsOutTimer -= dt;
                if (_state.LightsOutTimer <= 0f)
                {
                    _state.LightsOut = false;
                    _state.LightsOutTimer = 0f;
                }
            }
        }

        public bool Trigger(SabotageKind kind, PlayerState by)
        {
            if (by == null || !by.IsSaboteur) return false;
            if (!CanTrigger(kind)) return false;

            SabotageDef def = Find(kind);
            if (def == null) return false;

            string flavour = Execute(def, by);
            if (flavour == null) return false;

            _cooldowns[kind] = def.Cooldown;
            _state.AddChaos(def.ChaosGain);

            EvidenceEventKind evKind = def.IsTampering
                ? EvidenceEventKind.SabotageTriggered
                : (kind == SabotageKind.BananaPeel || kind == SabotageKind.WaterSpill
                    ? EvidenceEventKind.HazardPlaced
                    : EvidenceEventKind.SabotageTriggered);

            // Tampering with the cameras is exactly the thing cameras cannot record.
            if (kind != SabotageKind.CameraGlitch && kind != SabotageKind.FalseTrail)
                _evidence?.LogEvent(evKind, by.Id, by.Position, by.DisplayName + ": " + def.DisplayName);

            _sfx?.Play(def.IsTampering ? Sfx.Whoosh : Sfx.Sabotage);
            OnSabotage?.Invoke(def, by, flavour);
            return true;
        }

        /// <summary>Applies the effect. Returns a player-facing message, or null if it could not fire.</summary>
        private string Execute(SabotageDef def, PlayerState by)
        {
            switch (def.Kind)
            {
                case SabotageKind.BananaPeel:
                    _hazards.Spawn(HazardKind.Banana, by.Position, by.Id);
                    return "Bananenschale platziert";

                case SabotageKind.WaterSpill:
                    _hazards.Spawn(HazardKind.Puddle, by.Position, by.Id);
                    return "Wasser verschüttet";

                case SabotageKind.JamDoors:
                {
                    var occupied = new List<Vector2>();
                    for (int i = 0; i < _state.Players.Count; i++) occupied.Add(_state.Players[i].Position);

                    List<Vector2Int> jammed = _mapRuntime.JamDoorsNear(by.Position, 3, 14f, occupied);
                    if (jammed.Count == 0) return null;
                    _evidence?.LogEvent(EvidenceEventKind.DoorJammed, by.Id, by.Position,
                        by.DisplayName + " klemmt eine Tür");
                    return jammed.Count + " Türen klemmen";
                }

                case SabotageKind.MoveObject:
                {
                    StationDef moved = _tasks.MoveRandomUnfinishedStation(_state.Rng, _mapRuntime);
                    return moved == null ? null : moved.DisplayName + " steht jetzt woanders";
                }

                case SabotageKind.PrinterFrenzy:
                {
                    RoomDef office = _state.Map.RoomById("buero") ?? _state.Map.RoomAt(by.Position);
                    if (office == null) return null;
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 p = _mapRuntime.RandomWalkablePoint(_state.Rng, office, CCConfig.PlayerRadius);
                        _hazards.Spawn(HazardKind.Clutter, p, by.Id);
                    }
                    return "Der Drucker dreht durch";
                }

                case SabotageKind.LightsOut:
                    _state.LightsOut = true;
                    _state.LightsOutTimer = 18f;
                    return "Licht aus!";

                case SabotageKind.ElevatorHijack:
                {
                    RoomDef lift = _state.Map.RoomById("aufzug");
                    if (lift == null) return null;
                    int moved = 0;
                    for (int i = 0; i < _state.Players.Count; i++)
                    {
                        PlayerState p = _state.Players[i];
                        if (p.Id == by.Id || !lift.Contains(p.Position)) continue;
                        RoomDef target = PickOtherRoom(lift);
                        p.Position = _mapRuntime.RandomWalkablePoint(_state.Rng, target, CCConfig.PlayerRadius);
                        p.BusyStationId = null;
                        moved++;
                    }
                    return moved > 0 ? "Aufzug umgeleitet (" + moved + " unterwegs)" : "Aufzug ruckelt – niemand drin";
                }

                case SabotageKind.CameraGlitch:
                {
                    RoomDef room = _state.Map.RoomAt(by.Position);
                    if (room == null) return null;
                    if (!room.HasCamera) return "Hier ist gar keine Kamera";
                    _evidence?.GlitchRoom(room.Id, CCConfig.CameraGlitchSeconds);
                    return "Kamera " + room.DisplayName + " rauscht";
                }

                case SabotageKind.FalseTrail:
                    if (_evidence == null) return null;
                    _evidence.FalseTrailArmed = true;
                    return "Falsche Spur gelegt";

                default:
                    return null;
            }
        }

        private RoomDef PickOtherRoom(RoomDef exclude)
        {
            var options = new List<RoomDef>();
            for (int i = 0; i < _state.Map.Rooms.Count; i++)
                if (_state.Map.Rooms[i] != exclude) options.Add(_state.Map.Rooms[i]);
            return options[_state.Rng.Range(0, options.Count)];
        }
    }
}
