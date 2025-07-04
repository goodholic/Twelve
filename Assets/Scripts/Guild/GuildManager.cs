using UnityEngine;
using System;
using System.Collections.Generic;
using GuildMaster.Core;

using GuildMaster.Data;
using System.Linq;
using CoreResourceType = GuildMaster.Core.ResourceType;
using GuildMaster.Battle;
using GuildMaster.Systems;

namespace GuildMaster.Guild
{
    /// <summary>
    /// 길드 관리 시스템
    /// </summary>
    public class GuildManager : MonoBehaviour
    {
        private static GuildManager _instance;
        public static GuildManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GuildManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GuildManager");
                        _instance = go.AddComponent<GuildManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Guild Settings")]
        public string guildName = "Default Guild";
        public string guildId = "";
        public int guildLevel = 1;
        public int guildExperience = 0;
        public int maxMembers = 10;
        public int maxAdventurers = 10;
        public List<GuildMaster.Battle.Unit> adventurers = new List<GuildMaster.Battle.Unit>();

        public List<Building> buildings = new List<Building>();
        public Dictionary<string, Building> buildingDict = new Dictionary<string, Building>();

        // Events
        public event Action<Building> OnBuildingConstructed;
        public event Action<Building> OnBuildingUpgraded;
        public event Action<Building> OnBuildingDestroyed;
        public event Action<GuildBuilding> OnBuildingPlaced;
        public event Action<GuildMaster.Battle.Unit> OnAdventurerRecruited;
        public event Action<int> OnGuildLevelUp;

        // BuildingType enum은 BuildingData.cs에서 정의됨

        [System.Serializable]
        public class Building
        {
            public string id;
            public string name;
            public BuildingType type;
            public int level;
            public Vector3 position;
            public bool isConstructed;
            public float constructionTime;
            public Dictionary<CoreResourceType, int> constructionCost;
            public Dictionary<CoreResourceType, float> productionRate;

            // 호환성을 위한 속성들
            public BuildingType Type => type;
            public int currentLevel => level;

            public Building()
            {
                id = System.Guid.NewGuid().ToString();
                constructionCost = new Dictionary<CoreResourceType, int>();
                productionRate = new Dictionary<CoreResourceType, float>();
                isConstructed = false;
                level = 1;
            }

            public Dictionary<CoreResourceType, int> GetProductionAmount()
            {
                var production = new Dictionary<CoreResourceType, int>();
                
                foreach (var kvp in productionRate)
                {
                    production[kvp.Key] = Mathf.RoundToInt(kvp.Value * level);
                }
                
                return production;
            }

            public void InitializeBuilding()
            {
                constructionCost.Clear();
                productionRate.Clear();
                
                // 건물별 기본 설정
                switch (type)
                {
                    case BuildingType.Farm:
                        constructionCost[CoreResourceType.Gold] = 200;
                        constructionCost[CoreResourceType.Wood] = 100;
                        productionRate[CoreResourceType.Food] = 10f; // 시간당 식량 10
                        break;
                        
                    case BuildingType.Mine:
                        constructionCost[CoreResourceType.Gold] = 300;
                        constructionCost[CoreResourceType.Wood] = 50;
                        productionRate[CoreResourceType.Stone] = 8f; // 시간당 돌 8
                        productionRate[CoreResourceType.Gold] = 5f;  // 시간당 골드 5
                        break;
                        
                    case BuildingType.Lumbermill:
                        constructionCost[CoreResourceType.Gold] = 250;
                        constructionCost[CoreResourceType.Stone] = 50;
                        productionRate[CoreResourceType.Wood] = 12f; // 시간당 목재 12
                        break;
                        
                    case BuildingType.ManaExtractor:
                        constructionCost[CoreResourceType.Gold] = 500;
                        constructionCost[CoreResourceType.Stone] = 100;
                        constructionCost[CoreResourceType.Wood] = 100;
                        productionRate[CoreResourceType.ManaStone] = 3f; // 시간당 마나스톤 3
                        break;
                        
                    case BuildingType.Storage:
                        constructionCost[CoreResourceType.Gold] = 400;
                        constructionCost[CoreResourceType.Wood] = 200;
                        constructionCost[CoreResourceType.Stone] = 150;
                        // 창고는 자원 저장 용량 증가 (생산 없음)
                        break;
                        
                    default:
                        constructionCost[CoreResourceType.Gold] = 300;
                        constructionCost[CoreResourceType.Wood] = 100;
                        break;
                }
                
                // 레벨에 따른 건설 시간 (초)
                constructionTime = GetBaseBuildTime() + (level * 300f);
            }
            
            public float GetBaseBuildTime()
            {
                return type switch
                {
                    BuildingType.Farm => 300f,        // 5분
                    BuildingType.Mine => 600f,        // 10분
                    BuildingType.Lumbermill => 450f,  // 7.5분
                    BuildingType.ManaExtractor => 1200f, // 20분
                    BuildingType.Storage => 900f,     // 15분
                    BuildingType.Barracks => 800f,    // 13분
                    BuildingType.Library => 1000f,    // 16분
                    BuildingType.Workshop => 700f,    // 11분
                    BuildingType.TrainingGround => 600f, // 10분
                    BuildingType.Tavern => 500f,      // 8분
                    BuildingType.Market => 800f,      // 13분
                    BuildingType.ResearchLab => 1500f, // 25분
                    BuildingType.Temple => 1200f,     // 20분
                    BuildingType.Armory => 900f,      // 15분
                    _ => 300f
                };
            }

            public Dictionary<string, float> GetCurrentEffect()
            {
                var effects = new Dictionary<string, float>();
                
                switch (type)
                {
                    case BuildingType.Farm:
                        effects["food_production"] = level * 10f;
                        break;
                    case BuildingType.Mine:
                        effects["stone_production"] = level * 8f;
                        effects["gold_production"] = level * 5f;
                        break;
                    case BuildingType.Lumbermill:
                        effects["wood_production"] = level * 12f;
                        break;
                    case BuildingType.ManaExtractor:
                        effects["manastone_production"] = level * 3f;
                        break;
                    case BuildingType.Storage:
                        effects["storage_increase"] = level * 5000f;
                        break;
                    case BuildingType.ResearchLab:
                        effects["research_speed"] = 1f + (level * 0.1f);
                        break;
                    case BuildingType.Armory:
                        effects["attack_bonus"] = level * 2f;
                        break;
                    case BuildingType.Temple:
                        effects["heal_bonus"] = level * 1.5f;
                        break;
                    case BuildingType.TrainingGround:
                        effects["exp_bonus"] = level * 0.1f;
                        break;
                    case BuildingType.Tavern:
                        effects["recruitment_bonus"] = level * 0.05f;
                        break;
                    case BuildingType.Market:
                        effects["trade_discount"] = level * 0.02f;
                        break;
                    case BuildingType.Library:
                        effects["research_efficiency"] = level * 0.15f;
                        break;
                    case BuildingType.Workshop:
                        effects["craft_speed"] = level * 0.1f;
                        break;
                    case BuildingType.Barracks:
                        effects["training_speed"] = level * 0.1f;
                        break;
                    default:
                        effects["production_bonus"] = 1f + (level * 0.1f);
                        break;
                }
                
                return effects;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public Building GetBuildingById(string buildingId)
        {
            buildingDict.TryGetValue(buildingId, out Building building);
            return building;
        }

        public List<Building> GetBuildingsByType(BuildingType type)
        {
            return buildings.FindAll(b => b.type == type);
        }

        public void AddBuilding(Building building)
        {
            buildings.Add(building);
            buildingDict[building.id] = building;
            OnBuildingConstructed?.Invoke(building);
        }

        public void RemoveBuilding(string buildingId)
        {
            if (buildingDict.TryGetValue(buildingId, out Building building))
            {
                buildings.Remove(building);
                buildingDict.Remove(buildingId);
                OnBuildingDestroyed?.Invoke(building);
            }
        }

        public void UpgradeBuilding(string buildingId)
        {
            if (buildingDict.TryGetValue(buildingId, out Building building))
            {
                building.level++;
                OnBuildingUpgraded?.Invoke(building);
            }
        }
        
        // 호환성을 위한 추가 메서드들
        [System.Serializable]
        public class GuildData
        {
            public int GuildLevel;
            public int GuildReputation;
            public List<object> Adventurers = new List<object>();
        }
        
        private int reputation = 0;
        
        public void AddReputation(int amount)
        {
            reputation += amount;
        }
        
        public GuildData GetGuildData()
        {
            return new GuildData
            {
                GuildLevel = guildLevel,
                GuildReputation = reputation,
                Adventurers = new List<object>() // 실제 모험가 목록으로 대체 필요
            };
        }
        
        public Dictionary<CoreResourceType, float> GetResourceProduction()
        {
            var totalProduction = new Dictionary<CoreResourceType, float>();
            
            foreach (var building in buildings)
            {
                if (building.isConstructed)
                {
                    foreach (var kvp in building.productionRate)
                    {
                        if (totalProduction.ContainsKey(kvp.Key))
                            totalProduction[kvp.Key] += kvp.Value * building.level;
                        else
                            totalProduction[kvp.Key] = kvp.Value * building.level;
                    }
                }
            }
            
            return totalProduction;
        }
        
        // 건물 건설 메서드 추가
        public bool ConstructBuilding(BuildingType type, Vector3 position)
        {
            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null) return false;
            
            var building = new Building
            {
                type = type,
                name = GetBuildingName(type),
                position = position,
                level = 1
            };
            
            building.InitializeBuilding();
            
            // 건설 비용 확인 및 지불
            bool canAfford = true;
            foreach (var cost in building.constructionCost)
            {
                if (resourceManager.GetResource(cost.Key) < cost.Value)
                {
                    canAfford = false;
                    break;
                }
            }
            
            if (!canAfford) return false;
            
            // 자원 소모
            foreach (var cost in building.constructionCost)
            {
                resourceManager.SpendResource(cost.Key, cost.Value);
            }
            
            // 건물 추가
            AddBuilding(building);
            return true;
        }
        
        // 건물 이름 반환
        public string GetBuildingName(BuildingType type)
        {
            return type switch
            {
                BuildingType.Farm => "농장",
                BuildingType.Mine => "광산",
                BuildingType.Lumbermill => "제재소",
                BuildingType.ManaExtractor => "마나 추출기",
                BuildingType.Barracks => "병영",
                BuildingType.Library => "도서관",
                BuildingType.Workshop => "작업장",
                BuildingType.Storage => "창고",
                BuildingType.TrainingGround => "훈련장",
                BuildingType.Tavern => "주점",
                BuildingType.Market => "시장",
                BuildingType.ResearchLab => "연구소",
                BuildingType.Temple => "신전",
                BuildingType.Armory => "무기고",
                _ => "알 수 없는 건물"
            };
        }
        
        // 모험가 목록 반환 (GachaSystem 호환성)
        public List<GuildMaster.Battle.Unit> GetAdventurers()
        {
            // CharacterManager에서 활성 유닛들을 가져옴
            var characterManager = GuildMaster.Systems.CharacterManager.Instance;
            if (characterManager != null)
            {
                return characterManager.GetOwnedCharacters();
            }
            
            return new List<GuildMaster.Battle.Unit>();
        }
        
        // 건물 효과 총합 계산
        public Dictionary<string, float> GetTotalBuildingEffects()
        {
            var totalEffects = new Dictionary<string, float>();
            
            foreach (var building in buildings)
            {
                if (building.isConstructed)
                {
                    var effects = building.GetCurrentEffect();
                    foreach (var effect in effects)
                    {
                        if (totalEffects.ContainsKey(effect.Key))
                            totalEffects[effect.Key] += effect.Value;
                        else
                            totalEffects[effect.Key] = effect.Value;
                    }
                }
            }
            
            return totalEffects;
        }
        
        // 건물 업그레이드 비용 계산
        public Dictionary<CoreResourceType, int> GetUpgradeCost(string buildingId)
        {
            var building = GetBuildingById(buildingId);
            if (building == null) return new Dictionary<CoreResourceType, int>();
            
            var upgradeCost = new Dictionary<CoreResourceType, int>();
            
            foreach (var baseCost in building.constructionCost)
            {
                int cost = Mathf.RoundToInt(baseCost.Value * Mathf.Pow(1.5f, building.level));
                upgradeCost[baseCost.Key] = cost;
            }
            
            return upgradeCost;
        }
        
        // 건물 업그레이드 실행
        public bool UpgradeBuildingById(string buildingId)
        {
            var building = GetBuildingById(buildingId);
            if (building == null) return false;
            
            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null) return false;
            
            var upgradeCost = GetUpgradeCost(buildingId);
            
            // 비용 확인
            foreach (var cost in upgradeCost)
            {
                if (resourceManager.GetResource(cost.Key) < cost.Value)
                {
                    return false;
                }
            }
            
            // 비용 지불
            foreach (var cost in upgradeCost)
            {
                resourceManager.SpendResource(cost.Key, cost.Value);
            }
            
            // 업그레이드 실행
            UpgradeBuilding(buildingId);
            return true;
        }

        /// <summary>
        /// 건물 인스턴스를 가져옵니다
        /// </summary>
        public GuildBuilding GetBuildingInstance(int buildingId)
        {
            // GuildSimulationCore에서 건물 정보 가져오기
            if (GuildSimulationCore.Instance != null)
            {
                var buildings = GuildSimulationCore.Instance.GetBuildings();
                return buildings.FirstOrDefault(b => b.GetHashCode() == buildingId);
            }
            return null;
        }

        /// <summary>
        /// 건물 배치 가능한지 확인
        /// </summary>
        public bool CanPlaceBuilding(BuildingType type, Vector2Int position)
        {
            // 기본 건물 정보 (임시)
            var buildingInfo = GetBuildingInfo(type);
            if (buildingInfo == null) return false;
            
            // 비용 확인
            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
            {
                int gold = buildingInfo.cost.ContainsKey(CoreResourceType.Gold) ? buildingInfo.cost[CoreResourceType.Gold] : 0;
                int wood = buildingInfo.cost.ContainsKey(CoreResourceType.Wood) ? buildingInfo.cost[CoreResourceType.Wood] : 0;
                int stone = buildingInfo.cost.ContainsKey(CoreResourceType.Stone) ? buildingInfo.cost[CoreResourceType.Stone] : 0;
                int mana = buildingInfo.cost.ContainsKey(CoreResourceType.Mana) ? buildingInfo.cost[CoreResourceType.Mana] : 0;
                if (!resourceManager.CanAfford(gold, wood, stone, mana))
                {
                    Debug.Log($"자원이 부족합니다. 필요: {buildingInfo.cost}");
                    return false;
                }
            }
            
            // 공간 확인 (간단한 체크)
            if (!CheckSpaceAvailable(position, buildingInfo.size))
            {
                Debug.Log($"공간이 부족합니다. 위치: {position}, 크기: {buildingInfo.size}");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 건물 배치
        /// </summary>
        public GuildBuilding PlaceBuilding(BuildingType type, Vector2Int position)
        {
            if (!CanPlaceBuilding(type, position)) return null;
            
            var buildingInfo = GetBuildingInfo(type);
            var building = new GuildBuilding
            {
                buildingType = BuildingType.GuildHall,
                position = position,
                level = 1
            };
            
            buildings.Add(new Building
            {
                type = type,
                position = new Vector3(position.x, 0, position.y),
                level = 1,
                isConstructed = false,
                constructionTime = buildingInfo.constructionTime,
                constructionCost = buildingInfo.cost
            });
            
            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
            {
                int gold = buildingInfo.cost.ContainsKey(CoreResourceType.Gold) ? buildingInfo.cost[CoreResourceType.Gold] : 0;
                int wood = buildingInfo.cost.ContainsKey(CoreResourceType.Wood) ? buildingInfo.cost[CoreResourceType.Wood] : 0;
                int stone = buildingInfo.cost.ContainsKey(CoreResourceType.Stone) ? buildingInfo.cost[CoreResourceType.Stone] : 0;
                int mana = buildingInfo.cost.ContainsKey(CoreResourceType.Mana) ? buildingInfo.cost[CoreResourceType.Mana] : 0;
                resourceManager.SpendResources(gold, wood, stone, mana);
            }
            
            OnBuildingPlaced?.Invoke(building);
            
            Debug.Log($"건물 배치: {type} at {position}");
            return building;
        }

        // 헬퍼 메서드들
        private BuildingInfo GetBuildingInfo(BuildingType type)
        {
            // 기본 건물 정보 반환
            return new BuildingInfo
            {
                type = type,
                size = new Vector2Int(2, 2),
                constructionTime = 60f,
                cost = new Dictionary<CoreResourceType, int>
                {
                    { CoreResourceType.Gold, 100 },
                    { CoreResourceType.Wood, 50 },
                    { CoreResourceType.Stone, 30 }
                }
            };
        }
        
        private bool CheckSpaceAvailable(Vector2Int position, Vector2Int size)
        {
            // 간단한 공간 확인 로직
            return true; // 임시로 항상 true 반환
        }
        
        [System.Serializable]
        public class BuildingInfo
        {
            public BuildingType type;
            public Vector2Int size;
            public float constructionTime;
            public Dictionary<CoreResourceType, int> cost;
        }

        // 누락된 메서드들 추가
        public void Initialize()
        {
            if (string.IsNullOrEmpty(guildId))
            {
                guildId = System.Guid.NewGuid().ToString();
            }
            
            if (string.IsNullOrEmpty(guildName))
            {
                guildName = "Default Guild";
            }
            
            if (adventurers == null)
                adventurers = new List<GuildMaster.Battle.Unit>();
                
            if (buildings == null)
                buildings = new List<Building>();
                
            if (buildingDict == null)
                buildingDict = new Dictionary<string, Building>();
        }
        
        public string GetGuildName()
        {
            return guildName;
        }
        
        public int GetGuildLevel()
        {
            return guildLevel;
        }
        
        public string GetGuildId()
        {
            return guildId;
        }
        
        public void LevelUp()
        {
            guildLevel++;
            maxAdventurers += 2; // 레벨업시 최대 모험가 수 증가
            OnGuildLevelUp?.Invoke(guildLevel);
        }
        
        public void AddGuildExperience(int exp)
        {
            guildExperience += exp;
            
            // 레벨업 체크
            int requiredExp = guildLevel * 1000;
            while (guildExperience >= requiredExp)
            {
                guildExperience -= requiredExp;
                LevelUp();
                requiredExp = guildLevel * 1000;
            }
        }
        
        public void RemoveAdventurer(GuildMaster.Battle.Unit adventurer)
        {
            if (adventurers.Contains(adventurer))
            {
                adventurers.Remove(adventurer);
            }
        }
        
        public void IncreaseMaxAdventurers(int amount)
        {
            maxAdventurers += amount;
        }
        
        public int GetBuildingLevel(BuildingType type)
        {
            var building = buildings.FirstOrDefault(b => b.type == type);
            return building?.level ?? 0;
        }
        
        public void RecruitAdventurer(GuildMaster.Battle.Unit adventurer)
        {
            if (adventurers.Count < maxAdventurers)
            {
                adventurers.Add(adventurer);
                OnAdventurerRecruited?.Invoke(adventurer);
            }
        }
    }
    
    /// <summary>
    /// 건설 진행 상황 클래스
    /// </summary>
    [System.Serializable]
    public class BuildingConstruction
    {
        public GuildBuilding Building;
        public System.DateTime StartTime;
        public System.DateTime EstimatedCompletion;
        public float Progress => Mathf.Clamp01((float)(System.DateTime.Now - StartTime).TotalSeconds / (float)(EstimatedCompletion - StartTime).TotalSeconds);
        public System.TimeSpan RemainingTime => EstimatedCompletion - System.DateTime.Now;
    }

    public class GuildBuilding
    {
        public BuildingType buildingType;
        public Vector2Int position;
        public int level;
        public bool isUpgrading;
        public bool isConstructed;
        public float constructionProgress;
        public float upgradeProgress;
        public DateTime lastProductionTime;
        
        public GuildBuilding()
        {
            isConstructed = false;
            constructionProgress = 0f;
            upgradeProgress = 0f;
            lastProductionTime = DateTime.Now;
            level = 1;
        }
        
        public GuildMaster.Data.BuildingData GetData()
        {
            return DataManager.Instance?.GetBuildingData(buildingType.ToString()) ?? null;
        }
        
        public bool IsConstructionComplete()
        {
            return constructionProgress >= 1.0f;
        }
        
        public bool IsUpgradeComplete()
        {
            return upgradeProgress >= 1.0f;
        }
    }
} 