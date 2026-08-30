using System.IO;
using UnityEngine;

namespace GameIdle
{
    public static class SaveSystem
    {
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "gameidle_save.json");

        private const string BackupKey = "GameIdle_Save_Backup";

        // flushPrefs: PlayerPrefs.Save() trava a thread principal. So vale a pena
        // quando o app esta indo para segundo plano ou fechando.
        public static void Save(bool flushPrefs = false)
        {
            try
            {
                SaveData data = GameManager.Instance.GetSaveData();
                string json = JsonUtility.ToJson(data, false);
                File.WriteAllText(SavePath, json);
                PlayerPrefs.SetString(BackupKey, json);
                if (flushPrefs) PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Erro ao salvar: {e.Message}");
            }
        }

        public static void Load()
        {
            try
            {
                string json = null;

                if (File.Exists(SavePath))
                    json = File.ReadAllText(SavePath);
                else if (PlayerPrefs.HasKey(BackupKey))
                    json = PlayerPrefs.GetString(BackupKey);

                if (string.IsNullOrEmpty(json)) return;

                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return;

                GameManager.Instance.ApplySaveData(data);
                CharacterManager.Instance.ApplySaveData(data.characters);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Erro ao carregar: {e.Message}");
            }
        }

        public static void Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            PlayerPrefs.DeleteKey(BackupKey);
            PlayerPrefs.Save();
        }
    }
}
