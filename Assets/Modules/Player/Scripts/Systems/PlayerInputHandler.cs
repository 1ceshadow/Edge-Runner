using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EdgeRunner.Player.Systems
{
    /// <summary>
    /// 封装新输入系统，向其它子系统提供事件驱动的输入数据。
    /// 仅负责采集输入，不包含任何游戏逻辑，方便在单元测试中mock。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        private PlayerInputActions inputActions;

        /// <summary>最新的移动输入值（已归一化）。</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>移动输入发生变化时触发。</summary>
        public event Action<Vector2> MoveChanged;

        /// <summary>按下冲刺键时触发。</summary>
        public event Action DashPerformed;

        /// <summary>按下时缓键时触发。</summary>
        public event Action TimeSlowPerformed;

        /// <summary>按下攻击键时触发，便于战斗系统监听。</summary>
        public event Action AttackPerformed;

        private void Awake()
        {
            inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new PlayerInputActions();
            }
            
            inputActions.Enable();
            inputActions.Player.Move.performed += OnMovePerformed;
            inputActions.Player.Move.canceled += OnMoveCanceled;
            inputActions.Player.Dash.performed += OnDashPerformed;
            inputActions.Player.TimeSlow.performed += OnTimeSlowPerformed;
            inputActions.Player.Attack.performed += OnAttackPerformed;
        }

        private void OnDisable()
        {
            inputActions.Player.Move.performed -= OnMovePerformed;
            inputActions.Player.Move.canceled -= OnMoveCanceled;
            inputActions.Player.Dash.performed -= OnDashPerformed;
            inputActions.Player.TimeSlow.performed -= OnTimeSlowPerformed;
            inputActions.Player.Attack.performed -= OnAttackPerformed;
            inputActions.Disable();
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            MoveInput = ctx.ReadValue<Vector2>();
            MoveChanged?.Invoke(MoveInput);
        }

        private void OnMoveCanceled(InputAction.CallbackContext ctx)
        {
            MoveInput = Vector2.zero;
            MoveChanged?.Invoke(MoveInput);
        }

        private void OnDashPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
            {
                return;
            }
            DashPerformed?.Invoke();
        }

        private void OnTimeSlowPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
            {
                return;
            }
            TimeSlowPerformed?.Invoke();
        }

        private void OnAttackPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
            {
                return;
            }
            AttackPerformed?.Invoke();
        }
    }
}
