using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ObstacleDodge
{
    /// <summary>
    /// GameUI.cs from the guide, grown up: the hit counter (as hearts), the Game Over window with
    /// "Watch ad" / "Play again", plus the main menu, pause and "level complete" windows.
    /// Everything is drawn in "virtual" pixels: the shorter side of the screen is always 720.
    /// </summary>
    public sealed class GameUI
    {
        readonly BitmapFont font;
        readonly UIDraw ui;
        readonly UIInput input;
        readonly IPlatform platform;

        public Action ToggleSound;
        public Action Quit;
        public Action<SoundKind> Click;

        static readonly Color Green = new Color(67, 160, 71);
        static readonly Color Blue = new Color(30, 136, 229);
        static readonly Color Gold = new Color(255, 179, 0);
        static readonly Color Gray = new Color(96, 110, 120);
        static readonly Color PanelColor = new Color(20, 28, 48) * 0.88f;

        public Vector2 Screen { get; private set; }
        public RectangleF Banner { get; private set; }

        public GameUI(BitmapFont font, UIDraw ui, UIInput input, IPlatform platform)
        {
            this.font = font;
            this.ui = ui;
            this.input = input;
            this.platform = platform;
        }

        public static float GetScale(int width, int height) => Math.Min(width, height) / 720f;

        public void Layout(int widthPx, int heightPx)
        {
            float scale = GetScale(widthPx, heightPx);
            Screen = new Vector2(widthPx / scale, heightPx / scale);
            float bh = platform.BannerHeightPx / scale, bw = platform.BannerWidthPx / scale;
            Banner = bh > 0 ? new RectangleF((Screen.X - bw) / 2f, Screen.Y - bh, bw, bh) : new RectangleF(0, Screen.Y, 0, 0);
        }

        /// <summary>Space for windows: the whole screen except the banner strip.</summary>
        float UsableHeight => Screen.Y - Banner.Height;

        // ------------------------------------------------------------------ HUD

        public void DrawHud(SpriteBatch b, GameManager gm, World world, bool showPause)
        {
            // hearts: lives left (the guide's "Hits: x / 5", but easier to read)
            float x = 24f, y = 22f, size = 46f;
            for (int i = 0; i < gm.MaxHits; i++)
            {
                bool alive = i < gm.LivesLeft;
                var center = new Vector2(x + size / 2f + i * (size + 8f), y + size / 2f);
                ui.Icon(b, ui.Heart, center + new Vector2(2, 3), size, Color.Black * 0.35f);
                ui.Icon(b, ui.Heart, center, size, alive ? new Color(244, 67, 54) : new Color(255, 255, 255) * 0.35f);
            }
            font.DrawShadowed(b, string.Format(Strings.Hits, gm.Hits, gm.MaxHits), new Vector2(26f, y + size + 6f), 26f, Color.White);

            // level and progress to the finish line
            string level = string.Format(Strings.Level, gm.Level);
            font.DrawCentered(b, level, new Vector2(Screen.X / 2f, 40f), 38f, Color.White);
            float barW = Math.Min(420f, Screen.X * 0.34f);
            var bar = new RectangleF(Screen.X / 2f - barW / 2f, 70f, barW, 16f);
            ui.Panel(b, bar.Inflate(3), Color.Black * 0.35f, 10f);
            ui.Panel(b, new RectangleF(bar.X, bar.Y, Math.Max(16f, bar.Width * world.Progress), bar.Height), new Color(255, 213, 79), 8f);
            ui.Icon(b, ui.Star, new Vector2(bar.Right + 22f, bar.Y + 8f), 34f, Color.White);

            // time and the pause button
            font.DrawShadowed(b, gm.RoundTime.ToString("0.0") + " s", new Vector2(Screen.X - 250f, 30f), 32f, Color.White);
            if (showPause)
            {
                var pause = PauseButtonRect;
                bool pressed = input.IsPressing(pause);
                ui.Panel(b, pause, (pressed ? Color.White * 0.5f : Color.Black * 0.35f), 18f);
                ui.Rect(b, new RectangleF(pause.X + 24f, pause.Y + 20f, 10f, pause.Height - 40f), Color.White);
                ui.Rect(b, new RectangleF(pause.Right - 34f, pause.Y + 20f, 10f, pause.Height - 40f), Color.White);
            }
        }

        RectangleF PauseButtonRect => new RectangleF(Screen.X - 100f, 18f, 76f, 76f);

        public bool PauseClicked() => input.Clicked(PauseButtonRect.Inflate(8));

        // ------------------------------------------------------------------ windows

        public void DrawMenu(SpriteBatch b, GameManager gm, float time)
        {
            float h = UsableHeight;
            ui.Rect(b, new RectangleF(0, 0, Screen.X, Screen.Y), Color.Black * 0.18f);

            float bounce = MathF.Sin(time * 2.2f) * 6f;
            font.DrawCentered(b, Strings.Title, new Vector2(Screen.X / 2f, h * 0.2f + bounce), 92f, new Color(255, 213, 79));
            font.DrawCentered(b, Strings.Subtitle, new Vector2(Screen.X / 2f, h * 0.2f + 74f), 32f, Color.White);

            // level picker: ◀ 3-DARAJA ▶
            float rowY = h * 0.47f;
            var prev = new RectangleF(Screen.X / 2f - 250f, rowY - 38f, 76f, 76f);
            var next = new RectangleF(Screen.X / 2f + 174f, rowY - 38f, 76f, 76f);
            bool canPrev = gm.Level > 1, canNext = gm.Level < gm.Save.Level;
            DrawIconButton(b, prev, -MathHelper.PiOver2, canPrev);
            DrawIconButton(b, next, MathHelper.PiOver2, canNext);
            font.DrawCentered(b, string.Format(Strings.Level, gm.Level), new Vector2(Screen.X / 2f, rowY), 50f, Color.White);
            if (canPrev && input.Clicked(prev.Inflate(6))) { gm.SelectLevel(gm.Level - 1); Click?.Invoke(SoundKind.Click); }
            if (canNext && input.Clicked(next.Inflate(6))) { gm.SelectLevel(gm.Level + 1); Click?.Invoke(SoundKind.Click); }

            var play = new RectangleF(Screen.X / 2f - 200f, h * 0.6f, 400f, 104f);
            if (Button(b, play, Strings.Play, Green, 52f)) { gm.StartGame(); return; }

            string best = gm.Save.BestLevel > 0 ? string.Format(Strings.Best, gm.Save.BestLevel) : Strings.NoBest;
            font.DrawCentered(b, best, new Vector2(Screen.X / 2f, h * 0.6f + 140f), 30f, Color.White);
            font.DrawCentered(b, platform.IsMobile ? Strings.HintMobile : Strings.HintDesktop, new Vector2(Screen.X / 2f, h * 0.6f + 182f), 24f, Color.White * 0.85f);

            var sound = new RectangleF(Screen.X - 330f, 20f, 310f, 60f);
            if (Button(b, sound, gm.Save.SoundOn ? Strings.SoundOn : Strings.SoundOff, Gray, 26f)) ToggleSound?.Invoke();
        }

        public void DrawPause(SpriteBatch b, GameManager gm)
        {
            var panel = Window(b, 3, Strings.Paused, out float y);
            if (Button(b, Row(panel, ref y), Strings.Resume, Green)) gm.Resume();
            else if (Button(b, Row(panel, ref y), Strings.RestartLevel, Blue)) gm.Restart();
            else if (Button(b, Row(panel, ref y), Strings.Menu, Gray)) gm.GoToMenu();
        }

        public void DrawGameOver(SpriteBatch b, GameManager gm, World world)
        {
            AdManager ads = AdManager.Instance;
            bool offerAd = gm.CanContinue && ads != null && ads.IsRewardedReady();
            int rows = offerAd ? 3 : 2;
            var panel = Window(b, rows, Strings.GameOver, out float y, string.Format(Strings.Distance, (int)(world.Progress * 100)));

            if (offerAd)
            {
                // the rewarded ad is only shown when the player asks for it (guide 1.4)
                if (Button(b, Row(panel, ref y), string.Format(Strings.WatchAd, gm.BonusLives), Gold, 36f, true))
                {
                    ads.ShowRewarded(gm.ContinueAfterReward);
                    return;
                }
            }
            if (Button(b, Row(panel, ref y), Strings.PlayAgain, Green)) gm.Restart();
            else if (Button(b, Row(panel, ref y), Strings.Menu, Gray)) gm.GoToMenu();
        }

        public void DrawLevelComplete(SpriteBatch b, GameManager gm)
        {
            var panel = Window(b, 2, Strings.LevelComplete, out float y, string.Format(Strings.Time, gm.RoundTime, gm.Hits), true);
            // stars: 3 = no hits, 2 = up to two hits, 1 = finished
            float sy = panel.Y + 196f;
            for (int i = 0; i < 3; i++)
            {
                var c = new Vector2(panel.Center.X + (i - 1) * 86f, sy - (i == 1 ? 10f : 0f));
                ui.Icon(b, ui.Star, c + new Vector2(3, 4), 78f, Color.Black * 0.4f);
                ui.Icon(b, ui.Star, c, 78f, i < gm.LastStars ? new Color(255, 202, 40) : Color.White * 0.25f);
            }
            if (gm.LastWasNewBest)
                font.DrawCentered(b, Strings.NewBest, new Vector2(panel.Center.X, panel.Y + 122f), 28f, new Color(255, 213, 79));

            if (Button(b, Row(panel, ref y), Strings.NextLevel, Green)) gm.NextLevel();
            else if (Button(b, Row(panel, ref y), Strings.Menu, Gray)) gm.GoToMenu();
        }

        public void DrawAdOverlay(SpriteBatch b)
        {
            ui.Rect(b, new RectangleF(0, 0, Screen.X, Screen.Y), Color.Black * 0.55f);
            font.DrawCentered(b, Strings.AdShowing, new Vector2(Screen.X / 2f, UsableHeight / 2f), 44f, Color.White);
        }

        // ------------------------------------------------------------------ building blocks

        const float RowHeight = 92f, RowGap = 18f, PanelWidth = 600f;
        /// <summary>Draws the dark window with a title and returns where the first button goes.</summary>
        RectangleF Window(SpriteBatch b, int rows, string title, out float firstRowY, string subtitle = null, bool stars = false)
        {
            float header = 120f + (subtitle != null ? 46f : 0f) + (stars ? 130f : 0f);
            float height = header + rows * (RowHeight + RowGap) + 18f;
            float width = Math.Min(PanelWidth, Screen.X - 40f);
            float top = Math.Max(14f, (UsableHeight - height) / 2f);
            var panel = new RectangleF((Screen.X - width) / 2f, top, width, height);

            ui.Rect(b, new RectangleF(0, 0, Screen.X, Screen.Y), Color.Black * 0.35f);
            ui.Panel(b, new RectangleF(panel.X + 6, panel.Y + 8, panel.Width, panel.Height), Color.Black * 0.3f, 30f);
            ui.Panel(b, panel, PanelColor, 30f);
            font.DrawCentered(b, title, new Vector2(panel.Center.X, panel.Y + 66f), 60f, Color.White);
            if (subtitle != null)
                font.DrawCentered(b, subtitle, new Vector2(panel.Center.X, panel.Y + (stars ? 270f : 124f)), 28f, Color.White * 0.9f);

            firstRowY = panel.Y + header;
            return panel;
        }

        static RectangleF Row(RectangleF panel, ref float y)
        {
            var r = new RectangleF(panel.X + 36f, y, panel.Width - 72f, RowHeight);
            y += RowHeight + RowGap;
            return r;
        }

        bool Button(SpriteBatch b, RectangleF r, string text, Color color, float textSize = 40f, bool adIcon = false)
        {
            bool pressed = input.IsPressing(r);
            var face = pressed ? new RectangleF(r.X, r.Y + 4, r.Width, r.Height) : r;
            ui.Panel(b, new RectangleF(r.X, r.Y + 7, r.Width, r.Height), Darken(color, 0.55f), 24f);
            ui.Panel(b, face, pressed ? Darken(color, 0.85f) : color, 24f);
            ui.Panel(b, new RectangleF(face.X + 6, face.Y + 5, face.Width - 12, face.Height * 0.42f), Color.White * 0.12f, 18f);
            if (adIcon)
            {
                // a small "play" triangle in a circle: this button shows a video
                var c = new Vector2(face.X + 52f, face.Center.Y);
                ui.Icon(b, ui.Circle, c, 52f, Color.White * 0.95f);
                ui.Icon(b, ui.Arrow, c + new Vector2(3, 0), 26f, color, MathHelper.PiOver2);
                font.DrawCentered(b, text, new Vector2(face.Center.X + 28f, face.Center.Y), textSize, Color.White);
            }
            else
            {
                font.DrawCentered(b, text, face.Center, textSize, Color.White);
            }
            bool clicked = input.Clicked(r);
            if (clicked) Click?.Invoke(SoundKind.Click);
            return clicked;
        }

        void DrawIconButton(SpriteBatch b, RectangleF r, float angle, bool enabled)
        {
            bool pressed = enabled && input.IsPressing(r);
            ui.Panel(b, r, enabled ? (pressed ? Blue * 0.7f : Blue) : Gray * 0.6f, 20f);
            ui.Icon(b, ui.Arrow, r.Center, r.Width * 0.45f, Color.White * (enabled ? 1f : 0.5f), angle);
        }

        static Color Darken(Color c, float k) => new Color((int)(c.R * k), (int)(c.G * k), (int)(c.B * k), c.A);
    }
}
