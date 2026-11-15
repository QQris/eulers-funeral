using EulersFuneral.Audio;
using EulersFuneral.Core.Data;
using EulersFuneral.Graphics;
using EulersFuneral.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace EulersFuneral.Gameplay
{
    /// <summary>
    /// Constructor de escenas para Euler's Funeral.
    /// Provee los métodos para construir las escenas dedicadas (MainMenu.unity y Gameplay.unity)
    /// y actúa como respaldo únicamente si se abre una escena vacía sin controladores.
    /// </summary>
    public class EulersFuneralBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnLoad()
        {
            // Si la escena ya tiene MainMenuController (MainMenu.unity) o LevelManager (Gameplay.unity),
            // usar directamente los objetos pre-creados en la escena sin generar nada por código.
            if (Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include) != null ||
                Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            var bootstrapGo = new GameObject("EulersFuneral_System");
            bootstrapGo.AddComponent<EulersFuneralBootstrapper>().BuildCompleteScene();
        }

        public static MainMenuController BuildMainMenuSceneHierarchy(
            LevelData[] levelAssets,
            Texture2D backgroundTexture = null)
        {
            // 1. Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                mainCam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
            if (mainCam.GetComponent<CRTScreenEffect>() == null)
            {
                mainCam.gameObject.AddComponent<CRTScreenEffect>();
            }

            // 2. Audio System
            if (Object.FindFirstObjectByType<ProceduralAudioSystem>() == null)
            {
                var audioGo = new GameObject("ProceduralAudioSystem");
                audioGo.AddComponent<ProceduralAudioSystem>();
            }

            // 3. EventSystem
            EnsureEventSystem();

            // 4. Canvas
            Canvas canvas = CreateScreenCanvas("MainMenu_Canvas");

            // 5. Menus Hierarchy
            GameObject menuRootGo = CreatePanel(canvas.transform, "MenusRoot", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);
            CanvasGroup menuContentCg = menuRootGo.AddComponent<CanvasGroup>();

            var mainMenuCtrl = menuRootGo.AddComponent<MainMenuController>();
            mainMenuCtrl.ConfiguredLevels = levelAssets;

            PopulateMainMenuPanels(menuRootGo.transform, mainMenuCtrl, backgroundTexture);

            // 6. Scene Fade & Content Opacity Transition
            SceneFadeTransition sceneFader = AttachSceneFadeTransition(canvas.transform, menuContentCg);
            mainMenuCtrl.SceneFader = sceneFader;

            mainMenuCtrl.ShowMainMenu();
            return mainMenuCtrl;
        }

        public static LevelManager BuildGameplaySceneHierarchy(
            LevelData[] levelAssets,
            Material ballWhiteMat = null,
            Material starRedMat = null,
            Material wireGrayMat = null,
            VideoClip[] levelVideos = null)
        {
            // 1. Audio System
            if (Object.FindFirstObjectByType<ProceduralAudioSystem>() == null)
            {
                var audioGo = new GameObject("ProceduralAudioSystem");
                audioGo.AddComponent<ProceduralAudioSystem>();
            }

            // 2. Main Camera & Orbit Controller
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                mainCam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
            if (mainCam.GetComponent<CRTScreenEffect>() == null)
            {
                mainCam.gameObject.AddComponent<CRTScreenEffect>();
            }

            OrbitCameraController orbitCam = mainCam.GetComponent<OrbitCameraController>();
            if (orbitCam == null)
            {
                orbitCam = mainCam.gameObject.AddComponent<OrbitCameraController>();
            }
            orbitCam.ApplyCameraTransform();

            // 3. 3D Graphing Table & Surface Mesh Generator
            var axesGo = new GameObject("GraphingTable_3D");
            var axesVisualizer = axesGo.AddComponent<CoordinateAxesVisualizer>();
            axesVisualizer.RebuildTable(-5f, 5f, -5f, 5f);

            var surfaceGo = new GameObject("SurfaceMesh_100x100");
            var surfaceGen = surfaceGo.AddComponent<SurfaceMeshGenerator>();
            if (wireGrayMat != null)
            {
                surfaceGo.GetComponent<MeshRenderer>().sharedMaterial = wireGrayMat;
            }

            // 4. Blue Ball Sphere (RF-08, Sección 5)
            Color ballBlueColor = new Color(0.15f, 0.55f, 1.0f, 1.0f);
            GameObject ballGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballGo.name = "WhiteBall_Particle";
            RemoveCollider(ballGo);
            Renderer ballRend = ballGo.GetComponent<Renderer>();
            if (ballRend != null)
            {
                ballRend.sharedMaterial = ballWhiteMat != null
                    ? ballWhiteMat
                    : MaterialHelper.CreateUnlitColorMaterial(ballBlueColor);
            }
            var ballController = ballGo.AddComponent<BallController>();

            // 5. Pre-created StartMarker_A and 3 Red Stars visible in Scene Hierarchy
            var entitiesRoot = new GameObject("LevelEntities_3D");

            GameObject startMarkerA = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startMarkerA.name = "StartMarker_A";
            startMarkerA.transform.SetParent(entitiesRoot.transform, false);
            RemoveCollider(startMarkerA);
            startMarkerA.GetComponent<Renderer>().sharedMaterial = ballWhiteMat != null
                ? ballWhiteMat
                : MaterialHelper.CreateUnlitColorMaterial(ballBlueColor);

            var starObjects = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject starGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                starGo.name = $"Star_{i + 1}";
                starGo.transform.SetParent(entitiesRoot.transform, false);
                RemoveCollider(starGo);
                starGo.GetComponent<Renderer>().sharedMaterial = starRedMat != null
                    ? starRedMat
                    : MaterialHelper.CreateUnlitColorMaterial(new Color(1.0f, 0.18f, 0.18f, 1.0f));
                starObjects[i] = starGo;
            }

            // 6. EventSystem & UI Canvas
            EnsureEventSystem();
            Canvas canvas = CreateScreenCanvas("Gameplay_Canvas");

            // 7. LevelManager
            var levelMgrGo = new GameObject("LevelManager");
            var levelMgr = levelMgrGo.AddComponent<LevelManager>();
            levelMgr.ConfiguredLevels = levelAssets;
            levelMgr.AutoStartOnSceneLoad = true;
            levelMgr.SurfaceGenerator = surfaceGen;
            levelMgr.AxesVisualizer = axesVisualizer;
            levelMgr.OrbitCamera = orbitCam;
            levelMgr.Ball = ballController;
            levelMgr.StartMarkerA = startMarkerA;
            levelMgr.StarObjects = starObjects;

            // Position entities initially at Level 0 so they are visible in the Editor Scene View
            if (levelAssets != null && levelAssets.Length > 0 && levelAssets[0] != null)
            {
                levelMgr.PositionLevelEntities(levelAssets[0]);
                ballGo.transform.position = MathCoords.ToUnity(levelAssets[0].StartPointA);
                ballGo.transform.localScale = Vector3.one * (levelAssets[0].BallRadius * 2.0f * BallController.VisualScale);
            }

            PopulateGameplayHud(canvas.transform, levelMgr, levelVideos);
            return levelMgr;
        }

        public void BuildCompleteScene()
        {
            LevelData[] levels = LevelDefinitions.CreateAllLevels();
            LevelManager levelMgr = BuildGameplaySceneHierarchy(levels);
            levelMgr.AutoStartOnSceneLoad = false;

            Canvas canvas = levelMgr.GameplayHudRoot.GetComponentInParent<Canvas>();
            GameObject menuRootGo = CreatePanel(canvas.transform, "MenusRoot", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);
            CanvasGroup menuCg = menuRootGo.AddComponent<CanvasGroup>();
            var mainMenuCtrl = menuRootGo.AddComponent<MainMenuController>();
            mainMenuCtrl.ConfiguredLevels = levels;
            mainMenuCtrl.LevelManager = levelMgr;
            mainMenuCtrl.SceneFader = AttachSceneFadeTransition(canvas.transform, menuCg);

            PopulateMainMenuPanels(menuRootGo.transform, mainMenuCtrl, null);
            mainMenuCtrl.InitializeBindings();
            mainMenuCtrl.ShowMainMenu();
        }

        private static SceneFadeTransition AttachSceneFadeTransition(Transform canvasTransform, CanvasGroup contentCanvasGroup)
        {
            GameObject curtainGo = CreatePanel(canvasTransform, "SceneFadeCurtain", Color.black, Vector2.zero, Vector2.one);
            Image curtainImg = curtainGo.GetComponent<Image>();
            if (curtainImg != null)
            {
                curtainImg.raycastTarget = false;
            }

            CanvasGroup curtainCg = curtainGo.AddComponent<CanvasGroup>();
            curtainCg.alpha = 0f;
            curtainCg.blocksRaycasts = false;
            curtainCg.interactable = false;

            var fader = canvasTransform.gameObject.AddComponent<SceneFadeTransition>();
            fader.ContentCanvasGroup = contentCanvasGroup;
            fader.CurtainOverlayGroup = curtainCg;
            return fader;
        }

        private static void PopulateMainMenuPanels(
            Transform menuRoot,
            MainMenuController mainMenuCtrl,
            Texture2D backgroundTexture)
        {
            // Panel principal con la imagen Assets/Transitions/Background.png (que ya incluye el título "EULER'S FUNERAL")
            GameObject mainPanel = CreatePanel(menuRoot, "MainMenuPanel", Color.black, Vector2.zero, Vector2.one);
            mainPanel.AddComponent<CanvasGroup>();

            var bgGo = new GameObject("BackgroundArtwork");
            bgGo.transform.SetParent(mainPanel.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var bgRawImg = bgGo.AddComponent<RawImage>();
            bgRawImg.texture = backgroundTexture;
            bgRawImg.color = backgroundTexture != null ? Color.white : new Color(0.03f, 0.03f, 0.03f, 1f);
            bgRawImg.raycastTarget = false;

            // Botones alineados a la izquierda debajo del título de Background.png, sin fondo y con animación de subrayado
            mainMenuCtrl.PlayButton = CreateUnderlineMenuButton(
                mainPanel.transform, "PlayBtn", "Jugar",
                new Vector2(0.078f, 0.43f), new Vector2(0.32f, 0.51f),
                fontSize: 30f, align: TextAlignmentOptions.Left, maxUnderlineWidth: 0.52f);

            mainMenuCtrl.LevelSelectButton = CreateUnderlineMenuButton(
                mainPanel.transform, "LevelSelectBtn", "Selección de Niveles",
                new Vector2(0.078f, 0.33f), new Vector2(0.36f, 0.41f),
                fontSize: 30f, align: TextAlignmentOptions.Left, maxUnderlineWidth: 0.88f);

            mainMenuCtrl.SettingsButton = CreateUnderlineMenuButton(
                mainPanel.transform, "SettingsBtn", "Ajustes",
                new Vector2(0.078f, 0.23f), new Vector2(0.32f, 0.31f),
                fontSize: 30f, align: TextAlignmentOptions.Left, maxUnderlineWidth: 0.55f);

            mainMenuCtrl.QuitButton = CreateUnderlineMenuButton(
                mainPanel.transform, "QuitBtn", "Salir",
                new Vector2(0.078f, 0.13f), new Vector2(0.32f, 0.21f),
                fontSize: 30f, align: TextAlignmentOptions.Left, maxUnderlineWidth: 0.48f);

            mainMenuCtrl.MainMenuPanel = mainPanel;

            // Level Select Panel (RF-02) - Fondo negro sólido sin Background.png (el fondo solo es para el mainscreen)
            GameObject levelSelectPanel = CreatePanel(menuRoot, "LevelSelectPanel", new Color(0.03f, 0.03f, 0.03f, 0.98f), Vector2.zero, Vector2.one);
            levelSelectPanel.AddComponent<CanvasGroup>();

            CreateText(levelSelectPanel.transform, "Header", "<b>SELECCIÓN DE NIVELES (0 - 5)</b>", 38, TextAlignmentOptions.Left, new Vector2(0.078f, 0.84f), new Vector2(0.88f, 0.94f));
            mainMenuCtrl.LevelButtons = new Button[LevelDefinitions.TotalLevels];
            mainMenuCtrl.LevelButtonLabels = new TextMeshProUGUI[LevelDefinitions.TotalLevels];

            for (int i = 0; i < LevelDefinitions.TotalLevels; i++)
            {
                float top = 0.79f - i * 0.105f;
                float bottom = top - 0.085f;
                string lvlTitle = (mainMenuCtrl.ConfiguredLevels != null && i < mainMenuCtrl.ConfiguredLevels.Length && mainMenuCtrl.ConfiguredLevels[i] != null)
                    ? mainMenuCtrl.ConfiguredLevels[i].LevelName
                    : $"Nivel {i}";
                Button btn = CreateUnderlineMenuButton(
                    levelSelectPanel.transform, $"LevelBtn_{i}", lvlTitle,
                    new Vector2(0.078f, bottom), new Vector2(0.76f, top),
                    fontSize: 25f, align: TextAlignmentOptions.Left, maxUnderlineWidth: 0.92f);
                mainMenuCtrl.LevelButtons[i] = btn;
                mainMenuCtrl.LevelButtonLabels[i] = btn.GetComponentInChildren<TextMeshProUGUI>();
            }
            mainMenuCtrl.BackFromLevelSelectButton = CreateUnderlineMenuButton(
                levelSelectPanel.transform, "BackBtn", "Volver al Menú",
                new Vector2(0.078f, 0.06f), new Vector2(0.36f, 0.14f),
                fontSize: 26f, align: TextAlignmentOptions.Left, maxUnderlineWidth: 0.65f);
            mainMenuCtrl.LevelSelectPanel = levelSelectPanel;

            // Settings Panel (RF-01) - Fondo negro sólido sin Background.png (el fondo solo es para el mainscreen)
            GameObject settingsPanel = CreatePanel(menuRoot, "SettingsPanel", new Color(0.03f, 0.03f, 0.03f, 0.98f), Vector2.zero, Vector2.one);
            settingsPanel.AddComponent<CanvasGroup>();

            CreateText(settingsPanel.transform, "SettingsHeader", "<b>AJUSTES Y CONTROLES</b>", 38, TextAlignmentOptions.Left, new Vector2(0.078f, 0.82f), new Vector2(0.88f, 0.92f));
            CreateText(settingsPanel.transform, "ControlsInfo", InitialHelpPopup.DefaultHelpMessage, 20, TextAlignmentOptions.Left, new Vector2(0.078f, 0.20f), new Vector2(0.84f, 0.54f));
            mainMenuCtrl.BackFromSettingsButton = CreateUnderlineMenuButton(
                settingsPanel.transform, "BackSettingsBtn", "Volver al Menú",
                new Vector2(0.078f, 0.08f), new Vector2(0.36f, 0.16f),
                fontSize: 26f, align: TextAlignmentOptions.Left, maxUnderlineWidth: 0.65f);
            mainMenuCtrl.SettingsPanel = settingsPanel;
            mainMenuCtrl.EnsureSettingsControlsExist();
        }

        private static void PopulateGameplayHud(
            Transform canvasTransform,
            LevelManager levelMgr,
            VideoClip[] levelVideos)
        {
            GameObject hudRoot = CreatePanel(canvasTransform, "GameplayHUD", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);
            CanvasGroup hudCg = hudRoot.AddComponent<CanvasGroup>();
            levelMgr.GameplayHudRoot = hudRoot;

            // Top Bar (Centrada para evitar el difuminado en las esquinas del efecto CRT)
            GameObject topBar = CreatePanel(hudRoot.transform, "TopBar", new Color(0.05f, 0.05f, 0.05f, 0.88f), new Vector2(0.12f, 0.90f), new Vector2(0.88f, 0.97f));
            levelMgr.LevelHeaderText = CreateText(topBar.transform, "LevelHeader", "Nivel 0: Identidad Diagonal", 23, TextAlignmentOptions.Center, new Vector2(0.02f, 0f), new Vector2(0.38f, 1f));
            levelMgr.StarsCounterText = CreateText(topBar.transform, "StarsCounter", "Estrellas: 0 / 3", 23, TextAlignmentOptions.Center, new Vector2(0.39f, 0f), new Vector2(0.55f, 1f));
            levelMgr.TimerText = CreateText(topBar.transform, "TimerText", "Tiempo: 0.0 / 20 s", 23, TextAlignmentOptions.Center, new Vector2(0.56f, 0f), new Vector2(0.72f, 1f));
            Button toggleHintsBtn = CreateUnderlineMenuButton(topBar.transform, "ToggleHintsBtn", "Pistas (Gradiente)", new Vector2(0.73f, 0.12f), new Vector2(0.86f, 0.88f), 20f, TextAlignmentOptions.Center, 0.88f);
            Button backToMenuBtn = CreateUnderlineMenuButton(topBar.transform, "MenuBtn", "Menú", new Vector2(0.87f, 0.12f), new Vector2(0.98f, 0.88f), 20f, TextAlignmentOptions.Center, 0.85f);
            levelMgr.ReturnToMenuButton = backToMenuBtn;

            // Bottom Formula Bar (RF-06): barra más corta y centrada, con f(x, y) subido arriba
            GameObject bottomBar = CreatePanel(hudRoot.transform, "BottomFormulaBar", new Color(0.05f, 0.05f, 0.05f, 0.92f), new Vector2(0.18f, 0.03f), new Vector2(0.82f, 0.20f));
            CreateText(bottomBar.transform, "FormulaPrefix", "<b><i>f(x, y) =</i></b>", 25, TextAlignmentOptions.Center, new Vector2(0.20f, 0.68f), new Vector2(0.80f, 0.96f));
            TMP_InputField inputField = CreateInputField(bottomBar.transform, "FormulaInputField", "", new Vector2(0.12f, 0.28f), new Vector2(0.56f, 0.66f));
            Button confirmBtn = CreateUnderlineMenuButton(bottomBar.transform, "ConfirmBtn", "Confirmar Malla", new Vector2(0.44f, 0.28f), new Vector2(0.56f, 0.66f), 20f, TextAlignmentOptions.Center, 0.88f);
            Button launchBtn = CreateUnderlineMenuButton(bottomBar.transform, "LaunchBtn", "Lanzar Pelota", new Vector2(0.58f, 0.28f), new Vector2(0.74f, 0.66f), 20f, TextAlignmentOptions.Center, 0.88f);
            Button resetBtn = CreateUnderlineMenuButton(bottomBar.transform, "ResetBtn", "Reiniciar (R)", new Vector2(0.75f, 0.28f), new Vector2(0.91f, 0.66f), 20f, TextAlignmentOptions.Center, 0.88f);
            TextMeshProUGUI validationText = CreateText(bottomBar.transform, "ValidationText", "Escribe una expresión f(x, y), p. ej.: -0.2x - 0.2y", 18, TextAlignmentOptions.Center, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.25f));

            var formulaCtrl = bottomBar.AddComponent<FormulaInputController>();
            formulaCtrl.FormulaInputField = inputField;
            formulaCtrl.ValidationFeedbackText = validationText;
            formulaCtrl.ConfirmFormulaButton = confirmBtn;
            formulaCtrl.LaunchBallButton = launchBtn;
            formulaCtrl.ResetBallButton = resetBtn;
            levelMgr.FormulaInput = formulaCtrl;

            // Hints Panel (RF-07)
            GameObject hintsRoot = CreatePanel(hudRoot.transform, "HintsPanel", new Color(0.08f, 0.08f, 0.08f, 0.92f), new Vector2(0.56f, 0.60f), new Vector2(0.88f, 0.88f));
            TextMeshProUGUI coordsTxt = CreateText(hintsRoot.transform, "CoordsText", "Punto: (--, --)", 18, TextAlignmentOptions.Left, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.95f));
            TextMeshProUGUI gradTxt = CreateText(hintsRoot.transform, "GradientText", "Gradiente grad f: (--, --)", 18, TextAlignmentOptions.Left, new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.72f));
            TextMeshProUGUI slopeTxt = CreateText(hintsRoot.transform, "SlopeText", "Pendiente ||grad f||: --", 18, TextAlignmentOptions.Left, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.50f));
            TextMeshProUGUI conceptTxt = CreateText(hintsRoot.transform, "ConceptualHint", "", 16, TextAlignmentOptions.Left, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.30f));

            var hintsCtrl = hudRoot.AddComponent<HintsController>();
            hintsCtrl.HintsPanelRoot = hintsRoot;
            hintsCtrl.ToggleHintsButton = toggleHintsBtn;
            hintsCtrl.CursorCoordinatesText = coordsTxt;
            hintsCtrl.GradientText = gradTxt;
            hintsCtrl.SlopeText = slopeTxt;
            hintsCtrl.ConceptualHintText = conceptTxt;
            hintsRoot.SetActive(false);
            levelMgr.HintsPanel = hintsCtrl;

            // Gravity Toast (RF-13)
            GameObject toastGo = CreatePanel(hudRoot.transform, "GravityToast", new Color(0.18f, 0.12f, 0.12f, 0.94f), new Vector2(0.30f, 0.82f), new Vector2(0.70f, 0.89f));
            var toastCg = toastGo.AddComponent<CanvasGroup>();
            TextMeshProUGUI toastTxt = CreateText(toastGo.transform, "ToastText", "", 22, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            var gravityToast = toastGo.AddComponent<GravityToast>();
            gravityToast.ToastCanvasGroup = toastCg;
            gravityToast.ToastMessageText = toastTxt;
            toastGo.SetActive(false);
            levelMgr.GravityNotification = gravityToast;

            // Result Panel (Victory / Defeat - RF-12)
            GameObject resultPanel = CreatePanel(hudRoot.transform, "ResultPanel", new Color(0.05f, 0.05f, 0.05f, 0.95f), new Vector2(0.28f, 0.30f), new Vector2(0.72f, 0.72f));
            levelMgr.ResultPanelRoot = resultPanel;
            levelMgr.ResultTitleText = CreateText(resultPanel.transform, "ResultTitle", "", 36, TextAlignmentOptions.Center, new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.92f));
            levelMgr.ResultDetailsText = CreateText(resultPanel.transform, "ResultDetails", "", 22, TextAlignmentOptions.Center, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.66f));
            levelMgr.RetryButton = CreateUnderlineMenuButton(resultPanel.transform, "RetryBtn", "Reintentar", new Vector2(0.08f, 0.10f), new Vector2(0.35f, 0.28f), 22f, TextAlignmentOptions.Center, 0.85f);
            levelMgr.EditFormulaButton = CreateUnderlineMenuButton(resultPanel.transform, "EditBtn", "Editar Fórmula", new Vector2(0.38f, 0.10f), new Vector2(0.65f, 0.28f), 22f, TextAlignmentOptions.Center, 0.85f);
            levelMgr.NextLevelButton = CreateUnderlineMenuButton(resultPanel.transform, "NextBtn", "Siguiente Nivel", new Vector2(0.68f, 0.10f), new Vector2(0.92f, 0.28f), 22f, TextAlignmentOptions.Center, 0.85f);
            resultPanel.SetActive(false);

            // Initial Help Popup (RF-03)
            GameObject helpPopupGo = CreatePanel(hudRoot.transform, "InitialHelpPopup", new Color(0.04f, 0.04f, 0.04f, 0.96f), new Vector2(0.20f, 0.20f), new Vector2(0.80f, 0.82f));
            TextMeshProUGUI helpTxt = CreateText(helpPopupGo.transform, "HelpBody", InitialHelpPopup.DefaultHelpMessage, 21, TextAlignmentOptions.Left, new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.92f));
            Button closeHelpBtn = CreateUnderlineMenuButton(helpPopupGo.transform, "CloseHelpBtn", "Entendido", new Vector2(0.38f, 0.05f), new Vector2(0.62f, 0.16f), 24f, TextAlignmentOptions.Center, 0.8f);
            var helpPopup = helpPopupGo.AddComponent<InitialHelpPopup>();
            helpPopup.PopupRoot = helpPopupGo;
            helpPopup.HelpBodyText = helpTxt;
            helpPopup.CloseButton = closeHelpBtn;
            helpPopupGo.SetActive(false);
            levelMgr.InitialHelp = helpPopup;

            // Manim Video / LaTeX Title Animation Overlay (RF-04)
            GameObject titleOverlayGo = CreatePanel(canvasTransform, "LaTeXTitleOverlay", Color.black, Vector2.zero, Vector2.one);
            var titleCg = titleOverlayGo.AddComponent<CanvasGroup>();

            var videoSurfaceGo = new GameObject("ManimVideoRawImage");
            videoSurfaceGo.transform.SetParent(titleOverlayGo.transform, false);
            var videoRt = videoSurfaceGo.AddComponent<RectTransform>();
            videoRt.anchorMin = Vector2.zero;
            videoRt.anchorMax = Vector2.one;
            videoRt.offsetMin = Vector2.zero;
            videoRt.offsetMax = Vector2.zero;
            var videoRawImage = videoSurfaceGo.AddComponent<RawImage>();
            videoRawImage.color = Color.white;
            videoRawImage.raycastTarget = false;

            var videoPlayer = titleOverlayGo.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;

            TextMeshProUGUI primaryGlyph = CreateText(titleOverlayGo.transform, "PrimaryGlyph", "", 84, TextAlignmentOptions.Center, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.75f));
            TextMeshProUGUI secondaryGlyph = CreateText(titleOverlayGo.transform, "SecondaryGlyph", "", 84, TextAlignmentOptions.Center, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.75f));
            TextMeshProUGUI subtitleGlyph = CreateText(titleOverlayGo.transform, "SubtitleGlyph", "", 26, TextAlignmentOptions.Center, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.30f));

            var titleAnim = titleOverlayGo.AddComponent<LaTeXTitleAnimation>();
            titleAnim.OverlayGroup = titleCg;
            titleAnim.ManimVideoRawImage = videoRawImage;
            titleAnim.VideoPlayerComponent = videoPlayer;
            if (levelVideos != null && levelVideos.Length > 0)
            {
                titleAnim.LevelVideoClips = levelVideos;
            }
            titleAnim.PrimaryGlyphText = primaryGlyph;
            titleAnim.SecondaryGlyphText = secondaryGlyph;
            titleAnim.SubtitleText = subtitleGlyph;
            titleOverlayGo.SetActive(false);
            levelMgr.TitleAnimator = titleAnim;

            // Scene Fade & Content Opacity Transition
            SceneFadeTransition sceneFader = AttachSceneFadeTransition(canvasTransform, hudCg);
            levelMgr.SceneFader = sceneFader;

            formulaCtrl.InitializeBindings();
            hintsCtrl.InitializeBindings();
            helpPopup.InitializeBindings();
            levelMgr.InitializeBindings();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private static Canvas CreateScreenCanvas(string name)
        {
            var canvasGo = new GameObject(name);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Color bgColor, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = bgColor.a > 0.01f;
            return go;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            RuntimeFontHelper.EnsureFont(tmp);
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>
        /// Crea un botón estético sin fondo (transparente) con animación de subrayado inferior al pasar el mouse.
        /// </summary>
        private static Button CreateUnderlineMenuButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize = 26f,
            TextAlignmentOptions align = TextAlignmentOptions.Left,
            float maxUnderlineWidth = 0.85f)
        {
            GameObject panel = CreatePanel(parent, name, new Color(0f, 0f, 0f, 0f), anchorMin, anchorMax);
            Image bgImg = panel.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(0f, 0f, 0f, 0f);
                bgImg.raycastTarget = true;
            }

            var btn = panel.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            TextMeshProUGUI labelTmp = CreateText(
                panel.transform, "Label", label, fontSize, align,
                new Vector2(0f, 0.12f), new Vector2(1f, 1f));
            labelTmp.color = new Color(0.84f, 0.84f, 0.84f, 0.92f);

            var underlineGo = new GameObject("UnderlineBar");
            underlineGo.transform.SetParent(panel.transform, false);
            var uRt = underlineGo.AddComponent<RectTransform>();
            if (align == TextAlignmentOptions.Center)
            {
                uRt.pivot = new Vector2(0.5f, 0f);
                uRt.anchorMin = new Vector2(0.5f, 0.04f);
                uRt.anchorMax = new Vector2(0.5f, 0.04f);
            }
            else
            {
                uRt.pivot = new Vector2(0f, 0f);
                uRt.anchorMin = new Vector2(0f, 0.04f);
                uRt.anchorMax = new Vector2(0f, 0.04f);
            }
            uRt.anchoredPosition = Vector2.zero;
            uRt.sizeDelta = new Vector2(0f, 2.5f);

            var uImg = underlineGo.AddComponent<Image>();
            uImg.color = new Color(1f, 1f, 1f, 0f);
            uImg.raycastTarget = false;

            var effect = panel.AddComponent<UnderlineButtonEffect>();
            effect.LabelText = labelTmp;
            effect.UnderlineRect = uRt;
            effect.UnderlineImage = uImg;
            effect.MaxUnderlineWidthNormalized = maxUnderlineWidth;

            return btn;
        }

        private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject root = CreatePanel(parent, name, new Color(0.12f, 0.12f, 0.12f, 1f), anchorMin, anchorMax);
            TextMeshProUGUI textComp = CreateText(root.transform, "Text", "", 23, TextAlignmentOptions.Center, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f));
            TextMeshProUGUI placeholderComp = CreateText(root.transform, "Placeholder", placeholder, 20, TextAlignmentOptions.Center, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f));
            placeholderComp.color = new Color(0.55f, 0.55f, 0.55f, 0.85f);

            var input = root.AddComponent<TMP_InputField>();
            input.textViewport = root.GetComponent<RectTransform>();
            input.textComponent = textComp;
            input.placeholder = placeholderComp;
            return input;
        }
    }
}
