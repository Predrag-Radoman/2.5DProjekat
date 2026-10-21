using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2.5D player controller (belt-scroller stil):
/// kretanje po X osi + dubina po Z osi (koridor), skok po Y osi.
/// Input: WASD / strelice / gamepad left stick; skok: Space / gamepad South.
/// Shift = toggle trcanje/hodanje (stamina), Ctrl = toggle cucanj, C = toggle puzanje.
/// Ledge grab: auto u vazduhu, A/D = shimmy duz ivice, W = penjanje, S = pustanje.
/// Variable jump height: pustanje dugmeta u usponu sece skok (jump-cut).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Kretanje")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float walkSpeedFactor = 0.55f;
    [SerializeField] private float runSpeedFactor = 1.0f;
    [SerializeField] private float groundAcceleration = 12f;
    [SerializeField] private float airAcceleration = 4f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Stamina")]
    [SerializeField] private bool showStaminaHud = true;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainPerSecond = 30f;
    [SerializeField] private float staminaRegenPerSecond = 15f;
    [SerializeField] private float staminaRegenDelay = 0.7f;
    [SerializeField] private float staminaMinToStart = 15f;

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

    [Header("Cucanj / puzanje")]
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float crawlHeight = 0.8f;
    [SerializeField] private float crawlRadius = 0.4f;
    [SerializeField] private float crouchSpeedFactor = 0.35f;
    [SerializeField] private float crawlSpeedFactor = 0.18f;

    [Header("Skok")]
    [SerializeField] private float jumpVelocity = 5.5f;
    [SerializeField] private float jumpCutFactor = 0.45f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float groundCheckDistance = 0.15f;

    [Header("Ledge grab (hvatanje ivice)")]
    [SerializeField] private bool ledgeGrabEnabled = true;
    [SerializeField] private float ledgeGrabReach = 0.35f;
    [SerializeField] private float ledgeGrabMinHeight = 0.6f;
    [SerializeField] private float ledgeGrabMaxHeight = 2.3f;
    [SerializeField] private float hangFeetOffset = 1.95f;
    [SerializeField] private float ledgeShimmySpeed = 1.2f;
    [SerializeField] private float ledgeJumpVelocity = 5.5f;
    [SerializeField] private float ledgeJumpPush = 6f;
    [SerializeField] private string kickOffObjectName = "Container";
    [SerializeField] private float mantleTime = 0.9f;
    [SerializeField] private float containerClimbTime = 1.4f;
    [SerializeField] private float mantleInset = 0.12f;
    [SerializeField] private float mantleRecoveryTime = 0.3f;
    [SerializeField] private float ledgeRegrabDelay = 0.35f;

    /// <summary>Dogodjenaj skoka (za SFX, VFX, Animator trigger).</summary>
    public event System.Action OnJumped;
    /// <summary>Doskok (za SFX, VFX, kasnije Land animaciju).</summary>
    public event System.Action OnLanded;
    /// <summary>Dodge step / dash (za SFX, VFX).</summary>
    public event System.Action OnDashed;
    /// <summary>Promena cucnja (za SFX, VFX, Animator).</summary>
    public event System.Action<bool> OnCrouchChanged;
    /// <summary>Promena puzanja (prone crawl).</summary>
    public event System.Action<bool> OnCrawlChanged;

    public bool IsGrounded => grounded;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;
    public float MaxMoveSpeed => moveSpeed;
    public bool IsCrouching => crouching;
    public bool IsCrawling => crawling;
    public bool IsRunning => running;
    public float Stamina => stamina;
    public float MaxStamina => maxStamina;
    public float StaminaNormalized => maxStamina > 0.001f ? Mathf.Clamp01(stamina / maxStamina) : 0f;
    public bool IsExhausted => stamina <= 0.01f;
    public bool IsLedgeHanging => ledgeHanging;
    public bool IsMantling => mantling;
    public bool IsLedgeKicking => Time.time < ledgeKickUntil;
    public float LedgeKickProgress => IsLedgeKicking ? Mathf.Clamp01((Time.time - ledgeKickStart) / 0.22f) : 1f;
    /// <summary>-1..1, input duž ivice dok visis.</summary>
    public float LedgeShimmy => ledgeShimmy;
    /// <summary>0..1 napredak mantle animacije.</summary>
    public float MantleProgress => mantling ? Mathf.Clamp01(mantleTimer / Mathf.Max(0.001f, activeMantleTime)) : 0f;
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
    private InputAction crouchAction;
    private InputAction crawlAction;
    private InputAction sprintAction;
    private bool crouchToggled;
    private bool crawlToggled;
    private bool running;
    private float stamina;
    private float staminaRegenTimer;
    private bool crouching;
    private bool crawling;
    private bool ledgeHanging;
    private bool mantling;
    private bool ledgeMantleArmed;
    private bool ledgeKickAllowed;
    private Collider ledgeRootCollider;
    private Vector3 ledgePoint;
    private Vector3 ledgeNormal;
    private Vector3 mantleStart;
    private Vector3 mantleTarget;
    private float mantleTimer;
    private float activeMantleTime = 0.9f;
    private float ledgeShimmy;
    private float grabCooldownUntil;
    private float mantleRecoveryUntil;
    private float jumpCutLockUntil;
    private float ledgeKickStart;
    private float ledgeKickUntil;
    private float ledgeJumpFaceAt;
    private float ledgeJumpFaceDir;
    private float ledgeJumpSteerUntil;
    private CapsuleCollider capsule;
    private float standingHeight = 2f;
    private float standingRadius = 0.5f;
    private float feetLocalY = -1f;
    private float lastGroundedTime = -1f;
    private float jumpBufferedTime = -1f;

    private const float CapsuleRadius = 0.5f;
    private const float CapsuleHeight = 2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        stamina = maxStamina;

        if (showStaminaHud && GetComponent<StaminaHud>() == null)
            gameObject.AddComponent<StaminaHud>();

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

        dashAction = new InputAction("Dash", binding: "<Keyboard>/leftAlt");
        dashAction.AddBinding("<Gamepad>/buttonWest");

        sprintAction = new InputAction("Sprint", binding: "<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");

        crouchAction = new InputAction("Crouch", binding: "<Keyboard>/leftCtrl");
        crouchAction.AddBinding("<Gamepad>/buttonNorth");

        crawlAction = new InputAction("Crawl", binding: "<Keyboard>/c");
        crawlAction.AddBinding("<Gamepad>/buttonEast");

        capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            standingHeight = capsule.height;
            standingRadius = capsule.radius;
            feetLocalY = capsule.center.y - capsule.height * 0.5f;
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        dashAction.Enable();
        sprintAction.Enable();
        crouchAction.Enable();
        crawlAction.Enable();
        jumpAction.performed += OnJumpPerformed;
        jumpAction.canceled += OnJumpCanceled;
        dashAction.performed += OnDashPerformed;
        sprintAction.performed += OnSprintPerformed;
        crouchAction.performed += OnCrouchPerformed;
        crawlAction.performed += OnCrawlPerformed;
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPerformed;
        jumpAction.canceled -= OnJumpCanceled;
        dashAction.performed -= OnDashPerformed;
        sprintAction.performed -= OnSprintPerformed;
        crouchAction.performed -= OnCrouchPerformed;
        crawlAction.performed -= OnCrawlPerformed;
        moveAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
        sprintAction.Disable();
        crouchAction.Disable();
        crawlAction.Disable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        jumpBufferedTime = Time.time;
    }

    /// <summary>Variable jump height: pustis dugme u usponu -> skok se sece.</summary>
    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        if (Time.time < jumpCutLockUntil) return;
        if (rb.linearVelocity.y > 0f)
        {
            Vector3 v = rb.linearVelocity;
            v.y *= jumpCutFactor;
            rb.linearVelocity = v;
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext ctx)
    {
        if (ledgeHanging || mantling) return;
        if (crouching || crawling) return;
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

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        if (!running && stamina < staminaMinToStart) return;
        running = !running;
    }

    private void OnCrouchPerformed(InputAction.CallbackContext ctx)
    {
        if (ledgeHanging || mantling) return;
        crouchToggled = !crouchToggled;
        if (crouchToggled) crawlToggled = false;
    }

    private void OnCrawlPerformed(InputAction.CallbackContext ctx)
    {
        if (ledgeHanging || mantling) return;
        crawlToggled = !crawlToggled;
        if (crawlToggled) crouchToggled = false;
    }

    private void UpdateStance()
    {
        bool wantCrawl = crawlToggled;
        bool wantCrouch = crouchToggled;

        // puzanje ima prioritet i uvek je dozvoljeno (nize = bezbedno)
        if (wantCrawl)
        {
            SetStance(crouch: false, crawl: true);
            return;
        }

        // iz puzanja: prvo probaj stajanje, pa cucanj (ako ima mesta)
        if (crawling)
        {
            if (wantCrouch)
            {
                if (HeadroomClear(crouchHeight)) SetStance(true, false);
            }
            else if (HeadroomClear(standingHeight))
            {
                SetStance(false, false);
            }
            else if (HeadroomClear(crouchHeight))
            {
                crouchToggled = true;
                SetStance(true, false);
            }
            return;
        }

        if (wantCrouch)
        {
            SetStance(true, false);
            return;
        }

        if (crouching && HeadroomClear(standingHeight))
            SetStance(false, false);
    }

    private void SetStance(bool crouch, bool crawl)
    {
        if (crouching == crouch && crawling == crawl) return;
        bool crawlChanged = crawling != crawl;
        crouching = crouch;
        crawling = crawl;
        ApplyCapsuleHeight();
        if (crawlChanged) OnCrawlChanged?.Invoke(crawling);
        OnCrouchChanged?.Invoke(crouching);
    }

    private void ApplyCapsuleHeight()
    {
        if (capsule == null) return;
        float h = crawling ? crawlHeight : (crouching ? crouchHeight : standingHeight);
        capsule.height = h;
        capsule.radius = crawling ? crawlRadius : standingRadius;
        capsule.center = new Vector3(0f, feetLocalY + h * 0.5f, 0f);
    }

    private bool HeadroomClear(float targetHeight)
    {
        if (capsule == null) return true;
        float delta = targetHeight - capsule.height;
        if (delta <= 0f) return true;
        // za proveru koristi standing radius (najsiri profil), i kad si nizi
        float topY = transform.position.y + feetLocalY + capsule.height - standingRadius;
        Vector3 p = new Vector3(transform.position.x, topY, transform.position.z);
        return !Physics.SphereCast(p, standingRadius * 0.9f, Vector3.up, out _, delta + 0.05f, ~(1 << gameObject.layer));
    }

    private void FixedUpdate()
    {
        if (ledgeHanging)
        {
            UpdateLedgeState();
            return;
        }

        UpdateGrounded();
        if (grounded) lastGroundedTime = Time.time;
        if (grounded && !wasGrounded) OnLanded?.Invoke();
        wasGrounded = grounded;
        UpdateStance();

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        if (Time.time < mantleRecoveryUntil) moveInput = Vector2.zero;

        if (ledgeGrabEnabled && !grounded && !crouching && !crawling && dashTimer <= 0f &&
            moveInput.y > -0.5f && Time.time >= grabCooldownUntil && rb.linearVelocity.y <= 0.5f &&
            TryFindLedge(out Vector3 ledgeTop, out Vector3 ledgeHitNormal))
        {
            BeginLedgeHang(ledgeTop, ledgeHitNormal);
            return;
        }

        // 2.5D mapiranje: X input -> sveta X, Y input -> sveta Z (dubina koridora,
        // REPLACED: dubina sporija od bocnog kretanja)
        Vector3 wish = Vector3.ClampMagnitude(new Vector3(moveInput.x, 0f, moveInput.y), 1f);
        wish.z *= depthSpeedFactor;
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        UpdateStamina(wish.sqrMagnitude > 0.01f);

        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float effSpeed = moveSpeed;
        if (crawling) effSpeed *= crawlSpeedFactor;
        else if (crouching) effSpeed *= crouchSpeedFactor;
        else if (running) effSpeed *= runSpeedFactor;
        else effSpeed *= walkSpeedFactor;
        float speedRatio = Mathf.Clamp01(horizontal.magnitude / Mathf.Max(0.001f, effSpeed));

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
            Vector3 targetHorizontal = wish * effSpeed;
            horizontal = Vector3.Lerp(horizontal, targetHorizontal, t);
        }

        Vector3 newVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

        // REPLACED: tezina — floaty uspon, fast-fall pad
        float gMult = newVelocity.y < 0f ? fallGravityMult : riseGravityMult;
        newVelocity.y += Physics.gravity.y * (gMult - 1f) * Time.fixedDeltaTime;

        // coyote time + jump buffer = osecaj kontrole
        bool canJump = Time.time - lastGroundedTime <= coyoteTime && !crouching && !crawling;
        bool wantsJump = Time.time - jumpBufferedTime <= jumpBufferTime;
        if (canJump && wantsJump)
        {
            newVelocity.y = jumpVelocity;
            lastGroundedTime = -1f;
            jumpBufferedTime = -1f;
            OnJumped?.Invoke();
        }

        rb.linearVelocity = newVelocity;

        if (ledgeJumpFaceAt > 0f && Time.time >= ledgeJumpFaceAt)
        {
            if (Mathf.Abs(ledgeJumpFaceDir) > 0.1f)
                rb.rotation = Quaternion.LookRotation(new Vector3(ledgeJumpFaceDir, 0f, 0f), Vector3.up);
            ledgeJumpFaceAt = 0f;
        }

        if (!grounded && Time.time < ledgeJumpSteerUntil)
        {
            Vector2 steer = moveAction.ReadValue<Vector2>();
            if (Mathf.Abs(steer.x) > 0.1f)
                rb.rotation = Quaternion.LookRotation(new Vector3(Mathf.Sign(steer.x), 0f, 0f), Vector3.up);
        }

        // okretanje modela u smeru kretanja — SAMO na tlu:
        // realno, u vazduhu facing je zakljucan (ne moze da se okrene u letu)
        if (grounded && wish.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(wish);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private bool TryFindLedge(out Vector3 topPoint, out Vector3 normal)
    {
        topPoint = Vector3.zero;
        normal = Vector3.zero;
        if (capsule == null) return false;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) return false;
        forward.Normalize();

        int mask = ~(1 << gameObject.layer);
        float feetY = transform.position.y + feetLocalY;
        float reach = standingRadius + ledgeGrabReach;
        float probeStartY = feetY + ledgeGrabMaxHeight + 0.5f;
        float probeLength = ledgeGrabMaxHeight + 1f;

        for (int i = 0; i < 9; i++)
        {
            float height = Mathf.Lerp(0.4f, 2f, i / 8f);
            Vector3 origin = new Vector3(transform.position.x, feetY + height, transform.position.z);
            if (!Physics.Raycast(origin, forward, out RaycastHit wall, reach, mask, QueryTriggerInteraction.Ignore))
                continue;
            if (Mathf.Abs(Vector3.Dot(wall.normal, Vector3.up)) > 0.35f) continue;
            if (Vector3.Dot(wall.normal, forward) > -0.5f) continue;

            Vector3 inward = wall.normal;
            inward.y = 0f;
            inward = -inward.normalized;
            Vector3 probe = wall.point + inward * 0.02f;
            Vector3 probeStart = new Vector3(probe.x, probeStartY, probe.z);
            if (!Physics.Raycast(probeStart, Vector3.down, out RaycastHit top, probeLength, mask, QueryTriggerInteraction.Ignore))
                continue;
            if (Vector3.Dot(top.normal, Vector3.up) < 0.7f) continue;
            if (top.point.y <= wall.point.y) continue;

            float delta = top.point.y - feetY;
            if (delta < ledgeGrabMinHeight || delta > ledgeGrabMaxHeight) continue;

            topPoint = new Vector3(wall.point.x, top.point.y, wall.point.z);
            normal = new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
            ledgeKickAllowed = IsKickOffSurface(wall.collider);
            ledgeRootCollider = wall.collider.transform.root.GetComponent<Collider>();
            if (ledgeRootCollider == null) ledgeRootCollider = wall.collider;
            return true;
        }

        return false;
    }

    private void BeginLedgeHang(Vector3 topPoint, Vector3 normal)
    {
        ledgeHanging = true;
        mantling = false;
        ledgePoint = topPoint;
        ledgeNormal = normal;
        ledgeShimmy = 0f;
        crouching = false;
        crawling = false;
        crouchToggled = false;
        crawlToggled = false;
        ledgeMantleArmed = moveAction.ReadValue<Vector2>().y < 0.5f;
        ApplyCapsuleHeight();

        dashTimer = 0f;
        jumpBufferedTime = -1f;
        lastGroundedTime = -1f;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.position = LedgePosition(ledgePoint);
        rb.rotation = Quaternion.LookRotation(-ledgeNormal, Vector3.up);
    }

    private Vector3 LedgePosition(Vector3 point)
    {
        Vector3 p = point + ledgeNormal * (standingRadius + 0.05f);
        p.y = point.y - hangFeetOffset - feetLocalY;
        return p;
    }

    private void UpdateLedgeState()
    {
        if (mantling)
        {
            UpdateMantle();
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        ledgeShimmy = input.x;
        if (input.y < 0.5f) ledgeMantleArmed = true;

        if (Time.time - jumpBufferedTime <= jumpBufferTime)
        {
            jumpBufferedTime = -1f;
            if (ledgeKickAllowed)
            {
                PerformLedgeJump(input);
                return;
            }
        }

        if (input.y < -0.5f)
        {
            ReleaseLedge();
            return;
        }

        if (input.y > 0.5f && ledgeMantleArmed)
        {
            if (ledgeKickAllowed && TryBeginContainerClimb())
                return;
            if (CanMantle())
            {
                BeginMantle();
                return;
            }
        }

        Vector3 position = LedgePosition(ledgePoint);
        if (Mathf.Abs(input.x) > 0.1f)
        {
            Vector3 tangent = transform.right;
            tangent.y = 0f;
            tangent.Normalize();
            Vector3 candidate = ledgePoint + tangent * (input.x * ledgeShimmySpeed * Time.fixedDeltaTime);
            if (LedgeExistsAt(candidate))
            {
                ledgePoint = candidate;
                position = LedgePosition(ledgePoint);
            }
            else
            {
                ledgeShimmy = 0f;
            }
        }

        rb.MovePosition(position);
        rb.MoveRotation(Quaternion.LookRotation(-ledgeNormal, Vector3.up));
    }

    private bool IsKickOffSurface(Collider surface)
    {
        if (surface == null || string.IsNullOrEmpty(kickOffObjectName)) return false;
        string rootName = surface.transform.root.name;
        return rootName.IndexOf(kickOffObjectName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool LedgeExistsAt(Vector3 point)
    {
        int mask = ~(1 << gameObject.layer);
        Vector3 probe = point - ledgeNormal * 0.02f;
        Vector3 start = new Vector3(probe.x, point.y + 0.7f, probe.z);
        if (!Physics.Raycast(start, Vector3.down, out RaycastHit top, 1.4f, mask, QueryTriggerInteraction.Ignore))
            return false;
        if (Vector3.Dot(top.normal, Vector3.up) < 0.7f) return false;
        if (Mathf.Abs(top.point.y - ledgePoint.y) > 0.15f) return false;

        ledgeKickAllowed = IsKickOffSurface(top.collider);
        ledgeRootCollider = top.collider.transform.root.GetComponent<Collider>();
        if (ledgeRootCollider == null) ledgeRootCollider = top.collider;
        return true;
    }

    private Vector3 MantleTargetFeet()
    {
        Vector3 feet = ledgePoint - ledgeNormal * mantleInset;
        feet.y = ledgePoint.y + 0.02f;
        return feet;
    }

    private bool CanMantle()
    {
        int mask = ~(1 << gameObject.layer);
        Vector3 feet = MantleTargetFeet();

        if (!Physics.Raycast(feet + Vector3.up * 0.8f, Vector3.down, out RaycastHit ground, 1.6f, mask, QueryTriggerInteraction.Ignore))
            return false;
        if (Vector3.Dot(ground.normal, Vector3.up) < 0.7f) return false;
        if (Mathf.Abs(ground.point.y - ledgePoint.y) > 0.25f) return false;

        Vector3 bottom = feet + Vector3.up * (standingRadius + 0.02f);
        Vector3 top = feet + Vector3.up * (standingHeight - standingRadius - 0.02f);
        return !Physics.CheckCapsule(bottom, top, standingRadius * 0.95f, mask, QueryTriggerInteraction.Ignore);
    }

    private bool TryBeginContainerClimb()
    {
        if (ledgeRootCollider == null) return false;

        Bounds b = ledgeRootCollider.bounds;
        float topY = b.max.y;
        if (topY < ledgePoint.y + 0.4f) return false;

        Vector3 inward = -ledgeNormal;
        Vector3 feet = ledgePoint + inward * 0.6f;
        feet.x = Mathf.Clamp(feet.x, b.min.x + 0.3f, b.max.x - 0.3f);
        feet.z = Mathf.Clamp(feet.z, b.min.z + 0.3f, b.max.z - 0.3f);
        feet.y = topY + 0.02f;

        mantling = true;
        ledgeShimmy = 0f;
        mantleTimer = 0f;
        mantleStart = rb.position;
        mantleTarget = feet - Vector3.up * feetLocalY;
        activeMantleTime = containerClimbTime;
        jumpBufferedTime = -1f;
        return true;
    }

    private void BeginMantle()
    {
        mantling = true;
        ledgeShimmy = 0f;
        mantleTimer = 0f;
        mantleStart = rb.position;
        mantleTarget = MantleTargetFeet() - Vector3.up * feetLocalY;
        activeMantleTime = mantleTime;
        jumpBufferedTime = -1f;
    }

    private void UpdateMantle()
    {
        mantleTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(mantleTimer / Mathf.Max(0.001f, activeMantleTime));
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.7f));
        float move = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.7f) / 0.3f));
        Vector3 pos = new Vector3(
            Mathf.Lerp(mantleStart.x, mantleTarget.x, move),
            Mathf.Lerp(mantleStart.y, mantleTarget.y, rise),
            Mathf.Lerp(mantleStart.z, mantleTarget.z, move));

        rb.MovePosition(pos);
        rb.MoveRotation(Quaternion.LookRotation(-ledgeNormal, Vector3.up));

        if (t >= 1f) FinishMantle();
    }

    private void FinishMantle()
    {
        rb.position = mantleTarget;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        ledgeHanging = false;
        mantling = false;
        ledgeShimmy = 0f;
        grabCooldownUntil = Time.time + ledgeRegrabDelay;
        mantleRecoveryUntil = Time.time + mantleRecoveryTime;
    }

    private void ReleaseLedge()
    {
        ledgeHanging = false;
        mantling = false;
        ledgeShimmy = 0f;
        rb.position += ledgeNormal * 0.1f;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = ledgeNormal * 1.2f;
        grabCooldownUntil = Time.time + ledgeRegrabDelay;
    }

    private void PerformLedgeJump(Vector2 input)
    {
        float dir = Mathf.Abs(input.x) > 0.1f ? Mathf.Sign(input.x) : 0f;

        ledgeHanging = false;
        mantling = false;
        ledgeShimmy = 0f;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.position += ledgeNormal * 0.03f;
        rb.linearVelocity = new Vector3(dir * ledgeJumpPush, ledgeJumpVelocity, 0f);
        grabCooldownUntil = Time.time + ledgeRegrabDelay;
        jumpCutLockUntil = Time.time + 0.2f;
        ledgeKickStart = Time.time;
        ledgeKickUntil = Time.time + 0.22f;
        ledgeJumpFaceDir = dir;
        ledgeJumpFaceAt = Time.time + 0.22f;
        ledgeJumpSteerUntil = Time.time + 0.6f;
    }

    private void UpdateStamina(bool moving)
    {
        bool draining = running && moving && !crouching && !crawling;
        if (draining)
        {
            stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * Time.fixedDeltaTime);
            staminaRegenTimer = staminaRegenDelay;
            if (stamina <= 0f) running = false;
            return;
        }

        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= Time.fixedDeltaTime;
            return;
        }

        stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * Time.fixedDeltaTime);
    }

    private void UpdateGrounded()
    {
        // stance-aware: sfera na dnu TRENUTNOG capsule-a (cucanj/puzanje menjaju collider)
        float r = capsule != null ? capsule.radius : CapsuleRadius;
        float bottomSphereLocalY = capsule != null
            ? capsule.center.y - capsule.height * 0.5f + r
            : -(CapsuleHeight * 0.5f - CapsuleRadius);
        Vector3 p = transform.position + Vector3.up * bottomSphereLocalY;
        int mask = ~(1 << gameObject.layer);
        grounded = Physics.CapsuleCast(p, p, r * 0.9f, Vector3.down, out _, groundCheckDistance, mask);
    }
}
