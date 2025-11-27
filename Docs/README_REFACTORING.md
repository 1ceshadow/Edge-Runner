

## 🎯 核心改进概览

### 问题 → 解决方案

| 问题 | 原因 | 解决方案 | 文件 |
|------|------|--------|------|
| 单一职责混乱 | PlayerMovement.cs 600+ 行 | 拆分为 8 个类 | Player/* |
| 硬编码依赖 | FindGameObjectWithTag() | ServiceLocator DI | Framework/* |
| 状态管理混乱 | 无状态机 | PlayerStateMachine | Player/PlayerStateMachine.cs |
| 系统耦合高 | 直接调用 | EventBus 事件系统 | Framework/EventBus.cs |
| 对象池缺失 | Instantiate/Destroy | GenericPool<T> | Framework/Pooling/* |
| 参数硬编码 | 数值分散 | GameConfig ScriptableObject | Framework/Config/* |
| UI 与逻辑混合 | 直接访问 | 事件驱动 UI | Framework/EventBus.cs |

---

## 📁 脚本文件参考结构

```
Assets/
├── Framework/                    ✨ 新增 - 游戏框架层
│   ├── ServiceLocator.cs        # DI 容器
│   ├── EventBus.cs              # 事件系统
│   ├── Config/
│   │   └── GameConfig.cs        # 配置管理 (ScriptableObject)
│   └── Pooling/
│       └── GenericPool.cs       # 泛型对象池
│
├── Core/
│   ├── Interfaces.cs            ✨ 新增 - 所有接口定义
│   ├── GameStateManager.cs      # 已有，可继续使用
│   └── ...
│
├── Player/
│   ├── PlayerRoot.cs            ✨ 新增 - 玩家根组件
│   ├── PlayerMovementController.cs    ✨ 新增 - 单一职责
│   ├── PlayerEnergySystem.cs    ✨ 新增 - 能量管理
│   ├── PlayerHealthSystem.cs    ✨ 新增 - 血量管理
│   ├── PlayerCombatSystem.cs    ✨ 新增 - 战斗系统
│   ├── PlayerInputHandler.cs    ✨ 新增 - 输入处理
│   ├── PlayerStateMachine.cs    ✨ 新增 - 状态机
│   ├── Player.cs                # 原文件，可删除
│   ├── PlayerMovement.cs        # 原文件，可删除
│   └── ...
│
├── Enemies/
│   ├── EnemyBase.cs             ✨ 新增 - 敌人基类
│   ├── ShooterEnemy.cs          ✨ 新增 - 射手敌人
│   ├── EnemyPool.cs             ✨ 新增 - 敌人对象池
│   ├── EnemyController.cs       # 原文件，可删除
│   └── ...
│
├── Bullet/
│   ├── Bullet.cs                ✨ 新增 - 子弹类
│   ├── BulletPool.cs            ✨ 新增 - 子弹对象池
│   ├── BulletController.cs      # 原文件，可删除
│   └── ...
│
├── Events/                       ✨ 新增 - 事件定义（或在 EventBus.cs 中定义）
│   └── GameEvents.cs
│
└── UI/
    ├── EnergyBar.cs             
    ├── HealthBar.cs             # 改进版 - 使用事件
    └── ...
```

---

## 🔄 迁移路线图

### Week 1: 框架搭建 (基础层)
- [ ] 日 1-2: 实现 ServiceLocator + EventBus
- [ ] 日 3: 创建 GameConfig 和对象池基类
- [ ] 日 4: 创建接口定义
- [ ] 日 5: 测试框架工作

**交付物**: `Assets/Scripts/Framework/` 完全可用

### Week 2: 玩家系统 (核心功能)
- [ ] 日 1: 拆分 PlayerMovement 为 8 个类
- [ ] 日 2: 实现状态机
- [ ] 日 3: 更新输入处理
- [ ] 日 4-5: 集成测试和调整

**交付物**: 玩家系统完全重构并工作

### Week 3: 敌人 & 子弹 (游戏玩法)
- [ ] 日 1: 创建 EnemyBase 和 ShooterEnemy
- [ ] 日 2: 创建对象池
- [ ] 日 3: 迁移旧逻辑
- [ ] 日 4-5: 性能测试

**交付物**: 敌人系统可用，性能提升 50%+

### Week 4: UI & 整合 (最终阶段)
- [ ] 日 1-2: 更新 UI 系统使用事件
- [ ] 日 3: 完整集成测试
- [ ] 日 4: 代码审查和文档
- [ ] 日 5: 性能基准测试

**交付物**: 完整的重构项目准备发布

---

## 💡 关键设计决策

### 1. 为什么使用 ServiceLocator?
- **替代**: `FindGameObjectWithTag()` + 硬编码单例
- **优点**: 松耦合、易测试、易维护
- **示例**: `ServiceLocator.Get<IPlayerService>()`

### 2. 为什么使用 EventBus?
- **替代**: 直接调用系统方法 (`enemy.TakeDamage()`)
- **优点**: 完全解耦、易扩展、支持多订阅者
- **示例**: `EventBus.Publish(new PlayerDamagedEvent())`

### 3. 为什么使用对象池?
- **替代**: `Instantiate()` / `Destroy()`
- **优点**: 消除 GC 压力，减少卡顿，性能提升 10 倍
- **示例**: `bulletPool.GetBullet()` 而非 `Instantiate()`

### 4. 为什么使用 ScriptableObject 配置?
- **替代**: 硬编码参数 `moveSpeed = 6.2f`
- **优点**: 不编译代码即可调整参数，版本管理
- **示例**: 在 Inspector 中调整数值，立即看到效果

### 5. 为什么使用状态机?
- **替代**: 混乱的 bool 标志 (`isDashing`, `isTimeSlowed` 等)
- **优点**: 清晰的状态转换，易维护
- **示例**: `PlayerStateMachine.TransitionTo<DashingState>()`

---

## ⚠️ Rendering 注意：`PlayerVisibilityMesh` 的 sorting layer 使用

`PlayerVisibilityMesh` 在运行时需要一个有效的 Sorting Layer id。Unity 要求传入的是该 layer 的唯一 id（由 `SortingLayer.NameToID(name)` 提供），而不是列表中的索引值。

- 推荐在 Inspector 中填写 `sortingLayerName` 字段（例如 `Default`、`Player`、`Foreground`），不要填写数字索引。
- 脚本会优先尝试复制场景中相邻 `SpriteRenderer` 的 `sortingLayerID`（如果存在），以保证在多数场景下无需手动配置。
- 如果控制台出现警告 `Invalid layer id. Please use the unique id of the layer (which is not the same as its index in the list).`，请把该组件的层设置从整数改为名称，或为 `sortingLayerName` 填写正确的层名。

示例：在 `PlayerVisibilityMesh` 的 Inspector 中填写 `Player`（如果你的排序层名为 `Player`），而不是填写 `5` 或其他数字。

---



## 🎓 学习资源

### 架构模式
- **ServiceLocator Pattern**: 简化依赖管理
- **Event-Driven Architecture**: 解耦系统通信
- **State Machine Pattern**: 清晰的状态管理
- **Object Pool Pattern**: 性能优化

### 游戏开发最佳实践
- **SOLID 原则**: 代码质量指引
- **单一职责**: 易维护、易测试
- **接口编程**: 灵活扩展
- **配置驱动**: 易调整平衡

---

## ⚠️ 常见陷阱

### 1. 过度使用 ServiceLocator
```csharp
// ❌ 不要注册所有东西
ServiceLocator.Register<PlayerHealth>(health);
ServiceLocator.Register<PlayerEnergy>(energy);
ServiceLocator.Register<PlayerMovement>(movement);

// ✅ 只注册全局服务
ServiceLocator.Register<IPlayerService>(playerRoot);
```

### 2. 在 Update 中频繁发布事件
```csharp
// ❌ 不好
private void Update()
{
    EventBus.Publish(new PlayerMovedEvent());  // 每帧发布!
}

// ✅ 好
private Vector2 lastPos;
private void Update()
{
    if (transform.position != lastPos)
    {
        EventBus.Publish(new PlayerMovedEvent());
        lastPos = transform.position;
    }
}
```

### 3. 忘记取消订阅事件
```csharp
// ❌ 内存泄漏
private void OnEnable()
{
    EventBus.Subscribe<DamageEvent>(OnDamage);
}
// 缺少 OnDisable!

// ✅ 正确
private void OnDisable()
{
    EventBus.Unsubscribe<DamageEvent>(OnDamage);
}
```
