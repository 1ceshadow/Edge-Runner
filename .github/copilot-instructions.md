# Edge-Runner AI Coding Instructions

## 🎮 项目概述
2D 俯视角跑酷砍杀游戏，Unity 6 (6000.2.9f1)，使用 VContainer DI 框架和新 Input System。

## 🏗️ 架构核心

### VContainer 依赖注入（必须遵守）
```
ProjectLifetimeScope (DontDestroyOnLoad)     ← 全局单例服务
├── IGameStateManager → GameStateManager
└── IAudioManager → AudioManager

GameLifetimeScope (每场景一个)               ← 场景级服务
├── IPlayerService → Player
├── CameraController (通过 RegisterBuildCallback 注入)
└── EnemyController[] (通过 RegisterBuildCallback 注入)
```

**注入模式**：使用 `[Inject]` 标记的 `Construct` 方法，而非字段注入：
```csharp
[Inject]
public void Construct(IPlayerService playerService) {
    this.playerService = playerService;
}
```

**动态生成对象**：运行时 Instantiate 的对象需手动注入：
```csharp
resolver.Inject(newEnemyController);
```

### 目录结构
```
Assets/
├── Core/Scripts/           ← 全局系统
│   ├── Framework/          ← DI 接口和 LifetimeScope
│   ├── Manager/            ← GameStateManager, AudioManager, CameraController
│   └── Input/              ← PlayerInputActions
├── Modules/                ← 功能模块（每个含 Scripts/, Prefabs/, Audio/）
│   ├── Player/             ← Player.cs 实现 IPlayerService
│   ├── Enemies/            ← EnemyController 使用 [Inject]
│   ├── Bullet/
│   └── UI/
└── Art/                    ← 纯资源（Textures/, Sprites/）
```

## 🔧 关键接口

| 接口 | 实现 | 用途 |
|------|------|------|
| `IPlayerService` | `Player` | 获取玩家 Transform/GameObject/组件 |
| `IGameStateManager` | `GameStateManager` | 暂停/胜利/死亡状态控制 |
| `IAudioManager` | `AudioManager` | BGM 播放/暂停/音量 |

**获取玩家组件**（禁止使用 `GameObject.Find`）：
```csharp
if (playerService.TryGetComponent<PlayerMovement>(out var movement)) {
    // 使用 movement
}
```

## 📝 编码规范

- **命名**：文件/类 PascalCase，变量 camelCase，常量 UPPER_SNAKE_CASE
- **括号**：Allman 风格（单独一行）
- **单行条件**：必须加括号
- **Unity API**：使用 `FindFirstObjectByType` / `FindObjectsByType`（非已废弃的 `FindObjectOfType`）

## ⚡ 常见任务

### 添加新敌人类型
1. 继承或复制 `Modules/Enemies/Scripts/EnemyController.cs`
2. 保留 `[Inject] Construct(IPlayerService)` 模式
3. `GameLifetimeScope` 会自动通过 `FindObjectsByType` 发现并注入

### 添加新全局服务
1. 在 `Core/Scripts/Framework/` 定义接口 `IYourService`
2. 在 `ProjectLifetimeScope.Configure()` 中注册：
   ```csharp
   builder.RegisterComponent(yourManager).As<IYourService>();
   ```

### 场景设置检查清单
- [ ] 场景有 `GameLifetimeScope` GameObject
- [ ] 启动场景有 `ProjectLifetimeScope`（设置 DontDestroyOnLoad）
- [ ] Player 存在且有 `Player` 组件
- [ ] Console 显示 `✓ VContainer: 场景服务注册完成`

## 🚫 禁止事项
- ❌ `GameObject.FindGameObjectWithTag("Player")` → 用 `IPlayerService`
- ❌ 直接访问单例 `GameStateManager.Instance` → 用 `IGameStateManager` 注入
- ❌ `FindObjectOfType<T>(bool)` → 用 `FindFirstObjectByType<T>(FindObjectsInactive)`

## 回答规范和生成文档规范
- 用中文回答
- 文档使用 Markdown 格式

## 使用工具
- 尝试使用MCP服务Unity和unityMCP

## 修改权限
- 我们项目正在大改，你可以自由修改和重构现有代码以适应新的架构和需求。
- 对于文档和代码示例中的任何错误或不一致之处，请进行必要的更正和更新。
Context: Path: Docs/
使用指南放在 Docs/重构相关组件使用指南/