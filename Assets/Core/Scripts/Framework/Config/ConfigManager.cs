// ═══════════════════════════════════════════════════════════════════════════
//  ConfigManager - 配置管理器
//  提供全局配置访问接口
//  
//  使用方法：
//  - 通过 ConfigManager.Player.MoveSpeed 等直接访问配置
//  - 必须在场景中配置 GameConfig ScriptableObject
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace EdgeRunner.Config
{
    /// <summary>
    /// 配置管理器
    /// 提供全局配置访问
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
        //                     便捷访问器（带懒初始化）
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>玩家配置</summary>
        public static PlayerConfig Player => EnsurePlayer();
        
        /// <summary>敌人配置</summary>
        public static EnemyConfig Enemy => EnsureEnemy();
        
        /// <summary>子弹配置</summary>
        public static BulletConfig Bullet => EnsureBullet();
        
        /// <summary>摄像机配置</summary>
        public static CameraConfig Camera => EnsureCamera();
        
        /// <summary>音频配置</summary>
        public static AudioConfig Audio => EnsureAudio();
        
        /// <summary>对象池配置</summary>
        public static PoolConfig Pool => EnsurePool();

        /// <summary>战斗配置</summary>
        public static CombatConfig Combat => EnsureCombat();

        // ═══════════════════════════════════════════════════════════════
        //                     静态默认配置（Instance 为 null 时使用）
        // ═══════════════════════════════════════════════════════════════
        
        private static PlayerConfig _defaultPlayer;
        private static EnemyConfig _defaultEnemy;
        private static BulletConfig _defaultBullet;
        private static CameraConfig _defaultCamera;
        private static AudioConfig _defaultAudio;
        private static PoolConfig _defaultPool;
        private static CombatConfig _defaultCombat;
        
        // ═══════════════════════════════════════════════════════════════
        //                     懒初始化辅助方法
        // ═══════════════════════════════════════════════════════════════

        private static PlayerConfig EnsurePlayer()
        {
            if (Instance?.gameConfig != null)
                return Instance.gameConfig.Player ??= new PlayerConfig();
            return _defaultPlayer ??= new PlayerConfig();
        }

        private static EnemyConfig EnsureEnemy()
        {
            if (Instance?.gameConfig != null)
                return Instance.gameConfig.Enemy ??= new EnemyConfig();
            return _defaultEnemy ??= new EnemyConfig();
        }

        private static BulletConfig EnsureBullet()
        {
            if (Instance?.gameConfig != null)
                return Instance.gameConfig.Bullet ??= new BulletConfig();
            return _defaultBullet ??= new BulletConfig();
        }

        private static CameraConfig EnsureCamera()
        {
            if (Instance?.gameConfig != null)
                return Instance.gameConfig.Camera ??= new CameraConfig();
            return _defaultCamera ??= new CameraConfig();
        }

        private static AudioConfig EnsureAudio()
        {
            if (Instance?.gameConfig != null)
                return Instance.gameConfig.Audio ??= new AudioConfig();
            return _defaultAudio ??= new AudioConfig();
        }

        private static PoolConfig EnsurePool()
        {
            if (Instance?.gameConfig != null)
                return Instance.gameConfig.Pool ??= new PoolConfig();
            return _defaultPool ??= new PoolConfig();
        }

        private static CombatConfig EnsureCombat()
        {
            if (Instance?.gameConfig != null)
                return Instance.gameConfig.Combat ??= new CombatConfig();
            return _defaultCombat ??= new CombatConfig();
        }

        // ═══════════════════════════════════════════════════════════════
        //                 简化访问方法（直接返回配置值）
        // ═══════════════════════════════════════════════════════════════
        
        // --- 玩家配置 ---
        public static float GetMoveSpeed() => Player.MoveSpeed;
        public static float GetDashDistance() => Player.DashDistance;
        public static float GetDashCooldown() => Player.DashCooldown;
        public static float GetPerfectDashDetectRange() => Player.PerfectDashDetectRange;
        public static float GetPerfectDashReward() => Player.PerfectDashReward;
        public static float GetRotationSmoothness() => Player.RotationSmoothness;
        public static float GetMaxEnergy() => Player.MaxEnergy;
        public static float GetEnergyDrainRate() => Player.EnergyDrainRate;
        public static float GetKillEnergyReward() => Player.KillEnergyReward;
        public static float GetTimeSlowScale() => Player.TimeSlowScale;
        public static float GetMinEnergyThreshold() => Player.MinEnergyThreshold;
        public static float GetWallCheckExtra() => Player.WallCheckExtra;
        
        // --- 敌人配置 ---
        public static float GetShootInterval() => Enemy.ShootInterval;
        public static float GetShootDistance() => Enemy.ShootDistance;
        public static int GetEnemyBulletCount() => Enemy.BulletCount;
        public static float GetSpreadAngle() => Enemy.SpreadAngle;
        public static int GetEnemyMaxHealth() => Enemy.MaxHealth;
        public static float GetEnemyKillReward() => Enemy.KillEnergyReward;
        
        // --- 子弹配置 ---
        public static float GetBulletSpeed() => Bullet.Speed;
        public static float GetBulletMaxDistance() => Bullet.MaxDistance;
        public static int GetBulletDamage() => Bullet.Damage;

        // --- 战斗配置 ---
        public static float GetSwordLength() => Combat.SwordLength;
        public static float GetSwordWidth() => Combat.SwordWidth;
        public static float GetSwingDuration() => Combat.SwingDuration;
        public static float GetSwingAngle() => Combat.SwingAngle;
        public static int GetCombatBaseDamage() => Combat.BaseDamage;
        public static float GetAttackCooldown() => Combat.AttackCooldown;
        public static float GetComboResetTime() => Combat.ComboResetTime;
        public static int GetMaxComboCount() => Combat.MaxComboCount;

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
                Debug.LogError("[ConfigManager] ❌ GameConfig 未设置！请在 Inspector 中指定配置文件。游戏将无法正常运行。");
                return;
            }

            Debug.Log("✓ ConfigManager: 游戏配置已加载");

#if UNITY_EDITOR
            // 编辑器模式下输出关键配置
            Debug.Log($"  - 玩家移动速度: {gameConfig.Player.MoveSpeed}");
            Debug.Log($"  - 玩家冲刺距离: {gameConfig.Player.DashDistance}");
            Debug.Log($"  - 敌人射击间隔: {gameConfig.Enemy.ShootInterval}");
            Debug.Log($"  - 子弹池大小: {gameConfig.Pool.BulletPoolSize}");
#endif
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
