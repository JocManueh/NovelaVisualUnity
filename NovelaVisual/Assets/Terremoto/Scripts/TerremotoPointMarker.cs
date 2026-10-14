using System.Collections;
using UnityEngine;

namespace CaliNature
{
    // Señala el punto de encuentro del nivel del terremoto (FASE E).
    //
    // Vive en el NPC_PuntoDeEncuentro, que arranca INACTIVO y lo activa
    // ActionOrderPuzzle al resolver el Puzzle 2. Al activarse, este componente
    // dibuja un anillo con la etiqueta "Punto de encuentro" (igual que los
    // escondites del Puzzle 1) y, si queda fuera de cámara, una flecha en el
    // borde de la pantalla. Se oculta solo cuando el jugador llega: al empezar
    // el diálogo de cierre, al quedar registrada la decisión `drought/c` o al
    // completarse el capítulo.
    public sealed class TerremotoPointMarker : MonoBehaviour
    {
        [Header("Marcador")]
        // Vacío = usa la posición de este GameObject.
        [SerializeField] private Transform target;
        [SerializeField] private string label = "Punto de encuentro";
        [SerializeField, Min(0.2f)] private float markerRadius = 0.8f;

        [Header("Cuándo se oculta")]
        [SerializeField] private string chapterId = "drought";
        // Decisión que deja la conversación de cierre: al registrarse, ya llegó.
        [SerializeField] private string arrivedDecision = "drought/c";
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.25f;

        private TerremotoMarkerUI ui;
        private GameSession session;
        private bool alive, hidden;

        private void Awake()
        {
            if (!target) target = transform;
            ui = new TerremotoMarkerUI(fadeSeconds);
        }

        private void OnEnable()
        {
            session = GameSession.Instance;
            StartCoroutine(ShowRoutine());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            alive = false;
            hidden = false;
            if (ui != null) { ui.Hide(); ui.Remove(); }
        }

        private void Update()
        {
            if (!alive || session == null) return;

            // Llegó o se acabó el capítulo: fuera de una vez.
            if (ShouldStop())
            {
                alive = false;
                StartCoroutine(HideRoutine());
                return;
            }

            // En diálogo o con un modal abierto el overlay quedaría dibujado por
            // encima de la ventana: se oculta y se recupera al volver a explorar.
            bool want = session.State.Mode == GameMode.Exploration;
            if (want && hidden) { hidden = false; ui.SetVisible(true); }
            else if (!want && !hidden) { hidden = true; ui.SetVisible(false); }
            if (!hidden) ui.Tick();
        }

        private IEnumerator ShowRoutine()
        {
            if (session == null) session = GameSession.Instance;
            if (session == null) yield break;
            if (target == null) target = transform;

            if (!ui.Build(session, target, label, markerRadius))
            {
                Debug.LogWarning("Terremoto: no se pudo crear el marcador del punto de encuentro (falta la UI).");
                yield break;
            }

            alive = true;
            hidden = false;
            ui.Show();
            yield return ui.FadeIn();
        }

        private IEnumerator HideRoutine()
        {
            yield return ui.FadeOut();
            ui.Hide();
            ui.Remove();
        }

        // El marcador solo tiene sentido mientras el jugador no haya llegado al
        // punto de encuentro dentro del capítulo.
        private bool ShouldStop()
        {
            if (session == null) return true;
            var chapter = session.CurrentChapter;
            if (chapter == null || chapter.id != chapterId) return true;
            if (session.Save.Data.completedChapters.Contains(chapterId)) return true;
            if (!string.IsNullOrEmpty(arrivedDecision) && session.Save.Data.decisions.Exists(x => x.key == arrivedDecision)) return true;
            return false;
        }
    }
}
