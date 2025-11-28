using VContainer;
using VContainer.Unity;
using UnityEngine;

/// <summary>
/// 项目级别的 LifetimeScope
/// 用于注册全局服务（GameStateManager, AudioManager 等）
/// 这些服务会在整个游戏生命周期中持久存在
/// </summary>
public class ProjectLifetimeScope : LifetimeScope
{
    [Header("全局管理器引用")]
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private AudioManager audioManager;

    private static bool _initialized;

    protected override void Awake()
    {
        if (_initialized)
        {
            Destroy(gameObject);
            return;
        }
        _initialized = true;
        DontDestroyOnLoad(gameObject);
        base.Awake();
    }

    protected override void Configure(IContainerBuilder builder)
    {
        // 注册游戏状态管理器（单例）
        if (gameStateManager != null)
        {
            builder.RegisterComponent(gameStateManager).As<IGameStateManager>();
            Debug.Log("✓ VContainer: GameStateManager 已注册");
        }

        // 注册音频管理器（单例）
        if (audioManager != null)
        {
            builder.RegisterComponent(audioManager).As<IAudioManager>();
            Debug.Log("✓ VContainer: AudioManager 已注册");
        }

        // 注册其他全局服务
        // builder.Register<IYourService, YourService>(Lifetime.Singleton);
    }
}
