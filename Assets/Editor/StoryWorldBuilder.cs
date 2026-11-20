#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool that places the story content into the level scenes: NPCs, narrative triggers, message bottles
/// and environmental-storytelling props, all under a "Story World" object so they can be moved by hand afterwards.
/// Run it from the Unity menu: Story > Build Level 1 / 2 / 3 / Main Menu Scene. It is safe to run again
/// (the old "Story World" is replaced).
/// </summary>
public static class StoryWorldBuilder
{
    private const string FishDir = "Assets/Fish/";
    private const string ScenesDir = "Assets/Scenes/";

    // ------------------------------------------------------------------ menu entries

    [MenuItem("Story/Build Level 1")] public static void BuildLevel1Menu() { BuildLevel1(); }
    [MenuItem("Story/Build Level 2")] public static void BuildLevel2Menu() { BuildLevel2(); }
    [MenuItem("Story/Build Level 3")] public static void BuildLevel3Menu() { BuildLevel3(); }
    [MenuItem("Story/Build Main Menu Scene")] public static void BuildMenuMenu() { BuildMenuScene(); }

    // ------------------------------------------------------------------ levels

    public static string BuildLevel1()
    {
        Scene sc = Open(ScenesDir + "Level 1.unity");
        Transform w = NewRoot();

        StoryNPC marlo = MakeNpc(w, "Marlo", FishDir + "delfino_done.fbx", 16f, new Vector3(-18f, 8f, 18f), new Color(0.5f, 0.9f, 1f),
            C("", "metMentor", "l1_marlo_first"), C("foundBottle1", "", "l1_marlo_hint2"), C("metMentor", "", "l1_marlo_hint1"));
        marlo.swimRadius = 6.5f;
        marlo.swimSpeed = 2f;

        Vector3 bottlePos = ShipWreck(w, new Vector3(-40f, -3.2f, 32f));
        Bottle(w, bottlePos, "l1_bottle1", "foundBottle1");
        Zone(w, "Zone - Old Wreck", new Vector3(-40f, 3f, 32f), new Vector3(36f, 24f, 36f), "l1_wreck_zone", "", "");
        SceneStart(w, "Opening Crawl", "intro_crawl", 0.4f, "", "introSeen");
        OnFlag(w, "Beat - First Ruin Awakens", "l1_gate_open", "pearls1Collected");

        return Save(sc, "Level 1");
    }

    public static string BuildLevel2()
    {
        Scene sc = Open(ScenesDir + "Level 2.unity");
        Transform w = NewRoot();

        StoryNPC lumi2 = MakeNpc(w, "Lumi", FishDir + "Anglerfish_Final.fbx", 10f, new Vector3(-26f, 10f, 8f), new Color(1f, 0.85f, 0.4f),
            C("tookShortcut", "", "l2_lumi_after_shortcut"), C("savedPip", "", "l2_lumi_after_pip"), C("", "metAnglerfish", "l2_lumi_first"));
        lumi2.swimRadius = 4f;
        lumi2.swimSpeed = 1.2f;

        StoryNPC pip = MakeNpc(w, "Pip", FishDir + "nemofish.fbx", 3.5f, new Vector3(-32f, 3f, 12f), new Color(1f, 0.6f, 0.3f),
            C("", "choiceMade", "l2_pip_first"), C("savedPip", "", "l2_pip_joined"), C("tookShortcut", "", "l2_pip_left"));
        pip.bob = false;
        pip.sway = false;
        pip.labelHeight = 3.5f;
        pip.gameObject.AddComponent<StoryCompanion>().followWhenFlag = "savedPip";

        Bottle(w, new Vector3(30f, 1.5f, -28f), "l2_bottle2", "foundBottle2");
        SceneStart(w, "Arrival in the Open Water", "l2_arrival", 3.5f, "", "enteredOpenWater");

        return Save(sc, "Level 2");
    }

    public static string BuildLevel3()
    {
        Scene sc = Open(ScenesDir + "Level 3.unity");
        Transform w = NewRoot();

        StoryNPC lumi = MakeNpc(w, "Lumi", FishDir + "Anglerfish_Final.fbx", 10f, new Vector3(14f, 9f, 6f), new Color(1f, 0.85f, 0.4f),
            C("pearls3Collected", "", "l3_lumi_after"), C("", "", "l3_lumi_hint"));
        lumi.existsOnlyWhenFlag = "tookShortcut";
        lumi.swimRadius = 4f;
        lumi.swimSpeed = 1.2f;

        StoryNPC pip = MakeNpc(w, "Pip", FishDir + "nemofish.fbx", 3.5f, new Vector3(2f, 9f, -7f), new Color(1f, 0.6f, 0.3f),
            C("pearls3Collected", "", "l3_pip_after"), C("", "", "l3_pip_chat"));
        pip.existsOnlyWhenFlag = "savedPip";
        pip.bob = false;
        pip.sway = false;
        pip.labelHeight = 3.5f;
        pip.gameObject.AddComponent<StoryCompanion>().followWhenFlag = "savedPip";

        Bottle(w, new Vector3(33f, 1.5f, 16f), "l3_bottle3", "foundBottle3");
        Zone(w, "Zone - Sea King's Chamber", new Vector3(0f, 2f, 0f), new Vector3(46f, 34f, 46f), "l3_ruins_zone", "pearls3Collected", "goldenLightTaken");
        SceneStart(w, "Arrival (Pip)", "l3_arrival_pip", 3.5f, "savedPip", "");
        SceneStart(w, "Arrival (Lumi)", "l3_arrival_lumi", 3.5f, "tookShortcut", "");
        OnFlag(w, "Beat - Final Ruin Awakens", "l3_gate_open", "pearls3Collected");

        return Save(sc, "Level 3");
    }

    public static string BuildMenuScene()
    {
        EditorSceneManager.SaveOpenScenes();
        Scene sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.1f, 0.2f);
        camGO.AddComponent<AudioListener>();

        new GameObject("Main Menu").AddComponent<MainMenu>();

        EditorSceneManager.SaveScene(sc, ScenesDir + "MainMenu.unity");

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(ScenesDir + "MainMenu.unity", true),
            new EditorBuildSettingsScene(ScenesDir + "Level 1.unity", true),
            new EditorBuildSettingsScene(ScenesDir + "Level 2.unity", true),
            new EditorBuildSettingsScene(ScenesDir + "Level 3.unity", true)
        };
        return "Main menu scene saved; build order = MainMenu, Level 1, Level 2, Level 3";
    }

    // ------------------------------------------------------------------ scene helpers

    private static Scene Open(string path)
    {
        EditorSceneManager.SaveOpenScenes();
        return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    private static Transform NewRoot()
    {
        GameObject old = GameObject.Find("Story World");
        if (old != null) Object.DestroyImmediate(old);
        return new GameObject("Story World").transform;
    }

    private static string Save(Scene sc, string name)
    {
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
        GameObject w = GameObject.Find("Story World");
        return name + " built: " + (w != null ? w.transform.childCount : 0) + " story objects";
    }

    private static DialogueData D(string name)
    {
        DialogueData d = AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/Resources/Story/" + name + ".asset");
        if (d == null) Debug.LogWarning("[StoryWorldBuilder] missing dialogue asset: " + name);
        return d;
    }

    private static StoryNPC.Conversation C(string requires, string blockedBy, string asset)
    {
        return new StoryNPC.Conversation { requiresFlag = requires, blockedByFlag = blockedBy, dialogue = D(asset) };
    }

    private static Vector3 Free(Vector3 p, float radius)
    {
        Physics.SyncTransforms();
        for (int i = 0; i < 12 && Physics.CheckSphere(p, radius); i++) p.y += 2.5f;
        return p;
    }

    // ------------------------------------------------------------------ materials and primitives

    private static Material Mat(string name, Color color, float emission = 0f)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Story")) AssetDatabase.CreateFolder("Assets/Materials", "Story");
        string path = "Assets/Materials/Story/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", 0.15f);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * emission);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 euler, Vector3 scale, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localEulerAngles = euler;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    private static Light AddLight(Transform parent, Vector3 localPos, Color color, float intensity, float range)
    {
        GameObject g = new GameObject("Glow");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        Light l = g.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        return l;
    }

    // ------------------------------------------------------------------ characters

    private static StoryNPC MakeNpc(Transform parent, string npcName, string modelPath, float length, Vector3 pos, Color glow, params StoryNPC.Conversation[] conversations)
    {
        GameObject root = new GameObject("NPC - " + npcName);
        root.transform.SetParent(parent, false);
        root.transform.position = Free(pos, length * 0.4f);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (prefab != null) { AddModel(root.transform, prefab, length); ApplyTextureFix(root, modelPath); }
        else Debug.LogWarning("[StoryWorldBuilder] missing model " + modelPath);

        StoryNPC npc = root.AddComponent<StoryNPC>();
        npc.npcName = npcName;
        npc.conversations = conversations;
        npc.labelHeight = length * 0.45f + 2f;
        AddLight(root.transform, Vector3.up * 1f, glow, 3f, length * 1.6f);
        return npc;
    }

    /// <summary>The dolphin FBX arrives with an untextured material; use the project's textured Delfino material instead.</summary>
    private static void ApplyTextureFix(GameObject root, string modelPath)
    {
        string lowerPath = modelPath.ToLowerInvariant();
        if (lowerPath.Contains("angler") || lowerPath.Contains("nemo"))
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string n = mats[i] != null ? mats[i].name : "";
                    if (n == "LP_Mat") mats[i] = Mat("StoryLumiBody", new Color(0.28f, 0.16f, 0.38f));
                    else if (n == "LP_Mat_Eyes") mats[i] = Mat("StoryLumiEyes", new Color(1f, 0.85f, 0.2f), 1.3f);
                    else if (n == "cuerpo") mats[i] = Mat("StoryPipBody", new Color(1f, 0.5f, 0.1f));
                    else if (n == "aletas") mats[i] = Mat("StoryPipFins", new Color(0.9f, 0.3f, 0.05f));
                    else if (n == "ojo") { Material ojo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ojo.mat"); if (ojo != null) mats[i] = ojo; }
                }
                r.sharedMaterials = mats;
            }
            return;
        }
        if (!modelPath.ToLowerInvariant().Contains("delfino")) return;
        Material textured = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Materials/Delfino.mat");
        if (textured == null) return;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = textured;
            r.sharedMaterials = mats;
        }
    }

    private static void AddModel(Transform root, GameObject prefab, float length)
    {
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        foreach (Collider c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

        // Put the body's long axis along the NPC's Z axis, then scale to the wanted length and centre on the NPC
        Bounds b = BoundsOf(inst);
        if (b.size.x > b.size.z) inst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        b = BoundsOf(inst);
        float s = length / Mathf.Max(b.size.z, 0.0001f);
        inst.transform.localScale = inst.transform.localScale * s;
        b = BoundsOf(inst);
        inst.transform.position += root.position - b.center;

        // make sure the head points along +Z (the direction the character swims in)
        if (HeadSignZ(inst, root.position) < 0f) inst.transform.RotateAround(root.position, Vector3.up, 180f);
    }

    /// <summary>+1 if the model's head is toward +Z, -1 if toward -Z. Uses eye objects first, then the thicker end of the body.</summary>
    private static float HeadSignZ(GameObject inst, Vector3 center)
    {
        Vector3 eyeSum = Vector3.zero;
        int eyes = 0;
        foreach (Transform t in inst.GetComponentsInChildren<Transform>())
        {
            if (t != inst.transform && t.name.ToLowerInvariant().Contains("eye")) { eyeSum += t.position - center; eyes++; }
        }
        if (eyes > 0 && Mathf.Abs((eyeSum / eyes).z) > 0.05f) return Mathf.Sign((eyeSum / eyes).z);

        List<Vector3> pts = new List<Vector3>();
        float minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (MeshFilter mf in inst.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
            Vector3[] vs = mf.sharedMesh.vertices;
            int stride = Mathf.Max(1, vs.Length / 1500);
            for (int i = 0; i < vs.Length; i += stride)
            {
                Vector3 w = mf.transform.TransformPoint(vs[i]) - center;
                pts.Add(w);
                minZ = Mathf.Min(minZ, w.z); maxZ = Mathf.Max(maxZ, w.z);
            }
        }
        if (pts.Count < 20) return 1f;
        float len = maxZ - minZ, posMin = float.MaxValue, posMax = float.MinValue, negMin = float.MaxValue, negMax = float.MinValue;
        foreach (Vector3 v in pts)
        {
            if (v.z > maxZ - len * 0.25f) { posMin = Mathf.Min(posMin, v.x); posMax = Mathf.Max(posMax, v.x); }
            if (v.z < minZ + len * 0.25f) { negMin = Mathf.Min(negMin, v.x); negMax = Mathf.Max(negMax, v.x); }
        }
        return (posMax - posMin) >= (negMax - negMin) ? 1f : -1f;
    }

    private static Bounds BoundsOf(GameObject g)
    {
        Renderer[] rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // ------------------------------------------------------------------ narrative objects

    private static GameObject NewTriggerObject(Transform parent, string name, Vector3 pos)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.position = pos;
        return g;
    }

    private static void Zone(Transform parent, string name, Vector3 pos, Vector3 size, string asset, string requires, string blockedBy)
    {
        GameObject g = NewTriggerObject(parent, name, pos);
        BoxCollider box = g.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        NarrativeTrigger t = g.AddComponent<NarrativeTrigger>();
        t.mode = TriggerMode.Zone;
        t.dialogue = D(asset);
        t.requiresFlag = requires;
        t.blockedByFlag = blockedBy;
    }

    private static void SceneStart(Transform parent, string name, string asset, float delay, string requires, string blockedBy)
    {
        GameObject g = NewTriggerObject(parent, name, Vector3.zero);
        NarrativeTrigger t = g.AddComponent<NarrativeTrigger>();
        t.mode = TriggerMode.OnSceneStart;
        t.dialogue = D(asset);
        t.startDelay = delay;
        t.requiresFlag = requires;
        t.blockedByFlag = blockedBy;
    }

    private static void OnFlag(Transform parent, string name, string asset, string flag)
    {
        GameObject g = NewTriggerObject(parent, name, Vector3.zero);
        NarrativeTrigger t = g.AddComponent<NarrativeTrigger>();
        t.mode = TriggerMode.OnFlag;
        t.dialogue = D(asset);
        t.triggerFlag = flag;
    }

    private static void Bottle(Transform parent, Vector3 pos, string noteAsset, string flag)
    {
        GameObject g = NewTriggerObject(parent, "Message Bottle - " + noteAsset, Free(pos, 2f));
        Material glass = Mat("StoryBottleGlass", new Color(0.55f, 1f, 0.85f), 1.6f);
        Material cork = Mat("StoryCork", new Color(0.55f, 0.38f, 0.2f));
        Material paper = Mat("StoryPaper", new Color(0.95f, 0.88f, 0.65f), 0.4f);

        Transform vis = new GameObject("Visual").transform;
        vis.SetParent(g.transform, false);
        vis.localEulerAngles = new Vector3(0f, 0f, 35f);
        Prim(PrimitiveType.Capsule, "Body", vis, Vector3.zero, Vector3.zero, new Vector3(1.3f, 1.5f, 1.3f), glass);
        Prim(PrimitiveType.Cylinder, "Neck", vis, new Vector3(0f, 1.9f, 0f), Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f), glass);
        Prim(PrimitiveType.Cylinder, "Cork", vis, new Vector3(0f, 2.55f, 0f), Vector3.zero, new Vector3(0.6f, 0.25f, 0.6f), cork);
        Prim(PrimitiveType.Cylinder, "Scroll", vis, Vector3.zero, new Vector3(0f, 0f, 0f), new Vector3(0.5f, 1.0f, 0.5f), paper);
        // a faint beam so the bottle can be spotted from far away
        Prim(PrimitiveType.Cylinder, "Beam", g.transform, new Vector3(0f, 9f, 0f), Vector3.zero, new Vector3(0.25f, 9f, 0.25f), Mat("StoryBeam", new Color(0.55f, 1f, 0.85f), 0.8f));
        AddLight(g.transform, Vector3.up * 1f, new Color(0.6f, 1f, 0.9f), 6f, 16f);

        SphereCollider col = g.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 4.5f;
        StoryPickup p = g.AddComponent<StoryPickup>();
        p.note = D(noteAsset);
        p.flagToSet = flag;
    }

    // ------------------------------------------------------------------ environmental storytelling props

    /// <summary>
    /// The sunken ship (Assets/ship_wrecked.glb) half buried in the sand, with a hull collider.
    /// Returns where the message bottle should float: beside the hull, on the side facing the player's start.
    /// Falls back to the primitive wreck if the model is missing.
    /// </summary>
    private static Vector3 ShipWreck(Transform parent, Vector3 pos)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ship_wrecked.glb");
        if (prefab == null)
        {
            Wreck(parent, pos);
            return new Vector3(24f, 1.5f, 18f);
        }

        GameObject root = new GameObject("Old Wreck (ship)");
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
        foreach (Collider c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

        // about 24 units long, standing on the root's origin
        Bounds b = BoundsOf(inst);
        inst.transform.localScale = inst.transform.localScale * (24f / Mathf.Max(b.size.x, b.size.z));
        b = BoundsOf(inst);
        inst.transform.position += new Vector3(root.transform.position.x - b.center.x, root.transform.position.y - b.min.y, root.transform.position.z - b.center.z);
        b = BoundsOf(inst);

        // one box collider for the hull so the player and fish cannot swim through it
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, b.size.y * 0.42f, 0f);
        box.size = new Vector3(b.size.x * 0.8f, b.size.y * 0.7f, b.size.z * 0.85f);

        // it came to rest leaning over on the slope, keel buried in the sand (sand surface is at y = -4.5)
        root.transform.rotation = Quaternion.Euler(5f, 35f, 12f);
        b = BoundsOf(inst);
        root.transform.position += new Vector3(0f, (-4.5f - 1.5f) - b.min.y, 0f);

        Vector3 right = root.transform.right;
        Vector3 playerStart = new Vector3(-0.8f, 0f, 6.4f);
        float side = b.size.x * 0.5f + 4f;
        Vector3 a1 = root.transform.position + right * side;
        Vector3 a2 = root.transform.position - right * side;
        Vector3 pick = (new Vector3(a1.x, 0f, a1.z) - playerStart).sqrMagnitude < (new Vector3(a2.x, 0f, a2.z) - playerStart).sqrMagnitude ? a1 : a2;
        return new Vector3(pick.x, 1.5f, pick.z);
    }

    private static void Wreck(Transform parent, Vector3 pos)
    {
        Transform r = new GameObject("Old Wreck").transform;
        r.SetParent(parent, false);
        r.position = pos;
        r.eulerAngles = new Vector3(0f, 30f, 0f);

        Material wood = Mat("StoryWood", new Color(0.3f, 0.18f, 0.1f));
        Material dark = Mat("StoryWoodDark", new Color(0.18f, 0.11f, 0.07f));
        Material algae = Mat("StoryAlgae", new Color(0.14f, 0.38f, 0.22f));
        Material rope = Mat("StoryRope", new Color(0.55f, 0.47f, 0.32f));
        Material sail = Mat("StorySail", new Color(0.7f, 0.68f, 0.58f));
        Material metal = Mat("StoryMetalDead", new Color(0.22f, 0.23f, 0.27f));

        Prim(PrimitiveType.Cube, "Hull", r, new Vector3(0f, 1.5f, 0f), new Vector3(0f, 0f, -8f), new Vector3(18f, 3.5f, 6.5f), wood);
        Prim(PrimitiveType.Cube, "Keel", r, Vector3.zero, Vector3.zero, new Vector3(16f, 2f, 5f), dark);
        Prim(PrimitiveType.Cube, "Bow", r, new Vector3(9f, 3f, 0f), new Vector3(0f, 0f, 25f), new Vector3(3f, 5f, 5f), wood);
        Prim(PrimitiveType.Cube, "Cabin", r, new Vector3(-4f, 4.6f, 0f), new Vector3(0f, 5f, 0f), new Vector3(6f, 3.5f, 5f), dark);
        Prim(PrimitiveType.Cylinder, "Broken Mast", r, new Vector3(1f, 6.5f, 0f), new Vector3(0f, 0f, 68f), new Vector3(0.5f, 6.5f, 0.5f), wood);
        Prim(PrimitiveType.Cube, "Torn Sail", r, new Vector3(4.2f, 7.5f, 0.3f), new Vector3(0f, 0f, 62f), new Vector3(4.5f, 5f, 0.1f), sail);
        Prim(PrimitiveType.Sphere, "Dead Lantern", r, new Vector3(-6.3f, 6.7f, 2.7f), Vector3.zero, new Vector3(0.9f, 1.1f, 0.9f), metal);
        Prim(PrimitiveType.Cube, "Plank A", r, new Vector3(-10f, 0.3f, 4f), new Vector3(0f, 40f, 8f), new Vector3(5f, 0.35f, 0.9f), wood);
        Prim(PrimitiveType.Cube, "Plank B", r, new Vector3(11f, 0.2f, -5f), new Vector3(0f, -25f, -6f), new Vector3(4f, 0.35f, 0.9f), wood);
        Prim(PrimitiveType.Cube, "Plank C", r, new Vector3(2f, 0.2f, 7.5f), new Vector3(0f, 80f, 5f), new Vector3(4.5f, 0.35f, 0.9f), dark);
        Prim(PrimitiveType.Cube, "Torn Net A", r, new Vector3(-9f, 0.2f, -6f), new Vector3(0f, 25f, 4f), new Vector3(7f, 0.12f, 4.5f), rope);
        Prim(PrimitiveType.Cube, "Torn Net B", r, new Vector3(-12f, 0.2f, -3f), new Vector3(0f, -15f, 3f), new Vector3(4f, 0.12f, 3f), rope);
        Prim(PrimitiveType.Sphere, "Algae A", r, new Vector3(6f, 0.4f, 5f), Vector3.zero, new Vector3(3f, 1f, 2.5f), algae);
        Prim(PrimitiveType.Sphere, "Algae B", r, new Vector3(-2f, 3.4f, -3.4f), Vector3.zero, new Vector3(2.5f, 0.8f, 1.5f), algae);
    }

    private static void LanternGraveyard(Transform parent, Vector3 pos)
    {
        Transform r = new GameObject("Lantern Graveyard").transform;
        r.SetParent(parent, false);
        r.position = pos;

        Material metal = Mat("StoryMetalDead", new Color(0.22f, 0.23f, 0.27f));
        Material rust = Mat("StoryRust", new Color(0.35f, 0.2f, 0.12f));
        Material rope = Mat("StoryRope", new Color(0.55f, 0.47f, 0.32f));
        Material amber = Mat("StoryAmberDim", new Color(1f, 0.75f, 0.3f), 1.3f);

        // a rusted anchor with its chain
        Prim(PrimitiveType.Cylinder, "Anchor Shaft", r, new Vector3(0f, 2.5f, 0f), new Vector3(0f, 0f, 80f), new Vector3(0.5f, 3f, 0.5f), rust);
        Prim(PrimitiveType.Cylinder, "Anchor Bar", r, new Vector3(-2.7f, 3f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.35f, 1.8f, 0.35f), rust);
        for (int i = 0; i < 9; i++)
            Prim(PrimitiveType.Sphere, "Chain Link " + i, r, new Vector3(3f + i * 1.1f, 0.3f + Mathf.Sin(i) * 0.15f, 1f + i * 0.2f), Vector3.zero, new Vector3(0.7f, 0.5f, 0.7f), metal);

        // dead lanterns on broken poles, scattered in a ring, with one still glowing faintly
        Vector2[] spots = { new Vector2(-9f, 6f), new Vector2(-4f, -9f), new Vector2(6f, 9f), new Vector2(10f, -4f), new Vector2(-12f, -3f), new Vector2(2f, 13f), new Vector2(13f, 6f) };
        for (int i = 0; i < spots.Length; i++)
        {
            Transform pole = new GameObject("Lantern Pole " + i).transform;
            pole.SetParent(r, false);
            pole.localPosition = new Vector3(spots[i].x, 0f, spots[i].y);
            pole.localEulerAngles = new Vector3(((i * 37) % 30) - 15f, i * 50f, ((i * 53) % 30) - 15f);
            float h = 4f + (i % 3);
            Prim(PrimitiveType.Cylinder, "Pole", pole, new Vector3(0f, h * 0.5f, 0f), Vector3.zero, new Vector3(0.28f, h * 0.5f, 0.28f), metal);
            bool lit = i == 3;
            Prim(PrimitiveType.Sphere, "Lantern", pole, new Vector3(0f, h + 0.5f, 0f), Vector3.zero, new Vector3(1.3f, 1.6f, 1.3f), lit ? amber : metal);
            if (lit) AddLight(pole, new Vector3(0f, h + 0.5f, 0f), new Color(1f, 0.75f, 0.3f), 3f, 12f);
            Prim(PrimitiveType.Cylinder, "Rope", pole, new Vector3(0.6f, h * 0.3f, 0f), new Vector3(0f, 0f, 70f), new Vector3(0.08f, 1.4f, 0.08f), rope);
        }
    }

    private static void BoneField(Transform parent, Vector3 pos)
    {
        Transform r = new GameObject("Bone Field").transform;
        r.SetParent(parent, false);
        r.position = pos;
        r.eulerAngles = new Vector3(0f, -20f, 0f);

        Material bone = Mat("StoryBone", new Color(0.85f, 0.82f, 0.7f));
        Material hollow = Mat("StoryHollow", new Color(0.04f, 0.04f, 0.05f));

        Prim(PrimitiveType.Capsule, "Spine", r, new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 90f), new Vector3(1f, 10f, 1f), bone);
        for (int i = 0; i < 7; i++)
        {
            float x = -8f + i * 2.7f;
            float len = 3.2f + Mathf.Sin(i / 6f * Mathf.PI) * 2.2f;
            Prim(PrimitiveType.Capsule, "Rib L" + i, r, new Vector3(x, len * 0.8f, -len * 0.45f), new Vector3(-28f, 0f, 0f), new Vector3(0.6f, len, 0.6f), bone);
            Prim(PrimitiveType.Capsule, "Rib R" + i, r, new Vector3(x, len * 0.8f, len * 0.45f), new Vector3(28f, 0f, 0f), new Vector3(0.6f, len, 0.6f), bone);
        }
        Prim(PrimitiveType.Sphere, "Skull", r, new Vector3(-15f, 2.2f, 0f), Vector3.zero, new Vector3(6f, 4.4f, 5.4f), bone);
        Prim(PrimitiveType.Sphere, "Eye Hollow L", r, new Vector3(-17.6f, 3f, -1.7f), Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f), hollow);
        Prim(PrimitiveType.Sphere, "Eye Hollow R", r, new Vector3(-17.6f, 3f, 1.7f), Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f), hollow);
    }

    private static void RuneTrail(Transform parent, Vector3 from, Vector3 to, int count)
    {
        Transform r = new GameObject("Rune Trail").transform;
        r.SetParent(parent, false);
        Material rune = Mat("StoryRune", new Color(0.3f, 0.9f, 1f), 2.2f);
        for (int i = 0; i < count; i++)
        {
            Vector3 p = Vector3.Lerp(from, to, i / (float)(count - 1));
            p.y += Mathf.Sin(i * 1.7f) * 1.5f;
            p.z += Mathf.Cos(i * 2.3f) * 2.5f;
            GameObject g = Prim(PrimitiveType.Cube, "Rune " + i, r, Vector3.zero, new Vector3(0f, 45f, 0f), new Vector3(1.1f, 0.15f, 1.1f), rune);
            g.transform.position = p;
            if (i % 3 == 0) AddLight(g.transform, Vector3.up * 0.5f, new Color(0.3f, 0.9f, 1f), 2f, 9f);
        }
    }
}
#endif
