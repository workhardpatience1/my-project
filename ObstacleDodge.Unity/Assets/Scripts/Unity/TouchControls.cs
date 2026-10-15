using UnityEngine;

namespace ObstacleDodge
{
    /// <summary>
    /// The on-screen arrows of the phone version (ads guide, Part 4): "left/right" under the left thumb,
    /// "up/down" under the right thumb, several fingers at once. Never under the banner.
    /// </summary>
    public sealed class TouchControls
    {
        public static float Horizontal { get; private set; }
        public static float Vertical { get; private set; }

        public float ButtonSize = 128f;
        public float Gap = 18f;
        public float Margin = 34f;
        public bool ShowOnPC = false; // tick to test the arrows with the mouse in the editor

        Rect upRect, downRect, leftRect, rightRect;
        bool upDown, downDown, leftDown, rightDown;

        public bool IsVisible(bool isMobile, GameManager gm)
        {
            if (!isMobile && !ShowOnPC) return false;
            return gm != null && gm.State == GameState.Playing;
        }

        void BuildLayout(Vector2 screen, Rect banner)
        {
            float s = ButtonSize;
            float bottom = screen.y - Margin;
            float leftWidth = 2 * s + Gap;
            float leftBottom = Overlaps(Margin, Margin + leftWidth, banner) ? banner.y - Gap : bottom;
            float rightBottom = Overlaps(screen.x - Margin - s, screen.x - Margin, banner) ? banner.y - Gap : bottom;

            leftRect = new Rect(Margin, leftBottom - s, s, s);
            rightRect = new Rect(Margin + s + Gap, leftBottom - s, s, s);
            downRect = new Rect(screen.x - Margin - s, rightBottom - s, s, s);
            upRect = new Rect(screen.x - Margin - s, rightBottom - 2 * s - Gap, s, s);
        }

        static bool Overlaps(float x0, float x1, Rect banner) => banner.height > 0 && x1 > banner.x && x0 < banner.xMax;

        static Rect Inflate(Rect r, float d) => new Rect(r.x - d, r.y - d, r.width + 2 * d, r.height + 2 * d);

        public void Update(bool visible, Vector2 screen, Rect banner, float scale)
        {
            upDown = downDown = leftDown = rightDown = false;
            if (!visible)
            {
                Horizontal = 0f;
                Vertical = 0f;
                return;
            }
            BuildLayout(screen, banner);
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                Check(t.position, scale);
            }
            if (Input.touchCount == 0 && Input.GetMouseButton(0)) Check(Input.mousePosition, scale);
            Horizontal = (rightDown ? 1f : 0f) - (leftDown ? 1f : 0f);
            Vertical = (upDown ? 1f : 0f) - (downDown ? 1f : 0f);
        }

        void Check(Vector2 screenPos, float scale)
        {
            // touch (0,0) is bottom left, GUI (0,0) is top left
            var p = new Vector2(screenPos.x / scale, (Screen.height - screenPos.y) / scale);
            if (Inflate(upRect, 10).Contains(p)) upDown = true;
            if (Inflate(downRect, 10).Contains(p)) downDown = true;
            if (Inflate(leftRect, 10).Contains(p)) leftDown = true;
            if (Inflate(rightRect, 10).Contains(p)) rightDown = true;
        }

        public void Draw(UIKit ui, bool visible)
        {
            if (!visible) return;
            DrawButton(ui, upRect, 0f, upDown);
            DrawButton(ui, rightRect, Mathf.PI / 2f, rightDown);
            DrawButton(ui, downRect, Mathf.PI, downDown);
            DrawButton(ui, leftRect, -Mathf.PI / 2f, leftDown);
        }

        static void DrawButton(UIKit ui, Rect r, float angle, bool pressed)
        {
            ui.Panel(r, pressed ? new Color(1, 1, 1, 0.55f) : new Color(0, 0, 0, 0.38f), 26f);
            ui.Icon(ui.Arrow, r.center, r.width * 0.5f, pressed ? new Color(0, 0, 0, 0.85f) : new Color(1, 1, 1, 0.9f), angle);
        }
    }
}
