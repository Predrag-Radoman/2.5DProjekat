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

    [Header("Air / land")]
    [SerializeField] private float jumpTuckAngle = 32f;
    [SerializeField] private float armRaiseAngle = 42f;
    [SerializeField] private float landSquashTime = 0.16f;
    [SerializeField] private float landSquashAmp = 0.22f;
    [SerializeField] private float landCrouchAmp = 0.14f;

    private PlayerController controller;
    private ProceduralAudio audio;

    private Transform hips, body, head, legL, legR, armL, armR;
    private Vector3 baseHipsPos;

    private float walkPhase;
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

        float speed = controller.NormalizedHorizontalSpeed;
        bool grounded = controller.IsGrounded;
        float vvel = controller.VerticalVelocity;

        if (grounded)
        {
            walkPhase += speed * walkFrequency * Mathf.PI * 2f * Time.deltaTime;
            float sin = Mathf.Sin(walkPhase);
            float swing = sin * legSwingAmp * speed;

            if (legL != null) legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (armL != null) armL.localRotation = Quaternion.Euler(-swing * armSwingRatio, 0f, 0f);
            if (armR != null) armR.localRotation = Quaternion.Euler(swing * armSwingRatio, 0f, 0f);

            float bob = Mathf.Abs(sin) * bodyBobAmp * speed;
            float lean = speed * forwardLeanAmp;
            hips.localPosition = baseHipsPos + Vector3.up * bob - CrouchOffset();
            if (body != null) body.localRotation = Quaternion.Euler(lean, 0f, 0f);
            if (head != null) head.localRotation = Quaternion.Euler(-lean * 0.6f, 0f, 0f);

            float sign = Mathf.Sign(sin);
            if (speed > 0.15f && sign != prevSinSign && prevSinSign != 0f)
            {
                audio.PlayFootstep(nextFootstepLeft);
                nextFootstepLeft = !nextFootstepLeft;
            }
            prevSinSign = sign;

            ApplySquash(0f);
        }
        else
        {
            float tuck = vvel > 0f ? jumpTuckAngle : jumpTuckAngle * 0.35f;
            float raise = vvel > 0f ? -armRaiseAngle : -armRaiseAngle * 0.3f;
            if (legL != null) legL.localRotation = Quaternion.Euler(tuck, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(tuck * 0.6f, 0f, 0f);
            if (armL != null) armL.localRotation = Quaternion.Euler(raise, 0f, 0f);
            if (armR != null) armR.localRotation = Quaternion.Euler(raise, 0f, 0f);

            hips.localPosition = baseHipsPos - CrouchOffset();
            if (body != null) body.localRotation = Quaternion.Euler(forwardLeanAmp * 0.4f, 0f, 0f);
            if (head != null) head.localRotation = Quaternion.Euler(-forwardLeanAmp * 0.25f, 0f, 0f);

            ApplySquash(SquashCurve());
        }
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
        if (existing != null) Destroy(existing.gameObject);

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

        return rig.transform;
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

    private static GameObject Prim(string name, PrimitiveType type, Transform parent,
        Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = new Material(PickShader()) { color = color };
        return go;
    }
}
