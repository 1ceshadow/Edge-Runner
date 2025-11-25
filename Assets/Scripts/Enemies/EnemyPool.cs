using UnityEngine;

/// <summary>
/// 敌人池管理器 - 管理敌人对象池
/// 
/// 在 ServiceLocator 中注册为 EnemyPool 单例
/// </summary>
public class EnemyPool : MonoBehaviour
{
    [SerializeField] private EnemyBase enemyPrefab;
    [SerializeField] private int initialPoolSize = 20;

    private GenericPool<EnemyBase> pool;

    private void Awake()
    {
        // 创建池
        pool = new GenericPool<EnemyBase>(enemyPrefab, initialPoolSize, transform);

        // 在 ServiceLocator 中注册
        ServiceLocator.Register(this);

        Debug.Log($"✓ EnemyPool initialized with {initialPoolSize} enemies");
    }

    public EnemyBase GetEnemy()
    {
        var enemy = pool.Acquire();
        return enemy;
    }

    public void ReturnEnemy(EnemyBase enemy)
    {
        if (enemy != null)
            pool.Release(enemy);
    }

    public void ReleaseAll() => pool.ReleaseAll();

    public int GetAvailableCount() => pool.AvailableCount;
    public int GetActiveCount() => pool.ActiveCount;

    private void OnDestroy()
    {
        pool?.Clear();
        ServiceLocator.Unregister<EnemyPool>();
    }
}
