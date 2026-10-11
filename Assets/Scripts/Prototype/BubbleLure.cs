using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player action: drop a glowing bubble lure (G). Nearby PreyFish swim toward it,
/// changing their behaviour for as long as it exists.
/// </summary>
public class BubbleLure : MonoBehaviour
{
    public static Transform Active { get; private set; }
    public const float PullRadius = 45f;

    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float cooldown = 12f;
    [SerializeField] private float spawnDistance = 7f;

    private float cooldownTimer;
    private float lifeTimer;
    private GameObject lure;
    private Vector3 basePos;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Active = null; }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        Keyboard k = Keyboard.current;
        if (k != null && k.gKey.wasPressedThisFrame) TrySpawn();

        if (lure != null)
        {
            lifeTimer -= Time.deltaTime;
            lure.transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.4f;
            if (lifeTimer <= 0f) Despawn();
        }
    }

    private void TrySpawn()
    {
        if (lure != null) { GameHUD.Message("A lure is already out there", 1.5f); return; }
        if (cooldownTimer > 0f) { GameHUD.Message("Bubble lure recharging (" + cooldownTimer.ToString("0") + "s)", 2f); return; }

        basePos = transform.position + transform.forward * spawnDistance;
        lure = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lure.name = "Bubble Lure (runtime)";
        Collider c = lure.GetComponent<Collider>();
        if (c != null) Destroy(c);
        lure.transform.position = basePos;
        lure.transform.localScale = Vector3.one * 1.8f;

        Renderer r = lure.GetComponent<Renderer>();
        Material m = new Material(Shader.Find("Sprites/Default"));
        m.color = new Color(0.4f, 1f, 0.9f, 0.55f);
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Light l = lure.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.4f, 1f, 0.85f);
        l.range = 14f;
        l.intensity = 6f;
        l.shadows = LightShadows.None;

        Fx.Emitter(lure.transform, Vector3.zero, new Color(0.8f, 1f, 1f, 0.8f), 25f, 0.4f, 1.5f, 0.6f, 0.8f);

        Active = lure.transform;
        lifeTimer = lifetime;
        cooldownTimer = cooldown;
        GameHUD.Message("Bubble lure dropped - nearby fish swim toward it", 3f);
    }

    private void Despawn()
    {
        if (lure != null) Destroy(lure);
        lure = null;
        Active = null;
    }

    private void OnDestroy() { if (lure != null) Destroy(lure); Active = null; }

    /// <summary>Used by PreyFish: direction toward the lure if it is within range.</summary>
    public static bool TryGetPull(Vector3 from, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (Active == null) return false;
        Vector3 d = Active.position - from;
        if (d.magnitude > PullRadius || d.sqrMagnitude < 0.01f) return false;
        direction = d.normalized;
        return true;
    }
}
