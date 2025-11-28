# Edge-Runner 事件驱动架构使用指南

## 📋 概述

本项目已实现 **Event-Driven Architecture（事件驱动架构）**，用于解耦系统间的通信。核心组件包括：

- `EventBus.cs` - 事件总线（发布/订阅）
- `GameEvents.cs` - 所有游戏事件定义

## 🚀 快速开始

### 1. 引入命名空间

```csharp
using EdgeRunner.Events;
```

### 2. 订阅事件

在 `OnEnable()` 中订阅：

```csharp
private void OnEnable()
{
    EventBus.Subscribe<PlayerEnergyChangedEvent>(OnEnergyChanged);
    EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
}

private void OnEnergyChanged(PlayerEnergyChangedEvent evt)
{
    Debug.Log($"能量变化: {evt.CurrentEnergy}/{evt.MaxEnergy}");
}
```

### 3. 取消订阅（防止内存泄漏）

**重要**：必须在 `OnDisable()` 中取消订阅！

```csharp
private void OnDisable()
{
    EventBus.Unsubscribe<PlayerEnergyChangedEvent>(OnEnergyChanged);
    EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
}
```

### 4. 发布事件

```csharp
// 敌人被击杀时发布
EventBus.Publish(new EnemyDefeatedEvent
{
    Position = transform.position,
    EnemyType = "Shooter",
    EnergyReward = 10f,
    KilledByPlayer = true
});

// 能量变化时发布
EventBus.Publish(new PlayerEnergyChangedEvent
{
    CurrentEnergy = currentEnergy,
    MaxEnergy = maxEnergy,
    DeltaEnergy = delta,
    Reason = EnergyChangeReason.EnemyKill
});
```

## 📦 已实现的事件

### 玩家事件

| 事件 | 用途 | 关键字段 |
|------|------|----------|
| `PlayerEnergyChangedEvent` | 能量值变化 | `CurrentEnergy`, `MaxEnergy`, `Reason` |
| `PlayerRewardedEvent` | 玩家获得奖励 | `Type`, `Amount`, `Position` |
| `PlayerDamagedEvent` | 玩家受伤 | `Damage`, `CurrentHealth`, `DamageType` |
| `PlayerDiedEvent` | 玩家死亡 | `Position`, `Reason` |
| `PlayerDashedEvent` | 玩家冲刺 | `StartPosition`, `EndPosition`, `IsPerfectDash` |
| `TimeSlowStateChangedEvent` | 时缓状态切换 | `IsTimeSlowed`, `TimeScale` |

### 敌人事件

| 事件 | 用途 | 关键字段 |
|------|------|----------|
| `EnemyDefeatedEvent` | 敌人被击败 | `Position`, `EnemyType`, `EnergyReward` |
| `EnemySpawnedEvent` | 敌人生成 | `Position`, `EnemyType` |
| `EnemyDamagedEvent` | 敌人受伤 | `Damage`, `CurrentHealth` |

### 游戏状态事件

| 事件 | 用途 | 关键字段 |
|------|------|----------|
| `GamePausedEvent` | 游戏暂停/恢复 | `IsPaused` |
| `GameWonEvent` | 游戏胜利 | `LevelIndex`, `LevelName`, `CompletionTime` |
| `GameOverEvent` | 游戏失败 | `Reason`, `LevelIndex` |
| `SceneLoadedEvent` | 场景加载完成 | `SceneName`, `SceneIndex`, `IsMainMenu` |
| `LevelStartedEvent` | 关卡开始 | `LevelIndex`, `LevelName` |

### UI/音频事件

| 事件 | 用途 | 关键字段 |
|------|------|----------|
| `ShowToastEvent` | 显示提示信息 | `Message`, `Duration`, `Type` |
| `PlaySFXEvent` | 播放音效 | `SFXName`, `Position`, `Volume` |
| `PlayBGMEvent` | 播放背景音乐 | `BGMName`, `FadeIn`, `FadeDuration` |

## ⚠️ 注意事项

### 1. 避免在 Update 中频繁发布

```csharp
// ❌ 不好 - 每帧发布
void Update()
{
    EventBus.Publish(new PlayerEnergyChangedEvent { ... });
}

// ✅ 好 - 仅在有变化时发布
void Update()
{
    if (Mathf.Abs(currentEnergy - lastEnergy) > 0.1f)
    {
        EventBus.Publish(new PlayerEnergyChangedEvent { ... });
        lastEnergy = currentEnergy;
    }
}
```

### 2. 必须取消订阅

```csharp
// ❌ 内存泄漏风险
void OnEnable()
{
    EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
}
// 忘记 OnDisable！

// ✅ 正确
void OnDisable()
{
    EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
}
```

### 3. 使用 struct 事件减少 GC

所有事件类型都使用 `struct` 而非 `class`，减少垃圾回收压力。

## 🔄 迁移指南

### 从直接引用迁移到事件驱动

**之前（紧耦合）：**
```csharp
// EnemyController.cs - 直接修改玩家数据
private void Die()
{
    playerMovement.currentEnergy += playerMovement.killReward0;
    playerMovement.isKillRewarded0 = true;
}
```

**之后（事件驱动）：**
```csharp
// EnemyController.cs - 只发布事件
private void Die()
{
    EventBus.Publish(new EnemyDefeatedEvent
    {
        Position = transform.position,
        EnergyReward = killEnergyReward,
        KilledByPlayer = true
    });
}

// PlayerMovement.cs - 订阅事件处理奖励
private void OnEnemyDefeated(EnemyDefeatedEvent evt)
{
    if (evt.KilledByPlayer)
    {
        currentEnergy += evt.EnergyReward;
    }
}
```

## 📁 文件结构

```
Assets/Core/Scripts/Framework/Events/
├── EventBus.cs      # 事件总线核心
└── GameEvents.cs    # 所有事件定义
```

## 🎯 下一步

1. **对象池系统** - 替代 Instantiate/Destroy
2. **玩家状态机** - 清晰的状态管理
3. **配置系统** - ScriptableObject 参数管理
