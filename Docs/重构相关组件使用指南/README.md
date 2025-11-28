# 重构组件使用指南

本文档介绍重构后的核心系统及其使用方法。

## 📋 目录

1. [事件系统 (EventBus)](#1-事件系统-eventbus)
2. [对象池系统 (Object Pool)](#2-对象池系统-object-pool)
3. [配置系统 (GameConfig)](#3-配置系统-gameconfig)
4. [玩家状态机 (Player State Machine)](#4-玩家状态机-player-state-machine)
5. [场景设置](#5-场景设置)

---

## 快速链接

| 文档 | 内容 |
|------|------|
| [场景设置指南.md](场景设置指南.md) | PoolManager、ConfigManager 放置位置 |
| [EVENT_SYSTEM_GUIDE.md](EVENT_SYSTEM_GUIDE.md) | 事件系统详细说明 |
| [VContainer重构报告.md](VContainer重构报告.md) | 依赖注入详细说明 |
| [VContainer使用指南.cs](VContainer使用指南.cs) | VContainer 代码示例 |

---

## 1. 事件系统 (EventBus)

### 文件位置
- `Assets/Core/Scripts/Framework/Events/EventBus.cs`
- `Assets/Core/Scripts/Framework/Events/GameEvents.cs`

### 使用方法

```csharp
using EdgeRunner.Events;

public class MyComponent : MonoBehaviour
{
    void OnEnable()
    {
        // 订阅事件
        EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
    }

    void OnDisable()
    {
        // 必须取消订阅，防止内存泄漏！
        EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
    }

    private void OnEnemyDefeated(EnemyDefeatedEvent evt)
    {
        Debug.Log($"敌人被击杀，奖励能量: {evt.EnergyReward}");
    }

    // 发布事件
    private void SomeMethod()
    {
        EventBus.Publish(new PlayerDamagedEvent
        {
            Damage = 10,
            CurrentHealth = 90,
            MaxHealth = 100
        });
    }
}
```

### 可用事件列表

| 事件类型 | 用途 |
|---------|------|
| `PlayerEnergyChangedEvent` | 能量值变化 |
| `PlayerRewardedEvent` | 玩家获得奖励 |
| `PlayerDamagedEvent` | 玩家受伤 |
| `PlayerDiedEvent` | 玩家死亡 |
| `PlayerDashedEvent` | 玩家冲刺 |
| `TimeSlowStateChangedEvent` | 时缓状态变化 |
| `EnemyDefeatedEvent` | 敌人被击杀 |
| `GamePausedEvent` | 游戏暂停/恢复 |
| `GameWonEvent` | 游戏胜利 |
| `GameOverEvent` | 游戏失败 |
| `SceneLoadedEvent` | 场景加载完成 |

---

## 2. 对象池系统 (Object Pool)

### 文件位置
- `Assets/Core/Scripts/Framework/Pooling/IPoolable.cs`
- `Assets/Core/Scripts/Framework/Pooling/GenericPool.cs`
- `Assets/Core/Scripts/Framework/Pooling/PoolManager.cs`
- `Assets/Modules/Bullet/Scripts/PoolableBullet.cs`

### 设置步骤

1. **创建 PoolManager 对象**
   - 在场景中创建空 GameObject
   - 添加 `PoolManager` 组件
   - 设置 `bulletPrefab`（使用带 `PoolableBullet` 组件的预制体）

2. **创建池化预制体**
   - 复制现有子弹预制体
   - 替换 `BulletController` 为 `PoolableBullet`
   - 确保有 Rigidbody2D、Collider2D、SpriteRenderer

### 使用方法

```csharp
using EdgeRunner.Pooling;

// 获取子弹（替代 Instantiate）
PoolableBullet bullet = PoolManager.Instance.GetBullet(position, rotation);
bullet.Initialize(direction, speed, maxDistance);

// 返回子弹（在 PoolableBullet 内部自动调用，也可手动调用）
bullet.ReturnToPool();  // 替代 Destroy()
```

### 更新 EnemyController 使用对象池

```csharp
// 旧代码
GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

// 新代码
PoolableBullet bullet = PoolManager.Instance.GetBullet(
    transform.position, 
    Quaternion.Euler(0f, 0f, currentAngle)
);
bullet.Initialize(bulletDirection, bulletSpeed, bulletMaxDistance);
```

---

## 3. 配置系统 (GameConfig)

### 文件位置
- `Assets/Core/Scripts/Framework/Config/GameConfig.cs`
- `Assets/Core/Scripts/Framework/Config/ConfigManager.cs`

### 设置步骤

1. **创建配置资源**
   - 右键 Project 窗口 → Create → EdgeRunner → GameConfig
   - 命名为 `GameConfig`
   - 在 Inspector 中调整各项参数

2. **创建 ConfigManager**
   - 在场景中创建空 GameObject（建议放在 `ProjectLifetimeScope` 同级）
   - 添加 `ConfigManager` 组件
   - 将 GameConfig 资源拖入 `gameConfig` 字段

### 使用方法

```csharp
using EdgeRunner.Config;

public class MyComponent : MonoBehaviour
{
    void Start()
    {
        // 方式1：通过 Instance 访问
        float moveSpeed = ConfigManager.Instance.Config.Player.MoveSpeed;

        // 方式2：通过静态属性访问（推荐）
        float dashDistance = ConfigManager.Player.DashDistance;
        float bulletSpeed = ConfigManager.Bullet.Speed;
        int bulletPoolSize = ConfigManager.Pool.BulletPoolSize;
    }
}
```

### 配置项一览

| 分类 | 配置项 | 默认值 |
|------|--------|--------|
| **Player** | MoveSpeed | 6.2 |
| | DashDistance | 3.9 |
| | DashCooldown | 0.2 |
| | MaxEnergy | 80 |
| | TimeSlowScale | 0.3 |
| **Enemy** | ShootInterval | 1.8 |
| | BulletCount | 8 |
| | SpreadAngle | 10 |
| **Bullet** | Speed | 11.8 |
| | MaxDistance | 16 |
| **Pool** | BulletPoolSize | 100 |

---

## 4. 玩家状态机 (Player State Machine)

### 文件位置
- `Assets/Modules/Player/Scripts/States/IPlayerState.cs`
- `Assets/Modules/Player/Scripts/States/PlayerStateBase.cs`
- `Assets/Modules/Player/Scripts/States/PlayerStateMachine.cs`
- `Assets/Modules/Player/Scripts/States/PlayerController.cs`
- `Assets/Modules/Player/Scripts/States/IdleState.cs`
- `Assets/Modules/Player/Scripts/States/MovingState.cs`
- `Assets/Modules/Player/Scripts/States/DashingState.cs`
- `Assets/Modules/Player/Scripts/States/TimeSlowState.cs`

### 使用新系统

**方式1：完全替换（推荐用于新项目）**

1. 在 Player GameObject 上添加 `PlayerController` 组件
2. `PlayerStateMachine` 会自动添加
3. 移除旧的 `PlayerMovement` 组件

**方式2：并行使用（渐进迁移）**

保留 `PlayerMovement`，逐步将功能迁移到状态机系统。

### 状态转换图

```
                    ┌─────────────┐
                    │   Idle      │
                    └──────┬──────┘
                           │ 有移动输入
                           ▼
                    ┌─────────────┐
        ┌──────────▶│   Moving    │◀──────────┐
        │           └──────┬──────┘           │
        │                  │                   │
    无输入              按冲刺键             按时缓键
        │                  │                   │
        │                  ▼                   ▼
        │           ┌─────────────┐    ┌─────────────┐
        │           │   Dashing   │    │  TimeSlow   │
        │           └──────┬──────┘    └──────┬──────┘
        │                  │                   │
        └──────────────────┴───────────────────┘
                    冷却完成/能量耗尽
```

### 添加新状态

```csharp
using EdgeRunner.Player.States;

public class AttackingState : PlayerStateBase
{
    public override string StateName => "Attacking";

    public AttackingState(PlayerStateMachine stateMachine, PlayerController controller)
        : base(stateMachine, controller)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        // 进入攻击状态的逻辑
    }

    public override void OnUpdate()
    {
        // 攻击状态的每帧更新
    }

    public override void OnExit()
    {
        base.OnExit();
        // 退出攻击状态的逻辑
    }
}
```

然后在 `PlayerStateMachine.Initialize()` 中注册：

```csharp
RegisterState(new AttackingState(this, controller));
```

---

## 🔄 迁移建议

### 阶段1：并行运行
保留现有代码，只添加新系统，逐步验证。

### 阶段2：功能迁移
将功能从旧系统迁移到新系统：
1. 子弹生成 → 使用 `PoolManager`
2. 参数配置 → 使用 `ConfigManager`
3. 系统通信 → 使用 `EventBus`

### 阶段3：清理
移除旧代码和组件。

---

## 5. 场景设置

详细的场景设置说明请参阅 **[场景设置指南.md](场景设置指南.md)**。

### 快速概览

| 组件 | 放置位置 | 生命周期 |
|------|----------|----------|
| `ConfigManager` | Mainmenu 场景根层级 | DontDestroyOnLoad |
| `PoolManager` | Mainmenu 场景根层级 | 全局 |
| `PlayerController` | Player GameObject | 场景级 |

### 关于 PlayerMovement

✅ **可以移除**。`PlayerController` + 状态机 可以完全替代 `PlayerMovement`。

以下组件已更新支持新系统：
- `PlayerDeathHandler` - 兼容 PlayerController
- `PlayerCombat` - 已移除无用的 PlayerMovement 引用
- `EnergyBar` - 完全使用事件驱动，无任何直接引用

---

## ⚠️ 常见问题

### Q: 事件没有被触发？
检查是否在 `OnDisable` 中取消了订阅，或者对象被提前销毁。

### Q: 对象池中的对象行为异常？
确保在 `OnDespawn()` 中正确重置了所有状态。

### Q: 配置修改后没有生效？
ScriptableObject 在编辑器中修改会立即生效，但运行时缓存的值不会自动更新。

### Q: 状态机不响应输入？
检查 `PlayerController` 的 `inputActions` 是否正确启用，以及输入绑定是否正确。

### Q: ConfigManager.Player 返回 null？
确保 Mainmenu 场景有 ConfigManager，且已设置 GameConfig 资产。