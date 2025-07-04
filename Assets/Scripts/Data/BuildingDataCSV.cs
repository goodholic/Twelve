using UnityEngine;
using System;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class BuildingDataCSV
    {
        public string buildingId;
        public string buildingName;
        public string description;
        public BuildingType buildingType;
        public int buildCost;
        public int buildTime;
        public int requiredLevel;
        public int maxWorkers;
        public bool canProduce;
        public ResourceType productionType;
        public int productionAmount;
        public float productionTime;
        public int maxStorage;
        public int width;
        public int height;
        public string iconPath;
        public string prefabPath;
        
        // 호환성을 위한 속성들
        public string id { get => buildingId; set => buildingId = value; }
        public string nameKey = ""; // 로컬라이제이션 키
        public string descriptionKey = ""; // 로컬라이제이션 키
        public int maxLevel = 5;
        public int sizeX { get => width; set => width = value; }
        public int sizeY { get => height; set => height = value; }
        public int constructionTime { get => buildTime; set => buildTime = value; }
        public int baseCostGold { get => buildCost; set => buildCost = value; }
        public int baseCostWood = 0;
        public int baseCostStone = 0;
        public int baseProductionGold { get => productionAmount; set => productionAmount = value; }
        
        public BuildingDataCSV()
        {
            buildingId = "";
            buildingName = "";
            description = "";
        }
        
        public static BuildingDataCSV FromCSVLine(string csvLine)
        {
            string[] values = csvLine.Split(',');
            var data = new BuildingDataCSV();
            
            if (values.Length >= 16)
            {
                data.buildingId = values[0];
                data.buildingName = values[1];
                data.description = values[2];
                
                if (Enum.TryParse<BuildingType>(values[3], out BuildingType type))
                    data.buildingType = type;
                
                int.TryParse(values[4], out data.buildCost);
                int.TryParse(values[5], out data.buildTime);
                int.TryParse(values[6], out data.requiredLevel);
                int.TryParse(values[7], out data.maxWorkers);
                
                bool.TryParse(values[8], out data.canProduce);
                
                if (Enum.TryParse<ResourceType>(values[9], out ResourceType resType))
                    data.productionType = resType;
                
                int.TryParse(values[10], out data.productionAmount);
                float.TryParse(values[11], out data.productionTime);
                int.TryParse(values[12], out data.maxStorage);
                int.TryParse(values[13], out data.width);
                int.TryParse(values[14], out data.height);
                
                data.iconPath = values[15];
                if (values.Length > 16)
                    data.prefabPath = values[16];
            }
            
            return data;
        }
        
        public BuildingData ToBuildingData()
        {
            var buildingData = new BuildingData
            {
                buildingId = this.buildingId,
                buildingName = this.buildingName,
                description = this.description,
                buildingType = this.buildingType,
                buildCost = this.buildCost,
                buildTime = this.buildTime,
                requiredLevel = this.requiredLevel,
                maxWorkers = this.maxWorkers,
                canProduce = this.canProduce,
                productionType = this.productionType,
                productionAmount = this.productionAmount,
                productionTime = this.productionTime,
                maxStorage = this.maxStorage,
                width = this.width,
                height = this.height
            };
            
            return buildingData;
        }
    }
} 