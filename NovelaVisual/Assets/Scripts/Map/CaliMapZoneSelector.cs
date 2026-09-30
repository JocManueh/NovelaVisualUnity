using UnityEngine;

namespace CaliNature
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CaliMapZoneSelector : MonoBehaviour
    {
        [SerializeField, Range(0, 3)] private int chapterIndex;
        public int ChapterIndex => chapterIndex;
        public void Select()
        {
            var session = GameSession.Instance;
            if (session && session.State.Mode == GameMode.Map) session.SelectChapter(chapterIndex);
        }
    }
}
