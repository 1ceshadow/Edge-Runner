using UnityEngine;
using VContainer;
using EdgeRunner.Events;

namespace EdgeRunner.Player.Systems
{
    /// <summary>
    /// 统一处理玩家生命值、受击与死亡事件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealthSystem : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;

        private PlayerController controller;
        private IGameStateManager gameStateManager;
        private int currentHealth;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        [Inject]
        public void Construct(IGameStateManager gameStateManager)
        {
            this.gameStateManager = gameStateManager;
        }

        public void Initialize(PlayerController ctx)
        {
            controller = ctx;
            currentHealth = maxHealth;
            PublishHealthChanged();
        }

        public void RestoreFull()
        {
            currentHealth = maxHealth;
            PublishHealthChanged();
        }

        public void TakeDamage(int damage, DamageType damageType, Vector2 sourcePosition)
        {
            if (damage <= 0)
            {
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
            EventBus.Publish(new PlayerDamagedEvent
            {
                Damage = damage,
                CurrentHealth = currentHealth,
                MaxHealth = maxHealth,
                SourcePosition = sourcePosition,
                DamageType = damageType
            });

            PublishHealthChanged();

            if (currentHealth <= 0)
            {
                EventBus.Publish(new PlayerDiedEvent
                {
                    Position = controller != null ? (Vector2)controller.transform.position : sourcePosition,
                    Reason = damageType.ToString()
                });

                // 优先使用 DI 注入的服务
                if (gameStateManager != null)
                {
                    gameStateManager.TriggerDeath();
                }
                else
                {
                    // 向后兼容
                    GameStateManager.Instance?.TriggerDeath();
                }
            }
        }

        private void PublishHealthChanged()
        {
            EventBus.Publish(new PlayerHealthChangedEvent
            {
                CurrentHealth = currentHealth,
                MaxHealth = maxHealth
            });
        }
    }
}
