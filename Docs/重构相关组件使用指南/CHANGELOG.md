# Edge-Runner 变更日志

## 📅 最近更新 (2025-01)

### 🐛 Bug 修复

#### 冲刺穿墙修复
- **问题**：玩家冲刺可以穿过墙壁
- **原因**：原算法使用 `Raycast`（细线检测），容易从墙角穿过
- **解决**：改用 `CircleCast` 模拟玩家碰撞体的移动路径
- **相关文件**：`Assets/Modules/Player/Scripts/Systems/PlayerMovement.cs`

```csharp
// 修复后的核心算法
private Vector2 GetSafeDashPosition(Vector2 start, Vector2 target)
{
    RaycastHit2D hit = Physics2D.CircleCast(
        start, playerRadius, direction, totalDistance, wallLayerMask
    );
    if (hit.collider == null) return target;
    return start + direction * (hit.distance - collisionOffset);
}
```

#### 死亡动画位置错误修复
- **问题**：冲刺进入死亡区域时，死亡动画在旧位置播放
- **原因**：`Rigidbody2D.interpolation = Interpolate` 导致 `transform.position` 延迟
- **解决**：在 `FixedUpdate` 中同步设置 `transform.position`

```csharp
// PlayerMovement.cs - FixedUpdate()
rb.MovePosition(targetPos);
transform.position = targetPos;  // 新增：立即同步视觉位置
```

#### Layer 顺序问题
- **问题**：修改 Layer 列表顺序后，物理检测全部失效
- **原因**：Unity 的 Layer 使用索引引用，改顺序会打乱所有对象的 Layer 设置
- **解决**：记录并修复所有场景中受影响对象的 Layer

### ⚙️ 参数调整

| 参数 | 旧值 | 新值 | 说明 |
|------|------|------|------|
| `collisionOffset` | 0.05f | 0.15f | 冲刺安全距离，防止卡墙 |
| `wallCheckExtra` | - | 0.8f | 墙壁检测额外距离 |

### 📁 代码优化

#### 去除重复代码
- **PlayerMovement.cs**：移除 `[SerializeField]` 标记的 `wallLayerMask` 和 `billboardLayerMask`
- 这些字段现在通过 `PlayerController` 的 setter 方法设置，避免重复配置

### 📋 Layer 配置规范

当前 Layer 顺序（**请勿修改**）：
```
0: Default
1: TransparentFX
2: Ignore Raycast
3: (空)
4: Water
5: UI
6: Player
7: Enemy
8: Ground
9: Wall
10: DeathZone
11: Billboard
12: WinZone
```

---

## 🏗️ 架构重构 (2024-12 ~ 2025-01)

### 新架构总览

```
ProjectLifetimeScope (DontDestroyOnLoad)
├── GameStateManager (IGameStateManager)
├── AudioManager (IAudioManager)
├── ConfigManager
└── PoolManager

GameLifetimeScope (每场景)
├── Player (IPlayerService)
│   ├── PlayerController (协调器)
│   ├── PlayerStateMachine (状态机)
│   ├── PlayerMovement (移动/冲刺)
│   ├── PlayerEnergySystem (能量)
│   ├── PlayerHealthSystem (生命)
│   ├── PlayerCombatSystem (战斗)
│   └── PlayerInputHandler (输入)
├── CameraController
└── EnemyController[]
```

### 核心改进

| 方面 | 重构前 | 重构后 |
|------|--------|--------|
| 依赖管理 | 硬编码/FindObject | VContainer 依赖注入 |
| 配置管理 | 分散在各脚本 Inspector | 集中到 GameConfig SO |
| 系统通信 | 直接引用 | EventBus 事件驱动 |
| 对象创建 | Instantiate/Destroy | 对象池复用 |
| 代码结构 | 单文件 600+ 行 | 职责单一，多文件 |

### 新增系统

1. **EventBus** - 发布订阅事件系统
2. **GenericPool** - 泛型对象池
3. **ConfigManager** - 集中配置管理
4. **PlayerStateMachine** - 玩家状态机

---

## 🎮 战斗系统 (新增)

### 连击系统

| 连击 | 名称 | 伤害倍率 | 速度倍率 |
|------|------|----------|----------|
| 1 | 右斩 | 1.0x | 1.0x |
| 2 | 左斩 | 1.2x | 1.1x |
| 3 | 横扫 | 1.5x | 0.8x |

### 新增文件
```
Assets/Modules/Player/Scripts/Combat/
├── ComboSystem.cs      # 连击状态管理
├── SwordHitbox.cs      # 剑碰撞检测
└── SlashTrail.cs       # 挥砍拖尾特效
```

---

## 📂 目录结构

```
Assets/
├── Core/Scripts/
│   ├── Framework/
│   │   ├── GameLifetimeScope.cs
│   │   ├── ProjectLifetimeScope.cs
│   │   ├── IPlayerService.cs
│   │   ├── Events/EventBus.cs
│   │   ├── Config/ConfigManager.cs
│   │   └── Pooling/PoolManager.cs
│   └── Manager/
│       ├── GameStateManager.cs
│       ├── AudioManager.cs
│       └── CameraController.cs
│
└── Modules/
    ├── Player/Scripts/
    │   ├── States/PlayerController.cs
    │   ├── Systems/PlayerMovement.cs
    │   └── Combat/
    ├── Enemies/Scripts/
    └── Bullet/Scripts/
```

---

## ⚠️ 注意事项

1. **不要修改 Layer 顺序** - 会导致所有物理检测失效
2. **Ground 不需要 Collider** - 只是视觉层，不参与碰撞
3. **Player 的 Rigidbody2D 必须是 Dynamic** - 不要设为 Kinematic
4. **配置修改只在 GameConfig** - 不要在组件 Inspector 中配置参数
