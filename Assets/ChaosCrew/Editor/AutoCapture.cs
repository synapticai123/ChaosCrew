#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ChaosCrew.EditorTools
{
    /// <summary>
    /// Development helper: when a request file appears in Temp/, the editor enters play mode
    /// on its own, drives the game into a live round and writes screenshots plus a log of
    /// anything that went wrong. It exists so the look of the game can be checked without a
    /// human sitting at the editor pressing Play.
    ///
    /// Hooks playModeStateChanged rather than relying on a domain reload, because this
    /// project has "Enter Play Mode Options" with domain reload switched off.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoCapture
    {
        private const string RequestFile = "Temp/cc_capture_request.txt";
        private const string ArmedFile = "Temp/cc_capture_armed.txt";
        private const string DoneFile = "Temp/cc_capture_done.txt";
        private const string LogFile = "Temp/cc_capture_log.txt";
        private const string ShotDir = "Temp/cc_shots";

        private static double _t0;
        private static int _stage;
        private static string _log = "";
        private static bool _armed;
        private static bool _driving;

        static AutoCapture()
        {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>
        /// Watches for a request file. Runs on the editor tick, so it works with the window in
        /// the background — no need for anyone to click into Unity.
        ///
        /// Two steps on purpose: the first sighting imports any edited scripts (which reloads
        /// the domain and wipes static state, hence the marker file on disk), and only once
        /// compilation has settled does play mode start. Otherwise the capture would run
        /// against the previously compiled build.
        /// </summary>
        private static void Poll()
        {
            if (_armed || _driving || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            try
            {
                if (File.Exists(RequestFile))
                {
                    File.Delete(RequestFile);
                    if (File.Exists(DoneFile)) File.Delete(DoneFile);
                    if (File.Exists(LogFile)) File.Delete(LogFile);
                    Directory.CreateDirectory(ShotDir);
                    File.WriteAllText(ArmedFile, "armed");
                    AssetDatabase.Refresh();
                    return;
                }

                if (!File.Exists(ArmedFile)) return;
                File.Delete(ArmedFile);
            }
            catch (Exception)
            {
                return;
            }

            _log = "";
            _armed = true;
            TrySetPortraitGameView();
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && _armed)
            {
                _armed = false;
                _driving = true;
                _t0 = EditorApplication.timeSinceStartup;
                _stage = 0;
                Application.logMessageReceived += OnLog;
                EditorApplication.update += Drive;
            }
            else if (change == PlayModeStateChange.EnteredEditMode && _driving)
            {
                _driving = false;
                EditorApplication.update -= Drive;
                Application.logMessageReceived -= OnLog;
                File.WriteAllText(DoneFile, "done\n" + _log);
            }
        }

        private static void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Log) return;
            _log += type + ": " + condition + "\n" + stack + "\n";
            try { File.WriteAllText(LogFile, _log); }
            catch (Exception) { }
        }

        /// <summary>Scripted playthrough: splash, lobby, role card, then a live round.</summary>
        private static void Drive()
        {
            if (!Application.isPlaying) return;
            double t = EditorApplication.timeSinceStartup - _t0;

            try
            {
                switch (_stage)
                {
                    case 0 when t > 1.6: Shot("01_loading"); _stage++; break;
                    case 1 when t > 2.6: Invoke("GoToTitle"); _stage++; break;
                    case 2 when t > 3.6: Shot("02_title"); _stage++; break;
                    case 3 when t > 4.2: Invoke("OpenLobby"); _stage++; break;
                    case 4 when t > 8.0: Shot("03_lobby"); _stage++; break;
                    case 5 when t > 8.6: Invoke("RequestStart"); _stage++; break;
                    case 6 when t > 10.5: Shot("04_role"); _stage++; break;
                    case 7 when t > 15.5: Shot("05_round"); _stage++; break;
                    case 8 when t > 19.0: Shot("06_round_b"); _stage++; break;
                    // Cut the clock short so the meeting half of the match can be checked too.
                    case 9 when t > 20.0: ForceRoundEnd(); _stage++; break;
                    case 10 when t > 22.5: Shot("07_evidence"); _stage++; break;
                    case 11 when t > 23.5: Invoke("BeginVoting"); _stage++; break;
                    case 12 when t > 25.5: Shot("08_voting"); _stage++; break;
                    case 13 when t > 26.5: CastVote(); _stage++; break;
                    case 14 when t > 31.0: Shot("09_result"); _stage++; break;
                    case 15 when t > 33.0:
                        _stage++;
                        File.WriteAllText(LogFile, _log);
                        EditorApplication.ExitPlaymode();
                        break;
                }
            }
            catch (Exception e)
            {
                _log += "DRIVER: " + e + "\n";
                try { File.WriteAllText(LogFile, _log); }
                catch (Exception) { }
                _stage = 99;
                EditorApplication.ExitPlaymode();
            }
        }

        private static void Invoke(string method)
        {
            var director = UnityEngine.Object.FindAnyObjectByType<GameDirector>();
            if (director == null)
            {
                _log += "DRIVER: no GameDirector in scene\n";
                return;
            }
            MethodInfo mi = typeof(GameDirector).GetMethod(method,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi == null)
            {
                _log += "DRIVER: missing method " + method + "\n";
                return;
            }
            mi.Invoke(director, null);
        }

        private static void ForceRoundEnd()
        {
            var director = UnityEngine.Object.FindAnyObjectByType<GameDirector>();
            if (director == null || director.State == null)
            {
                _log += "DRIVER: cannot end round, no match state\n";
                return;
            }
            director.State.TimeLeft = 0.2f;
        }

        private static void CastVote()
        {
            var director = UnityEngine.Object.FindAnyObjectByType<GameDirector>();
            if (director == null || director.State == null) return;
            foreach (PlayerState p in director.State.Players)
            {
                if (p.IsLocal) continue;
                director.SubmitVote(p.Id);
                return;
            }
        }

        private static void Shot(string name)
        {
            string path = Path.GetFullPath(Path.Combine(ShotDir, name + ".png"));
            ScreenCapture.CaptureScreenshot(path);
        }

        /// <summary>
        /// Best effort: point the Game view at a 1080x1920 portrait size so captures match the
        /// target device. Uses internal editor API, so failure is tolerated silently.
        /// </summary>
        private static void TrySetPortraitGameView()
        {
            try
            {
                Assembly ed = typeof(UnityEditor.Editor).Assembly;
                Type sizesType = ed.GetType("UnityEditor.GameViewSizes");
                Type singleton = ed.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
                object sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)
                    .GetValue(null);
                object group = sizesType.GetMethod("GetGroup")
                    .Invoke(sizes, new object[] { (int)GameViewSizeGroupType.Standalone });

                Type groupType = ed.GetType("UnityEditor.GameViewSizeGroup");
                int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
                MethodInfo getSize = groupType.GetMethod("GetGameViewSize");

                Type sizeType = ed.GetType("UnityEditor.GameViewSize");
                PropertyInfo wProp = sizeType.GetProperty("width");
                PropertyInfo hProp = sizeType.GetProperty("height");

                int index = -1;
                for (int i = 0; i < total; i++)
                {
                    object s = getSize.Invoke(group, new object[] { i });
                    if ((int)wProp.GetValue(s) == 1080 && (int)hProp.GetValue(s) == 1920)
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0)
                {
                    Type sizeTypeEnum = ed.GetType("UnityEditor.GameViewSizeType");
                    ConstructorInfo ctor = sizeType.GetConstructor(new[]
                    {
                        sizeTypeEnum, typeof(int), typeof(int), typeof(string)
                    });
                    object newSize = ctor.Invoke(new object[]
                    {
                        Enum.Parse(sizeTypeEnum, "FixedResolution"), 1080, 1920, "ChaosCrew Portrait"
                    });
                    groupType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                    index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                }

                Type gameViewType = ed.GetType("UnityEditor.GameView");
                EditorWindow window = EditorWindow.GetWindow(gameViewType, false, "Game", false);
                gameViewType.GetMethod("SizeSelectionCallback",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(window, new object[] { index, null });
            }
            catch (Exception e)
            {
                _log += "GAMEVIEW: " + e.Message + "\n";
            }
        }
    }
}
#endif
