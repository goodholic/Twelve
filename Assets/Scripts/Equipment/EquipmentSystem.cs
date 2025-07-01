using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // Unit, JobClass를 위해 추가
using GuildMaster.Core;   // ResourceType을 위해 추가

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
        
        // Special effects
        public List<EquipmentEffect> Effects { get; set; }
        
        // Set bonus
        public string SetId { get; set; }
        
        // Ownership
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
            
            BaseStats = new Dictionary<EquipmentStat, float>();
            BonusStats = new Dictionary<EquipmentStat, float>();
            Effects = new List<EquipmentEffect>();
            
            GenerateBaseStats();
        }
        
        void GenerateBaseStats()
        {
            float rarityMultiplier = (int)Rarity;
            float levelMultiplier = 1f + (Level * 0.1f);
            
            switch (Type)
            {
                case EquipmentType.Weapon:
                    BaseStats[EquipmentStat.Attack] = 10f * rarityMultiplier * levelMultiplier;
                    BaseStats[EquipmentStat.CriticalRate] = 0.02f * rarityMultiplier;
                    
                    if (Rarity >= EquipmentRarity.Rare)
                    {
                        BaseStats[EquipmentStat.CriticalDamage] = 0.1f * (rarityMultiplier - 2);
                    }
                    break;
                    
                case EquipmentType.Armor:
                    BaseStats[EquipmentStat.Defense] = 8f * rarityMultiplier * levelMultiplier;
                    BaseStats[EquipmentStat.Health] = 50f * rarityMultiplier * levelMultiplier;
                    
                    if (Rarity >= EquipmentRarity.Rare)
                    {
                        BaseStats[EquipmentStat.Evasion] = 0.02f * (rarityMultiplier - 2);
                    }
                    break;
                    
                case EquipmentType.Accessory:
                    // Accessories have varied stats
                    switch (UnityEngine.Random.Range(0, 4))
                    {
                        case 0: // Attack accessory
                            BaseStats[EquipmentStat.Attack] = 5f * rarityMultiplier * levelMultiplier;
                            BaseStats[EquipmentStat.Speed] = 2f * rarityMultiplier;
                            break;
                        case 1: // Defense accessory
                            BaseStats[EquipmentStat.Defense] = 4f * rarityMultiplier * levelMultiplier;
                            BaseStats[EquipmentStat.Health] = 30f * rarityMultiplier * levelMultiplier;
                            break;
                        case 2: // Magic accessory
                            BaseStats[EquipmentStat.MagicPower] = 8f * rarityMultiplier * levelMultiplier;
                            break;
                        case 3: // Balanced accessory
                            BaseStats[EquipmentStat.AllStats] = 0.05f * rarityMultiplier;
                            break;
                    }
                    break;
            }
        }
        
        public float GetTotalStat(EquipmentStat stat)
        {
            float total = 0f;
            
            if (BaseStats.ContainsKey(stat))
                total += BaseStats[stat];
                
            if (BonusStats.ContainsKey(stat))
                total += BonusStats[stat];
                
            // Apply enhancement bonus
            total *= (1f + EnhanceLevel * 0.05f);
            
            return total;
        }
        
        public bool CanEquip(Unit unit)
        {
            if (unit.Level < RequiredLevel) return false;
            
            if (RequiredClass.HasValue && unit.JobClass != RequiredClass.Value)
                return false;
                
            return true;
        }
        
        public int GetEnhanceCost()
        {
            return (EnhanceLevel + 1) * 100 * (int)Rarity;
        }
        
        public float GetEnhanceSuccessRate()
        {
            // Success rate decreases with enhancement level
            float baseRate = 1f;
            
            if (EnhanceLevel < 5) baseRate = 0.9f;
            else if (EnhanceLevel < 10) baseRate = 0.7f;
            else if (EnhanceLevel < 15) baseRate = 0.5f;
            else baseRate = 0.3f;
            
            // Rarity affects success rate
            baseRate -= (int)Rarity * 0.05f;
            
            return Mathf.Max(0.1f, baseRate);
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
    
    public class EquipmentManager : MonoBehaviour
    {
        // Equipment storage
        private Dictionary<string, EquipmentItem> allEquipment;
        private List<EquipmentItem> inventory;
        private Dictionary<string, List<string>> unitEquipment; // UnitId -> Equipment IDs
        
        // Equipment sets
        private Dictionary<string, EquipmentSet> equipmentSets;
        
        // Templates for generation
        private Dictionary<string, EquipmentTemplate> equipmentTemplates;
        
        // Events
        public event Action<EquipmentItem> OnEquipmentObtained;
        public event Action<EquipmentItem, Unit> OnEquipmentEquipped;
        public event Action<EquipmentItem, Unit> OnEquipmentUnequipped;
        public event Action<EquipmentItem> OnEquipmentEnhanced;
        public event Action<EquipmentItem> OnEquipmentDestroyed;
        
        // Configuration
        private const int MAX_INVENTORY_SIZE = 500;
        private const int ENHANCE_MATERIALS_PER_LEVEL = 5;
        
        void Awake()
        {
            allEquipment = new Dictionary<string, EquipmentItem>();
            inventory = new List<EquipmentItem>();
            unitEquipment = new Dictionary<string, List<string>>();
            equipmentSets = new Dictionary<string, EquipmentSet>();
            equipmentTemplates = new Dictionary<string, EquipmentTemplate>();
            
            InitializeTemplates();
            InitializeEquipmentSets();
        }
        
        void InitializeTemplates()
        {
            // Weapon templates
            CreateWeaponTemplate("sword", "검", 1f, 0f);
            CreateWeaponTemplate("axe", "도끼", 1.2f, -0.1f);
            CreateWeaponTemplate("dagger", "단검", 0.8f, 0.2f);
            CreateWeaponTemplate("staff", "지팡이", 0.5f, 0f, true);
            CreateWeaponTemplate("bow", "활", 0.9f, 0.1f);
            
            // Armor templates
            CreateArmorTemplate("plate", "판금 갑옷", 1.2f, 1f);
            CreateArmorTemplate("leather", "가죽 갑옷", 0.8f, 0.8f);
            CreateArmorTemplate("cloth", "천 로브", 0.5f, 0.6f);
            
            // Accessory templates
            CreateAccessoryTemplate("ring", "반지");
            CreateAccessoryTemplate("necklace", "목걸이");
            CreateAccessoryTemplate("bracelet", "팔찌");
        }
        
        void CreateWeaponTemplate(string id, string baseName, float attackMod, float speedMod, bool isMagic = false)
        {
            var template = new EquipmentTemplate
            {
                TemplateId = id,
                BaseName = baseName,
                Type = EquipmentType.Weapon,
                AttackModifier = attackMod,
                SpeedModifier = speedMod,
                IsMagicWeapon = isMagic
            };
            equipmentTemplates[id] = template;
        }
        
        void CreateArmorTemplate(string id, string baseName, float defenseMod, float healthMod)
        {
            var template = new EquipmentTemplate
            {
                TemplateId = id,
                BaseName = baseName,
                Type = EquipmentType.Armor,
                DefenseModifier = defenseMod,
                HealthModifier = healthMod
            };
            equipmentTemplates[id] = template;
        }
        
        void CreateAccessoryTemplate(string id, string baseName)
        {
            var template = new EquipmentTemplate
            {
                TemplateId = id,
                BaseName = baseName,
                Type = EquipmentType.Accessory
            };
            equipmentTemplates[id] = template;
        }
        
        void InitializeEquipmentSets()
        {
            // Warrior Set
            var warriorSet = new EquipmentSet("전사의 분노");
            warriorSet.SetBonuses[2] = new EquipmentSetBonus
            {
                Description = "공격력 +10%",
                StatBonuses = { { EquipmentStat.Attack, 0.1f } }
            };
            warriorSet.SetBonuses[3] = new EquipmentSetBonus
            {
                Description = "공격력 +20%, 치명타 확률 +10%",
                StatBonuses = { 
                    { EquipmentStat.Attack, 0.2f },
                    { EquipmentStat.CriticalRate, 0.1f }
                }
            };
            equipmentSets[warriorSet.SetId] = warriorSet;
            
            // Mage Set
            var mageSet = new EquipmentSet("대마법사의 지혜");
            mageSet.SetBonuses[2] = new EquipmentSetBonus
            {
                Description = "마법 공격력 +15%",
                StatBonuses = { { EquipmentStat.MagicPower, 0.15f } }
            };
            mageSet.SetBonuses[3] = new EquipmentSetBonus
            {
                Description = "마법 공격력 +30%, 모든 스탯 +5%",
                StatBonuses = { 
                    { EquipmentStat.MagicPower, 0.3f },
                    { EquipmentStat.AllStats, 0.05f }
                }
            };
            equipmentSets[mageSet.SetId] = mageSet;
        }
        
        public EquipmentItem GenerateEquipment(int level, EquipmentRarity? forceRarity = null)
        {
            // Select random template
            var templates = equipmentTemplates.Values.ToList();
            var template = templates[UnityEngine.Random.Range(0, templates.Count)];
            
            // Determine rarity
            EquipmentRarity rarity = forceRarity ?? DetermineRarity();
            
            // Generate name
            string name = GenerateEquipmentName(template, rarity);
            
            // Create equipment
            var equipment = new EquipmentItem(name, template.Type, rarity, level);
            
            // Apply template modifiers
            ApplyTemplateModifiers(equipment, template);
            
            // Add special effects for higher rarities
            if (rarity >= EquipmentRarity.Rare)
            {
                AddSpecialEffects(equipment);
            }
            
            // Add to set if epic or higher
            if (rarity >= EquipmentRarity.Epic && UnityEngine.Random.value < 0.3f)
            {
                AssignToSet(equipment);
            }
            
            allEquipment[equipment.ItemId] = equipment;
            return equipment;
        }
        
        EquipmentRarity DetermineRarity()
        {
            float roll = UnityEngine.Random.value;
            
            if (roll < 0.005f) return EquipmentRarity.Mythic;    // 0.5%
            if (roll < 0.02f) return EquipmentRarity.Legendary;  // 1.5%
            if (roll < 0.07f) return EquipmentRarity.Epic;       // 5%
            if (roll < 0.2f) return EquipmentRarity.Rare;        // 13%
            if (roll < 0.5f) return EquipmentRarity.Uncommon;    // 30%
            return EquipmentRarity.Common;                        // 50%
        }
        
        string GenerateEquipmentName(EquipmentTemplate template, EquipmentRarity rarity)
        {
            string[] prefixes = { "강력한", "빛나는", "고대의", "저주받은", "축복받은", "전설의", "신화의" };
            string[] suffixes = { "의 가호", "의 분노", "의 지혜", "의 수호", "의 심판" };
            
            string name = template.BaseName;
            
            if (rarity >= EquipmentRarity.Rare)
            {
                name = prefixes[(int)rarity - 3] + " " + name;
            }
            
            if (rarity >= EquipmentRarity.Epic)
            {
                name += " " + suffixes[UnityEngine.Random.Range(0, suffixes.Length)];
            }
            
            return name;
        }
        
        void ApplyTemplateModifiers(EquipmentItem equipment, EquipmentTemplate template)
        {
            foreach (var stat in equipment.BaseStats.Keys.ToList())
            {
                switch (stat)
                {
                    case EquipmentStat.Attack:
                        equipment.BaseStats[stat] *= template.AttackModifier;
                        break;
                    case EquipmentStat.Defense:
                        equipment.BaseStats[stat] *= template.DefenseModifier;
                        break;
                    case EquipmentStat.Speed:
                        equipment.BaseStats[stat] *= template.SpeedModifier;
                        break;
                    case EquipmentStat.Health:
                        equipment.BaseStats[stat] *= template.HealthModifier;
                        break;
                }
            }
            
            // Convert attack to magic power for magic weapons
            if (template.IsMagicWeapon && equipment.BaseStats.ContainsKey(EquipmentStat.Attack))
            {
                equipment.BaseStats[EquipmentStat.MagicPower] = equipment.BaseStats[EquipmentStat.Attack];
                equipment.BaseStats.Remove(EquipmentStat.Attack);
            }
        }
        
        void AddSpecialEffects(EquipmentItem equipment)
        {
            int effectCount = (int)equipment.Rarity - 2; // 1 for rare, 2 for epic, etc.
            
            for (int i = 0; i < effectCount; i++)
            {
                var effect = GenerateRandomEffect(equipment);
                equipment.Effects.Add(effect);
            }
        }
        
        EquipmentEffect GenerateRandomEffect(EquipmentItem equipment)
        {
            var effect = new EquipmentEffect();
            
            switch (equipment.Type)
            {
                case EquipmentType.Weapon:
                    effect.Type = EquipmentEffect.EquipmentEffectType.OnHit;
                    effect.Name = "추가 피해";
                    effect.Description = "공격 시 추가 피해";
                    effect.Value = equipment.Level * 5f;
                    effect.Chance = 0.2f;
                    break;
                    
                case EquipmentType.Armor:
                    effect.Type = EquipmentEffect.EquipmentEffectType.OnHurt;
                    effect.Name = "피해 감소";
                    effect.Description = "받는 피해 감소";
                    effect.Value = 0.1f;
                    effect.Chance = 1f;
                    break;
                    
                case EquipmentType.Accessory:
                    effect.Type = EquipmentEffect.EquipmentEffectType.Passive;
                    effect.Name = "능력치 증가";
                    effect.Description = "모든 능력치 증가";
                    effect.Value = 0.05f;
                    effect.Chance = 1f;
                    break;
            }
            
            return effect;
        }
        
        void AssignToSet(EquipmentItem equipment)
        {
            var availableSets = equipmentSets.Values.Where(s => 
                s.RequiredItemIds.Count < 3 // Set not full
            ).ToList();
            
            if (availableSets.Count > 0)
            {
                var set = availableSets[UnityEngine.Random.Range(0, availableSets.Count)];
                equipment.SetId = set.SetId;
                set.RequiredItemIds.Add(equipment.ItemId);
            }
        }
        
        public bool EquipItem(string itemId, string unitId)
        {
            if (!allEquipment.ContainsKey(itemId)) return false;
            
            var equipment = allEquipment[itemId];
            var unit = GetUnit(unitId);
            
            if (unit == null || !equipment.CanEquip(unit)) return false;
            
            // Unequip current item of same type
            UnequipItemType(unitId, equipment.Type);
            
            // Equip new item
            equipment.EquippedByUnitId = unitId;
            
            if (!unitEquipment.ContainsKey(unitId))
                unitEquipment[unitId] = new List<string>();
                
            unitEquipment[unitId].Add(itemId);
            inventory.Remove(equipment);
            
            // Apply stats to unit
            ApplyEquipmentStats(unit, equipment, true);
            
            // Check set bonuses
            CheckSetBonuses(unitId);
            
            OnEquipmentEquipped?.Invoke(equipment, unit);
            
            return true;
        }
        
        public bool UnequipItem(string itemId)
        {
            if (!allEquipment.ContainsKey(itemId)) return false;
            
            var equipment = allEquipment[itemId];
            var unitId = equipment.EquippedByUnitId;
            
            if (string.IsNullOrEmpty(unitId)) return false;
            
            var unit = GetUnit(unitId);
            if (unit == null) return false;
            
            // Remove from unit
            equipment.EquippedByUnitId = null;
            unitEquipment[unitId].Remove(itemId);
            
            // Add back to inventory
            if (inventory.Count < MAX_INVENTORY_SIZE)
            {
                inventory.Add(equipment);
            }
            
            // Remove stats from unit
            ApplyEquipmentStats(unit, equipment, false);
            
            // Check set bonuses
            CheckSetBonuses(unitId);
            
            OnEquipmentUnequipped?.Invoke(equipment, unit);
            
            return true;
        }
        
        void UnequipItemType(string unitId, EquipmentType type)
        {
            if (!unitEquipment.ContainsKey(unitId)) return;
            
            var equipped = unitEquipment[unitId]
                .Select(id => allEquipment[id])
                .FirstOrDefault(e => e.Type == type);
                
            if (equipped != null)
            {
                UnequipItem(equipped.ItemId);
            }
        }
        
        void ApplyEquipmentStats(Unit unit, EquipmentItem equipment, bool equip)
        {
            float multiplier = equip ? 1f : -1f;
            
            foreach (var stat in equipment.BaseStats)
            {
                ApplyStatToUnit(unit, stat.Key, equipment.GetTotalStat(stat.Key) * multiplier);
            }
            
            foreach (var stat in equipment.BonusStats)
            {
                ApplyStatToUnit(unit, stat.Key, stat.Value * multiplier);
            }
        }
        
        void ApplyStatToUnit(Unit unit, EquipmentStat stat, float value)
        {
            switch (stat)
            {
                case EquipmentStat.Attack:
                    unit.attackPower += value;
                    break;
                case EquipmentStat.Defense:
                    unit.defense += value;
                    break;
                case EquipmentStat.MagicPower:
                    unit.magicPower += value;
                    break;
                case EquipmentStat.Health:
                    unit.maxHP += value;
                    if (value > 0) unit.currentHP += value;
                    break;
                case EquipmentStat.Speed:
                    unit.speed += value;
                    break;
                case EquipmentStat.CriticalRate:
                    unit.criticalRate += value;
                    break;
                case EquipmentStat.Accuracy:
                    unit.accuracy += value;
                    break;
                case EquipmentStat.AllStats:
                    unit.attackPower *= (1f + value);
                    unit.defense *= (1f + value);
                    unit.magicPower *= (1f + value);
                    unit.maxHP *= (1f + value);
                    unit.speed *= (1f + value);
                    break;
            }
        }
        
        void CheckSetBonuses(string unitId)
        {
            if (!unitEquipment.ContainsKey(unitId)) return;
            
            var unit = GetUnit(unitId);
            if (unit == null) return;
            
            // Count equipped items per set
            var setCounts = new Dictionary<string, int>();
            
            foreach (var itemId in unitEquipment[unitId])
            {
                var equipment = allEquipment[itemId];
                if (!string.IsNullOrEmpty(equipment.SetId))
                {
                    if (!setCounts.ContainsKey(equipment.SetId))
                        setCounts[equipment.SetId] = 0;
                    setCounts[equipment.SetId]++;
                }
            }
            
            // Apply set bonuses
            foreach (var setCount in setCounts)
            {
                if (equipmentSets.ContainsKey(setCount.Key))
                {
                    var set = equipmentSets[setCount.Key];
                    
                    foreach (var bonus in set.SetBonuses)
                    {
                        if (setCount.Value >= bonus.Key)
                        {
                            ApplySetBonus(unit, bonus.Value);
                        }
                    }
                }
            }
        }
        
        void ApplySetBonus(Unit unit, EquipmentSetBonus bonus)
        {
            foreach (var stat in bonus.StatBonuses)
            {
                ApplyStatToUnit(unit, stat.Key, stat.Value);
            }
            
            // TODO: Apply special effects
        }
        
        public bool EnhanceEquipment(string itemId)
        {
            if (!allEquipment.ContainsKey(itemId)) return false;
            
            var equipment = allEquipment[itemId];
            var gameManager = Core.GameManager.Instance;
            
            if (gameManager == null) return false;
            
            // Check costs
            int goldCost = equipment.GetEnhanceCost();
            int materialCount = GetEnhanceMaterialCount(equipment);
            
            if (!gameManager.ResourceManager.CanAfford(goldCost, 0, 0, 0))
                return false;
                
            if (materialCount < ENHANCE_MATERIALS_PER_LEVEL * (equipment.EnhanceLevel + 1))
                return false;
            
            // Pay costs
            gameManager.ResourceManager.SpendResources(goldCost, 0, 0, 0);
            // TODO: Consume enhancement materials
            
            // Check success
            float successRate = equipment.GetEnhanceSuccessRate();
            if (UnityEngine.Random.value <= successRate)
            {
                equipment.EnhanceLevel++;
                
                // If equipped, reapply stats
                if (!string.IsNullOrEmpty(equipment.EquippedByUnitId))
                {
                    var unit = GetUnit(equipment.EquippedByUnitId);
                    if (unit != null)
                    {
                        ApplyEquipmentStats(unit, equipment, false);
                        ApplyEquipmentStats(unit, equipment, true);
                    }
                }
                
                OnEquipmentEnhanced?.Invoke(equipment);
                return true;
            }
            else
            {
                // Enhancement failed
                if (equipment.EnhanceLevel >= 10 && UnityEngine.Random.value < 0.1f)
                {
                    // 10% chance to destroy item on failure at high enhancement
                    DestroyEquipment(itemId);
                }
                return false;
            }
        }
        
        void DestroyEquipment(string itemId)
        {
            if (!allEquipment.ContainsKey(itemId)) return;
            
            var equipment = allEquipment[itemId];
            
            // Unequip if equipped
            if (!string.IsNullOrEmpty(equipment.EquippedByUnitId))
            {
                UnequipItem(itemId);
            }
            
            // Remove from inventory
            inventory.Remove(equipment);
            allEquipment.Remove(itemId);
            
            OnEquipmentDestroyed?.Invoke(equipment);
        }
        
        int GetEnhanceMaterialCount(EquipmentItem equipment)
        {
            // TODO: Implement enhancement material system
            return 100; // Placeholder
        }
        
        Unit GetUnit(string unitId)
        {
            // TODO: Get unit from guild manager
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                return guildManager.GetAdventurers().FirstOrDefault(u => u.Name == unitId); // Using name as ID for now
            }
            return null;
        }
        
        public void AddToInventory(EquipmentItem equipment)
        {
            if (inventory.Count >= MAX_INVENTORY_SIZE)
            {
                Debug.LogWarning("Inventory full!");
                return;
            }
            
            allEquipment[equipment.ItemId] = equipment;
            inventory.Add(equipment);
            OnEquipmentObtained?.Invoke(equipment);
        }
        
        // Getters
        public List<EquipmentItem> GetInventory()
        {
            return new List<EquipmentItem>(inventory);
        }
        
        public List<EquipmentItem> GetUnitEquipment(string unitId)
        {
            if (unitEquipment.ContainsKey(unitId))
            {
                return unitEquipment[unitId]
                    .Select(id => allEquipment[id])
                    .ToList();
            }
            return new List<EquipmentItem>();
        }
        
        public EquipmentItem GetEquipment(string itemId)
        {
            return allEquipment.ContainsKey(itemId) ? allEquipment[itemId] : null;
        }
        
        // Equipment Template
        class EquipmentTemplate
        {
            public string TemplateId { get; set; }
            public string BaseName { get; set; }
            public EquipmentType Type { get; set; }
            public float AttackModifier { get; set; } = 1f;
            public float DefenseModifier { get; set; } = 1f;
            public float SpeedModifier { get; set; } = 1f;
            public float HealthModifier { get; set; } = 1f;
            public bool IsMagicWeapon { get; set; }
        }
    }
}