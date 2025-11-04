using UnityEngine;

public class BulletController : MonoBehaviour
{
    [Header("子弹设置")]
    public float speed = 8f;
    public float maxDistance = 15f;
    public int damage = 1;
    
    private Vector3 startPosition;
    private Vector2 direction;
    private bool isActive = true;

    void Start()
    {
        startPosition = transform.position;
        
        // 确保有碰撞体
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<CircleCollider2D>();
        }
    }

    void Update()
    {
        if (!isActive) return;
        
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
        
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }
    
    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        
        if (other.CompareTag("Player"))
        {
            Death player = other.GetComponent<Death>();
            if (player != null)
            {
                player.DieFromBullet();
            }
            Destroy(gameObject);
        }
    }
}