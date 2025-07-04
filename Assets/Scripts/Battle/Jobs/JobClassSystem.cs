using UnityEngine;
using System;
using System.Collections.Generic;

namespace GuildMaster.Battle
{
    /// <summary>
    /// README 기준 7개 직업 시스템 - 상용 게임 수준 구현
    /// </summary>
    public static class JobClassSystem
    {
        // 직업별 기본 능력치 배율
        public static readonly Dictionary<JobClass, JobStatMultipliers> BaseStatMultipliers = new Dictionary<JobClass, JobStatMultipliers>
        {
            [JobClass.Warrior] = new JobStatMultipliers
            {
                hpMultiplier = 1.4f,        // 높은 체력
                attackMultiplier = 1.2f,    // 높은 물리 공격력
                defenseMultiplier = 1.1f,   // 보통 방어력
                magicPowerMultiplier = 0.5f, // 낮은 마법력
                speedMultiplier = 0.9f,     // 느린 속도
                criticalRateBonus = 0.05f,  // 기본 크리티컬
                description = "전방에서 적을 막아서는 강인한 탱커"
            },
            
            [JobClass.Knight] = new JobStatMultipliers
            {
                hpMultiplier = 1.5f,        // 최고 체력
                attackMultiplier = 1.0f,    // 보통 공격력
                defenseMultiplier = 1.6f,   // 최고 방어력
                magicPowerMultiplier = 0.3f, // 매우 낮은 마법력
                speedMultiplier = 0.7f,     // 매우 느린 속도
                criticalRateBonus = 0.02f,  // 낮은 크리티컬
                description = "아군을 보호하는 최고의 방패"
            },
            
            [JobClass.Mage] = new JobStatMultipliers
            {
                hpMultiplier = 0.7f,        // 낮은 체력
                attackMultiplier = 0.5f,    // 낮은 물리 공격력
                defenseMultiplier = 0.6f,   // 낮은 방어력
                magicPowerMultiplier = 1.8f, // 최고 마법력
                speedMultiplier = 1.1f,     // 빠른 속도
                criticalRateBonus = 0.08f,  // 높은 마법 크리티컬
                description = "강력한 마법으로 적을 소멸시키는 마법사"
            }
        };
        
        /// <summary>
        /// 직업별 스탯 계산
        /// </summary>
        public static void ApplyJobStats(Unit unit)
        {
            if (!BaseStatMultipliers.ContainsKey(unit.jobClass)) return;
            
            var multipliers = BaseStatMultipliers[unit.jobClass];
            var baseStats = CalculateBaseStats(unit.level, unit.rarity);
            
            unit.maxHP = baseStats.Health * multipliers.hpMultiplier;
            unit.attackPower = baseStats.Attack * multipliers.attackMultiplier;
            unit.defense = baseStats.Defense * multipliers.defenseMultiplier;
            unit.magicPower = baseStats.MagicPower * multipliers.magicPowerMultiplier;
            unit.speed = baseStats.Speed * multipliers.speedMultiplier;
            unit.criticalRate = Mathf.Clamp01(0.05f + multipliers.criticalRateBonus);
            
            // 체력 최대치로 회복
            unit.currentHP = unit.maxHP;
        }
        
        /// <summary>
        /// 레벨과 등급에 따른 기본 스탯 계산
        /// </summary>
        public static BaseStats CalculateBaseStats(int level, Rarity rarity)
        {
            float rarityMultiplier = GetRarityMultiplier(rarity);
            
            return new BaseStats(
                100f + level * 10f * rarityMultiplier,
                20f + level * 2f * rarityMultiplier,
                15f + level * 1.5f * rarityMultiplier,
                10f + level * 1f * rarityMultiplier,
                0.05f + level * 0.001f * rarityMultiplier,
                1.5f + level * 0.01f * rarityMultiplier
            );
        }
        
        /// <summary>
        /// 등급별 능력치 배율
        /// </summary>
        public static float GetRarityMultiplier(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => 1.0f,
                Rarity.Uncommon => 1.2f,
                Rarity.Rare => 1.5f,
                Rarity.Epic => 2.0f,
                Rarity.Legendary => 3.0f,
                _ => 1.0f
            };
        }
    }
    
    // 데이터 구조체들
    [System.Serializable]
    public class JobStatMultipliers
    {
        public float hpMultiplier = 1f;
        public float attackMultiplier = 1f;
        public float defenseMultiplier = 1f;
        public float magicPowerMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float criticalRateBonus = 0f;
        public string description = "";
    }
    
    public struct BaseStats
    {
        public float Health;
        public float Attack;
        public float Defense;
        public float MagicPower;
        public float Speed;
        public float CriticalRate;
        public float CriticalDamage;
        
        public BaseStats(float health, float attack, float defense, float magicPower, float speed, float critRate = 0.05f, float critDamage = 1.5f)
        {
            Health = health;
            Attack = attack;
            Defense = defense;
            MagicPower = magicPower;
            Speed = speed;
            CriticalRate = critRate;
            CriticalDamage = critDamage;
        }
        
        public BaseStats Multiply(float multiplier)
        {
            return new BaseStats(
                Health * multiplier,
                Attack * multiplier,
                Defense * multiplier,
                MagicPower * multiplier,
                Speed * multiplier,
                CriticalRate,
                CriticalDamage
            );
        }
    }
}
