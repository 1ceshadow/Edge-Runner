// ═══════════════════════════════════════════════════════════════════════════
//  EnemyController - 敌人控制器（重构版）
//  
//  职责：
//  - 敌人行为控制（射击、面向玩家）
//  - 生命值管理
//  - 死亡处理
//  
//  配置来源：
//  - 所有参数从 ConfigManager.Enemy 读取
//  - 子弹参数从 ConfigManager.Bullet 读取
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using VContainer;
using EdgeRunner.Events;
using EdgeRunner.Config;

public class EnemyController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //                          资源引用（非参数）
    // ═══════════════════════════════════════════════════════════════

    [Header("═══ 敌人类型标识 ═══")]
    [SerializeField] private string enemyType = "Shooter";

    // ═══════════════════════════════════════════════════════════════
    //                          配置访问（从 ConfigManager）
    // ═══════════════════════════════════════════════════════════════

    // 敌人配置 - 使用 ConfigManager 安全访问方法
    public float ShootInterval => ConfigManager.GetShootInterval();
    public float ShootDistance => ConfigManager.GetShootDistance();
    public int BulletCount => ConfigManager.GetEnemyBulletCount();
    public float SpreadAngle => ConfigManager.GetSpreadAngle();
    public int MaxHealth => ConfigManager.GetEnemyMaxHealth();
    public float KillEnergyReward => ConfigManager.GetEnemyKillReward();

    // 子弹配置
    public float BulletSpeed => ConfigManager.GetBulletSpeed();
    public float BulletMaxDistance => ConfigManager.GetBulletMaxDistance();

    // ═══════════════════════════════════════════════════════════════
    //                          运行时状态
    // ═══════════════════════════════════════════════════════════════

    private Transform player;
    private float shootTimer;
    private bool canSeePlayer = false;
    private int currentHealth;

    // VContainer 依赖注入
    private IPlayerService playerService;
    private IBulletService bulletService;

    /// <summary>
    /// VContainer 依赖注入
    /// IBulletService 在 ProjectLifetimeScope 中注册
    /// IPlayerService 在 GameLifetimeScope 中注册
    /// </summary>
    [Inject]
    public void Construct(IPlayerService playerService, IBulletService bulletService)
    {
        this.playerService = playerService;
        this.bulletService = bulletService;
    }

    // ═══════════════════════════════════════════════════════════════
    //                          生命周期
    // ═══════════════════════════════════════════════════════════════

    void Start()
    {
        // 验证配置
        ValidateConfig();

        // 初始化生命值
        currentHealth = MaxHealth;

        // 使用注入的玩家服务获取玩家 Transform
        InitializePlayerReference();
        
        // 获取 BulletService（优先使用注入，其次查找）
        if (bulletService == null)
        {
            bulletService = FindFirstObjectByType<BulletService>();
        }

        shootTimer = ShootInterval;
    }
    
    /// <summary>
    /// 初始化玩家引用
    /// </summary>
    private void InitializePlayerReference()
    {
        if (playerService != null)
        {
            player = playerService.Transform;
            return;
        }
        
        // 回退：尝试通过 Player 组件查找
        var playerObj = FindFirstObjectByType<Player>();
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.LogWarning($"[{nameof(EnemyController)}] 使用回退方式查找玩家（建议配置 VContainer）");
        }
        else
        {
            Debug.LogError($"[{nameof(EnemyController)}] 未找到玩家！");
        }
    }

    private void ValidateConfig()
    {
        if (ConfigManager.Enemy == null)
        {
            Debug.LogWarning(
                $"[EnemyController:{gameObject.name}] ⚠ ConfigManager.Enemy 为 null，使用默认值\n" +
                "请确保 ConfigManager 已正确设置。"
            );
        }
        else
        {
            Debug.Log($"✓ EnemyController: 配置已加载 (射击间隔={ShootInterval}, 子弹数={BulletCount})");
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateFacingDirection();

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        canSeePlayer = distanceToPlayer <= ShootDistance;

        if (canSeePlayer)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                Shoot();
                shootTimer = ShootInterval;
            }
        }
        else
        {
            shootTimer = ShootInterval;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //                          行为方法
    // ═══════════════════════════════════════════════════════════════

    void UpdateFacingDirection()
    {
        if (player == null) return;

        Vector2 directionToPlayer = player.position - transform.position;
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg + 90;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Shoot()
    {
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;

        int bulletCount = BulletCount;
        float spreadAngle = SpreadAngle;
        float angleStep = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0f;
        float startAngle = baseAngle - (spreadAngle / 2f);

        for (int i = 0; i < bulletCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Vector2 bulletDirection = new Vector2(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            ).normalized;

            SpawnBullet(bulletDirection);
        }
    }

    private void SpawnBullet(Vector2 direction)
    {
        if (bulletService == null)
        {
            Debug.LogWarning("EnemyController: BulletService 未注入，无法生成子弹");
            return;
        }

        bulletService.SpawnBullet(new BulletSpawnRequest
        {
            Position = transform.position,
            Direction = direction,
            SpeedOverride = BulletSpeed,
            MaxDistanceOverride = BulletMaxDistance,
            IsPlayerBullet = false,
            SourceId = enemyType,
            DamageOverride = null
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //                          伤害处理
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"敌人受到 {damage} 点伤害，剩余生命: {currentHealth}");

        // 播放受伤效果
        PlayHitEffect();

        // 检查死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 播放受伤效果
    /// </summary>
    private void PlayHitEffect()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            StartCoroutine(HitEffectCoroutine(sr));
        }
    }

    private System.Collections.IEnumerator HitEffectCoroutine(SpriteRenderer sr)
    {
        Color originalColor = sr.color;
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = originalColor;
    }

    /// <summary>
    /// 死亡处理
    /// </summary>
    private void Die()
    {
        Debug.Log("敌人死亡！");

        // 🔔 发布敌人被击败事件（事件驱动，解耦奖励逻辑）
        EventBus.Publish(new EnemyDefeatedEvent
        {
            Position = transform.position,
            EnemyType = enemyType,
            EnergyReward = KillEnergyReward,
            KilledByPlayer = true
        });

        // 销毁敌人
        Destroy(gameObject);
    }

    // ═══════════════════════════════════════════════════════════════
    //                          Gizmos
    // ═══════════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ShootDistance);
    }
}
