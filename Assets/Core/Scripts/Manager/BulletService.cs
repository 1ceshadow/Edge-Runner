using UnityEngine;
using EdgeRunner.Config;
using EdgeRunner.Events;
using EdgeRunner.Framework;
using EdgeRunner.Pooling;

/// <summary>
/// 统一的子弹生成/回收服务，封装对象池与事件派发。
/// </summary>
public sealed class BulletService : MonoBehaviour, IBulletService
{
    [Header("═══ 依赖引用 ═══")]
    [Tooltip("显式指定 PoolManager，留空则自动寻找单例。")]
    [SerializeField] private PoolManager poolManager;
    [Tooltip("当对象池不可用时的兜底预制体。")]
    [SerializeField] private PoolableBullet fallbackBulletPrefab;

    private PoolManager ActivePoolManager
    {
        get
        {
            // 使用 ServiceHelper 处理服务查找和回退逻辑
            return ServiceHelper.GetServiceOrFind(poolManager, PoolManager.Instance, ref poolManager) 
                   ?? GetComponent<PoolManager>();
        }
    }

    public PoolableBullet SpawnBullet(in BulletSpawnRequest request)
    {
        Vector2 direction = request.Direction == Vector2.zero ? Vector2.right : request.Direction.normalized;
        float speed = request.SpeedOverride ?? (ConfigManager.Bullet?.Speed ?? 11.8f);
        float maxDistance = request.MaxDistanceOverride ?? (ConfigManager.Bullet?.MaxDistance ?? 16f);
        int damage = request.DamageOverride ?? (ConfigManager.Bullet?.Damage ?? 1);
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        PoolableBullet bullet = null;
        var manager = ActivePoolManager;
        if (manager != null)
        {
            bullet = manager.GetBullet(request.Position, rotation);
        }

        if (bullet == null && fallbackBulletPrefab != null)
        {
            bullet = Instantiate(fallbackBulletPrefab, request.Position, rotation);
            bullet.OnSpawn();
        }

        if (bullet == null)
        {
            Debug.LogWarning("[BulletService] 无法生成子弹，未找到对象池或兜底预制体。");
            return null;
        }

        bullet.Launch(request.Position, direction, speed, maxDistance, damage, request.IsPlayerBullet, request.SourceId);

        EventBus.Publish(new BulletFiredEvent
        {
            Position = request.Position,
            Direction = direction,
            Speed = speed,
            IsPlayerBullet = request.IsPlayerBullet
        });

        return bullet;
    }

    public void ReturnBullet(PoolableBullet bullet)
    {
        if (bullet == null)
        {
            return;
        }

        var manager = ActivePoolManager;
        if (manager != null)
        {
            manager.ReturnBullet(bullet);
        }
        else
        {
            bullet.gameObject.SetActive(false);
        }
    }
}
