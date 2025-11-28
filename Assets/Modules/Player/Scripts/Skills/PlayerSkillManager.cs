// ═══════════════════════════════════════════════════════════════════════════
//  PlayerSkillManager - 玩家技能管理器
//  负责在移动状态下可以随时开启/关闭的技能
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System.Collections;
using EdgeRunner.Events;
using VContainer;

namespace EdgeRunner.Player.Skills
{
    /// <summary>
    /// 玩家技能管理器
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerSkillManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        //                          依赖引用
        // ═══════════════════════════════════════════════════════════════

        private PlayerController controller;

        private bool isTimeSlowed;
        private Coroutine timeSlowVisualCoroutine;

        public bool IsTimeSlowed => isTimeSlowed;

        [Inject]
        public void Construct(IPlayerService playerService)
        {
            controller = playerService?.Controller ?? controller;
        }

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<PlayerController>();
            }

            isTimeSlowed = false;
        }

        /// <summary>
        /// 切换时缓状态
        /// </summary>
        public void ToggleTimeSlow()
        {
            if (!EnsureController())
            {
                return;
            }

            if (isTimeSlowed)
            {
                StopTimeSlow();
            }
            else
            {
                TryStartTimeSlow();
            }
        }

        /// <summary>
        /// 尝试开启时缓
        /// </summary>
        public bool TryStartTimeSlow()
        {
            if (!EnsureController())
            {
                return false;
            }

            if (isTimeSlowed)
            {
                return false;
            }

            if (!controller.CanUseTimeSlow())
            {
                Debug.Log($"能量不足！需要至少 {controller.MinEnergyThreshold} 点，当前: {controller.CurrentEnergy:F1}");
                return false;
            }

            StartTimeSlow();
            return true;
        }

        private void StartTimeSlow()
        {
            if (!EnsureController())
            {
                return;
            }

            isTimeSlowed = true;
            controller.SetTimeSlowState(true);

            // 修改时间缩放
            Time.timeScale = controller.TimeSlowScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // 播放音效
            controller.PlayOneShot(controller.TimeSlowStartClip);

            // 开始视觉效果
            if (timeSlowVisualCoroutine != null)
            {
                StopCoroutine(timeSlowVisualCoroutine);
            }
            timeSlowVisualCoroutine = StartCoroutine(TimeSlowVisualEffect());

            // 发布事件
            EventBus.Publish(new TimeSlowStateChangedEvent
            {
                IsTimeSlowed = true,
                TimeScale = controller.TimeSlowScale
            });

            Debug.Log($"时缓开启！当前能量: {controller.CurrentEnergy:F1}/{controller.MaxEnergy}");
        }

        /// <summary>
        /// 关闭时缓
        /// </summary>
        public void StopTimeSlow()
        {
            if (!EnsureController())
            {
                return;
            }

            if (!isTimeSlowed)
            {
                return;
            }

            StopTimeSlowInternal();
        }

        private void StopTimeSlowInternal()
        {
            if (!EnsureController())
            {
                return;
            }

            isTimeSlowed = false;
            controller.SetTimeSlowState(false);

            // 恢复时间缩放
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            // 播放音效
            controller.PlayOneShot(controller.TimeSlowEndClip);

            // 停止视觉效果
            if (timeSlowVisualCoroutine != null)
            {
                StopCoroutine(timeSlowVisualCoroutine);
                timeSlowVisualCoroutine = null;
            }

            // 发布事件
            EventBus.Publish(new TimeSlowStateChangedEvent
            {
                IsTimeSlowed = false,
                TimeScale = 1f
            });

            Debug.Log($"时缓关闭！剩余能量: {controller.CurrentEnergy:F1}/{controller.MaxEnergy}");
        }

        private IEnumerator TimeSlowVisualEffect()
        {
            if (!EnsureController())
            {
                yield break;
            }

            SpriteRenderer playerSR = controller.GetComponentInChildren<SpriteRenderer>();
            if (playerSR == null || playerSR.sprite == null) yield break;

            GameObject overlay = new GameObject("TimeSlowOverlay");
            overlay.transform.SetParent(controller.transform, false);

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

                // 实时更新 Sprite
                if (playerSR.sprite != sr.sprite)
                    sr.sprite = playerSR.sprite;

                yield return null;
            }

            Destroy(overlay);
        }

        // ═══════════════════════════════════════════════════════════════
        //                          每帧更新
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            if (!EnsureController())
            {
                return;
            }

            // 能量耗尽时自动关闭时缓
            if (isTimeSlowed && controller.CurrentEnergy < 1f)
            {
                StopTimeSlow();
            }

            // 击杀奖励音效
            if (controller.IsRewarded)
            {
                controller.IsRewarded = false;
                controller.PlayOneShot(controller.TimeSlowStartClip);
            }
        }

        private void OnDisable()
        {
            if (!isTimeSlowed)
            {
                return;
            }

            if (EnsureController())
            {
                StopTimeSlowInternal();
            }
            else
            {
                isTimeSlowed = false;
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
        }
        private bool EnsureController()
        {
            if (controller != null)
            {
                return true;
            }

            if (TryGetComponent(out PlayerController cached))
            {
                controller = cached;
                return true;
            }

            Debug.LogError("[PlayerSkillManager] 未找到 PlayerController，技能系统不可用");
            return false;
        }
    }
}