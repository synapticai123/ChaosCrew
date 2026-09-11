using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Floor 1 of the office. Room adjacency follows the concept art: reception, lift and
    /// toilets across the north side, the open-plan office down the west flank, and the
    /// break room stacked above the storage bay on the east.
    ///
    /// Authored as ASCII (top row = north / high Z). A second map is a second class like this.
    /// </summary>
    public static class OfficeMap
    {
        private static readonly string[] Layout =
        {
            "########################", // z27
            "#........#......#......#", // z26  Reception | Aufzug | Toiletten
            "#........#......#......#", // z25
            "#........#......#......#", // z24
            "#........#......#......#", // z23
            "#........#......#......#", // z22
            "#........#......#......#", // z21
            "#........#......#......#", // z20
            "####D#######..####D##D##", // z19  Türen: Empfang / Aufzug offen / 2x WC
            "#......................#", // z18  Nordflur
            "#......................#", // z17
            "#......................#", // z16
            "#####D#####....####D####", // z15  Türen: Büro / Flur offen / Küche
            "#.........#....#.......#", // z14  Büro | Mittelflur | Küche
            "#.........#....#.......#", // z13
            "#.........#....D.......#", // z12  Küchentür zum Flur
            "#.........#....#.......#", // z11
            "#.........D....#.......#", // z10  Bürotür zum Flur
            "#.........#....#.......#", // z9
            "#.........#....#########", // z8   Trennwand Küche / Lager
            "#.........#....#.......#", // z7   Büro | Mittelflur | Lager
            "#.........#....#.......#", // z6
            "#.........D....#.......#", // z5   zweite Bürotür
            "#.........#....D.......#", // z4   Lagertür zum Flur
            "#.........#....#.......#", // z3
            "#.........#....#.......#", // z2
            "#.........#....#.......#", // z1
            "########################", // z0
        };

        public static MapDefinition Build()
        {
            var map = new MapDefinition { DisplayName = "Etage 1" };
            map.ParseLayout(Layout);

            AddRoom(map, "empfang",    "Empfang",     new RectInt(1, 20, 8, 7),  Pal3D.CarpetBlue, true);
            AddRoom(map, "aufzug",     "Aufzug",      new RectInt(10, 19, 6, 8), Pal3D.FloorLift, false);
            AddRoom(map, "toilette",   "Toiletten",   new RectInt(17, 20, 6, 7), Pal3D.FloorWC, false);
            AddRoom(map, "flur_nord",  "Nordflur",    new RectInt(1, 16, 22, 3), Pal3D.FloorTileA, true);
            AddRoom(map, "buero",      "Großraumbüro", new RectInt(1, 1, 9, 14), Pal3D.FloorTileA, true);
            // Reaches up to z15 so the corridor gap in the wall row still belongs to a room.
            AddRoom(map, "flur_mitte", "Mittelflur",  new RectInt(11, 1, 4, 15), Pal3D.FloorTileA, true);
            AddRoom(map, "kueche",     "Teeküche",    new RectInt(16, 9, 7, 6),  Pal3D.FloorKitchen, true);
            AddRoom(map, "lager",      "Lager",       new RectInt(16, 1, 7, 7),  Pal3D.FloorStore, false);

            // Reception
            AddStation(map, "st_gaeste",  "Besucher eintragen",  "empfang",  4.5f, 23.5f, MiniGameKind.Keypad,    TaskIcon.Document);
            AddStation(map, "st_akten",   "Akten sortieren",     "empfang",  7.5f, 25.5f, MiniGameKind.TimingBar, TaskIcon.Document);
            // Elevator hall
            AddStation(map, "st_knopf",   "Aufzugknöpfe putzen", "aufzug",  11.5f, 22.5f, MiniGameKind.TapItems,  TaskIcon.Box);
            // Toilets
            AddStation(map, "st_rohr",    "Rohr abdichten",      "toilette",18.5f, 24.5f, MiniGameKind.Wires,     TaskIcon.Box);
            AddStation(map, "st_seife",   "Seife nachfüllen",    "toilette",21.5f, 22.5f, MiniGameKind.Mash,      TaskIcon.Box);
            // North corridor
            AddStation(map, "st_pflanze", "Pflanze gießen",      "flur_nord",12.5f, 17.5f, MiniGameKind.HoldGauge, TaskIcon.Coffee);
            AddStation(map, "st_feuer",   "Feuerlöscher prüfen", "flur_nord", 3.5f, 17.5f, MiniGameKind.TimingBar, TaskIcon.Box);
            // Open office
            AddStation(map, "st_drucker", "Drucker entstören",   "buero",    2.5f, 12.5f, MiniGameKind.Mash,      TaskIcon.Document);
            AddStation(map, "st_kabel",   "Kabel verbinden",     "buero",    5.5f,  8.5f, MiniGameKind.Wires,     TaskIcon.Box);
            AddStation(map, "st_bericht", "Bericht tippen",      "buero",    8.5f,  4.5f, MiniGameKind.Keypad,    TaskIcon.Document);
            AddStation(map, "st_monitor", "Monitor kalibrieren", "buero",    3.5f,  3.5f, MiniGameKind.TimingBar, TaskIcon.Document);
            // Break room
            AddStation(map, "st_kaffee",  "Kaffee kochen",       "kueche",  18.5f, 13.5f, MiniGameKind.HoldGauge, TaskIcon.Coffee);
            AddStation(map, "st_spuele",  "Spülmaschine leeren", "kueche",  21.5f, 10.5f, MiniGameKind.TapItems,  TaskIcon.Coffee);
            // Storage
            AddStation(map, "st_kisten",  "Kisten stapeln",      "lager",   18.5f,  5.5f, MiniGameKind.TapItems,  TaskIcon.Box);
            AddStation(map, "st_inventar","Inventar scannen",    "lager",   21.5f,  2.5f, MiniGameKind.Keypad,    TaskIcon.Box);
            // Middle corridor
            AddStation(map, "st_wischen", "Boden wischen",       "flur_mitte",12.5f, 6.5f, MiniGameKind.Mash,     TaskIcon.Coffee);

            map.ElevatorPad = new Vector2(12.5f, 23.5f);
            map.SpawnPoints.Add(new Vector2(11.5f, 21.5f));
            map.SpawnPoints.Add(new Vector2(13.5f, 21.5f));
            map.SpawnPoints.Add(new Vector2(11.5f, 24.5f));
            map.SpawnPoints.Add(new Vector2(14.5f, 24.5f));

            return map;
        }

        private static void AddRoom(MapDefinition map, string id, string name, RectInt bounds, Color floor, bool camera)
        {
            map.Rooms.Add(new RoomDef
            {
                Id = id,
                DisplayName = name,
                Bounds = bounds,
                FloorColor = floor,
                HasCamera = camera
            });
        }

        private static void AddStation(MapDefinition map, string id, string name, string room,
            float x, float z, MiniGameKind kind, TaskIcon icon)
        {
            map.Stations.Add(new StationDef
            {
                Id = id,
                DisplayName = name,
                RoomId = room,
                Position = new Vector2(x, z),
                MiniGame = kind,
                Icon = icon
            });
        }
    }

    /// <summary>Registry of playable maps. Extend by adding entries here.</summary>
    public static class MapCatalog
    {
        public static MapDefinition BuildDefault() => OfficeMap.Build();
    }
}
