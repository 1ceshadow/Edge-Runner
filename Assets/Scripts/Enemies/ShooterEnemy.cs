using UnityEngine;

/// <summary>
/// 射手敌人 - 会围绕玩家旋转并射击的敌人
/// 
/// 继承自 EnemyBase，只需实现具体的射击行为
/// </summary>
public class ShooterEnemy : EnemyBase
{
    private BulletPool bulletPool;
    private float shootTimer;
    private bool canSeePlayer = false;

    protected override void Start()
    {
        base.Start();

        bulletPool = ServiceLocator.Get<BulletPool>();
        if (bulletPool == null)
        {
            Debug.LogError("BulletPool not registered in ServiceLocator!");
        }

        shootTimer = config.Enemy.ShootInterval;
    }

    private void Update()
    {
        if (!isAlive || playerService == null) return;

        // 获取玩家位置
        var playerPos = playerService.GetPosition();
        float distanceToPlayer = Vector2.Distance(transform.position, playerPos);

        // 更新面向
        UpdateFacingDirection(playerPos);

        // 检查是否能看到玩家
        canSeePlayer = distanceToPlayer <= config.Enemy.ShootDistance;

        if (canSeePlayer)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                Shoot(playerPos);
                shootTimer = config.Enemy.ShootInterval;
            }
        }
        else
        {
            shootTimer = config.Enemy.ShootInterval;
        }
    }

    private void UpdateFacingDirection(Vector2 playerPos)
    {
        Vector2 directionToPlayer = (playerPos - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg + 90;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Shoot(Vector2 targetPos)
    {
        Vector2 directionToPlayer = (targetPos - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;

        float angleStep = config.Enemy.SpreadAngle / (config.Enemy.BulletCount - 1);
        float startAngle = baseAngle - (config.Enemy.SpreadAngle / 2f);

        for (int i = 0; i < config.Enemy.BulletCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Vector2 bulletDirection = new Vector2(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            ).normalized;

            if (bulletPool != null)
            {
                var bullet = bulletPool.GetBullet();
                bullet.Launch(
                    transform.position,
                    bulletDirection,
                    config.Enemy.BulletSpeed
                );
            }

            EventBus.Publish(new BulletFiredEvent
            {
                Origin = transform.position,
                Direction = bulletDirection,
                Speed = config.Enemy.BulletSpeed
            });
        }
    }

    public override void OnAcquire()
    {
        base.OnAcquire();
        shootTimer = config.Enemy.ShootInterval;
    }
}
