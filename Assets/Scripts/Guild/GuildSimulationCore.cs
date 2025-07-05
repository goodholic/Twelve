using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Guild; // GuildResources를 위해 추가
using GuildMaster.Core;
using GuildMaster.Data;

namespace GuildMaster.Guild
{
    /// <summary>
    /// README 기준 길드 시뮬레이션 핵심 시스템
    /// - 길드 건설: 훈련소, 연구소, 숙소, 상점 등 시설 건설
    /// - 시설 업그레이드: 각 시설의 레벨업으로 효과 증가
    /// - 자원 관리: 골드, 목재, 석재, 마나스톤 등 자원 수급
    /// - 연구 개발: 새로운 직업, 스킬, 전술 연구
    /// </summary>
    public class GuildSimulationCore : MonoBehaviour
    {
        public static GuildSimulationCore Instance { get; private set; }
        
        [Header("길드 기본 정보")]
        [SerializeField] private string guildName = "새로운 길드";
        [SerializeField] private int guildLevel = 1;
        [SerializeField] private int guildReputation = 100;
        [SerializeField] private int guildMemberCapacity = 20;
        
        [Header("자원 관리")]
        [SerializeField] private GuildResources resources = new GuildResources();
        [SerializeField] private float resourceProductionRate = 1.0f;
        [SerializeField] private float lastProductionTime;
        
        [Header("건물 시스템")]
        [SerializeField] private List<GuildBuilding> buildings = new List<GuildBuilding>();
        [SerializeField] private Vector2Int guildGridSize = new Vector2Int(10, 10);
        [SerializeField] private bool[,] occupiedTiles;
        
        [Header("연구 시스템")]
        [SerializeField] private List<ResearchData> availableResearch = new List<ResearchData>();
        [SerializeField] private List<ResearchData> completedResearch = new List<ResearchData>();
        [SerializeField] private ResearchData currentResearch;
        
        // 이벤트
        public event Action<GuildBuilding> OnBuildingConstructed;
        public event Action<GuildBuilding> OnBuildingUpgraded;
        public event Action<GuildResources> OnResourcesChanged;
        public event Action<ResearchData> OnResearchCompleted;
        public event Action<int> OnGuildLevelUp;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGuildSimulation();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            // 자원 자동 생산 (실시간)
            ProduceResources();
            
            // 연구 진행
            UpdateResearch();
            
            // 건물 완성 체크
            CheckBuildingCompletion();
        }
        
        /// <summary>
        /// 길드 시뮬레이션 초기화
        /// </summary>
        private void InitializeGuildSimulation()
        {
            occupiedTiles = new bool[guildGridSize.x, guildGridSize.y];
            lastProductionTime = Time.time;
            
            // 기본 건물 설치 (길드 홀)
            ConstructBuilding(BuildingType.GuildHall, new Vector2Int(4, 4), true);
            
            // 기본 자원 설정
            resources.gold = 1000;
            resources.wood = 500;
            resources.stone = 300;
            resources.manaStone = 100;
            
            // 연구 목록 초기화
            InitializeResearchTree();
            
            Debug.Log($"길드 '{guildName}' 시뮬레이션 초기화 완료!");
        }
        
        /// <summary>
        /// 자원 생산 시스템
        /// </summary>
        private void ProduceResources()
        {
            float currentTime = Time.time;
            float deltaTime = currentTime - lastProductionTime;
            
            if (deltaTime >= 1.0f) // 1초마다 생산
            {
                var production = CalculateResourceProduction();
                
                resources.gold += production.gold * deltaTime * resourceProductionRate;
                resources.wood += production.wood * deltaTime * resourceProductionRate;
                resources.stone += production.stone * deltaTime * resourceProductionRate;
                resources.manaStone += production.manaStone * deltaTime * resourceProductionRate;
                
                lastProductionTime = currentTime;
                OnResourcesChanged?.Invoke(resources);
            }
        }
        
        /// <summary>
        /// 건물별 자원 생산량 계산
        /// </summary>
        private GuildResources CalculateResourceProduction()
        {
            var totalProduction = new GuildResources();
            
            foreach (var building in buildings)
            {
                if (building.isConstructed && building.GetData() != null)
                {
                    var buildingData = building.GetData();
                    // GuildMaster.Data.BuildingData를 사용하여 자원 생산량 계산
                    if (buildingData.canProduce && buildingData.CanProduce())
                    {
                        int productionAmount = buildingData.ProduceResources();
                        
                        switch (buildingData.productionType)
                        {
                            case Data.ResourceType.Gold:
                                totalProduction.gold += productionAmount;
                                break;
                            case Data.ResourceType.Wood:
                                totalProduction.wood += productionAmount;
                                break;
                            case Data.ResourceType.Stone:
                                totalProduction.stone += productionAmount;
                                break;
                            case Data.ResourceType.ManaStone:
                                totalProduction.manaStone += productionAmount;
                                break;
                        }
                    }
                }
            }
            
            return totalProduction;
        }
        
        /// <summary>
        /// 건물 건설
        /// </summary>
        public bool ConstructBuilding(GuildMaster.Data.BuildingType buildingType, Vector2Int position, bool isInstant = false)
        {
            var buildingData = BuildingDatabase.GetBuildingData(buildingType);
            if (buildingData == null)
            {
                Debug.LogError($"건물 데이터를 찾을 수 없습니다: {buildingType}");
                return false;
            }
            
            // 자원 체크
            var cost = new GuildResources 
            { 
                gold = buildingData.buildCost, 
                wood = buildingData.baseWoodCost, 
                stone = buildingData.baseStoneCost, 
                manaStone = buildingData.baseManaCost 
            };
            if (!CanAfford(cost))
            {
                Debug.LogWarning("건설에 필요한 자원이 부족합니다!");
                return false;
            }
            
            // 위치 체크
            if (!CanPlaceBuilding(position, buildingData.size))
            {
                Debug.LogWarning("건물을 배치할 수 없는 위치입니다!");
                return false;
            }
            
            // 자원 소모
            SpendResources(cost);
            
            // 건물 생성
            var newBuilding = new GuildBuilding
            {
                buildingType = buildingType,
                position = position,
                level = 1,
                constructionProgress = 0f,
                upgradeProgress = 0f,
                                 lastProductionTime = Time.time
            };
            
            buildings.Add(newBuilding);
            OccupyTiles(position, buildingData.size, true);
            
            if (isInstant)
            {
                OnBuildingConstructed?.Invoke(newBuilding);
            }
            
            Debug.Log($"건물 건설 시작: {buildingType} at {position}");
            return true;
        }
        
        /// <summary>
        /// 건물 업그레이드
        /// </summary>
        public bool UpgradeBuilding(GuildBuilding building)
        {
            var buildingData = building.GetData();
            if (buildingData == null)
            {
                Debug.LogWarning("건물 데이터를 찾을 수 없습니다!");
                return false;
            }
            
            if (building.level >= buildingData.maxLevel)
            {
                Debug.LogWarning("이미 최대 레벨입니다!");
                return false;
            }
            
            // 업그레이드 가능 여부 확인
            if (!buildingData.CanUpgrade())
            {
                Debug.LogWarning("더 이상 업그레이드할 수 없습니다!");
                return false;
            }
            
            // 업그레이드 비용 계산
            var upgradeCost = new GuildResources
            {
                gold = buildingData.buildCost * (building.level + 1),
                wood = buildingData.baseWoodCost * (building.level + 1),
                stone = buildingData.baseStoneCost * (building.level + 1),
                manaStone = buildingData.baseManaCost * (building.level + 1)
            };
            
            if (!CanAfford(upgradeCost))
            {
                Debug.LogWarning("업그레이드에 필요한 자원이 부족합니다!");
                return false;
            }
            
            SpendResources(upgradeCost);
            building.level++;
            building.upgradeProgress = 0f;
            
            Debug.Log($"건물 업그레이드 시작: {building.buildingType} -> Lv.{building.level}");
            return true;
        }
        
        /// <summary>
        /// 연구 시작
        /// </summary>
        public bool StartResearch(ResearchData research)
        {
            if (currentResearch != null)
            {
                Debug.LogWarning("이미 진행 중인 연구가 있습니다!");
                return false;
            }
            
            if (!research.CanResearch(completedResearch, guildLevel))
            {
                Debug.LogWarning("연구 조건을 만족하지 않습니다!");
                return false;
            }
            
            if (!CanAfford(research.cost))
            {
                Debug.LogWarning("연구 비용이 부족합니다!");
                return false;
            }
            
            SpendResources(research.cost);
            currentResearch = research;
            currentResearch.progress = 0f;
            currentResearch.isInProgress = true;
            currentResearch.startTime = System.DateTime.Now;
            
            Debug.Log($"연구 시작: {research.name}");
            return true;
        }
        
        /// <summary>
        /// 연구 업데이트
        /// </summary>
        private void UpdateResearch()
        {
            if (currentResearch == null || !currentResearch.isInProgress) return;
            
            currentResearch.progress += Time.deltaTime / currentResearch.researchTime;
            currentResearch.progress = Mathf.Clamp01(currentResearch.progress);
            
            if (currentResearch.progress >= 1.0f)
            {
                CompleteResearch();
            }
        }
        
        /// <summary>
        /// 연구 완료
        /// </summary>
        private void CompleteResearch()
        {
            if (currentResearch == null) return;
            
            completedResearch.Add(currentResearch);
            currentResearch.ApplyResearchBonus();
            
            OnResearchCompleted?.Invoke(currentResearch);
            Debug.Log($"연구 완료: {currentResearch.name}");
            
            currentResearch = null;
        }
        
        /// <summary>
        /// 건물 완성 체크
        /// </summary>
        private void CheckBuildingCompletion()
        {
            foreach (var building in buildings)
            {
                // 건설 완료 체크
                if (!building.isConstructed && building.GetConstructionProgress() >= 1f)
                {
                    building.isConstructed = true;
                    OnBuildingConstructed?.Invoke(building);
                    Debug.Log($"건물 건설 완료: {building.buildingType}");
                }
                
                // 업그레이드 완료 체크
                if (building.isUpgrading && building.GetUpgradeProgress() < 1f)
                {
                    building.upgradeProgress += Time.deltaTime;
                    if (building.upgradeProgress >= 1f)
                    {
                        building.upgradeProgress = 1f;
                        OnBuildingUpgraded?.Invoke(building);
                        Debug.Log($"건물 업그레이드 완료: {building.buildingType} Lv.{building.level}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 자원 소모 가능 여부 체크
        /// </summary>
        private bool CanAfford(GuildResources cost)
        {
            return resources.gold >= cost.gold &&
                   resources.wood >= cost.wood &&
                   resources.stone >= cost.stone &&
                   resources.manaStone >= cost.manaStone;
        }
        
        /// <summary>
        /// 자원 소모
        /// </summary>
        private void SpendResources(GuildResources cost)
        {
            resources.gold -= cost.gold;
            resources.wood -= cost.wood;
            resources.stone -= cost.stone;
            resources.manaStone -= cost.manaStone;
            
            OnResourcesChanged?.Invoke(resources);
        }
        
        /// <summary>
        /// 건물 배치 가능 여부 체크
        /// </summary>
        private bool CanPlaceBuilding(Vector2Int position, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int checkPos = position + new Vector2Int(x, y);
                    if (checkPos.x < 0 || checkPos.x >= guildGridSize.x ||
                        checkPos.y < 0 || checkPos.y >= guildGridSize.y ||
                        occupiedTiles[checkPos.x, checkPos.y])
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        /// 타일 점유 설정
        /// </summary>
        private void OccupyTiles(Vector2Int position, Vector2Int size, bool occupy)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int tilePos = position + new Vector2Int(x, y);
                    if (tilePos.x >= 0 && tilePos.x < guildGridSize.x &&
                        tilePos.y >= 0 && tilePos.y < guildGridSize.y)
                    {
                        occupiedTiles[tilePos.x, tilePos.y] = occupy;
                    }
                }
            }
        }
        
        /// <summary>
        /// 연구 트리 초기화
        /// </summary>
        private void InitializeResearchTree()
        {
            availableResearch.AddRange(new List<ResearchData>
            {
                new ResearchData { id = "R1", name = "기초 전술", description = "전투력 +10%", requiredLevel = 1, researchTime = 300, cost = new GuildResources { gold = 300, wood = 200, stone = 100, manaStone = 60 }, isUnlocked = true, isCompleted = false, progress = 0f },
                new ResearchData { id = "R2", name = "자원 효율", description = "자원 생산 +20%", requiredLevel = 2, researchTime = 500, cost = new GuildResources { gold = 500, wood = 200, stone = 100, manaStone = 120 }, isUnlocked = true, isCompleted = false, progress = 0f },
                new ResearchData { id = "R3", name = "건설 기술", description = "건설 시간 -25%", requiredLevel = 3, researchTime = 400, cost = new GuildResources { gold = 400, wood = 300, stone = 200, manaStone = 50 }, isUnlocked = true, isCompleted = false, progress = 0f },
                new ResearchData { id = "R4", name = "고급 무기술", description = "물리 공격력 +15%", requiredLevel = 4, researchTime = 800, cost = new GuildResources { gold = 800, wood = 0, stone = 0, manaStone = 100 }, isUnlocked = true, isCompleted = false, progress = 0f },
                new ResearchData { id = "R5", name = "마법 연구", description = "마법 공격력 +20%", requiredLevel = 5, researchTime = 600, cost = new GuildResources { gold = 600, wood = 0, stone = 0, manaStone = 200 }, isUnlocked = true, isCompleted = false, progress = 0f },
                new ResearchData { id = "R6", name = "길드 확장", description = "멤버 수 +5명", requiredLevel = 6, researchTime = 1000, cost = new GuildResources { gold = 1000, wood = 500, stone = 300, manaStone = 150 }, isUnlocked = true, isCompleted = false, progress = 0f }
            });
        }
        
        /// <summary>
        /// 길드 레벨업
        /// </summary>
        public void LevelUpGuild()
        {
            guildLevel++;
            guildMemberCapacity += 2;
            OnGuildLevelUp?.Invoke(guildLevel);
            
            Debug.Log($"길드 레벨업! 현재 레벨: {guildLevel}");
        }
        
        // 접근자 메서드들
        public GuildResources GetResources() => resources;
        public List<GuildBuilding> GetBuildings() => buildings;
        public List<ResearchData> GetAvailableResearch() => availableResearch;
        public List<ResearchData> GetCompletedResearch() => completedResearch;
        public ResearchData GetCurrentResearch() => currentResearch;
        public string GetGuildName() => guildName;
        public int GetGuildLevel() => guildLevel;
        public int GetGuildReputation() => guildReputation;
        public int GetMemberCapacity() => guildMemberCapacity;
        
        // 편의 메서드들
        public GuildBuilding GetBuilding(BuildingType type) => buildings.FirstOrDefault(b => b.buildingType == type);
        public int GetBuildingCount(BuildingType type) => buildings.Count(b => b.buildingType == type && b.isConstructed);
        public bool HasBuilding(BuildingType type) => buildings.Any(b => b.buildingType == type && b.isConstructed);
        public float GetResourceProductionBonus() => completedResearch.Sum(r => r.bonuses.Sum(b => b.Value));
        public float GetCombatPowerBonus() => completedResearch.Sum(r => r.bonuses.Sum(b => b.Value));
        
        /// <summary>
        /// GameManager에서 호출하는 초기화 메서드
        /// </summary>
        public void Initialize()
        {
            if (Instance == null)
            {
                InitializeGuildSimulation();
            }
        }
        
        /// <summary>
        /// GameManager에서 호출하는 업데이트 메서드
        /// </summary>
        public void UpdateGuildSystems(float deltaTime)
        {
            // 이미 Update()에서 처리하고 있으므로 추가 로직 없음
            // 필요하면 여기에 추가 업데이트 로직 작성
        }
        
        /// <summary>
        public void SaveGuildData()
        {
            PlayerPrefs.SetString("GuildName", guildName);
            PlayerPrefs.SetInt("GuildLevel", guildLevel);
            PlayerPrefs.SetInt("GuildReputation", guildReputation);
            PlayerPrefs.SetFloat("GuildGold", resources.gold);
            PlayerPrefs.SetFloat("GuildWood", resources.wood);
            PlayerPrefs.SetFloat("GuildStone", resources.stone);
            PlayerPrefs.SetFloat("GuildManaStone", resources.manaStone);
            Debug.Log("길드 데이터 저장 완료");
        }
        
        /// <summary>
        /// 길드 데이터 로드
        /// </summary>
        public void LoadGuildData()
        {
            guildName = PlayerPrefs.GetString("GuildName", "새로운 길드");
            guildLevel = PlayerPrefs.GetInt("GuildLevel", 1);
            guildReputation = PlayerPrefs.GetInt("GuildReputation", 100);
            resources.gold = PlayerPrefs.GetFloat("GuildGold", 1000);
            resources.wood = PlayerPrefs.GetFloat("GuildWood", 500);
            resources.stone = PlayerPrefs.GetFloat("GuildStone", 300);
            resources.manaStone = PlayerPrefs.GetFloat("GuildManaStone", 100);
            Debug.Log("길드 데이터 로드 완료");
        }
        
        /// <summary>
        /// 길드 데이터 리셋
        /// </summary>
        public void ResetGuildData()
        {
            guildName = "새로운 길드";
            guildLevel = 1;
            guildReputation = 100;
            guildMemberCapacity = 20;
            resources.gold = 1000;
            resources.wood = 500;
            resources.stone = 300;
            resources.manaStone = 100;
            buildings.Clear();
            completedResearch.Clear();
            currentResearch = null;
            Debug.Log("길드 데이터 리셋 완료");
        }
    }
    
    // GuildResources는 별도 파일로 분리됨
    
    /// <summary>
    /// 연구 트리
    /// </summary>
    [System.Serializable]
    public class ResearchData
    {
        public string id;
        public string name;
        public string description;
        public int requiredLevel;
        public float researchTime;
        public GuildResources cost;
        public bool isUnlocked;
        public bool isCompleted;
        public float progress;
        
        // 연구 중 상태 관리
        [System.NonSerialized] public bool isInProgress = false;
        [System.NonSerialized] public System.DateTime startTime;
        [System.NonSerialized] public List<string> prerequisiteResearch = new List<string>();
        
        // 연구 완료 시 적용되는 보너스
        public Dictionary<string, float> bonuses = new Dictionary<string, float>();
        
        public void ApplyResearchBonus()
        {
            if (!isCompleted) return;
            
            // 보너스 적용 로직
            foreach (var bonus in bonuses)
            {
                Debug.Log($"연구 보너스 적용: {bonus.Key} +{bonus.Value}");
            }
        }
        
        public bool CanResearch(List<ResearchData> completedResearch, int guildLevel)
        {
            if (guildLevel < requiredLevel) return false;
            
            foreach (var prerequisite in prerequisiteResearch)
            {
                if (!completedResearch.Any(r => r.id == prerequisite))
                    return false;
            }
            
            return true;
        }
    }
}

/// <summary>
/// 건물 데이터베이스 (간단화)
/// </summary>
public static class BuildingDatabase
{
    public static GuildMaster.Data.BuildingData GetBuildingData(BuildingType type)
    {
        return new GuildMaster.Data.BuildingData
        {
            buildingId = type.ToString(),
            buildingName = type.ToString(),
            description = $"{type} 건물",
            buildingType = type,
            category = GuildMaster.Data.BuildingCategory.Economic,
            width = 2,
            height = 2,
            buildCost = 100,
            baseWoodCost = 50,
            baseStoneCost = 25,
            baseManaCost = 10,
            buildTime = 60,
            maxLevel = 5,
            canProduce = true,
            productionType = GuildMaster.Data.ResourceType.Gold,
            productionAmount = 10,
            productionTime = 3600f
        };
    }
} 