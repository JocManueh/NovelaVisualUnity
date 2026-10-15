using System.Collections;
using UnityEngine;

namespace CaliNature
{
    // Reacción de un personaje durante el sismo: una sacudida nerviosa en su sitio.
    //
    // Se engancha a EarthquakeSequenceController.Started, que ahora se dispara
    // justo cuando empieza el temblor. La posición de reposo se toma EN ESE
    // MOMENTO (no al cargar la escena): así cada personaje se mueve donde lo
    // sorprendió el sismo —el jugador junto a Bob, por ejemplo— y no vuelve al
    // punto inicial del nivel al terminar.
    public sealed class QuakePanic : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float amplitude = 0.08f;
        [SerializeField, Min(0f)] private float frequency = 12f;
        [SerializeField, Min(0f)] private float duration = 4f;

        private EarthquakeSequenceController controller;
        private Coroutine routine;
        private Vector3 startPos;
        private bool haveStart;

        private void Start()
        {
            controller = FindAnyObjectByType<EarthquakeSequenceController>();
            if (controller == null) return;
            controller.Started += OnStarted;
            controller.Finished += OnFinished;
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.Started -= OnStarted;
                controller.Finished -= OnFinished;
            }
        }

        private void OnStarted()
        {
            // Punto de reposo = donde está el personaje al empezar el temblor.
            startPos = transform.localPosition;
            haveStart = true;
            if (routine == null) routine = StartCoroutine(Jitter());
        }

        private void OnFinished() { StopJitter(); }

        private void OnDisable() { StopJitter(); }

        private void StopJitter()
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
            if (haveStart) { transform.localPosition = startPos; haveStart = false; }
        }

        private IEnumerator Jitter()
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed * frequency;
                float dx = amplitude * (Mathf.PerlinNoise(t, 0.1f) - 0.5f);
                float dy = amplitude * (Mathf.PerlinNoise(t + 5f, 0.3f) - 0.5f);
                transform.localPosition = startPos + new Vector3(dx, dy, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = startPos;
            routine = null;
        }
    }
}
