using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Deals task lists, answers "what can I interact with here?" and books completions.
    /// The saboteur is dealt a list of the same shape so their HUD is indistinguishable.
    /// </summary>
    public sealed class TaskSystem
    {
        private MatchState _state;
        private EvidenceRecorder _evidence;
        private SfxSynth _sfx;

        /// <summary>Runtime station positions, which the move-object sabotage can change.</summary>
        private readonly Dictionary<string, Vector2> _stationPositions = new Dictionary<string, Vector2>();

        public System.Action<TaskInstance> OnTaskCompleted;
        public System.Action<StationDef, Vector2> OnStationMoved;

        public void Bind(MatchState state, EvidenceRecorder evidence, SfxSynth sfx)
        {
            _state = state;
            _evidence = evidence;
            _sfx = sfx;
            _stationPositions.Clear();
            for (int i = 0; i < state.Map.Stations.Count; i++)
                _stationPositions[state.Map.Stations[i].Id] = state.Map.Stations[i].Position;
        }

        public Vector2 PositionOf(string stationId) =>
            _stationPositions.TryGetValue(stationId, out var p) ? p : Vector2.zero;

        public Vector2 PositionOf(StationDef s) => PositionOf(s.Id);

        /// <summary>Deals every player a task list. Only crew lists count toward the win.</summary>
        public void AssignTasks()
        {
            _state.Tasks.Clear();

            var pool = new List<StationDef>(_state.Map.Stations);
            _state.Rng.Shuffle(pool);

            int cursor = 0;
            for (int i = 0; i < _state.Players.Count; i++)
            {
                PlayerState p = _state.Players[i];
                p.TaskIds.Clear();
                p.CompletedTaskIds.Clear();

                for (int t = 0; t < CCConfig.TasksPerCrew; t++)
                {
                    if (pool.Count == 0) break;
                    StationDef station = pool[cursor % pool.Count];
                    cursor++;
                    p.TaskIds.Add(station.Id);

                    // Only real crew tasks enter the shared progress bar.
                    if (!p.IsSaboteur)
                        _state.Tasks.Add(new TaskInstance { StationId = station.Id, OwnerId = p.Id });
                }
            }
        }

        /// <summary>The player's own uncompleted task within reach, if any.</summary>
        public StationDef NearestActionableStation(PlayerState player, out float distance)
        {
            StationDef best = null;
            distance = float.MaxValue;

            for (int i = 0; i < player.TaskIds.Count; i++)
            {
                string id = player.TaskIds[i];
                if (player.CompletedTaskIds.Contains(id)) continue;
                StationDef s = _state.StationById(id);
                if (s == null) continue;
                float d = Vector2.Distance(player.Position, PositionOf(id));
                if (d < distance)
                {
                    distance = d;
                    best = s;
                }
            }

            return distance <= CCConfig.InteractRange ? best : null;
        }

        public bool IsTaskDone(PlayerState player, string stationId) => player.CompletedTaskIds.Contains(stationId);

        /// <summary>Books a finished mini game. Saboteurs record a fake-task event instead.</summary>
        public void CompleteTask(PlayerState player, string stationId)
        {
            if (player.CompletedTaskIds.Contains(stationId)) return;
            player.CompletedTaskIds.Add(stationId);

            StationDef station = _state.StationById(stationId);
            string label = station != null ? station.DisplayName : stationId;

            if (player.IsSaboteur)
            {
                // Pretending to work is the saboteur's cover, but a camera may notice the
                // hands moving over a machine that never actually turns on.
                _evidence?.LogEvent(EvidenceEventKind.FakeTask, player.Id, player.Position,
                    player.DisplayName + " tut nur so: " + label);
                if (player.IsLocal) _sfx?.Play(Sfx.Confirm);
                return;
            }

            TaskInstance task = _state.FindTask(player.Id, stationId);
            if (task != null) task.Completed = true;

            // Real work pushes the chaos meter back down: the two win conditions share a bar.
            _state.AddChaos(-CCConfig.ChaosPerTaskDone);

            _evidence?.LogEvent(EvidenceEventKind.TaskCompleted, player.Id, player.Position,
                player.DisplayName + " erledigt: " + label);
            if (player.IsLocal) _sfx?.Play(Sfx.TaskDone);
            OnTaskCompleted?.Invoke(task);
        }

        /// <summary>Relocates a station inside its own room (the move-object sabotage).</summary>
        public StationDef MoveRandomUnfinishedStation(DeterministicRng rng, MapRuntime mapRuntime)
        {
            var candidates = new List<StationDef>();
            for (int i = 0; i < _state.Tasks.Count; i++)
            {
                if (_state.Tasks[i].Completed) continue;
                StationDef s = _state.StationById(_state.Tasks[i].StationId);
                if (s != null && !candidates.Contains(s)) candidates.Add(s);
            }
            if (candidates.Count == 0) return null;

            StationDef target = candidates[rng.Range(0, candidates.Count)];
            RoomDef room = _state.Map.RoomById(target.RoomId);
            Vector2 old = PositionOf(target.Id);

            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector2 p = mapRuntime.RandomWalkablePoint(rng, room, CCConfig.PlayerRadius);
                if (Vector2.Distance(p, old) < 2.5f) continue;
                _stationPositions[target.Id] = p;
                OnStationMoved?.Invoke(target, p);
                return target;
            }
            return null;
        }
    }
}
