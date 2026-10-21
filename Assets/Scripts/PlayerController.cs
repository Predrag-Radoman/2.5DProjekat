using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2.5D player controller (belt-scroller stil):
/// kretanje po X osi + dubina po Z osi (koridor), skok po Y osi.
/// Input: WASD / strelice / gamepad left stick; skok: Space / gamepad South.
/// Variable jump height: pustanje dugmeta u usponu sece skok (jump-cut).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Kretanje")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float groundAcceleration = 12f;
    [SerializeField] private float airAcceleration = 4f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("REPLACED feel")]
    [SerializeField] private float runEaseNearTop = 0.55f;
    [SerializeField] private float groundFriction = 14f;
    [SerializeField] private float turnSlowdown = 0.6f;
    [SerializeField] private float riseGravityMult = 0.95f;
    [SerializeField] private float fallGravityMult = 1.35f;
    [SerializeField] private float dashSpeed = 9f;
    [SerializeField] private float dashTime = 0.16f;
    [SerializeField] private float dashCooldown = 0.55f;
    [SerializeField] private float depthSpeedFactor = 0.55f;

    [Header("Skok")]
    [SerializeField] private float jumpVelocity = 5.5f;
    [SerializeField] private float jumpCutFactor = 0.45f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float groundCheckDistance = 0.15f;

    /// <summary>Dogodjenaj skoka (za SFX, VFX, Animator trigger).</summary>
    public event System.Action OnJumped;
    /// <summary>Doskok (za SFX, VFX, kasnije Land animaciju).</summary>
    public event System.Action OnLanded;
    /// <summary>Dodge step / dash (za SFX, VFX).</summary>
    public event System.Action OnDashed;

    public bool IsGrounded => grounded;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;
    public float MaxMoveSpeed => moveSpeed;
    public Vector3 HorizontalVelocity => rb != null
        ? new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z)
        : Vector3.zero;
    /// <summary>0 = stoji, 1 = puna brzina.</summary>
    public float NormalizedHorizontalSpeed => Mathf.Clamp01(HorizontalVelocity.magnitude / Mathf.Max(0.001f, moveSpeed));

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;

    private bool grounded;
    private bool wasGrounded = true;
    private float dashTimer;
    private float lastDashTime = -1f;
    private Vector3 dashDir = Vector3.forward;
    private InputAction dashAction;
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

        dashAction = new InputAction("Dash", binding: "<Keyboard>/leftShift");
        dashAction.AddBinding("<Gamepad>/buttonWest");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        dashAction.Enable();
        jumpAction.performed += OnJumpPerformed;
        jumpAction.canceled += OnJumpCanceled;
        dashAction.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPerformed;
        jumpAction.canceled -= OnJumpCanceled;
        dashAction.performed -= OnDashPerformed;
        moveAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        jumpBufferedTime = Time.time;
    }

    /// <summary>Variable jump height: pustis dugme u usponu -> skok se sece.</summary>
    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        if (rb.linearVelocity.y > 0f)
        {
            Vector3 v = rb.linearVelocity;
            v.y *= jumpCutFactor;
            rb.linearVelocity = v;
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext ctx)
    {
        if (Time.time - lastDashTime < dashCooldown) return;
        Vector2 mi = moveAction.ReadValue<Vector2>();
        Vector3 dir = Vector3.ClampMagnitude(new Vector3(mi.x, 0f, mi.y), 1f);
        dir.z *= depthSpeedFactor;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dashDir = dir.normalized;
        dashTimer = dashTime;
        lastDashTime = Time.time;
        OnDashed?.Invoke();
    }

    private void FixedUpdate()
    {
        UpdateGrounded();
        if (grounded) lastGroundedTime = Time.time;
        if (grounded && !wasGrounded) OnLanded?.Invoke();
        wasGrounded = grounded;

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        // 2.5D mapiranje: X input -> sveta X, Y input -> sveta Z (dubina koridora,
        // REPLACED: dubina sporija od bocnog kretanja)
        Vector3 wish = Vector3.ClampMagnitude(new Vector3(moveInput.x, 0f, moveInput.y), 1f);
        wish.z *= depthSpeedFactor;
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float speedRatio = Mathf.Clamp01(horizontal.magnitude / Mathf.Max(0.001f, moveSpeed));

        if (dashTimer > 0f)
        {
            // dodge step: kratki burst-impuls override
            dashTimer -= Time.fixedDeltaTime;
            horizontal = dashDir * dashSpeed;
        }
        else
        {
            float accel = grounded ? groundAcceleration : airAcceleration;
            if (wish.sqrMagnitude > 0.01f)
            {
                // momentum run: ease-out ubrzanje blizu top speed-a
                accel *= 1f - runEaseNearTop * speedRatio;
                // skid pri smeni smera (turn pivot)
                if (Vector3.Dot(horizontal, wish) < 0f) accel *= turnSlowdown;
            }
            else
            {
                // friction/skid na pustanje (ne instant stop)
                accel = grounded ? groundFriction : airAcceleration * 0.5f;
            }
            float t = 1f - Mathf.Exp(-accel * Time.fixedDeltaTime);
            Vector3 targetHorizontal = wish * moveSpeed;
            horizontal = Vector3.Lerp(horizontal, targetHorizontal, t);
        }

        Vector3 newVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

        // REPLACED: tezina — floaty uspon, fast-fall pad
        float gMult = newVelocity.y < 0f ? fallGravityMult : riseGravityMult;
        newVelocity.y += Physics.gravity.y * (gMult - 1f) * Time.fixedDeltaTime;

        // coyote time + jump buffer = osecaj kontrole
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - jumpBufferedTime <= jumpBufferTime;
        if (canJump && wantsJump)
        {
            newVelocity.y = jumpVelocity;
            lastGroundedTime = -1f;
            jumpBufferedTime = -1f;
            OnJumped?.Invoke();
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
