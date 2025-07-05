using UnityEngine;
using System;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class BuildingData
    {
        public string id;
        public string buildingId;
        public string name;
        public string buildingName;
        public BuildingType buildingType;
        public BuildingCategory category;
        public int level = 1;
        public int maxLevel = 10;
        public Vector2Int size = new Vector2Int(1, 1);
        public int width;
        public int height;
        public int constructionCost;
        public int buildCost;
        public int upgradeCost;
        public float constructionTime;
        public float buildTime;
        public float upgradeTime;
        public string description;
        public Sprite icon;
        public GameObject prefab;
        
        // 자원 비용
        public int baseWoodCost;
        public int baseStoneCost;
        public int baseManaCost;
        
        // 생산 관련
        public bool canProduce;
        public ResourceType productionType;
        public int productionAmount;
        public float productionTime;
        
        // 건물별 특수 기능
        public bool producesResources;
        public bool storesResources;
        public bool providesBuffs;
        public bool allowsRecruitment;
        
        // 생산 관련
        public float productionRate;
        public string producedResource;
        public int storageCapacity;
        
        // 버프 관련
        public float buffStrength;
        public string buffType;
        
        public BuildingData()
        {
            id = Guid.NewGuid().ToString();
            name = "Unknown Building";
            buildingType = BuildingType.None;
        }
        
        public BuildingData(string buildingId, string buildingName, BuildingType type)
        {
            id = buildingId;
            name = buildingName;
            buildingType = type;
        }
        
        public int GetUpgradeCost()
        {
            return upgradeCost * level;
        }
        
        public float GetUpgradeTime()
        {
            return upgradeTime * level;
        }
        
        public bool CanUpgrade()
        {
            return level < maxLevel;
        }
        
        public bool CanProduce()
        {
            return canProduce && level > 0;
        }
        
        public int ProduceResources()
        {
            if (!CanProduce()) return 0;
            return productionAmount * level;
        }
    }
} 