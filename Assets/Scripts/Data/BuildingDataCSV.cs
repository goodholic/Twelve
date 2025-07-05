using UnityEngine;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class BuildingDataCSV
    {
        public string id;
        public string nameKey;
        public string descriptionKey;
        public BuildingType buildingType;
        public int level;
        public int maxLevel;
        public int sizeX;
        public int sizeY;
        public int constructionCost;
        public int upgradeCost;
        public float constructionTime;
        public float upgradeTime;
        public string description;
        public string iconPath;
        public string prefabPath;
        public int baseCostGold;
        public int baseCostWood;
        public int baseCostStone;
        public int baseProductionGold;
        
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
        
        public BuildingDataCSV()
        {
            id = "";
            nameKey = "Unknown Building";
            descriptionKey = "";
            buildingType = BuildingType.None;
            level = 1;
            maxLevel = 10;
            sizeX = 1;
            sizeY = 1;
            constructionCost = 100;
            upgradeCost = 50;
            constructionTime = 10f;
            upgradeTime = 5f;
            description = "";
            iconPath = "";
            prefabPath = "";
            baseCostGold = 0;
            baseCostWood = 0;
            baseCostStone = 0;
            baseProductionGold = 0;
        }
        
        public BuildingData ToBuildingData()
        {
            var buildingData = new BuildingData(id, nameKey, buildingType)
            {
                level = level,
                maxLevel = maxLevel,
                size = new Vector2Int(sizeX, sizeY),
                constructionCost = constructionCost,
                upgradeCost = upgradeCost,
                constructionTime = constructionTime,
                upgradeTime = upgradeTime,
                description = description,
                producesResources = producesResources,
                storesResources = storesResources,
                providesBuffs = providesBuffs,
                allowsRecruitment = allowsRecruitment,
                productionRate = productionRate,
                producedResource = producedResource,
                storageCapacity = storageCapacity,
                buffStrength = buffStrength,
                buffType = buffType
            };
            
            return buildingData;
        }
        

    }
} 