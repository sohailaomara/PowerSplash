using UnityEngine;

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

    private Rigidbody rb;
    private Transform playerTransform;
    private Vector3 spawnPoint;
    private Vector3 targetDirection;
    private float timer;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2.0f;

        spawnPoint = transform.position;
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
            else if (timer >= directionChangeInterval)
            {
                ChooseRandomDirection();
                timer = 0f;
            }
        }

        // Smooth rotation & thrust
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * turnSpeed);
        }

        float speed = isPanicking ? swimSpeed * panicSpeedMultiplier : swimSpeed;
        rb.AddForce((transform.forward * speed) - rb.linearVelocity, ForceMode.Acceleration);
    }

    private void ChooseRandomDirection()
    {
        targetDirection = Random.onUnitSphere;
    }
}