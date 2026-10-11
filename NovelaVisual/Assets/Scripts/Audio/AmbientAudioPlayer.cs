using UnityEngine;

namespace CaliNature
{
    // Audio ambiente continuo de una escena de capítulo. Respeta el volumen y el silencio de los ajustes.
    [RequireComponent(typeof(AudioSource))]
    public sealed class AmbientAudioPlayer : MonoBehaviour
    {
        [SerializeField] private GameSaveManager save;
        [SerializeField] private AudioClip clip;
        [SerializeField] private string resourcePath = "CaliNature/Ambient/river_flood";
        [SerializeField, Range(0f, 1f)] private float volumeScale = 0.5f;
        private AudioSource ambient;
        public bool Playing => ambient != null && ambient.isPlaying;
        private void Awake()
        {
            ambient = GetComponent<AudioSource>();
            ambient.playOnAwake = false;
            ambient.loop = true;
            if (!save) save = FindAnyObjectByType<GameSaveManager>();
        }
        private void Start()
        {
            if (!clip && !string.IsNullOrEmpty(resourcePath)) clip = Resources.Load<AudioClip>(resourcePath);
            if (!clip) { Debug.Log("Ambiente en silencio: coloca el audio en Resources/" + resourcePath + " o asígnalo en el Inspector."); return; }
            ambient.clip = clip;
            ApplySettings();
            ambient.Play();
        }
        private void Update()
        {
            if (ambient.isPlaying) ApplySettings();
        }
        private void ApplySettings()
        {
            ambient.volume = (save ? save.Data.volume : 1f) * volumeScale;
            ambient.mute = save ? save.Data.muted : false;
        }
        private void OnDisable() { if (ambient) ambient.Stop(); }
    }
}
