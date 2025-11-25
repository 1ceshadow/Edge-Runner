using UnityEngine;

/// <summary>
/// 玩家输入处理器 - 仅处理输入，不处理逻辑
/// 
/// 职责:
/// - 监听输入事件
/// - 将输入转发给相应系统
/// 
/// 注意: 这个类**不应该**包含任何游戏逻辑!
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerMovementController movementController;
    private PlayerCombatSystem combatSystem;

    public void Initialize(PlayerMovementController movement, PlayerCombatSystem combat)
    {
        movementController = movement;
        combatSystem = combat;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleCombatInput();
    }

    private void HandleMovementInput()
    {
        // 使用新输入系统或 Legacy Input
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        var moveInput = new Vector2(moveX, moveY);

        if (movementController != null)
        {
            movementController.SetMoveInput(moveInput);
        }
    }

    private void HandleCombatInput()
    {
        // 冲刺/闪现
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (combatSystem != null)
                combatSystem.TryDash();
        }

        // 时缓
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (combatSystem != null)
                combatSystem.TryActivateTimeSlow();
        }
    }
}
