using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EulersFuneral.Core.Data
{
    [Serializable]
    public class SaveProgressData
    {
        public List<int> CompletedLevels = new List<int>();
        public bool HasSeenInitialHelp = false;
        public float MasterVolume = 0.8f;
        public float MouseSensitivity = 1.0f;
    }

    public static class SaveSystem
    {
        private const string SaveFileName = "eulers_funeral_progress.json";
        private static SaveProgressData _cachedData;

        public static string SaveFilePath =>
            Path.Combine(Application.persistentDataPath, SaveFileName);

        public static SaveProgressData Load()
        {
            if (_cachedData != null)
                return _cachedData;

            try
            {
                string path = SaveFilePath;
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    _cachedData = JsonUtility.FromJson<SaveProgressData>(json) ?? new SaveProgressData();
                    return _cachedData;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] No se pudo leer el archivo de guardado: {ex.Message}");
            }

            _cachedData = new SaveProgressData();
            return _cachedData;
        }

        public static void Save()
        {
            if (_cachedData == null)
                _cachedData = new SaveProgressData();

            try
            {
                string json = JsonUtility.ToJson(_cachedData, true);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Error al guardar progreso: {ex.Message}");
            }
        }

        public static bool IsLevelCompleted(int levelIndex)
        {
            SaveProgressData data = Load();
            return data.CompletedLevels.Contains(levelIndex);
        }

        /// <summary>
        /// RF-02: El nivel 0 siempre está disponible. El nivel N+1 se desbloquea al completar el N.
        /// </summary>
        public static bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex <= 0)
                return true;

            return IsLevelCompleted(levelIndex - 1);
        }

        public static void MarkLevelCompleted(int levelIndex)
        {
            SaveProgressData data = Load();
            if (!data.CompletedLevels.Contains(levelIndex))
            {
                data.CompletedLevels.Add(levelIndex);
                data.CompletedLevels.Sort();
                Save();
            }
        }

        public static void MarkInitialHelpSeen()
        {
            SaveProgressData data = Load();
            if (!data.HasSeenInitialHelp)
            {
                data.HasSeenInitialHelp = true;
                Save();
            }
        }

        public static void ResetProgress()
        {
            _cachedData = new SaveProgressData();
            Save();
        }
    }
}
