using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EdgeRunner.Events;
using EdgeRunner.Player;

/// <summary>
/// 能量条 UI - 支持直接引用（流畅）和事件驱动（解耦）两种模式
/// 优先使用直接引用模式以保证流畅性
/// </summary>
public class EnergyBar : MonoBehaviour
{
    [Header("UI 引用")]
    public Image energyBarImg;
    public Image RewardEffecctImg;

    [Header("数据源（可选，留空则使用事件驱动）")]
    [SerializeField] private PlayerController playerController;
    
    private Coroutine rewardCoroutine;
    
    // 缓存能量值（用于事件驱动模式的后备）
    private float cachedCurrentEnergy;
    private float cachedMaxEnergy = 80f;

    // 是否使用直接引用模式
    private bool useDirectReference;

    void Start()
    {
        if (RewardEffecctImg != null)
        {
            RewardEffecctImg.fillAmount = 0f;
        }

        // 尝试自动查找 PlayerController
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        useDirectReference = playerController != null;
        
        if (useDirectReference)
        {
            Debug.Log("✓ EnergyBar: 使用直接引用模式（流畅）");
        }
        else
        {
            Debug.Log("✓ EnergyBar: 使用事件驱动模式（解耦）");
        }
    }

    void OnEnable()
    {
        // 🔔 订阅事件（作为后备或奖励特效）
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
        float current, max;

        if (useDirectReference && playerController != null)
        {
            // 直接引用模式：每帧读取，最流畅
            current = playerController.CurrentEnergy;
            max = playerController.MaxEnergy;
        }
        else
        {
            // 事件驱动模式：使用缓存值
            current = cachedCurrentEnergy;
            max = cachedMaxEnergy;
        }

        energyBarImg.fillAmount = max > 0 ? current / max : 0f;
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
