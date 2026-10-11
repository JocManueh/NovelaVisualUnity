using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================================
//  SESIÓN DE JUEGO — orquestador central
// ----------------------------------------------------------------------------
//  Coordina el estado global, el flujo entre escenas y, sobre todo, el
//  ARBITRAJE DEL RESULTADO DE UN CAPÍTULO.
//
//  RESULTADO DE UN CAPÍTULO
//  Ya NO se gana por "reunir pistas y responder". Ahora gana o pierde quien
//  elija el camino de diálogo correcto:
//
//      A1/A2 → Bx      B1/B2 → Cmid    B3 → Cgood   ← mayormente ganar (exige pista 0)
//      A3    → By      B4/B5 → Cbad/Cdead   B6 → Cmid  ← mayormente perder (exige pista 1)
//
//  El desenlace se decide en el NPC C: cada opción de cierre lleva
//  `outcome` = "win" o "lose". GameSession solo reacciona; NO decide.
//
//    • "win"  → marca el capítulo completado y muestra el resumen.
//    • "lose" → borra pistas y decisiones del capítulo y vuelve al mapa, para
//               que el jugador pueda reintentarlo desde cero.
//
//  El gating de NPCs (no hablar con B antes que A) vive en CanTalkToNpc().
// ============================================================================
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
            Dialogue.Ended += ResolveOutcome;
            State.Changed += ModeChanged;
            UI.RefreshMode(State.Mode);
        }
        private void ModeChanged(GameMode mode) { Narration.SetPaused(mode == GameMode.Pause); }
        private void OnDestroy()
        {
            if (Instance != this) return;
            Dialogue.Ended -= ResolveOutcome; State.Changed -= ModeChanged; Instance = null;
        }
        public void StartIntroduction() { Narration.UnlockAudio(); State.SetMode(GameMode.Introduction); }
        public void OpenMap() { Narration.UnlockAudio(); State.SetMode(GameMode.Map); UI.RefreshMap(); }
        public void SelectChapter(int index)
        {
            if (Catalog?.chapters == null || index < 0 || index >= Catalog.chapters.Length) return;
            SelectedChapter = Catalog.chapters[index];
            // El mapa es una escena compartida: su fondo cambia aquí, al elegir zona,
            // no al entrar en ella.
            ChapterSpriteApplicator.ApplyMapSprite(SelectedChapter);
            UI.ShowZone(SelectedChapter);
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
            StartCoroutine(ReturnToMapRoutine());
        }
        private IEnumerator ReturnToMapRoutine()
        {
            Narration.Stop(); Save.Store(); yield return LoadScene(mapScene, true);
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
        // ── ARBITRAJE DEL CAPÍTULO ──────────────────────────────────────────

        // Orden de las fases. El NPC con índice N solo abre si la fase N-1
        // (a=0, b=1, c=2) ya tiene una decisión registrada. El índice coincide
        // con npcIndex en la escena y con la posición en chapter.conversations.
        private static readonly string[] NpcPhases = { "a", "b", "c" };

        public const string OutcomeWin = "win";
        public const string OutcomeLose = "lose";

        // Último outcome visto, a la espera de que termine el diálogo.
        private string pendingOutcome;

        // GATING DE NPCs. El primero (Lucía) siempre está disponible; para hablar
        // con Mateo o Inés hace falta haber cerrado el diálogo del NPC anterior.
        // Si no, muestra un aviso y devuelve false: la conversación no abre.
        public bool CanTalkToNpc(int index)
        {
            if (index <= 0) return true;
            var chapter = CurrentChapter;
            if (chapter == null || index >= NpcPhases.Length) return false;
            var key = chapter.id + "/" + NpcPhases[index - 1];
            if (Save.Data.decisions.Exists(x => x.key == key)) return true;
            UI.ShowNotice("Antes de hablar aquí tienes que hablar con la persona anterior.");
            return false;
        }

        // Las pistas solo se pueden recoger después de cerrar el diálogo con Lucía
        // (nodo A). El objeto sigue visible en el mapa, pero no registra nada:
        // así el jugador ve que hay algo por investigar sin poder saltarse el orden.
        public bool CanCollectClues()
        {
            var chapter = CurrentChapter;
            if (chapter == null) return false;
            return Save.Data.decisions.Exists(x => x.key == chapter.id + "/" + NpcPhases[0]);
        }

        // ── RAMA ACTUAL ──────────────────────────────────────────────────────
        // Devuelve el nodo de B al que llevó la decisión tomada en el NPC A:
        // "bx_start" si fue A1/A2, "by_start" si fue A3. Vacío si aún no habló con A.
        //
        // Se deduce leyendo las rutas de la conversación de B, de modo que la
        // lógica no está duplicada: si mañana se añade una tercera rama, esto
        // sigue funcionando sin cambios.
        public string CurrentBRanch()
        {
            var chapter = CurrentChapter;
            if (chapter == null || chapter.conversations.Length < 2) return null;
            var record = Save.Data.decisions.Find(x => x.key == chapter.id + "/" + NpcPhases[0]);
            if (record == null) return null;
            foreach (var route in chapter.conversations[1].routes ?? Array.Empty<RouteRule>())
                if (route.decisionKey == record.key && route.choice == record.choice) return route.node;
            return null;
        }

        // ── LA PISTA QUE CORRESPONDE A TU RAMBA ──────────────────────────────────
        // Devuelve el `requiredClue` de la opción bloqueada que abre la rama
        // buena desde la rama en la que está el jugador. Ej: si va por By, la
        // pista que necesita es la que pide B6.
        //
        // Se deduce de los datos (nunca se escribe a mano): se busca la opción
        // de B con requiredClue cuya ruta lleve a la rama favorable. Así, si
        // mañana se reordena el JSON, esto sigue siendo correcto.
        private string BranchClueId()
        {
            var chapter = CurrentChapter;
            if (chapter == null || chapter.conversations.Length < 2) return null;
            string branch = CurrentBRanch();
            if (string.IsNullOrEmpty(branch)) return null;

            // La rama buena NO se deduce por frecuencia de rutas: varias ramas reciben
            // el mismo número de entradas. Se identifica por su PROPORCIÓN de
            // opciones ganadoras, que es lo que la define en el diseño.
            string bestNode = null; int bestWins = -1;
            foreach (var node in chapter.conversations[2].nodes)
            {
                var closing = (node.choices ?? Array.Empty<DialogueChoice>())
                    .Where(c => c.outcome == OutcomeWin || c.outcome == OutcomeLose).ToArray();
                if (closing.Length == 0) continue;
                int wins = closing.Count(c => c.outcome == OutcomeWin);
                if (wins > bestWins) { bestWins = wins; bestNode = node.id; }
            }
            if (bestNode == null) return null;

            // OJO: la transición de B a C vive en las rutas de la CONVERSACIÓN DE C.
            // Las rutas de la conversación de B solo van de A a B (A1→bx_start...) y
            // no contienen reglas del tipo B6→cgood_start. Usar las equivocadas hacía
            // que esta búsqueda nunca encontrara nada y la pista de la rama se perdiera.
            var routesToC = (chapter.conversations[2].routes ?? Array.Empty<RouteRule>())
                .Where(r => r.decisionKey.EndsWith("/" + NpcPhases[1], StringComparison.Ordinal))
                .ToArray();

            foreach (var node in chapter.conversations[1].nodes)
            {
                if (node.id != branch) continue;
                var choice = (node.choices ?? Array.Empty<DialogueChoice>())
                    .FirstOrDefault(c => !string.IsNullOrEmpty(c.requiredClue)
                                      && routesToC.Any(r => r.choice == c.id && r.node == bestNode));
                if (choice != null) return choice.requiredClue;
            }
            return null;
        }

        // Devuelve la pista que el jugador encuentra AHORA, según la rama en la que
        // está. No depende de qué objeto de la escena examinó: si va por By, cualquier
        // objeto le entrega la pista de By, y si va por Bx, la de Bx.
        //
        // Esto evita el fallo más importante de esta mecánica: con tres objetos en el
        // mapa, el jugador podía coger el "equivocado" y quedarse con una pista que
        // no desbloqueaba nada en su rama, sin ninguna pista para explicar por qué.
        public ClueData ResolveClue(ClueData fallback)
        {
            var chapter = CurrentChapter;
            if (chapter == null || chapter.clues == null) return fallback;
            var branch = CurrentBRanch();
            var required = BranchClueId();

            // Antes de hablar con Lucía todavía no hay rama: se entrega la primera
            // pista, que es la que corresponde a Bx por defecto.
            ClueData target = fallback;
            if (!string.IsNullOrEmpty(required))
            {
                var found = chapter.clues.FirstOrDefault(c => c.id == required);
                if (found != null) target = found;
            }
            // Si la rama no pudo resolverse se avisa por consola. Antes este caso
            // devolvía en silencio la pista del objeto, y el jugador se quedaba con
            // una pista que no abría nada en su rama sin ninguna explicación.
            if (string.IsNullOrEmpty(required))
                Debug.LogWarning("Cali: no se pudo determinar la pista de la rama " + branch
                    + " en el capítulo " + chapter.id + "; se entrega la del objeto.");
            if (target == null) return fallback;

            if (string.IsNullOrEmpty(branch)) return target;
            foreach (var variant in target.variants ?? Array.Empty<ClueVariant>())
                if (variant != null && variant.branch == branch)
                    return new ClueData
                    {
                        id = target.id, // el id es el que desbloquea la opción de B
                        title = string.IsNullOrEmpty(variant.title) ? target.title : variant.title,
                        text = string.IsNullOrEmpty(variant.text) ? target.text : variant.text,
                    };
            return target;
        }

        // El diálogo arranca con el outcome limpio: solo cuenta la última opción
        // elegida, nunca una de un diálogo anterior.
        public void NotifyDialogueStarted() { pendingOutcome = null; }

        // Llamado por DialogueController al elegir. Las opciones de los NPCs A y B
        // llegan con outcome vacío y por tanto no activan nada.
        public void NotifyChoice(DialogueChoice choice) { if (choice != null && !string.IsNullOrEmpty(choice.outcome)) pendingOutcome = choice.outcome; }

        // Se dispara al terminar cualquier diálogo (Dialogue.Ended). Si la última
        // opción fue una de cierre, aquí se arbitra el resultado del capítulo.
        private void ResolveOutcome()
        {
            var chapter = CurrentChapter;
            var outcome = pendingOutcome; pendingOutcome = null;
            if (chapter == null || string.IsNullOrEmpty(outcome)) return;
            if (outcome == OutcomeWin) CompleteChapter(chapter);
            else if (outcome == OutcomeLose) FailChapter(chapter);
        }

        private void CompleteChapter(ChapterData chapter)
        {
            Save.Complete(chapter.id); State.SetMode(GameMode.Summary); UI.ShowSummary(chapter);
        }

        // Al perder se borra lo hecho en el capítulo para poder volver a intentarlo
        // desde cero (ClearChapter quita pistas y decisiones con prefijo "capitulo/").
        // completedChapters NO se toca: un caso ya ganado sigue marcado en el mapa
        // aunque en un reintento posterior el jugador pierda.
        private void FailChapter(ChapterData chapter)
        {
            Save.ClearChapter(chapter.id);
            UI.ShowNotice("Has perdido este caso. Se borra lo que hiciste en él.");
            StartCoroutine(ReturnToMapRoutine());
        }
    }
}
