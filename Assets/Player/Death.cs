using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class Death : MonoBehaviour
{
    [Header("死亡区域设置")]
    public string deathZoneLayerName = "DeathZone";

    private bool isDead = false;
    private int deathZoneLayerMask;

    [Header("死亡画面设置")]
    [SerializeField] private Image deathImage;           // 拖入 DeathImage
    [SerializeField] private float fadeDuration = 1.5f;  // 淡入时间
    [SerializeField] private float DeathStay = 1f;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {

        inputActions.Enable();
        // 绑定输入事件
        inputActions.UI.Start.performed += OnRestart;
        inputActions.UI.Menu.performed += OnMenu;
    }

    private void OnDisable()
    {
        inputActions.UI.Start.performed -= OnRestart;
        inputActions.UI.Menu.performed -= OnMenu;
        inputActions.Disable();
    }

    void Start()
    {
        // 初始隐藏死亡界面
        if (deathImage != null)
        {
            deathImage.color = new Color(1, 1, 1, 0);
            deathImage.gameObject.SetActive(false);
        }

        // 获取死亡区域的图层掩码
        deathZoneLayerMask = LayerMask.GetMask(deathZoneLayerName);

        Debug.Log($"死亡检测器已启动，监听图层: {deathZoneLayerName}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.gameObject.layer == LayerMask.NameToLayer(deathZoneLayerName))
        {
            Debug.Log("玩家进入死亡区域！触发死亡");
            isDead = true;
        }
    }

    public void DieFromBullet()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("玩家被子弹击中死亡！");
    }

    private void Update()
    {
        if (!isDead) return;

        // 死亡逻辑
        Die();
    }

    void Die()
    {
        if (!isDead) return;

        // 禁用玩家移动
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
            Debug.Log("已禁用玩家移动");
        }

        // 停止物理运动
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            Debug.Log("已停止玩家移动");
        }

        // 视觉反馈
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.red;
            Debug.Log("玩家变为红色");
        }

        // 暂停游戏时间
        Time.timeScale = 0f;

        // 淡入死亡界面
        if (deathImage != null)
        {
            deathImage.gameObject.SetActive(true);
            StartCoroutine(FadeInDeathScreen());
        }
    }

    private IEnumerator FadeInDeathScreen()
    {
        deathImage.color = new Color(1, 1, 1, 0);
        float elapsed = 0f;

        // 等待 DeathStay 秒再开始淡入
        while (elapsed < DeathStay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;

        // 执行淡入动画
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0, 1, elapsed / fadeDuration);
            deathImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        deathImage.color = new Color(1, 1, 1, 1);
    }

    // 🔹 新输入系统：按下 start 键
   /*
    private void OnRestart(InputAction.CallbackContext context)
    {
        if (!isDead) return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
   */
    private void OnRestart(InputAction.CallbackContext context)
    {
        if (!isDead) return;

        Debug.Log("重新开始游戏");

        // 恢复时间流动
        Time.timeScale = 1f;

        // 清除死亡状态，避免新场景残留
        isDead = false;

        // 停止所有协程（防止旧的FadeIn继续运行）
        StopAllCoroutines();

        // 重新加载当前关卡
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    // 🔹 新输入系统：按下 Menu 键
    private void OnMenu(InputAction.CallbackContext context)
    {
        if (!isDead) return;

        Time.timeScale = 1f;
        // SceneManager.LoadScene("MainMenu"); // 替换为你的主菜单场景名
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (isDead)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
#endif
}
