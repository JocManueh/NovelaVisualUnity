using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaliNature
{
    [Serializable] public sealed class DecisionRecord { public string key, choice, consequence; public bool responsible; }
    [Serializable] public sealed class SaveData
    {
        public int version = 1;
        public List<string> clues = new List<string>();
        public List<string> completedChapters = new List<string>();
        public List<string> heardNarrations = new List<string>();
        public List<DecisionRecord> decisions = new List<DecisionRecord>();
        public float volume = 0.8f, textSpeed = 35f;
        public bool muted, subtitles = true, tutorialSeen;
    }
    public sealed class GameSaveManager : MonoBehaviour
    {
#if UNITY_EDITOR
        // PlayMode tests have their own storage namespace; they never reset the player's save.
        public static string TestSessionId { private get; set; }
        private static string Key => string.IsNullOrEmpty(TestSessionId) ? "CaliNature.Save.v1" : "CaliNature.Tests." + TestSessionId;
#else
        private const string Key = "CaliNature.Save.v1";
#endif
        public SaveData Data { get; private set; } = new SaveData();
        public event Action Changed;
        public bool HasSave => PlayerPrefs.HasKey(Key);
        private void Awake() { Load(); }
        public void Load()
        {
            try
            {
                if (!PlayerPrefs.HasKey(Key)) return;
                SaveData loaded = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
                if (loaded == null || loaded.version != 1) throw new FormatException("Versión de guardado desconocida.");
                loaded.clues ??= new List<string>(); loaded.decisions ??= new List<DecisionRecord>();
                loaded.completedChapters ??= new List<string>(); loaded.heardNarrations ??= new List<string>();
                loaded.volume = Mathf.Clamp01(loaded.volume); loaded.textSpeed = Mathf.Clamp(loaded.textSpeed, 10, 100);
                Data = loaded;
            }
            catch (Exception ex) { Debug.LogWarning("No se pudo recuperar la partida. Se conserva la copia original. " + ex.Message); }
        }
        public void Store() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data)); PlayerPrefs.Save(); Changed?.Invoke(); }
        public bool AddClue(string id) { if (string.IsNullOrEmpty(id) || Data.clues.Contains(id)) return false; Data.clues.Add(id); Store(); return true; }
        public bool HasClue(string id) => string.IsNullOrEmpty(id) || Data.clues.Contains(id);
        public bool RecordDecision(DialogueChoice choice)
        {
            if (string.IsNullOrEmpty(choice.decisionKey) || Data.decisions.Exists(x => x.key == choice.decisionKey)) return false;
            Data.decisions.Add(new DecisionRecord { key = choice.decisionKey, choice = choice.id, consequence = choice.consequence, responsible = choice.responsible });
            Store(); return true;
        }
        public void MarkHeard(string id) { if (!string.IsNullOrEmpty(id) && !Data.heardNarrations.Contains(id)) { Data.heardNarrations.Add(id); Store(); } }
        public void Complete(string id) { if (!Data.completedChapters.Contains(id)) { Data.completedChapters.Add(id); Store(); } }
        public void ResetProgress() { Data = new SaveData { volume = Data.volume, textSpeed = Data.textSpeed, muted = Data.muted, subtitles = Data.subtitles }; Store(); }
        private void OnApplicationPause(bool paused) { if (paused) Store(); }
        private void OnApplicationQuit() { Store(); }
    }
}
