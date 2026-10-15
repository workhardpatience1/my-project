using System.Collections.Generic;
using UnityEngine;
using Xna = Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    /// <summary>
    /// Shows a shared <see cref="World"/> with Unity objects: the checkered track, every obstacle
    /// (same sizes and colors as on the phone), Dodgy with eyes, particles, trap zones and the finish.
    /// The simulation itself (movement, collisions, hits) runs in the shared code; this class only
    /// copies positions and colors every frame. Colliders are removed: Unity physics is not used.
    /// </summary>
    public sealed class WorldView
    {
        sealed class Entry
        {
            public GameObject Main;
            public Renderer MainRenderer;
            public Transform Extra1, Extra2;
            public Renderer Extra1Renderer, Extra2Renderer;
        }

        sealed class TrapView
        {
            public TriggerProjectile Trap;
            public GameObject Root;
            public Renderer Zone;
        }

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly Color PlayerBlue = new Color32(33, 150, 243, 255);
        static readonly Color PlayerHit = new Color32(255, 82, 82, 255);

        readonly Camera cam;
        readonly Material baseMaterial;
        readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        readonly Transform persistentRoot;
        Transform levelRoot;
        Material groundMaterial;
        Texture2D groundTexture;
        World world;

        readonly Dictionary<Obstacle, Entry> entries = new Dictionary<Obstacle, Entry>();
        readonly HashSet<Obstacle> present = new HashSet<Obstacle>();
        readonly List<Obstacle> gone = new List<Obstacle>();
        readonly List<TrapView> traps = new List<TrapView>();

        // Dodgy
        Transform playerRoot, bodyPivot;
        Renderer bodyRenderer;

        // particles: a pool of small cubes
        readonly List<Transform> particlePool = new List<Transform>();
        readonly List<Renderer> particleRenderers = new List<Renderer>();

        public WorldView(Camera cam)
        {
            this.cam = cam;
            persistentRoot = new GameObject("World View").transform;

            // one shared material (the default one, made matte); colors come from a property block
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMaterial = new Material(probe.GetComponent<MeshRenderer>().sharedMaterial) { name = "Obstacle Dodge" };
            Object.Destroy(probe);
            if (baseMaterial.HasProperty("_Glossiness")) baseMaterial.SetFloat("_Glossiness", 0.12f);
            if (baseMaterial.HasProperty("_Metallic")) baseMaterial.SetFloat("_Metallic", 0f);
            baseMaterial.color = Color.white;

            CreateLights();
            CreatePlayer();
        }

        void CreateLights()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.48f, 0.52f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 38f;
            RenderSettings.fogEndDistance = 95f;
            QualitySettings.shadowDistance = 70f;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(persistentRoot);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.LookRotation(Cv.U(new Xna.Vector3(-0.45f, -1f, -0.35f)));
            sun.color = new Color(1f, 0.97f, 0.89f);
            sun.intensity = 0.95f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;

            var fill = new GameObject("Fill Light").AddComponent<Light>();
            fill.transform.SetParent(persistentRoot);
            fill.type = LightType.Directional;
            fill.transform.rotation = Quaternion.LookRotation(Cv.U(new Xna.Vector3(0.6f, -0.4f, 0.7f)));
            fill.color = new Color(0.72f, 0.8f, 1f);
            fill.intensity = 0.3f;
            fill.shadows = LightShadows.None;
        }

        void CreatePlayer()
        {
            // feet at the root, so turning and leaning happen around the feet
            playerRoot = new GameObject("Dodgy The Player").transform;
            playerRoot.SetParent(persistentRoot);
            bodyPivot = new GameObject("Body").transform;
            bodyPivot.SetParent(playerRoot, false);

            var body = Primitive(PrimitiveType.Capsule, "Capsule", bodyPivot);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            bodyRenderer = body.GetComponent<Renderer>();
            Paint(bodyRenderer, PlayerBlue);

            // the face looks along local -Z (MonoGame's +Z, see Cv)
            foreach (float side in new[] { -0.19f, 0.19f })
            {
                var eye = Primitive(PrimitiveType.Sphere, "Eye", bodyPivot);
                eye.transform.localPosition = new Vector3(side, 1.52f, -0.4f);
                eye.transform.localScale = new Vector3(0.3f, 0.36f, 0.22f);
                Paint(eye.GetComponent<Renderer>(), Color.white);
                var pupil = Primitive(PrimitiveType.Sphere, "Pupil", bodyPivot);
                pupil.transform.localPosition = new Vector3(side, 1.5f, -0.5f);
                pupil.transform.localScale = new Vector3(0.15f, 0.19f, 0.1f);
                Paint(pupil.GetComponent<Renderer>(), new Color32(25, 25, 30, 255));
            }
            var smile = Primitive(PrimitiveType.Cube, "Smile", bodyPivot);
            smile.transform.localPosition = new Vector3(0f, 1.22f, -0.48f);
            smile.transform.localScale = new Vector3(0.22f, 0.05f, 0.05f);
            Paint(smile.GetComponent<Renderer>(), new Color32(13, 71, 161, 255));
        }

        // ------------------------------------------------------------------ level

        void BuildLevel(World w)
        {
            if (levelRoot != null) Object.Destroy(levelRoot.gameObject);
            if (groundTexture != null) Object.Destroy(groundTexture);
            if (groundMaterial != null) Object.Destroy(groundMaterial);
            entries.Clear();
            traps.Clear();
            world = w;
            levelRoot = new GameObject("Level " + w.LevelNumber).transform;
            levelRoot.SetParent(persistentRoot);

            Color sky = Cv.U(w.Sky);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = sky;
            RenderSettings.fogColor = sky;

            // the track: 2 x 2 m checkered tiles, from 6 m behind the start to 30 m after the finish
            float zStart = -6f, zEnd = w.Length + 30f, length = zEnd - zStart, centerZ = (zStart + zEnd) / 2f;
            var ground = Primitive(PrimitiveType.Cube, "Ground", levelRoot);
            ground.transform.position = new Vector3(0f, -0.1f, centerZ);
            ground.transform.localScale = new Vector3(w.HalfWidth * 2f, 0.2f, length);
            groundTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
            };
            Color a = Cv.U(w.GroundA), b = Cv.U(w.GroundB);
            groundTexture.SetPixels(new[] { a, b, b, a });
            groundTexture.Apply();
            groundMaterial = new Material(baseMaterial) { mainTexture = groundTexture, color = Color.white };
            groundMaterial.mainTextureScale = new Vector2(w.HalfWidth * 2f / 4f, length / 4f);
            var groundRenderer = ground.GetComponent<Renderer>();
            groundRenderer.sharedMaterial = groundMaterial;
            groundRenderer.SetPropertyBlock(null);

            var grass = Primitive(PrimitiveType.Cube, "Grass", levelRoot);
            grass.transform.position = new Vector3(0f, -0.13f, centerZ);
            grass.transform.localScale = new Vector3(120f, 0.2f, length + 60f);
            Paint(grass.GetComponent<Renderer>(), Cv.U(w.Grass));

            foreach (var trap in w.Triggers) traps.Add(CreateTrap(trap));
        }

        TrapView CreateTrap(TriggerProjectile trap)
        {
            var root = new GameObject("Trigger Volume").transform;
            root.SetParent(levelRoot);
            var zone = Primitive(PrimitiveType.Cube, "Danger Zone", root);
            zone.transform.position = Cv.U(trap.Center + new Xna.Vector3(0, 0.03f, 0));
            zone.transform.localScale = new Vector3(trap.HalfSize.X * 2f, 0.02f, trap.HalfSize.Y * 2f);
            for (float x = -trap.HalfSize.X + 0.6f; x < trap.HalfSize.X; x += 1.4f)
            {
                var stripe = Primitive(PrimitiveType.Cube, "Stripe", root);
                stripe.transform.position = Cv.U(trap.Center + new Xna.Vector3(x, 0.05f, 0));
                stripe.transform.rotation = Cv.Yaw(0.5f);
                stripe.transform.localScale = new Vector3(0.5f, 0.02f, trap.HalfSize.Y * 2f * 0.9f);
                Paint(stripe.GetComponent<Renderer>(), new Color32(255, 235, 59, 255));
            }
            return new TrapView { Trap = trap, Root = root.gameObject, Zone = zone.GetComponent<Renderer>() };
        }

        Entry CreateEntry(Obstacle o)
        {
            var type = o.Shape == ShapeKind.Sphere ? PrimitiveType.Sphere
                     : o.Shape == ShapeKind.Cylinder ? PrimitiveType.Cylinder
                     : PrimitiveType.Cube;
            var main = Primitive(type, o.GetType().Name, levelRoot);
            var e = new Entry { Main = main, MainRenderer = main.GetComponent<Renderer>() };
            if (o is Spinner)
            {
                var pillar = Primitive(PrimitiveType.Cylinder, "Spinner Pillar", levelRoot);
                var cap = Primitive(PrimitiveType.Cylinder, "Spinner Cap", levelRoot);
                e.Extra1 = pillar.transform; e.Extra1Renderer = pillar.GetComponent<Renderer>();
                e.Extra2 = cap.transform; e.Extra2Renderer = cap.GetComponent<Renderer>();
                Paint(e.Extra1Renderer, new Color32(84, 84, 96, 255));
            }
            else if (o is FlyAtPlayer)
            {
                var core = Primitive(PrimitiveType.Sphere, "Core", levelRoot);
                e.Extra1 = core.transform; e.Extra1Renderer = core.GetComponent<Renderer>();
                Paint(e.Extra1Renderer, new Color32(255, 111, 0, 255));
            }
            return e;
        }

        // ------------------------------------------------------------------ every frame

        public void Sync(World w, float time)
        {
            if (w != world) BuildLevel(w);

            present.Clear();
            foreach (var o in w.Obstacles)
            {
                present.Add(o);
                if (!entries.TryGetValue(o, out var e)) entries[o] = e = CreateEntry(o);
                bool show = o.Visible && !o.Destroyed;
                if (e.Main.activeSelf != show) SetActive(e, show);
                if (!show) continue;

                var t = e.Main.transform;
                t.position = Cv.U(o.Position);
                t.rotation = Cv.Yaw(o.Yaw);
                // Unity's cylinder is 2 m high, MonoGame's is 1 m
                t.localScale = o.Shape == ShapeKind.Cylinder ? new Vector3(o.Size.X, o.Size.Y * 0.5f, o.Size.Z) : Cv.Size(o.Size);
                Paint(e.MainRenderer, Cv.U(o.DrawColor));

                if (o is Spinner s)
                {
                    float h = s.Position.Y + 0.6f;
                    e.Extra1.position = Cv.U(new Xna.Vector3(s.Position.X, h / 2f, s.Position.Z));
                    e.Extra1.localScale = new Vector3(s.PillarRadius * 2f, h * 0.5f, s.PillarRadius * 2f);
                    e.Extra2.position = Cv.U(new Xna.Vector3(s.Position.X, s.Position.Y + 0.45f, s.Position.Z));
                    e.Extra2.localScale = new Vector3(1.1f, 0.125f, 1.1f);
                    Paint(e.Extra2Renderer, s.IsHit ? Cv.U(s.DrawColor) : (Color)new Color32(255, 213, 79, 255));
                }
                else if (o is FlyAtPlayer)
                {
                    e.Extra1.position = Cv.U(o.Position + new Xna.Vector3(0, 0, 0.18f));
                    e.Extra1.localScale = Cv.Size(o.Size * 0.55f);
                }
            }

            // obstacles that were destroyed (projectiles) disappear
            gone.Clear();
            foreach (var pair in entries) if (!present.Contains(pair.Key)) gone.Add(pair.Key);
            foreach (var o in gone)
            {
                var e = entries[o];
                Object.Destroy(e.Main);
                if (e.Extra1 != null) Object.Destroy(e.Extra1.gameObject);
                if (e.Extra2 != null) Object.Destroy(e.Extra2.gameObject);
                entries.Remove(o);
            }

            // trap zones pulse until they fire
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 6f);
            foreach (var trap in traps)
            {
                bool show = !trap.Trap.Fired;
                if (trap.Root.activeSelf != show) trap.Root.SetActive(show);
                if (show) Paint(trap.Zone, Color.Lerp(new Color32(150, 30, 30, 255), new Color32(235, 70, 60, 255), pulse));
            }

            SyncPlayer(w.Player);
            SyncParticles(w.Particles);
        }

        void SyncPlayer(Mover p)
        {
            playerRoot.position = Cv.U(p.Position);
            playerRoot.rotation = Cv.Yaw(p.Yaw);
            float speed = p.Velocity.Length();
            float bob = Mathf.Abs(Mathf.Sin(p.WalkCycle * Mathf.PI)) * 0.08f * Mathf.Clamp01(speed / 4f);
            float lean = Mathf.Clamp01(speed / p.MoveSpeed) * 0.12f;
            bodyPivot.localPosition = new Vector3(0f, bob, 0f);
            bodyPivot.localRotation = Quaternion.Euler(-lean * Mathf.Rad2Deg, 0f, 0f);

            bool blinkHidden = p.Invulnerable > 0 && ((int)(p.Invulnerable * 10f)) % 2 == 0;
            if (bodyPivot.gameObject.activeSelf == blinkHidden) bodyPivot.gameObject.SetActive(!blinkHidden);
            bool red = p.HitBlink > 0 && ((int)(p.HitBlink * 14f)) % 2 == 0;
            Paint(bodyRenderer, red ? PlayerHit : PlayerBlue);
        }

        void SyncParticles(List<Particle> particles)
        {
            while (particlePool.Count < particles.Count)
            {
                var cube = Primitive(PrimitiveType.Cube, "Particle", persistentRoot);
                var r = cube.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                particlePool.Add(cube.transform);
                particleRenderers.Add(r);
            }
            for (int i = 0; i < particlePool.Count; i++)
            {
                var t = particlePool[i];
                bool active = i < particles.Count;
                if (t.gameObject.activeSelf != active) t.gameObject.SetActive(active);
                if (!active) continue;
                var p = particles[i];
                float scale = p.Size * Mathf.Clamp01(p.Life / 0.3f);
                t.position = Cv.U(p.Position);
                t.rotation = Quaternion.Euler(p.Spin * p.Life * Mathf.Rad2Deg, p.Spin * 0.7f * p.Life * Mathf.Rad2Deg, 0f);
                t.localScale = new Vector3(scale, scale, scale);
                Paint(particleRenderers[i], Cv.U(p.Color));
            }
        }

        // ------------------------------------------------------------------ helpers

        static void SetActive(Entry e, bool on)
        {
            e.Main.SetActive(on);
            if (e.Extra1 != null) e.Extra1.gameObject.SetActive(on);
            if (e.Extra2 != null) e.Extra2.gameObject.SetActive(on);
        }

        GameObject Primitive(PrimitiveType type, string name, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.GetComponent<Renderer>().sharedMaterial = baseMaterial;
            return go;
        }

        void Paint(Renderer r, Color c)
        {
            block.SetColor(ColorId, c);
            r.SetPropertyBlock(block);
        }
    }
}
