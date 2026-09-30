using System;
using UnityEngine;

namespace CaliNature
{
    public enum GameMode { Menu, Introduction, Map, Exploration, Dialogue, Cinematic, Pause, Loading, Summary }

    [DefaultExecutionOrder(-200)]
    public sealed class GameStateController : MonoBehaviour
    {
        [SerializeField] private GameMode initialMode = GameMode.Menu;
        public GameMode Mode { get; private set; }
        public event Action<GameMode> Changed;
        private GameMode resumeMode;
        private void Awake() { Mode = initialMode; }
        public void SetMode(GameMode value)
        {
            if (Mode == value) return;
            Mode = value;
            Changed?.Invoke(value);
        }
        public bool Pause()
        {
            if (Mode == GameMode.Loading || Mode == GameMode.Pause) return false;
            resumeMode = Mode;
            SetMode(GameMode.Pause);
            return true;
        }
        public void Resume() { if (Mode == GameMode.Pause) SetMode(resumeMode); }
    }
}
