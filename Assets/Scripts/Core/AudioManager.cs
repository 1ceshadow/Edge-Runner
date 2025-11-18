using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;  // 单例访问

    [Header("BGM 设置")]
    
    [SerializeField] private string mainMenuSceneName = "0Mainmenu";  // 主菜单场景名
    [SerializeField] private float fadeTime = 1f;  // 淡入淡出时间（秒）

    private AudioSource bgmSource;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // 单例模式：确保只有一个实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // 场景切换不销毁

            // 初始化 BGM Source（如果Inspector没赋值）
            if (bgmSource == null)
                bgmSource = GetComponent<AudioSource>();

            // 监听场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);  // 销毁多余实例
            return;
        }
    }

    private void OnDestroy()
    {
        // 清理事件监听
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        Debug.Log($"场景加载: {sceneName}");  // 调试用，可删除

        if (sceneName == mainMenuSceneName)
        {
            PauseBGM();  // 主菜单：淡出暂停 BGM
        }
        else
        {
            PlayBGM();   // 关卡：淡入播放 BGM
        }
    }

    public void PlayBGM()
    {
        if (bgmSource != null)
        {
            // 停止之前的淡入淡出协程
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeIn(fadeTime));

            // 如果没在播放，从暂停位置继续（不会重置进度）
            if (!bgmSource.isPlaying)
                bgmSource.Play();
        }
    }

    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            // 停止之前的淡入淡出协程
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeOut(fadeTime));
        }
    }

    // 淡入效果
    private IEnumerator FadeIn(float fadeTime)
    {
        bgmSource.volume = 0.2f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0.2f, 1f, elapsed / fadeTime);
            yield return null;
        }
        bgmSource.volume = 1f;
    }

    // 淡出效果
    private IEnumerator FadeOut(float fadeTime)
    {
        float startVolume = bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Pause();  // 淡出完成后暂停
    }

    // 从主菜单按钮调用：加载关卡（自动处理BGM）
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    // 可选：手动控制音量（UI滑块用）
    // public void SetBGMVolume(float volume)
    // {
    //     if (bgmSource != null)
    //         bgmSource.volume = volume;
    // }

    // 获取当前BGM状态
    public bool IsBGMPlaying => bgmSource != null && bgmSource.isPlaying;
}