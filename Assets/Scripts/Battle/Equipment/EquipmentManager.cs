using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;
using GuildMaster.Data;

namespace GuildMaster.Equipment
{
    /// <summary>
    /// 장비 시스템 전반을 관리하는 매니저 클래스
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        [Header("Equipment Settings")]
        public EquipmentSettings equipmentSettings;
        public List<EquipmentData> allEquipment = new List<EquipmentData>();
        public List<EnhancementMaterial> enhancementMaterials = new List<EnhancementMaterial>();

        [Header("Enhancement")]
        public EnhancementSettings enhancementSettings;
        public List<EnhancementResult> enhancementHistory = new List<EnhancementResult>();

        [Header("Set Bonuses")]
        public List<EquipmentSet> equipmentSets = new List<EquipmentSet>();

        private static EquipmentManager _instance;
        public static EquipmentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EquipmentManager>();
                }
                return _instance;
            }
        }

        [System.Serializable]
        public class EquipmentData
        {
            public string equipmentId;
            public string equipmentName;
            public Equipment.EquipmentSlot slot;
            public Equipment.EquipmentType type;
            public Rarity rarity;
            public Sprite icon;
            public Sprite fullArt;
            public string description;
            
            [Header("Requirements")]
            public int levelRequirement = 1;
            public List<string> classRequirements = new List<string>();
            public List<Equipment.StatType> requiredStats = new List<Equipment.StatType>();
            
            [Header("Base Stats")]
            public Dictionary<Equipment.StatType, int> baseStats = new Dictionary<Equipment.StatType, int>();
            public List<StatBonus> statBonuses = new List<StatBonus>();
            
            [Header("Special Properties")]
            public List<SpecialEffect> specialEffects = new List<SpecialEffect>();
            public string setId = "";
            public bool isUnique = false;
            public bool canBeEnhanced = true;
            public int maxEnhancementLevel = 15;
            
            [Header("Durability")]
            public bool hasDurability = true;
            public int maxDurability = 100;
            public float durabilityLossRate = 1f;

            [System.Serializable]
            public class StatBonus
            {
                public Equipment.StatType statType;
                public int minValue;
                public int maxValue;
                public BonusType bonusType;

                public enum BonusType
                {
                    Fixed,
                    Percentage,
                    PerLevel,
                    Random
                }

                public int GetActualValue(int level = 1)
                {
                    switch (bonusType)
                    {
                        case BonusType.Fixed:
                            return UnityEngine.Random.Range(minValue, maxValue + 1);
                        case BonusType.Percentage:
                            return UnityEngine.Random.Range(minValue, maxValue + 1);
                        case BonusType.PerLevel:
                            return UnityEngine.Random.Range(minValue, maxValue + 1) * level;
                        case BonusType.Random:
                            return UnityEngine.Random.Range(minValue, maxValue + 1);
                        default:
                            return minValue;
                    }
                }
            }

            [System.Serializable]
            public class SpecialEffect
            {
                public string effectId;
                public string effectName;
                public string description;
                public EffectTrigger trigger;
                public EffectType effectType;
                public float value;
                public float duration;
                public float cooldown;
                public float procChance = 1f;

                public enum EffectTrigger
                {
                    OnEquip,
                    OnUnequip,
                    OnAttack,
                    OnDefend,
                    OnCritical,
                    OnDamageReceived,
                    OnHeal,
                    OnSkillUse,
                    Passive,
                    OnKill
                }

                public enum EffectType
                {
                    StatBonus,
                    DamageBonus,
                    HealBonus,
                    StatusEffect,
                    SkillBonus,
                    ExperienceBonus,
                    GoldBonus,
                    DropRateBonus,
                    CriticalRateBonus,
                    DefenseBonus
                }
            }
        }

        [System.Serializable]
        public class EnhancementMaterial
        {
            public string materialId;
            public string materialName;
            public MaterialType type;
            public int enhancementPower;
            public float successRateBonus;
            public Sprite icon;
            public string description;

            public enum MaterialType
            {
                Basic,
                Intermediate,
                Advanced,
                Legendary,
                Special,
                Protection
            }
        }

        [System.Serializable]
        public class EnhancementSettings
        {
            public AnimationCurve successRateCurve = AnimationCurve.Linear(0, 1, 15, 0.1f);
            public AnimationCurve costCurve = AnimationCurve.EaseInOut(0, 100, 15, 10000);
            public float baseSuccessRate = 1f;
            public float enhancementStatMultiplier = 0.1f;
            public bool canBreakOnFailure = true;
            public float breakChance = 0.05f;
            public bool canDowngradeOnFailure = true;
            public float downgradeChance = 0.3f;
        }

        [System.Serializable]
        public class EquipmentSettings
        {
            public bool enableDurabilitySystem = true;
            public float durabilityRepairCostMultiplier = 1f;
            public bool enableSetBonuses = true;
            public bool enableRandomStats = true;
            public float randomStatVariation = 0.2f;
            public int maxSocketCount = 3;
        }

        [System.Serializable]
        public class EquipmentSet
        {
            public string setId;
            public string setName;
            public string description;
            public List<string> equipmentIds = new List<string>();
            public List<SetBonus> setBonuses = new List<SetBonus>();

            [System.Serializable]
            public class SetBonus
            {
                public int requiredPieces;
                public string bonusName;
                public string description;
                public List<StatBonus> statBonuses = new List<StatBonus>();
                public List<EquipmentData.SpecialEffect> specialEffects = new List<EquipmentData.SpecialEffect>();

                [System.Serializable]
                public class StatBonus
                {
                    public Equipment.StatType statType;
                    public int value;
                    public bool isPercentage;
                }
            }
        }

        [System.Serializable]
        public class EnhancementResult
        {
            public string equipmentId;
            public int previousLevel;
            public int newLevel;
            public bool success;
            public bool broken;
            public DateTime timestamp;
            public List<string> materialsUsed = new List<string>();
            public int costPaid;
        }

        // Events
        public static event Action<Equipment, int> OnEquipmentEnhanced;
        public static event Action<Equipment> OnEquipmentBroken;
        public static event Action<GuildMaster.Battle.Unit, Equipment> OnEquipmentEquipped;
        public static event Action<GuildMaster.Battle.Unit, Equipment> OnEquipmentUnequipped;
        public static event Action<GuildMaster.Battle.Unit, List<EquipmentSet.SetBonus>> OnSetBonusActivated;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                InitializeEquipmentSystem();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        void InitializeEquipmentSystem()
        {
            if (equipmentSettings == null)
            {
                equipmentSettings = new EquipmentSettings();
            }

            if (enhancementSettings == null)
            {
                enhancementSettings = new EnhancementSettings();
            }

            LoadEquipmentData();
            ValidateEquipmentSets();
        }

        void LoadEquipmentData()
        {
            // 장비 데이터 로딩 로직
            // 실제로는 JSON, ScriptableObject 등에서 로드
        }

        void ValidateEquipmentSets()
        {
            foreach (var set in equipmentSets)
            {
                foreach (var equipmentId in set.equipmentIds)
                {
                    var equipment = allEquipment.Find(e => e.equipmentId == equipmentId);
                    if (equipment != null)
                    {
                        equipment.setId = set.setId;
                    }
                }
            }
        }

        public Equipment CreateEquipment(string equipmentId, int level = 1)
        {
            var data = allEquipment.Find(e => e.equipmentId == equipmentId);
            if (data == null)
            {
                Debug.LogError($"Equipment data not found: {equipmentId}");
                return null;
            }

            var equipment = new Equipment();
            equipment.Initialize(data.equipmentId, data.type, data.slot);

            // 랜덤 스탯 적용
            if (equipmentSettings.enableRandomStats)
            {
                ApplyRandomStats(equipment, data);
            }

            return equipment;
        }

        void ApplyRandomStats(Equipment equipment, EquipmentData data)
        {
            foreach (var statBonus in data.statBonuses)
            {
                int randomValue = statBonus.GetActualValue(equipment.currentLevel);
                
                // 변동폭 적용
                float variation = UnityEngine.Random.Range(-equipmentSettings.randomStatVariation, 
                                                         equipmentSettings.randomStatVariation);
                randomValue = Mathf.RoundToInt(randomValue * (1f + variation));
                
                equipment.AddStatBonus(statBonus.statType, randomValue);
            }
        }

        public bool EnhanceEquipment(Equipment equipment, List<string> materialIds, int goldCost)
        {
            if (!equipment.canBeEnhanced || equipment.enhancementLevel >= equipment.maxEnhancementLevel)
            {
                return false;
            }

            // 재료 및 비용 체크
            if (!ValidateEnhancementRequirements(equipment, materialIds, goldCost))
            {
                return false;
            }

            // 성공률 계산
            float successRate = CalculateEnhancementSuccessRate(equipment, materialIds);
            bool success = UnityEngine.Random.value <= successRate;

            var result = new EnhancementResult
            {
                equipmentId = equipment.equipmentId,
                previousLevel = equipment.enhancementLevel,
                success = success,
                timestamp = DateTime.Now,
                materialsUsed = new List<string>(materialIds),
                costPaid = goldCost
            };

            if (success)
            {
                // 강화 성공
                equipment.enhancementLevel++;
                result.newLevel = equipment.enhancementLevel;
                
                // 스탯 증가
                ApplyEnhancementBonus(equipment);
                
                OnEquipmentEnhanced?.Invoke(equipment, equipment.enhancementLevel);
            }
            else
            {
                // 강화 실패
                result.newLevel = equipment.enhancementLevel;
                
                // 파괴 확률 체크
                if (enhancementSettings.canBreakOnFailure && 
                    UnityEngine.Random.value <= enhancementSettings.breakChance)
                {
                    result.broken = true;
                    OnEquipmentBroken?.Invoke(equipment);
                    return false;
                }
                
                // 하락 확률 체크
                if (enhancementSettings.canDowngradeOnFailure && 
                    UnityEngine.Random.value <= enhancementSettings.downgradeChance &&
                    equipment.enhancementLevel > 0)
                {
                    equipment.enhancementLevel--;
                    result.newLevel = equipment.enhancementLevel;
                    RecalculateEquipmentStats(equipment);
                }
            }

            enhancementHistory.Add(result);
            ConsumeMaterials(materialIds);
            ConsumeGold(goldCost);

            return success;
        }

        bool ValidateEnhancementRequirements(Equipment equipment, List<string> materialIds, int goldCost)
        {
            // 재료 보유량 체크
            foreach (var materialId in materialIds)
            {
                if (!HasMaterial(materialId))
                {
                    return false;
                }
            }

            // 골드 체크
            if (!HasEnoughGold(goldCost))
            {
                return false;
            }

            return true;
        }

        float CalculateEnhancementSuccessRate(Equipment equipment, List<string> materialIds)
        {
            float baseRate = enhancementSettings.successRateCurve.Evaluate(equipment.enhancementLevel);
            
            // 재료에 따른 보너스 적용
            foreach (var materialId in materialIds)
            {
                var material = enhancementMaterials.Find(m => m.materialId == materialId);
                if (material != null)
                {
                    baseRate += material.successRateBonus;
                }
            }

            return Mathf.Clamp01(baseRate);
        }

        void ApplyEnhancementBonus(Equipment equipment)
        {
            // 강화 레벨에 따른 스탯 보너스 적용
            float multiplier = enhancementSettings.enhancementStatMultiplier;
            
            var currentStats = equipment.GetTotalStats();
            foreach (var stat in currentStats.ToList())
            {
                int bonus = Mathf.RoundToInt(stat.Value * multiplier);
                equipment.AddStatBonus(stat.Key, bonus);
            }
        }

        void RecalculateEquipmentStats(Equipment equipment)
        {
            // 장비 스탯 재계산 로직
            equipment.RecalculateStats();
        }

        bool HasMaterial(string materialId)
        {
            // 재료 보유량 체크 로직
            return true; // 임시로 true 반환
        }

        bool HasEnoughGold(int amount)
        {
            // 골드 보유량 체크 로직
            return true; // 임시로 true 반환
        }

        void ConsumeMaterials(List<string> materialIds)
        {
            // 재료 소모 로직
            foreach (var materialId in materialIds)
            {
                Debug.Log($"Consumed material: {materialId}");
            }
        }

        void ConsumeGold(int amount)
        {
            // 골드 소모 로직
            Debug.Log($"Consumed gold: {amount}");
        }

        public bool EquipItem(GuildMaster.Battle.Unit unit, Equipment equipment)
        {
            if (!equipment.CanEquip(unit))
            {
                return false;
            }

            // 기존 장비 해제
            var currentEquipment = GetEquippedItem(unit, equipment.equipmentSlot);
            if (currentEquipment != null)
            {
                UnequipItem(unit, currentEquipment);
            }

            // 새 장비 착용 (Battle.Unit에 적합한 방식으로 구현)
            // unit.EquipItem(equipment); // Battle.Unit에 이 메서드가 없으면 주석 처리
            OnEquipmentEquipped?.Invoke(unit, equipment);

            // 세트 보너스 체크
            CheckSetBonuses(unit);

            return true;
        }

        public bool UnequipItem(GuildMaster.Battle.Unit unit, Equipment equipment)
        {
            // 장비 해제 로직 (Battle.Unit에 적합하게 수정)
            // if (!unit.UnequipItem(equipment.equipmentSlot)) // Battle.Unit에 이 메서드가 없으면 다른 방식으로 구현
            // {
            //     return false;
            // }

            OnEquipmentUnequipped?.Invoke(unit, equipment);

            // 세트 보너스 재계산
            CheckSetBonuses(unit);

            return true;
        }

        Equipment GetEquippedItem(GuildMaster.Battle.Unit unit, Equipment.EquipmentSlot slot)
        {
            // Battle.Unit의 장비 시스템에 맞게 수정 필요
            // return unit.equipmentSlots.ContainsKey(slot) ? unit.equipmentSlots[slot] : null;
            return null; // 임시로 null 반환
        }

        void CheckSetBonuses(GuildMaster.Battle.Unit unit)
        {
            if (!equipmentSettings.enableSetBonuses) return;

            var activeBonuses = new List<EquipmentSet.SetBonus>();
            var equippedSets = new Dictionary<string, int>();

            // 착용한 세트 아이템 카운트 (Battle.Unit에 맞게 수정)
            // Battle.Unit의 장비 시스템에 따라 구현 필요
            // foreach (var equippedItem in unit.equippedItems)
            // {
            //     if (!string.IsNullOrEmpty(equippedItem.setId))
            //     {
            //         if (!equippedSets.ContainsKey(equippedItem.setId))
            //         {
            //             equippedSets[equippedItem.setId] = 0;
            //         }
            //         equippedSets[equippedItem.setId]++;
            //     }
            // }

            // 세트 보너스 활성화 체크
            foreach (var setCount in equippedSets)
            {
                var equipmentSet = equipmentSets.Find(s => s.setId == setCount.Key);
                if (equipmentSet != null)
                {
                    foreach (var bonus in equipmentSet.setBonuses)
                    {
                        if (setCount.Value >= bonus.requiredPieces)
                        {
                            activeBonuses.Add(bonus);
                        }
                    }
                }
            }

            // 세트 보너스 적용
            if (activeBonuses.Count > 0)
            {
                OnSetBonusActivated?.Invoke(unit, activeBonuses);
                ApplySetBonuses(unit, activeBonuses);
            }
        }

        void ApplySetBonuses(GuildMaster.Battle.Unit unit, List<EquipmentSet.SetBonus> bonuses)
        {
            foreach (var bonus in bonuses)
            {
                // 스탯 보너스 적용
                foreach (var statBonus in bonus.statBonuses)
                {
                    // 세트 보너스 스탯 적용 로직
                    Debug.Log($"Applied set bonus: {statBonus.statType} +{statBonus.value}");
                }

                // 특수 효과 적용
                foreach (var effect in bonus.specialEffects)
                {
                    // 세트 보너스 특수 효과 적용 로직
                    Debug.Log($"Applied set effect: {effect.effectName}");
                }
            }
        }

        public bool RepairEquipment(Equipment equipment, int goldCost)
        {
            if (!equipmentSettings.enableDurabilitySystem || equipment.currentDurability >= equipment.maxDurability)
            {
                return false;
            }

            if (!HasEnoughGold(goldCost))
            {
                return false;
            }

            equipment.currentDurability = equipment.maxDurability;
            ConsumeGold(goldCost);

            Debug.Log($"Repaired {equipment.equipmentName} for {goldCost} gold");
            return true;
        }

        public int CalculateRepairCost(Equipment equipment)
        {
            if (!equipmentSettings.enableDurabilitySystem)
            {
                return 0;
            }

            float durabilityPercentage = 1f - ((float)equipment.currentDurability / equipment.maxDurability);
            int baseCost = GetEquipmentValue(equipment);
            
            return Mathf.RoundToInt(baseCost * durabilityPercentage * equipmentSettings.durabilityRepairCostMultiplier);
        }

        public int CalculateEnhancementCost(Equipment equipment)
        {
            return Mathf.RoundToInt(enhancementSettings.costCurve.Evaluate(equipment.enhancementLevel));
        }

        int GetEquipmentValue(Equipment equipment)
        {
            // 장비 가치 계산 로직
            int baseValue = 100;
            
            // 레어도에 따른 배율
            switch (equipment.rarity)
            {
                case Rarity.Common: baseValue *= 1; break;
                case Rarity.Uncommon: baseValue *= 2; break;
                case Rarity.Rare: baseValue *= 5; break;
                case Rarity.Epic: baseValue *= 10; break;
                case Rarity.Legendary: baseValue *= 25; break;
                case Rarity.Mythic: baseValue *= 50; break;
            }

            // 강화 레벨에 따른 배율
            baseValue *= (1 + equipment.enhancementLevel);

            return baseValue;
        }

        public List<Equipment> GetEquipmentBySlot(Equipment.EquipmentSlot slot)
        {
            return allEquipment.Where(e => e.slot == slot)
                              .Select(data => CreateEquipment(data.equipmentId))
                              .Where(e => e != null)
                              .ToList();
        }

        public List<Equipment> GetEquipmentByRarity(Rarity rarity)
        {
            return allEquipment.Where(e => e.rarity == rarity)
                              .Select(data => CreateEquipment(data.equipmentId))
                              .Where(e => e != null)
                              .ToList();
        }

        public List<Equipment> GetSetEquipment(string setId)
        {
            return allEquipment.Where(e => e.setId == setId)
                              .Select(data => CreateEquipment(data.equipmentId))
                              .Where(e => e != null)
                              .ToList();
        }

        public EquipmentSet GetEquipmentSet(string setId)
        {
            return equipmentSets.Find(s => s.setId == setId);
        }

        public void SaveEquipmentData()
        {
            // 장비 데이터 저장 로직
        }

        void OnDestroy()
        {
            SaveEquipmentData();
        }
    }
} 