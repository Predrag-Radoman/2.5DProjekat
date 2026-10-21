using UnityEngine;

/// <summary>
/// 2.5D kamera: fiksni blago kos ugao (side-view sa nagoveštajem dubine),
/// glatko prati playera po X osi (i Y pri skoku), sa granicama levela.
/// Ugao kamere se namešta rotacijom samog Camera objekta u Inspector-u.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, -8.5f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Granice levela (kamera staje na ivicama)")]
    [SerializeField] private bool clampX = true;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        if (clampX) desired.x = Mathf.Clamp(desired.x, minX, maxX);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
