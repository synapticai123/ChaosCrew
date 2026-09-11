using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    public enum Icon
    {
        Bolt,
        Gear,
        Signal,
        Document,
        Coffee,
        Box,
        Check,
        Hand,
        Runner,
        ArrowUp,
        ChevronDown,
        Eye,
        Skull
    }

    /// <summary>
    /// Vector icons rasterised once into white, tintable sprites. Shapes are described as
    /// polygon loops in a 0..1 box (y up); additive loops union together and subtractive
    /// loops punch holes, which covers everything from a gear to a coffee cup handle.
    /// </summary>
    public static class IconLab
    {
        private struct Loop
        {
            public Vector2[] Points;
            public bool Subtract;
        }

        private static readonly Dictionary<Icon, Sprite> Cache = new Dictionary<Icon, Sprite>();
        private const int Size = 128;
        private const int Samples = 3;

        public static Sprite Get(Icon icon)
        {
            if (Cache.TryGetValue(icon, out Sprite hit) && hit != null) return hit;
            Sprite s = Rasterise(Build(icon));
            s.name = "icon_" + icon;
            Cache[icon] = s;
            return s;
        }

        // ------------------------------------------------------------------ shapes

        private static List<Loop> Build(Icon icon)
        {
            var loops = new List<Loop>();

            switch (icon)
            {
                case Icon.Bolt:
                    Add(loops, new[]
                    {
                        V(0.60f, 1.00f), V(0.16f, 0.48f), V(0.44f, 0.48f),
                        V(0.34f, 0.00f), V(0.84f, 0.56f), V(0.54f, 0.56f)
                    });
                    break;

                case Icon.Gear:
                {
                    const int teeth = 8;
                    var pts = new List<Vector2>();
                    for (int i = 0; i < teeth * 4; i++)
                    {
                        float a = i * Mathf.PI * 2f / (teeth * 4);
                        // Two samples out on the tooth, two in on the valley.
                        int phase = i % 4;
                        float r = (phase == 1 || phase == 2) ? 0.50f : 0.38f;
                        pts.Add(V(0.5f + Mathf.Cos(a) * r, 0.5f + Mathf.Sin(a) * r));
                    }
                    Add(loops, pts.ToArray());
                    Sub(loops, CirclePts(new Vector2(0.5f, 0.5f), 0.17f, 28));
                    break;
                }

                case Icon.Signal:
                    for (int i = 0; i < 4; i++)
                    {
                        float x = 0.08f + i * 0.23f;
                        float h = 0.22f + i * 0.22f;
                        Add(loops, Rect(x, 0.08f, x + 0.16f, 0.08f + h));
                    }
                    break;

                case Icon.Document:
                    // Back sheet, then the front sheet with a folded corner and text lines.
                    Add(loops, Rect(0.06f, 0.22f, 0.58f, 0.94f));
                    Add(loops, new[]
                    {
                        V(0.34f, 0.06f), V(0.34f, 0.78f), V(0.78f, 0.78f),
                        V(0.94f, 0.62f), V(0.94f, 0.06f)
                    });
                    Sub(loops, Rect(0.44f, 0.56f, 0.84f, 0.63f));
                    Sub(loops, Rect(0.44f, 0.42f, 0.84f, 0.49f));
                    Sub(loops, Rect(0.44f, 0.28f, 0.70f, 0.35f));
                    break;

                case Icon.Coffee:
                    Add(loops, new[] { V(0.16f, 0.78f), V(0.68f, 0.78f), V(0.61f, 0.22f), V(0.23f, 0.22f) });
                    Add(loops, CirclePts(new Vector2(0.74f, 0.58f), 0.17f, 26));
                    Sub(loops, CirclePts(new Vector2(0.74f, 0.58f), 0.09f, 26));
                    Add(loops, Rect(0.08f, 0.08f, 0.78f, 0.19f));
                    break;

                case Icon.Box:
                    // Isometric crate: top, left and right faces unioned, edges scored in.
                    Add(loops, new[] { V(0.50f, 0.98f), V(0.94f, 0.74f), V(0.50f, 0.50f), V(0.06f, 0.74f) });
                    Add(loops, new[] { V(0.06f, 0.74f), V(0.50f, 0.50f), V(0.50f, 0.04f), V(0.06f, 0.28f) });
                    Add(loops, new[] { V(0.94f, 0.74f), V(0.50f, 0.50f), V(0.50f, 0.04f), V(0.94f, 0.28f) });
                    Sub(loops, Segment(V(0.50f, 0.50f), V(0.50f, 0.04f), 0.035f));
                    Sub(loops, Segment(V(0.06f, 0.74f), V(0.50f, 0.50f), 0.035f));
                    Sub(loops, Segment(V(0.94f, 0.74f), V(0.50f, 0.50f), 0.035f));
                    break;

                case Icon.Check:
                    Add(loops, new[]
                    {
                        V(0.06f, 0.52f), V(0.20f, 0.36f), V(0.41f, 0.55f),
                        V(0.80f, 0.14f), V(0.94f, 0.30f), V(0.41f, 0.84f)
                    });
                    break;

                case Icon.Hand:
                    Add(loops, Rect(0.20f, 0.04f, 0.80f, 0.58f));
                    Add(loops, CirclePts(new Vector2(0.50f, 0.20f), 0.30f, 26));
                    Add(loops, Round(0.24f, 0.46f, 0.36f, 0.80f));
                    Add(loops, Round(0.38f, 0.46f, 0.50f, 0.92f));
                    Add(loops, Round(0.52f, 0.46f, 0.64f, 0.89f));
                    Add(loops, Round(0.66f, 0.46f, 0.77f, 0.75f));
                    Add(loops, Segment(V(0.24f, 0.40f), V(0.06f, 0.52f), 0.13f));
                    break;

                case Icon.Runner:
                    Add(loops, CirclePts(new Vector2(0.66f, 0.85f), 0.12f, 24));
                    Add(loops, Segment(V(0.62f, 0.74f), V(0.44f, 0.46f), 0.15f));
                    Add(loops, Segment(V(0.58f, 0.66f), V(0.26f, 0.72f), 0.10f));
                    Add(loops, Segment(V(0.58f, 0.66f), V(0.84f, 0.54f), 0.10f));
                    Add(loops, Segment(V(0.46f, 0.48f), V(0.20f, 0.26f), 0.12f));
                    Add(loops, Segment(V(0.46f, 0.48f), V(0.68f, 0.34f), 0.12f));
                    Add(loops, Segment(V(0.68f, 0.34f), V(0.80f, 0.14f), 0.11f));
                    Add(loops, Segment(V(0.20f, 0.26f), V(0.08f, 0.16f), 0.10f));
                    break;

                case Icon.ArrowUp:
                    Add(loops, new[] { V(0.50f, 0.92f), V(0.92f, 0.20f), V(0.08f, 0.20f) });
                    break;

                case Icon.ChevronDown:
                    Add(loops, new[]
                    {
                        V(0.50f, 0.10f), V(0.94f, 0.66f), V(0.76f, 0.84f),
                        V(0.50f, 0.50f), V(0.24f, 0.84f), V(0.06f, 0.66f)
                    });
                    break;

                case Icon.Eye:
                    Add(loops, new[]
                    {
                        V(0.04f, 0.50f), V(0.28f, 0.80f), V(0.72f, 0.80f), V(0.96f, 0.50f),
                        V(0.72f, 0.20f), V(0.28f, 0.20f)
                    });
                    Sub(loops, CirclePts(new Vector2(0.50f, 0.50f), 0.19f, 26));
                    break;

                case Icon.Skull:
                    Add(loops, CirclePts(new Vector2(0.50f, 0.58f), 0.38f, 30));
                    Add(loops, Rect(0.28f, 0.10f, 0.72f, 0.34f));
                    Sub(loops, CirclePts(new Vector2(0.36f, 0.60f), 0.11f, 20));
                    Sub(loops, CirclePts(new Vector2(0.64f, 0.60f), 0.11f, 20));
                    Sub(loops, Rect(0.44f, 0.10f, 0.50f, 0.28f));
                    Sub(loops, Rect(0.55f, 0.10f, 0.61f, 0.28f));
                    break;
            }

            return loops;
        }

        // ------------------------------------------------------------------ helpers

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static void Add(List<Loop> loops, Vector2[] pts) =>
            loops.Add(new Loop { Points = pts, Subtract = false });

        private static void Sub(List<Loop> loops, Vector2[] pts) =>
            loops.Add(new Loop { Points = pts, Subtract = true });

        private static Vector2[] Rect(float x0, float y0, float x1, float y1) =>
            new[] { V(x0, y0), V(x1, y0), V(x1, y1), V(x0, y1) };

        /// <summary>Rectangle with the short ends capped, so fingers read as rounded.</summary>
        private static Vector2[] Round(float x0, float y0, float x1, float y1)
        {
            float r = (x1 - x0) * 0.5f;
            var pts = new List<Vector2>();
            var top = new Vector2((x0 + x1) * 0.5f, y1 - r);
            for (int i = 0; i <= 12; i++)
            {
                float a = Mathf.PI * i / 12f;
                pts.Add(top + new Vector2(-Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }
            pts.Add(V(x1, y0));
            pts.Add(V(x0, y0));
            return pts.ToArray();
        }

        private static Vector2[] CirclePts(Vector2 c, float r, int segments)
        {
            var pts = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                pts[i] = c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            return pts;
        }

        /// <summary>Thick line between two points, as a quad. The limb primitive.</summary>
        private static Vector2[] Segment(Vector2 a, Vector2 b, float width)
        {
            Vector2 d = (b - a).normalized;
            var n = new Vector2(-d.y, d.x) * width * 0.5f;
            Vector2 ext = d * width * 0.5f;
            return new[] { a - ext + n, b + ext + n, b + ext - n, a - ext - n };
        }

        // ------------------------------------------------------------------ raster

        private static Sprite Rasterise(List<Loop> loops)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var px = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            float fx = (x + (sx + 0.5f) / Samples) / Size;
                            float fy = (y + (sy + 0.5f) / Samples) / Size;
                            if (Inside(loops, new Vector2(fx, fy))) hits++;
                        }
                    }
                    float a = hits / (float)(Samples * Samples);
                    px[y * Size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static bool Inside(List<Loop> loops, Vector2 p)
        {
            bool inside = false;
            for (int i = 0; i < loops.Count; i++)
            {
                if (loops[i].Subtract) continue;
                if (PointInPolygon(loops[i].Points, p))
                {
                    inside = true;
                    break;
                }
            }
            if (!inside) return false;

            for (int i = 0; i < loops.Count; i++)
            {
                if (!loops[i].Subtract) continue;
                if (PointInPolygon(loops[i].Points, p)) return false;
            }
            return true;
        }

        private static bool PointInPolygon(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (poly[i].y > p.y == poly[j].y > p.y) continue;
                float t = (p.y - poly[i].y) / (poly[j].y - poly[i].y);
                if (p.x < poly[i].x + t * (poly[j].x - poly[i].x)) inside = !inside;
            }
            return inside;
        }
    }
}
