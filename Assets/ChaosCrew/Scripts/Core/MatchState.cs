using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    /// <summary>One participant. Bots and the local human share this type on purpose:
    /// the simulation never needs to know which is which, only the input source does.</summary>
    public sealed class PlayerState
    {
        public int Id;
        public string DisplayName;
        public int ColorIndex;
        public bool IsLocal;
        public bool IsBot;
        public Role Role;

        public Vector2 Position;
        public Vector2 Facing = Vector2.down;
        /// <summary>Current planar speed, used for the walk bob animation.</summary>
        public float Speed;

        public float StunTimer;
        /// <summary>Multiplier applied by clutter, puddles, etc. Recomputed every frame.</summary>
        public float SpeedMultiplier = 1f;
        /// <summary>Residual slide velocity from stepping in water.</summary>
        public Vector2 SlideVelocity;

        public readonly List<string> TaskIds = new List<string>();
        public readonly HashSet<string> CompletedTaskIds = new HashSet<string>();

        /// <summary>Which station the player is currently busy with, if any.</summary>
        public string BusyStationId;
        public float BusyTimer;

        /// <summary>Stamina for sprinting. Coffee refills it.</summary>
        public float Energy = CCConfig.EnergyMax;
        public bool SprintHeld;
        /// <summary>True while actually running, i.e. holding sprint with energy to spend.</summary>
        public bool Sprinting;
        /// <summary>Set once energy bottoms out, cleared when it recovers enough to run again.</summary>
        public bool Winded;

        /// <summary>Id of the crate in this player's hands, or null.</summary>
        public string CarriedId;
        public bool IsCarrying => !string.IsNullOrEmpty(CarriedId);

        public Color Color => Palette.Crew[ColorIndex % Palette.Crew.Length];
        public bool IsStunned => StunTimer > 0f;
        public bool IsSaboteur => Role == Role.Saboteur;

        public int RemainingTaskCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < TaskIds.Count; i++)
                    if (!CompletedTaskIds.Contains(TaskIds[i])) n++;
                return n;
            }
        }
    }

    /// <summary>A task handed to one crew member. Saboteurs get a matching fake list.</summary>
    public sealed class TaskInstance
    {
        public string StationId;
        public int OwnerId;
        public bool Completed;
    }

    /// <summary>All mutable state for one round. Cleared and rebuilt per match.</summary>
    public sealed class MatchState
    {
        public int Seed;
        public DeterministicRng Rng;

        public MapDefinition Map;
        public MapRuntime MapRuntime;

        public readonly List<PlayerState> Players = new List<PlayerState>();
        public readonly List<TaskInstance> Tasks = new List<TaskInstance>();

        public int LocalPlayerId;
        public float TimeLeft;
        public float Chaos;
        public bool LightsOut;
        public float LightsOutTimer;

        public RoundEndReason EndReason;
        public Role Winner;
        public int EjectedPlayerId = -1;
        public bool VoteWasTied;

        public PlayerState Local => PlayerById(LocalPlayerId);

        public PlayerState PlayerById(int id)
        {
            for (int i = 0; i < Players.Count; i++)
                if (Players[i].Id == id) return Players[i];
            return null;
        }

        public PlayerState Saboteur
        {
            get
            {
                for (int i = 0; i < Players.Count; i++)
                    if (Players[i].IsSaboteur) return Players[i];
                return null;
            }
        }

        public int TotalTaskCount => Tasks.Count;

        public int CompletedTaskCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Tasks.Count; i++)
                    if (Tasks[i].Completed) n++;
                return n;
            }
        }

        public float TaskProgress01 => TotalTaskCount == 0 ? 0f : CompletedTaskCount / (float)TotalTaskCount;
        public float Chaos01 => Mathf.Clamp01(Chaos / CCConfig.ChaosMax);

        public TaskInstance FindTask(int ownerId, string stationId)
        {
            for (int i = 0; i < Tasks.Count; i++)
                if (Tasks[i].OwnerId == ownerId && Tasks[i].StationId == stationId) return Tasks[i];
            return null;
        }

        public StationDef StationById(string id)
        {
            for (int i = 0; i < Map.Stations.Count; i++)
                if (Map.Stations[i].Id == id) return Map.Stations[i];
            return null;
        }

        public void AddChaos(float amount)
        {
            Chaos = Mathf.Clamp(Chaos + amount, 0f, CCConfig.ChaosMax);
        }
    }
}
