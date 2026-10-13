using System;
using System.Collections;
using UnityEngine;

namespace CaliNature
{
    // Evento de sismo del nivel del terremoto.
    //
    // Flujo: al cerrar el diálogo de Bob (decisión `drought/b`) se muestra un
    // contador de 5 s; al llegar a cero se bloquea el control, suena el audio del
    // sismo y la cámara tiembla 4 s; después se devuelve el control y se activa
    // el Puzzle 1 (FASE C, se engancha en `puzzleOne`).
    //
    // El bloqueo del control se hace con GameStateController.Pause(): deja el HUD
    // visible y, como PlayerGridMovement/PlayerInteraction solo actúan en
    // Exploration, congela movimiento e interacción sin tocar nada más.
    public sealed class EarthquakeSequenceController : MonoBehaviour
    {
        [Header("Disparo")]
        [SerializeField] private string triggerDecision = "drought/b";
        [SerializeField] private string doneFlag = "drought/event/quake";

        [Header("Tiempos")]
        [SerializeField, Min(0.5f)] private float countdownSeconds = 5f;
        [SerializeField, Min(0.1f)] private float shakeDuration = 4f;

        [Header("Sacudida de cámara")]
        [SerializeField, Min(0f)] private float shakeAmplitude = 0.15f;
        [SerializeField, Min(0.1f)] private float shakeFrequency = 8f;

        [Header("Audio del sismo")]
        [SerializeField] private AudioSource quakeSource;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
        [SerializeField] private AudioClip quakeClip;

        [Header("Puzzle 1 (FASE C)")]
        [SerializeField] private GameObject puzzleOne;

        // Se dispara al terminar la secuencia (o de inmediato si ya estaba hecha).
        public event Action Finished;

        // Marca de "el sismo ya ocurrió" (pista guardada). La usa el puzzle para
        // saber si debe retomarse al volver a entrar en la zona.
        public string DoneFlag => doneFlag;

        private EarthquakeOverlayUI overlay;
        private GameSession session;
        private bool running;

        private void Awake()
        {
            if (!quakeSource) quakeSource = GetComponent<AudioSource>();
            overlay = new EarthquakeOverlayUI();
        }

        private void Start()
        {
            session = GameSession.Instance;
            if (session) session.Dialogue.Ended += OnDialogueEnded;
            // Si el jugador salió a medias (diálogo de Bob hecho pero sismo sin
            // ocurrir, p. ej. cerró el juego durante la cuenta), se retoma al entrar.
            StartCoroutine(Retake());
        }

        private void OnDisable()
        {
            if (session) session.Dialogue.Ended -= OnDialogueEnded;
        }

        // Un frame de margen para que ZoneChapterController deje el capítulo listo.
        private IEnumerator Retake()
        {
            yield return null;
            TryRun();
        }

        private void OnDialogueEnded() { TryRun(); }

        private void TryRun()
        {
            if (!session || running) return;
            var chapter = session.CurrentChapter;
            if (chapter == null || chapter.id != "drought") return;
            if (session.Save.HasClue(doneFlag)) return;
            if (!session.Save.Data.decisions.Exists(x => x.key == triggerDecision)) return;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            running = true;

            yield return Countdown();

            session.State.Pause();
            CameraShake shake = BeginShake();
            PlayQuake();

            yield return new WaitForSeconds(shakeDuration);

            if (shake) shake.Stop();
            overlay.Hide();

            // Se marca como hecho al final: si se sale de la zona a medias, el
            // evento vuelve a dispararse al reabrir el diálogo de Bob.
            session.Save.AddClue(doneFlag);

            // Si hay un puzzle enganchado NO se reanuda el input: el puzzle mantiene
            // el bloqueo (su panel es modal) y lo devuelve al terminar. Sin puzzle,
            // el control vuelve aquí mismo.
            if (puzzleOne && !puzzleOne.activeSelf) puzzleOne.SetActive(true);
            Finished?.Invoke();
            if (puzzleOne == null) session.State.Resume();
            running = false;
        }

        private IEnumerator Countdown()
        {
            float total = Mathf.Max(0.5f, countdownSeconds);
            float left = total;
            while (left > 0f)
            {
                overlay.ShowCountdown(Mathf.CeilToInt(left), 1f - left / total);
                left -= Time.deltaTime;
                yield return null;
            }
            overlay.ShowCountdown(0, 1f);
        }

        private CameraShake BeginShake()
        {
            Camera camera = Camera.main;
            if (!camera) return null;
            CameraShake shake = camera.GetComponent<CameraShake>();
            if (!shake) shake = camera.gameObject.AddComponent<CameraShake>();
            shake.Begin(shakeDuration, shakeAmplitude, shakeFrequency);
            return shake;
        }

        private void PlayQuake()
        {
            if (!quakeSource || !quakeClip)
            {
                Debug.LogWarning("Terremoto: asigna el AudioClip del sismo en EarthquakeSequenceController.");
                return;
            }
            quakeSource.playOnAwake = false;
            quakeSource.volume = (session ? session.Save.Data.volume : 1f) * volumeScale;
            quakeSource.mute = session ? session.Save.Data.muted : false;
            quakeSource.PlayOneShot(quakeClip);
        }
    }
}
