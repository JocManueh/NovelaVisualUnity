using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaliNature
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameInputRouter : MonoBehaviour
    {
        [SerializeField] private GameStateController state;
        public event Action InteractPressed;
        public event Action AdvancePressed;
        public Vector2 Movement { get; private set; }
        private Vector2 touchDirection;
        private Vector2 pendingStep;
        private bool touchInteract, touchAdvance;
        private readonly HashSet<KeyCode> uiKeys = new HashSet<KeyCode>();
        public void KeyPressed(KeyCode key)
        {
            if (!state || !uiKeys.Add(key)) return;
            if (key == KeyCode.E && state.Mode == GameMode.Exploration) touchInteract = true;
            if (key == KeyCode.Space && state.Mode == GameMode.Dialogue) touchAdvance = true;
            if (state.Mode == GameMode.Exploration)
            {
                if (key == KeyCode.W) pendingStep = Vector2.up;
                else if (key == KeyCode.S) pendingStep = Vector2.down;
                else if (key == KeyCode.A) pendingStep = Vector2.left;
                else if (key == KeyCode.D) pendingStep = Vector2.right;
            }
        }
        public void KeyReleased(KeyCode key) { uiKeys.Remove(key); }
        private void Awake() { if (!state) state = GetComponent<GameStateController>(); }
        private void OnEnable() { if (state) state.Changed += ClearTransient; }
        private void OnDisable() { if (state) state.Changed -= ClearTransient; ReleaseAll(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseAll(); }
        public void ReleaseAll() { uiKeys.Clear(); ClearTransient(GameMode.Pause); }
        private void ClearTransient(GameMode _) { Movement = touchDirection = pendingStep = Vector2.zero; touchInteract = touchAdvance = false; }
        public void SetTouchDirection(Vector2 direction) { touchDirection = InputRules.Cardinal(direction); if (state && state.Mode == GameMode.Exploration && touchDirection != Vector2.zero) pendingStep = touchDirection; }
        public Vector2 ConsumeStep()
        {
            Vector2 result = Movement != Vector2.zero ? Movement : pendingStep;
            pendingStep = Vector2.zero;
            return state && state.Mode == GameMode.Exploration ? result : Vector2.zero;
        }
        public void TouchInteract() { touchInteract = true; }
        public void TouchAdvance() { touchAdvance = true; }
        private void Update()
        {
            if (!state) return;
            bool interact = touchInteract, advance = touchAdvance;
            Vector2 direction = touchDirection;
            if (direction == Vector2.zero) direction = new Vector2((uiKeys.Contains(KeyCode.D) ? 1 : 0) - (uiKeys.Contains(KeyCode.A) ? 1 : 0), (uiKeys.Contains(KeyCode.W) ? 1 : 0) - (uiKeys.Contains(KeyCode.S) ? 1 : 0));
            // One keyboard event source: UI Toolkit. Polling Input as well can deliver
            // the same press again on the next frame. uiKeys suppresses OS key repeat.
            touchInteract = touchAdvance = false;
            // Snapshot prevents an E callback that opens a dialogue from also advancing it.
            GameMode atStart = state.Mode;
            Movement = atStart == GameMode.Exploration ? InputRules.Cardinal(direction) : Vector2.zero;
            if (atStart == GameMode.Exploration && interact) InteractPressed?.Invoke();
            else if (atStart == GameMode.Dialogue && advance) AdvancePressed?.Invoke();
        }
    }
}
