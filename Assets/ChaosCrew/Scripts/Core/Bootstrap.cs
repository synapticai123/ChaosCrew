using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Entry point. The game builds itself entirely from code after any scene loads, so the
    /// prototype runs by pressing Play in a fresh, empty scene — there is no prefab wiring
    /// or scene setup that can drift out of sync.
    /// </summary>
    public static class Bootstrap
    {
        private static GameObject _root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (_root != null) return;
            if (Object.FindFirstObjectByType<GameDirector>() != null) return;

            HideLeftoverSceneContent();
            PrepareCamera();

            _root = new GameObject("ChaosCrew");
            Object.DontDestroyOnLoad(_root);
            _root.AddComponent<GameDirector>();
        }

        /// <summary>
        /// Whatever the open scene happened to contain (template cubes, extra lights) would
        /// show up inside the office. Disable it rather than delete it: the user's scene
        /// stays intact on disk.
        /// </summary>
        private static void HideLeftoverSceneContent()
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;

            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++) lights[i].enabled = false;
        }

        private static void PrepareCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("ChaosCrew Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Hex("1A2338");
            cam.allowHDR = false;
            cam.allowMSAA = true;
            Object.DontDestroyOnLoad(cam.gameObject);

            if (Object.FindFirstObjectByType<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();
        }
    }
}

