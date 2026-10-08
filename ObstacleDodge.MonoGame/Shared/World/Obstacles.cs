using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    /// <summary>A still wall or a small building (guide 5.2 and 5.4): just a box with ObjectHit.</summary>
    public sealed class Wall : Obstacle
    {
        public Wall(Vector3 position, Vector3 size, Color color, float yaw = 0f)
        {
            Position = position;
            Size = size;
            Color = color;
            Yaw = yaw;
        }
    }

    /// <summary>Something to look at that never collides (rails, finish gate, launch pads...).</summary>
    public sealed class Decoration : Obstacle
    {
        public float Alpha = 1f;

        public Decoration(Vector3 position, Vector3 size, Color color, ShapeKind shape = ShapeKind.Box, bool shadow = false)
        {
            Position = position;
            Size = size;
            Color = color;
            Shape = shape;
            Solid = false;
            CastsShadow = shadow;
        }
    }

    /// <summary>
    /// Spinner.cs from the guide: spins around its own axis without stopping.
    /// Here the speed is in degrees per SECOND (multiplied by dt), so it does not depend on the FPS.
    /// </summary>
    public sealed class Spinner : Obstacle
    {
        public float DegreesPerSecond = 90f;
        public float PillarRadius = 0.45f;

        public Spinner(Vector3 center, float length, float degreesPerSecond, Color color)
        {
            Position = center;
            Size = new Vector3(length, 0.7f, 0.7f);
            DegreesPerSecond = degreesPerSecond;
            Color = color;
        }

        public override void Update(World world, float dt)
        {
            base.Update(world, dt);
            Yaw += MathHelper.ToRadians(DegreesPerSecond) * dt;
        }

        public override bool Collide(Vector3 player, float radius, float height, out Vector3 push)
        {
            if (base.Collide(player, radius, height, out push)) return true;
            // the pillar in the middle
            return Collision.CircleVsCircle(player, radius, Position, PillarRadius, out push);
        }
    }

    /// <summary>
    /// Dropper.cs from the guide: hidden and without gravity at first, then it appears and falls.
    /// The guide waits for Time.time; on a long level we start the timer when the player comes close.
    /// </summary>
    public sealed class Dropper : Obstacle
    {
        public float TimeToWait = 0.15f;
        public float WakeDistance = 11f;
        public const float StartHeight = 14f;
        const float Gravity = 32f;

        enum Stage { Waiting, Delay, Falling, Landed }
        Stage stage = Stage.Waiting;
        float timer;
        float velocityY;

        public bool IsFalling => stage == Stage.Falling;
        public float GroundY => Size.Y * 0.5f;

        public Dropper(Vector3 groundPosition, float size, Color color)
        {
            Size = new Vector3(size);
            Position = new Vector3(groundPosition.X, StartHeight, groundPosition.Z);
            Color = color;
            Visible = false; // myMeshRenderer.enabled = false
        }

        public override void Update(World world, float dt)
        {
            base.Update(world, dt);
            switch (stage)
            {
                case Stage.Waiting:
                    // forward is -Z: the player is "before" the dropper while player.Z > Position.Z
                    if (world.Player.Position.Z - Position.Z < WakeDistance) stage = Stage.Delay;
                    break;
                case Stage.Delay:
                    timer += dt;
                    if (timer > TimeToWait)
                    {
                        Visible = true;   // myMeshRenderer.enabled = true
                        stage = Stage.Falling; // myRigidBody.useGravity = true
                    }
                    break;
                case Stage.Falling:
                    velocityY -= Gravity * dt;
                    Position.Y += velocityY * dt;
                    if (Position.Y <= GroundY)
                    {
                        Position.Y = GroundY;
                        stage = Stage.Landed;
                        world.OnDropperLanded(this);
                    }
                    break;
            }
        }

        /// <summary>0 = just started falling, 1 = on the ground (used for the warning shadow).</summary>
        public float FallProgress => 1f - MathHelper.Clamp((Position.Y - GroundY) / (StartHeight - GroundY), 0f, 1f);
    }

    /// <summary>
    /// FlyAtPlayer.cs from the guide: sleeps until a trap wakes it up, then flies to the spot
    /// where the player stood at that moment and destroys itself when it gets there.
    /// </summary>
    public sealed class FlyAtPlayer : Obstacle
    {
        public float Speed = 7f;
        Vector3 playerPosition;
        bool flying;

        public bool IsFlying => flying;

        public FlyAtPlayer(Vector3 position, float speed, Color color)
        {
            Position = position;
            Speed = speed;
            Color = color;
            Shape = ShapeKind.Sphere;
            Size = new Vector3(0.75f);
            Visible = false; // gameObject.SetActive(false) in Awake()
        }

        public void Launch(Vector3 player)
        {
            playerPosition = new Vector3(player.X, Position.Y, player.Z);
            Visible = true;
            flying = true;
        }

        public override void Update(World world, float dt)
        {
            base.Update(world, dt);
            if (!flying) return;
            Vector3 toTarget = playerPosition - Position;
            float step = Speed * dt;
            if (toTarget.Length() <= step)
            {
                Position = playerPosition;
                Destroyed = true; // DestroyWhenReached
                world.Burst(Position, Color, 6, 2.5f);
                return;
            }
            Position += Vector3.Normalize(toTarget) * step;
        }

        public override void OnPlayerCollision(World world)
        {
            base.OnPlayerCollision(world);
            Destroyed = true;
            world.Burst(Position, Color, 14, 5f);
        }
    }

    /// <summary>An extra obstacle for later levels: a wall that slides left and right.</summary>
    public sealed class Slider : Obstacle
    {
        public float BaseX, Amplitude, Speed, Phase;

        public Slider(Vector3 position, Vector3 size, float amplitude, float speed, float phase, Color color)
        {
            Position = position;
            Size = size;
            BaseX = position.X;
            Amplitude = amplitude;
            Speed = speed;
            Phase = phase;
            Color = color;
        }

        public override void Update(World world, float dt)
        {
            base.Update(world, dt);
            Position.X = BaseX + Amplitude * MathF.Sin(world.Time * Speed + Phase);
        }
    }

    /// <summary>
    /// TriggerProjectile.cs from the guide: an invisible zone (we paint it red on the ground so the
    /// player can see the danger). When the player enters, all its projectiles fly, and the zone is removed.
    /// Instead of five separate fields it keeps a list (the exercise from the guide).
    /// </summary>
    public sealed class TriggerProjectile
    {
        public Vector3 Center;
        public Vector2 HalfSize; // X: half width, Y: half depth (along Z)
        public readonly List<FlyAtPlayer> Projectiles = new List<FlyAtPlayer>();
        public bool Fired;

        public bool Contains(Vector3 p) =>
            Math.Abs(p.X - Center.X) <= HalfSize.X && Math.Abs(p.Z - Center.Z) <= HalfSize.Y;

        public void Fire(Vector3 playerPosition)
        {
            foreach (var p in Projectiles) p.Launch(playerPosition);
            Fired = true; // Destroy(gameObject)
        }
    }
}
