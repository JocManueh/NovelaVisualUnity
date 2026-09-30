using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CaliNature.Editor
{
    // Wiring utilities only: operate on a manually selected object. No geometry, maps, actors or scenes are created.
    public static class CaliBehaviourWiring
    {
        private static T Add<T>() where T : Component
        {
            var selected = Selection.activeGameObject;
            if (!selected || EditorUtility.IsPersistent(selected)) { Debug.LogError("Selecciona un objeto de la jerarquía de la escena."); return null; }
            var component = selected.GetComponent<T>();
            if (!component) component = Undo.AddComponent<T>(selected);
            EditorSceneManager.MarkSceneDirty(selected.scene);
            return component;
        }
        [MenuItem("Cali/Conectar objeto seleccionado/Jugador WASD %&p")]
        public static void Player()
        {
            var movement = Add<PlayerGridMovement>(); if (!movement) return;
            Add<PlayerInteraction>();
            foreach (var component in movement.GetComponents<MonoBehaviour>())
                if (component.GetType().FullName == "Cainos.PixelArtTopDown_Basic.TopDownCharacterController") { Undo.RecordObject(component, "Control WASD"); component.enabled = false; }
            var body = movement.GetComponent<Rigidbody2D>(); Undo.RecordObject(body, "Movimiento por casillas"); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0; body.freezeRotation = true;
        }
        [MenuItem("Cali/Conectar objeto seleccionado/Sistemas de juego %&g")]
        public static void Session() { Add<GameSession>(); }
        [MenuItem("Cali/Conectar objeto seleccionado/Entrada fija de zona %&e")]
        public static void Entry() { var entry = Add<ZoneEntryPoint>(); if (entry) entry.gameObject.name = "MainEntry"; Add<ZoneChapterController>(); }
        [MenuItem("Cali/Conectar objeto seleccionado/NPC 1 Lucia %&#1")]
        public static void Npc0() { Npc(0); }
        [MenuItem("Cali/Conectar objeto seleccionado/NPC 2 Mateo %&#2")]
        public static void Npc1() { Npc(1); }
        [MenuItem("Cali/Conectar objeto seleccionado/NPC 3 Ines %&#3")]
        public static void Npc2() { Npc(2); }
        private static void Npc(int index)
        {
            var npc = Add<NPCDialogueController>(); if (!npc) return;
            Undo.RecordObject(npc.gameObject, "Nombre de NPC"); npc.gameObject.name = new[] { "NPC_Lucia", "NPC_Mateo", "NPC_Ines" }[index];
            var data = new SerializedObject(npc); data.FindProperty("npcIndex").intValue = index; data.FindProperty("prompt").stringValue = "Conversar"; data.ApplyModifiedProperties();
            foreach (var script in npc.GetComponents<MonoBehaviour>())
                if (script is PlayerGridMovement || script is PlayerInteraction || script.GetType().FullName == "Cainos.PixelArtTopDown_Basic.TopDownCharacterController") { Undo.RecordObject(script, "NPC inmóvil"); script.enabled = false; }
            var body = npc.GetComponent<Rigidbody2D>(); if (body) { Undo.RecordObject(body, "NPC inmóvil"); body.bodyType = RigidbodyType2D.Kinematic; }
        }
        [MenuItem("Cali/Conectar objeto seleccionado/Pista 1 %&#4")]
        public static void Clue0() { Clue(0); }
        [MenuItem("Cali/Conectar objeto seleccionado/Pista 2 %&#5")]
        public static void Clue1() { Clue(1); }
        [MenuItem("Cali/Conectar objeto seleccionado/Pista 3 %&#6")]
        public static void Clue2() { Clue(2); }
        private static void Clue(int index)
        {
            var clue = Add<ClueInteractable>(); if (!clue) return;
            Undo.RecordObject(clue.gameObject, "Nombre de pista"); clue.gameObject.name = "Pista_" + (index + 1);
            var data = new SerializedObject(clue); data.FindProperty("clueIndex").intValue = index; data.FindProperty("prompt").stringValue = "Examinar pista"; data.ApplyModifiedProperties();
        }
        [MenuItem("Cali/Conectar objeto seleccionado/Marcador 1 Sequia %&#7")]
        public static void Marker0() { Marker(0); }
        [MenuItem("Cali/Conectar objeto seleccionado/Marcador 2 Inundacion %&#8")]
        public static void Marker1() { Marker(1); }
        [MenuItem("Cali/Conectar objeto seleccionado/Marcador 3 Vendaval %&#9")]
        public static void Marker2() { Marker(2); }
        [MenuItem("Cali/Conectar objeto seleccionado/Marcador 4 Torrencial %&#0")]
        public static void Marker3() { Marker(3); }
        private static void Marker(int index)
        {
            var marker = Add<CaliMapZoneSelector>(); if (!marker) return;
            var data = new SerializedObject(marker); data.FindProperty("chapterIndex").intValue = index; data.ApplyModifiedProperties();
        }
        [MenuItem("Cali/Conectar objeto seleccionado/Camara del mapa %&m")]
        public static void MapCamera() { Add<CaliMapNavigation>(); }
        [MenuItem("Cali/Conectar objeto seleccionado/Camara de exploracion %&f")]
        public static void LocalCamera()
        {
            var camera = Add<PlayerCameraFollow>(); if (!camera) return;
            foreach (var script in camera.GetComponents<MonoBehaviour>())
                if (script.GetType().FullName == "Cainos.PixelArtTopDown_Basic.CameraFollow") { Undo.RecordObject(script, "Camara del recorrido"); script.enabled = false; }
        }
    }
}
