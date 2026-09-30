using UnityEngine;

namespace CaliNature
{
    public sealed class PlayerCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private BoxCollider2D bounds;
        [SerializeField, Min(0.01f)] private float smoothing = 0.12f;
        private Vector3 velocity;
        private Camera view;
        private void Awake() { view = GetComponent<Camera>(); if (!target) foreach (var player in FindObjectsByType<PlayerGridMovement>()) if (player.enabled) { target = player.transform; break; } }
        private void LateUpdate()
        {
            if (!target) return;
            Vector3 next = new Vector3(target.position.x, target.position.y, transform.position.z);
            if (bounds && view)
            {
                Bounds b = bounds.bounds;
                float y = view.orthographicSize, x = y * view.aspect;
                next.x = b.size.x <= x * 2 ? b.center.x : Mathf.Clamp(next.x, b.min.x + x, b.max.x - x);
                next.y = b.size.y <= y * 2 ? b.center.y : Mathf.Clamp(next.y, b.min.y + y, b.max.y - y);
            }
            transform.position = Vector3.SmoothDamp(transform.position, next, ref velocity, smoothing);
        }
    }
}
