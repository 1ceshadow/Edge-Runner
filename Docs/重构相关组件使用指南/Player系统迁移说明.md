# Player 系统迁移说明

> 目的：将旧版 PlayerMovement/PlayerWallCollision 等脚本拆分为可测试、可注入的子系统（Input/Movement/Energy/Health/Combat），并让所有状态逻辑统一走 PlayerController + StateMachine。

## 已移除脚本

| 脚本 | 原职责 | 替代方案 |
| --- | --- | --- |
| `Assets/Modules/Player/Scripts/PlayerMovement.cs` | 输入采集、位移、冲刺、能量衰减全部耦合在同一类中 | `Systems/PlayerInputHandler` 提供事件，`Systems/PlayerMovementController` 负责位移/冲刺，`Systems/PlayerEnergySystem` 负责消耗/奖励 |
| `Assets/Modules/Player/Scripts/PlayerWallCollision.cs` | 冲刺时的墙体阻挡检测、位置修正 | `Systems/PlayerMovementController` 内部使用多条 Raycast 过滤墙体，`GetSafeDashPosition` 统一返回安全坐标 |

> 若场景/Prefab 仍引用上述脚本，Unity 会在打开 prefab 时提示 Missing。请改挂新的 Systems 脚本。

## 新结构速览

```
Assets/Modules/Player/Scripts/
├── PlayerController.cs
├── Player.cs (IPlayerService 实现)
├── PlayerDeathHandler.cs
├── Systems/
│   ├── PlayerInputHandler.cs
│   ├── PlayerMovementController.cs
│   ├── PlayerEnergySystem.cs
│   ├── PlayerHealthSystem.cs
│   └── PlayerCombatSystem.cs
└── States/
    ├── PlayerStateMachine.cs
    ├── PlayerIdleState.cs
    ├── PlayerMovingState.cs
    ├── PlayerDashingState.cs
    └── PlayerTimeSlowState.cs
```

- **PlayerController**：在 `Awake` 内装配全部 Systems，并把引用注入到 `PlayerStateMachine`。
- **Systems**：保持单一职责，可被独立禁用（例如死亡时由 `PlayerDeathHandler` 统一开关）。
- **States**：只关注状态切换与调度 Systems，不再直接访问 Unity Input。

## 迁移步骤

1. **Prefab 清理**：打开 `Assets/Modules/Player/Player.prefab`，移除遗留的 `PlayerMovement`、`PlayerWallCollision` 组件，确认新 Systems 全部挂载。
2. **脚本引用**：查找 `PlayerMovement` 或 `PlayerWallCollision` 字符串；如有引用，改为基于 `PlayerController` 暴露的属性（`MovementController`、`EnergySystem` 等）。
3. **事件改造**：攻击、能量相关 UI/特效需要通过 EventBus 监听 `PlayerEnergySystem` 发出的事件（详见 `Docs/重构相关组件使用指南/EVENT_SYSTEM_GUIDE.md`）。
4. **DI 接入**：所有需要玩家状态的对象应注入 `IPlayerService` 并通过 `TryGetComponent` 获取具体系统，避免 `GameObject.Find`。

## 注意事项

- Player 状态机依赖 `PlayerInputHandler` 事件。如果新增状态，需要在 `PlayerStateMachine` 中注册相应回调，并在 `OnDisable` 记得解除订阅。
- `PlayerEnergySystem` 通过 `Update` Tick 驱动，请保证 `PlayerController` 激活状态与游戏暂停逻辑同步。
- 若新增可实例化的敌人/子弹，需要在生成后调用 `resolver.Inject(instance)` 让其获取 `IPlayerService`。
- `PlayerVisibilityMesh` 已改为按 Sorting Layer 名称回退到 SpriteRenderer，如需自定义层级，直接在组件里填字符串即可。

## 验收清单

- Player prefab 不再报 Missing Script。
- Game 中冲刺/子弹检测正常，墙体阻挡由新 MovementController 处理。
- Energy、Health、Combat 等 UI/特效均通过新系统事件驱动。
- 场景加载日志出现 `✓ Player systems initialized`（来自 `PlayerController`）。
