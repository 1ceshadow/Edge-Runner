using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EdgeRunner.Events;
using EdgeRunner.Player;

/// <summary>
/// 能量条 UI - 使用事件驱动更新
/// 完全解耦，不直接引用任何玩家脚本
/// </summary>
public class EnergyBar : MonoBehaviour
{
    [Header("UI 引用")]
    public Image energyBarImg;
    public Image RewardEffecctImg;
    
    private Coroutine rewardCoroutine;
    
    // 缓存能量值（用于事件驱动模式）
    private float cachedCurrentEnergy;
    private float cachedMaxEnergy = 80f;

    void Start()
    {
        RewardEffecctImg.fillAmount = 0f;
        Debug.Log("✓ EnergyBar: 使用事件驱动模式");
    }

    void OnEnable()
    {
        // 🔔 订阅事件
        EventBus.Subscribe<PlayerEnergyChangedEvent>(OnEnergyChanged);
        EventBus.Subscribe<PlayerRewardedEvent>(OnPlayerRewarded);
    }

    void OnDisable()
    {
        // 🔔 取消订阅（防止内存泄漏）
        EventBus.Unsubscribe<PlayerEnergyChangedEvent>(OnEnergyChanged);
        EventBus.Unsubscribe<PlayerRewardedEvent>(OnPlayerRewarded);
    }

    void Update()
    {
        // 事件驱动模式：使用缓存值
        energyBarImg.fillAmount = cachedMaxEnergy > 0 
            ? cachedCurrentEnergy / cachedMaxEnergy 
            : 0f;
    }

    // ═══════════════════════════════════════════════════════════════
    //                          事件处理器
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 处理能量变化事件
    /// </summary>
    private void OnEnergyChanged(PlayerEnergyChangedEvent evt)
    {
        cachedCurrentEnergy = evt.CurrentEnergy;
        cachedMaxEnergy = evt.MaxEnergy;
    }

    /// <summary>
    /// 处理玩家奖励事件
    /// </summary>
    private void OnPlayerRewarded(PlayerRewardedEvent evt)
    {
        TriggerRewardEffect();
    }

    /// <summary>
    /// 触发奖励特效
    /// </summary>
    private void TriggerRewardEffect()
    {
        if (rewardCoroutine != null)
        {
            StopCoroutine(rewardCoroutine);
        }
        rewardCoroutine = StartCoroutine(ShowRewardEffect());
    }

    private IEnumerator ShowRewardEffect()
    {
        // 透明度-显示
        RewardEffecctImg.fillAmount = 1f;
        yield return new WaitForSeconds(0.4f);
        // 透明度-隐藏
        RewardEffecctImg.fillAmount = 0f;
    }
}
