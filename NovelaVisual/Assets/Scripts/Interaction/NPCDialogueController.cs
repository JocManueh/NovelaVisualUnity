using UnityEngine;

namespace CaliNature
{
    // Un NPC del capítulo. No guarda datos propios del diálogo: solo su posición.
    //
    // `npcIndex` cumple TRES funciones a la vez, y todas deben coincidir:
    //   1. Qué conversación se abre  →  chapter.conversations[npcIndex]
    //   2. Qué gating aplica          →  GameSession.CanTalkToNpc(npcIndex)
    //   3. Qué fase es                →  NpcPhases[npcIndex] = a / b / c
    //
    //   npcIndex 0 = Lucía (nodo A) · 1 = Mateo (nodo B) · 2 = Inés (nodo C)
    //
    // El orden es obligatorio: sin cerrar el diálogo del NPC anterior, el siguiente
    // se rechaza con un aviso. No hay que bloquear nada en la escena; el chequeo es
    // aquí, en el momento de interactuar.
    public sealed class NPCDialogueController : WorldInteractable
    {
        [SerializeField, Range(0, 2)] private int npcIndex;
        public int NpcIndex => npcIndex;
        private SpriteRenderer spriteRenderer;

        // Lo llama ChapterSpriteApplicator al entrar a la zona, con el sprite que
        // el capítulo declara en ChapterData.npcSprites[npcIndex]. Ruta vacía = no tocar.
        public void SetSprite(Sprite sprite)
        {
            if (!sprite) return;
            if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer) spriteRenderer.sprite = sprite;
        }

        public override void Interact()
        {
            var session = GameSession.Instance;
            if (!session || session.CurrentChapter == null) return;

            // Gate: revisar antes de abrir, o el jugador podría saltarse el orden.
            if (!session.CanTalkToNpc(npcIndex)) return;

            // La conversación vive en el capítulo activo. Cada una contiene TODAS
            // las variantes de rama de ese NPC; DialogueController.EntryNode()
            // selecciona cuál de ellas mostrar según las decisiones anteriores.
            session.Dialogue.Begin(session.CurrentChapter.conversations[npcIndex]);
        }
    }
}