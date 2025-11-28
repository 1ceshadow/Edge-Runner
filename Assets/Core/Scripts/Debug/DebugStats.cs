// ═══════════════════════════════════════════════════════════════════════════
//  DebugStats - 实时调试统计面板
//  显示对象池、能量、战斗等系统的运行时指标
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using VContainer;
using EdgeRunner.Events;
using EdgeRunner.Pooling;

namespace EdgeRunner.MyDebug
{
    /// <summary>
    /// 编辑器/开发环境下的实时调试面板
    /// </summary>
    public class DebugStats : MonoBehaviour
    {
        [Header("显示设置")]
        [SerializeField] private bool showStats = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        [Header("面板位置")]
        [SerializeField] private float panelX = 10f;
        [SerializeField] private float panelY = 10f;
        [SerializeField] private float panelWidth = 280f;

        // 运行时数据
        private float currentEnergy;
        private float maxEnergy;
        private int bulletsFired;
        private int bulletsHit;
        private int enemiesDefeated;
        private int dashCount;
        private int perfectDashCount;
        private float sessionTime;

        // DI 服务
        private IPoolManager poolManager;
        private IBulletManager bulletManager;

        [Inject]
        public void Construct(IPoolManager poolManager, IBulletManager bulletManager)
        {
            this.poolManager = poolManager;
            this.bulletManager = bulletManager;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerEnergyChangedEvent>(OnEnergyChanged);
            EventBus.Subscribe<BulletFiredEvent>(OnBulletFired);
            EventBus.Subscribe<BulletHitEvent>(OnBulletHit);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Subscribe<PlayerDashedEvent>(OnPlayerDashed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerEnergyChangedEvent>(OnEnergyChanged);
            EventBus.Unsubscribe<BulletFiredEvent>(OnBulletFired);
            EventBus.Unsubscribe<BulletHitEvent>(OnBulletHit);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Unsubscribe<PlayerDashedEvent>(OnPlayerDashed);
        }

        private void Update()
        {
            sessionTime += Time.unscaledDeltaTime;

            if (Input.GetKeyDown(toggleKey))
            {
                showStats = !showStats;
            }
        }

        private void OnEnergyChanged(PlayerEnergyChangedEvent evt)
        {
            currentEnergy = evt.CurrentEnergy;
            maxEnergy = evt.MaxEnergy;
        }

        private void OnBulletFired(BulletFiredEvent evt)
        {
            bulletsFired++;
        }

        private void OnBulletHit(BulletHitEvent evt)
        {
            bulletsHit++;
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            if (evt.KilledByPlayer) enemiesDefeated++;
        }

        private void OnPlayerDashed(PlayerDashedEvent evt)
        {
            dashCount++;
            if (evt.IsPerfectDash) perfectDashCount++;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!showStats) return;

            GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, 400f));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"═══ Debug Stats (F1 切换) ═══", GUI.skin.box);

            // 时间
            GUILayout.Label($"Session: {FormatTime(sessionTime)}");
            GUILayout.Label($"Time Scale: {Time.timeScale:F2}");

            GUILayout.Space(5f);

            // 玩家能量
            GUILayout.Label($"═══ 玩家 ═══", GUI.skin.box);
            float energyPercent = maxEnergy > 0 ? currentEnergy / maxEnergy : 0f;
            GUILayout.Label($"能量: {currentEnergy:F1} / {maxEnergy:F0} ({energyPercent * 100f:F0}%)");

            GUILayout.Space(5f);

            // 对象池
            GUILayout.Label($"═══ 对象池 ═══", GUI.skin.box);
            if (poolManager != null && poolManager.BulletPool != null)
            {
                var pool = poolManager.BulletPool;
                GUILayout.Label($"子弹池: {pool.ActiveCount} 活跃 / {pool.AvailableCount} 可用 / {pool.TotalCount} 总计");
            }
            else if (PoolManager.Instance != null && PoolManager.Instance.BulletPool != null)
            {
                var pool = PoolManager.Instance.BulletPool;
                GUILayout.Label($"子弹池: {pool.ActiveCount} 活跃 / {pool.AvailableCount} 可用 / {pool.TotalCount} 总计");
            }
            else
            {
                GUILayout.Label("子弹池: N/A");
            }

            GUILayout.Space(5f);

            // 战斗统计
            GUILayout.Label($"═══ 战斗统计 ═══", GUI.skin.box);
            GUILayout.Label($"子弹发射: {bulletsFired}");
            GUILayout.Label($"子弹命中: {bulletsHit} ({(bulletsFired > 0 ? (float)bulletsHit / bulletsFired * 100f : 0f):F1}%)");
            GUILayout.Label($"击杀敌人: {enemiesDefeated}");
            GUILayout.Label($"冲刺次数: {dashCount}");
            GUILayout.Label($"极限闪避: {perfectDashCount} ({(dashCount > 0 ? (float)perfectDashCount / dashCount * 100f : 0f):F1}%)");

            GUILayout.Space(5f);

            // 性能
            GUILayout.Label($"═══ 性能 ═══", GUI.skin.box);
            GUILayout.Label($"FPS: {1f / Time.unscaledDeltaTime:F0}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif

        private string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{mins:D2}:{secs:D2}";
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public void ResetStats()
        {
            bulletsFired = 0;
            bulletsHit = 0;
            enemiesDefeated = 0;
            dashCount = 0;
            perfectDashCount = 0;
            sessionTime = 0f;
        }
    }
}
