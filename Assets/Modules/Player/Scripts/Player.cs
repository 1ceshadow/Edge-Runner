using UnityEngine;
using EdgeRunner.Player;

/// <summary>
/// PlayerRoot：IPlayerService 的唯一实现，向 DI 提供统一入口。
/// 负责暴露 <see cref="PlayerController"/> 以及相关子系统，避免外部脚本直接使用 Find。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public class Player : MonoBehaviour, IPlayerService
{
    private PlayerController controller;

    public PlayerController Controller => controller;

    Transform IPlayerService.Transform => transform;
    GameObject IPlayerService.GameObject => gameObject;
    PlayerController IPlayerService.Controller => controller;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        if (controller == null)
        {
            Debug.LogError("PlayerRoot 缺少 PlayerController 组件，无法完成依赖注入");
        }
        else
        {
            Debug.Log("✓ PlayerRoot: IPlayerService 已就绪");
        }
    }

    T IPlayerService.GetComponent<T>()
    {
        return GetComponent<T>();
    }

    bool IPlayerService.TryGetComponent<T>(out T component)
    {
        return TryGetComponent(out component);
    }
}
