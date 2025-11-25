using UnityEngine;

/// <summary>
/// 子弹类 - 实现 IProjectile 和 IPoolable 接口
/// 
/// 职责:
/// - 管理子弹的生命周期
/// - 处理碰撞检测
/// - 支持对象池
/// </summary>
public class Bullet : MonoBehaviour, IProjectile, IPoolable
{
    private Vector2 direction;
    private float speed;
    private float maxDistance;
    private Vector2 startPosition;
    private float distanceTraveled;

    private Rigidbody2D rb;
    private CircleCollider2D collider;
    private bool isActive;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<CircleCollider2D>();
    }

    public void Launch(Vector2 position, Vector2 launchDirection, float launchSpeed)
    {
        transform.position = position;
        startPosition = position;
        direction = launchDirection.normalized;
        speed = launchSpeed;
        distanceTraveled = 0f;

        var config = GameConfig.Load();
        maxDistance = config.Enemy.BulletMaxDistance;

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        isActive = true;
        gameObject.SetActive(true);
    }

    private void FixedUpdate()
    {
        if (!isActive) return;

        distanceTraveled = Vector2.Distance(startPosition, transform.position);

        if (distanceTraveled > maxDistance)
        {
            Return();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActive) return;

        if (collision.CompareTag("Player"))
        {
            var playerService = ServiceLocator.Get<IPlayerService>();
            if (playerService != null)
            {
                playerService.TakeDamage(1, transform.position);
                Return();
            }
        }
    }

    public void Return()
    {
        if (!isActive) return;
        
        isActive = false;
        var pool = ServiceLocator.Get<BulletPool>();
        if (pool != null)
        {
            pool.ReturnBullet(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Vector2 GetPosition() => transform.position;
    public bool IsActive() => isActive;

    // IPoolable 实现
    public void OnAcquire()
    {
        isActive = true;
        if (collider != null)
            collider.enabled = true;
    }

    public void OnReturn()
    {
        isActive = false;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (collider != null)
            collider.enabled = false;
    }

    public GameObject GetGameObject() => gameObject;
}
