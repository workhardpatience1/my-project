using UnityEngine;
using Xna = Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    /// <summary>
    /// The shared game code uses MonoGame coordinates (right-handed, forward = -Z).
    /// Unity is left-handed with forward = +Z, so Z is flipped and the turn angle changes sign.
    /// </summary>
    public static class Cv
    {
        public static Vector3 U(Xna.Vector3 v) => new Vector3(v.X, v.Y, -v.Z);

        public static Quaternion Yaw(float xnaYawRadians) => Quaternion.Euler(0f, -xnaYawRadians * Mathf.Rad2Deg, 0f);

        public static Color U(Xna.Color c) => new Color32(c.R, c.G, c.B, c.A);

        public static Vector3 Size(Xna.Vector3 s) => new Vector3(s.X, s.Y, s.Z);
    }
}
