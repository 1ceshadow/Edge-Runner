using UnityEngine;
using UnityEngine.Tilemaps; // 这行很重要！

public class PlayerWallCollision : MonoBehaviour
{
    [Header("碰撞设置")]
    public string wallTilemapName = "Wall"; // 墙体Tilemap的名称
    public float collisionOffset = 0.1f;    // 碰撞偏移量，防止卡墙
    
    private CircleCollider2D playerCollider;
    private TilemapCollider2D wallCollider;
    private Vector2 lastValidPosition;
    
    void Start()
    {
        // 获取玩家碰撞体
        playerCollider = GetComponent<CircleCollider2D>();
        if (playerCollider == null)
        {
            Debug.LogError("玩家缺少CircleCollider2D组件！");
            return;
        }
        
        // 查找墙体Tilemap的碰撞体
        GameObject wallObject = GameObject.Find(wallTilemapName);
        if (wallObject != null)
        {
            wallCollider = wallObject.GetComponent<TilemapCollider2D>();
            if (wallCollider == null)
            {
                Debug.LogError($"找不到名为 {wallTilemapName} 的TilemapCollider2D！");
            }
        }
        else
        {
            Debug.LogError($"找不到名为 {wallTilemapName} 的墙体对象！");
        }
        
        lastValidPosition = transform.position;
    }
    
    void Update()
    {
        // 实时检测碰撞，如果穿墙则退回上一个有效位置
        if (wallCollider != null && IsCollidingWithWall())
        {
            transform.position = lastValidPosition;
        }
        else
        {
            // 更新最后一个有效位置
            lastValidPosition = transform.position;
        }
    }
    
    /// <summary>
    /// 检测玩家是否与墙体发生碰撞
    /// </summary>
    bool IsCollidingWithWall()
    {
        if (playerCollider == null || wallCollider == null)
            return false;
            
        // 使用OverlapCircle检测碰撞
        Vector2 circleCenter = (Vector2)transform.position + playerCollider.offset;
        float radius = playerCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        
        // 检测与墙体碰撞体的重叠
        return Physics2D.OverlapCircle(circleCenter, radius - collisionOffset, 
                                      LayerMask.GetMask("Wall")); // 建议为墙体设置专用Layer
    }
    
    /// <summary>
    /// 预测移动后是否会碰撞（用于提前阻止移动）
    /// </summary>
    public bool WillCollide(Vector2 moveDirection, float moveDistance)
    {
        if (playerCollider == null || wallCollider == null)
            return false;
            
        Vector2 predictedPosition = (Vector2)transform.position + moveDirection.normalized * moveDistance;
        Vector2 circleCenter = predictedPosition + playerCollider.offset;
        float radius = playerCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        
        return Physics2D.OverlapCircle(circleCenter, radius - collisionOffset, 
                                     LayerMask.GetMask("Wall"));
    }
    
    // 在Scene视图中显示碰撞范围（调试用）
    void OnDrawGizmosSelected()
    {
        if (playerCollider != null)
        {
            Gizmos.color = Color.red;
            Vector2 circleCenter = (Vector2)transform.position + playerCollider.offset;
            float radius = playerCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            Gizmos.DrawWireSphere(circleCenter, radius - collisionOffset);
        }
    }
}