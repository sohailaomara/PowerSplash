using UnityEngine;

/// <summary>
/// A real game fish model cruising across the main menu background.
/// It uses the same FishSwimAnimator as the in-game fish, so it swims, banks and wiggles its tail the same way.
/// When it leaves the screen it wraps around to the other side.
/// </summary>
public class MenuFish : MonoBehaviour
{
    public float dir = 1f;        // +1 swims to the right, -1 to the left
    public float speed = 4f;
    public float bound = 60f;     // x distance from the centre at which the fish wraps around

    private Rigidbody rb;
    private FishSwimAnimator swim;
    private float phase;
    private float born;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        swim = FishSwimAnimator.Ensure(gameObject);
        phase = Random.value * 6.28f;
        born = Time.time;
    }

    private void FixedUpdate()
    {
        if (rb == null || swim == null) return;

        // gentle rise and fall; for the first moments turn fast so the fish faces its lane straight away
        float bob = Mathf.Sin(Time.time * 0.7f + phase) * 0.12f;
        float turn = Time.time - born < 0.4f ? 2000f : 90f;
        swim.Steer(new Vector3(dir, bob, 0f), speed, turn, Time.fixedDeltaTime);

        Vector3 p = rb.position;
        if ((dir > 0f && p.x > bound) || (dir < 0f && p.x < -bound))
        {
            p.x = -Mathf.Sign(dir) * bound;
            rb.position = p;
        }
    }
}
