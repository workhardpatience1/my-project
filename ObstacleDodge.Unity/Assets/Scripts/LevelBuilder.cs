using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the whole level with code every time the scene loads: the ground, light, camera,
// "Dodgy The Player", GameManager/GameUI/TouchControls, AdManager and all the obstacles
// (Wall, Small building, Spinning Thing, Dropping Object, Trigger Volume + projectiles).
// It does with code exactly what guide 1 (Parts 3-6) does with the mouse, so the scene file
// itself can stay empty. Each level number always gives the same layout; later levels are
// longer and harder, and every pattern always leaves a free path.
public class LevelBuilder : MonoBehaviour
{
    const float HalfWidth = 7f;

    static readonly Color Green = new Color(0.49f, 0.78f, 0.39f);
    static readonly Color WallOrange = new Color(1f, 0.55f, 0f);
    static readonly Color BuildingBlue = new Color(0.36f, 0.42f, 0.75f);
    static readonly Color BuildingTeal = new Color(0.15f, 0.65f, 0.6f);
    static readonly Color SpinnerRed = new Color(0.9f, 0.22f, 0.21f);
    static readonly Color DropperPurple = new Color(0.67f, 0.28f, 0.74f);
    static readonly Color ProjectileYellow = new Color(1f, 0.84f, 0f);
    static readonly Color SliderPink = new Color(0.93f, 0.25f, 0.48f);
    static readonly Color RailGray = new Color(0.69f, 0.75f, 0.77f);
    static readonly Color PlayerBlue = new Color(0.13f, 0.59f, 0.95f);

    // Referenced on purpose, so IL2CPP code stripping keeps what CreatePrimitive needs (see link.xml).
    static readonly System.Type[] KeepForCreatePrimitive =
    {
        typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider), typeof(SphereCollider),
        typeof(CapsuleCollider), typeof(MeshCollider),
    };

    Transform root;
    Transform player;
    System.Random rng;

    // Create a LevelBuilder in every loaded scene that does not have one.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindAnyObjectByType<LevelBuilder>() == null)
            new GameObject("Level Builder").AddComponent<LevelBuilder>();
    }

    void Awake()
    {
        root = transform;
        // GameManager first: it knows which level to build.
        var managers = new GameObject("GameManager");
        managers.AddComponent<GameManager>();
        managers.AddComponent<GameUI>();
        managers.AddComponent<TouchControls>();

        // The object name "AdManager" matters for WebGL (SendMessage('AdManager', ...)).
        if (AdManager.Instance == null) new GameObject("AdManager").AddComponent<AdManager>();

        Build(GameManager.Instance.Level);
    }

    void Build(int level)
    {
        rng = new System.Random(level * 7919 + 17);
        float t = Mathf.Clamp01((level - 1) / 14f);
        float length = 70f + Mathf.Min(level - 1, 14) * 9f;

        BuildLightAndSky();
        BuildGround(length);
        BuildPlayerAndCamera();
        BuildFinish(length);

        float d = 12f;
        int index = 0;
        while (d < length - 10f)
        {
            float used;
            switch (PickPattern(level, index))
            {
                case 0: used = WallRow(d, t); break;
                case 1: used = Buildings(d); break;
                case 2: used = SpinningThing(d, t); break;
                case 3: used = Droppers(d, t); break;
                case 4: used = ProjectileTrap(d, t); break;
                case 5: used = Sliders(d, t); break;
                default: used = ZigZag(d, t); break;
            }
            d += used + Mathf.Lerp(9f, 5.5f, t) + Rand() * 3f;
            index++;
        }
    }

    int PickPattern(int level, int index)
    {
        if (level == 1) return index % 3 == 2 ? 1 : 0;
        if (level == 2) return new[] { 0, 2, 1, 3, 0, 2 }[index % 6];
        int[] weights = level < 4 ? new[] { 3, 2, 3, 2, 2, 0, 1 } : new[] { 2, 2, 3, 3, 3, 2, 2 };
        int total = 0;
        foreach (int w in weights) total += w;
        int roll = rng.Next(total);
        for (int i = 0; i < weights.Length; i++)
        {
            if (roll < weights[i]) return i;
            roll -= weights[i];
        }
        return 0;
    }

    float Rand() => (float)rng.NextDouble();

    // ------------------------------------------------------------------ scene basics

    void BuildLightAndSky()
    {
        var lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(root);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.1f;
        sun.shadows = LightShadows.Soft;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.62f, 0.8f, 0.95f);
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 100f;
    }

    void BuildGround(float length)
    {
        // A long green track with a checkered texture (made in code) and rails on both sides.
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(root);
        float total = length + 40f;
        ground.transform.position = new Vector3(0f, -0.5f, total / 2f - 10f);
        ground.transform.localScale = new Vector3(HalfWidth * 2f, 1f, total);
        var material = ground.GetComponent<MeshRenderer>().material;
        material.color = Color.white;
        material.mainTexture = CheckerTexture(Green, Green * 0.88f);
        material.mainTextureScale = new Vector2(HalfWidth, total / 2f);

        var grass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grass.name = "Grass";
        grass.transform.SetParent(root);
        grass.transform.position = new Vector3(0f, -0.52f, total / 2f - 10f);
        grass.transform.localScale = new Vector3(120f, 1f, total + 80f);
        Paint(grass, new Color(0.3f, 0.58f, 0.25f));

        foreach (float side in new[] { -1f, 1f })
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Rail";
            rail.transform.SetParent(root);
            rail.transform.position = new Vector3(side * (HalfWidth + 0.3f), 0.5f, total / 2f - 10f);
            rail.transform.localScale = new Vector3(0.6f, 1f, total);
            Paint(rail, RailGray);
        }

        // walls behind the start and after the finish, so nobody walks off the ground
        foreach (float z in new[] { -3f, total - 11f })
        {
            var end = GameObject.CreatePrimitive(PrimitiveType.Cube);
            end.name = "End Wall";
            end.transform.SetParent(root);
            end.transform.position = new Vector3(0f, 0.5f, z);
            end.transform.localScale = new Vector3(HalfWidth * 2f + 1.2f, 1f, 0.6f);
            Paint(end, RailGray);
        }
    }

    static Texture2D CheckerTexture(Color a, Color b)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { a, b, b, a });
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.Apply();
        return tex;
    }

    void BuildPlayerAndCamera()
    {
        // Guide 1, Part 4.1: a Capsule with a Rigidbody (rotation frozen) and the Player tag.
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Dodgy The Player";
        body.tag = "Player";
        body.transform.SetParent(root);
        body.transform.position = new Vector3(0f, 1f, 0f);
        Paint(body, PlayerBlue);
        var rb = body.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.AddComponent<Mover>();
        player = body.transform;

        // eyes (no colliders)
        foreach (float side in new[] { -0.19f, 0.19f })
        {
            var eye = Decoration(PrimitiveType.Sphere, "Eye", Vector3.zero, new Vector3(0.3f, 0.36f, 0.22f), Color.white);
            eye.transform.SetParent(body.transform, false);
            eye.transform.localPosition = new Vector3(side, 0.52f, 0.4f);
            var pupil = Decoration(PrimitiveType.Sphere, "Pupil", Vector3.zero, new Vector3(0.15f, 0.19f, 0.1f), new Color(0.1f, 0.1f, 0.12f));
            pupil.transform.SetParent(body.transform, false);
            pupil.transform.localPosition = new Vector3(side, 0.5f, 0.5f);
        }

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(root);
        var cam = cameraObject.AddComponent<Camera>();
        cam.fieldOfView = Screen.width >= Screen.height ? 50f : 75f;
        cam.farClipPlane = 150f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<FollowCamera>().SetTarget(player);
    }

    void BuildFinish(float length)
    {
        var finish = new GameObject("Finish Line");
        finish.transform.SetParent(root);
        finish.transform.position = new Vector3(0f, 1f, length);
        var zone = finish.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(HalfWidth * 2f, 2f, 1f);
        finish.AddComponent<FinishLine>();

        int squares = 14;
        float w = HalfWidth * 2f / squares;
        for (int row = 0; row < 2; row++)
            for (int i = 0; i < squares; i++)
                Decoration(PrimitiveType.Cube, "Finish Square",
                    new Vector3(-HalfWidth + w * (i + 0.5f), 0.02f, length + (row - 0.5f) * w),
                    new Vector3(w, 0.04f, w), (i + row) % 2 == 0 ? new Color(0.12f, 0.12f, 0.12f) : Color.white);
        Decoration(PrimitiveType.Cylinder, "Finish Post", new Vector3(-HalfWidth - 0.5f, 2.5f, length), new Vector3(0.5f, 2.5f, 0.5f), Color.white);
        Decoration(PrimitiveType.Cylinder, "Finish Post", new Vector3(HalfWidth + 0.5f, 2.5f, length), new Vector3(0.5f, 2.5f, 0.5f), Color.white);
        Decoration(PrimitiveType.Cube, "Finish Banner", new Vector3(0f, 5f, length), new Vector3(HalfWidth * 2f + 1.6f, 0.9f, 0.3f), new Color(1f, 0.77f, 0f));
    }

    // ------------------------------------------------------------------ patterns

    float WallRow(float d, float t)
    {
        float gap = Mathf.Lerp(3.6f, 2.3f, t);
        float gapX = (Rand() * 2f - 1f) * (HalfWidth - gap / 2f - 0.5f);
        WallSegment(-HalfWidth, gapX - gap / 2f, d);
        WallSegment(gapX + gap / 2f, HalfWidth, d);
        return 1f;
    }

    void WallSegment(float x0, float x1, float d)
    {
        float width = x1 - x0;
        if (width < 0.4f) return;
        // guide 1, Part 5.2: a Wall is a Cube with ObjectHit
        Obstacle(PrimitiveType.Cube, "Wall", new Vector3((x0 + x1) / 2f, 0.5f, d), new Vector3(width, 1f, 1f), WallOrange);
    }

    float ZigZag(float d, float t)
    {
        float gap = Mathf.Lerp(3.4f, 2.4f, t);
        bool leftFirst = rng.Next(2) == 0;
        float side = HalfWidth - gap / 2f - 0.6f;
        for (int i = 0; i < 3; i++)
        {
            float gapX = ((i % 2 == 0) == leftFirst) ? -side : side;
            WallSegment(-HalfWidth, gapX - gap / 2f, d + i * 5.5f);
            WallSegment(gapX + gap / 2f, HalfWidth, d + i * 5.5f);
        }
        return 11f;
    }

    float Buildings(float d)
    {
        float[] lanes = { -5.6f, -2.8f, 0f, 2.8f, 5.6f };
        int freeA = rng.Next(5), freeB = (freeA + 2 + rng.Next(2)) % 5;
        for (int i = 0; i < lanes.Length; i++)
        {
            if (i == freeA || i == freeB) continue;
            float h = 2f + Rand() * 2.5f;
            float dz = (Rand() * 2f - 1f) * 1.5f;
            // guide 1, Part 5.4: "Small building" = a taller, more square wall
            Obstacle(PrimitiveType.Cube, "Small building", new Vector3(lanes[i], h / 2f, d + dz), new Vector3(2f, h, 2f),
                rng.Next(2) == 0 ? BuildingBlue : BuildingTeal);
        }
        return 3f;
    }

    float SpinningThing(float d, float t)
    {
        float length = Mathf.Lerp(7f, 9f, t);
        float speed = Mathf.Lerp(70f, 150f, t) * (rng.Next(2) == 0 ? 1f : -1f);
        float x = (Rand() * 2f - 1f) * 1.2f;
        float z = d + length / 2f;
        // guide 1, Part 5.5: a Cube with Spinner + ObjectHit
        var bar = Obstacle(PrimitiveType.Cube, "Spinning Thing", new Vector3(x, 0.55f, z), new Vector3(length, 0.7f, 0.7f), SpinnerRed);
        bar.transform.rotation = Quaternion.Euler(0f, Rand() * 360f, 0f);
        bar.AddComponent<Spinner>().SetAngles(0f, speed, 0f);
        Kinematic(bar);
        Obstacle(PrimitiveType.Cylinder, "Spinner Pillar", new Vector3(x, 0.6f, z), new Vector3(0.9f, 0.6f, 0.9f), new Color(0.33f, 0.33f, 0.38f));
        return length;
    }

    float Droppers(float d, float t)
    {
        int count = 2 + Mathf.RoundToInt(t * 2f) + rng.Next(2);
        float used = 0f;
        for (int i = 0; i < count; i++)
        {
            float x = (Rand() * 2f - 1f) * (HalfWidth - 1.2f);
            float z = d + i * 3.2f + Rand();
            // guide 1, Part 5.6: a Cube with Rigidbody + Dropper + ObjectHit, high above the ground
            var box = Obstacle(PrimitiveType.Cube, "Dropping Object", new Vector3(x, 14f, z), Vector3.one * 1.6f, DropperPurple);
            var rb = box.AddComponent<Rigidbody>();
            rb.mass = 50f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            box.AddComponent<Dropper>().Setup(player, Mathf.Lerp(12f, 10f, t), 0.05f + Rand() * 0.35f);
            used = z - d;
        }
        return used + 1.6f;
    }

    float ProjectileTrap(float d, float t)
    {
        // guide 1, Part 5.8: a Trigger Volume and five projectiles in front of it
        var trap = new GameObject("Trigger Volume");
        trap.transform.SetParent(root);
        trap.transform.position = new Vector3(0f, 1f, d);
        var zone = trap.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(HalfWidth * 2f, 2f, 1.8f);
        // the guide's zone is invisible; we paint it red so players can see the danger
        var mark = Decoration(PrimitiveType.Cube, "Danger Zone", new Vector3(0f, 0.02f, d), new Vector3(HalfWidth * 2f, 0.04f, 1.8f), new Color(0.9f, 0.25f, 0.2f));
        mark.transform.SetParent(trap.transform, true);

        float speed = Mathf.Lerp(6.5f, 11f, t);
        float ahead = Mathf.Lerp(13f, 11f, t);
        float spread = HalfWidth - 1f;
        var projectiles = new GameObject[5];
        for (int i = 0; i < projectiles.Length; i++)
        {
            float x = -spread + 2f * spread * i / (projectiles.Length - 1) + (Rand() - 0.5f);
            var start = new Vector3(x, 1f, d + ahead + Rand() * 2f);
            Decoration(PrimitiveType.Cylinder, "Launch Pad", new Vector3(start.x, 0.25f, start.z), new Vector3(0.9f, 0.25f, 0.9f), new Color(0.26f, 0.26f, 0.31f));
            var shot = Obstacle(PrimitiveType.Sphere, "Projectile", start, Vector3.one * 0.75f, ProjectileYellow);
            Kinematic(shot);
            shot.AddComponent<FlyAtPlayer>().Setup(player, speed * (0.85f + Rand() * 0.3f)); // Awake switches it off
            projectiles[i] = shot;
        }
        trap.AddComponent<TriggerProjectile>().Setup(projectiles);
        return ahead + 2f;
    }

    float Sliders(float d, float t)
    {
        for (int i = 0; i < 2; i++)
        {
            var wall = Obstacle(PrimitiveType.Cube, "Sliding Wall", new Vector3(0f, 0.75f, d + i * 5f), new Vector3(3.2f, 1.5f, 1f), SliderPink);
            Kinematic(wall);
            wall.AddComponent<SlidingWall>().Setup(HalfWidth - 2.2f, Mathf.Lerp(1.2f, 2.2f, t), Rand() * Mathf.PI * 2f + i * Mathf.PI);
        }
        return 5f;
    }

    // ------------------------------------------------------------------ helpers

    GameObject Obstacle(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(root);
        go.transform.position = position;
        go.transform.localScale = scale;
        Paint(go, color);
        go.AddComponent<ObjectHit>();
        return go;
    }

    GameObject Decoration(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(root);
        go.transform.position = position;
        go.transform.localScale = scale;
        DestroyImmediate(go.GetComponent<Collider>()); // only something to look at
        Paint(go, color);
        return go;
    }

    static void Kinematic(GameObject go)
    {
        // moving obstacles get a kinematic Rigidbody, so collisions with the player are reported
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    static void Paint(GameObject go, Color color)
    {
        go.GetComponent<MeshRenderer>().material.color = color;
    }
}
