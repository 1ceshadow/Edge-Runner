using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    private Animator animator;
    //
    PlayerMovement playerMovement;
    
    [Header("攻击设置")]
    private float attackRange = 2.9f;
    private float attackAngle = 150f;
    private float attackCooldown = 0.3f;
    private int attackDamage = 1;
    // public LayerMask enemyLayer;

    [Header("攻击音效")]
    [SerializeField] private AudioClip attackSound;
    private AudioSource attackSource;

    [Header("视觉反馈")]
    public Color attackEffectColor = new Color(1f, 0.5f, 0f, 0.3f);
    private float effectDuration = 0.2f;

    private PlayerInputActions inputActions;
    private bool canAttack = true;
    private float cooldownTimer;
    private Vector2 lastAttackDirection = Vector2.up;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // 调试用
    private Vector2 debugAttackDirection;
    private int debugHitCount;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        //
        playerMovement = GetComponent<PlayerMovement>();
        attackSource = GetComponent<AudioSource>();

        inputActions = new PlayerInputActions();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        //if (enemyLayer == 0)
        //{
        //    enemyLayer = LayerMask.GetMask("Default");
        //    Debug.Log("使用默认层级检测敌人");
        //}
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Attack.performed += OnAttackInput;
    }

    void OnDisable()
    {
        inputActions.Disable();
        inputActions.Player.Attack.performed -= OnAttackInput;
    }

    void Update()
    {
        if (!canAttack)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                canAttack = true;
                cooldownTimer = 0f;
            }
        }
    }

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        if (canAttack && context.performed)
        {
            PerformAttack();
            animator.SetTrigger("TrigAttack");

            if (attackSource != null)
            {
                attackSource.PlayOneShot(attackSound);
            }
        }
    }

    private void PerformAttack()
    {
        if (!canAttack) return;

        Vector2 attackDirection = GetAttackDirection();
        lastAttackDirection = attackDirection;
        debugAttackDirection = attackDirection;

        Debug.Log($"开始攻击检测，方向: {attackDirection}");

        Collider2D[] hitEnemies = DetectEnemiesInSector(attackDirection);
        debugHitCount = hitEnemies.Length;

        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log($"击中敌人: {enemy.gameObject.name}");

            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.TakeDamage(attackDamage);
                Debug.Log($"调用敌人受伤方法");
            }
            else
            {
                Debug.Log($"敌人没有EnemyController，直接销毁");
                Destroy(enemy.gameObject);
            }
        }

        PlayAttackEffects(attackDirection, hitEnemies.Length > 0);
        StartCooldown();

        Debug.Log($"攻击完成！检测到 {hitEnemies.Length} 个敌人，实际击中: {debugHitCount}");
    }

    private Vector2 GetAttackDirection()
    {
        try
        {
            Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            if (moveInput != Vector2.zero)
            {
                Debug.Log($"使用移动输入方向: {moveInput.normalized}");
                return moveInput.normalized;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"获取移动输入失败: {e.Message}");
        }

        if (lastAttackDirection != Vector2.zero)
        {
            Debug.Log($"使用最后方向: {lastAttackDirection}");
            return lastAttackDirection;
        }

        Debug.Log($"使用默认方向: 上");
        return Vector2.up;
    }

    private Collider2D[] DetectEnemiesInSector(Vector2 direction)
    {
        Vector2 attackOrigin = (Vector2)transform.position;
        
        Debug.Log($"检测参数 - 原点: {attackOrigin}, 方向: {direction}, 范围: {attackRange}, 角度: {attackAngle}");

        Collider2D[] allColliders = Physics2D.OverlapCircleAll(attackOrigin, attackRange);
        System.Collections.Generic.List<Collider2D> enemiesInSector = new System.Collections.Generic.List<Collider2D>();

        foreach (Collider2D collider in allColliders)
        {
            // 支持多个敌人标签
            if (collider.CompareTag("Enemy"))
            {
                Vector2 toEnemy = (Vector2)collider.transform.position - attackOrigin;
                float distance = toEnemy.magnitude;

                if (distance <= attackRange && distance > 0.1f)
                {
                    float angle = Vector2.Angle(direction, toEnemy.normalized);
                    
                    if (angle <= attackAngle / 2f)
                    {
                        enemiesInSector.Add(collider);
                        Debug.Log($"敌人 {collider.gameObject.name} (标签: {collider.tag}) 在扇形内，角度: {angle}, 距离: {distance}");
                    }
                }
            }
        }

        Debug.Log($"扇形检测完成，找到 {enemiesInSector.Count} 个敌人");
        return enemiesInSector.ToArray();
    }

    private void StartCooldown()
    {
        canAttack = false;
        cooldownTimer = attackCooldown;
    }

    private void PlayAttackEffects(Vector2 direction, bool hitTarget)
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(AttackColorFlash());
        }

        // StartCoroutine(ShowAttackArc(direction));
    }

    private System.Collections.IEnumerator AttackColorFlash()
    {
        if (spriteRenderer == null) yield break;
        
        spriteRenderer.color = attackEffectColor;
        yield return new WaitForSeconds(effectDuration);
        spriteRenderer.color = originalColor;
    }

    /// <summary>
    /// 显示攻击弧线（修正版）
    /// </summary>
    private System.Collections.IEnumerator ShowAttackArc(Vector2 direction)
    {
        GameObject arc = new GameObject("AttackArc");
        LineRenderer lineRenderer = arc.AddComponent<LineRenderer>();
        
        lineRenderer.positionCount = 20;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.yellow;

        // 修正：统一使用Vector3计算
        Vector3[] points = CalculateArcPoints(direction);
        lineRenderer.SetPositions(points);

        yield return new WaitForSeconds(0.3f);
        Destroy(arc);
    }

    /// <summary>
    /// 计算弧线点（修正版）
    /// </summary>
    private Vector3[] CalculateArcPoints(Vector2 direction)
    {
        Vector3[] points = new Vector3[20];
        Vector3 origin = transform.position; // 使用Vector3避免类型冲突
        float angleStep = attackAngle / 19f;
        float startAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - (attackAngle / 2f);

        for (int i = 0; i < 20; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Vector3 dir = new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad),
                0f // 明确指定z轴为0
            );
            points[i] = origin + (dir * attackRange);
        }

        return points;
    }

    void OnDrawGizmosSelected()
    {
        // 攻击范围圆
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 攻击方向扇形
        Vector2 currentDirection = Application.isPlaying ? debugAttackDirection : Vector2.right;
        //DrawAttackSectorGizmo(currentDirection);

        // 显示检测结果
        if (Application.isPlaying)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                                    $"攻击方向: {debugAttackDirection}\n击中: {debugHitCount}");
            #endif
        }
    }

    /// <summary>
    /// 绘制扇形Gizmo（修正版）
    /// </summary>
    private void DrawAttackSectorGizmo(Vector2 direction)
    {
        if (direction == Vector2.zero) return;

        float directionAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float halfAngle = attackAngle / 2f;

        Vector3 origin = transform.position; // 使用Vector3
        int segments = 20;
        float angleStep = attackAngle / segments;

        Gizmos.color = Color.red;

        
        // 绘制扇形边界
        for (int i = 0; i <= segments; i++)
        {
            float angle = directionAngle - halfAngle + (angleStep * i);
            Vector3 point = origin + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * attackRange,
                Mathf.Sin(angle * Mathf.Deg2Rad) * attackRange,
                0f // 明确指定z轴
            );

            if (i == 0 || i == segments)
            {
                Gizmos.DrawLine(origin, point);
            }
            
            if (i > 0)
            {
                float prevAngle = directionAngle - halfAngle + (angleStep * (i - 1));
                Vector3 prevPoint = origin + new Vector3(
                    Mathf.Cos(prevAngle * Mathf.Deg2Rad) * attackRange,
                    Mathf.Sin(prevAngle * Mathf.Deg2Rad) * attackRange,
                    0f
                );
                Gizmos.DrawLine(prevPoint, point);
            }
        }
        
    }

    [ContextMenu("测试攻击")]
    public void TestAttack()
    {
        PerformAttack();
    }
}