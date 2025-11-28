using UnityEngine;
using VContainer;

public class EnemyController : MonoBehaviour
{
    [Header("射击设置")]
    public float shootInterval = 1.8f;
    public float shootDistance = 20f;
    public int bulletCount = 8;
    public float spreadAngle = 10f;
    public GameObject bulletPrefab;
    
    [Header("子弹参数")]
    public float bulletSpeed = 11.8f;
    public float bulletMaxDistance = 16f;
    
    private Transform player;
    private float shootTimer;
    private bool canSeePlayer = false;

    [Header("生命值设置")]
    public int maxHealth = 1;
    public int currentHealth;

    [Header("时缓能量奖励")]
    private PlayerMovement playerMovement;
    
    // VContainer 依赖注入
    private IPlayerService playerService;
    
    [Inject]
    public void Construct(IPlayerService playerService)
    {
        this.playerService = playerService;
    }

    void Start()
    {
        // 初始化生命值
        currentHealth = maxHealth;
        
        // 使用注入的玩家服务
        if (playerService != null)
        {
            player = playerService.Transform;
            
            // 尝试获取 PlayerMovement 组件
            if (playerService.TryGetComponent<PlayerMovement>(out var movement))
            {
                playerMovement = movement;
            }
            
            Debug.Log("✓ EnemyController: 已通过 VContainer 获取玩家服务");
        }
        else
        {
            Debug.LogError("EnemyController: PlayerService 未注入，请检查 VContainer 配置");
        }
        
        shootTimer = shootInterval;
    }

    void Update()
    {
        if (player == null) return;
        
        UpdateFacingDirection();
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        canSeePlayer = distanceToPlayer <= shootDistance;
        
        if (canSeePlayer)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                Shoot();
                shootTimer = shootInterval;
            }
        }
        else
        {
            shootTimer = shootInterval;
        }
    }
    
    void UpdateFacingDirection()
    {
        if (player == null) return;
        
        Vector2 directionToPlayer = player.position - transform.position;
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg + 90;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
    
    void Shoot()
    {
        // if (bulletPrefab == null)
        // {
        //     Debug.LogError("EnemyController: bulletPrefab未设置！");
        //     return;
        // }
        
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
        
        float angleStep = spreadAngle / (bulletCount - 1);
        float startAngle = baseAngle - (spreadAngle / 2f);
        
        for (int i = 0; i < bulletCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Vector2 bulletDirection = new Vector2(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            ).normalized;
            
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            BulletController bulletController = bullet.GetComponent<BulletController>();
            
            if (bulletController != null)
            {
                bulletController.speed = bulletSpeed;
                bulletController.maxDistance = bulletMaxDistance;
                bulletController.SetDirection(bulletDirection);
            }
            
            bullet.transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
        
        Debug.Log("敌人射击！");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shootDistance);
    }
    
/// <summary>
/// 受到伤害
/// </summary>
public void TakeDamage(int damage)
{
    currentHealth -= damage;
    Debug.Log($"敌人受到 {damage} 点伤害，剩余生命: {currentHealth}");
    
    // 播放受伤效果
    // PlayHitEffect();
    
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
    // 简单的颜色闪烁
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
    
    // 播放死亡效果（可选）
    // PlayDeathEffect();

    // 时缓能量奖励
    if (playerMovement != null)
    {
        playerMovement.currentEnergy += playerMovement.killReward0;
        playerMovement.isKillRewarded0 = true;
        playerMovement.isRewarded = true;
    }
    
    // 销毁敌人
    Destroy(gameObject);
}

/// <summary>
/// 播放死亡效果
/// </summary>
private void PlayDeathEffect()
{
    // 可以在这里添加死亡动画、音效等
    // 简单的消失效果
    StartCoroutine(FadeOutAndDestroy());
}

private System.Collections.IEnumerator FadeOutAndDestroy()
{
    SpriteRenderer sr = GetComponent<SpriteRenderer>();
    if (sr != null)
    {
        float fadeTime = 0.3f;
        float elapsed = 0f;
        Color startColor = sr.color;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
    }
    
    Destroy(gameObject);
}
}