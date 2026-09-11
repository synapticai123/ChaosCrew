using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Runtime sprite factory. The prototype ships zero binary art assets: every shape is
    /// baked here on first use and cached. Swapping in authored art later just means
    /// replacing the lookups in this class.
    /// </summary>
    public static class TextureLab
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static Sprite Register(string key, Texture2D tex, float pixelsPerUnit, Vector4 border)
        {
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(false, false);
            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            s.name = key;
            Cache[key] = s;
            return s;
        }

        private static Sprite Register(string key, Texture2D tex) => Register(key, tex, 100f, Vector4.zero);

        private static Texture2D NewTex(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "cc_tex",
                hideFlags = HideFlags.DontSave
            };
        }

        /// <summary>Plain white sprite; tint it with Image.color.</summary>
        public static Sprite Solid()
        {
            const string key = "solid";
            if (Cache.TryGetValue(key, out var hit)) return hit;
            var tex = NewTex(4, 4);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            return Register(key, tex);
        }

        /// <summary>White rounded rectangle exported as a 9-slice sprite so it scales cleanly.</summary>
        public static Sprite RoundedRect(int radius)
        {
            radius = Mathf.Clamp(radius, 2, 64);
            string key = "round_" + radius;
            if (Cache.TryGetValue(key, out var hit)) return hit;

            int size = radius * 2 + 8;
            var tex = NewTex(size, size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = RectCoverage(x + 0.5f, y + 0.5f, 0f, 0f, size, size, radius);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            float b = radius + 2;
            return Register(key, tex, 100f, new Vector4(b, b, b, b));
        }

        /// <summary>Hollow rounded rectangle used for outlines and focus rings.</summary>
        public static Sprite RoundedOutline(int radius, int thickness)
        {
            radius = Mathf.Clamp(radius, 2, 64);
            thickness = Mathf.Clamp(thickness, 1, radius);
            string key = "roundline_" + radius + "_" + thickness;
            if (Cache.TryGetValue(key, out var hit)) return hit;

            int size = radius * 2 + 8;
            var tex = NewTex(size, size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float outer = RectCoverage(x + 0.5f, y + 0.5f, 0f, 0f, size, size, radius);
                    float inner = RectCoverage(x + 0.5f, y + 0.5f, thickness, thickness,
                        size - thickness, size - thickness, Mathf.Max(1, radius - thickness));
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(outer - inner));
                }
            }
            tex.SetPixels(px);
            float b = radius + 2;
            return Register(key, tex, 100f, new Vector4(b, b, b, b));
        }

        public static Sprite Circle(int diameter)
        {
            string key = "circle_" + diameter;
            if (Cache.TryGetValue(key, out var hit)) return hit;
            var tex = NewTex(diameter, diameter);
            var px = new Color[diameter * diameter];
            float r = diameter * 0.5f;
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    px[y * diameter + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d));
                }
            }
            tex.SetPixels(px);
            return Register(key, tex);
        }

        public static Sprite Circle() => Circle(128);

        public static Sprite Ring(int diameter, int thickness)
        {
            string key = "ring_" + diameter + "_" + thickness;
            if (Cache.TryGetValue(key, out var hit)) return hit;
            var tex = NewTex(diameter, diameter);
            var px = new Color[diameter * diameter];
            float r = diameter * 0.5f;
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float a = Mathf.Clamp01(r - d) * Mathf.Clamp01(d - (r - thickness));
                    px[y * diameter + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
            }
            tex.SetPixels(px);
            return Register(key, tex);
        }

        /// <summary>Soft radial falloff for the lights-out vignette: transparent centre, opaque rim.</summary>
        public static Sprite Vignette(int size)
        {
            string key = "vignette_" + size;
            if (Cache.TryGetValue(key, out var hit)) return hit;
            var tex = NewTex(size, size);
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.30f, 0.95f, d));
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            return Register(key, tex);
        }

        /// <summary>Soft rounded body shape used for characters.</summary>
        public static Sprite Blob()
        {
            const string key = "blob";
            if (Cache.TryGetValue(key, out var hit)) return hit;
            const int size = 128;
            var tex = NewTex(size, size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    // Superellipse: reads as a friendly, slightly boxy body.
                    float v = Mathf.Pow(Mathf.Abs(nx) / 0.84f, 3.2f) + Mathf.Pow(Mathf.Abs(ny) / 0.94f, 2.6f);
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - v) * 6f));
                }
            }
            tex.SetPixels(px);
            return Register(key, tex);
        }

        /// <summary>Vertical black gradient, opaque at the bottom, for scrims over artwork.</summary>
        public static Sprite BottomFade()
        {
            const string key = "bottomfade";
            if (Cache.TryGetValue(key, out var hit)) return hit;
            const int h = 128;
            var tex = NewTex(4, h);
            var px = new Color[4 * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                float a = Mathf.SmoothStep(0.92f, 0f, t);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(0f, 0f, 0f, a);
            }
            tex.SetPixels(px);
            return Register(key, tex);
        }

        /// <summary>Static / noise tile for corrupted camera footage.</summary>
        public static Sprite Noise(int seed)
        {
            string key = "noise_" + seed;
            if (Cache.TryGetValue(key, out var hit)) return hit;
            const int size = 96;
            var rng = new DeterministicRng(seed);
            var tex = NewTex(size, size);
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++)
            {
                float v = rng.NextFloat();
                if (v < 0.5f) v *= 0.4f;
                px[i] = new Color(v, v, v, 1f);
            }
            tex.SetPixels(px);
            return Register(key, tex);
        }

        /// <summary>Draws an anti-aliased rounded rect into a raw pixel buffer (used by the map baker).</summary>
        public static void BlitRoundedRect(Color[] buffer, int bufW, int bufH,
            float x0, float y0, float x1, float y1, float radius, Color color)
        {
            int ix0 = Mathf.Max(0, Mathf.FloorToInt(x0) - 1);
            int iy0 = Mathf.Max(0, Mathf.FloorToInt(y0) - 1);
            int ix1 = Mathf.Min(bufW - 1, Mathf.CeilToInt(x1) + 1);
            int iy1 = Mathf.Min(bufH - 1, Mathf.CeilToInt(y1) + 1);
            for (int y = iy0; y <= iy1; y++)
            {
                for (int x = ix0; x <= ix1; x++)
                {
                    float cov = RectCoverage(x + 0.5f, y + 0.5f, x0, y0, x1, y1, radius);
                    if (cov <= 0f) continue;
                    int i = y * bufW + x;
                    buffer[i] = BlendOver(buffer[i], color, cov * color.a);
                }
            }
        }

        private static Color BlendOver(Color dst, Color src, float a)
        {
            float outA = a + dst.a * (1f - a);
            if (outA <= 0.0001f) return new Color(0f, 0f, 0f, 0f);
            float inv = dst.a * (1f - a);
            Color rgb = (src * a + dst * inv) / outA;
            rgb.a = outA;
            return rgb;
        }

        /// <summary>Anti-aliased coverage of a rounded rect at a sample point (signed distance based).</summary>
        private static float RectCoverage(float px, float py, float x0, float y0, float x1, float y1, float radius)
        {
            float hw = (x1 - x0) * 0.5f;
            float hh = (y1 - y0) * 0.5f;
            if (hw <= 0f || hh <= 0f) return 0f;
            radius = Mathf.Min(radius, Mathf.Min(hw, hh));
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float dx = Mathf.Abs(px - cx) - (hw - radius);
            float dy = Mathf.Abs(py - cy) - (hh - radius);
            float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude
                            + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
            return Mathf.Clamp01(0.5f - outside);
        }

        public static void ClearCache() => Cache.Clear();
    }
}
