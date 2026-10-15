// A tiny copy of the MonoGame/XNA math types (Vector2, Vector3, Matrix, Color, MathHelper).
// With it, the phone version's game logic (Scripts/Shared, copied from ObstacleDodge.MonoGame/Shared)
// compiles in Unity unchanged — so the levels, collisions, hits and ad rules are exactly the same.
// Only the members that the shared code uses are here. Coordinates are MonoGame's (forward = -Z);
// Scripts/Unity/Convert.cs turns them into Unity coordinates.
using System;

namespace Microsoft.Xna.Framework
{
    public static class MathHelper
    {
        public const float Pi = (float)Math.PI;
        public const float TwoPi = (float)(Math.PI * 2.0);
        public const float PiOver2 = (float)(Math.PI / 2.0);
        public const float PiOver4 = (float)(Math.PI / 4.0);

        public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
        public static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static float ToRadians(float degrees) => degrees * (Pi / 180f);
        public static float ToDegrees(float radians) => radians * (180f / Pi);

        public static float WrapAngle(float angle)
        {
            if (angle > -Pi && angle <= Pi) return angle;
            angle %= TwoPi;
            if (angle <= -Pi) return angle + TwoPi;
            if (angle > Pi) return angle - TwoPi;
            return angle;
        }
    }

    [Serializable]
    public struct Vector2 : IEquatable<Vector2>
    {
        public float X, Y;

        public Vector2(float x, float y) { X = x; Y = y; }
        public Vector2(float value) { X = value; Y = value; }

        public static Vector2 Zero => new Vector2(0f, 0f);
        public static Vector2 One => new Vector2(1f, 1f);
        public static Vector2 UnitX => new Vector2(1f, 0f);
        public static Vector2 UnitY => new Vector2(0f, 1f);

        public float Length() => MathF.Sqrt(X * X + Y * Y);
        public float LengthSquared() => X * X + Y * Y;

        public void Normalize()
        {
            float len = Length();
            if (len > 0f) { X /= len; Y /= len; }
        }

        public static Vector2 Normalize(Vector2 v) { v.Normalize(); return v; }
        public static float Distance(Vector2 a, Vector2 b) => (a - b).Length();
        public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;
        public static Vector2 Clamp(Vector2 v, Vector2 min, Vector2 max) =>
            new Vector2(MathHelper.Clamp(v.X, min.X, max.X), MathHelper.Clamp(v.Y, min.Y, max.Y));
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * t;

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.X, -a.Y);
        public static Vector2 operator *(Vector2 a, float s) => new Vector2(a.X * s, a.Y * s);
        public static Vector2 operator *(float s, Vector2 a) => new Vector2(a.X * s, a.Y * s);
        public static Vector2 operator /(Vector2 a, float s) => new Vector2(a.X / s, a.Y / s);
        public static bool operator ==(Vector2 a, Vector2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);

        public bool Equals(Vector2 other) => this == other;
        public override bool Equals(object obj) => obj is Vector2 v && this == v;
        public override int GetHashCode() => X.GetHashCode() * 31 + Y.GetHashCode();
        public override string ToString() => "{X:" + X + " Y:" + Y + "}";
    }

    [Serializable]
    public struct Vector3 : IEquatable<Vector3>
    {
        public float X, Y, Z;

        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public Vector3(float value) { X = value; Y = value; Z = value; }

        public static Vector3 Zero => new Vector3(0f, 0f, 0f);
        public static Vector3 One => new Vector3(1f, 1f, 1f);
        public static Vector3 UnitX => new Vector3(1f, 0f, 0f);
        public static Vector3 UnitY => new Vector3(0f, 1f, 0f);
        public static Vector3 UnitZ => new Vector3(0f, 0f, 1f);
        public static Vector3 Up => new Vector3(0f, 1f, 0f);
        public static Vector3 Down => new Vector3(0f, -1f, 0f);
        public static Vector3 Right => new Vector3(1f, 0f, 0f);
        public static Vector3 Left => new Vector3(-1f, 0f, 0f);
        public static Vector3 Forward => new Vector3(0f, 0f, -1f);
        public static Vector3 Backward => new Vector3(0f, 0f, 1f);

        public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);
        public float LengthSquared() => X * X + Y * Y + Z * Z;

        public void Normalize()
        {
            float len = Length();
            if (len > 0f) { X /= len; Y /= len; Z /= len; }
        }

        public static Vector3 Normalize(Vector3 v) { v.Normalize(); return v; }
        public static float Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static float Distance(Vector3 a, Vector3 b) => (a - b).Length();
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * t;

        /// <summary>Row vector times matrix, like XNA (rotation + scale + translation).</summary>
        public static Vector3 Transform(Vector3 v, Matrix m) => new Vector3(
            v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + m.M41,
            v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + m.M42,
            v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + m.M43);

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.X, -a.Y, -a.Z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.X * s, a.Y * s, a.Z * s);
        public static Vector3 operator *(float s, Vector3 a) => new Vector3(a.X * s, a.Y * s, a.Z * s);
        public static Vector3 operator *(Vector3 a, Vector3 b) => new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        public static Vector3 operator /(Vector3 a, float s) => new Vector3(a.X / s, a.Y / s, a.Z / s);
        public static bool operator ==(Vector3 a, Vector3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        public bool Equals(Vector3 other) => this == other;
        public override bool Equals(object obj) => obj is Vector3 v && this == v;
        public override int GetHashCode() => (X.GetHashCode() * 31 + Y.GetHashCode()) * 31 + Z.GetHashCode();
        public override string ToString() => "{X:" + X + " Y:" + Y + " Z:" + Z + "}";
    }

    /// <summary>A 4x4 matrix in XNA layout (row vectors, translation in M41..M43).</summary>
    [Serializable]
    public struct Matrix
    {
        public float M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44;

        public static Matrix Identity => new Matrix { M11 = 1f, M22 = 1f, M33 = 1f, M44 = 1f };

        public static Matrix CreateScale(float s) => CreateScale(new Vector3(s));
        public static Matrix CreateScale(float x, float y, float z) => CreateScale(new Vector3(x, y, z));
        public static Matrix CreateScale(Vector3 s) => new Matrix { M11 = s.X, M22 = s.Y, M33 = s.Z, M44 = 1f };

        public static Matrix CreateTranslation(Vector3 p)
        {
            var m = Identity;
            m.M41 = p.X; m.M42 = p.Y; m.M43 = p.Z;
            return m;
        }

        public static Matrix CreateTranslation(float x, float y, float z) => CreateTranslation(new Vector3(x, y, z));

        public static Matrix CreateRotationY(float radians)
        {
            float c = MathF.Cos(radians), s = MathF.Sin(radians);
            var m = Identity;
            m.M11 = c; m.M13 = -s;
            m.M31 = s; m.M33 = c;
            return m;
        }

        public static Matrix operator *(Matrix a, Matrix b) => new Matrix
        {
            M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
            M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
            M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
            M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,
            M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
            M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
            M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
            M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,
            M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
            M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
            M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
            M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,
            M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
            M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
            M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
            M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44,
        };
    }

    /// <summary>An RGBA color with 0..255 channels, like XNA's Color.</summary>
    [Serializable]
    public struct Color : IEquatable<Color>
    {
        public byte R, G, B, A;

        public Color(int r, int g, int b) : this(r, g, b, 255) { }

        public Color(int r, int g, int b, int a)
        {
            R = (byte)Math.Clamp(r, 0, 255);
            G = (byte)Math.Clamp(g, 0, 255);
            B = (byte)Math.Clamp(b, 0, 255);
            A = (byte)Math.Clamp(a, 0, 255);
        }

        public Color(float r, float g, float b) : this((int)(r * 255f + 0.5f), (int)(g * 255f + 0.5f), (int)(b * 255f + 0.5f), 255) { }

        public static Color Lerp(Color a, Color b, float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return new Color(
                (int)(a.R + (b.R - a.R) * t + 0.5f), (int)(a.G + (b.G - a.G) * t + 0.5f),
                (int)(a.B + (b.B - a.B) * t + 0.5f), (int)(a.A + (b.A - a.A) * t + 0.5f));
        }

        /// <summary>Premultiplied fade, like XNA: every channel is scaled.</summary>
        public static Color operator *(Color c, float s) =>
            new Color((int)(c.R * s), (int)(c.G * s), (int)(c.B * s), (int)(c.A * s));

        public Vector3 ToVector3() => new Vector3(R / 255f, G / 255f, B / 255f);

        public static Color Transparent => new Color(0, 0, 0, 0);
        public static Color Black => new Color(0, 0, 0);
        public static Color White => new Color(255, 255, 255);
        public static Color Orange => new Color(255, 165, 0);
        public static Color OrangeRed => new Color(255, 69, 0);
        public static Color Gold => new Color(255, 215, 0);
        public static Color Yellow => new Color(255, 255, 0);
        public static Color DeepSkyBlue => new Color(0, 191, 255);
        public static Color HotPink => new Color(255, 105, 180);
        public static Color LimeGreen => new Color(50, 205, 50);
        public static Color Purple => new Color(128, 0, 128);
        public static Color Red => new Color(255, 0, 0);

        public static bool operator ==(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
        public static bool operator !=(Color a, Color b) => !(a == b);
        public bool Equals(Color other) => this == other;
        public override bool Equals(object obj) => obj is Color c && this == c;
        public override int GetHashCode() => (R << 24) | (G << 16) | (B << 8) | A;
    }
}
