using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CaliNature.Tests
{
    public sealed class ControlsAndStoryTests
    {
        private GameObject inputObject;
        private GameStateController state;
        private GameInputRouter input;
        [SetUp] public void SetUpInput()
        {
            inputObject = new GameObject("Input test");
            state = inputObject.AddComponent<GameStateController>();
            input = inputObject.AddComponent<GameInputRouter>();
            typeof(GameInputRouter).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(input, null);
        }
        [TearDown] public void RemoveInput() { Object.DestroyImmediate(inputObject); }
        private void Tick() { typeof(GameInputRouter).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(input, null); }
        [Test] public void HeldSpaceOnlyAdvancesOnceUntilRelease()
        {
            state.SetMode(GameMode.Dialogue); int advances = 0;
            input.AdvancePressed += () => advances++;
            input.KeyPressed(KeyCode.Space); Tick();
            input.KeyPressed(KeyCode.Space); Tick(); Tick();
            Assert.AreEqual(1, advances);
            input.KeyReleased(KeyCode.Space); input.KeyPressed(KeyCode.Space); Tick();
            Assert.AreEqual(2, advances);
        }
        [Test] public void InteractionOpeningDialogueCannotAdvanceItsFirstLine()
        {
            state.SetMode(GameMode.Exploration); int advances = 0, interactions = 0;
            input.InteractPressed += () => { interactions++; state.SetMode(GameMode.Dialogue); };
            input.AdvancePressed += () => advances++;
            input.KeyPressed(KeyCode.E); input.KeyPressed(KeyCode.Space); Tick();
            input.KeyPressed(KeyCode.E); input.KeyPressed(KeyCode.Space); Tick();
            Assert.AreEqual(1, interactions); Assert.AreEqual(0, advances);
        }
        [Test] public void DialogueBlocksMovementAndInteractions()
        {
            state.SetMode(GameMode.Dialogue); int interactions = 0;
            input.InteractPressed += () => interactions++;
            input.KeyPressed(KeyCode.W); input.KeyPressed(KeyCode.E); Tick();
            Assert.AreEqual(Vector2.zero, input.Movement); Assert.AreEqual(0, interactions);
        }
        [Test] public void WASDReleasesWithoutStuckMovement()
        {
            state.SetMode(GameMode.Exploration); input.KeyPressed(KeyCode.D); Tick();
            Assert.AreEqual(Vector2.right, input.Movement);
            input.KeyReleased(KeyCode.D); Tick(); Assert.AreEqual(Vector2.zero, input.Movement);
        }
        [Test] public void ArrowKeysDoNotMovePlayer()
        {
            state.SetMode(GameMode.Exploration); input.KeyPressed(KeyCode.UpArrow); Tick();
            Assert.AreEqual(Vector2.zero, input.Movement);
        }
        [Test] public void FocusLossClearsPendingKeysAndTouchInput()
        {
            state.SetMode(GameMode.Exploration); int interactions = 0;
            input.InteractPressed += () => interactions++;
            input.KeyPressed(KeyCode.W); input.TouchInteract(); input.ReleaseAll(); Tick();
            Assert.AreEqual(Vector2.zero, input.Movement); Assert.AreEqual(0, interactions);
        }
        [Test] public void TouchAdvanceAndKeyboardInSameFrameAreOneAction()
        {
            state.SetMode(GameMode.Dialogue); int advances = 0;
            input.AdvancePressed += () => advances++;
            input.TouchAdvance(); input.KeyPressed(KeyCode.Space); Tick();
            Assert.AreEqual(1, advances);
        }
        [Test] public void BriefTapBetweenPhysicsTicksMovesExactlyOneStep()
        {
            state.SetMode(GameMode.Exploration);
            input.KeyPressed(KeyCode.D); input.KeyReleased(KeyCode.D); Tick();
            Assert.AreEqual(Vector2.right, input.ConsumeStep());
            Assert.AreEqual(Vector2.zero, input.ConsumeStep());
        }
        [Test] public void FocusLossDiscardsQueuedMovement()
        {
            state.SetMode(GameMode.Exploration); input.KeyPressed(KeyCode.D); input.ReleaseAll();
            Assert.AreEqual(Vector2.zero, input.ConsumeStep());
        }
        [Test] public void DiagonalInputResolvesToOneCardinalDirection() { Assert.AreEqual(Vector2.up, InputRules.Cardinal(Vector2.one)); }
        [Test] public void ZeroInputStops() { Assert.AreEqual(Vector2.zero, InputRules.Cardinal(Vector2.zero)); }
        [Test] public void NPCBehindCannotBeSelected() { Assert.IsFalse(InputRules.IsInFront(Vector2.zero, Vector2.up, Vector2.down, 2)); }
        [Test] public void NPCOutsideRangeCannotBeSelected() { Assert.IsFalse(InputRules.IsInFront(Vector2.zero, Vector2.up, Vector2.up * 3, 2)); }
        [Test] public void NPCInFrontWithinRangeCanBeSelected() { Assert.IsTrue(InputRules.IsInFront(Vector2.zero, Vector2.up, Vector2.up, 2)); }
        [Test] public void OpeningFrameCannotAdvance() { Assert.AreEqual(DialogueAdvanceAction.None, InputRules.Advance(true, true, false, 42, 42)); }
        [Test] public void FirstSpaceRevealsText() { Assert.AreEqual(DialogueAdvanceAction.Reveal, InputRules.Advance(true, true, false, 43, 42)); }
        [Test] public void SecondSpaceAdvancesCompletedLine() { Assert.AreEqual(DialogueAdvanceAction.Next, InputRules.Advance(true, false, false, 44, 42)); }
        [Test] public void SpaceNeverSelectsAnAnswer() { Assert.AreEqual(DialogueAdvanceAction.None, InputRules.Advance(true, false, true, 44, 42)); }
        [Test] public void BlockedDialogueCannotAdvance() { Assert.AreEqual(DialogueAdvanceAction.None, InputRules.Advance(false, false, false, 44, 42)); }
        [Test] public void ShortGestureIsClick() { var g = new MapPointerGesture(); g.Begin(Vector2.zero); g.Move(Vector2.one * 2, 10); Assert.IsTrue(g.End(Vector2.one * 2, 10)); }
        [Test] public void DragCannotBecomeClickWhenReturningToStart() { var g = new MapPointerGesture(); g.Begin(Vector2.zero); g.Move(Vector2.right * 20, 10); Assert.IsFalse(g.End(Vector2.zero, 10)); }
        [Test] public void ReleaseAfterLargeMoveIsNotClickEvenWithoutMoveEvent() { var g = new MapPointerGesture(); g.Begin(Vector2.zero); Assert.IsFalse(g.End(Vector2.right * 50, 10)); }
        [Test] public void CancelledGestureCannotSelect() { var g = new MapPointerGesture(); g.Begin(Vector2.zero); g.Cancel(); Assert.IsFalse(g.End(Vector2.zero, 10)); }
        [Test] public void ReleaseWithoutPressCannotSelect() { Assert.IsFalse(new MapPointerGesture().End(Vector2.zero, 10)); }
        [Test] public void FourChaptersHaveTwelveCluesAndTwelveDecisions()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            Assert.AreEqual(4, data.chapters.Length);
            Assert.AreEqual(12, data.chapters.SelectMany(c => c.clues).Select(c => c.id).Distinct().Count());
            // Tres fases por capítulo (a, b, c) = 12 claves de decisión distintas.
            Assert.AreEqual(12, data.chapters.SelectMany(c => c.conversations).SelectMany(c => c.nodes).SelectMany(n => n.choices).Select(c => c.decisionKey).Distinct().Count());
            foreach (var chapter in data.chapters)
            {
                Assert.AreEqual(3, chapter.conversations.Length);
                foreach (var conversation in chapter.conversations)
                {
                    var ids = conversation.nodes.Select(n => n.id).ToArray();
                    Assert.Contains(conversation.firstNode, ids);
                    foreach (var node in conversation.nodes)
                    {
                        if (!string.IsNullOrEmpty(node.next)) Assert.Contains(node.next, ids);
                        foreach (var choice in node.choices) Assert.Contains(choice.next, ids);
                    }
                    foreach (var route in conversation.routes ?? new RouteRule[0])
                    {
                        Assert.Contains(route.node, ids, "Ruta hacia un nodo inexistente en " + conversation.id);
                        Assert.IsFalse(string.IsNullOrEmpty(route.decisionKey));
                    }
                }
            }
        }
        // Árbol de ramas, idéntico en los 4 capítulos:
        //   A1/A2 → Bx · A3 → By
        //   B1 → Cmid · B2 → Cbad · B3 → Cgood (exige pista 0)
        //   B4 → Cdead · B5 → Cbad · B6 → Cmid (exige pista 1)
        // Cgood es la ÚNICA rama con mayoría de ganadoras (2 de 3).
        // Árbol de ramas, idéntico en los 4 capítulos. Balance de cierre 6 ganar / 6 perder.
//   A1/A2 → Bx · A3 → By
//   B1 → Cmid · B2 → Cbad · B3 → Cgood (exige clue/0, la pista de la rama Bx)
//   B4 → Cdead · B5 → Cbad · B6 → Cgood (exige clue/1, la pista de la rama By)
// El test de abajo solo mira el TOTAL de resultados: 6 ganar y 6 perder.
        // Eso NO alcanza. Con 6/6 se puede tener una opcion que gane 4 de 4 ramas
        // (elegir siempre la misma = victoria garantizada) y aun asi cumplir el
        // reparto. Ese fue el exploit real que hubo en el catalogo.
        //
        // Aqui se exige el reparto por opcion: cada choice.id debe ganar en la
        // mitad de las ramas de cierre (2 de 4). Asi ninguna estrategia fija
        // sirve y el jugador tiene que leer la rama en la que esta.
        [Test] public void NoClosingChoiceIsADominantStrategy()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            foreach (var chapter in data.chapters)
            {
                // Las ramas de cierre son los nodos que empiezan por "c" (cgood_start,
                // cmid_start, cbad_start, cdead_start). a_start/bx_start/by_start no.
                var closingNodes = chapter.conversations
                    .SelectMany(c => c.nodes ?? new DialogueNode[0])
                    .Where(n => !string.IsNullOrEmpty(n.id) && n.id[0] == 'c' && n.id.EndsWith("_start"))
                    .ToArray();
                Assert.AreEqual(4, closingNodes.Length, chapter.id + ": se esperaban cuatro ramas de cierre.");

                // Se registran TODOS los ids, ganen o no. Contar solo los que ganan
                // daria un falso "faltan opciones" cuando una nunca gana (que es
                // justo el sintoma de una estrategia dominante).
                var winsPerChoice = new Dictionary<string, int>();
                foreach (var node in closingNodes)
                    foreach (var choice in node.choices ?? new DialogueChoice[0])
                    {
                        if (string.IsNullOrEmpty(choice.id)) continue;
                        if (!winsPerChoice.ContainsKey(choice.id)) winsPerChoice[choice.id] = 0;
                        if (!string.Equals(choice.outcome, "win", System.StringComparison.OrdinalIgnoreCase)) continue;
                        winsPerChoice[choice.id] = winsPerChoice[choice.id] + 1;
                    }

                Assert.AreEqual(3, winsPerChoice.Count, chapter.id + ": se esperaban tres opciones de cierre.");
                foreach (var pair in winsPerChoice.OrderBy(p => p.Key, System.StringComparer.Ordinal))
                {
                    bool dominant = pair.Value > closingNodes.Length / 2;
                    Assert.IsFalse(dominant, chapter.id + ": la opcion " + pair.Key + " gana en "
                        + pair.Value + " de " + closingNodes.Length
                        + " ramas de cierre. Es una estrategia dominante: el jugador puede repetirla siempre.");
                }
            }
        }

        [Test] public void EveryChapterHasTheSameBranchingTreeAndABalancedSixSixSplit()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            // Orden alfabético de nodos: bx_start, by_start, cbad_start, cdead_start, cgood_start, cmid_start.
            const string expectedRoutes =
                "bx_start:A1,A2 by_start:A3 cbad_start:B2,B5 cdead_start:B4 cgood_start:B3,B6 cmid_start:B1";
            foreach (var chapter in data.chapters)
            {
                var routes = chapter.conversations.SelectMany(c => c.routes ?? new RouteRule[0])
                    .Select(r => r.node + ":" + r.choice).ToArray();
                // 3 reglas en la conversación de B + 6 en la de C.
                Assert.AreEqual(9, routes.Length, chapter.id + ": se esperaban nueve reglas de ruta.");
                string actual = string.Join(" ", routes.GroupBy(r => r.Split(':')[0])
                    .OrderBy(g => g.Key, System.StringComparer.Ordinal)
                    .Select(g => g.Key + ":" + string.Join(",", g.Select(x => x.Split(':')[1]).OrderBy(x => x, System.StringComparer.Ordinal))));
                Assert.AreEqual(expectedRoutes, actual, chapter.id + ": el arbol de ramas no coincide.");

                // El NPC C tiene cuatro nodos de cierre, cada uno con 3 opciones.
                var closing = chapter.conversations[2].nodes
                    .Where(n => n.choices != null && n.choices.Any(c => !string.IsNullOrEmpty(c.outcome))).ToArray();
                Assert.AreEqual(4, closing.Length, chapter.id + ": se esperaban cuatro nodos de cierre.");
                foreach (var node in closing) Assert.AreEqual(3, node.choices.Length, chapter.id + "/" + node.id);

                // Las cuatro ramas tienen un perfil fijo. Ninguna gana las tres: con la pista
                // en la mano el jugador debe poder seguir perdiendo.
                var wins = new Dictionary<string, int>();
                foreach (var node in closing) wins[node.id] = node.choices.Count(c => c.outcome == "win");
                Assert.AreEqual(2, wins["cgood_start"], chapter.id + ": Cgood debe tener dos ganadoras.");
                Assert.AreEqual(2, wins["cmid_start"], chapter.id + ": Cmid debe tener dos ganadoras.");
                Assert.AreEqual(1, wins["cbad_start"], chapter.id + ": Cbad debe tener una ganadora.");
                Assert.AreEqual(1, wins["cdead_start"], chapter.id + ": Cdead debe tener una ganadora.");

                // La rama de la pista es la única con dos ganadoras entre las de riesgo:
                // Cgood no es perfecta, solo es la mejor de las cuatro.
                var best = wins.Max(kv => kv.Value);
                Assert.AreEqual(2, best, chapter.id + ": ninguna rama debe superar dos ganadoras.");

                // Balance global del capítulo: 6 ganar / 6 perder.
                Assert.AreEqual(6, wins.Values.Sum(), chapter.id + ": se esperaban seis opciones ganadoras.");
                Assert.AreEqual(6, closing.Sum(n => n.choices.Count(c => c.outcome == "lose")), chapter.id + ": se esperaban seis perdedoras.");
            }
        }

        // La pista que abre la rama buena depende de la rama elegida en A: B3 (solo
        // alcanzable desde Bx) exige clue/0 y B6 (solo desde By) exige clue/1. No
        // son intercambiables, porque la pista que sirve en una rama no sirve en la otra.
        [Test] public void EachBranchInAHasItsOwnClueLeadingToTheFavourableBranch()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            foreach (var chapter in data.chapters)
            {
                var clueIds = chapter.clues.Select(c => c.id).ToArray();
                var conversationB = chapter.conversations[1];
                var gated = conversationB.nodes.SelectMany(n => n.choices)
                    .Where(c => !string.IsNullOrEmpty(c.requiredClue)).ToArray();

                // Solo B3 y B6 están bloqueados, ambos en el NPC B.
                Assert.AreEqual(2, gated.Length, chapter.id + ": se esperaban dos opciones bloqueadas.");
                Assert.IsTrue(gated.All(c => chapter.id + "/b" == c.decisionKey), chapter.id + ": la pista no debe bloquear nada fuera del NPC B.");
                foreach (var choice in gated) Assert.Contains(choice.requiredClue, clueIds, chapter.id + ": pista inexistente.");

                // B3 pertenece a Bx y usa clue/0; B6 pertenece a By y usa clue/1.
                var b3 = conversationB.nodes.Single(n => n.id == "bx_start").choices.Single(c => c.id == "B3");
                var b6 = conversationB.nodes.Single(n => n.id == "by_start").choices.Single(c => c.id == "B6");
                Assert.AreEqual(chapter.id + "/clue/0", b3.requiredClue, chapter.id + ": B3 (rama Bx) debe exigir clue/0.");
                Assert.AreEqual(chapter.id + "/clue/1", b6.requiredClue, chapter.id + ": B6 (rama By) debe exigir clue/1.");

                // Ambas abren la rama que siempre gana.
                var toGood = chapter.conversations[2].routes.Where(r => r.node == "cgood_start")
                    .Select(r => r.choice).OrderBy(x => x, System.StringComparer.Ordinal).ToArray();
                Assert.AreEqual(2, toGood.Length, chapter.id + ": Cgood debería abrirse desde B3 y B6.");
                Assert.AreEqual("B3", toGood[0]);
                Assert.AreEqual("B6", toGood[1]);

                // Ninguna opción de cierre puede depender de una pista: el desenlace
                // final no debe delatar cuál era la respuesta buena.
                foreach (var node in chapter.conversations[2].nodes)
                    foreach (var choice in node.choices)
                        Assert.IsTrue(string.IsNullOrEmpty(choice.requiredClue), chapter.id + "/" + node.id + ": el desenlace no debe exigir pista.");

                // El NPC A nunca puede exigir pista: es el primero y siempre está abierto.
                foreach (var node in chapter.conversations[0].nodes)
                    foreach (var choice in node.choices)
                        Assert.IsTrue(string.IsNullOrEmpty(choice.requiredClue), chapter.id + "/" + node.id);
            }
        }

        // La pista se lee DISTINTO según la rama de B. Sin esto el jugador examina
        // el mismo documento en Bx y en By y no tiene forma de saber cuál le sirve,
        // que es lo que hace que requiredClue parezca no funcionar.
        [Test] public void EveryClueHasADifferentReadingForEachBranch()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            foreach (var chapter in data.chapters)
            {
                // Las ramas reales de B, tomadas de sus propias rutas.
                var branches = chapter.conversations[1].routes
                    .Where(r => r.decisionKey.EndsWith("/a", System.StringComparison.Ordinal))
                    .Select(r => r.node).Distinct().OrderBy(x => x, System.StringComparer.Ordinal).ToArray();
                Assert.AreEqual(2, branches.Length, chapter.id + ": se esperaban dos ramas en B.");

                foreach (var clue in chapter.clues)
                {
                    var variants = clue.variants ?? new ClueVariant[0];
                    Assert.AreEqual(branches.Length, variants.Length,
                        chapter.id + "/" + clue.id + ": falta una variante por rama.");

                    foreach (var branch in branches)
                    {
                        var variant = variants.FirstOrDefault(v => v.branch == branch);
                        Assert.IsNotNull(variant, chapter.id + "/" + clue.id + ": no hay variante para " + branch + ".");
                        // El contenido debe ser realmente distinto entre ramas.
                        var others = variants.Where(v => v.branch != branch).Select(v => v.text).ToArray();
                        Assert.IsTrue(others.All(t => t != variant.text),
                            chapter.id + "/" + clue.id + ": " + branch + " tiene el mismo texto que otra rama.");
                        Assert.IsFalse(string.IsNullOrWhiteSpace(variant.text), chapter.id + "/" + clue.id + "/" + branch + ": texto vac\u00edo.");
                    }
                    // El texto por defecto debe existir como respaldo.
                    Assert.IsFalse(string.IsNullOrWhiteSpace(clue.text), chapter.id + "/" + clue.id + ": falta el texto por defecto.");
                }
            }
        }

        // Regresión: con varios objetos de pista en el mapa, el jugador podía examinar
// uno que no correspondía a su rama y quedarse con una pista inútil, sin que nada
// explicara por qué la opción seguía bloqueada. Ahora la pista entregada depende
// de la rama, no del objeto.
        [Test] public void EachBranchHasExactlyOneClueThatOpensItAndTheRestAreDecorative()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            foreach (var chapter in data.chapters)
            {
                var conversationB = chapter.conversations[1];
                var routesB = conversationB.routes.ToDictionary(r => r.choice, r => r.node);

                // La rama buena es la de más ganadoras, no la más frecuente.
                var conversationC = chapter.conversations[2];
                var bestNode = conversationC.nodes
                    .Where(n => n.choices != null && n.choices.Any(c => !string.IsNullOrEmpty(c.outcome)))
                    .OrderByDescending(n => n.choices.Count(c => c.outcome == "win"))
                    .First();
                Assert.AreEqual("cgood_start", bestNode.id, chapter.id + ": la rama buena debe ser cgood_start.");

                // Cada rama de B tiene exactamente una opción bloqueada que abre la buena.
                foreach (var branchNode in conversationB.nodes.Where(n => n.id.EndsWith("_start", System.StringComparison.Ordinal) && n.id.StartsWith("b", System.StringComparison.Ordinal)))
                {
                    var opening = branchNode.choices
                        .Where(c => !string.IsNullOrEmpty(c.requiredClue)
                                 && conversationC.routes.Any(r => r.choice == c.id && r.node == bestNode.id))
                        .ToArray();
                    Assert.AreEqual(1, opening.Length,
                        chapter.id + "/" + branchNode.id + ": se esperaba una única opción bloqueada hacia " + bestNode.id + ".");
                    Assert.IsTrue(chapter.clues.Any(c => c.id == opening[0].requiredClue),
                        chapter.id + "/" + branchNode.id + ": la pista exigida no existe en el catálogo.");
                }

                // Bx y By no pueden exigir la MISMA pista: si lo hicieran, una de las dos
                // rutas quedaría sin su propia pista y el jugador podría quedarse bloqueado.
                var bxClue = conversationB.nodes.Single(n => n.id == "bx_start").choices
                    .First(c => c.requiredClue != null && c.requiredClue.Length > 0).requiredClue;
                var byClue = conversationB.nodes.Single(n => n.id == "by_start").choices
                    .First(c => c.requiredClue != null && c.requiredClue.Length > 0).requiredClue;
                Assert.AreNotEqual(bxClue, byClue, chapter.id + ": Bx y By deben exigir pistas distintas.");

                // Las dos ramas de A que desembocan en Bx comparten la misma pista.
                var aIntoBx = conversationB.routes.Where(r => r.node == "bx_start").Select(r => r.choice).ToArray();
                Assert.AreEqual(2, aIntoBx.Length, chapter.id + ": A1 y A2 deben llevar a Bx.");
            }
        }

        // Regresión del bug que bloqueaba la rama By: la pista correcta se buscaba en
// las rutas de la conversación de B, que solo contienen transiciones de A a B.
// La comparación nunca coincidía y el método devolvía null, así que el jugador
// se quedaba con la pista del objeto (que no abría nada en su rama).
// Esta test replica esa búsqueda con los datos y exige que devuelva un id real.
        [Test] public void TheBranchClueIsResolvableForBothBranches()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            foreach (var chapter in data.chapters)
            {
                var conversationB = chapter.conversations[1];
                var conversationC = chapter.conversations[2];

                // Rama buena: la de más ganadoras.
                var bestNode = conversationC.nodes
                    .Where(n => n.choices != null && n.choices.Any(c => !string.IsNullOrEmpty(c.outcome)))
                    .OrderByDescending(n => n.choices.Count(c => c.outcome == "win"))
                    .First().id;

                // Transiciones de B a C: deben vivr en la conversación de C.
                var routesToC = conversationC.routes
                    .Where(r => r.decisionKey.EndsWith("/b", System.StringComparison.Ordinal)).ToArray();
                Assert.Greater(routesToC.Length, 0, chapter.id + ": la conversación de C debe tener rutas desde B.");

                foreach (var branchNodeId in new[] { "bx_start", "by_start" })
                {
                    var node = conversationB.nodes.FirstOrDefault(n => n.id == branchNodeId);
                    Assert.IsNotNull(node, chapter.id + ": falta el nodo " + branchNodeId + ".");

                    // Esta es la búsqueda que fallaba: debe devolver un id de pista real.
                    var match = node.choices.FirstOrDefault(c =>
                        !string.IsNullOrEmpty(c.requiredClue)
                        && routesToC.Any(r => r.choice == c.id && r.node == bestNode));
                    Assert.IsNotNull(match,
                        chapter.id + "/" + branchNodeId + ": no se encontró la opción bloqueada hacia " + bestNode + ".");
                    Assert.IsTrue(chapter.clues.Any(c => c.id == match.requiredClue),
                        chapter.id + "/" + branchNodeId + ": la pista '" + match.requiredClue + "' no existe en el catálogo.");
                }
            }
        }

        // Cada capítulo declara sus dos textos de cierre, que se muestran justo después
        // de la última conversación y antes de volver al mapa. Están en texto de
        // relleno: cada equipo debe escribir el suyo.
        // El capítulo de inundación NO comparte arbol con los demás: cada equipo
        // diseña el suyo. Los tests compartidos recorren los capítulos en orden y
        // se detienen en el primero que falla, así que el de inundación nunca se
        // llegaba a comprobar. Este lo valida a mano.
        //
        //   A   -> bx_start (A1, A2)        |  by_start (A3)
        //   Bx  -> csilencio (B2, B5)        |  calarma (B3, exige clue/0)
        //   By  -> cfestejo  (B1, B4)        |  caviso  (B6, exige clue/1)
        //
        // Reparto de desenlaces: calarma y cfestejo tienen dos ganadoras cada una;
        // csilencio y caviso, una. Ninguna rama gana las tres, y ningun C1/C2/C3
        // gana en mas de dos ramas, asi que no existe estrategia dominante.
        [Test] public void FloodChapterHasItsExactBranchingTree()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            var chapter = data.chapters.Single(c => c.id == "flood");
            Assert.AreEqual(3, chapter.conversations.Length, "flood: se esperaban tres conversaciones.");
            Assert.AreEqual(3, chapter.clues.Length, "flood: se esperaban tres pistas.");

            string actual = string.Join(" ", chapter.conversations.SelectMany(c => c.routes ?? new RouteRule[0])
                .Select(r => r.node + ":" + r.choice)
                .OrderBy(x => x, System.StringComparer.Ordinal));
            const string expected =
                "bx_start:A1 bx_start:A2 by_start:A3 " +
                "calarma_start:B3 caviso_start:B6 cfestejo_start:B1 cfestejo_start:B4 " +
                "csilencio_start:B2 csilencio_start:B5";
            Assert.AreEqual(expected, actual, "flood: el arbol de ramas no coincide.");

            // Ninguna ruta puede apuntar a un nodo o a una opcion que no existan.
            var nodeIds = chapter.conversations.SelectMany(c => c.nodes).Select(n => n.id).ToArray();
            var choiceIds = chapter.conversations.SelectMany(c => c.nodes).SelectMany(n => n.choices).Select(c => c.id).ToArray();
            foreach (var route in chapter.conversations.SelectMany(c => c.routes ?? new RouteRule[0]))
            {
                Assert.Contains(route.node, nodeIds, "flood: la ruta apunta a un nodo inexistente.");
                Assert.Contains(route.choice, choiceIds, "flood: la ruta apunta a una opcion inexistente.");
            }

            var closing = chapter.conversations[2].nodes
                .Where(n => n.choices != null && n.choices.Any(c => !string.IsNullOrEmpty(c.outcome))).ToArray();
            Assert.AreEqual(4, closing.Length, "flood: se esperaban cuatro cierres.");
            foreach (var node in closing) Assert.AreEqual(3, node.choices.Length, "flood/" + node.id);

            var wins = closing.ToDictionary(n => n.id, n => n.choices.Count(c => c.outcome == "win"));
            Assert.AreEqual(2, wins["calarma_start"], "flood: calarma es la rama buena y tiene dos ganadoras.");
            Assert.AreEqual(2, wins["cfestejo_start"]);
            Assert.AreEqual(1, wins["csilencio_start"]);
            Assert.AreEqual(1, wins["caviso_start"]);
            Assert.AreEqual(6, wins.Values.Sum(), "flood: seis ganadoras.");
            Assert.AreEqual(6, closing.Sum(n => n.choices.Count(c => c.outcome == "lose")), "flood: seis perdedoras.");

            // El 6/6 no basta: con 6/6 cabía que C1 ganara las cuatro y "elegir
            // siempre C1" fuera victoria garantizada. Se exige el reparto por opcion.
            var winsPerChoice = new Dictionary<string, int>();
            foreach (var node in closing)
                foreach (var choice in node.choices)
                {
                    if (!winsPerChoice.ContainsKey(choice.id)) winsPerChoice[choice.id] = 0;
                    if (choice.outcome == "win") winsPerChoice[choice.id] = winsPerChoice[choice.id] + 1;
                }
            Assert.AreEqual(3, winsPerChoice.Count, "flood: tres opciones de cierre.");
            foreach (var pair in winsPerChoice.OrderBy(p => p.Key, System.StringComparer.Ordinal))
                Assert.IsTrue(pair.Value <= closing.Length / 2,
                    "flood: la opcion " + pair.Key + " gana en " + pair.Value + " de " + closing.Length
                    + " cierres. Es una estrategia dominante.");

            // El desenlace final nunca debe depender de una pista, ni la primera
            // conversa: una pista no puede delatar cual era la respuesta buena.
            foreach (var node in closing)
                foreach (var choice in node.choices)
                    Assert.IsTrue(string.IsNullOrEmpty(choice.requiredClue), "flood/" + node.id + ": el desenlace no exige pista.");
            foreach (var node in chapter.conversations[0].nodes)
                foreach (var choice in node.choices)
                    Assert.IsTrue(string.IsNullOrEmpty(choice.requiredClue), "flood/" + node.id);

            // Cada rama de B abre algo con SU pista, y las dos son distintas: si
            // compartieran pista, una de las dos rutas quedaria sin la suya.
            var b = chapter.conversations[1];
            var gated = b.nodes.Where(n => n.id == "bx_start" || n.id == "by_start")
                .ToDictionary(n => n.id,
                    n => n.choices.Where(c => !string.IsNullOrEmpty(c.requiredClue)).Select(c => c.requiredClue).ToArray());
            Assert.AreEqual(1, gated["bx_start"].Length, "flood/bx_start: se esperaba una opcion bloqueada.");
            Assert.AreEqual(1, gated["by_start"].Length, "flood/by_start: se esperaba una opcion bloqueada.");
            Assert.AreEqual(chapter.id + "/clue/0", gated["bx_start"][0], "flood: bx_start exige clue/0.");
            Assert.AreEqual(chapter.id + "/clue/1", gated["by_start"][0], "flood: by_start exige clue/1.");
        }

        // Ningun texto puede delatar en que rama esta el jugador. La palabra "rama"
        // o un identificador de nodo escrito como si fuera diálogo rompe la tercera
        // persona y le regala al jugador la respuesta.
        [Test] public void FloodNeverNamesTheBranchInItsTexts()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            var chapter = data.chapters.Single(c => c.id == "flood");
            var nodeIds = chapter.conversations.SelectMany(c => c.nodes).Select(n => n.id).ToArray();

            var texts = new System.Collections.Generic.List<string>();
            foreach (var conversation in chapter.conversations)
                foreach (var node in conversation.nodes)
                {
                    texts.Add(node.speaker);
                    texts.Add(node.text);
                    foreach (var choice in node.choices) { texts.Add(choice.label); texts.Add(choice.consequence); }
                }
            foreach (var clue in chapter.clues)
            {
                texts.Add(clue.title); texts.Add(clue.text);
                foreach (var variant in clue.variants ?? new ClueVariant[0]) { texts.Add(variant.title); texts.Add(variant.text); }
            }
            foreach (var card in chapter.cinematic) { texts.Add(card.title); texts.Add(card.text); }

            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                foreach (var id in nodeIds)
                    Assert.IsFalse(text.Contains(id), "flood: un texto menciona el nodo '" + id + "': " + text);
                foreach (var forbidden in new[] { "rama", "nodo", "Estás en", "te lleva a" })
                    Assert.IsFalse(text.IndexOf(forbidden, System.StringComparison.OrdinalIgnoreCase) >= 0,
                        "flood: un texto dice '" + forbidden + "': " + text);
            }
        }

        // El desenlace se personaliza por decisión, no por victoria/derrota: cada
        // una de las doce decisiones de cierre tiene que contar su propio final.
        // Con un solo endingWin/endingLose todos los finales leían igual.
        [Test] public void EveryFloodClosingChoiceHasItsOwnEnding()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            var chapter = data.chapters.Single(c => c.id == "flood");
            var closing = chapter.conversations[2].nodes
                .Where(n => n.choices != null && n.choices.Any(c => !string.IsNullOrEmpty(c.outcome))).ToArray();

            var titulos = new List<string>();
            var cuerpos = new List<string>();
            foreach (var node in closing)
                foreach (var choice in node.choices)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.endingTitle),
                        "flood/" + node.id + "/" + choice.id + ": falta el título del final.");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.endingText),
                        "flood/" + node.id + "/" + choice.id + ": falta el texto del final.");
                    titulos.Add(choice.endingTitle);
                    cuerpos.Add(choice.endingText);
                }

            Assert.AreEqual(12, titulos.Count, "flood: se esperaban doce finales.");
            Assert.AreEqual(titulos.Count, titulos.Distinct().Count(),
                "flood: dos finales comparten título, así que no están personalizados.");
            Assert.AreEqual(cuerpos.Count, cuerpos.Distinct().Count(),
                "flood: dos finales comparten texto, así que no están personalizados.");

            // `consequence` es lo que hiciste; el final es lo que pasó después.
            // No pueden ser el mismo texto.
            foreach (var node in closing)
                foreach (var choice in node.choices)
                    Assert.AreNotEqual(choice.consequence, choice.endingText, "flood/" + node.id + "/" + choice.id);
        }

        [Test] public void EveryChapterDeclaresBothClosingTexts()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            foreach (var chapter in data.chapters)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(chapter.endingWin),
                    chapter.id + ": falta el texto de cierre al ganar.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(chapter.endingLose),
                    chapter.id + ": falta el texto de cierre al perder.");
            }
        }

        // Los sprites son configurables por capítulo. Hoy están vacíos a propósito:
        // el juego usa los que ya vienen en la escena. Los arrays deben existir
        // siempre para que rellenarlos más adelante no requiera tocar código.
        [Test] public void SpriteOverridesAreDeclaredAndEmptyByDefault()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            foreach (var chapter in data.chapters)
            {
                Assert.IsNotNull(chapter.npcSprites, chapter.id + ": falta npcSprites.");
                Assert.IsNotNull(chapter.clueSprites, chapter.id + ": falta clueSprites.");
                Assert.AreEqual(3, chapter.npcSprites.Length, chapter.id + ": se esperaban tres sprites de NPC.");
                Assert.AreEqual(3, chapter.clueSprites.Length, chapter.id + ": se esperaban tres sprites de pista.");
                // Cadena vacía = conservar el sprite que ya trae la escena (es el estado
                // actual). Si se rellena, el asset tiene que existir de verdad.
                foreach (var path in chapter.npcSprites.Concat(chapter.clueSprites).Concat(new[] { chapter.mapSprite }))
                {
                    if (string.IsNullOrEmpty(path)) continue;
                    Assert.IsTrue(Resources.Load<Sprite>(path) != null, chapter.id + ": el sprite '" + path + "' no existe en Resources.");
                }
            }
        }

        // El orden de las opciones está barajado y cambia entre capítulos: el jugador
        // no debe aprender posiciones fijas. Dentro de un capítulo sí es estable,
        // porque el barajado se aplica a los datos, no en tiempo de ejecución.
        [Test] public void ChoiceOrderIsShuffledAndDiffersBetweenChapters()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            var signatures = new List<string>();
            foreach (var chapter in data.chapters)
            {
                var parts = new List<string>();
                foreach (var node in chapter.conversations.SelectMany(c => c.nodes))
                {
                    if (node.choices == null || node.choices.Length < 2) continue;
                    var ids = node.choices.Select(c => c.id).ToArray();
                    parts.Add(node.id + ":" + string.Join(",", ids));
                }
                signatures.Add(string.Join("|", parts));
            }

            Assert.AreEqual(4, signatures.Distinct().Count(), "Los cuatro capítulos deberían presentar órdenes distintos.");

            // Que la lista esté barajada no significa que NINGÚN nodo salga desordenado:
            // con tres opciones el shuffle puede devolver el orden natural por casualidad.
            // Lo que sí debe cumplirse es que la mayoría de los nodos reordenados, y en
            // particular los de las ramas que ve el jugador, no sigan el orden A1,A2,A3.
            foreach (var chapter in data.chapters)
            {
                var shuffled = 0; var total = 0;
                foreach (var node in chapter.conversations.SelectMany(c => c.nodes).Where(n => n.choices != null && n.choices.Length > 1))
                {
                    total++;
                    var ids = node.choices.Select(c => c.id).ToArray();
                    if (!ids.SequenceEqual(ids.OrderBy(x => x, System.StringComparer.Ordinal))) shuffled++;
                }
                Assert.Greater(shuffled, total / 2, chapter.id + ": casi ningún nodo quedó barajado.");
            }
        }
    }
}
