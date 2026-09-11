using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    public enum TileKind
    {
        Void,
        Floor,
        Wall,
        Door
    }

    /// <summary>A named area of the map. Rooms drive labels, floor tint and camera coverage.</summary>
    public sealed class RoomDef
    {
        public string Id;
        public string DisplayName;
        /// <summary>Inclusive tile bounds.</summary>
        public RectInt Bounds;
        public Color FloorColor;
        /// <summary>Rooms without a camera are blind spots the saboteur can exploit.</summary>
        public bool HasCamera;

        public Vector2 Center => new Vector2(Bounds.x + Bounds.width * 0.5f, Bounds.y + Bounds.height * 0.5f);

        public bool Contains(Vector2 p) =>
            p.x >= Bounds.xMin && p.x < Bounds.xMax && p.y >= Bounds.yMin && p.y < Bounds.yMax;
    }

    /// <summary>Authoring data for one interactable task station.</summary>
    public sealed class StationDef
    {
        public string Id;
        public string DisplayName;
        public string RoomId;
        public Vector2 Position;
        public MiniGameKind MiniGame;
        /// <summary>Pictogram used in the task list and on the floor marker.</summary>
        public TaskIcon Icon;
    }

    public sealed class DoorDef
    {
        public Vector2Int Tile;
        public bool Horizontal;
    }

    /// <summary>
    /// A complete, immutable map description. Built from an ASCII layout plus room/station
    /// metadata, so adding a second map means adding one more builder class.
    /// </summary>
    public sealed class MapDefinition
    {
        public string DisplayName;
        public int Width;
        public int Height;
        public TileKind[] Tiles;
        public List<RoomDef> Rooms = new List<RoomDef>();
        public List<StationDef> Stations = new List<StationDef>();
        public List<DoorDef> Doors = new List<DoorDef>();
        public List<Vector2> SpawnPoints = new List<Vector2>();
        /// <summary>Elevator pad; the hijack sabotage teleports whoever stands on it.</summary>
        public Vector2 ElevatorPad;

        public TileKind At(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return TileKind.Void;
            return Tiles[y * Width + x];
        }

        public RoomDef RoomAt(Vector2 p)
        {
            for (int i = 0; i < Rooms.Count; i++)
                if (Rooms[i].Contains(p)) return Rooms[i];
            return null;
        }

        public RoomDef RoomById(string id)
        {
            for (int i = 0; i < Rooms.Count; i++)
                if (Rooms[i].Id == id) return Rooms[i];
            return null;
        }

        /// <summary>
        /// Parses rows given top-to-bottom (as they read in source) into a bottom-up tile grid.
        /// '#' wall, '.' floor, 'D' door, anything else void.
        /// </summary>
        public void ParseLayout(string[] rowsTopDown)
        {
            Height = rowsTopDown.Length;
            Width = 0;
            for (int i = 0; i < rowsTopDown.Length; i++)
                Width = Mathf.Max(Width, rowsTopDown[i].Length);

            Tiles = new TileKind[Width * Height];
            Doors.Clear();

            for (int row = 0; row < Height; row++)
            {
                string line = rowsTopDown[row];
                int y = Height - 1 - row;
                for (int x = 0; x < Width; x++)
                {
                    char c = x < line.Length ? line[x] : ' ';
                    TileKind kind;
                    switch (c)
                    {
                        case '#': kind = TileKind.Wall; break;
                        case '.': kind = TileKind.Floor; break;
                        case 'D': kind = TileKind.Door; break;
                        default: kind = TileKind.Void; break;
                    }
                    Tiles[y * Width + x] = kind;
                }
            }

            // Doors are recorded after the grid exists so orientation can look at neighbours.
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (At(x, y) != TileKind.Door) continue;
                    bool horizontal = At(x - 1, y) == TileKind.Wall && At(x + 1, y) == TileKind.Wall;
                    Doors.Add(new DoorDef { Tile = new Vector2Int(x, y), Horizontal = horizontal });
                }
            }
        }
    }
}
