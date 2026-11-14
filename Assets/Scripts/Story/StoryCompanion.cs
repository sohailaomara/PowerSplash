using UnityEngine;

/// <summary>
/// Makes a character swim behind the player once a story flag is set (Pip after the player chose to help him).
/// Before the flag is set, the character just floats in place. It follows into Level 3 because the flag persists.
/// </summary>
public class StoryCompanion : MonoBehaviour
{
    public string followWhenFlag = "savedPip";
    public float followDistance = 9f;
    public float heightOffset = 1.5f;
    public float maxSpeed = 24f;

    private Transform player;
    private Vector3 idleBase;
    private Vector3 velocity;
    private bool baseSet;

    private void Update()
    {
        if (!baseSet) { idleBase = transform.position; baseSet = true; }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            player = p.transform;
        }

        if (!StoryStateManager.Has(followWhenFlag))
        {
            transform.position = idleBase + Vector3.up * Mathf.Sin(Time.time * 1.3f) * 0.4f;
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > followDistance)
        {
            Vector3 target = player.position - toPlayer.normalized * followDistance + Vector3.up * heightOffset;
            transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, 0.6f, maxSpeed);
        }
        else
        {
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * 3f);
            transform.position += Vector3.up * Mathf.Sin(Time.time * 1.6f) * 0.01f;
        }

        // Face the way the companion is swimming
        if (velocity.sqrMagnitude > 0.5f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity), Time.deltaTime * 4f);
    }
}
