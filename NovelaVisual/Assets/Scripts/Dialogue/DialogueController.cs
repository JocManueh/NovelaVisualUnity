using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaliNature
{
    public sealed class DialogueController : MonoBehaviour
    {
        [SerializeField] private GameStateController state;
        [SerializeField] private GameInputRouter input;
        [SerializeField] private GameSaveManager save;
        [SerializeField] private DialogueNarrationPlayer narration;
        private ConversationData conversation;
        private DialogueNode node;
        private float visible;
        private int openedFrame, lastAdvanceFrame = -1;
        private readonly List<string> history = new List<string>();
        public event Action Changed;
        public event Action Ended;
        public DialogueNode Current => node;
        public bool IsTyping => node != null && visible < node.text.Length;
        public int VisibleCharacters => Mathf.Min(node?.text.Length ?? 0, Mathf.FloorToInt(visible));
        public IReadOnlyList<string> History => history;
        public bool ShowingChoices => node != null && !IsTyping && node.choices != null && node.choices.Length > 0;
        private void Awake()
        {
            if (!state) state = GetComponent<GameStateController>(); if (!input) input = GetComponent<GameInputRouter>();
            if (!save) save = GetComponent<GameSaveManager>(); if (!narration) narration = GetComponent<DialogueNarrationPlayer>();
        }
        private void OnEnable() { if (input) input.AdvancePressed += Advance; }
        private void OnDisable() { if (input) input.AdvancePressed -= Advance; if (narration) narration.Stop(); }
        public bool Begin(ConversationData data)
        {
            if (!state || state.Mode != GameMode.Exploration || data == null) return false;
            conversation = data; history.Clear(); openedFrame = Time.frameCount;
            state.SetMode(GameMode.Dialogue); ShowNode(data.firstNode); return true;
        }
        private void ShowNode(string id)
        {
            narration.Stop();
            if (string.IsNullOrEmpty(id)) { Finish(); return; }
            node = Array.Find(conversation.nodes, item => item.id == id);
            if (node == null) { Debug.LogError("Nodo de diálogo inexistente: " + id); Finish(); return; }
            node.text ??= ""; visible = 0;
            history.Add(node.speaker + ": " + node.text);
            narration.Play(conversation.id + "/" + node.id, node.voiceResource);
            Changed?.Invoke();
        }
        private void Update()
        {
            if (state.Mode != GameMode.Dialogue || !IsTyping) return;
            visible = Mathf.Min(node.text.Length, visible + save.Data.textSpeed * Time.unscaledDeltaTime);
            Changed?.Invoke();
        }
        public void Advance()
        {
            if (lastAdvanceFrame == Time.frameCount) return;
            var action = InputRules.Advance(state.Mode == GameMode.Dialogue && node != null, IsTyping, ShowingChoices, Time.frameCount, openedFrame);
            if (action == DialogueAdvanceAction.None) return;
            lastAdvanceFrame = Time.frameCount;
            if (action == DialogueAdvanceAction.Reveal) { visible = node.text.Length; Changed?.Invoke(); }
            else ShowNode(node.next);
        }
        public void Choose(int index)
        {
            if (state.Mode != GameMode.Dialogue || !ShowingChoices || index < 0 || index >= node.choices.Length) return;
            DialogueChoice choice = node.choices[index];
            if (!save.HasClue(choice.requiredClue)) return;
            save.RecordDecision(choice); history.Add("Tú: " + choice.label);
            lastAdvanceFrame = Time.frameCount;
            ShowNode(choice.next);
        }
        private void Finish()
        {
            narration.Stop(); node = null; conversation = null;
            state.SetMode(GameMode.Exploration); Changed?.Invoke(); Ended?.Invoke();
        }
    }
}
