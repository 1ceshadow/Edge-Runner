// ═══════════════════════════════════════════════════════════════════════════
//  PerformanceMonitor - 性能监控工具
//  追踪 GC、帧时间、内存使用等指标
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.Profiling;

namespace EdgeRunner.MyDebug
{
    /// <summary>
    /// 性能监控组件，提供 GC 和帧时间追踪
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("采样设置")]
        [SerializeField] private float sampleInterval = 1f;
        [SerializeField] private int frameSampleCount = 60;

        [Header("警告阈值")]
        [SerializeField] private float frameTimeWarningMs = 16.67f; // 60 FPS
        [SerializeField] private long memoryWarningMB = 512;

        // 统计数据
        private float[] frameTimes;
        private int frameIndex;
        private float lastSampleTime;
        private int gcCollectionCount;
        private long lastAllocatedMemory;

        // 公开属性
        public float AverageFrameTimeMs { get; private set; }
        public float MaxFrameTimeMs { get; private set; }
        public float MinFrameTimeMs { get; private set; }
        public long AllocatedMemoryMB { get; private set; }
        public int GCCollectionsThisSession { get; private set; }

        private void Awake()
        {
            frameTimes = new float[frameSampleCount];
            gcCollectionCount = System.GC.CollectionCount(0);
        }

        private void Update()
        {
            // 记录帧时间
            frameTimes[frameIndex] = Time.unscaledDeltaTime * 1000f;
            frameIndex = (frameIndex + 1) % frameSampleCount;

            // 定期采样
            if (Time.unscaledTime - lastSampleTime >= sampleInterval)
            {
                lastSampleTime = Time.unscaledTime;
                ComputeStats();
            }
        }

        private void ComputeStats()
        {
            float sum = 0f;
            float max = float.MinValue;
            float min = float.MaxValue;

            for (int i = 0; i < frameSampleCount; i++)
            {
                float t = frameTimes[i];
                sum += t;
                if (t > max) max = t;
                if (t < min) min = t;
            }

            AverageFrameTimeMs = sum / frameSampleCount;
            MaxFrameTimeMs = max;
            MinFrameTimeMs = min;

            // 内存
            AllocatedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);

            // GC 检测
            int currentGC = System.GC.CollectionCount(0);
            if (currentGC > gcCollectionCount)
            {
                GCCollectionsThisSession += currentGC - gcCollectionCount;
                gcCollectionCount = currentGC;
            }

            // 警告输出
            if (MaxFrameTimeMs > frameTimeWarningMs)
            {
                UnityEngine.Debug.LogWarning($"[PerformanceMonitor] 帧时间过长: {MaxFrameTimeMs:F2}ms (阈值: {frameTimeWarningMs}ms)");
            }

            if (AllocatedMemoryMB > memoryWarningMB)
            {
                UnityEngine.Debug.LogWarning($"[PerformanceMonitor] 内存使用过高: {AllocatedMemoryMB}MB (阈值: {memoryWarningMB}MB)");
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            // 简化的性能指标（右上角）
            GUILayout.BeginArea(new Rect(Screen.width - 160, 10, 150, 80));
            GUILayout.BeginVertical("box");
            GUILayout.Label($"Avg: {AverageFrameTimeMs:F1}ms");
            GUILayout.Label($"Max: {MaxFrameTimeMs:F1}ms");
            GUILayout.Label($"Mem: {AllocatedMemoryMB}MB");
            GUILayout.Label($"GC: {GCCollectionsThisSession}");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif
    }
}
