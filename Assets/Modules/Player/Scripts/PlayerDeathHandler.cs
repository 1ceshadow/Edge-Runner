using UnityEngine;
using VContainer;
using EdgeRunner.Player;
using EdgeRunner.Player.Systems;

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
    
    // 新版模块化引用
    private PlayerController playerController;
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    private PlayerCombatSystem combatSystem;

    // DI 注入（优先使用）
    private IGameStateManager gameStateManager;

    [Inject]
    public void Construct(IGameStateManager gameStateManager)
    {
        this.gameStateManager = gameStateManager;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        
        playerController = GetComponent<PlayerController>();
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        combatSystem = GetComponent<PlayerCombatSystem>();
        
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

        // 2. 禁用关键控制脚本
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        if (inputHandler != null)
        {
            inputHandler.enabled = false;
        }
        if (movement != null)
        {
            movement.CancelDash();
            movement.enabled = false;
        }
        if (combatSystem != null)
        {
            combatSystem.enabled = false;
        }

        // 3. 视觉反馈：变红
        if (sr != null)
            sr.color = deathColor;

        Debug.Log("玩家死亡！准备显示死亡界面...");

        // 4. 通知 GameStateManager 显示死亡界面（带延迟淡入）
        // 优先使用 DI 注入的服务
        if (gameStateManager != null)
        {
            gameStateManager.TriggerDeath();
        }
        else if (GameStateManager.Instance != null)
        {
            // 向后兼容：回退到静态单例
            GameStateManager.Instance.PlayerDieWithDelay(deathStayBeforeFade);
        }
        else
        {
            Debug.LogError("GameStateManager 未找到！请确保在 MainMenu 场景中创建！");
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
        
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        if (inputHandler != null)
        {
            inputHandler.enabled = true;
        }
        if (movement != null)
        {
            movement.enabled = true;
        }
        if (combatSystem != null)
        {
            combatSystem.enabled = true;
        }
        
        if (sr != null) sr.color = Color.white;
    }
}