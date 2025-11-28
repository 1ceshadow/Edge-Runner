using UnityEngine;
using EdgeRunner.Player.Systems;

namespace EdgeRunner.Player.States
{
    /// <summary>
    /// 状态机访问玩家运行时数据与能力的抽象，避免直接依赖 PlayerController 实现细节。
    /// </summary>
    public interface IPlayerContext
    {
        Transform Transform { get; }
        Rigidbody2D Rigidbody { get; }
        PlayerMovement Movement { get; }

        Vector2 MoveInput { get; }
        Vector2 LastMoveDirection { get; set; }
        float CurrentMoveSpeed { get; }
        float RotationSmoothness { get; }
        float TargetRotation { get; set; }
        bool IsDashing { get; }

        void UpdateWallTouching();
        Vector2 GetFilteredMoveInput();
    }
}
