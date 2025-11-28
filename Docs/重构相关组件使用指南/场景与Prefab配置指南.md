# 场景与 Prefab 配置指南

本文档汇总 Edge-Runner 项目中所有关键场景配置步骤与 Prefab 最佳实践，确保新手快速上手、老手避免踩坑。

---

## 1. 启动场景配置（Mainmenu）

启动场景是全局服务的唯一初始化入口，需包含以下 GameObject：

| GameObject 名称 | 关键组件 | 说明 |
|----------------|---------|------|
| `ProjectLifetimeScope` | `ProjectLifetimeScope`, `VContainer.Unity.LifetimeScope` | DontDestroyOnLoad，挂载全局管理器引用 |
| `GameStateManager` | `GameStateManager` | 暂停/胜利/死亡流程 |
| `AudioManager` | `AudioManager`, `AudioSource` | BGM 控制 |
| `ConfigManager` | `ConfigManager` | 挂载 `GameConfig` ScriptableObject |
| `PoolManager` | `PoolManager` | 子弹/敌人对象池，需指定 `bulletPrefab` |
| `BulletManager` | `BulletManager` | 极限闪避检测用的活跃子弹追踪 |
| `BulletService` | `BulletService` | 统一子弹生成入口 |

### ProjectLifetimeScope Inspector 配置

确保 Inspector 中以下字段已赋值：

```
Game State Manager  → 拖入 GameStateManager GameObject
Audio Manager       → 拖入 AudioManager GameObject
Bullet Service      → 拖入 BulletService GameObject
Bullet Manager      → 拖入 BulletManager GameObject
Pool Manager        → 拖入 PoolManager GameObject
```

### 验收日志

运行后 Console 应出现：
```
✓ VContainer: GameStateManager 已注册
✓ VContainer: AudioManager 已注册
✓ VContainer: BulletService 已注册
✓ VContainer: BulletManager 已注册
✓ VContainer: PoolManager 已注册
✓ ConfigManager: 游戏配置已加载
✓ PoolManager: 子弹池初始化完成，容量 100
```

---

## 2. 游戏关卡配置（Level0, Level1, …）

每个关卡场景需包含：

| GameObject 名称 | 关键组件 | 说明 |
|----------------|---------|------|
| `GameLifetimeScope` | `GameLifetimeScope` | 场景级 DI，自动发现并注入 Player、Camera、Enemy |
| `Player` | `Player`, `PlayerController`, 各 Systems | 玩家根对象 |
| `Main Camera` | `CameraController` | 跟随玩家 |
| `WinTrigger` | `WinTrigger`, `Collider2D (Trigger)` | 胜利检测区域 |
| `Enemies` | 各 `EnemyController` | 敌人（FindObjectsByType 自动注入） |

### GameLifetimeScope 自动注入流程

`GameLifetimeScope.Configure` 会执行：
1. 找到 `Player` 并注册为 `IPlayerService`
2. 找到所有 `CameraController`、`EnemyController`、`WinTrigger` 并逐一调用 `resolver.Inject`
3. 对 Player 上的 `PlayerDeathHandler`、`PlayerHealthSystem`、`PlayerStateMachine` 注入全局服务

### 验收日志

```
✓ VContainer: 场景服务注册完成 Player:1 Camera:1 Enemies:5 WinTriggers:1
✓ PlayerController: 配置已加载
✓ PlayerStateMachine: 初始化完成，注册了 4 个状态
✓ CameraController: 玩家服务已注入
✓ EnemyController: 已通过 VContainer 获取玩家服务
```

---

## 3. Prefab 配置指南

### 3.1 Player Prefab

路径：`Assets/Modules/Player/Player.prefab`

必须挂载组件：
- `Player`（IPlayerService 实现）
- `PlayerController`
- `PlayerStateMachine`
- `PlayerInputHandler`
- `PlayerMovementController`
- `PlayerEnergySystem`
- `PlayerHealthSystem`
- `PlayerCombatSystem`
- `PlayerDeathHandler`
- `Rigidbody2D`（Dynamic, 无重力, FreezeRotation）
- `CircleCollider2D`
- `AudioSource`

Inspector 配置要点：
- `PlayerController.wallLayerMask` 设置为包含墙体的 Layer
- `PlayerCombatSystem.attackCollider` 指定攻击碰撞体
- `PlayerCombatSystem.enemyLayerMask` 设置为 Enemy Layer

### 3.2 PoolableBullet Prefab

路径：`Assets/Modules/Bullet/Prefabs/PoolableBullet.prefab`

必须挂载组件：
- `PoolableBullet`
- `Rigidbody2D`（Kinematic 或 Dynamic, 无重力）
- `Collider2D`
- `SpriteRenderer`

Tag 设置：根据友军判断设置为 `Bullet` 或自定义

### 3.3 Enemy Prefab

路径：`Assets/Modules/Enemies/Prefabs/*.prefab`

必须挂载组件：
- `EnemyController`
- `Rigidbody2D`
- `Collider2D`
- `SpriteRenderer`

Tag 设置：`Enemy`

注意：无需手动配置 `bulletPrefab`，敌人射击通过注入的 `IBulletService` 实现。

---

## 4. Layer 与 Tag 规范

### Layer

| Layer 名称 | 用途 |
|-----------|------|
| Default | 默认 |
| Player | 玩家碰撞 |
| Enemy | 敌人碰撞 |
| Bullet | 子弹碰撞 |
| Wall | 墙体/掩体 |
| DeathZone | 死亡区域 |

### Tag

| Tag 名称 | 用途 |
|---------|------|
| Player | 玩家识别 |
| Enemy | 敌人识别 |
| Cover | 掩体识别（子弹碰撞消失） |

---

## 5. 常见问题排查

### Q: 敌人不射击或报 `BulletService 未注入`
**A**: 检查 `ProjectLifetimeScope` 是否正确引用 `BulletService`，并确保从 Mainmenu 场景启动游戏。

### Q: 玩家移动异常或参数不生效
**A**: 检查 `ConfigManager` 是否挂载了 `GameConfig`，Console 是否有「配置已加载」日志。

### Q: Missing Script 报错
**A**: 检查 Prefab 是否仍引用已删除的 `PlayerMovement`、`PlayerWallCollision`、`BulletController`，替换为新系统组件。

### Q: 极限闪避不触发
**A**: 确保 `BulletManager` 存在且 `PoolableBullet.OnSpawn` 正常调用 `RegisterBullet`。

### Q: 时缓能量不消耗
**A**: 检查 `PlayerController.Update` 是否调用了 `Energy.Tick(IsTimeSlowed)`。

---

## 6. 调试工具

在场景中添加以下组件可获得实时调试面板：

- `Assets/Core/Scripts/Debug/DebugStats.cs` → F1 切换，显示能量、对象池、战斗统计
- `Assets/Core/Scripts/Debug/PerformanceMonitor.cs` → 显示帧时间、内存、GC 次数

这两个组件也会通过 `GameLifetimeScope` 自动注入 `IPoolManager` 和 `IBulletManager`（如果需要的话）。

---

## 7. 检查清单

### 启动场景 Checklist

- [x] `ProjectLifetimeScope` 存在且 DontDestroyOnLoad
- [x] 所有全局管理器引用已赋值
- [x] `ConfigManager.GameConfig` 已设置
- [x] `PoolManager.bulletPrefab` 已设置

### 关卡场景 Checklist

- [x] `GameLifetimeScope` 存在
- [x] `Player` Prefab 实例存在
- [x] `Main Camera` 带有 `CameraController`
- [x] 至少一个 `WinTrigger` 存在
- [x] 敌人带有 `EnemyController` 且 Tag 为 `Enemy`
