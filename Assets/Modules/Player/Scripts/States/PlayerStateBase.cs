// ═══════════════════════════════════════════════════════════════════════════
//  PlayerStateBase - 玩家状态基类
//  提供所有状态共享的功能和对 PlayerController 的访问
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace EdgeRunner.Player.States
{
    /// <summary>
    /// 玩家状态基类
    /// 所有具体状态继承此类
    /// </summary>
    public abstract class PlayerStateBase : IPlayerState
    {
        // ═══════════════════════════════════════════════════════════════
        //                          引用
        // ═══════════════════════════════════════════════════════════════

        protected readonly PlayerStateMachine stateMachine;
        protected readonly IPlayerContext context;

        // ═══════════════════════════════════════════════════════════════
        //                          属性
        // ═══════════════════════════════════════════════════════════════

        public abstract string StateName { get; }

        /// <summary>状态进入时间</summary>
        protected float stateEnterTime;

        /// <summary>状态持续时间</summary>
        protected float StateDuration => Time.time - stateEnterTime;

        // ═══════════════════════════════════════════════════════════════
        //                          构造函数
        // ═══════════════════════════════════════════════════════════════

        protected PlayerStateBase(PlayerStateMachine stateMachine, IPlayerContext context)
        {
            this.stateMachine = stateMachine;
            this.context = context;
        }

        // ═══════════════════════════════════════════════════════════════
        //                          IPlayerState 实现
        // ═══════════════════════════════════════════════════════════════

        public virtual void OnEnter()
        {
            stateEnterTime = Time.time;
            Debug.Log($"[PlayerState] 进入状态: {StateName}");
        }

        public virtual void OnUpdate()
        {
            // 子类重写
        }

        public virtual void OnFixedUpdate()
        {
            // 子类重写
        }

        public virtual void OnExit()
        {
            Debug.Log($"[PlayerState] 退出状态: {StateName}");
        }

        public virtual bool CanEnter()
        {
            return true;
        }

        // ═══════════════════════════════════════════════════════════════
        //                          通用移动/旋转方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 根据移动输入更新角色朝向目标角度
        /// </summary>
        protected void UpdateCharacterRotation()
        {
            if (context.MoveInput == Vector2.zero) return;
            float angle = Mathf.Atan2(context.MoveInput.y, context.MoveInput.x) * Mathf.Rad2Deg + 90f;
            context.TargetRotation = angle;
        }

        /// <summary>
        /// 平滑应用旋转
        /// </summary>
        protected void ApplyRotationSmoothly()
        {
            Quaternion target = Quaternion.Euler(0f, 0f, context.TargetRotation);
            if (context.RotationSmoothness <= 0f)
            {
                context.Transform.rotation = target;
            }
            else
            {
                context.Transform.rotation = Quaternion.Lerp(
                    context.Transform.rotation,
                    target,
                    context.RotationSmoothness * Time.deltaTime
                );
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          状态切换方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 切换到另一个状态
        /// </summary>
        protected void TransitionTo<T>() where T : IPlayerState
        {
            stateMachine.TransitionTo<T>();
        }
    }
}
