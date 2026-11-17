using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes NPC fish swim like fish instead of gliding like floating objects. PreyFish and PredatorFish decide
/// WHERE to go; this component decides HOW the body gets there:
///  - the heading turns at a limited rate (a fish needs room to turn) and never points steeply up or down,
///  - the fish surges forward with every tail beat and glides in between, and slows down in sharp turns,
///  - the body wags from side to side with each tail beat (more when swimming fast, none when stunned),
///  - the body banks into turns and noses up or down with its vertical movement,
///  - models that sit sideways on their root object are turned to face the way they swim.
/// It is added automatically by PreyFish and PredatorFish.
/// </summary>
[DisallowMultipleComponent]
public class FishSwimAnimator : MonoBehaviour
{
    public enum HeadAxis { Auto, PlusZ, MinusZ, PlusX, MinusX }

    [Header("Model orientation")]
    [Tooltip("Which local axis the model's head points along. Auto only corrects models that clearly lie sideways.")]
    public HeadAxis headAxis = HeadAxis.Auto;
    [Tooltip("Octopus, jellyfish, crab and seahorse drift instead of wagging (set automatically from the name).")]
    public bool nonFishBody;

    [Header("Swimming")]
    [SerializeField] private float maxPitchDegrees = 28f;
    [SerializeField] private float beatHz = 1.7f;
    [SerializeField] private float wiggleDegrees = 13f;
    [SerializeField] private float bankDegrees = 22f;
    [SerializeField] [Range(0f, 0.6f)] private float burstDepth = 0.3f;

    /// <summary>The direction the fish's head is pointing, in world space.</summary>
    public Vector3 Heading { get { return heading; } }

    private Rigidbody rb;
    private Quaternion headOffset = Quaternion.identity;
    private Quaternion baseRotation;
    private Vector3 heading = Vector3.forward;
    private float bank, beat, phase, speed01, clearance = 2.5f;
    private float lastSteerTime = -10f;

    public static FishSwimAnimator Ensure(GameObject go)
    {
        FishSwimAnimator a = go.GetComponent<FishSwimAnimator>();
        return a != null ? a : go.AddComponent<FishSwimAnimator>();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        phase = Random.value;
        beat = Random.value;
        SetupOrientation();
        baseRotation = transform.rotation;
    }

    private void Update()
    {
        beat += Time.deltaTime * beatHz * (0.7f + 0.8f * speed01);
    }

    // ------------------------------------------------------------------ steering (called every FixedUpdate by the AI)

    /// <summary>Swim toward desiredDir at roughly cruiseSpeed, turning no faster than maxTurnDegPerSec.</summary>
    public void Steer(Vector3 desiredDir, float cruiseSpeed, float maxTurnDegPerSec, float dt, float pitchLimit = -1f)
    {
        lastSteerTime = Time.time;
        float pitch = pitchLimit > 0f ? pitchLimit : maxPitchDegrees;
        if (desiredDir.sqrMagnitude < 0.0001f) desiredDir = heading;
        desiredDir = ClampPitch(desiredDir, pitch);

        Vector3 before = heading;
        heading = Vector3.RotateTowards(heading, desiredDir, maxTurnDegPerSec * Mathf.Deg2Rad * dt, 0f);
        heading = ClampPitch(heading, pitch + 4f);

        // surge with each tail beat, and slow down while the body is still swinging around
        float angleToTarget = Vector3.Angle(heading, desiredDir);
        float turnFactor = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(angleToTarget / 70f));
        float surge = 1f - burstDepth * (0.5f + 0.5f * Mathf.Sin(beat * 2f * Mathf.PI));
        float targetSpeed = cruiseSpeed * turnFactor * surge * (nonFishBody ? 0.6f : 1f);
        rb.AddForce(heading * targetSpeed - rb.linearVelocity, ForceMode.Acceleration);
        speed01 = Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(cruiseSpeed, 0.1f));

        // bank into the turn
        float yawRate = Vector3.SignedAngle(Flat(before), Flat(heading), Vector3.up) / Mathf.Max(dt, 0.0001f);
        float bankTarget = Mathf.Clamp(-yawRate * 0.25f, -bankDegrees, bankDegrees);
        bank = Mathf.Lerp(bank, bankTarget, 1f - Mathf.Exp(-4f * dt));

        baseRotation = Quaternion.LookRotation(heading, Vector3.up) * Quaternion.Euler(0f, 0f, bank) * headOffset;
        transform.rotation = baseRotation;
    }

    private void LateUpdate()
    {
        // The wiggle is only applied on top of the steered rotation. When nothing steers the fish
        // (it is stunned, or being eaten) the wiggle fades out and the fish is left alone.
        float active = Mathf.Clamp01(1f - (Time.time - lastSteerTime) * 5f);
        if (active <= 0f) return;

        float amp = wiggleDegrees * (0.5f + 0.5f * speed01) * active * (nonFishBody ? 0.3f : 1f);
        float yaw = Mathf.Sin((beat + phase) * 2f * Mathf.PI) * amp;
        transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * baseRotation;
    }

    // ------------------------------------------------------------------ helpers for the AI scripts

    /// <summary>A mostly horizontal direction, so fish cruise along instead of diving like missiles.</summary>
    public static Vector3 RandomSwimDirection(float maxPitchDeg = 12f)
    {
        return Quaternion.Euler(Random.Range(-maxPitchDeg, maxPitchDeg), Random.Range(0f, 360f), 0f) * Vector3.forward;
    }

    /// <summary>Bends dir away from the sea floor, the surface and walls or rocks straight ahead.</summary>
    public Vector3 AvoidObstacles(Vector3 dir)
    {
        Vector3 pos = transform.position;
        RaycastHit hit;
        if (Physics.Raycast(pos, Vector3.down, out hit, clearance, ~0, QueryTriggerInteraction.Ignore) && hit.rigidbody == null)
            dir.y = Mathf.Max(dir.y, 0.4f);
        if (Physics.Raycast(pos, Vector3.up, out hit, clearance, ~0, QueryTriggerInteraction.Ignore) && hit.rigidbody == null)
            dir.y = Mathf.Min(dir.y, -0.3f);

        Vector3 flat = Flat(dir);
        if (flat.sqrMagnitude > 0.01f && Physics.Raycast(pos, flat.normalized, out hit, clearance * 2.5f, ~0, QueryTriggerInteraction.Ignore) && hit.rigidbody == null)
        {
            Vector3 slide = Vector3.ProjectOnPlane(flat, hit.normal);
            if (slide.sqrMagnitude < 0.01f) slide = Vector3.Cross(Vector3.up, hit.normal);
            Vector3 away = Flat(slide.normalized + hit.normal * 0.6f).normalized;
            dir = new Vector3(away.x, dir.y, away.z);
        }
        return dir.normalized;
    }

    // ------------------------------------------------------------------ orientation of the model

    private struct Part
    {
        public Mesh mesh;
        public Transform t;
    }

    private void SetupOrientation()
    {
        string lower = name.ToLowerInvariant();
        if (lower.Contains("octopus") || lower.Contains("jelly") || lower.Contains("crab") || lower.Contains("seahorse")) nonFishBody = true;

        List<Part> parts = new List<Part>();
        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null) parts.Add(new Part { mesh = mf.sharedMesh, t = mf.transform });
        foreach (SkinnedMeshRenderer s in GetComponentsInChildren<SkinnedMeshRenderer>())
            if (s.sharedMesh != null) parts.Add(new Part { mesh = s.sharedMesh, t = s.transform });

        Collider col = GetComponentInChildren<Collider>();
        if (col != null) clearance = Mathf.Max(2.5f, col.bounds.extents.magnitude * 1.1f);

        Vector3 head = Vector3.forward;
        if (headAxis != HeadAxis.Auto)
        {
            head = AxisVector(headAxis);
        }
        else if (!nonFishBody && parts.Count > 0)
        {
            Bounds lb = LocalBounds(parts);
            // Only models that clearly lie sideways (long axis along local X) are turned; the rest were set up facing +Z.
            if (lb.size.x > lb.size.z * 1.15f) head = Vector3.right * DetectHeadSignAlongX(parts, lb);
        }

        if (head != Vector3.forward) headOffset = Quaternion.FromToRotation(head, Vector3.forward);

        // start out heading where the model's head currently points
        heading = Vector3.Normalize(transform.rotation * (Quaternion.Inverse(headOffset) * Vector3.forward));
    }

    private Bounds LocalBounds(List<Part> parts)
    {
        bool first = true;
        Bounds lb = new Bounds();
        foreach (Part p in parts)
        {
            Bounds mb = p.mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 local = transform.InverseTransformPoint(p.t.TransformPoint(c));
                if (first) { lb = new Bounds(local, Vector3.zero); first = false; }
                else lb.Encapsulate(local);
            }
        }
        return lb;
    }

    /// <summary>+1 if the head is toward +X, -1 if toward -X. Uses eye objects first, then the thicker end of the body.</summary>
    private float DetectHeadSignAlongX(List<Part> parts, Bounds lb)
    {
        Vector3 eyeSum = Vector3.zero;
        int eyes = 0;
        foreach (Transform t in GetComponentsInChildren<Transform>())
        {
            if (t != transform && t.name.ToLowerInvariant().Contains("eye"))
            {
                eyeSum += transform.InverseTransformPoint(t.position);
                eyes++;
            }
        }
        if (eyes > 0)
        {
            float d = (eyeSum / eyes - lb.center).x;
            if (Mathf.Abs(d) > lb.size.x * 0.05f) return Mathf.Sign(d);
        }

        // a fish is thicker at the head than at the tail fin: compare the sideways spread of both ends
        float minS = float.MaxValue, maxS = float.MinValue;
        List<Vector3> pts = new List<Vector3>();
        foreach (Part p in parts)
        {
            if (!p.mesh.isReadable) continue;
            Vector3[] vs = p.mesh.vertices;
            int stride = Mathf.Max(1, vs.Length / 1500);
            for (int i = 0; i < vs.Length; i += stride)
            {
                Vector3 local = transform.InverseTransformPoint(p.t.TransformPoint(vs[i]));
                pts.Add(local);
                minS = Mathf.Min(minS, local.x);
                maxS = Mathf.Max(maxS, local.x);
            }
        }
        if (pts.Count < 20) return 1f;

        float len = maxS - minS;
        float posMin = float.MaxValue, posMax = float.MinValue, negMin = float.MaxValue, negMax = float.MinValue;
        foreach (Vector3 v in pts)
        {
            if (v.x > maxS - len * 0.25f) { posMin = Mathf.Min(posMin, v.z); posMax = Mathf.Max(posMax, v.z); }
            if (v.x < minS + len * 0.25f) { negMin = Mathf.Min(negMin, v.z); negMax = Mathf.Max(negMax, v.z); }
        }
        return (posMax - posMin) >= (negMax - negMin) ? 1f : -1f;
    }

    private static Vector3 AxisVector(HeadAxis a)
    {
        switch (a)
        {
            case HeadAxis.MinusZ: return Vector3.back;
            case HeadAxis.PlusX: return Vector3.right;
            case HeadAxis.MinusX: return Vector3.left;
            default: return Vector3.forward;
        }
    }

    private static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    private static Vector3 ClampPitch(Vector3 d, float maxDeg)
    {
        d.Normalize();
        float maxY = Mathf.Sin(maxDeg * Mathf.Deg2Rad);
        if (Mathf.Abs(d.y) <= maxY) return d;
        Vector3 flat = Flat(d);
        flat = flat.sqrMagnitude < 1e-6f ? Vector3.forward : flat.normalized;
        float y = Mathf.Sign(d.y) * maxY;
        return (flat * Mathf.Sqrt(1f - y * y) + Vector3.up * y).normalized;
    }
}
