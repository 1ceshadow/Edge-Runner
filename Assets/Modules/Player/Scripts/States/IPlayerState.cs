// ═══════════════════════════════════════════════════════════════════════════
//  IPlayerState - 玩家状态接口
//  定义所有玩家状态必须实现的方法
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace EdgeRunner.Player.States
{
    /// <summary>
    /// 玩家状态接口
    /// </summary>
    public interface IPlayerState
    {
        /// <summary>状态名称（调试用）</summary>
        string StateName { get; }

        /// <summary>进入状态时调用</summary>
        void OnEnter();

        /// <summary>每帧更新（Update）</summary>
        void OnUpdate();

        /// <summary>物理更新（FixedUpdate）</summary>
        void OnFixedUpdate();

        /// <summary>退出状态时调用</summary>
        void OnExit();

        /// <summary>检查是否可以转换到此状态</summary>
        bool CanEnter();
    }
}
