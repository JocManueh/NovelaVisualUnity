using UnityEngine;

namespace CaliNature
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CaliMapZoneSelector : MonoBehaviour
    {
        [SerializeField, Range(0, 3)] private int chapterIndex;
        public int ChapterIndex => chapterIndex;
        public static readonly Color IncompleteColor = new Color32(204, 42, 42, 255);
        public static readonly Color CompletedColor = new Color32(24, 132, 75, 255);
        private GameSaveManager observedSave;
        private SpriteRenderer marker;
        public bool IsCompleted
        {
            get
            {
                var session = GameSession.Instance;
                var chapters = session ? session.Catalog?.chapters : null;
                return chapters != null && chapterIndex >= 0 && chapterIndex < chapters.Length
                    && session.Save && session.Save.Data.completedChapters.Contains(chapters[chapterIndex].id);
            }
        }
        private void OnEnable() { ConnectSave(); RefreshStatus(); }
        private void Start() { ConnectSave(); RefreshStatus(); }
        private void OnDisable()
        {
            if (observedSave) observedSave.Changed -= RefreshStatus;
            observedSave = null;
        }
        private void ConnectSave()
        {
            var session = GameSession.Instance;
            var save = session ? session.Save : null;
            if (save == observedSave) return;
            if (observedSave) observedSave.Changed -= RefreshStatus;
            observedSave = save;
            if (observedSave) observedSave.Changed += RefreshStatus;
        }
        public void RefreshStatus()
        {
            if (!marker) marker = GetComponent<SpriteRenderer>();
            if (marker) marker.color = IsCompleted ? CompletedColor : IncompleteColor;
        }
        private void OnValidate()
        {
            if (!Application.isPlaying) RefreshStatus();
        }
        public void Select()
        {
            var session = GameSession.Instance;
            if (session && session.State.Mode == GameMode.Map) session.SelectChapter(chapterIndex);
        }
    }
}
