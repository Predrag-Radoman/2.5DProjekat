using UnityEngine;

/// <summary>
/// Code-driven animacije placeholder rig-a (idle bob, walk cycle, run,
/// jump tuck, land squash) + footstep eventi na promenama faze.
/// Cita stanje iz PlayerController-a; menja se pravim Animator rig-om
/// kad FBX stigne (vidi docs/ANIMACIJE.md).
/// Zahteva PlaceholderRig pod istim objektom (Tools > Build Placeholder Rig).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class ProceduralPlayerAnimator : MonoBehaviour
{
    [Header("Walk cycle")]
    [SerializeField] private float walkFrequency = 1.6f;
    [SerializeField] private float legSwingAmp = 38f;
    [SerializeField] private float armSwingRatio = 0.7f;
    [SerializeField] private float bodyBobAmp = 0.06f;
    [SerializeField] private float forwardLeanAmp = 12f;

    [Header("Cucanj / puzanje")]
    [SerializeField] private float crouchDropAmp = 0.45f;
    [SerializeField] private float crouchLegBend = 35f;
    [SerializeField] private float crouchLeanAmp = 28f;
    [SerializeField] private float crouchArmHold = 20f;
    [SerializeField] private float crouchSwingScale = 0.5f;

    [Header("Puzanje (prone crawl)")]
    [SerializeField] private float crawlBodyPitch = 78f;
    [SerializeField] private float crawlHipsDrop = 0.62f;
    [SerializeField] private float crawlArmReach = 125f;
    [SerializeField] private float crawlLegExtend = 8f;
    [SerializeField] private float crawlSwingAmp = 22f;
    [SerializeField] private float crawlFrequency = 1.8f;
    [SerializeField] private float crawlHeadLift = 40f;

    [Header("Air / land")]
    [SerializeField] private float jumpTuckAngle = 32f;
    [SerializeField] private float armRaiseAngle = 42f;
    [SerializeField] private float landSquashTime = 0.16f;
    [SerializeField] private float landSquashAmp = 0.22f;
    [SerializeField] private float landCrouchAmp = 0.14f;

    [Header("Ledge grab")]
    [SerializeField] private float hangArmRaise = 160f;
    [SerializeField] private float hangLegBend = 12f;
    [SerializeField] private float hangBodyLean = 8f;
    [SerializeField] private float hangShimmySway = 10f;
    [SerializeField] private float mantleLegTuck = 45f;

    [Header("Sapletanje / pad")]
    [SerializeField] private float tripStuckPitch = 18f;
    [SerializeField] private float tripStuckDrop = 0.12f;
    [SerializeField] private float tripStuckArm = 45f;
    [SerializeField] private float tripStuckLegBend = 22f;
    [SerializeField] private float tripStuckLean = 10f;
    [SerializeField] private float tripPitch = 88f;
    [SerializeField] private float tripHipsDrop = 0.68f;
    [SerializeField] private float tripArmReach = 100f;

    [Header("Prekoracenje (vault)")]
    [SerializeField] private float vaultPlantLean = 26f;
    [SerializeField] private float vaultPlantArm = 65f;
    [SerializeField] private float vaultPlantLeg = 20f;
    [SerializeField] private float vaultOverPitch = 52f;
    [SerializeField] private float vaultOverDrop = 0.22f;
    [SerializeField] private float vaultOverArm = 85f;
    [SerializeField] private float vaultLegTuck = 55f;
    [SerializeField] private float vaultLandCrouch = 0.16f;

    [Header("Prekoracenje malih prepreka (step-over)")]
    [SerializeField] private float stepOverLean = 8f;
    [SerializeField] private float stepOverPitch = 5f;
    [SerializeField] private float stepOverDrop = 0.04f;
    [SerializeField] private float stepOverArm = 16f;
    [SerializeField] private float stepOverLeadLeg = 34f;
    [SerializeField] private float stepOverTrailLeg = 10f;
    [SerializeField] private float stepOverCrouch = 0.03f;

    private PlayerController controller;
    private ProceduralAudio audio;

    private Transform hips, body, head, legL, legR, armL, armR;
    private Vector3 baseHipsPos;

    private float walkPhase;
    private float crawlPhase;
    private float prevSinSign;
    private bool nextFootstepLeft = true;
    private float landTimer;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        audio = GetComponent<ProceduralAudio>();
        if (audio == null) audio = gameObject.AddComponent<ProceduralAudio>();

        Transform rig = transform.Find("PlaceholderRig");
        if (rig == null) rig = BuildRig(gameObject);
        SanitizeRig(rig, gameObject.layer);

        hips = rig.Find("Hips");
        body = hips != null ? hips.Find("Body") : null;
        head = hips != null ? hips.Find("Head") : null;
        legL = hips != null ? hips.Find("LegL") : null;
        legR = hips != null ? hips.Find("LegR") : null;
        armL = hips != null ? hips.Find("ArmL") : null;
        armR = hips != null ? hips.Find("ArmR") : null;
        if (hips != null) baseHipsPos = hips.localPosition;

        controller.OnLanded += OnLanded;
    }

    private void Update()
    {
        if (hips == null) return;

        if (controller.IsTripping)
        {
            UpdateTripPose();
            ApplySquash(0f);
            return;
        }

        if (controller.IsVaulting)
        {
            if (controller.IsSteppingOver)
                UpdateStepOverPose();
            else
                UpdateVaultPose();
            ApplySquash(0f);
            return;
        }

        if (controller.IsLedgeHanging || controller.IsMantling)
        {
            UpdateLedgePose();
            ApplySquash(0f);
            return;
        }

        if (controller.IsLedgeKicking)
        {
            UpdateLedgeKickPose();
            ApplySquash(0f);
            return;
        }

        float speed = controller.NormalizedHorizontalSpeed;
        bool grounded = controller.IsGrounded;
        float vvel = controller.VerticalVelocity;

        if (grounded)
        {
            if (controller.IsCrawling)
            {
                UpdateCrawlPose(speed);
            }
            else
            {
                bool crouch = controller.IsCrouching;
                float swingScale = crouch ? crouchSwingScale : 1f;
                walkPhase += speed * walkFrequency * Mathf.PI * 2f * Time.deltaTime;
                float sin = Mathf.Sin(walkPhase);
                float swing = sin * legSwingAmp * speed * swingScale;
                float bend = crouch ? crouchLegBend : 0f;
                float armHold = crouch ? crouchArmHold : 0f;

                if (legL != null) legL.localRotation = Quaternion.Euler(bend + swing, 0f, 0f);
                if (legR != null) legR.localRotation = Quaternion.Euler(bend - swing, 0f, 0f);
                if (armL != null) armL.localRotation = Quaternion.Euler(armHold - swing * armSwingRatio, 0f, 0f);
                if (armR != null) armR.localRotation = Quaternion.Euler(armHold + swing * armSwingRatio, 0f, 0f);

                float bob = Mathf.Abs(sin) * bodyBobAmp * speed * swingScale;
                float lean = crouch ? crouchLeanAmp : speed * forwardLeanAmp;
                float drop = crouch ? crouchDropAmp : 0f;
                hips.localRotation = Quaternion.identity;
                hips.localPosition = baseHipsPos + Vector3.up * (bob - drop) - CrouchOffset();
                if (body != null) body.localRotation = Quaternion.Euler(lean, 0f, 0f);
                if (head != null) head.localRotation = Quaternion.Euler(-lean * 0.6f, 0f, 0f);

                FootstepCheck(sin, speed, 0.15f);
            }

            ApplySquash(SquashCurve());
        }
        else
        {
            float tuck = vvel > 0f ? jumpTuckAngle : jumpTuckAngle * 0.35f;
            float raise = vvel > 0f ? -armRaiseAngle : -armRaiseAngle * 0.3f;
            if (legL != null) legL.localRotation = Quaternion.Euler(tuck, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(tuck * 0.6f, 0f, 0f);
            if (armL != null) armL.localRotation = Quaternion.Euler(raise, 0f, 0f);
            if (armR != null) armR.localRotation = Quaternion.Euler(raise, 0f, 0f);

            hips.localRotation = Quaternion.identity;
            hips.localPosition = baseHipsPos - CrouchOffset();
            if (body != null) body.localRotation = Quaternion.Euler(forwardLeanAmp * 0.4f, 0f, 0f);
            if (head != null) head.localRotation = Quaternion.Euler(-forwardLeanAmp * 0.25f, 0f, 0f);

            ApplySquash(SquashCurve());
        }
    }

    /// <summary>Visenje o ivicu + shimmy + mantle (penjanje).</summary>
    private void UpdateLedgePose()
    {
        float progress = controller.IsMantling ? controller.MantleProgress : 0f;
        float shimmy = controller.IsMantling ? 0f : controller.LedgeShimmy;
        float sway = Mathf.Sin(Time.time * 9f) * hangShimmySway * Mathf.Abs(shimmy);

        float arm = Mathf.Lerp(-hangArmRaise, 8f, progress);
        float legBend = Mathf.Lerp(hangLegBend, 0f, progress) +
                        mantleLegTuck * Mathf.Sin(progress * Mathf.PI);

        if (armL != null) armL.localRotation = Quaternion.Euler(arm + sway, 0f, 0f);
        if (armR != null) armR.localRotation = Quaternion.Euler(arm - sway, 0f, 0f);
        if (legL != null) legL.localRotation = Quaternion.Euler(legBend + sway * 0.4f, 0f, 0f);
        if (legR != null) legR.localRotation = Quaternion.Euler(legBend - sway * 0.4f, 0f, 0f);

        hips.localRotation = Quaternion.identity;
        hips.localPosition = baseHipsPos + Vector3.up * (Mathf.Abs(shimmy) * 0.01f);
        if (body != null) body.localRotation = Quaternion.Euler(hangBodyLean * (1f - progress), 0f, 0f);
        if (head != null) head.localRotation = Quaternion.Euler(-25f * (1f - progress), 0f, 0f);
    }

    /// <summary>Odgur od zida nogama posle skoka sa ivice.</summary>
    private void UpdateLedgeKickPose()
    {
        float t = Mathf.Clamp01(controller.LedgeKickProgress);
        float kick = Mathf.Sin(t * Mathf.PI);

        if (legL != null) legL.localRotation = Quaternion.Euler(78f * kick, 0f, 0f);
        if (legR != null) legR.localRotation = Quaternion.Euler(64f * kick, 0f, 0f);
        if (armL != null) armL.localRotation = Quaternion.Euler(28f * kick, 0f, 0f);
        if (armR != null) armR.localRotation = Quaternion.Euler(28f * kick, 0f, 0f);

        hips.localRotation = Quaternion.identity;
        hips.localPosition = baseHipsPos;
        if (body != null) body.localRotation = Quaternion.Euler(-12f * kick, 0f, 0f);
        if (head != null) head.localRotation = Quaternion.Euler(8f * kick, 0f, 0f);
    }

    private void UpdateTripPose()
    {
        float t = Mathf.Clamp01(controller.TripPhaseProgress);
        float pitch = 0f;
        float drop = 0f;
        float arm = 0f;
        float leg = 0f;
        float lean = 0f;
        float headPitch = 0f;

        switch (controller.CurrentTripPhase)
        {
            case PlayerController.TripPhase.Stuck:
                pitch = tripStuckPitch;
                drop = tripStuckDrop;
                arm = -tripStuckArm;
                leg = tripStuckLegBend;
                lean = tripStuckLean;
                headPitch = -8f;
                break;

            case PlayerController.TripPhase.Falling:
                float fall = Mathf.SmoothStep(0f, 1f, t);
                pitch = Mathf.Lerp(tripStuckPitch, tripPitch, fall);
                drop = Mathf.Lerp(tripStuckDrop, tripHipsDrop, fall);
                arm = Mathf.Lerp(-tripStuckArm, -tripArmReach, fall);
                leg = Mathf.Lerp(tripStuckLegBend, 0f, fall);
                lean = Mathf.Lerp(tripStuckLean, 0f, fall);
                headPitch = -8f;
                break;

            case PlayerController.TripPhase.Lying:
                pitch = tripPitch;
                drop = tripHipsDrop + Mathf.Sin(Time.time * 3f) * 0.005f;
                arm = -tripArmReach;
                headPitch = -12f;
                break;

            case PlayerController.TripPhase.GettingUp:
                float rise = Mathf.SmoothStep(0f, 1f, t);
                pitch = Mathf.Lerp(tripPitch, 0f, rise);
                drop = Mathf.Lerp(tripHipsDrop, 0f, rise);
                arm = Mathf.Lerp(-tripArmReach, 0f, rise);
                headPitch = Mathf.Lerp(-12f, 0f, rise);
                break;

            default:
                return;
        }

        hips.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        hips.localPosition = baseHipsPos - Vector3.up * drop;
        if (body != null) body.localRotation = Quaternion.Euler(lean, 0f, 0f);
        if (head != null) head.localRotation = Quaternion.Euler(headPitch, 0f, 0f);
        if (legL != null) legL.localRotation = Quaternion.Euler(leg, 0f, 0f);
        if (legR != null) legR.localRotation = Quaternion.Euler(leg + 4f, 0f, 0f);
        if (armL != null) armL.localRotation = Quaternion.Euler(arm, 0f, 0f);
        if (armR != null) armR.localRotation = Quaternion.Euler(arm, 0f, 0f);

        if (controller.CurrentTripPhase == PlayerController.TripPhase.Stuck)
        {
            if (legL != null) legL.localRotation = Quaternion.Euler(leg + 14f, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(leg - 8f, 0f, 0f);
            if (armL != null) armL.localRotation = Quaternion.Euler(arm - 12f, 0f, 0f);
            if (armR != null) armR.localRotation = Quaternion.Euler(arm + 18f, 0f, 0f);
        }
    }

    private void UpdateStepOverPose()
    {
        float t = Mathf.Clamp01(controller.VaultPhaseProgress);
        float lean = 0f;
        float pitch = 0f;
        float drop = 0f;
        float arm = 0f;
        float leadLeg = 0f;
        float trailLeg = 0f;
        float headPitch = 0f;

        switch (controller.CurrentVaultPhase)
        {
            case PlayerController.VaultPhase.Plant:
                lean = 3f;
                pitch = 2f;
                drop = 0.02f;
                arm = -8f;
                leadLeg = 12f;
                trailLeg = 2f;
                headPitch = -2f;
                break;

            case PlayerController.VaultPhase.Over:
                float over = Mathf.SmoothStep(0f, 1f, t);
                lean = Mathf.Lerp(3f, stepOverLean, over);
                pitch = Mathf.Lerp(2f, stepOverPitch, over);
                drop = Mathf.Lerp(0.02f, stepOverDrop, over);
                arm = Mathf.Lerp(-8f, -stepOverArm, over);
                leadLeg = Mathf.Lerp(12f, stepOverLeadLeg, over);
                trailLeg = Mathf.Lerp(2f, stepOverTrailLeg, over);
                headPitch = -2f;
                break;

            case PlayerController.VaultPhase.Land:
                float land = Mathf.SmoothStep(0f, 1f, t);
                lean = Mathf.Lerp(stepOverLean, 0f, land);
                pitch = Mathf.Lerp(stepOverPitch, 0f, land);
                drop = Mathf.Lerp(stepOverDrop, stepOverCrouch, land);
                arm = Mathf.Lerp(-stepOverArm, 0f, land);
                leadLeg = Mathf.Lerp(stepOverLeadLeg, 4f, land);
                trailLeg = Mathf.Lerp(stepOverTrailLeg, 0f, land);
                headPitch = Mathf.Lerp(-2f, 0f, land);
                break;

            default:
                return;
        }

        hips.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        hips.localPosition = baseHipsPos - Vector3.up * drop;
        if (body != null) body.localRotation = Quaternion.Euler(lean, 0f, 0f);
        if (head != null) head.localRotation = Quaternion.Euler(headPitch, 0f, 0f);
        if (legL != null) legL.localRotation = Quaternion.Euler(leadLeg, 0f, 0f);
        if (legR != null) legR.localRotation = Quaternion.Euler(trailLeg, 0f, 0f);
        if (armL != null) armL.localRotation = Quaternion.Euler(arm - 4f, 0f, 0f);
        if (armR != null) armR.localRotation = Quaternion.Euler(arm + 5f, 0f, 0f);
    }

    private void UpdateVaultPose()
    {
        float t = Mathf.Clamp01(controller.VaultPhaseProgress);
        float pitch = 0f;
        float drop = 0f;
        float arm = 0f;
        float leg = 0f;
        float lean = 0f;
        float headPitch = 0f;

        switch (controller.CurrentVaultPhase)
        {
            case PlayerController.VaultPhase.Plant:
                pitch = 8f;
                drop = 0.05f;
                arm = -vaultPlantArm;
                leg = vaultPlantLeg;
                lean = vaultPlantLean;
                headPitch = -10f;
                break;

            case PlayerController.VaultPhase.Over:
                float over = Mathf.SmoothStep(0f, 1f, t);
                pitch = Mathf.Lerp(8f, vaultOverPitch, over);
                drop = Mathf.Lerp(0.05f, vaultOverDrop, over);
                arm = Mathf.Lerp(-vaultPlantArm, -vaultOverArm, over);
                leg = Mathf.Lerp(vaultPlantLeg, vaultLegTuck, over);
                lean = Mathf.Lerp(vaultPlantLean, 8f, over);
                headPitch = -10f;
                break;

            case PlayerController.VaultPhase.Land:
                float land = Mathf.SmoothStep(0f, 1f, t);
                pitch = Mathf.Lerp(vaultOverPitch, 0f, land);
                drop = Mathf.Lerp(vaultOverDrop, vaultLandCrouch, land);
                arm = Mathf.Lerp(-vaultOverArm, 0f, land);
                leg = Mathf.Lerp(vaultLegTuck, 8f, land);
                lean = Mathf.Lerp(8f, 0f, land);
                headPitch = Mathf.Lerp(-10f, 0f, land);
                break;

            default:
                return;
        }

        hips.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        hips.localPosition = baseHipsPos - Vector3.up * drop;
        if (body != null) body.localRotation = Quaternion.Euler(lean, 0f, 0f);
        if (head != null) head.localRotation = Quaternion.Euler(headPitch, 0f, 0f);
        if (legL != null) legL.localRotation = Quaternion.Euler(leg, 0f, 0f);
        if (legR != null) legR.localRotation = Quaternion.Euler(leg + 6f, 0f, 0f);
        if (armL != null) armL.localRotation = Quaternion.Euler(arm - 10f, 0f, 0f);
        if (armR != null) armR.localRotation = Quaternion.Euler(arm + 12f, 0f, 0f);
    }

    /// <summary>Prone crawl: telo horizontalno, noge ispruzene, ruke naizmenicno vuku.</summary>
    private void UpdateCrawlPose(float speed)
    {
        if (speed > 0.05f)
            crawlPhase += speed * crawlFrequency * Mathf.PI * 2f * Time.deltaTime;

        float sin = Mathf.Sin(crawlPhase);
        float swingScale = Mathf.Clamp01(speed * 3f);
        float swing = sin * crawlSwingAmp * swingScale;

        hips.localRotation = Quaternion.Euler(crawlBodyPitch, 0f, 0f);
        hips.localPosition = baseHipsPos - Vector3.up * (crawlHipsDrop - Mathf.Abs(sin) * 0.02f * swingScale);

        if (body != null) body.localRotation = Quaternion.identity;
        if (head != null) head.localRotation = Quaternion.Euler(-crawlHeadLift, 0f, 0f);

        if (legL != null) legL.localRotation = Quaternion.Euler(crawlLegExtend + swing, 0f, 0f);
        if (legR != null) legR.localRotation = Quaternion.Euler(crawlLegExtend - swing, 0f, 0f);
        if (armL != null) armL.localRotation = Quaternion.Euler(-crawlArmReach - swing * 0.6f, 0f, 0f);
        if (armR != null) armR.localRotation = Quaternion.Euler(-crawlArmReach + swing * 0.6f, 0f, 0f);

        FootstepCheck(sin, speed, 0.08f);
    }

    private void FootstepCheck(float sin, float speed, float threshold)
    {
        float sign = Mathf.Sign(sin);
        if (speed > threshold && sign != prevSinSign && prevSinSign != 0f)
        {
            audio.PlayFootstep(nextFootstepLeft);
            nextFootstepLeft = !nextFootstepLeft;
        }
        prevSinSign = sign;
    }

    private void OnLanded()
    {
        landTimer = landSquashTime;
        audio.PlayLand();
    }

    private float SquashCurve()
    {
        if (landTimer <= 0f) return 0f;
        landTimer -= Time.deltaTime;
        return Mathf.Clamp01(landTimer / Mathf.Max(0.001f, landSquashTime)) * landSquashAmp;
    }

    private Vector3 CrouchOffset()
    {
        float t = landTimer > 0f ? Mathf.Clamp01(landTimer / Mathf.Max(0.001f, landSquashTime)) : 0f;
        return Vector3.up * (t * landCrouchAmp);
    }

    private void ApplySquash(float squash)
    {
        if (hips == null) return;
        hips.localScale = new Vector3(1f + squash, 1f - squash, 1f + squash);
    }

    /// <summary>Gradi stilizovani placeholder humanoid od primitiva (runtime-safe).</summary>
    public static Transform BuildRig(GameObject player)
    {
        Transform existing = player.transform.Find("PlaceholderRig");
        if (existing != null) DestroyNow(existing.gameObject);

        GameObject rig = new GameObject("PlaceholderRig");
        rig.transform.SetParent(player.transform, false);

        // Player-ov origin je na CENTRU capsule-a -> racunamo local Y stopala
        // da rig ne bi lebdela (offseti delova su od stopala)
        float feetLocalY = -1f;
        Collider col = player.GetComponent<Collider>();
        if (col is CapsuleCollider cc) feetLocalY = cc.center.y - cc.height * 0.5f;
        else if (col != null) feetLocalY = -col.bounds.extents.y;

        GameObject hips = Part("Hips", rig.transform, new Vector3(0f, feetLocalY + 0.95f, 0f));

        // sakrij Player-ov sopstveni capsule mesh (rig preuzima vizuel)
        Renderer own = player.GetComponent<Renderer>();
        if (own != null) own.enabled = false;
        Prim("Body", PrimitiveType.Capsule, hips.transform, new Vector3(0f, 0.35f, 0f),
            new Vector3(0.62f, 0.42f, 0.38f), new Color(0.20f, 0.55f, 0.60f));
        Prim("Head", PrimitiveType.Sphere, hips.transform, new Vector3(0f, 0.82f, 0f),
            new Vector3(0.42f, 0.42f, 0.42f), new Color(0.90f, 0.75f, 0.60f));

        GameObject legL = Part("LegL", hips.transform, new Vector3(0.18f, 0f, 0f));
        GameObject legR = Part("LegR", hips.transform, new Vector3(-0.18f, 0f, 0f));
        Prim("LegMeshL", PrimitiveType.Capsule, legL.transform, new Vector3(0f, -0.42f, 0f),
            new Vector3(0.20f, 0.42f, 0.20f), new Color(0.25f, 0.30f, 0.40f));
        Prim("LegMeshR", PrimitiveType.Capsule, legR.transform, new Vector3(0f, -0.42f, 0f),
            new Vector3(0.20f, 0.42f, 0.20f), new Color(0.25f, 0.30f, 0.40f));

        GameObject armL = Part("ArmL", hips.transform, new Vector3(0.40f, 0.52f, 0f));
        GameObject armR = Part("ArmR", hips.transform, new Vector3(-0.40f, 0.52f, 0f));
        Prim("ArmMeshL", PrimitiveType.Capsule, armL.transform, new Vector3(0f, -0.32f, 0f),
            new Vector3(0.14f, 0.32f, 0.14f), new Color(0.20f, 0.55f, 0.60f));
        Prim("ArmMeshR", PrimitiveType.Capsule, armR.transform, new Vector3(0f, -0.32f, 0f),
            new Vector3(0.14f, 0.32f, 0.14f), new Color(0.20f, 0.55f, 0.60f));

        SanitizeRig(rig.transform, player.layer);
        return rig.transform;
    }

    /// <summary>
    /// Placeholder delovi ne smeju da ucestvuju u fizici: sklanja collidere
    /// (CreatePrimitive ih podrazumevano dodaje) i stavlja rig na Player layer
    /// da ne ulazi u ground-check cast.
    /// </summary>
    private static void SanitizeRig(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        var collider = root.GetComponent<Collider>();
        if (collider != null) DestroyNow(collider);
        foreach (Transform child in root) SanitizeRig(child, layer);
    }

    private static void DestroyNow(Object obj)
    {
        if (Application.isPlaying) Object.Destroy(obj);
        else Object.DestroyImmediate(obj);
    }

    private static Shader PickShader()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Diffuse");
        return sh;
    }

    private static GameObject Part(string name, Transform parent, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader sh = PickShader();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            const string dir = "Assets/Materials/Placeholder";
            string path = dir + "/Placeholder_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Materials"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Materials");
            if (!UnityEditor.AssetDatabase.IsValidFolder(dir))
                UnityEditor.AssetDatabase.CreateFolder("Assets/Materials", "Placeholder");

            var asset = new Material(sh) { color = color };
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            return asset;
        }
#endif
        return new Material(sh) { color = color };
    }

    private static GameObject Prim(string name, PrimitiveType type, Transform parent,
        Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
        return go;
    }
}
