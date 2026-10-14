using System.Collections;
using UnityEngine;

namespace CaliNature
{
    public sealed class DoorRattle : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float amplitude = 2f;
        [SerializeField, Min(0f)] private float frequency = 8f;
        [SerializeField, Min(0f)] private float duration = 4f;

        private EarthquakeSequenceController controller;
        private Coroutine routine;

        private void Start()
        {
            controller = FindFirstObjectByType<EarthquakeSequenceController>();
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

        private void OnStarted() { if (routine == null) routine = StartCoroutine(Rattle()); }

        private void OnFinished() { if (routine != null) { StopCoroutine(routine); routine = null; transform.localEulerAngles = Vector3.zero; } }

        private IEnumerator Rattle()
        {
            float elapsed = 0f;
            Vector3 startRot = transform.localEulerAngles;
            while (elapsed < duration)
            {
                float t = elapsed * frequency;
                float angle = amplitude * (Mathf.PerlinNoise(t, 0f) - 0.5f);
                transform.localEulerAngles = startRot + new Vector3(0f, 0f, angle);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localEulerAngles = startRot;
            routine = null;
        }
    }
}
