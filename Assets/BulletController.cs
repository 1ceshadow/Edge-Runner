using UnityEngine;

public class BulletController : MonoBehaviour
{
    [Header("子弹设置")]
    public float speed = 11.8f;
    public float maxDistance = 16f;
    public int damage = 1;
    
    [Header("碰撞检测")]
    public bool useContinuousDetection = true; // 高速子弹建议开启
    public float detectionStep = 0.5f; // 连续检测步长
    
    private Vector3 startPosition;
    private Vector2 direction;
    private bool isActive = true;
    private Rigidbody2D rb;

    void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        
        // 确保有刚体用于Collision检测
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 连续碰撞检测
        }
        
        // 确保有碰撞体
        if (GetComponent<Collider2D>() == null)
        {
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = false; // 使用Collision，不是Trigger
        }
    }

    void Update()
    {
        if (!isActive) return;
        
        // 高速子弹使用连续检测
        if (useContinuousDetection && speed > 10f)
        {
            MoveWithContinuousDetection();
        }
        else
        {
            MoveNormally();
        }
        
        // 距离检测
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            DestroyBullet();
        }
    }
    
    /// <summary>
    /// 普通移动
    /// </summary>
    private void MoveNormally()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }
    
    /// <summary>
    /// 连续碰撞检测移动（防高速穿透）
    /// </summary>
    private void MoveWithContinuousDetection()
    {
        float moveDistance = speed * Time.deltaTime;
        int steps = Mathf.CeilToInt(moveDistance / detectionStep);
        float stepSize = moveDistance / steps;
        
        for (int i = 0; i < steps; i++)
        {
            transform.Translate(direction * stepSize, Space.World);
            
            // 每步后立即检查碰撞
            if (CheckImmediateCollision())
                break;
        }
    }
    
    /// <summary>
    /// 立即碰撞检测
    /// </summary>
    private bool CheckImmediateCollision()
    {
        // 使用OverlapCircle检测周围碰撞
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.1f);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject && (hit.CompareTag("Cover") || hit.CompareTag("Player")))
            {
                HandleCollision(hit.gameObject);
                return true;
            }
        }
        return false;
    }
    
    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        
        // 设置子弹旋转方向
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    /// <summary>
    /// Collision碰撞处理（主要方法）
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;
        HandleCollision(collision.gameObject);
    }
    
    /// <summary>
    /// 统一处理碰撞
    /// </summary>
    private void HandleCollision(GameObject hitObject)
    {
        Debug.Log($"子弹碰撞: {hitObject.name} (标签: {hitObject.tag})");
        
        if (hitObject.CompareTag("Cover"))
        {
            Debug.Log("子弹击中掩体");
            DestroyBullet();
            return;
        }

        if (hitObject.CompareTag("Player"))
        {
            Debug.Log("子弹击中玩家");
            Death player = hitObject.GetComponent<Death>();
            if (player != null)
            {
                player.DieFromBullet();
            }
            DestroyBullet();
            return;
        }
    }
    
    /// <summary>
    /// 安全销毁子弹
    /// </summary>
    private void DestroyBullet()
    {
        if (!isActive) return;
        
        isActive = false;
        
        // 立即禁用渲染和碰撞
        if (TryGetComponent<SpriteRenderer>(out var renderer))
            renderer.enabled = false;
        if (TryGetComponent<Collider2D>(out var collider))
            collider.enabled = false;
            
        // 延迟销毁
        Destroy(gameObject, 0.1f);
    }
    
    /// <summary>
    /// 调试显示
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isActive) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 1f);
        
        // 显示检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}