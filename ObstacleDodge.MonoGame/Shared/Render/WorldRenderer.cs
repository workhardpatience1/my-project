using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ObstacleDodge
{
    /// <summary>Draws a <see cref="World"/>: ground, obstacles, the player, shadows and particles.</summary>
    public sealed class WorldRenderer : IDisposable
    {
        readonly GraphicsDevice device;
        readonly BasicEffect lit;
        readonly BasicEffect flat;
        readonly Mesh cube, sphere, cylinder, capsule, disc;
        Mesh ground;
        World groundFor;

        static readonly Color PlayerBlue = new Color(33, 150, 243);
        static readonly RasterizerState NoCull = new RasterizerState { CullMode = CullMode.None };

        public WorldRenderer(GraphicsDevice device)
        {
            this.device = device;
            cube = MeshBuilder.Cube(device);
            sphere = MeshBuilder.Sphere(device);
            cylinder = MeshBuilder.Cylinder(device);
            capsule = MeshBuilder.Capsule(device);
            disc = MeshBuilder.Disc(device);

            lit = new BasicEffect(device)
            {
                VertexColorEnabled = true,
                LightingEnabled = true,
                PreferPerPixelLighting = false,
                AmbientLightColor = new Vector3(0.48f, 0.48f, 0.52f),
                SpecularPower = 24f,
                SpecularColor = new Vector3(0.08f),
                FogEnabled = true,
                FogStart = 38f,
                FogEnd = 95f,
            };
            lit.DirectionalLight0.Enabled = true;
            lit.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-0.45f, -1f, -0.35f));
            lit.DirectionalLight0.DiffuseColor = new Vector3(0.62f, 0.6f, 0.55f);
            lit.DirectionalLight0.SpecularColor = new Vector3(0.15f);
            lit.DirectionalLight1.Enabled = true;
            lit.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0.6f, -0.4f, 0.7f));
            lit.DirectionalLight1.DiffuseColor = new Vector3(0.18f, 0.2f, 0.25f);
            lit.DirectionalLight1.SpecularColor = Vector3.Zero;
            lit.DirectionalLight2.Enabled = false;

            flat = new BasicEffect(device)
            {
                VertexColorEnabled = true,
                LightingEnabled = false,
                FogEnabled = true,
                FogStart = 38f,
                FogEnd = 95f,
            };
        }

        public void Draw(World world, FollowCamera camera)
        {
            if (groundFor != world)
            {
                ground?.Dispose();
                ground = MeshBuilder.Ground(device, world);
                groundFor = world;
            }

            device.RasterizerState = NoCull;
            device.DepthStencilState = DepthStencilState.Default;
            device.BlendState = BlendState.Opaque;

            foreach (var effect in new[] { lit, flat })
            {
                effect.View = camera.View;
                effect.Projection = camera.Projection;
                effect.FogColor = world.Sky.ToVector3();
            }

            float viewMinZ = camera.Position.Z - 105f, viewMaxZ = camera.Position.Z + 6f;

            // ground
            DrawLit(ground, Matrix.Identity, Color.White);

            // obstacles
            foreach (var o in world.Obstacles)
            {
                if (!o.Visible || o.Destroyed) continue;
                if (o.Position.Z + o.Size.Length() < viewMinZ || o.Position.Z - o.Size.Length() > viewMaxZ) continue;
                if (o is Decoration deco && deco.Alpha < 1f) continue;
                DrawObstacle(o);
            }

            DrawPlayer(world);

            // particles: tiny spinning cubes
            foreach (var p in world.Particles)
            {
                float scale = p.Size * MathHelper.Clamp(p.Life / 0.3f, 0f, 1f);
                var m = Matrix.CreateScale(scale) * Matrix.CreateRotationX(p.Spin * p.Life) * Matrix.CreateRotationY(p.Spin * 0.7f * p.Life) * Matrix.CreateTranslation(p.Position);
                DrawLit(cube, m, p.Color);
            }

            // transparent things last: trigger zones and soft shadows
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.DepthRead;

            foreach (var trigger in world.Triggers)
            {
                if (trigger.Fired) continue;
                float pulse = 0.28f + 0.12f * MathF.Sin(world.Time * 6f);
                var size = new Vector3(trigger.HalfSize.X * 2f, 1f, trigger.HalfSize.Y * 2f);
                DrawFlat(cube, Matrix.CreateScale(size.X, 0.02f, size.Z) * Matrix.CreateTranslation(trigger.Center + new Vector3(0, 0.03f, 0)), new Color(229, 57, 53), pulse);
                // stripes
                for (float x = -trigger.HalfSize.X + 0.6f; x < trigger.HalfSize.X; x += 1.4f)
                    DrawFlat(cube, Matrix.CreateScale(0.5f, 0.02f, size.Z * 0.9f) * Matrix.CreateRotationY(0.5f) * Matrix.CreateTranslation(trigger.Center + new Vector3(x, 0.05f, 0)), new Color(255, 235, 59), 0.5f);
            }

            // player shadow
            Shadow(world.Player.Position, 1.1f, 0.32f);
            foreach (var o in world.Obstacles)
            {
                if (o.Destroyed) continue;
                if (o is Dropper dropper)
                {
                    if (dropper.IsFalling)
                    {
                        float k = dropper.FallProgress;
                        Shadow(dropper.Position, dropper.Size.X * (0.6f + 0.8f * k), 0.12f + 0.4f * k);
                    }
                    continue;
                }
                if (!o.Visible || !o.CastsShadow) continue;
                if (o is FlyAtPlayer)
                    Shadow(o.Position, 0.8f, 0.25f);
            }

            device.BlendState = BlendState.Opaque;
            device.DepthStencilState = DepthStencilState.Default;
        }

        void DrawObstacle(Obstacle o)
        {
            Mesh mesh = o.Shape switch
            {
                ShapeKind.Sphere => sphere,
                ShapeKind.Cylinder => cylinder,
                _ => cube,
            };
            DrawLit(mesh, o.WorldMatrix, o.DrawColor);

            if (o is Spinner s)
            {
                // the pillar and a cap in the middle
                float h = s.Position.Y + 0.6f;
                DrawLit(cylinder, Matrix.CreateScale(s.PillarRadius * 2f, h, s.PillarRadius * 2f) * Matrix.CreateTranslation(s.Position.X, h / 2f, s.Position.Z), new Color(84, 84, 96));
                DrawLit(cylinder, Matrix.CreateScale(1.1f, 0.25f, 1.1f) * Matrix.CreateTranslation(s.Position.X, s.Position.Y + 0.45f, s.Position.Z), s.IsHit ? s.DrawColor : new Color(255, 213, 79));
            }
            else if (o is FlyAtPlayer)
            {
                // a dark core so the shot reads as a "ball"
                DrawLit(sphere, Matrix.CreateScale(o.Size * 0.55f) * Matrix.CreateTranslation(o.Position + new Vector3(0, 0, 0.18f)), new Color(255, 111, 0));
            }
        }

        void DrawPlayer(World world)
        {
            var player = world.Player;
            if (player.Invulnerable > 0 && ((int)(player.Invulnerable * 10f)) % 2 == 0) return;

            float bob = MathF.Abs(MathF.Sin(player.WalkCycle * MathF.PI)) * 0.08f * MathHelper.Clamp(player.Velocity.Length() / 4f, 0f, 1f);
            float lean = MathHelper.Clamp(player.Velocity.Length() / player.MoveSpeed, 0f, 1f) * 0.12f;
            Matrix body = Matrix.CreateRotationX(lean) * Matrix.CreateRotationY(player.Yaw) * Matrix.CreateTranslation(player.Position + new Vector3(0, bob, 0));

            Color color = PlayerBlue;
            if (player.HitBlink > 0 && ((int)(player.HitBlink * 14f)) % 2 == 0) color = new Color(255, 82, 82);
            DrawLit(capsule, body, color);

            // eyes look where Dodgy walks (local +Z is the face)
            foreach (float side in new[] { -0.19f, 0.19f })
            {
                DrawLit(sphere, Matrix.CreateScale(0.3f, 0.36f, 0.22f) * Matrix.CreateTranslation(side, 1.52f, 0.4f) * body, Color.White);
                DrawLit(sphere, Matrix.CreateScale(0.15f, 0.19f, 0.1f) * Matrix.CreateTranslation(side, 1.5f, 0.5f) * body, new Color(25, 25, 30));
            }
            // a little smile
            DrawLit(cube, Matrix.CreateScale(0.22f, 0.05f, 0.05f) * Matrix.CreateTranslation(0, 1.22f, 0.48f) * body, new Color(13, 71, 161));
        }

        void Shadow(Vector3 position, float size, float alpha)
        {
            DrawFlat(disc, Matrix.CreateScale(size, 1f, size) * Matrix.CreateTranslation(position.X, 0.07f, position.Z), Color.Black, alpha);
        }

        void DrawLit(Mesh mesh, Matrix world, Color color)
        {
            lit.World = world;
            lit.DiffuseColor = color.ToVector3();
            lit.Alpha = 1f;
            lit.CurrentTechnique.Passes[0].Apply();
            mesh.Draw(device);
        }

        void DrawFlat(Mesh mesh, Matrix world, Color color, float alpha)
        {
            flat.World = world;
            flat.DiffuseColor = color.ToVector3();
            flat.Alpha = alpha;
            flat.CurrentTechnique.Passes[0].Apply();
            mesh.Draw(device);
        }

        public void Dispose()
        {
            cube.Dispose(); sphere.Dispose(); cylinder.Dispose(); capsule.Dispose(); disc.Dispose();
            ground?.Dispose();
            lit.Dispose(); flat.Dispose();
        }
    }
}
