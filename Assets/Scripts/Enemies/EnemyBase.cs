using UnityEngine;

/// <summary>
/// 敌人基类 - 所有敌人类型的父类
/// 
/// 职责:
/// - 实现通用的敌人功能 (生命值、移动、AI)
/// - 发送敌人相关事件
/// - 实现 IEnemy 接口
/// 
/// 子类只需要实现具体的攻击行为
/// </summary>
public class EnemyBase : MonoBehaviour, IEnemy, IPoolable
{
    protected GameConfig config;
    protected IPlayerService playerService;

    protected int currentHealth;
    protected bool isAlive = true;
    protected Vector2 targetPosition;

    public event System.Action OnDefeated;

    protected virtual void Start()
    {
        config = GameConfig.Load();
        playerService = ServiceLocator.Get<IPlayerService>();

        if (playerService == null)
        {
            Debug.LogError("PlayerService not registered in ServiceLocator!");
        }

        currentHealth = config.Enemy.MaxHealth;
    }

    public virtual void TakeDamage(int damage)
    {
        if (!isAlive) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Defeat();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, config.Enemy.MaxHealth);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => config.Enemy.MaxHealth;

    public Vector2 GetPosition() => transform.position;

    public void SetTarget(Vector2 newTarget)
    {
        targetPosition = newTarget;
    }

    public bool IsAlive() => isAlive;

    protected virtual void Defeat()
    {
        isAlive = false;
        OnDefeated?.Invoke();

        EventBus.Publish(new EnemyDefeatedEvent
        {
            EnemyId = gameObject.GetInstanceID(),
            DefeatPosition = transform.position,
            RewardGiven = (int)config.Player.KillReward
        });

        // 返回到池或销毁
        var poolable = GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnReturn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // IPoolable 实现
    public virtual void OnAcquire()
    {
        isAlive = true;
        currentHealth = config.Enemy.MaxHealth;
        gameObject.SetActive(true);
    }

    public virtual void OnReturn()
    {
        isAlive = false;
        gameObject.SetActive(false);
    }

    public GameObject GetGameObject() => gameObject;
}
