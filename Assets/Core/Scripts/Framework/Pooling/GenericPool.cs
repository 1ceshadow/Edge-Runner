// ═══════════════════════════════════════════════════════════════════════════
//  GenericPool<T> - 泛型对象池
//  高性能对象复用，消除 Instantiate/Destroy 造成的 GC 压力
//  
//  使用示例：
//  var bulletPool = new GenericPool<Bullet>(bulletPrefab, parent, 50);
//  Bullet bullet = bulletPool.Get();
//  bulletPool.Return(bullet);
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EdgeRunner.Pooling
{
    /// <summary>
    /// 泛型对象池
    /// </summary>
    /// <typeparam name="T">池化对象类型（必须是 MonoBehaviour 且实现 IPoolable）</typeparam>
    public class GenericPool<T> where T : MonoBehaviour, IPoolable
    {
        // ═══════════════════════════════════════════════════════════════
        //                          字段
        // ═══════════════════════════════════════════════════════════════

        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> availableObjects;
        private readonly HashSet<T> activeObjects;
        private readonly int maxSize;
        private readonly bool autoExpand;

        // ═══════════════════════════════════════════════════════════════
        //                          属性
        // ═══════════════════════════════════════════════════════════════

        /// <summary>池中可用对象数量</summary>
        public int AvailableCount => availableObjects.Count;

        /// <summary>当前活跃对象数量</summary>
        public int ActiveCount => activeObjects.Count;

        /// <summary>总创建的对象数量</summary>
        public int TotalCount => AvailableCount + ActiveCount;

        // ═══════════════════════════════════════════════════════════════
        //                          构造函数
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 创建对象池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">父对象（可选）</param>
        /// <param name="initialSize">初始池大小</param>
        /// <param name="maxSize">最大池大小（0 表示无限制）</param>
        /// <param name="autoExpand">是否自动扩展</param>
        public GenericPool(T prefab, Transform parent = null, int initialSize = 10, int maxSize = 0, bool autoExpand = true)
        {
            this.prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            this.parent = parent;
            this.maxSize = maxSize;
            this.autoExpand = autoExpand;

            availableObjects = new Queue<T>(initialSize);
            activeObjects = new HashSet<T>();

            // 预热池
            Prewarm(initialSize);
        }

        // ═══════════════════════════════════════════════════════════════
        //                          公共方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 预热池（创建初始对象）
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (maxSize > 0 && TotalCount >= maxSize) break;

                T obj = CreateNew();
                obj.gameObject.SetActive(false);
                availableObjects.Enqueue(obj);
            }

            Debug.Log($"[GenericPool<{typeof(T).Name}>] 预热完成，池中有 {AvailableCount} 个对象");
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>池化对象</returns>
        public T Get()
        {
            T obj;

            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else if (autoExpand && (maxSize == 0 || TotalCount < maxSize))
            {
                obj = CreateNew();
                Debug.Log($"[GenericPool<{typeof(T).Name}>] 池自动扩展，当前总数: {TotalCount}");
            }
            else
            {
                Debug.LogWarning($"[GenericPool<{typeof(T).Name}>] 池已耗尽且不能扩展！");
                return null;
            }

            obj.gameObject.SetActive(true);
            activeObjects.Add(obj);
            obj.OnSpawn();

            return obj;
        }

        /// <summary>
        /// 从池中获取对象并设置位置和旋转
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        /// <summary>
        /// 将对象返回池中
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;

            if (!activeObjects.Contains(obj))
            {
                Debug.LogWarning($"[GenericPool<{typeof(T).Name}>] 尝试返回不属于此池的对象");
                return;
            }

            obj.OnDespawn();
            obj.gameObject.SetActive(false);
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }

        /// <summary>
        /// 返回所有活跃对象到池中
        /// </summary>
        public void ReturnAll()
        {
            // 复制列表避免修改时遍历
            var activeList = new List<T>(activeObjects);
            foreach (var obj in activeList)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// 清空池（销毁所有对象）
        /// </summary>
        public void Clear()
        {
            ReturnAll();

            while (availableObjects.Count > 0)
            {
                var obj = availableObjects.Dequeue();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }

            activeObjects.Clear();
            Debug.Log($"[GenericPool<{typeof(T).Name}>] 池已清空");
        }

        // ═══════════════════════════════════════════════════════════════
        //                          私有方法
        // ═══════════════════════════════════════════════════════════════

        private T CreateNew()
        {
            T obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.name = $"{prefab.name}_Pooled_{TotalCount}";
            return obj;
        }
    }
}
