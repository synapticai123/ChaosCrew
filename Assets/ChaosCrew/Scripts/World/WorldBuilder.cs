using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Builds and drives the 3D office: floors, partition walls, furniture, characters,
    /// hazard props and lighting. Everything is generated at runtime from primitives.
    /// </summary>
    public sealed class WorldBuilder : MonoBehaviour
    {
        private const float PartitionHeight = 1.25f;
        private const float BackWallHeight = 2.45f;
        private const float PerimeterFarHeight = 3.1f;
        private const float PerimeterNearHeight = 0.4f;

        private MatchState _state;
        private MapRuntime _mapRuntime;
        private TaskSystem _tasks;
        private HazardSystem _hazards;

        private Transform _root;
        private Transform _staticRoot;
        private Transform _dynamicRoot;
        private Light _sun;
        private Light _playerLamp;

        private readonly List<CharacterRig> _rigs = new List<CharacterRig>();
        private readonly Dictionary<string, Transform> _markers = new Dictionary<string, Transform>();
        private readonly Dictionary<string, Transform> _markerIcons = new Dictionary<string, Transform>();
        private readonly Dictionary<Hazard, Transform> _hazardViews = new Dictionary<Hazard, Transform>();
        private readonly Dictionary<Vector2Int, Transform> _doorViews = new Dictionary<Vector2Int, Transform>();
        private readonly List<Hazard> _dead = new List<Hazard>();

        public IsoCamera Cam { get; private set; }

        // ------------------------------------------------------------------ build

        public void Build(MatchState state, MapRuntime mapRuntime, TaskSystem tasks, HazardSystem hazards,
            IsoCamera cam)
        {
            _state = state;
            _mapRuntime = mapRuntime;
            _tasks = tasks;
            _hazards = hazards;
            Cam = cam;

            Clear();

            _root = new GameObject("OfficeWorld").transform;
            _root.SetParent(transform, false);
            _staticRoot = PropKit.Group(_root, "Static", Vector3.zero);
            _dynamicRoot = PropKit.Group(_root, "Dynamic", Vector3.zero);

            BuildLighting();
            BuildFloors();
            BuildWalls();
            BuildDoorFrames();
            DressRooms();
            BuildStationMarkers();
            BuildCharacters();

            PropKit.SetShadows(_staticRoot, true);
        }

        public void Clear()
        {
            _rigs.Clear();
            _markers.Clear();
            _markerIcons.Clear();
            _hazardViews.Clear();
            _doorViews.Clear();
            if (_root != null) Destroy(_root.gameObject);
            _root = null;
        }

        private void BuildLighting()
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(_root, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            _sun = sunGo.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.color = Palette.Hex("FFF3DE");
            _sun.intensity = 1.55f;
            _sun.shadows = LightShadows.Soft;
            _sun.shadowStrength = 0.75f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex("9CB6CE");
            RenderSettings.ambientEquatorColor = Palette.Hex("7A90A8");
            RenderSettings.ambientGroundColor = Palette.Hex("556272");

            // Personal lamp: invisible in daylight, the only light source during a blackout.
            var lampGo = new GameObject("PlayerLamp");
            lampGo.transform.SetParent(_dynamicRoot, false);
            _playerLamp = lampGo.AddComponent<Light>();
            _playerLamp.type = LightType.Point;
            _playerLamp.color = Palette.Hex("FFE2B0");
            _playerLamp.range = CCConfig.LightsOutViewRadius * 2.4f;
            _playerLamp.intensity = 0f;
            _playerLamp.shadows = LightShadows.None;
        }

        private void BuildFloors()
        {
            Texture2D tile = MaterialLab.FloorTexture();

            // Apron well beyond the walls: an isometric camera always sees past the map edge,
            // and raw clear colour out there reads as a bug rather than as floor.
            PropKit.Part(_staticRoot,
                new Vector3(_state.Map.Width * 0.5f, -0.06f, _state.Map.Height * 0.5f),
                new Vector3(_state.Map.Width + 70f, 0.1f, _state.Map.Height + 70f),
                Palette.Hex("223047"), 0.05f);

            for (int i = 0; i < _state.Map.Rooms.Count; i++)
            {
                RoomDef room = _state.Map.Rooms[i];
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var col = go.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
                go.name = "Floor_" + room.Id;
                go.transform.SetParent(_staticRoot, false);
                go.transform.position = new Vector3(room.Bounds.center.x, 0f, room.Bounds.center.y);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                go.transform.localScale = new Vector3(room.Bounds.width, room.Bounds.height, 1f);

                bool carpet = room.Id == "empfang";
                Material m = carpet
                    ? MaterialLab.Get(room.FloorColor, 0.08f)
                    : MaterialLab.Textured(room.FloorColor, tile,
                        new Vector2(room.Bounds.width * 0.5f, room.Bounds.height * 0.5f), 0.62f);
                go.GetComponent<MeshRenderer>().sharedMaterial = m;
                go.GetComponent<MeshRenderer>().shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // Doorway patches so gaps in walls still have ground under them.
            for (int i = 0; i < _state.Map.Doors.Count; i++)
            {
                Vector2Int t = _state.Map.Doors[i].Tile;
                PropKit.Part(_staticRoot, new Vector3(t.x + 0.5f, 0.005f, t.y + 0.5f),
                    new Vector3(1.02f, 0.01f, 1.02f), Pal3D.FloorTileA, 0.55f);
            }
        }

        private float WallHeight(int x, int y)
        {
            MapDefinition map = _state.Map;
            if (y == map.Height - 1 || x == map.Width - 1) return PerimeterFarHeight;
            if (y == 0 || x == 0) return PerimeterNearHeight;
            // The corridor's back wall carries the doors, so it stands taller than a partition.
            if (y == 19) return BackWallHeight;
            return PartitionHeight;
        }

        private Color WallColour(int x, int y)
        {
            MapDefinition map = _state.Map;
            if (y == map.Height - 1 || x == map.Width - 1) return Pal3D.WallDeep;
            if (y == 19) return Pal3D.WallTeal;
            return Pal3D.WallTeal;
        }

        /// <summary>
        /// Walls are slim panels down the middle of their tile, merged into runs in both
        /// directions. Rendering them a full tile thick made them read as concrete slabs at
        /// this camera distance; a partition wants to look like a partition.
        /// Overlaps at T-junctions are harmless, so both passes span the full run.
        /// </summary>
        private void BuildWalls()
        {
            MapDefinition map = _state.Map;

            // Horizontal runs.
            for (int y = 0; y < map.Height; y++)
            {
                int start = -1;
                for (int x = 0; x <= map.Width; x++)
                {
                    bool wall = x < map.Width && map.At(x, y) == TileKind.Wall;
                    if (wall && start < 0) start = x;
                    if (!wall && start >= 0)
                    {
                        if (x - start >= 2) EmitWall(start, x, y, true);
                        start = -1;
                    }
                }
            }

            // Vertical runs.
            for (int x = 0; x < map.Width; x++)
            {
                int start = -1;
                for (int y = 0; y <= map.Height; y++)
                {
                    bool wall = y < map.Height && map.At(x, y) == TileKind.Wall;
                    if (wall && start < 0) start = y;
                    if (!wall && start >= 0)
                    {
                        if (y - start >= 2) EmitWall(x, start, y, false);
                        start = -1;
                    }
                }
            }

            // Anything still isolated becomes a short post so no gap is left behind.
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (map.At(x, y) != TileKind.Wall) continue;
                    bool hasNeighbour = map.At(x - 1, y) == TileKind.Wall || map.At(x + 1, y) == TileKind.Wall
                                        || map.At(x, y - 1) == TileKind.Wall || map.At(x, y + 1) == TileKind.Wall;
                    if (hasNeighbour) continue;
                    float h = WallHeight(x, y);
                    PropKit.Box(_staticRoot, new Vector3(x + 0.5f, 0f, y + 0.5f),
                        new Vector3(WallThickness, h, WallThickness), WallColour(x, y), 0.14f);
                }
            }
        }

        private const float WallThickness = 0.42f;

        /// <summary>
        /// Horizontal run: <paramref name="from"/>..<paramref name="to"/> along X on row
        /// <paramref name="lane"/>. Vertical run: <paramref name="from"/> is the column and
        /// the run spans <paramref name="to"/>..<paramref name="lane"/> along Z.
        /// </summary>
        private void EmitWall(int from, int to, int lane, bool horizontal)
        {
            float height;
            Color colour;
            Vector3 centre;
            Vector3 size;

            if (horizontal)
            {
                float length = to - from;
                height = WallHeight(from, lane);
                colour = WallColour(from, lane);
                centre = new Vector3(from + length * 0.5f, 0f, lane + 0.5f);
                size = new Vector3(length, height, WallThickness);
            }
            else
            {
                float length = lane - to;
                height = WallHeight(from, to);
                colour = WallColour(from, to);
                centre = new Vector3(from + 0.5f, 0f, to + length * 0.5f);
                size = new Vector3(WallThickness, height, length);
            }

            PropKit.Box(_staticRoot, centre, size, colour, 0.14f);

            // Cap rail: the pale edge that makes a partition read as furniture, not architecture.
            var capSize = new Vector3(size.x + 0.05f, 0.07f, size.z + 0.05f);
            PropKit.Part(_staticRoot, centre + new Vector3(0f, height + 0.05f, 0f), capSize,
                Pal3D.WallCapLight, 0.3f);

            if (height > 1.6f)
                PropKit.Part(_staticRoot, centre + new Vector3(0f, 0.1f, 0f),
                    new Vector3(size.x + 0.02f, 0.2f, size.z + 0.02f), Pal3D.WallTrim, 0.2f);
        }

        private void BuildDoorFrames()
        {
            for (int i = 0; i < _state.Map.Doors.Count; i++)
            {
                DoorDef d = _state.Map.Doors[i];
                float h = WallHeight(d.Tile.x, d.Tile.y);
                var centre = new Vector3(d.Tile.x + 0.5f, 0f, d.Tile.y + 0.5f);

                Transform g = PropKit.Group(_staticRoot, "DoorFrame", centre, d.Horizontal ? 0f : 90f);
                PropKit.Box(g, new Vector3(-0.52f, 0f, 0f), new Vector3(0.16f, h, 1.02f), Pal3D.WallTrim, 0.2f);
                PropKit.Box(g, new Vector3(0.52f, 0f, 0f), new Vector3(0.16f, h, 1.02f), Pal3D.WallTrim, 0.2f);
                PropKit.Part(g, new Vector3(0f, h - 0.1f, 0f), new Vector3(1.2f, 0.2f, 1.02f), Pal3D.WallTrim, 0.2f);

                // The blocker only appears while the door is jammed.
                Transform jam = PropKit.Group(g, "Jam", Vector3.zero);
                for (int s = 0; s < 4; s++)
                {
                    Transform slat = PropKit.Part(jam, new Vector3(-0.42f + s * 0.28f, 0.55f, 0f),
                        new Vector3(0.3f, 0.55f, 0.12f), s % 2 == 0 ? Pal3D.Yellow : Pal3D.PlasticDark, 0.25f);
                    slat.localRotation = Quaternion.Euler(0f, 0f, 24f);
                }
                jam.gameObject.SetActive(false);
                _doorViews[d.Tile] = jam;
            }
        }

        // ------------------------------------------------------------------ dressing

        private void DressRooms()
        {
            var rng = new DeterministicRng(_state.Seed ^ 0x51A7);
            Transform d = PropKit.Group(_staticRoot, "Dressing", Vector3.zero);

            DressReception(d);
            DressElevator(d);
            DressToilets(d);
            DressCorridors(d, rng);
            DressOffice(d, rng);
            DressKitchen(d);
            DressStorage(d, rng);
        }

        private static Vector3 P(float x, float z, float y = 0f) => new Vector3(x, y, z);

        private void DressReception(Transform d)
        {
            PropLibrary.Rug(d, P(3.4f, 22.2f), new Vector2(4.6f, 3.4f), Pal3D.CarpetDeep);
            PropLibrary.ReceptionDesk(d, P(4.6f, 25.1f), 0f);
            PropLibrary.Sofa(d, P(1.7f, 21.6f), 90f, Pal3D.Yellow);
            PropLibrary.Sofa(d, P(5.4f, 20.7f), 0f, Pal3D.Cyan);
            PropLibrary.CoffeeTable(d, P(3.1f, 21.6f), 0f);
            PropLibrary.Plant(d, P(1.6f, 26.2f), 1.2f, true);
            PropLibrary.Plant(d, P(8.2f, 20.6f), 1.0f, true);
            PropLibrary.Plant(d, P(7.9f, 24.0f), 0.85f, false);
            PropLibrary.ArtFrame(d, P(3.0f, 26.85f, 1.9f), 0f);
            PropLibrary.ArtFrame(d, P(6.4f, 26.85f, 1.9f), 0f);
            PropLibrary.OfficeChair(d, P(4.6f, 26.1f), 180f);
            PropLibrary.FilingCabinet(d, P(8.1f, 25.9f), 180f);
            PropLibrary.RoomLight(d, P(4.5f, 23.5f), 8.5f, 1.35f);
            PropLibrary.RoomLight(d, P(4.5f, 26.0f), 8.5f, 1.35f);
        }

        private void DressElevator(Transform d)
        {
            PropLibrary.Elevator(d, P(12.5f, 26.4f), 0f);
            PropLibrary.FloorSign(d, P(14.8f, 26.75f), 0f);
            PropLibrary.Plant(d, P(10.6f, 25.8f), 1.05f, false);
            PropLibrary.Bench(d, P(14.4f, 21.4f), 0f);
            PropLibrary.Noticeboard(d, P(10.35f, 23.4f), 90f);
            PropLibrary.Rug(d, P(12.5f, 24.4f), new Vector2(3.2f, 2.4f), Pal3D.CarpetDeep);
            PropLibrary.RoomLight(d, P(12.5f, 23.0f), 8.5f, 1.35f);
            PropLibrary.RoomLight(d, P(12.5f, 25.6f), 8.5f, 1.35f);
        }

        private void DressToilets(Transform d)
        {
            PropLibrary.ToiletDoor(d, P(18.6f, 26.75f), 0f, false);
            PropLibrary.ToiletDoor(d, P(21.0f, 26.75f), 0f, true);
            PropLibrary.Sink(d, P(22.0f, 23.6f), -90f);
            PropLibrary.Plant(d, P(17.4f, 21.0f), 0.9f, true);
            PropLibrary.Bench(d, P(18.6f, 20.9f), 0f);
            PropLibrary.RoomLight(d, P(19.8f, 23.0f), 8.5f, 1.35f);
            PropLibrary.RoomLight(d, P(19.8f, 25.6f), 8.5f, 1.35f);
            PropLibrary.WallLamp(d, P(22.7f, 25.4f), 90f);
        }

        private void DressCorridors(Transform d, DeterministicRng rng)
        {
            PropLibrary.ArtFrame(d, P(16.0f, 19.35f, 1.6f), 180f);
            PropLibrary.ArtFrame(d, P(8.0f, 19.35f, 1.6f), 180f);
            PropLibrary.Plant(d, P(12.5f, 17.6f), 1.05f, true);
            PropLibrary.Plant(d, P(3.5f, 17.6f), 0.9f, false);
            PropLibrary.Plant(d, P(21.4f, 17.3f), 0.95f, true);
            PropLibrary.RoomLight(d, P(6.0f, 17.5f), 8.5f, 1.35f);
            PropLibrary.RoomLight(d, P(17.0f, 17.5f), 8.5f, 1.35f);

            PropLibrary.Plant(d, P(12.6f, 6.5f), 0.95f, true);
            PropLibrary.Plant(d, P(11.4f, 12.4f), 0.8f, false);
            PropLibrary.ArtFrame(d, P(14.65f, 9.5f, 1.5f), 90f);
            PropLibrary.ArtFrame(d, P(10.35f, 3.4f, 1.5f), -90f);
            PropLibrary.FireExtinguisher(d, P(14.7f, 13.4f), 90f);
            PropLibrary.FireExtinguisher(d, P(1.3f, 17.4f), -90f);
            PropLibrary.Noticeboard(d, P(6.0f, 18.85f), 180f);
            PropLibrary.Bench(d, P(19.5f, 17.4f), 0f);
            PropLibrary.RoomLight(d, P(12.5f, 11.0f), 8.5f, 1.35f);
            PropLibrary.RoomLight(d, P(12.5f, 4.0f), 8.5f, 1.35f);

            for (int i = 0; i < 4; i++)
                PropLibrary.PaperSheet(d, P(rng.Range(11.3f, 14.6f), rng.Range(2.0f, 14.0f)),
                    rng.Range(0f, 360f), rng.Range(-4f, 4f));
        }

        private void DressOffice(Transform d, DeterministicRng rng)
        {
            PropLibrary.Printer(d, P(2.5f, 12.8f), 0f);
            for (int i = 0; i < 8; i++)
                PropLibrary.PaperSheet(d, P(2.5f + rng.Range(-1.6f, 1.6f), 12.0f + rng.Range(-1.3f, 0.6f)),
                    rng.Range(0f, 360f), rng.Range(-6f, 6f));

            PropLibrary.Desk(d, P(3.0f, 9.0f), 0f, true);
            PropLibrary.OfficeChair(d, P(3.0f, 7.9f), 0f);
            PropLibrary.Desk(d, P(7.0f, 9.0f), 0f, true);
            PropLibrary.OfficeChair(d, P(7.0f, 7.9f), 0f);
            PropLibrary.Desk(d, P(3.0f, 3.6f), 0f, true);
            PropLibrary.OfficeChair(d, P(3.0f, 2.5f), 0f);
            PropLibrary.Desk(d, P(7.4f, 3.6f), 0f, true);
            PropLibrary.OfficeChair(d, P(7.4f, 2.5f), 0f);

            PropLibrary.Desk(d, P(3.0f, 6.3f), 180f, true);
            PropLibrary.OfficeChair(d, P(3.0f, 7.3f), 180f);
            PropLibrary.Desk(d, P(7.4f, 6.3f), 180f, true);
            PropLibrary.OfficeChair(d, P(7.4f, 7.3f), 180f);

            PropLibrary.FilingCabinet(d, P(1.4f, 13.6f), -90f);
            PropLibrary.FilingCabinet(d, P(1.4f, 12.4f), -90f);
            PropLibrary.Whiteboard(d, P(5.0f, 0.55f), 180f);
            PropLibrary.Plant(d, P(1.5f, 5.8f), 1.15f, true);
            PropLibrary.Plant(d, P(8.6f, 12.4f), 1.0f, false);
            PropLibrary.Plant(d, P(8.7f, 1.5f), 0.85f, true);
            PropLibrary.Poster(d, P(8.9f, 10.5f, 1.0f), -90f, Pal3D.Cyan, 1.1f, 0.8f);
            PropLibrary.RoomLight(d, P(5.0f, 12.5f), 8.5f, 1.35f);
            PropLibrary.RoomLight(d, P(5.0f, 8.5f), 8.5f, 1.35f);
            PropLibrary.RoomLight(d, P(5.0f, 4.0f), 8.5f, 1.35f);
        }

        private void DressKitchen(Transform d)
        {
            PropLibrary.KitchenCounter(d, P(22.25f, 11.8f), 90f, 5.0f);
            PropLibrary.CoffeeMachine(d, P(22.1f, 13.6f, 0.97f), 90f);
            PropLibrary.Mug(d, P(22.1f, 12.6f, 0.97f));
            PropLibrary.Mug(d, P(22.25f, 12.3f, 0.97f));
            PropLibrary.PaperTowel(d, P(22.1f, 10.0f, 0.97f));
            PropLibrary.WaterCooler(d, P(21.6f, 9.5f), 0f);

            PropLibrary.Poster(d, P(22.85f, 13.4f, 1.75f), -90f, Pal3D.Yellow, 1.0f, 0.72f);
            PropLibrary.Poster(d, P(22.85f, 11.9f, 1.75f), -90f, Pal3D.WallWarm, 1.0f, 0.72f);

            PropLibrary.Fridge(d, P(16.7f, 13.9f), 90f);
            PropLibrary.RoundTable(d, P(18.6f, 10.6f));
            PropLibrary.Stool(d, P(17.6f, 10.6f));
            PropLibrary.Stool(d, P(19.6f, 10.6f));
            PropLibrary.Stool(d, P(18.6f, 9.7f));
            PropLibrary.WetFloorSign(d, P(19.9f, 12.6f), 24f);
            PropLibrary.CoffeeSpill(d, P(20.7f, 12.1f));
            PropLibrary.Plant(d, P(16.6f, 9.6f), 0.9f, true);
            PropLibrary.RoomLight(d, P(19.5f, 12.6f), 8.5f, 1.35f);
            PropLibrary.RoomLight(d, P(19.5f, 10.0f), 8.5f, 1.35f);
        }

        private void DressStorage(Transform d, DeterministicRng rng)
        {
            PropLibrary.Shelf(d, P(22.1f, 5.6f), 90f, rng);
            PropLibrary.Shelf(d, P(22.1f, 2.6f), 90f, rng);
            PropLibrary.Barrier(d, P(17.6f, 3.4f), 90f);

            for (int i = 0; i < 5; i++)
                PropLibrary.CardboardBox(d, P(rng.Range(16.6f, 20.5f), rng.Range(1.6f, 6.6f)),
                    rng.Range(0f, 360f), rng.Range(0.8f, 1.25f));

            PropLibrary.Ladder(d, P(17.0f, 6.6f), 200f);
            PropLibrary.Shelf(d, P(18.0f, 1.5f), 0f, rng);
            PropLibrary.RoomLight(d, P(19.5f, 4.0f), 8.5f, 1.35f);
            PropLibrary.RoomLight(d, P(19.5f, 6.4f), 8.5f, 1.35f);
            PropLibrary.WallLamp(d, P(16.35f, 6.0f), -90f);
        }

        // ------------------------------------------------------------------ markers

        private void BuildStationMarkers()
        {
            for (int i = 0; i < _state.Map.Stations.Count; i++)
            {
                StationDef s = _state.Map.Stations[i];
                Vector2 p = _tasks.PositionOf(s.Id);

                Transform g = PropKit.Group(_dynamicRoot, "Marker_" + s.Id, new Vector3(p.x, 0f, p.y));
                PropLibrary.SelectionRing(g, 1.3f, Pal3D.Yellow);

                Transform icon = PropKit.Group(g, "Icon", new Vector3(0f, 1.5f, 0f));
                Color tint = IconTint(s.Icon);
                Transform body = PropKit.Part(icon, Vector3.zero, new Vector3(0.34f, 0.34f, 0.34f), tint, 0.3f, 1.1f);
                body.localRotation = Quaternion.Euler(0f, 45f, 45f);
                PropKit.SetShadows(g, false);

                _markers[s.Id] = g;
                _markerIcons[s.Id] = icon;
                g.gameObject.SetActive(false);
            }
        }

        private static Color IconTint(TaskIcon icon)
        {
            switch (icon)
            {
                case TaskIcon.Coffee: return Pal3D.Coffee;
                case TaskIcon.Box: return Pal3D.WoodLight;
                default: return Pal3D.Cyan;
            }
        }

        private void BuildCharacters()
        {
            for (int i = 0; i < _state.Players.Count; i++)
            {
                PlayerState p = _state.Players[i];
                _rigs.Add(CharacterRig.Create(_dynamicRoot, p, p.IsLocal));
            }
        }

        public CharacterRig RigFor(int playerId)
        {
            for (int i = 0; i < _rigs.Count; i++)
                if (_rigs[i].Player.Id == playerId) return _rigs[i];
            return null;
        }

        // ------------------------------------------------------------------ per frame

        public void Tick(float dt)
        {
            if (_state == null || _root == null) return;

            SyncMarkers();
            SyncDoors();
            SyncHazards();
            SyncLighting(dt);

            PlayerState local = _state.Local;
            for (int i = 0; i < _rigs.Count; i++)
            {
                PlayerState p = _rigs[i].Player;
                bool visible = true;
                if (_state.LightsOut && !p.IsLocal && local != null)
                    visible = Vector2.Distance(p.Position, local.Position) < CCConfig.LightsOutViewRadius * 1.2f;
                _rigs[i].Tick(dt, visible);
            }
        }

        private void SyncMarkers()
        {
            PlayerState local = _state.Local;
            if (local == null) return;

            foreach (var kv in _markers)
            {
                bool mine = local.TaskIds.Contains(kv.Key);
                if (kv.Value.gameObject.activeSelf != mine) kv.Value.gameObject.SetActive(mine);
                if (!mine) continue;

                Vector2 p = _tasks.PositionOf(kv.Key);
                kv.Value.position = new Vector3(p.x, 0f, p.y);

                bool done = local.CompletedTaskIds.Contains(kv.Key);
                if (_markerIcons.TryGetValue(kv.Key, out Transform icon))
                {
                    icon.localPosition = new Vector3(0f, 1.5f + Mathf.Sin(Time.time * 2.2f) * 0.1f, 0f);
                    icon.localRotation = Quaternion.Euler(0f, Time.time * 55f, 0f);
                    icon.gameObject.SetActive(!done);
                }
                kv.Value.GetChild(0).gameObject.SetActive(!done);
            }
        }

        private void SyncDoors()
        {
            foreach (var kv in _doorViews)
            {
                bool jammed = _mapRuntime.IsDoorJammed(kv.Key);
                if (kv.Value.gameObject.activeSelf != jammed) kv.Value.gameObject.SetActive(jammed);
            }
        }

        private void SyncHazards()
        {
            _dead.Clear();
            foreach (var kv in _hazardViews)
                if (!_hazards.Hazards.Contains(kv.Key)) _dead.Add(kv.Key);

            for (int i = 0; i < _dead.Count; i++)
            {
                if (_hazardViews.TryGetValue(_dead[i], out Transform t) && t != null) Destroy(t.gameObject);
                _hazardViews.Remove(_dead[i]);
            }

            for (int i = 0; i < _hazards.Hazards.Count; i++)
            {
                Hazard h = _hazards.Hazards[i];
                if (!_hazardViews.TryGetValue(h, out Transform view))
                {
                    var pos = new Vector3(h.Position.x, 0f, h.Position.y);
                    switch (h.Kind)
                    {
                        case HazardKind.Banana: view = PropLibrary.BananaPeel(_dynamicRoot, pos, h.Spin); break;
                        case HazardKind.Puddle: view = PropLibrary.Puddle(_dynamicRoot, pos); break;
                        default: view = PropLibrary.PaperSheet(_dynamicRoot, pos, h.Spin, 0f); break;
                    }
                    PropKit.SetShadows(view, false);
                    _hazardViews[h] = view;
                }
                view.position = new Vector3(h.Position.x, 0f, h.Position.y);
            }
        }

        private void SyncLighting(float dt)
        {
            bool dark = _state.LightsOut;
            float wantSun = dark ? 0.12f : 1.55f;
            _sun.intensity = Mathf.Lerp(_sun.intensity, wantSun, 1f - Mathf.Exp(-3f * dt));

            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor,
                dark ? Palette.Hex("1A2235") : Palette.Hex("9CB6CE"), 1f - Mathf.Exp(-3f * dt));
            RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor,
                dark ? Palette.Hex("141A2A") : Palette.Hex("7A90A8"), 1f - Mathf.Exp(-3f * dt));

            PlayerState local = _state.Local;
            if (local != null)
            {
                _playerLamp.transform.position = new Vector3(local.Position.x, 1.9f, local.Position.y);
                _playerLamp.intensity = Mathf.Lerp(_playerLamp.intensity, dark ? 5.5f : 0f,
                    1f - Mathf.Exp(-3f * dt));
            }
        }
    }
}
