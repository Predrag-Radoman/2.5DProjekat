using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2.5D player controller (belt-scroller stil):
/// kretanje po X osi + dubina po Z osi (koridor), skok po Y osi.
/// Input: WASD / strelice / gamepad left stick; skok: Space / gamepad South.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Kretanje")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float groundAcceleration = 12f;
    [SerializeField] private float airAcceleration = 4f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Skok")]
    [SerializeField] private float jumpVelocity = 5.5f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float groundCheckDistance = 0.15f;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;

    private bool grounded;
    private float lastGroundedTime = -1f;
    private float jumpBufferedTime = -1f;

    private const float CapsuleRadius = 0.5f;
    private const float CapsuleHeight = 2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        jumpAction.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPerformed;
        moveAction.Disable();
        jumpAction.Disable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        jumpBufferedTime = Time.time;
    }

    private void FixedUpdate()
    {
        UpdateGrounded();
        if (grounded) lastGroundedTime = Time.time;

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        // 2.5D mapiranje: X input -> sveta X, Y input -> sveta Z (dubina koridora)
        Vector3 wish = Vector3.ClampMagnitude(new Vector3(moveInput.x, 0f, moveInput.y), 1f);

        Vector3 velocity = rb.linearVelocity;
        float accel = grounded ? groundAcceleration : airAcceleration;
        float t = 1f - Mathf.Exp(-accel * Time.fixedDeltaTime);
        Vector3 targetHorizontal = wish * moveSpeed;
        Vector3 newVelocity = new Vector3(
            Mathf.Lerp(velocity.x, targetHorizontal.x, t),
            velocity.y,
            Mathf.Lerp(velocity.z, targetHorizontal.z, t));

        // coyote time + jump buffer = osecaj kontrole
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - jumpBufferedTime <= jumpBufferTime;
        if (canJump && wantsJump)
        {
            newVelocity.y = jumpVelocity;
            lastGroundedTime = -1f;
            jumpBufferedTime = -1f;
        }

        rb.linearVelocity = newVelocity;

        // okretanje modela u smeru kretanja
        if (wish.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(wish);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void UpdateGrounded()
    {
        // sfera na dnu capsule-a + mali cast nadole, bez samo-detekcije (maska bez Player layer-a)
        float bottomSphereY = CapsuleHeight * 0.5f - CapsuleRadius;
        Vector3 p = transform.position + Vector3.down * bottomSphereY;
        int mask = ~(1 << gameObject.layer);
        grounded = Physics.CapsuleCast(p, p, CapsuleRadius * 0.9f, Vector3.down, out _, groundCheckDistance, mask);
    }
}
