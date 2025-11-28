// ═══════════════════════════════════════════════════════════════════════════
//  PoolableBullet - 支持对象池的子弹（重构版）
//  
//  配置来源：
//  - 所有参数从 ConfigManager.Bullet 读取
//  - 不允许本地配置覆盖
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using EdgeRunner.Pooling;
using EdgeRunner.Events;
using EdgeRunner.Config;

public class PoolableBullet : MonoBehaviour, IPoolable
{
    // ═══════════════════════════════════════════════════════════════
    //                          配置访问（从 ConfigManager）
    // ═══════════════════════════════════════════════════════════════

    private BulletConfig Config => ConfigManager.Bullet;

    // 从全局配置读取
    public float DefaultSpeed => Config?.Speed ?? 11.8f;
    public float DefaultMaxDistance => Config?.MaxDistance ?? 16f;
    private int BaseDamage => Config?.Damage ?? 1;
    public int CurrentDamage => currentDamage;
    public bool UseContinuousDetection => Config?.UseContinuousDetection ?? true;
    public float DetectionStep => Config?.DetectionStep ?? 0.5f;

    // ═══════════════════════════════════════════════════════════════
    //                          运行时参数（可被 Launch 覆盖）
    // ═══════════════════════════════════════════════════════════════

    private float speed;
    private float maxDistance;

    // ═══════════════════════════════════════════════════════════════
    //                          私有字段
    // ═══════════════════════════════════════════════════════════════

    private Vector3 startPosition;
    private Vector2 direction;
    private bool isActive;
    private bool isPlayerBullet;
    private string sourceId;
    private int currentDamage;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bulletCollider;

    // ═══════════════════════════════════════════════════════════════
    //                          Unity 生命周期
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        // 缓存组件引用
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletCollider = GetComponent<Collider2D>();
        currentDamage = BaseDamage;
    }

    private void Update()
    {
        if (!isActive) return;

        // 高速子弹使用连续检测
        if (UseContinuousDetection && speed > 10f)
        {
            MoveWithContinuousDetection();
        }
        else
        {
            MoveNormally();
        }

        // 距离检测
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            ReturnToPool();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //                          IPoolable 实现
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 从池中取出时调用
    /// </summary>
    public void OnSpawn()
    {
        isActive = true;
        startPosition = transform.position;

        // 启用渲染和碰撞
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (bulletCollider != null) bulletCollider.enabled = true;

        // 注册到管理器
        if (BulletManager.Instance != null)
            BulletManager.Instance.RegisterBullet(transform);
    }

    /// <summary>
    /// 返回池中时调用
    /// </summary>
    public void OnDespawn()
    {
        isActive = false;
        direction = Vector2.zero;
        isPlayerBullet = false;
        sourceId = null;
        currentDamage = BaseDamage;

        // 禁用渲染和碰撞
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (bulletCollider != null) bulletCollider.enabled = false;

        // 从管理器移除
        if (BulletManager.Instance != null)
            BulletManager.Instance.UnregisterBullet(transform);

        // 重置速度
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    /// <summary>
    /// 返回到池中（替代 Destroy）
    /// </summary>
    public void ReturnToPool()
    {
        if (!isActive) return;

        // 使用 PoolManager 返回
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnBullet(this);
        }
        else
        {
            // 降级：如果没有池管理器，直接禁用
            gameObject.SetActive(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //                          公共接口
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 初始化子弹（发射时调用）
    /// </summary>
    public void Initialize(Vector2 dir, float bulletSpeed, float bulletMaxDistance)
    {
        Launch(transform.position, dir, bulletSpeed, bulletMaxDistance);
    }

    /// <summary>
    /// 发射子弹（设置位置、方向、速度）
    /// </summary>
    public void Launch(
        Vector3 position,
        Vector2 dir,
        float bulletSpeed,
        float bulletMaxDistance,
        int damage = -1,
        bool ownerIsPlayer = false,
        string originId = null)
    {
        transform.position = position;
        direction = dir == Vector2.zero ? Vector2.right : dir.normalized;
        speed = bulletSpeed;
        maxDistance = bulletMaxDistance;
        startPosition = position;
        isActive = true;
        isPlayerBullet = ownerIsPlayer;
        sourceId = originId;
        currentDamage = damage >= 0 ? damage : BaseDamage;

        // 设置旋转
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// 发射子弹（使用全局配置的速度和距离）
    /// </summary>
    public void Launch(Vector3 position, Vector2 dir)
    {
        Launch(position, dir, DefaultSpeed, DefaultMaxDistance);
    }

    /// <summary>
    /// 设置方向（兼容旧接口）
    /// </summary>
    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //                          移动逻辑
    // ═══════════════════════════════════════════════════════════════

    private void MoveNormally()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void MoveWithContinuousDetection()
    {
        float moveDistance = speed * Time.deltaTime;
        int steps = Mathf.CeilToInt(moveDistance / DetectionStep);
        float stepSize = moveDistance / steps;

        for (int i = 0; i < steps; i++)
        {
            transform.Translate(direction * stepSize, Space.World);
            if (CheckImmediateCollision())
                break;
        }
    }

    private bool CheckImmediateCollision()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.1f);
        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == gameObject)
            {
                continue;
            }

            if (ShouldProcessCollision(hit.gameObject))
            {
                HandleCollision(hit.gameObject);
                return true;
            }
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════════
    //                          碰撞处理
    // ═══════════════════════════════════════════════════════════════

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;
        HandleCollision(collision.gameObject);
    }

    private void HandleCollision(GameObject hitObject)
    {
        Debug.Log($"子弹碰撞: {hitObject.name} (标签: {hitObject.tag})");

        if (hitObject.CompareTag("Cover"))
        {
            Debug.Log("子弹击中掩体");
            PublishHitEvent(hitObject);
            ReturnToPool();
            return;
        }

        if (hitObject.CompareTag("Player") && !isPlayerBullet)
        {
            Debug.Log("子弹击中玩家 → 触发死亡");
            var player = hitObject.GetComponent<PlayerDeathHandler>();
            if (player != null)
            {
                player.DieFromExternal();
            }
            else
            {
                Debug.LogWarning("Player 没有 PlayerDeathHandler 组件！");
            }
            PublishHitEvent(hitObject);
            ReturnToPool();
            return;
        }

        if (hitObject.CompareTag("Enemy") && isPlayerBullet)
        {
            if (hitObject.TryGetComponent(out EnemyController enemy))
            {
                enemy.TakeDamage(currentDamage);
            }
            PublishHitEvent(hitObject);
            ReturnToPool();
            return;
        }
    }

    private bool ShouldProcessCollision(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.CompareTag("Cover")) return true;
        if (target.CompareTag("Player")) return !isPlayerBullet;
        if (target.CompareTag("Enemy")) return isPlayerBullet;
        return false;
    }

    private void PublishHitEvent(GameObject target)
    {
        EventBus.Publish(new BulletHitEvent
        {
            Position = transform.position,
            HitTag = target != null ? target.tag : string.Empty,
            IsPlayerBullet = isPlayerBullet,
            Damage = currentDamage,
            SourceId = sourceId
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //                          调试
    // ═══════════════════════════════════════════════════════════════

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isActive) return;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 1f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}
