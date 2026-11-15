using System.Globalization;
using EulersFuneral.Audio;
using EulersFuneral.Core.Data;
using EulersFuneral.Gameplay;
using EulersFuneral.Graphics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EulersFuneral.UI
{
    /// <summary>
    /// RF-01 & RF-02:
    /// Menú principal de Euler's Funeral (Jugar, Selección de niveles, Ajustes, Salir)
    /// y selector de niveles 0 a 5 con estados (Bloqueado, Disponible, Completado).
    /// Soporta tanto escena dedicada (MainMenu.unity -> Gameplay.unity) como escena única.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Paneles Principales")]
        public GameObject MainMenuPanel;
        public GameObject LevelSelectPanel;
        public GameObject SettingsPanel;

        [Header("Botones Menú Principal (RF-01)")]
        public Button PlayButton;
        public Button LevelSelectButton;
        public Button SettingsButton;
        public Button QuitButton;

        [Header("Selección de Niveles (RF-02)")]
        public LevelData[] ConfiguredLevels;
        public Button[] LevelButtons = new Button[LevelDefinitions.TotalLevels];
        public TextMeshProUGUI[] LevelButtonLabels = new TextMeshProUGUI[LevelDefinitions.TotalLevels];
        public Button BackFromLevelSelectButton;

        [Header("Ajustes (RF-01)")]
        public Slider VolumeSlider;
        public TextMeshProUGUI VolumeValueText;
        public Slider SensitivitySlider;
        public TextMeshProUGUI SensitivityValueText;
        public Button BackFromSettingsButton;

        [Header("Referencia Gameplay (Opcional si está en escena separada)")]
        public LevelManager LevelManager;
        public SceneFadeTransition SceneFader;

        private bool _bindingsInitialized;

        private void Start()
        {
            InitializeBindings();
            ShowMainMenu();
        }

        public void InitializeBindings()
        {
            if (_bindingsInitialized)
                return;
            _bindingsInitialized = true;

            if (ConfiguredLevels == null || ConfiguredLevels.Length == 0)
            {
                ConfiguredLevels = LevelDefinitions.CreateAllLevels();
            }

            EnsureSettingsControlsExist();

            if (PlayButton != null)
                PlayButton.onClick.AddListener(OnPlayClicked);

            if (LevelSelectButton != null)
                LevelSelectButton.onClick.AddListener(ShowLevelSelect);

            if (SettingsButton != null)
                SettingsButton.onClick.AddListener(ShowSettings);

            if (QuitButton != null)
                QuitButton.onClick.AddListener(OnQuitClicked);

            if (BackFromLevelSelectButton != null)
                BackFromLevelSelectButton.onClick.AddListener(ShowMainMenu);

            if (BackFromSettingsButton != null)
                BackFromSettingsButton.onClick.AddListener(ShowMainMenu);

            SaveProgressData saved = SaveSystem.Load();
            if (VolumeSlider != null)
            {
                VolumeSlider.minValue = 0.0f;
                VolumeSlider.maxValue = 1.0f;
                VolumeSlider.SetValueWithoutNotify(saved.MasterVolume);
                VolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                UpdateVolumeLabel(saved.MasterVolume);
            }

            if (SensitivitySlider != null)
            {
                SensitivitySlider.minValue = 0.2f;
                SensitivitySlider.maxValue = 3.0f;
                SensitivitySlider.SetValueWithoutNotify(saved.MouseSensitivity);
                SensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
                UpdateSensitivityLabel(saved.MouseSensitivity);
            }

            for (int i = 0; i < LevelButtons.Length; i++)
            {
                int capturedIndex = i;
                if (LevelButtons[i] != null)
                {
                    LevelButtons[i].onClick.AddListener(() => LaunchLevelFromSelector(capturedIndex));
                }
            }
        }

        public void EnsureSettingsControlsExist()
        {
            if (SettingsPanel == null)
                return;

            // Ajustar el bloque de texto de controles para dejar espacio superior a los sliders de Música y Sensibilidad
            Transform controlsInfoTr = SettingsPanel.transform.Find("ControlsInfo");
            if (controlsInfoTr != null)
            {
                RectTransform rt = controlsInfoTr.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.078f, 0.20f);
                    rt.anchorMax = new Vector2(0.84f, 0.54f);
                }
            }

            if (VolumeSlider == null)
            {
                VolumeSlider = CreateStyledSliderRow(
                    SettingsPanel.transform,
                    "VolumeSliderRow",
                    "Volumen de Música y Efectos",
                    new Vector2(0.078f, 0.69f),
                    new Vector2(0.72f, 0.78f),
                    0.0f, 1.0f,
                    SaveSystem.Load().MasterVolume,
                    out VolumeValueText);
            }

            if (SensitivitySlider == null)
            {
                SensitivitySlider = CreateStyledSliderRow(
                    SettingsPanel.transform,
                    "SensitivitySliderRow",
                    "Sensibilidad de Cámara (Ratón)",
                    new Vector2(0.078f, 0.57f),
                    new Vector2(0.72f, 0.66f),
                    0.2f, 3.0f,
                    SaveSystem.Load().MouseSensitivity,
                    out SensitivityValueText);
            }
        }

        public static Slider CreateStyledSliderRow(
            Transform parent,
            string rowName,
            string title,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float minVal,
            float maxVal,
            float initialVal,
            out TextMeshProUGUI valueText)
        {
            var rowGo = new GameObject(rowName);
            rowGo.transform.SetParent(parent, false);
            var rowRt = rowGo.AddComponent<RectTransform>();
            rowRt.anchorMin = anchorMin;
            rowRt.anchorMax = anchorMax;
            rowRt.offsetMin = Vector2.zero;
            rowRt.offsetMax = Vector2.zero;

            // Etiqueta izquierda
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(rowGo.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.42f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            RuntimeFontHelper.EnsureFont(labelTmp);
            labelTmp.text = title;
            labelTmp.fontSize = 23f;
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            labelTmp.raycastTarget = false;

            // Contenedor del Slider
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(rowGo.transform, false);
            var sliderRt = sliderGo.AddComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.44f, 0.25f);
            sliderRt.anchorMax = new Vector2(0.86f, 0.75f);
            sliderRt.offsetMin = Vector2.zero;
            sliderRt.offsetMax = Vector2.zero;

            // Fondo de la barra
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.35f);
            bgRt.anchorMax = new Vector2(1f, 0.65f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.22f, 0.22f, 0.22f, 1f);

            // Área de relleno
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.35f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.65f);
            fillAreaRt.offsetMin = Vector2.zero;
            fillAreaRt.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.20f, 0.60f, 1.0f, 1.0f);

            // Área del Handle
            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = Vector2.zero;
            handleAreaRt.offsetMax = Vector2.zero;

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(18f, 28f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = Color.white;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = minVal;
            slider.maxValue = maxVal;
            slider.SetValueWithoutNotify(initialVal);

            // Texto de valor numérico a la derecha
            var valGo = new GameObject("ValueText");
            valGo.transform.SetParent(rowGo.transform, false);
            var valRt = valGo.AddComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.88f, 0f);
            valRt.anchorMax = new Vector2(1f, 1f);
            valRt.offsetMin = Vector2.zero;
            valRt.offsetMax = Vector2.zero;
            valueText = valGo.AddComponent<TextMeshProUGUI>();
            RuntimeFontHelper.EnsureFont(valueText);
            valueText.fontSize = 22f;
            valueText.alignment = TextAlignmentOptions.Left;
            valueText.color = Color.white;
            valueText.raycastTarget = false;

            return slider;
        }

        public void ShowMainMenu()
        {
            gameObject.SetActive(true);
            if (MainMenuPanel != null) MainMenuPanel.SetActive(true);
            if (LevelSelectPanel != null) LevelSelectPanel.SetActive(false);
            if (SettingsPanel != null) SettingsPanel.SetActive(false);
            if (LevelManager != null && LevelManager.GameplayHudRoot != null)
                LevelManager.GameplayHudRoot.SetActive(false);

            if (ProceduralAudioSystem.Instance != null)
            {
                ProceduralAudioSystem.Instance.StartGlobalMusicPlaylistIfNeeded();
            }

            if (SceneFader != null && MainMenuPanel != null)
            {
                SceneFader.AnimatePanelOpacityIn(MainMenuPanel, 0.45f);
            }
        }

        public void ShowLevelSelect()
        {
            if (MainMenuPanel != null) MainMenuPanel.SetActive(false);
            if (LevelSelectPanel != null) LevelSelectPanel.SetActive(true);
            if (SettingsPanel != null) SettingsPanel.SetActive(false);

            RefreshLevelSelectButtons();

            if (SceneFader != null && LevelSelectPanel != null)
            {
                SceneFader.AnimatePanelOpacityIn(LevelSelectPanel, 0.45f);
            }
        }

        public void ShowSettings()
        {
            EnsureSettingsControlsExist();

            if (MainMenuPanel != null) MainMenuPanel.SetActive(false);
            if (LevelSelectPanel != null) LevelSelectPanel.SetActive(false);
            if (SettingsPanel != null) SettingsPanel.SetActive(true);

            SaveProgressData data = SaveSystem.Load();
            if (VolumeSlider != null)
            {
                VolumeSlider.SetValueWithoutNotify(data.MasterVolume);
                UpdateVolumeLabel(data.MasterVolume);
            }
            if (SensitivitySlider != null)
            {
                SensitivitySlider.SetValueWithoutNotify(data.MouseSensitivity);
                UpdateSensitivityLabel(data.MouseSensitivity);
            }

            if (SceneFader != null && SettingsPanel != null)
            {
                SceneFader.AnimatePanelOpacityIn(SettingsPanel, 0.45f);
            }
        }

        public void RefreshLevelSelectButtons()
        {
            if (ConfiguredLevels == null || ConfiguredLevels.Length == 0)
                ConfiguredLevels = LevelDefinitions.CreateAllLevels();

            for (int i = 0; i < ConfiguredLevels.Length && i < LevelButtons.Length; i++)
            {
                bool completed = SaveSystem.IsLevelCompleted(i);
                bool unlocked = SaveSystem.IsLevelUnlocked(i);

                string stateLabel = completed
                    ? "<color=#73E68C>[Completado]</color>"
                    : (unlocked ? "<color=#E6E6E6>[Disponible]</color>" : "<color=#777777>[Bloqueado]</color>");

                if (LevelButtons[i] != null)
                {
                    LevelButtons[i].interactable = unlocked;
                }

                if (LevelButtonLabels[i] != null && ConfiguredLevels[i] != null)
                {
                    LevelButtonLabels[i].text = $"{ConfiguredLevels[i].LevelName}   {stateLabel}";
                }
            }
        }

        private void OnPlayClicked()
        {
            int targetLevel = 0;
            for (int i = 0; i < LevelDefinitions.TotalLevels; i++)
            {
                if (SaveSystem.IsLevelUnlocked(i) && !SaveSystem.IsLevelCompleted(i))
                {
                    targetLevel = i;
                    break;
                }
            }
            LaunchLevelFromSelector(targetLevel);
        }

        public void LaunchLevelFromSelector(int levelIndex)
        {
            if (!SaveSystem.IsLevelUnlocked(levelIndex))
                return;

            GameSessionContext.SelectedLevelIndex = levelIndex;

            if (ProceduralAudioSystem.Instance != null)
            {
                ProceduralAudioSystem.Instance.StartGlobalMusicPlaylistIfNeeded();
            }

            if (LevelManager != null)
            {
                if (MainMenuPanel != null) MainMenuPanel.SetActive(false);
                if (LevelSelectPanel != null) LevelSelectPanel.SetActive(false);
                if (SettingsPanel != null) SettingsPanel.SetActive(false);
                LevelManager.StartLevel(levelIndex);
            }
            else if (SceneFader != null)
            {
                SceneFader.TransitionToScene(GameSessionContext.GameplaySceneName);
            }
            else
            {
                SceneManager.LoadScene(GameSessionContext.GameplaySceneName);
            }
        }

        private void OnVolumeChanged(float val)
        {
            UpdateVolumeLabel(val);
            if (ProceduralAudioSystem.Instance != null)
            {
                ProceduralAudioSystem.Instance.SetMasterVolume(val);
            }
            else
            {
                SaveProgressData data = SaveSystem.Load();
                data.MasterVolume = val;
                SaveSystem.Save();
            }
        }

        private void OnSensitivityChanged(float val)
        {
            float clamped = Mathf.Clamp(val, 0.2f, 3.0f);
            UpdateSensitivityLabel(clamped);
            SaveProgressData data = SaveSystem.Load();
            data.MouseSensitivity = clamped;
            SaveSystem.Save();
        }

        private void UpdateVolumeLabel(float val)
        {
            if (VolumeValueText != null)
            {
                VolumeValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void UpdateSensitivityLabel(float val)
        {
            if (SensitivityValueText != null)
            {
                SensitivityValueText.text = string.Format(CultureInfo.InvariantCulture, "{0:F1}x", val);
            }
        }

        private void OnQuitClicked()
        {
            Application.Quit();
        }
    }
}
