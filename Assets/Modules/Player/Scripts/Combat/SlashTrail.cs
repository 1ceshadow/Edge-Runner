// ═══════════════════════════════════════════════════════════════════════════
//  SlashTrail - 挥砍拖尾特效
//  在剑挥动时生成弧形拖尾效果
//  
//  实现方式：
//  - 使用 LineRenderer 绘制弧线
//  - 动态更新顶点跟随剑的运动
//  - 支持渐变颜色和宽度
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using EdgeRunner.Config;

namespace EdgeRunner.Player.Combat
{
    /// <summary>
    /// 挥砍拖尾特效
    /// 创建剑挥动时的视觉弧线效果
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class SlashTrail : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        //                          序列化字段
        // ═══════════════════════════════════════════════════════════════

        [Header("拖尾参数")]
        [SerializeField] private int segmentCount = 20;
        [SerializeField] private float trailWidth = 0.15f;
        [SerializeField] private float trailWidthEnd = 0.02f;
        
        [Header("颜色")]
        [SerializeField] private Color trailStartColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color trailEndColor = new Color(0.8f, 0.9f, 1f, 0f);

        [Header("淡出")]
        [SerializeField] private float fadeOutDuration = 0.1f;

        // ═══════════════════════════════════════════════════════════════
        //                          私有字段
        // ═══════════════════════════════════════════════════════════════

        private LineRenderer lineRenderer;
        private Vector3[] positions;
        
        private bool isActive;
        private float trailProgress;
        private float trailDuration;
        private float fadeOutTimer;
        
        private Vector2 facingDirection;
        private float startAngle;
        private float endAngle;
        private float swordLength;

        // ═══════════════════════════════════════════════════════════════
        //                          初始化
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            positions = new Vector3[segmentCount];

            // 配置 LineRenderer
            ConfigureLineRenderer();
            
            // 初始隐藏
            SetTrailVisible(false);
        }

        private void ConfigureLineRenderer()
        {
            lineRenderer.positionCount = segmentCount;
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCapVertices = 3;
            lineRenderer.numCornerVertices = 3;

            // 设置宽度曲线
            lineRenderer.widthCurve = AnimationCurve.Linear(0f, trailWidth, 1f, trailWidthEnd);

            // 设置颜色渐变
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(trailStartColor, 0f),
                    new GradientColorKey(trailEndColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(trailStartColor.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            lineRenderer.colorGradient = gradient;
        }

        // ═══════════════════════════════════════════════════════════════
        //                          公共方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 开始绘制拖尾
        /// </summary>
        /// <param name="direction">玩家朝向</param>
        /// <param name="startAngle">起始角度</param>
        /// <param name="endAngle">结束角度</param>
        /// <param name="duration">持续时间</param>
        public void StartTrail(Vector2 direction, float startAngle, float endAngle, float duration)
        {
            facingDirection = direction.normalized;
            if (facingDirection == Vector2.zero)
            {
                facingDirection = Vector2.up;
            }

            this.startAngle = startAngle;
            this.endAngle = endAngle;
            this.trailDuration = duration;
            this.swordLength = ConfigManager.GetSwordLength();

            trailProgress = 0f;
            fadeOutTimer = 0f;
            isActive = true;

            SetTrailVisible(true);
            UpdateTrailPositions(0f);
        }

        /// <summary>
        /// 停止拖尾（立即）
        /// </summary>
        public void StopTrail()
        {
            isActive = false;
            SetTrailVisible(false);
        }

        // ═══════════════════════════════════════════════════════════════
        //                          更新循环
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            if (!isActive) return;

            // 更新进度
            trailProgress += Time.deltaTime / trailDuration;

            if (trailProgress >= 1f)
            {
                // 挥砍完成，开始淡出
                fadeOutTimer += Time.deltaTime;
                
                if (fadeOutTimer >= fadeOutDuration)
                {
                    StopTrail();
                    return;
                }

                // 淡出效果
                float fadeAlpha = 1f - (fadeOutTimer / fadeOutDuration);
                UpdateTrailAlpha(fadeAlpha);
            }
            else
            {
                // 正在挥砍，更新拖尾
                UpdateTrailPositions(trailProgress);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //                          拖尾计算
        // ═══════════════════════════════════════════════════════════════

        private void UpdateTrailPositions(float progress)
        {
            // 获取父对象位置（玩家位置）
            Vector3 pivotPos = transform.parent != null ? transform.parent.position : transform.position;

            // 计算基础朝向角度
            float baseAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;

            // 使用缓动使拖尾更自然
            float easedProgress = EaseInOutQuad(progress);

            // 计算当前挥砍角度
            float currentSwingAngle = Mathf.Lerp(startAngle, endAngle, easedProgress);

            // 生成弧线顶点
            for (int i = 0; i < segmentCount; i++)
            {
                float segmentT = (float)i / (segmentCount - 1);
                
                // 每个顶点的角度位置（从起始到当前位置）
                float segmentAngle;
                if (progress > 0.01f)
                {
                    // 顶点沿着已挥过的弧线分布
                    float trailStartAngle = Mathf.Lerp(startAngle, currentSwingAngle, segmentT * 0.3f);
                    segmentAngle = Mathf.Lerp(trailStartAngle, currentSwingAngle, segmentT);
                }
                else
                {
                    segmentAngle = startAngle;
                }

                // 计算世界坐标角度
                float worldAngle = (baseAngle + segmentAngle - 90f) * Mathf.Deg2Rad;

                // 距离从剑根到剑尖渐变
                float distance = swordLength * (0.2f + 0.8f * segmentT);

                // 计算位置
                positions[i] = pivotPos + new Vector3(
                    Mathf.Cos(worldAngle) * distance,
                    Mathf.Sin(worldAngle) * distance,
                    0f
                );
            }

            lineRenderer.SetPositions(positions);
        }

        private void UpdateTrailAlpha(float alpha)
        {
            var gradient = lineRenderer.colorGradient;
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(trailStartColor.a * alpha, 0f),
                new GradientAlphaKey(0f, 1f)
            };
            gradient.SetKeys(gradient.colorKeys, alphaKeys);
            lineRenderer.colorGradient = gradient;
        }

        // ═══════════════════════════════════════════════════════════════
        //                          工具方法
        // ═══════════════════════════════════════════════════════════════

        private void SetTrailVisible(bool visible)
        {
            lineRenderer.enabled = visible;
        }

        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }
    }
}
