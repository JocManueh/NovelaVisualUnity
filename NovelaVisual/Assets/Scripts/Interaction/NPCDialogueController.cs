using UnityEngine;

namespace CaliNature
{
    public sealed class NPCDialogueController : WorldInteractable
    {
        [SerializeField, Range(0, 2)] private int npcIndex;
        public override void Interact()
        {
            var session = GameSession.Instance;
            if (session && session.CurrentChapter != null) session.Dialogue.Begin(session.CurrentChapter.conversations[npcIndex]);
        }
    }
}
