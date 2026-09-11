using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Drives the three non-local seats. Crew bots work through their task list; the
    /// saboteur bot fakes tasks and waits for privacy before pulling a prank. This is the
    /// stand-in for remote players, so it only ever uses inputs a real client would have.
    /// </summary>
    public sealed class BotBrain
    {
        private enum BotMode
        {
            Idle,
            Walking,
            Working,
            Unjamming,
            Loitering
        }

        private sealed class Bot
        {
            public PlayerState Player;
            public BotMode Mode;
            public List<Vector2> Path = new List<Vector2>();
            public Vector2 Goal;
            public string GoalStationId;
            public float WorkTimer;
            public float RepathTimer;
            public float LoiterTimer;
            public Vector2 LastPos;
            public float StuckTimer;
            public Vector2Int JamTarget;
            public float PatienceTimer;
            /// <summary>Per-bot pace. Bots stand in for human players, so they should not
            /// play optimally — some dawdle, some hustle.</summary>
            public float Diligence = 1f;
        }

        private readonly List<Bot> _bots = new List<Bot>();

        private MatchState _state;
        private MapRuntime _map;
        private TaskSystem _tasks;
        private SabotageSystem _sabotage;
        private EvidenceRecorder _evidence;

        public void Bind(MatchState state, MapRuntime map, TaskSystem tasks, SabotageSystem sabotage,
            EvidenceRecorder evidence)
        {
            _state = state;
            _map = map;
            _tasks = tasks;
            _sabotage = sabotage;
            _evidence = evidence;

            _bots.Clear();
            for (int i = 0; i < state.Players.Count; i++)
            {
                if (!state.Players[i].IsBot) continue;
                _bots.Add(new Bot
                {
                    Player = state.Players[i],
                    Mode = BotMode.Idle,
                    LastPos = state.Players[i].Position,
                    Diligence = state.Rng.Range(0.8f, 1.45f)
                });
            }
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < _bots.Count; i++) TickBot(_bots[i], dt);
        }

        private void TickBot(Bot bot, float dt)
        {
            PlayerState p = bot.Player;
            if (p.IsStunned)
            {
                bot.Path.Clear();
                bot.Mode = BotMode.Idle;
                return;
            }

            if (p.IsSaboteur) ConsiderSabotage(bot, dt);

            switch (bot.Mode)
            {
                case BotMode.Idle:
                    ChooseGoal(bot);
                    break;

                case BotMode.Walking:
                    Walk(bot, dt);
                    break;

                case BotMode.Working:
                    bot.WorkTimer -= dt;
                    if (bot.WorkTimer <= 0f)
                    {
                        if (!string.IsNullOrEmpty(bot.GoalStationId))
                            _tasks.CompleteTask(p, bot.GoalStationId);
                        bot.GoalStationId = null;
                        // A short breather before heading off, the way a person would.
                        bot.LoiterTimer = _state.Rng.Range(0.9f, 3.2f);
                        bot.Mode = BotMode.Loitering;
                    }
                    break;

                case BotMode.Unjamming:
                    bot.WorkTimer -= dt;
                    if (Vector2.Distance(p.Position, bot.JamTarget + new Vector2(0.5f, 0.5f)) > CCConfig.InteractRange * 1.6f)
                    {
                        bot.Mode = BotMode.Idle;
                    }
                    else if (bot.WorkTimer <= 0f)
                    {
                        _map.UnjamDoor(bot.JamTarget);
                        bot.Mode = BotMode.Idle;
                    }
                    break;

                case BotMode.Loitering:
                    bot.LoiterTimer -= dt;
                    if (bot.LoiterTimer <= 0f) bot.Mode = BotMode.Idle;
                    break;
            }
        }

        // ------------------------------------------------------------ goal selection

        private void ChooseGoal(Bot bot)
        {
            PlayerState p = bot.Player;
            string next = NextTaskId(p);

            if (next != null)
            {
                bot.GoalStationId = next;
                bot.Goal = _tasks.PositionOf(next);
            }
            else
            {
                // Out of tasks: drift around so the round still looks alive.
                RoomDef room = _state.Map.Rooms[_state.Rng.Range(0, _state.Map.Rooms.Count)];
                bot.GoalStationId = null;
                bot.Goal = _map.RandomWalkablePoint(_state.Rng, room, CCConfig.PlayerRadius);
            }

            Repath(bot);
            bot.PatienceTimer = 18f;
            bot.Mode = bot.Path.Count > 0 ? BotMode.Walking : BotMode.Loitering;
            if (bot.Mode == BotMode.Loitering)
            {
                bot.LoiterTimer = 0.8f;
                TryUnjamNearby(bot);
            }
        }

        private string NextTaskId(PlayerState p)
        {
            string best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < p.TaskIds.Count; i++)
            {
                string id = p.TaskIds[i];
                if (p.CompletedTaskIds.Contains(id)) continue;
                float d = Vector2.Distance(p.Position, _tasks.PositionOf(id));
                if (d < bestDist)
                {
                    bestDist = d;
                    best = id;
                }
            }
            return best;
        }

        private void Repath(Bot bot)
        {
            bot.Path = _map.FindPath(bot.Player.Position, bot.Goal, CCConfig.PlayerRadius);
            bot.RepathTimer = CCConfig.BotRepathInterval;
        }

        // ------------------------------------------------------------ locomotion

        private void Walk(Bot bot, float dt)
        {
            PlayerState p = bot.Player;

            bot.RepathTimer -= dt;
            bot.PatienceTimer -= dt;

            if (bot.Path.Count == 0)
            {
                bot.Mode = BotMode.Idle;
                return;
            }

            Vector2 target = bot.Path[0];
            Vector2 delta = target - p.Position;
            float dist = delta.magnitude;

            if (dist < 0.18f)
            {
                bot.Path.RemoveAt(0);
                if (bot.Path.Count == 0) ArriveAtGoal(bot);
                return;
            }

            Vector2 dir = delta / Mathf.Max(0.0001f, dist);
            float speed = CCConfig.WalkSpeed * p.SpeedMultiplier * 0.94f;
            Vector2 before = p.Position;
            p.Position = _map.MoveWithCollision(p.Position, dir * speed * dt, CCConfig.PlayerRadius);
            p.Facing = dir;
            p.Speed = Vector2.Distance(before, p.Position) / Mathf.Max(0.0001f, dt);

            // Wedged against geometry, or the route got jammed shut mid-walk.
            if (Vector2.Distance(before, p.Position) < 0.01f)
            {
                bot.StuckTimer += dt;
                if (bot.StuckTimer > 0.5f)
                {
                    bot.StuckTimer = 0f;
                    Repath(bot);
                    if (bot.Path.Count == 0)
                    {
                        TryUnjamNearby(bot);
                        if (bot.Mode != BotMode.Unjamming) bot.Mode = BotMode.Idle;
                    }
                }
            }
            else
            {
                bot.StuckTimer = 0f;
            }

            if (bot.RepathTimer <= 0f) Repath(bot);
            if (bot.PatienceTimer <= 0f) bot.Mode = BotMode.Idle;
        }

        private void ArriveAtGoal(Bot bot)
        {
            PlayerState p = bot.Player;
            if (!string.IsNullOrEmpty(bot.GoalStationId))
            {
                p.Speed = 0f;
                bot.WorkTimer = p.IsSaboteur ? CCConfig.BotFakeTaskSeconds : CCConfig.BotTaskSeconds;
                bot.WorkTimer *= bot.Diligence * _state.Rng.Range(0.85f, 1.2f);
                bot.Mode = BotMode.Working;
            }
            else
            {
                bot.LoiterTimer = _state.Rng.Range(0.6f, 2.2f);
                bot.Mode = BotMode.Loitering;
            }
        }

        /// <summary>Crew bots will walk over and free a stuck door rather than give up.</summary>
        private void TryUnjamNearby(Bot bot)
        {
            if (bot.Player.IsSaboteur) return;

            Vector2Int best = default;
            float bestD = 7f;
            bool found = false;
            foreach (Vector2Int door in _map.JammedDoors)
            {
                float d = Vector2.Distance(bot.Player.Position, door + new Vector2(0.5f, 0.5f));
                if (d < bestD)
                {
                    bestD = d;
                    best = door;
                    found = true;
                }
            }
            if (!found) return;

            if (bestD <= CCConfig.InteractRange * 1.5f)
            {
                bot.JamTarget = best;
                bot.WorkTimer = CCConfig.DoorUnjamSeconds;
                bot.Mode = BotMode.Unjamming;
            }
            else
            {
                bot.Goal = best + new Vector2(0.5f, 0.5f);
                bot.GoalStationId = null;
                Repath(bot);
                if (bot.Path.Count > 0)
                {
                    bot.Mode = BotMode.Walking;
                    bot.PatienceTimer = 10f;
                }
            }
        }

        // ------------------------------------------------------------ saboteur logic

        private void ConsiderSabotage(Bot bot, float dt)
        {
            PlayerState p = bot.Player;

            var ready = new List<SabotageDef>();
            for (int i = 0; i < SabotageSystem.Catalog.Length; i++)
            {
                SabotageDef def = SabotageSystem.Catalog[i];
                if (_sabotage.CanTrigger(def.Kind)) ready.Add(def);
            }
            if (ready.Count == 0) return;

            bool watched = IsWatched(p);
            RoomDef room = _state.Map.RoomAt(p.Position);
            bool blindSpot = room != null && !room.HasCamera;
            bool filmed = room != null && room.HasCamera
                          && !(_evidence != null && _evidence.IsRoomGlitched(room.Id));

            // Standing in shot with a full toolbox: go find a blind spot instead of waiting.
            if (filmed && ready.Count >= 2 && bot.Mode != BotMode.Working && _state.Rng.Chance(0.5f * dt))
            {
                RoomDef hideout = PickBlindRoom();
                if (hideout != null)
                {
                    bot.GoalStationId = null;
                    bot.Goal = _map.RandomWalkablePoint(_state.Rng, hideout, CCConfig.PlayerRadius);
                    Repath(bot);
                    if (bot.Path.Count > 0)
                    {
                        bot.Mode = BotMode.Walking;
                        bot.PatienceTimer = 20f;
                    }
                }
            }

            // A competent saboteur waits for privacy rather than performing on camera.
            float urge = 0.32f * dt;
            if (blindSpot) urge *= 2.4f;
            if (filmed) urge *= 0.35f;
            if (watched) urge *= 0.12f;

            if (!_state.Rng.Chance(urge)) return;

            // Standing in a live camera room, killing the feed comes first.
            if (filmed && _sabotage.CanTrigger(SabotageKind.CameraGlitch))
            {
                _sabotage.Trigger(SabotageKind.CameraGlitch, p);
                return;
            }

            var picks = new List<SabotageDef>();
            for (int i = 0; i < ready.Count; i++)
            {
                SabotageDef def = ready[i];
                // Glitching a room that is not being filmed would be wasted.
                if (def.Kind == SabotageKind.CameraGlitch && !filmed) continue;
                // On camera with no way to hide it, only untraceable tampering is worth it.
                if ((watched || filmed) && !def.IsTampering) continue;
                // Laying a false trail is only useful with a prank ready to pin on someone.
                if (def.Kind == SabotageKind.FalseTrail && ready.Count < 2) continue;
                picks.Add(def);
            }
            if (picks.Count == 0) return;

            SabotageDef choice = picks[_state.Rng.Range(0, picks.Count)];
            _sabotage.Trigger(choice.Kind, p);
        }

        private RoomDef PickBlindRoom()
        {
            var blind = new List<RoomDef>();
            for (int i = 0; i < _state.Map.Rooms.Count; i++)
                if (!_state.Map.Rooms[i].HasCamera) blind.Add(_state.Map.Rooms[i]);
            return blind.Count == 0 ? null : blind[_state.Rng.Range(0, blind.Count)];
        }

        /// <summary>True when any other player has a clear, close view of this one.</summary>
        private bool IsWatched(PlayerState p)
        {
            float radius = _state.LightsOut ? CCConfig.LightsOutViewRadius : 6.5f;
            for (int i = 0; i < _state.Players.Count; i++)
            {
                PlayerState other = _state.Players[i];
                if (other.Id == p.Id) continue;
                if (Vector2.Distance(other.Position, p.Position) > radius) continue;
                if (_map.HasLineOfSight(other.Position, p.Position)) return true;
            }
            return false;
        }
    }
}
