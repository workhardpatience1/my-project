using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ObstacleDodge
{
    /// <summary>
    /// TouchControls.cs from the guide (Part 4): arrow buttons on the screen for the phone.
    /// Mover reads TouchControls.Horizontal and TouchControls.Vertical (-1, 0 or 1).
    /// Layout: "left" and "right" under the left thumb, "up" and "down" under the right thumb,
    /// so both thumbs can be used at the same time (diagonal movement).
    /// </summary>
    public sealed class TouchControls
    {
        public static float Horizontal { get; private set; }
        public static float Vertical { get; private set; }

        public float ButtonSize = 128f;  // in virtual pixels
        public float Gap = 18f;
        public float Margin = 34f;
        public bool ShowOnPC = false;    // tick this to test the buttons with the mouse

        RectangleF upRect, downRect, leftRect, rightRect;
        bool upDown, downDown, leftDown, rightDown;

        public bool IsVisible(bool isMobile, GameManager gm)
        {
            if (!isMobile && !ShowOnPC) return false;
            return gm != null && gm.State == GameState.Playing;
        }

        /// <param name="banner">The banner ad area in virtual pixels (the buttons never go under it).</param>
        void BuildLayout(Vector2 screen, RectangleF banner)
        {
            float s = ButtonSize;
            float bottom = screen.Y - Margin;
            float leftWidth = 2 * s + Gap;
            // raise a group only if it would touch the banner
            float leftBottom = Overlaps(Margin, Margin + leftWidth, banner) ? banner.Y - Gap : bottom;
            float rightBottom = Overlaps(screen.X - Margin - s, screen.X - Margin, banner) ? banner.Y - Gap : bottom;

            leftRect = new RectangleF(Margin, leftBottom - s, s, s);
            rightRect = new RectangleF(Margin + s + Gap, leftBottom - s, s, s);
            downRect = new RectangleF(screen.X - Margin - s, rightBottom - s, s, s);
            upRect = new RectangleF(screen.X - Margin - s, rightBottom - 2 * s - Gap, s, s);
        }

        static bool Overlaps(float x0, float x1, RectangleF banner) =>
            banner.Height > 0 && x1 > banner.X && x0 < banner.Right;

        public void Update(UIInput input, bool visible, Vector2 screen, RectangleF banner)
        {
            upDown = downDown = leftDown = rightDown = false;
            if (!visible)
            {
                Horizontal = 0f;
                Vertical = 0f;
                return;
            }
            BuildLayout(screen, banner);
            // a little bigger than drawn, fingers are not precise
            upDown = input.IsHeld(upRect.Inflate(10));
            downDown = input.IsHeld(downRect.Inflate(10));
            leftDown = input.IsHeld(leftRect.Inflate(10));
            rightDown = input.IsHeld(rightRect.Inflate(10));
            Horizontal = (rightDown ? 1f : 0f) - (leftDown ? 1f : 0f);
            Vertical = (upDown ? 1f : 0f) - (downDown ? 1f : 0f);
        }

        public void Draw(SpriteBatch batch, UIDraw ui, bool visible)
        {
            if (!visible) return;
            DrawButton(batch, ui, upRect, 0f, upDown);
            DrawButton(batch, ui, rightRect, MathHelper.PiOver2, rightDown);
            DrawButton(batch, ui, downRect, MathHelper.Pi, downDown);
            DrawButton(batch, ui, leftRect, -MathHelper.PiOver2, leftDown);
        }

        static void DrawButton(SpriteBatch batch, UIDraw ui, RectangleF r, float angle, bool pressed)
        {
            // dark square, lighter while pressed
            ui.Panel(batch, r, pressed ? new Color(255, 255, 255) * 0.55f : new Color(0, 0, 0) * 0.38f, 26f);
            ui.Icon(batch, ui.Arrow, r.Center, r.Width * 0.5f, pressed ? new Color(0, 0, 0) * 0.85f : Color.White * 0.9f, angle);
        }
    }
}
