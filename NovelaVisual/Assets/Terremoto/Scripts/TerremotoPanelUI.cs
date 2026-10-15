using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace CaliNature
{
    // Panel modal del nivel del terremoto (UI Toolkit, estilos en código).
    //
    // Se añade DENTRO del panel de CaliGameUI —el único UIDocument del juego—
    // como último hijo de la raíz, así que queda por encima de las pantallas y no
    // necesita ni un segundo UIDocument ni un Canvas uGUI. Es reutilizable: el
    // Puzzle 1 (FASE C) y el Puzzle 2 (FASE D) usan la misma clase.
    public sealed class TerremotoPanelUI
    {
        private static readonly Color Ink = new Color32(24, 35, 39, 255);
        private static readonly Color Paper = new Color32(246, 244, 235, 255);
        private static readonly Color Accent = new Color32(25, 102, 86, 255);
        private static readonly Color Scrim = new Color32(18, 27, 30, 150);

        private readonly float fadeSeconds;

        private VisualElement layer;
        private VisualElement buttons;
        private Label feedback;

        public TerremotoPanelUI(float fadeSeconds)
        {
            this.fadeSeconds = Mathf.Max(0.05f, fadeSeconds);
        }

        // Construye el panel. Devuelve false si no hay UI a la que engancharse.
        public bool Build(GameSession session, string question, string[] options, Action<int> onOption)
        {
            if (layer != null) return true;

            var document = session && session.UI ? session.UI.GetComponent<UIDocument>() : null;
            VisualElement root = document ? document.rootVisualElement : null;
            if (root == null) return false;

            layer = new VisualElement { name = "terremoto-panel" };
            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.top = 0;
            layer.style.right = 0;
            layer.style.bottom = 0;
            layer.style.backgroundColor = Scrim;
            layer.style.alignItems = Align.Center;
            layer.style.justifyContent = Justify.Center;
            layer.style.opacity = 0f;
            root.Add(layer);

            var card = new VisualElement();
            card.style.width = 620;
            card.style.backgroundColor = Paper;
            card.style.borderTopWidth = 4;
            card.style.borderBottomWidth = 4;
            card.style.borderLeftWidth = 4;
            card.style.borderRightWidth = 4;
            card.style.borderTopColor = Accent;
            card.style.borderBottomColor = Accent;
            card.style.borderLeftColor = Accent;
            card.style.borderRightColor = Accent;
            card.style.borderTopLeftRadius = 22;
            card.style.borderTopRightRadius = 22;
            card.style.borderBottomLeftRadius = 22;
            card.style.borderBottomRightRadius = 22;
            card.style.paddingLeft = 40;
            card.style.paddingRight = 40;
            card.style.paddingTop = 30;
            card.style.paddingBottom = 30;
            card.style.alignItems = Align.Center;
            layer.Add(card);

            var title = new Label(question) { pickingMode = PickingMode.Ignore };
            title.style.fontSize = 36;
            title.style.color = Ink;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 20;
            title.style.whiteSpace = WhiteSpace.Normal;
            card.Add(title);

            buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Column;
            buttons.style.alignSelf = Align.Stretch;
            card.Add(buttons);

            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    int index = i;
                    var button = new Button(() => onOption(index)) { text = options[i] };
                    button.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    button.style.color = Ink;
                    button.style.fontSize = 22;
                    button.style.marginTop = 6;
                    button.style.marginBottom = 6;
                    button.style.paddingLeft = 24;
                    button.style.paddingRight = 24;
                    button.style.paddingTop = 14;
                    button.style.paddingBottom = 14;
                    button.style.borderTopWidth = 2;
                    button.style.borderBottomWidth = 2;
                    button.style.borderLeftWidth = 2;
                    button.style.borderRightWidth = 2;
                    button.style.borderTopColor = Accent;
                    button.style.borderBottomColor = Accent;
                    button.style.borderLeftColor = Accent;
                    button.style.borderRightColor = Accent;
                    button.style.borderTopLeftRadius = 12;
                    button.style.borderTopRightRadius = 12;
                    button.style.borderBottomLeftRadius = 12;
                    button.style.borderBottomRightRadius = 12;
                    button.style.unityTextAlign = TextAnchor.MiddleLeft;
                    button.style.whiteSpace = WhiteSpace.Normal;
                    buttons.Add(button);
                }
            }

            feedback = new Label("") { pickingMode = PickingMode.Ignore };
            feedback.style.fontSize = 20;
            feedback.style.color = Ink;
            feedback.style.unityTextAlign = TextAnchor.MiddleCenter;
            feedback.style.marginTop = 16;
            feedback.style.minHeight = 28;
            feedback.style.maxWidth = 540;
            feedback.style.whiteSpace = WhiteSpace.Normal;
            card.Add(feedback);

            return true;
        }

        public void ShowFeedback(string message)
        {
            if (feedback != null) feedback.text = message ?? "";
            if (buttons != null) buttons.SetEnabled(false);
        }

        public void HideFeedback()
        {
            if (feedback != null) feedback.text = "";
            if (buttons != null) buttons.SetEnabled(true);
        }

        public IEnumerator FadeIn() { return Fade(0f, 1f); }
        public IEnumerator FadeOut() { return Fade(1f, 0f); }

        private IEnumerator Fade(float from, float to)
        {
            if (layer == null) yield break;
            layer.style.opacity = from;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                layer.style.opacity = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeSeconds));
                yield return null;
            }
            layer.style.opacity = to;
        }

        public void Remove()
        {
            if (layer == null) return;
            layer.RemoveFromHierarchy();
            layer = null;
            buttons = null;
            feedback = null;
        }
    }
}
