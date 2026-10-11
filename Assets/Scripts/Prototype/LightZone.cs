using UnityEngine;

/// <summary>
/// Lighting behaviour 3 (environment-triggered): entering this trigger volume
/// ("The Deep") smoothly darkens the sun, ambient light and fog. The DayNightCycle
/// reads LightZone.Darkness every frame.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class LightZone : MonoBehaviour
{
    public static float Darkness { get; private set; }
    public static string CurrentZoneName { get; private set; }

    [SerializeField] private string zoneName = "The Deep";
    [SerializeField, Range(0f, 1f)] private float targetDarkness = 1f;
    [SerializeField] private float fadeSeconds = 2.5f;

    private const string DefaultZone = "Sunlit Reef";
    private BoxCollider box;
    private Transform player;
    private bool inside;
    private float current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Darkness = 0f; CurrentZoneName = DefaultZone; }

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        CurrentZoneName = DefaultZone;
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else return;
        }

        bool nowInside = box.bounds.Contains(player.position);
        if (nowInside != inside)
        {
            inside = nowInside;
            CurrentZoneName = inside ? zoneName : DefaultZone;
            GameHUD.Message(inside ? "Entering " + zoneName + " - it gets dark. Press L for your flashlight" : "Back in the " + DefaultZone, 3.5f);
        }

        current = Mathf.MoveTowards(current, inside ? targetDarkness : 0f, Time.deltaTime / Mathf.Max(0.05f, fadeSeconds));
        Darkness = current;
    }

    private void OnDrawGizmos()
    {
        BoxCollider b = GetComponent<BoxCollider>();
        if (b == null) return;
        Gizmos.color = new Color(0.1f, 0.1f, 0.4f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(b.center, b.size);
    }
}
