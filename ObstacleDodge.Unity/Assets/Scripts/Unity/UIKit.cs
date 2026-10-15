using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObstacleDodge
{
    /// <summary>
    /// Drawing helpers for the Unity UI (IMGUI): rounded panels, icons made in code and text in the
    /// same font as the phone version (DejaVu Sans Bold). All sizes are "virtual" pixels: the shorter
    /// side of the screen is 720, exactly like the phone version's UI.
    /// </summary>
    public sealed class UIKit
    {
        public readonly Texture2D Heart, Star, Arrow, Circle;
        readonly Font font;
        readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
        readonly GUIContent content = new GUIContent();

        // The phone version's "size" is the line height; Unity's fontSize is the em size.
        const float LineToEm = 1f / 1.164f;

        public UIKit()
        {
            font = Resources.Load<Font>("Fonts/DejaVuSans-Bold");
            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (font == null) try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            Circle = MakeShape(64, (x, y) => Mathf.Sqrt((x - 32) * (x - 32) + (y - 32) * (y - 32)) - 31f);
            Heart = MakeShape(64, HeartDistance);
            Star = MakeShape(64, StarDistance);
            Arrow = MakeShape(64, ArrowDistance);
        }

        public static Color C(int r, int g, int b, float a = 1f) => new Color(r / 255f, g / 255f, b / 255f, a);

        // ---------------------------------------------------------------- shapes

        public void Rect(Rect r, Color c)
        {
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        }

        public void Panel(Rect r, Color c, float radius)
        {
            if (r.width <= 0 || r.height <= 0) return;
            float k = Mathf.Min(radius, Mathf.Min(r.width, r.height) / 2f);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, k);
        }

        public void Icon(Texture2D tex, Vector2 center, float size, Color c, float rotationRadians = 0f)
        {
            Matrix4x4 saved = GUI.matrix;
            if (rotationRadians != 0f) GUIUtility.RotateAroundPivot(rotationRadians * Mathf.Rad2Deg, center);
            GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), tex,
                ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
            GUI.matrix = saved;
        }

        // ---------------------------------------------------------------- text

        GUIStyle Style(float size)
        {
            int px = Mathf.Max(1, Mathf.RoundToInt(size * LineToEm));
            if (!styles.TryGetValue(px, out var style))
            {
                style = new GUIStyle
                {
                    font = font,
                    fontSize = px,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false,
                    clipping = TextClipping.Overflow,
                    richText = false,
                };
                style.normal.textColor = Color.white;
                styles[px] = style;
            }
            return style;
        }

        public Vector2 Measure(string text, float size)
        {
            content.text = text;
            return Style(size).CalcSize(content);
        }

        public void Text(string text, Vector2 topLeft, float size, Color color)
        {
            var style = Style(size);
            content.text = text;
            Vector2 s = style.CalcSize(content);
            style.normal.textColor = color;
            GUI.Label(new Rect(topLeft.x, topLeft.y, s.x + 4f, s.y), content, style);
        }

        /// <summary>Text with a soft dark shadow, so it is readable over the 3D scene.</summary>
        public void TextShadowed(string text, Vector2 topLeft, float size, Color color, float shadow = 0.06f)
        {
            float o = Mathf.Max(2f, size * shadow);
            Text(text, topLeft + new Vector2(o * 0.6f, o), size, new Color(0f, 0f, 0f, 0.6f * color.a));
            Text(text, topLeft, size, color);
        }

        public void TextCentered(string text, Vector2 center, float size, Color color, bool shadow = true)
        {
            Vector2 m = Measure(text, size);
            var pos = new Vector2(Mathf.Round(center.x - m.x / 2f), Mathf.Round(center.y - m.y / 2f));
            if (shadow) TextShadowed(text, pos, size, color);
            else Text(text, pos, size, color);
        }

        // ---------------------------------------------------------------- textures made in code

        static Texture2D MakeShape(int size, Func<float, float, float> distance)
        {
            // 4 x 4 samples per pixel give smooth edges; row 0 of a Unity texture is the BOTTOM
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                            if (distance(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f) < 0f) inside++;
                    byte a = (byte)(inside * 255 / 16);
                    pixels[(size - 1 - y) * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        static float HeartDistance(float x, float y)
        {
            float u = (x - 32f) / 26f, v = (34f - y) / 26f;
            float a = u * u + v * v - 1f;
            return a * a * a - u * u * v * v * v; // < 0 inside
        }

        static float StarDistance(float x, float y)
        {
            const float cx = 32f, cy = 34f, outer = 31f, inner = 12.5f;
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                float ai = -Mathf.PI / 2f + i * Mathf.PI / 5f, aj = -Mathf.PI / 2f + j * Mathf.PI / 5f;
                float ri = i % 2 == 0 ? outer : inner, rj = j % 2 == 0 ? outer : inner;
                float xi = cx + Mathf.Cos(ai) * ri, yi = cy + Mathf.Sin(ai) * ri;
                float xj = cx + Mathf.Cos(aj) * rj, yj = cy + Mathf.Sin(aj) * rj;
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside ? -1f : 1f;
        }

        static float ArrowDistance(float x, float y)
        {
            float top = 8f, bottom = 54f;
            if (y < top || y > bottom) return 1.5f;
            float half = (y - top) / (bottom - top) * 26f;
            return Mathf.Abs(x - 32f) - half;
        }
    }
}
