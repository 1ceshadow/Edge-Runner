// ═══════════════════════════════════════════════════════════════════════════
//  IPoolable - 可池化对象接口
//  所有需要使用对象池的对象都应实现此接口
// ═══════════════════════════════════════════════════════════════════════════

namespace EdgeRunner.Pooling
{
    /// <summary>
    /// 可池化对象接口
    /// 实现此接口的对象可以被对象池管理
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 当对象从池中取出时调用（相当于 Start）
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 当对象返回池中时调用（相当于销毁前的清理）
        /// </summary>
        void OnDespawn();

        /// <summary>
        /// 将对象返回池中（替代 Destroy）
        /// </summary>
        void ReturnToPool();
    }
}
