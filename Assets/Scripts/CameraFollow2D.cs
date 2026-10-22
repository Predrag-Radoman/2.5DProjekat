using UnityEngine;

/// <summary>
/// 2.5D kamera: fiksni blago kos ugao (side-view sa nagoveštajem dubine),
/// glatko prati playera po X osi (i Y pri skoku), sa look-ahead-om u smeru
/// kretanja i granicama levela. Ugao se namešta rotacijom Camera objekta.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, -8.5f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Look-ahead (kamera gleda malo ispred playera)")]
    [SerializeField] private float lookAheadDistance = 1.2f;
    [SerializeField] private float lookAheadSmoothTime = 0.25f;

    [Header("Granice levela (kamera staje na ivicama)")]
    [SerializeField] private bool clampX = true;
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 20f;

    private PlayerController playerController;
    private Vector3 velocity;
    private float lookAhead;
    private float lookAheadVelocity;

    private void Awake()
    {
        if (target != null) playerController = target.GetComponent<PlayerController>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // look-ahead proporcionalan horizontalnoj brzini, izgladjeno
        if (playerController != null)
        {
            float normalized = playerController.HorizontalVelocity.x / Mathf.Max(0.001f, playerController.MaxMoveSpeed);
            float desiredLook = Mathf.Clamp(normalized, -1f, 1f) * lookAheadDistance;
            lookAhead = Mathf.SmoothDamp(lookAhead, desiredLook, ref lookAheadVelocity, lookAheadSmoothTime);
        }

        Vector3 desired = target.position + offset;
        desired.x += lookAhead;
        if (clampX) desired.x = Mathf.Clamp(desired.x, minX, maxX);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
