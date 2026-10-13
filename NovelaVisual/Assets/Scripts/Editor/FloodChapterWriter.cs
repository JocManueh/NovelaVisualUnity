using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CaliNature.Editor
{
    /// Escribe el contenido real del capítulo de INUNDACIÓN (flood).
    ///
    /// Se ejecuta una vez y luego el capítulo vive en StoryCatalog.json, igual que
    /// los demás. Este script es la fuente con la que se generó, por si hay que
    /// regenerarlo.
    ///
    /// ── CÓMO SE ESCONDE QUÉ ELECCIÓN PIERDE ────────────────────────────────
    /// Ninguna etiqueta anuncia el destino ni la calidad del desenlace. Las tres
    /// opciones de cada nodo suenan razonables y todas tienen al menos una forma de
    /// ganar en algún camino. Lo que separa el bien del mal es un detalle de fondo:
    ///
    ///   · La opción "heroica" suele ser la equivocada (evacuar por exceso, bajar a
    ///     la orilla, tapar los ojos). Es la que el jugador elige por instinto.
    ///   · La opción correcta suele sonar burocrática o legalista (revisar la fecha,
    ///     anotar quién puso qué, decir cuándo se reabre). Es la que el jugador
    ///     descarta por poco interesante.
    ///
    /// ── POR QUÉ CADA RAMBA PIERDE EN UNA OPCIÓN DISTINTA ───────────────────
    /// Antes, C1 ganaba en las cuatro ramas: "elegir siempre C1" ganaba siempre.
    /// Ahora cada rama reparte la victoria de forma distinta y ningún id domina:
    ///
    ///   Cgood  C1 ✓  C2 ✓  C3 ✗      Cmid  C1 ✓  C2 ✗  C3 ✓
    ///   Cbad   C1 ✗  C2 ✓  C3 ✗      Cdead C1 ✗  C2 ✗  C3 ✓
    ///
    /// Cada opción gana en 2 de 4 ramas: ninguna estrategia fija sirve.
    public static class FloodChapterWriter
    {
        const string ChapterId = "flood";

        // ── Utilidades de construcción ───────────────────────────────────────
        static DialogueChoice Ch(string id, string label, string next, string clue,
            string key, string outcome, bool responsible, string consequence) => new DialogueChoice
        {
            id = id, label = label, next = next, requiredClue = clue,
            decisionKey = key, outcome = outcome, responsible = responsible,
            consequence = consequence,
        };

        static DialogueNode Nd(string id, string speaker, string text, params DialogueChoice[] choices) => new DialogueNode
        {
            id = id, speaker = speaker, text = text, next = "",
            choices = choices, portraitResource = "", voiceResource = "",
        };

        [MenuItem("Cali/Escribir diálogos del capítulo de inundación")]
        public static void Write()
        {
            var asset = Resources.Load<TextAsset>("CaliNature/StoryCatalog");
            var catalog = JsonUtility.FromJson<StoryCatalog>(asset.text);
            var chapter = catalog.chapters.FirstOrDefault(c => c.id == ChapterId);
            if (chapter == null) { Debug.LogError("Cali: no está el capítulo " + ChapterId + "."); return; }

            const string keyA = ChapterId + "/a";
            const string keyB = ChapterId + "/b";
            const string keyC = ChapterId + "/c";
            const string clue0 = ChapterId + "/clue/0";   // croquis: abre B3
            const string clue1 = ChapterId + "/clue/1";   // boletín: abre B6

            // ── NPC A · Lucía ─────────────────────────────────────────────────
            // A1 y A2 llevan a Bx, A3 lleva a By. A3 ES LA TRAMPA: suena la más
            // responsable (avisar temprano) y mete en la rama más dura sin pistas.
            var nodeA = Nd("a_start", "Lucía",
                "Soy Lucía, de la iniciativa del barrio. El río volvió a subir y ya circulan tres versiones de lo que pasó. Una foto de la margen mojada anda como si probara que la zona se inundó. ¿Por dónde empezamos?",
                Ch("A2", "Separemos tres cosas: dónde está el cauce, qué construimos en sus márgenes y qué está pasando hoy.", "a_end", "", keyA, "", true,
                    "Anotas el cauce, los márgenes y el día de hoy como tres asuntos distintos."),
                Ch("A3", "Avisemos ya, mientras la gente todavía pueda sacar lo suyo de ahí.", "a_end", "", keyA, "", true,
                    "Anotas que conviene avisar temprano, aunque la información esté incompleta."),
                Ch("A1", "Antes de repetir nada, revisemos quién publicó esa foto y de qué día es.", "a_end", "", keyA, "", true,
                    "Anotas que la foto necesita fuente y fecha antes de volver a circular."));

            // ── NPC B · Mateo, rama Bx ─────────────────────────────────────────
            // B2 ES LA TRAMPA: "cerrar por exceso" suena prudente y lleva a Cbad.
            // B3 es la correcta pero suena la más floja ("no sirve para nada serio").
            var nodeBx = Nd("bx_start", "Mateo",
                "Soy Mateo, de la cuadra de abajo. Lucía me contó por dónde ibas a empezar. Yo no tengo ni el plano ni el aviso; lo que tengo es lo que vi desde el patio.",
                Ch("B2", "Digamos desde ya que la zona dejó de ser transitable y cerremos el paso de abajo. Es preferible pasarnos de cautions que quedarnos cortos.", "b_end", "", keyB, "", false,
                    "Queda acordado cerrar el paso de la margen antes de evaluar nada."),
                Ch("B1", "Cada cuadra anote hasta dónde le llegó el agua. Ese dato no sale de un plano: sale de nosotros.", "b_end", "", keyB, "", true,
                    "Queda acordada una medición casera de alturas, cuadra por cuadra."),
                Ch("B3", "El croquis ese no sirve para nada serio, pero sí para algo: dónde mirar primero y a quién preguntarle por el nivel.", "b_end", clue0, keyB, "", true,
                    "Anotas para qué sirve el croquis y para qué no."));

            // ── NPC B · Mateo, rama By ─────────────────────────────────────────
            // B4 ES LA TRAMPA MÁS PELIGROSA: "bajar a mirar" parece la opción más
            // rigurosa y es la que más cuesta (Cdead).
            var nodeBy = Nd("by_start", "Mateo",
                "Soy Mateo. Lucía ya avisó y media cuadra sacó cajas al patio. Yo no vi subir el agua; vi a la gente mover cosas. ¿Qué hacemos ahora?",
                Ch("B4", "Bajo a la orilla a mirar hasta dónde llegó. Así hablamos con un dato propio y no con el rumor.", "b_end", "", keyB, "", false,
                    "Anotas bajar a la orilla a comprobarlo."),
                Ch("B6", "Revisemos el emisor y la fecha del aviso. Si sigue vigente, ya tenemos en qué basarnos.", "b_end", clue1, keyB, "", true,
                    "Anotas que el aviso sirve si se confirma su vigencia."),
                Ch("B5", "Pidamos que se despeje la zona de la margen. Antes que lamentar, exagerar.", "b_end", "", keyB, "", false,
                    "Anotas pedir el despeje de la margen."));

            // ── NPC C · Inés, rama favorable ──────────────────────────────────
            // C3 PIERDE aquí: "no se afirma nada sin estudio oficial" es la lección
            // del capítulo convertida en parálisis. Suena la más prudente.
            var nodeCgood = Nd("cgood_start", "Inés",
                "Soy Inés. Llevo la libreta del proyecto. Escribamos lo que le sirva a la gente mañana, no lo que nos quede bien a nosotros.",
                Ch("C2", "Anoto qué se puede usar: dónde mirar, a quién preguntarle y qué está sin confirmar.", "c_end", "", keyC, "win", true,
                    "La libreta queda con criterios que la comunidad puede comprobar por su cuenta."),
                Ch("C3", "Anoto que no se puede afirmar nada sin un estudio oficial. Es lo más prudente.", "c_end", "", keyC, "lose", false,
                    "La libreta queda con una advertencia y nada que hacer."),
                Ch("C1", "Anoto una versión corta: qué sabemos, qué no, y quién trae lo que falta.", "c_end", "", keyC, "win", true,
                    "La libreta queda con un resumen corto y un pendiente con nombre."));

            // ── NPC C · Inés, rama de mediciones caseras ───────────────────────
            // C2 PIERDE: dejar los datos separados "porque nadie leerá un mapa raro"
            // suena prudente y tira el trabajo a la basura.
            var nodeCmid = Nd("cmid_start", "Inés",
                "Soy Inés. Llegaron las alturas que anotó cada cuadra. Salieron desigualadas, pero son nuestras. ¿Cómo las dejamos?",
                Ch("C3", "Dejo constancia de quién anotó qué y en qué fecha, para que se pueda revisar.", "c_end", "", keyC, "win", true,
                    "Cada dato queda con autor y fecha, y se puede auditar."),
                Ch("C2", "Mejor las dejamos en el cuaderno de cada cuadra, sin juntarlas. Nadie va a leer un mapa raro.", "c_end", "", keyC, "lose", false,
                    "Los registros quedan dispersos y no se pueden comparar entre sí."),
                Ch("C1", "Las ordenamos y publicamos un mapa de la comunidad, aunque sea un borrador.", "c_end", "", keyC, "win", true,
                    "Se publica un mapa colaborativo, todavía en borrador."));

            // ── NPC C · Inés, rama de zona cerrada ─────────────────────────────
            // C2 GANA y es la más burocrática: decir QUÉ se cerró, DESDE CUÁNDO y
            // A QUIÉN llamar. C1 y C3 pierden, y C3 es "lo más prudente".
            var nodeCbad = Nd("cbad_start", "Inés",
                "Soy Inés. Ya está cerrado el paso y la gente pregunta si se puede volver a la casa. Tengo que escribir algo hoy. ¿Qué pongo?",
                Ch("C3", "Pongo 'zona cerrada temporalmente'. Es lo más prudente y no compromete a nadie.", "c_end", "", keyC, "lose", false,
                    "La libreta cierra sin decir cómo se vuelve a entrar."),
                Ch("C1", "Pongo que el barrio es zona de riesgo. Es lo que todos esperan leer.", "c_end", "", keyC, "lose", false,
                    "La libreta declara riesgo una zona que nadie evaluó."),
                Ch("C2", "Pongo qué se cerró, desde cuándo, y a quién llamar para reabrirlo.", "c_end", "", keyC, "win", true,
                    "La libreta dice cuándo se reabre y a quién llamar."));

            // ── NPC C · Inés, rama de la orilla ───────────────────────────────
            // C3 GANA y es la que separa observación de anuncio. C1 es la que todos
            // quieren oír, y por eso pierde.
            var nodeCdead = Nd("cdead_start", "Inés",
                "Soy Inés. Tú y Mateo bajaron a la orilla. Nadie los vio cruzar, pero ya corre el rumor de que el agua llegó a la calle. ¿Qué escribo?",
                Ch("C1", "Escribo que la margen se inundó. Es lo que todos quieren oír y lo van a decir igual.", "c_end", "", keyC, "lose", false,
                    "La libreta convierte una observación en un anuncio."),
                Ch("C2", "No escribo nada todavía. Si no lo cuento, el rumor se enfría solo.", "c_end", "", keyC, "lose", false,
                    "La libreta queda en blanco y el rumor sigue corriendo."),
                Ch("C3", "Escribo lo que se vio, con hora, y que fue un punto de observación y no un aviso.", "c_end", "", keyC, "win", true,
                    "La libreta separa lo observado de lo que se afirma."));

            chapter.conversations = new[]
            {
                new ConversationData
                {
                    id = keyA, firstNode = "a_start",
                    nodes = new[] { nodeA, Nd("a_end", "Lucía",
                        "Terminaste el diálogo A. Lucía deja la libreta sobre la mesa y te dice que el que trae la razón se la sabe.") },
                },
                new ConversationData
                {
                    id = keyB, firstNode = "bx_start",
                    routes = new[]
                    {
                        new RouteRule { decisionKey = keyA, choice = "A1", node = "bx_start" },
                        new RouteRule { decisionKey = keyA, choice = "A2", node = "bx_start" },
                        new RouteRule { decisionKey = keyA, choice = "A3", node = "by_start" },
                    },
                    nodes = new[] { nodeBx, nodeBy, Nd("b_end", "Mateo",
                        "Terminaste el diálogo B. Mateo señala que Inés está en la puerta con la libreta.") },
                },
                new ConversationData
                {
                    id = keyC, firstNode = "cmid_start",
                    routes = new[]
                    {
                        new RouteRule { decisionKey = keyB, choice = "B1", node = "cmid_start" },
                        new RouteRule { decisionKey = keyB, choice = "B2", node = "cbad_start" },
                        new RouteRule { decisionKey = keyB, choice = "B3", node = "cgood_start" },
                        new RouteRule { decisionKey = keyB, choice = "B4", node = "cdead_start" },
                        new RouteRule { decisionKey = keyB, choice = "B5", node = "cbad_start" },
                        new RouteRule { decisionKey = keyB, choice = "B6", node = "cgood_start" },
                    },
                    nodes = new[] { nodeCgood, nodeCmid, nodeCbad, nodeCdead,
                        Nd("c_end", "Inés", "Terminaste el diálogo C. Inés cierra la libreta.") },
                },
            };

            // ── Pistas: dos lecturas según la rama ─────────────────────────────
            WriteClue(chapter, 0,
                "Croquis a mano: marca dónde dobla el cauce, pero no trae alturas ni fecha. Sirve para ubicarse, no para decidir.",
                "El croquis que llevaba Lucía. Además de dónde dobla el cauce tiene una raya de dos inviernos atrás. No dice a qué altura, pero sí dónde mirar primero.",
                "El mismo croquis, pero hoy la raya que marca el agua queda rodeada de casas nuevas. Dónde mirar ya no es la pregunta: es quién vive ahí.");
            WriteClue(chapter, 1,
                "Antes de usar un aviso, comprueba quién lo mandó, de qué día es y para qué zona. La vigencia es la mitad del dato.",
                "El boletín del martes: viene firmado y con fecha, pero solo cubre dos cuadras. Para el resto del barrio no dice nada.",
                "El boletín de la mañana, ya vencido. Dice qué hacer, pero la parte de las alturas es de la temporada pasada.");
            WriteClue(chapter, 2,
                "Su experiencia ayuda a preguntar mejor; no sustituye un estudio ni una instrucción oficial.",
                "Lo que Lucía cuenta del día que se fue el agua: empezó por la calle de arriba, no por el río.",
                "La misma tarde, contada por el vecino que cerró a tiempo: el agua nunca llegó a su cuadra.");

            File.WriteAllText(AssetDatabase.GetAssetPath(asset), JsonUtility.ToJson(catalog, true));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
            Debug.Log("Cali: diálogos del capítulo de inundación escritos.");
        }

        static void WriteClue(ChapterData chapter, int index, string plain, string forBx, string forBy)
        {
            var clue = chapter.clues[index];
            clue.text = plain;
            clue.variants = new[]
            {
                new ClueVariant { branch = "bx_start", text = forBx },
                new ClueVariant { branch = "by_start", text = forBy },
            };
        }
    }
}