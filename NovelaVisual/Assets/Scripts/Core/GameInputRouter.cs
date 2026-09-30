using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

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
        private bool touchInteract, touchAdvance;
        private readonly HashSet<KeyCode> uiKeys = new HashSet<KeyCode>();
        public void KeyPressed(KeyCode key)
        {
            if (!uiKeys.Add(key)) return;
            if (key == KeyCode.E && state.Mode == GameMode.Exploration) touchInteract = true;
            if (key == KeyCode.Space && state.Mode == GameMode.Dialogue) touchAdvance = true;
        }
        public void KeyReleased(KeyCode key) { uiKeys.Remove(key); }
        private void Awake() { if (!state) state = GetComponent<GameStateController>(); }
        private void OnEnable() { if (state) state.Changed += ClearTransient; }
        private void OnDisable() { if (state) state.Changed -= ClearTransient; ClearTransient(GameMode.Menu); }
        private void OnApplicationFocus(bool focused) { if (!focused) { uiKeys.Clear(); ClearTransient(GameMode.Pause); } }
        private void ClearTransient(GameMode _) { Movement = touchDirection = Vector2.zero; touchInteract = touchAdvance = false; }
        public void SetTouchDirection(Vector2 direction) { touchDirection = InputRules.Cardinal(direction); }
        public void TouchInteract() { touchInteract = true; }
        public void TouchAdvance() { touchAdvance = true; }
        private void Update()
        {
            if (!state) return;
            bool interact = touchInteract, advance = touchAdvance;
            Vector2 direction = touchDirection;
            if (direction == Vector2.zero) direction = new Vector2((uiKeys.Contains(KeyCode.D) ? 1 : 0) - (uiKeys.Contains(KeyCode.A) ? 1 : 0), (uiKeys.Contains(KeyCode.W) ? 1 : 0) - (uiKeys.Contains(KeyCode.S) ? 1 : 0));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (direction == Vector2.zero) direction = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                interact |= keyboard.eKey.wasPressedThisFrame;
                advance |= keyboard.spaceKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (direction == Vector2.zero) direction = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0), (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            interact |= Input.GetKeyDown(KeyCode.E);
            advance |= Input.GetKeyDown(KeyCode.Space);
#endif
            touchInteract = touchAdvance = false;
            // Snapshot prevents an E callback that opens a dialogue from also advancing it.
            GameMode atStart = state.Mode;
            Movement = atStart == GameMode.Exploration ? InputRules.Cardinal(direction) : Vector2.zero;
            if (atStart == GameMode.Exploration && interact) InteractPressed?.Invoke();
            else if (atStart == GameMode.Dialogue && advance) AdvancePressed?.Invoke();
        }
    }
}
