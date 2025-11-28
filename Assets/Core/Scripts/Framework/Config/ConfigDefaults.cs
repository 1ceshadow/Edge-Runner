// ═══════════════════════════════════════════════════════════════════════════
//  ConfigDefaults - 配置默认值常量
//  集中管理所有配置的默认值，避免硬编码分散在各个文件中
// ═══════════════════════════════════════════════════════════════════════════

namespace EdgeRunner.Config
{
    /// <summary>
    /// 游戏配置默认值
    /// 当 ConfigManager 中的配置为 null 时使用这些值
    /// </summary>
    public static class ConfigDefaults
    {
        /// <summary>
        /// 玩家相关默认值
        /// </summary>
        public static class Player
        {
            public const float MoveSpeed = 6.2f;
            public const float DashDistance = 3.9f;
            public const float DashCooldown = 0.2f;
            public const float PerfectDashDetectRange = 1.4f;
            public const float RotationSmoothness = 8f;
            public const float PlayerRadius = 0.3f;
            
            // 能量相关
            public const float MaxEnergy = 100f;
            public const float EnergyDrainRate = 20f;
            public const float PerfectDashReward = 10f;
            public const float KillReward = 10f;
            
            // 时缓相关
            public const float TimeSlowScale = 0.05f;
            public const float MinEnergyToTimeSlow = 1f;
        }

        /// <summary>
        /// 敌人相关默认值
        /// </summary>
        public static class Enemy
        {
            public const float ShootInterval = 1.8f;
            public const float ShootDistance = 20f;
            public const int BulletCount = 8;
            public const float SpreadAngle = 10f;
            public const int MaxHealth = 1;
            public const float KillEnergyReward = 10f;
        }

        /// <summary>
        /// 子弹相关默认值
        /// </summary>
        public static class Bullet
        {
            public const float Speed = 11.8f;
            public const float MaxDistance = 16f;
            public const int Damage = 1;
        }

        /// <summary>
        /// 物理相关默认值
        /// </summary>
        public static class Physics
        {
            public const float CollisionOffset = 0.1f;
            public const float WallCheckExtra = 0.8f;
            public const float SafeDistanceMultiplier = 1.2f;
            public const float MinAngleFactor = 0.5f;
        }

        /// <summary>
        /// UI/视觉相关默认值
        /// </summary>
        public static class Visual
        {
            public const float FadeDuration = 1.5f;
            public const float DeathFadeDelay = 1f;
        }

        /// <summary>
        /// 层名称常量
        /// </summary>
        public static class Layers
        {
            public const string Wall = "Wall";
            public const string Player = "Player";
            public const string Enemy = "Enemy";
            public const string Bullet = "Bullet";
        }

        /// <summary>
        /// 场景名称常量
        /// </summary>
        public static class Scenes
        {
            public const string MainMenu = "0MainMenu";
            public const string Level0 = "Level0";
            public const string Level1 = "Level1";
            public const string Level2 = "Level2";
            public const string Level3 = "Level3";
        }
    }
}
