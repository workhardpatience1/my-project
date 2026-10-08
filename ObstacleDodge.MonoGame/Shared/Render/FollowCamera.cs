using System;
using Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    /// <summary>
    /// A camera that follows the player smoothly from behind and above
    /// (the "Cinemachine" upgrade suggested in the guide, done by hand).
    /// </summary>
    public sealed class FollowCamera
    {
        public Vector3 Position = new Vector3(0, 9, 10);
        public Vector3 Target = new Vector3(0, 0, -4);
        public Matrix View, Projection;
        readonly Random random = new Random(1);
        float orbit;

        public void SnapTo(World world)
        {
            ComputeDesired(world, out Position, out Target);
        }

        public void Follow(World world, float dt)
        {
            ComputeDesired(world, out Vector3 pos, out Vector3 target);
            float k = 1f - MathF.Exp(-6f * dt);
            Position = Vector3.Lerp(Position, pos, k);
            Target = Vector3.Lerp(Target, target, k);
        }

        /// <summary>Slow flight around the start of the level for the main menu.</summary>
        public void Orbit(World world, float dt)
        {
            orbit += dt * 0.15f;
            var center = new Vector3(0, 0, -14f);
            var pos = center + new Vector3(MathF.Sin(orbit) * 16f, 9f, MathF.Cos(orbit) * 16f);
            float k = 1f - MathF.Exp(-3f * dt);
            Position = Vector3.Lerp(Position, pos, k);
            Target = Vector3.Lerp(Target, center, k);
        }

        static void ComputeDesired(World world, out Vector3 pos, out Vector3 target)
        {
            Vector3 p = world.Player.Position;
            target = new Vector3(p.X * 0.55f, 0.6f, p.Z - 4.5f);
            pos = target + new Vector3(0, 9.5f, 11.5f);
        }

        public void BuildMatrices(float aspect, float shake)
        {
            Vector3 offset = Vector3.Zero;
            if (shake > 0)
            {
                float s = shake * 0.8f;
                offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, 0) * s;
            }
            View = Matrix.CreateLookAt(Position + offset, Target + offset * 0.5f, Vector3.Up);
            // A tall (portrait) screen needs a wider vertical angle to show the whole track.
            float fov = aspect >= 1.2f ? 50f : aspect >= 0.8f ? 62f : 78f;
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fov), aspect, 0.1f, 140f);
        }
    }
}
