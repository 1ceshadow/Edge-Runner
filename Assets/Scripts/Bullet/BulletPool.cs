using UnityEngine;

/// <summary>
/// 子弹池管理器 - 管理子弹对象池
/// 
/// 在 ServiceLocator 中注册为 BulletPool 单例
/// </summary>
public class BulletPool : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int initialPoolSize = 100;

    private GenericPool<Bullet> pool;

    private void Awake()
    {
        // 创建池
        pool = new GenericPool<Bullet>(bulletPrefab, initialPoolSize, transform);

        // 在 ServiceLocator 中注册
        ServiceLocator.Register(this);

        Debug.Log($"✓ BulletPool initialized with {initialPoolSize} bullets");
    }

    public Bullet GetBullet() => pool.Acquire();

    public void ReturnBullet(Bullet bullet)
    {
        if (bullet != null)
            pool.Release(bullet);
    }

    public void ReleaseAll() => pool.ReleaseAll();

    private void OnDestroy()
    {
        pool?.Clear();
        ServiceLocator.Unregister<BulletPool>();
    }
}
