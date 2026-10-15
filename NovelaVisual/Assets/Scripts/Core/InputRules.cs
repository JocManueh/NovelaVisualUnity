using UnityEngine;

namespace CaliNature
{
    public enum DialogueAdvanceAction { None, Reveal, Next }
    public static class InputRules
    {
        public static Vector2 Cardinal(Vector2 value)
        {
            if (Mathf.Abs(value.y) > 0.01f) return new Vector2(0, Mathf.Sign(value.y));
            return Mathf.Abs(value.x) > 0.01f ? new Vector2(Mathf.Sign(value.x), 0) : Vector2.zero;
        }
        public static DialogueAdvanceAction Advance(bool active, bool typing, bool choices, int frame, int openedFrame)
        {
            if (!active || frame <= openedFrame || (!typing && choices)) return DialogueAdvanceAction.None;
            return typing ? DialogueAdvanceAction.Reveal : DialogueAdvanceAction.Next;
        }
        public static bool IsInFront(Vector2 origin, Vector2 facing, Vector2 target, float range)
        {
            Vector2 offset = target - origin;
            return offset.sqrMagnitude > 0.001f && offset.sqrMagnitude <= range * range && Vector2.Dot(offset.normalized, facing) >= 0.7f;
        }
    }
    public sealed class MapPointerGesture
    {
        public bool Active { get; private set; }
        public bool Dragged { get; private set; }
        public Vector2 Start { get; private set; }
        public Vector2 Last { get; private set; }
        public void Begin(Vector2 point) { Active = true; Dragged = false; Start = Last = point; }
        public Vector2 Move(Vector2 point, float threshold)
        {
            if (!Active) return Vector2.zero;
            if (!Dragged && Vector2.Distance(point, Start) >= threshold) Dragged = true;
            Vector2 delta = point - Last;
            Last = point;
            return Dragged ? delta : Vector2.zero;
        }
        public bool End(Vector2 point, float threshold)
        {
            if (!Active) return false;
            Move(point, threshold);
            bool click = !Dragged;
            Cancel();
            return click;
        }
        public void Cancel() { Active = false; Dragged = false; }
    }
}
