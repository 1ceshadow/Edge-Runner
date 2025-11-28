using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    private IGameStateManager gameStateManager;

    private void Start()
    {
        // 尝试查找 GameStateManager
        gameStateManager = FindFirstObjectByType<GameStateManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (gameStateManager != null)
            {
                gameStateManager.TriggerWin();
            }
            else
            {
                // Fallback for backward compatibility
                GameStateManager.Instance?.WinGame();
            }
        }
    }
}