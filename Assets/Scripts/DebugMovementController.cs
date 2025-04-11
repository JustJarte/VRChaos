using Fusion;
using UnityEngine;
using UnityEngine.XR;

public class DebugMovementController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.0f;
    public float jumpForce = 6.0f;
    public float gravity = -9.8f;
    public float crouchHeight = 0.36f;
    public float standingHeight = 0.78f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private Camera mainCam;
    private bool isCrouching;

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            enabled = false;
            return;
        }

        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        controller.height = standingHeight;
        controller.center = new Vector3(0.0f, 0.15f, 0.0f);

        mainCam = Camera.main;
    }

    private void Update()
    {
        if (!HasInputAuthority || VRModeManager.UseGorillaLocomotion)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        transform.Rotate(Vector3.up * mouseX * 4.0f);

        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0.0f)
        {
            velocity.y = -2.0f;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 input = new Vector3(h, 0.0f, v).normalized;

        if (Camera.main != null)
        {
            Vector3 move = transform.TransformDirection(input);
            move.y = 0.0f;

            controller.Move(move * moveSpeed * Time.deltaTime);
        }

        //Jumping input
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = jumpForce;
        }

        //Apply gravity to jump
        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        //Crounching input
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            controller.height = crouchHeight;
            controller.center = new Vector3(0.0f, crouchHeight / 2.0f, 0.0f);
            isCrouching = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            controller.height = standingHeight;
            controller.center = new Vector3(0.0f, standingHeight / 2.0f, 0.0f);
            isCrouching = false;
        }
    }
}
