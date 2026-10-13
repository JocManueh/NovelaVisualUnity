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
            new Color32(247, 125, 118, 255), new Color32(34, 197, 223, 255),
            new Color32(88, 171, 220, 255), new Color32(112, 194, 140, 255)
        };
        private static readonly Color Ink = new Color32(26, 26, 26, 255);
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
                Stroke(p, a, c, d, b, new Color(0.1f, 0.1f, 0.1f, .8f), 9);
                Stroke(p, a, c, d, b, highlight ? new Color32(143, 233, 255, 255) : Accents[i], highlight ? 6 : 4);
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
                DisasterEffect(p, i, at, radius, time);
                Disc(p, at, radius, StatusColors[i]);
                Ring(p, at, radius + 7, Ink, 8);
                Ring(p, at, radius + 7, Accents[i], 5);
                if (highlighted)
                    Ring(p, at, radius + 15 + Mathf.Sin(time * 3) * 2, new Color(0.56f, .91f, 1, .9f), 3);
                Ring(p, at, radius - 2, new Color(1, 1, 1, .85f), 2);
                Icon(p, i, at, Mathf.Min(radius * .5f, 18));
            }
        }
        // Decorative effects follow the same projected position/zoom as the selectable marker.
        // They never take pointer input or change completion state.
        private static void DisasterEffect(Painter2D p, int kind, Vector2 at, float radius, float time)
        {
            float r = Mathf.Max(12, radius), outer = r + 14;
            if (kind == 0)
            {
                float cycle = Mathf.Repeat(time * .32f, 1);
                float growth = Mathf.Clamp01(cycle * 3);
                float alpha = cycle < .65f ? .9f : Mathf.Lerp(.9f, 0, (cycle - .65f) / .35f);
                for (int k = 0; k < 6; k++)
                {
                    float angle = (k * 60 + 12) * Mathf.Deg2Rad;
                    Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 side = new Vector2(-axis.y, axis.x);
                    Vector2 a = at + axis * outer;
                    Vector2 b = a + (axis * r * .22f + side * r * .12f) * growth;
                    Vector2 c = a + (axis * r * .43f - side * r * .10f) * growth;
                    Vector2 d = a + (axis * r * .72f + side * r * .08f) * growth;
                    for (int pass = 0; pass < 2; pass++)
                    {
                        p.strokeColor = pass == 0 ? new Color(.08f,.07f,.07f,alpha) : new Color(1,.59f,.40f,alpha);
                        p.lineWidth = pass == 0 ? 6 : 2;
                        p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.LineTo(c); p.LineTo(d); p.Stroke();
                        p.BeginPath(); p.MoveTo(c); p.LineTo(c + (axis * r * .08f + side * r * .28f) * growth); p.Stroke();
                    }
                }
            }
            else if (kind == 1)
            {
                for (int bank = -1; bank <= 1; bank += 2)
                for (int k = 0; k < 3; k++)
                {
                    float phase = Mathf.Repeat(time * .38f + k / 3f, 1);
                    float y = bank * (outer + phase * r * .58f);
                    float alpha = Mathf.Sin(phase * Mathf.PI) * .8f;
                    Vector2 a = at + new Vector2(-r * .95f, y);
                    Vector2 b = at + new Vector2(r * .95f, y);
                    float swell = Mathf.Sin(time * 2.5f + k) * r * .24f;
                    Stroke(p,a,a+new Vector2(r*.60f,-r*.35f+swell),b+new Vector2(-r*.60f,r*.35f-swell),b,new Color(0,.28f,.40f,alpha),7);
                    Stroke(p,a,a+new Vector2(r*.60f,-r*.35f+swell),b+new Vector2(-r*.60f,r*.35f-swell),b,new Color(.28f,.85f,1,alpha),3);
                }
            }
            else if (kind == 2)
            {
                for (int k = 0; k < 7; k++)
                {
                    float angle = (195 + k * 25) * Mathf.Deg2Rad;
                    Vector2 baseAt = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (outer + r * .08f);
                    float flicker = .75f + .25f * Mathf.Sin(time * 5 + k * 2.1f);
                    float height = r * (.55f + .25f * flicker);
                    Flame(p,baseAt,r*.18f,height,Mathf.Sin(time*3+k)*r*.12f,new Color(1,.28f,.08f,.85f));
                    Flame(p,baseAt,r*.09f,height*.58f,Mathf.Sin(time*4+k)*r*.06f,new Color(1,.78f,.22f,.95f));
                }
                for (int k = 0; k < 5; k++)
                {
                    float phase = Mathf.Repeat(time * .21f + k * .2f, 1);
                    Vector2 puff = at + new Vector2(Mathf.Sin(k * 2.2f + time * .7f) * r * .65f, -outer - r * .3f - phase * r * 1.05f);
                    Disc(p,puff,r*(.13f+.17f*phase),new Color(.22f,.25f,.28f,(1-phase)*.4f));
                }
            }
            else
            {
                for (int k = 0; k < 7; k++)
                {
                    float phase = Mathf.Repeat(time * .38f + k / 7f, 1);
                    float side = k % 2 == 0 ? -1 : 1;
                    Vector2 rockAt = at + new Vector2(side * (outer + r * .17f + phase * r * .27f), -r * .9f + phase * phase * r * 2.15f);
                    float alpha = Mathf.Clamp01(phase * 6) * Mathf.Clamp01((1-phase)*5);
                    float size = r * (.10f + (k % 3) * .035f);
                    Stroke(p,rockAt-new Vector2(side*5,14),rockAt-new Vector2(side*3,9),rockAt-new Vector2(side*2,6),rockAt,new Color(.62f,.55f,.43f,alpha*.4f),2);
                    Rock(p,rockAt,size,time*side*2+k,new Color(.48f,.39f,.29f,alpha));
                    Disc(p,rockAt+new Vector2(-size*.2f,-size*.2f),size*.25f,new Color(.88f,.77f,.55f,alpha));
                }
            }
        }
        private static void Flame(Painter2D p, Vector2 at, float width, float height, float sway, Color color)
        {
            p.fillColor=color; p.BeginPath(); p.MoveTo(at+new Vector2(-width,0));
            p.BezierCurveTo(at+new Vector2(-width*1.6f,-height*.5f),at+new Vector2(sway-width,-height*.65f),at+new Vector2(sway,-height));
            p.BezierCurveTo(at+new Vector2(width+sway,-height*.42f),at+new Vector2(width*1.3f,-height*.3f),at+new Vector2(width,0));
            p.ClosePath(); p.Fill();
        }
        private static void Rock(Painter2D p, Vector2 at, float size, float rotation, Color color)
        {
            p.fillColor=color; p.strokeColor=new Color(.12f,.13f,.12f,color.a); p.lineWidth=1.5f; p.BeginPath();
            for(int i=0;i<5;i++)
            {
                float angle=rotation+i*Mathf.PI*2/5;
                Vector2 v=at+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size*(i%2==0?1:.75f);
                if(i==0)p.MoveTo(v);else p.LineTo(v);
            }
            p.ClosePath();p.Fill();p.Stroke();
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
                // Broken ground, rather than the previous drought sun.
                p.BeginPath(); p.MoveTo(at + new Vector2(-size, size * .65f));
                p.LineTo(at + new Vector2(-size * .2f, size * .65f));
                p.LineTo(at + new Vector2(size * .12f, size * .1f));
                p.LineTo(at + new Vector2(-size * .25f, -size * .25f));
                p.LineTo(at + new Vector2(size * .15f, -size)); p.Stroke();
                p.BeginPath(); p.MoveTo(at + new Vector2(size * .18f, size * .85f));
                p.LineTo(at + new Vector2(size * .4f, size * .45f));
                p.LineTo(at + new Vector2(size, size * .45f)); p.Stroke();
            }
            else if (kind == 1)
            {
                for (int k=-1;k<=1;k++) {
                    Vector2 a=at+new Vector2(-size,k*size*.65f);
                    Stroke(p,a,a+new Vector2(size*.65f,-size*.55f),a+new Vector2(size*1.35f,size*.55f),a+new Vector2(size*2,0),Color.white,2.5f);
                }
            }
            else if (kind == 2)
            {
                p.BeginPath(); p.MoveTo(at + new Vector2(0, -size));
                p.BezierCurveTo(at + new Vector2(-size * .15f, -size * .2f), at + new Vector2(-size, -size * .05f), at + new Vector2(-size * .7f, size * .55f));
                p.BezierCurveTo(at + new Vector2(-size * .35f, size * 1.2f), at + new Vector2(size * .8f, size), at + new Vector2(size * .7f, size * .15f));
                p.LineTo(at + new Vector2(size * .35f, -size * .35f));
                p.LineTo(at + new Vector2(size * .2f, size * .1f));
                p.ClosePath(); p.Stroke();
            }
            else
            {
                p.BeginPath(); p.MoveTo(at+new Vector2(-size,size*.7f));
                p.LineTo(at+new Vector2(-size*.15f,-size));p.LineTo(at+new Vector2(size*.55f,size*.15f));p.Stroke();
                Disc(p, at + new Vector2(size * .65f, size * .45f), size * .18f, Color.white);
                Disc(p, at + new Vector2(size * .3f, size * .8f), size * .13f, Color.white);
                Disc(p, at + new Vector2(size, size * .95f), size * .16f, Color.white);
            }
        }
    }
}
