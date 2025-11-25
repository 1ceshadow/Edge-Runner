using UnityEngine;

/// <summary>
/// 游戏配置 (ScriptableObject)
/// 集中管理所有游戏参数，避免硬编码
/// 在 Assets/Resources/Config/ 中创建此对象
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Edge-Runner/Game Config", order = 1)]
public class GameConfig : ScriptableObject
{
    [Header("=== 玩家配置 ===")]
    public PlayerConfig Player = new();

    [Header("=== 敌人配置 ===")]
    public EnemyConfig Enemy = new();

    [Header("=== 摄像机配置 ===")]
    public CameraConfig Camera = new();

    [Header("=== 音频配置 ===")]
    public AudioConfig Audio = new();

    [Header("=== 游戏平衡性 ===")]
    public GameBalanceConfig Balance = new();

    /// <summary>
    /// 从 Resources 加载配置
    /// </summary>
    public static GameConfig Load()
    {
        var config = Resources.Load<GameConfig>("Config/GameConfig");
        if (config == null)
        {
            Debug.LogError("GameConfig not found in Resources/Config/!");
            return ScriptableObject.CreateInstance<GameConfig>();
        }
        return config;
    }
}

[System.Serializable]
public class PlayerConfig
{
    [Header("移动")]
    [Range(1, 20)] public float MoveSpeed = 6.2f;

    [Header("闪现/冲刺")]
    [Range(1, 10)] public float DashDistance = 3.9f;
    [Range(0.1f, 1f)] public float DashCooldown = 0.2f;

    [Header("时缓能量")]
    [Range(10, 200)] public float MaxEnergy = 80f;
    [Range(0.1f, 10)] public float EnergyRechargeRate = 2f;
    [Range(1, 30)] public float EnergyDrainRate = 10f;
    [Range(0.1f, 1f)] public float TimeSlowScale = 0.3f;

    [Header("极限闪避")]
    [Range(0.5f, 3f)] public float PerfectDashDetectRange = 1.4f;
    [Range(1, 50)] public float PerfectDashReward = 8f;

    [Header("击杀奖励")]
    [Range(1, 50)] public float KillReward = 10f;

    [Header("其他")]
    [Range(1, 20)] public float RotationSmoothness = 8f;
}

[System.Serializable]
public class EnemyConfig
{
    [Header("射击")]
    [Range(0.5f, 5f)] public float ShootInterval = 1.8f;
    [Range(5, 50)] public float ShootDistance = 20f;
    [Range(1, 20)] public int BulletCount = 8;
    [Range(0, 45)] public float SpreadAngle = 10f;

    [Header("子弹")]
    [Range(1, 30)] public float BulletSpeed = 11.8f;
    [Range(5, 50)] public float BulletMaxDistance = 16f;

    [Header("生命值")]
    [Range(1, 10)] public int MaxHealth = 1;

    [Header("对象池")]
    [Range(10, 100)] public int PoolSize = 50;
    [Range(10, 100)] public int BulletPoolSize = 100;
}

[System.Serializable]
public class CameraConfig
{
    [Range(1, 15)] public float OrthographicSize = 5f;
    [Range(0.1f, 1f)] public float FollowSpeed = 0.125f;
    [Range(0, 5)] public float FollowDeadZone = 0.5f;
}

[System.Serializable]
public class AudioConfig
{
    [Range(0, 1)] public float MasterVolume = 0.8f;
    [Range(0, 1)] public float EffectsVolume = 0.7f;
    [Range(0, 1)] public float MusicVolume = 0.5f;
    [Range(0, 1)] public float UIVolume = 0.6f;

    public bool MuteOnPause = true;
}

[System.Serializable]
public class GameBalanceConfig
{
    [Header("难度")]
    [Range(0.5f, 2f)] public float DifficultyMultiplier = 1f;

    [Header("时间")]
    [Range(0.1f, 3f)] public float TimeslowDuration = 2f;
    [Range(0.5f, 5f)] public float TimeslowCooldown = 3f;

    [Header("其他")]
    public bool EnableDebugMode = false;
    public bool ShowHitboxes = false;
}
