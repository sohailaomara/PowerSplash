using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class FishMovement3D : MonoBehaviour
{
    [Header("3D Movement Speeds")]
    [SerializeField] private float swimSpeed = 10f;
    [SerializeField] private float sprintSpeed = 18f;
    [SerializeField] private float turnSpeed = 3f;
    [SerializeField] private float pitchSpeed = 2.5f;
    [SerializeField] private float rollAmount = 20f;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    private float currentStamina;

    private Rigidbody rb;
    private float currentSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Zero gravity for underwater swimming

        // Unity 6.3 damping settings
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2.0f;

        currentStamina = maxStamina;
        currentSpeed = swimSpeed;
    }

    private void FixedUpdate()
    {
        Handle3DSwimming();
    }

    private void Handle3DSwimming()
    {
        float inputX = 0f; // Yaw (Left / Right)
        float inputZ = 0f; // Forward / Backward
        float inputY = 0f; // Pitch (Up / Down)

        if (Keyboard.current != null)
        {
            // Steering Left / Right (X-axis)
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputX = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputX = 1f;

            // Forward / Backward swimming (Z-axis / into screen)
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputZ = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputZ = -1f;

            // Pitching Up / Down (Y-axis)
            if (Keyboard.current.eKey.isPressed || Keyboard.current.spaceKey.isPressed) inputY = 1f;   // Swim Up
            if (Keyboard.current.qKey.isPressed || Keyboard.current.leftCtrlKey.isPressed) inputY = -1f; // Swim Down
        }

        // Sprinting toggle
        bool isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && currentStamina > 0;
        currentSpeed = isSprinting ? sprintSpeed : swimSpeed;

        if (isSprinting) currentStamina -= Time.fixedDeltaTime * 20f;
        else if (currentStamina < maxStamina) currentStamina += Time.fixedDeltaTime * 10f;

        // 1. Rotate Fish in 3D Space (Yaw, Pitch, Roll)
        transform.Rotate(Vector3.up, inputX * turnSpeed, Space.World);
        transform.Rotate(Vector3.right, -inputY * pitchSpeed, Space.Self);

        // Visual banking/rolling when turning left/right
        float targetRoll = -inputX * rollAmount;
        Vector3 curRot = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(curRot.x, curRot.y, Mathf.LerpAngle(curRot.z, targetRoll, Time.fixedDeltaTime * 5f));

        // 2. Thrust in the direction the fish is facing (True 3D Vector)
        if (inputZ != 0f)
        {
            Vector3 swimDirection = transform.forward * inputZ * currentSpeed;
            rb.AddForce(swimDirection - rb.linearVelocity, ForceMode.Acceleration);
        }
    }
}