using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ObstacleDodge
{
    /// <summary>Small textures made in code (no picture files) and helpers to draw panels and buttons.</summary>
    public sealed class UIDraw
    {
        public readonly Texture2D Pixel;
        public readonly Texture2D Rounded;   // 64x64, radius 22, for 9-slice panels and buttons
        public readonly Texture2D Circle;
        public readonly Texture2D Heart;
        public readonly Texture2D Star;
        public readonly Texture2D Arrow;     // a triangle pointing up (MakeArrowTexture from the guide)
        public readonly Texture2D SkyGradient;
        const int RoundedSize = 64, Radius = 22;

        public UIDraw(GraphicsDevice device)
        {
            Pixel = new Texture2D(device, 1, 1);
            Pixel.SetData(new[] { Color.White });
            Rounded = MakeShape(device, RoundedSize, (x, y) => RoundedRectDistance(x, y, RoundedSize, Radius));
            Circle = MakeShape(device, 64, (x, y) => MathF.Sqrt((x - 32) * (x - 32) + (y - 32) * (y - 32)) - 31f);
            Heart = MakeShape(device, 64, HeartDistance);
            Star = MakeShape(device, 64, StarDistance);
            Arrow = MakeShape(device, 64, ArrowDistance);
            SkyGradient = new Texture2D(device, 1, 256);
            var sky = new Color[256];
            for (int i = 0; i < 256; i++) sky[i] = Color.White * (1f - i / 255f); // opaque at the top, clear at the bottom
            SkyGradient.SetData(sky);
        }

        // ---- shape textures, anti-aliased with a signed distance ----

        static Texture2D MakeShape(GraphicsDevice device, int size, Func<float, float, float> distance)
        {
            // 4 x 4 samples per pixel give smooth (anti-aliased) edges
            var tex = new Texture2D(device, size, size);
            var data = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                            if (distance(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f) < 0f) inside++;
                    float a = inside / 16f;
                    data[y * size + x] = new Color(a, a, a, a); // premultiplied white
                }
            tex.SetData(data);
            return tex;
        }

        static float RoundedRectDistance(float x, float y, int size, int r)
        {
            float cx = MathHelper.Clamp(x, r, size - r);
            float cy = MathHelper.Clamp(y, r, size - r);
            return MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r + 0.5f;
        }

        static float HeartDistance(float x, float y)
        {
            // classic implicit heart, sampled and turned into a rough distance
            float u = (x - 32f) / 26f, v = (34f - y) / 26f;
            float a = u * u + v * v - 1f;
            return a * a * a - u * u * v * v * v; // < 0 inside
        }

        static float StarDistance(float x, float y)
        {
            // a five-pointed star as a 10-corner polygon; returns -1 inside, 1 outside
            const float cx = 32f, cy = 34f, outer = 31f, inner = 12.5f;
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                float ai = -MathF.PI / 2f + i * MathF.PI / 5f, aj = -MathF.PI / 2f + j * MathF.PI / 5f;
                float ri = i % 2 == 0 ? outer : inner, rj = j % 2 == 0 ? outer : inner;
                float xi = cx + MathF.Cos(ai) * ri, yi = cy + MathF.Sin(ai) * ri;
                float xj = cx + MathF.Cos(aj) * rj, yj = cy + MathF.Sin(aj) * rj;
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside ? -1f : 1f;
        }

        static float ArrowDistance(float x, float y)
        {
            // triangle: narrow at the top, wide at the bottom
            float top = 8f, bottom = 54f;
            if (y < top || y > bottom) return 1.5f;
            float half = (y - top) / (bottom - top) * 26f;
            return MathF.Abs(x - 32f) - half;
        }

        // ---- drawing helpers ----

        public void Rect(SpriteBatch b, RectangleF r, Color c) =>
            b.Draw(Pixel, new Rectangle((int)r.X, (int)r.Y, (int)MathF.Ceiling(r.Width), (int)MathF.Ceiling(r.Height)), c);

        /// <summary>A rounded rectangle drawn as 9 pieces, so the corners stay round at any size.</summary>
        public void Panel(SpriteBatch b, RectangleF r, Color c, float radius = 22f)
        {
            float k = MathF.Min(radius, MathF.Min(r.Width, r.Height) / 2f);
            int s = Radius; // source corner size
            int m = RoundedSize - 2 * s;
            float x0 = r.X, x1 = r.X + k, x2 = r.Right - k;
            float y0 = r.Y, y1 = r.Y + k, y2 = r.Bottom - k;
            float midW = MathF.Max(0f, x2 - x1), midH = MathF.Max(0f, y2 - y1);
            Piece(b, new Rectangle(0, 0, s, s), x0, y0, k, k, c);
            Piece(b, new Rectangle(s, 0, m, s), x1, y0, midW, k, c);
            Piece(b, new Rectangle(s + m, 0, s, s), x2, y0, k, k, c);
            Piece(b, new Rectangle(0, s, s, m), x0, y1, k, midH, c);
            Piece(b, new Rectangle(s, s, m, m), x1, y1, midW, midH, c);
            Piece(b, new Rectangle(s + m, s, s, m), x2, y1, k, midH, c);
            Piece(b, new Rectangle(0, s + m, s, s), x0, y2, k, k, c);
            Piece(b, new Rectangle(s, s + m, m, s), x1, y2, midW, k, c);
            Piece(b, new Rectangle(s + m, s + m, s, s), x2, y2, k, k, c);
        }

        void Piece(SpriteBatch b, Rectangle src, float x, float y, float w, float h, Color c)
        {
            if (w <= 0 || h <= 0) return;
            b.Draw(Rounded, new Vector2(x, y), src, c, 0f, Vector2.Zero, new Vector2(w / src.Width, h / src.Height), SpriteEffects.None, 0f);
        }

        public void Icon(SpriteBatch b, Texture2D tex, Vector2 center, float size, Color c, float rotation = 0f)
        {
            b.Draw(tex, center, null, c, rotation, new Vector2(tex.Width / 2f, tex.Height / 2f), size / tex.Width, SpriteEffects.None, 0f);
        }
    }

    /// <summary>A rectangle with float coordinates (the UI works in "virtual" pixels).</summary>
    public struct RectangleF
    {
        public float X, Y, Width, Height;
        public RectangleF(float x, float y, float w, float h) { X = x; Y = y; Width = w; Height = h; }
        public float Right => X + Width;
        public float Bottom => Y + Height;
        public Vector2 Center => new Vector2(X + Width / 2f, Y + Height / 2f);
        public bool Contains(Vector2 p) => p.X >= X && p.X <= Right && p.Y >= Y && p.Y <= Bottom;
        public RectangleF Inflate(float d) => new RectangleF(X - d, Y - d, Width + 2 * d, Height + 2 * d);
    }
}
