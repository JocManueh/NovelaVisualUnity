using System;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace CaliNature
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class CaliGameUI : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;
        private UIDocument document;
        private VisualElement root;
        private GameSession session;
        private PlayerInteraction player;
        private string modalKind;
        private bool tutorial;
        private DialogueNode displayedNode;
        private readonly string[] screenNames = { "menu", "intro", "map", "hud", "dialogue", "cinematic", "loading", "summary" };
        private void Awake()
        {
            // Al recargar una escena se crea un GameObject duplicado con su propia UI. Ese duplicado se
            // autodestruye en GameSession.Awake, pero antes de hacerlo renderizaría el menú inicial (única
            // pantalla visible por defecto en el UXML). Solo la interfaz de la sesión viva debe pintar.
            if (GameSession.Instance != null && GameSession.Instance.UI != this)
            {
                var duplicate = GetComponent<UIDocument>();
                if (duplicate) duplicate.enabled = false;
                enabled = false;
                return;
            }
            document = GetComponent<UIDocument>();
            if (!layout) layout = Resources.Load<VisualTreeAsset>("CaliNature/CaliGameUI");
            if (!panelSettings) panelSettings = Resources.Load<PanelSettings>("CaliNature/CaliPanelSettings");
            if (panelSettings && !panelSettings.themeStyleSheet) panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("CaliNature/CaliDefaultTheme");
            document.visualTreeAsset = layout; document.panelSettings = panelSettings;
            document.sortingOrder = 50;
        }
        private void Start()
        {
            session = GetComponent<GameSession>(); root = document.rootVisualElement;
            if (root == null || root.Q("menu") == null) { Debug.LogError("Falta la interfaz CaliGameUI.uxml o CaliPanelSettings.asset."); return; }
            root.pickingMode = PickingMode.Position;
            root.focusable = true; root.tabIndex = -1;
            root.RegisterCallback<GeometryChangedEvent>(_ => root.EnableInClassList("compact", root.layout.height < 650 || root.layout.width < 1000));
            root.RegisterCallback<FocusOutEvent>(_ => session.Input.ReleaseAll());
            BindMapPointer();
            root.RegisterCallback<NavigationSubmitEvent>(e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(e => { session.Input.KeyPressed(e.keyCode); if (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.E) e.StopImmediatePropagation(); }, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyUpEvent>(e => session.Input.KeyReleased(e.keyCode), TrickleDown.TrickleDown);
            root.Query<Button>().ForEach(button => button.focusable = false);
            Bind("new-game", () => { session.Narration.UnlockAudio(); if (session.Save.HasSave) OpenReset(); else session.StartIntroduction(); });
            Bind("continue", () => session.OpenMap());
            Bind("intro-map", () => session.OpenMap());
            Bind("menu-help", () => OpenHelp(false)); Bind("help", () => OpenHelp(false));
            Bind("settings", OpenSettings); Bind("notebook", OpenNotebook);
            Bind("close-modal", CloseModal); Bind("confirm-reset", () => { session.Save.ResetProgress(); CloseModal(); session.StartIntroduction(); });
            Bind("enter-zone", () => session.EnterSelected()); Bind("back-map", () => session.ReturnToMap());
            Bind("summary-map", () => session.ReturnToMap());
            Bind("touch-interact", () => session.Input.TouchInteract()); Bind("touch-advance", () => session.Input.TouchAdvance());
            Bind("history", OpenHistory); Bind("skip-cinematic", () => session.Cinematic.Skip());
            Bind("zoom-in", () => FindAnyObjectByType<CaliMapNavigation>()?.Zoom(-1));
            Bind("zoom-out", () => FindAnyObjectByType<CaliMapNavigation>()?.Zoom(1));
            Bind("filter-all", () => FilterMap(-1));
            for (int i = 0; i < 4; i++) { int index = i; Bind("filter-" + i, () => FilterMap(index)); }
            BindTouch("move-up", Vector2.up); BindTouch("move-down", Vector2.down); BindTouch("move-left", Vector2.left); BindTouch("move-right", Vector2.right);
            var volume = root.Q<Slider>("volume"); volume.RegisterValueChangedCallback(e => { session.Save.Data.volume = e.newValue; session.Save.Store(); });
            var speed = root.Q<Slider>("text-speed"); speed.RegisterValueChangedCallback(e => { session.Save.Data.textSpeed = e.newValue; session.Save.Store(); });
            root.Q<Toggle>("muted").RegisterValueChangedCallback(e => { session.Save.Data.muted = e.newValue; session.Save.Store(); });
            root.Q<Toggle>("subtitles").RegisterValueChangedCallback(e => { session.Save.Data.subtitles = e.newValue; session.Save.Store(); RefreshDialogue(); });
            session.State.Changed += RefreshMode; session.Dialogue.Changed += RefreshDialogue;
            RefreshMode(session.State.Mode);
            root.Focus();
        }
        private void OnDestroy()
        {
            if (session) { session.State.Changed -= RefreshMode; session.Dialogue.Changed -= RefreshDialogue; }
            if (player) player.PromptChanged -= UpdatePrompt;
        }
        private void LateUpdate()
        {
            if (!session || root == null || session.State.Mode != GameMode.Map || !Camera.main) return;
            for (int i = 0; i < 4; i++) Show("marker-label-" + i, false);
            foreach (var marker in FindObjectsByType<CaliMapZoneSelector>())
            {
                var label = root.Q<Label>("marker-label-" + marker.ChapterIndex);
                if (label == null) continue;
                bool completed = marker.IsCompleted;
                label.style.backgroundColor = completed ? CaliMapZoneSelector.CompletedColor : CaliMapZoneSelector.IncompleteColor;
                label.tooltip = completed ? "Misión completada" : "Misión incompleta";
                Vector3 point = Camera.main.WorldToViewportPoint(marker.transform.position);
                bool visible = point.z > 0 && point.x > 0 && point.x < 1 && point.y > 0 && point.y < 1;
                label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                label.style.left = Mathf.Clamp(point.x * root.layout.width + 12, 0, root.layout.width - 190);
                label.style.top = (1 - point.y) * root.layout.height + 24;
            }
        }
        private void Bind(string name, Action action) { var button = root.Q<Button>(name); if (button != null) button.clicked += () => { action(); root.Focus(); }; }
        private Vector2 ScreenPoint(Vector2 panelPoint)
        {
            Vector2 local = root.WorldToLocal(panelPoint);
            return new Vector2(local.x / root.layout.width * Screen.width, (1 - local.y / root.layout.height) * Screen.height);
        }
        private void BindMapPointer()
        {
            root.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || session.State.Mode != GameMode.Map) return;
                var map = FindAnyObjectByType<CaliMapNavigation>();
                if (map && map.BeginPointer(e.pointerId, ScreenPoint(e.position))) root.CapturePointer(e.pointerId);
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerMoveEvent>(e => FindAnyObjectByType<CaliMapNavigation>()?.MovePointer(e.pointerId, ScreenPoint(e.position)), TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.button != 0) return;
                FindAnyObjectByType<CaliMapNavigation>()?.EndPointer(e.pointerId, ScreenPoint(e.position));
                if (root.HasPointerCapture(e.pointerId)) root.ReleasePointer(e.pointerId);
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCancelEvent>(_ => FindAnyObjectByType<CaliMapNavigation>()?.Cancel(), TrickleDown.TrickleDown);
            root.RegisterCallback<WheelEvent>(e =>
            {
                if (session.State.Mode == GameMode.Map && !IsPointerBlocked(ScreenPoint(e.mousePosition))) FindAnyObjectByType<CaliMapNavigation>()?.Zoom(e.delta.y / 3f);
            }, TrickleDown.TrickleDown);
        }
        private void BindTouch(string name, Vector2 direction)
        {
            var button = root.Q<Button>(name);
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; session.Input.SetTouchDirection(direction); button.CapturePointer(e.pointerId); e.StopPropagation(); });
            button.RegisterCallback<PointerUpEvent>(e => { session.Input.SetTouchDirection(Vector2.zero); button.ReleasePointer(e.pointerId); e.StopPropagation(); });
            button.RegisterCallback<PointerCaptureOutEvent>(_ => session.Input.SetTouchDirection(Vector2.zero));
            button.RegisterCallback<PointerCancelEvent>(_ => session.Input.SetTouchDirection(Vector2.zero));
        }
        private void Show(string name, bool visible) { var element = root?.Q(name); if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None; }
        private void Text(string name, string text) { var label = root?.Q<Label>(name); if (label != null) label.text = text; }
        public void RefreshMode(GameMode mode)
        {
            if (root == null) return;
            if (mode == GameMode.Pause) return;
            foreach (string screen in screenNames) Show(screen, false);
            string target = mode switch { GameMode.Menu => "menu", GameMode.Introduction => "intro", GameMode.Map => "map", GameMode.Exploration => "hud", GameMode.Dialogue => "dialogue", GameMode.Cinematic => "cinematic", GameMode.Loading => "loading", GameMode.Summary => "summary", _ => "menu" };
            Show(target, true); Show("topbar", mode != GameMode.Loading && mode != GameMode.Cinematic);
            Show("modal", false); session.Input.SetTouchDirection(Vector2.zero);
            if (mode == GameMode.Menu) root.Q<Button>("continue").SetEnabled(session.Save.HasSave);
            if (mode == GameMode.Exploration) { Text("chapter-name", session.CurrentChapter?.title ?? "Exploración"); Text("objective", session.CurrentChapter?.objective ?? ""); }
            if (mode == GameMode.Dialogue) RefreshDialogue();
            root.Focus();
        }
        public void RefreshMap() { Text("map-progress", "Capítulos completados: " + session.Save.Data.completedChapters.Count + "/4"); Show("zone-info", false); }
        private void FilterMap(int index)
        {
            foreach (var marker in FindObjectsByType<CaliMapZoneSelector>(FindObjectsInactive.Include)) marker.gameObject.SetActive(index < 0 || marker.ChapterIndex == index);
            Show("zone-info", false);
        }
        public void ShowZone(ChapterData data)
        {
            Text("zone-title", data.title); Text("zone-description", data.period + "\n\n" + data.description + "\n\n" + data.objective);
            Text("zone-progress", session.Save.Data.completedChapters.Contains(data.id) ? "Completado · puedes volver a explorar" : "Disponible"); Show("zone-info", true);
        }
        public bool IsPointerBlocked(Vector2 screenPoint)
        {
            if (root?.panel == null) return true;
            Vector2 point = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screenPoint.x, Screen.height - screenPoint.y));
            VisualElement element = root.panel.Pick(point);
            for (; element != null; element = element.parent) if (element.ClassListContains("blocks-world") || element is Button) return true;
            return false;
        }
        public void BindPlayer(PlayerInteraction value)
        {
            if (player) player.PromptChanged -= UpdatePrompt;
            player = value; if (player) player.PromptChanged += UpdatePrompt;
            UpdatePrompt(player ? player.CurrentPrompt : "");
        }
        private void UpdatePrompt(string value) { Text("interaction-prompt", value); }
        private void RefreshDialogue()
        {
            if (root == null || !session) return;
            var dialogue = session.Dialogue; var node = dialogue.Current;
            if (node == null) return;
            Text("speaker", node.speaker);
            // Text remains visible when no recorded voice exists: never turn a missing recording into an inaccessible blank line.
            bool readable = session.Save.Data.subtitles || !session.Narration.HasClip;
            Text("dialogue-text", readable ? node.text.Substring(0, dialogue.VisibleCharacters) : "Subtítulos desactivados · puedes activarlos en Ajustes");
            Text("voice-status", session.Narration.HasClip ? "Narración" : "Narración pendiente de grabación");
            Text("dialogue-hint", dialogue.ShowingChoices ? "Elige una respuesta con clic" : dialogue.IsTyping ? "Espacio · mostrar frase completa" : "Espacio · continuar");
            var choices = root.Q("choices");
            if (displayedNode != node) { choices.Clear(); displayedNode = node; }
            if (!dialogue.IsTyping && choices.childCount == 0 && dialogue.ShowingChoices)
            {
                for (int i = 0; i < node.choices.Length; i++)
                {
                    int choice = i; var data = node.choices[i];
                    var button = new Button(() => dialogue.Choose(choice)) { text = data.label, focusable = false };
                    button.AddToClassList("choice"); button.SetEnabled(session.Save.HasClue(data.requiredClue));
                    if (!button.enabledSelf) button.text += "  [Requiere una pista de la libreta]";
                    choices.Add(button);
                }
            }
            if (!dialogue.ShowingChoices) choices.Clear();
            root.Q<Button>("touch-advance").SetEnabled(!dialogue.ShowingChoices);
        }
        private void OpenModal(string title, string text, string kind)
        {
            if (session.State.Mode != GameMode.Pause) session.State.Pause();
            modalKind = kind; Text("modal-title", title); Text("modal-text", text);
            Show("modal", true); Show("settings-fields", kind == "settings"); Show("confirm-reset", kind == "reset");
        }
        public void OpenHelp(bool firstTime) { tutorial = firstTime; OpenModal(firstTime ? "Tu primera visita · Controles" : "Ayuda · Controles", ControlsHelpContent.Text, "help"); }
        public void ShowNotice(string text) { OpenModal("Libreta de campo", text, "notice"); }
        private void OpenReset() { OpenModal("¿Reiniciar el recorrido?", "Se borrarán las decisiones, las pistas y los capítulos completados de esta partida. Los ajustes se conservarán.", "reset"); }
        private void OpenSettings()
        {
            OpenModal("Ajustes", "Configura el sonido y la lectura. Las respuestas siempre se seleccionan con clic o toque.", "settings");
            root.Q<Slider>("volume").SetValueWithoutNotify(session.Save.Data.volume); root.Q<Slider>("text-speed").SetValueWithoutNotify(session.Save.Data.textSpeed);
            root.Q<Toggle>("muted").SetValueWithoutNotify(session.Save.Data.muted); root.Q<Toggle>("subtitles").SetValueWithoutNotify(session.Save.Data.subtitles);
        }
        private void OpenNotebook()
        {
            var text = new StringBuilder();
            foreach (var chapter in session.Catalog.chapters)
            {
                text.AppendLine(chapter.title);
                foreach (var clue in chapter.clues) if (session.Save.HasClue(clue.id)) text.AppendLine("• " + clue.title + ": " + clue.text);
                foreach (var decision in session.Save.Data.decisions) if (decision.key.StartsWith(chapter.id + "/", StringComparison.Ordinal)) text.AppendLine("Decisión: " + decision.consequence);
                text.AppendLine();
            }
            OpenModal("Libreta · pistas y decisiones", text.ToString(), "notebook");
        }
        private void OpenHistory() { OpenModal("Historial de conversación", string.Join("\n\n", session.Dialogue.History), "history"); }
        private void CloseModal()
        {
            Show("modal", false);
            if (tutorial) { tutorial = false; session.Save.Data.tutorialSeen = true; session.Save.Store(); }
            session.State.Resume();
            if (modalKind == "notice") session.CheckChapterCompletion();
        }
        public void ShowCinematic(CinematicCard card) { Text("cinematic-title", card.title); Text("cinematic-text", card.text); }
        public void AnimateCinematic(float progress)
        {
            var card = root?.Q("cinematic-card"); if (card != null) { card.style.opacity = Mathf.Clamp01(progress * 8); card.style.translate = new Translate(0, Mathf.Lerp(24, 0, progress)); }
        }
        public void ShowSummary(ChapterData chapter)
        {
            var text = new StringBuilder(chapter.summary + "\n\nTUS DECISIONES\n");
            foreach (var item in session.Save.Data.decisions) if (item.key.StartsWith(chapter.id + "/", StringComparison.Ordinal)) text.AppendLine("• " + item.consequence);
            Text("summary-title", chapter.title + " · recorrido completado"); Text("summary-text", text.ToString());
        }
        public void SetLoading(float progress) { var bar = root?.Q<ProgressBar>("load-progress"); if (bar != null) bar.value = progress * 100; }
        public void ShowLoadError(string error) { session.State.SetMode(GameMode.Map); ShowNotice("No se pudo abrir la zona. " + error); }
    }
}
