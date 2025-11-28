using UnityEngine;
using VContainer;

public class CameraController : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target; // 玩家角色
    
    [Header("镜头位置设置")]
    [Range(0f, 1f)]
    public float horizontalPosition = 0.5f; // 水平位置（0=左，1=右，0.5=中间）
    [Range(0f, 1f)]
    public float verticalPosition = 0.42f; // 垂直位置（0=下，1=上，0.333=下三分之一）
    
    [Header("跟随平滑度")]
    public float smoothTime = 0.13f; // 平滑跟随时间（秒）
    public float maxSpeed = Mathf.Infinity; // 最大跟随速度
    
    [Header("镜头边界限制")]
    public bool enableBounds = false; // 是否启用边界限制
    public Vector2 minBounds = new Vector2(-10, -10); // 最小边界
    public Vector2 maxBounds = new Vector2(10, 10);   // 最大边界
    
    private Camera mainCamera;
    private Vector3 currentVelocity;
    private Vector3 targetPosition;
    
    // 缓存相机尺寸
    private float cameraOrthographicSize;
    private float cameraAspect;
    
    // VContainer 依赖注入
    private IPlayerService playerService;
    
    [Inject]
    public void Construct(IPlayerService playerService)
    {
        this.playerService = playerService;
        Debug.Log("✓ CameraController: 玩家服务已注入");
    }
    
    void Start()
    {
        // 获取主相机
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("找不到主相机！");
                return;
            }
        }
        
        // 缓存相机参数
        cameraOrthographicSize = mainCamera.orthographicSize;
        cameraAspect = mainCamera.aspect;
        
        // 如果没有手动设置目标，使用注入的玩家服务
        if (target == null && playerService != null)
        {
            target = playerService.Transform;
            Debug.Log("✓ CameraController: 已通过 VContainer 获取玩家 Transform");
        }
        
        // 立即移动到目标位置（避免初始跳跃）
        if (target != null)
        {
            transform.position = CalculateTargetPosition();
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // 计算目标位置
        targetPosition = CalculateTargetPosition();
        
        // 应用边界限制
        if (enableBounds)
        {
            targetPosition = ApplyBounds(targetPosition);
        }
        
        // 平滑移动到目标位置
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref currentVelocity, 
            smoothTime, 
            maxSpeed, 
            Time.deltaTime
        );
    }
    
    /// <summary>
    /// 计算镜头应该跟随的目标位置
    /// </summary>
    Vector3 CalculateTargetPosition()
    {
        if (target == null) return transform.position;
        
        // 获取目标的世界位置
        Vector3 targetWorldPos = target.position;
        
        // 计算相机视口尺寸（世界单位）
        float cameraHeight = cameraOrthographicSize * 2f;
        float cameraWidth = cameraHeight * cameraAspect;
        
        // 计算目标在屏幕中的偏移
        float horizontalOffset = (horizontalPosition - 0.5f) * cameraWidth;
        float verticalOffset = (verticalPosition - 0.5f) * cameraHeight;
        
        // 计算最终目标位置（保持Z轴不变）
        Vector3 finalPosition = new Vector3(
            targetWorldPos.x - horizontalOffset,
            targetWorldPos.y - verticalOffset,
            transform.position.z // 保持相机原有的Z轴位置
        );
        
        return finalPosition;
    }
    
    /// <summary>
    /// 应用边界限制
    /// </summary>
    Vector3 ApplyBounds(Vector3 position)
    {
        // 计算相机的实际边界
        float cameraHeight = cameraOrthographicSize;
        float cameraWidth = cameraHeight * cameraAspect;
        
        float minX = minBounds.x + cameraWidth;
        float maxX = maxBounds.x - cameraWidth;
        float minY = minBounds.y + cameraHeight;
        float maxY = maxBounds.y - cameraHeight;
        
        // 限制位置
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        
        return position;
    }
    
    /// <summary>
    /// 立即跳转到目标位置（用于闪现后的快速定位）
    /// </summary>
    public void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = CalculateTargetPosition();
            currentVelocity = Vector3.zero; // 重置速度
        }
    }
    
    /// <summary>
    /// 设置新的跟随目标
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            SnapToTarget(); // 立即跳转到新目标
        }
    }
    
    /// <summary>
    /// 设置镜头位置偏移（用于过场动画等）
    /// </summary>
    public void SetScreenPosition(float horizontal, float vertical)
    {
        horizontalPosition = Mathf.Clamp01(horizontal);
        verticalPosition = Mathf.Clamp01(vertical);
    }
    
    /// <summary>
    /// 重置为默认屏幕位置（水平居中，垂直下三分之一）
    /// </summary>
    public void ResetScreenPosition()
    {
        horizontalPosition = 0.5f;
        verticalPosition = 0.333f;
    }
    
    /// <summary>
    /// 调试显示：在Scene视图中显示相机边界
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!enableBounds) return;
        
        Gizmos.color = Color.yellow;
        
        // 绘制边界框
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) * 0.5f,
            (minBounds.y + maxBounds.y) * 0.5f,
            transform.position.z
        );
        
        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            0.1f
        );
        
        Gizmos.DrawWireCube(center, size);
    }
}