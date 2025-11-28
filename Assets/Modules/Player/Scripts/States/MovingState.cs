// ═══════════════════════════════════════════════════════════════════════════
//  MovingState - 移动状态
//  玩家正常移动时的状态
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace EdgeRunner.Player.States
{
    /// <summary>
    /// 移动状态
    /// </summary>
    public class MovingState : PlayerStateBase
    {
        public override string StateName => "Moving";

        public MovingState(PlayerStateMachine stateMachine, IPlayerContext context)
            : base(stateMachine, context)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnUpdate()
        {
            // 检查是否停止移动 → 切换到待机状态
            if (context.MoveInput.sqrMagnitude < 0.01f)
            {
                TransitionTo<IdleState>();
                return;
            }

            // 更新朝向
            UpdateCharacterRotation();
        }

        public override void OnFixedUpdate()
        {
            if (context.Movement == null)
            {
                return;
            }

            // 冲刺中不执行普通移动
            if (context.IsDashing)
            {
                return;
            }
            
            // 更新墙体检测（方向射线）
            context.UpdateWallTouching();

            // 获取过滤后的输入
            Vector2 filteredInput = context.GetFilteredMoveInput();

            // 使用 Movement.Move() 进行移动，内部会处理穿墙检测和回退
            context.Movement.Move(filteredInput, context.CurrentMoveSpeed, Time.fixedDeltaTime);

            // 更新旋转（Movement 内部会更新 LastMoveDirection）
            context.Movement.UpdateRotation(context.MoveInput, context.RotationSmoothness, Time.fixedDeltaTime);
        }
    }
}
