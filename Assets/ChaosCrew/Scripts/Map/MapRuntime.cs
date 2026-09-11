using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Live state and queries for a map: door jamming, circle-vs-tile collision, A* paths
    /// for bots and line-of-sight checks. Deliberately free of Unity physics so a round is
    /// fully deterministic and cheap on mobile.
    /// </summary>
    public sealed class MapRuntime
    {
        public readonly MapDefinition Def;

        private readonly Dictionary<Vector2Int, float> _jammedDoors = new Dictionary<Vector2Int, float>();
        private readonly List<Vector2Int> _expired = new List<Vector2Int>();

        // A* scratch buffers, reused to avoid per-call allocation.
        private readonly Dictionary<int, int> _cameFrom = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _gScore = new Dictionary<int, float>();
        private readonly List<int> _open = new List<int>();
        private readonly HashSet<int> _closed = new HashSet<int>();

        public MapRuntime(MapDefinition def)
        {
            Def = def;
        }

        // ---------------------------------------------------------------- doors

        public bool IsDoorJammed(Vector2Int tile) => _jammedDoors.ContainsKey(tile);

        public float DoorJamRemaining(Vector2Int tile) =>
            _jammedDoors.TryGetValue(tile, out float t) ? t : 0f;

        public IEnumerable<Vector2Int> JammedDoors => _jammedDoors.Keys;

        public void JamDoor(Vector2Int tile, float seconds)
        {
            _jammedDoors.TryGetValue(tile, out float existing);
            _jammedDoors[tile] = Mathf.Max(existing, seconds);
        }

        public void UnjamDoor(Vector2Int tile) => _jammedDoors.Remove(tile);

        /// <summary>
        /// Jams the <paramref name="count"/> doors closest to a point. Doorways someone is
        /// standing in are skipped: a door cannot slam shut through a person, and letting it
        /// would leave that player embedded in solid geometry.
        /// </summary>
        public List<Vector2Int> JamDoorsNear(Vector2 point, int count, float seconds, List<Vector2> occupied)
        {
            var sorted = new List<DoorDef>(Def.Doors);
            sorted.Sort((a, b) =>
            {
                float da = ((Vector2)a.Tile - point).sqrMagnitude;
                float db = ((Vector2)b.Tile - point).sqrMagnitude;
                return da.CompareTo(db);
            });

            var result = new List<Vector2Int>();
            for (int i = 0; i < sorted.Count && result.Count < count; i++)
            {
                Vector2Int tile = sorted[i].Tile;
                if (IsOccupied(tile, occupied)) continue;
                JamDoor(tile, seconds);
                result.Add(tile);
            }
            return result;
        }

        private static bool IsOccupied(Vector2Int tile, List<Vector2> occupied)
        {
            if (occupied == null) return false;
            Vector2 centre = tile + new Vector2(0.5f, 0.5f);
            for (int i = 0; i < occupied.Count; i++)
            {
                // Half a tile plus the body radius: enough to cover anyone overlapping it.
                if (Vector2.Distance(occupied[i], centre) < 0.5f + CCConfig.PlayerRadius + 0.05f) return true;
            }
            return false;
        }

        public void Tick(float dt)
        {
            if (_jammedDoors.Count == 0) return;
            _expired.Clear();
            var keys = new List<Vector2Int>(_jammedDoors.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                float t = _jammedDoors[keys[i]] - dt;
                if (t <= 0f) _expired.Add(keys[i]);
                else _jammedDoors[keys[i]] = t;
            }
            for (int i = 0; i < _expired.Count; i++) _jammedDoors.Remove(_expired[i]);
        }

        public void ResetDoors() => _jammedDoors.Clear();

        // ---------------------------------------------------------------- geometry

        public bool IsSolid(int x, int y)
        {
            TileKind k = Def.At(x, y);
            if (k == TileKind.Wall || k == TileKind.Void) return true;
            if (k == TileKind.Door) return IsDoorJammed(new Vector2Int(x, y));
            return false;
        }

        public bool IsWalkableTile(int x, int y)
        {
            TileKind k = Def.At(x, y);
            return k == TileKind.Floor || k == TileKind.Door;
        }

        /// <summary>True when a circle of the given radius fits at <paramref name="p"/>.</summary>
        public bool IsCircleFree(Vector2 p, float radius)
        {
            int x0 = Mathf.FloorToInt(p.x - radius);
            int x1 = Mathf.FloorToInt(p.x + radius);
            int y0 = Mathf.FloorToInt(p.y - radius);
            int y1 = Mathf.FloorToInt(p.y + radius);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!IsSolid(x, y)) continue;
                    if (CircleOverlapsTile(p, radius, x, y)) return false;
                }
            }
            return true;
        }

        private static bool CircleOverlapsTile(Vector2 p, float radius, int tx, int ty)
        {
            float cx = Mathf.Clamp(p.x, tx, tx + 1f);
            float cy = Mathf.Clamp(p.y, ty, ty + 1f);
            float dx = p.x - cx;
            float dy = p.y - cy;
            return dx * dx + dy * dy < radius * radius;
        }

        /// <summary>
        /// Moves a circle by <paramref name="delta"/>, resolving each axis separately so the
        /// player slides along walls instead of sticking to them.
        /// </summary>
        public Vector2 MoveWithCollision(Vector2 from, Vector2 delta, float radius)
        {
            Vector2 pos = from;

            Vector2 tryX = new Vector2(pos.x + delta.x, pos.y);
            if (IsCircleFree(tryX, radius)) pos = tryX;

            Vector2 tryY = new Vector2(pos.x, pos.y + delta.y);
            if (IsCircleFree(tryY, radius)) pos = tryY;

            // Safety net: if we somehow ended up inside geometry, nudge back out.
            if (!IsCircleFree(pos, radius)) pos = PushOut(pos, radius);
            return pos;
        }

        private Vector2 PushOut(Vector2 p, float radius)
        {
            for (int ring = 1; ring <= 6; ring++)
            {
                float step = ring * 0.15f;
                for (int a = 0; a < 12; a++)
                {
                    float ang = a * Mathf.PI * 2f / 12f;
                    Vector2 c = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * step;
                    if (IsCircleFree(c, radius)) return c;
                }
            }
            return p;
        }

        /// <summary>Unobstructed straight line for a circle of the given radius.</summary>
        public bool HasClearPath(Vector2 a, Vector2 b, float radius)
        {
            float dist = Vector2.Distance(a, b);
            int steps = Mathf.Max(2, Mathf.CeilToInt(dist / 0.25f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                if (!IsCircleFree(p, radius)) return false;
            }
            return true;
        }

        /// <summary>Sight line ignoring radius: used for "can that crewmate see me?" checks.</summary>
        public bool HasLineOfSight(Vector2 a, Vector2 b)
        {
            float dist = Vector2.Distance(a, b);
            int steps = Mathf.Max(2, Mathf.CeilToInt(dist / 0.3f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                TileKind k = Def.At(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
                if (k == TileKind.Wall || k == TileKind.Void) return false;
            }
            return true;
        }

        public Vector2 RandomWalkablePoint(DeterministicRng rng, RoomDef room, float radius)
        {
            for (int attempt = 0; attempt < 200; attempt++)
            {
                float x, y;
                if (room != null)
                {
                    x = rng.Range(room.Bounds.xMin, room.Bounds.xMax) + 0.5f;
                    y = rng.Range(room.Bounds.yMin, room.Bounds.yMax) + 0.5f;
                }
                else
                {
                    x = rng.Range(1, Def.Width - 1) + 0.5f;
                    y = rng.Range(1, Def.Height - 1) + 0.5f;
                }
                var p = new Vector2(x, y);
                if (IsCircleFree(p, radius)) return p;
            }
            return room != null ? room.Center : new Vector2(Def.Width * 0.5f, Def.Height * 0.5f);
        }

        // ---------------------------------------------------------------- pathfinding

        private int Index(int x, int y) => y * Def.Width + x;

        /// <summary>
        /// A* over walkable tiles, then a string-pulling pass so bots walk in natural
        /// diagonals instead of hugging the tile grid. Returns world-space waypoints.
        /// </summary>
        public List<Vector2> FindPath(Vector2 from, Vector2 to, float radius)
        {
            var result = new List<Vector2>();

            var start = new Vector2Int(Mathf.FloorToInt(from.x), Mathf.FloorToInt(from.y));
            var goal = new Vector2Int(Mathf.FloorToInt(to.x), Mathf.FloorToInt(to.y));

            if (!IsWalkableTile(goal.x, goal.y))
            {
                if (!TryFindNearbyWalkable(goal, out goal)) return result;
            }
            if (!IsWalkableTile(start.x, start.y))
            {
                if (!TryFindNearbyWalkable(start, out start)) return result;
            }

            if (start == goal)
            {
                result.Add(to);
                return result;
            }

            _cameFrom.Clear();
            _gScore.Clear();
            _open.Clear();
            _closed.Clear();

            int startIdx = Index(start.x, start.y);
            int goalIdx = Index(goal.x, goal.y);
            _gScore[startIdx] = 0f;
            _open.Add(startIdx);

            bool found = false;
            int guard = 0;
            while (_open.Count > 0 && guard++ < 20000)
            {
                // Linear scan for the best node: the grid is tiny, a heap would be overkill.
                int best = 0;
                float bestF = float.MaxValue;
                for (int i = 0; i < _open.Count; i++)
                {
                    int idx = _open[i];
                    float f = _gScore[idx] + Heuristic(idx, goal);
                    if (f < bestF)
                    {
                        bestF = f;
                        best = i;
                    }
                }

                int current = _open[best];
                if (current == goalIdx)
                {
                    found = true;
                    break;
                }

                _open.RemoveAt(best);
                _closed.Add(current);

                int cx = current % Def.Width;
                int cy = current / Def.Width;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (!IsWalkableTile(nx, ny)) continue;
                        if (IsSolid(nx, ny)) continue;

                        // No corner cutting: both orthogonal neighbours must be open.
                        if (dx != 0 && dy != 0)
                        {
                            if (IsSolid(cx + dx, cy) || IsSolid(cx, cy + dy)) continue;
                        }

                        int nIdx = Index(nx, ny);
                        if (_closed.Contains(nIdx)) continue;

                        float step = (dx != 0 && dy != 0) ? 1.41421f : 1f;
                        float tentative = _gScore[current] + step;
                        if (_gScore.TryGetValue(nIdx, out float known) && tentative >= known) continue;

                        _gScore[nIdx] = tentative;
                        _cameFrom[nIdx] = current;
                        if (!_open.Contains(nIdx)) _open.Add(nIdx);
                    }
                }
            }

            if (!found) return result;

            // Rebuild, then smooth.
            var tiles = new List<Vector2>();
            int node = goalIdx;
            while (node != startIdx)
            {
                tiles.Add(new Vector2(node % Def.Width + 0.5f, node / Def.Width + 0.5f));
                if (!_cameFrom.TryGetValue(node, out node)) break;
            }
            tiles.Reverse();
            if (tiles.Count > 0) tiles[tiles.Count - 1] = to;

            Vector2 cursor = from;
            int i2 = 0;
            while (i2 < tiles.Count)
            {
                int farthest = i2;
                for (int j = tiles.Count - 1; j >= i2; j--)
                {
                    if (HasClearPath(cursor, tiles[j], radius))
                    {
                        farthest = j;
                        break;
                    }
                }
                result.Add(tiles[farthest]);
                cursor = tiles[farthest];
                i2 = farthest + 1;
            }
            return result;
        }

        private float Heuristic(int idx, Vector2Int goal)
        {
            int x = idx % Def.Width;
            int y = idx / Def.Width;
            float dx = Mathf.Abs(x - goal.x);
            float dy = Mathf.Abs(y - goal.y);
            return Mathf.Max(dx, dy) + 0.41421f * Mathf.Min(dx, dy);
        }

        private bool TryFindNearbyWalkable(Vector2Int origin, out Vector2Int found)
        {
            for (int r = 0; r <= 6; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        int nx = origin.x + dx;
                        int ny = origin.y + dy;
                        if (IsWalkableTile(nx, ny) && !IsSolid(nx, ny))
                        {
                            found = new Vector2Int(nx, ny);
                            return true;
                        }
                    }
                }
            }
            found = origin;
            return false;
        }
    }
}
