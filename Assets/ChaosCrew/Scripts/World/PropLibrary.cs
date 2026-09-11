using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Every piece of office furniture in the game, assembled from primitives. Each builder
    /// takes the floor position its footprint sits on and a yaw in degrees.
    /// </summary>
    public static class PropLibrary
    {
        private static readonly Quaternion FaceUp = Quaternion.Euler(-90f, 0f, 0f);

        // ------------------------------------------------------------------ reception

        public static Transform ReceptionDesk(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "ReceptionDesk", pos, yaw);

            // Main counter body with a lighter sign band across the front.
            PropKit.Box(g, Vector3.zero, new Vector3(4.4f, 1.05f, 1.3f), Pal3D.WoodLight, 0.3f);
            PropKit.Part(g, new Vector3(0f, 0.62f, -0.68f), new Vector3(3.2f, 0.34f, 0.06f), Pal3D.WoodDark, 0.2f);
            // Worktop overhang.
            PropKit.Part(g, new Vector3(0f, 1.09f, 0f), new Vector3(4.7f, 0.09f, 1.55f), Pal3D.CounterWhite, 0.45f);

            // Return wing, which is what gives the desk its wrapped, curved read.
            PropKit.Box(g, new Vector3(2.45f, 0f, 1.1f), new Vector3(1.3f, 1.05f, 2.4f), Pal3D.WoodLight, 0.3f);
            PropKit.Part(g, new Vector3(2.45f, 1.09f, 1.1f), new Vector3(1.55f, 0.09f, 2.6f), Pal3D.CounterWhite, 0.45f);

            Monitor(g, new Vector3(-1.1f, 1.14f, 0.2f), 190f);
            Monitor(g, new Vector3(0.6f, 1.14f, 0.25f), 165f);
            PenCup(g, new Vector3(1.6f, 1.14f, -0.1f));
            return g;
        }

        public static Transform Sofa(Transform parent, Vector3 pos, float yaw, Color colour)
        {
            Transform g = PropKit.Group(parent, "Sofa", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(2.0f, 0.42f, 0.95f), colour, 0.16f);
            PropKit.Part(g, new Vector3(0f, 0.62f, -0.38f), new Vector3(2.0f, 0.62f, 0.22f), colour, 0.16f);
            PropKit.Part(g, new Vector3(-0.95f, 0.55f, 0f), new Vector3(0.22f, 0.34f, 0.95f), colour, 0.16f);
            PropKit.Part(g, new Vector3(0.95f, 0.55f, 0f), new Vector3(0.22f, 0.34f, 0.95f), colour, 0.16f);
            return g;
        }

        public static Transform CoffeeTable(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "CoffeeTable", pos, yaw);
            PropKit.Cylinder(g, Vector3.zero, 0.14f, 0.42f, Pal3D.MetalDark, 0.5f);
            PropKit.Cylinder(g, new Vector3(0f, 0.42f, 0f), 1.15f, 0.08f, Pal3D.WoodDark, 0.35f);
            PropKit.Part(g, new Vector3(0.12f, 0.53f, 0.05f), new Vector3(0.42f, 0.03f, 0.3f), Pal3D.Cyan, 0.3f);
            PropKit.Part(g, new Vector3(-0.14f, 0.54f, -0.1f), new Vector3(0.38f, 0.03f, 0.28f), Pal3D.Red, 0.3f);
            return g;
        }

        // ------------------------------------------------------------------ elevator

        public static Transform Elevator(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Elevator", pos, yaw);

            // Recessed frame.
            PropKit.Box(g, Vector3.zero, new Vector3(3.0f, 2.9f, 0.35f), Pal3D.MetalLight, 0.55f);
            PropKit.Part(g, new Vector3(0f, 1.45f, -0.1f), new Vector3(2.5f, 2.4f, 0.2f), Pal3D.MetalDark, 0.7f);

            // Doors with a warm glow in the gap, as if the car is lit.
            PropKit.Part(g, new Vector3(-0.62f, 1.32f, -0.22f), new Vector3(1.18f, 2.2f, 0.1f), Pal3D.MetalLight, 0.8f);
            PropKit.Part(g, new Vector3(0.62f, 1.32f, -0.22f), new Vector3(1.18f, 2.2f, 0.1f), Pal3D.MetalLight, 0.8f);
            PropKit.Part(g, new Vector3(0f, 1.32f, -0.18f), new Vector3(0.05f, 2.2f, 0.02f), Pal3D.WarmLight, 0.2f, 1.6f);

            // Call panel: two arrows above the doors.
            PropKit.Part(g, new Vector3(0f, 2.62f, -0.2f), new Vector3(0.9f, 0.3f, 0.06f), Pal3D.PlasticDark, 0.3f);
            PropKit.Part(g, new Vector3(-0.18f, 2.62f, -0.25f), new Vector3(0.18f, 0.16f, 0.03f), Pal3D.Red, 0.2f, 1.4f);
            PropKit.Part(g, new Vector3(0.18f, 2.62f, -0.25f), new Vector3(0.18f, 0.16f, 0.03f), Pal3D.Yellow, 0.2f, 1.4f);

            PropKit.Part(g, new Vector3(1.72f, 1.5f, -0.12f), new Vector3(0.14f, 0.42f, 0.1f), Pal3D.PlasticDark, 0.3f);
            return g;
        }

        /// <summary>The "FLOOR 1" plate beside the lift.</summary>
        public static Transform FloorSign(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "FloorSign", pos, yaw);
            PropKit.Part(g, new Vector3(0f, 1.9f, 0f), new Vector3(0.95f, 1.15f, 0.09f), Pal3D.WallTrim, 0.25f);
            PropKit.Part(g, new Vector3(0f, 2.24f, -0.06f), new Vector3(0.62f, 0.16f, 0.03f), Pal3D.WallCap, 0.2f);
            PropKit.Part(g, new Vector3(0f, 1.72f, -0.06f), new Vector3(0.2f, 0.5f, 0.03f), Pal3D.WallCap, 0.2f);
            return g;
        }

        // ------------------------------------------------------------------ toilets

        public static Transform ToiletDoor(Transform parent, Vector3 pos, float yaw, bool female)
        {
            Transform g = PropKit.Group(parent, "ToiletDoor", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(1.15f, 2.35f, 0.14f), Pal3D.WallCap, 0.3f);
            PropKit.Part(g, new Vector3(0f, 1.18f, -0.09f), new Vector3(0.95f, 2.1f, 0.04f), Pal3D.PlasticDark, 0.35f);

            // Pictogram: head, body, and a skirt silhouette for the second door.
            Color ink = Pal3D.WallTrim;
            PropKit.Sphere(g, new Vector3(0f, 1.76f, -0.13f), 0.17f, ink, 0.2f);
            if (female)
            {
                PropKit.Part(g, new Vector3(0f, 1.5f, -0.13f), new Vector3(0.13f, 0.22f, 0.03f), ink, 0.2f);
                PropKit.Part(g, new Vector3(0f, 1.32f, -0.13f), new Vector3(0.42f, 0.16f, 0.03f), ink, 0.2f);
                PropKit.Part(g, new Vector3(-0.07f, 1.15f, -0.13f), new Vector3(0.08f, 0.22f, 0.03f), ink, 0.2f);
                PropKit.Part(g, new Vector3(0.07f, 1.15f, -0.13f), new Vector3(0.08f, 0.22f, 0.03f), ink, 0.2f);
            }
            else
            {
                PropKit.Part(g, new Vector3(0f, 1.44f, -0.13f), new Vector3(0.34f, 0.36f, 0.03f), ink, 0.2f);
                PropKit.Part(g, new Vector3(-0.09f, 1.12f, -0.13f), new Vector3(0.11f, 0.32f, 0.03f), ink, 0.2f);
                PropKit.Part(g, new Vector3(0.09f, 1.12f, -0.13f), new Vector3(0.11f, 0.32f, 0.03f), ink, 0.2f);
            }

            PropKit.Sphere(g, new Vector3(0.38f, 1.05f, -0.16f), 0.1f, Pal3D.MetalLight, 0.7f);
            return g;
        }

        // ------------------------------------------------------------------ kitchen

        public static Transform KitchenCounter(Transform parent, Vector3 pos, float yaw, float length)
        {
            Transform g = PropKit.Group(parent, "KitchenCounter", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(length, 0.88f, 0.75f), Pal3D.CabinetBlue, 0.22f);
            PropKit.Part(g, new Vector3(0f, 0.92f, 0f), new Vector3(length + 0.1f, 0.1f, 0.85f), Pal3D.CounterWhite, 0.5f);

            // Cabinet doors with slim handles.
            int doors = Mathf.Max(1, Mathf.RoundToInt(length / 0.8f));
            for (int i = 0; i < doors; i++)
            {
                float x = -length * 0.5f + length * (i + 0.5f) / doors;
                PropKit.Part(g, new Vector3(x, 0.42f, -0.385f), new Vector3(length / doors - 0.08f, 0.66f, 0.03f),
                    Pal3D.CabinetDeep, 0.25f);
                PropKit.Part(g, new Vector3(x, 0.68f, -0.41f), new Vector3(0.24f, 0.035f, 0.03f), Pal3D.MetalLight, 0.7f);
            }
            return g;
        }

        public static Transform CoffeeMachine(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "CoffeeMachine", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(0.52f, 0.66f, 0.46f), Pal3D.PlasticDark, 0.4f);
            PropKit.Part(g, new Vector3(0f, 0.52f, -0.24f), new Vector3(0.3f, 0.2f, 0.03f), Pal3D.ScreenBlue, 0.3f, 1.3f);
            PropKit.Part(g, new Vector3(0f, 0.2f, -0.2f), new Vector3(0.26f, 0.06f, 0.12f), Pal3D.MetalLight, 0.7f);
            PropKit.Part(g, new Vector3(0f, 0.03f, -0.2f), new Vector3(0.34f, 0.03f, 0.2f), Pal3D.MetalDark, 0.6f);
            Mug(g, new Vector3(0f, 0.06f, -0.22f));
            return g;
        }

        public static Transform WaterCooler(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "WaterCooler", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(0.42f, 0.5f, 0.42f), Pal3D.CounterWhite, 0.3f);
            PropKit.Cylinder(g, new Vector3(0f, 0.5f, 0f), 0.4f, 0.46f, Pal3D.WaterBlue, 0.55f);
            PropKit.Part(g, new Vector3(0f, 0.28f, -0.23f), new Vector3(0.1f, 0.1f, 0.06f), Pal3D.MetalDark, 0.6f);
            return g;
        }

        public static Transform Mug(Transform parent, Vector3 pos)
        {
            Transform g = PropKit.Group(parent, "Mug", pos, 0f);
            PropKit.Cylinder(g, Vector3.zero, 0.12f, 0.13f, Pal3D.CounterWhite, 0.4f);
            PropKit.Cylinder(g, new Vector3(0f, 0.115f, 0f), 0.09f, 0.02f, Pal3D.Coffee, 0.55f);
            return g;
        }

        public static Transform PaperTowel(Transform parent, Vector3 pos)
        {
            Transform g = PropKit.Group(parent, "PaperTowel", pos, 0f);
            PropKit.Cylinder(g, Vector3.zero, 0.16f, 0.26f, Pal3D.PaperWhite, 0.2f);
            return g;
        }

        // ------------------------------------------------------------------ open office

        public static Transform Desk(Transform parent, Vector3 pos, float yaw, bool withDivider)
        {
            Transform g = PropKit.Group(parent, "Desk", pos, yaw);
            PropKit.Part(g, new Vector3(0f, 0.73f, 0f), new Vector3(1.85f, 0.07f, 0.95f), Pal3D.WoodLight, 0.35f);
            PropKit.Part(g, new Vector3(-0.85f, 0.36f, 0f), new Vector3(0.08f, 0.72f, 0.9f), Pal3D.MetalDark, 0.5f);
            PropKit.Part(g, new Vector3(0.85f, 0.36f, 0f), new Vector3(0.08f, 0.72f, 0.9f), Pal3D.MetalDark, 0.5f);

            // Drawer pedestal.
            PropKit.Box(g, new Vector3(0.6f, 0f, 0f), new Vector3(0.55f, 0.68f, 0.8f), Pal3D.WoodDark, 0.3f);
            PropKit.Part(g, new Vector3(0.6f, 0.5f, -0.41f), new Vector3(0.42f, 0.03f, 0.02f), Pal3D.MetalLight, 0.7f);

            if (withDivider)
                PropKit.Part(g, new Vector3(0f, 1.05f, 0.5f), new Vector3(1.95f, 0.72f, 0.09f), Pal3D.CarpetBlue, 0.12f);

            Monitor(g, new Vector3(-0.35f, 0.77f, 0.24f), 0f);
            Keyboard(g, new Vector3(-0.32f, 0.77f, -0.22f));
            return g;
        }

        public static Transform Monitor(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Monitor", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(0.22f, 0.03f, 0.16f), Pal3D.PlasticDark, 0.4f);
            PropKit.Part(g, new Vector3(0f, 0.12f, 0f), new Vector3(0.05f, 0.22f, 0.05f), Pal3D.PlasticDark, 0.4f);
            PropKit.Part(g, new Vector3(0f, 0.4f, 0.02f), new Vector3(0.72f, 0.44f, 0.04f), Pal3D.PlasticDark, 0.4f);
            PropKit.Part(g, new Vector3(0f, 0.4f, -0.01f), new Vector3(0.66f, 0.38f, 0.02f), Pal3D.ScreenBlue, 0.35f, 1.1f);
            return g;
        }

        public static Transform Keyboard(Transform parent, Vector3 pos)
        {
            Transform g = PropKit.Group(parent, "Keyboard", pos, 0f);
            PropKit.Part(g, Vector3.zero, new Vector3(0.5f, 0.025f, 0.18f), Pal3D.PlasticDark, 0.3f);
            PropKit.Part(g, new Vector3(0.42f, 0f, 0f), new Vector3(0.1f, 0.03f, 0.14f), Pal3D.PlasticDark, 0.3f);
            return g;
        }

        public static Transform OfficeChair(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "OfficeChair", pos, yaw);
            PropKit.Cylinder(g, Vector3.zero, 0.5f, 0.06f, Pal3D.PlasticDark, 0.4f);
            PropKit.Cylinder(g, new Vector3(0f, 0.06f, 0f), 0.09f, 0.36f, Pal3D.MetalDark, 0.6f);
            PropKit.Part(g, new Vector3(0f, 0.46f, 0f), new Vector3(0.5f, 0.1f, 0.48f), Pal3D.PlasticDark, 0.25f);
            PropKit.Part(g, new Vector3(0f, 0.78f, 0.22f), new Vector3(0.48f, 0.56f, 0.09f), Pal3D.CarpetBlue, 0.2f);
            return g;
        }

        public static Transform Printer(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Printer", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(1.15f, 0.78f, 0.95f), Pal3D.MetalLight, 0.3f);
            PropKit.Part(g, new Vector3(0f, 0.86f, 0.02f), new Vector3(1.2f, 0.18f, 0.9f), Pal3D.CounterWhite, 0.35f);
            PropKit.Part(g, new Vector3(0f, 0.99f, 0.16f), new Vector3(0.9f, 0.08f, 0.5f), Pal3D.PlasticDark, 0.4f);
            PropKit.Part(g, new Vector3(0.36f, 1.0f, -0.28f), new Vector3(0.28f, 0.06f, 0.18f), Pal3D.ScreenBlue, 0.3f, 1.1f);
            // Output tray with a few sheets already spat out.
            PropKit.Part(g, new Vector3(0f, 0.6f, -0.52f), new Vector3(0.8f, 0.03f, 0.28f), Pal3D.PlasticDark, 0.3f);
            PropKit.Part(g, new Vector3(0f, 0.63f, -0.55f), new Vector3(0.62f, 0.02f, 0.22f), Pal3D.PaperWhite, 0.15f);
            return g;
        }

        public static Transform PenCup(Transform parent, Vector3 pos)
        {
            Transform g = PropKit.Group(parent, "PenCup", pos, 0f);
            PropKit.Cylinder(g, Vector3.zero, 0.13f, 0.16f, Pal3D.MetalDark, 0.4f);
            PropKit.Part(g, new Vector3(-0.02f, 0.22f, 0f), new Vector3(0.025f, 0.2f, 0.025f), Pal3D.Red, 0.3f);
            PropKit.Part(g, new Vector3(0.03f, 0.23f, 0.02f), new Vector3(0.025f, 0.22f, 0.025f), Pal3D.Cyan, 0.3f);
            PropKit.Part(g, new Vector3(0.01f, 0.21f, -0.03f), new Vector3(0.025f, 0.18f, 0.025f), Pal3D.Yellow, 0.3f);
            return g;
        }

        // ------------------------------------------------------------------ storage

        public static Transform Shelf(Transform parent, Vector3 pos, float yaw, DeterministicRng rng)
        {
            Transform g = PropKit.Group(parent, "Shelf", pos, yaw);
            PropKit.Part(g, new Vector3(-0.95f, 1.0f, 0f), new Vector3(0.09f, 2.0f, 0.6f), Pal3D.MetalDark, 0.5f);
            PropKit.Part(g, new Vector3(0.95f, 1.0f, 0f), new Vector3(0.09f, 2.0f, 0.6f), Pal3D.MetalDark, 0.5f);

            for (int level = 0; level < 3; level++)
            {
                float y = 0.55f + level * 0.62f;
                PropKit.Part(g, new Vector3(0f, y, 0f), new Vector3(2.0f, 0.06f, 0.62f), Pal3D.MetalDark, 0.5f);

                int boxes = rng.Range(2, 4);
                for (int b = 0; b < boxes; b++)
                {
                    float w = rng.Range(0.34f, 0.52f);
                    float h = rng.Range(0.28f, 0.42f);
                    float x = -0.75f + b * 0.62f + rng.Range(-0.05f, 0.05f);
                    Color tone = Color.Lerp(Pal3D.WoodLight, Pal3D.WoodDark, rng.NextFloat() * 0.6f);
                    Transform box = PropKit.Part(g, new Vector3(x, y + 0.03f + h * 0.5f, rng.Range(-0.05f, 0.05f)),
                        new Vector3(w, h, 0.45f), tone, 0.15f);
                    box.localRotation = Quaternion.Euler(0f, rng.Range(-8f, 8f), 0f);
                    PropKit.Part(box, new Vector3(0f, 0.02f, -0.51f), new Vector3(0.5f, 0.12f, 0.02f),
                        Pal3D.PaperWhite, 0.1f);
                }
            }
            return g;
        }

        public static Transform CardboardBox(Transform parent, Vector3 pos, float yaw, float scale)
        {
            Transform g = PropKit.Group(parent, "Box", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(0.5f, 0.42f, 0.5f) * scale, Pal3D.WoodLight, 0.14f);
            PropKit.Part(g, new Vector3(0f, 0.42f * scale, 0f), new Vector3(0.52f, 0.03f, 0.14f) * scale,
                Pal3D.WoodDark, 0.12f);
            return g;
        }

        /// <summary>Striped "out of order" barrier from the storage bay.</summary>
        public static Transform Barrier(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Barrier", pos, yaw);
            PropKit.Part(g, new Vector3(-0.9f, 0.5f, 0f), new Vector3(0.1f, 1.0f, 0.1f), Pal3D.Red, 0.3f);
            PropKit.Part(g, new Vector3(0.9f, 0.5f, 0f), new Vector3(0.1f, 1.0f, 0.1f), Pal3D.Red, 0.3f);

            for (int bar = 0; bar < 2; bar++)
            {
                float y = 0.42f + bar * 0.42f;
                for (int i = 0; i < 8; i++)
                {
                    float x = -0.84f + i * 0.24f;
                    Color c = i % 2 == 0 ? Pal3D.Yellow : Pal3D.PlasticDark;
                    Transform slat = PropKit.Part(g, new Vector3(x, y, 0f), new Vector3(0.25f, 0.26f, 0.06f), c, 0.2f);
                    slat.localRotation = Quaternion.Euler(0f, 0f, 22f);
                }
            }
            PropKit.Part(g, new Vector3(0f, 0.63f, -0.06f), new Vector3(0.8f, 0.34f, 0.03f), Pal3D.PaperWhite, 0.15f);
            return g;
        }

        // ------------------------------------------------------------------ more furniture

        public static Transform FilingCabinet(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Cabinet", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(0.7f, 1.25f, 0.55f), Pal3D.MetalLight, 0.3f);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.25f + i * 0.4f;
                PropKit.Part(g, new Vector3(0f, y, -0.29f), new Vector3(0.6f, 0.32f, 0.03f), Pal3D.CabinetBlue, 0.25f);
                PropKit.Part(g, new Vector3(0f, y, -0.32f), new Vector3(0.22f, 0.04f, 0.03f), Pal3D.MetalDark, 0.6f);
            }
            return g;
        }

        public static Transform Whiteboard(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Whiteboard", pos, yaw);
            PropKit.Part(g, new Vector3(0f, 1.55f, 0f), new Vector3(1.9f, 1.1f, 0.07f), Pal3D.MetalLight, 0.35f);
            PropKit.Part(g, new Vector3(0f, 1.55f, -0.05f), new Vector3(1.76f, 0.96f, 0.02f), Pal3D.PaperWhite, 0.4f);
            PropKit.Part(g, new Vector3(-0.4f, 1.7f, -0.07f), new Vector3(0.7f, 0.05f, 0.01f), Pal3D.Cyan, 0.2f);
            PropKit.Part(g, new Vector3(-0.2f, 1.55f, -0.07f), new Vector3(1.0f, 0.05f, 0.01f), Pal3D.Red, 0.2f);
            PropKit.Part(g, new Vector3(-0.45f, 1.4f, -0.07f), new Vector3(0.5f, 0.05f, 0.01f), Pal3D.PlasticDark, 0.2f);
            PropKit.Part(g, new Vector3(0f, 0.98f, -0.06f), new Vector3(1.9f, 0.07f, 0.12f), Pal3D.MetalDark, 0.5f);
            return g;
        }

        public static Transform Fridge(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Fridge", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(0.78f, 1.85f, 0.72f), Pal3D.CounterWhite, 0.45f);
            PropKit.Part(g, new Vector3(0f, 1.2f, -0.37f), new Vector3(0.72f, 1.15f, 0.03f), Pal3D.MetalLight, 0.55f);
            PropKit.Part(g, new Vector3(0f, 0.42f, -0.37f), new Vector3(0.72f, 0.75f, 0.03f), Pal3D.MetalLight, 0.55f);
            PropKit.Part(g, new Vector3(0.28f, 1.15f, -0.4f), new Vector3(0.05f, 0.5f, 0.05f), Pal3D.MetalDark, 0.7f);
            PropKit.Part(g, new Vector3(-0.18f, 1.45f, -0.39f), new Vector3(0.16f, 0.2f, 0.01f), Pal3D.Yellow, 0.2f);
            return g;
        }

        public static Transform RoundTable(Transform parent, Vector3 pos)
        {
            Transform g = PropKit.Group(parent, "RoundTable", pos, 0f);
            PropKit.Cylinder(g, Vector3.zero, 0.16f, 0.72f, Pal3D.MetalDark, 0.55f);
            PropKit.Cylinder(g, new Vector3(0f, 0.05f, 0f), 0.62f, 0.05f, Pal3D.MetalDark, 0.5f);
            PropKit.Cylinder(g, new Vector3(0f, 0.72f, 0f), 1.05f, 0.08f, Pal3D.CounterWhite, 0.45f);
            return g;
        }

        public static Transform Stool(Transform parent, Vector3 pos)
        {
            Transform g = PropKit.Group(parent, "Stool", pos, 0f);
            PropKit.Cylinder(g, Vector3.zero, 0.34f, 0.04f, Pal3D.MetalDark, 0.55f);
            PropKit.Cylinder(g, new Vector3(0f, 0.04f, 0f), 0.08f, 0.6f, Pal3D.MetalLight, 0.65f);
            PropKit.Cylinder(g, new Vector3(0f, 0.64f, 0f), 0.42f, 0.09f, Pal3D.CabinetBlue, 0.3f);
            return g;
        }

        public static Transform Bench(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Bench", pos, yaw);
            PropKit.Part(g, new Vector3(0f, 0.44f, 0f), new Vector3(1.7f, 0.1f, 0.48f), Pal3D.WoodLight, 0.35f);
            PropKit.Part(g, new Vector3(-0.72f, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.44f), Pal3D.MetalDark, 0.55f);
            PropKit.Part(g, new Vector3(0.72f, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.44f), Pal3D.MetalDark, 0.55f);
            return g;
        }

        public static Transform FireExtinguisher(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Extinguisher", pos, yaw);
            PropKit.Part(g, new Vector3(0f, 0.85f, 0f), new Vector3(0.36f, 0.62f, 0.1f), Pal3D.PaperWhite, 0.25f);
            PropKit.Cylinder(g, new Vector3(0f, 0.62f, -0.07f), 0.2f, 0.46f, Pal3D.Red, 0.45f);
            PropKit.Cylinder(g, new Vector3(0f, 1.08f, -0.07f), 0.08f, 0.1f, Pal3D.PlasticDark, 0.4f);
            return g;
        }

        public static Transform Rug(Transform parent, Vector3 pos, Vector2 size, Color colour)
        {
            Transform g = PropKit.Group(parent, "Rug", pos, 0f);
            PropKit.Part(g, new Vector3(0f, 0.015f, 0f), new Vector3(size.x, 0.03f, size.y), colour, 0.1f);
            PropKit.Part(g, new Vector3(0f, 0.022f, 0f), new Vector3(size.x - 0.35f, 0.03f, size.y - 0.35f),
                Palette.Lighten(colour, 0.14f), 0.1f);
            return g;
        }

        public static Transform Sink(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Sink", pos, yaw);
            PropKit.Box(g, Vector3.zero, new Vector3(1.6f, 0.8f, 0.5f), Pal3D.CounterWhite, 0.4f);
            PropKit.Part(g, new Vector3(0f, 0.84f, 0f), new Vector3(1.7f, 0.09f, 0.56f), Pal3D.MetalLight, 0.6f);
            for (int i = -1; i <= 1; i += 2)
            {
                PropKit.Cylinder(g, new Vector3(i * 0.4f, 0.86f, 0.02f), 0.3f, 0.04f, Pal3D.PaperWhite, 0.5f);
                PropKit.Part(g, new Vector3(i * 0.4f, 1.0f, 0.16f), new Vector3(0.05f, 0.22f, 0.05f),
                    Pal3D.MetalLight, 0.75f);
                // Mirror above each basin.
                PropKit.Part(g, new Vector3(i * 0.4f, 1.62f, 0.24f), new Vector3(0.56f, 0.7f, 0.04f),
                    Pal3D.Cyan, 0.75f);
            }
            return g;
        }

        public static Transform Ladder(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Ladder", pos, yaw);
            for (int i = -1; i <= 1; i += 2)
            {
                Transform rail = PropKit.Part(g, new Vector3(i * 0.22f, 0.85f, 0f),
                    new Vector3(0.07f, 1.7f, 0.07f), Pal3D.MetalLight, 0.6f);
                rail.localRotation = Quaternion.Euler(10f, 0f, 0f);
            }
            for (int r = 0; r < 5; r++)
                PropKit.Part(g, new Vector3(0f, 0.3f + r * 0.32f, -0.05f + r * 0.055f),
                    new Vector3(0.5f, 0.05f, 0.07f), Pal3D.MetalLight, 0.6f);
            return g;
        }

        public static Transform Noticeboard(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "Noticeboard", pos, yaw);
            PropKit.Part(g, new Vector3(0f, 1.6f, 0f), new Vector3(1.5f, 1.0f, 0.07f), Pal3D.WoodDark, 0.25f);
            PropKit.Part(g, new Vector3(0f, 1.6f, -0.04f), new Vector3(1.38f, 0.88f, 0.02f), Pal3D.WallWarm, 0.2f);
            var rng = new DeterministicRng(Mathf.RoundToInt(pos.x * 17f + pos.z * 29f) | 1);
            for (int i = 0; i < 5; i++)
            {
                Color c = i % 2 == 0 ? Pal3D.Yellow : Pal3D.Cyan;
                PropKit.Part(g, new Vector3(rng.Range(-0.5f, 0.5f), 1.6f + rng.Range(-0.3f, 0.3f), -0.055f),
                    new Vector3(0.26f, 0.3f, 0.01f), c, 0.15f);
            }
            return g;
        }

        // ------------------------------------------------------------------ dressing

        public static Transform Plant(Transform parent, Vector3 pos, float scale, bool whitePot)
        {
            Transform g = PropKit.Group(parent, "Plant", pos, 0f);
            Color pot = whitePot ? Pal3D.CounterWhite : Pal3D.WoodDark;
            PropKit.Cylinder(g, Vector3.zero, 0.42f * scale, 0.4f * scale, pot, 0.3f);
            PropKit.Cylinder(g, new Vector3(0f, 0.38f * scale, 0f), 0.44f * scale, 0.06f * scale, pot, 0.3f);

            var rng = new DeterministicRng(Mathf.RoundToInt(pos.x * 91f + pos.z * 37f) | 1);
            int leaves = 7;
            for (int i = 0; i < leaves; i++)
            {
                float ang = i * 360f / leaves + rng.Range(-14f, 14f);
                float lean = rng.Range(22f, 46f);
                float len = rng.Range(0.45f, 0.78f) * scale;
                Transform leaf = PropKit.Part(g, Vector3.zero, new Vector3(0.26f * scale, len, 0.05f * scale),
                    Color.Lerp(Pal3D.Green, Pal3D.GreenDeep, rng.NextFloat()), 0.22f);
                leaf.localRotation = Quaternion.Euler(lean, ang, 0f);
                leaf.localPosition = leaf.localRotation * new Vector3(0f, len * 0.5f, 0f)
                                     + new Vector3(0f, 0.42f * scale, 0f);
            }
            return g;
        }

        public static Transform Poster(Transform parent, Vector3 pos, float yaw, Color tint, float width, float height)
        {
            Transform g = PropKit.Group(parent, "Poster", pos, yaw);
            PropKit.Part(g, Vector3.zero, new Vector3(width, height, 0.05f), Pal3D.WallTrim, 0.2f);
            PropKit.Part(g, new Vector3(0f, 0f, -0.032f), new Vector3(width - 0.12f, height - 0.12f, 0.02f), tint, 0.15f);
            return g;
        }

        /// <summary>Framed art with three colour blocks, like the pictures in the corridors.</summary>
        public static Transform ArtFrame(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "ArtFrame", pos, yaw);
            PropKit.Part(g, Vector3.zero, new Vector3(0.78f, 0.62f, 0.05f), Pal3D.WallTrim, 0.2f);
            PropKit.Part(g, new Vector3(0f, 0f, -0.032f), new Vector3(0.66f, 0.5f, 0.02f), Pal3D.WallWarm, 0.15f);
            PropKit.Part(g, new Vector3(-0.16f, -0.04f, -0.045f), new Vector3(0.22f, 0.3f, 0.01f), Pal3D.Cyan, 0.15f);
            PropKit.Part(g, new Vector3(0.06f, 0.05f, -0.045f), new Vector3(0.18f, 0.18f, 0.01f), Pal3D.Yellow, 0.15f);
            PropKit.Part(g, new Vector3(0.2f, -0.08f, -0.045f), new Vector3(0.14f, 0.24f, 0.01f), Pal3D.Red, 0.15f);
            return g;
        }

        /// <summary>
        /// A warm pool of light. There is no ceiling to hang a fixture from at this camera
        /// angle, so this is a bare light rather than a visible lamp.
        /// </summary>
        public static Transform RoomLight(Transform parent, Vector3 pos, float range, float intensity)
        {
            Transform g = PropKit.Group(parent, "RoomLight", pos + new Vector3(0f, 2.5f, 0f), 0f);
            var light = g.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Pal3D.WarmLight;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return g;
        }

        public static Transform WallLamp(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "WallLamp", pos, yaw);
            PropKit.Part(g, new Vector3(0f, 1.85f, 0f), new Vector3(0.14f, 0.5f, 0.07f), Pal3D.WarmLight, 0.1f, 2.0f);
            return g;
        }

        // ------------------------------------------------------------------ hazards

        public static Transform WetFloorSign(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "WetFloorSign", pos, yaw);
            Transform a = PropKit.Part(g, new Vector3(0f, 0.3f, -0.1f), new Vector3(0.42f, 0.62f, 0.04f), Pal3D.Yellow, 0.3f);
            a.localRotation = Quaternion.Euler(14f, 0f, 0f);
            Transform b = PropKit.Part(g, new Vector3(0f, 0.3f, 0.1f), new Vector3(0.42f, 0.62f, 0.04f), Pal3D.Yellow, 0.3f);
            b.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            PropKit.Part(g, new Vector3(0f, 0.34f, -0.16f), new Vector3(0.2f, 0.26f, 0.02f), Pal3D.PlasticDark, 0.2f);
            return g;
        }

        public static Transform BananaPeel(Transform parent, Vector3 pos, float yaw)
        {
            Transform g = PropKit.Group(parent, "BananaPeel", pos, yaw);
            for (int i = 0; i < 3; i++)
            {
                Transform strip = PropKit.Part(g, new Vector3(0f, 0.035f, 0f), new Vector3(0.1f, 0.05f, 0.34f),
                    Pal3D.Yellow, 0.35f);
                strip.localRotation = Quaternion.Euler(-16f, i * 60f - 60f, 0f);
                strip.localPosition = strip.localRotation * new Vector3(0f, 0f, 0.14f) + new Vector3(0f, 0.04f, 0f);
            }
            PropKit.Sphere(g, new Vector3(0f, 0.05f, 0f), 0.12f, Pal3D.YellowDeep, 0.4f);
            return g;
        }

        public static Transform Puddle(Transform parent, Vector3 pos)
        {
            Transform g = PropKit.Group(parent, "Puddle", pos, 0f);
            PropKit.Panel(g, new Vector3(0f, 0.012f, 0f), new Vector2(1.5f, 1.15f), FaceUp,
                new Color(Pal3D.WaterBlue.r, Pal3D.WaterBlue.g, Pal3D.WaterBlue.b, 1f), 0.25f);
            PropKit.Panel(g, new Vector3(0.22f, 0.02f, 0.12f), new Vector2(0.55f, 0.35f), FaceUp,
                Color.white, 0.5f);
            return g;
        }

        public static Transform CoffeeSpill(Transform parent, Vector3 pos)
        {
            Transform g = PropKit.Group(parent, "Spill", pos, 0f);
            PropKit.Panel(g, new Vector3(0f, 0.012f, 0f), new Vector2(0.9f, 0.7f), FaceUp, Pal3D.Coffee);
            PropKit.Panel(g, new Vector3(0.35f, 0.013f, -0.2f), new Vector2(0.3f, 0.24f), FaceUp, Pal3D.Coffee);
            return g;
        }

        public static Transform PaperSheet(Transform parent, Vector3 pos, float yaw, float tilt)
        {
            Transform g = PropKit.Group(parent, "Paper", pos, yaw);
            Transform sheet = PropKit.Part(g, new Vector3(0f, 0.012f, 0f), new Vector3(0.26f, 0.006f, 0.34f),
                Pal3D.PaperWhite, 0.1f);
            sheet.localRotation = Quaternion.Euler(tilt, 0f, tilt * 0.5f);
            return g;
        }

        /// <summary>Glow ring under the player, matching the selection marker in the mock-up.</summary>
        public static Transform SelectionRing(Transform parent, float diameter, Color colour)
        {
            Transform g = PropKit.Group(parent, "SelectionRing", Vector3.zero, 0f);
            int segments = 40;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var p = new Vector3(Mathf.Cos(a) * diameter * 0.5f, 0.03f, Mathf.Sin(a) * diameter * 0.5f);
                Transform seg = PropKit.Part(g, p, new Vector3(0.06f, 0.02f, 0.12f), colour, 0.2f, 2.4f);
                seg.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
            PropKit.SetShadows(g, false);
            return g;
        }
    }
}
