using System;
using UnityEngine;
using UnityEngine.Rendering;

public class Tutorial3 : MonoBehaviour
{
    // Canvas settings
    private Texture2D tex;
    private Color[] bg;
    private const int W = 1280, H = 720;

    // Drawing utilities
    private struct Paint
    {
        public Texture2D T;

        public void P(int x, int y, Color c)
        {
            if ((uint)x < W && (uint)y < H) T.SetPixel(x, y, c);
        }

        public void Line(int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                P(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        public void RectFill(int x, int y, int w, int h, Color c)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    if ((uint)xx < W && (uint)yy < H) T.SetPixel(xx, yy, c);
        }

        public void RectStroke(int x, int y, int w, int h, Color c)
        {
            Line(x, y, x + w, y, c);
            Line(x + w, y, x + w, y + h, c);
            Line(x + w, y + h, x, y + h, c);
            Line(x, y + h, x, y, c);
        }

        public void EllipseFill(int cx, int cy, int rx, int ry, Color c)
        {
            if (rx <= 0 || ry <= 0) return;
            for (int y = -ry; y <= ry; y++)
            {
                float t = 1f - (y * y) / (float)(ry * ry);
                int span = Mathf.RoundToInt(rx * Mathf.Sqrt(Mathf.Max(0f, t)));
                int yy = cy + y;
                for (int x = cx - span; x <= cx + span; x++)
                    if ((uint)x < W && (uint)yy < H) T.SetPixel(x, yy, c);
            }
        }

        public void Arc(Vector2 c, float r, float a0, float a1, int samples, Color col)
        {
            Vector2 prev = c + new Vector2(r * Mathf.Cos(a0), r * Mathf.Sin(a0));
            for (int i = 1; i <= samples; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)samples);
                Vector2 p = c + new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a));
                Line(Mathf.RoundToInt(prev.x), Mathf.RoundToInt(prev.y),
                     Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), col);
                prev = p;
            }
        }
    }

    private Paint G;

    // Scene colors
    private readonly Color sky = new Color(0.88f, 0.88f, 0.9f);
    private readonly Color steel = new Color(0.15f, 0.15f, 0.18f);
    private readonly Color craneBlue = new Color(0.10f, 0.45f, 0.80f);
    private readonly Color boomBlue = new Color(0.08f, 0.35f, 0.65f);
    private readonly Color counter = new Color(0.08f, 0.25f, 0.45f);
    private readonly Color ropeCol = new Color(0.25f, 0.25f, 0.28f);
    private readonly Color ballCol = new Color(0.12f, 0.12f, 0.12f);
    private readonly Color mortar = new Color(0.20f, 0.20f, 0.22f);
    private readonly Color brickCol = new Color(0.75f, 0.35f, 0.18f);
    private readonly Color pathCol = new Color(1f, 1f, 1f, 0.55f);

    // Crane parts
    private Rect tower;   // vertical support
    private Rect cab;     // operator cab
    private Rect cw;      // counterweight block
    private Vector2 apex; // top point of the boom where rope attaches

    // Pendulum settings
    [Range(0.2f, 1.2f)] public float speed = 0.65f;
    [Range(10f, 40f)] public float amplitudeDeg = 26f;
    public float ropeLen = 220f;
    public int ballR = 36;

    // Wall settings
    private Rect wall = new Rect(900, 320, 240, 260);
    private const int cols = 6, rows = 6;
    private bool[,] broken = new bool[cols, rows];

    void Awake()
    {
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 360;
        var surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
        surface.transform.localScale = new Vector3(W, H, 1);

        tex = new Texture2D(W, H) { filterMode = FilterMode.Point };
        surface.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/Texture");
        surface.GetComponent<Renderer>().material.mainTexture = tex;

        bg = new Color[W * H];
        for (int i = 0; i < bg.Length; i++) bg[i] = sky;

        G = new Paint { T = tex };
    }

    void Start()
    {
        tower = new Rect(210, 260, 120, 260);
        cab = new Rect(330, 430, 120, 80);
        cw = new Rect(190, 430, 70, 60);
        apex = new Vector2(cab.xMax + 70f, cab.yMax + 35f);
    }

    void Update()
    {
        tex.SetPixels(bg);

        DrawTrussTower();
        DrawCabAndBoom();

        // Pendulum motion
        float t = Time.time;
        float a = amplitudeDeg * Mathf.Deg2Rad * Mathf.Sin(t * speed * Mathf.PI * 2f);
        Vector2 bob = new Vector2(
            apex.x + ropeLen * Mathf.Sin(a),
            apex.y - ropeLen * Mathf.Cos(a)
        );

        // Rope
        G.Line(Mathf.RoundToInt(apex.x), Mathf.RoundToInt(apex.y),
               Mathf.RoundToInt(bob.x), Mathf.RoundToInt(bob.y), ropeCol);

        // Path arc
        G.Arc(apex, ropeLen, -amplitudeDeg * Mathf.Deg2Rad, amplitudeDeg * Mathf.Deg2Rad, 80, pathCol);

        // Wall
        DrawStaggeredWall();

        // Collision check
        Rect ballAabb = new Rect(bob.x - ballR, bob.y - ballR, ballR * 2, ballR * 2);
        if (Overlap(ballAabb, wall)) Smash(bob);

        // Wrecking ball
        G.EllipseFill(Mathf.RoundToInt(bob.x), Mathf.RoundToInt(bob.y), ballR, ballR, ballCol);

        tex.Apply();
    }

    // Draws the vertical tower with a truss pattern
    private void DrawTrussTower()
    {
        G.RectFill((int)tower.x, (int)tower.y, (int)tower.width, (int)tower.height, craneBlue);
        G.RectStroke((int)tower.x, (int)tower.y, (int)tower.width, (int)tower.height, steel);

        int x0 = (int)tower.x + 6, x1 = (int)(tower.x + tower.width) - 6;
        int y0 = (int)tower.y + 6, y1 = (int)(tower.y + tower.height) - 6;
        int step = 26;

        for (int y = y0; y < y1; y += step)
        {
            G.Line(x0, y, x1, Mathf.Min(y + step, y1), steel);
            G.Line(x1, y, x0, Mathf.Min(y + step, y1), steel);
            G.Line(x0, y, x0, Mathf.Min(y + step, y1), steel);
            G.Line(x1, y, x1, Mathf.Min(y + step, y1), steel);
        }
    }

    // Draws the operator cab, counterweight, and boom
    private void DrawCabAndBoom()
    {
        G.RectFill((int)cw.x, (int)cw.y, (int)cw.width, (int)cw.height, counter);
        G.RectStroke((int)cw.x, (int)cw.y, (int)cw.width, (int)cw.height, steel);

        G.RectFill((int)cab.x, (int)cab.y, (int)cab.width, (int)cab.height, craneBlue);
        G.RectStroke((int)cab.x, (int)cab.y, (int)cab.width, (int)cab.height, steel);

        G.RectFill((int)cab.x + 12, (int)cab.y + 18, 32, 24, new Color(0.85f, 0.92f, 1f));
        G.RectStroke((int)cab.x + 12, (int)cab.y + 18, 32, 24, steel);

        Vector2 root = new Vector2(tower.xMax, tower.yMax);
        G.Line((int)root.x, (int)root.y, (int)apex.x, (int)apex.y, boomBlue);
        G.Line((int)root.x, (int)root.y - 6, (int)apex.x + 6, (int)apex.y - 6, boomBlue);

        Vector2 mid = Vector2.Lerp(root, apex, 0.55f);
        G.Line((int)(cab.center.x), (int)cab.yMax, (int)mid.x, (int)mid.y, steel);
    }

    // Draws a staggered brick wall
    private void DrawStaggeredWall()
    {
        int bw = Mathf.FloorToInt(wall.width / cols);
        int bh = Mathf.FloorToInt(wall.height / rows);

        for (int r = 0; r < rows; r++)
        {
            int offset = (r % 2 == 0) ? 0 : bw / 2;
            for (int c = -1; c <= cols; c++)
            {
                int x = (int)wall.xMin + c * bw + offset;
                int y = (int)wall.yMin + r * bh;

                if (x < wall.xMin || x + bw > wall.xMax) continue;

                int ci = Mathf.Clamp(c, 0, cols - 1);
                int ri = r;

                if (!broken[ci, ri])
                    G.RectFill(x + 1, y + 1, bw - 2, bh - 2, brickCol);

                G.RectStroke(x, y, bw, bh, mortar);
            }
        }
    }

    // Marks a brick as broken at the given position
    private void Smash(Vector2 p)
    {
        int bw = Mathf.FloorToInt(wall.width / cols);
        int bh = Mathf.FloorToInt(wall.height / rows);

        int ri = Mathf.Clamp(Mathf.FloorToInt((p.y - wall.yMin) / bh), 0, rows - 1);
        int offset = (ri % 2 == 0) ? 0 : bw / 2;
        int ci = Mathf.Clamp(Mathf.FloorToInt((p.x - wall.xMin - offset) / bw), 0, cols - 1);

        broken[ci, ri] = true;
    }

    // Checks rectangle overlap
    private static bool Overlap(Rect a, Rect b)
    {
        return a.xMin <= b.xMax && a.xMax >= b.xMin &&
               a.yMin <= b.yMax && a.yMax >= b.yMin;
    }
}









