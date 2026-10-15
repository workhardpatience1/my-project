using System;
using UnityEngine;
using Xna = Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    /// <summary>
    /// The whole game in Unity — the same loop as the phone version (ObstacleDodge.MonoGame/Shared/ObstacleDodgeGame.cs):
    /// menu → play → Game Over (ad) → "+3 jon" / play again → finish → next level.
    /// The rules live in the shared code (Scripts/Shared); this class only connects Unity to it.
    /// It is created automatically when any scene starts (see GameBoot), so the scene can stay empty.
    /// </summary>
    public sealed class ObstacleDodgeGame : MonoBehaviour
    {
        UnityPlatform platform;
        SaveData save;
        Sfx sfx;
        World world;
        GameManager gm;
        AdManager ads;
        WorldView view;
        FollowCam followCam;
        Camera cam;
        UIKit kit;
        GameUI gameUI;
        TouchControls touch;
        float menuTime;
        string fatalError;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;
            try { Init(); }
            catch (Exception e) { Fail("Start", e); }
        }

        void Init()
        {
            platform = new UnityPlatform();

            var camObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = camObject.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 140f;
            cam.depth = 10f;
            camObject.AddComponent<AudioListener>();

            view = new WorldView(cam);
            followCam = new FollowCam();
            kit = new UIKit();
            touch = new TouchControls();
            save = SaveData.Load(platform.SaveDirectory);
            sfx = new Sfx(gameObject) { Enabled = save.SoundOn };

            ads = new AdManager(platform.Ads);
            ads.SetBannerVisible(true); // the banner stays at the bottom the whole game

            gm = new GameManager(save, LoadLevel);
            gm.HitRegistered += () => platform.Vibrate(45);
            gm.Continued += () => world.Player.Invulnerable = 2f;
            gm.StateChanged += OnStateChanged;

            gameUI = new GameUI(kit, platform)
            {
                ToggleSound = () =>
                {
                    save.SoundOn = !save.SoundOn;
                    sfx.Enabled = save.SoundOn;
                    save.Write();
                    sfx.Play(SoundKind.Click);
                },
                Click = k => sfx.Play(k),
            };

            LoadLevel(gm.Level);
        }

        void LoadLevel(int level)
        {
            world = LevelGenerator.Generate(level);
            world.PlaySound = k => sfx.Play(k);
            followCam.SnapTo(world);
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

        void OnApplicationPause(bool paused)
        {
            if (paused) gm?.Pause(); // the phone went to the home screen
        }

        void Fail(string where, Exception e)
        {
            if (fatalError != null) return;
            fatalError = where + ": " + e;
            Debug.LogException(e);
            platform?.ReportError(fatalError);
        }

        // ------------------------------------------------------------------ every frame

        void Update()
        {
            if (fatalError != null) return;
            try { Step(); }
            catch (Exception e) { Fail("Update", e); }
        }

        void Step()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
            ads.Update();
            gameUI.Layout(Screen.width, Screen.height);
            HandleKeys();

            bool touchVisible = touch.IsVisible(platform.IsMobile, gm) && !ads.IsShowingAd;
            touch.Update(touchVisible, gameUI.Screen, gameUI.Banner, gameUI.Scale);

            switch (gm.State)
            {
                case GameState.Playing:
                    if (!ads.IsShowingAd)
                    {
                        world.Update(dt, ReadMove(), true);
                        followCam.Follow(world, dt);
                    }
                    break;
                case GameState.Menu:
                    menuTime += dt;
                    world.Update(dt, Xna.Vector2.Zero, false);
                    followCam.Orbit(dt);
                    break;
                case GameState.LevelComplete:
                    world.Update(dt, Xna.Vector2.Zero, false); // let the confetti fly
                    followCam.Follow(world, dt);
                    break;
                    // Paused and GameOver: time stands still
            }
            if (!ads.IsShowingAd) gm.Tick(dt); // round time + the Game Over ad timer

            followCam.Apply(cam, world.Shake);
            view.Sync(world, Time.unscaledTime);
        }

        /// <summary>Keyboard (WASD / arrows) + on-screen arrows, clamped to -1..1.</summary>
        Xna.Vector2 ReadMove()
        {
            float h = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                    - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float v = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                    - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            return new Xna.Vector2(
                Mathf.Clamp(h + TouchControls.Horizontal, -1f, 1f),
                Mathf.Clamp(v + TouchControls.Vertical, -1f, 1f));
        }

        void HandleKeys()
        {
            if (ads.IsShowingAd) return;
            // Escape is also the Android "back" button
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                switch (gm.State)
                {
                    case GameState.Playing: gm.Pause(); break;
                    case GameState.Paused: gm.Resume(); break;
                    case GameState.Menu: platform.Quit(); break;
                    default: gm.GoToMenu(); break;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
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

        // ------------------------------------------------------------------ UI

        void OnGUI()
        {
            if (gameUI == null)
            {
                if (fatalError != null) GUI.Label(new Rect(20, 20, Screen.width - 40, Screen.height - 40), "Xato: " + fatalError);
                return;
            }
            gameUI.Begin();
            if (fatalError != null) { gameUI.DrawError(fatalError); return; }
            try { DrawUI(); }
            catch (Exception e) { Fail("OnGUI", e); }
        }

        void DrawUI()
        {
            bool touchVisible = touch.IsVisible(platform.IsMobile, gm) && !ads.IsShowingAd;
            switch (gm.State)
            {
                case GameState.Menu:
                    gameUI.DrawMenu(gm, menuTime);
                    break;
                case GameState.Playing:
                    gameUI.DrawHud(gm, world, true);
                    touch.Draw(kit, touchVisible);
                    if (!ads.IsShowingAd && gameUI.PauseClicked()) gm.Pause();
                    break;
                case GameState.Paused:
                    gameUI.DrawHud(gm, world, false);
                    gameUI.DrawPause(gm);
                    break;
                case GameState.GameOver:
                    gameUI.DrawHud(gm, world, false);
                    if (!ads.IsShowingAd) gameUI.DrawGameOver(gm, world);
                    break;
                case GameState.LevelComplete:
                    gameUI.DrawHud(gm, world, false);
                    if (!ads.IsShowingAd) gameUI.DrawLevelComplete(gm);
                    break;
            }
            gameUI.DrawNotice();
            if (ads.IsShowingAd) gameUI.DrawAdOverlay();
            gameUI.DrawTestAds(platform.TestAds);
        }
    }

    /// <summary>Starts the game in whatever scene is opened (SampleScene is empty on purpose).</summary>
    public static class GameBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (UnityEngine.Object.FindAnyObjectByType<ObstacleDodgeGame>() != null) return;
            var go = new GameObject("Obstacle Dodge");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ObstacleDodgeGame>();
        }
    }
}
