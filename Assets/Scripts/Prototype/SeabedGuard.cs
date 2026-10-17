using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps every fish (the player, prey and predators) above the sand. Fish are pushed by thrust forces in
/// random directions and some were placed overlapping the seabed, so without this they could slip under it
/// while the player, which is controlled differently, could not.
/// Created automatically when the game starts, so no scene needs to be edited.
/// </summary>
public class SeabedGuard : MonoBehaviour
{
    private const float Margin = 0.05f;
    private const float DefaultFloorY = -4.5f;   // top of the sand tiles in every level

    private static SeabedGuard instance;

    private float floorY = DefaultFloorY;
    private float refreshTimer;
    private readonly List<Body> bodies = new List<Body>();

    private class Body
    {
        public Rigidbody rb;
        public Renderer[] renderers;
        public Collider[] colliders;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;
        GameObject go = new GameObject("Seabed Guard");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SeabedGuard>();
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Refresh(); }

    private void Start() { Refresh(); }

    private void Refresh()
    {
        refreshTimer = 1f;

        // The floor is the top of the large, thin "Sand" box colliders.
        float top = float.NegativeInfinity;
        foreach (BoxCollider c in FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            if (c.isTrigger || !c.name.StartsWith("Sand")) continue;
            Bounds b = c.bounds;
            if (b.size.y > 3f || b.size.x < 20f) continue;
            if (b.max.y > top) top = b.max.y;
        }
        floorY = float.IsNegativeInfinity(top) ? DefaultFloorY : top;

        bodies.Clear();
        foreach (EntityData e in FindObjectsByType<EntityData>(FindObjectsSortMode.None))
        {
            Rigidbody rb = e.GetComponent<Rigidbody>();
            if (rb == null) continue;
            if (!e.CompareTag("Player")) rb.useGravity = false;   // fish swim, they never sink
            Body body = new Body();
            body.rb = rb;
            body.renderers = e.GetComponentsInChildren<Renderer>(true);
            body.colliders = e.GetComponentsInChildren<Collider>(true);
            bodies.Add(body);
        }
    }

    private void FixedUpdate() { Enforce(); }

    private void LateUpdate()
    {
        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f) Refresh();
        Enforce();
    }

    private void Enforce()
    {
        float minY = floorY + Margin;
        for (int i = bodies.Count - 1; i >= 0; i--)
        {
            Body body = bodies[i];
            if (body.rb == null) { bodies.RemoveAt(i); continue; }
            if (!body.rb.gameObject.activeInHierarchy) continue;

            float bottom = float.PositiveInfinity;
            foreach (Renderer r in body.renderers)
                if (r != null && r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer) && !(r is TrailRenderer) && !(r is LineRenderer))
                    bottom = Mathf.Min(bottom, r.bounds.min.y);
            foreach (Collider c in body.colliders)
                if (c != null && c.enabled && !c.isTrigger && c.gameObject.activeInHierarchy)
                    bottom = Mathf.Min(bottom, c.bounds.min.y);
            if (float.IsPositiveInfinity(bottom) || bottom >= minY) continue;

            float lift = minY - bottom;
            Vector3 p = body.rb.position;
            p.y += lift;
            body.rb.position = p;
            body.rb.transform.position = p;

            Vector3 v = body.rb.linearVelocity;
            if (v.y < 0f) { v.y = 0f; body.rb.linearVelocity = v; }
        }
    }
}
