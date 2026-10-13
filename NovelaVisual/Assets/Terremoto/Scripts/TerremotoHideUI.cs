using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CaliNature
{
    // Indicador y resaltado del Puzzle 1 del nivel del terremoto (UI Toolkit,
    // estilos en código).
    //
    // Se añade DENTRO del panel de CaliGameUI —el único UIDocument del juego—
    // como último hijo de la raíz, así que queda por encima del HUD, sin crear
    // un segundo panel ni tocar la interfaz compartida. Ignora el picking:
    // nunca intercepta clics ni teclas.
    //
    // Dibuja un anillo pulsante sobre cada punto de escondite proyectando su
    // posición de mundo a pantalla (el mismo truco que usan los marcadores del
    // mapa en CaliGameUI.LateUpdate) y escribe una etiqueta encima de cada uno.
    // El controlador llama a Tick() cada frame mientras el puzzle está activo.
    public sealed class TerremotoHideUI
    {
        private static readonly Color Ink = new Color32(24, 35, 39, 255);
        private static readonly Color Paper = new Color32(246, 244, 235, 255);
        private static readonly Color Accent = new Color32(25, 102, 86, 255);
        private static readonly Color Good = new Color32(56, 168, 112, 255);

        private readonly float fadeSeconds;
        private readonly string title;

        private VisualElement root;
        private VisualElement layer;
        private VisualElement draw;
        private Label message;
        private readonly List<Label> labels = new List<Label>();

        private HideSpot[] spots;
        private HideSpot correct;
        private float radius = 0.7f;
        private bool visible;

        public TerremotoHideUI(float fadeSeconds, string title)
        {
            this.fadeSeconds = Mathf.Max(0.05f, fadeSeconds);
            this.title = title;
        }

        // Construye el overlay. Devuelve false si no hay UI a la que engancharse.
        public bool Build(GameSession session, HideSpot[] spots, float worldRadius)
        {
            this.spots = spots;
            this.radius = Mathf.Max(0.2f, worldRadius);
            if (layer != null) return true;

            var document = session && session.UI ? session.UI.GetComponent<UIDocument>() : null;
            root = document ? document.rootVisualElement : null;
            if (root == null) return false;

            layer = new VisualElement { name = "terremoto-hide", pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.top = 0;
            layer.style.right = 0;
            layer.style.bottom = 0;
            layer.style.display = DisplayStyle.None;
            root.Add(layer);

            draw = new VisualElement { name = "terremoto-hide-draw", pickingMode = PickingMode.Ignore };
            draw.style.position = Position.Absolute;
            draw.style.left = 0;
            draw.style.top = 0;
            draw.style.right = 0;
            draw.style.bottom = 0;
            draw.generateVisualContent += OnGenerateVisualContent;
            layer.Add(draw);

            if (spots != null)
            {
                for (int i = 0; i < spots.Length; i++)
                {
                    var label = new Label(spots[i] != null ? spots[i].Label : "") { pickingMode = PickingMode.Ignore };
                    label.style.position = Position.Absolute;
                    label.style.width = 220;
                    label.style.fontSize = 17;
                    label.style.color = Ink;
                    label.style.backgroundColor = Paper;
                    label.style.unityTextAlign = TextAnchor.MiddleCenter;
                    label.style.whiteSpace = WhiteSpace.Normal;
                    label.style.paddingLeft = 8;
                    label.style.paddingRight = 8;
                    label.style.paddingTop = 3;
                    label.style.paddingBottom = 3;
                    label.style.borderTopLeftRadius = 8;
                    label.style.borderTopRightRadius = 8;
                    label.style.borderBottomLeftRadius = 8;
                    label.style.borderBottomRightRadius = 8;
                    label.style.display = DisplayStyle.None;
                    layer.Add(label);
                    labels.Add(label);
                }
            }

            BuildBanner();
            return true;
        }

        private void BuildBanner()
        {
            var holder = new VisualElement { pickingMode = PickingMode.Ignore };
            holder.style.position = Position.Absolute;
            holder.style.left = 0;
            holder.style.right = 0;
            holder.style.top = 0;
            holder.style.marginTop = 22;
            holder.style.alignItems = Align.Center;
            layer.Add(holder);

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
            card.style.borderTopLeftRadius = 18;
            card.style.borderTopRightRadius = 18;
            card.style.borderBottomLeftRadius = 18;
            card.style.borderBottomRightRadius = 18;
            card.style.paddingLeft = 34;
            card.style.paddingRight = 34;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 14;
            card.style.alignItems = Align.Center;
            holder.Add(card);

            var heading = new Label(title) { pickingMode = PickingMode.Ignore };
            heading.style.fontSize = 34;
            heading.style.color = Accent;
            heading.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(heading);

            message = new Label("") { pickingMode = PickingMode.Ignore };
            message.style.fontSize = 20;
            message.style.color = Ink;
            message.style.unityTextAlign = TextAnchor.MiddleCenter;
            message.style.whiteSpace = WhiteSpace.Normal;
            message.style.maxWidth = 620;
            message.style.marginTop = 4;
            card.Add(message);
        }

        public void SetMessage(string text)
        {
            if (message != null) message.text = text ?? "";
        }

        public void MarkCorrect(HideSpot spot) { correct = spot; }

        public void Show(string hint)
        {
            if (layer == null) return;
            SetMessage(hint);
            visible = true;
            layer.style.display = DisplayStyle.Flex;
            layer.style.opacity = 0f;
        }

        // Reposiciona las etiquetas y repinta los anillos. La llama el controlador.
        public void Tick()
        {
            if (!visible || layer == null || root == null) return;
            for (int i = 0; i < labels.Count; i++)
            {
                var spot = spots != null && i < spots.Length ? spots[i] : null;
                if (spot == null || !TryProject(spot.transform.position, out Vector2 at, out float r))
                {
                    if (labels[i] != null) labels[i].style.display = DisplayStyle.None;
                    continue;
                }
                labels[i].style.display = DisplayStyle.Flex;
                labels[i].style.left = at.x - 110f;
                labels[i].style.top = at.y - r - 42f;
            }
            draw?.MarkDirtyRepaint();
        }

        public IEnumerator FadeIn()
        {
            if (layer == null) yield break;
            layer.style.opacity = 0f;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                layer.style.opacity = Mathf.Clamp01(t / fadeSeconds);
                yield return null;
            }
            layer.style.opacity = 1f;
        }

        public IEnumerator FadeOut()
        {
            if (layer == null) yield break;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                layer.style.opacity = 1f - Mathf.Clamp01(t / fadeSeconds);
                yield return null;
            }
            layer.style.opacity = 0f;
        }

        public void Hide()
        {
            visible = false;
            if (layer != null) layer.style.display = DisplayStyle.None;
        }

        public void Remove()
        {
            if (draw != null) draw.generateVisualContent -= OnGenerateVisualContent;
            if (layer != null) layer.RemoveFromHierarchy();
            layer = null;
            draw = null;
            message = null;
            labels.Clear();
            visible = false;
        }

        private bool TryProject(Vector2 world, out Vector2 point, out float screenRadius)
        {
            point = default;
            screenRadius = 24f;
            var camera = Camera.main;
            if (!camera || root == null || root.layout.width < 1f) return false;
            Vector3 viewport = camera.WorldToViewportPoint(new Vector3(world.x, world.y, 0f));
            if (viewport.z < 0f) return false;
            point = new Vector2(viewport.x * root.layout.width, (1f - viewport.y) * root.layout.height);
            Vector3 edge = camera.WorldToViewportPoint(new Vector3(world.x + radius, world.y, 0f));
            screenRadius = Mathf.Clamp(Mathf.Abs(edge.x - viewport.x) * root.layout.width, 14f, 240f);
            return true;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (!visible || spots == null) return;
            var p = context.painter2D;
            float time = Time.unscaledTime;
            for (int i = 0; i < spots.Length; i++)
            {
                if (spots[i] == null) continue;
                if (!TryProject(spots[i].transform.position, out Vector2 at, out float r)) continue;
                bool isCorrect = spots[i] == correct;
                Color tint = isCorrect ? Good : Accent;
                float pulse = 1f + 0.08f * Mathf.Sin(time * 3f + i);

                Disc(p, at, r * 0.55f, new Color(tint.r, tint.g, tint.b, 0.16f));
                Ring(p, at, r * pulse, new Color(1f, 1f, 1f, 0.75f), 7f);
                Ring(p, at, r * pulse, tint, 4f);

                float wave = Mathf.Repeat(time * 0.6f + i * 0.33f, 1f);
                Ring(p, at, r * (0.55f + wave * 0.9f), new Color(tint.r, tint.g, tint.b, (1f - wave) * 0.85f), 3f);

                if (isCorrect) Disc(p, at, r * 0.26f, Good);
                else Disc(p, at, r * 0.22f, new Color(tint.r, tint.g, tint.b, 0.85f));
            }
        }

        private static void Ring(Painter2D p, Vector2 center, float radius, Color color, float width)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.BeginPath();
            p.Arc(center, radius, 0, 360);
            p.ClosePath();
            p.Stroke();
        }

        private static void Disc(Painter2D p, Vector2 center, float radius, Color color)
        {
            p.fillColor = color;
            p.BeginPath();
            p.Arc(center, radius, 0, 360);
            p.ClosePath();
            p.Fill();
        }
    }
}
