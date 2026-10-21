using UnityEngine;

/// <summary>
/// Most izmedju PlayerController-a i Animator-a:
/// svake frejma upisuje Speed / VerticalVel / Grounded parametre,
/// a na OnJumped okida Jump trigger.
/// Zahteva Animator sa "PlayerAnimatorController" controller-om na istom objektu.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private PlayerController controller;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalVelHash = Animator.StringToHash("VerticalVel");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlayerController>();
        controller.OnJumped += () => animator.SetTrigger(JumpHash);

        if (GetComponent<ProceduralPlayerAnimator>() == null)
            gameObject.AddComponent<ProceduralPlayerAnimator>();
    }

    private void Update()
    {
        animator.SetFloat(SpeedHash, controller.NormalizedHorizontalSpeed);
        animator.SetFloat(VerticalVelHash, controller.VerticalVelocity);
        animator.SetBool(GroundedHash, controller.IsGrounded);
    }
}
