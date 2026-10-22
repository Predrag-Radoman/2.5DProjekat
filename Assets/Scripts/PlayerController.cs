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
    [SerializeField] private float groundFriction = 16f;
    [SerializeField] private float turnSlowdown = 0.6f;
    [SerializeField] private float riseGravityMult = 1.12f;
    [SerializeField] private float fallGravityMult = 1.75f;
    [SerializeField] private float hardLandSpeed = 4.5f;
    [SerializeField] private float landRecoveryTime = 0.12f;
    [SerializeField] private float landRecoverySpeedFactor = 0.55f;
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
    [SerializeField] private float jumpVelocity = 5.2f;
    [SerializeField] private float jumpCutFactor = 0.35f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float groundCheckDistance = 0.15f;

    [Header("Ledge grab (hvatanje ivice)")]
    [SerializeField] private bool ledgeGrabEnabled = true;
    [SerializeField] private float ledgeGrabReach = 0.35f;
    [SerializeField] private float ledgeGrabMinHeight = 0.65f;
    [SerializeField] private float ledgeGrabMaxHeight = 2.3f;
    [SerializeField] private float hangFeetOffset = 1.8f;
    [SerializeField] private float hangBodyDistance = 0.45f;
    [SerializeField] private float ledgeShimmySpeed = 1.2f;
    [SerializeField] private float ledgeJumpVelocity = 5.5f;
    [SerializeField] private float ledgeJumpPush = 6f;
    [SerializeField] private string kickOffObjectName = "Container";
    [SerializeField] private float mantleTime = 1.05f;
    [SerializeField] private float containerClimbTime = 1.4f;
    [SerializeField] private bool autoMantleLowLedges = false;
    [SerializeField] private float autoMantleMaxHeight = 1.5f;
    [SerializeField] private float quickMantleTime = 0.55f;
    [SerializeField] private bool autoMantleAfterJump = false;
    [SerializeField] private float autoMantleJumpWindow = 1.1f;
    [SerializeField] private float mantleInset = 0.12f;
    [SerializeField] private float mantleRecoveryTime = 0.3f;
    [SerializeField] private float ledgeRegrabDelay = 0.35f;

    [Header("Skok na ivicu (Space)")]
    [SerializeField] private bool jumpOntoEnabled = true;
    [SerializeField] private float jumpOntoMinHeight = 0.5f;
    [SerializeField] private float jumpOntoMaxHeight = 2.4f;
    [SerializeField] private float jumpOntoReach = 1f;
    [SerializeField] private float jumpOntoInset = 0.2f;
    [SerializeField] private float jumpOntoTime = 0.6f;
    [SerializeField] private float jumpOntoHop = 0.3f;
    [SerializeField] private float jumpOntoMinFacing = 0.5f;

    [Header("Sapletanje / pad")]
    [SerializeField] private bool tripEnabled = true;
    [SerializeField] private float tripMinObstacleHeight = 0.2f;
    [SerializeField] private float tripMaxObstacleHeight = 0.9f;
    [SerializeField] private float tripMinSpeed = 4f;
    [SerializeField] private float tripStuckTime = 0.25f;
    [SerializeField] private float tripFallTime = 0.35f;
    [SerializeField] private float tripLieTime = 0.6f;
    [SerializeField] private float tripGetUpTime = 0.55f;
    [SerializeField] private float tripCooldown = 0.7f;
    [SerializeField] private float tripHopHeight = 0.35f;
    [SerializeField] private float tripLandingMargin = 0.4f;
    [SerializeField] private float tripTravelSpeed = 3.2f;
    [SerializeField] private float tripClearance = 0.1f;

    [Header("Prekoracenje (vault)")]
    [SerializeField] private bool vaultEnabled = true;
    [SerializeField] private float vaultMinSpeed = 1.2f;
    [SerializeField] private float vaultMaxSpeed = 4.5f;
    [SerializeField] private float vaultPlantTime = 0.15f;
    [SerializeField] private float vaultOverTime = 0.5f;
    [SerializeField] private float vaultLandTime = 0.18f;
    [SerializeField] private float vaultHopHeight = 0.55f;
    [SerializeField] private float vaultLandingMargin = 0.35f;
    [SerializeField] private float vaultCooldown = 0.6f;
    [SerializeField] private float vaultTravelSpeed = 5f;
    [SerializeField] private float vaultClearance = 0.15f;
    [SerializeField] private float vaultMaxTravel = 2.6f;

    [Header("Prekoracenje malih prepreka (step-over)")]
    [SerializeField] private float stepOverMaxHeight = 0.65f;
    [SerializeField] private float stepOverPlantTime = 0.08f;
    [SerializeField] private float stepOverOverTime = 0.28f;
    [SerializeField] private float stepOverLandTime = 0.1f;
    [SerializeField] private float stepOverHopHeight = 0.12f;
    [SerializeField] private float stepOverLandingMargin = 0.25f;
    [SerializeField] private float stepOverTravelSpeed = 4.2f;

    public enum TripPhase { None, Stuck, Falling, Lying, GettingUp }
    public enum VaultPhase { None, Plant, Over, Land }

    /// <summary>Plan prelaska preko prepreke (pravac, cilj, udaljenosti do ivica).</summary>
    private struct TraversalPlan
    {
        public Vector3 Facing;
        public Vector3 Target;
        public float Entry;
        public float Exit;
        public float Travel;
    }

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
    public bool IsJumpOnto => jumpOnto;
    public float JumpOntoProgress => jumpOnto ? Mathf.Clamp01(jumpOntoTimer / Mathf.Max(0.001f, activeJumpOntoTime)) : 0f;
    public bool IsTripping => tripping;
    public TripPhase CurrentTripPhase => tripPhase;
    public float TripProgress => tripping
        ? Mathf.Clamp01((Time.time - tripStartTime) / Mathf.Max(0.001f, tripGetUpUntil - tripStartTime))
        : 0f;
    public float TripPhaseProgress
    {
        get
        {
            if (!tripping) return 0f;
            float now = Time.time;
            switch (tripPhase)
            {
                case TripPhase.Stuck:
                    return Mathf.Clamp01((now - tripStartTime) / Mathf.Max(0.001f, tripStuckTime));
                case TripPhase.Falling:
                    return Mathf.Clamp01((now - tripStuckUntil) / Mathf.Max(0.001f, activeTripFallTime));
                case TripPhase.Lying:
                    return Mathf.Clamp01((now - tripFallUntil) / Mathf.Max(0.001f, tripLieTime));
                case TripPhase.GettingUp:
                    return Mathf.Clamp01((now - tripLieUntil) / Mathf.Max(0.001f, tripGetUpTime));
                default:
                    return 0f;
            }
        }
    }
    public bool IsVaulting => vaulting;
    public bool IsSteppingOver => vaulting && vaultStepOver;
    public VaultPhase CurrentVaultPhase => vaultPhase;
    public float VaultProgress => vaulting
        ? Mathf.Clamp01((Time.time - vaultStartTime) / Mathf.Max(0.001f, vaultLandUntil - vaultStartTime))
        : 0f;
    public float VaultPhaseProgress
    {
        get
        {
            if (!vaulting) return 0f;
            float now = Time.time;
            switch (vaultPhase)
            {
                case VaultPhase.Plant:
                    return Mathf.Clamp01((now - vaultStartTime) / Mathf.Max(0.001f, activeVaultPlantTime));
                case VaultPhase.Over:
                    return Mathf.Clamp01((now - vaultPlantUntil) / Mathf.Max(0.001f, activeVaultOverTime));
                case VaultPhase.Land:
                    return Mathf.Clamp01((now - vaultOverUntil) / Mathf.Max(0.001f, activeVaultLandTime));
                default:
                    return 0f;
            }
        }
    }
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
    private float landRecoveryUntil;
    private float lastFallSpeed;
    private float ledgeKickStart;
    private float ledgeKickUntil;
    private float ledgeJumpFaceAt;
    private float ledgeJumpFaceDir;
    private float ledgeJumpSteerUntil;
    private bool jumpOnto;
    private float jumpOntoTimer;
    private float activeJumpOntoTime = 0.6f;
    private Vector3 jumpOntoStart;
    private Vector3 jumpOntoTarget;
    private CapsuleCollider capsule;
    private float standingHeight = 2f;
    private float standingRadius = 0.5f;
    private float feetLocalY = -1f;
    private float lastGroundedTime = -1f;
    private float jumpBufferedTime = -1f;
    private float lastJumpTime = -10f;
    private bool tripping;
    private TripPhase tripPhase;
    private float tripStartTime;
    private float tripStuckUntil;
    private float tripFallUntil;
    private float tripLieUntil;
    private float tripGetUpUntil;
    private float tripCooldownUntil;
    private Vector3 lastPlanarVelocity;
    private float lastPlanarSpeed;
    private Vector3 tripStartPos;
    private Vector3 tripTargetPos;
    private bool vaulting;
    private bool vaultStepOver;
    private VaultPhase vaultPhase;
    private float activeVaultPlantTime = 0.15f;
    private float activeVaultOverTime = 0.5f;
    private float activeVaultLandTime = 0.18f;
    private float activeVaultHopHeight = 0.55f;
    private float vaultStartTime;
    private float vaultPlantUntil;
    private float vaultOverUntil;
    private float vaultLandUntil;
    private float vaultCooldownUntil;
    private Vector3 vaultStartPos;
    private Vector3 vaultTargetPos;
    private Vector3 vaultDir;
    private float vaultExitSpeed;
    private float vaultRiseEnd;
    private float vaultHoldEnd;
    private float tripRiseEnd;
    private float tripHoldEnd;
    private float activeTripFallTime = 0.35f;
    private float activeTripHopHeight = 0.35f;

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
        if (tripping || jumpOnto) return;
        if (vaulting)
        {
            if (vaultPhase == VaultPhase.Plant)
                CancelVaultForJump();
            else
                jumpBufferedTime = Time.time; // skok se desi odmah posle vault-a
            return;
        }
        if (grounded && jumpOntoEnabled && TryBeginJumpOnto()) return;
        jumpBufferedTime = Time.time;
    }

    /// <summary>Variable jump height: pustis dugme u usponu -> skok se sece.</summary>
    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        if (tripping || vaulting) return;
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
        if (tripping || vaulting || jumpOnto || ledgeHanging || mantling) return;
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
        if (tripping || vaulting || jumpOnto) return;
        if (!running && stamina < staminaMinToStart) return;
        running = !running;
    }

    private void OnCrouchPerformed(InputAction.CallbackContext ctx)
    {
        if (tripping || vaulting || jumpOnto || ledgeHanging || mantling) return;
        crouchToggled = !crouchToggled;
        if (crouchToggled) crawlToggled = false;
    }

    private void OnCrawlPerformed(InputAction.CallbackContext ctx)
    {
        if (tripping || vaulting || jumpOnto || ledgeHanging || mantling) return;
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
        float h = tripping || vaulting ? crawlHeight : (crawling ? crawlHeight : (crouching ? crouchHeight : standingHeight));
        capsule.height = h;
        capsule.radius = tripping || vaulting || crawling ? crawlRadius : standingRadius;
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
        if (tripping)
        {
            UpdateTrip();
            return;
        }

        if (vaulting)
        {
            UpdateVault();
            return;
        }

        if (jumpOnto)
        {
            UpdateJumpOnto();
            return;
        }

        if (ledgeHanging)
        {
            UpdateLedgeState();
            return;
        }

        UpdateGrounded();
        if (grounded) lastGroundedTime = Time.time;
        if (grounded && !wasGrounded)
        {
            OnLanded?.Invoke();
            if (lastFallSpeed < -hardLandSpeed)
                landRecoveryUntil = Time.time + landRecoveryTime;
        }
        wasGrounded = grounded;
        UpdateStance();

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        if (Time.time < mantleRecoveryUntil) moveInput = Vector2.zero;

        if (ledgeGrabEnabled && !grounded && !crouching && !crawling && dashTimer <= 0f &&
            moveInput.y > -0.5f && Time.time >= grabCooldownUntil && rb.linearVelocity.y <= 0.1f &&
            TryFindLedge(out Vector3 ledgeTop, out Vector3 ledgeHitNormal))
        {
            float ledgeHeight = ledgeTop.y - (transform.position.y + feetLocalY);
            Vector3 planarVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            bool approachingWall = Vector3.Dot(planarVelocity, -ledgeHitNormal) > -0.5f;

            if (approachingWall)
            {
                bool jumpedRecently = autoMantleAfterJump &&
                                      Time.time - lastJumpTime <= autoMantleJumpWindow;
                bool lowLedge = autoMantleLowLedges && ledgeHeight <= autoMantleMaxHeight;

                if ((jumpedRecently || lowLedge) &&
                    TryBeginQuickMantle(ledgeTop, ledgeHitNormal))
                    return;

                BeginLedgeHang(ledgeTop, ledgeHitNormal);
                return;
            }
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
        if (Time.time < landRecoveryUntil) effSpeed *= landRecoverySpeedFactor;
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
            lastJumpTime = Time.time;
            OnJumped?.Invoke();
        }

        rb.linearVelocity = newVelocity;
        lastFallSpeed = newVelocity.y;
        lastPlanarVelocity = new Vector3(newVelocity.x, 0f, newVelocity.z);
        lastPlanarSpeed = lastPlanarVelocity.magnitude;

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

            Vector3 n = new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
            Vector3 hangSpot = new Vector3(wall.point.x, top.point.y, wall.point.z) +
                               n * hangBodyDistance;
            float hangFeetY = top.point.y - hangFeetOffset;
            if (Physics.Raycast(new Vector3(hangSpot.x, top.point.y - 0.1f, hangSpot.z), Vector3.down,
                                out RaycastHit below, hangFeetOffset + 0.6f, mask,
                                QueryTriggerInteraction.Ignore) &&
                below.point.y > hangFeetY + 0.05f)
                continue;

            topPoint = new Vector3(wall.point.x, top.point.y, wall.point.z);
            normal = n;
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
        Vector3 p = point + ledgeNormal * hangBodyDistance;
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

    private Vector3 MantleTargetFeet(Vector3 top, Vector3 normal)
    {
        return MantleTargetFeet(top, normal, mantleInset);
    }

    private Vector3 MantleTargetFeet(Vector3 top, Vector3 normal, float inset)
    {
        Vector3 feet = top - normal * inset;
        feet.y = top.y + 0.02f;
        return feet;
    }

    private Vector3 MantleTargetFeet()
    {
        return MantleTargetFeet(ledgePoint, ledgeNormal);
    }

    private bool CanMantleAt(Vector3 top, Vector3 normal)
    {
        return CanStandAt(MantleTargetFeet(top, normal));
    }

    private bool CanStandAt(Vector3 feet)
    {
        int mask = ~(1 << gameObject.layer);

        if (!Physics.Raycast(feet + Vector3.up * 0.8f, Vector3.down, out RaycastHit ground, 1.6f, mask, QueryTriggerInteraction.Ignore))
            return false;
        if (Vector3.Dot(ground.normal, Vector3.up) < 0.7f) return false;
        if (Mathf.Abs(ground.point.y - feet.y) > 0.25f) return false;

        Vector3 bottom = feet + Vector3.up * (standingRadius + 0.02f);
        Vector3 upper = feet + Vector3.up * (standingHeight - standingRadius - 0.02f);
        return !Physics.CheckCapsule(bottom, upper, standingRadius * 0.95f, mask, QueryTriggerInteraction.Ignore);
    }

    private bool CanMantle()
    {
        return CanMantleAt(ledgePoint, ledgeNormal);
    }

    private bool TryBeginQuickMantle(Vector3 top, Vector3 normal)
    {
        if (!CanMantleAt(top, normal)) return false;

        Vector3 entry = rb.position;
        ledgeHanging = true;
        mantling = true;
        ledgePoint = top;
        ledgeNormal = normal;
        ledgeShimmy = 0f;
        crouching = false;
        crawling = false;
        crouchToggled = false;
        crawlToggled = false;
        ledgeMantleArmed = true;
        ApplyCapsuleHeight();

        dashTimer = 0f;
        jumpBufferedTime = -1f;
        lastGroundedTime = -1f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.rotation = Quaternion.LookRotation(-normal, Vector3.up);

        float height = top.y - (transform.position.y + feetLocalY);
        mantleTimer = 0f;
        mantleStart = entry;
        mantleTarget = MantleTargetFeet(top, normal) - Vector3.up * feetLocalY;
        activeMantleTime = Mathf.Clamp(quickMantleTime + Mathf.Max(0f, height - 1f) * 0.18f,
                                       quickMantleTime, mantleTime);
        return true;
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
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.78f));
        float move = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.5f) / 0.5f));
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

    // --- Skok na ivicu (Space): lik skoci i doceka se na platformu ispred ---
    private bool TryBeginJumpOnto()
    {
        if (capsule == null) return false;
        if (crouching || crawling || dashTimer > 0f) return false;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();

        Vector2 mi = moveAction.ReadValue<Vector2>();
        Vector3 wish = Vector3.ClampMagnitude(new Vector3(mi.x, 0f, mi.y), 1f);
        wish.z *= depthSpeedFactor;
        bool hasInput = wish.sqrMagnitude > 0.04f;
        Vector3 bias = hasInput ? wish.normalized : forward;

        // smer inputa/pogleda prvo, pa blago u stranu (platforma ume da bude uz bok)
        float[] angles = { 0f, -35f, 35f, -70f, 70f, -90f, 90f };
        for (int a = 0; a < angles.Length; a++)
        {
            Vector3 dir = Quaternion.Euler(0f, angles[a], 0f) * bias;
            if (TryJumpOntoDirection(dir, wish, hasInput)) return true;
        }
        return false;
    }

    private bool TryJumpOntoDirection(Vector3 dir, Vector3 wish, bool hasInput)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return false;
        dir.Normalize();

        int mask = ~(1 << gameObject.layer);
        float feetY = transform.position.y + feetLocalY;
        float reach = standingRadius + jumpOntoReach;
        float[] heights = { 0.3f, 0.7f, 1.1f, 1.5f, 1.9f };

        for (int i = 0; i < heights.Length; i++)
        {
            Vector3 origin = new Vector3(transform.position.x, feetY + heights[i], transform.position.z);
            if (!Physics.Raycast(origin, dir, out RaycastHit wall, reach, mask, QueryTriggerInteraction.Ignore))
                continue;
            if (Mathf.Abs(Vector3.Dot(wall.normal, Vector3.up)) > 0.35f) continue;
            if (Vector3.Dot(wall.normal, dir) > -jumpOntoMinFacing) continue;

            Vector3 normal = new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
            Vector3 inward = -normal;
            Vector3 probe = wall.point + inward * 0.05f;

            float probeStartY = feetY + jumpOntoMaxHeight + 0.8f;
            if (!Physics.Raycast(new Vector3(probe.x, probeStartY, probe.z), Vector3.down,
                                 out RaycastHit top, jumpOntoMaxHeight + 1.4f, mask,
                                 QueryTriggerInteraction.Ignore))
                continue;
            if (Vector3.Dot(top.normal, Vector3.up) < 0.7f) continue;

            float delta = top.point.y - feetY;
            if (delta < jumpOntoMinHeight || delta > jumpOntoMaxHeight) continue;

            // ne skaci ako igrac drzi smer od ivice
            if (hasInput && Vector3.Dot(wish.normalized, inward) < -0.5f) continue;

            Vector3 landDeep = MantleTargetFeet(top.point, normal, standingRadius + jumpOntoInset);
            Vector3 landEdge = MantleTargetFeet(top.point, normal, standingRadius * 0.7f);
            Vector3 landFeet;
            if (CanStandAt(landDeep)) landFeet = landDeep;
            else if (CanStandAt(landEdge)) landFeet = landEdge;
            else continue;

            BeginJumpOnto(landFeet, inward, delta);
            return true;
        }
        return false;
    }

    private void BeginJumpOnto(Vector3 landFeet, Vector3 inward, float height)
    {
        jumpOnto = true;
        jumpOntoTimer = 0f;
        jumpOntoStart = rb.position;
        jumpOntoTarget = landFeet - Vector3.up * feetLocalY;

        float planar = PlanarDistance(jumpOntoStart, jumpOntoTarget);
        activeJumpOntoTime = Mathf.Clamp(jumpOntoTime + height * 0.1f + planar * 0.12f, 0.45f, 1f);

        crouchToggled = false;
        crawlToggled = false;
        crouching = false;
        crawling = false;
        dashTimer = 0f;
        jumpBufferedTime = -1f;
        ledgeHanging = false;
        mantling = false;
        lastJumpTime = Time.time;
        lastGroundedTime = Time.time;
        ApplyCapsuleHeight();

        rb.rotation = Quaternion.LookRotation(inward, Vector3.up);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;
        OnJumped?.Invoke();
    }

    private void UpdateJumpOnto()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        jumpOntoTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(jumpOntoTimer / Mathf.Max(0.01f, activeJumpOntoTime));
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.62f));
        float forward = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.12f) / 0.88f));

        Vector3 pos = new Vector3(
            Mathf.Lerp(jumpOntoStart.x, jumpOntoTarget.x, forward),
            Mathf.Lerp(jumpOntoStart.y, jumpOntoTarget.y, rise) +
                jumpOntoHop * Mathf.Sin(Mathf.PI * t),
            Mathf.Lerp(jumpOntoStart.z, jumpOntoTarget.z, forward));
        rb.MovePosition(pos);

        if (t >= 1f) FinishJumpOnto();
    }

    private void FinishJumpOnto()
    {
        jumpOnto = false;
        ApplyCapsuleHeight();
        rb.position = jumpOntoTarget;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        lastGroundedTime = Time.time;
        grabCooldownUntil = Time.time + 0.15f;
        OnLanded?.Invoke();
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
        lastJumpTime = Time.time;
        grabCooldownUntil = Time.time + ledgeRegrabDelay;
        jumpCutLockUntil = Time.time + 0.2f;
        ledgeKickStart = Time.time;
        ledgeKickUntil = Time.time + 0.22f;
        ledgeJumpFaceDir = dir;
        ledgeJumpFaceAt = Time.time + 0.22f;
        ledgeJumpSteerUntil = Time.time + 0.6f;
    }

    // Plan prelaska preko prepreke: pravac, cilj i udaljenosti do blize/dalje ivice.
    // Stari obrazac "centar prepreke + projekcija extenta" je za duboku zidnu
    // prepreku davao ogroman cilj pri dijagonalnom prilazu -> lik je "poletao" napred.
    private TraversalPlan PlanTraversal(Vector3 awayFromObstacle, Bounds obstacleBounds, float landingMargin)
    {
        TraversalPlan plan = new TraversalPlan();

        Vector3 facing = -awayFromObstacle;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.001f) facing = transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.001f) facing = Vector3.forward;
        facing.Normalize();
        plan.Facing = facing;

        float nearDist;
        float farDist;
        if (TryGetTraversalSpan(facing, obstacleBounds, out nearDist, out farDist))
        {
            plan.Entry = nearDist;
            plan.Exit = farDist;
        }
        else
        {
            float forwardExtent = Mathf.Abs(facing.x) * obstacleBounds.extents.x +
                                  Mathf.Abs(facing.z) * obstacleBounds.extents.z;
            float toCenter = Vector3.Dot(obstacleBounds.center - transform.position, facing);
            plan.Entry = Mathf.Max(0f, toCenter - forwardExtent);
            plan.Exit = toCenter + forwardExtent;
        }

        float clearDistance = Mathf.Max(0.9f, plan.Exit + standingRadius + landingMargin);
        plan.Target = transform.position + facing * clearDistance;
        plan.Target.y = transform.position.y;
        plan.Travel = PlanarDistance(transform.position, plan.Target);
        return plan;
    }

    // Udaljenosti do blize i dalje ivice prepreke duz pravca kretanja (ray vs AABB u XZ).
    private bool TryGetTraversalSpan(Vector3 facing, Bounds bounds, out float nearDist, out float farDist)
    {
        Vector3 origin = transform.position;
        float tMin = 0f;
        float tMax = float.MaxValue;

        for (int axis = 0; axis < 2; axis++)
        {
            float o = axis == 0 ? origin.x : origin.z;
            float d = axis == 0 ? facing.x : facing.z;
            float min = axis == 0 ? bounds.min.x : bounds.min.z;
            float max = axis == 0 ? bounds.max.x : bounds.max.z;

            if (Mathf.Abs(d) < 0.0001f)
            {
                if (o < min || o > max)
                {
                    nearDist = 0f;
                    farDist = -1f;
                    return false;
                }
                continue;
            }

            float inv = 1f / d;
            float t1 = (min - o) * inv;
            float t2 = (max - o) * inv;
            if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
        }

        if (tMax < tMin || tMax < 0f)
        {
            nearDist = 0f;
            farDist = -1f;
            return false;
        }

        nearDist = Mathf.Max(0f, tMin);
        farDist = tMax;
        return true;
    }

    // Pravac "od prepreke" iz kontakt normale umesto iz centra prepreke.
    // Centar prepreke je daleko od ivice (npr. 4.8 m duboka zidna prepreka),
    // pa je dijagonalni prilaz davao ogroman cilj -> lik je "poletao" napred.
    private Vector3 CollisionAwayDirection(Collision collision, Bounds bounds)
    {
        if (collision != null && collision.contactCount > 0)
        {
            Vector3 n = collision.GetContact(0).normal;
            n.y = 0f;
            if (n.sqrMagnitude > 0.0001f) return n.normalized;
        }

        Vector3 fallback = transform.position - bounds.center;
        fallback.y = 0f;
        if (fallback.sqrMagnitude > 0.0001f) return fallback.normalized;
        return Vector3.zero;
    }

    private void BeginTrip(TraversalPlan plan, float obstacleHeight)
    {
        float now = Time.time;
        tripping = true;
        tripPhase = TripPhase.Stuck;
        tripStartTime = now;
        tripStuckUntil = now + tripStuckTime;

        running = false;
        crouchToggled = false;
        crawlToggled = false;
        crouching = false;
        crawling = false;
        dashTimer = 0f;
        jumpBufferedTime = -1f;
        ledgeHanging = false;
        mantling = false;
        ApplyCapsuleHeight();

        Vector3 facing = plan.Facing;

        tripStartPos = rb.position;
        tripTargetPos = plan.Target;
        tripTargetPos.y = tripStartPos.y;

        float travel = PlanarDistance(tripStartPos, tripTargetPos);
        activeTripFallTime = Mathf.Max(tripFallTime, 1.5f * travel / Mathf.Max(0.5f, tripTravelSpeed));
        tripFallUntil = tripStuckUntil + activeTripFallTime;
        tripLieUntil = tripFallUntil + tripLieTime;
        tripGetUpUntil = tripLieUntil + tripGetUpTime;

        // Pad preko prepreke: lik se podigne samo toliko da prede prepreku
        // (bez velikog luka -> bez "poletanja") i ne prolazi kroz nju.
        activeTripHopHeight = Mathf.Max(tripHopHeight, obstacleHeight + tripClearance);
        ComputeHeightWindows(travel, tripStuckUntil, activeTripFallTime,
                             plan.Entry - crawlRadius, plan.Exit + crawlRadius,
                             out tripRiseEnd, out tripHoldEnd);

        rb.rotation = Quaternion.LookRotation(facing, Vector3.up);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Prozori visine: podizanje do blize ivice, drzanje iznad prepreke, spust posle daleje.
    // "entryDist"/"exitDist" su udaljenosti po putanji gde kapsula dodirne, odnosno
    // prede prepreku -> profil se ne moze racunati od centra prepreke.
    private static void ComputeHeightWindows(float travel, float motionStart, float moveTime,
                                             float entryDist, float exitDist,
                                             out float riseEnd, out float holdEnd)
    {
        float e = Mathf.Clamp(entryDist, 0.02f, travel * 0.6f);
        float x = Mathf.Clamp(exitDist, e + 0.02f, travel);
        riseEnd = motionStart + InverseSmoothStep(e / Mathf.Max(0.01f, travel)) * moveTime;
        holdEnd = motionStart + InverseSmoothStep(x / Mathf.Max(0.01f, travel)) * moveTime;
    }

    // Visina tela po profilu: podizanje -> drzanje -> spust.
    private static float ProfileHeight(float now, float start, float riseEnd, float holdEnd, float end, float height)
    {
        if (height <= 0f) return 0f;

        if (now <= riseEnd)
            return height * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((now - start) / Mathf.Max(0.001f, riseEnd - start)));

        if (now <= holdEnd) return height;

        return height * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((now - holdEnd) / Mathf.Max(0.001f, end - holdEnd))));
    }


    private void UpdateTrip()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float now = Time.time;
        if (now < tripStuckUntil)
        {
            tripPhase = TripPhase.Stuck;
            rb.MovePosition(tripStartPos);
        }
        else if (now < tripFallUntil)
        {
            tripPhase = TripPhase.Falling;
            float t = Mathf.Clamp01((now - tripStuckUntil) / Mathf.Max(0.001f, activeTripFallTime));
            float forward = Mathf.SmoothStep(0f, 1f, t);
            Vector3 pos = Vector3.Lerp(tripStartPos, tripTargetPos, forward);
            pos.y += ProfileHeight(now, tripStuckUntil, tripRiseEnd, tripHoldEnd, tripFallUntil, activeTripHopHeight);
            rb.MovePosition(pos);
        }
        else if (now < tripLieUntil)
        {
            tripPhase = TripPhase.Lying;
            rb.MovePosition(tripTargetPos);
        }
        else if (now < tripGetUpUntil)
        {
            tripPhase = TripPhase.GettingUp;
            rb.MovePosition(tripTargetPos);
        }
        else FinishTrip();
    }

    private void FinishTrip()
    {
        tripping = false;
        tripPhase = TripPhase.None;
        tripCooldownUntil = Time.time + tripCooldown;
        grabCooldownUntil = Time.time + ledgeRegrabDelay;
        jumpBufferedTime = -1f;
        lastGroundedTime = Time.time;
        ApplyCapsuleHeight();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTrip(collision);
        TryVault(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryTrip(collision);
        TryVault(collision);
    }

    private void TryTrip(Collision collision)
    {
        if (!tripEnabled || tripping || vaulting || ledgeHanging || mantling) return;
        if (!running || !grounded) return;
        if (Time.time < tripCooldownUntil) return;
        if (lastPlanarSpeed < tripMinSpeed) return;

        Collider other = collision.collider;
        if (other == null) return;

        Bounds b = other.bounds;
        float feetY = transform.position.y + feetLocalY;
        float height = b.max.y - feetY;
        if (height < tripMinObstacleHeight || height > tripMaxObstacleHeight) return;

        Vector3 away = CollisionAwayDirection(collision, b);
        if (away.sqrMagnitude < 0.0001f) return;
        if (Vector3.Dot(away, lastPlanarVelocity.normalized) > -0.25f) return;

        TraversalPlan plan = PlanTraversal(away, b, tripLandingMargin);
        BeginTrip(plan, height);
    }

    private void TryVault(Collision collision)
    {
        if (!vaultEnabled || vaulting || tripping || ledgeHanging || mantling) return;
        if (running || crouching || crawling) return;
        if (!grounded || dashTimer > 0f) return;
        if (Time.time < vaultCooldownUntil) return;
        if (Time.time - jumpBufferedTime <= jumpBufferTime) return;
        if (lastPlanarSpeed < vaultMinSpeed || lastPlanarSpeed > vaultMaxSpeed) return;

        Collider other = collision.collider;
        if (other == null) return;

        Bounds b = other.bounds;
        float feetY = transform.position.y + feetLocalY;
        float height = b.max.y - feetY;
        if (height < tripMinObstacleHeight || height > tripMaxObstacleHeight) return;

        Vector3 away = CollisionAwayDirection(collision, b);
        if (away.sqrMagnitude < 0.0001f) return;
        if (Vector3.Dot(away, lastPlanarVelocity.normalized) > -0.25f) return;

        bool stepOver = height <= stepOverMaxHeight;
        float landingMargin = stepOver ? stepOverLandingMargin : vaultLandingMargin;
        TraversalPlan plan = PlanTraversal(away, b, landingMargin);
        if (plan.Travel > vaultMaxTravel) return;
        if (!VaultTargetIsClear(plan.Target)) return;

        BeginVault(plan, stepOver, height);
    }

    private bool VaultTargetIsClear(Vector3 target)
    {
        int mask = ~(1 << gameObject.layer);
        Vector3 feet = new Vector3(target.x, transform.position.y + feetLocalY, target.z);
        if (!Physics.Raycast(feet + Vector3.up * 0.8f, Vector3.down, out RaycastHit ground,
                1.6f, mask, QueryTriggerInteraction.Ignore))
            return false;
        if (Vector3.Dot(ground.normal, Vector3.up) < 0.7f) return false;

        Vector3 bottom = feet + Vector3.up * (standingRadius + 0.02f);
        Vector3 top = feet + Vector3.up * (standingHeight - standingRadius - 0.02f);
        return !Physics.CheckCapsule(bottom, top, standingRadius * 0.95f, mask, QueryTriggerInteraction.Ignore);
    }

    private void BeginVault(TraversalPlan plan, bool stepOver, float obstacleHeight)
    {
        float now = Time.time;
        vaulting = true;
        vaultStepOver = stepOver;
        activeVaultPlantTime = stepOver ? stepOverPlantTime : vaultPlantTime;
        activeVaultLandTime = stepOver ? stepOverLandTime : vaultLandTime;

        crouchToggled = false;
        crawlToggled = false;
        crouching = false;
        crawling = false;
        dashTimer = 0f;
        jumpBufferedTime = -1f;
        ledgeHanging = false;
        mantling = false;
        ApplyCapsuleHeight();

        Vector3 facing = plan.Facing;

        vaultStartPos = rb.position;
        vaultTargetPos = plan.Target;
        vaultTargetPos.y = vaultStartPos.y;

        float travel = PlanarDistance(vaultStartPos, vaultTargetPos);
        float speedLimit = stepOver ? stepOverTravelSpeed : vaultTravelSpeed;
        float baseOverTime = stepOver ? stepOverOverTime : vaultOverTime;
        activeVaultOverTime = Mathf.Max(baseOverTime, 1.5f * travel / Mathf.Max(0.5f, speedLimit));
        activeVaultHopHeight = Mathf.Max(stepOver ? stepOverHopHeight : vaultHopHeight,
                                        obstacleHeight + vaultClearance);

        vaultPhase = VaultPhase.Plant;
        vaultStartTime = now;
        vaultPlantUntil = now + activeVaultPlantTime;
        vaultOverUntil = vaultPlantUntil + activeVaultOverTime;
        vaultLandUntil = vaultOverUntil + activeVaultLandTime;

        // Profil visine: podizanje pre blize ivice, drzanje iznad prepreke, spust posle daleje ivice.
        // Sinusni luk je davao "poletanje" (vrh visoko iznad prepreke) ili klizanje kroz prepreku.
        ComputeHeightWindows(travel, vaultPlantUntil, activeVaultOverTime,
                             plan.Entry - crawlRadius, plan.Exit + crawlRadius,
                             out vaultRiseEnd, out vaultHoldEnd);

        vaultDir = travel > 0.01f ? vaultTargetPos - vaultStartPos : facing;
        vaultDir.y = 0f;
        if (vaultDir.sqrMagnitude < 0.0001f) vaultDir = facing;
        vaultDir.Normalize();

        vaultExitSpeed = Mathf.Clamp(lastPlanarSpeed, 0f, moveSpeed) * 0.6f;

        rb.rotation = Quaternion.LookRotation(facing, Vector3.up);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Obrnuto od Mathf.SmoothStep: daje t (0..1) za zadatu vrednost smoothstep-a.
    private static float InverseSmoothStep(float y)
    {
        y = Mathf.Clamp01(y);
        return 0.5f - Mathf.Sin(Mathf.Asin(1f - 2f * y) / 3f);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    private void UpdateVault()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float now = Time.time;
        if (now < vaultPlantUntil)
        {
            vaultPhase = VaultPhase.Plant;
            Vector3 plant = vaultStartPos;
            plant.y += ProfileHeight(now, vaultStartTime, vaultRiseEnd, vaultHoldEnd, vaultLandUntil, activeVaultHopHeight);
            rb.MovePosition(plant);
        }
        else if (now < vaultOverUntil)
        {
            vaultPhase = VaultPhase.Over;
            float t = Mathf.Clamp01((now - vaultPlantUntil) / Mathf.Max(0.001f, activeVaultOverTime));
            float forward = Mathf.SmoothStep(0f, 1f, t);
            Vector3 pos = Vector3.Lerp(vaultStartPos, vaultTargetPos, forward);
            pos.y += ProfileHeight(now, vaultStartTime, vaultRiseEnd, vaultHoldEnd, vaultLandUntil, activeVaultHopHeight);
            rb.MovePosition(pos);
        }
        else if (now < vaultLandUntil)
        {
            vaultPhase = VaultPhase.Land;
            Vector3 pos = vaultTargetPos;
            pos.y += ProfileHeight(now, vaultStartTime, vaultRiseEnd, vaultHoldEnd, vaultLandUntil, activeVaultHopHeight);
            rb.MovePosition(pos);
        }
        else FinishVault();
    }

    private void CancelVaultForJump()
    {
        vaulting = false;
        vaultStepOver = false;
        vaultPhase = VaultPhase.None;
        vaultCooldownUntil = Time.time + 0.2f;
        lastGroundedTime = Time.time;
        ApplyCapsuleHeight();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.position = vaultStartPos; // skok kreće sa zemlje, ne sa visine "podizanja"
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        jumpBufferedTime = Time.time;
    }

    private void FinishVault()
    {
        vaulting = false;
        vaultPhase = VaultPhase.None;
        vaultCooldownUntil = Time.time + vaultCooldown;
        lastGroundedTime = Time.time;
        ApplyCapsuleHeight();
        rb.isKinematic = false;
        rb.useGravity = true;
        // momentum se nosi dalje u pravcu vault-a (ne stop, ali ni "poletanje")
        rb.linearVelocity = vaultDir * vaultExitSpeed;
        rb.angularVelocity = Vector3.zero;
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
