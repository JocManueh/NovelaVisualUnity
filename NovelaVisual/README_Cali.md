# Cali ante la naturaleza — prototipo Unity

Proyecto: `NovelaVisual`, Unity **6000.6.3f1**. Abrir la carpeta interior `NovelaVisualUnity/NovelaVisual`, que contiene `Assets`, `Packages` y `ProjectSettings`. No abrir otra copia del repositorio.

## Ejecutar

1. Abrir `Assets/Scenes/01_MainMenu.unity`.
2. Usar **Cali > Configurar escenas existentes y tema**. Conserva las escenas anteriores y coloca el menú primero en la compilación.
3. Presionar Play. Comenzar recorrido → introducción → mapa → clic en un marcador → Entrar a la zona.
4. Maximizar Game o usar una proporción 16:9 para revisar la presentación.

## Controles prioritarios

- W/A/S/D: arriba/izquierda/abajo/derecha, exclusivamente en exploración local.
- E: una interacción con el NPC u objeto enfrente y dentro de rango. No avanza conversaciones.
- Espacio: completa la frase; una nueva pulsación avanza. Mantenerlo pulsado no repite avances. Las respuestas requieren clic o toque.
- Mapa: clic izquierdo selecciona; arrastrar desplaza; rueda acerca/aleja. El botón Entrar confirma el destino. Los paneles bloquean el mapa posterior.
- Botones táctiles equivalentes visibles. Los diálogos, cinemáticas y menús bloquean movimiento e interacción.

El tutorial y Ayuda contienen estas asignaciones. Las flechas no controlan el personaje y no existe movimiento local por clic.

## Escenas guardadas en el editor

| Archivo en Assets/Scenes | Función |
|---|---|
| 01_MainMenu | Menú, introducción y mapa general mediante estados; cuatro marcadores y fondo guardados en la escena. |
| 04_DroughtChapter | Primer capítulo, tres NPC y tres pistas; entrada MainEntry. |
| 05_RiverFloodChapter | Segundo capítulo con contenido propio y los sistemas compartidos. |
| 06_WindstormChapter | Tercer capítulo con contenido propio y los sistemas compartidos. |
| 07_FlashFloodChapter | Cuarto capítulo con contenido propio y los sistemas compartidos. |
| SampleScene | Escena original conservada. |

Los cuatro escenarios locales reutilizan, como montaje provisional, el patio del demo de Cainos. Son escenas editables, creadas con Save As y colocación manual de objetos en Unity. **Su adaptación visual a lugares reconocibles de Cali aún requiere trabajo**. No se generan mapas ni decoración al iniciar.

## Componentes y referencias

Las referencias vacías de servicios se resuelven entre componentes del mismo objeto o en la escena. No se generan escenarios.

| GameObject | Componentes / configuración |
|---|---|
| GameObject, en 01_MainMenu | GameSession; GameStateController; GameInputRouter; GameSaveManager; DialogueController; DialogueNarrationPlayer; CaliGameUI; ChapterCinematicController; UIDocument; AudioSource. GameSession persiste entre escenas. |
| Main Camera, en 01_MainMenu | Camera ortográfica y CaliMapNavigation. |
| Circle, Circle (1), Circle (2), Circle (3) | SpriteRenderer, BoxCollider2D y CaliMapZoneSelector con Chapter Index 0, 1, 2 y 3. Posiciones ilustrativas. |
| CaliMapIllustration | SpriteRenderer con orden negativo, detrás de marcadores. |
| PF Player, en cada zona | PlayerGridMovement + PlayerInteraction, Rigidbody2D cinemático, collider y Animator del recurso existente. TopDownCharacterController de Cainos desactivado. |
| NPC_Lucia / NPC_Mateo / NPC_Ines | NPCDialogueController, NPC Index 0/1/2; movimiento e interacción del clon desactivados. |
| Pista_1 / Pista_2 / Pista_3 | ClueInteractable, Clue Index 0/1/2; objetos existentes del paquete. |
| MainEntry | ZoneEntryPoint + ZoneChapterController. Transform colocado manualmente. El controlador busca únicamente un jugador habilitado. |

`CaliGameUI` usa `Assets/Resources/CaliNature/CaliGameUI.uxml`, `.uss`, `CaliPanelSettings.asset` y `CaliDefaultTheme.tss`. `GameSession` carga `CaliNature/StoryCatalog`. Los nombres de escena en ese catálogo deben coincidir con Build Profiles.

### Scripts por función

Todos están en `Assets/Scripts`:

- Core/GameSession: navegación, carga asíncrona, selección y condición de cierre (tres pistas + decisiones antes/durante/después).
- Core/GameStateController: estados y pausa. Core/GameInputRouter: WASD, E, Espacio y botones táctiles. Core/InputRules: reglas de dirección, alcance y gestos.
- Player/PlayerGridMovement: pasos interpolados, orientación, colisiones y animador. Player/PlayerCameraFollow: seguimiento con límites configurables; verificar su conexión en cada cámara local antes de sustituir el seguimiento de Cainos.
- Interaction/PlayerInteraction: un objetivo frontal, comprobación de obstáculos y aviso. WorldInteractable: contrato compartido. NPCDialogueController: conversación del índice. ClueInteractable: pista del índice.
- Dialogue/DialogueController: texto progresivo, opciones, historial y transiciones. DialogueData: formato serializable del catálogo.
- Audio/DialogueNarrationPlayer: una voz a la vez, pausa y registro de escucha completa. Revelar texto no registra escucha. Avanzar detiene la voz anterior.
- Map/CaliMapNavigation: clic/arrastre/zoom. CaliMapZoneSelector: selección. ZoneEntryPoint: entrada fija.
- Chapters/ZoneChapterController: entrada y arranque del capítulo. ChapterCinematicController: secuencia de tarjetas temporizadas con voz y transición.
- Save/GameSaveManager: guardado local, decisiones y pistas sin duplicados.
- UI/CaliGameUI: pantallas y bloqueo de entradas por paneles. ControlsHelpContent: tutorial y ayuda.
- Editor/CaliProjectTools: configuración de escenas existentes, validación y pruebas. CaliBehaviourWiring: conexión de comportamientos a objetos seleccionados; no genera escenarios.

## Editar con el equipo

- Mapas: editar las escenas fuera de Play, usando los sprites/prefabs/Tilemaps existentes. No generar escenarios por scripts. Guardar escena y sus `.meta`.
- Entradas: mover MainEntry en la escena. Cada visita desde el mapa coloca al jugador allí. Terminar diálogo no recoloca al jugador.
- Narrativa: editar `Assets/Resources/CaliNature/StoryCatalog.json`. Mantener IDs estables; `next` y `choices[].next` apuntan a nodos de la misma conversación. `requiredClue` condiciona respuestas. `decisionKey` corresponde a capítulo/before, capítulo/during o capítulo/after.
- Voz: `voiceResource` es una ruta relativa a Resources sin extensión. Los 60 WAV actuales son narración sintética provisional, no grabaciones humanas. Véase inventario.
- Estilo: colores, tamaños y espaciados en CaliGameUI.uss. Identidad UAO pendiente de aprobación y aplicación del manual.
- Antes de integrar: salir de Play, guardar, comprobar que GitHub Desktop está en esta misma raíz, commit de Assets/Packages/ProjectSettings y sus metas; no Library/Temp/Logs. Fetch + Pull de la rama antes de mezclar. Un merge no modifica una copia diferente que Unity tenga abierta.

## Guardado

PlayerPrefs, clave `CaliNature.Save.v1`. Continuar abre el mapa; no restaura conversaciones a medias. Al entrar reaparece en la entrada fija, conservando pistas y decisiones. Reiniciar requiere confirmación dentro del juego y conserva los ajustes. En Web, el almacenamiento depende del navegador/origen; borrar sus datos puede borrar la partida.

## Validación

Menú **Cali > Ejecutar pruebas de controles**, resultado en `Library/CaliNature/TestResults.xml`. **Cali > Validar datos y escena abierta**, informe en `Library/CaliNature/Validation.txt`. Leer `Documentation/Pruebas.md` para distinguir pruebas automáticas, comprobaciones reales y pendientes.

## Alcance pendiente

No se declara terminada la entrega académica. Falta el documento `ASM_Proyecto_2a_Descripcion_Desastres_Naturales`, la confirmación de temas con el profesor, cartografía oficial con niveles/rutas verificados, identidad institucional autorizada y acabado multimedia/visual. La matriz enumera estos faltantes. La instalación actual de Unity no incluye Web Build Support; véase `Documentation/Web.md`.
