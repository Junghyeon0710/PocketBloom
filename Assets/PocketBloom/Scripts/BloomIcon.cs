using UnityEngine;
using UnityEngine.UI;

namespace PocketBloom
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BloomIcon : MaskableGraphic
    {
        public enum Kind { Gear, Seed, Back, Pause, Undo, Hint, Shuffle, Chevron }
        public Kind kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch (kind)
            {
                case Kind.Back: Stroke(vh, .64f, .17f, .32f, .5f); Stroke(vh, .32f, .5f, .64f, .83f); break;
                case Kind.Chevron: Stroke(vh, .3f, .17f, .7f, .5f); Stroke(vh, .7f, .5f, .3f, .83f); break;
                case Kind.Pause: Stroke(vh, .32f, .18f, .32f, .82f); Stroke(vh, .68f, .18f, .68f, .82f); break;
                case Kind.Gear:
                    Arc(vh, .5f, .5f, .24f, 0, 360);
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; Stroke(vh, .5f + Mathf.Cos(a) * .26f, .5f + Mathf.Sin(a) * .26f, .5f + Mathf.Cos(a) * .4f, .5f + Mathf.Sin(a) * .4f); }
                    break;
                case Kind.Seed:
                    Stroke(vh, .5f, .12f, .54f, .64f);
                    Ellipse(vh, .3f, .57f, .19f, .29f, 45); Ellipse(vh, .69f, .72f, .18f, .29f, -40); break;
                case Kind.Hint:
                    Ellipse(vh, .5f, .59f, .24f, .29f, 0); Stroke(vh, .39f, .24f, .61f, .24f); Stroke(vh, .44f, .13f, .56f, .13f);
                    for (int i = 0; i < 5; i++) { float a = (i * 45) * Mathf.Deg2Rad; Stroke(vh, .5f + Mathf.Cos(a) * .36f, .59f + Mathf.Sin(a) * .36f, .5f + Mathf.Cos(a) * .45f, .59f + Mathf.Sin(a) * .45f, .055f); }
                    break;
                case Kind.Undo:
                    Arc(vh, .53f, .44f, .29f, -60, 175); Triangle(vh, .1f, .52f, .37f, .79f, .37f, .3f); break;
                case Kind.Shuffle:
                    Arc(vh, .5f, .5f, .29f, 35, 155); Arc(vh, .5f, .5f, .29f, 215, 335);
                    Triangle(vh, .82f, .5f, .61f, .86f, .95f, .77f); Triangle(vh, .18f, .5f, .39f, .14f, .05f, .23f); break;
            }
        }
        Vector2 Point(float x, float y) { var r = rectTransform.rect; return new Vector2(r.xMin + x * r.width, r.yMin + y * r.height); }
        void Stroke(VertexHelper vh, float x1, float y1, float x2, float y2, float thickness = .105f)
        {
            var a = Point(x1, y1); var b = Point(x2, y2); float width = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * thickness;
            var n = new Vector2(-(b - a).y, (b - a).x).normalized * width / 2;
            int s = vh.currentVertCount; Vertex(vh, a - n); Vertex(vh, a + n); Vertex(vh, b + n); Vertex(vh, b - n);
            vh.AddTriangle(s, s + 1, s + 2); vh.AddTriangle(s, s + 2, s + 3);
            Disc(vh, a, width / 2); Disc(vh, b, width / 2);
        }
        void Arc(VertexHelper vh, float x, float y, float radius, float start, float end)
        {
            for (int i = 0; i < 24; i++) { float a = Mathf.Lerp(start, end, i / 24f) * Mathf.Deg2Rad, b = Mathf.Lerp(start, end, (i + 1) / 24f) * Mathf.Deg2Rad;
                Stroke(vh, x + Mathf.Cos(a) * radius, y + Mathf.Sin(a) * radius, x + Mathf.Cos(b) * radius, y + Mathf.Sin(b) * radius); }
        }
        void Ellipse(VertexHelper vh, float x, float y, float rx, float ry, float angle)
        {
            int s = vh.currentVertCount; Vertex(vh, Point(x, y)); float a = angle * Mathf.Deg2Rad;
            for (int i = 0; i < 24; i++) { float t = i * Mathf.PI / 12; var p = new Vector2(Mathf.Cos(t) * rx, Mathf.Sin(t) * ry); Vertex(vh, Point(x + p.x * Mathf.Cos(a) - p.y * Mathf.Sin(a), y + p.x * Mathf.Sin(a) + p.y * Mathf.Cos(a))); }
            for (int i = 0; i < 24; i++) vh.AddTriangle(s, s + 1 + i, s + 1 + (i + 1) % 24);
        }
        void Disc(VertexHelper vh, Vector2 p, float radius)
        {
            int s = vh.currentVertCount; Vertex(vh, p);
            for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6; Vertex(vh, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius); }
            for (int i = 0; i < 12; i++) vh.AddTriangle(s, s + 1 + i, s + 1 + (i + 1) % 12);
        }
        void Triangle(VertexHelper vh, float x1, float y1, float x2, float y2, float x3, float y3)
        { int s = vh.currentVertCount; Vertex(vh, Point(x1, y1)); Vertex(vh, Point(x2, y2)); Vertex(vh, Point(x3, y3)); vh.AddTriangle(s, s + 1, s + 2); }
        void Vertex(VertexHelper vh, Vector2 p) { var v = UIVertex.simpleVert; v.position = p; v.color = color; vh.AddVert(v); }
    }
}
