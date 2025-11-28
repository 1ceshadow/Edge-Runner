# 重构文档索引

## 📋 核心文档

| 文档 | 用途 |
|------|------|
| [CHANGELOG.md](CHANGELOG.md) | **变更日志** - 最近修复、参数调整、架构改动 |
| [常见问题排查.md](常见问题排查.md) | **故障排除** - Layer/Rigidbody/Collider 问题 |

## 📖 详细指南

| 文档 | 内容 |
|------|------|
| [重构完整报告.md](重构完整报告.md) | 完整架构说明，各系统详解 |
| [场景设置指南.md](场景设置指南.md) | 场景配置步骤，LifetimeScope 设置 |
| [剑攻击系统配置指南.md](剑攻击系统配置指南.md) | 战斗系统配置，连击设置 |

## 🔧 参考资料

| 文档 | 内容 |
|------|------|
| [VContainer使用指南.cs](VContainer使用指南.cs) | 依赖注入代码示例 |
| [EVENT_SYSTEM_GUIDE.md](EVENT_SYSTEM_GUIDE.md) | 事件系统 API |

---

## ⚡ 快速查询

### 遇到问题？

1. **冲刺穿墙** → 检查 `wallLayerMask` 是否包含 Wall 层
2. **死亡不触发** → 检查 DeathZone 的 Layer 是否正确
3. **玩家不动** → 检查 Rigidbody2D Body Type 是否为 Dynamic
4. **物理全失效** → Layer 顺序可能被改了，见 [常见问题排查.md](常见问题排查.md)

### 添加新功能？

1. **新状态** → 继承 `PlayerStateBase`，在 `PlayerStateMachine` 注册
2. **新事件** → 在 `GameEvents.cs` 定义，用 `EventBus.Publish/Subscribe`
3. **新配置** → 在 `GameConfig.cs` 添加字段，通过 `ConfigManager` 访问

### 关键路径

```
配置文件: Assets/Core/Scripts/Framework/Config/GameConfig.cs
玩家移动: Assets/Modules/Player/Scripts/Systems/PlayerMovement.cs
状态机:   Assets/Modules/Player/Scripts/States/PlayerStateMachine.cs
事件总线: Assets/Core/Scripts/Framework/Events/EventBus.cs
```