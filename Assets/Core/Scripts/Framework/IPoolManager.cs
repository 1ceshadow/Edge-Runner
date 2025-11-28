using EdgeRunner.Pooling;

/// <summary>
/// 对象池管理器接口 - 统一池化资源的访问协议。
/// </summary>
public interface IPoolManager
{
    GenericPool<PoolableBullet> BulletPool { get; }
    PoolableBullet GetBullet(UnityEngine.Vector3 position, UnityEngine.Quaternion rotation);
    void ReturnBullet(PoolableBullet bullet);
    void ReturnAllBullets();
    void ClearAllPools();
}
