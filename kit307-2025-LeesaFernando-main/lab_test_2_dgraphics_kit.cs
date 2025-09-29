using System;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------------------------------------------------------
// LabTest_2DGraphicsKit
// One file you can drop into Assets/Scripts/ for the in‑tutorial test.
// - All FILLS use HORIZONTAL LINES ONLY (scanline or mirrored bands)
// - Includes DDA line, arcs via trig, filled triangle/polygon, filled pie wedge,
//   filled ring segment, rounded rect, and simple 3x3 transforms (T/R/S + compose)
// - Includes a minimal demo MonoBehaviour that hits every rubric item and is easy
//   to adapt to any theme.
// -----------------------------------------------------------------------------

public static class Texture2DExtensions
{
    // ================== BASIC PIXEL I/O ==================
    public static void SetPixelSafe(this Texture2D tex, int x, int y, Color c)
    {
        if (!tex) return;
        if ((uint)x >= (uint)tex.width || (uint)y >= (uint)tex.height) return;
        tex.SetPixel(x, y, c);
    }

    public static void DrawHLine(this Texture2D tex, int y, int x0, int x1, Color c)
    {
        if (!tex) return;
        if (y < 0 || y >= tex.height) return;
        if (x0 > x1) { var t = x0; x0 = x1; x1 = t; }
        x0 = Mathf.Clamp(x0, 0, tex.width - 1);
        x1 = Mathf.Clamp(x1, 0, tex.width - 1);
        for (int x = x0; x <= x1; x++) tex.SetPixel(x, y, c);
    }

    // ================== LINES ==================
    // (Given) DDA line — kept verbatim style for your rubric
    public static void DrawDDALine(this Texture2D texture, int x1, int y1, int x2, int y2, Color c)
    {
        int deltay = (y2 - y1);
        int deltax = (x2 - x1);
        int steps = Mathf.Max(Mathf.Abs(deltax), Mathf.Abs(deltay));
        float xInc = (float)deltax / steps;
        float yInc = (float)deltay / steps;
        float x = x1;
        float y = y1;
        for (int i = 0; i <= steps; i++)
        {
            texture.SetPixel(Mathf.RoundToInt(x), Mathf.RoundToInt(y), c);
            x += xInc; y += yInc;
        }
    }

    // Optional: Bresenham (integer-heavy)
    public static void DrawBresenhamLine(this Texture2D tex, int x0, int y0, int x1, int y1, Color c)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            tex.SetPixelSafe(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    // ================== ARCS & CIRCLES ==================
    // Arc using trigonometry (angles in degrees). Samples ~1px spacing.
    public static void DrawArcTrig(this Texture2D tex, int cx, int cy, float radius, float startDeg, float endDeg, Color c)
    {
        if (radius <= 0) return;
        float arcLen = Mathf.Deg2Rad * Mathf.Abs(endDeg - startDeg) * radius;
        int steps = Mathf.Max(6, Mathf.CeilToInt(arcLen));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float a = Mathf.LerpAngle(startDeg, endDeg, t) * Mathf.Deg2Rad;
            int x = cx + Mathf.RoundToInt(radius * Mathf.Cos(a));
            int y = cy + Mathf.RoundToInt(radius * Mathf.Sin(a));
            tex.SetPixelSafe(x, y, c);
        }
    }

    // Filled circle via Pythagoras, mirrored horizontal bands (as requested style)
    public static void DrawFilledCirclePythagoras(this Texture2D texture, int centrex, int centrey, int radius, Color c)
    {
        for (int y = 0; y <= radius; y++)
        {
            int x = Mathf.RoundToInt(Mathf.Sqrt(radius * radius - y * y));
            texture.DrawDDALine(centrex - x, centrey + y, centrex + x, centrey + y, c);
            texture.DrawDDALine(centrex - x, centrey - y, centrex + x, centrey - y, c);
        }
    }

    // ================== FILLED STRAIGHT-EDGED SHAPES ==================
    // Triangle fill (scanline using only horizontal lines)
    public static void DrawFilledTriangle(this Texture2D tex, Vector2 a, Vector2 b, Vector2 c, Color col)
    {
        var pts = new List<Vector2> { a, b, c };
        tex.DrawFilledPolygon(pts, col);
    }

    // Generic polygon scanline fill using H-lines only
    public static void DrawFilledPolygon(this Texture2D tex, IList<Vector2> pts, Color col)
    {
        if (pts == null || pts.Count < 3) return;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var p in pts)
        {
            int py = Mathf.RoundToInt(p.y);
            if (py < minY) minY = py;
            if (py > maxY) maxY = py;
        }
        for (int y = minY; y <= maxY; y++)
        {
            var xs = new List<int>();
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 v0 = pts[i];
                Vector2 v1 = pts[(i + 1) % pts.Count];
                if (v0.y > v1.y) { var t = v0; v0 = v1; v1 = t; }
                if (Mathf.Approximately(v0.y, v1.y)) continue; // skip horizontals
                if (y < v0.y || y >= v1.y) continue;
                float t2 = (y - v0.y) / (v1.y - v0.y);
                float x = Mathf.Lerp(v0.x, v1.x, t2);
                xs.Add(Mathf.RoundToInt(x));
            }
            xs.Sort();
            for (int k = 0; k + 1 < xs.Count; k += 2)
                tex.DrawHLine(y, xs[k], xs[k + 1], col);
        }
    }

    // ================== FILLED CURVED-EDGE SHAPES (NON-CIRCLE) ==================
    // Pie wedge: center -> arc -> center, filled with scanline polygon
    public static void DrawFilledPieWedge(this Texture2D tex, int cx, int cy, float radius, float startDeg, float endDeg, Color col)
    {
        if (radius <= 0) return;
        float arcLen = Mathf.Deg2Rad * Mathf.Abs(endDeg - startDeg) * radius;
        int steps = Mathf.Max(6, Mathf.CeilToInt(arcLen));
        var poly = new List<Vector2>(steps + 3);
        poly.Add(new Vector2(cx, cy));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float a = Mathf.LerpAngle(startDeg, endDeg, t) * Mathf.Deg2Rad;
            poly.Add(new Vector2(cx + radius * Mathf.Cos(a), cy + radius * Mathf.Sin(a)));
        }
        poly.Add(new Vector2(cx, cy));
        tex.DrawFilledPolygon(poly, col);
    }

    // Ring segment (donut slice) between inner/outer radii and two angles
    public static void DrawFilledRingSegment(this Texture2D tex, int cx, int cy, float rInner, float rOuter, float startDeg, float endDeg, Color col)
    {
        if (rOuter <= 0 || rOuter <= rInner) return;
        float arcLen = Mathf.Deg2Rad * Mathf.Abs(endDeg - startDeg) * rOuter;
        int steps = Mathf.Max(6, Mathf.CeilToInt(arcLen));
        var poly = new List<Vector2>(2 * steps + 4);
        // Outer arc
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float a = Mathf.LerpAngle(startDeg, endDeg, t) * Mathf.Deg2Rad;
            poly.Add(new Vector2(cx + rOuter * Mathf.Cos(a), cy + rOuter * Mathf.Sin(a)));
        }
        // Inner arc (reverse)
        for (int i = steps; i >= 0; i--)
        {
            float t = i / (float)steps;
            float a = Mathf.LerpAngle(startDeg, endDeg, t) * Mathf.Deg2Rad;
            poly.Add(new Vector2(cx + rInner * Mathf.Cos(a), cy + rInner * Mathf.Sin(a)));
        }
        tex.DrawFilledPolygon(poly, col);
    }

    // Rounded rectangle via 4 quarter arcs approximated into a polygon
    public static void DrawFilledRoundedRect(this Texture2D tex, Rect r, float radius, Color col)
    {
        radius = Mathf.Clamp(radius, 0f, Mathf.Min(r.width, r.height) * 0.5f);
        int steps = Mathf.Max(6, Mathf.CeilToInt(radius));
        var poly = new List<Vector2>(steps * 4 + 4);
        void AddArc(float cx, float cy, float a0, float a1)
        {
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float a = Mathf.Lerp(a0, a1, t) * Mathf.Deg2Rad;
                poly.Add(new Vector2(cx + radius * Mathf.Cos(a), cy + radius * Mathf.Sin(a)));
            }
        }
        // Start at top-left, go clockwise
        AddArc(r.x + radius, r.yMax - radius, 180, 90);
        AddArc(r.xMax - radius, r.yMax - radius, 90, 0);
        AddArc(r.xMax - radius, r.y + radius, 0, -90);
        AddArc(r.x + radius, r.y + radius, -90, -180);
        tex.DrawFilledPolygon(poly, col);
    }

    // ================== 2D TRANSFORMS (3x3 AFFINE) ==================
    public struct Mat3
    {
        // Row-major
        public float m00, m01, m02;
        public float m10, m11, m12;
        public float m20, m21, m22;
        public static Mat3 Identity => new Mat3 { m00 = 1, m11 = 1, m22 = 1 };
        public static Mat3 operator *(Mat3 a, Mat3 b)
        {
            Mat3 r;
            r.m00 = a.m00*b.m00 + a.m01*b.m10 + a.m02*b.m20;
            r.m01 = a.m00*b.m01 + a.m01*b.m11 + a.m02*b.m21;
            r.m02 = a.m00*b.m02 + a.m01*b.m12 + a.m02*b.m22;
            r.m10 = a.m10*b.m00 + a.m11*b.m10 + a.m12*b.m20;
            r.m11 = a.m10*b.m01 + a.m11*b.m11 + a.m12*b.m21;
            r.m12 = a.m10*b.m02 + a.m11*b.m12 + a.m12*b.m22;
            r.m20 = a.m20*b.m00 + a.m21*b.m10 + a.m22*b.m20;
            r.m21 = a.m20*b.m01 + a.m21*b.m11 + a.m22*b.m21;
            r.m22 = a.m20*b.m02 + a.m21*b.m12 + a.m22*b.m22;
            return r;
        }
        public Vector2 MulPoint(Vector2 p)
        {
            return new Vector2(
                m00 * p.x + m01 * p.y + m02,
                m10 * p.x + m11 * p.y + m12
            );
        }
        public static Mat3 Translation(float tx, float ty)
        { var m = Identity; m.m02 = tx; m.m12 = ty; return m; }
        public static Mat3 Scale(float sx, float sy)
        { var m = Identity; m.m00 = sx; m.m11 = sy; return m; }
        public static Mat3 Rotation(float deg)
        {
            float a = deg * Mathf.Deg2Rad; float c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Mat3 { m00 = c, m01 = -s, m02 = 0, m10 = s, m11 = c, m12 = 0, m20 = 0, m21 = 0, m22 = 1 };
        }
    }

    public static Mat3 TRS(Vector2 t, float rotDeg, Vector2 s)
        => Mat3.Translation(t.x, t.y) * Mat3.Rotation(rotDeg) * Mat3.Scale(s.x, s.y);

    public static List<Vector2> TransformPoints(this IList<Vector2> pts, Mat3 m)
    {
        var outPts = new List<Vector2>(pts.Count);
        for (int i = 0; i < pts.Count; i++) outPts.Add(m.MulPoint(pts[i]));
        return outPts;
    }

    public static void DrawPolygonOutline(this Texture2D tex, IList<Vector2> pts, Color col)
    {
        if (pts == null || pts.Count < 2) return;
        for (int i = 0; i < pts.Count; i++)
        {
            var a = pts[i]; var b = pts[(i + 1) % pts.Count];
            tex.DrawBresenhamLine(Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y), Mathf.RoundToInt(b.x), Mathf.RoundToInt(b.y), col);
        }
    }

    public static void DrawTransformedPolygon(this Texture2D tex, IList<Vector2> pts, Mat3 m, Color fill, bool doFill)
    {
        var tpts = pts.TransformPoints(m);
        if (doFill) tex.DrawFilledPolygon(tpts, fill);
        tex.DrawPolygonOutline(tpts, Color.black);
    }
}

// -----------------------------------------------------------------------------
// Minimal demo for the lab test (hits all rubric items). Adapt colors/shapes to theme.
// -----------------------------------------------------------------------------
public class LabTestDemo : MonoBehaviour
{
    public Color background = new Color(0.95f, 0.98f, 1f);
    public Color accentA = new Color(1f, 0.9f, 0.5f);   // arcs/wedges
    public Color accentB = new Color(0.3f, 0.6f, 0.85f); // polygon fill
    public Color accentC = new Color(0.2f, 0.5f, 0.2f);  // ring segment

    Texture2D tex;
    Renderer quadRend;
    Color[] bg;

    void Start()
    {
        // Setup render surface
        tex = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.localScale = new Vector3(12.8f, 7.2f, 1);
        quadRend = quad.GetComponent<Renderer>();
        quadRend.material = new Material(Shader.Find("Unlit/Texture"));
        quadRend.material.mainTexture = tex;

        // Pre-draw large fills ONCE (performance)
        for (int y = 0; y < tex.height; y++) tex.DrawHLine(y, 0, tex.width - 1, background);
        // Big rounded backdrop
        tex.DrawFilledRoundedRect(new Rect(60, 60, 420, 260), 30, new Color(0.93f, 0.96f, 1f));
        // Big wedge in background (curved + straight edges)
        tex.DrawFilledPieWedge(280, 220, 130, 210, 330, new Color(1f, 0.95f, 0.85f));

        tex.Apply();
        bg = tex.GetPixels();
    }

    void Update()
    {
        // Restore background each frame to avoid redrawing big fills
        if (bg != null && bg.Length == tex.width * tex.height) { tex.SetPixels(bg); }

        // 1) LINE DRAWING (~10 lines): complex outline (polyline closed manually)
        var outline = new List<Vector2> {
            new Vector2(120,480), new Vector2(200,520), new Vector2(280,500), new Vector2(340,450), new Vector2(320,380),
            new Vector2(260,340), new Vector2(200,360), new Vector2(160,400), new Vector2(140,440), new Vector2(120,480)
        };
        for (int i = 0; i + 1 < outline.Count; i++)
        {
            var a = outline[i]; var b = outline[i + 1];
            tex.DrawDDALine(Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y), Mathf.RoundToInt(b.x), Mathf.RoundToInt(b.y), Color.black);
        }

        // 2) ARC / CURVE (trig)
        tex.DrawArcTrig(520, 520, 90, 30, 300, accentA);

        // 3) FILLED STRAIGHT-EDGE SHAPE (scanline triangle)
        tex.DrawFilledTriangle(new Vector2(540,180), new Vector2(660,180), new Vector2(600,320), accentB);
        tex.DrawBresenhamLine(540,180,660,180, Color.black);
        tex.DrawBresenhamLine(660,180,600,320, Color.black);
        tex.DrawBresenhamLine(600,320,540,180, Color.black);

        // 4) FILLED CURVED-EDGE SHAPE (non-circle): ring segment + wedge
        tex.DrawFilledRingSegment(1040, 520, 36, 62, 220, 320, accentC);
        tex.DrawFilledPieWedge(1040, 520, 40, 30, 140, new Color(1f,1f,1f,0.35f));

        // 5-7) TRANSFORMS: TRS composition on a moving polygon (anim story)
        float t = (Mathf.Sin(Time.time * 0.9f) * 0.5f + 0.5f); // 0..1 loop
        var kite = new List<Vector2> { new Vector2(-20,0), new Vector2(0,40), new Vector2(20,0), new Vector2(0,-60) };
        var M = Texture2DExtensions.TRS(new Vector2(Mathf.Lerp(100, 1180, t), 520), Mathf.Lerp(-20, 20, Mathf.Sin(Time.time)), new Vector2(1,1));
        tex.DrawTransformedPolygon(kite, M, new Color(0.95f,0.95f,1f), doFill:true);

        tex.Apply();
    }
}
