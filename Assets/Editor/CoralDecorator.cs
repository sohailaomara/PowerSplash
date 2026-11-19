#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fills the empty seabed using only models that are already imported in the project (Assets/Corals):
///  - copies of the corals already placed in each level (keeping the scale and height you set), and
///  - the other coral models from Assets/Corals that the level does not use yet.
/// Copies are spread over the whole play area with a minimum spacing, kept clear of the player's start, the pearls,
/// the story characters, the ship and the gate, and very heavy models (hundreds of thousands of vertices) are only copied
/// a couple of times so the frame rate stays healthy. Nothing here is built from primitive shapes.
/// Menu: Story > Ocean > Decorate Level 1 / 2 / 3. Safe to run again (the old "Seabed Decor" object is replaced).
/// </summary>
public static class CoralDecorator
{
    [MenuItem("Story/Ocean/Decorate Level 1")] public static void Level1Menu() { Decorate(1); }
    [MenuItem("Story/Ocean/Decorate Level 2")] public static void Level2Menu() { Decorate(2); }
    [MenuItem("Story/Ocean/Decorate Level 3")] public static void Level3Menu() { Decorate(3); }

    private struct Circle
    {
        public Vector2 c;
        public float r;
        public Circle(Vector2 center, float radius) { c = center; r = radius; }
    }

    private class Area
    {
        public string scene;
        public float minX, maxX, minZ, maxZ;
        public Vector2 spawn, gate;
        public int count;
    }

    /// <summary>One thing that can be copied: a coral already in the scene, or a coral model from Assets/Corals.</summary>
    private class Source
    {
        public Transform sceneObject;      // copy this scene object (keeps its scale and height)
        public GameObject prefab;          // or instantiate this model
        public float targetHeight;         // prefab only: scale the model to this height
        public int vertices;
        public int cap;                    // most copies allowed
        public int weight;
        public int used;
        public string label;
    }

    private static Area AreaFor(int level)
    {
        Area a = new Area();
        if (level == 1)
        {
            a.scene = "Level 1"; a.minX = -154f; a.maxX = 26f; a.minZ = -48f; a.maxZ = 59f;
            a.spawn = new Vector2(-0.8f, 6.4f); a.gate = new Vector2(28f, 0f); a.count = 55;
        }
        else if (level == 2)
        {
            a.scene = "Level 2"; a.minX = -154f; a.maxX = 134f; a.minZ = -48f; a.maxZ = 27f;
            a.spawn = new Vector2(7.7f, -1.8f); a.gate = new Vector2(0f, 28f); a.count = 60;
        }
        else
        {
            a.scene = "Level 3"; a.minX = -26f; a.maxX = 134f; a.minZ = -48f; a.maxZ = 59f;
            a.spawn = new Vector2(7.7f, -1.8f); a.gate = new Vector2(-28f, 0f); a.count = 34;   // the Deep is barer
        }
        return a;
    }

    public static string Decorate(int level)
    {
        Area a = AreaFor(level);
        EditorSceneManager.SaveOpenScenes();
        Scene sc = EditorSceneManager.OpenScene("Assets/Scenes/" + a.scene + ".unity", OpenSceneMode.Single);

        GameObject old = RootByName("Seabed Decor");
        if (old != null) Object.DestroyImmediate(old);
        Transform root = new GameObject("Seabed Decor").transform;

        System.Random rng = new System.Random(7000 + level);
        List<Circle> placed = new List<Circle>();
        placed.Add(new Circle(a.spawn, 16f));
        placed.Add(new Circle(a.gate, 18f));
        foreach (Pearl p in Object.FindObjectsByType<Pearl>(FindObjectsSortMode.None))
            placed.Add(new Circle(new Vector2(p.transform.position.x, p.transform.position.z), 7f));
        GameObject story = RootByName("Story World");
        if (story != null)
        {
            foreach (Transform t in story.transform)
            {
                if (t.GetComponent<NarrativeTrigger>() != null) continue;   // invisible trigger zones
                placed.Add(new Circle(new Vector2(t.position.x, t.position.z), t.name.Contains("ship") ? 18f : 10f));
            }
        }

        // ---- what can be copied
        List<Source> sources = new List<Source>();
        GameObject coralsRoot = RootByName("Corals");
        HashSet<string> usedModels = new HashSet<string>();
        if (coralsRoot != null)
        {
            foreach (Transform c in coralsRoot.transform)
            {
                placed.Add(new Circle(new Vector2(c.position.x, c.position.z), 3.5f));
                int v = VertexCount(c);
                usedModels.Add(ModelName(c));
                sources.Add(new Source { sceneObject = c, vertices = v, cap = CapFor(v, 10), weight = v < 100000 ? 3 : 1, label = c.name });
            }
        }
        // the coral models in Assets/Corals that this level does not use yet
        foreach (string guid in AssetDatabase.FindAssets("t:GameObject", new string[] { "Assets/Corals" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = path.ToLowerInvariant();
            if (!(lower.EndsWith(".glb") || lower.EndsWith(".fbx") || lower.EndsWith(".blend"))) continue;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || usedModels.Contains(System.IO.Path.GetFileNameWithoutExtension(path))) continue;
            int v = VertexCount(prefab.transform);
            if (v > 100000) continue;                         // very heavy and unused: leave it out
            if (BoundsOf(prefab).size.y < 0.0001f) continue;
            sources.Add(new Source { prefab = prefab, vertices = v, cap = CapFor(v, 6), weight = 1, targetHeight = 2.6f + (float)rng.NextDouble() * 1.6f, label = System.IO.Path.GetFileNameWithoutExtension(path) });
        }
        if (sources.Count == 0) return a.scene + ": no coral sources found";

        // ---- place copies
        Transform holder = new GameObject("Corals").transform;
        holder.SetParent(root, false);
        int made = 0;
        Dictionary<string, int> tally = new Dictionary<string, int>();
        for (int i = 0; i < a.count; i++)
        {
            Source src = PickSource(rng, sources);
            if (src == null) break;
            float footprint = src.sceneObject != null ? Mathf.Clamp(Footprint(src.sceneObject) * 0.5f, 2f, 6f) : 3f;
            Vector2 p;
            if (!TryPlace(rng, a, placed, footprint, out p)) break;

            GameObject g;
            if (src.sceneObject != null)
            {
                g = Object.Instantiate(src.sceneObject.gameObject, holder);
                g.transform.position = new Vector3(p.x, src.sceneObject.position.y, p.y);
                g.transform.rotation = Quaternion.AngleAxis(Rand(rng, 0f, 360f), Vector3.up) * src.sceneObject.rotation;
                g.transform.localScale = src.sceneObject.localScale * Rand(rng, 0.85f, 1.25f);
            }
            else
            {
                g = (GameObject)PrefabUtility.InstantiatePrefab(src.prefab, holder);
                Bounds b = BoundsOf(g);
                g.transform.localScale = g.transform.localScale * (src.targetHeight / Mathf.Max(b.size.y, 0.0001f));
                b = BoundsOf(g);
                g.transform.position += new Vector3(p.x - b.center.x, (-4.5f - 0.1f) - b.min.y, p.y - b.center.z);   // on the sand, slightly sunk in
                g.transform.rotation = Quaternion.AngleAxis(Rand(rng, 0f, 360f), Vector3.up) * g.transform.rotation;
            }
            g.name = src.label + " (decor)";
            src.used++;
            placed.Add(new Circle(p, footprint));
            int n; tally.TryGetValue(src.label, out n); tally[src.label] = n + 1;
            made++;
        }

        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
        string list = "";
        foreach (var kv in tally) list += kv.Key + " x" + kv.Value + "; ";
        return a.scene + ": copies placed=" + made + " | " + list;
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>The most copies a model may get, by how heavy it is. Models over a million vertices are never copied.</summary>
    private static int CapFor(int vertices, int light)
    {
        if (vertices < 100000) return light;
        if (vertices < 1000000) return 1;
        return 0;
    }

    private static Source PickSource(System.Random rng, List<Source> sources)
    {
        List<Source> open = new List<Source>();
        int sum = 0;
        foreach (Source s in sources)
        {
            if (s.used >= s.cap) continue;
            open.Add(s);
            sum += s.weight;
        }
        if (open.Count == 0) return null;
        int roll = rng.Next(sum);
        foreach (Source s in open)
        {
            roll -= s.weight;
            if (roll < 0) return s;
        }
        return open[0];
    }

    private static bool TryPlace(System.Random rng, Area a, List<Circle> placed, float radius, out Vector2 p)
    {
        const float margin = 10f;
        for (int attempt = 0; attempt < 60; attempt++)
        {
            p = new Vector2(Rand(rng, a.minX + margin, a.maxX - margin), Rand(rng, a.minZ + margin, a.maxZ - margin));
            bool ok = true;
            foreach (Circle c in placed)
                if (Vector2.Distance(p, c.c) < c.r + radius + 3f) { ok = false; break; }
            if (ok) return true;
        }
        p = Vector2.zero;
        return false;
    }

    private static int VertexCount(Transform t)
    {
        int n = 0;
        foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh != null) n += mf.sharedMesh.vertexCount;
        foreach (SkinnedMeshRenderer s in t.GetComponentsInChildren<SkinnedMeshRenderer>()) if (s.sharedMesh != null) n += s.sharedMesh.vertexCount;
        return n;
    }

    private static string ModelName(Transform t)
    {
        foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            string ap = AssetDatabase.GetAssetPath(mf.sharedMesh);
            if (!string.IsNullOrEmpty(ap)) return System.IO.Path.GetFileNameWithoutExtension(ap);
        }
        return t.name;
    }

    private static Bounds BoundsOf(GameObject g)
    {
        Renderer[] rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    private static float Footprint(Transform t)
    {
        Bounds b = BoundsOf(t.gameObject);
        return Mathf.Max(b.size.x, b.size.z);
    }

    private static float Rand(System.Random rng, float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

    private static GameObject RootByName(string name)
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }
}
#endif
