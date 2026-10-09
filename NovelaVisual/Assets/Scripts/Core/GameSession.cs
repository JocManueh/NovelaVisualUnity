using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CaliNature
{
    [DefaultExecutionOrder(-300)]
    [RequireComponent(typeof(GameStateController), typeof(GameInputRouter), typeof(GameSaveManager))]
    [RequireComponent(typeof(DialogueController), typeof(DialogueNarrationPlayer), typeof(CaliGameUI))]
    [RequireComponent(typeof(ChapterCinematicController))]
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        [SerializeField] private TextAsset storyCatalog;
        [SerializeField] private string mapScene = "01_MainMenu";
        public GameStateController State { get; private set; }
        public GameInputRouter Input { get; private set; }
        public GameSaveManager Save { get; private set; }
        public DialogueController Dialogue { get; private set; }
        public DialogueNarrationPlayer Narration { get; private set; }
        public CaliGameUI UI { get; private set; }
        public ChapterCinematicController Cinematic { get; private set; }
        public StoryCatalog Catalog { get; private set; }
        public ChapterData CurrentChapter { get; private set; }
        public ChapterData SelectedChapter { get; private set; }
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                // Escena duplicada (p. ej. al recargar el mapa). Su EventSystem hijo, si llegara a
                // activarse, tomaría activeEventSystem y al destruirse lo dejaría en null, provocando
                // NullReferenceException en PanelEventHandler al procesar navegación. Se apagan antes
                // de destruirse; además su UIDocument crearía un panel que se destruye en el mismo frame.
                foreach (var system in GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)) system.enabled = false;
                foreach (var doc in GetComponentsInChildren<UnityEngine.UIElements.UIDocument>(true)) doc.enabled = false;
                Destroy(gameObject); return;
            }
            Instance = this; DontDestroyOnLoad(gameObject);
            State = GetComponent<GameStateController>(); Input = GetComponent<GameInputRouter>(); Save = GetComponent<GameSaveManager>();
            Dialogue = GetComponent<DialogueController>(); Narration = GetComponent<DialogueNarrationPlayer>(); UI = GetComponent<CaliGameUI>(); Cinematic = GetComponent<ChapterCinematicController>();
            if (!storyCatalog) storyCatalog = Resources.Load<TextAsset>("CaliNature/StoryCatalog");
            try { Catalog = JsonUtility.FromJson<StoryCatalog>(storyCatalog.text); }
            catch (Exception ex) { Debug.LogError("No se pudo leer StoryCatalog: " + ex.Message); }
        }
        private void Start()
        {
            if (Instance != this) return;
            Dialogue.Ended += CheckChapterCompletion;
            State.Changed += ModeChanged;
            UI.RefreshMode(State.Mode);
        }
        private void ModeChanged(GameMode mode) { Narration.SetPaused(mode == GameMode.Pause); }
        private void OnDestroy()
        {
            if (Instance != this) return;
            Dialogue.Ended -= CheckChapterCompletion; State.Changed -= ModeChanged; Instance = null;
        }
        public void StartIntroduction() { Narration.UnlockAudio(); State.SetMode(GameMode.Introduction); }
        public void OpenMap() { Narration.UnlockAudio(); State.SetMode(GameMode.Map); UI.RefreshMap(); }
        public void SelectChapter(int index)
        {
            if (Catalog?.chapters == null || index < 0 || index >= Catalog.chapters.Length) return;
            SelectedChapter = Catalog.chapters[index]; UI.ShowZone(SelectedChapter);
        }
        public void EnterSelected()
        {
            if (State.Mode != GameMode.Map || SelectedChapter == null) return;
            CurrentChapter = SelectedChapter;
            StartCoroutine(LoadScene(CurrentChapter.sceneName, false));
        }
        public void ReturnToMap()
        {
            if (State.Mode != GameMode.Exploration && State.Mode != GameMode.Summary) return;
            Narration.Stop(); Save.Store(); StartCoroutine(LoadScene(mapScene, true));
        }
        private IEnumerator LoadScene(string scene, bool toMap)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene)) { UI.ShowNotice("La escena " + scene + " todavía no está montada o incluida en Build Profiles."); yield break; }
            State.SetMode(GameMode.Loading); UI.SetLoading(0);
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(scene); }
            catch (Exception ex) { UI.ShowLoadError(ex.Message); }
            if (operation == null) yield break;
            while (!operation.isDone) { UI.SetLoading(Mathf.Clamp01(operation.progress / 0.9f)); yield return null; }
            ClaimEventSystem();
            if (toMap) { CurrentChapter = null; State.SetMode(GameMode.Map); UI.RefreshMap(); }
        }
        // La escena que se descarga trae un EventSystem duplicado que, al enabled=true en su OnEnable,
        // se registra como activeEventSystem de UI Toolkit; al destruirse lo deja en null y el próximo
        // PanelEventHandler.OnMove lanza NullReferenceException. El EventSystem vivo debe reafirmarse.
        private void ClaimEventSystem()
        {
            foreach (var system in GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                if (system && system.isActiveAndEnabled) { system.enabled = false; system.enabled = true; break; }
        }
        public void CheckChapterCompletion()
        {
            var chapter = CurrentChapter;
            if (chapter == null || Save.Data.completedChapters.Contains(chapter.id)) return;
            foreach (var clue in chapter.clues) if (!Save.HasClue(clue.id)) return;
            foreach (string phase in new[] { "before", "during", "after" }) if (!Save.Data.decisions.Exists(x => x.key == chapter.id + "/" + phase)) return;
            Save.Complete(chapter.id); State.SetMode(GameMode.Summary); UI.ShowSummary(chapter);
        }
    }
}
