using UnityEngine;

/// <summary>
/// 游戏状态管理器接口
/// 提供暂停、胜利、死亡等游戏状态管理功能
/// </summary>
public interface IGameStateManager
{
    bool IsPaused { get; }
    bool IsWin { get; }
    bool IsDead { get; }

    void PauseGame();
    void ResumeGame();
    void TriggerWin();
    void TriggerDeath();
    void RestartLevel();
    void BackToMainMenu();
    void LoadNextLevel();
}
