using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ObstacleDodge
{
    /// <summary>
    /// Automatic test run: plays the game by itself (menu → play → Game Over → rewarded continue →
    /// finish → next level → pause), saves a screenshot of every screen and prints what happened.
    /// Usage: ObstacleDodge --shots out_dir [--size 1280x720] [--mobile]
    /// </summary>
    static class ScreenshotTour
    {
        /// <summary>A clumsy bot: walks into the nearest obstacle that was not hit yet (tests collisions).</summary>
        static Vector2 SeekObstacle(World world)
        {
            var p = world.Player.Position;
            Obstacle best = null;
            float bestDist = float.MaxValue;
            foreach (var o in world.Obstacles)
            {
                if (!o.Solid || o.IsHit || !o.Visible || o is FlyAtPlayer) continue;
                if (o.Position.Z > p.Z + 0.5f) continue; // behind us
                float d = Vector2.Distance(new Vector2(o.Position.X, o.Position.Z), new Vector2(p.X, p.Z));
                if (d < bestDist) { bestDist = d; best = o; }
            }
            if (best == null) return new Vector2(0, 1);
            var dir = new Vector2(best.Position.X - p.X, -(best.Position.Z - p.Z));
            if (dir.LengthSquared() > 1e-4f) dir.Normalize();
            return dir;
        }

        public static void Attach(ObstacleDodgeGame game, string dir, int width, int height)
        {
            Directory.CreateDirectory(dir);
            game.IsFixedTimeStep = true;
            game.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
            game.Graphics.SynchronizeWithVerticalRetrace = false;
            game.Graphics.PreferredBackBufferWidth = width;
            game.Graphics.PreferredBackBufferHeight = height;

            float time = 0f, stepTime = 0f;
            int step = 0;
            string pendingShot = null;
            var log = new List<string>();
            bool sizeApplied = false;
            GameState lastState = GameState.Menu;
            bool shotPending = false;

            void Next() { step++; stepTime = 0f; }
            void Log(string s) { Console.WriteLine("[Tour] " + s); log.Add(s); }

            game.AfterUpdate += dt =>
            {
                time += dt;
                stepTime += dt;
                var gm = game.Manager;
                var world = game.CurrentWorld;
                if (!sizeApplied) { game.Graphics.ApplyChanges(); sizeApplied = true; }
                if (gm.State != lastState) { Log($"state {lastState} -> {gm.State} (hits {gm.Hits}, level {gm.Level})"); lastState = gm.State; }

                switch (step)
                {
                    case 0:
                        if (stepTime > 1.5f) { pendingShot = "01_menu"; Next(); }
                        break;
                    case 1:
                        gm.StartGame();
                        game.Touch.ShowOnPC = true;
                        game.MoveOverride = () => SeekObstacle(game.CurrentWorld);
                        Next();
                        break;
                    case 2:
                        if (stepTime > 2.2f) { pendingShot = "02_play"; Next(); }
                        break;
                    case 3:
                        // keep running forward; obstacles will be hit until Game Over (or the finish)
                        if (gm.State == GameState.GameOver && gm.GameOverAdPending && stepTime > 0.3f && !shotPending)
                        {
                            pendingShot = "03a_gameover_before_ad"; // title only: the ad comes first
                            shotPending = true;
                        }
                        else if (gm.State == GameState.GameOver && !gm.GameOverAdPending) { pendingShot = "03_gameover"; Next(); }
                        else if (gm.State == GameState.LevelComplete) { Log("finished before game over"); step = 6; }
                        else if (stepTime > 40f) { Log("TIMEOUT waiting for game over"); step = 6; }
                        break;
                    case 4:
                        if (stepTime > 0.5f)
                        {
                            Log($"rewarded ready: {AdManager.Instance.IsRewardedReady()}, can continue: {gm.CanContinue}");
                            AdManager.Instance.ShowRewarded(gm.ContinueAfterReward, () => Log("reward FAILED"));
                            Next();
                        }
                        break;
                    case 5:
                        if (stepTime > 0.6f)
                        {
                            Log($"after reward: state {gm.State}, hits {gm.Hits}, lives {gm.LivesLeft}");
                            pendingShot = "04_continued";
                            Next();
                        }
                        break;
                    case 6:
                        if (gm.State != GameState.Playing) { Log("not playing at teleport, state " + gm.State); }
                        world.Player.Position = new Vector3(0, 0, world.FinishZ + 4f);
                        game.MoveOverride = () => new Vector2(0, 1f);
                        Next();
                        break;
                    case 7:
                        if (gm.State == GameState.LevelComplete && stepTime > 1.2f) { pendingShot = "05_complete"; Next(); }
                        else if (stepTime > 10f) { Log("TIMEOUT waiting for level complete"); Next(); }
                        break;
                    case 8:
                        gm.NextLevel();
                        game.MoveOverride = () => Vector2.Zero;
                        Next();
                        break;
                    case 9:
                        if (stepTime > 1f) { gm.Pause(); Next(); }
                        break;
                    case 10:
                        if (stepTime > 0.3f) { pendingShot = "06_pause"; Next(); }
                        break;
                    case 11:
                        gm.GoToMenu();
                        Next();
                        break;
                    case 12:
                        if (stepTime > 1f)
                        {
                            Log($"save: level {gm.Save.Level}, best {gm.Save.BestLevel}");
                            pendingShot = "07_menu_after";
                            Next();
                        }
                        break;
                    case 13:
                        if (stepTime > 0.3f)
                        {
                            File.WriteAllLines(Path.Combine(dir, "tour.log"), log);
                            game.Exit();
                        }
                        break;
                }
            };

            game.AfterDraw += () =>
            {
                if (pendingShot == null) return;
                var device = game.GraphicsDevice;
                int w = device.PresentationParameters.BackBufferWidth, h = device.PresentationParameters.BackBufferHeight;
                var data = new Color[w * h];
                device.GetBackBufferData(data);
                for (int i = 0; i < data.Length; i++) data[i].A = 255;
                using var tex = new Texture2D(device, w, h);
                tex.SetData(data);
                using var file = File.Create(Path.Combine(dir, pendingShot + ".png"));
                tex.SaveAsPng(file, w, h);
                Log("screenshot " + pendingShot);
                pendingShot = null;
            };
        }
    }
}
