using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("死亡检测设置")]
    [SerializeField] private string deathZoneLayerName = "DeathZone";
    [SerializeField] private float deathStayBeforeFade = 1f;     // 死亡后停留时间
    [SerializeField] private Color deathColor = Color.red;      // 死亡变色

    private int deathZoneLayer;
    private bool isDead = false;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PlayerMovement playerMovement; // 你的移动脚本

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        playerMovement = GetComponent<PlayerMovement>();
        deathZoneLayer = LayerMask.NameToLayer(deathZoneLayerName);
    }

    private void OnEnable()
    {
        // 可选：如果你想让子弹脚本直接调用
        // Bullet.OnPlayerHit += DieFromBullet;
    }

    private void OnDisable()
    {
        // Bullet.OnPlayerHit -= DieFromBullet;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.gameObject.layer == deathZoneLayer)
        {
            TriggerDeath();
        }
    }

    // 外部调用：子弹、陷阱、敌人攻击等
    public void DieFromExternal()
    {
        if (!isDead) TriggerDeath();
    }

    private void TriggerDeath()
    {
        if (isDead) return;
        isDead = true;

        // 1. 停止运动
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic; // 防止物理干扰

        // 2. 禁用移动脚本
        if (playerMovement != null)
            playerMovement.enabled = false;

        // 3. 视觉反馈：变红
        if (sr != null)
            sr.color = deathColor;

        Debug.Log("玩家死亡！准备显示死亡界面...");

        // 4. 通知 GameStateManager 显示死亡界面（带延迟淡入）
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.PlayerDieWithDelay(deathStayBeforeFade);
        }
        else
        {
            Debug.LogError("GameStateManager 未找到！请确保在 MainMenu 场景中创建！");
            // Debug.LogError("GameStateManager 未找到！创建临时单例...");
            // // 自动创建！解决直接玩关卡问题
            // var tempManager = new GameObject("GameStateManager");
            // tempManager.AddComponent<GameStateManager>();
            // GameStateManager.Instance.PlayerDieWithDelay(deathStayBeforeFade);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (isDead)
        {
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, 0.5f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, "DEAD");
        }
    }
#endif

    // 重置状态（新关卡加载后调用，或由 GameStateManager 自动重置）
    public void ResetDeathState()
    {
        isDead = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        if (playerMovement != null) playerMovement.enabled = true;
        if (sr != null) sr.color = Color.white;
    }
}