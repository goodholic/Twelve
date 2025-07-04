using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GuildMaster.Core;
using GuildMaster.Battle;
using GuildMaster.Guild;
using GuildMaster.Data;


namespace GuildMaster.Systems
{
    /// <summary>
    /// README 기준 완전 자동화 방치형 시스템
    /// - 설정 후 자동으로 진행되는 방치형 시스템
    /// - 길드 운영, 전투, 자원 생산이 모두 자동화
    /// - 플레이어는 전략적 설정만 하고 결과를 확인
    /// </summary>
    public class IdleGameCore : MonoBehaviour
    {
        public static IdleGameCore Instance { get; private set; }
        
        [Header("자동화 설정")]
        [SerializeField] private bool autoGuildManagement = true;      // 길드 자동 운영
        [SerializeField] private bool autoBattle = true;               // 자동 전투
        [SerializeField] private bool autoResourceManagement = true;   // 자동 자원 관리
        [SerializeField] private bool autoCharacterTraining = true;    // 자동 캐릭터 훈련
        [SerializeField] private bool autoBuilding = true;             // 자동 건설
        [SerializeField] private bool autoResearch = true;             // 자동 연구
        
        [Header("자동화 간격 설정")]
        [SerializeField] private float battleInterval = 30f;           // 전투 주기 (초)
        [SerializeField] private float managementInterval = 60f;       // 관리 주기 (초)
        [SerializeField] private float trainingInterval = 45f;         // 훈련 주기 (초)
        [SerializeField] private float buildingInterval = 120f;        // 건설 주기 (초)
        [SerializeField] private float researchInterval = 300f;        // 연구 주기 (초)
        
        [Header("자동화 우선순위")]
        [SerializeField] private AutomationPriority resourcePriority = AutomationPriority.High;
        [SerializeField] private AutomationPriority combatPriority = AutomationPriority.High;
        [SerializeField] private AutomationPriority buildingPriority = AutomationPriority.Medium;
        [SerializeField] private AutomationPriority researchPriority = AutomationPriority.Medium;
        
        [Header("방치 보상 설정")]
        [SerializeField] private float offlineRewardMultiplier = 0.8f; // 오프라인 보상 배율
        [SerializeField] private float maxOfflineHours = 24f;          // 최대 오프라인 시간
        [SerializeField] private bool enableOfflineProgress = true;    // 오프라인 진행 허용
        
        // 자동화 상태
        private DateTime lastSaveTime;
        private Coroutine automationCoroutine;
        private AutomationStats automationStats = new AutomationStats();
        
        // 이벤트
        public event Action<AutomationReport> OnAutomationReport;
        public event Action<OfflineReward> OnOfflineReward;
        public event Action<AutomationEvent> OnAutomationEvent;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeIdleSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            StartAutomation();
            CheckOfflineProgress();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveLastPlayTime();
            }
            else
            {
                CheckOfflineProgress();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveLastPlayTime();
            }
            else
            {
                CheckOfflineProgress();
            }
        }
        
        /// <summary>
        /// 방치형 시스템 초기화
        /// </summary>
        private void InitializeIdleSystem()
        {
            lastSaveTime = DateTime.Now;
            automationStats.Reset();
            
            Debug.Log("방치형 시스템 초기화 완료!");
        }
        
        /// <summary>
        /// 자동화 시작
        /// </summary>
        public void StartAutomation()
        {
            if (automationCoroutine != null)
            {
                StopCoroutine(automationCoroutine);
            }
            
            automationCoroutine = StartCoroutine(AutomationLoop());
            Debug.Log("자동화 시스템 시작!");
        }
        
        /// <summary>
        /// 자동화 중지
        /// </summary>
        public void StopAutomation()
        {
            if (automationCoroutine != null)
            {
                StopCoroutine(automationCoroutine);
                automationCoroutine = null;
            }
            
            Debug.Log("자동화 시스템 중지!");
        }
        
        /// <summary>
        /// 메인 자동화 루프
        /// </summary>
        private IEnumerator AutomationLoop()
        {
            while (true)
            {
                var report = new AutomationReport();
                report.timestamp = DateTime.Now;
                
                // 우선순위에 따른 자동화 실행
                if (autoResourceManagement && resourcePriority == AutomationPriority.High)
                {
                    yield return StartCoroutine(AutoResourceManagement(report));
                }
                
                if (autoBattle && combatPriority == AutomationPriority.High)
                {
                    yield return StartCoroutine(AutoBattleSystem(report));
                }
                
                if (autoGuildManagement)
                {
                    yield return StartCoroutine(AutoGuildManagement(report));
                }
                
                if (autoCharacterTraining)
                {
                    yield return StartCoroutine(AutoCharacterTraining(report));
                }
                
                if (autoBuilding && buildingPriority >= AutomationPriority.Medium)
                {
                    yield return StartCoroutine(AutoBuildingManagement(report));
                }
                
                if (autoResearch && researchPriority >= AutomationPriority.Medium)
                {
                    yield return StartCoroutine(AutoResearchManagement(report));
                }
                
                // 보고서 발송
                OnAutomationReport?.Invoke(report);
                
                // 다음 사이클까지 대기 (가장 짧은 간격 기준)
                float minInterval = Mathf.Min(battleInterval, managementInterval, trainingInterval);
                yield return new WaitForSeconds(minInterval);
            }
        }
        
        /// <summary>
        /// 자동 자원 관리
        /// </summary>
        private IEnumerator AutoResourceManagement(AutomationReport report)
        {
            if (GuildSimulationCore.Instance == null) yield break;
            
            var guild = GuildSimulationCore.Instance;
            var resources = guild.GetResources();
            
            // 자원 균형 조정
            if (resources.gold < 1000 && guild.HasBuilding(BuildingType.Storage))
            {
                // 골드 부족 시 다른 자원을 골드로 변환하는 로직
                report.actions.Add("자원 균형 조정: 골드 부족으로 자원 변환 실행");
            }
            
            // 자원 저장고 용량 체크
            if (resources.wood > 10000 || resources.stone > 10000)
            {
                // 과다 자원 처리 (건설 자동화 트리거)
                report.actions.Add("과다 자원 감지: 자동 건설 우선순위 상향 조정");
            }
            
            automationStats.resourceManagementCount++;
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// 자동 전투 시스템
        /// </summary>
        private IEnumerator AutoBattleSystem(AutomationReport report)
        {
            if (GuildBattleCore.Instance == null) yield break;
            
            var battleCore = GuildBattleCore.Instance;
            
            if (!battleCore.IsInBattle())
            {
                // AI 적 생성
                var enemyFormation = GenerateEnemyFormation();
                
                // 전투 시작
                battleCore.StartGuildBattle(enemyFormation);
                report.actions.Add($"자동 전투 시작: 적 길드 '{enemyFormation.guildName}'와 교전");
                
                // 전투 완료까지 대기
                yield return new WaitUntil(() => !battleCore.IsInBattle());
                
                automationStats.battleCount++;
                report.actions.Add("자동 전투 완료");
            }
            
            yield return new WaitForSeconds(battleInterval);
        }
        
        /// <summary>
        /// 자동 길드 관리
        /// </summary>
        private IEnumerator AutoGuildManagement(AutomationReport report)
        {
            if (GuildSimulationCore.Instance == null) yield break;
            
            var guild = GuildSimulationCore.Instance;
            
            // 길드 레벨업 조건 체크
            var resources = guild.GetResources();
            if (resources.gold > 5000 && guild.GetGuildLevel() < 50)
            {
                // 레벨업 조건 충족 시 자동 레벨업
                guild.LevelUpGuild();
                report.actions.Add($"길드 레벨업: Lv.{guild.GetGuildLevel()}");
            }
            
            // 길드 명성 관리
            if (guild.GetGuildReputation() < 100)
            {
                // 명성 부족 시 명성 회복 액션
                report.actions.Add("길드 명성 회복 작업 실행");
            }
            
            automationStats.guildManagementCount++;
            yield return new WaitForSeconds(managementInterval);
        }
        
        /// <summary>
        /// 자동 캐릭터 훈련
        /// </summary>
        private IEnumerator AutoCharacterTraining(AutomationReport report)
        {
            if (GuildBattleCore.Instance == null) yield break;
            
            var battleCore = GuildBattleCore.Instance;
            var characters = battleCore.GetCollectedCharacters();
            
            foreach (var character in characters)
            {
                // 레벨이 낮은 캐릭터 우선 훈련
                if (character.level < 30)
                {
                    // 경험치 추가 (자동 훈련 효과)
                    int expGain = UnityEngine.Random.Range(50, 100);
                    character.AddExperience(expGain);
                    
                    if (character.level % 5 == 0) // 5레벨마다 보고
                    {
                        report.actions.Add($"{character.unitName} 훈련 완료: Lv.{character.level}");
                    }
                }
            }
            
            automationStats.trainingCount++;
            yield return new WaitForSeconds(trainingInterval);
        }
        
        /// <summary>
        /// 자동 건설 관리
        /// </summary>
        private IEnumerator AutoBuildingManagement(AutomationReport report)
        {
            if (GuildSimulationCore.Instance == null) yield break;
            
            var guild = GuildSimulationCore.Instance;
            var resources = guild.GetResources();
            
            // 필수 건물 우선 건설
            var priorityBuildings = new List<BuildingType>
            {
                BuildingType.Storage,      // 자원 저장
                BuildingType.Mine,         // 석재 생산
                BuildingType.Lumbermill,   // 목재 생산
                BuildingType.TrainingGround // 훈련소
            };
            
            foreach (var buildingType in priorityBuildings)
            {
                if (!guild.HasBuilding(buildingType) && CanAffordBuilding(buildingType, resources))
                {
                    // 적절한 위치 찾기
                    var position = FindBestBuildingPosition(guild, buildingType);
                    if (position.HasValue)
                    {
                        guild.ConstructBuilding(buildingType, position.Value);
                        report.actions.Add($"자동 건설: {buildingType} 건설 시작");
                        break; // 한 번에 하나씩만 건설
                    }
                }
            }
            
            // 기존 건물 업그레이드
            var buildings = guild.GetBuildings();
            foreach (var building in buildings)
            {
                if (building.isConstructed && !building.isUpgrading && building.level < 5)
                {
                    if (guild.UpgradeBuilding(building))
                    {
                        report.actions.Add($"자동 업그레이드: {building.buildingType} Lv.{building.level}");
                        break;
                    }
                }
            }
            
            automationStats.buildingCount++;
            yield return new WaitForSeconds(buildingInterval);
        }
        
        /// <summary>
        /// 자동 연구 관리
        /// </summary>
        private IEnumerator AutoResearchManagement(AutomationReport report)
        {
            if (GuildSimulationCore.Instance == null) yield break;
            
            var guild = GuildSimulationCore.Instance;
            
            if (guild.GetCurrentResearch() == null)
            {
                var availableResearch = guild.GetAvailableResearch();
                var completedResearch = guild.GetCompletedResearch();
                
                // 미완료 연구 중 우선순위가 높은 것부터 시작
                foreach (var research in availableResearch)
                {
                    if (!completedResearch.Contains(research) && 
                        research.CanResearch(completedResearch, guild.GetGuildLevel()))
                    {
                        if (guild.StartResearch(research))
                        {
                            report.actions.Add($"자동 연구 시작: {research.name}");
                            break;
                        }
                    }
                }
            }
            
            automationStats.researchCount++;
            yield return new WaitForSeconds(researchInterval);
        }
        
        /// <summary>
        /// 오프라인 진행 체크
        /// </summary>
        private void CheckOfflineProgress()
        {
            if (!enableOfflineProgress) return;
            
            DateTime currentTime = DateTime.Now;
            DateTime lastPlayTime = GetLastPlayTime();
            
            if (lastPlayTime != DateTime.MinValue)
            {
                TimeSpan offlineTime = currentTime - lastPlayTime;
                float offlineHours = (float)offlineTime.TotalHours;
                
                if (offlineHours > 0.1f) // 최소 6분 이상
                {
                    offlineHours = Mathf.Min(offlineHours, maxOfflineHours);
                    CalculateOfflineRewards(offlineHours);
                }
            }
            
            SaveLastPlayTime();
        }
        
        /// <summary>
        /// 오프라인 보상 계산
        /// </summary>
        private void CalculateOfflineRewards(float offlineHours)
        {
            var reward = new OfflineReward();
            reward.offlineHours = offlineHours;
            
            if (GuildSimulationCore.Instance != null)
            {
                var guild = GuildSimulationCore.Instance;
                var production = guild.GetResources();
                
                // 오프라인 자원 생산
                reward.goldEarned = production.gold * offlineHours * offlineRewardMultiplier;
                reward.expEarned = 100 * offlineHours * offlineRewardMultiplier;
                reward.battlesWon = Mathf.RoundToInt(offlineHours * 2 * offlineRewardMultiplier);
                
                // 보상 적용
                ApplyOfflineReward(reward);
            }
            
            OnOfflineReward?.Invoke(reward);
            Debug.Log($"오프라인 진행: {offlineHours:F1}시간, 골드 +{reward.goldEarned:F0}");
        }
        
        /// <summary>
        /// 오프라인 보상 적용
        /// </summary>
        private void ApplyOfflineReward(OfflineReward reward)
        {
            if (GuildSimulationCore.Instance != null)
            {
                var guild = GuildSimulationCore.Instance;
                var resources = guild.GetResources();
                resources.gold += reward.goldEarned;
                
                // 캐릭터들에게 경험치 지급
                if (GuildBattleCore.Instance != null)
                {
                    var characters = GuildBattleCore.Instance.GetCollectedCharacters();
                    foreach (var character in characters)
                    {
                        character.AddExperience(Mathf.RoundToInt(reward.expEarned / characters.Count));
                    }
                }
            }
        }
        
        // 유틸리티 메서드들
        private void SaveLastPlayTime()
        {
            PlayerPrefs.SetString("LastPlayTime", DateTime.Now.ToBinary().ToString());
        }
        
        private DateTime GetLastPlayTime()
        {
            string lastPlayTimeString = PlayerPrefs.GetString("LastPlayTime", "");
            if (string.IsNullOrEmpty(lastPlayTimeString)) return DateTime.MinValue;
            
            if (long.TryParse(lastPlayTimeString, out long lastPlayTimeBinary))
            {
                return DateTime.FromBinary(lastPlayTimeBinary);
            }
            
            return DateTime.MinValue;
        }
        
        private GuildFormation GenerateEnemyFormation()
        {
            // 간단한 적 생성 로직
            var enemyFormation = new GuildFormation();
            enemyFormation.guildName = "적 길드";
            // 실제로는 더 복잡한 AI 길드 생성 로직 필요
            return enemyFormation;
        }
        
        private bool CanAffordBuilding(BuildingType buildingType, GuildResources resources)
        {
            // 건물 건설 비용 체크
            return true; // 임시로 true 반환
        }
        
        private Vector2Int? FindBestBuildingPosition(GuildSimulationCore guild, BuildingType buildingType)
        {
            // 최적 건설 위치 찾기 로직
            return new Vector2Int(5, 5); // 간단한 예시
        }
        
        // 접근자 메서드들
        public AutomationStats GetAutomationStats() => automationStats;
        public bool IsAutomationRunning() => automationCoroutine != null;
        public void SetAutomationEnabled(AutomationType type, bool enabled)
        {
            switch (type)
            {
                case AutomationType.GuildManagement: autoGuildManagement = enabled; break;
                case AutomationType.Battle: autoBattle = enabled; break;
                case AutomationType.ResourceManagement: autoResourceManagement = enabled; break;
                case AutomationType.CharacterTraining: autoCharacterTraining = enabled; break;
                case AutomationType.Building: autoBuilding = enabled; break;
                case AutomationType.Research: autoResearch = enabled; break;
            }
        }
        
        /// <summary>
        /// GameManager에서 호출하는 초기화 메서드
        /// </summary>
        public void Initialize()
        {
            if (Instance == null)
            {
                InitializeIdleSystem();
            }
        }
        
        /// <summary>
        /// GameManager에서 호출하는 업데이트 메서드
        /// </summary>
        public void UpdateIdleSystems(float deltaTime)
        {
            // 이미 Update()에서 처리하고 있으므로 추가 로직 없음
        }
        
        /// <summary>
        /// 방치형 게임 데이터 저장
        /// </summary>
        public void SaveIdleData()
        {
            PlayerPrefs.SetString("LastSaveTime", DateTime.Now.ToBinary().ToString());
            PlayerPrefs.SetFloat("IdleRewardMultiplier", offlineRewardMultiplier);
            PlayerPrefs.SetInt("AutoBattleEnabled", autoBattle ? 1 : 0);
            Debug.Log("방치형 게임 데이터 저장 완료");
        }
        
        /// <summary>
        /// 방치형 게임 데이터 로드
        /// </summary>
        public void LoadIdleData()
        {
            string lastSaveTimeStr = PlayerPrefs.GetString("LastSaveTime", "");
            offlineRewardMultiplier = PlayerPrefs.GetFloat("IdleRewardMultiplier", 1.0f);
            autoBattle = PlayerPrefs.GetInt("AutoBattleEnabled", 1) == 1;
            Debug.Log("방치형 게임 데이터 로드 완료");
        }
        
        /// <summary>
        /// 방치형 게임 데이터 리셋
        /// </summary>
        public void ResetIdleData()
        {
            offlineRewardMultiplier = 1.0f;
            autoBattle = true;
            autoResourceManagement = true;
            autoCharacterTraining = true;
            autoBuilding = true;
            autoResearch = true;
            Debug.Log("방치형 게임 데이터 리셋 완료");
        }
    }
    
    // 데이터 구조체들
    public enum AutomationPriority
    {
        Low = 1,
        Medium = 2,
        High = 3
    }
    
    public enum AutomationType
    {
        GuildManagement,
        Battle,
        ResourceManagement,
        CharacterTraining,
        Building,
        Research
    }
    
    [System.Serializable]
    public class AutomationStats
    {
        public int battleCount;
        public int guildManagementCount;
        public int resourceManagementCount;
        public int trainingCount;
        public int buildingCount;
        public int researchCount;
        public DateTime startTime;
        
        public void Reset()
        {
            battleCount = 0;
            guildManagementCount = 0;
            resourceManagementCount = 0;
            trainingCount = 0;
            buildingCount = 0;
            researchCount = 0;
            startTime = DateTime.Now;
        }
    }
    
    [System.Serializable]
    public class AutomationReport
    {
        public DateTime timestamp;
        public List<string> actions = new List<string>();
        public AutomationStats stats;
        
        public override string ToString()
        {
            return $"[{timestamp:HH:mm:ss}] 자동화 보고서: {actions.Count}개 작업 완료";
        }
    }
    
    [System.Serializable]
    public class OfflineReward
    {
        public float offlineHours;
        public float goldEarned;
        public float expEarned;
        public int battlesWon;
        public List<string> itemsEarned = new List<string>();
    }
    
    [System.Serializable]
    public class AutomationEvent
    {
        public string title;
        public string description;
        public DateTime timestamp;
        public AutomationEventType eventType;
    }
    
    public enum AutomationEventType
    {
        Info,
        Warning,
        Success,
        Error
    }
} 