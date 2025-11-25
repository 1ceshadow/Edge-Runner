using UnityEngine;

/// <summary>
/// 玩家根组件 - 协调所有玩家相关的系统
/// 
/// 职责:
/// - 初始化所有子系统
/// - 在 ServiceLocator 中注册 IPlayerService
/// - 协调子系统间的通信 (通过事件)
/// 
/// 架构:
/// PlayerRoot (此脚本)
/// ├── PlayerMovementController (移动控制)
/// ├── PlayerEnergySystem (能量管理)
/// ├── PlayerHealthSystem (生命值管理)
/// ├── PlayerStateMachine (状态机)
/// ├── PlayerCombatSystem (战斗系统)
/// ├── PlayerInputHandler (输入处理)
/// └── Rigidbody2D, Animator (Unity 组件)
/// </summary>
public class PlayerRoot : MonoBehaviour, IPlayerService
{
    [Header("=== 子系统引用 ===")]
    [SerializeField] private PlayerMovementController movementController;
    [SerializeField] private PlayerEnergySystem energySystem;
    [SerializeField] private PlayerHealthSystem healthSystem;
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private PlayerCombatSystem combatSystem;
    [SerializeField] private PlayerInputHandler inputHandler;

    private bool isAlive = true;

    private void Awake()
    {
        // 获取或创建子系统
        if (movementController == null)
            movementController = GetComponent<PlayerMovementController>();
        if (energySystem == null)
            energySystem = GetComponent<PlayerEnergySystem>();
        if (healthSystem == null)
            healthSystem = GetComponent<PlayerHealthSystem>();
        if (stateMachine == null)
            stateMachine = GetComponent<PlayerStateMachine>();
        if (combatSystem == null)
            combatSystem = GetComponent<PlayerCombatSystem>();
        if (inputHandler == null)
            inputHandler = GetComponent<PlayerInputHandler>();

        // 在 ServiceLocator 中注册此玩家为 IPlayerService
        ServiceLocator.Register<IPlayerService>(this);

        // 初始化子系统
        InitializeSubsystems();
    }

    private void InitializeSubsystems()
    {
        // 按顺序初始化 (顺序很重要!)
        if (energySystem != null)
            energySystem.Initialize();

        if (healthSystem != null)
            healthSystem.Initialize();

        if (movementController != null)
            movementController.Initialize();

        if (combatSystem != null)
            combatSystem.Initialize(energySystem);

        if (stateMachine != null)
            stateMachine.Initialize(this);

        if (inputHandler != null)
            inputHandler.Initialize(movementController, combatSystem);
    }

    private void OnEnable()
    {
        // 订阅事件
        EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnDisable()
    {
        // 取消订阅
        EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnPlayerDamaged(PlayerDamagedEvent evt)
    {
        Debug.Log($"Player damaged for {evt.DamageAmount} points from {evt.DamageSource}");
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        isAlive = false;
        Debug.Log($"Player died at position {evt.DeathPosition}");
    }

    private void OnDestroy()
    {
        // 注销玩家服务
        ServiceLocator.Unregister<IPlayerService>();
    }

    // ═══════════════════════════════════════════════════════════
    //                      IPlayerService 实现
    // ═══════════════════════════════════════════════════════════

    public Vector2 GetPosition() => transform.position;

    public void TakeDamage(int damage, Vector2 damageSource)
    {
        if (!isAlive) return;

        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damage);
            EventBus.Publish(new PlayerDamagedEvent
            {
                DamageAmount = damage,
                DamageSource = damageSource
            });
        }
    }

    public bool HasBulletsInDashRange()
    {
        if (combatSystem == null) return false;
        return combatSystem.CheckBulletsInDashRange();
    }

    public float GetCurrentEnergy() => energySystem != null ? energySystem.GetCurrentEnergy() : 0f;

    public float GetMaxEnergy() => energySystem != null ? energySystem.GetMaxEnergy() : 100f;

    public bool IsAlive() => isAlive;
}
