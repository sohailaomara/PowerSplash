using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lighting behaviour 2: a player-controlled spotlight on the fish (toggle with L).
/// URP has no volumetric lighting, so a spot light only brightens the surfaces it hits. To make the beam visible
/// in open water as well, soft glowing cones are drawn along the light. They live on their own object (not under the
/// fish) so the fish's renderer scans, first-person hiding and stun flash never touch them.
/// </summary>
public class PlayerFlashlight : MonoBehaviour
{
    public static bool IsOn { get; private set; }

    public Light spot;
    [SerializeField] private bool startOn = false;

    [Header("Visible beam")]
    [SerializeField] private bool showBeam = true;
    [SerializeField] private Material beamMaterial;
    [SerializeField] private Color beamColor = new Color(1f, 0.95f, 0.8f, 1f);
    [SerializeField, Range(0.2f, 1f)] private float beamLengthOfRange = 0.65f;
    [SerializeField, Range(0f, 2f)] private float beamStrength = 0.8f;
    [SerializeField] private float fadeSeconds = 0.15f;

    private GameObject beamRoot;
    private Material beamInstance;
    private Mesh beamMesh;
    private float beamAmount;   // 0..1, fades in and out with the toggle

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { IsOn = false; }

    private void Start()
    {
        IsOn = startOn;
        if (spot != null) spot.enabled = IsOn;
        if (showBeam && spot != null) BuildBeam();
        beamAmount = IsOn ? 1f : 0f;
        ApplyBeam();
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.lKey.wasPressedThisFrame)
        {
            IsOn = !IsOn;
            if (spot != null) spot.enabled = IsOn;
        }
        beamAmount = Mathf.MoveTowards(beamAmount, IsOn ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
        ApplyBeam();
    }

    private void LateUpdate()
    {
        if (beamRoot != null && beamRoot.activeSelf && spot != null)
            beamRoot.transform.SetPositionAndRotation(spot.transform.position, spot.transform.rotation);
    }

    private void OnDestroy()
    {
        if (beamRoot != null) Destroy(beamRoot);
        if (beamInstance != null) Destroy(beamInstance);
        if (beamMesh != null) Destroy(beamMesh);
    }

    private void ApplyBeam()
    {
        if (beamRoot == null) return;
        bool visible = beamAmount > 0.001f;
        if (beamRoot.activeSelf != visible) beamRoot.SetActive(visible);
        if (!visible) return;

        // the darker the water, the more the beam shows (it stays faint in bright daylight)
        float darkness = Mathf.Clamp01(Mathf.InverseLerp(0.34f, 0.03f, RenderSettings.fogColor.grayscale));
        float strength = Mathf.Lerp(0.2f, 1f, darkness) * beamStrength * beamAmount;
        beamInstance.SetFloat("_Intensity", strength);
        beamInstance.SetColor("_Color", beamColor);
    }

    private void BuildBeam()
    {
        Shader shader = beamMaterial != null ? beamMaterial.shader : Shader.Find("Story/FlashlightBeam");
        if (shader == null) return;
        beamInstance = beamMaterial != null ? new Material(beamMaterial) : new Material(shader);
        beamInstance.renderQueue = 3100;

        float length = spot.range * beamLengthOfRange;
        float outer = spot.spotAngle * 0.5f;
        float inner = spot.innerSpotAngle * 0.5f;

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        AddCone(verts, normals, colors, uvs, tris, length, outer * 0.85f, 0.45f);            // wide, faint halo
        AddCone(verts, normals, colors, uvs, tris, length, Mathf.Max(3f, inner * 0.8f), 1f);   // bright core

        beamMesh = new Mesh();
        beamMesh.name = "Flashlight Beam";
        beamMesh.SetVertices(verts);
        beamMesh.SetNormals(normals);
        beamMesh.SetColors(colors);
        beamMesh.SetUVs(0, uvs);
        beamMesh.SetTriangles(tris, 0);
        beamMesh.RecalculateBounds();

        beamRoot = new GameObject("Flashlight Beam");
        beamRoot.AddComponent<MeshFilter>().sharedMesh = beamMesh;
        MeshRenderer mr = beamRoot.AddComponent<MeshRenderer>();
        mr.sharedMaterial = beamInstance;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        beamRoot.SetActive(false);
    }

    private static void AddCone(List<Vector3> verts, List<Vector3> normals, List<Color> colors, List<Vector2> uvs, List<int> tris, float length, float halfAngleDeg, float weight)
    {
        const int segments = 32;
        float a = halfAngleDeg * Mathf.Deg2Rad;
        float radius = length * Mathf.Tan(a);
        float cosA = Mathf.Cos(a), sinA = Mathf.Sin(a);

        int apexStart = verts.Count;
        for (int i = 0; i <= segments; i++)
        {
            float phi = i / (float)segments * Mathf.PI * 2f;
            verts.Add(Vector3.zero);
            normals.Add(new Vector3(Mathf.Cos(phi) * cosA, Mathf.Sin(phi) * cosA, -sinA));
            colors.Add(new Color(1f, 1f, 1f, weight));
            uvs.Add(new Vector2(0f, 0f));
        }
        int ringStart = verts.Count;
        for (int i = 0; i <= segments; i++)
        {
            float phi = i / (float)segments * Mathf.PI * 2f;
            float cp = Mathf.Cos(phi), sn = Mathf.Sin(phi);
            verts.Add(new Vector3(cp * radius, sn * radius, length));
            normals.Add(new Vector3(cp * cosA, sn * cosA, -sinA));
            colors.Add(new Color(1f, 1f, 1f, weight));
            uvs.Add(new Vector2(1f, 0f));
        }
        for (int i = 0; i < segments; i++)
        {
            tris.Add(apexStart + i); tris.Add(ringStart + i); tris.Add(ringStart + i + 1);
        }
    }
}
