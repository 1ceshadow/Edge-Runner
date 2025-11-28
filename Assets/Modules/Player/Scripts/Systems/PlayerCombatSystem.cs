using System.Collections;
using UnityEngine;
using EdgeRunner.Events;
using EdgeRunner.Player;

namespace EdgeRunner.Player.Systems
{
    /// <summary>
    /// 负责玩家攻击的碰撞体激活、命中检测与事件派发。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerCombatSystem : MonoBehaviour
    {
        [Header("碰撞体配置")]
        [SerializeField] private Collider2D attackCollider;
        [SerializeField] private float hitboxActiveTime = 0.12f;
        [SerializeField] private LayerMask enemyLayerMask = ~0;

        [Header("攻击参数")]
        [SerializeField] private float attackCooldown = 0.3f;
        [SerializeField] private int attackDamage = 1;

        [Header("音效与动画")]
        [SerializeField] private AudioClip attackClip;
        [SerializeField] private Animator animator;
        [SerializeField] private string attackTriggerName = "TrigAttack";

        private readonly Collider2D[] overlapResults = new Collider2D[16];
        private ContactFilter2D contactFilter = new()
        {
            useLayerMask = true,
            useTriggers = true
        };

        private PlayerController controller;
        private AudioSource audioSource;
        private bool canAttack = true;
        private Coroutine attackRoutine;

        public void Initialize(PlayerController ctx)
        {
            controller = ctx;
            audioSource = GetComponent<AudioSource>();
            contactFilter.layerMask = enemyLayerMask;

            if (attackCollider != null)
            {
                attackCollider.enabled = false;
            }
        }

        public void PerformAttack()
        {
            if (!canAttack || attackCollider == null)
            {
                return;
            }

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }

            attackRoutine = StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            canAttack = false;
            attackCollider.enabled = true;

            animator?.SetTrigger(attackTriggerName);
            PlayAttackAudio();

            yield return null; // 等一帧确保碰撞体已经启用

            int hitCount = Physics2D.OverlapCollider(attackCollider, contactFilter, overlapResults);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D target = overlapResults[i];
                if (target == null)
                {
                    continue;
                }

                var enemy = target.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(attackDamage);
                }

                EventBus.Publish(new PlayerAttackEvent
                {
                    Position = target.transform.position,
                    Direction = controller != null ? controller.Movement.LastMoveDirection : Vector2.up,
                    Damage = attackDamage,
                    Range = hitboxActiveTime
                });
            }

            yield return new WaitForSeconds(hitboxActiveTime);
            attackCollider.enabled = false;

            yield return new WaitForSeconds(Mathf.Max(0f, attackCooldown - hitboxActiveTime));
            canAttack = true;
            attackRoutine = null;
        }

        private void PlayAttackAudio()
        {
            if (attackClip == null || audioSource == null)
            {
                return;
            }
            audioSource.PlayOneShot(attackClip);
        }
    }
}
