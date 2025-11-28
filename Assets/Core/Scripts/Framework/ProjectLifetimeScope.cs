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
    [Header("全局管理器引用（留空则自动查找）")]
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private BulletService bulletService;
    [SerializeField] private BulletManager bulletManager;
    [SerializeField] private EdgeRunner.Pooling.PoolManager poolManager;

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
        
        // 自动查找未分配的组件
        AutoFindComponents();
        
        // 将全局服务设为 DontDestroyOnLoad，防止场景切换时被销毁
        MakeServicesPersistent();
        
        base.Awake();
    }
    
    /// <summary>
    /// 自动查找场景中的服务组件
    /// </summary>
    private void AutoFindComponents()
    {
        if (gameStateManager == null)
            gameStateManager = FindFirstObjectByType<GameStateManager>();
        if (audioManager == null)
            audioManager = FindFirstObjectByType<AudioManager>();
        if (bulletService == null)
            bulletService = FindFirstObjectByType<BulletService>();
        if (bulletManager == null)
            bulletManager = FindFirstObjectByType<BulletManager>();
        if (poolManager == null)
            poolManager = FindFirstObjectByType<EdgeRunner.Pooling.PoolManager>();
    }
    
    /// <summary>
    /// 将全局服务设为持久化，防止场景切换时被销毁
    /// </summary>
    private void MakeServicesPersistent()
    {
        MakePersistent(bulletService, nameof(BulletService));
        MakePersistent(bulletManager, nameof(BulletManager));
        MakePersistent(poolManager, nameof(EdgeRunner.Pooling.PoolManager));
        MakePersistent(gameStateManager, nameof(GameStateManager));
        MakePersistent(audioManager, nameof(AudioManager));
    }
    
    /// <summary>
    /// 将单个服务设为 DontDestroyOnLoad
    /// </summary>
    private void MakePersistent(Component service, string serviceName)
    {
        if (service != null && service.transform.parent == null)
        {
            DontDestroyOnLoad(service.gameObject);
            Debug.Log($"✓ {serviceName} 设为 DontDestroyOnLoad");
        }
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

        if (bulletService != null)
        {
            builder.RegisterComponent(bulletService).As<IBulletService>();
            Debug.Log("✓ VContainer: BulletService 已注册");
        }

        if (bulletManager != null)
        {
            builder.RegisterComponent(bulletManager).As<IBulletManager>();
            Debug.Log("✓ VContainer: BulletManager 已注册");
        }

        if (poolManager != null)
        {
            builder.RegisterComponent(poolManager).As<IPoolManager>();
            Debug.Log("✓ VContainer: PoolManager 已注册");
        }

        // 注册其他全局服务
        // builder.Register<IYourService, YourService>(Lifetime.Singleton);
    }
}
