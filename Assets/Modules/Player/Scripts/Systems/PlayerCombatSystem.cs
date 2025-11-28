// ═══════════════════════════════════════════════════════════════════════════
//  PlayerCombatSystem - 玩家战斗系统 (重构版)
//  整合剑攻击和连击系统，负责攻击的协调和事件派发
//  
//  职责：
//  - 接收攻击输入，协调 SwordHitbox 和 ComboSystem
//  - 处理命中检测结果，造成伤害
//  - 派发攻击相关事件
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;
using EdgeRunner.Events;
using EdgeRunner.Config;
using EdgeRunner.Player.Combat;

namespace EdgeRunner.Player.Systems
{
    /// <summary>
    /// 玩家战斗系统
    /// 管理剑攻击、连击和伤害判定
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerCombatSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        //                          序列化字段
        // ═══════════════════════════════════════════════════════════════

        [Header("剑配置")]
        [SerializeField] private SwordHitbox swordHitbox;
        [SerializeField] private LayerMask enemyLayerMask = ~0;

        [Header("音效")]
        [SerializeField] private AudioClip[] swingSounds;       // 挥砍音效（可多个随机播放）
        [SerializeField] private AudioClip[] hitSounds;         // 命中音效
        [SerializeField] private AudioClip comboFinisherSound;  // 终结技音效

        [Header("视觉效果")]
        [SerializeField] private SlashTrail slashTrail;         // 挥砍拖尾特效

        [Header("调试")]
        [SerializeField] private bool logComboInfo = false;

        // ═══════════════════════════════════════════════════════════════
        //                          私有字段
        // ═══════════════════════════════════════════════════════════════

        private PlayerController controller;
        private AudioSource audioSource;
        private ComboSystem comboSystem;
        
        private float attackCooldown;
        private float lastAttackTime;
        private int baseDamage;
        private float swingDuration;

        private Coroutine attackRoutine;

        // ═══════════════════════════════════════════════════════════════
        //                          公共属性
        // ═══════════════════════════════════════════════════════════════

        /// <summary>当前连击数</summary>
        public int CurrentCombo => comboSystem?.CurrentCombo ?? 0;

        /// <summary>是否正在攻击</summary>
        public bool IsAttacking => comboSystem?.IsAttacking ?? false;

        /// <summary>是否可以攻击</summary>
        public bool CanAttack => Time.time >= lastAttackTime + attackCooldown && !IsAttacking;

        // ═══════════════════════════════════════════════════════════════
        //                          初始化
        // ═══════════════════════════════════════════════════════════════

        public void Initialize(PlayerController ctx)
        {
            controller = ctx;
            audioSource = GetComponent<AudioSource>();

            // 从配置加载参数
            attackCooldown = ConfigManager.GetAttackCooldown();
            baseDamage = ConfigManager.GetCombatBaseDamage();
            swingDuration = ConfigManager.GetSwingDuration();

            // 初始化连击系统
            comboSystem = new ComboSystem();
            comboSystem.OnComboChanged += OnComboChanged;
            comboSystem.OnComboReset += OnComboReset;

            // 初始化剑
            if (swordHitbox != null)
            {
                swordHitbox.Initialize(enemyLayerMask);
                swordHitbox.OnHitEnemy += OnSwordHitEnemy;
            }
            else
            {
                Debug.LogWarning("[PlayerCombatSystem] SwordHitbox 未设置！攻击系统将无法正常工作。");
            }

            lastAttackTime = -999f;
        }

        private void OnDestroy()
        {
            if (comboSystem != null)
            {
                comboSystem.OnComboChanged -= OnComboChanged;
                comboSystem.OnComboReset -= OnComboReset;
            }

            if (swordHitbox != null)
            {
                swordHitbox.OnHitEnemy -= OnSwordHitEnemy;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          更新循环
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            comboSystem?.Update();
        }

        // ═══════════════════════════════════════════════════════════════
        //                          攻击执行
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 执行攻击
        /// </summary>
        public void PerformAttack()
        {
            // 检查冷却
            if (Time.time < lastAttackTime + attackCooldown)
            {
                return;
            }

            // 检查是否正在攻击
            if (IsAttacking)
            {
                return;
            }

            // 尝试获取连击数据
            if (comboSystem == null)
            {
                Debug.LogWarning("[PlayerCombatSystem] ComboSystem 未初始化");
                return;
            }

            var comboData = comboSystem.TryAttack();
            if (!comboData.HasValue)
            {
                return;
            }

            lastAttackTime = Time.time;

            // 停止之前的攻击协程
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }

            attackRoutine = StartCoroutine(AttackRoutine(comboData.Value));
        }

        private IEnumerator AttackRoutine(ComboData comboData)
        {
            // 获取玩家朝向
            Vector2 facingDirection = GetFacingDirection();

            // 计算实际挥砍时间
            float actualDuration = swingDuration / comboData.SpeedMultiplier;

            // 播放挥砍音效
            PlaySwingSound(comboData.ComboIndex);

            // 触发拖尾特效（可选）
            slashTrail?.StartTrail(facingDirection, comboData.StartAngle, comboData.EndAngle, actualDuration);

            // 开始挥砍（可选 - 没有 SwordHitbox 也能攻击）
            swordHitbox?.StartSwing(facingDirection, comboData.StartAngle, comboData.EndAngle, actualDuration);

            // 如果没有 SwordHitbox，使用传统的 OverlapCircle 检测
            if (swordHitbox == null)
            {
                PerformFallbackAttack(facingDirection, comboData);
            }

            if (logComboInfo)
            {
                Debug.Log($"[Combat] {comboData.Name} (连击 {comboData.ComboIndex + 1}) - 角度: {comboData.StartAngle}° → {comboData.EndAngle}°");
            }

            // 等待挥砍完成
            yield return new WaitForSeconds(actualDuration);

            // 通知连击系统攻击完成
            comboSystem.OnAttackComplete();
            attackRoutine = null;
        }

        // ═══════════════════════════════════════════════════════════════
        //                          命中处理
        // ═══════════════════════════════════════════════════════════════

        private void OnSwordHitEnemy(Collider2D enemyCollider)
        {
            if (enemyCollider == null) return;

            // 计算伤害
            int damage = comboSystem.CalculateDamage(baseDamage);

            // 尝试对敌人造成伤害
            var enemy = enemyCollider.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // 播放命中音效
            PlayHitSound();

            // 派发攻击事件
            EventBus.Publish(new PlayerAttackEvent
            {
                Position = enemyCollider.transform.position,
                Direction = GetFacingDirection(),
                Damage = damage,
                Range = ConfigManager.GetSwordLength()
            });

            if (logComboInfo)
            {
                Debug.Log($"[Combat] 命中敌人！伤害: {damage} (连击 {comboSystem.CurrentCombo})");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          连击事件
        // ═══════════════════════════════════════════════════════════════

        private void OnComboChanged(int comboCount)
        {
            // 可以在这里触发 UI 更新、屏幕震动等
            EventBus.Publish(new ComboChangedEvent
            {
                ComboCount = comboCount,
                IsFinisher = comboSystem.IsFinisher
            });
        }

        private void OnComboReset()
        {
            EventBus.Publish(new ComboResetEvent());
        }

        // ═══════════════════════════════════════════════════════════════
        //                          音效
        // ═══════════════════════════════════════════════════════════════

        private void PlaySwingSound(int comboIndex)
        {
            if (audioSource == null) return;

            // 终结技使用特殊音效
            if (comboIndex >= ConfigManager.GetMaxComboCount() - 1 && comboFinisherSound != null)
            {
                audioSource.PlayOneShot(comboFinisherSound);
                return;
            }

            // 普通挥砍随机选择音效
            if (swingSounds != null && swingSounds.Length > 0)
            {
                var clip = swingSounds[Random.Range(0, swingSounds.Length)];
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
        }

        private void PlayHitSound()
        {
            if (audioSource == null || hitSounds == null || hitSounds.Length == 0) return;

            var clip = hitSounds[Random.Range(0, hitSounds.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          工具方法
        // ═══════════════════════════════════════════════════════════════

        private Vector2 GetFacingDirection()
        {
            if (controller?.Movement != null)
            {
                var dir = controller.Movement.LastMoveDirection;
                if (dir != Vector2.zero)
                {
                    return dir;
                }
            }
            return Vector2.up; // 默认朝上
        }

        /// <summary>
        /// 回退攻击检测（当没有 SwordHitbox 时使用 OverlapCircle）
        /// </summary>
        private void PerformFallbackAttack(Vector2 facingDirection, ComboData comboData)
        {
            float attackRange = ConfigManager.GetSwordLength();
            Vector2 attackCenter = (Vector2)transform.position + facingDirection * (attackRange * 0.5f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRange, enemyLayerMask);

            foreach (var hit in hits)
            {
                if (hit != null && hit.gameObject != gameObject)
                {
                    // 计算伤害
                    int damage = Mathf.RoundToInt(baseDamage * comboData.DamageMultiplier);

                    // 尝试造成伤害
                    if (hit.TryGetComponent<EnemyController>(out var enemy))
                    {
                        enemy.TakeDamage(damage);
                        PlayHitSound();

                        // 发布攻击事件
                        EventBus.Publish(new PlayerAttackEvent
                        {
                            Position = hit.transform.position,
                            Direction = facingDirection,
                            Damage = damage,
                            Range = attackRange
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 强制重置连击状态
        /// </summary>
        public void ResetCombo()
        {
            comboSystem?.ResetCombo();
            swordHitbox?.StopSwing();

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
        }
    }
}
