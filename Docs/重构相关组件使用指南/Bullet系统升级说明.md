# Bullet 系统升级说明

本次重构聚焦于「敌人/子弹」链路，核心目标：

- ✅ **统一发射入口**：通过 `IBulletService` + `BulletService`，所有子弹生成都走 DI 管线，可自由切换对象池或兜底实例化。
- ✅ **事件驱动效果**：`BulletService` 在生成时派发 `BulletFiredEvent`，`PoolableBullet` 命中时派发 `BulletHitEvent`，方便 UI/VFX/音频等系统订阅。
- ✅ **彻底池化**：`EnemyController` 不再直接 Instantiate，`PoolableBullet` 负责友军判定、能量奖励触发入口。

## 关键脚本变更

| 脚本 | 变化摘要 |
| --- | --- |
| `Assets/Core/Scripts/Framework/IBulletService.cs` | 新增接口与 `BulletSpawnRequest` 结构，定义统一发射/回收协议。 |
| `Assets/Core/Scripts/Manager/BulletService.cs` | 实现池化发射、兜底 Instantiate，并在 `ProjectLifetimeScope` 中注册为 `IBulletService`。 |
| `Assets/Modules/Enemies/Scripts/EnemyController.cs` | 通过 `[Inject]` 获取 `IBulletService`，`SpawnBullet` 仅负责构造 `BulletSpawnRequest`。旧的 `BulletController` + prefab 回退逻辑全部移除。 |
| `Assets/Modules/Bullet/Scripts/PoolableBullet.cs` | 扩展 Launch 元数据（阵营/伤害/来源），新增敌友识别、命中事件派发、`Enemy` 受击处理以及一致的回收行为。 |
| `Assets/Core/Scripts/Framework/Events/GameEvents.cs` | 新增 `BulletHitEvent`，用于后续扩展（震动、屏幕特效等）。 |
| `Assets/Core/Scripts/Framework/ProjectLifetimeScope.cs` | 序列化并注册 `BulletService`，确保可被任意系统注入。 |
| `Assets/Modules/Bullet/Scripts/BulletController.cs` | 已删除，全部功能由 `PoolableBullet` + `BulletService` 接管。 |

## 接入步骤

1. **ProjectLifetimeScope 赋值**：在启动场景的 `ProjectLifetimeScope` 上填入 `BulletService` 组件引用（可与 `PoolManager` 同挂在一个 GameObject 上）。
2. **Prefab 清理**：确认敌人 Prefab 中不再引用 `BulletController` 或旧版子弹预制体。若还有 `Missing Script`，改挂 `PoolableBullet` 与 `PoolManager` 的池化预制体。
3. **新发射方式**：任何需要发射子弹的脚本（敌人/玩家/陷阱）通过 DI 注入 `IBulletService`，按需构造 `BulletSpawnRequest`：
   ```csharp
   bulletService.SpawnBullet(new BulletSpawnRequest {
       Position = firePoint.position,
       Direction = firePoint.right,
       IsPlayerBullet = true,
       SpeedOverride = chargedSpeed,
       DamageOverride = 3,
       SourceId = "ChargedShot"
   });
   ```
4. **事件订阅**：
   - `BulletFiredEvent`：适合做枪口火焰、音效、摄像机震动。
   - `BulletHitEvent`：可驱动命中特效、得分飘字或震屏。
   详见 `Docs/重构相关组件使用指南/EVENT_SYSTEM_GUIDE.md` 的事件总线章节。
5. **Perfect Dash 兼容**：`PoolableBullet.OnSpawn/OnDespawn` 仍会向 `BulletManager` 注册，极限闪避检测逻辑无需修改。

## 验收清单

- [ ] 场景内再无 `BulletController` 相关的 Missing Script 报错。
- [ ] 运行后 Console 出现 `✓ VContainer: BulletService 已注册`，敌人射击正常。
- [ ] 在 `EventBus` 调试输出中能看到 `BulletFiredEvent/BulletHitEvent` 的订阅或派发日志。
- [ ] 对象池 HUD 显示子弹活跃/可用数量，长时间运行不再频繁 Instantiate/GC。
