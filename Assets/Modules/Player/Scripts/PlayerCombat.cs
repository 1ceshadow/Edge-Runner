using UnityEngine;
using EdgeRunner.Player.Systems;

/// <summary>
/// 旧版 PlayerCombat 的兼容包装。
/// 新的战斗逻辑全部在 <see cref="PlayerCombatSystem"/> 中实现。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerCombatSystem))]
public class PlayerCombat : MonoBehaviour
{
    private PlayerCombatSystem combatSystem;

    private void Awake()
    {
        combatSystem = GetComponent<PlayerCombatSystem>();
    }

    /// <summary>
    /// 供动画事件或旧脚本调用，实际会触发新的战斗系统。
    /// </summary>
    public void PerformAttack()
    {
        combatSystem?.PerformAttack();
    }
}