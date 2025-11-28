// ═══════════════════════════════════════════════════════════════════════════
//  ComboSystem - 连击系统
//  管理连击状态、计数、超时重置和不同连击阶段的参数
//  
//  连击设计：
//  - 第1击：右斩（从右上到左下，标准斩击）
//  - 第2击：左斩（从左上到右下，反向斩击）  
//  - 第3击：横扫（360°旋转斩，大范围AOE）
// ═══════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;
using EdgeRunner.Config;

namespace EdgeRunner.Player.Combat
{
    /// <summary>
    /// 连击数据结构
    /// </summary>
    [Serializable]
    public struct ComboData
    {
        /// <summary>连击索引 (0, 1, 2)</summary>
        public int ComboIndex;
        /// <summary>挥砍起始角度</summary>
        public float StartAngle;
        /// <summary>挥砍结束角度</summary>
        public float EndAngle;
        /// <summary>伤害倍率</summary>
        public float DamageMultiplier;
        /// <summary>挥砍速度倍率</summary>
        public float SpeedMultiplier;
        /// <summary>连击名称（用于调试/UI）</summary>
        public string Name;
    }

    /// <summary>
    /// 连击系统
    /// 管理连击状态机和参数
    /// </summary>
    public sealed class ComboSystem
    {
        // ═══════════════════════════════════════════════════════════════
        //                          事件
        // ═══════════════════════════════════════════════════════════════

        /// <summary>连击数变化时触发</summary>
        public event Action<int> OnComboChanged;
        
        /// <summary>连击重置时触发</summary>
        public event Action OnComboReset;

        // ═══════════════════════════════════════════════════════════════
        //                          私有字段
        // ═══════════════════════════════════════════════════════════════

        private readonly ComboData[] comboDataArray;
        private int currentComboIndex;
        private float lastAttackTime;
        private float comboResetTime;
        private int maxComboCount;
        private bool isAttacking;

        // ═══════════════════════════════════════════════════════════════
        //                          公共属性
        // ═══════════════════════════════════════════════════════════════

        /// <summary>当前连击数 (1-based，显示用)</summary>
        public int CurrentCombo => currentComboIndex + 1;

        /// <summary>当前连击索引 (0-based)</summary>
        public int CurrentComboIndex => currentComboIndex;

        /// <summary>是否处于最后一击</summary>
        public bool IsFinisher => currentComboIndex >= maxComboCount - 1;

        /// <summary>是否正在攻击</summary>
        public bool IsAttacking => isAttacking;

        /// <summary>距离上次攻击的时间</summary>
        public float TimeSinceLastAttack => Time.time - lastAttackTime;

        /// <summary>连击是否即将超时</summary>
        public bool IsComboExpiring => TimeSinceLastAttack > comboResetTime * 0.7f;

        // ═══════════════════════════════════════════════════════════════
        //                          构造函数
        // ═══════════════════════════════════════════════════════════════

        public ComboSystem()
        {
            // 从配置加载参数
            comboResetTime = ConfigManager.GetComboResetTime();
            maxComboCount = ConfigManager.GetMaxComboCount();

            // 初始化连击数据
            comboDataArray = BuildComboData();
            currentComboIndex = 0;
            lastAttackTime = -999f;
        }

        // ═══════════════════════════════════════════════════════════════
        //                          连击数据构建
        // ═══════════════════════════════════════════════════════════════

        private ComboData[] BuildComboData()
        {
            var combat = ConfigManager.Combat;

            return new ComboData[]
            {
                // 第1击 - 右斩
                new ComboData
                {
                    ComboIndex = 0,
                    StartAngle = combat.Combo1StartAngle,
                    EndAngle = combat.Combo1EndAngle,
                    DamageMultiplier = 1.0f,
                    SpeedMultiplier = 1.0f,
                    Name = "右斩"
                },
                // 第2击 - 左斩
                new ComboData
                {
                    ComboIndex = 1,
                    StartAngle = combat.Combo2StartAngle,
                    EndAngle = combat.Combo2EndAngle,
                    DamageMultiplier = 1.2f,   // 第2击伤害稍高
                    SpeedMultiplier = 1.1f,    // 第2击稍快
                    Name = "左斩"
                },
                // 第3击 - 横扫 (终结技)
                new ComboData
                {
                    ComboIndex = 2,
                    StartAngle = combat.Combo3StartAngle,
                    EndAngle = combat.Combo3EndAngle,
                    DamageMultiplier = 1.5f,   // 终结技伤害最高
                    SpeedMultiplier = 0.8f,    // 终结技稍慢但范围大
                    Name = "横扫"
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════
        //                          核心方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 更新连击系统（每帧调用）
        /// </summary>
        public void Update()
        {
            // 检查连击是否超时
            if (currentComboIndex > 0 && !isAttacking && TimeSinceLastAttack > comboResetTime)
            {
                ResetCombo();
            }
        }

        /// <summary>
        /// 尝试执行攻击，返回当前连击数据
        /// </summary>
        /// <returns>如果可以攻击，返回连击数据；否则返回 null</returns>
        public ComboData? TryAttack()
        {
            if (isAttacking)
            {
                return null; // 正在攻击中
            }

            // 检查连击是否已超时，需要重置
            if (currentComboIndex > 0 && TimeSinceLastAttack > comboResetTime)
            {
                ResetCombo();
            }

            // 获取当前连击数据
            var comboData = comboDataArray[currentComboIndex];

            // 标记开始攻击
            isAttacking = true;
            lastAttackTime = Time.time;

            // 触发事件
            OnComboChanged?.Invoke(currentComboIndex + 1);

            return comboData;
        }

        /// <summary>
        /// 攻击动作完成时调用
        /// </summary>
        public void OnAttackComplete()
        {
            isAttacking = false;

            // 推进连击计数
            currentComboIndex++;
            if (currentComboIndex >= maxComboCount)
            {
                // 完成完整连击后重置
                ResetCombo();
            }
        }

        /// <summary>
        /// 强制重置连击
        /// </summary>
        public void ResetCombo()
        {
            if (currentComboIndex > 0)
            {
                OnComboReset?.Invoke();
            }
            
            currentComboIndex = 0;
            isAttacking = false;
        }

        /// <summary>
        /// 获取指定索引的连击数据
        /// </summary>
        public ComboData GetComboData(int index)
        {
            index = Mathf.Clamp(index, 0, comboDataArray.Length - 1);
            return comboDataArray[index];
        }

        /// <summary>
        /// 计算当前连击的实际伤害
        /// </summary>
        public int CalculateDamage(int baseDamage)
        {
            if (currentComboIndex < 0 || currentComboIndex >= comboDataArray.Length)
            {
                return baseDamage;
            }

            float multiplier = comboDataArray[currentComboIndex].DamageMultiplier;
            return Mathf.RoundToInt(baseDamage * multiplier);
        }

        /// <summary>
        /// 获取当前连击的挥砍速度倍率
        /// </summary>
        public float GetCurrentSpeedMultiplier()
        {
            if (currentComboIndex < 0 || currentComboIndex >= comboDataArray.Length)
            {
                return 1f;
            }
            return comboDataArray[currentComboIndex].SpeedMultiplier;
        }
    }
}
