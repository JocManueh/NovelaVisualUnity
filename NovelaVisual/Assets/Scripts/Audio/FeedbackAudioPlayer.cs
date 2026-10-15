using UnityEngine;

namespace CaliNature
{
    // ========================================================================
    //  EFECTOS DE SONIDO CORTOS (acierto / fallo / clic de UI)
    // ------------------------------------------------------------------------
    //  Reproduce efectos breves de feedback:
    //    · CORRECTO / INCORRECTO al elegir una respuesta en una PREGUNTA de
    //      diálogo (nodo con opciones correctas e incorrectas: p. ej. la
    //      pregunta de Amelia) y al acertar o fallar un paso de puzzle
    //      (orden de acciones, escondite),
    //    · CLIC al pulsar cualquier botón de la interfaz (lo dispara
    //      CaliGameUI desde la raíz de la UI, así cubre también los botones
    //      creados en runtime: opciones de diálogo y del puzzle).
    //
    //  Vive en el objeto persistente de GameSession y se crea solo la primera
    //  vez que se pide (igual que otros sistemas del nivel). Carga los clips
    //  desde Resources y respeta el volumen y el silencio de los ajustes.
    //
    //  Usa un AudioSource PROPIO (en un hijo) para no pisar la voz narrativa,
    //  que usa el AudioSource del propio GameSession.
    public sealed class FeedbackAudioPlayer : MonoBehaviour
    {
        [SerializeField] private string correctResource = "CaliNature/Sfx/Acierto";
        [SerializeField] private string wrongResource = "CaliNature/Sfx/Fallo";
        [SerializeField] private string clickResource = "CaliNature/Sfx/UiClick";
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
        [Tooltip("Volumen del clic de UI, relativo a los demás efectos. Bajo a propósito.")]
        [SerializeField, Range(0f, 1f)] private float clickScale = 0.6f;

        private static FeedbackAudioPlayer instance;
        private AudioSource source;
        private AudioClip correctClip, wrongClip, clickClip;
        private bool loaded, warned;

        private enum Sfx { Correct, Wrong, Click }

        // ── API estática ─────────────────────────────────────────────────────
        // Se puede llamar desde cualquier punto aunque el reproductor todavía no
        // exista: la primera llamada lo crea en el GameSession.
        public static void Correct() { Play(Sfx.Correct); }
        public static void Wrong() { Play(Sfx.Wrong); }
        public static void Click() { Play(Sfx.Click); }

        private static void Play(Sfx kind)
        {
            var player = Ensure();
            if (player != null) player.PlayInternal(kind);
        }

        private static FeedbackAudioPlayer Ensure()
        {
            if (instance != null) return instance;
            var session = GameSession.Instance;
            if (session == null) return null;
            instance = session.GetComponentInChildren<FeedbackAudioPlayer>(true);
            if (instance == null) instance = session.gameObject.AddComponent<FeedbackAudioPlayer>();
            return instance;
        }

        private void Awake()
        {
            // Si ya hay uno (p. ej. reconexión tras un domain reload sin recargar
            // la escena) no duplicar el AudioSource.
            if (instance != null && instance != this) { Destroy(this); return; }
            instance = this;
            BuildSource();
        }

        private void BuildSource()
        {
            if (source != null) return;
            const string childName = "FeedbackSfx (Audio)";
            Transform child = transform.Find(childName);
            GameObject host = child != null ? child.gameObject : new GameObject(childName);
            if (host.transform.parent != transform) host.transform.SetParent(transform, false);
            host.hideFlags = HideFlags.HideInHierarchy;
            source = host.GetComponent<AudioSource>();
            if (source == null) source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;   // 2D: se oye igual sin importar dónde esté el jugador
            source.volume = 1f;
            source.mute = false;
        }

        private void PlayInternal(Sfx kind)
        {
            LoadClips();
            AudioClip clip = kind == Sfx.Correct ? correctClip : kind == Sfx.Wrong ? wrongClip : clickClip;
            if (clip == null || source == null) return;

            GameSaveManager save = GameSession.Instance ? GameSession.Instance.Save : null;
            if (save != null && (save.Data.muted || save.Data.volume <= 0f)) return;

            float scale = kind == Sfx.Click ? clickScale : volumeScale;
            float volume = Mathf.Clamp01((save != null ? save.Data.volume : 1f) * scale);
            source.PlayOneShot(clip, volume);
        }

        private void LoadClips()
        {
            if (loaded) return;
            loaded = true;
            correctClip = Load(correctResource);
            wrongClip = Load(wrongResource);
            clickClip = Load(clickResource);
            if ((correctClip == null || wrongClip == null || clickClip == null) && !warned)
            {
                warned = true;
                Debug.Log("Efectos de sonido incompletos: revisa Resources/ (" +
                          correctResource + ", " + wrongResource + ", " + clickResource + ").");
            }
        }

        private static AudioClip Load(string resource)
        {
            return string.IsNullOrEmpty(resource) ? null : Resources.Load<AudioClip>(resource);
        }
    }
}
