using UnityEngine;
using UnityEngine.InputSystem; // Added for New Input System

[RequireComponent(typeof(Rigidbody))]
public class FishMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float normalSpeed = 8f;
    [SerializeField] private float sprintSpeed = 15f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float turnSpeed = 4f;

    [Header("Pitch & Roll (Banking) Settings")]
    [SerializeField] private float pitchSpeed = 3f;
    [SerializeField] private float bankAmount = 25f;

    [Header("Stamina Mechanics")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina;
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRegenRate = 15f;

    private Rigidbody rb;
    private float currentSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        // Unity 6 / modern Unity syntax for damping
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2.0f;

        currentStamina = maxStamina;
        currentSpeed = normalSpeed;
    }

    private void FixedUpdate()
    {
        HandleSwimming();
    }

    private void HandleSwimming()
    {
        // 1. Get Input via New Input System Keyboard API
        float moveHorizontal = 0f;
        float moveVertical = 0f;
        float movePitch = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveHorizontal = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveHorizontal = 1f;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveVertical = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveVertical = -1f;

            if (Keyboard.current.eKey.isPressed) movePitch = 1f;   // Swim Up
            if (Keyboard.current.qKey.isPressed) movePitch = -1f;  // Swim Down
        }

        // 2. Sprint / Stamina Management
        bool isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && currentStamina > 0 && moveVertical > 0;

        if (isSprinting)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, sprintSpeed, Time.fixedDeltaTime * acceleration);
            currentStamina -= staminaDrainRate * Time.fixedDeltaTime;
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, normalSpeed, Time.fixedDeltaTime * acceleration);
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.fixedDeltaTime;
            }
        }

        // 3. Apply Steering & Rotation
        transform.Rotate(Vector3.up, moveHorizontal * turnSpeed, Space.World);
        transform.Rotate(Vector3.right, movePitch * pitchSpeed, Space.Self);

        // Visual Banking / Tilting on Turn
        float targetRoll = -moveHorizontal * bankAmount;
        Vector3 currentEuler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, Mathf.LerpAngle(currentEuler.z, targetRoll, Time.fixedDeltaTime * 5f));

        // 4. Apply Thrust Forward
        if (moveVertical > 0)
        {
            Vector3 targetVelocity = transform.forward * currentSpeed;
            rb.AddForce(targetVelocity - rb.linearVelocity, ForceMode.Acceleration);
        }
    }

    public float GetStaminaPercentage() => currentStamina / maxStamina;
}