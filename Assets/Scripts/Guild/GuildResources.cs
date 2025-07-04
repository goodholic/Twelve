using UnityEngine;
using GuildMaster.Data;

namespace GuildMaster.Guild
{
    /// <summary>
    /// 길드 자원 정보
    /// </summary>
    [System.Serializable]
    public class GuildResources
    {
        public float gold;
        public float wood;
        public float stone;
        public float manaStone;
        
        public GuildResources()
        {
            gold = 1000f;
            wood = 500f;
            stone = 500f;
            manaStone = 100f;
        }
        
        public GuildResources(float gold, float wood, float stone, float manaStone)
        {
            this.gold = gold;
            this.wood = wood;
            this.stone = stone;
            this.manaStone = manaStone;
        }
        
        public bool CanAfford(GuildResources cost)
        {
            return gold >= cost.gold && 
                   wood >= cost.wood && 
                   stone >= cost.stone && 
                   manaStone >= cost.manaStone;
        }
        
        public void Subtract(GuildResources cost)
        {
            gold -= cost.gold;
            wood -= cost.wood;
            stone -= cost.stone;
            manaStone -= cost.manaStone;
        }
        
        public void Add(GuildResources amount)
        {
            gold += amount.gold;
            wood += amount.wood;
            stone += amount.stone;
            manaStone += amount.manaStone;
        }
    }
    
    // BuildingType enum은 BuildingData.cs에서 정의됨
    
    /// <summary>
    /// 건물 데이터
    /// </summary>
    [System.Serializable]
    public class BuildingData
    {
        public string name;
        public string description;
        public Vector2Int size;
        public int maxLevel;
        public float constructionTime;
        public float upgradeTime;
        public GuildResources baseCost;
        public GuildResources baseProduction;
        
        public GuildResources GetConstructionCost(int level)
        {
            float multiplier = 1f + (level - 1) * 0.5f;
            return new GuildResources
            {
                gold = baseCost.gold * multiplier,
                wood = baseCost.wood * multiplier,
                stone = baseCost.stone * multiplier,
                manaStone = baseCost.manaStone * multiplier
            };
        }
        
        public GuildResources GetUpgradeCost(int level)
        {
            return GetConstructionCost(level);
        }
        
        public GuildResources GetResourceProduction(int level)
        {
            float multiplier = level;
            return new GuildResources
            {
                gold = baseProduction.gold * multiplier,
                wood = baseProduction.wood * multiplier,
                stone = baseProduction.stone * multiplier,
                manaStone = baseProduction.manaStone * multiplier
            };
        }
    }
} 