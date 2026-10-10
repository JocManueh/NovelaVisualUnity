using System;

namespace CaliNature
{
    [Serializable] public sealed class StoryCatalog { public ChapterData[] chapters; }
    [Serializable] public sealed class ChapterData
    {
        public string id, title, subtitle, phenomenon, period, objective, description, summary, sceneName;
        public string[] sources;
        public ClueData[] clues;
        public ConversationData[] conversations;
        public CinematicCard[] cinematic;
    }
    [Serializable] public sealed class ClueData { public string id, title, text; }
    [Serializable] public sealed class CinematicCard { public string title, text, imageResource, voiceResource; public float seconds = 5; }
    [Serializable] public sealed class ConversationData { public string id, firstNode; public DialogueNode[] nodes; }
    [Serializable] public sealed class DialogueNode
    {
        public string id, speaker, text, portraitResource, voiceResource, next;
        public DialogueChoice[] choices;
    }
    [Serializable] public sealed class DialogueChoice
    {
        public string id, label, next, requiredClue, decisionKey, consequence;
        public bool responsible;
    }
}
