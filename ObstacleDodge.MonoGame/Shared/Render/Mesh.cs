using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ObstacleDodge
{
    /// <summary>A vertex with position, normal (for light) and color (for the checkered ground).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionNormalColor : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;

        public static readonly VertexDeclaration Declaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0));

        public VertexPositionNormalColor(Vector3 position, Vector3 normal, Color color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }

        VertexDeclaration IVertexType.VertexDeclaration => Declaration;
    }

    public sealed class Mesh : IDisposable
    {
        public VertexBuffer Vertices;
        public IndexBuffer Indices;
        public int PrimitiveCount;

        public void Draw(GraphicsDevice device)
        {
            device.SetVertexBuffer(Vertices);
            device.Indices = Indices;
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, PrimitiveCount);
        }

        public void Dispose()
        {
            Vertices?.Dispose();
            Indices?.Dispose();
        }
    }

    /// <summary>Builds the simple shapes Unity gives you with "3D Object →" (Cube, Sphere, Capsule...).</summary>
    public sealed class MeshBuilder
    {
        readonly List<VertexPositionNormalColor> vertices = new List<VertexPositionNormalColor>();
        readonly List<int> indices = new List<int>();

        public int VertexCount => vertices.Count;

        public int Add(Vector3 p, Vector3 n, Color c)
        {
            vertices.Add(new VertexPositionNormalColor(p, n, c));
            return vertices.Count - 1;
        }

        public void Triangle(int a, int b, int c)
        {
            indices.Add(a); indices.Add(b); indices.Add(c);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, Color color)
        {
            int i0 = Add(a, n, color), i1 = Add(b, n, color), i2 = Add(c, n, color), i3 = Add(d, n, color);
            Triangle(i0, i1, i2);
            Triangle(i0, i2, i3);
        }

        public Mesh Build(GraphicsDevice device)
        {
            var mesh = new Mesh
            {
                Vertices = new VertexBuffer(device, VertexPositionNormalColor.Declaration, vertices.Count, BufferUsage.WriteOnly),
                PrimitiveCount = indices.Count / 3,
            };
            mesh.Vertices.SetData(vertices.ToArray());
            if (vertices.Count > ushort.MaxValue)
            {
                mesh.Indices = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
                mesh.Indices.SetData(indices.ToArray());
            }
            else
            {
                var shorts = new ushort[indices.Count];
                for (int i = 0; i < shorts.Length; i++) shorts[i] = (ushort)indices[i];
                mesh.Indices = new IndexBuffer(device, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
                mesh.Indices.SetData(shorts);
            }
            return mesh;
        }

        // ---------- ready shapes (size 1, centered) ----------

        public static Mesh Cube(GraphicsDevice device)
        {
            var b = new MeshBuilder();
            Color w = Color.White;
            float h = 0.5f;
            b.Quad(new Vector3(-h, h, -h), new Vector3(h, h, -h), new Vector3(h, h, h), new Vector3(-h, h, h), Vector3.Up, w);
            b.Quad(new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, -h, -h), new Vector3(-h, -h, -h), Vector3.Down, w);
            b.Quad(new Vector3(-h, -h, h), new Vector3(-h, h, h), new Vector3(h, h, h), new Vector3(h, -h, h), Vector3.Backward, w);
            b.Quad(new Vector3(h, -h, -h), new Vector3(h, h, -h), new Vector3(-h, h, -h), new Vector3(-h, -h, -h), Vector3.Forward, w);
            b.Quad(new Vector3(h, -h, h), new Vector3(h, h, h), new Vector3(h, h, -h), new Vector3(h, -h, -h), Vector3.Right, w);
            b.Quad(new Vector3(-h, -h, -h), new Vector3(-h, h, -h), new Vector3(-h, h, h), new Vector3(-h, -h, h), Vector3.Left, w);
            return b.Build(device);
        }

        public static Mesh Sphere(GraphicsDevice device, int slices = 18, int stacks = 12)
        {
            var b = new MeshBuilder();
            for (int i = 0; i <= stacks; i++)
            {
                float v = (float)i / stacks;
                float phi = v * MathF.PI;
                for (int j = 0; j <= slices; j++)
                {
                    float theta = (float)j / slices * MathF.PI * 2f;
                    var n = new Vector3(MathF.Sin(phi) * MathF.Cos(theta), MathF.Cos(phi), MathF.Sin(phi) * MathF.Sin(theta));
                    b.Add(n * 0.5f, n, Color.White);
                }
            }
            for (int i = 0; i < stacks; i++)
                for (int j = 0; j < slices; j++)
                {
                    int a = i * (slices + 1) + j, c = a + slices + 1;
                    b.Triangle(a, c, a + 1);
                    b.Triangle(a + 1, c, c + 1);
                }
            return b.Build(device);
        }

        public static Mesh Cylinder(GraphicsDevice device, int slices = 20)
        {
            var b = new MeshBuilder();
            for (int j = 0; j <= slices; j++)
            {
                float theta = (float)j / slices * MathF.PI * 2f;
                var n = new Vector3(MathF.Cos(theta), 0, MathF.Sin(theta));
                b.Add(n * 0.5f + new Vector3(0, -0.5f, 0), n, Color.White);
                b.Add(n * 0.5f + new Vector3(0, 0.5f, 0), n, Color.White);
            }
            for (int j = 0; j < slices; j++)
            {
                int a = j * 2;
                b.Triangle(a, a + 1, a + 2);
                b.Triangle(a + 2, a + 1, a + 3);
            }
            foreach (float y in new[] { -0.5f, 0.5f })
            {
                var n = y > 0 ? Vector3.Up : Vector3.Down;
                int center = b.Add(new Vector3(0, y, 0), n, Color.White);
                int first = b.VertexCount;
                for (int j = 0; j <= slices; j++)
                {
                    float theta = (float)j / slices * MathF.PI * 2f;
                    b.Add(new Vector3(MathF.Cos(theta) * 0.5f, y, MathF.Sin(theta) * 0.5f), n, Color.White);
                }
                for (int j = 0; j < slices; j++) b.Triangle(center, first + j, first + j + 1);
            }
            return b.Build(device);
        }

        /// <summary>The player capsule: radius 0.5, height 2, standing on y = 0.</summary>
        public static Mesh Capsule(GraphicsDevice device, int slices = 20, int capStacks = 8)
        {
            var b = new MeshBuilder();
            const float r = 0.5f;
            // rings from the top pole to the bottom pole; the two hemispheres are 1 metre apart
            var rings = new List<(float y, float radius, float ny)>();
            for (int i = 0; i <= capStacks; i++)
            {
                float a = (float)i / capStacks * MathF.PI / 2f; // 0 = pole, pi/2 = equator
                rings.Add((1.5f + MathF.Cos(a) * r, MathF.Sin(a) * r, MathF.Cos(a)));
            }
            for (int i = 0; i <= capStacks; i++)
            {
                float a = (float)i / capStacks * MathF.PI / 2f;
                rings.Add((0.5f - MathF.Sin(a) * r, MathF.Cos(a) * r, -MathF.Sin(a)));
            }
            foreach (var ring in rings)
            {
                for (int j = 0; j <= slices; j++)
                {
                    float theta = (float)j / slices * MathF.PI * 2f;
                    float horizontal = MathF.Sqrt(MathF.Max(0f, 1f - ring.ny * ring.ny));
                    var n = new Vector3(MathF.Cos(theta) * horizontal, ring.ny, MathF.Sin(theta) * horizontal);
                    b.Add(new Vector3(MathF.Cos(theta) * ring.radius, ring.y, MathF.Sin(theta) * ring.radius), n, Color.White);
                }
            }
            for (int i = 0; i < rings.Count - 1; i++)
                for (int j = 0; j < slices; j++)
                {
                    int a = i * (slices + 1) + j, c = a + slices + 1;
                    b.Triangle(a, c, a + 1);
                    b.Triangle(a + 1, c, c + 1);
                }
            return b.Build(device);
        }

        /// <summary>A flat circle (diameter 1) facing up — used for soft shadows.</summary>
        public static Mesh Disc(GraphicsDevice device, int slices = 24)
        {
            var b = new MeshBuilder();
            int center = b.Add(Vector3.Zero, Vector3.Up, Color.White);
            for (int j = 0; j <= slices; j++)
            {
                float theta = (float)j / slices * MathF.PI * 2f;
                b.Add(new Vector3(MathF.Cos(theta) * 0.5f, 0, MathF.Sin(theta) * 0.5f), Vector3.Up, Color.White);
            }
            for (int j = 0; j < slices; j++) b.Triangle(center, j + 1, j + 2);
            return b.Build(device);
        }

        /// <summary>The checkered track (2 x 2 m tiles) and the grass around it.</summary>
        public static Mesh Ground(GraphicsDevice device, World world)
        {
            var b = new MeshBuilder();
            float tile = 2f;
            int across = (int)MathF.Ceiling(world.HalfWidth * 2 / tile);
            float startZ = 6f, endZ = world.FinishZ - 30f;
            int along = (int)MathF.Ceiling((startZ - endZ) / tile);
            for (int i = 0; i < along; i++)
                for (int k = 0; k < across; k++)
                {
                    float z0 = startZ - i * tile, z1 = z0 - tile;
                    float x0 = -world.HalfWidth + k * tile, x1 = MathF.Min(x0 + tile, world.HalfWidth);
                    Color c = (i + k) % 2 == 0 ? world.GroundA : world.GroundB;
                    b.Quad(new Vector3(x0, 0, z0), new Vector3(x0, 0, z1), new Vector3(x1, 0, z1), new Vector3(x1, 0, z0), Vector3.Up, c);
                }
            // grass on both sides, a bit lower so it never fights with the track
            float wide = 60f;
            b.Quad(new Vector3(-wide, -0.02f, startZ + 20), new Vector3(-wide, -0.02f, endZ - 40),
                   new Vector3(-world.HalfWidth, -0.02f, endZ - 40), new Vector3(-world.HalfWidth, -0.02f, startZ + 20), Vector3.Up, world.Grass);
            b.Quad(new Vector3(world.HalfWidth, -0.02f, startZ + 20), new Vector3(world.HalfWidth, -0.02f, endZ - 40),
                   new Vector3(wide, -0.02f, endZ - 40), new Vector3(wide, -0.02f, startZ + 20), Vector3.Up, world.Grass);
            b.Quad(new Vector3(-world.HalfWidth, -0.02f, endZ), new Vector3(-world.HalfWidth, -0.02f, endZ - 40),
                   new Vector3(world.HalfWidth, -0.02f, endZ - 40), new Vector3(world.HalfWidth, -0.02f, endZ), Vector3.Up, world.Grass);
            b.Quad(new Vector3(-world.HalfWidth, -0.02f, startZ + 20), new Vector3(-world.HalfWidth, -0.02f, startZ),
                   new Vector3(world.HalfWidth, -0.02f, startZ), new Vector3(world.HalfWidth, -0.02f, startZ + 20), Vector3.Up, world.Grass);
            return b.Build(device);
        }
    }
}
