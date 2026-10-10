using UnityEngine;

namespace CaliNature
{
    public sealed class ClueInteractable : WorldInteractable
    {
        [SerializeField, Range(0, 2)] private int clueIndex;
        public override void Interact()
        {
            var session = GameSession.Instance;
            if (!session || session.CurrentChapter == null) return;
            ClueData clue = session.CurrentChapter.clues[clueIndex];
            session.Save.AddClue(clue.id);
            session.UI.ShowNotice(clue.title + "\n\n" + clue.text);
            session.CheckChapterCompletion();
        }
    }
}
