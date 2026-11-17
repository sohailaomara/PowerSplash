using UnityEngine;

/// <summary>
/// Small fish AI: wanders around its spawn point, swims toward a bubble lure, and panics when the player is right on top of it.
/// The AI only chooses a direction and speed; FishSwimAnimator turns that into believable swimming.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(EntityData))]
public class PreyFish : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float swimSpeed = 3f;
    [SerializeField] private float turnSpeed = 2f;
    [SerializeField] private float wanderRadius = 25f;
    [SerializeField] private float directionChangeInterval = 5f;

    [Header("Panic Reaction")]
    [SerializeField] private float eatenRangeThreshold = 3.5f; // Flee only when very close
    [SerializeField] private float panicSpeedMultiplier = 2f;

    private const float VerticalBand = 7f;   // how far above/below its spawn height a fish wanders

    private Rigidbody rb;
    private FishSwimAnimator swim;
    private Transform playerTransform;
    private Vector3 spawnPoint;
    private Vector3 targetDirection;
    private float timer;
    private float nextChange;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2.0f;

        swim = FishSwimAnimator.Ensure(gameObject);
        spawnPoint = transform.position;
        targetDirection = swim.Heading;
        ChooseRandomDirection();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        bool isPanicking = false;

        // Check distance to player
        if (playerTransform != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            // Only react if player is right on top of them (eaten-range)
            if (distToPlayer <= eatenRangeThreshold)
            {
                isPanicking = true;
                targetDirection = (transform.position - playerTransform.position).normalized;
            }
        }

        if (!isPanicking)
        {
            Vector3 lureDir;
            if (BubbleLure.TryGetPull(transform.position, out lureDir))
            {
                // Player dropped a bubble lure: swim toward it
                targetDirection = lureDir;
            }
            else if (Vector3.Distance(transform.position, spawnPoint) > wanderRadius)
            {
                targetDirection = (spawnPoint - transform.position).normalized;
            }
            else if (timer >= nextChange)
            {
                ChooseRandomDirection();
            }

            // stay inside a comfortable band of water around the spawn height
            float dy = transform.position.y - spawnPoint.y;
            if (dy > VerticalBand) targetDirection.y = Mathf.Min(targetDirection.y, -0.3f);
            else if (dy < -VerticalBand) targetDirection.y = Mathf.Max(targetDirection.y, 0.3f);
        }

        Vector3 dir = swim.AvoidObstacles(targetDirection);
        float speed = isPanicking ? swimSpeed * panicSpeedMultiplier : swimSpeed;
        float turnRate = turnSpeed * 28f * (isPanicking ? 2.2f : 1f);   // degrees per second
        swim.Steer(dir, speed, turnRate, Time.fixedDeltaTime);
    }

    private void ChooseRandomDirection()
    {
        targetDirection = FishSwimAnimator.RandomSwimDirection();
        timer = 0f;
        nextChange = directionChangeInterval * Random.Range(0.7f, 1.5f);   // fish do not all turn at the same moment
    }
}
