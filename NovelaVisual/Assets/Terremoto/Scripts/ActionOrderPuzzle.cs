using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaliNature
{
    // Puzzle 2 del nivel del terremoto (FASE D).
    //
    // Después del sismo y del Puzzle 1, Bob pregunta si el jugador recuerda el
    // ORDEN correcto de acciones. Se abre un panel (modal, reutiliza
    // TerremotoPanelUI) con las cuatro acciones barajadas y hay que pulsarlas
    // en orden:
    //   1. Revisar si hay heridos.
    //   2. Cerrar la llave del gas.
    //   3. Usar las escaleras, no el ascensor.
    //   4. Ir al punto de encuentro.
    //
    // Un fallo explica por qué ese paso todavía no toca (y cuál es el correcto)
    // y reinicia el orden: no hay límite de intentos. Al completarlo queda la
    // decisión `drought/puzzle2` y se activa el NPC del punto de encuentro, que
    // da el cierre del capítulo (FASE E).
    public sealed class ActionOrderPuzzle : MonoBehaviour
    {
        [Header("Enganche")]
        [SerializeField] private string chapterId = "drought";
        // Decisión que deja Bob al preguntar por el orden (lo dispara el panel).
        [SerializeField] private string triggerDecision = "drought/b2";
        // Decisión que marca el puzzle como resuelto y alimenta las `routes` de Bob.
        [SerializeField] private string solvedDecisionKey = "drought/puzzle2";
        // NPC que se activa al resolver el puzzle (el punto de encuentro).
        [SerializeField] private GameObject npcToActivate;

        [Header("Pasos (en el orden correcto)")]
        [SerializeField] private string[] steps =
        {
            "Revisar si hay heridos.",
            "Cerrar la llave del gas.",
            "Usar las escaleras, no el ascensor.",
            "Ir al punto de encuentro."
        };

        [Header("Motivos (por qué cada paso va en ese lugar)")]
        [SerializeField] private string[] reasons =
        {
            "lo primero es saber quién necesita ayuda",
            "cortar el gas evita incendios o explosiones antes de moverse",
            "tras un sismo el ascensor puede fallar o quedar atrapado",
            "al final, reúnete donde el grupo acordó"
        };

        [Header("Panel")]
        [SerializeField] private string title = "¿Recuerdas el orden correcto?";
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.15f;

        [Header("Fallos")]
        [SerializeField, Min(0.2f)] private float wrongSeconds = 1.8f;
        [SerializeField] private string wrongPrefix = "Ese no es el siguiente paso.";

        [Header("Después del puzzle")]
        [SerializeField] private string unlockNotice = "Ve al punto de encuentro.";

        private TerremotoPanelUI panel;
        private GameSession session;
        private readonly List<int> shownOrder = new List<int>();
        private int pendingPick = -1;
        private int correct;
        private bool running, solved;

        private void Awake()
        {
            panel = new TerremotoPanelUI(fadeSeconds);
        }

        private void OnEnable()
        {
            session = GameSession.Instance;
            solved = IsSolved();
            if (session != null) session.Dialogue.Ended += OnDialogueEnded;
            // Reentrada: si el jugador eligió la pregunta de Bob pero cerró el
            // juego antes de ordenar, el panel se vuelve a abrir al entrar.
            if (!solved) StartCoroutine(Retake());
        }

        private void OnDisable()
        {
            if (session != null) session.Dialogue.Ended -= OnDialogueEnded;
            // Si se descarga la escena con el panel abierto, no dejar el input bloqueado.
            if (running && session != null) session.State.Resume();
            End();
        }

        private void OnDialogueEnded() { TryRun(); }

        private IEnumerator Retake()
        {
            yield return null;
            TryRun();
        }

        private void TryRun()
        {
            if (!InChapter() || solved || running) return;
            // Bob deja `drought/b2` al preguntar por el orden; con eso se abre el panel.
            if (!session.Save.Data.decisions.Exists(x => x.key == triggerDecision)) return;
            StartCoroutine(Run());
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

        private IEnumerator Run()
        {
            if (steps == null || steps.Length == 0) yield break;
            running = true;
            correct = 0;

            // El panel es modal: bloquea el movimiento mientras está abierto.
            session.State.Pause();

            if (!BuildPanel()) { AbortWithoutUi(); yield break; }
            yield return panel.FadeIn();

            while (true)
            {
                pendingPick = -1;
                while (pendingPick < 0) yield return null;

                int picked = pendingPick;
                if (picked == correct)
                {
                    correct++;
                    if (correct >= steps.Length) break;      // orden completo
                    BuildPanel();
                    yield return panel.FadeIn();
                }
                else
                {
                    panel.ShowFeedback(WrongMessage());
                    yield return new WaitForSecondsRealtime(wrongSeconds);
                    correct = 0;                              // se reinicia el orden
                    BuildPanel();
                    yield return panel.FadeIn();
                }
            }

            yield return Finish();
        }

        // Recompone el panel con los pasos que aún no se han colocado, en un
        // orden aleatorio distinto cada vez. Devuelve false si no hay UI.
        private bool BuildPanel()
        {
            panel.Remove();
            shownOrder.Clear();
            for (int i = correct; i < steps.Length; i++) shownOrder.Add(i);
            for (int i = shownOrder.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int swap = shownOrder[i]; shownOrder[i] = shownOrder[j]; shownOrder[j] = swap;
            }

            var labels = new string[shownOrder.Count];
            for (int i = 0; i < shownOrder.Count; i++) labels[i] = steps[shownOrder[i]];
            return panel.Build(session, title + "  (" + correct + "/" + steps.Length + ")", labels, OnPick);
        }

        // Si no hay UI a la que enganchar el panel, no dejar el puzzle bloqueado:
        // se marca resuelto y se sigue, para que el jugador no quede atrapado.
        private void AbortWithoutUi()
        {
            Debug.LogError("Terremoto: no se pudo crear el panel del Puzzle 2 (falta la UI).");
            RecordSolved();
            solved = true;
            End();
            if (npcToActivate != null) npcToActivate.SetActive(true);
            session.State.Resume();
        }

        // El panel entrega el índice VISIBLE; se traduce al índice real del paso.
        private void OnPick(int displayIndex)
        {
            if (displayIndex < 0 || displayIndex >= shownOrder.Count) return;
            pendingPick = shownOrder[displayIndex];
        }

        private string WrongMessage()
        {
            string expected = steps[correct];
            string reason = (reasons != null && correct < reasons.Length) ? reasons[correct] : "";
            return wrongPrefix + " Primero: «" + expected + "» — " + reason + ".";
        }

        private IEnumerator Finish()
        {
            RecordSolved();
            solved = true;
            yield return panel.FadeOut();
            End();
            if (npcToActivate != null) npcToActivate.SetActive(true);
            session.State.Resume();
            if (!string.IsNullOrEmpty(unlockNotice)) session.UI.ShowNotice(unlockNotice);
        }

        // Deja la decisión `drought/puzzle2`: alimenta la `routes` de Bob (FASE E)
        // y marca el puzzle como resuelto para no repetirlo.
        private void RecordSolved()
        {
            session.Save.RecordDecision(new DialogueChoice
            {
                id = "solved",
                decisionKey = solvedDecisionKey,
                consequence = "El jugador ordenó correctamente las acciones tras un sismo.",
                responsible = true
            });
        }

        private void End()
        {
            if (panel != null) panel.Remove();
            shownOrder.Clear();
            pendingPick = -1;
            correct = 0;
            running = false;
        }
    }
}
