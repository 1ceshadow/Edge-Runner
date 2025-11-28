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
            foreach (var cam in cameras)
            {
                resolver.Inject(cam);
            }

            foreach (var enemy in enemies)
            {
                resolver.Inject(enemy);
            }
        });

        Debug.Log($"✓ VContainer: 场景服务注册完成 Player:{(player!=null?1:0)} Camera:{cameras.Length} Enemies:{enemies.Length}");
    }
}
