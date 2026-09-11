using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    public sealed class CarryItem
    {
        public string Id;
        public Vector2 Position;
        /// <summary>-1 when the crate is standing on the floor.</summary>
        public int CarriedBy = -1;
        public bool Delivered;
        public float Bob;
    }

    /// <summary>
    /// Crates that have to be walked from the storage bay to the office drop zone. This is
    /// the mechanic behind the carry button, and it is the one job the saboteur can quietly
    /// undo: pick a crate up, wander off, and leave it somewhere useless.
    /// </summary>
    public sealed class CarrySystem
    {
        public readonly List<CarryItem> Items = new List<CarryItem>();
        public Vector2 DropZone { get; private set; }
        public int DeliveredCount { get; private set; }
        public int TotalCount => Items.Count;

        private MatchState _state;
        private MapRuntime _map;
        private EvidenceRecorder _evidence;
        private SfxSynth _sfx;

        public System.Action<CarryItem, PlayerState> OnPickedUp;
        public System.Action<CarryItem, PlayerState> OnDropped;
        public System.Action<CarryItem, PlayerState> OnDelivered;

        public void Bind(MatchState state, MapRuntime map, EvidenceRecorder evidence, SfxSynth sfx)
        {
            _state = state;
            _map = map;
            _evidence = evidence;
            _sfx = sfx;

            Items.Clear();
            DeliveredCount = 0;

            RoomDef storage = state.Map.RoomById("lager");
            DropZone = new Vector2(5.5f, 11.0f); // open-plan office, beside the printer

            for (int i = 0; i < CCConfig.CrateCount; i++)
            {
                Vector2 p = storage != null
                    ? _map.RandomWalkablePoint(state.Rng, storage, CCConfig.PlayerRadius)
                    : new Vector2(18.5f, 4.5f);
                Items.Add(new CarryItem { Id = "crate_" + i, Position = p, Bob = state.Rng.Range(0f, 6f) });
            }
        }

        public CarryItem ById(string id)
        {
            for (int i = 0; i < Items.Count; i++)
                if (Items[i].Id == id) return Items[i];
            return null;
        }

        public CarryItem CarriedBy(int playerId)
        {
            for (int i = 0; i < Items.Count; i++)
                if (Items[i].CarriedBy == playerId) return Items[i];
            return null;
        }

        /// <summary>Nearest crate on the floor within pickup range.</summary>
        public CarryItem NearestFree(Vector2 p)
        {
            CarryItem best = null;
            float bestD = CCConfig.CarryPickupRange;
            for (int i = 0; i < Items.Count; i++)
            {
                CarryItem it = Items[i];
                if (it.CarriedBy >= 0 || it.Delivered) continue;
                float d = Vector2.Distance(it.Position, p);
                if (d < bestD)
                {
                    bestD = d;
                    best = it;
                }
            }
            return best;
        }

        public bool TryPickUp(PlayerState player)
        {
            if (player.IsCarrying || player.IsStunned) return false;
            CarryItem item = NearestFree(player.Position);
            if (item == null) return false;

            item.CarriedBy = player.Id;
            player.CarriedId = item.Id;
            if (player.IsLocal) _sfx?.Play(Sfx.Confirm, 0.85f);
            OnPickedUp?.Invoke(item, player);
            return true;
        }

        /// <summary>Puts the crate down. Inside the drop zone that counts as a delivery.</summary>
        public bool TryDrop(PlayerState player)
        {
            CarryItem item = CarriedBy(player.Id);
            if (item == null) return false;

            item.CarriedBy = -1;
            player.CarriedId = null;
            item.Position = player.Position;

            bool inZone = Vector2.Distance(player.Position, DropZone) <= CCConfig.DropZoneRadius;
            if (inZone && !player.IsSaboteur)
            {
                item.Delivered = true;
                item.Position = DropZone + new Vector2(
                    Mathf.Cos(DeliveredCount * 2.1f) * 0.5f, Mathf.Sin(DeliveredCount * 2.1f) * 0.5f);
                DeliveredCount++;
                _state.AddChaos(-CCConfig.ChaosPerTaskDone);
                _evidence?.LogEvent(EvidenceEventKind.TaskCompleted, player.Id, player.Position,
                    player.DisplayName + " liefert eine Kiste ab");
                if (player.IsLocal) _sfx?.Play(Sfx.TaskDone);
                OnDelivered?.Invoke(item, player);
                return true;
            }

            // Dumping a crate far from where it belongs is exactly the saboteur's trick.
            if (player.IsSaboteur)
            {
                RoomDef room = _state.Map.RoomAt(player.Position);
                bool hidden = room != null && !room.HasCamera;
                _state.AddChaos(hidden ? 3f : 1.5f);
                _evidence?.LogEvent(EvidenceEventKind.HazardPlaced, player.Id, player.Position,
                    player.DisplayName + " lässt eine Kiste stehen");
            }

            if (player.IsLocal) _sfx?.Play(Sfx.Tap, 0.7f);
            OnDropped?.Invoke(item, player);
            return true;
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                CarryItem it = Items[i];
                if (it.CarriedBy < 0) continue;
                PlayerState p = _state.PlayerById(it.CarriedBy);
                if (p == null)
                {
                    it.CarriedBy = -1;
                    continue;
                }
                it.Position = p.Position;
            }
        }

        public bool AllDelivered => DeliveredCount >= Items.Count && Items.Count > 0;
    }
}
