using UnityEngine;
using UnityEngine.UIElements;

namespace CaliNature
{
    // Screen-space interface only: follows the map's existing, editor-authored markers.
    public sealed class CaliMapConnections : VisualElement
    {
        public readonly Vector2[] Points = new Vector2[4];
        public readonly float[] Radii = new float[4];
        public readonly bool[] Active = new bool[4];
        public readonly Color[] StatusColors = new Color[4];
        public int Selected = -1, Hovered = -1;
        public static readonly Color[] Accents = {
            new Color32(241, 147, 45, 255), new Color32(39, 142, 211, 255),
            new Color32(156, 107, 204, 255), new Color32(230, 101, 67, 255)
        };
        private static readonly Color Ink = new Color32(20, 54, 68, 255);
        public CaliMapConnections()
        {
            name = "map-connections";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = style.top = style.right = style.bottom = 0;
            generateVisualContent += Draw;
        }
        public int Hit(Vector2 point)
        {
            for (int i = 0; i < 4; i++)
                if (Active[i] && Vector2.Distance(point, Points[i]) <= Radii[i] + 10) return i;
            return -1;
        }
        private void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D;
            float time = Time.unscaledTime;
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                if (!Active[i] || !Active[j]) continue;
                Vector2 a = Points[i], b = Points[j], delta = b - a;
                Vector2 bend = new Vector2(-delta.y, delta.x).normalized * Mathf.Min(65, delta.magnitude * .17f);
                Vector2 c = a + delta / 3 + bend, d = a + delta * 2 / 3 + bend;
                bool highlight = Selected == i || Selected == j || Hovered == i || Hovered == j;
                Stroke(p, a, c, d, b, new Color(1, 1, 1, .92f), 10);
                Stroke(p, a, c, d, b, highlight ? new Color32(238, 126, 40, 255) : Accents[i], highlight ? 6 : 4);
                for (int k = 1; k < 3; k++)
                {
                    float t = Mathf.Repeat(time * .075f + k * .4f + i * .14f, 1);
                    Vector2 pos = Curve(a, c, d, b, t);
                    Disc(p, pos, 6, new Color(1, 1, 1, .3f));
                    Disc(p, pos, 3, Color.white);
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (!Active[i]) continue;
                Vector2 at = Points[i]; float radius = Radii[i];
                bool highlighted = i == Selected || i == Hovered;
                Disc(p, at, radius, StatusColors[i]);
                Ring(p, at, radius + 7, Ink, 8);
                Ring(p, at, radius + 7, Accents[i], 5);
                if (highlighted)
                    Ring(p, at, radius + 15 + Mathf.Sin(time * 3) * 2, new Color(1, .79f, .27f, .9f), 3);
                Ring(p, at, radius - 2, new Color(1, 1, 1, .85f), 2);
                Icon(p, i, at, Mathf.Min(radius * .5f, 18));
            }
        }
        private static Vector2 Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        { float u = 1 - t; return u*u*u*a + 3*u*u*t*b + 3*u*t*t*c + t*t*t*d; }
        private static void Stroke(Painter2D p, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color, float width)
        { p.strokeColor = color; p.lineWidth = width; p.BeginPath(); p.MoveTo(a); p.BezierCurveTo(b,c,d); p.Stroke(); }
        private static void Ring(Painter2D p, Vector2 center, float radius, Color color, float width)
        { p.strokeColor=color; p.lineWidth=width; p.BeginPath(); p.Arc(center, radius, 0, 360); p.ClosePath(); p.Stroke(); }
        private static void Disc(Painter2D p, Vector2 center, float radius, Color color)
        { p.fillColor=color; p.BeginPath(); p.Arc(center, radius, 0, 360); p.ClosePath(); p.Fill(); }
        private static void Icon(Painter2D p, int kind, Vector2 at, float size)
        {
            p.strokeColor=Color.white; p.lineWidth=2.5f;
            if (kind == 0)
            {
                Ring(p,at,size*.48f,Color.white,2.5f);
                for(int k=0;k<8;k++) {
                    float angle=k*Mathf.PI/4; Vector2 ray=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    p.BeginPath();p.MoveTo(at+ray*size*.72f);p.LineTo(at+ray*size);p.Stroke();
                }
            }
            else if (kind == 1 || kind == 2)
            {
                for (int k=-1;k<=1;k++) {
                    Vector2 a=at+new Vector2(-size,k*size*.65f);
                    Stroke(p,a,a+new Vector2(size*.65f,-size*.55f),a+new Vector2(size*1.35f,size*.55f),a+new Vector2(size*2,0),Color.white,2.5f);
                    if(kind==2) Ring(p,a+new Vector2(size*2,-size*.19f),size*.19f,Color.white,2);
                }
            }
            else
            {
                p.BeginPath(); p.MoveTo(at+new Vector2(-size,size*.7f));
                p.LineTo(at+new Vector2(-size*.15f,-size));p.LineTo(at+new Vector2(size,size*.7f));p.Stroke();
                Stroke(p,at+new Vector2(-size,size),at+new Vector2(0,size*.65f),at+new Vector2(0,size*1.35f),at+new Vector2(size,size),Color.white,2.5f);
            }
        }
    }
}
