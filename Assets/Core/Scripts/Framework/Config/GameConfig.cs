// ═══════════════════════════════════════════════════════════════════════════
//  GameConfig - 游戏配置 ScriptableObject
//  集中管理所有游戏参数，支持在 Inspector 中调整而无需重新编译
//  
//  ⚠️ 这是唯一的配置数据源，所有参数默认值都在这里定义
//  
//  使用方法：
//  1. 在 Unity 中 Create > EdgeRunner > GameConfig 创建配置资源
//  2. 在 Inspector 中调整参数
//  3. 通过 ConfigManager.Player.MoveSpeed 等访问配置
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace EdgeRunner.Config
{
    /// <summary>
    /// 游戏配置 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "EdgeRunner/GameConfig", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("═══ 玩家配置 ═══")]
        public PlayerConfig Player = new PlayerConfig();

        [Header("═══ 敌人配置 ═══")]
        public EnemyConfig Enemy = new EnemyConfig();

        [Header("═══ 子弹配置 ═══")]
        public BulletConfig Bullet = new BulletConfig();

        [Header("═══ 摄像机配置 ═══")]
        public CameraConfig Camera = new CameraConfig();

        [Header("═══ 音频配置 ═══")]
        public AudioConfig Audio = new AudioConfig();

        [Header("═══ 对象池配置 ═══")]
        public PoolConfig Pool = new PoolConfig();

        [Header("═══ 战斗配置 ═══")]
        public CombatConfig Combat = new CombatConfig();

        /// <summary>
        /// 确保所有配置对象都被初始化
        /// Unity 序列化可能导致旧资源中的引用类型字段为 null
        /// </summary>
        private void OnEnable()
        {
            EnsureConfigsInitialized();
        }

        private void OnValidate()
        {
            EnsureConfigsInitialized();
        }

        private void EnsureConfigsInitialized()
        {
            Player ??= new PlayerConfig();
            Enemy ??= new EnemyConfig();
            Bullet ??= new BulletConfig();
            Camera ??= new CameraConfig();
            Audio ??= new AudioConfig();
            Pool ??= new PoolConfig();
            Combat ??= new CombatConfig();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          玩家配置
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class PlayerConfig
    {
        [Header("移动")]
        [Tooltip("基础移动速度")]
        public float MoveSpeed = 6.2f;

        [Tooltip("旋转平滑度")]
        public float RotationSmoothness = 8f;

        [Header("冲刺")]
        [Tooltip("冲刺距离")]
        public float DashDistance = 3.9f;

        [Tooltip("冲刺冷却时间")]
        public float DashCooldown = 0.2f;

        [Tooltip("极限闪避检测范围")]
        public float PerfectDashDetectRange = 1.4f;

        [Tooltip("极限闪避能量奖励")]
        public float PerfectDashReward = 10f;

        [Header("时缓")]
        [Tooltip("时缓时间缩放")]
        [Range(0.01f, 0.5f)]
        public float TimeSlowScale = 0.05f;

        [Tooltip("时缓状态下玩家速度")]
        public float TimeSlowPlayerSpeed = 20f;

        [Header("能量")]
        [Tooltip("最大能量值")]
        public float MaxEnergy = 100f;

        [Tooltip("能量回复速率（每秒）")]
        public float EnergyRechargeRate = 2f;

        [Tooltip("时缓能量消耗速率（每秒）")]
        public float EnergyDrainRate = 20f;

        [Tooltip("使用时缓的最低能量门槛")]
        public float MinEnergyThreshold = 1f;

        [Tooltip("击杀敌人能量奖励")]
        public float KillEnergyReward = 10f;

        [Header("爬墙")]
        [Tooltip("墙体检测额外距离")]
        public float WallCheckExtra = 0.8f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          敌人配置
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class EnemyConfig
    {
        [Header("射击")]
        [Tooltip("射击间隔（秒）")]
        public float ShootInterval = 1.8f;

        [Tooltip("射击检测距离")]
        public float ShootDistance = 20f;

        [Tooltip("每次射击的子弹数量")]
        public int BulletCount = 8;

        [Tooltip("子弹散射角度")]
        public float SpreadAngle = 10f;

        [Header("属性")]
        [Tooltip("基础生命值")]
        public int MaxHealth = 1;

        [Tooltip("击杀能量奖励")]
        public float KillEnergyReward = 10f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          子弹配置
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class BulletConfig
    {
        [Tooltip("子弹速度")]
        public float Speed = 11.8f;

        [Tooltip("子弹最大飞行距离")]
        public float MaxDistance = 16f;

        [Tooltip("子弹伤害")]
        public int Damage = 1;

        [Tooltip("是否使用连续碰撞检测")]
        public bool UseContinuousDetection = true;

        [Tooltip("连续检测步长")]
        public float DetectionStep = 0.5f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          摄像机配置
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class CameraConfig
    {
        [Tooltip("相机跟随平滑度")]
        public float FollowSmoothness = 5f;

        [Tooltip("相机偏移")]
        public Vector3 Offset = new Vector3(0, 0, -10);

        [Tooltip("相机缩放")]
        public float ZoomLevel = 5f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          音频配置
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class AudioConfig
    {
        [Range(0f, 1f)]
        [Tooltip("主音量")]
        public float MasterVolume = 1f;

        [Range(0f, 1f)]
        [Tooltip("BGM 音量")]
        public float BGMVolume = 0.7f;

        [Range(0f, 1f)]
        [Tooltip("SFX 音量")]
        public float SFXVolume = 1f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          对象池配置
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class PoolConfig
    {
        [Tooltip("子弹池初始大小")]
        public int BulletPoolSize = 100;

        [Tooltip("敌人池初始大小")]
        public int EnemyPoolSize = 20;

        [Tooltip("特效池初始大小")]
        public int VFXPoolSize = 50;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          战斗配置
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class CombatConfig
    {
        [Header("剑参数")]
        [Tooltip("剑长度")]
        public float SwordLength = 1.5f;

        [Tooltip("剑宽度")]
        public float SwordWidth = 0.3f;

        [Tooltip("单次挥砍持续时间")]
        public float SwingDuration = 0.15f;

        [Tooltip("挥砍角度范围")]
        public float SwingAngle = 150f;

        [Header("攻击参数")]
        [Tooltip("基础伤害")]
        public int BaseDamage = 1;

        [Tooltip("攻击冷却（连击窗口内可继续攻击）")]
        public float AttackCooldown = 0.1f;

        [Tooltip("连击重置时间（超过此时间连击数归零）")]
        public float ComboResetTime = 0.5f;

        [Tooltip("最大连击数")]
        public int MaxComboCount = 3;

        [Header("连击1 - 右斩")]
        [Tooltip("第1击起始角度")]
        public float Combo1StartAngle = 75f;
        [Tooltip("第1击结束角度")]
        public float Combo1EndAngle = -75f;

        [Header("连击2 - 左斩")]
        [Tooltip("第2击起始角度")]
        public float Combo2StartAngle = -60f;
        [Tooltip("第2击结束角度")]
        public float Combo2EndAngle = 60f;

        [Header("连击3 - 横扫")]
        [Tooltip("第3击起始角度")]
        public float Combo3StartAngle = 0f;
        [Tooltip("第3击结束角度")]
        public float Combo3EndAngle = 180f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          常量定义（Layer/Scene 名称）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 层名称常量
    /// </summary>
    public static class LayerNames
    {
        public const string Wall = "Wall";
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Bullet = "Bullet";
        public const string Billboard = "Billboard";
    }

    /// <summary>
    /// 场景名称常量
    /// </summary>
    public static class SceneNames
    {
        public const string MainMenu = "0MainMenu";
        public const string Level0 = "Level0";
        public const string Level1 = "Level1";
        public const string Level2 = "Level2";
        public const string Level3 = "Level3";
    }
}
