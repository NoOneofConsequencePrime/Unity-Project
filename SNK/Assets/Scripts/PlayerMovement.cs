using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // References
    public Transform playerBody;
    private CharacterController controller;
    public Camera playerCamera;
    public Transform debugHitPointTransform;

    // Play Settings
    float speed = 12f;
    float gravity = -96.2361f;
    float jumpHeight = 3f;
    float grappleSpeedMultiplier = 2f;
    float grappleSpeedMin = 10f;
    float grappleSpeedMax = 40f;
    float momentumDrag = 3f;
    float momentumExtraSpeed = 7f;// For flinging out from grapple jump

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    public float mouseSensitivity = 100f;
    private State state;
    
    // Variables
    Vector3 velocity;
    Vector3 velocityMomentum;
    bool isGrounded;
    float xRotation = 0f;
    Vector3 grapplePosition;

    private enum State
    {
        Normal,
        GrappleFlyingPlayer
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        state = State.Normal;
    }

    void Update()
    {
        switch (state)
        {
            default:
            case State.Normal:
                HandleCharacterMovement();
                HandleCharacterLook();
                HandleGrappleStart();
                break;
            case State.GrappleFlyingPlayer:
                HandleCharacterLook();
                HandleGrappleMovement();
                break;
        }
    }

    private void HandleCharacterMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        // Apply Momentum
        velocity += velocityMomentum;

        Debug.Log(velocityMomentum);
        // Move Character Controller
        controller.Move(velocity * Time.deltaTime);

        // Dampen Momentum
        if (velocityMomentum.magnitude >= 0f)
        {
            velocityMomentum -= velocityMomentum * momentumDrag * Time.deltaTime;
            if (velocityMomentum.magnitude < .0f)
            {
                velocityMomentum = Vector3.zero;
            }
        }
    }

    private void HandleCharacterLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private void HandleGrappleStart()
    {
        if (Input.GetButtonDown("Grapple"))
        {
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit raycastHit))
            {
                // Hit something
                debugHitPointTransform.position = raycastHit.point;
                grapplePosition = raycastHit.point;
                state = State.GrappleFlyingPlayer;
            }
        }
    }

    private void ResetGravityEffect()
    {
        velocity.y = 0f;
    }

    private void HandleGrappleMovement()
    {
        Vector3 grappleDir = (grapplePosition - transform.position).normalized;

        float grappleSpeed = Mathf.Clamp(Vector3.Distance(transform.position, grapplePosition), grappleSpeedMin, grappleSpeedMax);

        // Move Character Controller
        controller.Move(grappleDir * grappleSpeed * grappleSpeedMultiplier * Time.deltaTime);

        float reachedGrapplePositionDistance = 1f;
        if (Vector3.Distance(transform.position, grapplePosition) < reachedGrapplePositionDistance)
        {
            // Reached Grapple Position
            state = State.Normal;
            ResetGravityEffect();
        }

        if (Input.GetButtonDown("Grapple"))
        {
            // Cancel Grapple
            state = State.Normal;
            ResetGravityEffect();
        }

        if (Input.GetButtonDown("Jump"))
        {
            // Cancelled with Jump
            velocityMomentum = grappleDir * grappleSpeed * momentumExtraSpeed;
            state = State.Normal;
            ResetGravityEffect();
        }
    }
}
