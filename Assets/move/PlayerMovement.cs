
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("瞬移闪现设置")]
    [SerializeField] private float dashDistance = 4.5f;
    [SerializeField] private float dashCooldown = 0.2f;

    [Header("闪现音效")]
    [SerializeField] private AudioClip dashAudioClip;


    [Header("时缓设置")]
    [SerializeField] private float timeSlowScale = 0.3f;           // 时间变慢倍数
    [SerializeField] private float timeSlowPlayerSpeed = 20f;      // 玩家在时缓中的速度
    [SerializeField] private float timeSlowDuration = 5f;          // 持续时间
    [SerializeField] private float timeSlowCooldown = 3f;          // 冷却时间

    [Header("时缓音效设置")]
    [SerializeField] private AudioClip timeSlowStartClip;  // 启动音效
    // [SerializeField] private AudioClip timeSlowEndClip;    // 结束音效
    private AudioSource audioSource;


    private bool isTimeSlowed = false;
    private bool canTimeSlow = true;
    private float defaultMoveSpeed;

    private Vector2? pendingDashPosition = null;  // 标记待瞬移位置

    [Header("角色朝向设置")]
    public bool flipWithMovement = true; // 是否根据移动方向翻转AA
    public float rotationSmoothness = 8f; // 旋转平滑度

    private Rigidbody2D rb;
    private PlayerWallCollision wallCollision;
    private PlayerInputActions inputActions;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;

    private bool canDash = true;
    private bool isDashing = false;  // 防止连点

    // 角色朝向相关
    private float targetRotation = 90f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultMoveSpeed = moveSpeed;
        audioSource = GetComponent<AudioSource>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        wallCollision = GetComponent<PlayerWallCollision>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Move.performed += ctx => OnMovePerformed(ctx);
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Dash.performed += ctx => TryDash();

        inputActions.Player.TimeSlow.performed += ctx => TryTimeSlow();

    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();


        moveInput = input;

        if (moveInput != Vector2.zero)
        {
            lastMoveDirection = moveInput.normalized;
            UpdateCharacterRotation();
        }
    }

    private void TryDash()
    {
        if (canDash && !isDashing)
            StartCoroutine(InstantDash());
    }

    private void FixedUpdate()
    {
        // 1. 处理待瞬移
        if (pendingDashPosition.HasValue)
        {
            rb.MovePosition(pendingDashPosition.Value);
            pendingDashPosition = null;
            return;  // 本帧只做瞬移
        }

        // 2. 正常移动
        if (isDashing) return;


        Vector2 movement = moveInput * moveSpeed * Time.fixedDeltaTime;


        if (wallCollision == null || !wallCollision.WillCollide(moveInput, moveSpeed * Time.fixedDeltaTime))
        {
            rb.MovePosition(rb.position + movement);
        }

        // 持续更新角色朝向（更平滑）
        if (moveInput != Vector2.zero && flipWithMovement)
        {
            UpdateCharacterRotation();
        }

        // 应用旋转平滑
        ApplyRotationSmoothly();
    }

    /// <summary>
    /// 更新角色朝向
    /// </summary>
    private void UpdateCharacterRotation()
    {
        if (moveInput == Vector2.zero) return;

        // 计算移动方向的角度（以度为单位）
        float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg + 90f;

        // 设置目标旋转角度
        targetRotation = angle;


    }

    /// <summary>
    /// 平滑应用旋转
    /// </summary>
    private void ApplyRotationSmoothly()
    {
        if (rotationSmoothness <= 0)
        {
            // 无平滑，直接设置旋转
            transform.rotation = Quaternion.Euler(0f, 0f, targetRotation);
        }
        else
        {
            // 平滑旋转
            Quaternion currentRotation = transform.rotation;
            Quaternion targetQuat = Quaternion.Euler(0f, 0f, targetRotation);
            transform.rotation = Quaternion.Lerp(currentRotation, targetQuat, rotationSmoothness * Time.deltaTime);
        }
    }

    // 瞬移闪现
    private IEnumerator InstantDash()
    {
        isDashing = true;
        canDash = false;

        // 1. 获取方向
        Vector2 direction = moveInput != Vector2.zero ? moveInput.normalized : lastMoveDirection;
        if (direction == Vector2.zero) direction = Vector2.right;

        // 2. 计算安全位置
        Vector2 start = rb.position;
        Vector2 target = start + direction * dashDistance;
        Vector2 safeTarget = GetSafeDashPosition(start, target);

        // 3. 瞬间移动（必须在 FixedUpdate 里！）
        // → 方案：用标志位，让 FixedUpdate 执行一次瞬移
        pendingDashPosition = safeTarget;
        yield return new WaitForFixedUpdate();  // 等待下一帧 FixedUpdate

        // 4. 清除速度
        rb.linearVelocity = Vector2.zero;

        // 5. 启动冷却（独立协程）
        StartCoroutine(DashCooldown());

        // 6. 特效
        StartCoroutine(DashVisualEffect(start, safeTarget));

        // 7. 状态恢复
        isDashing = false;
    }

    private Vector2 GetSafeDashPosition(Vector2 start, Vector2 target)
    {
        Vector2 dir = (target - start).normalized;
        float dist = Vector2.Distance(start, target);

        RaycastHit2D hit = Physics2D.Raycast(start, dir, dist, LayerMask.GetMask("Wall"));
        if (hit.collider != null)
        {
            return hit.point - dir * 0.05f;  // 停在墙前一点
        }
        return target;
    }


    private IEnumerator DashCooldown()
    {
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private IEnumerator DashVisualEffect(Vector2 start, Vector2 end)
    {
        // 🎧 播放启动音效
        if (dashAudioClip && audioSource)
            audioSource.PlayOneShot(dashAudioClip);


        // 简易闪光 + 残影
        var flash = new GameObject("DashFlash");
        var sr = flash.AddComponent<SpriteRenderer>();
        sr.sprite = GetComponentInChildren<SpriteRenderer>().sprite;
        sr.color = new Color(1, 1, 1, 0.7f);
        flash.transform.position = end;
        flash.transform.localScale = transform.localScale;
        Destroy(flash, 0.1f);

        // 残影（可选）
        for (int i = 0; i < 3; i++)
        {
            float t = i / 3f;
            Vector2 pos = Vector2.Lerp(start, end, t);
            var ghost = new GameObject("Ghost");
            ghost.transform.position = pos;
            ghost.transform.localScale = transform.localScale;
            var gsr = ghost.AddComponent<SpriteRenderer>();
            gsr.sprite = GetComponentInChildren<SpriteRenderer>().sprite;
            gsr.color = new Color(1, 1, 1, 0.5f - i * 0.15f);
            Destroy(ghost, 0.2f);
            yield return new WaitForSeconds(0.03f);
        }
    }

    // ======== 时缓技能（单次触发版） ========
    private void TryTimeSlow()
    {
        if (!canTimeSlow || isTimeSlowed) return;
        StartCoroutine(TimeSlowRoutine());
    }

    private IEnumerator TimeSlowRoutine()
    {
        if (isTimeSlowed) yield break;

        // 启动时缓
        isTimeSlowed = true;
        canTimeSlow = false;

        // 🎧 播放时缓启动音效
        if (timeSlowStartClip && audioSource)
            audioSource.PlayOneShot(timeSlowStartClip);

        // 特效
        StartCoroutine(TimeSlowVisualEffect());

        // 改变时间与速度
        Time.timeScale = timeSlowScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        moveSpeed = timeSlowPlayerSpeed;

        // 等待时缓持续时间（不受 timeScale 影响）
        yield return new WaitForSecondsRealtime(timeSlowDuration);

        // 🎧 播放时缓结束音效
        // if (timeSlowEndClip && audioSource)
        //     audioSource.PlayOneShot(timeSlowEndClip);

        // 恢复正常状态
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        moveSpeed = defaultMoveSpeed;
        isTimeSlowed = false;

        // 进入冷却阶段
        yield return new WaitForSecondsRealtime(timeSlowCooldown);
        canTimeSlow = true;
    }



    private IEnumerator TimeSlowVisualEffect()
    {
        var playerSR = GetComponentInChildren<SpriteRenderer>();
        if (playerSR == null)
            yield break;

        // 创建光罩对象
        GameObject overlay = new GameObject("TimeSlowOverlay");
        var sr = overlay.AddComponent<SpriteRenderer>();

        // 关键：使用同样的 Sorting Layer，并提高排序
        sr.sortingLayerID = playerSR.sortingLayerID;
        sr.sortingOrder = playerSR.sortingOrder + 1;

        // 蓝色泛光
        sr.color = new Color(0.3f, 0.6f, 1f, 0.25f);
        sr.sprite = playerSR.sprite;  // 用同样的贴图当作发光覆盖层

        overlay.transform.SetParent(transform);
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localScale = Vector3.one;

        float pulse = 0f;
        while (isTimeSlowed)
        {
            pulse += Time.unscaledDeltaTime * 3f;
            float alpha = 0.25f + Mathf.Sin(pulse * 6f) * 0.05f;
            sr.color = new Color(0.3f, 0.6f, 1f, alpha);
            yield return null;
        }

        Destroy(overlay);
    }

    // Exit

}

