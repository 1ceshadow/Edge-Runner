using System;
using System.Collections;
using UnityEngine;

namespace EdgeRunner.Player.Systems
{
    /// <summary>
    /// 玩家移动控制器 - 负责移动、冲刺、墙体检测
    /// 
    /// 核心特性：
    /// - 正常移动：CircleCast 连续碰撞检测，确保不穿墙
    /// - 冲刺：Raycast + CircleCast 混合检测计算安全位置
    /// - 广告牌吸附：四方向广告牌接触检测（仅 Billboard 层）
    /// - pendingDashPosition 模式：冲刺在 FixedUpdate 中执行
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        //                          配置
        // ═══════════════════════════════════════════════════════════════
        
        [Header("碰撞检测")]
        [SerializeField] private LayerMask wallLayerMask;       // 墙体层（冲刺碰撞）
        [SerializeField] private LayerMask billboardLayerMask;  // 广告牌层（吸附功能）
        [SerializeField] private float wallCheckExtra = 0.8f;
        [SerializeField] private float collisionOffset = 0.05f;
        
        // ═══════════════════════════════════════════════════════════════
        //                          组件缓存
        // ═══════════════════════════════════════════════════════════════
        
        private Rigidbody2D rb;
        private CircleCollider2D circleCollider;
        private SpriteRenderer spriteRenderer;
        
        // ═══════════════════════════════════════════════════════════════
        //                          运行时状态
        // ═══════════════════════════════════════════════════════════════
        
        // 墙体接触状态
        private bool touchTop, touchBottom, touchLeft, touchRight;
        
        // 冲刺状态
        private Vector2? pendingDashPosition;
        private Vector2 lastValidPosition;
        private bool isDashing;
        private bool canDash = true;
        
        // 旋转
        private float targetRotation = 90f;
        private Vector2 lastMoveDirection = Vector2.right;
        
        // ═══════════════════════════════════════════════════════════════
        //                          公开属性
        // ═══════════════════════════════════════════════════════════════
        
        public bool IsDashing => isDashing;
        public bool CanDash => canDash && !isDashing;
        public Vector2 LastMoveDirection => lastMoveDirection;
        public float TargetRotation => targetRotation;
        
        /// <summary>冲刺完成事件（参数：起点，终点，是否完美闪避）</summary>
        public event Action<Vector2, Vector2, bool> OnDashCompleted;
        
        // ═══════════════════════════════════════════════════════════════
        //                          生命周期
        // ═══════════════════════════════════════════════════════════════
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            circleCollider = GetComponent<CircleCollider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            lastValidPosition = rb.position;
        }
        
        private void FixedUpdate()
        {
            // 处理挂起的冲刺（必须在 FixedUpdate 中执行）
            // 完全复制原始 PlayerMovement 的逻辑
            if (pendingDashPosition.HasValue)
            {
                rb.MovePosition(pendingDashPosition.Value);
                pendingDashPosition = null;
                return; // 本帧只做瞬移
            }
        }
        
        // ═══════════════════════════════════════════════════════════════
        //                          公开方法
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// 设置墙体检测层（用于冲刺碰撞）
        /// </summary>
        public void SetWallLayerMask(LayerMask mask) => wallLayerMask = mask;
        
        /// <summary>
        /// 设置广告牌检测层（用于吸附）
        /// </summary>
        public void SetBillboardLayerMask(LayerMask mask) => billboardLayerMask = mask;
        
        /// <summary>
        /// 更新广告牌接触状态（每帧调用，用于吸附）
        /// 注意：只检测 Billboard 层，墙体没有吸附功能
        /// </summary>
        public void UpdateWallContacts()
        {
            Vector2 pos = rb.position;
            float dist = GetWallCheckDistance();
            
            // 只对广告牌进行吸附检测
            LayerMask adhesionMask = GetEffectiveBillboardMask();
            touchTop = Physics2D.Raycast(pos, Vector2.up, dist, adhesionMask).collider != null;
            touchBottom = Physics2D.Raycast(pos, Vector2.down, dist, adhesionMask).collider != null;
            touchLeft = Physics2D.Raycast(pos, Vector2.left, dist, adhesionMask).collider != null;
            touchRight = Physics2D.Raycast(pos, Vector2.right, dist, adhesionMask).collider != null;
            
            // 更新有效位置（使用墙体层检测穿墙）
            if (!IsOverlappingWall(pos))
            {
                lastValidPosition = pos;
            }
        }
        
        /// <summary>
        /// 过滤输入实现广告牌吸附效果
        /// 碰到上面的广告牌 → 不能向下（禁止离开广告牌）
        /// 碰到右边的广告牌 → 不能向左
        /// 碰到左边的广告牌 → 不能向右  
        /// 碰到下面的广告牌 → 不能向上
        /// 注意：只有广告牌有吸附，墙体没有
        /// </summary>
        public Vector2 GetFilteredInput(Vector2 rawInput)
        {
            // 统计碰到的墙的数量 >= 2
            int wallCount = (touchTop ? 1 : 0) + (touchBottom ? 1 : 0) + 
                           (touchLeft ? 1 : 0) + (touchRight ? 1 : 0);
            // 同时碰到多个墙，不限制玩家移动
            if (wallCount >= 2) return rawInput;
            
            Vector2 filtered = rawInput;
            
            // 只限制“向墙方向”的移动（吸附效果）
            if (touchTop && filtered.y < 0f) filtered.y = 0f;      // 上墙 → 禁向下
            if (touchBottom && filtered.y > 0f) filtered.y = 0f;   // 下墙 → 禁向上
            if (touchLeft && filtered.x > 0f) filtered.x = 0f;     // 左墙 → 禁向右
            if (touchRight && filtered.x < 0f) filtered.x = 0f;    // 右墙 → 禁向左
            
            return filtered;
        }
        
        /// <summary>
        /// 执行移动（使用 CircleCast 碰撞检测）
        /// </summary>
        public void Move(Vector2 input, float speed, float deltaTime)
        {
            if (isDashing || pendingDashPosition.HasValue) return;
            
            Vector2 movement = input * speed * deltaTime;
            if (movement.sqrMagnitude < 0.0001f) return;
            
            // CircleCast 检测前方碰撞
            float radius = GetScaledRadius();
            Vector2 dir = movement.normalized;
            float dist = movement.magnitude;
            
            RaycastHit2D hit = Physics2D.CircleCast(
                rb.position, radius, dir, dist, wallLayerMask
            );
            
            if (hit.collider != null)
            {
                // 计算安全距离
                dist = Mathf.Max(0f, hit.distance - collisionOffset);
                movement = dir * dist;
            }
            
            Vector2 newPos = rb.position + movement;
            rb.MovePosition(newPos);
            
            // 验证并更新有效位置
            if (!IsOverlappingWall(newPos))
            {
                lastValidPosition = newPos;
            }
        }
        
        /// <summary>
        /// 更新角色旋转
        /// </summary>
        public void UpdateRotation(Vector2 moveInput, float smoothness, float deltaTime)
        {
            if (moveInput != Vector2.zero)
            {
                lastMoveDirection = moveInput.normalized;
                targetRotation = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg + 90f;
            }
            
            Quaternion target = Quaternion.Euler(0f, 0f, targetRotation);
            transform.rotation = smoothness <= 0f 
                ? target 
                : Quaternion.Lerp(transform.rotation, target, smoothness * deltaTime);
        }
        
        /// <summary>
        /// 尝试执行冲刺
        /// </summary>
        /// <param name="direction">冲刺方向（为零时使用最后移动方向）</param>
        /// <param name="distance">冲刺距离</param>
        /// <param name="cooldown">冷却时间</param>
        /// <param name="isPerfectDash">是否触发完美闪避</param>
        /// <returns>是否成功开始冲刺</returns>
        public bool TryDash(Vector2 direction, float distance, float cooldown, bool isPerfectDash = false)
        {
            if (!CanDash) return false;
            
            Vector2 dir = direction != Vector2.zero ? direction.normalized : lastMoveDirection;
            if (dir == Vector2.zero) return false;
            
            StartCoroutine(DashRoutine(dir, distance, cooldown, isPerfectDash));
            return true;
        }
        
        /// <summary>
        /// 取消当前冲刺
        /// </summary>
        public void CancelDash()
        {
            StopAllCoroutines();
            isDashing = false;
            canDash = true;
            pendingDashPosition = null;
        }
        
        /// <summary>
        /// 播放冲刺残影特效
        /// </summary>
        public IEnumerator PlayDashVisualEffect(Vector2 start, Vector2 end)
        {
            CreateGhost(end, 0.7f, 0.1f);
            
            for (int i = 0; i < 3; i++)
            {
                float t = (i + 1) / 4f;
                Vector2 pos = Vector2.Lerp(start, end, t);
                float alpha = 0.5f - i * 0.15f;
                CreateGhost(pos, alpha, 0.2f);
                yield return new WaitForSeconds(0.03f);
            }
        }
        
        // ═══════════════════════════════════════════════════════════════
        //                          冲刺逻辑
        // ═══════════════════════════════════════════════════════════════
        
        private IEnumerator DashRoutine(Vector2 dir, float distance, float cooldown, bool isPerfect)
        {
            isDashing = true;
            canDash = false;
            
            Vector2 start = rb.position;
            Vector2 rawTarget = start + dir * distance;
            Vector2 safeTarget = GetSafeDashPosition(start, rawTarget);
            
            // 设置挂起位置，等待 FixedUpdate 执行
            pendingDashPosition = safeTarget;
            yield return new WaitForFixedUpdate();
            
            // 清除速度
            rb.linearVelocity = Vector2.zero;
            isDashing = false;
            
            // 播放残影
            StartCoroutine(PlayDashVisualEffect(start, safeTarget));
            
            // 触发事件
            OnDashCompleted?.Invoke(start, safeTarget, isPerfect);
            
            // 冷却
            yield return new WaitForSeconds(cooldown);
            canDash = true;
        }
        
        /// <summary>
        /// 计算安全瞬移位置（纯 CircleCast 检测，最可靠）
        /// </summary>
        private Vector2 GetSafeDashPosition(Vector2 start, Vector2 target)
        {
            Vector2 dir = (target - start).normalized;
            float totalDist = Vector2.Distance(start, target);
            float playerRadius = GetScaledRadius();
            LayerMask effectiveMask = GetEffectiveWallMask();

            // CircleCast：模拟玩家圆形碰撞体的移动路径
            RaycastHit2D hit = Physics2D.CircleCast(
                start, 
                playerRadius, 
                dir, 
                totalDist, 
                effectiveMask
            );

            if (hit.collider == null)
            {
                return target; // 没有碰撞，直接到达目标
            }

            // 计算安全停止位置
            // hit.distance 是圆心移动的距离，需要再留一点余量
            float safeDistance = Mathf.Max(0f, hit.distance - collisionOffset);
            
            return start + dir * safeDistance;
        }
        
        // ═══════════════════════════════════════════════════════════════
        //                          工具方法
        // ═══════════════════════════════════════════════════════════════
        
        private float GetScaledRadius()
        {
            if (circleCollider == null) return 0.3f;
            return circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }
        
        private float GetWallCheckDistance()
        {
            return circleCollider != null 
                ? circleCollider.radius + wallCheckExtra 
                : wallCheckExtra;
        }
        
        private bool IsOverlappingWall(Vector2 position)
        {
            float radius = GetScaledRadius() - collisionOffset;
            Vector2 offset = circleCollider != null ? circleCollider.offset : Vector2.zero;
            LayerMask effectiveMask = GetEffectiveWallMask();
            return Physics2D.OverlapCircle(position + offset, radius, effectiveMask) != null;
        }
        
        /// <summary>
        /// 获取有效的墙体层（冲刺碰撞用）
        /// </summary>
        private LayerMask GetEffectiveWallMask()
        {
            return wallLayerMask != 0 ? wallLayerMask : LayerMask.GetMask("Wall");
        }
        
        /// <summary>
        /// 获取有效的广告牌层（吸附用）
        /// </summary>
        private LayerMask GetEffectiveBillboardMask()
        {
            return billboardLayerMask != 0 ? billboardLayerMask : LayerMask.GetMask("Billboard");
        }
        
        private void CreateGhost(Vector2 pos, float alpha, float lifetime)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;
            
            var ghost = new GameObject("DashGhost");
            ghost.transform.position = pos;
            ghost.transform.localScale = transform.localScale;
            
            var sr = ghost.AddComponent<SpriteRenderer>();
            sr.sprite = spriteRenderer.sprite;
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder = spriteRenderer.sortingOrder + 1;
            sr.color = new Color(1f, 1f, 1f, alpha);
            
            Destroy(ghost, lifetime);
        }
        
        // ═══════════════════════════════════════════════════════════════
        //                          调试可视化
        // ═══════════════════════════════════════════════════════════════
        
        private void OnDrawGizmosSelected()
        {
            var col = circleCollider != null ? circleCollider : GetComponent<CircleCollider2D>();
            if (col == null) return;
            
            Vector2 center = (Vector2)transform.position + col.offset;
            float radius = col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            
            // 碰撞检测范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, radius - collisionOffset);
            
            // 墙体检测范围
            Gizmos.color = Color.cyan;
            float dist = GetWallCheckDistance();
            Gizmos.DrawRay(center, Vector2.up * dist);
            Gizmos.DrawRay(center, Vector2.down * dist);
            Gizmos.DrawRay(center, Vector2.left * dist);
            Gizmos.DrawRay(center, Vector2.right * dist);
        }
    }
}
