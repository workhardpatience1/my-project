using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ObstacleDodge.Tests
{
    sealed class CountingAds : IAdsProvider
    {
        public int Interstitials, Rewarded;
        public bool RewardWorks = true;
        public void Initialize() { }
        public bool IsRewardedReady => true;
        public bool IsInterstitialReady => true;
        public void ShowRewarded(Action onRewarded, Action onFailed) { Rewarded++; (RewardWorks ? onRewarded : onFailed)(); }
        public void ShowInterstitial(Action onClosed) { Interstitials++; onClosed(); }
        public void SetBannerVisible(bool visible) { }
        public bool PrivacyOptionsRequired => false;
        public void ShowPrivacyOptions() { }
    }

    static class Program
    {
        static int failed, passed;

        static void Check(bool condition, string what)
        {
            if (condition) { passed++; Console.WriteLine("  ok   " + what); }
            else { failed++; Console.WriteLine("  FAIL " + what); }
        }

        static int Main()
        {
            GameLoop();
            ObjectHitCountsOnce();
            DropperFallsWhenPlayerIsNear();
            TrapFiresProjectiles();
            LevelsAreDeterministic();
            LevelsCanBeFinished();
            Console.WriteLine($"\n{passed} passed, {failed} failed");
            return failed == 0 ? 0 : 1;
        }

        static (GameManager gm, CountingAds ads, World[] world) NewGame()
        {
            var ads = new CountingAds();
            var adManager = new AdManager(ads) { MinSecondsBetweenInterstitials = 0 };
            var holder = new World[1];
            string dir = Path.Combine(Path.GetTempPath(), "od-tests-" + Guid.NewGuid().ToString("N"));
            var gm = new GameManager(SaveData.Load(dir), level => holder[0] = LevelGenerator.Generate(level));
            GameManager.ResetRoundCounterForTests();
            return (gm, ads, holder);
        }

        static void GameLoop()
        {
            Console.WriteLine("Game loop (ads guide, Part 2)");
            var (gm, ads, _) = NewGame();
            var adManager = AdManager.Instance;
            adManager.MinSecondsBetweenInterstitials = 1000; // the Game Over ad must ignore this
            void Frame(float dt) { adManager.Update(); gm.Tick(dt); adManager.Update(); }

            gm.StartGame();
            Check(gm.State == GameState.Playing && gm.Hits == 0, "starts playing with 0 hits");
            for (int i = 0; i < 4; i++) gm.RegisterHit();
            Check(gm.State == GameState.Playing && gm.LivesLeft == 1, "4 hits: still playing, 1 life left");
            gm.RegisterHit();
            Check(gm.State == GameState.GameOver, "5th hit: GAME OVER");
            gm.RegisterHit();
            Check(gm.Hits == 5, "hits are not counted after Game Over");

            Check(gm.GameOverAdPending && ads.Interstitials == 0, "Game Over: the ad is waiting (short pause first)");
            gm.Restart();
            Check(gm.State == GameState.GameOver, "buttons do nothing until the Game Over ad was shown");
            Frame(0.5f);
            Check(ads.Interstitials == 0, "no ad during the first 0.5 s");
            Frame(0.6f);
            Check(ads.Interstitials == 1 && !gm.GameOverAdPending, "interstitial after Game Over (lives gone #1)");

            AdManager.Instance.ShowRewarded(gm.ContinueAfterReward);
            AdManager.Instance.Update();
            Check(gm.State == GameState.Playing && gm.Hits == 2 && gm.LivesLeft == 3, "rewarded ad: +3 lives (hits 2 / 5)");
            Check(!gm.CanContinue, "continue only once per round");

            for (int i = 0; i < 3; i++) gm.RegisterHit();
            Check(gm.State == GameState.GameOver, "Game Over again");
            Frame(1.1f);
            Check(ads.Interstitials == 2, "interstitial again after every Game Over (even right after another ad)");

            gm.Restart();
            Frame(0.1f);
            Check(gm.State == GameState.Playing && gm.CanContinue && gm.Hits == 0, "restart resets hits and the continue");
            Check(ads.Interstitials == 2, "'Qayta o'ynash' itself shows no extra ad");

            adManager.MinSecondsBetweenInterstitials = 0;
            int before = ads.Interstitials;
            for (int level = 0; level < 3; level++)
            {
                gm.CompleteLevel();
                if (level == 0)
                {
                    Check(gm.State == GameState.LevelComplete && gm.LastStars == 3, "finish without hits = 3 stars");
                    Check(gm.Save.Level == 2 && gm.Save.BestLevel == 1, "progress saved (level 2 unlocked)");
                }
                gm.NextLevel();
                adManager.Update();
            }
            Check(gm.Level == 4 && gm.State == GameState.Playing, "next level starts");
            Check(ads.Interstitials == before + 1, "one interstitial per 3 finished levels");
        }

        static void ObjectHitCountsOnce()
        {
            Console.WriteLine("ObjectHit: every obstacle is counted once");
            var (gm, _, holder) = NewGame();
            gm.StartGame();
            var world = new World(1) { Length = 50 };
            world.Player.Reset(new Vector3(0, 0, 0));
            var wall = new Wall(new Vector3(0, 0.5f, -3f), new Vector3(4, 1, 1), Color.Orange);
            world.Obstacles.Add(wall);
            for (int i = 0; i < 180; i++) world.Update(1f / 60f, new Vector2(0, 1), true); // push into the wall for 3 s
            Check(wall.IsHit, "the wall got the 'Hit' mark");
            Check(gm.Hits == 1, $"pushing into the same wall = 1 hit (got {gm.Hits})");
            Check(world.Player.Position.Z > -3f + 0.5f, "the player cannot walk through the wall");
        }

        static void DropperFallsWhenPlayerIsNear()
        {
            Console.WriteLine("Dropper");
            var (gm, _, _) = NewGame();
            gm.StartGame();
            var world = new World(2) { Length = 80 };
            world.Player.Reset(Vector3.Zero);
            var dropper = new Dropper(new Vector3(3, 0, -30), 1.6f, Color.Purple) { WakeDistance = 10, TimeToWait = 0.1f };
            world.Obstacles.Add(dropper);
            for (int i = 0; i < 60; i++) world.Update(1f / 60f, Vector2.Zero, true);
            Check(!dropper.Visible && dropper.Position.Y > 10, "hidden and waiting while the player is far");
            world.Player.Position = new Vector3(-3, 0, -22);
            for (int i = 0; i < 120; i++) world.Update(1f / 60f, Vector2.Zero, true);
            Check(dropper.Visible && Math.Abs(dropper.Position.Y - dropper.GroundY) < 0.001f, "falls and lands on the ground");
        }

        static void TrapFiresProjectiles()
        {
            Console.WriteLine("TriggerProjectile + FlyAtPlayer");
            var (gm, _, _) = NewGame();
            gm.StartGame();
            var world = new World(3) { Length = 80 };
            world.Player.Reset(Vector3.Zero);
            var trap = new TriggerProjectile { Center = new Vector3(0, 0, -5), HalfSize = new Vector2(7, 1) };
            for (int i = 0; i < 5; i++)
            {
                var p = new FlyAtPlayer(new Vector3(-6 + i * 3, 1, -18), 8f, Color.Yellow);
                trap.Projectiles.Add(p);
                world.Obstacles.Add(p);
            }
            world.Triggers.Add(trap);
            for (int i = 0; i < 30; i++) world.Update(1f / 60f, Vector2.Zero, true);
            Check(!trap.Fired && trap.Projectiles.All(p => !p.Visible), "projectiles sleep before the trap");
            world.Player.Position = new Vector3(0, 0, -5);
            world.Update(1f / 60f, Vector2.Zero, true);
            Check(trap.Fired && trap.Projectiles.All(p => p.IsFlying), "entering the zone launches all 5");
            for (int i = 0; i < 300 && gm.State == GameState.Playing; i++) world.Update(1f / 60f, Vector2.Zero, true);
            Check(world.Obstacles.OfType<FlyAtPlayer>().Count() == 0, "projectiles are destroyed (hit or reached the spot)");
            Check(gm.Hits >= 1, $"standing still gets you hit (hits {gm.Hits})");
        }

        static void LevelsAreDeterministic()
        {
            Console.WriteLine("Levels");
            var a = LevelGenerator.Generate(7);
            var b = LevelGenerator.Generate(7);
            Check(a.Obstacles.Count == b.Obstacles.Count &&
                  a.Obstacles.Zip(b.Obstacles).All(p => p.First.Position == p.Second.Position),
                  "the same level number always gives the same level");
            Check(LevelGenerator.Generate(20).Length > LevelGenerator.Generate(1).Length, "later levels are longer");
        }

        /// <summary>
        /// For levels 1..60: put every obstacle that can stand still on a grid (walls, buildings,
        /// landed droppers, spinner pillars), make it fatter by the player's radius, and check
        /// that a path from the start to the finish exists.
        /// </summary>
        static void LevelsCanBeFinished()
        {
            const float cell = 0.2f;
            int bad = 0;
            for (int level = 1; level <= 60; level++)
            {
                var world = LevelGenerator.Generate(level);
                int nx = (int)(world.HalfWidth * 2 / cell);
                int nz = (int)((world.Length + 4) / cell);
                var blocked = new bool[nx, nz];
                float limit = world.HalfWidth - Mover.Radius;
                for (int ix = 0; ix < nx; ix++)
                    for (int iz = 0; iz < nz; iz++)
                    {
                        var p = new Vector3(-world.HalfWidth + (ix + 0.5f) * cell, 0, 1f - (iz + 0.5f) * cell);
                        if (Math.Abs(p.X) > limit) { blocked[ix, iz] = true; continue; }
                        foreach (var o in world.Obstacles)
                        {
                            if (!o.Solid || o is FlyAtPlayer || o is Slider) continue;
                            Vector3 push;
                            bool hit = o switch
                            {
                                Dropper d => Collision.CircleVsBox(p, Mover.Radius, new Vector3(d.Position.X, 0, d.Position.Z), d.Size * 0.5f, 0, out push),
                                Spinner s => Collision.CircleVsCircle(p, Mover.Radius, s.Position, s.PillarRadius, out push),
                                _ => o.Collide(p, Mover.Radius, Mover.Height, out push),
                            };
                            if (hit) { blocked[ix, iz] = true; break; }
                        }
                    }

                // breadth-first search from the start row to the finish row
                var seen = new bool[nx, nz];
                var queue = new Queue<(int, int)>();
                int startZ = (int)((1f - (-1f)) / cell);
                for (int ix = 0; ix < nx; ix++)
                    if (!blocked[ix, startZ]) { seen[ix, startZ] = true; queue.Enqueue((ix, startZ)); }
                int finishZ = (int)((1f - world.FinishZ) / cell);
                bool reached = false;
                while (queue.Count > 0 && !reached)
                {
                    var (x, z) = queue.Dequeue();
                    if (z >= finishZ) { reached = true; break; }
                    foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        int x2 = x + dx, z2 = z + dz;
                        if (x2 < 0 || z2 < 0 || x2 >= nx || z2 >= nz || seen[x2, z2] || blocked[x2, z2]) continue;
                        seen[x2, z2] = true;
                        queue.Enqueue((x2, z2));
                    }
                }
                bool startClear = world.Obstacles.All(o => !o.Solid || o.Position.Z < -8f || o is Dropper);
                if (!reached || !startClear)
                {
                    bad++;
                    Console.WriteLine($"  level {level}: path {(reached ? "ok" : "BLOCKED")}, start {(startClear ? "clear" : "BLOCKED")}");
                }
            }
            Check(bad == 0, "levels 1..60 all have a free path from start to finish");
        }
    }
}
