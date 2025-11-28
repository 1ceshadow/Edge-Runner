// ═══════════════════════════════════════════════════════════════
//  GameStateManager - 全局游戏状态管理器（单例 + DontDestroyOnLoad）
//  功能：暂停、胜利、死亡、输入、场景流程、UI淡入淡出
//  作者：1ceshadow
//  Unity版本：Unity 6+ 完全兼容
//  架构亮点：零耦合、高复用、热重载安全、防穿透淡入
// ═══════════════════════════════════════════════════════════════

// GameStateManager (DontDestroyOnLoad) ← 全局管理
// ├── Pause System
// ├── Win System（淡入动画 + 自动下一关）
// ├── Death System（重试 + 回菜单）
// ├── Input System（统一绑定）
// └── Scene Auto-Progress（Level1 → Level2 → ...）

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using EdgeRunner.Events;
//using System.Linq;

public class GameStateManager : MonoBehaviour, IGameStateManager
{
    // =============================================================
    //                          单例实例（保留向后兼容）
    // =============================================================
    public static GameStateManager Instance { get; private set; }

    // =============================================================
    //                          UI 引用
    // =============================================================
    [Header("═══ UI 引用 ═══")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Image winFadeImage;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Image deathFadeImage;

    // =============================================================
    //                          配置参数
    // =============================================================
    [Header("═══ 配置参数 ═══")]
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private string mainMenuScene = "0MainMenu";
    [SerializeField] private string[] levelScenes = { "Level0", "Level1", "Level2", "Level3" };

    // =============================================================
    //                          输入系统
    // =============================================================
    private PlayerInputActions inputs;

    // =============================================================
    //                          游戏状态（公开只读，实现接口）
    // =============================================================
    public bool IsPaused => isPaused;
    public bool IsWin => isWin;
    public bool IsDead => isDead;
    
    private bool isPaused;
    private bool isWin;
    private bool isDead;

    // =============================================================
    //                          Unity 生命周期
    // =============================================================
    private void Awake()
    {
        InitializeSingleton();

        // PreserveGlobalUI();
    }

    private void OnEnable()
    {
        InitializeInputSystem();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        CleanupInputSystem();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // VContainer 会自动管理生命周期，无需手动注销

    private void Start()
    {
        InitializeGameState();
        Debug.Log($"GameStateManager 已就绪 | 当前场景: {SceneManager.GetActiveScene().name}");
    }

    // =============================================================
    //                          初始化
    // =============================================================
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("✓ GameStateManager 初始化完成（将通过 VContainer 注册）");
        }
        else if (Instance != this)
        {
            Debug.Log("检测到重复 GameStateManager | 自动销毁");
            Destroy(gameObject);
        }
    }

    private void PreserveGlobalUI()
    {
        // 关键：将整个 UI 根物体设为永不销毁
        Transform uiRoot = transform.Find("GameUI");
        if (uiRoot != null)
        {
            DontDestroyOnLoad(uiRoot.gameObject);
            Debug.Log("全局 UI 已永久保留（跨所有关卡）");
        }
        else
        {
            Debug.LogWarning("未找到 GameUI！请在 Hierarchy 中创建并拖入 UI");
        }
    }

    // =============================================================
    //                          输入系统
    // =============================================================
    private void InitializeInputSystem()
    {
        inputs ??= new PlayerInputActions();
        inputs.Enable();

        inputs.UI.Pause.performed += OnPause;
        inputs.UI.Resume.performed += OnResume;
        inputs.UI.Start.performed += OnRestart;
        inputs.UI.BackToMenu.performed += OnBackToMenu;
        inputs.UI.NextLevel.performed += OnNextLevel;
    }

    private void CleanupInputSystem()
    {
        if (inputs == null) return;

        inputs.UI.Pause.performed -= OnPause;
        inputs.UI.Resume.performed -= OnResume;
        inputs.UI.Start.performed -= OnRestart;
        inputs.UI.BackToMenu.performed -= OnBackToMenu;
        inputs.UI.NextLevel.performed -= OnNextLevel;

        inputs.Disable();
    }


    // =============================================================
    //                          场景加载回调
    // =============================================================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"场景加载完成: {scene.name}");

        // 进入新关卡 → 重置状态
        //ResumeGame();
        ResetGameState();
        HideAllUI();
        
        // 🔔 发布场景加载事件
        int levelIndex = System.Array.IndexOf(levelScenes, scene.name);
        bool isMainMenu = scene.name == mainMenuScene;
        
        EventBus.Publish(new SceneLoadedEvent
        {
            SceneName = scene.name,
            SceneIndex = levelIndex,
            IsMainMenu = isMainMenu
        });
        
        // 如果是游戏关卡，发布关卡开始事件
        if (levelIndex >= 0)
        {
            EventBus.Publish(new LevelStartedEvent
            {
                LevelIndex = levelIndex,
                LevelName = scene.name
            });
        }
    }


    // =============================================================
    //                          状态管理
    // =============================================================
    private void InitializeGameState()
    {
        ResetGameState();
        HideAllUI();
    }
    private void ResetGameState()
    {
        isPaused = false;
        isWin = false;
        isDead = false;
        Time.timeScale = 1f;
    }
    private void HideAllUI()
    {
        if (pauseMenuUI) pauseMenuUI.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (deathPanel) deathPanel.SetActive(false);
    }

    // =============================================================
    //                          输入回调
    // =============================================================
    private void OnPause(InputAction.CallbackContext ctx)
    {
        if (isWin || isDead || !IsInGameScene()) return;
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    private void OnResume(InputAction.CallbackContext ctx) => ResumeGame();

    private void OnRestart(InputAction.CallbackContext context)
    {
        if (!isDead) return;

        RestartGame();
    }

    private void OnBackToMenu(InputAction.CallbackContext ctx)
    {
        if (!isPaused) { return; }

        BackToMenu();
    }

    private void OnNextLevel(InputAction.CallbackContext ctx)
    {
        if (isWin) GoToNextLevel();
    }

    // =============================================================
    //                          公共接口（实现 IGameStateManager）
    // =============================================================
    public void PauseGame()
    {
        if (!IsInGameScene()) return;

        isPaused = true;
        Time.timeScale = 0f;
        pauseMenuUI?.SetActive(true);
        
        // 🔔 发布暂停事件
        EventBus.Publish(new GamePausedEvent { IsPaused = true });
        
        Debug.Log("游戏暂停");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pauseMenuUI?.SetActive(false);
        
        // 🔔 发布恢复事件
        EventBus.Publish(new GamePausedEvent { IsPaused = false });
        
        Debug.Log("游戏继续");
    }
    
    public void TriggerWin()
    {
        if (isWin || isDead) return;
        isWin = true;
        Time.timeScale = 0f;
        if (winPanel) winPanel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeInWin());
        
        // 🔔 发布胜利事件
        int levelIndex = GetCurrentLevelIndex();
        EventBus.Publish(new GameWonEvent
        {
            LevelIndex = levelIndex,
            LevelName = SceneManager.GetActiveScene().name,
            CompletionTime = Time.timeSinceLevelLoad
        });
    }
    
    public void TriggerDeath()
    {
        PlayerDieWithDelay(1f);
    }
    
    public void RestartLevel()
    {
        RestartGame();
    }
    
    public void BackToMainMenu()
    {
        BackToMenu();
    }
    
    public void LoadNextLevel()
    {
        GoToNextLevel();
    }
    public void WinGame()
    {
        TriggerWin();
    }

    public void PlayerDieWithDelay(float delay = 1f)
    {
        if (isDead || isWin) return;
        isDead = true;
        Time.timeScale = 0f;

        if (deathPanel)
        {
            deathPanel.SetActive(true);
            StartCoroutine(FadeInDeathWithDelay(delay));  // 非阻塞协程
        }
        
        // 🔔 发布游戏失败事件
        EventBus.Publish(new GameOverEvent
        {
            Reason = "玩家死亡",
            LevelIndex = GetCurrentLevelIndex()
        });
        
        Debug.Log("💀 死亡触发，延迟淡入...");
    }


    //public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    // =============================================================
    //                          游戏流程控制（内部实现）
    // =============================================================

    public void RestartGame()
    {
        Debug.Log("重新开始游戏");
        Time.timeScale = 1f;
        isDead = false;
        StopAllCoroutines();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextLevel()
    {
        if (!isWin) return;

        Time.timeScale = 1f;
        string next = GetNextLevelName();
        string target = string.IsNullOrEmpty(next) ? mainMenuScene : next;
        SceneManager.LoadScene(target);
        Debug.Log($"加载下一关: {target}");
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        isPaused = true;
        StopAllCoroutines();
        SceneManager.LoadScene(mainMenuScene);
    }


    // =============================================================
    //                          协程动画
    // =============================================================
    
    /// <summary>
    /// 通用图片淡入协程
    /// </summary>
    /// <param name="image">要淡入的图片</param>
    /// <param name="duration">淡入持续时间</param>
    /// <param name="delay">淡入前的延迟（可选）</param>
    private IEnumerator FadeInImage(UnityEngine.UI.Image image, float duration, float delay = 0f)
    {
        if (image == null) yield break;
        
        image.color = new Color(1, 1, 1, 0);
        
        // 延迟阶段
        if (delay > 0f)
        {
            float timer = 0f;
            while (timer < delay)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        
        // 淡入阶段
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            image.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, t / duration));
            yield return null;
        }
        
        image.color = Color.white;
    }
    
    private IEnumerator FadeInWin()
    {
        yield return FadeInImage(winFadeImage, fadeDuration);
    }

    private IEnumerator FadeInDeathWithDelay(float delay)
    {
        yield return FadeInImage(deathFadeImage, fadeDuration, delay);
    }

    // private IEnumerator FadeInWin()
    // {
    //     if (winFadeImage == null) yield break;
    //     winFadeImage.color = new Color(1, 1, 1, 0);
    //     float t = 0;
    //     while (t < fadeDuration)
    //     {
    //         t += Time.unscaledDeltaTime;
    //         float a = Mathf.Lerp(0, 1, t / fadeDuration);
    //         winFadeImage.color = new Color(1, 1, 1, a);
    //         yield return null;
    //     }
    // }

    // =============================================================
    //                          工具方法
    // =============================================================
    private string GetNextLevelName()
    {
        string current = SceneManager.GetActiveScene().name;
        int index = System.Array.IndexOf(levelScenes, current);
        return (index >= 0 && index < levelScenes.Length - 1) ? levelScenes[index + 1] : null;
    }
    
    /// <summary>
    /// 获取当前关卡索引（用于事件发布）
    /// </summary>
    private int GetCurrentLevelIndex()
    {
        string current = SceneManager.GetActiveScene().name;
        return System.Array.IndexOf(levelScenes, current);
    }

    // private bool IsInGameScene()
    // {
    //     string current = SceneManager.GetActiveScene().name;
    //     return levelScenes.Contains(current);
    // }
    private bool IsInGameScene()
        => System.Array.IndexOf(levelScenes, SceneManager.GetActiveScene().name) >= 0;

    public void switchLevelN(int num)
    {
        SceneManager.LoadScene(levelScenes[num]);
    }

}