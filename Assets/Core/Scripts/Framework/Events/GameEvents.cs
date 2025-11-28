// ═══════════════════════════════════════════════════════════════════════════
//  GameEvents - 游戏事件定义
//  所有事件类型集中管理，便于维护和查找
//  命名规范：{Subject}{Action}Event，如 PlayerDamagedEvent
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace EdgeRunner.Events
{
    // ═══════════════════════════════════════════════════════════════════════
    //                          玩家相关事件
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 玩家能量变化事件
    /// 用于 UI 更新、技能可用性判断
    /// </summary>
    public struct PlayerEnergyChangedEvent : IGameEvent
    {
        /// <summary>当前能量值</summary>
        public float CurrentEnergy;
        /// <summary>最大能量值</summary>
        public float MaxEnergy;
        /// <summary>能量变化量（正为获取，负为消耗）</summary>
        public float DeltaEnergy;
        /// <summary>变化原因</summary>
        public EnergyChangeReason Reason;

        public float NormalizedEnergy => MaxEnergy > 0 ? CurrentEnergy / MaxEnergy : 0f;
    }

    /// <summary>
    /// 能量变化原因枚举
    /// </summary>
    public enum EnergyChangeReason
    {
        /// <summary>自然回复</summary>
        Recharge,
        /// <summary>时缓消耗</summary>
        TimeSlowDrain,
        /// <summary>极限闪避奖励</summary>
        PerfectDash,
        /// <summary>击杀敌人奖励</summary>
        EnemyKill,
        /// <summary>技能消耗</summary>
        SkillCost,
        /// <summary>其他</summary>
        Other
    }

    /// <summary>
    /// 玩家获得奖励事件（通用奖励效果触发）
    /// 用于 UI 显示奖励特效
    /// </summary>
    public struct PlayerRewardedEvent : IGameEvent
    {
        /// <summary>奖励类型</summary>
        public RewardType Type;
        /// <summary>奖励数值</summary>
        public float Amount;
        /// <summary>奖励发生位置</summary>
        public Vector2 Position;
    }

    /// <summary>
    /// 奖励类型枚举
    /// </summary>
    public enum RewardType
    {
        /// <summary>极限闪避</summary>
        PerfectDash,
        /// <summary>击杀敌人</summary>
        EnemyKill,
        /// <summary>连击奖励</summary>
        Combo,
        /// <summary>其他</summary>
        Other
    }

    /// <summary>
    /// 玩家受伤事件
    /// </summary>
    public struct PlayerDamagedEvent : IGameEvent
    {
        /// <summary>伤害值</summary>
        public int Damage;
        /// <summary>当前生命值</summary>
        public int CurrentHealth;
        /// <summary>最大生命值</summary>
        public int MaxHealth;
        /// <summary>伤害来源位置</summary>
        public Vector2 SourcePosition;
        /// <summary>伤害类型</summary>
        public DamageType DamageType;
    }

    /// <summary>
    /// 伤害类型枚举
    /// </summary>
    public enum DamageType
    {
        Bullet,
        Contact,
        Environmental,
        Other
    }

    /// <summary>
    /// 玩家生命值变化事件
    /// </summary>
    public struct PlayerHealthChangedEvent : IGameEvent
    {
        public int CurrentHealth;
        public int MaxHealth;
        public float NormalizedHealth => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
    }

    /// <summary>
    /// 玩家死亡事件
    /// </summary>
    public struct PlayerDiedEvent : IGameEvent
    {
        /// <summary>死亡位置</summary>
        public Vector2 Position;
        /// <summary>死亡原因</summary>
        public string Reason;
    }

    /// <summary>
    /// 玩家冲刺事件
    /// </summary>
    public struct PlayerDashedEvent : IGameEvent
    {
        /// <summary>冲刺起点</summary>
        public Vector2 StartPosition;
        /// <summary>冲刺终点</summary>
        public Vector2 EndPosition;
        /// <summary>是否为极限闪避</summary>
        public bool IsPerfectDash;
    }

    /// <summary>
    /// 时缓状态变化事件
    /// </summary>
    public struct TimeSlowStateChangedEvent : IGameEvent
    {
        /// <summary>是否进入时缓</summary>
        public bool IsTimeSlowed;
        /// <summary>时缓倍率</summary>
        public float TimeScale;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          敌人相关事件
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 敌人被击败事件
    /// 用于触发奖励、更新统计、播放特效
    /// </summary>
    public struct EnemyDefeatedEvent : IGameEvent
    {
        /// <summary>敌人位置</summary>
        public Vector2 Position;
        /// <summary>敌人类型（用于不同奖励）</summary>
        public string EnemyType;
        /// <summary>击杀奖励能量值</summary>
        public float EnergyReward;
        /// <summary>击杀者是否为玩家</summary>
        public bool KilledByPlayer;
    }

    /// <summary>
    /// 敌人生成事件
    /// </summary>
    public struct EnemySpawnedEvent : IGameEvent
    {
        public Vector2 Position;
        public string EnemyType;
    }

    /// <summary>
    /// 敌人受伤事件
    /// </summary>
    public struct EnemyDamagedEvent : IGameEvent
    {
        public int Damage;
        public int CurrentHealth;
        public int MaxHealth;
        public Vector2 Position;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          战斗相关事件
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 子弹发射事件
    /// </summary>
    public struct BulletFiredEvent : IGameEvent
    {
        public Vector2 Position;
        public Vector2 Direction;
        public float Speed;
        public bool IsPlayerBullet;
    }

    /// <summary>
    /// 子弹命中事件，用于触发特效/震动等。
    /// </summary>
    public struct BulletHitEvent : IGameEvent
    {
        public Vector2 Position;
        public string HitTag;
        public bool IsPlayerBullet;
        public int Damage;
        public string SourceId;
    }

    /// <summary>
    /// 玩家攻击事件
    /// </summary>
    public struct PlayerAttackEvent : IGameEvent
    {
        /// <summary>攻击位置</summary>
        public Vector2 Position;
        /// <summary>攻击方向</summary>
        public Vector2 Direction;
        /// <summary>攻击伤害</summary>
        public int Damage;
        /// <summary>攻击范围</summary>
        public float Range;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          游戏状态事件
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 游戏暂停事件
    /// </summary>
    public struct GamePausedEvent : IGameEvent
    {
        public bool IsPaused;
    }

    /// <summary>
    /// 游戏胜利事件
    /// </summary>
    public struct GameWonEvent : IGameEvent
    {
        /// <summary>当前关卡索引</summary>
        public int LevelIndex;
        /// <summary>关卡名称</summary>
        public string LevelName;
        /// <summary>完成时间</summary>
        public float CompletionTime;
    }

    /// <summary>
    /// 游戏失败事件
    /// </summary>
    public struct GameOverEvent : IGameEvent
    {
        public string Reason;
        public int LevelIndex;
    }

    /// <summary>
    /// 场景加载事件
    /// </summary>
    public struct SceneLoadedEvent : IGameEvent
    {
        public string SceneName;
        public int SceneIndex;
        public bool IsMainMenu;
    }

    /// <summary>
    /// 关卡开始事件
    /// </summary>
    public struct LevelStartedEvent : IGameEvent
    {
        public int LevelIndex;
        public string LevelName;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          UI 相关事件
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 显示提示信息事件
    /// </summary>
    public struct ShowToastEvent : IGameEvent
    {
        public string Message;
        public float Duration;
        public ToastType Type;
    }

    /// <summary>
    /// 提示类型枚举
    /// </summary>
    public enum ToastType
    {
        Info,
        Warning,
        Error,
        Success
    }

    // ═══════════════════════════════════════════════════════════════════════
    //                          音频相关事件
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 播放音效事件
    /// </summary>
    public struct PlaySFXEvent : IGameEvent
    {
        public string SFXName;
        public Vector2 Position;
        public float Volume;
    }

    /// <summary>
    /// 播放背景音乐事件
    /// </summary>
    public struct PlayBGMEvent : IGameEvent
    {
        public string BGMName;
        public bool FadeIn;
        public float FadeDuration;
    }
}
