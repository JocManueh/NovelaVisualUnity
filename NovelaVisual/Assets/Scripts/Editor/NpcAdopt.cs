using UnityEditor;
using UnityEngine;

namespace CaliNature.Editor
{
    public static class NpcAdopt
    {
    /// Traslada toda la información de diálogo de los NPC antiguos a los personajes
    /// nuevos que el usuario colocó en la escena, y después borra los NPC.
    ///
    /// POR QUÉ NO SE COPIAN A MANO: el NPCDialogueController tiene npcIndex, y la
    /// clase base WorldInteractable tiene prompt y range, todos con [SerializeField]
    /// privado. Se leen y escriben por SerializedObject para copiar exactamente lo que
    /// había, sin depender de conocer los valores.
    ///
    /// QUÉ SE TRANSFIERE POR CADA PAR:
    ///   - NPCDialogueController con npcIndex, prompt y range idénticos.
    ///   - Collider2D (mismo tipo, tamaño y offset) para que bloqueen al jugador.
    ///   - Rigidbody2D en el mismo bodyType para que nada los empuje.
    ///
    /// NO se copia el sprite ni el animator: los personajes nuevos ya tienen su arte.
    [MenuItem("Cali/Adoptar personajes nuevos como NPC")]
        public static void Adopt()
        {
            var pairs = new[]
            {
                new { From = "NPC_Lucia", To = "Lucia" },
                new { From = "NPC_Mateo", To = "Mateo" },
                new { From = "NPC_Ines",  To = "Ines" },
            };

            int adopted = 0;
            foreach (var pair in pairs)
            {
                var source = FindAny(pair.From);
                var target = FindAny(pair.To);
                if (!source || !target)
                {
                    Debug.LogError("Cali: no se encontró " +
                        (source == null ? pair.From : pair.To) + ". Se omite el par.");
                    continue;
                }

                CopyDialogue(source, target);
                CopyCollider(source, target);
                CopyBody(source, target);

                Object.DestroyImmediate(source);
                adopted++;
                Debug.Log("Cali: " + pair.To + " adoptó los datos de " + pair.From + ".");
            }

            EditorSceneManagerMarkDirty();
            Debug.Log("Cali: transferencia terminada. Personajes adoptados: " + adopted);
        }

        /// Busca un objeto por nombre tanto si está activo como inactivo.
        static GameObject FindAny(string name)
        {
            var direct = GameObject.Find(name);
            if (direct) return direct;
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
                if (candidate.name == name && candidate.scene.IsValid()) return candidate;
            return null;
        }

        static void CopyDialogue(GameObject source, GameObject target)
        {
            var sourceController = source.GetComponent<NPCDialogueController>();
            if (!sourceController) { Debug.LogWarning("Cali: " + source.name + " no tiene NPCDialogueController."); return; }

            var controller = target.GetComponent<NPCDialogueController>();
            if (!controller) controller = target.AddComponent<NPCDialogueController>();

            var from = new SerializedObject(sourceController);
            var to = new SerializedObject(controller);

            // npcIndex es el que decide qué conversación abre: es el dato crítico.
            Copy(from, to, "npcIndex");
            Copy(from, to, "prompt");
            Copy(from, to, "range");

            to.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        static void Copy(SerializedObject from, SerializedObject to, string property)
        {
            var sourceProperty = from.FindProperty(property);
            var targetProperty = to.FindProperty(property);
            // SerializedProperty no admite el operador ! en esta versión de Unity.
            // Si la propiedad no existe en alguno de los dos, no hay nada que copiar.
            if (sourceProperty == null || targetProperty == null) return;
            switch (sourceProperty.propertyType)
            {
                case SerializedPropertyType.Integer: targetProperty.intValue = sourceProperty.intValue; break;
                case SerializedPropertyType.Float:   targetProperty.floatValue = sourceProperty.floatValue; break;
                case SerializedPropertyType.Boolean: targetProperty.boolValue = sourceProperty.boolValue; break;
                case SerializedPropertyType.String:  targetProperty.stringValue = sourceProperty.stringValue; break;
                default: Debug.LogWarning("Cali: la propiedad '" + property + "' no se copió (tipo " + sourceProperty.propertyType + ")."); break;
            }
        }

        static void CopyCollider(GameObject source, GameObject target)
        {
            var sourceCollider = source.GetComponent<Collider2D>();
            if (!sourceCollider) return;
            if (target.GetComponent<Collider2D>()) return;   // ya tiene uno: no se toca

            if (sourceCollider is BoxCollider2D box)
            {
                var added = target.AddComponent<BoxCollider2D>();
                added.size = box.size;
                added.offset = box.offset;
                added.isTrigger = box.isTrigger;
                EditorUtility.SetDirty(added);
            }
            else
            {
                Debug.LogWarning("Cali: " + source.name + " usa " + sourceCollider.GetType().Name
                    + ", que no se copia automáticamente. Añádelo a mano si hace falta.");
            }
        }

        static void CopyBody(GameObject source, GameObject target)
        {
            var sourceBody = source.GetComponent<Rigidbody2D>();
            if (!sourceBody || target.GetComponent<Rigidbody2D>()) return;
            var added = target.AddComponent<Rigidbody2D>();
            added.bodyType = sourceBody.bodyType;
            added.gravityScale = sourceBody.gravityScale;
            added.freezeRotation = true;
            EditorUtility.SetDirty(added);
        }

        static void EditorSceneManagerMarkDirty()
            => UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                   UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}