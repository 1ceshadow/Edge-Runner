using UnityEngine;
using EdgeRunner.Pooling;

/// <summary>
/// 全局子弹服务接口，负责统一的生成与回收逻辑。
/// </summary>
public interface IBulletService
{
    /// <summary>
    /// 根据请求生成/激活一个子弹实例。
    /// </summary>
    /// <param name="request">发射参数</param>
    /// <returns>成功生成的子弹实例，失败时返回 null。</returns>
    PoolableBullet SpawnBullet(in BulletSpawnRequest request);

    /// <summary>
    /// 主动回收子弹（可用于立即清场或失败处理）。
    /// </summary>
    /// <param name="bullet">需要回收的子弹</param>
    void ReturnBullet(PoolableBullet bullet);
}

/// <summary>
/// 子弹生成请求参数。
/// </summary>
public struct BulletSpawnRequest
{
    /// <summary>发射世界坐标。</summary>
    public Vector3 Position;
    /// <summary>单位方向。</summary>
    public Vector2 Direction;
    /// <summary>速度覆盖值，未设置则走配置。</summary>
    public float? SpeedOverride;
    /// <summary>最大距离覆盖值，未设置则走配置。</summary>
    public float? MaxDistanceOverride;
    /// <summary>是否为玩家子弹。</summary>
    public bool IsPlayerBullet;
    /// <summary>用于调试/事件的来源标识。</summary>
    public string SourceId;
    /// <summary>可选伤害覆盖值。</summary>
    public int? DamageOverride;
}
