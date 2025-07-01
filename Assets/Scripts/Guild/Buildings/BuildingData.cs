using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // JobClass를 위해 추가

namespace GuildMaster.Guild
{
    [System.Serializable]
    public class BuildingData
    {
        public Core.GuildManager.BuildingType Type { get; set; }
        public string Name { get; set; }
        public string name { get; set; } // UI에서 사용하는 필드
        public string Description { get; set; }
        public int Level { get; set; }
        public int maxLevel { get; set; } = 10; // 최대 레벨
        public int baseGoldCost { get; set; } = 100; // 기본 골드 비용
        public Core.GuildManager.BuildingType buildingType { get; set; } // 타입 별칭
        public Vector2Int Position { get; set; }
        public bool IsConstructing { get; set; }
        public float ConstructionTimeRemaining { get; set; }
        
        // Building Stats
        public float BaseEffect { get; set; }
        public float EffectPerLevel { get; set; }
        public float ProductionRate { get; set; }
        public float LastProductionTime { get; set; }
        public bool isProducer { get; set; } // 생산 건물 여부
        
        // Construction Requirements
        public BuildingRequirements BaseRequirements { get; set; }
        public float CostMultiplierPerLevel { get; set; } = 1.5f;
        public float TimeMultiplierPerLevel { get; set; } = 1.2f;
        
        public BuildingData()
        {
            name = Name; // name 필드를 Name으로 동기화
            buildingType = Type; // buildingType을 Type으로 동기화
        }
        
        public BuildingData(Core.GuildManager.BuildingType type)
        {
            Type = type;
            buildingType = type;
            Level = 1;
            InitializeBuildingData();
            name = Name; // 초기화 후 동기화
        }
        
        void InitializeBuildingData()
        {
            switch (Type)
            {
                case Core.GuildManager.BuildingType.GuildHall:
                    Name = "길드 홀";
                    Description = "길드의 중심. 레벨업으로 다른 시설 해금";
                    BaseEffect = 1;
                    EffectPerLevel = 1;
                    BaseRequirements = new BuildingRequirements { Gold = 0 };
                    break;
                    
                case Core.GuildManager.BuildingType.TrainingGround:
                    Name = "훈련소";
                    Description = "모험가들의 경험치 획득량 증가";
                    BaseEffect = 5; // 5% exp bonus
                    EffectPerLevel = 2; // +2% per level
                    BaseRequirements = new BuildingRequirements { Gold = 100, Wood = 50 };
                    break;
                    
                case Core.GuildManager.BuildingType.ResearchLab:
                    Name = "연구소";
                    Description = "새로운 직업 클래스와 스킬 연구";
                    BaseEffect = 1; // 1 research point per hour
                    EffectPerLevel = 0.5f;
                    ProductionRate = 1;
                    isProducer = true;
                    BaseRequirements = new BuildingRequirements { Gold = 200, Stone = 100, ManaStone = 50 };
                    break;
                    
                case Core.GuildManager.BuildingType.Dormitory:
                    Name = "숙소";
                    Description = "모험가 최대 수용 인원 확장";
                    BaseEffect = 9; // 9 adventurers per dormitory
                    EffectPerLevel = 0; // Doesn't increase with level
                    BaseRequirements = new BuildingRequirements { Gold = 150, Wood = 100, Stone = 50 };
                    break;
                    
                case Core.GuildManager.BuildingType.Armory:
                    Name = "무기고";
                    Description = "장비 제작 및 강화 시설";
                    BaseEffect = 10; // 10% equipment bonus
                    EffectPerLevel = 5;
                    BaseRequirements = new BuildingRequirements { Gold = 250, Stone = 150 };
                    break;
                    
                case Core.GuildManager.BuildingType.MageTower:
                    Name = "마법탑";
                    Description = "마법사 계열 스킬 강화";
                    BaseEffect = 15; // 15% magic power bonus
                    EffectPerLevel = 5;
                    BaseRequirements = new BuildingRequirements { Gold = 300, Stone = 100, ManaStone = 100 };
                    break;
                    
                case Core.GuildManager.BuildingType.Temple:
                    Name = "신전";
                    Description = "성직자 계열 치유 효과 증가";
                    BaseEffect = 20; // 20% healing bonus
                    EffectPerLevel = 5;
                    BaseRequirements = new BuildingRequirements { Gold = 200, Stone = 150, ManaStone = 50 };
                    break;
                    
                case Core.GuildManager.BuildingType.Shop:
                    Name = "상점";
                    Description = "자동 수익 창출 및 아이템 판매";
                    BaseEffect = 10; // 10 gold per minute
                    EffectPerLevel = 5;
                    ProductionRate = 10;
                    isProducer = true;
                    BaseRequirements = new BuildingRequirements { Gold = 100, Wood = 100 };
                    break;
                    
                case Core.GuildManager.BuildingType.Warehouse:
                    Name = "창고";
                    Description = "자원 및 아이템 보관 용량 확장";
                    BaseEffect = 500; // +500 storage per resource
                    EffectPerLevel = 250;
                    BaseRequirements = new BuildingRequirements { Gold = 150, Wood = 150, Stone = 100 };
                    break;
                    
                case Core.GuildManager.BuildingType.ScoutPost:
                    Name = "정찰대";
                    Description = "새로운 지역 및 던전 발견 속도 증가";
                    BaseEffect = 10; // 10% exploration speed
                    EffectPerLevel = 5;
                    BaseRequirements = new BuildingRequirements { Gold = 100, Wood = 80 };
                    break;
            }
        }
        
        public float GetCurrentEffect()
        {
            return BaseEffect + (EffectPerLevel * (Level - 1));
        }
        
        public float GetProductionAmount()
        {
            return ProductionRate * Level;
        }
        
        public BuildingRequirements GetUpgradeRequirements()
        {
            BuildingRequirements requirements = new BuildingRequirements();
            float multiplier = Mathf.Pow(CostMultiplierPerLevel, Level - 1);
            
            requirements.Gold = Mathf.RoundToInt(BaseRequirements.Gold * multiplier);
            requirements.Wood = Mathf.RoundToInt(BaseRequirements.Wood * multiplier);
            requirements.Stone = Mathf.RoundToInt(BaseRequirements.Stone * multiplier);
            requirements.ManaStone = Mathf.RoundToInt(BaseRequirements.ManaStone * multiplier);
            requirements.GuildLevel = GetRequiredGuildLevel();
            
            return requirements;
        }
        
        public float GetConstructionTime()
        {
            float baseTime = GetBaseConstructionTime();
            return baseTime * Mathf.Pow(TimeMultiplierPerLevel, Level - 1);
        }
        
        float GetBaseConstructionTime()
        {
            switch (Type)
            {
                case Core.GuildManager.BuildingType.GuildHall: return 0f;
                case Core.GuildManager.BuildingType.TrainingGround: return 60f;
                case Core.GuildManager.BuildingType.ResearchLab: return 120f;
                case Core.GuildManager.BuildingType.Dormitory: return 90f;
                case Core.GuildManager.BuildingType.Armory: return 150f;
                case Core.GuildManager.BuildingType.MageTower: return 180f;
                case Core.GuildManager.BuildingType.Temple: return 150f;
                case Core.GuildManager.BuildingType.Shop: return 60f;
                case Core.GuildManager.BuildingType.Warehouse: return 120f;
                case Core.GuildManager.BuildingType.ScoutPost: return 45f;
                default: return 60f;
            }
        }
        
        int GetRequiredGuildLevel()
        {
            switch (Type)
            {
                case Core.GuildManager.BuildingType.GuildHall: return 1;
                case Core.GuildManager.BuildingType.TrainingGround: return 1;
                case Core.GuildManager.BuildingType.Shop: return 1;
                case Core.GuildManager.BuildingType.Dormitory: return 2;
                case Core.GuildManager.BuildingType.Warehouse: return 2;
                case Core.GuildManager.BuildingType.ScoutPost: return 3;
                case Core.GuildManager.BuildingType.ResearchLab: return 4;
                case Core.GuildManager.BuildingType.Armory: return 5;
                case Core.GuildManager.BuildingType.Temple: return 6;
                case Core.GuildManager.BuildingType.MageTower: return 7;
                default: return 1;
            }
        }
        
        public bool CanBeBuilt(int currentGuildLevel)
        {
            return currentGuildLevel >= GetRequiredGuildLevel();
        }
        
        public int GetMaxLevel()
        {
            switch (Type)
            {
                case Core.GuildManager.BuildingType.GuildHall: return 10;
                default: return 5;
            }
        }

        public Dictionary<string, int> GetRefundResources(int level = 1)
        {
            var refund = new Dictionary<string, int>();
            refund["gold"] = baseGoldCost * level / 2;
            refund["wood"] = BaseRequirements?.Wood ?? 0 * level / 2;
            refund["stone"] = BaseRequirements?.Stone ?? 0 * level / 2;
            refund["mana"] = BaseRequirements?.ManaStone ?? 0 * level / 2;
            return refund;
        }
    }
    
    [System.Serializable]
    public class BuildingRequirements
    {
        public int Gold { get; set; }
        public int Wood { get; set; }
        public int Stone { get; set; }
        public int ManaStone { get; set; }
        public int GuildLevel { get; set; }
        
        public bool HasResources(Core.ResourceManager resourceManager)
        {
            if (resourceManager == null) return false;
            
            return resourceManager.CanAfford(Gold, Wood, Stone, ManaStone);
        }
        
        public void SpendResources(Core.ResourceManager resourceManager)
        {
            if (resourceManager == null) return;
            
            resourceManager.SpendResources(Gold, Wood, Stone, ManaStone);
        }
    }
    
    public static class BuildingEffects
    {
        public static void ApplyBuildingEffects(BuildingData building, Core.GameManager gameManager)
        {
            if (gameManager == null) return;
            
            float effect = building.GetCurrentEffect();
            
            switch (building.Type)
            {
                case Core.GuildManager.BuildingType.Warehouse:
                    // Increase storage limits
                    gameManager.ResourceManager.IncreaseGoldLimit((int)effect);
                    gameManager.ResourceManager.IncreaseWoodLimit((int)effect);
                    gameManager.ResourceManager.IncreaseStoneLimit((int)effect);
                    gameManager.ResourceManager.IncreaseManaStoneLimit((int)(effect * 0.5f));
                    break;
                    
                case Core.GuildManager.BuildingType.Shop:
                    // Set gold production rate
                    gameManager.ResourceManager.SetGoldProductionRate(building.GetProductionAmount());
                    break;
                    
                // Other building effects are applied during combat or other systems
            }
        }
        
        public static float GetCombatBonus(Core.GuildManager guildManager, JobClass jobClass, string statType)
        {
            if (guildManager == null) return 0f;
            
            float bonus = 0f;
            var buildings = guildManager.GetGuildData().Buildings;
            
            foreach (var building in buildings)
            {
                if (building.IsConstructing) continue;
                
                switch (building.Type)
                {
                    case Core.GuildManager.BuildingType.TrainingGround:
                        if (statType == "experience")
                            bonus += building.GetCurrentEffect();
                        break;
                        
                    case Core.GuildManager.BuildingType.Armory:
                        if (statType == "attack" || statType == "defense")
                            bonus += building.GetCurrentEffect();
                        break;
                        
                    case Core.GuildManager.BuildingType.MageTower:
                        if ((jobClass == JobClass.Mage || jobClass == JobClass.Sage) && statType == "magic")
                            bonus += building.GetCurrentEffect();
                        break;
                        
                    case Core.GuildManager.BuildingType.Temple:
                        if ((jobClass == JobClass.Priest || jobClass == JobClass.Sage) && statType == "healing")
                            bonus += building.GetCurrentEffect();
                        break;
                }
            }
            
            return bonus / 100f; // Convert percentage to multiplier
        }
    }

    [System.Serializable]
    public class BuildingStats
    {
        public int maxAdventurers;
        public float trainingSpeed;
        public float researchSpeed;
        public int storageCapacity;
        public int goldProduction;
        public int woodProduction;
        public int stoneProduction;
        public int manaProduction;
        public int defenseBonus;
        public int attackBonus;
        public int healthBonus;
        public int manaBonus;
        
        public BuildingStats()
        {
            maxAdventurers = 0;
            trainingSpeed = 1.0f;
            researchSpeed = 1.0f;
            storageCapacity = 0;
            goldProduction = 0;
            woodProduction = 0;
            stoneProduction = 0;
            manaProduction = 0;
            defenseBonus = 0;
            attackBonus = 0;
            healthBonus = 0;
            manaBonus = 0;
        }
    }
}