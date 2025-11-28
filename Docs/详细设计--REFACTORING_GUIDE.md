# Edge-Runner 项目重构架构方案

## 📋 执行摘要

本文档提供了一套完整的游戏架构重构方案，目标是将 Edge-Runner 从当前的**紧耦合、单一职责混乱**的结构，升级为**高内聚、低耦合、易扩展**的现代游戏架构。

---

## 🔍 第一部分：当前架构问题分析

### 1.1 主要问题点

| 问题 | 影响 | 严重程度 |
|------|------|--------|
| **单一职责违反** | `Player.cs` 空壳、`PlayerMovement.cs` 混合了移动、能量、碰撞、输入处理 | 🔴 严重 |
| **硬编码依赖** | `GameObject.FindGameObjectWithTag("Player")` 分散在各处 | 🔴 严重 |
| **状态管理混乱** | `isTimeSlowed`、`isDashing`、`isPerfectDashed` 等混在一起，无清晰状态机 | 🔴 严重 |
| **事件系统缺失** | 各系统直接调用，耦合度高，难以扩展 | 🟠 中等 |
| **对象池缺失** | 子弹、敌人频繁 Instantiate/Destroy，性能差 | 🟠 中等 |
| **配置硬编码** | 所有参数写在脚本中，难以调整和版本管理 | 🟠 中等 |
| **UI 与逻辑混合** | `EnergyBar` 直接读取 `PlayerMovement` 的数据 | 🟡 轻微 |

---

## 🏗️ 第二部分：新架构设计

### 2.1 整体架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                          Game Framework                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │    VContainer    │  │  EventBus        │  │ ConfigManager │  │
│  │  (DI Container)  │  │  (Event System)  │  │(ScriptableObj)|  │
│  └────────┬─────────┘  └────────┬─────────┘  └──────┬────────┘  │
│           │                     │                   │           │
├───────────┼─────────────────────┼───────────────────┼───────────┤
│           │                     │                   │           │
│  ┌────────┴────────┐    ┌───────┴────────┐   ┌──────┴────────┐  │
│  │  Core Systems   │    │  Gameplay Loop │   │ Subsystems    │  │
│  ├─────────────────┤    ├────────────────┤   ├───────────────┤  │
│  │ • Game Manager  │    │ • Player State │   │ • Bullet Pool │  │
│  │ • Scene Manager │    │ • Input System │   │ • Enemy Pool  │  │
│  │ • Audio Manager │    │ • Physics Mgr  │   │ • VFX System  │  │
│  │ • UI Manager    │    │ • Camera Ctrl  │   │ • Audio Mgr   │  │
│  └─────────────────┘    └────────────────┘   └───────────────┘  │
│           │                     │                    │          │
├───────────┴─────────────────────┴────────────────────┴──────────┤
│                                                                 │
│              Input System (New Input System Package)            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 核心设计原则

#### **SOLID 原则应用**

1. **单一职责原则 (SRP)**
   - `PlayerMovement` → `PlayerMovementController` (仅处理移动逻辑)
   - `PlayerEnergySystem` (独立能量管理)
   - `PlayerStateManager` (独立状态管理)
   - `PlayerCombatSystem` (独立战斗逻辑)

2. **开闭原则 (OCP)**
   - 使用接口定义扩展点
   - 事件系统允许新功能订阅而无需修改现有代码

3. **里氏替换原则 (LSP)**
   - 所有敌人继承 `IEnemy` 接口
   - 所有投射物继承 `IProjectile` 接口

4. **接口隔离原则 (ISP)**
   - `IMoveable`, `ICollideable`, `IHealable` 等小接口
   - 类只实现需要的接口

5. **依赖倒置原则 (DIP)**
   - 依赖于抽象 (接口)，而不是具体实现
   - 使用 VContainer 注入依赖

---

## 🎯 第三部分：核心模块详解

### 3.1 依赖注入容器 (ServiceLocator)

**文件位置**: `Assets/Scripts/Framework/ServiceLocator.cs`

**设计目的**:
- 替代 `FindGameObjectWithTag`、硬编码单例
- 统一的服务注册和获取机制
- 便于单元测试 (Mock 服务)

**使用示例**:
```csharp
// 注册服务
ServiceLocator.Register<IPlayerService>(playerService);

// 获取服务
var playerService = ServiceLocator.Get<IPlayerService>();

// 注销服务
ServiceLocator.Unregister<IPlayerService>();
```

### 3.2 事件系统 (EventBus)

**文件位置**: `Assets/Scripts/Framework/EventBus.cs`

**设计目的**:
- 解耦各系统间的通信
- 支持弱引用，防止内存泄漏
- 类型安全的事件订阅

**关键事件**:
```csharp
// 玩家事件
public class PlayerDamagedEvent { public int Damage; }
public class PlayerDiedEvent { }
public class PlayerEnergyChangedEvent { public float NewEnergy; }

// 战斗事件
public class EnemyDefeatedEvent { public IEnemy Enemy; }
public class BulletFiredEvent { public Vector2 Direction; }

// 游戏状态事件
public class GamePausedEvent { }
public class GameResumedEvent { }
public class LevelCompleteEvent { public int LevelIndex; }
```

### 3.3 配置管理 (ConfigManager)

**文件位置**: `Assets/Scripts/Framework/Config/GameConfig.cs` (ScriptableObject)

**数据结构**:
```csharp
[System.Serializable]
public class PlayerConfig
{
    public float MoveSpeed = 6.2f;
    public float DashDistance = 3.9f;
    public float DashCooldown = 0.2f;
    public float MaxEnergy = 80f;
    public float EnergyRechargeRate = 2f;
    // ... 其他参数
}

[System.Serializable]
public class EnemyConfig
{
    public float ShootInterval = 1.8f;
    public int BulletCount = 8f;
    public float BulletSpeed = 11.8f;
    // ...
}

public class GameConfig : ScriptableObject
{
    public PlayerConfig Player;
    public EnemyConfig Enemy;
    public CameraConfig Camera;
    public AudioConfig Audio;
    // ...
}
```

### 3.4 玩家系统 (Player Module)

**重构前后对比**:

| 重构前 | 重构后 |
|-------|-------|
| `Player.cs` (空壳) + `PlayerMovement.cs` (600+ 行) | 6-8 个单一职责类 |
| 混乱的状态管理 | 清晰的状态机 (`PlayerStateMachine`) |
| 硬编码参数 | 配置驱动 |
| 直接调用其他系统 | 事件驱动通信 |

**新的玩家架构**:
```
PlayerRoot (MonoBehaviour)
├── PlayerMovementController (实现IMoveable)
│   ├── 移动逻辑 (方向、速度)
│   ├── 碰撞检测
│   └── 动画同步
├── PlayerEnergySystem (能量管理)
│   ├── 能量储存
│   ├── 充能/消耗
│   └── 发送 PlayerEnergyChangedEvent
├── PlayerStateMachine (状态管理)
│   ├── IdleState
│   ├── MovingState
│   ├── DashingState
│   ├── TimeslowState
│   └── DeadState
├── PlayerCombatSystem (战斗逻辑)
│   ├── 攻击判定
│   ├── 伤害处理
│   └── 发送 EnemyDefeatedEvent
├── PlayerInputHandler (输入处理)
│   └── 仅转发输入，不处理逻辑
└── PlayerHealthSystem (生命值)
    ├── 当前血量
    └── 发送 PlayerDamagedEvent
```

### 3.5 敌人系统 (Enemy Module)

**改进**:
- 基类 `EnemyBase` 实现 `IEnemy` 接口
- 独立的行为系统 (Behavior Tree / 状态机)
- 对象池管理
- 事件驱动的击杀/复活

**类结构**:
```
EnemyPool (对象池)
├── 预留 10-20 个敌人实例
├── Spawn/Despawn 方法
└── 自动管理生命周期

EnemyBase (抽象基类)
├── Health System
├── AI Behavior
├── Shooting System
└── 发送 EnemyDefeatedEvent

ShooterEnemy (具体实现)
├── 继承 EnemyBase
├── 围绕玩家旋转
└── 定时射击
```

### 3.6 投射物系统 (Projectile Module)

**改进**:
- 对象池替代 Instantiate/Destroy
- 统一接口 `IProjectile`
- 性能提升 10 倍以上

**实现**:
```csharp
public interface IProjectile
{
    void Launch(Vector2 position, Vector2 direction, float speed);
    void Return();  // 返回池
    Vector2 Position { get; }
}

public class BulletPool : MonoBehaviour
{
    private Queue<Bullet> availableBullets;
    
    public void Prewarm(int count)  // 预热池
    {
        for (int i = 0; i < count; i++)
        {
            var bullet = Instantiate(bulletPrefab);
            availableBullets.Enqueue(bullet);
        }
    }
    
    public IProjectile GetBullet() { /* ... */ }
    public void ReturnBullet(IProjectile bullet) { /* ... */ }
}
```

---

## ⚡ 第四部分：性能优化方案

### 4.1 内存管理

| 技术 | 收益 | 实施难度 |
|------|------|--------|
| **对象池 (Object Pool)** | 减少 GC 次数 80% | ⭐⭐ |
| **组件缓存** | 消除 GetComponent 调用 | ⭐ |
| **值类型 (struct)** | 减少堆分配 | ⭐⭐ |
| **内存预热 (Prewarm)** | 消除运行时卡顿 | ⭐ |

### 4.2 渲染优化

```csharp
// 2D 物体分层策略
public enum SortingLayer
{
    Background = 0,    // -10
    TilemapBase = 1,   // 0
    Enemy = 2,         // 10
    Player = 3,        // 20
    Projectile = 4,    // 30
    UI = 5,            // 100
}

// Canvas.sortingOrder 应设置为 100，确保 UI 始终在顶层
```

### 4.3 物理优化

```csharp
// 使用 Physics2D.OverlapCircle 替代逐帧检测
private bool CheckPerfectDash()
{
    Collider2D[] bulletsInRange = Physics2D.OverlapCircle(
        transform.position, 
        perfectDashDetectRange,
        bulletLayer
    );
    return bulletsInRange.Length > 0;
}
```

---

## 📝 第五部分：迁移路线图

### Phase 1: 基础框架 (第 1-2 周)
- [ ] 实现 ServiceLocator
- [ ] 实现 EventBus
- [ ] 创建 GameConfig (ScriptableObject)
- [ ] 重构 GameStateManager

### Phase 2: 玩家系统 (第 3 周)
- [ ] 拆分 PlayerMovement → 6-8 个单一职责类
- [ ] 实现 PlayerStateMachine
- [ ] 迁移所有逻辑到新系统
- [ ] 更新 UI 订阅事件

### Phase 3: 敌人 & 投射物 (第 4 周)
- [ ] 实现 BulletPool / EnemyPool
- [ ] 重构 EnemyController → EnemyBase
- [ ] 集成池系统
- [ ] 性能测试

### Phase 4: 整合 & 优化 (第 5 周)
- [ ] 完整的集成测试
- [ ] 性能基准测试
- [ ] 代码审查
- [ ] 文档更新

---

## 💡 第六部分：代码示例

### 示例 1: 使用 ServiceLocator

**之前 (紧耦合)**:
```csharp
public class EnemyController : MonoBehaviour
{
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = player.GetComponent<PlayerMovement>();  // 硬编码
    }
}
```

**之后 (松耦合)**:
```csharp
public class EnemyController : MonoBehaviour
{
    private IPlayerService playerService;
    
    private void Start()
    {
        playerService = ServiceLocator.Get<IPlayerService>();
    }
}
```

### 示例 2: 事件驱动

**之前 (直接调用)**:
```csharp
// PlayerMovement.cs
public void DealDamageToEnemy(EnemyController enemy)
{
    enemy.TakeDamage(10);  // 紧耦合
}
```

**之后 (事件驱动)**:
```csharp
// PlayerCombatSystem.cs
public void OnPlayerAttackHit(Collider2D enemy)
{
    EventBus.Publish(new PlayerAttackEvent 
    { 
        DamageDealt = 10,
        TargetPosition = enemy.transform.position
    });
}

// EnemyController.cs (订阅)
private void OnEnable()
{
    EventBus.Subscribe<PlayerAttackEvent>(OnPlayerAttack);
}

private void OnPlayerAttack(PlayerAttackEvent evt)
{
    if (Vector2.Distance(transform.position, evt.TargetPosition) < 1f)
        TakeDamage(evt.DamageDealt);
}
```

### 示例 3: 状态机

```csharp
public class PlayerStateMachine : MonoBehaviour
{
    private Dictionary<Type, IPlayerState> states = new();
    private IPlayerState currentState;
    
    private void Start()
    {
        states[typeof(IdleState)] = new IdleState(this);
        states[typeof(MovingState)] = new MovingState(this);
        states[typeof(DashingState)] = new DashingState(this);
        
        TransitionTo<IdleState>();
    }
    
    public void TransitionTo<T>() where T : IPlayerState
    {
        currentState?.OnExit();
        currentState = states[typeof(T)];
        currentState.OnEnter();
    }
    
    private void Update()
    {
        currentState?.Update();
    }
}

public interface IPlayerState
{
    void OnEnter();
    void Update();
    void OnExit();
}
```

### 示例 4: 对象池

```csharp
public class BulletPool : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int poolSize = 100;
    
    private Queue<Bullet> availableBullets;
    private HashSet<Bullet> activeBullets;
    
    private void Awake()
    {
        availableBullets = new Queue<Bullet>(poolSize);
        activeBullets = new HashSet<Bullet>();
        Prewarm();
    }
    
    private void Prewarm()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var bullet = Instantiate(bulletPrefab, transform);
            bullet.OnReturned += () => ReturnBullet(bullet);
            availableBullets.Enqueue(bullet);
        }
    }
    
    public Bullet GetBullet()
    {
        Bullet bullet;
        if (availableBullets.Count > 0)
        {
            bullet = availableBullets.Dequeue();
        }
        else
        {
            bullet = Instantiate(bulletPrefab, transform);
            bullet.OnReturned += () => ReturnBullet(bullet);
        }
        
        bullet.gameObject.SetActive(true);
        activeBullets.Add(bullet);
        return bullet;
    }
    
    public void ReturnBullet(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
        activeBullets.Remove(bullet);
        availableBullets.Enqueue(bullet);
    }
}
```

---

## 🎓 第七部分：最佳实践

### 7.1 命名规范

| 类型 | 模式 | 示例 |
|------|------|------|
| 接口 | `I{Name}` | `IEnemy`, `IProjectile` |
| 管理器 | `{Name}Manager` | `GameManager`, `AudioManager` |
| 控制器 | `{Name}Controller` | `PlayerController` |
| 系统 | `{Name}System` | `EnergySystem`, `InputSystem` |
| 事件 | `{Name}Event` | `PlayerDamagedEvent` |
| 配置 | `{Name}Config` | `PlayerConfig` |
| 状态 | `{Name}State` | `IdleState`, `DashingState` |

### 7.2 文件组织

```
Assets/Scripts/
├── Framework/                          # 框架层
│   ├── ServiceLocator.cs              # 依赖注入
│   ├── EventBus.cs                    # 事件系统
│   ├── Config/
│   │   ├── GameConfig.cs              # ScriptableObject
│   │   └── ConfigManager.cs           # 配置管理
│   └── Pooling/
│       ├── IPoolable.cs               # 接口
│       └── GenericPool.cs             # 泛型池
│
├── Core/                               # 核心系统
│   ├── GameManager.cs                 # 游戏主管理器
│   ├── SceneManager.cs                # 场景管理
│   ├── AudioManager.cs                # 音频管理
│   └── CameraController.cs            # 摄像机
│
├── Player/                             # 玩家模块
│   ├── PlayerRoot.cs                  # 玩家根组件
│   ├── IPlayerService.cs              # 接口定义
│   ├── Movement/
│   │   ├── PlayerMovementController.cs
│   │   └── IMoveable.cs
│   ├── Combat/
│   │   ├── PlayerCombatSystem.cs
│   │   └── IAttackable.cs
│   ├── Energy/
│   │   └── PlayerEnergySystem.cs
│   ├── Health/
│   │   └── PlayerHealthSystem.cs
│   ├── Input/
│   │   └── PlayerInputHandler.cs
│   └── States/
│       ├── PlayerStateMachine.cs
│       ├── IdleState.cs
│       ├── MovingState.cs
│       ├── DashingState.cs
│       └── TimeslowState.cs
│
├── Enemies/                            # 敌人模块
│   ├── EnemyBase.cs                   # 基类
│   ├── IEnemy.cs                      # 接口
│   ├── ShooterEnemy.cs                # 具体实现
│   ├── EnemyPool.cs                   # 对象池
│   └── AI/
│       └── EnemyBehavior.cs
│
├── Projectiles/                        # 投射物模块
│   ├── Bullet.cs                      # 子弹
│   ├── IProjectile.cs                 # 接口
│   ├── BulletPool.cs                  # 对象池
│   └── ProjectileManager.cs           # 管理器
│
├── Events/                             # 事件定义
│   ├── PlayerEvents.cs
│   ├── EnemyEvents.cs
│   ├── CombatEvents.cs
│   └── GameStateEvents.cs
│
├── UI/                                 # UI 层
│   ├── UIManager.cs
│   ├── EnergyBar.cs
│   ├── HealthBar.cs
│   └── PauseMenu.cs
│
└── Utilities/                          # 工具类
    ├── Constants.cs
    ├── Helpers.cs
    └── Extensions.cs
```

### 7.3 代码审查清单

- [ ] 是否遵循单一职责原则？
- [ ] 是否使用了接口而非具体类？
- [ ] 是否通过事件系统通信而非直接调用？
- [ ] 是否消除了 FindGameObjectWithTag 调用？
- [ ] 是否正确处理了生命周期 (OnEnable/OnDisable)?
- [ ] 是否避免了 Update 中的对象创建？
- [ ] 是否使用了对象池而非 Instantiate/Destroy?
- [ ] 参数是否来自 GameConfig 而非硬编码？

---


## 🔗 参考资源

- [SOLID 原则在游戏开发中的应用](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
- [Unity 官方最佳实践](https://learn.unity.com)
- [Game Programming Patterns](https://gameprogrammingpatterns.com/)
- [Clean Code 读书笔记](https://clean-code-developer.com/)

---