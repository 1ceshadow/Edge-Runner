// ═══════════════════════════════════════════════════════════════════════════
//  ServiceHelper - 服务查找工具类
//  提供统一的服务回退查找逻辑，避免各处重复的 null 检查和 FindFirstObjectByType
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace EdgeRunner.Framework
{
    /// <summary>
    /// 服务查找工具类
    /// </summary>
    public static class ServiceHelper
    {
        /// <summary>
        /// 获取服务，如果缓存为 null 则自动查找
        /// </summary>
        /// <typeparam name="T">服务类型（必须是 UnityEngine.Object 派生类）</typeparam>
        /// <param name="cached">缓存的服务引用</param>
        /// <returns>服务实例，如果未找到则返回 null</returns>
        public static T GetOrFind<T>(ref T cached) where T : Object
        {
            // 检查缓存是否有效（考虑 Unity 的 fake null）
            if (cached != null && cached)
            {
                return cached;
            }
            
            // 重置缓存（可能已被销毁）
            cached = null;
            
            // 查找服务
            cached = Object.FindFirstObjectByType<T>();
            return cached;
        }

        /// <summary>
        /// 获取服务，优先使用注入的服务，其次使用单例，最后查找
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="injected">注入的服务</param>
        /// <param name="singleton">单例实例</param>
        /// <returns>可用的服务实例</returns>
        public static T GetService<T>(T injected, T singleton) where T : class
        {
            if (injected != null) return injected;
            if (singleton != null) return singleton;
            return null;
        }

        /// <summary>
        /// 获取服务，优先使用注入的服务，其次使用单例，最后在场景中查找
        /// </summary>
        /// <typeparam name="T">服务类型（必须是 UnityEngine.Object 派生类）</typeparam>
        /// <param name="injected">注入的服务</param>
        /// <param name="singleton">单例实例</param>
        /// <param name="cached">缓存的查找结果</param>
        /// <returns>可用的服务实例</returns>
        public static T GetServiceOrFind<T>(T injected, T singleton, ref T cached) where T : Object
        {
            // 优先使用注入的服务
            if (injected != null && injected) return injected;
            
            // 其次使用单例
            if (singleton != null && singleton) return singleton;
            
            // 最后查找
            return GetOrFind(ref cached);
        }

        /// <summary>
        /// 检查服务是否有效（非 null 且未被销毁）
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="service">服务实例</param>
        /// <returns>服务是否有效</returns>
        public static bool IsValid<T>(T service) where T : Object
        {
            return service != null && service;
        }
    }
}
