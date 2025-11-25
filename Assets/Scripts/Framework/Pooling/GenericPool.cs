using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可池化接口 - 所有可被对象池管理的对象必须实现此接口
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 对象被获取时调用
    /// </summary>
    void OnAcquire();

    /// <summary>
    /// 对象被返回到池时调用
    /// </summary>
    void OnReturn();

    /// <summary>
    /// 获取对象的游戏物体
    /// </summary>
    GameObject GetGameObject();
}

/// <summary>
/// 泛型对象池 - 支持任何实现 IPoolable 的类型
/// 
/// 使用示例:
///     var bulletPool = new GenericPool<Bullet>(bulletPrefab, 100, parent);
///     var bullet = bulletPool.Acquire();
///     // ... 使用 bullet
///     bulletPool.Release(bullet);
/// </summary>
public class GenericPool<T> where T : Component, IPoolable
{
    private readonly T prefab;
    private readonly Transform parent;
    private readonly Queue<T> availableObjects;
    private readonly HashSet<T> activeObjects;
    private readonly int maxPoolSize;

    public int AvailableCount => availableObjects.Count;
    public int ActiveCount => activeObjects.Count;
    public int TotalCount => AvailableCount + ActiveCount;

    public GenericPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.maxPoolSize = initialSize * 2;  // 池最多可以容纳 2 倍的初始大小
        
        availableObjects = new Queue<T>(initialSize);
        activeObjects = new HashSet<T>();

        // 预热池
        Prewarm(initialSize);
    }

    /// <summary>
    /// 预热池 - 提前创建对象，避免运行时卡顿
    /// </summary>
    private void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            CreateNewObject();
        }
    }

    /// <summary>
    /// 创建新对象
    /// </summary>
    private T CreateNewObject()
    {
        var instance = Object.Instantiate(prefab, parent);
        instance.gameObject.SetActive(false);
        availableObjects.Enqueue(instance);
        return instance;
    }

    /// <summary>
    /// 获取对象
    /// </summary>
    public T Acquire()
    {
        T obj;

        if (availableObjects.Count > 0)
        {
            obj = availableObjects.Dequeue();
        }
        else
        {
            // 如果池中没有可用对象，创建新对象
            obj = CreateNewObject();
            availableObjects.Dequeue();
        }

        obj.gameObject.SetActive(true);
        activeObjects.Add(obj);
        obj.OnAcquire();

        return obj;
    }

    /// <summary>
    /// 释放对象回池
    /// </summary>
    public void Release(T obj)
    {
        if (obj == null) return;

        if (!activeObjects.Contains(obj))
        {
            Debug.LogWarning($"Tried to release object that is not active: {obj.name}");
            return;
        }

        activeObjects.Remove(obj);
        obj.OnReturn();
        obj.gameObject.SetActive(false);

        // 如果池未满，将对象放回
        if (availableObjects.Count < maxPoolSize)
        {
            availableObjects.Enqueue(obj);
        }
        else
        {
            // 否则销毁对象
            Object.Destroy(obj.gameObject);
        }
    }

    /// <summary>
    /// 释放所有活跃对象
    /// </summary>
    public void ReleaseAll()
    {
        var activeList = new List<T>(activeObjects);
        foreach (var obj in activeList)
        {
            Release(obj);
        }
    }

    /// <summary>
    /// 清空整个池
    /// </summary>
    public void Clear()
    {
        ReleaseAll();
        availableObjects.Clear();
        activeObjects.Clear();
    }

    /// <summary>
    /// 预留更多对象到池中
    /// </summary>
    public void Expand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (availableObjects.Count < maxPoolSize)
            {
                CreateNewObject();
            }
        }
    }
}
