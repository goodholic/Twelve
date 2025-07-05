using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;
using GuildMaster.Data;

namespace GuildMaster.Equipment
{
    /// <summary>
    /// 장비 기본 클래스
    /// </summary>
    [System.Serializable]
    public class Equipment
    {
        [Header("Basic Info")]
        public string equipmentId;
        public string equipmentName;
        public string description;
        public EquipmentType equipmentType;
        public EquipmentSlot equipmentSlot;
        public Rarity rarity;
        public Sprite icon;

        [Header("Level & Quality")]
        public int level = 1;
        public int maxLevel = 10;
        public int enhanceLevel = 0;
        public int maxEnhanceLevel = 15;
        public float durability = 100f;
        public float maxDurability = 100f;
        
        // 호환성을 위한 속성들
        public int currentLevel => level;
        public int enhancementLevel { get => enhanceLevel; set => enhanceLevel = value; }
        public int maxEnhancementLevel => maxEnhanceLevel;
        public bool canBeEnhanced => enhanceLevel < maxEnhanceLevel;
        public float currentDurability { get => durability; set => durability = value; }
        public string setId = "";  // 세트 아이템 ID

        [Header("Requirements")]
        public int requiredLevel = 1;
        public List<string> requiredClasses = new List<string>();
        public Dictionary<StatType, int> requirements = new Dictionary<StatType, int>();

        [Header("Stats")]
        public Dictionary<StatType, int> baseStats = new Dictionary<StatType, int>();
        public Dictionary<StatType, int> enhancedStats = new Dictionary<StatType, int>();
        public List<EquipmentSkill> skills = new List<EquipmentSkill>();
        public List<SetBonus> setBonuses = new List<SetBonus>();

        [Header("Market Info")]
        public int basePrice = 100;
        public bool isTradeable = true;
        public bool isUnique = false;
        public DateTime acquiredDate;

        public enum EquipmentType
        {
            Weapon,
            Armor,
            Accessory,
            Consumable,
            Material,
            Special
        }

        public enum EquipmentSlot
        {
            MainHand,
            OffHand,
            Head,
            Chest,
            Legs,
            Feet,
            Hands,
            Ring,
            Necklace,
            Earring,
            Belt,
            Cape
        }

        public enum StatType
        {
            Health,
            Mana,
            Attack,
            Defense,
            Speed,
            CriticalRate,
            CriticalDamage,
            Accuracy,
            Evasion,
            MagicPower,
            MagicResistance,
            Luck
        }

        [System.Serializable]
        public class EquipmentSkill
        {
            public string skillId;
            public string skillName;
            public string description;
            public int level;
            public float triggerChance;
            public SkillType skillType;
            public Dictionary<string, object> parameters;

            public enum SkillType
            {
                Passive,
                OnAttack,
                OnHit,
                OnCritical,
                OnUse,
                Aura
            }

            public EquipmentSkill()
            {
                parameters = new Dictionary<string, object>();
            }
        }

        [System.Serializable]
        public class SetBonus
        {
            public string setName;
            public int requiredPieces;
            public Dictionary<StatType, int> bonusStats;
            public List<EquipmentSkill> bonusSkills;

            public SetBonus()
            {
                bonusStats = new Dictionary<StatType, int>();
                bonusSkills = new List<EquipmentSkill>();
            }
        }

        public Equipment()
        {
            equipmentId = Guid.NewGuid().ToString();
            baseStats = new Dictionary<StatType, int>();
            enhancedStats = new Dictionary<StatType, int>();
            requirements = new Dictionary<StatType, int>();
            acquiredDate = DateTime.Now;
        }

        public Equipment(string name, EquipmentType type, EquipmentSlot slot, Rarity rarity)
        {
            equipmentId = Guid.NewGuid().ToString();
            equipmentName = name;
            equipmentType = type;
            equipmentSlot = slot;
            this.rarity = rarity;
            baseStats = new Dictionary<StatType, int>();
            enhancedStats = new Dictionary<StatType, int>();
            requirements = new Dictionary<StatType, int>();
            acquiredDate = DateTime.Now;
        }

        public Dictionary<StatType, int> GetTotalStats()
        {
            var totalStats = new Dictionary<StatType, int>();

            // 기본 스탯 추가
            foreach (var stat in baseStats)
            {
                totalStats[stat.Key] = stat.Value;
            }

            // 강화 스탯 추가
            foreach (var stat in enhancedStats)
            {
                if (totalStats.ContainsKey(stat.Key))
                    totalStats[stat.Key] += stat.Value;
                else
                    totalStats[stat.Key] = stat.Value;
            }

            // 강화 레벨에 따른 추가 보너스
            float enhanceMultiplier = 1f + (enhanceLevel * 0.1f);
            foreach (var key in totalStats.Keys.ToList())
            {
                totalStats[key] = Mathf.RoundToInt(totalStats[key] * enhanceMultiplier);
            }

            return totalStats;
        }

        public bool CanEquip(GuildMaster.Battle.Unit unit)
        {
            // 레벨 요구사항 확인
            if (unit.level < requiredLevel)
                return false;

            // 클래스 요구사항 확인
            if (requiredClasses.Count > 0 && !requiredClasses.Contains(unit.jobClass.ToString()))
                return false;

            // 스탯 요구사항 확인 (Battle.Unit의 스탯 시스템에 맞게 조정)
            foreach (var requirement in requirements)
            {
                float unitStat = GetUnitStat(unit, requirement.Key);
                if (unitStat < requirement.Value)
                    return false;
            }

            return true;
        }

        private float GetUnitStat(GuildMaster.Battle.Unit unit, StatType statType)
        {
            return statType switch
            {
                StatType.Health => unit.maxHP,
                StatType.Mana => unit.maxMP,
                StatType.Attack => unit.attackPower,
                StatType.Defense => unit.defense,
                StatType.Speed => unit.speed,
                StatType.CriticalRate => unit.criticalRate,
                StatType.CriticalDamage => unit.criticalDamage,
                StatType.MagicPower => unit.magicPower,
                StatType.Accuracy => unit.accuracy,
                StatType.Evasion => unit.evasion,
                _ => 0f
            };
        }

        public bool CanEnhance()
        {
            return enhanceLevel < maxEnhanceLevel && durability > 0;
        }

        public bool Enhance()
        {
            if (!CanEnhance())
                return false;

            enhanceLevel++;
            
            // 강화 성공 시 스탯 증가
            RecalculateEnhancedStats();
            
            // 내구도 소모
            durability = Mathf.Max(0, durability - 5f);

            return true;
        }

        void RecalculateEnhancedStats()
        {
            enhancedStats.Clear();
            
            foreach (var baseStat in baseStats)
            {
                int enhanceBonus = Mathf.RoundToInt(baseStat.Value * enhanceLevel * 0.05f);
                enhancedStats[baseStat.Key] = enhanceBonus;
            }
        }

        public void Repair(float amount = 100f)
        {
            durability = Mathf.Min(maxDurability, durability + amount);
        }

        public bool IsDestroyed()
        {
            return durability <= 0;
        }

        public float GetDurabilityPercentage()
        {
            return (durability / maxDurability) * 100f;
        }

        public int GetMarketPrice()
        {
            float priceMultiplier = 1f;

            // 희귀도에 따른 가격 배수
            switch (rarity)
            {
                case Rarity.Common: priceMultiplier = 1f; break;
                case Rarity.Uncommon: priceMultiplier = 2f; break;
                case Rarity.Rare: priceMultiplier = 5f; break;
                case Rarity.Epic: priceMultiplier = 10f; break;
                case Rarity.Legendary: priceMultiplier = 25f; break;
                case Rarity.Mythic: priceMultiplier = 100f; break;
            }

            // 강화 레벨에 따른 가격 증가
            priceMultiplier *= (1f + enhanceLevel * 0.2f);

            // 내구도에 따른 가격 조정
            priceMultiplier *= (durability / maxDurability);

            return Mathf.RoundToInt(basePrice * priceMultiplier);
        }

        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case Rarity.Common: return Color.white;
                case Rarity.Uncommon: return Color.green;
                case Rarity.Rare: return Color.blue;
                case Rarity.Epic: return Color.magenta;
                case Rarity.Legendary: return Color.yellow;
                case Rarity.Mythic: return Color.red;
                default: return Color.white;
            }
        }

        public void AddStat(StatType statType, int value)
        {
            if (baseStats.ContainsKey(statType))
                baseStats[statType] += value;
            else
                baseStats[statType] = value;
        }

        public void AddSkill(EquipmentSkill skill)
        {
            skills.Add(skill);
        }

        public void AddSetBonus(SetBonus setBonus)
        {
            setBonuses.Add(setBonus);
        }

        public List<EquipmentSkill> GetActiveSkills()
        {
            var activeSkills = new List<EquipmentSkill>(skills);
            
            // 세트 보너스 스킬 추가 (조건 만족 시)
            foreach (var setBonus in setBonuses)
            {
                // 세트 조건 확인 로직은 EquipmentManager에서 처리
                activeSkills.AddRange(setBonus.bonusSkills);
            }
            
            return activeSkills;
        }

        public Equipment CreateCopy()
        {
            var copy = new Equipment(equipmentName, equipmentType, equipmentSlot, rarity)
            {
                description = this.description,
                level = this.level,
                enhanceLevel = this.enhanceLevel,
                durability = this.durability,
                maxDurability = this.maxDurability,
                requiredLevel = this.requiredLevel,
                basePrice = this.basePrice,
                isTradeable = this.isTradeable,
                isUnique = this.isUnique
            };

            // 딕셔너리와 리스트 복사
            copy.baseStats = new Dictionary<StatType, int>(this.baseStats);
            copy.enhancedStats = new Dictionary<StatType, int>(this.enhancedStats);
            copy.requirements = new Dictionary<StatType, int>(this.requirements);
            copy.requiredClasses = new List<string>(this.requiredClasses);
            copy.skills = new List<EquipmentSkill>(this.skills);
            copy.setBonuses = new List<SetBonus>(this.setBonuses);

            return copy;
        }
        
        // 추가 호환성 메서드들
        public void Initialize(string name, EquipmentType type, EquipmentSlot slot)
        {
            equipmentName = name;
            equipmentType = type;
            equipmentSlot = slot;
            level = 1;
            enhanceLevel = 0;
            durability = maxDurability;
        }
        
        public void AddStatBonus(StatType statType, int value)
        {
            if (enhancedStats.ContainsKey(statType))
                enhancedStats[statType] += value;
            else
                enhancedStats[statType] = value;
        }
        
        public void RecalculateStats()
        {
            // 기존 RecalculateEnhancedStats 메서드 호출
            RecalculateEnhancedStats();
        }
    }
} 