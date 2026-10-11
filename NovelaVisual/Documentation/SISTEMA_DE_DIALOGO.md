# Prompt — Diálogos de NovelaVisual

> Pásale esto a tu IA. No hace falta contexto previo: ya está conectada al proyecto.

---

## 1. Antes de tocar nada

**¿Cuál es tu capítulo?** Responde con el nombre o el id:

| `id` | Capítulo | Escena |
|---|---|---|
| `drought` | Cuando el agua escasea | `04_DroughtChapter` |
| `flood` | El río y sus márgenes | `05_RiverFloodChapter` |
| `wind` | Cuando cambia el viento | `06_WindstormChapter` |
| `flash` | Una creciente que sorprende | `07_FlashFloodChapter` |

**El archivo `Assets/Resources/CaliNature/StoryCatalog.json` es compartido: cuatro personas
lo editan a la vez.** Modifica **solo** los arrays `conversations` y `clues` de tu
capítulo. No toques `sources`, `title`, `summary`, `objective`, `npcSprites`,
`clueSprites`, `mapSprite`, `cinematic` ni ningún otro capítulo.

## 2. La estructura que respetas

Todo es **data-driven**. Cero lógica por capítulo en C#.

**Tres NPC, en orden obligatorio** (`npcIndex` = posición en `conversations[]`):

| `npcIndex` | NPC | Fase | Personaje |
|---|---|---|---|
| 0 | Lucía | `a` | plantea el marco |
| 1 | Mateo | `b` | el riesgo concreto |
| 2 | Inés | `c` | el registro final |

No se puede hablar con el siguiente sin cerrar el anterior. Las pistas tampoco se
pueden recoger antes de hablar con Lucía.

**Árbol** (idéntico en los 4 capítulos):

```
A1 ┐
A2 ┴─→ bx_start  B1 → cmid_start    B4 → cdead_start
A3 ──→ by_start  B2 → cbad_start    B5 → cbad_start
                  B3 [🔒 clue/0] → cgood_start
                                    B6 [🔒 clue/1] → cgood_start
```

**Reglas que no puedes romper:**

1. Los `id` (`A1`…`A3`, `B1`…`B6`, `C1`…`C3`) **no se cambian**. Son el contrato con el código.
2. `outcome` (`"win"`/`"lose"`/`""`) **solo en el NPC C**. A y B van con `""`.
3. `next` apunta siempre a un nodo **de la misma conversación**.
4. `routes` nunca salta de NPC: el destino existe en su misma conversación.
5. `requiredClue` **solo en B3 y B6**. El NPC A nunca exige pista. El NPC C tampoco.
6. **Balance de cierre, exacto:** `cgood` 2/3 · `cmid` 2/3 · `cbad` 1/3 · `cdead` 1/3.
   Total **6 ganar / 6 perder**. **Ninguna rama gana las 3** (con la pista también se
   puede perder).
7. **Cada pista necesita sus dos variantes** (`bx_start` y `by_start`), con textos
   realmente distintos. El validador lo exige.
8. El orden de las opciones **no se asume**: se busca por `id`, nunca por posición.

**Campos de `DialogueChoice`:** `id`, `label` (lo que ve el jugador), `next`,
`requiredClue` (pista que lo bloquea; `""` = siempre disponible), `decisionKey`
(`capítulo/a|b|c`), `outcome`, `responsible` (decorativo, hoy no afecta al juego),
`consequence` (lo que va a la libreta).

**Campos de `ClueData`:** `id`, `title`, `text` (por defecto) y `variants`, cada una
con `branch`, `title` y `text`.

## 3. Entrevista

Haz estas preguntas **una por una**, espera respuesta, y solo después escribe.

1. **La situación.** ¿Qué está pasando en la comunidad? ¿Qué rumor o error se repite?
2. **Las dos ramas.** ¿Qué dos situaciones distintas había según lo que le
   preguntaste a Lucía? Una lleva al desahogo, la otra a un callejón. ¿Qué cambia
   entre ellas?
3. **Las pistas.** Para cada una de las tres: ¿qué encuentra el jugador si va por
   **Bx**? ¿Y si va por **By**? ¿Por qué son documentos distintos?
   Para las dos que abren la rama buena: ¿cuál es la de Bx (pista 0) y cuál la de
   By (pista 1)? ¿Por qué una no sirve para la otra?
4. **Los cuatro cierres.** ¿De qué habla cada uno? ¿Qué le dice Inés al jugador
   cuando acierta y cuando falla? Recuerda: en `cgood` una de las tres **pierde**.
5. **El tono.** ¿Más técnico, más cercano, más preventivo? ¿Alguien con voz propia?

## 4. Escribir

En este orden: `a_start`, `a_end`, `bx_start`, `by_start`, `b_end`, `cgood_start`,
`cmid_start`, `cbad_start`, `cdead_start`, `c_end`, y las `variants` de las tres pistas.

- Las tres opciones de una rama deben ser **distintas entre sí** pero llevar a la
  misma conclusión. Las ramas se diferencian entre sí.
- Las dos lecturas de una pista se sostienen por sí solas: una no puede depender
  de que el jugador haya leído la otra.
- Varía el orden en el que escribes las opciones.
- Textos en español, segunda persona.

## 5. Sprites (opcional)

Si ya tienes arte, se declara en tu capítulo. **Cadena vacía = conservar el actual.**

```json
"npcSprites":  ["CaliNature/NPCs/lucia_sequia", "", ""],   // 0 Lucía · 1 Mateo · 2 Inés
"clueSprites": ["CaliNature/Clues/registro", "", ""],      // 0 · 1 · 2
"mapSprite":   "CaliNature/Maps/rio"                       // al elegir la zona
```

Rutas en `Resources`, sin extensión. Los PNGs van bajo
`Assets/Resources/CaliNature/`, importados como **Sprite (2D and UI)**.

- **El personaje principal (Alex) NO cambia**: no hay campo para él.
- Hoy los tres NPC usan el mismo sprite (prefab `PF Player`). Que se vean iguales
  es lo esperado, no un error.
- No hay que tocar escenas ni el Inspector.

Si no tienes arte, déjalo vacío y sigue.

## 6. Verificar

```
Unity → Cali → Validar datos y escena abierta        → ERRORES: 0
Unity → Cali → Ejecutar pruebas de controles          → 32/32
```

Informe en `Library/CaliNature/Validation.txt`.

Si algo no cuadra con lo que quieres contar, **cambia el sistema, no el diálogo**:
dímelo y lo ajustamos.

---

**Pendiente conocido:** los textos actuales son placeholders. Al cerrar hay que
regrabar el audio de los nodos nuevos.