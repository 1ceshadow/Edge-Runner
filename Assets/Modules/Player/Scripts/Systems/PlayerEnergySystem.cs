using UnityEngine;
using EdgeRunner.Events;

namespace EdgeRunner.Player.Systems
{
    /// <summary>
    /// 管理玩家的能量、时缓状态以及与 UI 的事件通信。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEnergySystem : MonoBehaviour
    {
        [SerializeField] [Range(0.01f, 1f)] private float eventThreshold = 0.05f;

        private PlayerController controller;
        private float currentEnergy;
        private bool isTimeSlowed;

        public float CurrentEnergy => currentEnergy;
        public bool IsTimeSlowed => isTimeSlowed;

        public void Initialize(PlayerController ctx)
        {
            controller = ctx;
            currentEnergy = controller.MaxEnergy;
            PublishEnergyChanged(EnergyChangeReason.Recharge, 0f);
        }

        /// <summary>
        /// 每帧调用，负责自然回复或时缓消耗。
        /// </summary>
        public void Tick(bool consumeForTimeSlow)
        {
            if (controller == null)
            {
                return;
            }

            float delta = consumeForTimeSlow
                ? -controller.EnergyDrainRate * Time.unscaledDeltaTime
                : controller.RechargeRate * Time.unscaledDeltaTime;

            EnergyChangeReason reason = consumeForTimeSlow ? EnergyChangeReason.TimeSlowDrain : EnergyChangeReason.Recharge;
            Modify(delta, reason);
        }

        public void AddReward(float amount, EnergyChangeReason reason)
        {
            if (amount <= 0f)
            {
                return;
            }

            Modify(amount, reason);
        }

        public bool TryConsume(float amount, EnergyChangeReason reason)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (currentEnergy < amount)
            {
                return false;
            }

            Modify(-amount, reason);
            return true;
        }

        public bool CanUseTimeSlow(float minThreshold)
        {
            return currentEnergy >= minThreshold;
        }

        public void SetTimeSlowState(bool active)
        {
            isTimeSlowed = active;
        }

        private void Modify(float delta, EnergyChangeReason reason)
        {
            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            float previous = currentEnergy;
            float maxEnergy = controller?.MaxEnergy ?? 0f;
            currentEnergy = Mathf.Clamp(currentEnergy + delta, 0f, maxEnergy);
            float applied = currentEnergy - previous;

            if (Mathf.Abs(applied) >= eventThreshold)
            {
                PublishEnergyChanged(reason, applied);
            }
        }

        private void PublishEnergyChanged(EnergyChangeReason reason, float delta)
        {
            EventBus.Publish(new PlayerEnergyChangedEvent
            {
                CurrentEnergy = currentEnergy,
                MaxEnergy = controller?.MaxEnergy ?? 0f,
                DeltaEnergy = delta,
                Reason = reason
            });
        }
    }
}
