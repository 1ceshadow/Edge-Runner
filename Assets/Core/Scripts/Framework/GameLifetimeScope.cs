using VContainer;
using VContainer.Unity;
using UnityEngine;

/// <summary>
/// 场景级别的 LifetimeScope
/// 用于注册场景特定的服务（Player, Camera 等）
/// 场景切换时会自动销毁和重新创建
/// </summary>
public class GameLifetimeScope : LifetimeScope
{
    /// <summary>
    /// 查找父级 LifetimeScope（自动查找 ProjectLifetimeScope）
    /// </summary>
    protected override LifetimeScope FindParent()
    {
        var projectScope = FindFirstObjectByType<ProjectLifetimeScope>();
        if (projectScope != null)
        {
            Debug.Log("✓ GameLifetimeScope: 找到 ProjectLifetimeScope 作为父级");
            return projectScope;
        }
        
        // 开发模式下自动创建 ProjectLifetimeScope
        Debug.LogWarning("⚠ GameLifetimeScope: 未找到 ProjectLifetimeScope，正在自动创建（仅限开发调试）");
        var go = new GameObject("ProjectLifetimeScope (Auto-Created)");
        DontDestroyOnLoad(go);
        projectScope = go.AddComponent<ProjectLifetimeScope>();
        return projectScope;
    }
    
    protected override void Configure(IContainerBuilder builder)
    {
        var player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (player != null)
        {
            builder.RegisterComponent(player).As<IPlayerService>();
        }

        var cameras = Object.FindObjectsByType<CameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        builder.RegisterBuildCallback(resolver =>
        {
            // 安全注入 - 使用 try-catch 防止单个组件注入失败导致整个过程崩溃
            foreach (var cam in cameras)
            {
                TryInject(resolver, cam, "CameraController");
            }

            foreach (var enemy in enemies)
            {
                TryInject(resolver, enemy, "EnemyController");
            }

            // 注入 Player 相关组件
            if (player != null)
            {
                var deathHandler = player.GetComponent<PlayerDeathHandler>();
                if (deathHandler != null) TryInject(resolver, deathHandler, "PlayerDeathHandler");

                var healthSystem = player.GetComponent<EdgeRunner.Player.Systems.PlayerHealthSystem>();
                if (healthSystem != null) TryInject(resolver, healthSystem, "PlayerHealthSystem");

                var stateMachine = player.GetComponent<EdgeRunner.Player.States.PlayerStateMachine>();
                if (stateMachine != null) TryInject(resolver, stateMachine, "PlayerStateMachine");

                var skillManager = player.GetComponent<EdgeRunner.Player.Skills.PlayerSkillManager>();
                if (skillManager != null) TryInject(resolver, skillManager, "PlayerSkillManager");
            }
        });

        Debug.Log($"✓ VContainer: 场景服务注册完成 Player:{(player!=null?1:0)} Camera:{cameras.Length} Enemies:{enemies.Length}");
    }
    
    /// <summary>
    /// 安全注入，失败时记录警告而不是抛出异常
    /// </summary>
    private void TryInject(IObjectResolver resolver, object target, string componentName)
    {
        try
        {
            resolver.Inject(target);
        }
        catch (VContainerException ex)
        {
            Debug.LogWarning($"[GameLifetimeScope] ⚠ {componentName} 注入失败（部分依赖不可用）: {ex.Message}");
        }
    }
}
