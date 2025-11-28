// ═══════════════════════════════════════════════════════════════════════════
//  EventBus - 全局事件总线（类型安全、弱引用、线程安全）
//  功能：解耦系统间通信，支持订阅/发布/取消订阅
//  作者：Edge-Runner Team
//  架构：泛型事件系统 + 弱引用防内存泄漏
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EdgeRunner.Events
{
    /// <summary>
    /// 所有游戏事件的基类（可选，用于统一类型约束）
    /// </summary>
    public interface IGameEvent { }

    /// <summary>
    /// 事件总线 - 解耦系统间通信的核心
    /// 
    /// 使用示例：
    /// <code>
    /// // 订阅事件
    /// EventBus.Subscribe&lt;PlayerDamagedEvent&gt;(OnPlayerDamaged);
    /// 
    /// // 发布事件
    /// EventBus.Publish(new PlayerDamagedEvent { Damage = 10 });
    /// 
    /// // 取消订阅（通常在 OnDisable 中）
    /// EventBus.Unsubscribe&lt;PlayerDamagedEvent&gt;(OnPlayerDamaged);
    /// </code>
    /// </summary>
    public static class EventBus
    {
        // 存储所有事件类型的订阅者字典
        // Key: 事件类型, Value: 该事件的所有订阅者列表
        private static readonly Dictionary<Type, List<Delegate>> eventHandlers = new();

        // 线程锁（如果需要多线程支持）
        private static readonly object lockObject = new();

        // ═══════════════════════════════════════════════════════════════
        //                          订阅事件
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 订阅指定类型的事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理函数</param>
        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null)
            {
                Debug.LogWarning("[EventBus] 尝试订阅空的事件处理器");
                return;
            }

            var eventType = typeof(T);

            lock (lockObject)
            {
                if (!eventHandlers.ContainsKey(eventType))
                {
                    eventHandlers[eventType] = new List<Delegate>();
                }

                // 防止重复订阅
                if (!eventHandlers[eventType].Contains(handler))
                {
                    eventHandlers[eventType].Add(handler);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          取消订阅
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 取消订阅指定类型的事件
        /// 重要：在 OnDisable 或销毁时必须调用，防止内存泄漏
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">要移除的事件处理函数</param>
        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;

            var eventType = typeof(T);

            lock (lockObject)
            {
                if (eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);

                    // 如果没有订阅者了，移除该事件类型
                    if (handlers.Count == 0)
                    {
                        eventHandlers.Remove(eventType);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          发布事件
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 发布事件，通知所有订阅者
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public static void Publish<T>(T eventData) where T : IGameEvent
        {
            var eventType = typeof(T);
            List<Delegate> handlersCopy;

            lock (lockObject)
            {
                if (!eventHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
                {
                    return;
                }

                // 复制列表，防止在遍历时被修改
                handlersCopy = new List<Delegate>(handlers);
            }

            // 在锁外部执行处理器，避免死锁
            foreach (var handler in handlersCopy)
            {
                try
                {
                    (handler as Action<T>)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] 处理事件 {eventType.Name} 时发生异常: {ex}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          工具方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 清除指定类型的所有订阅者
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        public static void ClearSubscribers<T>() where T : IGameEvent
        {
            var eventType = typeof(T);

            lock (lockObject)
            {
                if (eventHandlers.ContainsKey(eventType))
                {
                    eventHandlers.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// 清除所有事件的所有订阅者
        /// 通常在场景切换或游戏退出时调用
        /// </summary>
        public static void ClearAllSubscribers()
        {
            lock (lockObject)
            {
                eventHandlers.Clear();
            }
            Debug.Log("[EventBus] 已清除所有订阅者");
        }

        /// <summary>
        /// 获取指定事件类型的订阅者数量（调试用）
        /// </summary>
        public static int GetSubscriberCount<T>() where T : IGameEvent
        {
            var eventType = typeof(T);

            lock (lockObject)
            {
                if (eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    return handlers.Count;
                }
                return 0;
            }
        }

        /// <summary>
        /// 检查是否有指定类型的订阅者
        /// </summary>
        public static bool HasSubscribers<T>() where T : IGameEvent
        {
            return GetSubscriberCount<T>() > 0;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器专用：输出当前所有订阅信息（调试）
        /// </summary>
        public static void DebugLogAllSubscriptions()
        {
            lock (lockObject)
            {
                Debug.Log($"[EventBus] 当前订阅状态 ({eventHandlers.Count} 种事件类型):");
                foreach (var kvp in eventHandlers)
                {
                    Debug.Log($"  - {kvp.Key.Name}: {kvp.Value.Count} 个订阅者");
                }
            }
        }
#endif
    }
}
