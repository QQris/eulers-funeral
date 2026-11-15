using System.IO;
using EulersFuneral.Core.Data;
using EulersFuneral.Gameplay;
using EulersFuneral.Graphics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace EulersFuneral.Editor
{
    /// <summary>
    /// Genera y guarda en disco los assets persistentes (Assets/Levels/, Assets/Materials/)
    /// y las dos escenas dedicadas (Assets/Scenes/MainMenu.unity y Assets/Scenes/Gameplay.unity)
    /// integrando el fondo Assets/Transitions/Background.png y los videos Manim Assets/Transitions/Nivel0..5.mp4.
    /// </summary>
    [InitializeOnLoad]
    public static class EulersFuneralSceneBaker
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";
        private const string LevelsDir = "Assets/Levels";
        private const string MaterialsDir = "Assets/Materials";
        private const string TransitionsDir = "Assets/Transitions";
        private const string BakeVersionSentinelPath = "Assets/Scenes/.baked_manim_v6";

        static EulersFuneralSceneBaker()
        {
            EditorApplication.delayCall += AutoBakeIfMissing;
        }

        private static void AutoBakeIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            LevelData lvl0 = AssetDatabase.LoadAssetAtPath<LevelData>($"{LevelsDir}/Level_0.asset");
            bool needsBake = !File.Exists(MainMenuScenePath)
                || !File.Exists(GameplayScenePath)
                || lvl0 == null
                || lvl0.TitleVideoClip == null
                || !File.Exists(BakeVersionSentinelPath);

            if (needsBake)
            {
                BakeAllScenesAndAssets();
            }
        }

        [MenuItem("Euler's Funeral/Regenerar Escenas y Assets (MainMenu + Gameplay)")]
        public static void BakeAllScenesAndAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[EulersFuneralSceneBaker] Detén el modo Play antes de regenerar las escenas.");
                return;
            }

            EnsureDirectory("Assets/Scenes");
            EnsureDirectory(LevelsDir);
            EnsureDirectory(MaterialsDir);

            // 1. Crear o actualizar Materiales en Assets/Materials/
            Material ballWhiteMat = CreateOrUpdateMaterial($"{MaterialsDir}/BallWhite.mat", new Color(0.15f, 0.55f, 1.0f, 1.0f));
            Material starRedMat = CreateOrUpdateMaterial($"{MaterialsDir}/StarRed.mat", new Color(1.0f, 0.18f, 0.18f, 1.0f));
            Material wireGrayMat = CreateOrUpdateMaterial($"{MaterialsDir}/WireframeGray.mat", new Color(0.75f, 0.75f, 0.75f, 1.0f));

            // 2. Cargar Fondo y Videos Manim desde Assets/Transitions/
            Texture2D backgroundTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TransitionsDir}/Background.png");
            var levelVideos = new VideoClip[LevelDefinitions.TotalLevels];
            for (int i = 0; i < LevelDefinitions.TotalLevels; i++)
            {
                // En Linux, el VideoClipImporter de Unity soporta nativamente VP8 (.webm),
                // mientras que en Windows/macOS soporta tanto .mp4 como .webm.
                VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>($"{TransitionsDir}/Nivel{i}.webm");
                if (clip == null)
                {
                    clip = AssetDatabase.LoadAssetAtPath<VideoClip>($"{TransitionsDir}/Nivel{i}.mp4");
                }
                levelVideos[i] = clip;
            }

            // 3. Crear o actualizar LevelData ScriptableObjects en Assets/Levels/
            LevelData[] runtimeLevels = LevelDefinitions.CreateAllLevels();
            var levelAssets = new LevelData[runtimeLevels.Length];

            for (int i = 0; i < runtimeLevels.Length; i++)
            {
                runtimeLevels[i].TitleVideoClip = levelVideos[i];
                string assetPath = $"{LevelsDir}/Level_{i}.asset";
                LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(runtimeLevels[i], assetPath);
                    levelAssets[i] = runtimeLevels[i];
                }
                else
                {
                    EditorUtility.CopySerialized(runtimeLevels[i], existing);
                    existing.TitleVideoClip = levelVideos[i];
                    EditorUtility.SetDirty(existing);
                    levelAssets[i] = existing;
                }
            }

            AssetDatabase.SaveAssets();

            // 4. Construir y guardar Assets/Scenes/Gameplay.unity
            Scene gameplayScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EulersFuneralBootstrapper.BuildGameplaySceneHierarchy(
                levelAssets, ballWhiteMat, starRedMat, wireGrayMat, levelVideos);
            EditorSceneManager.MarkSceneDirty(gameplayScene);
            EditorSceneManager.SaveScene(gameplayScene, GameplayScenePath);

            // 5. Construir y guardar Assets/Scenes/MainMenu.unity
            Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EulersFuneralBootstrapper.BuildMainMenuSceneHierarchy(levelAssets, backgroundTex);
            EditorSceneManager.MarkSceneDirty(mainMenuScene);
            EditorSceneManager.SaveScene(mainMenuScene, MainMenuScenePath);

            // 6. Registrar ambas escenas en Build Settings (0: MainMenu, 1: Gameplay)
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };

            File.WriteAllText(BakeVersionSentinelPath, "manim_v6");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[EulersFuneralSceneBaker] Escenas 'MainMenu.unity' y 'Gameplay.unity' actualizadas con Background.png y transiciones Manim (Nivel0..5.mp4).");
        }

        private static Material CreateOrUpdateMaterial(string assetPath, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing == null)
            {
                Material mat = MaterialHelper.CreateUnlitColorMaterial(color);
                AssetDatabase.CreateAsset(mat, assetPath);
                return mat;
            }

            existing.color = color;
            if (existing.HasProperty("_Color")) existing.SetColor("_Color", color);
            if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
