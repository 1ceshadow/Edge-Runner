// ═══════════════════════════════════════════════════════════════════════════
//  IdleState - 待机状态
//  玩家不移动时的状态
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace EdgeRunner.Player.States
{
    /// <summary>
    /// 待机状态
    /// </summary>
    public class IdleState : PlayerStateBase
    {
        public override string StateName => "Idle";

        public IdleState(PlayerStateMachine stateMachine, IPlayerContext context)
            : base(stateMachine, context)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            // 停止移动
            if (context.Rigidbody != null)
            {
                context.Rigidbody.linearVelocity = Vector2.zero;
            }
        }

        public override void OnUpdate()
        {
            // 检查是否有移动输入 → 切换到移动状态
            if (context.MoveInput.sqrMagnitude > 0.01f)
            {
                TransitionTo<MovingState>();
            }
        }

        public override void OnFixedUpdate()
        {
            // 待机时也更新墙体检测
            context.UpdateWallTouching();
        }
    }
}
