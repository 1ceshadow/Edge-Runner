using UnityEngine;

/// <summary>
/// 玩家能量系统 - 独立管理能量相关逻辑
/// 
/// 职责:
/// - 管理当前能量值
/// - 处理能量充能和消耗
/// - 发送能量变化事件
/// 
/// 能量来源:
/// - 自动充能 (RechargeRate)
/// - 极限闪避 (PerfectDashReward)
/// - 击杀敌人 (KillReward)
/// 
/// 能量消耗:
/// - 时缓技能 (DrainRate)
/// </summary>
public class PlayerEnergySystem : MonoBehaviour, IEnergyProvider
{
    private GameConfig config;
    private float currentEnergy;

    public event System.Action<float> OnEnergyChanged;

    private bool isInitialized = false;

    public void Initialize()
    {
        config = GameConfig.Load();
        currentEnergy = config.Player.MaxEnergy;
        isInitialized = true;

        // 订阅击杀事件
        EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);

        Debug.Log("✓ PlayerEnergySystem initialized");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 自动充能
        AddEnergy(config.Player.EnergyRechargeRate * Time.deltaTime);
    }

    public void AddEnergy(float amount)
    {
        if (!isInitialized) return;

        float oldEnergy = currentEnergy;
        currentEnergy = Mathf.Min(currentEnergy + amount, config.Player.MaxEnergy);

        if (!Mathf.Approximately(oldEnergy, currentEnergy))
        {
            OnEnergyChanged?.Invoke(currentEnergy);
            EventBus.Publish(new PlayerEnergyChangedEvent
            {
                CurrentEnergy = currentEnergy,
                MaxEnergy = config.Player.MaxEnergy
            });
        }
    }

    public void ConsumeEnergy(float amount)
    {
        if (!isInitialized) return;

        float oldEnergy = currentEnergy;
        currentEnergy = Mathf.Max(currentEnergy - amount, 0);

        if (!Mathf.Approximately(oldEnergy, currentEnergy))
        {
            OnEnergyChanged?.Invoke(currentEnergy);
            EventBus.Publish(new PlayerEnergyChangedEvent
            {
                CurrentEnergy = currentEnergy,
                MaxEnergy = config.Player.MaxEnergy
            });
        }
    }

    public bool TryConsumeEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            ConsumeEnergy(amount);
            return true;
        }
        return false;
    }

    public float GetCurrentEnergy() => currentEnergy;
    public float GetMaxEnergy() => config.Player.MaxEnergy;

    private void OnEnemyDefeated(EnemyDefeatedEvent evt)
    {
        AddEnergy(config.Player.KillReward);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
    }
}
