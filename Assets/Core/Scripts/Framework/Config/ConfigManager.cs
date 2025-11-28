// ═══════════════════════════════════════════════════════════════════════════
//  ConfigManager - 配置管理器
//  提供全局配置访问接口，支持运行时热重载
//  
//  使用方法：
//  - 所有模块统一通过 ConfigManager.Player.MoveSpeed 等访问配置
//  - 如果 GameConfig 未加载，自动使用 ConfigDefaults 中的默认值
//  - 测试时只需修改 GameConfig.asset 文件即可
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace EdgeRunner.Config
{
    /// <summary>
    /// 配置管理器
    /// 提供全局配置访问，自动处理回退逻辑
    /// </summary>
    public class ConfigManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        //                          单例
        // ═══════════════════════════════════════════════════════════════

        public static ConfigManager Instance { get; private set; }

        // ═══════════════════════════════════════════════════════════════
        //                          配置引用
        // ═══════════════════════════════════════════════════════════════

        [SerializeField]
        [Tooltip("游戏配置 ScriptableObject")]
        private GameConfig gameConfig;

        /// <summary>游戏配置</summary>
        public GameConfig Config => gameConfig;

        // ═══════════════════════════════════════════════════════════════
        //                     便捷访问器（带自动回退）
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>玩家配置（如果未加载则返回 null，使用时需配合 GetXXX 方法）</summary>
        public static PlayerConfig Player => Instance?.gameConfig?.Player;
        
        /// <summary>敌人配置</summary>
        public static EnemyConfig Enemy => Instance?.gameConfig?.Enemy;
        
        /// <summary>子弹配置</summary>
        public static BulletConfig Bullet => Instance?.gameConfig?.Bullet;
        
        /// <summary>摄像机配置</summary>
        public static CameraConfig Camera => Instance?.gameConfig?.Camera;
        
        /// <summary>音频配置</summary>
        public static AudioConfig Audio => Instance?.gameConfig?.Audio;
        
        /// <summary>对象池配置</summary>
        public static PoolConfig Pool => Instance?.gameConfig?.Pool;

        // ═══════════════════════════════════════════════════════════════
        //                 安全访问方法（自动使用 ConfigDefaults 回退）
        // ═══════════════════════════════════════════════════════════════
        
        // --- 玩家配置 ---
        public static float GetMoveSpeed() => Player?.MoveSpeed ?? ConfigDefaults.Player.MoveSpeed;
        public static float GetDashDistance() => Player?.DashDistance ?? ConfigDefaults.Player.DashDistance;
        public static float GetDashCooldown() => Player?.DashCooldown ?? ConfigDefaults.Player.DashCooldown;
        public static float GetPerfectDashDetectRange() => Player?.PerfectDashDetectRange ?? ConfigDefaults.Player.PerfectDashDetectRange;
        public static float GetPerfectDashReward() => Player?.PerfectDashReward ?? ConfigDefaults.Player.PerfectDashReward;
        public static float GetRotationSmoothness() => Player?.RotationSmoothness ?? ConfigDefaults.Player.RotationSmoothness;
        public static float GetMaxEnergy() => Player?.MaxEnergy ?? ConfigDefaults.Player.MaxEnergy;
        public static float GetEnergyDrainRate() => Player?.EnergyDrainRate ?? ConfigDefaults.Player.EnergyDrainRate;
        public static float GetKillEnergyReward() => Player?.KillEnergyReward ?? ConfigDefaults.Player.KillReward;
        public static float GetTimeSlowScale() => Player?.TimeSlowScale ?? ConfigDefaults.Player.TimeSlowScale;
        public static float GetMinEnergyThreshold() => Player?.MinEnergyThreshold ?? ConfigDefaults.Player.MinEnergyToTimeSlow;
        public static float GetWallCheckExtra() => Player?.WallCheckExtra ?? ConfigDefaults.Physics.WallCheckExtra;
        
        // --- 敌人配置 ---
        public static float GetShootInterval() => Enemy?.ShootInterval ?? ConfigDefaults.Enemy.ShootInterval;
        public static float GetShootDistance() => Enemy?.ShootDistance ?? ConfigDefaults.Enemy.ShootDistance;
        public static int GetEnemyBulletCount() => Enemy?.BulletCount ?? ConfigDefaults.Enemy.BulletCount;
        public static float GetSpreadAngle() => Enemy?.SpreadAngle ?? ConfigDefaults.Enemy.SpreadAngle;
        public static int GetEnemyMaxHealth() => Enemy?.MaxHealth ?? ConfigDefaults.Enemy.MaxHealth;
        public static float GetEnemyKillReward() => Enemy?.KillEnergyReward ?? ConfigDefaults.Enemy.KillEnergyReward;
        
        // --- 子弹配置 ---
        public static float GetBulletSpeed() => Bullet?.Speed ?? ConfigDefaults.Bullet.Speed;
        public static float GetBulletMaxDistance() => Bullet?.MaxDistance ?? ConfigDefaults.Bullet.MaxDistance;
        public static int GetBulletDamage() => Bullet?.Damage ?? ConfigDefaults.Bullet.Damage;

        // ═══════════════════════════════════════════════════════════════
        //                          生命周期
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                ValidateConfig();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          验证
        // ═══════════════════════════════════════════════════════════════

        private void ValidateConfig()
        {
            if (gameConfig == null)
            {
                Debug.LogError("[ConfigManager] GameConfig 未设置！请在 Inspector 中指定配置文件。");
                return;
            }

            Debug.Log("✓ ConfigManager: 游戏配置已加载");

            // 输出关键配置
            Debug.Log($"  - 玩家移动速度: {gameConfig.Player.MoveSpeed}");
            Debug.Log($"  - 玩家冲刺距离: {gameConfig.Player.DashDistance}");
            Debug.Log($"  - 敌人射击间隔: {gameConfig.Enemy.ShootInterval}");
            Debug.Log($"  - 子弹池大小: {gameConfig.Pool.BulletPoolSize}");
        }

        // ═══════════════════════════════════════════════════════════════
        //                          运行时重载（编辑器用）
        // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中重新加载配置（用于热重载测试）
        /// </summary>
        [ContextMenu("重新加载配置")]
        public void ReloadConfig()
        {
            ValidateConfig();
            Debug.Log("[ConfigManager] 配置已重新加载");
        }
#endif
    }
}
