using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaliNature
{
    // Puzzle 1 del nivel del terremoto, ahora EN EL MUNDO.
    //
    // Al terminar el sismo (EarthquakeSequenceController.Finished) el jugador
    // debe esconderse: se resaltan tres puntos —mesa, ventana y repisa— y se
    // interactúa con ellos acercándose y pulsando E. La mesa resistente es el
    // escondite seguro; la ventana y la repisa dan una explicación y se puede
    // reintentar.
    //
    // A diferencia del panel modal anterior, aquí el jugador CAMINA: al empezar
    // se devuelve el input (Resume) y el bloqueo del sismo queda liberado.
    // Mientras el puzzle está activo, los NPC se desactivan para que no se
    // pueda saltar la secuencia; se rehabilitan al terminar.
    public sealed class HidePlacePuzzle : MonoBehaviour
    {
        [Header("Enganche")]
        [SerializeField] private EarthquakeSequenceController quake;
        [SerializeField] private string chapterId = "drought";
        [SerializeField] private string solvedDecisionKey = "drought/puzzle1";

        [Header("Puntos")]
        [SerializeField] private HideSpot[] spots;

        [Header("Textos")]
        [SerializeField] private string title = "¡ESCÓNDETE!";
        [SerializeField] private string hint = "Corre a un lugar seguro y pulsa E.";
        [SerializeField] private string successMessage = "¡Muy bien! Bajo la mesa resistente estás a salvo.";
        [SerializeField] private string revealMessage = "La respuesta correcta es «Bajo la mesa resistente».";
        [SerializeField] private string wrongFallback = "Ese no es un buen lugar para esconderse.";

        [Header("Reglas")]
        [SerializeField, Min(1)] private int maxAttempts = 3;
        [SerializeField, Min(0.1f)] private float feedbackSeconds = 2f;
        [SerializeField, Min(0.1f)] private float revealSeconds = 3f;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.2f;

        [Header("Resaltado")]
        [SerializeField, Min(0.2f)] private float markerRadius = 0.75f;

        [Header("Después del puzzle")]
        [SerializeField] private string unlockNotice = "Habla con Bob.";

        private TerremotoHideUI ui;
        private GameSession session;
        private Transform anchor;
        private SpriteRenderer playerSprite;
        private string playerLayer;
        private int playerOrder;
        private HideSpot hiddenSpot;
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private int attempts;
        private bool active, busy, solved;

        private void Awake()
        {
            if (!quake) quake = FindAnyObjectByType<EarthquakeSequenceController>();
            if (spots == null || spots.Length == 0) spots = GetComponentsInChildren<HideSpot>(true);
            if (spots != null)
                for (int i = 0; i < spots.Length; i++)
                    if (spots[i]) spots[i].enabled = false;

            var anchorObject = new GameObject("HideAnchor (Terremoto)");
            anchorObject.hideFlags = HideFlags.HideInHierarchy;
            anchorObject.transform.SetParent(transform, false);
            anchor = anchorObject.transform;

            ui = new TerremotoHideUI(fadeSeconds, title);
        }

        private void OnEnable()
        {
            session = GameSession.Instance;
            solved = IsSolved();
            if (quake != null) quake.Finished += OnQuakeFinished;
            // Reentrada: el sismo ya ocurrió pero el puzzle quedó sin resolver.
            if (!solved && QuakeAlreadyHappened()) StartCoroutine(Begin());
        }

        private void OnDisable()
        {
            if (quake != null) quake.Finished -= OnQuakeFinished;
            End();
        }

        private void Update()
        {
            if (active && ui != null) ui.Tick();
        }

        private void OnQuakeFinished()
        {
            if (!InChapter() || solved || active) return;
            StartCoroutine(Begin());
        }

        // Lo llama HideSpot cuando el jugador interactúa con un punto.
        public void OnSpotChosen(HideSpot spot)
        {
            if (!active || busy || spot == null) return;

            if (spot.IsCorrect) { busy = true; StartCoroutine(Win(spot)); return; }

            attempts++;
            if (attempts >= maxAttempts) { busy = true; StartCoroutine(Reveal()); return; }

            string reason = string.IsNullOrEmpty(spot.WrongReason) ? wrongFallback : spot.WrongReason;
            busy = true;
            StartCoroutine(Feedback(reason));
        }

        private bool InChapter()
        {
            var chapter = session != null ? session.CurrentChapter : null;
            return chapter != null && chapter.id == chapterId;
        }

        private bool IsSolved()
        {
            return session != null && session.Save.Data.decisions.Exists(x => x.key == solvedDecisionKey);
        }

        private bool QuakeAlreadyHappened()
        {
            return session != null && quake != null && session.Save.HasClue(quake.DoneFlag);
        }

        private IEnumerator Begin()
        {
            if (active) yield break;
            session = GameSession.Instance;
            if (session == null) yield break;

            if (!ui.Build(session, spots, markerRadius))
            {
                Debug.LogError("Terremoto: no se pudo crear el resaltado del Puzzle 1 (falta la UI).");
                FinishPuzzle();
                yield break;
            }

            active = true;
            attempts = 0;
            busy = false;

            // El sismo dejó el estado en Pause; el jugador debe poder caminar.
            session.State.Resume();

            SuspendOthers();
            if (spots != null)
                for (int i = 0; i < spots.Length; i++)
                    if (spots[i]) { spots[i].Bind(this); spots[i].enabled = true; }

            ui.MarkCorrect(CorrectSpot());
            ui.Show(hint);
            yield return ui.FadeIn();
        }

        private HideSpot CorrectSpot()
        {
            if (spots == null) return null;
            for (int i = 0; i < spots.Length; i++) if (spots[i] != null && spots[i].IsCorrect) return spots[i];
            return null;
        }

        private IEnumerator Feedback(string reason)
        {
            ui.SetMessage(reason);
            yield return new WaitForSecondsRealtime(feedbackSeconds);
            ui.SetMessage(hint);
            busy = false;
        }

        private IEnumerator Win(HideSpot spot)
        {
            HidePlayer(spot);
            FreezePlayer(true);
            // Detener audio del sismo al meterse debajo de la mesa
            var esc2 = FindAnyObjectByType<EarthquakeSequenceController>();
            var qsrc2 = esc2 ? esc2.GetComponent<AudioSource>() : null;
            if (qsrc2) qsrc2.Stop();
            var door2 = FindAnyObjectByType<DoorRattle>();
            if (door2) door2.enabled = false;
            ui.MarkCorrect(spot);
            ui.SetMessage(successMessage);
            yield return new WaitForSecondsRealtime(feedbackSeconds);
            ShowPlayer();
            FreezePlayer(false);
            yield return Close();
        }

        private IEnumerator Reveal()
        {
            ui.MarkCorrect(CorrectSpot());
            ui.SetMessage(revealMessage);
            yield return new WaitForSecondsRealtime(revealSeconds);
            yield return Close();
        }

        private IEnumerator Close()
        {
            RecordSolved();
            solved = true;
            yield return ui.FadeOut();
            End();
            FinishPuzzle();
        }

        // Marca el puzzle como resuelto Y alimenta las `routes` de Bob (FASE D):
        // su conversación abre por una regla con decisionKey `drought/puzzle1`.
        private void RecordSolved()
        {
            session.Save.RecordDecision(new DialogueChoice
            {
                id = "solved",
                decisionKey = solvedDecisionKey,
                consequence = "El jugador ya sabe dónde cubrirse durante un sismo.",
                responsible = true
            });
        }

        private void End()
        {
            RestoreOthers();
            if (spots != null)
                for (int i = 0; i < spots.Length; i++)
                    if (spots[i]) { spots[i].enabled = false; spots[i].Bind(null); }
            ShowPlayer();
            FreezePlayer(false);
            if (ui != null) { ui.Hide(); ui.Remove(); }
            active = false;
            busy = false;
        }

        private void FinishPuzzle()
        {
            var current = GameSession.Instance;
            if (current == null) return;

            current.State.Resume();
            if (!string.IsNullOrEmpty(unlockNotice)) current.UI.ShowNotice(unlockNotice);
        }

        // ── "Meterse debajo de la mesa" ──────────────────────────────────────

        private void HidePlayer(HideSpot spot)
        {
            var movement = FindAnyObjectByType<PlayerGridMovement>();
            if (movement == null) return;

            playerSprite = movement.GetComponentInChildren<SpriteRenderer>();
            if (playerSprite != null)
            {
                playerLayer = playerSprite.sortingLayerName;
                playerOrder = playerSprite.sortingOrder;
                // La mesa también es "Layer 1", pero con más orden: así el mueble
                // se dibuja por encima y el personaje queda escondido debajo.
                playerSprite.sortingLayerName = "Layer 1";
                playerSprite.sortingOrder = 0;
            }

            if (anchor != null)
            {
                anchor.position = spot.HidePosition;
                movement.PlaceAt(anchor);
            }
            hiddenSpot = spot;
        }

        private void ShowPlayer()
        {
            if (playerSprite != null)
            {
                playerSprite.sortingLayerName = playerLayer;
                playerSprite.sortingOrder = playerOrder;
                playerSprite = null;
            }
            // Sale de debajo del mueble: lo devuelve a un punto despejado ANTES
            // de devolver el control, para que no arranque dentro de un
            // colisionador (si no, PlayerGridMovement queda bloqueado).
            if (hiddenSpot != null)
            {
                var movement = FindAnyObjectByType<PlayerGridMovement>();
                if (movement != null) movement.PlaceAt(hiddenSpot.transform);
                hiddenSpot = null;
            }
        }

        // Congela el control mientras el personaje está dentro del mueble: en
        // Pause no se ejecuta PlayerGridMovement, así que no se comprueba la
        // colisión mientras está solapado. Se descongela al salir.
        private void FreezePlayer(bool frozen)
        {
            if (session == null) return;
            if (frozen) { if (session.State.Mode == GameMode.Exploration) session.State.Pause(); }
            else if (session.State.Mode == GameMode.Pause) session.State.Resume();
        }

        // ── Suspensión de otros interactuables ───────────────────────────────

        private void SuspendOthers()
        {
            suspended.Clear();
            foreach (var npc in FindObjectsByType<NPCDialogueController>())
            {
                if (!npc.enabled) continue;
                npc.enabled = false;
                suspended.Add(npc);
            }
        }

        private void RestoreOthers()
        {
            foreach (var behaviour in suspended) if (behaviour) behaviour.enabled = true;
            suspended.Clear();
        }
    }
}
