using UnityEngine;

namespace CaliNature
{
    // Una pista del capítulo.
    //
    // REGLA: no se puede recoger antes de haber hablado con Lucía (nodo A).
    // El objeto permanece visible en el mapa para que el jugador vea que ahí hay
    // algo por investigar, pero no registra la pista ni desbloquea opciones.
    //
    // Además el sprite es configurable por capítulo desde StoryCatalog.json
    // (ChapterData.clueSprites[clueIndex]); ver ChapterSpriteApplicator.
    public sealed class ClueInteractable : WorldInteractable
    {
        [SerializeField, Range(0, 2)] private int clueIndex;

        public int ClueIndex => clueIndex;
        private SpriteRenderer spriteRenderer;

        // Lo llama ChapterSpriteApplicator al entrar a la zona. Ruta vacía = no tocar.
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

            // Antes de seguir hay que hablar con la primera persona. Sin esta
            // comprobación el jugador podría recorrer las pistas antes de obtener
            // el contexto que les da sentido.
            if (!session.CanCollectClues())
            {
                session.UI.ShowNotice("Todavía no sabes qué buscar. Habla primero con Lucía.");
                return;
            }

            // El contenido y el id dependen de la rama elegida en A, no del objeto que se
            // examinó. Si vas por By, cualquier objeto te entrega la pista de By;
            // si vas por Bx, la de Bx. Así nunca se puede recoger una pista que
            // no abra la rama en la que estás.
            ClueData clue = session.ResolveClue(session.CurrentChapter.clues[clueIndex]);

            // Se vuelve a mostrar aunque ya estuviera recogida: el jugador pudo
            // recogerla en un intento anterior y ahora estar en otra rama.
            session.Save.AddClue(clue.id);
            session.UI.ShowNotice(clue.title + "\n\n" + clue.text);
        }
    }
}