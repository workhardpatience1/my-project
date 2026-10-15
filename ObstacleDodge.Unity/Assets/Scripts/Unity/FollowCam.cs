using System;
using UnityEngine;
using Xna = Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    /// <summary>The phone version's FollowCamera: smooth follow from behind and above, orbit in the menu, shake.</summary>
    public sealed class FollowCam
    {
        Xna.Vector3 position = new Xna.Vector3(0, 9, 10);
        Xna.Vector3 target = new Xna.Vector3(0, 0, -4);
        readonly System.Random random = new System.Random(1);
        float orbit;

        public void SnapTo(World world) => Desired(world, out position, out target);

        public void Follow(World world, float dt)
        {
            Desired(world, out var pos, out var tgt);
            float k = 1f - MathF.Exp(-6f * dt);
            position = Xna.Vector3.Lerp(position, pos, k);
            target = Xna.Vector3.Lerp(target, tgt, k);
        }

        public void Orbit(float dt)
        {
            orbit += dt * 0.15f;
            var center = new Xna.Vector3(0, 0, -14f);
            var pos = center + new Xna.Vector3(MathF.Sin(orbit) * 16f, 9f, MathF.Cos(orbit) * 16f);
            float k = 1f - MathF.Exp(-3f * dt);
            position = Xna.Vector3.Lerp(position, pos, k);
            target = Xna.Vector3.Lerp(target, center, k);
        }

        static void Desired(World world, out Xna.Vector3 pos, out Xna.Vector3 tgt)
        {
            var p = world.Player.Position;
            tgt = new Xna.Vector3(p.X * 0.55f, 0.6f, p.Z - 4.5f);
            pos = tgt + new Xna.Vector3(0, 9.5f, 11.5f);
        }

        public void Apply(Camera cam, float shake)
        {
            var offset = Xna.Vector3.Zero;
            if (shake > 0)
            {
                float s = shake * 0.8f;
                offset = new Xna.Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, 0) * s;
            }
            cam.transform.position = Cv.U(position + offset);
            cam.transform.LookAt(Cv.U(target + offset * 0.5f), Vector3.up);
            float aspect = cam.aspect;
            cam.fieldOfView = aspect >= 1.2f ? 50f : aspect >= 0.8f ? 62f : 78f;
        }
    }
}
