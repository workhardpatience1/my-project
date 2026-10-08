using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace ObstacleDodge
{
    /// <summary>
    /// Fingers and the mouse in "virtual" UI pixels. A button is clicked when a finger
    /// is lifted inside it AND the press also started inside it (so a finger that was holding
    /// an arrow does not press a Game Over button by accident).
    /// </summary>
    public sealed class UIInput
    {
        public struct Pointer
        {
            public int Id;
            public Vector2 Position;
            public Vector2 Start;
            public bool Down;
            public bool Released;
        }

        readonly List<Pointer> pointers = new List<Pointer>();
        readonly Dictionary<int, Vector2> starts = new Dictionary<int, Vector2>();
        bool mouseWasDown;
        bool consumed;

        public IReadOnlyList<Pointer> Pointers => pointers;

        public void Update(float scale, bool useMouse, bool windowActive)
        {
            pointers.Clear();
            consumed = false;

            foreach (var t in TouchPanel.GetState())
            {
                Vector2 pos = t.Position / scale;
                switch (t.State)
                {
                    case TouchLocationState.Pressed:
                        starts[t.Id] = pos;
                        pointers.Add(new Pointer { Id = t.Id, Position = pos, Start = pos, Down = true });
                        break;
                    case TouchLocationState.Moved:
                        pointers.Add(new Pointer { Id = t.Id, Position = pos, Start = starts.TryGetValue(t.Id, out var s) ? s : pos, Down = true });
                        break;
                    case TouchLocationState.Released:
                        pointers.Add(new Pointer { Id = t.Id, Position = pos, Start = starts.TryGetValue(t.Id, out var s2) ? s2 : pos, Released = true });
                        starts.Remove(t.Id);
                        break;
                }
            }

            if (useMouse && windowActive)
            {
                var m = Mouse.GetState();
                Vector2 pos = new Vector2(m.X, m.Y) / scale;
                bool down = m.LeftButton == ButtonState.Pressed;
                if (down && !mouseWasDown) starts[-1] = pos;
                if (down)
                    pointers.Add(new Pointer { Id = -1, Position = pos, Start = starts.TryGetValue(-1, out var s) ? s : pos, Down = true });
                else if (mouseWasDown)
                    pointers.Add(new Pointer { Id = -1, Position = pos, Start = starts.TryGetValue(-1, out var s) ? s : pos, Released = true });
                mouseWasDown = down;
            }
        }

        /// <summary>True once when a button is tapped. After one click, other buttons ignore this frame.</summary>
        public bool Clicked(RectangleF r)
        {
            if (consumed) return false;
            foreach (var p in pointers)
            {
                if (p.Released && r.Contains(p.Position) && r.Contains(p.Start))
                {
                    consumed = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Is a finger pressing this button right now (for the "pressed" look)?</summary>
        public bool IsPressing(RectangleF r)
        {
            foreach (var p in pointers)
                if (p.Down && r.Contains(p.Position) && r.Contains(p.Start)) return true;
            return false;
        }

        /// <summary>Is any finger on this area (used by the arrow buttons, which work while held).</summary>
        public bool IsHeld(RectangleF r)
        {
            foreach (var p in pointers)
                if (p.Down && r.Contains(p.Position)) return true;
            return false;
        }
    }
}
