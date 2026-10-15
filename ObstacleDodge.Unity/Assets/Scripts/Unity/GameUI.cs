using System;
using UnityEngine;

namespace ObstacleDodge
{
    /// <summary>
    /// The phone version's GameUI (Shared/UI/GameUI.cs) drawn with Unity's IMGUI: hearts, level and
    /// progress bar, timer, pause button, main menu, pause / Game Over / level complete windows,
    /// notices and the "ad is showing" overlay. Same layout numbers, colors and texts.
    /// Call the Draw... methods from OnGUI after <see cref="Begin"/>.
    /// </summary>
    public sealed class GameUI
    {
        readonly UIKit ui;
        readonly IPlatform platform;

        public Action ToggleSound;
        public Action<SoundKind> Click;

        static readonly Color Green = UIKit.C(67, 160, 71);
        static readonly Color Blue = UIKit.C(30, 136, 229);
        static readonly Color Gold = UIKit.C(255, 179, 0);
        static readonly Color Gray = UIKit.C(96, 110, 120);
        static readonly Color PanelColor = UIKit.C(20, 28, 48, 0.88f);
        static readonly Color Yellow = UIKit.C(255, 213, 79);

        public Vector2 Screen { get; private set; }
        public Rect Banner { get; private set; }
        public float Scale { get; private set; } = 1f;

        public GameUI(UIKit ui, IPlatform platform)
        {
            this.ui = ui;
            this.platform = platform;
        }

        public static float GetScale(int width, int height) => Math.Min(width, height) / 720f;

        public void Layout(int widthPx, int heightPx)
        {
            Scale = GetScale(widthPx, heightPx);
            Screen = new Vector2(widthPx / Scale, heightPx / Scale);
            float bh = platform.BannerHeightPx / Scale, bw = platform.BannerWidthPx / Scale;
            Banner = bh > 0 ? new Rect((Screen.x - bw) / 2f, Screen.y - bh, bw, bh) : new Rect(0, Screen.y, 0, 0);
        }

        /// <summary>Sets the virtual-pixel matrix. Call first in OnGUI.</summary>
        public void Begin()
        {
            Layout(UnityEngine.Screen.width, UnityEngine.Screen.height);
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
            GUI.color = Color.white;
        }

        float UsableHeight => Screen.y - Banner.height;

        // ------------------------------------------------------------------ input helpers

        static bool PointerDown => Input.GetMouseButton(0) || Input.touchCount > 0;

        static bool IsPressing(Rect r) => PointerDown && r.Contains(Event.current.mousePosition);

        static bool Clicked(Rect r) => GUI.Button(r, GUIContent.none, GUIStyle.none);

        static Rect Inflate(Rect r, float d) => new Rect(r.x - d, r.y - d, r.width + 2 * d, r.height + 2 * d);

        // ------------------------------------------------------------------ HUD

        public void DrawHud(GameManager gm, World world, bool showPause)
        {
            float x = 24f, y = 22f, size = 46f;
            for (int i = 0; i < gm.MaxHits; i++)
            {
                bool alive = i < gm.LivesLeft;
                var center = new Vector2(x + size / 2f + i * (size + 8f), y + size / 2f);
                ui.Icon(ui.Heart, center + new Vector2(2, 3), size, new Color(0, 0, 0, 0.35f));
                ui.Icon(ui.Heart, center, size, alive ? UIKit.C(244, 67, 54) : new Color(1, 1, 1, 0.35f));
            }
            ui.TextShadowed(string.Format(Strings.Hits, gm.Hits, gm.MaxHits), new Vector2(26f, y + size + 6f), 26f, Color.white);

            float top = Screen.x < 1000f ? 128f : 0f;
            ui.TextCentered(string.Format(Strings.Level, gm.Level), new Vector2(Screen.x / 2f, 40f + top), 38f, Color.white);
            float barW = Math.Min(420f, Screen.x * (top > 0 ? 0.6f : 0.34f));
            var bar = new Rect(Screen.x / 2f - barW / 2f, 70f + top, barW, 16f);
            ui.Panel(Inflate(bar, 3), new Color(0, 0, 0, 0.35f), 10f);
            ui.Panel(new Rect(bar.x, bar.y, Math.Max(16f, bar.width * world.Progress), bar.height), Yellow, 8f);
            ui.Icon(ui.Star, new Vector2(bar.xMax + 22f, bar.y + 8f), 34f, Color.white);

            ui.TextShadowed(gm.RoundTime.ToString("0.0") + " s", new Vector2(Screen.x - 250f, 30f), 32f, Color.white);
            if (showPause)
            {
                var pause = PauseButtonRect;
                bool pressed = IsPressing(pause);
                ui.Panel(pause, pressed ? new Color(1, 1, 1, 0.5f) : new Color(0, 0, 0, 0.35f), 18f);
                ui.Rect(new Rect(pause.x + 24f, pause.y + 20f, 10f, pause.height - 40f), Color.white);
                ui.Rect(new Rect(pause.xMax - 34f, pause.y + 20f, 10f, pause.height - 40f), Color.white);
            }
        }

        Rect PauseButtonRect => new Rect(Screen.x - 100f, 18f, 76f, 76f);

        public bool PauseClicked()
        {
            bool clicked = Clicked(Inflate(PauseButtonRect, 8));
            if (clicked) Click?.Invoke(SoundKind.Click);
            return clicked;
        }

        // ------------------------------------------------------------------ windows

        public void DrawMenu(GameManager gm, float time)
        {
            float h = UsableHeight;
            ui.Rect(new Rect(0, 0, Screen.x, Screen.y), new Color(0, 0, 0, 0.18f));

            float bounce = Mathf.Sin(time * 2.2f) * 6f;
            float titleSize = Mathf.Min(92f, 92f * (Screen.x - 60f) / Mathf.Max(1f, ui.Measure(Strings.Title, 92f).x));
            ui.TextCentered(Strings.Title, new Vector2(Screen.x / 2f, h * 0.2f + bounce), titleSize, Yellow);
            ui.TextCentered(Strings.Subtitle, new Vector2(Screen.x / 2f, h * 0.2f + 74f), 32f, Color.white);

            float rowY = h * 0.47f;
            var prev = new Rect(Screen.x / 2f - 250f, rowY - 38f, 76f, 76f);
            var next = new Rect(Screen.x / 2f + 174f, rowY - 38f, 76f, 76f);
            bool canPrev = gm.Level > 1, canNext = gm.Level < gm.Save.Level;
            DrawIconButton(prev, -Mathf.PI / 2f, canPrev);
            DrawIconButton(next, Mathf.PI / 2f, canNext);
            ui.TextCentered(string.Format(Strings.Level, gm.Level), new Vector2(Screen.x / 2f, rowY), 50f, Color.white);
            if (canPrev && Clicked(Inflate(prev, 6))) { gm.SelectLevel(gm.Level - 1); Click?.Invoke(SoundKind.Click); }
            if (canNext && Clicked(Inflate(next, 6))) { gm.SelectLevel(gm.Level + 1); Click?.Invoke(SoundKind.Click); }

            var play = new Rect(Screen.x / 2f - 200f, h * 0.6f, 400f, 104f);
            if (Button(play, Strings.Play, Green, 52f)) { gm.StartGame(); return; }

            string best = gm.Save.BestLevel > 0 ? string.Format(Strings.Best, gm.Save.BestLevel) : Strings.NoBest;
            ui.TextCentered(best, new Vector2(Screen.x / 2f, h * 0.6f + 140f), 30f, Color.white);
            ui.TextCentered(platform.IsMobile ? Strings.HintMobile : Strings.HintDesktop, new Vector2(Screen.x / 2f, h * 0.6f + 182f), 24f, new Color(1, 1, 1, 0.85f));

            var sound = new Rect(Screen.x - 330f, 20f, 310f, 60f);
            if (Button(sound, gm.Save.SoundOn ? Strings.SoundOn : Strings.SoundOff, Gray, 26f)) ToggleSound?.Invoke();

            var privacy = new Rect(20f, 20f, 290f, 60f);
            if (Button(privacy, Strings.Privacy, Gray, 26f)) platform.OpenUrl(Strings.PrivacyUrl);
            AdManager ads = AdManager.Instance;
            if (ads != null && ads.PrivacyOptionsRequired)
            {
                var adSettings = new Rect(20f, 92f, 290f, 60f);
                if (Button(adSettings, Strings.AdSettings, Gray, 26f)) ads.ShowPrivacyOptions();
            }
        }

        public void DrawPause(GameManager gm)
        {
            var panel = Window(3, Strings.Paused, out float y);
            if (Button(Row(panel, ref y), Strings.Resume, Green)) gm.Resume();
            else if (Button(Row(panel, ref y), Strings.RestartLevel, Blue)) gm.Restart();
            else if (Button(Row(panel, ref y), Strings.Menu, Gray)) gm.GoToMenu();
        }

        public void DrawGameOver(GameManager gm, World world)
        {
            string distance = string.Format(Strings.Distance, (int)(world.Progress * 100));
            if (gm.GameOverAdPending)
            {
                // all lives are gone: the ad comes first, the buttons appear after it
                Window(0, Strings.GameOver, out _, distance);
                return;
            }

            AdManager ads = AdManager.Instance;
            bool offerAd = gm.CanContinue && ads != null;
            var panel = Window(offerAd ? 3 : 2, Strings.GameOver, out float y, distance);

            if (offerAd)
            {
                var row = Row(panel, ref y);
                if (ads.IsRewardedReady())
                {
                    if (Button(row, string.Format(Strings.WatchAd, gm.BonusLives), Gold, 36f, true))
                    {
                        ads.ShowRewarded(
                            () =>
                            {
                                gm.ContinueAfterReward();
                                if (gm.State == GameState.Playing) ShowNotice(string.Format(Strings.RewardEarned, gm.BonusLives));
                            },
                            () => ShowNotice(Strings.RewardNotEarned));
                        return;
                    }
                }
                else
                {
                    DisabledButton(row, Strings.AdLoading);
                }
            }
            if (Button(Row(panel, ref y), Strings.PlayAgain, Green)) gm.Restart();
            else if (Button(Row(panel, ref y), Strings.Menu, Gray)) gm.GoToMenu();
        }

        public void DrawLevelComplete(GameManager gm)
        {
            var panel = Window(2, Strings.LevelComplete, out float y, string.Format(Strings.Time, gm.RoundTime, gm.Hits), true);
            float sy = panel.y + 196f;
            for (int i = 0; i < 3; i++)
            {
                var c = new Vector2(panel.center.x + (i - 1) * 86f, sy - (i == 1 ? 10f : 0f));
                ui.Icon(ui.Star, c + new Vector2(3, 4), 78f, new Color(0, 0, 0, 0.4f));
                ui.Icon(ui.Star, c, 78f, i < gm.LastStars ? UIKit.C(255, 202, 40) : new Color(1, 1, 1, 0.25f));
            }
            if (gm.LastWasNewBest)
                ui.TextCentered(Strings.NewBest, new Vector2(panel.center.x, panel.y + 122f), 28f, Yellow);

            if (Button(Row(panel, ref y), Strings.NextLevel, Green)) gm.NextLevel();
            else if (Button(Row(panel, ref y), Strings.Menu, Gray)) gm.GoToMenu();
        }

        public void DrawAdOverlay()
        {
            ui.Rect(new Rect(0, 0, Screen.x, Screen.y), new Color(0, 0, 0, 0.55f));
            ui.TextCentered(Strings.AdShowing, new Vector2(Screen.x / 2f, UsableHeight / 2f), 44f, Color.white);
        }

        /// <summary>Draws the test ads used in the Unity editor (full screen ad + bottom banner).</summary>
        public void DrawTestAds(EditorTestAds ads)
        {
            if (ads == null) return;
            if (ads.BannerVisible && Banner.height > 0)
            {
                ui.Panel(Banner, UIKit.C(250, 250, 250, 0.95f), 6f);
                ui.TextCentered("Banner reklama (TEST)", Banner.center, 24f, UIKit.C(60, 60, 60), false);
            }
            var ad = ads.Current;
            if (ad == null) return;
            ui.Rect(new Rect(0, 0, Screen.x, Screen.y), UIKit.C(15, 20, 35, 0.97f));
            ui.TextCentered(ad.Rewarded ? "MUKOFOTLI REKLAMA (TEST)" : "REKLAMA (TEST)", new Vector2(Screen.x / 2f, Screen.y * 0.3f), 56f, Yellow);
            ui.TextCentered("Unity muharririda haqiqiy reklama o'rniga shu oyna chiqadi", new Vector2(Screen.x / 2f, Screen.y * 0.3f + 70f), 26f, Color.white);
            int left = Mathf.CeilToInt(ad.SecondsLeft);
            var close = new Rect(Screen.x / 2f - 170f, Screen.y * 0.55f, 340f, 92f);
            if (left > 0)
            {
                DisabledButton(close, left + " ...");
            }
            else if (Button(close, ad.Rewarded ? "Mukofotni olish" : "Yopish  ✕", Green, 36f))
            {
                ads.Close(true);
            }
            if (ad.Rewarded && left > 0)
            {
                var skip = new Rect(Screen.x / 2f - 170f, Screen.y * 0.55f + 112f, 340f, 70f);
                if (Button(skip, "O'tkazib yuborish", Gray, 28f)) ads.Close(false);
            }
        }

        // ------------------------------------------------------------------ building blocks

        const float RowHeight = 92f, RowGap = 18f, PanelWidth = 600f;

        Rect Window(int rows, string title, out float firstRowY, string subtitle = null, bool stars = false)
        {
            float header = 120f + (subtitle != null ? 46f : 0f) + (stars ? 130f : 0f);
            float height = header + rows * (RowHeight + RowGap) + 18f;
            float width = Math.Min(PanelWidth, Screen.x - 40f);
            float top = Math.Max(14f, (UsableHeight - height) / 2f);
            var panel = new Rect((Screen.x - width) / 2f, top, width, height);

            ui.Rect(new Rect(0, 0, Screen.x, Screen.y), new Color(0, 0, 0, 0.35f));
            ui.Panel(new Rect(panel.x + 6, panel.y + 8, panel.width, panel.height), new Color(0, 0, 0, 0.3f), 30f);
            ui.Panel(panel, PanelColor, 30f);
            ui.TextCentered(title, new Vector2(panel.center.x, panel.y + 66f), 60f, Color.white);
            if (subtitle != null)
                ui.TextCentered(subtitle, new Vector2(panel.center.x, panel.y + (stars ? 270f : 124f)), 28f, new Color(1, 1, 1, 0.9f));

            firstRowY = panel.y + header;
            return panel;
        }

        static Rect Row(Rect panel, ref float y)
        {
            var r = new Rect(panel.x + 36f, y, panel.width - 72f, RowHeight);
            y += RowHeight + RowGap;
            return r;
        }

        bool Button(Rect r, string text, Color color, float textSize = 40f, bool adIcon = false)
        {
            bool pressed = IsPressing(r);
            var face = pressed ? new Rect(r.x, r.y + 4, r.width, r.height) : r;
            ui.Panel(new Rect(r.x, r.y + 7, r.width, r.height), Darken(color, 0.55f), 24f);
            ui.Panel(face, pressed ? Darken(color, 0.85f) : color, 24f);
            ui.Panel(new Rect(face.x + 6, face.y + 5, face.width - 12, face.height * 0.42f), new Color(1, 1, 1, 0.12f), 18f);
            if (adIcon)
            {
                var c = new Vector2(face.x + 52f, face.center.y);
                ui.Icon(ui.Circle, c, 52f, new Color(1, 1, 1, 0.95f));
                ui.Icon(ui.Arrow, c + new Vector2(3, 0), 26f, color, Mathf.PI / 2f);
                ui.TextCentered(text, new Vector2(face.center.x + 28f, face.center.y), textSize, Color.white);
            }
            else
            {
                ui.TextCentered(text, face.center, textSize, Color.white);
            }
            bool clicked = Clicked(r);
            if (clicked) Click?.Invoke(SoundKind.Click);
            return clicked;
        }

        void DrawIconButton(Rect r, float angle, bool enabled)
        {
            bool pressed = enabled && IsPressing(r);
            Color c = enabled ? (pressed ? Darken(Blue, 0.75f) : Blue) : new Color(Gray.r, Gray.g, Gray.b, 0.6f);
            ui.Panel(r, c, 20f);
            ui.Icon(ui.Arrow, r.center, r.width * 0.45f, new Color(1, 1, 1, enabled ? 1f : 0.5f), angle);
        }

        void DisabledButton(Rect r, string text)
        {
            ui.Panel(new Rect(r.x, r.y + 7, r.width, r.height), Darken(Gray, 0.55f), 24f);
            ui.Panel(r, new Color(Gray.r, Gray.g, Gray.b, 0.75f), 24f);
            ui.TextCentered(text, r.center, 32f, new Color(1, 1, 1, 0.75f));
        }

        // ---- a short message at the top of the screen ----

        string notice;
        float noticeUntil;

        public void ShowNotice(string text)
        {
            notice = text;
            noticeUntil = Time.unscaledTime + 3f;
        }

        public void DrawNotice()
        {
            if (notice == null || Time.unscaledTime > noticeUntil) return;
            Vector2 size = ui.Measure(notice, 30f);
            var box = new Rect(Screen.x / 2f - size.x / 2f - 24f, 110f, size.x + 48f, 58f);
            ui.Panel(box, new Color(0, 0, 0, 0.7f), 20f);
            ui.TextCentered(notice, box.center, 30f, Color.white, false);
        }

        public void DrawError(string text)
        {
            ui.Rect(new Rect(0, 0, Screen.x, Screen.y), UIKit.C(40, 10, 10, 0.95f));
            ui.Text("Xato:\n" + text, new Vector2(30f, 30f), 22f, Color.white);
        }

        static Color Darken(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    }
}
