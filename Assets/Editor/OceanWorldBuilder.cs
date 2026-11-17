#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Turns the cube-and-slab level boundaries into natural scenery and spreads the fish out:
///  - the flat "Water" backdrop is hidden (the fog and the cliffs do its job),
///  - the Ancient Ruins wall becomes a rock ridge (the gate slab becomes a boulder and still sinks open),
///  - the other edges of the level are closed by rock cliffs, backed by invisible colliders so nothing leaves the level,
///  - the fish are spread over the whole play area (Level 2 also gets extra fish so Tier 3 is reachable).
/// Menu: Story > Ocean > Build Level 1 / 2 / 3. It is safe to run again: the old "Natural Boundaries" object is replaced.
/// </summary>
public static class OceanWorldBuilder
{
    private const string EnvDir = "Assets/Environment";
    private const int Variants = 4;

    [MenuItem("Story/Ocean/Build Level 1")] public static void Level1Menu() { Build(1); }
    [MenuItem("Story/Ocean/Build Level 2")] public static void Level2Menu() { Build(2); }
    [MenuItem("Story/Ocean/Build Level 3")] public static void Level3Menu() { Build(3); }

    private class Plan
    {
        public string scene;
        public float minX, maxX, minZ, maxZ;   // play area, inside the cliffs
        public int wallSide;                   // which side the ruins wall closes: 0 = +X, 1 = -X, 2 = +Z
        public float wallCoord;                // x (or z) of the ruins wall plane
        public Vector3 spawn;
        public int addFish;
        public float fogDensity;
    }

    private class Rocks
    {
        public Mesh[] visual = new Mesh[Variants];
        public Mesh[] collision = new Mesh[Variants];
        public Material[] mats = new Material[4];
    }

    private struct Layer
    {
        public float bottom, height;
        public Layer(float b, float h) { bottom = b; height = h; }
    }

    // stacked rows of rock, from the sand up past the water surface
    private static readonly Layer[] Layers =
    {
        new Layer(-6f, 16f), new Layer(4f, 24f), new Layer(18f, 26f), new Layer(34f, 26f), new Layer(48f, 22f)
    };

    private static Plan PlanFor(int level)
    {
        Plan p = new Plan();
        if (level == 1)
        {
            p.scene = "Level 1"; p.minX = -154f; p.maxX = 26f; p.minZ = -48f; p.maxZ = 59f;
            p.wallSide = 0; p.wallCoord = 28f; p.spawn = new Vector3(-0.8f, 4.4f, 6.4f); p.addFish = 4; p.fogDensity = 0.010f;
        }
        else if (level == 2)
        {
            p.scene = "Level 2"; p.minX = -154f; p.maxX = 134f; p.minZ = -48f; p.maxZ = 27f;
            p.wallSide = 2; p.wallCoord = 28f; p.spawn = new Vector3(7.7f, 8.9f, -1.8f); p.addFish = 8; p.fogDensity = 0.010f;
        }
        else
        {
            p.scene = "Level 3"; p.minX = -26f; p.maxX = 134f; p.minZ = -48f; p.maxZ = 59f;
            p.wallSide = 1; p.wallCoord = -28f; p.spawn = new Vector3(7.7f, 8.9f, -1.8f); p.addFish = 5; p.fogDensity = 0.012f;
        }
        return p;
    }

    // ------------------------------------------------------------------ main entry

    public static string Build(int level)
    {
        Plan p = PlanFor(level);
        EditorSceneManager.SaveOpenScenes();
        Scene sc = EditorSceneManager.OpenScene("Assets/Scenes/" + p.scene + ".unity", OpenSceneMode.Single);
        Rocks rocks = EnsureRockAssets();

        GameObject old = GameObject.Find("Natural Boundaries");
        if (old != null) Object.DestroyImmediate(old);
        Transform root = new GameObject("Natural Boundaries").transform;

        // the flat water-picture cube is no longer drawn (its collider stays as an extra safety edge)
        GameObject water = RootByName("Water");
        if (water != null && water.GetComponent<MeshRenderer>() != null) water.GetComponent<MeshRenderer>().enabled = false;

        int ridge = BuildRuinsWall(p, root, rocks);
        int cliffs = BuildCliffs(p, root, rocks);
        BuildBounds(p, root);

        DayNightCycle dn = Object.FindFirstObjectByType<DayNightCycle>();
        if (dn != null)
        {
            SerializedObject so = new SerializedObject(dn);
            SerializedProperty fog = so.FindProperty("baseFogDensity");
            if (fog != null) { fog.floatValue = p.fogDensity; so.ApplyModifiedProperties(); }
        }

        string fishLog = SpreadFish(p, level);

        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
        AssetDatabase.SaveAssets();
        return p.scene + ": ruins ridge rocks=" + ridge + ", cliff rocks=" + cliffs + " | " + fishLog;
    }

    // ------------------------------------------------------------------ the ruins wall

    private static int BuildRuinsWall(Plan p, Transform root, Rocks rocks)
    {
        Transform ruins = GameObject.Find("Ancient Ruins").transform;
        Vector3 start, end, outward;
        float gateT;
        if (p.wallSide == 2) { start = new Vector3(-158f, 0f, p.wallCoord); end = new Vector3(138f, 0f, p.wallCoord); outward = Vector3.forward; gateT = 158f; }
        else { start = new Vector3(p.wallCoord, 0f, -50f); end = new Vector3(p.wallCoord, 0f, 60f); outward = p.wallSide == 0 ? Vector3.right : Vector3.left; gateT = 50f; }

        // the old slabs stay as invisible colliders, but now reach from the sand to the surface
        foreach (string n in new string[] { "Wall Left", "Wall Right", "Lintel", "Gate Slab" })
        {
            Transform t = ruins.Find(n);
            if (t == null) continue;
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }
        Transform left = ruins.Find("Wall Left"), right = ruins.Find("Wall Right"), lintel = ruins.Find("Lintel");
        foreach (Transform w in new Transform[] { left, right })
        {
            if (w == null) continue;
            Vector3 s = w.localScale, pos = w.position;
            s.y = 67f; pos.y = 28.5f;
            w.localScale = s; w.position = pos;
        }
        if (lintel != null)
        {
            Vector3 s = lintel.localScale, pos = lintel.position;
            s.y = 56f; pos.y = 34f;
            lintel.localScale = s; lintel.position = pos;
        }
        // Level 1 only: the right wall stopped short of the level edge, so swimmers could go around the gate
        if (p.wallSide == 0 && right != null)
        {
            Vector3 s = right.localScale, pos = right.position;
            s.z = 56f; pos.z = 32f;
            right.localScale = s; right.position = pos;
        }

        Transform ridgeRoot = new GameObject("Ruins Ridge").transform;
        ridgeRoot.SetParent(root, false);
        int count = Ridge(ridgeRoot, start, end, outward, 4.5f, 6.5f, 0f, 0f, false, gateT, 4f, rocks, 100 + p.scene.Length * 7 + p.wallSide);

        // the gate slab becomes a boulder (it still sinks into the seabed when the pearls are found)
        Transform slab = ruins.Find("Gate Slab");
        if (slab != null)
        {
            Transform oldBoulder = slab.Find("Gate Boulder");
            if (oldBoulder != null) Object.DestroyImmediate(oldBoulder.gameObject);
            Vector3 dir = (end - start).normalized;
            GameObject g = new GameObject("Gate Boulder");
            float height = 11.6f, width = 9.8f, thick = 5f;
            g.transform.position = new Vector3(slab.position.x, -4.8f + 0.5f * (height / 1.45f), slab.position.z);
            g.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg, 0f);
            g.transform.localScale = new Vector3(width / 2f, height / 1.45f, thick / 2f);
            g.AddComponent<MeshFilter>().sharedMesh = rocks.visual[1];
            g.AddComponent<MeshRenderer>().sharedMaterial = rocks.mats[1];
            g.transform.SetParent(slab, true);
            count++;
        }
        return count;
    }

    // ------------------------------------------------------------------ cliffs around the rest of the level

    private static int BuildCliffs(Plan p, Transform root, Rocks rocks)
    {
        Transform cliffRoot = new GameObject("Perimeter Cliffs").transform;
        cliffRoot.SetParent(root, false);
        int count = 0;
        for (int side = 0; side < 4; side++)
        {
            // 0 = +X, 1 = -X, 2 = +Z, 3 = -Z; the ruins wall already closes one side
            if (side == p.wallSide) continue;
            Vector3 start, end, outward;
            SideLine(p, side, 6f, out start, out end, out outward);
            count += Ridge(cliffRoot, start, end, outward, 8f, 13f, 5f, 2.2f, true, -1f, 0f, rocks, 500 + side * 31 + p.scene.Length);
        }
        return count;
    }

    private static void SideLine(Plan p, int side, float extend, out Vector3 start, out Vector3 end, out Vector3 outward)
    {
        if (side == 0) { start = new Vector3(p.maxX, 0f, p.minZ - extend); end = new Vector3(p.maxX, 0f, p.maxZ + extend); outward = Vector3.right; }
        else if (side == 1) { start = new Vector3(p.minX, 0f, p.minZ - extend); end = new Vector3(p.minX, 0f, p.maxZ + extend); outward = Vector3.left; }
        else if (side == 2) { start = new Vector3(p.minX - extend, 0f, p.maxZ); end = new Vector3(p.maxX + extend, 0f, p.maxZ); outward = Vector3.forward; }
        else { start = new Vector3(p.minX - extend, 0f, p.minZ); end = new Vector3(p.maxX + extend, 0f, p.minZ); outward = Vector3.back; }
    }

    /// <summary>Invisible colliders behind the cliffs and above the water, so the fish and the player stay in the level.</summary>
    private static void BuildBounds(Plan p, Transform root)
    {
        Transform b = new GameObject("Invisible Bounds").transform;
        b.SetParent(root, false);
        for (int side = 0; side < 4; side++)
        {
            if (side == p.wallSide) continue;
            Vector3 start, end, outward;
            SideLine(p, side, 10f, out start, out end, out outward);
            GameObject g = new GameObject("Bound " + side);
            g.transform.SetParent(b, false);
            Vector3 mid = (start + end) * 0.5f + outward * 2f;
            g.transform.position = new Vector3(mid.x, 28f, mid.z);
            Vector3 along = end - start;
            bool alongX = Mathf.Abs(along.x) > Mathf.Abs(along.z);
            g.AddComponent<BoxCollider>().size = alongX ? new Vector3(Mathf.Abs(along.x), 72f, 4f) : new Vector3(4f, 72f, Mathf.Abs(along.z));
        }
        GameObject ceiling = new GameObject("Ceiling");
        ceiling.transform.SetParent(b, false);
        ceiling.transform.position = new Vector3((p.minX + p.maxX) * 0.5f, 63f, (p.minZ + p.maxZ) * 0.5f);
        ceiling.AddComponent<BoxCollider>().size = new Vector3(p.maxX - p.minX + 40f, 2f, p.maxZ - p.minZ + 40f);
    }

    // ------------------------------------------------------------------ rock ridge placement

    private static int Ridge(Transform parent, Vector3 start, Vector3 end, Vector3 outward, float thickMin, float thickMax,
                             float outwardBase, float lean, bool wobble, float gateT, float gateHalf, Rocks rocks, int seed)
    {
        System.Random rng = new System.Random(seed);
        Vector3 line = end - start;
        float length = line.magnitude;
        Vector3 dir = line / length;
        int count = 0;

        for (int li = 0; li < Layers.Length; li++)
        {
            float t = -(float)rng.NextDouble() * 4f;
            while (t < length + 4f)
            {
                float width = Rand(rng, 7f, 15f);
                float height = Layers[li].height * Rand(rng, 0.95f, 1.3f);
                float thick = Rand(rng, thickMin, thickMax);
                float center = t + width * 0.5f;
                t += width * Rand(rng, 0.5f, 0.68f);   // neighbours overlap, so there are no gaps

                // keep the doorway (and the space above it) free of the low rows
                if (gateT >= 0f && li < 2 && Mathf.Abs(center - gateT) < gateHalf + width * 0.35f) continue;

                float offset = outwardBase + lean * li + Rand(rng, -1.5f, 1.5f);
                if (wobble) offset += (Mathf.PerlinNoise(center * 0.045f + seed, li * 3.1f) - 0.5f) * 8f;
                PlaceRock(parent, start, dir, outward, center, Layers[li].bottom, height, width, thick, offset, rng, rocks);
                count++;
            }
        }

        // rocks across the top of the doorway
        if (gateT >= 0f)
        {
            for (int i = -1; i <= 1; i++)
            {
                PlaceRock(parent, start, dir, outward, gateT + i * 3.8f, 5.2f, 13f, 8.4f, thickMax, 0f, rng, rocks);
                count++;
            }
        }
        // loose boulders and rubble along the foot of the ridge, on the player's side
        for (float t = 0f; t < length; t += Rand(rng, 5f, 9f))
        {
            if (rng.NextDouble() > 0.6) continue;
            if (gateT >= 0f && Mathf.Abs(t - gateT) < gateHalf + 3f) continue;
            float size = Rand(rng, 2.5f, 7f);
            PlaceRock(parent, start, dir, outward, t, -5.5f, size * Rand(rng, 0.6f, 1f), size, size * 0.9f, -(thickMax * 0.5f + Rand(rng, 0f, 5f)), rng, rocks);
            count++;
        }
        return count;
    }

    private static void PlaceRock(Transform parent, Vector3 start, Vector3 dir, Vector3 outward, float along, float bottomY,
                                  float height, float width, float thick, float outwardOffset, System.Random rng, Rocks rocks)
    {
        GameObject g = new GameObject("Rock");
        g.transform.SetParent(parent, false);
        Vector3 pos = start + dir * along + outward * outwardOffset;
        pos.y = bottomY + 0.5f * (height / 1.45f);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg, 0f)
                               * Quaternion.Euler(Rand(rng, -6f, 6f), Rand(rng, -14f, 14f), Rand(rng, -6f, 6f));
        g.transform.localScale = new Vector3(width / 2f, height / 1.45f, thick / 2f);

        int v = rng.Next(Variants);
        g.AddComponent<MeshFilter>().sharedMesh = rocks.visual[v];
        double m = rng.NextDouble();
        g.AddComponent<MeshRenderer>().sharedMaterial = rocks.mats[m < 0.3 ? 0 : (m < 0.6 ? 1 : (m < 0.8 ? 2 : 3))];
        MeshCollider mc = g.AddComponent<MeshCollider>();
        mc.sharedMesh = rocks.collision[v];
        mc.convex = true;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
    }

    /// <summary>Finds a top-level object by name (GameObject.Find can return a child with the same name instead).</summary>
    private static GameObject RootByName(string name)
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }

    private static float Rand(System.Random rng, float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

    // ------------------------------------------------------------------ fish

    private static string SpreadFish(Plan p, int level)
    {
        GameObject fishRootGO = RootByName("Fish");
        if (fishRootGO == null) return "no Fish root";
        Transform fishRoot = fishRootGO.transform;

        // remove fish added by an earlier run
        for (int i = fishRoot.childCount - 1; i >= 0; i--)
            if (fishRoot.GetChild(i).name.Contains("(spread")) Object.DestroyImmediate(fishRoot.GetChild(i).gameObject);

        List<EntityData> fish = new List<EntityData>();
        foreach (Transform c in fishRoot)
        {
            EntityData e = c.GetComponent<EntityData>();
            if (e != null && (c.GetComponent<PreyFish>() != null || c.GetComponent<PredatorFish>() != null)) fish.Add(e);
        }

        System.Random rng = new System.Random(1000 + level);

        // extra small fish, copies of ordinary prey already in the level
        List<EntityData> prey = fish.FindAll(f => f.creatureType == CreatureType.Prey);
        int added = 0;
        for (int i = 0; i < p.addFish && prey.Count > 0; i++)
        {
            EntityData src = prey[rng.Next(prey.Count)];
            GameObject clone = Object.Instantiate(src.gameObject, fishRoot);
            clone.name = src.name.Split(' ')[0] + " (spread " + (i + 1) + ")";
            fish.Add(clone.GetComponent<EntityData>());
            added++;
        }

        float margin = 14f;
        float x0 = p.minX + margin, x1 = p.maxX - margin, z0 = p.minZ + margin, z1 = p.maxZ - margin;
        Vector2 spawn = new Vector2(p.spawn.x, p.spawn.z);
        List<Vector2> occupied = new List<Vector2> { spawn };
        foreach (Pearl pearl in Object.FindObjectsByType<Pearl>(FindObjectsSortMode.None))
            occupied.Add(new Vector2(pearl.transform.position.x, pearl.transform.position.z));

        // big hunters first so they end up far apart, then glowing creatures, then ordinary prey
        fish.Sort((a, b) => Rank(a).CompareTo(Rank(b)));

        int nearStart = 0;
        float minX = 1e9f, maxX = -1e9f;
        foreach (EntityData e in fish)
        {
            bool predator = e.creatureType == CreatureType.Predator;
            bool openingFood = !predator && e.creatureType == CreatureType.Prey && nearStart < 3;
            Vector2 best = Vector2.zero;
            float bestScore = -1f;
            for (int c = 0; c < 140; c++)
            {
                Vector2 cand = new Vector2(Rand(rng, x0, x1), Rand(rng, z0, z1));
                float dSpawn = Vector2.Distance(cand, spawn);
                if (predator && (dSpawn < 38f || dSpawn > 85f)) continue;   // hunters patrol the middle distance: found while exploring, not at the start
                if (!predator && dSpawn < 8f) continue;
                if (openingFood && (dSpawn < 14f || dSpawn > 30f)) continue;
                float score = float.MaxValue;
                foreach (Vector2 o in occupied) score = Mathf.Min(score, Vector2.Distance(cand, o));
                if (score > bestScore) { bestScore = score; best = cand; }
            }
            if (bestScore < 0f) best = new Vector2(Rand(rng, x0, x1), Rand(rng, z0, z1));
            if (openingFood) nearStart++;

            float y = Mathf.Clamp(e.transform.position.y, -4f, 12f);
            e.transform.position = new Vector3(best.x, y, best.y);
            Rigidbody rb = e.GetComponent<Rigidbody>();
            if (rb != null) { rb.position = e.transform.position; rb.linearVelocity = Vector3.zero; }
            occupied.Add(best);
            minX = Mathf.Min(minX, best.x); maxX = Mathf.Max(maxX, best.x);
        }
        return "fish=" + fish.Count + " (added " + added + "), spread over x [" + minX.ToString("0") + ".." + maxX.ToString("0") + "]";
    }

    private static int Rank(EntityData e)
    {
        if (e.creatureType == CreatureType.Predator) return 0;
        if (e.creatureType == CreatureType.AbilityCreature) return 1;
        return 2;
    }

    // ------------------------------------------------------------------ generated assets (meshes, texture, materials)

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static Rocks EnsureRockAssets()
    {
        EnsureFolder(EnvDir + "/Meshes");
        EnsureFolder(EnvDir + "/Textures");
        EnsureFolder(EnvDir + "/Materials");
        Rocks r = new Rocks();

        for (int i = 0; i < Variants; i++)
        {
            string vp = EnvDir + "/Meshes/Rock_v3_" + i + ".asset";
            Mesh vm = AssetDatabase.LoadAssetAtPath<Mesh>(vp);
            if (vm == null) { vm = BuildRock(i * 7 + 3, 3, true); vm.name = "Rock_v3_" + i; AssetDatabase.CreateAsset(vm, vp); }
            r.visual[i] = vm;

            string cp = EnvDir + "/Meshes/RockCollider_v3_" + i + ".asset";
            Mesh cm = AssetDatabase.LoadAssetAtPath<Mesh>(cp);
            if (cm == null) { cm = BuildRock(i * 7 + 3, 1, false); cm.name = "RockCollider_v3_" + i; AssetDatabase.CreateAsset(cm, cp); }
            r.collision[i] = cm;
        }

        string tp = EnvDir + "/Textures/rock_albedo_v2.png";
        if (!File.Exists(tp))
        {
            File.WriteAllBytes(tp, MakeRockTexture().EncodeToPNG());
            AssetDatabase.ImportAsset(tp);
            TextureImporter ti = AssetImporter.GetAtPath(tp) as TextureImporter;
            if (ti != null) { ti.wrapMode = TextureWrapMode.Repeat; ti.anisoLevel = 4; ti.SaveAndReimport(); }
        }
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);

        r.mats[0] = RockMat("OceanRock_Dark", new Color(0.62f, 0.66f, 0.72f), tex);
        r.mats[1] = RockMat("OceanRock_Slate", new Color(0.85f, 0.88f, 0.92f), tex);
        r.mats[2] = RockMat("OceanRock_Warm", new Color(0.88f, 0.80f, 0.73f), tex);
        r.mats[3] = RockMat("OceanRock_Algae", new Color(0.74f, 0.80f, 0.75f), tex);
        return r;
    }

    private static Material RockMat(string name, Color tint, Texture2D tex)
    {
        string path = EnvDir + "/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", 0.12f);
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>A lumpy, flat-shaded boulder: a subdivided icosahedron pushed around with noise, flat on the bottom.</summary>
    private static Mesh BuildRock(int seed, int subdiv, bool flat)
    {
        List<Vector3> verts = new List<Vector3>();
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        Vector3[] ico =
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
        };
        foreach (Vector3 v in ico) verts.Add(v.normalized);
        List<int> tris = new List<int>
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };

        for (int s = 0; s < subdiv; s++)
        {
            Dictionary<long, int> cache = new Dictionary<long, int>();
            List<int> next = new List<int>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                int ab = Mid(verts, cache, a, b), bc = Mid(verts, cache, b, c), ca = Mid(verts, cache, c, a);
                next.AddRange(new int[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            tris = next;
        }

        float ox = seed * 17.3f + 40f;
        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 d = verts[i];
            // push the sphere toward a rounded box, so the rocks look like blocky boulders instead of eggs
            const float k = 0.74f;
            Vector3 q = new Vector3(Mathf.Sign(d.x) * Mathf.Pow(Mathf.Abs(d.x), k), Mathf.Sign(d.y) * Mathf.Pow(Mathf.Abs(d.y), k), Mathf.Sign(d.z) * Mathf.Pow(Mathf.Abs(d.z), k));
            float n1 = Noise3(d * 1.1f + new Vector3(ox, ox * 0.7f, ox * 1.3f));
            float n2 = Noise3(d * 3.4f + new Vector3(ox * 1.9f, ox, ox * 0.3f));
            float n3 = Noise3(d * 8f + new Vector3(ox * 0.4f, ox * 2.1f, ox));
            float disp = 1f + (n1 - 0.5f) * 0.8f + (n2 - 0.5f) * 0.5f + (n3 - 0.5f) * 0.24f;
            Vector3 p = q * 0.82f * disp;
            p.y = Mathf.Lerp(p.y, Mathf.Round(p.y * 5f) / 5f, 0.35f);          // faint horizontal strata steps
            if (p.y < -0.5f) p.y = -0.5f + (p.y + 0.5f) * 0.1f;                // flat bottom, to sit in the sand
            verts[i] = p;
        }

        Mesh mesh = new Mesh();
        if (flat)
        {
            List<Vector3> fv = new List<Vector3>(), fn = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> ft = new List<int>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = verts[tris[i]], b = verts[tris[i + 1]], c = verts[tris[i + 2]];
                Vector3 nrm = Vector3.Cross(b - a, c - a).normalized;
                Vector3 an = new Vector3(Mathf.Abs(nrm.x), Mathf.Abs(nrm.y), Mathf.Abs(nrm.z));
                foreach (Vector3 q in new Vector3[] { a, b, c })
                {
                    fv.Add(q);
                    fn.Add(nrm);
                    // box projection so the texture never stretches
                    Vector2 w = an.x > an.y && an.x > an.z ? new Vector2(q.z, q.y) : (an.y > an.z ? new Vector2(q.x, q.z) : new Vector2(q.x, q.y));
                    uv.Add(w * 2f);
                    ft.Add(fv.Count - 1);
                }
            }
            mesh.SetVertices(fv);
            mesh.SetNormals(fn);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(ft, 0);
        }
        else
        {
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
        }
        mesh.RecalculateBounds();
        return mesh;
    }

    private static int Mid(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
    {
        long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
        int idx;
        if (cache.TryGetValue(key, out idx)) return idx;
        verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
        idx = verts.Count - 1;
        cache[key] = idx;
        return idx;
    }

    private static float Noise3(Vector3 p)
    {
        return (Mathf.PerlinNoise(p.x, p.y) + Mathf.PerlinNoise(p.y, p.z) + Mathf.PerlinNoise(p.z, p.x)) / 3f;
    }

    /// <summary>A tileable grey-blue rock texture: soft noise, horizontal strata and a few dark cracks.</summary>
    private static Texture2D MakeRockTexture()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float n = Tile(u, v, 4, 0f) * 0.5f + Tile(u, v, 8, 7f) * 0.3f + Tile(u, v, 16, 3f) * 0.2f;
                float strata = 0.96f + 0.04f * Mathf.Sin(v * Mathf.PI * 2f * 10f + n * 6f);
                float ridged = 1f - Mathf.Abs(2f * Tile(u, v, 12, 11f) - 1f);
                float crack = ridged > 0.975f ? 0.82f : 1f;
                float grain = 0.94f + 0.12f * Tile(u, v, 32, 5f);
                float c = (0.44f + n * 0.34f) * strata * crack * grain;
                px[y * size + x] = new Color(c * 0.95f, c, c * 1.05f, 1f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    private static float Tile(float u, float v, int freq, float offset)
    {
        float U = u * freq + 100f + offset, V = v * freq + 100f + offset;
        float a = Mathf.PerlinNoise(U, V);
        float b = Mathf.PerlinNoise(U - freq, V);
        float c = Mathf.PerlinNoise(U, V - freq);
        float d = Mathf.PerlinNoise(U - freq, V - freq);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }
}
#endif
