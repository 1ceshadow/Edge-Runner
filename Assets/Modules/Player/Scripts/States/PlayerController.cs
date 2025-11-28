// ═══════════════════════════════════════════════════════════════════════════
//  PlayerController - 玩家控制器（简化版）
//  
//  职责：
//  - 作为状态机和各子系统的中央协调器
//  - 提供对玩家组件和数据的统一访问
//  - 所有参数强制从 ConfigManager 读取
//  
//  架构：
//  ┌─────────────────────────────────────────────────────────────┐
//  │                     PlayerController                        │
//  │  ┌─────────────┐  ┌─────────────┐  ┌───────────────────┐    │
//  │  │ StateMachine│  │ SkillManager│  │ PlayerMovement    │    │
//  │  │ (运动状态) │  │ (技能管理) │  │ (移动+冲刺+墙检测)│    │
//  │  └─────────────┘  └─────────────┘  └───────────────────┘    │
//  └─────────────────────────────────────────────────────────────┘
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using EdgeRunner.Events;
using EdgeRunner.Player.States;
using EdgeRunner.Player.Skills;
using EdgeRunner.Config;
using EdgeRunner.Player.Systems;

namespace EdgeRunner.Player
{
    /// <summary>
    /// 玩家控制器 - 状态机和技能管理器的上下文
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(AudioSource))]
    public class PlayerController : MonoBehaviour, IPlayerContext
    {
        // ═══════════════════════════════════════════════════════════════
        //                          组件引用
        // ═══════════════════════════════════════════════════════════════

        public Rigidbody2D Rb { get; private set; }
        public AudioSource AudioSource { get; private set; }
        public CircleCollider2D CircleCollider { get; private set; }
        
        // 子系统
        public PlayerStateMachine StateMachine { get; private set; }
        public PlayerSkillManager SkillManager { get; private set; }
        public PlayerMovement Movement { get; private set; }
        public PlayerEnergySystem Energy { get; private set; }
        public PlayerHealthSystem Health { get; private set; }
        public PlayerCombatSystem Combat { get; private set; }
        public PlayerInputHandler InputHandler { get; private set; }

        // ═══════════════════════════════════════════════════════════════
        //                          资源引用
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ 资源引用 ═══")]
        [SerializeField] private LayerMask wallLayerMask;       // 墙体层（冲刺碰撞）
        [SerializeField] private LayerMask billboardLayerMask;  // 广告牌层（吸附功能）
        
        [Header("═══ 音效 ═══")]
        [SerializeField] private AudioClip dashAudioClip;
        [SerializeField] private AudioClip timeSlowStartClip;
        [SerializeField] private AudioClip timeSlowEndClip;

        // ═══════════════════════════════════════════════════════════════
        //                          配置访问
        // ═══════════════════════════════════════════════════════════════

        private PlayerConfig Config => ConfigManager.Player;

        public float MoveSpeed => ConfigManager.GetMoveSpeed();
        public float RotationSmoothness => ConfigManager.GetRotationSmoothness();
        public float DashDistance => ConfigManager.GetDashDistance();
        public float DashCooldown => ConfigManager.GetDashCooldown();
        public float PerfectDashDetectRange => ConfigManager.GetPerfectDashDetectRange();
        public float PerfectDashReward => ConfigManager.GetPerfectDashReward();
        public float TimeSlowScale => ConfigManager.GetTimeSlowScale();
        public float TimeSlowPlayerSpeed => Config?.TimeSlowPlayerSpeed ?? 20f;
        public float MaxEnergy => ConfigManager.GetMaxEnergy();
        public float RechargeRate => Config?.EnergyRechargeRate ?? 2f;
        public float EnergyDrainRate => ConfigManager.GetEnergyDrainRate();
        public float MinEnergyThreshold => ConfigManager.GetMinEnergyThreshold();
        public float KillReward => ConfigManager.GetKillEnergyReward();
        public float WallCheckExtra => ConfigManager.GetWallCheckExtra();

        // 资源引用
        public LayerMask WallLayerMask => wallLayerMask;
        public AudioClip DashAudioClip => dashAudioClip;
        public AudioClip TimeSlowStartClip => timeSlowStartClip;
        public AudioClip TimeSlowEndClip => timeSlowEndClip;

        // ═══════════════════════════════════════════════════════════════
        //                          运行时状态
        // ═══════════════════════════════════════════════════════════════

        public Vector2 MoveInput { get; private set; }
        
        public Vector2 LastMoveDirection
        {
            get => Movement?.LastMoveDirection ?? Vector2.right;
            set { /* Movement 内部管理，忽略外部设置 */ }
        }
        
        public float TargetRotation
        {
            get => Movement?.TargetRotation ?? 90f;
            set { /* Movement 内部管理，忽略外部设置 */ }
        }
        
        public float CurrentEnergy => Energy?.CurrentEnergy ?? 0f;
        public bool IsRewarded { get; set; }
        
        // 冲刺状态（委托给 Movement）
        public bool CanDash => Movement?.CanDash ?? false;
        public bool IsDashing => Movement?.IsDashing ?? false;
        
        // 时缓状态
        public bool IsTimeSlowed => SkillManager?.IsTimeSlowed ?? false;
        
        // 计算属性
        public float CurrentMoveSpeed => IsTimeSlowed ? TimeSlowPlayerSpeed : MoveSpeed;

        // IPlayerContext 实现
        Transform IPlayerContext.Transform => transform;
        Rigidbody2D IPlayerContext.Rigidbody => Rb;

        // ═══════════════════════════════════════════════════════════════
        //                          生命周期
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // 缓存组件
            Rb = GetComponent<Rigidbody2D>();
            AudioSource = GetComponent<AudioSource>();
            CircleCollider = GetComponent<CircleCollider2D>();

            // 配置刚体
            Rb.bodyType = RigidbodyType2D.Dynamic;
            Rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            Rb.gravityScale = 0f;
            Rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // 初始化子系统
            InputHandler = EnsureComponent<PlayerInputHandler>();
            
            Movement = EnsureComponent<PlayerMovement>();
            // 确保 wallLayerMask 有效（用于冲刺碰撞）
            if (wallLayerMask == 0)
            {
                wallLayerMask = LayerMask.GetMask("Wall");
            }
            Movement.SetWallLayerMask(wallLayerMask);
            // 确保 billboardLayerMask 有效（用于吸附）
            if (billboardLayerMask == 0)
            {
                billboardLayerMask = LayerMask.GetMask("Billboard");
            }
            Movement.SetBillboardLayerMask(billboardLayerMask);
            Movement.OnDashCompleted += OnDashCompleted;
            
            Energy = EnsureComponent<PlayerEnergySystem>();
            Energy.Initialize(this);
            
            Health = EnsureComponent<PlayerHealthSystem>();
            Health.Initialize(this);
            
            Combat = EnsureComponent<PlayerCombatSystem>();
            Combat.Initialize(this);
            
            SkillManager = EnsureComponent<PlayerSkillManager>();
            StateMachine = EnsureComponent<PlayerStateMachine>();
        }

        private void Start()
        {
            ValidateConfig();
            StateMachine.Initialize(this);
        }

        private void ValidateConfig()
        {
            if (Config == null)
            {
                Debug.LogWarning(
                    "[PlayerController] ConfigManager.Player 为 null，使用默认值"
                );
            }
            else
            {
                Debug.Log($"✓ PlayerController: 配置已加载 (速度={MoveSpeed}, 冲刺={DashDistance})");
            }
        }

        private void OnEnable()
        {
            SubscribeInputEvents();
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        private void OnDisable()
        {
            UnsubscribeInputEvents();
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            Movement?.CancelDash();
        }

        private void Update()
        {
            Energy?.Tick(IsTimeSlowed);
        }

        // ═══════════════════════════════════════════════════════════════
        //                          事件处理
        // ═══════════════════════════════════════════════════════════════

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            if (!evt.KilledByPlayer) return;
            GrantReward(evt.EnergyReward, RewardType.EnemyKill, evt.Position);
        }

        private void OnDashCompleted(Vector2 start, Vector2 end, bool isPerfect)
        {
            PlayOneShot(dashAudioClip);
            
            if (isPerfect)
            {
                GrantReward(PerfectDashReward, RewardType.PerfectDash, start);
                PlayOneShot(timeSlowStartClip);
            }
            
            EventBus.Publish(new PlayerDashedEvent
            {
                StartPosition = start,
                EndPosition = end,
                IsPerfectDash = isPerfect
            });
        }

        // ═══════════════════════════════════════════════════════════════
        //                          公共方法
        // ═══════════════════════════════════════════════════════════════

        public void PlayOneShot(AudioClip clip)
        {
            if (clip != null && AudioSource != null)
            {
                AudioSource.PlayOneShot(clip);
            }
        }

        public void PublishEnergyChangedEvent(EnergyChangeReason reason, float delta = 0f)
        {
            EventBus.Publish(new PlayerEnergyChangedEvent
            {
                CurrentEnergy = CurrentEnergy,
                MaxEnergy = MaxEnergy,
                DeltaEnergy = delta,
                Reason = reason
            });
        }

        public void GrantReward(float amount, RewardType type, Vector2 position)
        {
            if (amount <= 0f) return;

            EnergyChangeReason reason = type switch
            {
                RewardType.PerfectDash => EnergyChangeReason.PerfectDash,
                RewardType.EnemyKill => EnergyChangeReason.EnemyKill,
                _ => EnergyChangeReason.Other
            };

            Energy?.AddReward(amount, reason);
            IsRewarded = true;

            EventBus.Publish(new PlayerRewardedEvent
            {
                Type = type,
                Amount = amount,
                Position = position
            });
        }

        /// <summary>
        /// 更新墙体检测
        /// </summary>
        public void UpdateWallTouching() => Movement?.UpdateWallContacts();

        /// <summary>
        /// 获取过滤后的移动输入
        /// </summary>
        public Vector2 GetFilteredMoveInput() => Movement?.GetFilteredInput(MoveInput) ?? MoveInput;

        /// <summary>
        /// 获取玩家半径
        /// </summary>
        public float GetPlayerRadius() => CircleCollider != null ? CircleCollider.radius : 0.3f;

        /// <summary>
        /// 检查是否可以使用时缓
        /// </summary>
        public bool CanUseTimeSlow() => Energy != null && Energy.CanUseTimeSlow(MinEnergyThreshold);

        /// <summary>
        /// 设置时缓状态
        /// </summary>
        public void SetTimeSlowState(bool active) => Energy?.SetTimeSlowState(active);

        /// <summary>
        /// 尝试执行冲刺
        /// </summary>
        public bool TryDash()
        {
            if (Movement == null || !Movement.CanDash) return false;
            
            // 检测完美闪避
            bool isPerfect = CheckPerfectDash();
            
            return Movement.TryDash(MoveInput, DashDistance, DashCooldown, isPerfect);
        }
        
        private bool CheckPerfectDash()
        {
            if (BulletManager.Instance == null) return false;
            return BulletManager.Instance.CheckBulletsInRange(Rb.position, PerfectDashDetectRange);
        }

        // ═══════════════════════════════════════════════════════════════
        //                          输入订阅
        // ═══════════════════════════════════════════════════════════════

        private void SubscribeInputEvents()
        {
            if (InputHandler == null) return;
            
            InputHandler.MoveChanged += v => MoveInput = v;
            InputHandler.DashPerformed += () => TryDash();
            InputHandler.TimeSlowPerformed += () => SkillManager?.ToggleTimeSlow();
            InputHandler.AttackPerformed += () => Combat?.PerformAttack();
        }

        private void UnsubscribeInputEvents()
        {
            // InputHandler 会在 OnDisable 时自动清理
        }

        private T EnsureComponent<T>() where T : Component
        {
            return TryGetComponent<T>(out var c) ? c : gameObject.AddComponent<T>();
        }
    }
}
