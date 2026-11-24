using System.Globalization;
using EulersFuneral.Audio;
using EulersFuneral.Core.Data;
using EulersFuneral.Gameplay;
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
        public Button[] LevelButtons = new Button[LevelData.TotalLevels];
        public TextMeshProUGUI[] LevelButtonLabels = new TextMeshProUGUI[LevelData.TotalLevels];
        public Button BackFromLevelSelectButton;

        [Header("Ajustes (RF-01)")]
        public Slider VolumeSlider;
        public TextMeshProUGUI VolumeValueText;
        public Slider SensitivitySlider;
        public TextMeshProUGUI SensitivityValueText;
        public Button BackFromSettingsButton;

        [Header("Referencia Gameplay (Opcional)")]
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
            if (ConfiguredLevels == null)
                return;

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
            for (int i = 0; i < LevelData.TotalLevels; i++)
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
