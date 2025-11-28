// ═══════════════════════════════════════════════════════════════════════════
//  SwordHitbox - 剑碰撞体控制器
//  负责剑的物理形状、挥砍动画和碰撞检测
//  
//  设计：
//  - 作为玩家的子对象，跟随玩家移动
//  - 挥砍时绕玩家旋转，模拟真实剑击轨迹
//  - 使用 Trigger 检测命中，避免物理推挤
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using UnityEngine;
using EdgeRunner.Config;

namespace EdgeRunner.Player.Combat
{
    /// <summary>
    /// 剑碰撞体控制器
    /// 管理剑的显示、旋转动画和命中检测
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SwordHitbox : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        //                          序列化字段
        // ═══════════════════════════════════════════════════════════════

        [Header("碰撞体")]
        [SerializeField] private PolygonCollider2D swordCollider;
        
        [Header("视觉")]
        [SerializeField] private SpriteRenderer swordRenderer;
        [SerializeField] private Color activeColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0f);

        [Header("调试")]
        [SerializeField] private bool showDebugGizmos = true;

        // ═══════════════════════════════════════════════════════════════
        //                          事件
        // ═══════════════════════════════════════════════════════════════

        /// <summary>命中敌人时触发，参数为被命中的碰撞体</summary>
        public event Action<Collider2D> OnHitEnemy;

        // ═══════════════════════════════════════════════════════════════
        //                          私有字段
        // ═══════════════════════════════════════════════════════════════

        private readonly HashSet<Collider2D> hitTargetsThisSwing = new HashSet<Collider2D>();
        private LayerMask enemyLayerMask;
        private bool isSwinging;
        private float swingProgress;
        private float swingStartAngle;
        private float swingEndAngle;
        private float swingDuration;
        private Vector2 facingDirection;

        // 剑的形状参数
        private float swordLength;
        private float swordWidth;

        // ═══════════════════════════════════════════════════════════════
        //                          公共属性
        // ═══════════════════════════════════════════════════════════════

        /// <summary>是否正在挥砍</summary>
        public bool IsSwinging => isSwinging;

        /// <summary>当前挥砍进度 (0-1)</summary>
        public float SwingProgress => swingProgress;

        // ═══════════════════════════════════════════════════════════════
        //                          初始化
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // 自动获取组件
            if (swordCollider == null)
            {
                swordCollider = GetComponent<PolygonCollider2D>();
            }
            if (swordRenderer == null)
            {
                swordRenderer = GetComponent<SpriteRenderer>();
            }

            // 确保碰撞体是 Trigger
            if (swordCollider != null)
            {
                swordCollider.isTrigger = true;
            }

            // 初始隐藏
            SetSwordVisible(false);
        }

        /// <summary>
        /// 初始化剑参数
        /// </summary>
        public void Initialize(LayerMask enemyMask)
        {
            enemyLayerMask = enemyMask;
            
            // 从配置读取剑参数
            swordLength = ConfigManager.GetSwordLength();
            swordWidth = ConfigManager.GetSwordWidth();

            // 生成剑形碰撞体
            GenerateSwordShape();
        }

        // ═══════════════════════════════════════════════════════════════
        //                          挥砍控制
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 开始挥砍
        /// </summary>
        /// <param name="direction">玩家朝向</param>
        /// <param name="startAngle">起始角度（相对于朝向）</param>
        /// <param name="endAngle">结束角度（相对于朝向）</param>
        /// <param name="duration">挥砍持续时间</param>
        public void StartSwing(Vector2 direction, float startAngle, float endAngle, float duration)
        {
            if (isSwinging) return;

            facingDirection = direction.normalized;
            if (facingDirection == Vector2.zero)
            {
                facingDirection = Vector2.up;
            }

            swingStartAngle = startAngle;
            swingEndAngle = endAngle;
            swingDuration = duration;
            swingProgress = 0f;
            isSwinging = true;

            hitTargetsThisSwing.Clear();
            SetSwordVisible(true);

            // 设置初始角度
            UpdateSwordRotation(0f);
        }

        /// <summary>
        /// 停止挥砍
        /// </summary>
        public void StopSwing()
        {
            isSwinging = false;
            swingProgress = 0f;
            SetSwordVisible(false);
            hitTargetsThisSwing.Clear();
        }

        // ═══════════════════════════════════════════════════════════════
        //                          更新循环
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            if (!isSwinging) return;

            // 更新挥砍进度
            swingProgress += Time.deltaTime / swingDuration;

            if (swingProgress >= 1f)
            {
                StopSwing();
                return;
            }

            // 使用缓动函数使挥砍更自然（快-慢-快）
            float easedProgress = EaseInOutQuad(swingProgress);
            UpdateSwordRotation(easedProgress);
        }

        private void UpdateSwordRotation(float progress)
        {
            // 计算当前角度（插值）
            float currentAngle = Mathf.Lerp(swingStartAngle, swingEndAngle, progress);

            // 计算基础朝向角度
            float baseAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;

            // 最终旋转角度（朝向 + 挥砍偏移，-90 是因为剑默认朝上）
            float finalAngle = baseAngle + currentAngle - 90f;

            transform.localRotation = Quaternion.Euler(0, 0, finalAngle);
        }

        // ═══════════════════════════════════════════════════════════════
        //                          碰撞检测
        // ═══════════════════════════════════════════════════════════════

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isSwinging) return;

            // 检查是否是敌人层
            if ((enemyLayerMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            // 避免同一次挥砍重复命中同一目标
            if (hitTargetsThisSwing.Contains(other))
            {
                return;
            }

            hitTargetsThisSwing.Add(other);
            OnHitEnemy?.Invoke(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // 使用 Stay 确保快速移动时也能检测
            OnTriggerEnter2D(other);
        }

        // ═══════════════════════════════════════════════════════════════
        //                          形状生成
        // ═══════════════════════════════════════════════════════════════

        private void GenerateSwordShape()
        {
            if (swordCollider == null) return;

            // 生成剑形多边形（类似细长三角形）
            // 剑尖在上，剑柄在下（原点位置）
            float halfWidth = swordWidth * 0.5f;
            float tipWidth = swordWidth * 0.15f; // 剑尖更细

            Vector2[] points = new Vector2[]
            {
                // 剑柄（底部）
                new Vector2(-halfWidth * 0.6f, 0f),
                new Vector2(halfWidth * 0.6f, 0f),
                // 剑身（中部）
                new Vector2(halfWidth, swordLength * 0.2f),
                new Vector2(halfWidth * 0.8f, swordLength * 0.7f),
                // 剑尖（顶部）
                new Vector2(tipWidth, swordLength * 0.9f),
                new Vector2(0f, swordLength),  // 剑尖
                new Vector2(-tipWidth, swordLength * 0.9f),
                // 剑身（中部，左侧）
                new Vector2(-halfWidth * 0.8f, swordLength * 0.7f),
                new Vector2(-halfWidth, swordLength * 0.2f),
            };

            swordCollider.SetPath(0, points);
        }

        // ═══════════════════════════════════════════════════════════════
        //                          视觉控制
        // ═══════════════════════════════════════════════════════════════

        private void SetSwordVisible(bool visible)
        {
            if (swordCollider != null)
            {
                swordCollider.enabled = visible;
            }

            if (swordRenderer != null)
            {
                swordRenderer.color = visible ? activeColor : inactiveColor;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          工具方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 缓动函数 - 快入快出
        /// </summary>
        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        // ═══════════════════════════════════════════════════════════════
        //                          编辑器调试
        // ═══════════════════════════════════════════════════════════════

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            float length = Application.isPlaying ? swordLength : ConfigManager.GetSwordLength();
            float width = Application.isPlaying ? swordWidth : ConfigManager.GetSwordWidth();

            // 绘制剑的范围
            Gizmos.color = Color.cyan;
            Vector3 tipPos = transform.position + transform.up * length;
            Gizmos.DrawLine(transform.position, tipPos);
            Gizmos.DrawWireSphere(tipPos, width * 0.5f);
        }
    }
}
