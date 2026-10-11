using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives three Cinemachine virtual cameras purely through priorities, so the
/// CinemachineBrain does all the blending:
///   1. Third-person follow  (default)
///   2. First-person POV     (toggle with V)
///   3. Cinematic camera     (orbits a focus target for a few seconds when a world event fires)
/// </summary>
public class CameraDirector : MonoBehaviour
{
    public static CameraDirector Instance { get; private set; }

    public CinemachineCamera thirdPerson;
    public CinemachineCamera firstPerson;
    public CinemachineCamera cinematic;
    public Transform player;

    private bool firstPersonMode;
    private float cinematicTimer;
    private Transform focus;
    private Vector3 offset;
    private float orbitSpeed;
    private float orbitAngle;
    private Renderer[] bodyRenderers;

    public string CurrentLabel
    {
        get
        {
            if (cinematicTimer > 0f) return "Cinematic (Cinemachine blend)";
            return firstPersonMode ? "First-person (POV)" : "Third-person (follow)";
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null) bodyRenderers = player.GetComponentsInChildren<Renderer>();
        Apply();
    }

    /// <summary>Blend to the cinematic camera, orbiting 'target' for 'seconds'.</summary>
    public void PlayCinematic(Transform target, float seconds, Vector3 worldOffset, float orbitDegreesPerSecond = 8f)
    {
        if (cinematic == null || target == null) return;
        focus = target;
        offset = worldOffset;
        orbitSpeed = orbitDegreesPerSecond;
        orbitAngle = 0f;
        cinematicTimer = seconds;
        cinematic.transform.position = focus.position + offset;
        cinematic.Target.TrackingTarget = null;
        cinematic.Target.LookAtTarget = focus;
        Apply();
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.vKey.wasPressedThisFrame && cinematicTimer <= 0f)
            firstPersonMode = !firstPersonMode;

        if (cinematicTimer > 0f)
        {
            cinematicTimer -= Time.deltaTime;
            orbitAngle += orbitSpeed * Time.deltaTime;
            if (focus != null)
                cinematic.transform.position = focus.position + Quaternion.AngleAxis(orbitAngle, Vector3.up) * offset;
        }

        Apply();
    }

    private void Apply()
    {
        if (thirdPerson != null) thirdPerson.Priority.Value = firstPersonMode ? 10 : 20;
        if (firstPerson != null) firstPerson.Priority.Value = firstPersonMode ? 20 : 10;
        if (cinematic != null) cinematic.Priority.Value = cinematicTimer > 0f ? 30 : 0;

        // hide the fish body while looking out of its eyes
        bool showBody = !firstPersonMode || cinematicTimer > 0f;
        if (bodyRenderers != null)
            foreach (Renderer r in bodyRenderers) if (r != null) r.enabled = showBody;
    }
}
