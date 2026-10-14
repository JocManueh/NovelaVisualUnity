using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace CaliNature
{
    // Marcador de mundo del nivel del terremoto (UI Toolkit, estilos en código).
    //
    // Dibuja UN anillo pulsante con su etiqueta sobre un punto del mundo, igual
    // que TerremotoHideUI hace con los escondites del Puzzle 1, pero sin banda
    // superior. Se usa para señalar el punto de encuentro cuando el Puzzle 2 lo
    // habilita.
    //
    // Se añade DENTRO del panel de CaliGameUI —el único UIDocument del juego—
    // como último hijo de la raíz, así que queda por encima del HUD. Ignora el
    // picking: nunca intercepta clics ni teclas.
    //
    // Si el punto queda FUERA de cámara, en lugar del anillo dibuja una flecha
    // pegada al borde de la pantalla que apunta hacia él, para que el jugador
    // sepa por dónde ir aunque esté lejos.
    public sealed class TerremotoMarkerUI
    {
        private static readonly Color Ink = new Color32(24, 35, 39, 255);
        private static readonly Color Paper = new Color32(246, 244, 235, 255);
        private static readonly Color Accent = new Color32(25, 102, 86, 255);

        private const float LabelWidth = 220f;
        private const float EdgeMargin = 54f;

        private readonly float fadeSeconds;

        private VisualElement root;
        private VisualElement layer;
        private VisualElement draw;
        private Label label;

        private Transform target;
        private float radius = 0.8f;
        private bool visible;

        public TerremotoMarkerUI(float fadeSeconds)
        {
            this.fadeSeconds = Mathf.Max(0.05f, fadeSeconds);
        }

        // Construye el overlay. Devuelve false si no hay UI a la que engancharse.
        public bool Build(GameSession session, Transform target, string text, float worldRadius)
        {
            this.target = target;
            radius = Mathf.Max(0.2f, worldRadius);
            if (layer != null) return true;

            var document = session && session.UI ? session.UI.GetComponent<UIDocument>() : null;
            root = document ? document.rootVisualElement : null;
            if (root == null) return false;

            layer = new VisualElement { name = "terremoto-marker", pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.top = 0;
            layer.style.right = 0;
            layer.style.bottom = 0;
            layer.style.display = DisplayStyle.None;
            root.Add(layer);

            draw = new VisualElement { name = "terremoto-marker-draw", pickingMode = PickingMode.Ignore };
            draw.style.position = Position.Absolute;
            draw.style.left = 0;
            draw.style.top = 0;
            draw.style.right = 0;
            draw.style.bottom = 0;
            draw.generateVisualContent += OnGenerateVisualContent;
            layer.Add(draw);

            label = new Label(text ?? "") { pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.width = LabelWidth;
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
            layer.Add(label);

            return true;
        }

        public void SetLabel(string text)
        {
            if (label != null) label.text = text ?? "";
        }

        public void Show()
        {
            if (layer == null) return;
            visible = true;
            layer.style.display = DisplayStyle.Flex;
            layer.style.opacity = 0f;
        }

        // Muestra u oculta el overlay sin tocar la opacidad: se usa para pausarlo
        // mientras hay un diálogo o un modal abierto (no debe dibujarse encima).
        public void SetVisible(bool value)
        {
            visible = value;
            if (layer != null) layer.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Reposiciona la etiqueta y repinta el anillo o la flecha. La llama el controlador.
        public void Tick()
        {
            if (!visible || layer == null || root == null || label == null) return;
            if (!TryProject(out Vector2 at, out float r))
            {
                label.style.display = DisplayStyle.None;
                draw?.MarkDirtyRepaint();
                return;
            }

            float w = root.layout.width;
            float h = root.layout.height;
            Vector2 anchor = OnScreen(at, w, h) ? new Vector2(at.x, at.y - r - 42f) : ClampToEdge(at, w, h, out _);
            label.style.display = DisplayStyle.Flex;
            label.style.left = Mathf.Clamp(anchor.x - LabelWidth * 0.5f, 6f, Mathf.Max(6f, w - LabelWidth - 6f));
            label.style.top = Mathf.Clamp(anchor.y + 44f, 6f, Mathf.Max(6f, h - 72f));
            draw?.MarkDirtyRepaint();
        }

        public IEnumerator FadeIn()
        {
            if (layer == null) yield break;
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
            label = null;
            visible = false;
        }

        private bool TryProject(out Vector2 point, out float screenRadius)
        {
            point = default;
            screenRadius = 24f;
            var camera = Camera.main;
            if (!target || !camera || root == null || root.layout.width < 1f) return false;
            Vector3 viewport = camera.WorldToViewportPoint(target.position);
            if (viewport.z < 0f) return false;
            point = new Vector2(viewport.x * root.layout.width, (1f - viewport.y) * root.layout.height);
            Vector3 edge = camera.WorldToViewportPoint(target.position + Vector3.right * radius);
            screenRadius = Mathf.Clamp(Mathf.Abs(edge.x - viewport.x) * root.layout.width, 14f, 240f);
            return true;
        }

        private static bool OnScreen(Vector2 point, float width, float height)
        {
            return point.x >= 0f && point.x <= width && point.y >= 0f && point.y <= height;
        }

        // Punto sobre el borde de la pantalla en la dirección del objetivo, y el
        // ángulo hacia el que debe apuntar la flecha.
        private static Vector2 ClampToEdge(Vector2 point, float width, float height, out float angle)
        {
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            Vector2 dir = point - center;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();
            angle = Mathf.Atan2(dir.y, dir.x);

            float halfW = Mathf.Max(1f, width * 0.5f - EdgeMargin);
            float halfH = Mathf.Max(1f, height * 0.5f - EdgeMargin);
            float tx = Mathf.Abs(dir.x) > 0.0001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue;
            float ty = Mathf.Abs(dir.y) > 0.0001f ? halfH / Mathf.Abs(dir.y) : float.MaxValue;
            return center + dir * Mathf.Min(tx, ty);
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (!visible || !TryProject(out Vector2 at, out float r)) return;
            var p = context.painter2D;
            float time = Time.unscaledTime;
            float w = root.layout.width;
            float h = root.layout.height;

            if (OnScreen(at, w, h))
            {
                float pulse = 1f + 0.08f * Mathf.Sin(time * 3f);
                Disc(p, at, r * 0.55f, new Color(Accent.r, Accent.g, Accent.b, 0.16f));
                Ring(p, at, r * pulse, new Color(1f, 1f, 1f, 0.75f), 7f);
                Ring(p, at, r * pulse, Accent, 4f);

                float wave = Mathf.Repeat(time * 0.6f, 1f);
                Ring(p, at, r * (0.55f + wave * 0.9f), new Color(Accent.r, Accent.g, Accent.b, (1f - wave) * 0.85f), 3f);
                Disc(p, at, r * 0.22f, new Color(Accent.r, Accent.g, Accent.b, 0.85f));
                return;
            }

            // Fuera de cámara: flecha pegada al borde apuntando al objetivo.
            Vector2 edge = ClampToEdge(at, w, h, out float angle);
            float size = 20f + 2f * Mathf.Sin(time * 3f);
            Disc(p, edge, size * 0.95f, Paper);
            Ring(p, edge, size * 0.95f, Accent, 4f);
            Arrow(p, edge + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (size * 0.1f), angle, size);
        }

        private static void Arrow(Painter2D p, Vector2 center, float angle, float size)
        {
            p.fillColor = Accent;
            p.BeginPath();
            p.MoveTo(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size);
            p.LineTo(center + new Vector2(Mathf.Cos(angle + 2.5f), Mathf.Sin(angle + 2.5f)) * size);
            p.LineTo(center + new Vector2(Mathf.Cos(angle - 2.5f), Mathf.Sin(angle - 2.5f)) * size);
            p.ClosePath();
            p.Fill();
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
