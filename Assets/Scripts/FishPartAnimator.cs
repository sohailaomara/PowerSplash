using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tail-fin swing for fish models that have no skeleton but are built from separate pieces (body, tail fin, eyes ...).
/// It finds the piece at the tail end, works out where its "joint" is (the edge nearest the body) and swings the piece
/// around that joint in time with the fish's tail beat. A vertical tail fin swings left and right, a flat whale-style
/// fluke swings up and down. Added automatically by FishSwimAnimator; if no suitable tail piece is found it removes itself.
/// </summary>
public class FishPartAnimator : MonoBehaviour
{
    private FishSwimAnimator swim;
    private Transform tail;
    private Vector3 restPos, jointParent, axisParent;
    private Quaternion restRot;
    private float amplitude;

    private struct Part
    {
        public Transform t;
        public Bounds b;   // bounds in the fish's local space
    }

    /// <summary>Returns false when there is no tail piece to animate.</summary>
    public bool Init(FishSwimAnimator owner, Vector3 headLocal)
    {
        swim = owner;
        headLocal = headLocal.normalized;
        Vector3 lateral = Vector3.Cross(Vector3.up, headLocal).normalized;
        Vector3 headAbs = new Vector3(Mathf.Abs(headLocal.x), Mathf.Abs(headLocal.y), Mathf.Abs(headLocal.z));
        Vector3 latAbs = new Vector3(Mathf.Abs(lateral.x), Mathf.Abs(lateral.y), Mathf.Abs(lateral.z));

        List<Part> parts = new List<Part>();
        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null || mf.transform == transform) continue;   // the main body may sit on the root itself
            parts.Add(new Part { t = mf.transform, b = LocalBounds(mf) });
        }
        MeshFilter rootMesh = GetComponent<MeshFilter>();
        if (parts.Count == 0) return false;

        // overall body range along the head direction
        float minP = float.MaxValue, maxP = float.MinValue;
        float mainVolume = 0f;
        foreach (Part p in parts)
        {
            float c = Vector3.Dot(p.b.center, headLocal), e = Vector3.Dot(p.b.size, headAbs) * 0.5f;
            minP = Mathf.Min(minP, c - e);
            maxP = Mathf.Max(maxP, c + e);
            mainVolume = Mathf.Max(mainVolume, p.b.size.x * p.b.size.y * p.b.size.z);
        }
        if (rootMesh != null && rootMesh.sharedMesh != null)
        {
            Bounds rb = LocalBounds(rootMesh);
            minP = Mathf.Min(minP, Vector3.Dot(rb.center, headLocal) - Vector3.Dot(rb.size, headAbs) * 0.5f);
            maxP = Mathf.Max(maxP, Vector3.Dot(rb.center, headLocal) + Vector3.Dot(rb.size, headAbs) * 0.5f);
        }
        float len = maxP - minP;
        if (len < 0.0001f) return false;

        // the piece nearest the tail end that is a small part of the body (not the body, not an eye)
        int best = -1;
        float bestProj = float.MaxValue;
        for (int i = 0; i < parts.Count; i++)
        {
            Part p = parts[i];
            if (p.t.name.ToLowerInvariant().Contains("eye")) continue;
            float extent = Vector3.Dot(p.b.size, headAbs);
            if (extent > 0.7f * len) continue;
            float vol = p.b.size.x * p.b.size.y * p.b.size.z;
            if (vol >= mainVolume * 0.98f && parts.Count > 1) continue;
            float c = Vector3.Dot(p.b.center, headLocal);
            if (c > minP + 0.64f * len) continue;                         // must lie toward the rear (tail fins overlap the body a little)
            if (c < bestProj) { bestProj = c; best = i; }
        }
        if (best < 0) return false;

        Part tailPart = parts[best];
        tail = tailPart.t;
        float ext = Vector3.Dot(tailPart.b.size, headAbs);
        float pc = Vector3.Dot(tailPart.b.center, headLocal);
        // joint: the front part of the tail piece, where it joins the body
        Vector3 jointLocal = tailPart.b.center + headLocal * (ext * 0.25f);

        // a fin that is thin from side to side is a vertical tail (swings left/right); a flat one is a fluke (swings up/down)
        float thinSide = Vector3.Dot(tailPart.b.size, latAbs);
        Vector3 axisLocal = thinSide <= tailPart.b.size.y ? Vector3.up : lateral;
        amplitude = thinSide <= tailPart.b.size.y ? 28f : 16f;

        Transform par = tail.parent != null ? tail.parent : transform;
        jointParent = par.InverseTransformPoint(transform.TransformPoint(jointLocal));
        axisParent = par.InverseTransformDirection(transform.TransformDirection(axisLocal)).normalized;
        restPos = tail.localPosition;
        restRot = tail.localRotation;
        return true;
    }

    private Bounds LocalBounds(MeshFilter mf)
    {
        Bounds mb = mf.sharedMesh.bounds;
        bool first = true;
        Bounds lb = new Bounds();
        for (int i = 0; i < 8; i++)
        {
            Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            Vector3 local = transform.InverseTransformPoint(mf.transform.TransformPoint(c));
            if (first) { lb = new Bounds(local, Vector3.zero); first = false; }
            else lb.Encapsulate(local);
        }
        return lb;
    }

    private void LateUpdate()
    {
        if (tail == null || swim == null) return;
        float a = amplitude * (0.35f + 0.65f * swim.Speed01) * swim.ActiveAmount;
        // the tail lags a little behind the body wag, so the motion travels along the fish
        float angle = Mathf.Sin((swim.Beat + swim.Phase - 0.2f) * 2f * Mathf.PI) * a;
        Quaternion q = Quaternion.AngleAxis(angle, axisParent);
        tail.localRotation = q * restRot;
        tail.localPosition = jointParent + q * (restPos - jointParent);
    }
}
