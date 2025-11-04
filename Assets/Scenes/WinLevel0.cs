using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GoalTrigger : MonoBehaviour
{
    [Header("Win动画设置")]
    [SerializeField] private Image winImage;           // 拖入 winImage
    [SerializeField] private float fadeDuration = 1.5f;  // 淡入时间

    public GameObject pauseMenuUI;
    private string NextScene = "Level1";

    private bool isWin = false;
    private bool isPaused = false;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void Start()
    {
        if (winImage != null)
        {
            winImage.color = new Color(1, 1, 1, 0);
            winImage.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.UI.Pause.performed += PauseGame;
        inputActions.UI.Continue.performed += ResumeGame;
        inputActions.UI.Menu.performed += BackMenu;

        inputActions.UI.Next.performed += SwitchNextScene;

    }

    private void OnDisable()
    {
        inputActions.UI.Pause.performed -= PauseGame;
        inputActions.UI.Continue.performed -= ResumeGame;
        inputActions.UI.Menu.performed -= BackMenu;
        inputActions.UI.Next.performed -= SwitchNextScene;
        inputActions.Disable();
    }

    // 触发时调用
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("碰撞");
        if (other.CompareTag("Player"))  // 检查碰撞体是否是玩家
        {
            isWin = true;
            WinGame();  // 调用胜利函数
        }
    }

    // 胜利逻辑
    private void WinGame()
    {
        // 这里可以处理胜利的行为，比如显示胜利UI，停止游戏等
        Debug.Log("You Win!");
        // 比如显示胜利UI（假设有一个Canvas）
        // victoryUI.SetActive(true);  // 取消注释来显示UI

        // 你可以在这里播放胜利动画，切换场景等
        Time.timeScale = 0f;  // 暂停游戏
        // 淡入win界面
        if (winImage != null)
        {
            winImage.gameObject.SetActive(true);
            StartCoroutine(FadeInWinScreen());

        }
    }

    private IEnumerator FadeInWinScreen()
    {
        winImage.color = new Color(1, 1, 1, 0);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0, 1, elapsed / fadeDuration);
            winImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        winImage.color = new Color(1, 1, 1, 1);
    }

    private void SwitchNextScene(InputAction.CallbackContext context)
    {
        if (!isWin) { return; }

        Time.timeScale = 1f;
        isWin = false;

        // 停止所有协程（防止旧的FadeIn继续运行）
        StopAllCoroutines();
        SceneManager.LoadScene(NextScene);
    }

    private void PauseGame(InputAction.CallbackContext context)
    {
        if (isPaused) { return; }
        Time.timeScale = 0f;
        Debug.Log("暂停游戏");
        pauseMenuUI.SetActive(true);
        isPaused = true;
    }
    private void ResumeGame(InputAction.CallbackContext context)
    {
        if (!isPaused) { return; }
        Time.timeScale = 1f;
        Debug.Log("继续游戏");
        pauseMenuUI.SetActive(false);
        isPaused = false;
    }

    private void BackMenu(InputAction.CallbackContext context)
    {
        if (!isPaused) { return; }

        Time.timeScale = 1f;
        isPaused = true;
        StopAllCoroutines();
        SceneManager.LoadScene("0MainMenu");
    }
}
