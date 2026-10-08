using System;
using Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    /// <summary>
    /// "Dodgy The Player" with the logic of Mover.cs from the guide: keyboard + on-screen buttons
    /// give a direction, and the character moves with moveSpeed metres per second
    /// (multiplied by the frame time, so the speed does not depend on the FPS).
    /// </summary>
    public sealed class Mover
    {
        public const float Radius = 0.5f;   // Capsule Collider radius
        public const float Height = 2f;     // Capsule height

        public float MoveSpeed = 8f;
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Knockback;
        public float Yaw = MathHelper.Pi;   // facing forward (-Z)
        public float Invulnerable;          // seconds without new hits (after "continue")
        public float HitBlink;              // seconds of red blinking after a hit
        public float WalkCycle;

        public void Reset(Vector3 position)
        {
            Position = position;
            Velocity = Knockback = Vector3.Zero;
            Yaw = MathHelper.Pi;
            Invulnerable = HitBlink = WalkCycle = 0f;
        }

        /// <param name="input">X: -1 left .. 1 right, Y: -1 back .. 1 forward (already clamped).</param>
        public void MovePlayer(Vector2 input, float dt)
        {
            // Forward on the screen is -Z in MonoGame (right-handed coordinates).
            var wish = new Vector3(input.X, 0f, -input.Y);
            if (wish.LengthSquared() > 1f) wish.Normalize();

            // A short acceleration makes the movement feel smooth instead of "jumpy".
            float blend = 1f - MathF.Exp(-14f * dt);
            Velocity = Vector3.Lerp(Velocity, wish * MoveSpeed, blend);
            Position += (Velocity + Knockback) * dt;
            Knockback *= MathF.Exp(-7f * dt);

            if (wish.LengthSquared() > 0.01f)
            {
                float targetYaw = MathF.Atan2(wish.X, wish.Z);
                Yaw = LerpAngle(Yaw, targetYaw, 1f - MathF.Exp(-12f * dt));
            }

            if (Invulnerable > 0) Invulnerable -= dt;
            if (HitBlink > 0) HitBlink -= dt;
            WalkCycle += Velocity.Length() * dt * 1.6f;
        }

        static float LerpAngle(float from, float to, float t)
        {
            float diff = MathHelper.WrapAngle(to - from);
            return MathHelper.WrapAngle(from + diff * t);
        }
    }
}
