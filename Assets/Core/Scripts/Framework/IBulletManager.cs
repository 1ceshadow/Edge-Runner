/// <summary>
/// 子弹管理器接口 - 负责追踪活跃子弹以及极限闪避检测。
/// </summary>
public interface IBulletManager
{
    /// <summary>
    /// 注册活跃子弹。
    /// </summary>
    void RegisterBullet(UnityEngine.Transform bullet);

    /// <summary>
    /// 注销子弹。
    /// </summary>
    void UnregisterBullet(UnityEngine.Transform bullet);

    /// <summary>
    /// 检测指定位置周围是否有子弹（极限闪避判定）。
    /// </summary>
    bool CheckBulletsInRange(UnityEngine.Vector2 position, float radius);
}
