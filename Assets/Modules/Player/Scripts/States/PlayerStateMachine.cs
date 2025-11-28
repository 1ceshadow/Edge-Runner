// ═══════════════════════════════════════════════════════════════════════════
//  PlayerStateMachine - 玩家状态机
//  管理玩家的所有状态和状态转换
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using EdgeRunner.Events;

namespace EdgeRunner.Player.States
{
    /// <summary>
    /// 玩家状态机
    /// 管理玩家状态的注册、转换和更新
    /// </summary>
    public class PlayerStateMachine : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        //                          字段
        // ═══════════════════════════════════════════════════════════════

        private Dictionary<Type, IPlayerState> states = new();
        private IPlayerState currentState;
        private IPlayerState previousState;

        // DI 注入的服务
        private IBulletManager bulletManager;

        // ═══════════════════════════════════════════════════════════════
        //                          属性
        // ═══════════════════════════════════════════════════════════════

        /// <summary>当前状态</summary>
        public IPlayerState CurrentState => currentState;

        /// <summary>前一个状态</summary>
        public IPlayerState PreviousState => previousState;

        /// <summary>当前状态名称</summary>
        public string CurrentStateName => currentState?.StateName ?? "None";

        // ═══════════════════════════════════════════════════════════════
        //                          公共事件
        // ═══════════════════════════════════════════════════════════════

        /// <summary>状态变化事件</summary>
        public event Action<IPlayerState, IPlayerState> OnStateChanged;

        // ═══════════════════════════════════════════════════════════════
        //                          DI 注入
        // ═══════════════════════════════════════════════════════════════

        [Inject]
        public void Construct(IBulletManager bulletManager)
        {
            this.bulletManager = bulletManager;
        }

        // ═══════════════════════════════════════════════════════════════
        //                          初始化
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 初始化状态机
        /// </summary>
        public void Initialize(PlayerController playerController)
        {
            IPlayerContext context = playerController;

            // 只注册基础运动状态（Idle/Moving）
            // 时缓等辅助技能由 PlayerSkillManager 处理
            RegisterState(new IdleState(this, context));
            RegisterState(new MovingState(this, context));

            // 默认进入 Idle 状态
            TransitionTo<IdleState>();

            Debug.Log($"✓ PlayerStateMachine: 初始化完成，注册了 {states.Count} 个状态");
        }
        
        private void OnDisable()
        {
            // 状态机不再订阅输入事件，由 PlayerController 统一处理
        }

        // ═══════════════════════════════════════════════════════════════
        //                          状态管理
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 注册状态
        /// </summary>
        public void RegisterState(IPlayerState state)
        {
            var type = state.GetType();
            if (states.ContainsKey(type))
            {
                Debug.LogWarning($"[PlayerStateMachine] 状态 {type.Name} 已注册，将被覆盖");
            }
            states[type] = state;
        }

        /// <summary>
        /// 获取状态
        /// </summary>
        public T GetState<T>() where T : IPlayerState
        {
            if (states.TryGetValue(typeof(T), out var state))
            {
                return (T)state;
            }
            return default;
        }

        /// <summary>
        /// 转换到指定状态
        /// </summary>
        public void TransitionTo<T>() where T : IPlayerState
        {
            TransitionTo(typeof(T));
        }

        /// <summary>
        /// 转换到指定状态（Type 版本）
        /// </summary>
        public void TransitionTo(Type stateType)
        {
            if (!states.TryGetValue(stateType, out var newState))
            {
                Debug.LogError($"[PlayerStateMachine] 未找到状态: {stateType.Name}");
                return;
            }

            // 检查是否可以进入新状态
            if (!newState.CanEnter())
            {
                Debug.Log($"[PlayerStateMachine] 无法进入状态 {newState.StateName}");
                return;
            }

            // 退出当前状态
            previousState = currentState;
            currentState?.OnExit();

            // 进入新状态
            currentState = newState;
            currentState.OnEnter();

            // 触发事件
            OnStateChanged?.Invoke(previousState, currentState);
        }

        /// <summary>
        /// 返回前一个状态
        /// </summary>
        public void TransitionToPrevious()
        {
            if (previousState != null)
            {
                TransitionTo(previousState.GetType());
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          更新
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            currentState?.OnUpdate();
        }

        private void FixedUpdate()
        {
            currentState?.OnFixedUpdate();
        }

        // ═══════════════════════════════════════════════════════════════
        //                          检查方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 检查当前是否处于指定状态
        /// </summary>
        public bool IsInState<T>() where T : IPlayerState
        {
            return currentState?.GetType() == typeof(T);
        }

        /// <summary>
        /// 检查当前是否处于指定状态之一
        /// </summary>
        public bool IsInAnyState(params Type[] stateTypes)
        {
            if (currentState == null) return false;
            var currentType = currentState.GetType();
            foreach (var type in stateTypes)
            {
                if (currentType == type) return true;
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════
        //                          调试
        // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 10, 200, 60));
            GUILayout.BeginVertical("box");
            GUILayout.Label($"当前状态: {CurrentStateName}");
            GUILayout.Label($"前状态: {previousState?.StateName ?? "None"}");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif
    }
}
