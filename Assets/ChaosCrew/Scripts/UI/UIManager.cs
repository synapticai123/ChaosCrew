using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ChaosCrew
{
    /// <summary>Base for a full-screen page. Each one builds its own widgets once.</summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        public RectTransform Root { get; private set; }
        protected GameDirector D;

        public void Init(RectTransform root, GameDirector director)
        {
            Root = root;
            D = director;
            Build();
            SetVisible(false);
        }

        protected abstract void Build();

        public virtual void OnShow() { }

        public void SetVisible(bool visible)
        {
            if (Root == null) return;
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
            if (visible) OnShow();
        }
    }

    /// <summary>
    /// Builds the canvas and owns every screen. Portrait reference resolution with
    /// match-width scaling, so layouts hold from a small phone to a tablet.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        public RectTransform ScreenLayer { get; private set; }
        public RectTransform OverlayLayer { get; private set; }

        public LoadingScreen Loading { get; private set; }
        public TitleScreen Title { get; private set; }
        public SettingsScreen SettingsScreen { get; private set; }
        public LobbyScreen Lobby { get; private set; }
        public RoleRevealScreen RoleReveal { get; private set; }
        public HudScreen Hud { get; private set; }
        public EvidenceScreen EvidenceScreen { get; private set; }
        public VotingScreen Voting { get; private set; }
        public ResultScreen Result { get; private set; }

        private GameDirector _director;
        private CanvasScaler _scaler;

        public static UIManager Create(GameDirector director)
        {
            var go = new GameObject("ChaosCrew UI");
            go.transform.SetParent(director.transform, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = CCConfig.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // width drives the scale: the map keeps its feel

            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            var ui = go.AddComponent<UIManager>();
            ui._scaler = scaler;
            ui.BuildLayers(director, (RectTransform)go.transform);
            return ui;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        private void BuildLayers(GameDirector director, RectTransform canvasRt)
        {
            _director = director;

            ScreenLayer = UIKit.NewRect("ScreenLayer", canvasRt);
            UIKit.Stretch(ScreenLayer);

            OverlayLayer = UIKit.NewRect("OverlayLayer", canvasRt);
            UIKit.Stretch(OverlayLayer);

            Title = Make<TitleScreen>("TitleScreen");
            Loading = Make<LoadingScreen>("LoadingScreen");
            SettingsScreen = Make<SettingsScreen>("SettingsScreen");
            Lobby = Make<LobbyScreen>("LobbyScreen");
            RoleReveal = Make<RoleRevealScreen>("RoleRevealScreen");
            Hud = Make<HudScreen>("HudScreen");
            EvidenceScreen = Make<EvidenceScreen>("EvidenceScreen");
            Voting = Make<VotingScreen>("VotingScreen");
            Result = Make<ResultScreen>("ResultScreen");
        }

        private T Make<T>(string name) where T : ScreenBase
        {
            RectTransform rt = UIKit.NewRect(name, ScreenLayer);
            UIKit.Stretch(rt);
            var screen = rt.gameObject.AddComponent<T>();
            screen.Init(rt, _director);
            return screen;
        }

        /// <summary>
        /// The layout is authored for portrait. On a window that is relatively wider than the
        /// reference — a landscape Game view, say — matching width would push the top and
        /// bottom bars off screen, so match height instead and let the sides breathe.
        /// </summary>
        private void Update()
        {
            if (_scaler == null || Screen.height <= 0) return;
            float reference = CCConfig.ReferenceResolution.x / CCConfig.ReferenceResolution.y;
            float actual = Screen.width / (float)Screen.height;
            _scaler.matchWidthOrHeight = actual > reference ? 1f : 0f;
        }

        /// <summary>Switches the visible page. The world layer stays up during a round.</summary>
        public void Show(GamePhase phase)
        {
            Loading.SetVisible(phase == GamePhase.Loading);
            Title.SetVisible(phase == GamePhase.Title);
            SettingsScreen.SetVisible(phase == GamePhase.Settings);
            Lobby.SetVisible(phase == GamePhase.Lobby);
            RoleReveal.SetVisible(phase == GamePhase.RoleReveal);
            Hud.SetVisible(phase == GamePhase.Round);
            EvidenceScreen.SetVisible(phase == GamePhase.Evidence);
            Voting.SetVisible(phase == GamePhase.Voting);
            Result.SetVisible(phase == GamePhase.Result);
        }
    }
}
