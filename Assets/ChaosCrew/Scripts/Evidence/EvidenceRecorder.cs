using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    public struct PlayerSnapshot
    {
        public int PlayerId;
        public Vector2 Position;
        public bool Busy;
    }

    public sealed class EvidenceSample
    {
        public float Time;
        public PlayerSnapshot[] Players;
    }

    public sealed class EvidenceEvent
    {
        public float Time;
        public EvidenceEventKind Kind;
        public int ActorId;
        /// <summary>Who the footage appears to show. Differs from ActorId after a false trail.</summary>
        public int AttributedId;
        public Vector2 Position;
        public string RoomId;
        public string Label;
        public bool Incriminating;
    }

    /// <summary>One reviewable snippet of security footage.</summary>
    public sealed class CameraClip
    {
        public string RoomId;
        public string RoomName;
        public string Caption;
        public float StartTime;
        public float EndTime;
        public List<EvidenceSample> Frames = new List<EvidenceSample>();
        public bool Corrupted;
        /// <summary>Identity swap injected by the false-trail sabotage; -1 when clean.</summary>
        public int SwapA = -1;
        public int SwapB = -1;
        public EvidenceEvent Anchor;

        public float Duration => Mathf.Max(0.01f, EndTime - StartTime);

        /// <summary>Maps a real player id to the id the footage appears to show.</summary>
        public int DisplayIdFor(int playerId)
        {
            if (SwapA < 0 || SwapB < 0) return playerId;
            if (playerId == SwapA) return SwapB;
            if (playerId == SwapB) return SwapA;
            return playerId;
        }
    }

    /// <summary>
    /// Records the round so the evidence phase has something to show. Positions are sampled
    /// at a low rate for every player; only rooms that actually have a camera can be turned
    /// into clips, which is what makes blind spots valuable to the saboteur.
    /// </summary>
    public sealed class EvidenceRecorder
    {
        public readonly List<EvidenceSample> Samples = new List<EvidenceSample>();
        public readonly List<EvidenceEvent> Events = new List<EvidenceEvent>();

        private MatchState _state;
        private float _elapsed;
        private float _sampleAccumulator;

        /// <summary>Rooms whose footage is currently being jammed, keyed by room id.</summary>
        private readonly Dictionary<string, float> _glitchUntil = new Dictionary<string, float>();
        private readonly List<Vector2> _glitchWindows = new List<Vector2>();
        private readonly List<string> _glitchWindowRooms = new List<string>();

        /// <summary>Armed by the false-trail sabotage; consumed by the saboteur's next event.</summary>
        public bool FalseTrailArmed;

        public float Elapsed => _elapsed;

        public void Begin(MatchState state)
        {
            _state = state;
            _elapsed = 0f;
            _sampleAccumulator = 0f;
            Samples.Clear();
            Events.Clear();
            _glitchUntil.Clear();
            _glitchWindows.Clear();
            _glitchWindowRooms.Clear();
            FalseTrailArmed = false;
        }

        public void Tick(float dt)
        {
            if (_state == null) return;
            _elapsed += dt;
            _sampleAccumulator += dt;

            float interval = 1f / CCConfig.EvidenceSampleHz;
            if (_sampleAccumulator < interval) return;
            _sampleAccumulator -= interval;

            var snap = new PlayerSnapshot[_state.Players.Count];
            for (int i = 0; i < _state.Players.Count; i++)
            {
                PlayerState p = _state.Players[i];
                snap[i] = new PlayerSnapshot
                {
                    PlayerId = p.Id,
                    Position = p.Position,
                    Busy = !string.IsNullOrEmpty(p.BusyStationId)
                };
            }
            Samples.Add(new EvidenceSample { Time = _elapsed, Players = snap });
        }

        public void GlitchRoom(string roomId, float seconds)
        {
            if (string.IsNullOrEmpty(roomId)) return;
            _glitchUntil[roomId] = _elapsed + seconds;
            _glitchWindows.Add(new Vector2(_elapsed, _elapsed + seconds));
            _glitchWindowRooms.Add(roomId);
        }

        public bool IsRoomGlitched(string roomId)
        {
            return _glitchUntil.TryGetValue(roomId, out float until) && _elapsed < until;
        }

        private bool WasRoomGlitchedAt(string roomId, float time)
        {
            for (int i = 0; i < _glitchWindows.Count; i++)
            {
                if (_glitchWindowRooms[i] != roomId) continue;
                if (time >= _glitchWindows[i].x && time <= _glitchWindows[i].y) return true;
            }
            return false;
        }

        public EvidenceEvent LogEvent(EvidenceEventKind kind, int actorId, Vector2 position, string label)
        {
            if (_state == null) return null;
            RoomDef room = _state.Map.RoomAt(position);
            bool incriminating = kind == EvidenceEventKind.SabotageTriggered
                                 || kind == EvidenceEventKind.HazardPlaced
                                 || kind == EvidenceEventKind.FakeTask
                                 || kind == EvidenceEventKind.DoorJammed;

            int attributed = actorId;
            if (incriminating && FalseTrailArmed)
            {
                attributed = PickScapegoat(actorId);
                FalseTrailArmed = false;
            }

            var ev = new EvidenceEvent
            {
                Time = _elapsed,
                Kind = kind,
                ActorId = actorId,
                AttributedId = attributed,
                Position = position,
                RoomId = room != null ? room.Id : null,
                Label = label,
                Incriminating = incriminating
            };
            Events.Add(ev);
            return ev;
        }

        private int PickScapegoat(int actorId)
        {
            var candidates = new List<int>();
            for (int i = 0; i < _state.Players.Count; i++)
                if (_state.Players[i].Id != actorId) candidates.Add(_state.Players[i].Id);
            if (candidates.Count == 0) return actorId;
            return candidates[_state.Rng.Range(0, candidates.Count)];
        }

        // ------------------------------------------------------------ clip building

        /// <summary>
        /// Picks the most interesting moments that a camera actually saw and turns them into
        /// short replayable clips. Prefers incriminating events, then spreads across rooms.
        /// </summary>
        public List<CameraClip> BuildClips(int maxClips)
        {
            var clips = new List<CameraClip>();
            if (_state == null) return clips;

            var candidates = new List<EvidenceEvent>();
            for (int i = 0; i < Events.Count; i++)
            {
                EvidenceEvent ev = Events[i];
                if (string.IsNullOrEmpty(ev.RoomId)) continue;
                RoomDef room = _state.Map.RoomById(ev.RoomId);
                if (room == null || !room.HasCamera) continue;
                candidates.Add(ev);
            }

            candidates.Sort((a, b) =>
            {
                int sa = Score(a);
                int sb = Score(b);
                if (sa != sb) return sb.CompareTo(sa);
                return a.Time.CompareTo(b.Time);
            });

            var usedRooms = new HashSet<string>();
            for (int pass = 0; pass < 2 && clips.Count < maxClips; pass++)
            {
                for (int i = 0; i < candidates.Count && clips.Count < maxClips; i++)
                {
                    EvidenceEvent ev = candidates[i];
                    // First pass keeps rooms distinct so the montage feels varied.
                    if (pass == 0 && usedRooms.Contains(ev.RoomId)) continue;
                    if (AlreadyCovered(clips, ev)) continue;
                    clips.Add(BuildClip(ev));
                    usedRooms.Add(ev.RoomId);
                }
            }

            // Nothing incriminating was caught: show a quiet room so the phase still plays.
            if (clips.Count == 0) clips.Add(BuildAmbientClip());

            clips.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            return clips;
        }

        private static int Score(EvidenceEvent ev)
        {
            switch (ev.Kind)
            {
                case EvidenceEventKind.SabotageTriggered: return 100;
                case EvidenceEventKind.HazardPlaced: return 90;
                case EvidenceEventKind.FakeTask: return 80;
                case EvidenceEventKind.DoorJammed: return 70;
                case EvidenceEventKind.PlayerSlipped: return 40;
                case EvidenceEventKind.TaskCompleted: return 10;
                default: return 0;
            }
        }

        private static bool AlreadyCovered(List<CameraClip> clips, EvidenceEvent ev)
        {
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i].RoomId != ev.RoomId) continue;
                if (ev.Time >= clips[i].StartTime && ev.Time <= clips[i].EndTime) return true;
            }
            return false;
        }

        private CameraClip BuildClip(EvidenceEvent ev)
        {
            RoomDef room = _state.Map.RoomById(ev.RoomId);
            var clip = new CameraClip
            {
                RoomId = ev.RoomId,
                RoomName = room != null ? room.DisplayName : "Unbekannt",
                StartTime = Mathf.Max(0f, ev.Time - CCConfig.ClipHalfLength),
                EndTime = ev.Time + CCConfig.ClipHalfLength,
                Anchor = ev,
                Corrupted = WasRoomGlitchedAt(ev.RoomId, ev.Time),
                Caption = ev.Label
            };

            if (ev.AttributedId != ev.ActorId)
            {
                clip.SwapA = ev.ActorId;
                clip.SwapB = ev.AttributedId;
            }

            FillFrames(clip, room);
            return clip;
        }

        /// <summary>
        /// Fallback when nothing incriminating was filmed. Picks the busiest moment in front
        /// of any camera, so the phase always plays something with people in it rather than
        /// an empty room.
        /// </summary>
        private CameraClip BuildAmbientClip()
        {
            RoomDef room = null;
            float mid = Mathf.Max(CCConfig.ClipHalfLength, _elapsed * 0.5f);
            int best = -1;

            for (int r = 0; r < _state.Map.Rooms.Count; r++)
            {
                RoomDef candidate = _state.Map.Rooms[r];
                if (!candidate.HasCamera) continue;
                if (room == null) room = candidate;

                for (int i = 0; i < Samples.Count; i++)
                {
                    int count = 0;
                    for (int p = 0; p < Samples[i].Players.Length; p++)
                        if (candidate.Contains(Samples[i].Players[p].Position)) count++;

                    if (count <= best) continue;
                    best = count;
                    room = candidate;
                    mid = Mathf.Max(CCConfig.ClipHalfLength, Samples[i].Time);
                }
            }
            var clip = new CameraClip
            {
                RoomId = room != null ? room.Id : "",
                RoomName = room != null ? room.DisplayName : "Flur",
                StartTime = mid - CCConfig.ClipHalfLength,
                EndTime = mid + CCConfig.ClipHalfLength,
                Caption = "Ruhiger Moment – keine Auffälligkeiten"
            };
            FillFrames(clip, room);
            return clip;
        }

        /// <summary>Copies the sampled positions inside the clip window, filtered to the room.</summary>
        private void FillFrames(CameraClip clip, RoomDef room)
        {
            for (int i = 0; i < Samples.Count; i++)
            {
                EvidenceSample s = Samples[i];
                if (s.Time < clip.StartTime || s.Time > clip.EndTime) continue;

                var inRoom = new List<PlayerSnapshot>();
                for (int p = 0; p < s.Players.Length; p++)
                {
                    if (room != null && !room.Contains(s.Players[p].Position)) continue;
                    inRoom.Add(s.Players[p]);
                }
                clip.Frames.Add(new EvidenceSample { Time = s.Time, Players = inRoom.ToArray() });
            }
        }

        /// <summary>Per-player suspicion derived from the footage, used by bot voters.</summary>
        public Dictionary<int, float> BuildSuspicion(List<CameraClip> clips)
        {
            var score = new Dictionary<int, float>();
            for (int i = 0; i < _state.Players.Count; i++) score[_state.Players[i].Id] = 0f;

            for (int i = 0; i < clips.Count; i++)
            {
                CameraClip clip = clips[i];
                if (clip.Corrupted || clip.Anchor == null) continue;
                if (!clip.Anchor.Incriminating) continue;

                int shown = clip.DisplayIdFor(clip.Anchor.ActorId);
                if (score.ContainsKey(shown)) score[shown] += 3f;

                // Everyone else visible at the anchor moment picks up a little heat.
                for (int f = 0; f < clip.Frames.Count; f++)
                {
                    if (Mathf.Abs(clip.Frames[f].Time - clip.Anchor.Time) > 0.5f) continue;
                    for (int p = 0; p < clip.Frames[f].Players.Length; p++)
                    {
                        int id = clip.DisplayIdFor(clip.Frames[f].Players[p].PlayerId);
                        if (id != shown && score.ContainsKey(id)) score[id] += 0.4f;
                    }
                }
            }
            return score;
        }
    }
}
