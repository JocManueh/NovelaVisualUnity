using System;
using UnityEngine;

namespace CaliNature
{
    [RequireComponent(typeof(PlayerGridMovement))]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private GameInputRouter input;
        [SerializeField] private GameStateController state;
        [SerializeField] private LayerMask blockers = ~0;
        private PlayerGridMovement movement;
        private WorldInteractable candidate;
        public event Action<string> PromptChanged;
        public string CurrentPrompt => candidate ? "E · " + candidate.Prompt : "";
        private void Awake()
        {
            movement = GetComponent<PlayerGridMovement>();
            if (!input) input = FindAnyObjectByType<GameInputRouter>();
            if (!state) state = FindAnyObjectByType<GameStateController>();
        }
        private void OnEnable() { if (input) input.InteractPressed += Interact; }
        private void OnDisable() { if (input) input.InteractPressed -= Interact; }
        private void Update() { SetCandidate(FindCandidate()); }
        private void SetCandidate(WorldInteractable next)
        {
            if (candidate == next) return;
            candidate = next; PromptChanged?.Invoke(CurrentPrompt);
        }
        private WorldInteractable FindCandidate()
        {
            if (!state || state.Mode != GameMode.Exploration || movement.IsStepping) return null;
            WorldInteractable best = null; float nearest = float.MaxValue;
            foreach (var item in FindObjectsByType<WorldInteractable>())
            {
                if (!item.isActiveAndEnabled || !InputRules.IsInFront(transform.position, movement.Facing, item.transform.position, item.Range)) continue;
                Vector2 delta = item.transform.position - transform.position;
                bool occluded = false;
                foreach (var hit in Physics2D.RaycastAll(transform.position, delta.normalized, delta.magnitude, blockers))
                {
                    if (hit.collider.isTrigger || hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(item.transform)) continue;
                    occluded = true; break;
                }
                if (!occluded && delta.sqrMagnitude < nearest) { best = item; nearest = delta.sqrMagnitude; }
            }
            return best;
        }
        private void Interact()
        {
            var item = FindCandidate();
            if (!item) return;
            SetCandidate(null); item.Interact();
        }
    }
}
