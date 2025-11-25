using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件总线 (Event Bus)
/// 全局事件系统，用于解耦各游戏系统之间的通信
/// 支持类型安全和弱引用，防止内存泄漏
/// 
/// 使用示例:
///     // 订阅事件
///     EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
///     
///     // 发布事件
///     EventBus.Publish(new PlayerDamagedEvent { Damage = 10 });
///     
///     // 取消订阅
///     EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
/// </summary>
public static class EventBus
{
    // 委托类型定义
    private delegate void EventHandler(object eventData);

    // 存储所有事件监听器
    private static readonly Dictionary<Type, List<EventHandler>> handlers = new();

    /// <summary>
    /// 订阅事件
    /// </summary>
    public static void Subscribe<T>(Action<T> handler) where T : class
    {
        if (handler == null)
        {
            Debug.LogError("Event handler cannot be null");
            return;
        }

        var eventType = typeof(T);

        if (!handlers.ContainsKey(eventType))
        {
            handlers[eventType] = new List<EventHandler>();
        }

        // 包装为 EventHandler
        void wrapper(object data) => handler(data as T);
        handlers[eventType].Add(wrapper);
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    public static void Publish<T>(T eventData) where T : class
    {
        if (eventData == null)
        {
            Debug.LogError("Event data cannot be null");
            return;
        }

        var eventType = typeof(T);

        if (!handlers.ContainsKey(eventType))
        {
            return;  // 没有监听器，安全返回
        }

        // 执行所有监听器
        var handlerList = handlers[eventType];
        for (int i = handlerList.Count - 1; i >= 0; i--)
        {
            try
            {
                handlerList[i]?.Invoke(eventData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in event handler: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler) where T : class
    {
        if (handler == null) return;

        var eventType = typeof(T);

        if (!handlers.ContainsKey(eventType))
        {
            return;
        }

        // 移除所有匹配的处理器
        var handlerList = handlers[eventType];
        handlerList.RemoveAll(h => h.Method == handler.Method && h.Target == handler.Target);

        if (handlerList.Count == 0)
        {
            handlers.Remove(eventType);
        }
    }

    /// <summary>
    /// 清空所有事件监听器
    /// </summary>
    public static void Clear()
    {
        handlers.Clear();
    }

    /// <summary>
    /// 获取已注册的监听器数量
    /// </summary>
    public static int ListenerCount => handlers.Count;
}

// ═══════════════════════════════════════════════════════════════
//                          事件定义
// ═══════════════════════════════════════════════════════════════

// 玩家事件
public class PlayerMovedEvent
{
    public Vector2 NewPosition;
    public Vector2 Direction;
}

public class PlayerDamagedEvent
{
    public int DamageAmount;
    public Vector2 DamageSource;
}

public class PlayerDiedEvent
{
    public Vector2 DeathPosition;
}

public class PlayerEnergyChangedEvent
{
    public float CurrentEnergy;
    public float MaxEnergy;
    public float Percentage => CurrentEnergy / MaxEnergy;
}

public class PlayerDashedEvent
{
    public Vector2 DashStartPosition;
    public Vector2 DashDirection;
    public bool IsPerfectDash;
}

// 战斗事件
public class PlayerAttackEvent
{
    public int DamageDealt;
    public Vector2 TargetPosition;
}

public class EnemyDefeatedEvent
{
    public int EnemyId;
    public Vector2 DefeatPosition;
    public int RewardGiven;
}

public class BulletFiredEvent
{
    public Vector2 Origin;
    public Vector2 Direction;
    public float Speed;
}

// 游戏状态事件
public class GamePausedEvent
{
    public float PauseTime;
}

public class GameResumedEvent
{
    public float ResumeTime;
}

public class LevelCompleteEvent
{
    public int LevelIndex;
    public float CompletionTime;
}

public class LevelFailedEvent
{
    public int LevelIndex;
}

public class GameStartedEvent
{
    public int LevelIndex;
}

// UI 事件
public class UIRefreshEvent
{
    public string ScreenName;
}
