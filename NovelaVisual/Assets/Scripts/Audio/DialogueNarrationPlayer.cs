using UnityEngine;

namespace CaliNature
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class DialogueNarrationPlayer : MonoBehaviour
    {
        [SerializeField] private GameSaveManager save;
        private AudioSource voice;
        private string playingId;
        private double finishesAt, pausedAt;
        private bool tracking, audible, paused, userUnlocked, everPlayed;
        public bool HasClip { get; private set; }
        public bool Playing => tracking;
        private void Awake() { voice = GetComponent<AudioSource>(); voice.playOnAwake = false; voice.loop = false; if (!save) save = GetComponent<GameSaveManager>(); }
        public void UnlockAudio() { userUnlocked = true; AudioListener.pause = false; }
        public void Play(string id, string resource)
        {
            Stop();
            AudioClip clip = string.IsNullOrEmpty(resource) ? null : Resources.Load<AudioClip>(resource);
            HasClip = clip;
            if (!clip || !userUnlocked || !save || save.Data.muted || save.Data.volume <= 0) return;
            voice.clip = clip; voice.volume = save.Data.volume; voice.mute = false;
            playingId = id; finishesAt = AudioSettings.dspTime + clip.length;
            tracking = audible = true; everPlayed = false; voice.pitch = 1; voice.Play();
        }
        private void Update()
        {
            if (!tracking || paused) return;
            voice.volume = save.Data.volume; voice.mute = save.Data.muted;
            if (voice.isPlaying) everPlayed = true;
            if (voice.mute || voice.volume <= 0 || AudioListener.volume <= 0) audible = false;
            if (!voice.isPlaying && AudioSettings.dspTime >= finishesAt)
            {
                if (audible && everPlayed) save.MarkHeard(playingId);
                tracking = false;
            }
        }
        public void Stop() { if (voice) voice.Stop(); tracking = audible = paused = false; playingId = null; HasClip = false; }
        public void SetPaused(bool value)
        {
            if (!tracking || paused == value) return;
            paused = value;
            if (value) { pausedAt = AudioSettings.dspTime; voice.Pause(); }
            else { finishesAt += AudioSettings.dspTime - pausedAt; voice.UnPause(); }
        }
        private void OnApplicationFocus(bool focus) { if (!focus) { audible = false; } }
        private void OnDisable() { Stop(); }
    }
}
