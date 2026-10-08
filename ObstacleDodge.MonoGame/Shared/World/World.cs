using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    public enum SoundKind { Hit, Land, Finish, Launch, Click, Lose }

    public sealed class Particle
    {
        public Vector3 Position, Velocity;
        public Color Color;
        public float Life, MaxLife, Size, Spin;
    }

    /// <summary>One level: the player, the obstacles, the traps and the finish line.</summary>
    public sealed class World
    {
        public readonly Mover Player = new Mover();
        public readonly List<Obstacle> Obstacles = new List<Obstacle>();
        public readonly List<TriggerProjectile> Triggers = new List<TriggerProjectile>();
        public readonly List<Particle> Particles = new List<Particle>();

        public int LevelNumber;
        public float HalfWidth = 7f;
        public float Length = 80f;
        public float FinishZ => -Length;
        public float Time;
        public float Shake;
        public bool Finished;
        public Color GroundA = new Color(110, 190, 90), GroundB = new Color(96, 172, 78);
        public Color Grass = new Color(70, 140, 60);
        public Color Sky = new Color(135, 200, 245);

        /// <summary>The game plugs its sound player in here.</summary>
        public Action<SoundKind> PlaySound;

        readonly Random random;

        public World(int seed) { random = new Random(seed); }

        public float Progress => MathHelper.Clamp(-Player.Position.Z / Length, 0f, 1f);

        public void Update(float dt, Vector2 input, bool simulatePlayer)
        {
            Time += dt;

            for (int i = 0; i < Obstacles.Count; i++) Obstacles[i].Update(this, dt);

            if (simulatePlayer && !Finished)
            {
                foreach (var trigger in Triggers)
                {
                    if (!trigger.Fired && trigger.Contains(Player.Position))
                    {
                        trigger.Fire(Player.Position);
                        PlaySound?.Invoke(SoundKind.Launch);
                    }
                }

                Player.MovePlayer(input, dt);
                ResolveCollisions();
                ClampToTrack();

                if (Player.Position.Z <= FinishZ)
                {
                    Finished = true;
                    Confetti(new Vector3(Player.Position.X, 1.5f, FinishZ));
                    PlaySound?.Invoke(SoundKind.Finish);
                    GameManager.Instance?.CompleteLevel();
                }
            }

            UpdateParticles(dt);
            Shake = MathF.Max(0f, Shake - dt);
            Obstacles.RemoveAll(o => o.Destroyed);
        }

        void ResolveCollisions()
        {
            for (int iteration = 0; iteration < 3; iteration++)
            {
                bool touched = false;
                for (int i = 0; i < Obstacles.Count; i++)
                {
                    var o = Obstacles[i];
                    if (!o.Collide(Player.Position, Mover.Radius, Mover.Height, out Vector3 push)) continue;

                    if (!(o is FlyAtPlayer))
                    {
                        touched = true;
                        Player.Position += push;
                        if (push.LengthSquared() > 1e-8f)
                        {
                            Vector3 n = Vector3.Normalize(push);
                            float into = Vector3.Dot(Player.Velocity, n);
                            if (into < 0) Player.Velocity -= n * into;
                        }
                    }
                    if (Player.Invulnerable <= 0f) o.OnPlayerCollision(this);
                }
                if (!touched) break;
            }
        }

        void ClampToTrack()
        {
            float limit = HalfWidth - Mover.Radius;
            Player.Position.X = MathHelper.Clamp(Player.Position.X, -limit, limit);
            Player.Position.Z = MathHelper.Clamp(Player.Position.Z, FinishZ - 2f, 1.5f);
            Player.Position.Y = 0f;
        }

        /// <summary>Called by an obstacle the first time the player touches it.</summary>
        public void ReportHit(Obstacle obstacle)
        {
            Vector3 away = Player.Position - obstacle.Position;
            away.Y = 0;
            if (away.LengthSquared() < 1e-4f) away = Vector3.UnitZ;
            away.Normalize();
            Player.Knockback = away * 6f;
            Player.HitBlink = 0.6f;
            Shake = 0.3f;
            Burst(Player.Position + new Vector3(0, 1.2f, 0), obstacle.Color, 12, 5f);
            PlaySound?.Invoke(SoundKind.Hit);
            GameManager.Instance?.RegisterHit();
        }

        public void OnDropperLanded(Dropper dropper)
        {
            Burst(new Vector3(dropper.Position.X, 0.2f, dropper.Position.Z), new Color(160, 150, 140), 10, 4f);
            float distance = Vector3.Distance(dropper.Position, Player.Position);
            if (distance < 8f)
            {
                Shake = MathF.Max(Shake, 0.18f);
                PlaySound?.Invoke(SoundKind.Land);
            }
        }

        public void Burst(Vector3 position, Color color, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var dir = new Vector3((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 1.2f + 0.2f, (float)random.NextDouble() * 2 - 1);
                Particles.Add(new Particle
                {
                    Position = position,
                    Velocity = dir * speed * (0.4f + (float)random.NextDouble() * 0.6f),
                    Color = color,
                    Life = 0.6f + (float)random.NextDouble() * 0.4f,
                    MaxLife = 1f,
                    Size = 0.12f + (float)random.NextDouble() * 0.12f,
                    Spin = (float)random.NextDouble() * 10f,
                });
            }
        }

        public void Confetti(Vector3 position)
        {
            Color[] colors = { Color.Gold, Color.DeepSkyBlue, Color.HotPink, Color.LimeGreen, Color.OrangeRed, Color.White };
            for (int i = 0; i < 90; i++)
            {
                var dir = new Vector3((float)random.NextDouble() * 2 - 1, 1.2f + (float)random.NextDouble() * 1.5f, (float)random.NextDouble() * 2 - 1);
                Particles.Add(new Particle
                {
                    Position = position + new Vector3((float)random.NextDouble() * 8 - 4, 0, 0),
                    Velocity = dir * (4f + (float)random.NextDouble() * 4f),
                    Color = colors[i % colors.Length],
                    Life = 1.6f + (float)random.NextDouble() * 1.2f,
                    MaxLife = 2.8f,
                    Size = 0.15f + (float)random.NextDouble() * 0.1f,
                    Spin = (float)random.NextDouble() * 12f,
                });
            }
        }

        void UpdateParticles(float dt)
        {
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                var p = Particles[i];
                p.Life -= dt;
                if (p.Life <= 0) { Particles.RemoveAt(i); continue; }
                p.Velocity.Y -= 12f * dt;
                p.Velocity *= MathF.Exp(-1.2f * dt);
                p.Position += p.Velocity * dt;
                if (p.Position.Y < p.Size * 0.5f)
                {
                    p.Position.Y = p.Size * 0.5f;
                    p.Velocity.Y *= -0.3f;
                    p.Velocity.X *= 0.7f;
                    p.Velocity.Z *= 0.7f;
                }
            }
        }
    }
}
