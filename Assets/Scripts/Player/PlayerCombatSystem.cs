using UnityEngine;

/// <summary>
/// 玩家战斗系统 - 处理攻击、冲刺、时缓等战斗相关逻辑
/// 
/// 职责:
/// - 管理闪现/冲刺
/// - 管理时缓技能
/// - 检测攻击碰撞
/// - 发送战斗相关事件
/// </summary>
public class PlayerCombatSystem : MonoBehaviour, IAttackable
{
    private GameConfig config;
    private PlayerMovementController movementController;
    private IEnergyProvider energyProvider;
    private CircleCollider2D circleCollider;

    private float dashCooldownTimer = 0f;
    private float timeSlowCooldownTimer = 0f;
    private bool isInTimeSlow = false;

    private bool isInitialized = false;

    public void Initialize(IEnergyProvider energy)
    {
        config = GameConfig.Load();
        energyProvider = energy;
        movementController = GetComponent<PlayerMovementController>();
        circleCollider = GetComponent<CircleCollider2D>();

        isInitialized = true;
        Debug.Log("✓ PlayerCombatSystem initialized");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 更新冷却时间
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        if (timeSlowCooldownTimer > 0)
            timeSlowCooldownTimer -= Time.deltaTime;

        // 时缓状态检查
        if (isInTimeSlow && Input.GetKeyUp(KeyCode.E))
        {
            DeactivateTimeSlow();
        }
    }

    public void TryDash()
    {
        if (dashCooldownTimer > 0) return;

        var moveDir = movementController.GetDirection();
        if (moveDir == Vector2.zero) return;

        // 冲刺
        PerformDash(moveDir);
    }

    private void PerformDash(Vector2 direction)
    {
        var dashPos = (Vector2)transform.position + direction * config.Player.DashDistance;

        // 检测是否是极限闪避
        bool isPerfectDash = CheckBulletsInDashRange();

        if (isPerfectDash)
        {
            energyProvider.AddEnergy(config.Player.PerfectDashReward);
        }

        EventBus.Publish(new PlayerDashedEvent
        {
            DashStartPosition = transform.position,
            DashDirection = direction,
            IsPerfectDash = isPerfectDash
        });

        // 实际冲刺逻辑 (移动到目标位置)
        transform.position = dashPos;

        dashCooldownTimer = config.Player.DashCooldown;
    }

    public void TryActivateTimeSlow()
    {
        if (isInTimeSlow) return;
        if (timeSlowCooldownTimer > 0) return;

        // 检查能量
        if (!energyProvider.TryConsumeEnergy(10f))  // 启动需要 10 能量
            return;

        ActivateTimeSlow();
    }

    private void ActivateTimeSlow()
    {
        isInTimeSlow = true;
        Time.timeScale = 0.3f;  // 时缓为原来的 30%

        // 开始消耗能量
        EventBus.Publish(new GamePausedEvent { PauseTime = Time.realtimeSinceStartup });
    }

    private void DeactivateTimeSlow()
    {
        isInTimeSlow = false;
        Time.timeScale = 1f;

        timeSlowCooldownTimer = 3f;  // 3 秒冷却

        EventBus.Publish(new GameResumedEvent { ResumeTime = Time.realtimeSinceStartup });
    }

    public bool CheckBulletsInDashRange()
    {
        // 使用物理查询检测范围内的子弹
        var bulletsInRange = Physics2D.OverlapCircleAll(
            transform.position,
            config.Player.PerfectDashDetectRange,
            LayerMask.GetMask("Projectile")  // 需要在 Project Settings 中设置此层
        );

        return bulletsInRange.Length > 0;
    }

    // IAttackable 实现
    public void Attack()
    {
        // 暂未实现，可用于剑击等
    }

    public bool CanAttack() => true;
    public float GetAttackCooldown() => 0f;
}
