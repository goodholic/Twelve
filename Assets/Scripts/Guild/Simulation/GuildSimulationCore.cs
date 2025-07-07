using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using GuildMaster.Guild; // GuildResources 제거됨
using GuildMaster.Core;
// using GuildMaster.Data; // Commented out - building types removed

namespace GuildMaster.Guild
{
    // TODO: Replace with proper building system implementation
    // Building classes removed - temporary implementation was removed
    
    // GuildBuildingTemp 및 GuildResources 타입이 제거됨

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
        // GuildResources 타입이 제거됨
        // [SerializeField] private GuildResources resources = new GuildResources();
        [SerializeField] private float resourceProductionRate = 1.0f;
        [SerializeField] private float lastProductionTime;
        
        [Header("건물 시스템")]
        // TODO: Implement proper building list
        // GuildBuildingTemp 타입이 제거됨
        // [SerializeField] private List<GuildBuildingTemp> buildings = new List<GuildBuildingTemp>();
        [SerializeField] private Vector2Int guildGridSize = new Vector2Int(10, 10);
        [SerializeField] private bool[,] occupiedTiles;
        
        [Header("연구 시스템")]
        [SerializeField] private List<ResearchData> availableResearch = new List<ResearchData>();
        [SerializeField] private List<ResearchData> completedResearch = new List<ResearchData>();
        [SerializeField] private ResearchData currentResearch;
        
        // 이벤트
        // TODO: Update to proper building type when available
        // 타입이 제거되어 주석 처리
        // public event Action<GuildBuildingTemp> OnBuildingConstructed;
        // public event Action<GuildBuildingTemp> OnBuildingUpgraded;
        // public event Action<GuildResources> OnResourcesChanged;
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
            
            // TODO: Re-enable when building system is implemented
            // 기본 건물 설치 (길드 홀)
            // ConstructBuilding("GuildHall", new Vector2Int(4, 4), true);
            
            // 기본 자원 설정
            // TODO: Re-enable when GuildResources is available
            /*
            resources.gold = 1000;
            resources.wood = 500;
            resources.stone = 300;
            resources.manaStone = 100;
            */
            
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
                // TODO: Re-enable when GuildResources is available
                /*
                var production = CalculateResourceProduction();
                
                resources.gold += production.gold * deltaTime * resourceProductionRate;
                resources.wood += production.wood * deltaTime * resourceProductionRate;
                resources.stone += production.stone * deltaTime * resourceProductionRate;
                resources.manaStone += production.manaStone * deltaTime * resourceProductionRate;
                
                lastProductionTime = currentTime;
                OnResourcesChanged?.Invoke(resources);
                */
            }
        }
        
        /// <summary>
        /// 건물별 자원 생산량 계산
        /// </summary>
        // TODO: Re-enable when GuildResources is available
        /*
        private GuildResources CalculateResourceProduction()
        {
            var totalProduction = new GuildResources();
            
            // TODO: Implement building-based resource production
            // Currently returns base production values
            totalProduction.gold = 5.0f;
            totalProduction.wood = 3.0f;
            totalProduction.stone = 2.0f;
            totalProduction.manaStone = 1.0f;
            
            return totalProduction;
        }
        */
        
        /// <summary>
        /// 건물 건설
        /// </summary>
        // TODO: Re-enable when GuildBuildingTemp is available
        /*
        public bool ConstructBuilding(string buildingType, Vector2Int position, bool isInstant = false)
        {
            // TODO: Implement proper building construction logic
            var newBuilding = new GuildBuildingTemp
            {
                buildingType = buildingType,
                position = position,
                level = 1,
                isConstructed = isInstant,
                isUpgrading = false,
                size = new Vector2Int(2, 2)
            };
            
            buildings.Add(newBuilding);
            if (isInstant)
            {
                OnBuildingConstructed?.Invoke(newBuilding);
            }
            return true;
        }
        */
        
        /// <summary>
        /// 건물 업그레이드
        /// </summary>
        // TODO: Re-enable when GuildBuildingTemp is available
        /*
        public bool UpgradeBuilding(GuildBuildingTemp building)
        {
            // TODO: Implement proper building upgrade logic
            if (building != null && building.isConstructed && !building.isUpgrading)
            {
                building.isUpgrading = true;
                building.level++;
                OnBuildingUpgraded?.Invoke(building);
                return true;
            }
            return false;
        }
        */
        
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
            
            // TODO: Re-enable when CanAfford and SpendResources are available
            /*
            if (!CanAfford(research.cost))
            {
                Debug.LogWarning("연구 비용이 부족합니다!");
                return false;
            }
            
            SpendResources(research.cost);
            */
            
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
            // TODO: Implement when building system is ready
            // This method will check for building construction and upgrade completion
        }
        
        /// <summary>
        /// 자원 소모 가능 여부 체크
        /// </summary>
        // TODO: Re-enable when GuildResources is available
        /*
        private bool CanAfford(GuildResources cost)
        {
            return resources.gold >= cost.gold &&
                   resources.wood >= cost.wood &&
                   resources.stone >= cost.stone &&
                   resources.manaStone >= cost.manaStone;
        }
        */
        
        /// <summary>
        /// 자원 소모
        /// </summary>
        // TODO: Re-enable when GuildResources is available
        /*
        private void SpendResources(GuildResources cost)
        {
            resources.gold -= cost.gold;
            resources.wood -= cost.wood;
            resources.stone -= cost.stone;
            resources.manaStone -= cost.manaStone;
            
            OnResourcesChanged?.Invoke(resources);
        }
        */
        
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
            // 임시로 GuildResources 없이 연구 데이터 초기화
            availableResearch.AddRange(new List<ResearchData>
            {
                new ResearchData { id = "R1", name = "기초 전술", description = "전투력 +10%", requiredLevel = 1, researchTime = 300, isUnlocked = true, isCompleted = false, progress = 0f },
                new ResearchData { id = "R2", name = "자원 효율", description = "자원 생산 +20%", requiredLevel = 2, researchTime = 500, isUnlocked = true, isCompleted = false, progress = 0f },
                new ResearchData { id = "R3", name = "건설 기술", description = "건설 시간 -25%", requiredLevel = 3, researchTime = 400, isUnlocked = true, isCompleted = false, progress = 0f },
                new ResearchData { id = "R4", name = "고급 무기술", description = "물리 공격력 +15%", requiredLevel = 4, researchTime = 800, isUnlocked = true, isCompleted = false, progress = 0f },
                new ResearchData { id = "R5", name = "마법 연구", description = "마법 공격력 +20%", requiredLevel = 5, researchTime = 600, isUnlocked = true, isCompleted = false, progress = 0f },
                new ResearchData { id = "R6", name = "길드 확장", description = "멤버 수 +5명", requiredLevel = 6, researchTime = 1000, isUnlocked = true, isCompleted = false, progress = 0f }
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
        // TODO: Re-enable when GuildResources and GuildBuildingTemp are available
        /*
        public GuildResources GetResources() => resources;
        public List<GuildBuildingTemp> GetBuildings() => buildings;
        */
        public List<ResearchData> GetAvailableResearch() => availableResearch;
        public List<ResearchData> GetCompletedResearch() => completedResearch;
        public ResearchData GetCurrentResearch() => currentResearch;
        public string GetGuildName() => guildName;
        public int GetGuildLevel() => guildLevel;
        public int GetGuildReputation() => guildReputation;
        public int GetMemberCapacity() => guildMemberCapacity;
        
        // 편의 메서드들
        // TODO: Re-enable when GuildBuildingTemp is available
        /*
        public GuildBuildingTemp GetBuilding(string type) => buildings.FirstOrDefault(b => b.buildingType == type);
        public int GetBuildingCount(string type) => buildings.Count(b => b.buildingType == type && b.isConstructed);
        public bool HasBuilding(string type) => buildings.Any(b => b.buildingType == type && b.isConstructed);
        */
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
        /// 길드 데이터 저장
        /// </summary>
        public void SaveGuildData()
        {
            PlayerPrefs.SetString("GuildName", guildName);
            PlayerPrefs.SetInt("GuildLevel", guildLevel);
            PlayerPrefs.SetInt("GuildReputation", guildReputation);
            // TODO: Re-enable when GuildResources is available
            /*
            PlayerPrefs.SetFloat("GuildGold", resources.gold);
            PlayerPrefs.SetFloat("GuildWood", resources.wood);
            PlayerPrefs.SetFloat("GuildStone", resources.stone);
            PlayerPrefs.SetFloat("GuildManaStone", resources.manaStone);
            */
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
            // TODO: Re-enable when GuildResources is available
            /*
            resources.gold = PlayerPrefs.GetFloat("GuildGold", 1000);
            resources.wood = PlayerPrefs.GetFloat("GuildWood", 500);
            resources.stone = PlayerPrefs.GetFloat("GuildStone", 300);
            resources.manaStone = PlayerPrefs.GetFloat("GuildManaStone", 100);
            */
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
            // TODO: Re-enable when GuildResources is available
            /*
            resources.gold = 1000;
            resources.wood = 500;
            resources.stone = 300;
            resources.manaStone = 100;
            */
            // TODO: Re-enable when building system is implemented
            // buildings.Clear();
            completedResearch.Clear();
            currentResearch = null;
            Debug.Log("길드 데이터 리셋 완료");
        }
        
        // Temporary building data method removed - no longer needed
    }
    
    // GuildResources는 별도 파일로 분리됨
    
    /// <summary>
    /// 임시 ResearchData 클래스 - GuildResources 없이 컴파일 가능하도록
    /// </summary>
    [System.Serializable]
    public class ResearchData
    {
        public string id;
        public string name;
        public string description;
        public int requiredLevel;
        public float researchTime;
        // public GuildResources cost; // TODO: Re-enable when GuildResources is available
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
    
    // TODO: Original ResearchData with GuildResources - Re-enable when GuildResources is available
    /*
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
    */
}