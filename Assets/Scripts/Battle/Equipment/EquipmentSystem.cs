using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // Unit, JobClass를 위해 추가
using GuildMaster.Core;   // ResourceType을 위해 추가
using Unit = GuildMaster.Battle.UnitStatus;

namespace GuildMaster.Equipment
{
    public enum EquipmentType
    {
        Weapon,
        Armor,
        Accessory
    }
    
    public enum EquipmentRarity
    {
        Common = 1,     // 일반 (흰색)
        Uncommon = 2,   // 고급 (녹색)
        Rare = 3,       // 희귀 (파란색)
        Epic = 4,       // 영웅 (보라색)
        Legendary = 5,  // 전설 (주황색)
        Mythic = 6      // 신화 (빨간색)
    }
    
    public enum EquipmentStat
    {
        Attack,
        Defense,
        MagicPower,
        Health,
        Speed,
        CriticalRate,
        CriticalDamage,
        Accuracy,
        Evasion,
        AllStats
    }
    
    [System.Serializable]
    public class EquipmentItem
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public EquipmentType Type { get; set; }
        public EquipmentRarity Rarity { get; set; }
        public int Level { get; set; }
        public int EnhanceLevel { get; set; }
        
        // Requirements
        public int RequiredLevel { get; set; }
        public JobClass? RequiredClass { get; set; }
        
        // Stats
        public Dictionary<EquipmentStat, float> BaseStats { get; set; }
        public Dictionary<EquipmentStat, float> BonusStats { get; set; }
        
        // Effects and Sets
        public List<EquipmentEffect> Effects { get; set; }
        public string SetId { get; set; }
        
        // State
        public string EquippedByUnitId { get; set; }
        public bool IsLocked { get; set; }
        
        public EquipmentItem(string name, EquipmentType type, EquipmentRarity rarity, int level)
        {
            ItemId = Guid.NewGuid().ToString();
            Name = name;
            Type = type;
            Rarity = rarity;
            Level = level;
            EnhanceLevel = 0;
            RequiredLevel = level;
            RequiredClass = null;
            BaseStats = new Dictionary<EquipmentStat, float>();
            BonusStats = new Dictionary<EquipmentStat, float>();
            Effects = new List<EquipmentEffect>();
            SetId = "";
            EquippedByUnitId = "";
            IsLocked = false;
            
            GenerateBaseStats();
        }
        
        void GenerateBaseStats()
        {
            float rarityMultiplier = GetRarityMultiplier();
            float levelMultiplier = 1f + (Level - 1) * 0.1f;
            
            switch (Type)
            {
                case EquipmentType.Weapon:
                    BaseStats[EquipmentStat.Attack] = 10f * rarityMultiplier * levelMultiplier;
                    BaseStats[EquipmentStat.Speed] = 5f * rarityMultiplier;
                    break;
                case EquipmentType.Armor:
                    BaseStats[EquipmentStat.Defense] = 8f * rarityMultiplier * levelMultiplier;
                    BaseStats[EquipmentStat.Health] = 20f * rarityMultiplier * levelMultiplier;
                    break;
                case EquipmentType.Accessory:
                    BaseStats[EquipmentStat.CriticalRate] = 0.05f * rarityMultiplier;
                    BaseStats[EquipmentStat.CriticalDamage] = 0.2f * rarityMultiplier;
                    break;
            }
        }
        
        float GetRarityMultiplier()
        {
            switch (Rarity)
            {
                case EquipmentRarity.Common: return 1f;
                case EquipmentRarity.Uncommon: return 1.2f;
                case EquipmentRarity.Rare: return 1.5f;
                case EquipmentRarity.Epic: return 2f;
                case EquipmentRarity.Legendary: return 3f;
                case EquipmentRarity.Mythic: return 5f;
                default: return 1f;
            }
        }
        
        public float GetTotalStat(EquipmentStat stat)
        {
            float total = 0f;
            
            if (BaseStats.ContainsKey(stat))
                total += BaseStats[stat];
            
            if (BonusStats.ContainsKey(stat))
                total += BonusStats[stat];
            
            // Enhancement bonus
            if (EnhanceLevel > 0)
            {
                total += total * (EnhanceLevel * 0.1f);
            }
            
            return total;
        }
        
        public bool CanEquip(UnitStatus unit)
        {
            if (RequiredLevel > unit.level) return false;
            if (RequiredClass.HasValue && RequiredClass.Value != unit.jobClass) return false;
            return true;
        }
        
        public int GetEnhanceCost()
        {
            return 100 * (EnhanceLevel + 1) * (int)Rarity;
        }
        
        public float GetEnhanceSuccessRate()
        {
            return Mathf.Max(0.1f, 1f - (EnhanceLevel * 0.1f));
        }
    }
    
    [System.Serializable]
    public class EquipmentEffect
    {
        public string EffectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public EquipmentEffectType Type { get; set; }
        public float Value { get; set; }
        public float Chance { get; set; } // Proc chance for conditional effects
        
        public enum EquipmentEffectType
        {
            OnHit,          // Triggers when attacking
            OnHurt,         // Triggers when taking damage
            OnKill,         // Triggers on killing enemy
            OnBattleStart,  // Triggers at battle start
            Passive         // Always active
        }
    }
    
    [System.Serializable]
    public class EquipmentSet
    {
        public string SetId { get; set; }
        public string Name { get; set; }
        public List<string> RequiredItemIds { get; set; }
        public Dictionary<int, EquipmentSetBonus> SetBonuses { get; set; } // pieces -> bonus
        
        public EquipmentSet(string name)
        {
            SetId = Guid.NewGuid().ToString();
            Name = name;
            RequiredItemIds = new List<string>();
            SetBonuses = new Dictionary<int, EquipmentSetBonus>();
        }
    }
    
    [System.Serializable]
    public class EquipmentSetBonus
    {
        public string Description { get; set; }
        public Dictionary<EquipmentStat, float> StatBonuses { get; set; }
        public List<EquipmentEffect> Effects { get; set; }
        
        public EquipmentSetBonus()
        {
            StatBonuses = new Dictionary<EquipmentStat, float>();
            Effects = new List<EquipmentEffect>();
        }
    }
}