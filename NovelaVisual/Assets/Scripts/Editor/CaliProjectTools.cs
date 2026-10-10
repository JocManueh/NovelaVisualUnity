using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.TestTools.TestRunner.Api;

namespace CaliNature.Editor
{
    // Only validates existing assets, configures builds and runs tests. Never creates a map or populates a scene.
    public static class CaliProjectTools
    {
        private static TestRunnerApi runner;
        [MenuItem("Cali/Configurar escenas existentes y tema")]
        public static void Configure()
        {
            string[] paths = { "Assets/Scenes/01_MainMenu.unity", "Assets/Scenes/04_DroughtChapter.unity", "Assets/Scenes/05_RiverFloodChapter.unity", "Assets/Scenes/06_WindstormChapter.unity", "Assets/Scenes/07_FlashFloodChapter.unity" };
            var scenes = paths.Where(File.Exists).Select(path => new EditorBuildSettingsScene(path, true)).ToList();
            scenes.AddRange(EditorBuildSettings.scenes.Where(scene => !paths.Contains(scene.path)));
            EditorBuildSettings.scenes = scenes.ToArray();
            var start = AssetDatabase.LoadAssetAtPath<SceneAsset>(paths[0]);
            if (start) EditorSceneManager.playModeStartScene = start;
            var panel = Resources.Load<PanelSettings>("CaliNature/CaliPanelSettings");
            if (panel) { panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("CaliNature/CaliDefaultTheme"); EditorUtility.SetDirty(panel); AssetDatabase.SaveAssets(); }
            Debug.Log("Cali: escenas existentes configuradas. No se generaron escenarios.");
        }
        [MenuItem("Cali/Validar datos y escena abierta")]
        public static void Validate()
        {
            var problems = new List<string>(); var warnings = new List<string>();
            var asset = Resources.Load<TextAsset>("CaliNature/StoryCatalog");
            var catalog = asset ? JsonUtility.FromJson<StoryCatalog>(asset.text) : null;
            if (catalog?.chapters == null) problems.Add("Falta StoryCatalog.json");
            else foreach (var chapter in catalog.chapters)
            {
                if (chapter.clues?.Length != 3 || chapter.conversations?.Length != 3) problems.Add(chapter.id + ": se requieren tres pistas y tres conversaciones.");
                if (!File.Exists("Assets/Scenes/" + chapter.sceneName + ".unity")) warnings.Add("Montaje pendiente: " + chapter.sceneName);
                foreach (var dialogue in chapter.conversations)
                {
                    var ids = new HashSet<string>(); foreach (var node in dialogue.nodes) if (!ids.Add(node.id)) problems.Add("Nodo repetido: " + dialogue.id + "/" + node.id);
                    if (!ids.Contains(dialogue.firstNode)) problems.Add("Inicio inválido: " + dialogue.id);
                    foreach (var node in dialogue.nodes)
                    {
                        if (!string.IsNullOrEmpty(node.next) && !ids.Contains(node.next)) problems.Add("Siguiente inválido: " + node.next);
                        foreach (var choice in node.choices ?? Array.Empty<DialogueChoice>())
                        {
                            if (!ids.Contains(choice.next)) problems.Add("Destino de opción inválido: " + choice.next);
                            if (!string.IsNullOrEmpty(choice.requiredClue) && !chapter.clues.Any(c => c.id == choice.requiredClue)) problems.Add("Pista inexistente: " + choice.requiredClue);
                        }
                        if (!string.IsNullOrEmpty(node.voiceResource) && !Resources.Load<AudioClip>(node.voiceResource)) warnings.Add("Grabación pendiente: " + node.voiceResource);
                    }
                }
            }
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0) problems.Add("Script perdido: " + transform.name);
            Directory.CreateDirectory("Library/CaliNature");
            File.WriteAllText("Library/CaliNature/Validation.txt", "ERRORES: " + problems.Count + "\n" + string.Join("\n", problems) + "\nPENDIENTES: " + warnings.Count + "\n" + string.Join("\n", warnings));
            Debug.Log("Cali: validación terminada. Errores: " + problems.Count + ". Pendientes: " + warnings.Count + ". Informe: Library/CaliNature/Validation.txt");
        }
        [MenuItem("Cali/Ejecutar pruebas de controles")]
        public static void RunTests()
        {
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new TestResults());
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "CaliNature.Tests" } }));
        }
        private sealed class TestResults : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("Library/CaliNature");
                TestRunnerApi.SaveResultToFile(result, "Library/CaliNature/TestResults.xml");
                Debug.Log("Cali: pruebas terminadas: " + result.ResultState + ", " + result.PassCount + " correctas, " + result.FailCount + " fallidas.");
            }
        }
    }
}
