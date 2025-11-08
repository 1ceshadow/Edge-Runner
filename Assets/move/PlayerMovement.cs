using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    //===================================================================
    //============================ 参数设置 =============================
    //===================================================================

    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 6.2f;

    [Header("瞬移闪现设置")]
    [SerializeField] private float dashDistance = 3.9f;
    [SerializeField] private float dashCooldown = 0.2f;
    [SerializeField] private AudioClip dashAudioClip;

    [Header("时缓设置")]
    [SerializeField] private float timeSlowScale = 0.3f;
    [SerializeField] private float timeSlowPlayerSpeed = 20f;
    [SerializeField] private float timeSlowDuration = 5f;
    [SerializeField] private float timeSlowCooldown = 3f;
    [SerializeField] private AudioClip timeSlowStartClip;

    [Header("角色朝向设置")]
    private float rotationSmoothness = 8f;

    /*
    [Header("爬墙标签")]
    [SerializeField] private string wallHorizontalTag = "WallHorizontal";
    [SerializeField] private string wallVerticalTag = "WallVertical";
    */
    [Header("爬墙检测")]
    [SerializeField] private float wallCheckExtra = 0.8f;  // 超出边缘的缓冲
    [SerializeField] private LayerMask wallLayerMask = -1;    // 墙体层（Inspector设置）
    
    //private string BillboardLayerName = "Billboard";

    //===================================================================
    //============================ 私有字段 =============================
    //===================================================================

    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private AudioSource audioSource;
    private PlayerWallCollision wallCollision;
    private PlayerInputActions inputActions;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;

    private bool isDashing = false;
    private bool canDash = true; // 两个有同时存在的必要，以便后续开发
    private bool isTimeSlowed = false;
    private bool canTimeSlow = true;

    private float defaultMoveSpeed;
    private Vector2? pendingDashPosition = null;
    private float targetRotation = 90f;

    // 爬墙状态
    //private bool isStickingWallHorizontal = false;
    //private bool isStickingWallVertical = false;
    private float wallCheckDistance;  // 检测距离
    // 精细化爬墙状态（4个方向独立检测）
    private bool isTouchingTopWall = false;    // 碰到上面的墙
    private bool isTouchingBottomWall = false; // 碰到下面的墙  
    private bool isTouchingLeftWall = false;   // 碰到左边的墙
    private bool isTouchingRightWall = false;  // 碰到右边的墙

    // 视觉缓存
    private Sprite playerSprite;
    private int sortingLayerID;
    private int sortingOrder;

    //===================================================================
    //============================ 初始化 ==============================
    //===================================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        wallCollision = GetComponent<PlayerWallCollision>();
        inputActions = new PlayerInputActions();
        circleCollider = GetComponent<CircleCollider2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        defaultMoveSpeed = moveSpeed;
        wallCheckDistance = circleCollider.radius + wallCheckExtra; // 适配广告牌探测距离；

        // 缓存 SpriteRenderer
        CacheVisuals();
    }

    private void CacheVisuals()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            playerSprite = sr.sprite;
            sortingLayerID = sr.sortingLayerID;
            sortingOrder = sr.sortingOrder;
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Dash.performed += ctx => TryDash();
        inputActions.Player.TimeSlow.performed += ctx => TryTimeSlow();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    //===================================================================
    //============================ 物理更新 =============================
    //===================================================================

    private void FixedUpdate()
    {
        // 1. 处理瞬移
        if (pendingDashPosition.HasValue)
        {
            rb.MovePosition(pendingDashPosition.Value);
            pendingDashPosition = null;
            // 重置爬墙状态
            //ResetWallSticking();
            return;  // 本帧只做瞬移
        }

        // 2. 闪现中不移动
        if (isDashing) return;

        // 3. 精细化爬墙检测 & 运动限制
        UpdateWallTouching();
        Vector2 filteredInput = GetFilteredMoveInput();

        // 4. 正常移动
        //Vector2 filteredInput = GetFilteredMoveInput();
        Vector2 movement = filteredInput * moveSpeed * Time.fixedDeltaTime;
       

        if (wallCollision == null || !wallCollision.WillCollide(filteredInput, moveSpeed * Time.fixedDeltaTime))
        {
            rb.MovePosition(rb.position + movement);
        }

        // 5. 更新朝向
        if (moveInput != Vector2.zero)
        {
            lastMoveDirection = moveInput.normalized;
            UpdateCharacterRotation();
        }
        ApplyRotationSmoothly();

    }

    //===================================================================
    //============================ 输入处理 =============================
    //===================================================================

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    //===================================================================
    //============================ 瞬移技能 =============================
    //===================================================================

    private void TryDash()
    {
        if (!canDash || isDashing) return;
        StartCoroutine(InstantDash());
    }

    private IEnumerator InstantDash()
    {
        isDashing = true;
        canDash = false;

        // 1. 获取方向
        Vector2 dir = moveInput != Vector2.zero ? moveInput.normalized : lastMoveDirection;

        // 2. 计算安全位置
        Vector2 start = rb.position;
        Vector2 target = start + dir * dashDistance;
        Vector2 safeTarget = GetSafeDashPositionHybrid(start, target);

        // 3. 瞬间移动（必须在 FixedUpdate 里！）
        // → 方案：用标志位，让 FixedUpdate 执行一次瞬移
        pendingDashPosition = safeTarget;
        yield return new WaitForFixedUpdate();  // 等待下一帧 FixedUpdate

        // 4. 清除速度
        rb.linearVelocity = Vector2.zero;

        // 音效和特效
        PlayOneShot(dashAudioClip);
        StartCoroutine(DashVisualEffect(start, safeTarget));

        // 5. 启动冷却（独立协程）
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);

        canDash = true;
        
    }
    //计算安全瞬移位置（混合射线+圆形检测）
    private Vector2 GetSafeDashPositionHybrid(Vector2 start, Vector2 target)
    {
        Vector2 dir = (target - start).normalized;
        float totalDist = Vector2.Distance(start, target);
        float playerRadius = GetPlayerRadius();
        
        // 第一步：快速射线检测
        RaycastHit2D hit = Physics2D.Raycast(start, dir, totalDist, LayerMask.GetMask("Wall"));
        if (hit.collider == null)
            return target; // 没有碰撞，直接到达目标
        
        // 第二步：精确圆形检测找到碰撞点
        float collisionDist = hit.distance;
        RaycastHit2D preciseHit = Physics2D.CircleCast(start, playerRadius, dir, collisionDist + 0.5f, LayerMask.GetMask("Wall"));
        
        if (preciseHit.collider != null)
        {
            collisionDist = preciseHit.distance;
        }
        
        // 第三步：计算安全距离（考虑玩家朝向和墙面法线）
        float safeDistance = CalculateSafeDistance(dir, preciseHit.normal, playerRadius);
        float finalDist = Mathf.Max(0, collisionDist - safeDistance);
        
        return start + dir * finalDist;
    }
    // 计算安全距离（考虑玩家朝向和墙面法线）
    private float CalculateSafeDistance(Vector2 moveDir, Vector2 wallNormal, float playerRadius)
    {
        // 根据碰撞角度调整安全距离
        float dot = Vector2.Dot(moveDir, -wallNormal);
        float angleFactor = Mathf.Clamp(dot, 0.5f, 1f);

        return playerRadius * 1.2f * angleFactor;
    }
    
    private float GetPlayerRadius()
    {
        // 获取玩家碰撞体半径
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        return collider != null ? collider.radius : 0.3f;
    }

    private IEnumerator DashVisualEffect(Vector2 start, Vector2 end)
    {
        // 闪光
        CreateGhost(end, 0.7f, 0.1f);

        // 残影
        for (int i = 0; i < 3; i++)
        {
            float t = (i + 1) / 4f;
            Vector2 pos = Vector2.Lerp(start, end, t);
            float alpha = 0.5f - i * 0.15f;
            CreateGhost(pos, alpha, 0.2f);
            yield return new WaitForSeconds(0.03f);
        }
    }

    //===================================================================
    //============================ 时缓技能 =============================
    //===================================================================

    private void TryTimeSlow()
    {
        if (!canTimeSlow || isTimeSlowed) return;
        StartCoroutine(TimeSlowRoutine());
    }

    private IEnumerator TimeSlowRoutine()
    {
        isTimeSlowed = true;
        canTimeSlow = false;

        PlayOneShot(timeSlowStartClip);
        StartCoroutine(TimeSlowVisualEffect());

        Time.timeScale = timeSlowScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        moveSpeed = timeSlowPlayerSpeed;

        yield return new WaitForSecondsRealtime(timeSlowDuration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        moveSpeed = defaultMoveSpeed;
        isTimeSlowed = false;

        yield return new WaitForSecondsRealtime(timeSlowCooldown);
        canTimeSlow = true;
    }

    private IEnumerator TimeSlowVisualEffect()
    {
        SpriteRenderer playerSR = GetComponentInChildren<SpriteRenderer>();
        if (playerSR == null || playerSR.sprite == null)
        {
            Debug.LogWarning("TimeSlow: 未找到 SpriteRenderer 或 Sprite，特效已跳过");
            yield break;
        }

        GameObject overlay = new GameObject("TimeSlowOverlay");
        overlay.transform.SetParent(transform, false);

        var sr = overlay.AddComponent<SpriteRenderer>();
        sr.sprite = playerSR.sprite;
        sr.sortingLayerID = playerSR.sortingLayerID;
        sr.sortingOrder = playerSR.sortingOrder + 1;
        sr.color = new Color(0.3f, 0.6f, 1f, 0.25f);

        float pulse = 0f;
        while (isTimeSlowed)
        {
            pulse += Time.unscaledDeltaTime * 3f;
            float alpha = 0.25f + Mathf.Sin(pulse * 6f) * 0.05f;
            sr.color = new Color(0.3f, 0.6f, 1f, alpha);

            // 实时更新 Sprite（支持动画切换）
            if (playerSR.sprite != sr.sprite)
                sr.sprite = playerSR.sprite;

            yield return null;
        }

        Destroy(overlay);
    }

    //===================================================================
    //============================ 旋转系统 =============================
    //===================================================================

    private void UpdateCharacterRotation()
    {
        if (moveInput == Vector2.zero) return;
        float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg + 90f;
        targetRotation = angle;
    }

    private void ApplyRotationSmoothly()
    {
        Quaternion target = Quaternion.Euler(0f, 0f, targetRotation);
        if (rotationSmoothness <= 0f)
        {
            transform.rotation = target;
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, target, rotationSmoothness * Time.deltaTime);
        }
    }

    //===================================================================
    //============================ 精细化爬墙系统 =====================
    //===================================================================

    /// <summary>
    /// 每帧检测4个方向的墙体接触
    /// </summary>
    private void UpdateWallTouching()
    {
        Vector2 pos = transform.position;

        // 4方向Raycast检测（从角色中心向外射线）
        isTouchingTopWall = Physics2D.Raycast(pos, Vector2.up, wallCheckDistance, wallLayerMask).collider != null;
        isTouchingBottomWall = Physics2D.Raycast(pos, Vector2.down, wallCheckDistance, wallLayerMask).collider != null;
        isTouchingLeftWall = Physics2D.Raycast(pos, Vector2.left, wallCheckDistance, wallLayerMask).collider != null;
        isTouchingRightWall = Physics2D.Raycast(pos, Vector2.right, wallCheckDistance, wallLayerMask).collider != null;


        // Debug（测试后可删除）
        if (isTouchingTopWall || isTouchingBottomWall || isTouchingLeftWall || isTouchingRightWall)
        {
            Debug.Log($"墙体接触: 上={isTouchingTopWall}, 下={isTouchingBottomWall}, 左={isTouchingLeftWall}, 右={isTouchingRightWall}");
        }
    }

    /// <summary>
    /// 根据墙体接触状态过滤输入（精细化限制）
    /// 碰到上面的墙 → 不能向上
    /// 碰到右边的墙 → 不能向右
    /// 碰到左边的墙 → 不能向左  
    /// 碰到下面的墙 → 不能向下
    /// </summary>
    private Vector2 GetFilteredMoveInput()
    {
        // 统计碰到的墙的数量 >= 2
        bool MultipleWalls = 
        (isTouchingTopWall    ? 1 : 0) +
        (isTouchingBottomWall ? 1 : 0) +
        (isTouchingLeftWall   ? 1 : 0) +
        (isTouchingRightWall  ? 1 : 0) >= 2;
        // 同时碰到多个墙，不限制玩家移动
        if (MultipleWalls) return moveInput;
    
        Vector2 filtered = moveInput;

        // 只限制"向墙方向"的移动
        if (isTouchingTopWall && filtered.y < 0f) filtered.y = 0f;    // 上墙 → 禁向下
        if (isTouchingBottomWall && filtered.y > 0f) filtered.y = 0f;    // 下墙 → 禁向上
        if (isTouchingLeftWall && filtered.x > 0f) filtered.x = 0f;    // 左墙 → 禁向右
        if (isTouchingRightWall && filtered.x < 0f) filtered.x = 0f;    // 右墙 → 禁向左

        return filtered;
    }

    /// <summary>
    /// 瞬移后重置所有墙体接触状态
    /// </summary>
    private void ResetWallTouching()
    {
        isTouchingTopWall = false;
        isTouchingBottomWall = false;
        isTouchingLeftWall = false;
        isTouchingRightWall = false;
    }

    //===================================================================
    //============================ 工具方法 =============================
    //===================================================================

    private void PlayOneShot(AudioClip clip)
    {
        if (clip && audioSource) audioSource.PlayOneShot(clip);
    }

    private void CreateGhost(Vector2 pos, float alpha, float lifetime)
    {
        SpriteRenderer playerSR = GetComponentInChildren<SpriteRenderer>();
        if (playerSR == null || playerSR.sprite == null)
        {
            Debug.LogWarning("CreateGhost: 未找到 SpriteRenderer 或 Sprite，残影已跳过");
            return;
        }

        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = pos;
        ghost.transform.localScale = transform.localScale;

        var sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = playerSR.sprite;
        sr.sortingLayerID = playerSR.sortingLayerID;
        sr.sortingOrder = playerSR.sortingOrder + 1;  // 永远在玩家上面
        sr.color = new Color(1f, 1f, 1f, alpha);

        Destroy(ghost, lifetime);
    }

    //===================================================================
    //============================ 可视化调试 =============================
    //===================================================================
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector2 pos = transform.position;
        float dist = wallCheckDistance;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(pos, Vector2.up * dist);
        Gizmos.DrawRay(pos, Vector2.down * dist);
        Gizmos.DrawRay(pos, Vector2.left * dist);
        Gizmos.DrawRay(pos, Vector2.right * dist);
    }
}