# VContainer 设置步骤

## 📦 1. 安装 VContainer

### 方法 A: 通过 Package Manager（推荐）
1. 打开 Unity Editor
2. 菜单: `Window > Package Manager`
3. 点击左上角 `+` 按钮
4. 选择 `Add package from git URL...`
5. 输入: `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer`
6. 点击 `Add`
7. 等待安装完成

### 方法 B: 编辑 manifest.json
1. 打开 `Packages/manifest.json`
2. 在 `dependencies` 中添加:
```json
{
  "dependencies": {
    "jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer"
  }
}
```
3. 保存文件，Unity 会自动安装

---

## 🎯 2. 设置 ProjectLifetimeScope（全局作用域）

### 在启动场景中创建
1. 打开你的第一个场景（如 `0Mainmenu`）
2. 创建空 GameObject: `右键 Hierarchy > Create Empty`
3. 命名为 `ProjectLifetimeScope`
4. 添加组件: `Add Component > ProjectLifetimeScope`
5. **重要**: 在 Inspector 中，找到 `ProjectLifetimeScope` 组件
6. 设置 `Parent` 为 `DontDestroyOnLoad` 或勾选相应选项

### 配置全局服务
1. 确保场景中有 `GameStateManager` GameObject
2. 确保场景中有 `AudioManager` GameObject
3. 在 `ProjectLifetimeScope` 组件中:
   - 拖入 `GameStateManager` 到 `Game State Manager` 字段
   - 拖入 `AudioManager` 到 `Audio Manager` 字段

结构应该如下:
```
Scene: 0Mainmenu
├── ProjectLifetimeScope (DontDestroyOnLoad)
│   └── ProjectLifetimeScope (Component)
│       ├── Game State Manager: GameStateManager
│       └── Audio Manager: AudioManager
├── GameStateManager
└── AudioManager
```

---

## 🎮 3. 为每个游戏场景添加 GameLifetimeScope

### 对于每个关卡场景（Level0, Level1, Level2...）

1. 打开场景（如 `Level0`）
2. 创建空 GameObject: `右键 Hierarchy > Create Empty`
3. 命名为 `GameLifetimeScope`
4. 添加组件: `Add Component > GameLifetimeScope`
5. 确保场景中有:
   - ✅ Player GameObject
   - ✅ Main Camera（带 CameraController）
   - ✅ 敌人（带 EnemyController）

### 自动配置
`GameLifetimeScope` 会自动:
- 查找并注册 `Player` 组件为 `IPlayerService`
- 注册场景中所有 `CameraController`
- 注册场景中所有 `EnemyController`

结构应该如下:
```
Scene: Level0
├── GameLifetimeScope
│   └── GameLifetimeScope (Component)
├── Player
├── Main Camera
│   └── CameraController (Component)
└── Enemies
    ├── Enemy1 (EnemyController)
    └── Enemy2 (EnemyController)
```

---

## ✅ 4. 验证设置

### 检查清单

#### ProjectLifetimeScope
- [ ] 在启动场景中创建
- [ ] 设置为 DontDestroyOnLoad
- [ ] GameStateManager 引用已设置
- [ ] AudioManager 引用已设置

#### GameLifetimeScope
- [ ] 每个游戏场景都有
- [ ] 场景中有 Player GameObject
- [ ] CameraController 存在
- [ ] EnemyController 存在（如果有敌人）

### 运行测试
1. 进入 Play 模式
2. 查看 Console，应该看到:
```
✓ VContainer: GameStateManager 已注册
✓ VContainer: AudioManager 已注册
✓ VContainer: Player 已注册
✓ VContainer: 场景服务已注册完成
✓ CameraController: 玩家服务已注入
✓ EnemyController: 已通过 VContainer 获取玩家服务
```

3. 如果看到错误:
   - `DependencyResolutionException`: 服务未注册或依赖缺失
   - `NullReferenceException`: 检查 LifetimeScope 设置

---

## 🔧 5. 常见问题排查

### 问题 1: "Service not found" 错误
**原因**: LifetimeScope 未正确配置
**解决**:
1. 确认场景中有 GameLifetimeScope
2. 确认 Player 对象存在
3. 确认 ProjectLifetimeScope 设置正确

### 问题 2: 注入的服务为 null
**原因**: 在 Awake 中过早访问
**解决**: 在 Start() 或 [Inject] 构造函数中使用

### 问题 3: 场景切换后服务丢失
**原因**: ProjectLifetimeScope 未设置为 DontDestroyOnLoad
**解决**: 检查 ProjectLifetimeScope 的 Parent 设置

### 问题 4: 编译错误
**原因**: VContainer 包未正确安装
**解决**: 重新安装 VContainer 包

---

## 📁 6. 项目结构建议

```
Assets/
├── Core/
│   └── Scripts/
│       └── Framework/
│           ├── ProjectLifetimeScope.cs
│           ├── GameLifetimeScope.cs
│           ├── IGameStateManager.cs
│           ├── IAudioManager.cs
│           └── IPlayerService.cs
│
├── Scenes/
│   ├── 0Mainmenu.unity (有 ProjectLifetimeScope)
│   └── Levels/
│       ├── Level0.unity (有 GameLifetimeScope)
│       ├── Level1.unity (有 GameLifetimeScope)
│       └── Level2.unity (有 GameLifetimeScope)
│
└── Modules/
    ├── Player/
    ├── Enemies/
    └── ...
```

---

## 🚀 7. 下一步

设置完成后，你可以:

1. ✅ 在任何 MonoBehaviour 中使用 `[Inject]` 注入服务
2. ✅ 创建自定义服务接口和实现
3. ✅ 在 LifetimeScope 中注册新服务
4. ✅ 享受类型安全的依赖注入

参考文档:
- `VContainer使用指南.cs` - 代码示例
- `VContainer重构报告.md` - 详细说明

---

**设置完成！** 🎉
你的项目现在使用企业级的依赖注入系统。
