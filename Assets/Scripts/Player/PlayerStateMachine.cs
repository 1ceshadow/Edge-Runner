using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家状态机 - 管理玩家状态转换
/// 
/// 职责:
/// - 定义所有可能的玩家状态
/// - 管理状态间的转换
/// - 在每帧调用当前状态的 Update 方法
/// 
/// 可能的状态:
/// - Idle (空闲)
/// - Moving (移动中)
/// - Dashing (冲刺中)
/// - Timeslowing (时缓中)
/// - Dead (已死亡)
/// </summary>
public class PlayerStateMachine : MonoBehaviour
{
    private Dictionary<System.Type, IPlayerState> states = new();
    private IPlayerState currentState;
    private PlayerRoot playerRoot;

    public void Initialize(PlayerRoot root)
    {
        playerRoot = root;

        // 创建所有状态
        states[typeof(PlayerIdleState)] = new PlayerIdleState(this, playerRoot);
        states[typeof(PlayerMovingState)] = new PlayerMovingState(this, playerRoot);
        states[typeof(PlayerDashingState)] = new PlayerDashingState(this, playerRoot);
        states[typeof(PlayerDeadState)] = new PlayerDeadState(this, playerRoot);

        // 初始状态为 Idle
        TransitionTo<PlayerIdleState>();

        // 订阅事件
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);

        Debug.Log("✓ PlayerStateMachine initialized");
    }

    private void Update()
    {
        currentState?.Update();
    }

    public void TransitionTo<T>() where T : IPlayerState
    {
        var targetType = typeof(T);
        if (!states.ContainsKey(targetType))
        {
            Debug.LogError($"State {targetType.Name} not registered!");
            return;
        }

        currentState?.OnExit();
        currentState = states[targetType];
        currentState.OnEnter();
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        TransitionTo<PlayerDeadState>();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
    }
}

// ═══════════════════════════════════════════════════════════════
//                          具体状态实现
// ═══════════════════════════════════════════════════════════════

public class PlayerIdleState : IPlayerState
{
    private PlayerStateMachine stateMachine;
    private PlayerRoot playerRoot;

    public PlayerIdleState(PlayerStateMachine sm, PlayerRoot root)
    {
        stateMachine = sm;
        playerRoot = root;
    }

    public void OnEnter() { }

    public void Update()
    {
        // 检查是否应该转换到其他状态
        // 此处可添加条件检查
    }

    public void OnExit() { }
}

public class PlayerMovingState : IPlayerState
{
    private PlayerStateMachine stateMachine;
    private PlayerRoot playerRoot;

    public PlayerMovingState(PlayerStateMachine sm, PlayerRoot root)
    {
        stateMachine = sm;
        playerRoot = root;
    }

    public void OnEnter() { }

    public void Update()
    {
        // 移动状态逻辑
    }

    public void OnExit() { }
}

public class PlayerDashingState : IPlayerState
{
    private PlayerStateMachine stateMachine;
    private PlayerRoot playerRoot;
    private float dashDuration = 0.2f;
    private float dashTimer = 0f;

    public PlayerDashingState(PlayerStateMachine sm, PlayerRoot root)
    {
        stateMachine = sm;
        playerRoot = root;
    }

    public void OnEnter()
    {
        dashTimer = dashDuration;
    }

    public void Update()
    {
        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0)
        {
            stateMachine.TransitionTo<PlayerIdleState>();
        }
    }

    public void OnExit() { }
}

public class PlayerDeadState : IPlayerState
{
    private PlayerStateMachine stateMachine;
    private PlayerRoot playerRoot;

    public PlayerDeadState(PlayerStateMachine sm, PlayerRoot root)
    {
        stateMachine = sm;
        playerRoot = root;
    }

    public void OnEnter()
    {
        playerRoot.gameObject.SetActive(false);
    }

    public void Update() { }

    public void OnExit() { }
}
