using UnityEngine;

namespace CaliNature
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerGridMovement : MonoBehaviour
    {
        [SerializeField] private GameInputRouter input;
        [SerializeField] private GameStateController state;
        [SerializeField, Min(0.1f)] private float cellSize = 0.5f;
        [SerializeField, Min(0.1f)] private float speed = 3f;
        [SerializeField] private LayerMask obstacles = ~0;
        private Rigidbody2D body;
        private Animator animator;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private Vector2 target;
        public Vector2 Facing { get; private set; } = Vector2.down;
        public bool IsStepping { get; private set; }
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); animator = GetComponent<Animator>();
            if (!input) input = FindAnyObjectByType<GameInputRouter>();
            if (!state) state = FindAnyObjectByType<GameStateController>();
            body.gravityScale = 0; body.freezeRotation = true;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
        }
        private void FixedUpdate()
        {
            if (!state || !input || state.Mode != GameMode.Exploration) { Animate(false); return; }
            if (!IsStepping)
            {
                Vector2 direction = input.Movement;
                if (direction == Vector2.zero) { Animate(false); return; }
                Facing = direction;
                if (Blocked(direction, cellSize)) { Animate(false); return; }
                target = body.position + direction * cellSize;
                IsStepping = true;
            }
            Vector2 delta = target - body.position;
            float distance = Mathf.Min(speed * Time.fixedDeltaTime, delta.magnitude);
            if (distance > 0 && Blocked(delta.normalized, distance)) { IsStepping = false; Animate(false); return; }
            Vector2 next = Vector2.MoveTowards(body.position, target, distance);
            body.MovePosition(next);
            if ((next - target).sqrMagnitude < 0.00001f) IsStepping = false;
            Animate(true);
        }
        private bool Blocked(Vector2 direction, float distance)
        {
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = obstacles };
            int count = body.Cast(direction, filter, hits, distance + 0.015f);
            for (int i = 0; i < count; i++) if (hits[i].collider && hits[i].distance < distance + 0.01f) return true;
            return false;
        }
        private void Animate(bool walking)
        {
            if (!animator) return;
            animator.SetBool("IsMoving", walking);
            animator.SetInteger("Direction", Facing.y > 0 ? 1 : Facing.y < 0 ? 0 : Facing.x > 0 ? 2 : 3);
        }
        public void PlaceAt(Transform entry)
        {
            if (!entry) return;
            IsStepping = false; body.linearVelocity = Vector2.zero;
            body.position = entry.position; transform.position = entry.position; target = body.position;
            Facing = Vector2.down; Animate(false);
        }
    }
}
