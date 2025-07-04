using UnityEngine;
using System;
using System.Collections.Generic;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class BuildingData
    {
        [Header("기본 정보")]
        public string buildingId;
        public string buildingName;
        public string description;
        
        // 호환성을 위한 속성들
        public string id { get => buildingId; set => buildingId = value; }
        public string name { get => buildingName; set => buildingName = value; }
        public string nameKey = ""; // 로컬라이제이션 키
        public string descriptionKey = ""; // 로컬라이제이션 키
        
        [Header("타입 및 카테고리")]
        public BuildingType buildingType;
        public BuildingCategory category;
        
        [Header("건설 정보")]
        public int buildCost = 1000;
        public int buildTime = 60; // 초 단위
        public int requiredLevel = 1;
        public List<BuildingRequirement> requirements = new List<BuildingRequirement>();
        
        // 세부 비용 호환성 속성들
        public int baseGoldCost { get => buildCost; set => buildCost = value; }
        public int baseWoodCost = 0;
        public int baseStoneCost = 0;
        public int baseManaCost = 0;
        public int baseConstructionTime { get => buildTime; set => buildTime = value; }
        
        [Header("운영 정보")]
        public int maintenanceCost = 10; // 일일 유지비
        public int maxWorkers = 1;
        public int currentWorkers = 0;
        public float efficiency = 1.0f;
        
        [Header("생산 정보")]
        public bool canProduce = false;
        public ResourceType productionType = ResourceType.Gold;
        public int productionAmount = 100;
        public float productionTime = 3600f; // 초 단위 (1시간)
        public int baseProductionGold { get => productionAmount; set => productionAmount = value; } // 호환성
        
        [Header("저장 정보")]
        public bool canStore = false;
        public ResourceType storageType = ResourceType.Gold;
        public int maxStorage = 1000;
        public int currentStorage = 0;
        
        [Header("크기 및 배치")]
        public int width = 1;
        public int height = 1;
        public bool canRotate = true;
        public Vector2Int size => new Vector2Int(width, height);
        
        // 크기 호환성 속성들
        public int sizeX { get => width; set => width = value; }
        public int sizeY { get => height; set => height = value; }
        
        [Header("업그레이드")]
        public bool canUpgrade = true;
        public int maxLevel = 5;
        public int currentLevel = 1;
        public List<UpgradeData> upgrades = new List<UpgradeData>();
        
        [Header("특수 효과")]
        public List<BuildingEffect> effects = new List<BuildingEffect>();
        public bool providesBonus = false;
        public BonusType bonusType = BonusType.None;
        public float bonusAmount = 0f;
        public float bonusRadius = 0f;
        
        [Header("비주얼")]
        public GameObject buildingPrefab;
        public Sprite buildingIcon;
        public Sprite constructionSprite;
        public Material buildingMaterial;
        
        [Header("상태")]
        public BuildingState state = BuildingState.Available;
        public float constructionProgress = 0f;
        public DateTime lastProductionTime;
        public bool isActive = true;
        public bool needsRepair = false;
        public float durability = 100f;
        
        public BuildingData()
        {
            buildingId = System.Guid.NewGuid().ToString();
            buildingName = "Unknown Building";
            description = "";
            lastProductionTime = DateTime.Now;
        }
        
        public BuildingData(string id, string name, BuildingType type, BuildingCategory cat)
        {
            buildingId = id;
            buildingName = name;
            buildingType = type;
            category = cat;
            lastProductionTime = DateTime.Now;
        }
        
        public bool CanBuild(int playerLevel, Dictionary<ResourceType, int> availableResources)
        {
            if (playerLevel < requiredLevel)
                return false;
                
            if (!availableResources.ContainsKey(ResourceType.Gold) || 
                availableResources[ResourceType.Gold] < buildCost)
                return false;
                
            foreach (var requirement in requirements)
            {
                if (!requirement.IsMet(playerLevel, availableResources))
                    return false;
            }
            
            return true;
        }
        
        public bool CanUpgrade(Dictionary<ResourceType, int> availableResources)
        {
            if (!canUpgrade || currentLevel >= maxLevel)
                return false;
                
            var upgradeData = GetUpgradeData(currentLevel + 1);
            if (upgradeData == null)
                return false;
                
            return upgradeData.CanAfford(availableResources);
        }
        
        public UpgradeData GetUpgradeData(int level)
        {
            return upgrades.Find(u => u.targetLevel == level);
        }
        
        public void StartConstruction()
        {
            state = BuildingState.UnderConstruction;
            constructionProgress = 0f;
        }
        
        public void UpdateConstruction(float deltaTime)
        {
            if (state != BuildingState.UnderConstruction)
                return;
                
            constructionProgress += deltaTime;
            
            if (constructionProgress >= buildTime)
            {
                CompleteConstruction();
            }
        }
        
        public void CompleteConstruction()
        {
            state = BuildingState.Built;
            constructionProgress = buildTime;
            isActive = true;
        }
        
        public bool CanProduce()
        {
            if (!canProduce || !isActive || needsRepair)
                return false;
                
            if (state != BuildingState.Built)
                return false;
                
            var timeSinceProduction = DateTime.Now - lastProductionTime;
            return timeSinceProduction.TotalSeconds >= productionTime;
        }
        
        public int ProduceResources()
        {
            if (!CanProduce())
                return 0;
                
            lastProductionTime = DateTime.Now;
            int actualProduction = Mathf.RoundToInt(productionAmount * efficiency);
            
            // 작업자 보너스 적용
            if (currentWorkers > 0)
            {
                float workerBonus = 1f + (currentWorkers * 0.1f);
                actualProduction = Mathf.RoundToInt(actualProduction * workerBonus);
            }
            
            return actualProduction;
        }
        
        public bool CanStore(ResourceType resourceType, int amount)
        {
            if (!canStore || storageType != resourceType)
                return false;
                
            return currentStorage + amount <= maxStorage;
        }
        
        public int Store(ResourceType resourceType, int amount)
        {
            if (!CanStore(resourceType, amount))
                return 0;
                
            int actualStored = Mathf.Min(amount, maxStorage - currentStorage);
            currentStorage += actualStored;
            return actualStored;
        }
        
        public int Withdraw(int amount)
        {
            int actualWithdraw = Mathf.Min(amount, currentStorage);
            currentStorage -= actualWithdraw;
            return actualWithdraw;
        }
        
        public void AddWorker()
        {
            if (currentWorkers < maxWorkers)
                currentWorkers++;
        }
        
        public void RemoveWorker()
        {
            if (currentWorkers > 0)
                currentWorkers--;
        }
        
        public void UpdateDurability(float damage)
        {
            durability = Mathf.Max(0f, durability - damage);
            
            if (durability <= 20f)
                needsRepair = true;
                
            if (durability <= 0f)
                isActive = false;
        }
        
        public void Repair(float repairAmount)
        {
            durability = Mathf.Min(100f, durability + repairAmount);
            
            if (durability > 20f)
            {
                needsRepair = false;
                isActive = true;
            }
        }
        
        public string GetBuildingInfo()
        {
            string info = $"<b>{buildingName}</b>\n";
            info += $"타입: {GetBuildingTypeName()}\n";
            info += $"레벨: {currentLevel}/{maxLevel}\n";
            
            if (!string.IsNullOrEmpty(description))
                info += $"\n{description}\n";
            
            info += $"\n건설 비용: {buildCost} 골드\n";
            info += $"건설 시간: {buildTime}초\n";
            info += $"유지비: {maintenanceCost} 골드/일\n";
            
            if (canProduce)
            {
                info += $"\n생산: {productionAmount} {productionType}/시간\n";
                info += $"작업자: {currentWorkers}/{maxWorkers}\n";
            }
            
            if (canStore)
            {
                info += $"\n저장: {currentStorage}/{maxStorage} {storageType}\n";
            }
            
            info += $"\n크기: {width}x{height}\n";
            info += $"내구도: {durability:F1}%\n";
            
            if (needsRepair)
                info += "\n<color=red>수리 필요</color>\n";
            
            return info;
        }
        
        private string GetBuildingTypeName()
        {
            return buildingType switch
            {
                BuildingType.Residence => "주거시설",
                BuildingType.Production => "생산시설",
                BuildingType.Storage => "저장시설",
                BuildingType.Defense => "방어시설",
                BuildingType.Special => "특수시설",
                BuildingType.Decoration => "장식물",
                _ => "알 수 없음"
            };
        }
    }
    
    [System.Serializable]
    public class BuildingRequirement
    {
        public RequirementType type;
        public string targetId;
        public int amount;
        public int level;
        
        public bool IsMet(int playerLevel, Dictionary<ResourceType, int> resources)
        {
            switch (type)
            {
                case RequirementType.PlayerLevel:
                    return playerLevel >= level;
                case RequirementType.Resource:
                    if (Enum.TryParse<ResourceType>(targetId, out ResourceType resourceType))
                    {
                        return resources.ContainsKey(resourceType) && resources[resourceType] >= amount;
                    }
                    return false;
                case RequirementType.Building:
                    // 다른 건물 존재 확인 (별도 구현 필요)
                    return true;
                default:
                    return true;
            }
        }
    }
    
    [System.Serializable]
    public class UpgradeData
    {
        public int targetLevel;
        public int cost;
        public int time;
        public Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
        public string description;
        
        public bool CanAfford(Dictionary<ResourceType, int> availableResources)
        {
            if (!availableResources.ContainsKey(ResourceType.Gold) || 
                availableResources[ResourceType.Gold] < cost)
                return false;
                
            foreach (var requirement in resources)
            {
                if (!availableResources.ContainsKey(requirement.Key) || 
                    availableResources[requirement.Key] < requirement.Value)
                    return false;
            }
            
            return true;
        }
    }
    
    [System.Serializable]
    public class BuildingEffect
    {
        public EffectType effectType;
        public float effectValue;
        public string targetType;
        public float radius;
        public string description;
    }
    
    public enum BuildingType
{
    None = 0,
    GuildHall = 1,      // 길드 홀 (중심 건물)
    Farm = 2,           // 농장: 식량 생산
    Mine = 3,           // 광산: 광물 및 돌 생산
    Lumbermill = 4,     // 제재소: 목재 생산
    ManaExtractor = 5,  // 마나 추출기: 마나스톤 생산
    Barracks = 6,       // 병영: 유닛 훈련
    Library = 7,        // 도서관: 연구 속도 증가
    Workshop = 8,       // 작업장: 장비 제작
    Storage = 9,        // 창고: 자원 저장 용량 증가
    TrainingGround = 10, // 훈련장: 유닛 경험치 보너스
    Tavern = 11,        // 주점: 모험가 모집
    Market = 12,        // 시장: 자원 거래
    ResearchLab = 13,   // 연구소: 기술 연구
    Temple = 14,        // 신전: 치료 및 부활
    Armory = 15,        // 무기고: 공격력 보너스
    Residence = 16,     // 주거시설
    Production = 17,    // 생산시설
    Defense = 18,       // 방어시설
    Special = 19,       // 특수시설
    Decoration = 20     // 장식물
}

public enum BuildingCategory
{
    Housing,
    Economic,
    Military,
    Utility,
    Decoration,
    Special,
    Core
}

public enum ResourceType
{
    Gold,
    Wood,
    Stone,
    Iron,
    Food,
    Population,
    Energy,
    Magic
}

public enum BuildingState
{
    Available,
    Locked,
    UnderConstruction,
    Built,
    Upgrading,
    Damaged,
    Destroyed
}

public enum BonusType
{
    None,
    ProductionSpeed,
    ResourceGeneration,
    DefenseBonus,
    PopulationBonus,
    ExperienceBonus
}

public enum RequirementType
{
    PlayerLevel,
    Resource,
    Building,
    Research,
    Quest
}

public enum EffectType
{
    ProductionBonus,
    DefenseBonus,
    PopulationBonus,
    ResourceGeneration,
    AreaBonus
}
}