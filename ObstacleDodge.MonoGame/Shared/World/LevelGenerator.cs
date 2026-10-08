using System;
using Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    /// <summary>
    /// Builds a level from the same building blocks as the guide (walls, small buildings,
    /// Spinning Thing, Dropping Object, Trigger Volume + projectiles). Every level number
    /// always gives the same layout, and later levels are longer and harder.
    /// Every pattern leaves a free path, so each level can be finished without a hit.
    /// </summary>
    public static class LevelGenerator
    {
        static readonly Color WallOrange = new Color(255, 140, 0);
        static readonly Color BuildingBlue = new Color(92, 107, 192);
        static readonly Color BuildingTeal = new Color(38, 166, 154);
        static readonly Color SpinnerRed = new Color(229, 57, 53);
        static readonly Color DropperPurple = new Color(171, 71, 188);
        static readonly Color ProjectileYellow = new Color(255, 213, 0);
        static readonly Color SliderPink = new Color(236, 64, 122);
        static readonly Color RailGray = new Color(176, 190, 197);

        // ground colors per "world" (changes every 5 levels)
        static readonly (Color a, Color b, Color grass, Color sky)[] Themes =
        {
            (new Color(126, 200, 100), new Color(110, 184, 86), new Color(76, 150, 64), new Color(135, 200, 245)),
            (new Color(232, 205, 140), new Color(220, 190, 122), new Color(196, 160, 96), new Color(250, 200, 150)),
            (new Color(200, 225, 240), new Color(182, 210, 230), new Color(150, 180, 205), new Color(170, 200, 230)),
            (new Color(150, 130, 190), new Color(134, 114, 176), new Color(90, 74, 130), new Color(60, 50, 110)),
        };

        public static World Generate(int level)
        {
            level = Math.Max(1, level);
            var rng = new Random(level * 7919 + 17);
            var world = new World(level) { LevelNumber = level };

            float t = MathHelper.Clamp((level - 1) / 14f, 0f, 1f); // 0 = easy, 1 = hardest
            world.Length = 70f + Math.Min(level - 1, 14) * 9f;
            world.Player.MoveSpeed = 8f;
            world.Player.Reset(new Vector3(0, 0, -1f));

            var theme = Themes[((level - 1) / 5) % Themes.Length];
            world.GroundA = theme.a;
            world.GroundB = theme.b;
            world.Grass = theme.grass;
            world.Sky = theme.sky;

            AddRailsAndFinish(world);

            float d = 12f; // distance from the start; z = -d
            float end = world.Length - 10f;
            int patternIndex = 0;
            while (d < end)
            {
                int pattern = PickPattern(level, patternIndex, rng);
                float used = pattern switch
                {
                    0 => WallRow(world, rng, d, t),
                    1 => Buildings(world, rng, d, t),
                    2 => SpinningThing(world, rng, d, t),
                    3 => Droppers(world, rng, d, t),
                    4 => ProjectileTrap(world, rng, d, t, level),
                    5 => Sliders(world, rng, d, t),
                    _ => ZigZag(world, rng, d, t),
                };
                d += used + MathHelper.Lerp(9f, 5.5f, t) + (float)rng.NextDouble() * 3f;
                patternIndex++;
            }
            return world;
        }

        static int PickPattern(int level, int index, Random rng)
        {
            // The first two levels teach one thing at a time.
            if (level == 1) return index % 3 == 2 ? 1 : 0;
            if (level == 2) return new[] { 0, 2, 1, 3, 0, 2 }[index % 6];

            // weights: wall, buildings, spinner, droppers, trap, sliders, zigzag
            int[] weights = level < 4
                ? new[] { 3, 2, 3, 2, 2, 0, 1 }
                : new[] { 2, 2, 3, 3, 3, 2, 2 };
            int total = 0;
            foreach (int w in weights) total += w;
            int roll = rng.Next(total);
            for (int i = 0; i < weights.Length; i++)
            {
                if (roll < weights[i]) return i;
                roll -= weights[i];
            }
            return 0;
        }

        static void AddRailsAndFinish(World world)
        {
            float len = world.Length + 8f;
            float x = world.HalfWidth + 0.3f;
            world.Obstacles.Add(new Decoration(new Vector3(-x, 0.3f, -len / 2 + 4f), new Vector3(0.6f, 0.6f, len), RailGray));
            world.Obstacles.Add(new Decoration(new Vector3(x, 0.3f, -len / 2 + 4f), new Vector3(0.6f, 0.6f, len), RailGray));

            // start line
            world.Obstacles.Add(new Decoration(new Vector3(0, 0.02f, 0.5f), new Vector3(world.HalfWidth * 2, 0.04f, 0.4f), Color.White));

            // finish: a checkered strip, two posts and a banner
            float z = world.FinishZ;
            int squares = 14;
            float w = world.HalfWidth * 2 / squares;
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < squares; i++)
                {
                    bool dark = (i + row) % 2 == 0;
                    world.Obstacles.Add(new Decoration(
                        new Vector3(-world.HalfWidth + w * (i + 0.5f), 0.03f, z + (row - 0.5f) * w),
                        new Vector3(w, 0.05f, w), dark ? new Color(30, 30, 30) : Color.White));
                }
            world.Obstacles.Add(new Decoration(new Vector3(-x - 0.2f, 2.5f, z), new Vector3(0.5f, 5f, 0.5f), new Color(250, 250, 250), ShapeKind.Cylinder, true));
            world.Obstacles.Add(new Decoration(new Vector3(x + 0.2f, 2.5f, z), new Vector3(0.5f, 5f, 0.5f), new Color(250, 250, 250), ShapeKind.Cylinder, true));
            world.Obstacles.Add(new Decoration(new Vector3(0, 5f, z), new Vector3(x * 2 + 1f, 0.9f, 0.3f), new Color(255, 196, 0)));
        }

        // ---- patterns: each returns how much track (in metres) it used ----

        static float WallRow(World world, Random rng, float d, float t)
        {
            float gap = MathHelper.Lerp(3.6f, 2.3f, t);
            float gapX = (float)(rng.NextDouble() * 2 - 1) * (world.HalfWidth - gap / 2 - 0.5f);
            AddWallSegment(world, -world.HalfWidth, gapX - gap / 2, d);
            AddWallSegment(world, gapX + gap / 2, world.HalfWidth, d);
            return 1f;
        }

        static void AddWallSegment(World world, float x0, float x1, float d)
        {
            float width = x1 - x0;
            if (width < 0.4f) return;
            // like the guide's Wall: 1 high, 1 thick, long
            world.Obstacles.Add(new Wall(new Vector3((x0 + x1) / 2, 0.5f, -d), new Vector3(width, 1f, 1f), WallOrange));
        }

        static float ZigZag(World world, Random rng, float d, float t)
        {
            float gap = MathHelper.Lerp(3.4f, 2.4f, t);
            bool leftFirst = rng.Next(2) == 0;
            float side = world.HalfWidth - gap / 2 - 0.6f;
            for (int i = 0; i < 3; i++)
            {
                float gapX = ((i % 2 == 0) == leftFirst) ? -side : side;
                AddWallSegment(world, -world.HalfWidth, gapX - gap / 2, d + i * 5.5f);
                AddWallSegment(world, gapX + gap / 2, world.HalfWidth, d + i * 5.5f);
            }
            return 11f;
        }

        static float Buildings(World world, Random rng, float d, float t)
        {
            // five lanes; two random lanes stay free
            float[] lanes = { -5.6f, -2.8f, 0f, 2.8f, 5.6f };
            int freeA = rng.Next(5), freeB = (freeA + 2 + rng.Next(2)) % 5;
            for (int i = 0; i < lanes.Length; i++)
            {
                if (i == freeA || i == freeB) continue;
                float h = 2f + (float)rng.NextDouble() * 2.5f;
                float dz = (float)(rng.NextDouble() * 2 - 1) * 1.5f;
                Color c = rng.Next(2) == 0 ? BuildingBlue : BuildingTeal;
                // like the guide's "Small building": scale (2, 3, 2)
                world.Obstacles.Add(new Wall(new Vector3(lanes[i], h / 2, -(d + dz)), new Vector3(2f, h, 2f), c));
            }
            return 3f;
        }

        static float SpinningThing(World world, Random rng, float d, float t)
        {
            float length = MathHelper.Lerp(7f, 9f, t);
            float speed = MathHelper.Lerp(70f, 150f, t) * (rng.Next(2) == 0 ? 1 : -1);
            float x = (float)(rng.NextDouble() * 2 - 1) * 1.2f;
            var spinner = new Spinner(new Vector3(x, 0.55f, -(d + length / 2)), length, speed, SpinnerRed)
            {
                Yaw = (float)rng.NextDouble() * MathHelper.TwoPi,
            };
            world.Obstacles.Add(spinner);
            return length;
        }

        static float Droppers(World world, Random rng, float d, float t)
        {
            int count = 2 + (int)MathF.Round(t * 2f) + rng.Next(2);
            float used = 0f;
            for (int i = 0; i < count; i++)
            {
                float x = (float)(rng.NextDouble() * 2 - 1) * (world.HalfWidth - 1.2f);
                float z = d + i * 3.2f + (float)rng.NextDouble();
                var dropper = new Dropper(new Vector3(x, 0, -z), 1.6f, DropperPurple)
                {
                    WakeDistance = MathHelper.Lerp(12f, 10f, t),
                    TimeToWait = 0.05f + (float)rng.NextDouble() * 0.35f,
                };
                world.Obstacles.Add(dropper);
                used = z - d;
            }
            return used + 1.6f;
        }

        static float ProjectileTrap(World world, Random rng, float d, float t, int level)
        {
            var trigger = new TriggerProjectile
            {
                Center = new Vector3(0, 0, -d),
                HalfSize = new Vector2(world.HalfWidth, 0.9f),
            };
            float speed = MathHelper.Lerp(6.5f, 11f, t);
            int count = 5; // the guide uses five projectiles
            float spread = world.HalfWidth - 1f;
            float ahead = MathHelper.Lerp(13f, 11f, t);
            for (int i = 0; i < count; i++)
            {
                float x = -spread + 2 * spread * i / (count - 1) + (float)(rng.NextDouble() - 0.5);
                var start = new Vector3(x, 1f, -(d + ahead + (float)rng.NextDouble() * 2f));
                var projectile = new FlyAtPlayer(start, speed * (0.85f + (float)rng.NextDouble() * 0.3f), ProjectileYellow);
                world.Obstacles.Add(projectile);
                trigger.Projectiles.Add(projectile);
                // a small launch pad so the player can see where the shots will come from
                world.Obstacles.Add(new Decoration(new Vector3(start.X, 0.25f, start.Z), new Vector3(0.9f, 0.5f, 0.9f), new Color(66, 66, 80), ShapeKind.Cylinder, true));
            }
            world.Triggers.Add(trigger);
            return ahead + 2f;
        }

        static float Sliders(World world, Random rng, float d, float t)
        {
            int count = 2;
            for (int i = 0; i < count; i++)
            {
                float speed = MathHelper.Lerp(1.2f, 2.2f, t);
                world.Obstacles.Add(new Slider(new Vector3(0, 0.75f, -(d + i * 5f)), new Vector3(3.2f, 1.5f, 1f),
                    world.HalfWidth - 2.2f, speed, (float)rng.NextDouble() * MathHelper.TwoPi + i * MathHelper.Pi, SliderPink));
            }
            return 5f;
        }
    }
}
