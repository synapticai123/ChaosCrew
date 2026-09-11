using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Runtime material cache. The template is lifted from a throwaway primitive so we always
    /// get the render pipeline's own default lit shader — no Shader.Find guessing, works in
    /// URP and Built-in alike.
    /// </summary>
    public static class MaterialLab
    {
        private static readonly Dictionary<int, Material> Cache = new Dictionary<int, Material>();
        private static Shader _litShader;

        private static Shader LitShader
        {
            get
            {
                if (_litShader != null) return _litShader;
                var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                probe.hideFlags = HideFlags.HideAndDontSave;
                var r = probe.GetComponent<MeshRenderer>();
                _litShader = r.sharedMaterial != null ? r.sharedMaterial.shader : Shader.Find("Standard");
                Object.DestroyImmediate(probe);
                return _litShader;
            }
        }

        /// <param name="smoothness">0 = matte plastic, 1 = mirror. Floors sit around 0.6.</param>
        /// <param name="emission">Self-lit amount, for screens, signs and light panels.</param>
        public static Material Get(Color color, float smoothness = 0.12f, float emission = 0f)
        {
            int key = color.GetHashCode()
                      ^ (Mathf.RoundToInt(smoothness * 100f) << 8)
                      ^ (Mathf.RoundToInt(emission * 100f) << 18);
            if (Cache.TryGetValue(key, out Material hit) && hit != null) return hit;

            var m = new Material(LitShader) { hideFlags = HideFlags.DontSave };
            SetColor(m, color);
            SetSmoothness(m, smoothness);
            SetMetallic(m, 0f);

            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (m.HasProperty("_EmissionColor"))
                    m.SetColor("_EmissionColor", color * emission);
            }

            Cache[key] = m;
            return m;
        }

        /// <summary>Metal for elevator doors, appliance bodies and chair frames.</summary>
        public static Material Metal(Color color, float smoothness = 0.72f)
        {
            int key = color.GetHashCode() ^ (Mathf.RoundToInt(smoothness * 100f) << 8) ^ (1 << 28);
            if (Cache.TryGetValue(key, out Material hit) && hit != null) return hit;

            var m = new Material(LitShader) { hideFlags = HideFlags.DontSave };
            SetColor(m, color);
            SetSmoothness(m, smoothness);
            SetMetallic(m, 0.85f);
            Cache[key] = m;
            return m;
        }

        /// <summary>Tinted material carrying a tiling texture, for floors and carpets.</summary>
        public static Material Textured(Color color, Texture2D tex, Vector2 tiling, float smoothness = 0.4f)
        {
            var m = new Material(LitShader) { hideFlags = HideFlags.DontSave };
            SetColor(m, color);
            SetSmoothness(m, smoothness);
            SetMetallic(m, 0f);
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTextureScale("_BaseMap", tiling);
            }
            if (m.HasProperty("_MainTex"))
            {
                m.SetTexture("_MainTex", tex);
                m.SetTextureScale("_MainTex", tiling);
            }
            return m;
        }

        private static Texture2D _floorTex;

        /// <summary>Square floor tiles with grout lines and a faint sheen variation.</summary>
        public static Texture2D FloorTexture()
        {
            if (_floorTex != null) return _floorTex;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { hideFlags = HideFlags.DontSave };
            var px = new Color[size * size];
            var rng = new DeterministicRng(5150);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Two tiles per texture so the checker survives any tiling factor.
                    int tx = x < size / 2 ? 0 : 1;
                    int ty = y < size / 2 ? 0 : 1;
                    Color c = (tx + ty) % 2 == 0 ? Color.white : new Color(0.94f, 0.955f, 0.97f);

                    int gx = x % (size / 2);
                    int gy = y % (size / 2);
                    int edge = size / 2 - 1;
                    if (gx <= 1 || gy <= 1 || gx >= edge - 1 || gy >= edge - 1)
                        c = new Color(0.80f, 0.83f, 0.87f);

                    float n = 0.985f + rng.NextFloat() * 0.03f;
                    px[y * size + x] = new Color(c.r * n, c.g * n, c.b * n, 1f);
                }
            }

            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 4;
            tex.Apply(true, false);
            _floorTex = tex;
            return tex;
        }

        private static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        private static void SetSmoothness(Material m, float v)
        {
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", v);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", v);
        }

        private static void SetMetallic(Material m, float v)
        {
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", v);
        }

        public static void Clear() => Cache.Clear();
    }
}
