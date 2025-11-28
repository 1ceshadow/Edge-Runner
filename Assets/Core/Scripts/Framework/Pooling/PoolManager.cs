// ═══════════════════════════════════════════════════════════════════════════
//  PoolManager - 全局对象池管理器（重构版）
//  
//  配置来源：
//  - 池大小从 ConfigManager.Pool 读取
//  - 预制体从 Inspector 设置（资源引用，非参数）
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using EdgeRunner.Config;

namespace EdgeRunner.Pooling
{
    /// <summary>
    /// 对象池管理器
    /// 管理多个不同类型的对象池
    /// </summary>
    public class PoolManager : MonoBehaviour, IPoolManager
    {
        // ═══════════════════════════════════════════════════════════════
        //                          单例（向后兼容）
        // ═══════════════════════════════════════════════════════════════

        public static PoolManager Instance { get; private set; }

        // ═══════════════════════════════════════════════════════════════
        //                          资源引用（非参数配置）
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ 预制体引用 ═══")]
        [SerializeField] private PoolableBullet bulletPrefab;
        [SerializeField] private Transform bulletContainer;

        // ═══════════════════════════════════════════════════════════════
        //                          配置访问（从 ConfigManager）
        // ═══════════════════════════════════════════════════════════════

        private PoolConfig Config => ConfigManager.Pool;

        public int BulletPoolSize => Config?.BulletPoolSize ?? 100;
        public int EnemyPoolSize => Config?.EnemyPoolSize ?? 20;
        public int VFXPoolSize => Config?.VFXPoolSize ?? 50;

        // ═══════════════════════════════════════════════════════════════
        //                          对象池
        // ═══════════════════════════════════════════════════════════════

        private GenericPool<PoolableBullet> bulletPool;

        /// <summary>子弹池</summary>
        public GenericPool<PoolableBullet> BulletPool => bulletPool;

        // ═══════════════════════════════════════════════════════════════
        //                          生命周期
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializePools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                ClearAllPools();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          初始化
        // ═══════════════════════════════════════════════════════════════

        private void InitializePools()
        {
            // 验证配置
            if (Config == null)
            {
                Debug.LogWarning("[PoolManager] ConfigManager.Pool 为 null，使用默认值");
            }

            // 创建容器（如果没有指定）
            if (bulletContainer == null)
            {
                var containerObj = new GameObject("BulletPool_Container");
                containerObj.transform.SetParent(transform);
                bulletContainer = containerObj.transform;
            }

            // 初始化子弹池
            if (bulletPrefab != null)
            {
                int poolSize = BulletPoolSize;
                bulletPool = new GenericPool<PoolableBullet>(
                    bulletPrefab,
                    bulletContainer,
                    poolSize,
                    maxSize: poolSize * 2,
                    autoExpand: true
                );
                Debug.Log($"✓ PoolManager: 子弹池初始化完成，容量 {poolSize}");
            }
            else
            {
                Debug.LogWarning("PoolManager: bulletPrefab 未设置，子弹池未初始化");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          公共接口
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 获取子弹
        /// </summary>
        public PoolableBullet GetBullet(Vector3 position, Quaternion rotation)
        {
            if (bulletPool == null)
            {
                Debug.LogError("PoolManager: 子弹池未初始化！");
                return null;
            }
            return bulletPool.Get(position, rotation);
        }

        /// <summary>
        /// 返回子弹
        /// </summary>
        public void ReturnBullet(PoolableBullet bullet)
        {
            bulletPool?.Return(bullet);
        }

        /// <summary>
        /// 返回所有子弹
        /// </summary>
        public void ReturnAllBullets()
        {
            bulletPool?.ReturnAll();
        }

        /// <summary>
        /// 清空所有池
        /// </summary>
        public void ClearAllPools()
        {
            bulletPool?.Clear();
        }

        // ═══════════════════════════════════════════════════════════════
        //                          调试信息
        // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 100));
            GUILayout.BeginVertical("box");

            GUILayout.Label("对象池状态", GUI.skin.box);

            if (bulletPool != null)
            {
                GUILayout.Label($"子弹: {bulletPool.ActiveCount} 活跃 / {bulletPool.AvailableCount} 可用");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif
    }
}
