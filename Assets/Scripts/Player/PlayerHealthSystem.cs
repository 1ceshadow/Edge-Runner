using UnityEngine;

/// <summary>
/// 玩家生命值系统 - 独立管理血量
/// 
/// 职责:
/// - 管理当前血量
/// - 处理伤害和治疗
/// - 发送血量变化和死亡事件
/// </summary>
public class PlayerHealthSystem : MonoBehaviour, IHealable
{
    private int currentHealth;
    private int maxHealth = 1;  // 默认值，可在 Inspector 中修改
    private bool isAlive = true;

    public event System.Action<int> OnHealthChanged;
    public event System.Action OnDeath;

    public void Initialize()
    {
        currentHealth = maxHealth;
        isAlive = true;
        Debug.Log("✓ PlayerHealthSystem initialized");
    }

    public void TakeDamage(int damage)
    {
        if (!isAlive) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (!isAlive) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    private void Die()
    {
        isAlive = false;
        OnDeath?.Invoke();

        EventBus.Publish(new PlayerDiedEvent
        {
            DeathPosition = transform.position
        });
    }
}
