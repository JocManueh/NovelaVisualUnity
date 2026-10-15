using UnityEngine;

namespace CaliNature
{
    // ========================================================================
    //  MÚSICA DE FONDO (continua, para todo el juego)
    // ------------------------------------------------------------------------
    //  Se crea sola al arrancar (RuntimeInitializeOnLoadMethod) y sobrevive a
    //  los cambios de escena (DontDestroyOnLoad): suena de principio a fin sin
    //  reiniciarse en cada capítulo.
    //
    //  Usa su PROPIO AudioSource (en un hijo) para no pisar la voz narrativa ni
    //  los efectos de acierto/fallo, y respeta el volumen y el silencio de los
    //  ajustes (save.Data.volume / muted). Va a volumen BAJO a propósito: es
    //  música de fondo por debajo de las voces y los efectos.
    //
    //  Si algún día quieres ajustarla desde el Inspector (volumen, clip), añade
    //  este componente a un objeto de la escena: el arranque automático detecta
    //  que ya existe y no crea otro.
    public sealed class BackgroundMusicPlayer : MonoBehaviour
    {
        [SerializeField] private string resourcePath = "CaliNature/Music/BackgroundMusic";
        [SerializeField] private AudioClip clip;
        [Tooltip("Volumen relativo sobre el volumen general. Bajo a propósito: es música de fondo.")]
        [SerializeField, Range(0f, 1f)] private float volumeScale = 0.3f;
        [SerializeField] private bool loop = true;

        private static BackgroundMusicPlayer instance;
        private AudioSource source;
        private GameSaveManager save;
        private bool loaded;

        // Arranque automático: música para todo el juego, se empiece donde se empiece.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance == null) instance = FindAnyObjectByType<BackgroundMusicPlayer>();
            if (instance != null) return;   // ya existe una (en la escena o de un arranque previo)
            var host = new GameObject("BackgroundMusic (CaliNature)");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<BackgroundMusicPlayer>();
        }

        private void Awake()
        {
            // Si ya hay una instancia (p. ej. puesta a mano en la escena), no duplicar.
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            // Persistir entre escenas (solo si es un objeto raíz; DontDestroyOnLoad no
            // funciona sobre hijos).
            if (transform.parent == null) DontDestroyOnLoad(gameObject);

            // El GameSaveManager vive en el GameSession persistente; se busca una
            // sola vez (no cada frame). Si no existe, se usa el volumen por defecto.
            if (!save) save = GetComponent<GameSaveManager>();
            if (!save) save = FindAnyObjectByType<GameSaveManager>();
            BuildSource();
        }

        private void Start() { Play(); }

        private void Update()
        {
            // Solo reaplica ajustes mientras suena (para reflejar cambios de volumen).
            if (source != null && source.isPlaying) ApplySettings();
        }

        private void BuildSource()
        {
            if (source != null) return;
            const string childName = "BackgroundMusic (Audio)";
            Transform child = transform.Find(childName);
            GameObject host = child != null ? child.gameObject : new GameObject(childName);
            if (host.transform.parent != transform) host.transform.SetParent(transform, false);
            host.hideFlags = HideFlags.HideInHierarchy;
            source = host.GetComponent<AudioSource>();
            if (source == null) source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;   // 2D: se oye igual en toda la escena
            source.volume = 1f;
            source.mute = false;
        }

        private void Play()
        {
            if (!loaded)
            {
                loaded = true;
                if (!clip && !string.IsNullOrEmpty(resourcePath)) clip = Resources.Load<AudioClip>(resourcePath);
                if (!clip)
                    Debug.Log("Música de fondo en silencio: coloca el audio en Resources/" + resourcePath + " o asígnalo en el Inspector.");
            }
            if (clip == null || source == null) return;
            source.clip = clip;
            source.loop = loop;
            ApplySettings();
            source.Play();
        }

        // Respeta el volumen y el silencio de los ajustes. Se reaplica mientras
        // suena para que un cambio de volumen en los ajustes se note al momento.
        private void ApplySettings()
        {
            if (source == null) return;
            float volume = (save ? save.Data.volume : 1f) * volumeScale;
            source.volume = Mathf.Clamp01(volume);
            source.mute = save ? save.Data.muted : false;
        }
    }
}
