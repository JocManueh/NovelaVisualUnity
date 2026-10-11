using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
//  MOTOR DE DIÁLOGO
// ----------------------------------------------------------------------------
//  Recorre los nodos de UNA conversación (la del NPC con el que el jugador
//  interactuó). Este controlador no sabe nada del árbol de decisiones: solo
//  sigue punteros `next`. Toda la lógica de ramas vive en los datos:
//
//   • ¿Con qué nodo SE ABRE la conversación?  → EntryNode(), leyendo `routes`
//   • ¿Qué opciones están bloqueadas?         → DialogueChoice.requiredClue
//   • ¿Se ganó o se perdió el capítulo?       → DialogueChoice.outcome
//
//  Flujo de una partida típica:
//      Interact() en NPC A  → Begin(conversations[0]) → nodo a_start → elige A1
//      Interact() en NPC B  → Begin(conversations[1]) → EntryNode ve "A1" → bx_start
//      Interact() en NPC C  → Begin(conversations[2]) → EntryNode ve "B3" → cgood_start
//      elige C1 (outcome "win") → Ended → GameSession completa el capítulo
// ============================================================================
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
            if (GameSession.Instance) GameSession.Instance.NotifyDialogueStarted();
            state.SetMode(GameMode.Dialogue); ShowNode(EntryNode(data)); return true;
        }
        // ── SELECCIÓN DE RAMA ───────────────────────────────────────────────
        // Elige con qué nodo abre la conversación según lo que el jugador YA
        // respondió en el NPC anterior (guardado en SaveData.decisions).
        //
        // Por ejemplo, la conversación del NPC C tiene estas reglas:
        //     { decisionKey: "drought/b", choice: "B1", node: "cmid_start" }
        //     { decisionKey: "drought/b", choice: "B3", node: "cgood_start" }
        //     ...
        // Se devuelve la PRIMERA regla que coincida. Gana la que aparece antes en
        // el array, así que no debe haber dos reglas ambiguas para la misma decisión.
        //
        // Fallback: si el jugador no tiene ninguna decisión registrada todavía
        // (entra a un capítulo sin haber hablado con nadie), se usa `firstNode`.
        private string EntryNode(ConversationData data)
        {
            foreach (var rule in data.routes ?? Array.Empty<RouteRule>())
            {
                if (string.IsNullOrEmpty(rule.decisionKey) || string.IsNullOrEmpty(rule.node)) continue;
                var record = save.Data.decisions.Find(x => x.key == rule.decisionKey);
                if (record == null) continue;
                if (!string.IsNullOrEmpty(rule.choice) && record.choice != rule.choice) continue;
                return rule.node;
            }
            return data.firstNode;
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
        // ── ELEGIR UNA RESPUESTA ────────────────────────────────────────────
        // `index` es la posición VISIBLE en la lista. El orden de las opciones es
        // barajado por capítulo en el JSON, por eso no se busca por id: se usa
        // el índice tal cual lo pasó la UI.
        public void Choose(int index)
        {
            if (state.Mode != GameMode.Dialogue || !ShowingChoices || index < 0 || index >= node.choices.Length) return;

            DialogueChoice choice = node.choices[index];

            // BLOQUEO POR PISTA. requiredClue vacío = opción siempre disponible.
            // HasClue("") devuelve true por diseño. Si falta la pista, la opción
            // llega deshabilitada desde la UI y aquí se descarta por si acaso.
            if (!save.HasClue(choice.requiredClue)) return;

            // Guardar la decisión es lo que alimenta `routes` del NPC siguiente.
            // RecordDecision ignora claves ya presentes: si el jugador reabre un NPC
            // ya respondido, la PRIMERA respuesta es la que cuenta y no se pisa.
            save.RecordDecision(choice);
            history.Add("Tú: " + choice.label);

            // Si la opción trae "win"/"lose", se avisa a GameSession para que resuelva
            // el capítulo cuando este diálogo termine. Las opciones de A y B van con
            // outcome vacío: aquí todavía no se decide nada.
            if (GameSession.Instance) GameSession.Instance.NotifyChoice(choice);

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
