using System;

// ============================================================================
//  MODELO DE DATOS DEL SISTEMA DE DIÁLOGO
// ----------------------------------------------------------------------------
//  Este proyecto NO tiene código por capítulo. Todo el diálogo vive como datos en
//  Assets/Resources/CaliNature/StoryCatalog.json y estas clases son su esquema.
//
//  EL ÁRBOL DE DECISIONES (idéntico en los 4 capítulos)
//
//      NPC A (Lucía)   ─┬─ A1 ─┐
//                        └─ A2 ─┴───────────────► Bx  (Mateo)
//                            └─ A3 ──────────────► By  (Mateo)
//
//      Bx (Mateo)       ─┬─ B1 ─┐
//                        └─ B2 ─┴───────────────► Cmid
//                            └─ B3 ──────────────► Cgood   ◄ requiere la pista 0
//
//      By (Mateo)       ─┬─ B4 ─┐
//                        └─ B5 ─┴───────────────► Cbad / Cdead
//                            └─ B6 ──────────────► Cmid   ◄ requiere la pista 1
//
//      NPC C (Inés) — el desenlace final ocurre aquí:
//        Cgood   C1 ✓  C2 ✓  C3 ✗      ← ÚNICA rama con mayoría de ganadoras
//        Cmid    C1 ✓  C2 ✗  C3 ✗
//        Cbad    C1 ✗  C2 ✗  C3 ✗
//        Cdead   C1 ✗  C2 ✗  C3 ✗
//
//  Idea de diseño: la pista NO da la respuesta winning, solo abre la puerta a la
//  rama donde las respuestas favorables son MAYORITARIAS. El jugador puede ganar
//  sin pista, pero con menos frecuencia. Esto evita tells obvios en el nivel C.
//
//  SIN la pista: B1/B2/B4/B5 → techo de 1 victoria de 3.
//  CON la pista 0: B3 → Cgood → 2 victorias de 3.
// ============================================================================

namespace CaliNature
{
    // Raíz del catálogo. El JSON tiene un único array "chapters".
    [Serializable] public sealed class StoryCatalog { public ChapterData[] chapters; }

    // Un capítulo = un caso de estudio autónomo. No depende de ningún otro capítulo:
    // se puede entrar a cualquiera en cualquier orden.
    [Serializable] public sealed class ChapterData
    {
        public string id, title, subtitle, phenomenon, period, objective, description, summary, sceneName;
        public string[] sources;
        public ClueData[] clues;
        // SIEMPRE exactamente 3, en orden A, B, C. El índice lo usa npcIndex en la escena.
        public ConversationData[] conversations;
        public CinematicCard[] cinematic;

        // ── TEXTO DE CIERRE ──────────────────────────────────────────────────
        // Lo que se lee en la pantalla final, justo después de la última
        // conversación y antes de volver al mapa. El título lo pone el juego
        // ("Ganaste el capítulo" / "Perdiste el capítulo"); el cuerpo es estos
        // campos, que son de escritura de cada capítulo.
        //
        // Hoy están en texto de relleno: cada equipo debe escribir el suyo.
        // Si vienen vacíos, la pantalla cae al `summary` del capítulo.
        public string endingWin;
        public string endingLose;

        // ── SPRITES POR CAPÍTULO ────────────────────────────────────────────
        // Rutas de asset en Resources, SIN extensión (p. ej. "CaliNature/NPCs/lucia_sequia").
        // Cadena vacía = "deja el que ya hay en la escena".
        //
        // Hoy los tres están vacíos: el juego usa los sprites con los que ya viene
        // montado cada escena. Cuando haya arte propio, basta con rellenar estas
        // rutas en el JSON; no hay que tocar código ni escenas.
        //
        //   npcSprites[i]  → NPC con npcIndex == i  (0 Lucía, 1 Mateo, 2 Inés)
        //   clueSprites[i] → pista con clueIndex == i
        //   mapSprite     → fondo del mapa de este capítulo
        public string[] npcSprites;
        public string[] clueSprites;
        public string mapSprite;
    }

    // ── PISTA ───────────────────────────────────────────────────────────
// `text` es el contenido por defecto. `variants` permite que la pista se lea
// de forma distinta según la rama de B a la que llevó la decisión en A:
//
//   "variants": [
//     { "branch": "bx_start", "title": "...", "text": "..." },
//     { "branch": "by_start", "title": "...", "text": "..." }
//   ]
//
// El `branch` es el id del nodo de B abierto por la decisión tomada en A
// ("bx_start" si fue A1/A2, "by_start" si fue A3). Si no hay variante para la
// rama actual, se usa el `text` por defecto.
//
// POR QUÉ EXISTE: la pista que abre la rama buena es distinta en Bx y en By.
// Sin esto el jugador examina el mismo documento en ambos caminos y no tiene
// forma de saber cuál le sirve: el bloqueo de `requiredClue` se volvía opaco.
[Serializable] public sealed class ClueData
{
    public string id, title, text;
    public ClueVariant[] variants;
}

[Serializable] public sealed class ClueVariant { public string branch, title, text; }
    [Serializable] public sealed class CinematicCard { public string title, text, imageResource, voiceResource; public float seconds = 5; }

    // ── NODO ────────────────────────────────────────────────────────────────
    // Una línea que dice un NPC. `choices` vacío = parlamento sin decisiones.
    [Serializable] public sealed class DialogueNode
    {
        public string id, speaker, text, portraitResource, voiceResource, next;
        public DialogueChoice[] choices;
    }

    // ── OPCIÓN ──────────────────────────────────────────────────────────────
    // Una respuesta del jugador. Elegirla avanza al nodo `next` de ESTA MISMA
    // conversación (no puede saltar a otro NPC: eso lo decide `routes` al reabrir).
    [Serializable] public sealed class DialogueChoice
    {
        public string id, label, next, requiredClue, decisionKey, consequence;

        // Flag pedagógico: marca la opción "más responsable". Se guarda en la partida
        // pero hoy NO altera el desenlace; existe para cuando se undefa el veredicto
        // final por calidad de decisión en lugar de por ruta.
        public bool responsible;

        // "win" / "lose" / vacío (""). SOLO los nodos de cierre del NPC C (Cgood, Cmid,
        // Cbad, Cdead) llevan valor. GameSession.ResolveOutcome lo lee al terminar
        // el diálogo para completar o borrar el capítulo.
        public string outcome;
    }

    // ── CONVERSACIÓN ────────────────────────────────────────────────────────
    // Una conversación = TODOS los nodos posibles de UN NPC, no un camino recto.
    //
    // Al abrirse, DialogueController.EntryNode() busca la primera regla de `routes`
    // cuya `decisionKey` ya esté registrada en la partida y abre ese nodo. Si ninguna
    // coincide (p. ej. jugador sin decisiones previas), cae a `firstNode`.
    //
    // Esto permite que el NPC B tenga dos versiones distintas (Bx y By) y el NPC C
    // cuatro (Cgood, Cmid, Cbad, Cdead) sin romper el mapeo npcIndex → conversations[n].
    [Serializable] public sealed class ConversationData { public string id, firstNode; public RouteRule[] routes; public DialogueNode[] nodes; }

    // Una regla de entrada: "si en la decisión `decisionKey` se eligió `choice`,
    // este NPC abre en el nodo `node`". Si `choice` viene vacío, la regla aplica
    // con cualquier respuesta de esa decisión.
    [Serializable] public sealed class RouteRule { public string decisionKey, choice, node; }
}