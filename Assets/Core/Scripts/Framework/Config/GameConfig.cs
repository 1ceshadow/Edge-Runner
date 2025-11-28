// ═══════════════════════════════════════════════════════════════════════════
//  GameConfig - 游戏配置 ScriptableObject
//  集中管理所有游戏参数，支持在 Inspector 中调整而无需重新编译
//  
//  使用方法：
//  1. 在 Unity 中 Create > EdgeRunner > GameConfig 创建配置资源
//  2. 在 Inspector 中调整参数
//  3. 通过 ConfigManager.Instance.Config 访问配置
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
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          玩家配置
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class PlayerConfig
    {
        [Header("移动")]
        [Tooltip("基础移动速度")]
        public float MoveSpeed = ConfigDefaults.Player.MoveSpeed;

        [Tooltip("旋转平滑度")]
        public float RotationSmoothness = ConfigDefaults.Player.RotationSmoothness;

        [Header("冲刺")]
        [Tooltip("冲刺距离")]
        public float DashDistance = ConfigDefaults.Player.DashDistance;

        [Tooltip("冲刺冷却时间")]
        public float DashCooldown = ConfigDefaults.Player.DashCooldown;

        [Tooltip("极限闪避检测范围")]
        public float PerfectDashDetectRange = ConfigDefaults.Player.PerfectDashDetectRange;

        [Tooltip("极限闪避能量奖励")]
        public float PerfectDashReward = ConfigDefaults.Player.PerfectDashReward;

        [Header("时缓")]
        [Tooltip("时缓时间缩放")]
        [Range(0.01f, 0.5f)]
        public float TimeSlowScale = ConfigDefaults.Player.TimeSlowScale;

        [Tooltip("时缓状态下玩家速度")]
        public float TimeSlowPlayerSpeed = 20f;

        [Header("能量")]
        [Tooltip("最大能量值")]
        public float MaxEnergy = ConfigDefaults.Player.MaxEnergy;

        [Tooltip("能量回复速率（每秒）")]
        public float EnergyRechargeRate = 2f;

        [Tooltip("时缓能量消耗速率（每秒）")]
        public float EnergyDrainRate = ConfigDefaults.Player.EnergyDrainRate;

        [Tooltip("使用时缓的最低能量门槛")]
        public float MinEnergyThreshold = ConfigDefaults.Player.MinEnergyToTimeSlow;

        [Tooltip("击杀敌人能量奖励")]
        public float KillEnergyReward = ConfigDefaults.Player.KillReward;

        [Header("爬墙")]
        [Tooltip("墙体检测额外距离")]
        public float WallCheckExtra = ConfigDefaults.Physics.WallCheckExtra;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          敌人配置
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class EnemyConfig
    {
        [Header("射击")]
        [Tooltip("射击间隔（秒）")]
        public float ShootInterval = ConfigDefaults.Enemy.ShootInterval;

        [Tooltip("射击检测距离")]
        public float ShootDistance = ConfigDefaults.Enemy.ShootDistance;

        [Tooltip("每次射击的子弹数量")]
        public int BulletCount = ConfigDefaults.Enemy.BulletCount;

        [Tooltip("子弹散射角度")]
        public float SpreadAngle = ConfigDefaults.Enemy.SpreadAngle;

        [Header("属性")]
        [Tooltip("基础生命值")]
        public int MaxHealth = ConfigDefaults.Enemy.MaxHealth;

        [Tooltip("击杀能量奖励")]
        public float KillEnergyReward = ConfigDefaults.Enemy.KillEnergyReward;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          子弹配置
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class BulletConfig
    {
        [Tooltip("子弹速度")]
        public float Speed = ConfigDefaults.Bullet.Speed;

        [Tooltip("子弹最大飞行距离")]
        public float MaxDistance = ConfigDefaults.Bullet.MaxDistance;

        [Tooltip("子弹伤害")]
        public int Damage = ConfigDefaults.Bullet.Damage;

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
}
