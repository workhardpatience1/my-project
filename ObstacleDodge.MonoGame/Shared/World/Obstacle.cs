using System;
using Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    public enum ShapeKind { Box, Sphere, Cylinder }

    /// <summary>
    /// Base class of every obstacle. It contains the logic of ObjectHit.cs from the guide:
    /// when the player touches it for the first time, a hit is registered in GameManager,
    /// the obstacle turns black and gets the "Hit" mark, so it is never counted again.
    /// </summary>
    public abstract class Obstacle
    {
        public Vector3 Position;            // center of the object
        public Vector3 Size = Vector3.One;  // full size (for a sphere/cylinder: X = diameter)
        public float Yaw;                   // rotation around the vertical axis, radians
        public Color Color = Color.Orange;
        public ShapeKind Shape = ShapeKind.Box;

        public bool Visible = true;
        public bool Solid = true;
        public bool Destroyed;
        public bool CastsShadow = true;

        /// <summary>The "Hit" tag from the guide: true after the first touch.</summary>
        public bool IsHit;
        float flash;

        public Color DrawColor
        {
            get
            {
                Color c = IsHit ? new Color(34, 34, 38) : Color;
                return flash > 0 ? Color.Lerp(c, Color.White, MathHelper.Clamp(flash / 0.25f, 0f, 1f)) : c;
            }
        }

        public virtual Matrix WorldMatrix =>
            Matrix.CreateScale(Size) * Matrix.CreateRotationY(Yaw) * Matrix.CreateTranslation(Position);

        public virtual void Update(World world, float dt)
        {
            if (flash > 0) flash -= dt;
        }

        /// <summary>ObjectHit.OnCollisionEnter: the player touched this obstacle.</summary>
        public virtual void OnPlayerCollision(World world)
        {
            if (!IsHit)
            {
                IsHit = true;
                flash = 0.25f;
                world.ReportHit(this);
            }
        }

        /// <summary>
        /// Does the player's capsule (circle on the ground, given height) touch this obstacle?
        /// <paramref name="push"/> is how far to move the player so they no longer overlap.
        /// </summary>
        public virtual bool Collide(Vector3 player, float radius, float height, out Vector3 push)
        {
            push = Vector3.Zero;
            if (!Solid || Destroyed || !Visible) return false;
            float bottom = Position.Y - Size.Y * 0.5f, top = Position.Y + Size.Y * 0.5f;
            if (bottom > player.Y + height || top < player.Y) return false;

            switch (Shape)
            {
                case ShapeKind.Sphere:
                case ShapeKind.Cylinder:
                    return Collision.CircleVsCircle(player, radius, Position, Size.X * 0.5f, out push);
                default:
                    return Collision.CircleVsBox(player, radius, Position, Size * 0.5f, Yaw, out push);
            }
        }
    }

    /// <summary>Collision math on the ground plane (X and Z). The height is checked separately.</summary>
    public static class Collision
    {
        public static bool CircleVsCircle(Vector3 a, float ra, Vector3 b, float rb, out Vector3 push)
        {
            var d = new Vector2(a.X - b.X, a.Z - b.Z);
            float r = ra + rb;
            float lenSq = d.LengthSquared();
            if (lenSq >= r * r) { push = Vector3.Zero; return false; }
            float len = MathF.Sqrt(lenSq);
            Vector2 n = len > 1e-5f ? d / len : Vector2.UnitX;
            push = new Vector3(n.X, 0, n.Y) * (r - len);
            return true;
        }

        public static bool CircleVsBox(Vector3 p, float r, Vector3 center, Vector3 half, float yaw, out Vector3 push)
        {
            // Move the player into the box's own (not rotated) space.
            Vector3 local = Vector3.Transform(new Vector3(p.X - center.X, 0, p.Z - center.Z), Matrix.CreateRotationY(-yaw));
            float cx = MathHelper.Clamp(local.X, -half.X, half.X);
            float cz = MathHelper.Clamp(local.Z, -half.Z, half.Z);
            float dx = local.X - cx, dz = local.Z - cz;
            float distSq = dx * dx + dz * dz;
            if (distSq >= r * r) { push = Vector3.Zero; return false; }

            Vector3 normal;
            float depth;
            if (distSq > 1e-8f)
            {
                float dist = MathF.Sqrt(distSq);
                normal = new Vector3(dx / dist, 0, dz / dist);
                depth = r - dist;
            }
            else
            {
                // The center is inside the box: leave by the closest side.
                float px = half.X - MathF.Abs(local.X);
                float pz = half.Z - MathF.Abs(local.Z);
                if (px < pz) { normal = new Vector3(local.X < 0 ? -1 : 1, 0, 0); depth = px + r; }
                else { normal = new Vector3(0, 0, local.Z < 0 ? -1 : 1); depth = pz + r; }
            }
            push = Vector3.Transform(normal, Matrix.CreateRotationY(yaw)) * depth;
            return true;
        }
    }
}
