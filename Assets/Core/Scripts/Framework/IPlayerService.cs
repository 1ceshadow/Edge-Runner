using UnityEngine;

/// <summary>
/// 玩家服务接口
/// 提供玩家对象和组件的访问
/// </summary>
public interface IPlayerService
{
    Transform Transform { get; }
    GameObject GameObject { get; }
    
    T GetComponent<T>() where T : Component;
    bool TryGetComponent<T>(out T component) where T : Component;
}
