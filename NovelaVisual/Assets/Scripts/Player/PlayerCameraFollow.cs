using UnityEngine;

namespace CaliNature
{
    public sealed class PlayerCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private BoxCollider2D bounds;
        [SerializeField] private Rect worldBounds = new Rect(-17, -11, 33, 32);
        [SerializeField, Min(0.01f)] private float smoothing = 0.12f;
        private Vector3 velocity;
        private Camera view;
        private void Awake() { view = GetComponent<Camera>(); if (!target) foreach (var player in FindObjectsByType<PlayerGridMovement>()) if (player.enabled) { target = player.transform; break; } }
        private void LateUpdate()
        {
            if (!target) return;
            Vector3 next = new Vector3(target.position.x, target.position.y, transform.position.z);
            if (view)
            {
                Bounds b = bounds ? bounds.bounds : new Bounds(new Vector3(worldBounds.center.x, worldBounds.center.y, 0), new Vector3(worldBounds.width, worldBounds.height, 0));
                float y = view.orthographicSize, x = y * view.aspect;
                next.x = b.size.x <= x * 2 ? b.center.x : Mathf.Clamp(next.x, b.min.x + x, b.max.x - x);
                next.y = b.size.y <= y * 2 ? b.center.y : Mathf.Clamp(next.y, b.min.y + y, b.max.y - y);
            }
            transform.position = Vector3.SmoothDamp(transform.position, next, ref velocity, smoothing);
            if (view)
            {
                Bounds b = bounds ? bounds.bounds : new Bounds(new Vector3(worldBounds.center.x, worldBounds.center.y, 0), new Vector3(worldBounds.width, worldBounds.height, 0));
                Vector3 position = transform.position;
                float halfY = view.orthographicSize, halfX = halfY * view.aspect;
                position.x = b.size.x <= halfX * 2 ? b.center.x : Mathf.Clamp(position.x, b.min.x + halfX, b.max.x - halfX);
                position.y = b.size.y <= halfY * 2 ? b.center.y : Mathf.Clamp(position.y, b.min.y + halfY, b.max.y - halfY);
                transform.position = position;
            }
        }
    }
}
