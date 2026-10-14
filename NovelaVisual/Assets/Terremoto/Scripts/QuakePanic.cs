using System.Collections;
using UnityEngine;

namespace CaliNature
{
    public sealed class QuakePanic : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float amplitude = 0.08f;
        [SerializeField, Min(0f)] private float frequency = 12f;
        [SerializeField, Min(0f)] private float duration = 4f;

        private EarthquakeSequenceController controller;
        private Coroutine routine;
        private Vector3 startPos;

        private void Start()
        {
            controller = FindFirstObjectByType<EarthquakeSequenceController>();
            if (controller == null) return;
            controller.Started += OnStarted;
            controller.Finished += OnFinished;
            startPos = transform.localPosition;
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.Started -= OnStarted;
                controller.Finished -= OnFinished;
            }
        }

        private void OnStarted() { if (routine == null) routine = StartCoroutine(Jitter()); }

        private void OnFinished() { if (routine != null) { StopCoroutine(routine); routine = null; transform.localPosition = startPos; } }

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
