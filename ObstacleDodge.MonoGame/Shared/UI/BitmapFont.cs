using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ObstacleDodge
{
    /// <summary>
    /// Text drawing from a pre-made atlas (Content/font.png + font.txt, made by tools/make_font_atlas.py).
    /// This needs no MonoGame content pipeline and works the same on the phone and the computer.
    /// </summary>
    public sealed class BitmapFont
    {
        struct Glyph
        {
            public Rectangle Source;
            public float OffsetX, OffsetY, Advance;
        }

        readonly Texture2D texture;
        readonly Dictionary<char, Glyph> glyphs = new Dictionary<char, Glyph>();
        public float LineHeight { get; }
        public float BaseSize { get; }

        BitmapFont(Texture2D texture, string metrics)
        {
            this.texture = texture;
            using var reader = new StringReader(metrics);
            string header = reader.ReadLine();
            var h = header.Split(' ');
            LineHeight = float.Parse(h[1], CultureInfo.InvariantCulture);
            BaseSize = LineHeight;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var p = line.Split(' ');
                if (p.Length < 8) continue;
                char c = (char)int.Parse(p[0], CultureInfo.InvariantCulture);
                glyphs[c] = new Glyph
                {
                    Source = new Rectangle(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4])),
                    OffsetX = float.Parse(p[5], CultureInfo.InvariantCulture),
                    OffsetY = float.Parse(p[6], CultureInfo.InvariantCulture),
                    Advance = float.Parse(p[7], CultureInfo.InvariantCulture),
                };
            }
        }

        public static BitmapFont LoadEmbedded(GraphicsDevice device)
        {
            var assembly = typeof(BitmapFont).GetTypeInfo().Assembly;
            using Stream png = assembly.GetManifestResourceStream("ObstacleDodge.font.png")
                ?? throw new InvalidOperationException("font.png resource is missing");
            using Stream txt = assembly.GetManifestResourceStream("ObstacleDodge.font.txt")
                ?? throw new InvalidOperationException("font.txt resource is missing");
            var texture = Texture2D.FromStream(device, png, DefaultColorProcessors.PremultiplyAlpha);
            using var reader = new StreamReader(txt);
            return new BitmapFont(texture, reader.ReadToEnd());
        }

        /// <summary>Size of the text drawn at the given pixel size.</summary>
        public Vector2 Measure(string text, float size)
        {
            float scale = size / BaseSize;
            float width = 0, lineWidth = 0;
            int lines = 1;
            foreach (char ch in text)
            {
                if (ch == '\n') { width = MathF.Max(width, lineWidth); lineWidth = 0; lines++; continue; }
                if (glyphs.TryGetValue(ch, out var g) || glyphs.TryGetValue('?', out g)) lineWidth += g.Advance;
            }
            width = MathF.Max(width, lineWidth);
            return new Vector2(width * scale, lines * LineHeight * scale);
        }

        public void Draw(SpriteBatch batch, string text, Vector2 position, float size, Color color)
        {
            float scale = size / BaseSize;
            float x = position.X, y = position.Y;
            foreach (char ch in text)
            {
                if (ch == '\n') { x = position.X; y += LineHeight * scale; continue; }
                if (!glyphs.TryGetValue(ch, out var g) && !glyphs.TryGetValue('?', out g)) continue;
                if (g.Source.Width > 1 && ch != ' ')
                {
                    batch.Draw(texture, new Vector2(x + g.OffsetX * scale, y + g.OffsetY * scale), g.Source, color,
                        0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                x += g.Advance * scale;
            }
        }

        /// <summary>Text with a soft dark shadow, so it is readable over the 3D scene.</summary>
        public void DrawShadowed(SpriteBatch batch, string text, Vector2 position, float size, Color color, float shadow = 0.06f)
        {
            float o = MathF.Max(2f, size * shadow);
            Draw(batch, text, position + new Vector2(o * 0.6f, o), size, new Color(0, 0, 0, 150) * (color.A / 255f));
            Draw(batch, text, position, size, color);
        }

        public void DrawCentered(SpriteBatch batch, string text, Vector2 center, float size, Color color, bool shadow = true)
        {
            Vector2 m = Measure(text, size);
            var pos = new Vector2(MathF.Round(center.X - m.X / 2f), MathF.Round(center.Y - m.Y / 2f));
            if (shadow) DrawShadowed(batch, text, pos, size, color);
            else Draw(batch, text, pos, size, color);
        }
    }
}
