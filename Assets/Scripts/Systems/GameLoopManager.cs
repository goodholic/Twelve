using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;
using GuildMaster.Battle;
using GuildMaster.UI;
using GuildMaster.Data;
using GuildMaster.Exploration;
using GuildMaster.Guild;


namespace GuildMaster.Systems
{
    /// <summary>
    /// 게임 루프 관리자
    /// 모든 시스템을 통합하고 게임 진행을 관리
    /// </summary>
    public class GameLoopManager : MonoBehaviour
    {
        private static GameLoopManager instance;
        public static GameLoopManager Instance => instance;
        
        [Header("Game Flow")]
        [SerializeField] private float dayDuration = 300f; // 5 minutes per day
        [SerializeField] private bool pauseOnUI = true;
        [SerializeField] private bool autoSaveEnabled = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        
        [Header("Daily Events")]
        [SerializeField] private int dailyBattleLimit = 5;
        [SerializeField] private int dailyDungeonLimit = 3;
        [SerializeField] private int merchantVisitChance = 30; // 30% chance per day
        
        [Header("Season Settings")]
        [SerializeField] private int daysPerSeason = 30;
        [SerializeField] private bool enableSeasonalEvents = true;
        
        // System references
        private GameManager gameManager;
        private SaveManager saveManager;
        private GuildManager guildManager;
        private ResourceManager resourceManager;
        private BattleManager battleManager;
        private DungeonSystem dungeonSystem;
        private StoryManager storyManager;
        private AchievementSystem achievementSystem;
        private TutorialSystem tutorialSystem;
        private MonoBehaviour productionSystem; // ResourceProductionSystem 대신 MonoBehaviour로 변경
        private MonoBehaviour guildBattleSystem; // AIGuildBattleSystem 대신 MonoBehaviour로 변경
        private BattleSimulationSystem simulationSystem;
        
        // Game state
        private GameState currentState = GameState.MainMenu;
        private int currentDay = 1;
        private int currentSeason = 0;
        private float currentDayTime = 0f;
        private bool isPaused = false;
        private float gameSpeed = 1f;
        private float lastAutoSaveTime = 0f;
        
        // Daily tracking
        private DailyProgress todayProgress;
        private List<DailyEvent> todayEvents;
        private Queue<GameEvent> eventQueue;
        
        // Events
        public event Action<int> OnDayChanged;
        public event Action<int> OnSeasonChanged;
        public event Action<GameState> OnGameStateChanged;
        public event Action<DailyEvent> OnDailyEventTriggered;
        public event Action<float> OnDayProgressChanged;
        
        public enum GameState
        {
            MainMenu,
            Playing,
            Paused,
            Battle,
            Story,
            GameOver,
            Victory
        }
        
        public enum TimeOfDay
        {
            Morning,    // 0-25%
            Afternoon,  // 25-50%
            Evening,    // 50-75%
            Night       // 75-100%
        }
        
        public enum Season
        {
            Spring,
            Summer,
            Autumn,
            Winter
        }
        
        [System.Serializable]
        public class DailyProgress
        {
            public int day;
            public int battlesCompleted;
            public int dungeonsCompleted;
            public int questsCompleted;
            public int merchantsVisited;
            public float resourcesGathered;
            public List<string> completedEvents;
            
            public DailyProgress(int day)
            {
                this.day = day;
                completedEvents = new List<string>();
            }
        }
        
        [System.Serializable]
        public class DailyEvent
        {
            public string eventId;
            public string eventName;
            public EventType type;
            public string description;
            public float triggerTime;
            public Dictionary<string, object> parameters;
            public bool isCompleted;
            
            public enum EventType
            {
                MerchantVisit,
                GuildChallenge,
                StoryEvent,
                SpecialDungeon,
                ResourceBonus,
                RandomEncounter,
                SeasonalEvent
            }
        }
        
        [System.Serializable]
        public class GameEvent
        {
            public string eventId;
            public Action eventAction;
            public float delay;
            public bool isImmediate;
        }
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                eventQueue = new Queue<GameEvent>();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            InitializeReferences();
            InitializeGameLoop();
        }
        
        void InitializeReferences()
        {
            gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                saveManager = gameManager.SaveManager;
                guildManager = gameManager.GuildManager;
                resourceManager = gameManager.ResourceManager;
                battleManager = gameManager.BattleManager;
            }
            
            dungeonSystem = DungeonSystem.Instance;
            storyManager = FindObjectOfType<StoryManager>();
            achievementSystem = FindObjectOfType<AchievementSystem>();
            tutorialSystem = FindObjectOfType<TutorialSystem>();
            productionSystem = GameObject.FindObjectOfType(System.Type.GetType("GuildMaster.Systems.ResourceProductionSystem")) as MonoBehaviour;
            guildBattleSystem = GameObject.FindObjectOfType(System.Type.GetType("GuildMaster.Systems.AIGuildBattleSystem")) as MonoBehaviour;
            simulationSystem = BattleSimulationSystem.Instance;
        }
        
        void InitializeGameLoop()
        {
            // Load saved game state if exists
            LoadGameState();
            
            // Initialize daily progress
            todayProgress = new DailyProgress(currentDay);
            todayEvents = new List<DailyEvent>();
            
            // Start the game loop
            if (currentState == GameState.Playing)
            {
                StartCoroutine(GameLoopCoroutine());
                StartCoroutine(EventProcessingCoroutine());
            }
        }
        
        void Update()
        {
            if (currentState != GameState.Playing || isPaused) return;
            
            // Update day time
            currentDayTime += Time.deltaTime * gameSpeed / dayDuration;
            
            if (currentDayTime >= 1f)
            {
                EndDay();
            }
            
            OnDayProgressChanged?.Invoke(currentDayTime);
            
            // Auto save
            if (autoSaveEnabled && Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                AutoSave();
                lastAutoSaveTime = Time.time;
            }
            
            // Process time-based events
            ProcessTimeBasedEvents();
        }
        
        IEnumerator GameLoopCoroutine()
        {
            while (currentState != GameState.GameOver && currentState != GameState.Victory)
            {
                yield return null;
                
                // Check win/lose conditions
                CheckGameEndConditions();
                
                // Process queued events
                while (eventQueue.Count > 0 && !isPaused)
                {
                    var gameEvent = eventQueue.Dequeue();
                    
                    if (gameEvent.delay > 0 && !gameEvent.isImmediate)
                    {
                        yield return new WaitForSeconds(gameEvent.delay);
                    }
                    
                    gameEvent.eventAction?.Invoke();
                }
            }
        }
        
        IEnumerator EventProcessingCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                if (currentState == GameState.Playing && !isPaused)
                {
                    // Process recurring systems
                    UpdateProductionSystem();
                    UpdateMerchantSystem();
                    CheckDailyQuests();
                    UpdateGuildRelations();
                }
            }
        }
        
        void ProcessTimeBasedEvents()
        {
            var timeOfDay = GetTimeOfDay();
            
            // Morning events
            if (timeOfDay == TimeOfDay.Morning && !todayProgress.completedEvents.Contains("morning"))
            {
                ProcessMorningEvents();
                todayProgress.completedEvents.Add("morning");
            }
            
            // Afternoon events
            else if (timeOfDay == TimeOfDay.Afternoon && !todayProgress.completedEvents.Contains("afternoon"))
            {
                ProcessAfternoonEvents();
                todayProgress.completedEvents.Add("afternoon");
            }
            
            // Evening events
            else if (timeOfDay == TimeOfDay.Evening && !todayProgress.completedEvents.Contains("evening"))
            {
                ProcessEveningEvents();
                todayProgress.completedEvents.Add("evening");
            }
            
            // Night events
            else if (timeOfDay == TimeOfDay.Night && !todayProgress.completedEvents.Contains("night"))
            {
                ProcessNightEvents();
                todayProgress.completedEvents.Add("night");
            }
        }
        
        void ProcessMorningEvents()
        {
            // Morning production bonus
            if (productionSystem != null)
            {
                var productionComponent = productionSystem.GetComponent<ResourceProductionSystem>();
                if (productionComponent != null)
                {
                    // 아침 보너스 효과 (임시 구현)
                    if (resourceManager != null)
                    {
                        resourceManager.AddGold(100);
                        ShowNotification("아침 생산 보너스를 받았습니다!", GuildMaster.Data.NotificationType.Success);
                    }
                }
            }
            
            // Daily quest refresh
            if (achievementSystem != null)
            {
                achievementSystem.RefreshDailyQuests();
            }
            
            // Morning notification
            if (currentDay > 1)
            {
                ShowNotification("새로운 날이 시작되었습니다!", GuildMaster.Data.NotificationType.Info);
            }
            
            currentDayTime = 0.25f; // Morning ends
        }
        
        void ProcessAfternoonEvents()
        {
            // Merchant arrival chance
            if (UnityEngine.Random.Range(0, 100) < merchantVisitChance)
            {
                TriggerMerchantVisit();
            }
            
            // Guild challenge chance
            if (UnityEngine.Random.Range(0, 100) < 20)
            {
                TriggerGuildChallenge();
            }
        }
        
        void ProcessEveningEvents()
        {
            // Special dungeon availability
            if (dungeonSystem != null && UnityEngine.Random.Range(0, 100) < 15)
            {
                var specialDungeon = new DailyEvent
                {
                    eventId = $"special_dungeon_{currentDay}",
                    eventName = "특별 던전 출현",
                    type = DailyEvent.EventType.SpecialDungeon,
                    description = "한정 시간 동안 특별한 던전이 열렸습니다!",
                    triggerTime = currentDayTime
                };
                
                TriggerDailyEvent(specialDungeon);
            }
        }
        
        void ProcessNightEvents()
        {
            // Night production bonus
            if (productionSystem != null)
            {
                // 밤 보너스 효과 (임시 구현)
                if (resourceManager != null)
                {
                    resourceManager.AddGold(50);
                    ShowNotification("야간 생산 보너스를 받았습니다!", GuildMaster.Data.NotificationType.Info);
                }
            }
            
            // Story events
            if (storyManager != null && UnityEngine.Random.Range(0, 100) < 10)
            {
                TriggerStoryEvent();
            }
        }
        
        void EndDay()
        {
            currentDay++;
            currentDayTime = 0f;
            
            // Process end of day
            ProcessEndOfDay();
            
            // Check season change
            if (currentDay % daysPerSeason == 0)
            {
                ChangeSeason();
            }
            
            // Reset daily progress
            todayProgress = new DailyProgress(currentDay);
            todayEvents.Clear();
            
            // Trigger day change event
            OnDayChanged?.Invoke(currentDay);
            
            // Daily report
            ShowDailyReport();
        }
        
        void ProcessEndOfDay()
        {
            // Calculate daily income
            if (resourceManager != null && productionSystem != null)
            {
                // 일일 생산량 계산 (임시 구현)
                var dailyGold = 500;
                var dailyWood = 100;
                var dailyStone = 100;
                var dailyManaStone = 50;
                
                resourceManager.AddGold(dailyGold);
                resourceManager.AddWood(dailyWood);
                resourceManager.AddStone(dailyStone);
                resourceManager.AddManaStone(dailyManaStone);
            }
            
            // Guild maintenance costs
            if (guildManager != null)
            {
                int maintenanceCost = CalculateMaintenanceCost();
                if (!resourceManager.SpendGold(maintenanceCost))
                {
                    // Penalty for not paying maintenance
                    guildManager.AddReputation(-10);
                    ShowNotification("유지비를 지불할 수 없습니다! 명성이 감소합니다.", GuildMaster.Data.NotificationType.Warning);
                }
            }
            
            // Update guild relations (임시 구현)
            if (guildManager != null)
            {
                // 길드 관계 업데이트 로직
                ShowNotification("길드 관계가 업데이트되었습니다.", GuildMaster.Data.NotificationType.Info);
            }
            
            // Check achievements
            if (achievementSystem != null)
            {
                achievementSystem.CheckAchievements();
            }
        }
        
        int CalculateMaintenanceCost()
        {
            int baseCost = 100;
            
            if (guildManager != null)
            {
                var guildData = guildManager.GetGuildData();
                baseCost += guildData.GuildLevel * 50;
                baseCost += guildData.Adventurers.Count * 10;
                baseCost += guildManager.GetBuildingsByType(BuildingType.GuildHall).Count * 20;
            }
            
            // Season modifier
            if (GetCurrentSeason() == Season.Winter)
            {
                baseCost = Mathf.RoundToInt(baseCost * 1.5f);
            }
            
            return baseCost;
        }
        
        void ChangeSeason()
        {
            currentSeason = (currentSeason + 1) % 4;
            OnSeasonChanged?.Invoke(currentSeason);
            
            var season = GetCurrentSeason();
            ShowNotification($"{GetSeasonName(season)} 시즌이 시작되었습니다!", GuildMaster.Data.NotificationType.System);
            
            // Apply seasonal effects
            ApplySeasonalEffects(season);
            
            // Trigger seasonal events
            if (enableSeasonalEvents)
            {
                TriggerSeasonalEvent(season);
            }
        }
        
        void ApplySeasonalEffects(Season season)
        {
            switch (season)
            {
                case Season.Spring:
                    if (productionSystem != null)
                    {
                        // productionSystem.SetSeasonalModifier(1.2f);
                        ShowNotification("봄 시즌 생산 보너스가 적용되었습니다!", GuildMaster.Data.NotificationType.Info);
                    }
                    break;
                    
                case Season.Summer:
                    if (battleManager != null)
                    {
                        // Apply summer combat bonuses
                        ShowNotification("여름 시즌 전투 보너스가 적용되었습니다!", GuildMaster.Data.NotificationType.Info);
                    }
                    break;
                    
                case Season.Autumn:
                    if (resourceManager != null)
                    {
                        resourceManager.AddGold(1000);
                        resourceManager.AddWood(500);
                        ShowNotification("가을 수확 보너스를 받았습니다!", GuildMaster.Data.NotificationType.Success);
                    }
                    break;
                    
                case Season.Winter:
                    // Increased costs
                    if (productionSystem != null)
                    {
                        // productionSystem.SetSeasonalModifier(0.8f);
                        ShowNotification("겨울 시즌 효과가 적용되었습니다!", GuildMaster.Data.NotificationType.Warning);
                    }
                    break;
            }
        }
        
        void TriggerSeasonalEvent(Season season)
        {
            var seasonalEvent = new DailyEvent
            {
                eventId = $"seasonal_{season}_{currentDay}",
                eventName = $"{GetSeasonName(season)} 축제",
                type = DailyEvent.EventType.SeasonalEvent,
                description = $"{GetSeasonName(season)} 시즌을 기념하는 특별 이벤트!",
                triggerTime = currentDayTime
            };
            
            TriggerDailyEvent(seasonalEvent);
        }
        
        void ShowDailyReport()
        {
            string report = $"=== {currentDay}일차 일일 보고서 ===\n";
            report += $"전투: {todayProgress.battlesCompleted}/{dailyBattleLimit}\n";
            report += $"던전: {todayProgress.dungeonsCompleted}/{dailyDungeonLimit}\n";
            report += $"퀘스트: {todayProgress.questsCompleted}\n";
            report += $"방문 상인: {todayProgress.merchantsVisited}\n";
            
            Debug.Log(report);
            
            // Show UI notification
            // TODO: Implement daily report UI
        }
        
        void CheckGameEndConditions()
        {
            // Victory conditions
            if (CheckVictoryConditions())
            {
                TriggerVictory();
                return;
            }
            
            // Defeat conditions
            if (CheckDefeatConditions())
            {
                TriggerGameOver();
                return;
            }
        }
        
        bool CheckVictoryConditions()
        {
            if (guildManager == null) return false;
            
            var guildData = guildManager.GetGuildData();
            
            // Victory condition 1: Reach maximum guild level
            if (guildData.GuildLevel >= 50)
            {
                return true;
            }
            
            // Victory condition 2: Complete all main story chapters
            if (storyManager != null && storyManager.GetCompletedMainChapterCount() >= 10)
            {
                return true;
            }
            
            // Special victory condition: Rank 1 in guild battles
            if (guildBattleSystem != null)
            {
                // guildBattleSystem.GetPlayerRanking() == 1
                // 임시 구현: 랭킹 1위 체크는 별도 시스템에서 처리
                // return true;
            }
            
            return false;
        }
        
        bool CheckDefeatConditions()
        {
            if (guildManager == null || resourceManager == null) return false;
            
            var guildData = guildManager.GetGuildData();
            
            // Defeat condition 1: Bankruptcy
            if (resourceManager.GetGold() < -1000)
            {
                return true;
            }
            
            // Defeat condition 2: No adventurers
            if (guildData.Adventurers.Count == 0 && currentDay > 7)
            {
                return true;
            }
            
            // Defeat condition 3: Zero reputation
            if (guildData.GuildReputation <= 0)
            {
                return true;
            }
            
            return false;
        }
        
        void TriggerVictory()
        {
            ChangeGameState(GameState.Victory);
            
            // Calculate final score
            int finalScore = CalculateFinalScore();
            
            // Save victory data
            SaveVictoryData(finalScore);
            
            // Show victory screen
            ShowVictoryScreen(finalScore);
        }
        
        void TriggerGameOver()
        {
            ChangeGameState(GameState.GameOver);
            
            // Save game over data
            SaveGameOverData();
            
            // Show game over screen
            ShowGameOverScreen();
        }
        
        int CalculateFinalScore()
        {
            int score = 0;
            
            if (guildManager != null)
            {
                var guildData = guildManager.GetGuildData();
                score += guildData.GuildLevel * 1000;
                score += guildData.GuildReputation * 10;
                score += guildData.Adventurers.Count * 100;
            }
            
            if (resourceManager != null)
            {
                score += resourceManager.GetGold();
                score += resourceManager.GetWood() * 2;
                score += resourceManager.GetStone() * 2;
                score += resourceManager.GetManaStone() * 5;
            }
            
            score += currentDay * 50;
            
            return score;
        }
        
        void UpdateProductionSystem()
        {
            if (productionSystem == null) return;
            
            // Production system updates itself
            // We just need to check for special conditions
            
            // Weekend bonus
            if (currentDay % 7 == 0 || currentDay % 7 == 6)
            {
                // productionSystem.ApplyWeekendBonus();
                ShowNotification("주말 생산 보너스가 적용되었습니다!", GuildMaster.Data.NotificationType.Info);
            }
        }
        
        void UpdateMerchantSystem()
        {
            // Merchant system manages itself through MerchantManager
            // We can add additional logic here if needed
        }
        
        void CheckDailyQuests()
        {
            if (achievementSystem == null) return;
            
            // Check quest completion
            achievementSystem.UpdateDailyProgress("battles", todayProgress.battlesCompleted);
            achievementSystem.UpdateDailyProgress("dungeons", todayProgress.dungeonsCompleted);
            achievementSystem.UpdateDailyProgress("resources", (int)todayProgress.resourcesGathered);
        }
        
        void UpdateGuildRelations()
        {
            if (guildBattleSystem == null) return;
            
            // Periodic relation updates
            if (currentDay % 3 == 0)
            {
                // guildBattleSystem.UpdateGuildRelations();
                ShowNotification("길드 관계가 업데이트되었습니다.", GuildMaster.Data.NotificationType.Info);
            }
        }
        
        void TriggerMerchantVisit()
        {
            var merchantEvent = new DailyEvent
            {
                eventId = $"merchant_{currentDay}_{UnityEngine.Random.Range(0, 1000)}",
                eventName = "상인 방문",
                type = DailyEvent.EventType.MerchantVisit,
                description = "특별한 상인이 길드를 방문했습니다!",
                triggerTime = currentDayTime
            };
            
            TriggerDailyEvent(merchantEvent);
            todayProgress.merchantsVisited++;
        }
        
        void TriggerGuildChallenge()
        {
            if (todayProgress.battlesCompleted >= dailyBattleLimit) return;
            
            var challengeEvent = new DailyEvent
            {
                eventId = $"challenge_{currentDay}_{UnityEngine.Random.Range(0, 1000)}",
                eventName = "길드 도전",
                type = DailyEvent.EventType.GuildChallenge,
                description = "다른 길드가 대전을 신청했습니다!",
                triggerTime = currentDayTime
            };
            
            TriggerDailyEvent(challengeEvent);
        }
        
        void TriggerStoryEvent()
        {
            var storyEvent = new DailyEvent
            {
                eventId = $"story_{currentDay}_{UnityEngine.Random.Range(0, 1000)}",
                eventName = "스토리 이벤트",
                type = DailyEvent.EventType.StoryEvent,
                description = "특별한 이야기가 펼쳐집니다...",
                triggerTime = currentDayTime
            };
            
            TriggerDailyEvent(storyEvent);
        }
        
        void TriggerDailyEvent(DailyEvent dailyEvent)
        {
            todayEvents.Add(dailyEvent);
            OnDailyEventTriggered?.Invoke(dailyEvent);
            
            // Queue event processing
            QueueEvent(new GameEvent
            {
                eventId = dailyEvent.eventId,
                eventAction = () => ProcessDailyEvent(dailyEvent),
                delay = 0f,
                isImmediate = false
            });
        }
        
        void ProcessDailyEvent(DailyEvent dailyEvent)
        {
            switch (dailyEvent.type)
            {
                case DailyEvent.EventType.MerchantVisit:
                    // Merchant system handles this
                    ShowNotification(dailyEvent.description, GuildMaster.Data.NotificationType.Info);
                    break;
                    
                case DailyEvent.EventType.GuildChallenge:
                    if (guildBattleSystem != null)
                    {
                        // guildBattleSystem.TriggerRandomChallenge();
                        ShowNotification("길드 도전이 시작되었습니다!", GuildMaster.Data.NotificationType.System);
                    }
                    break;
                    
                case DailyEvent.EventType.StoryEvent:
                    if (storyManager != null)
                    {
                        // storyManager.TriggerRandomEvent();
                    }
                    break;
                    
                case DailyEvent.EventType.SpecialDungeon:
                    if (dungeonSystem != null)
                    {
                        // Unlock special dungeon temporarily
                        ShowNotification(dailyEvent.description, GuildMaster.Data.NotificationType.System);
                    }
                    break;
                    
                case DailyEvent.EventType.ResourceBonus:
                    int bonusAmount = UnityEngine.Random.Range(100, 500);
                    resourceManager.AddGold(bonusAmount);
                    ShowNotification($"보너스 골드 +{bonusAmount}!", GuildMaster.Data.NotificationType.Success);
                    break;
                    
                case DailyEvent.EventType.SeasonalEvent:
                    ProcessSeasonalEvent(dailyEvent);
                    break;
            }
            
            dailyEvent.isCompleted = true;
        }
        
        void ProcessSeasonalEvent(DailyEvent seasonalEvent)
        {
            var season = GetCurrentSeason();
            
            switch (season)
            {
                case Season.Spring:
                    // Spring festival - growth bonus
                    if (guildManager != null)
                    {
                        foreach (var adventurer in guildManager.GetGuildData().Adventurers)
                        {
                            ((GuildMaster.Battle.Unit)adventurer).AddExperience(100);
                        }
                        ShowNotification("봄 축제! 모든 모험가가 경험치를 획득했습니다!", GuildMaster.Data.NotificationType.Reward);
                    }
                    break;
                    
                case Season.Summer:
                    // Summer tournament
                    if (guildBattleSystem != null)
                    {
                        // guildBattleSystem.StartTournament();
                        ShowNotification("여름 토너먼트가 시작되었습니다!", GuildMaster.Data.NotificationType.Important);
                    }
                    break;
                    
                case Season.Autumn:
                    // Harvest festival - resource bonus
                    resourceManager.AddGold(2000);
                    resourceManager.AddWood(1000);
                    resourceManager.AddStone(1000);
                    ShowNotification("가을 수확제! 대량의 자원을 획득했습니다!", GuildMaster.Data.NotificationType.Reward);
                    break;
                    
                case Season.Winter:
                    // Winter challenge - survival mode
                    ShowNotification("겨울 도전! 유지비가 증가합니다!", GuildMaster.Data.NotificationType.Warning);
                    break;
            }
        }
        
        public void QueueEvent(GameEvent gameEvent)
        {
            eventQueue.Enqueue(gameEvent);
        }
        
        public void ChangeGameState(GameState newState)
        {
            var previousState = currentState;
            currentState = newState;
            
            OnGameStateChanged?.Invoke(newState);
            
            // Handle state transitions
            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    isPaused = false;
                    break;
                    
                case GameState.Playing:
                    Time.timeScale = gameSpeed;
                    isPaused = false;
                    break;
                    
                case GameState.Paused:
                    Time.timeScale = 0f;
                    isPaused = true;
                    break;
                    
                case GameState.Battle:
                    if (pauseOnUI)
                    {
                        Time.timeScale = 0f;
                        isPaused = true;
                    }
                    break;
                    
                case GameState.Story:
                    if (pauseOnUI)
                    {
                        Time.timeScale = 0f;
                        isPaused = true;
                    }
                    break;
                    
                case GameState.GameOver:
                case GameState.Victory:
                    Time.timeScale = 0f;
                    isPaused = true;
                    StopAllCoroutines();
                    break;
            }
        }
        
        public void SetGameSpeed(float speed)
        {
            gameSpeed = Mathf.Clamp(speed, 0.1f, 4f);
            
            if (currentState == GameState.Playing && !isPaused)
            {
                Time.timeScale = gameSpeed;
            }
        }
        
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeGameState(GameState.Paused);
            }
        }
        
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeGameState(GameState.Playing);
            }
        }
        
        public void StartNewGame()
        {
            // Reset game state
            currentDay = 1;
            currentSeason = 0;
            currentDayTime = 0f;
            todayProgress = new DailyProgress(1);
            todayEvents.Clear();
            eventQueue.Clear();
            
            // Initialize systems
            if (tutorialSystem != null)
            {
                tutorialSystem.StartTutorial("beginner_tutorial");
            }
            
            ChangeGameState(GameState.Playing);
            
            // Start game loop coroutines
            StartCoroutine(GameLoopCoroutine());
            StartCoroutine(EventProcessingCoroutine());
        }
        
        public void LoadGame(int slotIndex)
        {
            if (saveManager != null)
            {
                var saveData = saveManager.LoadGame(slotIndex);
                if (saveData != null)
                {
                    ApplySaveData(saveData);
                    ChangeGameState(GameState.Playing);
                    
                    // Resume game loop
                    StartCoroutine(GameLoopCoroutine());
                    StartCoroutine(EventProcessingCoroutine());
                }
            }
        }
        
        void ApplySaveData(GuildMaster.Core.SaveData saveData)
        {
            currentDay = saveData.currentDay;
            currentSeason = saveData.currentSeason;
            currentDayTime = saveData.dayProgress;
            
            // Apply other save data to systems
            // This would be implemented based on SaveData structure
        }
        
        void AutoSave()
        {
            if (saveManager != null && currentState == GameState.Playing)
            {
                saveManager.SaveGame(0); // Auto save to slot 0
                ShowNotification("자동 저장 완료", GuildMaster.Data.NotificationType.Info);
            }
        }
        
        void SaveGameState()
        {
            PlayerPrefs.SetInt("CurrentDay", currentDay);
            PlayerPrefs.SetInt("CurrentSeason", currentSeason);
            PlayerPrefs.SetFloat("CurrentDayTime", currentDayTime);
            PlayerPrefs.SetString("CurrentState", currentState.ToString());
            PlayerPrefs.Save();
        }
        
        void LoadGameState()
        {
            currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
            currentSeason = PlayerPrefs.GetInt("CurrentSeason", 0);
            currentDayTime = PlayerPrefs.GetFloat("CurrentDayTime", 0f);
            
            string savedState = PlayerPrefs.GetString("CurrentState", "MainMenu");
            if (Enum.TryParse<GameState>(savedState, out GameState state))
            {
                currentState = state;
            }
        }
        
        void SaveVictoryData(int score)
        {
            PlayerPrefs.SetInt("VictoryScore", score);
            PlayerPrefs.SetInt("VictoryDay", currentDay);
            PlayerPrefs.SetString("VictoryDate", DateTime.Now.ToString());
            
            // Update high score
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            if (score > highScore)
            {
                PlayerPrefs.SetInt("HighScore", score);
                PlayerPrefs.SetString("HighScoreDate", DateTime.Now.ToString());
            }
            
            PlayerPrefs.Save();
        }
        
        void SaveGameOverData()
        {
            PlayerPrefs.SetInt("GameOverDay", currentDay);
            PlayerPrefs.SetString("GameOverDate", DateTime.Now.ToString());
            PlayerPrefs.Save();
        }
        
        void ShowVictoryScreen(int score)
        {
            Debug.Log($"Victory! Final Score: {score}");
            // TODO: Implement victory UI
        }
        
        void ShowGameOverScreen()
        {
            Debug.Log($"Game Over on day {currentDay}");
            // TODO: Implement game over UI
        }
        
        void ShowNotification(string message, GuildMaster.Data.NotificationType type)
        {
            Debug.Log($"[{type}] {message}");
            // TODO: Implement notification UI
        }
        
        // Public getters
        public GameState CurrentState => currentState;
        public int CurrentDay => currentDay;
        public Season GetCurrentSeason() => (Season)currentSeason;
        public TimeOfDay GetTimeOfDay()
        {
            if (currentDayTime < 0.25f) return TimeOfDay.Morning;
            if (currentDayTime < 0.5f) return TimeOfDay.Afternoon;
            if (currentDayTime < 0.75f) return TimeOfDay.Evening;
            return TimeOfDay.Night;
        }
        public float GetDayProgress() => currentDayTime;
        public DailyProgress GetTodayProgress() => todayProgress;
        public List<DailyEvent> GetTodayEvents() => new List<DailyEvent>(todayEvents);
        
        string GetSeasonName(Season season)
        {
            return season switch
            {
                Season.Spring => "봄",
                Season.Summer => "여름",
                Season.Autumn => "가을",
                Season.Winter => "겨울",
                _ => season.ToString()
            };
        }
        
        // Battle tracking
        public void OnBattleCompleted(bool victory)
        {
            if (todayProgress != null)
            {
                todayProgress.battlesCompleted++;
                
                if (victory && achievementSystem != null)
                {
                    achievementSystem.UnlockAchievement("daily_battle_victory");
                }
            }
        }
        
        // Dungeon tracking
        public void OnDungeonCompleted(bool success)
        {
            if (todayProgress != null)
            {
                todayProgress.dungeonsCompleted++;
                
                if (success && achievementSystem != null)
                {
                    achievementSystem.UnlockAchievement("daily_dungeon_clear");
                }
            }
        }
        
        // Quest tracking
        public void OnQuestCompleted(string questId)
        {
            if (todayProgress != null)
            {
                todayProgress.questsCompleted++;
            }
        }
        
        // Resource tracking
        public void OnResourcesGathered(float amount)
        {
            if (todayProgress != null)
            {
                todayProgress.resourcesGathered += amount;
            }
        }
    }
}