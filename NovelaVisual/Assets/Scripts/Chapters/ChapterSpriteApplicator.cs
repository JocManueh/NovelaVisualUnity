using UnityEngine;

namespace CaliNature
{
    /// Aplica los sprites de un capítulo al entrar a su zona.
    ///
    /// Los sprites son 100% configurables por datos desde StoryCatalog.json
    /// (ChapterData.npcSprites / clueSprites / mapSprite). Si la ruta está vacía
    /// se deja intacto el sprite que ya traía la escena montada.
    ///
    /// Esto permite que cada capítulo tenga su propio arte sin tocar escenas ni
    /// código: basta con rellenar la ruta del asset en el JSON.
    [DefaultExecutionOrder(200)]
    public sealed class ChapterSpriteApplicator : MonoBehaviour
    {
        private void Start() => Apply(GameSession.Instance?.CurrentChapter);

        /// Reaplica los sprites del capítulo indicado. Vacío o sin rutas = no hace nada.
        public void Apply(ChapterData chapter)
        {
            if (chapter == null) return;

            foreach (var npc in FindObjectsByType<NPCDialogueController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string resource = PathAt(chapter.npcSprites, npc.NpcIndex);
                var sprite = Load(resource);
                // -1: la ruta estaba vacía. No se toca nada.
                if (sprite) npc.SetSprite(sprite);
            }

            foreach (var clue in FindObjectsByType<ClueInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string resource = PathAt(chapter.clueSprites, clue.ClueIndex);
                var sprite = Load(resource);
                if (sprite) clue.SetSprite(sprite);
            }
        }

        private static string PathAt(string[] paths, int index)
            => paths != null && index >= 0 && index < paths.Length ? paths[index] : "";

        private static Sprite Load(string resource)
        {
            if (string.IsNullOrEmpty(resource)) return null;
            var sprite = Resources.Load<Sprite>(resource);
            if (!sprite) Debug.LogWarning("Cali: no se encontró el sprite '" + resource + "'. Se conserva el de la escena.");
            return sprite;
        }

        // El mapa vive en una escena aparte y es COMPARTIDO por los cuatro capítulos,
        // así que no se sustituye al entrar a una zona sino al SELECCIONAR un capítulo
        // en el mapa. Ver GameSession.SelectChapter.
        public static void ApplyMapSprite(ChapterData chapter)
        {
            if (chapter == null || string.IsNullOrEmpty(chapter.mapSprite)) return;
            var sprite = Load(chapter.mapSprite);
            if (!sprite) return;

            // El objeto real del mapa se llama "CaliMapIllustration" (lo busca
            // CaliMapNavigation). Se acepta el nombre alternativo por si se renombra.
            foreach (var candidate in new[] { "CaliMapIllustration", "MapBackground" })
            {
                var target = GameObject.Find(candidate);
                var renderer = target ? target.GetComponent<SpriteRenderer>() : null;
                if (!renderer) continue;
                renderer.sprite = sprite;
                return;
            }
            Debug.LogWarning("Cali: el capítulo " + chapter.id + " define mapSprite pero no se encontró el objeto del mapa.");
        }
    }
}