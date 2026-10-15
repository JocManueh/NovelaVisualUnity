using UnityEngine;
using UnityEngine.UIElements;

namespace CaliNature
{
    // Overlay del nivel del terremoto (contador y, más adelante, paneles de los
    // puzzles). Vive DENTRO del panel de CaliGameUI —el único UIDocument del
    // juego— para no crear un segundo panel ni tocar la interfaz compartida:
    // se añade como último hijo de la raíz, por encima de las pantallas, y se
    // controla con Show/Hide.
    //
    // Ignora el picking: nunca intercepta clics ni teclas, de modo que el HUD y
    // el movimiento siguen funcionando mientras el contador está visible.
    public sealed class EarthquakeOverlayUI
    {
        private static readonly Color Ink = new Color32(24, 35, 39, 255);
        private static readonly Color Paper = new Color32(246, 244, 235, 255);
        private static readonly Color Accent = new Color32(25, 102, 86, 255);
        private static readonly Color Line = new Color32(184, 195, 184, 255);

        private VisualElement layer;
        private Label countdownValue;
        private VisualElement barFill;

        public bool Ready => layer != null;

        // Crea el overlay la primera vez que se necesita. Idempotente.
        public void Ensure()
        {
            if (layer != null && layer.panel != null) return;
            layer = null;

            var session = GameSession.Instance;
            if (!session || !session.UI) return;
            var document = session.UI.GetComponent<UIDocument>();
            VisualElement root = document ? document.rootVisualElement : null;
            if (root == null) return;

            Build(root);
        }

        public void ShowCountdown(int secondsLeft, float progress)
        {
            Ensure();
            if (layer == null) return;
            layer.style.display = DisplayStyle.Flex;
            if (countdownValue != null) countdownValue.text = Mathf.Max(0, secondsLeft).ToString();
            if (barFill != null) barFill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
        }

        public void Hide()
        {
            if (layer != null) layer.style.display = DisplayStyle.None;
        }

        private void Build(VisualElement root)
        {
            layer = new VisualElement { name = "terremoto-layer", pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.top = 0;
            layer.style.right = 0;
            layer.style.bottom = 0;
            layer.style.display = DisplayStyle.None;
            root.Add(layer);

            var center = new VisualElement { pickingMode = PickingMode.Ignore };
            center.style.position = Position.Absolute;
            center.style.left = 0;
            center.style.top = 0;
            center.style.right = 0;
            center.style.bottom = 0;
            center.style.alignItems = Align.Center;
            center.style.justifyContent = Justify.Center;
            layer.Add(center);

            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.style.backgroundColor = Paper;
            card.style.borderTopWidth = 4;
            card.style.borderBottomWidth = 4;
            card.style.borderLeftWidth = 4;
            card.style.borderRightWidth = 4;
            card.style.borderTopColor = Accent;
            card.style.borderBottomColor = Accent;
            card.style.borderLeftColor = Accent;
            card.style.borderRightColor = Accent;
            card.style.borderTopLeftRadius = 20;
            card.style.borderTopRightRadius = 20;
            card.style.borderBottomLeftRadius = 20;
            card.style.borderBottomRightRadius = 20;
            card.style.paddingLeft = 44;
            card.style.paddingRight = 44;
            card.style.paddingTop = 24;
            card.style.paddingBottom = 28;
            card.style.alignItems = Align.Center;
            center.Add(card);

            var caption = new Label("PREPÁRATE") { pickingMode = PickingMode.Ignore };
            caption.style.fontSize = 18;
            caption.style.color = Accent;
            caption.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(caption);

            countdownValue = new Label("5") { pickingMode = PickingMode.Ignore };
            countdownValue.style.fontSize = 110;
            countdownValue.style.color = Ink;
            countdownValue.style.unityTextAlign = TextAnchor.MiddleCenter;
            countdownValue.style.marginTop = 2;
            countdownValue.style.marginBottom = 6;
            card.Add(countdownValue);

            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.style.width = 320;
            track.style.height = 10;
            track.style.backgroundColor = Line;
            track.style.borderTopLeftRadius = 5;
            track.style.borderTopRightRadius = 5;
            track.style.borderBottomLeftRadius = 5;
            track.style.borderBottomRightRadius = 5;
            card.Add(track);

            barFill = new VisualElement { pickingMode = PickingMode.Ignore };
            barFill.style.height = 10;
            barFill.style.width = Length.Percent(0);
            barFill.style.backgroundColor = Accent;
            barFill.style.borderTopLeftRadius = 5;
            barFill.style.borderTopRightRadius = 5;
            barFill.style.borderBottomLeftRadius = 5;
            barFill.style.borderBottomRightRadius = 5;
            track.Add(barFill);
        }
    }
}
