using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 服务定位器 (Service Locator) / 依赖注入容器
/// 用于注册和获取全局服务，替代 FindGameObjectWithTag 和硬编码单例
/// 
/// 使用示例:
///     ServiceLocator.Register<IPlayerService>(playerService);
///     var player = ServiceLocator.Get<IPlayerService>();
///     ServiceLocator.Unregister<IPlayerService>();
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> services = new();

    /// <summary>
    /// 注册服务
    /// </summary>
    public static void Register<T>(T service) where T : class
    {
        if (service == null)
        {
            Debug.LogError($"Cannot register null service of type {typeof(T).Name}");
            return;
        }

        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"Service {type.Name} is already registered. Overwriting...");
        }

        services[type] = service;
        Debug.Log($"✓ Service registered: {type.Name}");
    }

    /// <summary>
    /// 获取服务
    /// </summary>
    public static T Get<T>() where T : class
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var service))
        {
            return service as T;
        }

        Debug.LogError($"Service {type.Name} is not registered!");
        return null;
    }

    /// <summary>
    /// 尝试获取服务
    /// </summary>
    public static bool TryGet<T>(out T service) where T : class
    {
        service = null;
        var type = typeof(T);
        if (services.TryGetValue(type, out var svc))
        {
            service = svc as T;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 注销服务
    /// </summary>
    public static bool Unregister<T>() where T : class
    {
        var type = typeof(T);
        if (services.Remove(type))
        {
            Debug.Log($"✓ Service unregistered: {type.Name}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 清空所有服务
    /// </summary>
    public static void Clear()
    {
        services.Clear();
        Debug.Log("✓ All services cleared");
    }

    /// <summary>
    /// 获取已注册服务数量
    /// </summary>
    public static int ServiceCount => services.Count;
}
