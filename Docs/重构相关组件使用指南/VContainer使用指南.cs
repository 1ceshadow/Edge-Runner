// ================================================================
// VContainer 依赖注入使用快速参考
// ================================================================

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 1. 基础：构造函数注入（推荐）
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

using VContainer;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // 私有字段存储注入的依赖
    private IPlayerService playerService;
    private IGameStateManager gameState;
    
    // [Inject] 标记的方法会在 Awake 之前自动调用
    [Inject]
    public void Construct(IPlayerService player, IGameStateManager state)
    {
        this.playerService = player;
        this.gameState = state;
        Debug.Log("✓ 依赖已注入");
    }
    
    void Start()
    {
        // 可以安全使用注入的服务
        Transform target = playerService.Transform;
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 2. 字段注入（简洁但不推荐用于必需依赖）
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class CameraFollow : MonoBehaviour
{
    // 直接在字段上使用 [Inject]
    [Inject] private IPlayerService playerService;
    [Inject] private IAudioManager audioManager;
    
    void Start()
    {
        // 字段会在 Start 之前被注入
        transform.position = playerService.Transform.position;
        audioManager.PlayBGM();
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 3. 属性注入
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class UIManager : MonoBehaviour
{
    [Inject] public IGameStateManager GameState { get; private set; }
    [Inject] public IAudioManager Audio { get; private set; }
    
    public void OnPauseButtonClicked()
    {
        GameState.PauseGame();
        Audio.PauseBGM();
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 4. 获取玩家服务
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class Missile : MonoBehaviour
{
    [Inject] private IPlayerService playerService;
    
    private Transform target;
    
    void Start()
    {
        // 获取玩家 Transform
        target = playerService.Transform;
    }
    
    void Update()
    {
        if (target != null)
        {
            // 追踪玩家
            Vector3 direction = target.position - transform.position;
            transform.position += direction.normalized * speed * Time.deltaTime;
        }
    }
}


// 获取玩家组件
public class HealthPickup : MonoBehaviour
{
    [Inject] private IPlayerService playerService;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否是玩家
        if (other.gameObject == playerService.GameObject)
        {
            // 获取玩家的 Health 组件
            if (playerService.TryGetComponent<PlayerHealth>(out var health))
            {
                health.Heal(20);
            }
        }
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 5. 多个依赖注入
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class GameController : MonoBehaviour
{
    private IPlayerService player;
    private IGameStateManager gameState;
    private IAudioManager audio;
    
    [Inject]
    public void Construct(
        IPlayerService playerService,
        IGameStateManager gameStateManager,
        IAudioManager audioManager)
    {
        this.player = playerService;
        this.gameState = gameStateManager;
        this.audio = audioManager;
    }
    
    public void StartGame()
    {
        gameState.ResumeGame();
        audio.PlayBGM();
        player.Transform.position = Vector3.zero;
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 6. 在 LifetimeScope 中注册服务
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

//using VContainer;
//using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 注册 MonoBehaviour 组件（场景中已存在）
        var player = FindObjectOfType<Player>();
        builder.RegisterComponent(player).As<IPlayerService>();
        
        // 注册层级中的组件（自动查找并注入依赖）
        builder.RegisterComponentInHierarchy<CameraController>();
        builder.RegisterComponentInHierarchy<EnemyController>();
        
        // 注册纯 C# 类（非 MonoBehaviour）
        builder.Register<ScoreCalculator>(Lifetime.Singleton);
        
        // 注册接口实现
        builder.Register<IInputService, InputService>(Lifetime.Singleton);
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 7. 实际案例：敌人追踪系统
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class EnemyChaser : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 10f;
    
    // 注入的依赖
    private IPlayerService playerService;
    
    [Inject]
    public void Construct(IPlayerService player)
    {
        this.playerService = player;
    }
    
    void Update()
    {
        if (playerService == null) return;
        
        Transform playerTransform = playerService.Transform;
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distance < detectionRange)
        {
            // 追踪玩家
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 8. 实际案例：触发器
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class WinTrigger : MonoBehaviour
{
    [Inject] private IPlayerService playerService;
    [Inject] private IGameStateManager gameState;
    [Inject] private IAudioManager audio;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否是玩家
        if (other.gameObject == playerService.GameObject)
        {
            // 触发胜利
            gameState.TriggerWin();
            audio.PlayBGM(); // 播放胜利音乐
        }
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 9. 实际案例：UI 系统
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class PauseMenuUI : MonoBehaviour
{
    [Inject] private IGameStateManager gameState;
    [Inject] private IAudioManager audio;
    
    public void OnResumeButtonClick()
    {
        gameState.ResumeGame();
        audio.PlayBGM();
    }
    
    public void OnRestartButtonClick()
    {
        gameState.RestartLevel();
    }
    
    public void OnMainMenuButtonClick()
    {
        gameState.BackToMainMenu();
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 10. 高级：可选依赖
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class DebugOverlay : MonoBehaviour
{
    private IPlayerService playerService;
    
    // 可选依赖：如果不存在也不会报错
    [Inject]
    public void Construct([Inject(Optional = true)] IPlayerService player)
    {
        this.playerService = player;
        
        if (player != null)
        {
            Debug.Log("玩家服务已注入");
        }
        else
        {
            Debug.LogWarning("玩家服务未找到（可选依赖）");
        }
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 11. 创建自定义服务
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// 步骤 1: 定义接口
public interface IScoreService
{
    int CurrentScore { get; }
    void AddScore(int points);
    void ResetScore();
}

// 步骤 2: 实现接口
public class ScoreService : IScoreService
{
    public int CurrentScore { get; private set; }
    
    public void AddScore(int points)
    {
        CurrentScore += points;
        Debug.Log($"Score: {CurrentScore}");
    }
    
    public void ResetScore()
    {
        CurrentScore = 0;
    }
}

// 步骤 3: 在 LifetimeScope 中注册
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IScoreService, ScoreService>(Lifetime.Singleton);
    }
}

// 步骤 4: 使用服务
public class Enemy : MonoBehaviour
{
    [Inject] private IScoreService scoreService;
    
    void Die()
    {
        scoreService.AddScore(100);
        Destroy(gameObject);
    }
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 12. 常见错误和解决方案
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// ❌ 错误 1: 忘记添加 [Inject] 特性
public class BadExample : MonoBehaviour
{
    private IPlayerService playerService;
    
    // 这个方法不会被调用！
    public void Construct(IPlayerService player)
    {
        this.playerService = player;
    }
}

// ✅ 正确：添加 [Inject]
public class GoodExample : MonoBehaviour
{
    private IPlayerService playerService;
    
    [Inject]  // 必须添加这个特性
    public void Construct(IPlayerService player)
    {
        this.playerService = player;
    }
}


// ❌ 错误 2: 在 Awake 中使用注入的服务
public class BadTiming : MonoBehaviour
{
    [Inject] private IPlayerService playerService;
    
    void Awake()
    {
        // 可能为 null！Inject 在 Awake 之前，但字段注入可能还未完成
        playerService.Transform.position = Vector3.zero;  // NullReferenceException!
    }
}

// ✅ 正确：在 Start 或构造函数中使用
public class GoodTiming : MonoBehaviour
{
    private IPlayerService playerService;
    
    [Inject]
    public void Construct(IPlayerService player)
    {
        this.playerService = player;
        // 这里可以安全使用
        player.Transform.position = Vector3.zero;
    }
    
    void Start()
    {
        // 或者在 Start 中使用
        playerService.Transform.position = Vector3.zero;
    }
}


// ❌ 错误 3: 忘记在场景中添加 LifetimeScope
// 症状：所有注入都是 null
// 解决方案：确保场景中有 GameLifetimeScope 组件


// ❌ 错误 4: 服务未注册
// 症状：启动时报错 "DependencyResolutionException"
// 解决方案：在 LifetimeScope 的 Configure 方法中注册服务


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 13. 调试技巧
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class DebugHelper : MonoBehaviour
{
    [Inject]
    public void Construct(IPlayerService player, IGameStateManager state)
    {
        // 在构造函数中添加日志
        Debug.Log($"✓ 注入成功 - Player: {player != null}, State: {state != null}");
    }
    
    void Start()
    {
        // 在 Start 中验证依赖
        Debug.Assert(player != null, "Player service not injected!");
    }
}

// 查看依赖树（在编辑器中）
// 选中 LifetimeScope GameObject
// 在 Inspector 中可以看到所有注册的服务


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 14. 最佳实践总结
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

/*
✅ DO（推荐做法）:
1. 使用构造函数注入，依赖关系更清晰
2. 依赖接口而非具体类型
3. 在 LifetimeScope 中集中管理注册
4. 为每个场景创建 GameLifetimeScope
5. 使用 [Inject] 标记注入方法
6. 在 Start() 或构造函数中使用注入的服务

❌ DON'T（避免做法）:
1. 不要在 Awake() 中使用字段注入的服务
2. 不要手动调用 new 创建需要注入的对象
3. 不要在多个地方注册同一个服务
4. 不要创建循环依赖
5. 不要忘记添加 [Inject] 特性
6. 不要在没有 LifetimeScope 的场景中使用注入
*/
