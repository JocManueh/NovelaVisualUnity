using UnityEngine;

namespace CaliNature
{
    [RequireComponent(typeof(Camera))]
    public sealed class CaliMapNavigation : MonoBehaviour
    {
        [SerializeField] private float minimumZoom = 3, maximumZoom = 14;
        [SerializeField] private Vector2 minimumPosition = new Vector2(-20, -20), maximumPosition = new Vector2(20, 20);
        [SerializeField, Min(3)] private float dragThresholdPixels = 10;
        private Camera view;
        private readonly MapPointerGesture gesture = new MapPointerGesture();
        private int pointerId = -1;
        private bool Available => GameSession.Instance && GameSession.Instance.State.Mode == GameMode.Map;
        private void Awake() { view = GetComponent<Camera>(); }
        private void OnDisable() { Cancel(); }
        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        private void Update() { if (!Available) Cancel(); }
        public void Cancel() { gesture.Cancel(); pointerId = -1; }
        // UI Toolkit supplies screen coordinates for mouse and touch through the same event path.
        public bool BeginPointer(int id, Vector2 point)
        {
            if (!Available || gesture.Active || GameSession.Instance.UI.IsPointerBlocked(point)) return false;
            pointerId = id; gesture.Begin(point); return true;
        }
        public void MovePointer(int id, Vector2 point)
        {
            if (!Available || id != pointerId || !gesture.Active) return;
            Vector2 delta = gesture.Move(point, dragThresholdPixels);
            if (delta == Vector2.zero) return;
            Vector3 position = transform.position - (view.ScreenToWorldPoint(point) - view.ScreenToWorldPoint(point - delta));
            position.x = Mathf.Clamp(position.x, minimumPosition.x, maximumPosition.x);
            position.y = Mathf.Clamp(position.y, minimumPosition.y, maximumPosition.y);
            transform.position = position;
        }
        public void EndPointer(int id, Vector2 point)
        {
            if (id != pointerId) return;
            bool click = gesture.End(point, dragThresholdPixels);
            pointerId = -1;
            if (!Available || !click || GameSession.Instance.UI.IsPointerBlocked(point)) return;
            foreach (Collider2D hit in Physics2D.OverlapPointAll(view.ScreenToWorldPoint(point)))
            {
                var marker = hit.GetComponent<CaliMapZoneSelector>();
                if (marker && marker.isActiveAndEnabled) { marker.Select(); break; }
            }
        }
        public void Zoom(float steps)
        {
            if (!Available) return;
            if (!view) view = GetComponent<Camera>();
            view.orthographicSize = Mathf.Clamp(view.orthographicSize * Mathf.Pow(1.12f, steps), minimumZoom, maximumZoom);
        }
    }
}
