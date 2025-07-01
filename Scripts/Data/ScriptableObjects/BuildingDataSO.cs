using UnityEngine;
using System.Collections.Generic;

namespace GuildMaster.Data
{
    [CreateAssetMenu(fileName = "BuildingData", menuName = "GuildMaster/Data/Building", order = 4)]
    public class BuildingDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string buildingId;
        public string buildingName;
        [TextArea(3, 5)]
        public string description;
        
        [Header("건물 타입")]
        public BuildingType buildingType;
        public BuildingCategory category;
        
        [Header("크기 및 모델")]
        public int sizeX = 1;
        public int sizeY = 1;
        public GameObject buildingPrefab;
        public Sprite buildingIcon;
        
        [Header("건설 정보")]
        public ResourceCost buildCost;
        public float buildTime = 60f; // 초 단위
        public int requiredGuildLevel = 1;
        
        [Header("업그레이드")]
        public int maxLevel = 10;
        public UpgradeData[] upgrades;
        
        [Header("효과")]
        public BuildingEffect[] effects;
        
        [Header("생산")]
        public bool isProducer = false;
        public ResourceProduction production;
        
        [Header("특수 기능")]
        public bool hasSpecialFunction = false;
        public SpecialFunction specialFunction;
        
        /// <summary>
        /// 특정 레벨의 업그레이드 비용
        /// </summary>
        public ResourceCost GetUpgradeCost(int currentLevel)
        {
            if (currentLevel >= maxLevel || currentLevel >= upgrades.Length)
                return null;
                
            return upgrades[currentLevel].upgradeCost;
        }
        
        /// <summary>
        /// 특정 레벨의 건물 효과
        /// </summary>
        public BuildingStats GetStatsAtLevel(int level)
        {
            var stats = new BuildingStats();
            
            foreach (var effect in effects)
            {
                float value = effect.baseValue + (effect.perLevelIncrease * (level - 1));
                
                switch (effect.effectType)
                {
                    case BuildingEffectType.MaxAdventurers:
                        stats.maxAdventurers = (int)value;
                        break;
                    case BuildingEffectType.TrainingSpeed:
                        stats.trainingSpeed = value;
                        break;
                    case BuildingEffectType.ResearchSpeed:
                        stats.researchSpeed = value;
                        break;
                    case BuildingEffectType.StorageCapacity:
                        stats.storageCapacity = (int)value;
                        break;
                    case BuildingEffectType.ReputationGain:
                        stats.reputationGain = value;
                        break;
                }
            }
            
            // 생산 건물인 경우
            if (isProducer && production != null)
            {
                float multiplier = 1f + (0.1f * (level - 1)); // 레벨당 10% 증가
                
                stats.goldProduction = production.goldPerHour * multiplier;
                stats.woodProduction = production.woodPerHour * multiplier;
                stats.stoneProduction = production.stonePerHour * multiplier;
                stats.manaProduction = production.manaPerHour * multiplier;
            }
            
            return stats;
        }
        
        /// <summary>
        /// 철거 시 환급 자원
        /// </summary>
        public ResourceCost GetRefundResources()
        {
            return new ResourceCost
            {
                gold = buildCost.gold / 2,
                wood = buildCost.wood / 2,
                stone = buildCost.stone / 2,
                mana = buildCost.mana / 2
            };
        }

        /// <summary>
        /// CSV 데이터로부터 건물 데이터 초기화
        /// CSV 형식: buildingId,buildingName,buildingType,category,sizeX,sizeY,gold,wood,stone,mana,buildTime,requiredLevel,maxLevel,description
        /// </summary>
        public void InitializeFromCSV(string csvLine)
        {
            string[] values = csvLine.Split(',');
            
            if (values.Length >= 14)
            {
                buildingId = values[0];
                buildingName = values[1];
                description = values[13];
                
                // BuildingType 매핑 (GameData.cs의 BuildingType 사용)
                if (System.Enum.TryParse<BuildingType>(values[2], true, out var parsedType))
                {
                    buildingType = parsedType;
                }
                else
                {
                    // 특수 매핑 처리
                    switch (values[2])
                    {
                        case "TrainingGround":
                            buildingType = BuildingType.Barracks; // 가장 유사한 타입으로 매핑
                            break;
                        case "Laboratory":
                            buildingType = BuildingType.MagesTower; // 가장 유사한 타입으로 매핑
                            break;
                        case "MageTower":
                            buildingType = BuildingType.MagesTower;
                            break;
                        case "ScoutPost":
                            buildingType = BuildingType.Workshop; // 가장 유사한 타입으로 매핑
                            break;
                        case "Armory":
                        case "Shop":
                        case "Dormitory":
                        case "Warehouse":
                            buildingType = BuildingType.Workshop; // 가장 유사한 타입으로 매핑
                            break;
                        default:
                            buildingType = BuildingType.GuildHall;
                            Debug.LogWarning($"Unknown building type: {values[2]}, defaulting to GuildHall");
                            break;
                    }
                }
                
                // BuildingCategory 매핑
                if (System.Enum.TryParse<BuildingCategory>(values[3], true, out var parsedCategory))
                {
                    category = parsedCategory;
                }
                else
                {
                    // 특수 매핑 처리
                    switch (values[3])
                    {
                        case "Economic":
                            category = BuildingCategory.Production;
                            break;
                        default:
                            category = BuildingCategory.Core;
                            Debug.LogWarning($"Unknown building category: {values[3]}");
                            break;
                    }
                }
                
                // 크기
                if (int.TryParse(values[4], out int parsedSizeX))
                    sizeX = parsedSizeX;
                if (int.TryParse(values[5], out int parsedSizeY))
                    sizeY = parsedSizeY;
                
                // 건설 비용
                buildCost = new ResourceCost();
                if (int.TryParse(values[6], out int parsedGold))
                    buildCost.gold = parsedGold;
                if (int.TryParse(values[7], out int parsedWood))
                    buildCost.wood = parsedWood;
                if (int.TryParse(values[8], out int parsedStone))
                    buildCost.stone = parsedStone;
                if (int.TryParse(values[9], out int parsedMana))
                    buildCost.mana = parsedMana;
                
                // 건설 시간
                if (float.TryParse(values[10], out float parsedBuildTime))
                    buildTime = parsedBuildTime;
                
                // 필요 길드 레벨
                if (int.TryParse(values[11], out int parsedRequiredLevel))
                    requiredGuildLevel = parsedRequiredLevel;
                
                // 최대 레벨
                if (int.TryParse(values[12], out int parsedMaxLevel))
                    maxLevel = parsedMaxLevel;
                
                // 기본 생산 설정 (상점이나 워크샵인 경우)
                if (values[2] == "Shop" || buildingType == BuildingType.Workshop)
                {
                    isProducer = true;
                    production = new ResourceProduction
                    {
                        goldPerHour = 10f,
                        woodPerHour = 0f,
                        stonePerHour = 0f,
                        manaPerHour = 0f
                    };
                }
            }
        }
    }
    
    // BuildingType과 BuildingCategory는 GameData.cs에서 이미 정의됨
    
    /// <summary>
    /// 건물 효과 타입
    /// </summary>
    public enum BuildingEffectType
    {
        MaxAdventurers,     // 최대 모험가 수
        TrainingSpeed,      // 훈련 속도
        ResearchSpeed,      // 연구 속도
        StorageCapacity,    // 저장 용량
        ReputationGain,     // 명성 획득량
        AttackBonus,        // 공격력 보너스
        DefenseBonus,       // 방어력 보너스
        ProductionBonus,    // 생산량 보너스
        DiscountRate        // 할인율
    }
    
    /// <summary>
    /// 자원 비용
    /// </summary>
    [System.Serializable]
    public class ResourceCost
    {
        public int gold;
        public int wood;
        public int stone;
        public int mana;
        
        public bool IsEmpty()
        {
            return gold == 0 && wood == 0 && stone == 0 && mana == 0;
        }
    }
    
    /// <summary>
    /// 업그레이드 데이터
    /// </summary>
    [System.Serializable]
    public class UpgradeData
    {
        public int level;
        public ResourceCost upgradeCost;
        public float upgradeTime = 120f;
        public string upgradeDescription;
    }
    
    /// <summary>
    /// 건물 효과
    /// </summary>
    [System.Serializable]
    public class BuildingEffect
    {
        public BuildingEffectType effectType;
        public float baseValue;
        public float perLevelIncrease;
        public string description;
    }
    
    /// <summary>
    /// 자원 생산
    /// </summary>
    [System.Serializable]
    public class ResourceProduction
    {
        public float goldPerHour;
        public float woodPerHour;
        public float stonePerHour;
        public float manaPerHour;
    }
    
    /// <summary>
    /// 특수 기능
    /// </summary>
    [System.Serializable]
    public class SpecialFunction
    {
        public SpecialFunctionType functionType;
        public string functionId;
        public Dictionary<string, float> parameters;
    }
    
    public enum SpecialFunctionType
    {
        UnlockUnit,         // 유닛 해금
        UnlockSkill,        // 스킬 해금
        UnlockResearch,     // 연구 해금
        EnableTrade,        // 거래 활성화
        EnableQuest,        // 퀘스트 활성화
        BuffAura,           // 버프 오라
        DefenseStructure    // 방어 구조물
    }
    
    /// <summary>
    /// 건물 스탯 (GuildStats와 동일한 구조)
    /// </summary>
    [System.Serializable]
    public class BuildingStats
    {
        public int maxAdventurers;
        public float trainingSpeed;
        public float researchSpeed;
        public int storageCapacity;
        public float goldProduction;
        public float woodProduction;
        public float stoneProduction;
        public float manaProduction;
        public float reputationGain;
    }
}