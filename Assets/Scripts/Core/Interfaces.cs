using UnityEngine;

/// <summary>
/// 玩家服务接口 - 其他系统通过此接口与玩家交互，而不是直接依赖 PlayerController
/// </summary>
public interface IPlayerService
{
    /// <summary>
    /// 获取玩家位置
    /// </summary>
    Vector2 GetPosition();

    /// <summary>
    /// 对玩家造成伤害
    /// </summary>
    void TakeDamage(int damage, Vector2 damageSource);

    /// <summary>
    /// 对玩家的极限闪避范围内是否有子弹
    /// </summary>
    bool HasBulletsInDashRange();

    /// <summary>
    /// 获取当前能量
    /// </summary>
    float GetCurrentEnergy();

    /// <summary>
    /// 获取最大能量
    /// </summary>
    float GetMaxEnergy();

    /// <summary>
    /// 玩家是否存活
    /// </summary>
    bool IsAlive();
}

/// <summary>
/// 可移动接口
/// </summary>
public interface IMoveable
{
    Vector2 GetPosition();
    Vector2 GetDirection();
    void SetDirection(Vector2 newDirection);
}

/// <summary>
/// 可伤害接口
/// </summary>
public interface IHealable
{
    void TakeDamage(int damage);
    void Heal(int amount);
    int GetCurrentHealth();
    int GetMaxHealth();
}

/// <summary>
/// 可攻击接口
/// </summary>
public interface IAttackable
{
    void Attack();
    bool CanAttack();
    float GetAttackCooldown();
}

/// <summary>
/// 能量管理接口
/// </summary>
public interface IEnergyProvider
{
    float GetCurrentEnergy();
    float GetMaxEnergy();
    bool TryConsumeEnergy(float amount);
    void AddEnergy(float amount);
    event System.Action<float> OnEnergyChanged;
}

/// <summary>
/// 玩家状态接口
/// </summary>
public interface IPlayerState
{
    void OnEnter();
    void Update();
    void OnExit();
}

/// <summary>
/// 敌人接口
/// </summary>
public interface IEnemy : IHealable
{
    Vector2 GetPosition();
    void SetTarget(Vector2 targetPosition);
    bool IsAlive();
    event System.Action OnDefeated;
}

/// <summary>
/// 投射物接口
/// </summary>
public interface IProjectile
{
    void Launch(Vector2 position, Vector2 direction, float speed);
    void Return();  // 返回到池
    Vector2 GetPosition();
    bool IsActive();
}
