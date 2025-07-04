using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Core;
using GuildMaster.Data;
using CoreResourceType = GuildMaster.Core.ResourceType;


namespace GuildMaster.Guild
{
    /// <summary>
    /// 건물 데이터를 정의하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "New Building Data", menuName = "GuildMaster/Building Data")]
    public class BuildingDataSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string buildingName;
        public string description;
        public BuildingType buildingType;
        public Sprite icon;
        public GameObject prefab;
        
        [Header("Construction")]
        public float constructionTime = 60f;
        public Dictionary<CoreResourceType, int> constructionCost = new Dictionary<CoreResourceType, int>();
        public Dictionary<CoreResourceType, int> upgradeCost = new Dictionary<CoreResourceType, int>();
        public Dictionary<CoreResourceType, int> maintenanceCost = new Dictionary<CoreResourceType, int>();
        
        [Header("Production")]
        public Dictionary<CoreResourceType, float> baseProductionRates = new Dictionary<CoreResourceType, float>();
        public Dictionary<CoreResourceType, float> baseStorageCapacity = new Dictionary<CoreResourceType, float>();
        public float productionInterval = 60f; // seconds
        
        [Header("Workers")]
        public int maxWorkers = 1;
        public float workerEfficiencyBonus = 0.2f; // 20% per worker
        
        [Header("Requirements")]
        public int requiredGuildLevel = 1;
        public List<BuildingType> requiredBuildings = new List<BuildingType>();
        
        [Header("Dimensions")]
        public Vector2Int size = Vector2Int.one;
        public bool rotatable = true;
        
        [Header("Effects")]
        public List<BuildingEffect> buildingEffects = new List<BuildingEffect>();
        
        [System.Serializable]
        public class BuildingEffect
        {
            public EffectType type;
            public float value;
            public string description;
        }
        
        public enum EffectType
        {
            ProductionBonus,
            StorageBonus,
            EfficiencyBonus,
            CostReduction,
            SpeedBonus,
            QualityBonus
        }

        void OnEnable()
        {
            if (constructionCost == null)
                constructionCost = new Dictionary<CoreResourceType, int>();
            if (upgradeCost == null)
                upgradeCost = new Dictionary<CoreResourceType, int>();
            if (maintenanceCost == null)
                maintenanceCost = new Dictionary<CoreResourceType, int>();
            if (baseProductionRates == null)
                baseProductionRates = new Dictionary<CoreResourceType, float>();
            if (baseStorageCapacity == null)
                baseStorageCapacity = new Dictionary<CoreResourceType, float>();
            if (requiredBuildings == null)
                requiredBuildings = new List<BuildingType>();
            if (buildingEffects == null)
                buildingEffects = new List<BuildingEffect>();
        }

        public bool CanConstruct(int guildLevel, List<BuildingType> existingBuildings)
        {
            // 길드 레벨 체크
            if (guildLevel < requiredGuildLevel)
                return false;
                
            // 필요한 건물들이 존재하는지 체크
            foreach (var requiredBuilding in requiredBuildings)
            {
                if (!existingBuildings.Contains(requiredBuilding))
                    return false;
            }
            
            return true;
        }

        public Dictionary<CoreResourceType, int> GetConstructionCost(int level = 1)
        {
            var cost = new Dictionary<CoreResourceType, int>();
            foreach (var kvp in constructionCost)
            {
                cost[kvp.Key] = Mathf.RoundToInt(kvp.Value * Mathf.Pow(1.5f, level - 1));
            }
            return cost;
        }

        public Dictionary<CoreResourceType, int> GetUpgradeCost(int currentLevel)
        {
            var cost = new Dictionary<CoreResourceType, int>();
            foreach (var kvp in upgradeCost)
            {
                cost[kvp.Key] = Mathf.RoundToInt(kvp.Value * Mathf.Pow(2f, currentLevel));
            }
            return cost;
        }

        public Dictionary<CoreResourceType, float> GetProductionRates(int level = 1)
        {
            var rates = new Dictionary<CoreResourceType, float>();
            foreach (var kvp in baseProductionRates)
            {
                rates[kvp.Key] = kvp.Value * (1f + (level - 1) * 0.3f); // 30% increase per level
            }
            return rates;
        }

        public Dictionary<CoreResourceType, float> GetStorageCapacity(int level = 1)
        {
            var capacity = new Dictionary<CoreResourceType, float>();
            foreach (var kvp in baseStorageCapacity)
            {
                capacity[kvp.Key] = kvp.Value * (1f + (level - 1) * 0.5f); // 50% increase per level
            }
            return capacity;
        }
    }
} 