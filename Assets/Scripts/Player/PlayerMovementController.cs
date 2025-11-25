using UnityEngine;

/// <summary>
/// 玩家移动控制器 - 仅处理移动逻辑
/// 
/// 职责:
/// - 处理移动输入和速度
/// - 管理朝向和旋转
/// - 与物理引擎交互
/// - 发送移动事件
/// 
/// 不做的事情:
/// - 处理能量管理 (由 PlayerEnergySystem 处理)
/// - 处理状态管理 (由 PlayerStateMachine 处理)
/// - 处理输入绑定 (由 PlayerInputHandler 处理)
/// </summary>
public class PlayerMovementController : MonoBehaviour, IMoveable
{
    private GameConfig config;
    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 moveDirection = Vector2.right;
    private float currentSpeed;
    private bool isInitialized = false;

    public void Initialize()
    {
        config = GameConfig.Load();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        currentSpeed = config.Player.MoveSpeed;
        isInitialized = true;

        Debug.Log("✓ PlayerMovementController initialized");
    }

    public void SetMoveInput(Vector2 input)
    {
        if (!isInitialized) return;

        if (input != Vector2.zero)
        {
            moveDirection = input.normalized;
            UpdateRotation();
        }
    }

    public void SetSpeed(float speed)
    {
        currentSpeed = speed;
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;

        // 更新物理速度
        rb.linearVelocity = moveDirection * currentSpeed;

        // 更新动画
        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetFloat("DirectionX", moveDirection.x);
            animator.SetFloat("DirectionY", moveDirection.y);
        }
    }

    private void UpdateRotation()
    {
        if (animator == null) return;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.AngleAxis(angle, Vector3.forward),
            config.Player.RotationSmoothness * Time.deltaTime
        );
    }

    // IMoveable 实现
    public Vector2 GetPosition() => transform.position;
    public Vector2 GetDirection() => moveDirection;
    public void SetDirection(Vector2 newDirection) => moveDirection = newDirection.normalized;
}
