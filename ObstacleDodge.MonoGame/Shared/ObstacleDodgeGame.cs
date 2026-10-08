using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ObstacleDodge
{
    /// <summary>
    /// The whole game. It plays the role of the Unity scene: it creates the level, the camera,
    /// GameManager, AdManager, the UI, and calls them every frame.
    /// </summary>
    public class ObstacleDodgeGame : Game
    {
        readonly IPlatform platform;
        readonly GraphicsDeviceManager graphics;

        SpriteBatch batch;
        BitmapFont font;
        UIDraw ui;
        UIInput input;
        GameUI gameUI;
        TouchControls touch;
        WorldRenderer renderer;
        FollowCamera camera;
        Sfx sfx;
        SaveData save;
        World world;
        GameManager gm;
        AdManager ads;
        float menuTime;
        KeyboardState previousKeys;
        GamePadState previousPad;

        // hooks used by the automatic screenshot test (Desktop project)
        public Func<Vector2> MoveOverride;
        public event Action<float> AfterUpdate;
        public event Action AfterDraw;
        public GameManager Manager => gm;
        public World CurrentWorld => world;
        public TouchControls Touch => touch;
        public GraphicsDeviceManager Graphics => graphics;

        public ObstacleDodgeGame(IPlatform platform)
        {
            this.platform = platform;
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredDepthStencilFormat = DepthFormat.Depth24,
                SynchronizeWithVerticalRetrace = true,
            };
            if (platform.IsMobile)
            {
                graphics.IsFullScreen = true;
                graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
            }
            else
            {
                graphics.PreferredBackBufferWidth = 1280;
                graphics.PreferredBackBufferHeight = 720;
                Window.AllowUserResizing = true;
                IsMouseVisible = true;
            }
            IsFixedTimeStep = false;
            Window.Title = "Obstacle Dodge";
        }

        // If anything throws, the game stops and shows the error text instead of crashing silently.
        string fatalError;

        void Fail(string where, Exception e)
        {
            if (fatalError != null) return;
            fatalError = where + ": " + e;
            Console.WriteLine("[Fatal] " + fatalError);
            try { platform.ReportError(fatalError); } catch { }
        }

        protected override void LoadContent()
        {
            try { LoadContentCore(); }
            catch (Exception e) { Fail("LoadContent", e); }
        }

        void LoadContentCore()
        {
            batch = new SpriteBatch(GraphicsDevice);
            font = BitmapFont.LoadEmbedded(GraphicsDevice);
            ui = new UIDraw(GraphicsDevice);
            input = new UIInput();
            touch = new TouchControls();
            renderer = new WorldRenderer(GraphicsDevice);
            camera = new FollowCamera();
            save = SaveData.Load(platform.SaveDirectory);
            sfx = new Sfx { Enabled = save.SoundOn };

            ads = new AdManager(platform.Ads);
            ads.SetBannerVisible(true); // the banner stays at the bottom the whole game (guide 5.9)

            gm = new GameManager(save, LoadLevel);
            gm.HitRegistered += () => platform.Vibrate(45);
            gm.Continued += () => world.Player.Invulnerable = 2f;
            gm.StateChanged += OnStateChanged;

            gameUI = new GameUI(font, ui, input, platform)
            {
                ToggleSound = () =>
                {
                    save.SoundOn = !save.SoundOn;
                    sfx.Enabled = save.SoundOn;
                    save.Write();
                    sfx.Play(SoundKind.Click);
                },
                Quit = () => platform.Quit(),
                Click = k => sfx.Play(k),
            };

            LoadLevel(gm.Level);
        }

        void LoadLevel(int level)
        {
            world = LevelGenerator.Generate(level);
            world.PlaySound = k => sfx.Play(k);
            camera.SnapTo(world);
        }

        void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.GameOver:
                    sfx.Play(SoundKind.Lose);
                    platform.Vibrate(200);
                    break;
                case GameState.Menu:
                    LoadLevel(gm.Level); // a fresh level as the menu background
                    break;
            }
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            base.OnDeactivated(sender, args);
            gm?.Pause(); // the phone went to the home screen: pause the round
        }

        protected override void Update(GameTime gameTime)
        {
            if (fatalError == null)
            {
                try { UpdateCore(gameTime); }
                catch (Exception e) { Fail("Update", e); }
            }
            base.Update(gameTime);
        }

        void UpdateCore(GameTime gameTime)
        {
            float dt = (float)Math.Min(gameTime.ElapsedGameTime.TotalSeconds, 1.0 / 20.0);
            ads.Update();

            var vp = GraphicsDevice.Viewport;
            gameUI.Layout(vp.Width, vp.Height);
            input.Update(GameUI.GetScale(vp.Width, vp.Height), !platform.IsMobile, IsActive);
            HandleKeys();

            bool touchVisible = touch.IsVisible(platform.IsMobile, gm) && !ads.IsShowingAd;
            touch.Update(input, touchVisible, gameUI.Screen, gameUI.Banner);

            switch (gm.State)
            {
                case GameState.Playing:
                    if (!ads.IsShowingAd)
                    {
                        world.Update(dt, ReadMove(), true);
                        gm.Tick(dt);
                        camera.Follow(world, dt);
                    }
                    break;
                case GameState.Menu:
                    menuTime += dt;
                    world.Update(dt, Vector2.Zero, false);
                    camera.Orbit(world, dt);
                    break;
                case GameState.LevelComplete:
                    world.Update(dt, Vector2.Zero, false); // let the confetti fly
                    camera.Follow(world, dt);
                    break;
                    // Paused and GameOver: time stands still (Time.timeScale = 0 in the guide)
            }

            AfterUpdate?.Invoke(dt);
        }

        /// <summary>Keyboard (like Input.GetAxis) + on-screen buttons, clamped to -1..1 (guide 4.3).</summary>
        Vector2 ReadMove()
        {
            if (MoveOverride != null) return MoveOverride();
            var k = Keyboard.GetState();
            float h = (k.IsKeyDown(Keys.D) || k.IsKeyDown(Keys.Right) ? 1f : 0f) - (k.IsKeyDown(Keys.A) || k.IsKeyDown(Keys.Left) ? 1f : 0f);
            float v = (k.IsKeyDown(Keys.W) || k.IsKeyDown(Keys.Up) ? 1f : 0f) - (k.IsKeyDown(Keys.S) || k.IsKeyDown(Keys.Down) ? 1f : 0f);
            var pad = GamePad.GetState(PlayerIndex.One);
            h += pad.ThumbSticks.Left.X;
            v += pad.ThumbSticks.Left.Y;
            return new Vector2(
                MathHelper.Clamp(h + TouchControls.Horizontal, -1f, 1f),
                MathHelper.Clamp(v + TouchControls.Vertical, -1f, 1f));
        }

        void HandleKeys()
        {
            var keys = Keyboard.GetState();
            var pad = GamePad.GetState(PlayerIndex.One);
            bool back = Pressed(keys, Keys.Escape) || (pad.Buttons.Back == ButtonState.Pressed && previousPad.Buttons.Back != ButtonState.Pressed);
            bool confirm = Pressed(keys, Keys.Enter) || Pressed(keys, Keys.Space);
            previousKeys = keys;
            previousPad = pad;
            if (ads.IsShowingAd) return;

            if (back)
            {
                switch (gm.State)
                {
                    case GameState.Playing: gm.Pause(); break;
                    case GameState.Paused: gm.Resume(); break;
                    case GameState.Menu: platform.Quit(); if (!platform.IsMobile) Exit(); break;
                    default: gm.GoToMenu(); break;
                }
            }
            else if (confirm)
            {
                switch (gm.State)
                {
                    case GameState.Menu: gm.StartGame(); break;
                    case GameState.Paused: gm.Resume(); break;
                    case GameState.GameOver: gm.Restart(); break;
                    case GameState.LevelComplete: gm.NextLevel(); break;
                }
            }
        }

        bool Pressed(KeyboardState keys, Keys key) => keys.IsKeyDown(key) && !previousKeys.IsKeyDown(key);

        protected override void Draw(GameTime gameTime)
        {
            if (fatalError == null)
            {
                try { DrawCore(); }
                catch (Exception e) { Fail("Draw", e); }
            }
            if (fatalError != null) GraphicsDevice.Clear(new Color(40, 10, 10));
            base.Draw(gameTime);
            AfterDraw?.Invoke();
        }

        void DrawCore()
        {
            var vp = GraphicsDevice.Viewport;
            GraphicsDevice.Clear(world.Sky);

            // sky: darker at the top, the fog color at the horizon
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            batch.Draw(ui.SkyGradient, new Rectangle(0, 0, vp.Width, (int)(vp.Height * 0.55f)), new Color(world.Sky.ToVector3() * 0.72f));
            batch.End();

            camera.BuildMatrices(vp.AspectRatio, world.Shake);
            renderer.Draw(world, camera);

            float scale = GameUI.GetScale(vp.Width, vp.Height);
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Matrix.CreateScale(scale));

            bool touchVisible = touch.IsVisible(platform.IsMobile, gm) && !ads.IsShowingAd;
            switch (gm.State)
            {
                case GameState.Menu:
                    gameUI.DrawMenu(batch, gm, menuTime);
                    break;
                case GameState.Playing:
                    gameUI.DrawHud(batch, gm, world, true);
                    touch.Draw(batch, ui, touchVisible);
                    if (!ads.IsShowingAd && gameUI.PauseClicked()) gm.Pause();
                    break;
                case GameState.Paused:
                    gameUI.DrawHud(batch, gm, world, false);
                    gameUI.DrawPause(batch, gm);
                    break;
                case GameState.GameOver:
                    gameUI.DrawHud(batch, gm, world, false);
                    if (!ads.IsShowingAd) gameUI.DrawGameOver(batch, gm, world);
                    break;
                case GameState.LevelComplete:
                    gameUI.DrawHud(batch, gm, world, false);
                    if (!ads.IsShowingAd) gameUI.DrawLevelComplete(batch, gm);
                    break;
            }
            if (ads.IsShowingAd) gameUI.DrawAdOverlay(batch);
            batch.End();
        }
    }
}
