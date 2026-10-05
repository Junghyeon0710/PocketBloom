using UnityEngine;
using UnityEngine.UI;

namespace PocketBloom
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BloomGraphic : MaskableGraphic
    {
        public float radius = 18;
        public bool flower;
        public bool inset;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            if (flower)
            {
                float size = Mathf.Min(r.width, r.height), petal = size * .22f;
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2 / 5 + Mathf.PI / 2;
                    Circle(vh, r.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size * .22f, petal, color);
                }
                Circle(vh, r.center, size * .16f, new Color(1, .93f, .61f, color.a));
                return;
            }
            if (radius > 0)
            {
                Rounded(vh, r, radius, Color.Lerp(color, inset ? Color.white : Color.black, inset ? .2f : .18f), false);
                var inner = new Rect(r.x + 2, r.y + 2, Mathf.Max(0, r.width - 4), Mathf.Max(0, r.height - 4));
                Rounded(vh, inner, Mathf.Max(0, radius - 2), color, true);
            }
            else Rounded(vh, r, 0, color, false);
        }
        void Rounded(VertexHelper vh, Rect r, float radius, Color surface, bool gradient)
        {
            float rad = Mathf.Min(radius, Mathf.Min(r.width, r.height) / 2);
            int center = vh.currentVertCount; Vertex(vh, r.center, surface);
            for (int c = 0; c < 4; c++)
            {
                var corner = new Vector2(c == 0 || c == 3 ? r.xMax - rad : r.xMin + rad,
                    c < 2 ? r.yMax - rad : r.yMin + rad);
                for (int j = 0; j <= 12; j++)
                {
                    float a = (c * 90 + j * 7.5f) * Mathf.Deg2Rad;
                    var p = corner + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                    var top = Color.Lerp(surface, inset ? Color.black : Color.white, inset ? .13f : .22f);
                    var bottom = Color.Lerp(surface, inset ? Color.white : Color.black, inset ? .08f : .06f);
                    top.a = bottom.a = surface.a;
                    Vertex(vh, p, gradient ? Color.Lerp(bottom, top, Mathf.InverseLerp(r.yMin, r.yMax, p.y)) : surface);
                }
            }
            for (int i = 0; i < 52; i++) vh.AddTriangle(center, center + 1 + i, center + 1 + (i + 1) % 52);
        }
        static void Vertex(VertexHelper vh, Vector2 p, Color c)
        { var v = UIVertex.simpleVert; v.position = p; v.color = c; vh.AddVert(v); }
        static void Circle(VertexHelper vh, Vector2 p, float radius, Color c)
        {
            int start = vh.currentVertCount; Vertex(vh, p, c);
            for (int i = 0; i < 20; i++)
            { float a = i * Mathf.PI * 2 / 20; Vertex(vh, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, c); }
            for (int i = 0; i < 20; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 20);
        }
    }
}
