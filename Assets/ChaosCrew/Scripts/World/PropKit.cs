using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Small vocabulary for assembling props out of Unity primitives. Everything in the
    /// office is built from these four calls, which keeps the whole art pass asset-free.
    /// </summary>
    public static class PropKit
    {
        /// <summary>Box with its pivot on the floor: position is the footprint centre.</summary>
        public static Transform Box(Transform parent, Vector3 centreOnFloor, Vector3 size, Color color,
            float smoothness = 0.12f, float emission = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Strip(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centreOnFloor + new Vector3(0f, size.y * 0.5f, 0f);
            go.transform.localScale = size;
            Paint(go, MaterialLab.Get(color, smoothness, emission));
            return go.transform;
        }

        /// <summary>Box centred on its own pivot, for parts stacked inside another prop.</summary>
        public static Transform Part(Transform parent, Vector3 centre, Vector3 size, Color color,
            float smoothness = 0.12f, float emission = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Strip(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.transform.localScale = size;
            Paint(go, MaterialLab.Get(color, smoothness, emission));
            return go.transform;
        }

        public static Transform Cylinder(Transform parent, Vector3 centreOnFloor, float diameter, float height,
            Color color, float smoothness = 0.12f, float emission = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Strip(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centreOnFloor + new Vector3(0f, height * 0.5f, 0f);
            // Unity cylinders are 2 units tall by default.
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            Paint(go, MaterialLab.Get(color, smoothness, emission));
            return go.transform;
        }

        public static Transform Sphere(Transform parent, Vector3 centre, float diameter, Color color,
            float smoothness = 0.12f, float emission = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Strip(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.transform.localScale = Vector3.one * diameter;
            Paint(go, MaterialLab.Get(color, smoothness, emission));
            return go.transform;
        }

        public static Transform Capsule(Transform parent, Vector3 centre, float diameter, float height,
            Color color, float smoothness = 0.12f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Strip(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            Paint(go, MaterialLab.Get(color, smoothness));
            return go.transform;
        }

        /// <summary>Flat quad lying on a wall or floor; used for posters, signs and decals.</summary>
        public static Transform Panel(Transform parent, Vector3 centre, Vector2 size, Quaternion rotation,
            Color color, float emission = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Strip(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.transform.localRotation = rotation;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            Material m = MaterialLab.Get(color, 0.1f, emission);
            Paint(go, m);
            return go.transform;
        }

        public static Transform Group(Transform parent, string name, Vector3 position, float yawDegrees = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            return go.transform;
        }

        public static void Paint(GameObject go, Material m)
        {
            var r = go.GetComponent<MeshRenderer>();
            if (r != null) r.sharedMaterial = m;
        }

        public static void Paint(Transform t, Color color, float smoothness = 0.12f, float emission = 0f)
        {
            Paint(t.gameObject, MaterialLab.Get(color, smoothness, emission));
        }

        /// <summary>
        /// Props are decoration only — the simulation does its own tile collision, so the
        /// primitive colliders would just cost memory and confuse raycasts.
        /// </summary>
        private static void Strip(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            go.isStatic = false;
        }

        public static void SetShadows(Transform root, bool cast)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = cast
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
                renderers[i].receiveShadows = true;
            }
        }
    }
}
