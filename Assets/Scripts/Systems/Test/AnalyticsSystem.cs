using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using GuildMaster.Core;
using GuildMaster.Data;
using GuildMaster.Battle;
using GuildMaster.Guild;
using JobClass = GuildMaster.Battle.JobClass;
using Unit = GuildMaster.Battle.Unit;
using Rarity = GuildMaster.Data.Rarity;
// using ResourceType = GuildMaster.Core.ResourceType; // ResourceType removed

namespace GuildMaster.Systems
{
    public class AnalyticsSystem : MonoBehaviour
    {
        private static AnalyticsSystem _instance;
        public static AnalyticsSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AnalyticsSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AnalyticsSystem");
                        _instance = go.AddComponent<AnalyticsSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #region Data Structures

        // Session Data
        [System.Serializable]
        public class SessionData
        {
            public string sessionId;
            public DateTime startTime;
            public DateTime endTime;
            public float totalPlayTime;
            public int actionsPerformed;
            public bool didCrash;
            public Dictionary<string, float> timeSpentByState = new Dictionary<string, float>();
        }

        // Player Behavior Data
        [System.Serializable]
        public class PlayerBehaviorData
        {
            // Play Time
            public float totalPlayTime;
            public float averageSessionLength;
            public int totalSessions;
            public Dictionary<DayOfWeek, float> playTimeByDay = new Dictionary<DayOfWeek, float>();
            public Dictionary<int, float> playTimeByHour = new Dictionary<int, float>();
            
            // Actions
            public Dictionary<string, int> actionCounts = new Dictionary<string, int>();
            public List<string> mostFrequentActions = new List<string>();
            
            // Menu Navigation
            public Dictionary<string, int> screenVisits = new Dictionary<string, int>();
            public Dictionary<string, float> screenDurations = new Dictionary<string, float>();
            
            // Feature Usage
            public Dictionary<string, float> featureEngagement = new Dictionary<string, float>();
        }

        // Resource Analytics
        [System.Serializable]
        public class ResourceAnalytics
        {
            // Earning Patterns
            // ResourceType changed to string
            public Dictionary<string, float> totalEarned = new Dictionary<string, float>();
            public Dictionary<string, float> averageEarningRate = new Dictionary<string, float>();
            public Dictionary<string, Dictionary<string, float>> earningBySources = new Dictionary<string, Dictionary<string, float>>();
            
            // Spending Patterns
            public Dictionary<string, float> totalSpent = new Dictionary<string, float>();
            public Dictionary<string, Dictionary<string, float>> spendingByCategory = new Dictionary<string, Dictionary<string, float>>();
            
            // Balance Tracking
            public Dictionary<string, List<float>> balanceHistory = new Dictionary<string, List<float>>();
            public Dictionary<string, float> peakBalance = new Dictionary<string, float>();
            public Dictionary<string, float> lowestBalance = new Dictionary<string, float>();
            
            // Economic Health
            public float inflationRate;
            public float resourceVelocity;
            public Dictionary<string, float> resourceScarcity = new Dictionary<string, float>();
        }

        // Building Analytics
        [System.Serializable]
        public class BuildingAnalytics
        {
            public Dictionary<string, int> buildingCounts = new Dictionary<string, int>(); // Changed BuildingType to string
            public Dictionary<string, float> averageLevels = new Dictionary<string, float>(); // Changed BuildingType to string
            public List<string> constructionOrder = new List<string>(); // Changed BuildingType to string
            public Dictionary<string, float> timeToConstruct = new Dictionary<string, float>(); // Changed BuildingType to string
            public Dictionary<string, int> upgradeFrequency = new Dictionary<string, int>(); // Changed BuildingType to string
            public float averageConstructionTime;
            public int totalBuildings;
            public int totalUpgrades;
        }

        // Battle Analytics
        [System.Serializable]
        public class BattleAnalytics
        {
            // Overall Stats
            public int totalBattles;
            public int victories;
            public int defeats;
            public int draws;
            public float winRate;
            public float averageBattleDuration;
            
            // Damage and Healing
            public long totalDamageDealt;
            public long totalDamageTaken;
            public long totalHealingDone;
            public float damagePerBattle;
            public float healingPerBattle;
            
            // Unit Performance
            public Dictionary<string, int> unitUsageCount = new Dictionary<string, int>();
            public Dictionary<string, float> unitWinRates = new Dictionary<string, float>();
            public Dictionary<string, float> unitDamageContribution = new Dictionary<string, float>();
            public Dictionary<JobClass, float> classWinRates = new Dictionary<JobClass, float>();
            
            // Squad Analytics
            public Dictionary<string, int> squadCompositions = new Dictionary<string, int>();
            public List<string> mostSuccessfulSquads = new List<string>();
            
            // Skill Usage
            public Dictionary<string, int> skillUsageCounts = new Dictionary<string, int>();
            public Dictionary<string, float> skillEffectiveness = new Dictionary<string, float>();
            
            // Enemy Analytics
            public Dictionary<string, int> enemiesDefeated = new Dictionary<string, int>();
            public Dictionary<string, float> difficultyWinRates = new Dictionary<string, float>();
        }

        // Character Analytics
        [System.Serializable]
        public class CharacterAnalytics
        {
            public Dictionary<string, CharacterStats> characterStats = new Dictionary<string, CharacterStats>();
            public Dictionary<JobClass, int> classCounts = new Dictionary<JobClass, int>();
            public Dictionary<Rarity, int> rarityCounts = new Dictionary<Rarity, int>();
            public float averageLevel;
            public float averageAwakenLevel;
            public List<string> mostUsedCharacters = new List<string>();
            public List<string> highestLevelCharacters = new List<string>();
            
            [System.Serializable]
            public class CharacterStats
            {
                public string characterId;
                public string characterName;
                public JobClass jobClass;
                public int level;
                public int awakenLevel;
                public float totalDamageDealt;
                public float totalHealingDone;
                public int battlesParticipated;
                public int victories;
                public float experienceGained;
                public DateTime lastUsed;
            }
        }

        // Performance Metrics
        [System.Serializable]
        public class PerformanceMetrics
        {
            // Frame Rate
            public float averageFPS;
            public float minFPS;
            public float maxFPS;
            public List<float> fpsHistory = new List<float>();
            public int frameDropEvents;
            
            // Memory Usage
            public float averageMemoryUsage;
            public float peakMemoryUsage;
            public List<float> memoryHistory = new List<float>();
            public int memoryWarnings;
            
            // Load Times
            public Dictionary<string, float> sceneLoadTimes = new Dictionary<string, float>();
            public float averageLoadTime;
            public float longestLoadTime;
            
            // Errors and Crashes
            public List<ErrorLog> errorLogs = new List<ErrorLog>();
            public int totalErrors;
            public int totalCrashes;
            public Dictionary<string, int> errorFrequency = new Dictionary<string, int>();
            
            [System.Serializable]
            public class ErrorLog
            {
                public DateTime timestamp;
                public string errorType;
                public string message;
                public string stackTrace;
                public string gameState;
            }
        }

        // Progression Analytics
        [System.Serializable]
        public class ProgressionAnalytics
        {
            // Level Progression
            public int currentGuildLevel;
            public float timeToReachLevel;
            public Dictionary<int, float> levelProgressionSpeed = new Dictionary<int, float>();
            public List<DateTime> levelUpTimestamps = new List<DateTime>();
            
            // Content Completion
            public Dictionary<string, bool> contentCompleted = new Dictionary<string, bool>();
            public float overallCompletionRate;
            public Dictionary<string, float> categoryCompletionRates = new Dictionary<string, float>();
            
            // Achievement Progress
            public Dictionary<string, AchievementProgress> achievements = new Dictionary<string, AchievementProgress>();
            public int totalAchievements;
            public int unlockedAchievements;
            public float achievementCompletionRate;
            
            // Daily Progress
            public Dictionary<DateTime, DailyProgress> dailyProgress = new Dictionary<DateTime, DailyProgress>();
            
            [System.Serializable]
            public class AchievementProgress
            {
                public string achievementId;
                public string achievementName;
                public bool isUnlocked;
                public float progress;
                public DateTime unlockedDate;
                public int attempts;
            }
            
            [System.Serializable]
            public class DailyProgress
            {
                public DateTime date;
                public int questsCompleted;
                public float resourcesEarned;
                public int battlesWon;
                public int buildingsConstructed;
                public float playTime;
            }
        }

        #endregion

        #region Private Variables

        // Analytics Data
        private SessionData currentSession;
        private PlayerBehaviorData playerBehavior;
        private ResourceAnalytics resourceAnalytics;
        private BuildingAnalytics buildingAnalytics;
        private BattleAnalytics battleAnalytics;
        private CharacterAnalytics characterAnalytics;
        private PerformanceMetrics performanceMetrics;
        private ProgressionAnalytics progressionAnalytics;

        // Tracking Variables
        private float lastFPSUpdate;
        private float fpsUpdateInterval = 1f;
        private int frameCount;
        private float deltaTime;
        
        private float lastMemoryCheck;
        private float memoryCheckInterval = 5f;
        
        private float lastAnalyticsSave;
        private float analyticsSaveInterval = 60f; // Save every minute
        
        private Dictionary<string, float> screenStartTimes = new Dictionary<string, float>();
        private string currentScreen = "";
        
        // Performance Settings
        [Header("Analytics Settings")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enablePerformanceTracking = true;
        [SerializeField] private bool enableDetailedLogging = false;
        [SerializeField] private bool enablePrivacyMode = false;
        [SerializeField] private int maxHistorySize = 1000;
        [SerializeField] private int maxErrorLogs = 100;

        // Export Settings
        [Header("Export Settings")]
        [SerializeField] private string exportDirectory = "Analytics";
        [SerializeField] private bool exportJSON = true;
        [SerializeField] private bool exportCSV = true;
        [SerializeField] private bool compressExports = true;

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeAnalytics();
        }

        void Start()
        {
            if (enableAnalytics)
            {
                StartSession();
                SubscribeToEvents();
                StartCoroutine(AnalyticsUpdateCoroutine());
                
                if (enablePerformanceTracking)
                {
                    StartCoroutine(PerformanceTrackingCoroutine());
                }
            }
        }

        void Update()
        {
            if (!enableAnalytics) return;
            
            // Track FPS
            if (enablePerformanceTracking)
            {
                frameCount++;
                deltaTime += Time.unscaledDeltaTime;
                
                if (Time.time - lastFPSUpdate >= fpsUpdateInterval)
                {
                    UpdateFPS();
                }
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (enableAnalytics)
            {
                if (pauseStatus)
                {
                    PauseSession();
                }
                else
                {
                    ResumeSession();
                }
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (enableAnalytics && !hasFocus)
            {
                SaveAnalytics();
            }
        }

        void OnDestroy()
        {
            if (enableAnalytics)
            {
                EndSession();
                SaveAnalytics();
                UnsubscribeFromEvents();
            }
        }

        #endregion

        #region Initialization

        void InitializeAnalytics()
        {
            // Initialize all analytics data structures
            playerBehavior = new PlayerBehaviorData();
            resourceAnalytics = new ResourceAnalytics();
            buildingAnalytics = new BuildingAnalytics();
            battleAnalytics = new BattleAnalytics();
            characterAnalytics = new CharacterAnalytics();
            performanceMetrics = new PerformanceMetrics();
            progressionAnalytics = new ProgressionAnalytics();
            
            // Initialize dictionaries
            InitializeResourceAnalytics();
            InitializeBattleAnalytics();
            InitializeCharacterAnalytics();
            
            // Load previous analytics data if exists
            LoadAnalytics();
        }

        void InitializeResourceAnalytics()
        {
            // Initialize with known resource types as strings
            string[] resourceTypes = { "Gold", "Wood", "Stone", "ManaStone" };
            foreach (string type in resourceTypes)
            {
                resourceAnalytics.totalEarned[type] = 0;
                resourceAnalytics.totalSpent[type] = 0;
                resourceAnalytics.averageEarningRate[type] = 0;
                resourceAnalytics.earningBySources[type] = new Dictionary<string, float>();
                resourceAnalytics.spendingByCategory[type] = new Dictionary<string, float>();
                resourceAnalytics.balanceHistory[type] = new List<float>();
                resourceAnalytics.peakBalance[type] = 0;
                resourceAnalytics.lowestBalance[type] = float.MaxValue;
                resourceAnalytics.resourceScarcity[type] = 0;
            }
        }

        void InitializeBattleAnalytics()
        {
            foreach (JobClass jobClass in Enum.GetValues(typeof(JobClass)))
            {
                battleAnalytics.classWinRates[jobClass] = 0;
            }
        }

        void InitializeCharacterAnalytics()
        {
            foreach (JobClass jobClass in Enum.GetValues(typeof(JobClass)))
            {
                characterAnalytics.classCounts[jobClass] = 0;
            }
            
            foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
            {
                characterAnalytics.rarityCounts[rarity] = 0;
            }
        }

        #endregion

        #region Session Management

        void StartSession()
        {
            currentSession = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = DateTime.Now,
                totalPlayTime = 0,
                actionsPerformed = 0,
                didCrash = false
            };
            
            playerBehavior.totalSessions++;
            
            LogAnalyticsEvent("Session Started", new Dictionary<string, object>
            {
                { "sessionId", currentSession.sessionId },
                { "startTime", currentSession.startTime.ToString() }
            });
        }

        void EndSession()
        {
            if (currentSession != null)
            {
                currentSession.endTime = DateTime.Now;
                currentSession.totalPlayTime = (float)(currentSession.endTime - currentSession.startTime).TotalSeconds;
                
                playerBehavior.totalPlayTime += currentSession.totalPlayTime;
                playerBehavior.averageSessionLength = playerBehavior.totalPlayTime / playerBehavior.totalSessions;
                
                // Update play time by day and hour
                var dayOfWeek = currentSession.startTime.DayOfWeek;
                if (!playerBehavior.playTimeByDay.ContainsKey(dayOfWeek))
                    playerBehavior.playTimeByDay[dayOfWeek] = 0;
                playerBehavior.playTimeByDay[dayOfWeek] += currentSession.totalPlayTime;
                
                var hour = currentSession.startTime.Hour;
                if (!playerBehavior.playTimeByHour.ContainsKey(hour))
                    playerBehavior.playTimeByHour[hour] = 0;
                playerBehavior.playTimeByHour[hour] += currentSession.totalPlayTime;
                
                LogAnalyticsEvent("Session Ended", new Dictionary<string, object>
                {
                    { "sessionId", currentSession.sessionId },
                    { "duration", currentSession.totalPlayTime },
                    { "actions", currentSession.actionsPerformed }
                });
            }
        }

        void PauseSession()
        {
            if (currentSession != null)
            {
                SaveAnalytics();
            }
        }

        void ResumeSession()
        {
            // Session continues
        }

        #endregion

        #region Event Subscriptions

        void SubscribeToEvents()
        {
            // Game Manager Events
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
                gameManager.OnGameInitialized += OnGameInitialized;
            }
            
            // Event Manager Events
            var eventManager = EventManager.Instance;
            if (eventManager != null)
            {
                // Resource Events
                eventManager.Subscribe(GuildMaster.Core.EventType.ResourceChanged, OnResourceChanged);
                eventManager.Subscribe(GuildMaster.Core.EventType.ResourceCapacityReached, OnResourceCapacityReached);
                
                // Guild Events
                eventManager.Subscribe(GuildMaster.Core.EventType.GuildLevelUp, OnGuildLevelUp);
                eventManager.Subscribe(GuildMaster.Core.EventType.NewAdventurer, OnNewAdventurer);
                eventManager.Subscribe(GuildMaster.Core.EventType.AdventurerLevelUp, OnAdventurerLevelUp);
                
                // Building Events
                eventManager.Subscribe(GuildMaster.Core.EventType.BuildingConstructed, OnBuildingConstructed);
                eventManager.Subscribe(GuildMaster.Core.EventType.BuildingUpgraded, OnBuildingUpgraded);
                eventManager.Subscribe(GuildMaster.Core.EventType.BuildingProductionComplete, OnBuildingProductionComplete);
                
                // Battle Events
                eventManager.Subscribe(GuildMaster.Core.EventType.BattleStarted, OnBattleStarted);
                eventManager.Subscribe(GuildMaster.Core.EventType.BattleEnded, OnBattleEnded);
                eventManager.Subscribe(GuildMaster.Core.EventType.BattleVictory, OnBattleVictory);
                eventManager.Subscribe(GuildMaster.Core.EventType.BattleDefeat, OnBattleDefeat);
                eventManager.Subscribe(GuildMaster.Core.EventType.UnitKilled, OnUnitKilled);
                
                // Quest Events
                eventManager.Subscribe(GuildMaster.Core.EventType.QuestCompleted, OnQuestCompleted);
                eventManager.Subscribe(GuildMaster.Core.EventType.AchievementUnlocked, OnAchievementUnlocked);
                
                // UI Events
                eventManager.Subscribe(GuildMaster.Core.EventType.ScreenOpened, OnScreenOpened);
                eventManager.Subscribe(GuildMaster.Core.EventType.ScreenClosed, OnScreenClosed);
            }
            
            // Resource Manager Events - Commented out - ResourceManager removed
            // var resourceManager = ResourceManager.Instance;
            // if (resourceManager != null)
            // {
            //     resourceManager.OnResourcesChanged += OnResourcesChanged;
            // }
            
            // Battle Manager Events
            var battleManager = gameManager?.BattleManager;
            if (battleManager != null)
            {
                battleManager.OnUnitAttack += OnUnitAttack;
                battleManager.OnUnitHeal += OnUnitHeal;
                battleManager.OnUnitDeath += OnUnitDeath;
            }
            
            // Application Events
            Application.logMessageReceived += OnLogMessageReceived;
        }

        void UnsubscribeFromEvents()
        {
            // Unsubscribe from all events
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= OnGameStateChanged;
                gameManager.OnGameInitialized -= OnGameInitialized;
            }
            
            // Unsubscribe from EventManager events
            var eventManager = EventManager.Instance;
            if (eventManager != null)
            {
                // Unsubscribe all event types
                foreach (GuildMaster.Core.EventType eventType in Enum.GetValues(typeof(GuildMaster.Core.EventType)))
                {
                    eventManager.Unsubscribe(eventType, GetEventHandler(eventType));
                }
            }
            
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        Action<GameEvent> GetEventHandler(GuildMaster.Core.EventType eventType)
        {
            return eventType switch
            {
                GuildMaster.Core.EventType.ResourceChanged => OnResourceChanged,
                GuildMaster.Core.EventType.ResourceCapacityReached => OnResourceCapacityReached,
                GuildMaster.Core.EventType.GuildLevelUp => OnGuildLevelUp,
                GuildMaster.Core.EventType.NewAdventurer => OnNewAdventurer,
                GuildMaster.Core.EventType.AdventurerLevelUp => OnAdventurerLevelUp,
                GuildMaster.Core.EventType.BuildingConstructed => OnBuildingConstructed,
                GuildMaster.Core.EventType.BuildingUpgraded => OnBuildingUpgraded,
                GuildMaster.Core.EventType.BuildingProductionComplete => OnBuildingProductionComplete,
                GuildMaster.Core.EventType.BattleStarted => OnBattleStarted,
                GuildMaster.Core.EventType.BattleEnded => OnBattleEnded,
                GuildMaster.Core.EventType.BattleVictory => OnBattleVictory,
                GuildMaster.Core.EventType.BattleDefeat => OnBattleDefeat,
                GuildMaster.Core.EventType.UnitKilled => OnUnitKilled,
                GuildMaster.Core.EventType.QuestCompleted => OnQuestCompleted,
                GuildMaster.Core.EventType.AchievementUnlocked => OnAchievementUnlocked,
                GuildMaster.Core.EventType.ScreenOpened => OnScreenOpened,
                GuildMaster.Core.EventType.ScreenClosed => OnScreenClosed,
                _ => null
            };
        }

        #endregion

        #region Event Handlers

        void OnGameStateChanged(GameManager.GameState previousState, GameManager.GameState newState)
        {
            TrackAction("GameStateChanged", new Dictionary<string, object>
            {
                { "previousState", previousState.ToString() },
                { "newState", newState.ToString() }
            });
            
            // Track time spent in each state
            if (currentSession != null)
            {
                var stateKey = previousState.ToString();
                if (!currentSession.timeSpentByState.ContainsKey(stateKey))
                    currentSession.timeSpentByState[stateKey] = 0;
                
                // Add time spent in previous state
                currentSession.timeSpentByState[stateKey] += Time.time;
            }
        }

        void OnGameInitialized()
        {
            TrackAction("GameInitialized");
        }

        void OnResourceChanged(GameEvent gameEvent)
        {
            // Changed to handle string resource type
            var resourceType = gameEvent.GetParameter<string>("resourceType");
            if (string.IsNullOrEmpty(resourceType)) return;
            
            var amount = gameEvent.GetParameter<int>("amount");
            var total = gameEvent.GetParameter<int>("total");
            
            // Initialize if new resource type
            if (!resourceAnalytics.totalEarned.ContainsKey(resourceType))
            {
                resourceAnalytics.totalEarned[resourceType] = 0;
                resourceAnalytics.totalSpent[resourceType] = 0;
                resourceAnalytics.earningBySources[resourceType] = new Dictionary<string, float>();
                resourceAnalytics.spendingByCategory[resourceType] = new Dictionary<string, float>();
                resourceAnalytics.balanceHistory[resourceType] = new List<float>();
                resourceAnalytics.peakBalance[resourceType] = 0;
                resourceAnalytics.lowestBalance[resourceType] = float.MaxValue;
            }
            
            if (amount > 0)
            {
                resourceAnalytics.totalEarned[resourceType] += amount;
                
                var source = gameEvent.GetParameter<string>("source") ?? "Unknown";
                if (!resourceAnalytics.earningBySources[resourceType].ContainsKey(source))
                    resourceAnalytics.earningBySources[resourceType][source] = 0;
                resourceAnalytics.earningBySources[resourceType][source] += amount;
            }
            else if (amount < 0)
            {
                resourceAnalytics.totalSpent[resourceType] += Math.Abs(amount);
                
                var category = gameEvent.GetParameter<string>("category") ?? "Unknown";
                if (!resourceAnalytics.spendingByCategory[resourceType].ContainsKey(category))
                    resourceAnalytics.spendingByCategory[resourceType][category] = 0;
                resourceAnalytics.spendingByCategory[resourceType][category] += Math.Abs(amount);
            }
            
            // Update balance tracking
            resourceAnalytics.balanceHistory[resourceType].Add(total);
            if (resourceAnalytics.balanceHistory[resourceType].Count > maxHistorySize)
                resourceAnalytics.balanceHistory[resourceType].RemoveAt(0);
            
            resourceAnalytics.peakBalance[resourceType] = Math.Max(resourceAnalytics.peakBalance[resourceType], total);
            resourceAnalytics.lowestBalance[resourceType] = Math.Min(resourceAnalytics.lowestBalance[resourceType], total);
        }

        void OnResourceCapacityReached(GameEvent gameEvent)
        {
            TrackAction("ResourceCapacityReached", new Dictionary<string, object>
            {
                { "resourceType", gameEvent.GetParameter<string>("resourceType") } // Changed to string
            });
        }

        // Commented out - ResourceManager removed
        // void OnResourcesChanged(ResourceManager.Resources resources)
        // {
        //     // Additional resource tracking if needed
        // }

        void OnGuildLevelUp(GameEvent gameEvent)
        {
            var newLevel = gameEvent.GetParameter<int>("newLevel");
            progressionAnalytics.currentGuildLevel = newLevel;
            progressionAnalytics.levelUpTimestamps.Add(DateTime.Now);
            
            if (progressionAnalytics.levelProgressionSpeed.ContainsKey(newLevel - 1))
            {
                var timeSinceLastLevel = DateTime.Now - progressionAnalytics.levelUpTimestamps[progressionAnalytics.levelUpTimestamps.Count - 2];
                progressionAnalytics.levelProgressionSpeed[newLevel] = (float)timeSinceLastLevel.TotalHours;
            }
            
            TrackAction("GuildLevelUp", new Dictionary<string, object>
            {
                { "newLevel", newLevel },
                { "totalPlayTime", playerBehavior.totalPlayTime }
            });
        }

        void OnNewAdventurer(GameEvent gameEvent)
        {
            var adventurer = gameEvent.GetParameter<Unit>("adventurer");
            if (adventurer != null)
            {
                UpdateCharacterAnalytics(adventurer);
                TrackAction("NewAdventurer", new Dictionary<string, object>
                {
                    { "name", adventurer.unitName },
                    { "class", adventurer.jobClass.ToString() },
                    { "rarity", adventurer.rarity.ToString() }
                });
            }
        }

        void OnAdventurerLevelUp(GameEvent gameEvent)
        {
            var adventurer = gameEvent.GetParameter<Unit>("adventurer");
            var newLevel = gameEvent.GetParameter<int>("newLevel");
            
            if (adventurer != null)
            {
                UpdateCharacterAnalytics(adventurer);
                TrackAction("AdventurerLevelUp", new Dictionary<string, object>
                {
                    { "name", adventurer.unitName },
                    { "newLevel", newLevel }
                });
            }
        }

        void OnBuildingConstructed(GameEvent gameEvent)
        {
            var buildingType = gameEvent.GetParameter<string>("buildingType"); // Changed BuildingType to string
            
            if (!buildingAnalytics.buildingCounts.ContainsKey(buildingType))
                buildingAnalytics.buildingCounts[buildingType] = 0;
            buildingAnalytics.buildingCounts[buildingType]++;
            
            buildingAnalytics.constructionOrder.Add(buildingType);
            buildingAnalytics.totalBuildings++;
            
            TrackAction("BuildingConstructed", new Dictionary<string, object>
            {
                { "buildingType", buildingType.ToString() },
                { "totalBuildings", buildingAnalytics.totalBuildings }
            });
        }

        void OnBuildingUpgraded(GameEvent gameEvent)
        {
            var buildingType = gameEvent.GetParameter<string>("buildingType"); // Changed BuildingType to string
            var newLevel = gameEvent.GetParameter<int>("newLevel");
            
            if (!buildingAnalytics.upgradeFrequency.ContainsKey(buildingType))
                buildingAnalytics.upgradeFrequency[buildingType] = 0;
            buildingAnalytics.upgradeFrequency[buildingType]++;
            
            buildingAnalytics.totalUpgrades++;
            
            TrackAction("BuildingUpgraded", new Dictionary<string, object>
            {
                { "buildingType", buildingType.ToString() },
                { "newLevel", newLevel }
            });
        }

        void OnBuildingProductionComplete(GameEvent gameEvent)
        {
            var buildingType = gameEvent.GetParameter<string>("buildingType"); // Changed BuildingType to string
            var productionAmount = gameEvent.GetParameter<float>("amount");
            
            TrackAction("BuildingProduction", new Dictionary<string, object>
            {
                { "buildingType", buildingType.ToString() },
                { "amount", productionAmount }
            });
        }

        void OnBattleStarted(GameEvent gameEvent)
        {
            battleAnalytics.totalBattles++;
            TrackAction("BattleStarted");
        }

        void OnBattleEnded(GameEvent gameEvent)
        {
            var duration = gameEvent.GetParameter<float>("duration");
            battleAnalytics.averageBattleDuration = 
                ((battleAnalytics.averageBattleDuration * (battleAnalytics.totalBattles - 1)) + duration) / battleAnalytics.totalBattles;
            
            TrackAction("BattleEnded", new Dictionary<string, object>
            {
                { "duration", duration }
            });
        }

        void OnBattleVictory(GameEvent gameEvent)
        {
            battleAnalytics.victories++;
            battleAnalytics.winRate = (float)battleAnalytics.victories / battleAnalytics.totalBattles;
            
            var goldReward = gameEvent.GetParameter<int>("goldReward");
            var expReward = gameEvent.GetParameter<int>("expReward");
            
            TrackAction("BattleVictory", new Dictionary<string, object>
            {
                { "goldReward", goldReward },
                { "expReward", expReward }
            });
        }

        void OnBattleDefeat(GameEvent gameEvent)
        {
            battleAnalytics.defeats++;
            battleAnalytics.winRate = (float)battleAnalytics.victories / battleAnalytics.totalBattles;
            
            TrackAction("BattleDefeat");
        }

        void OnUnitKilled(GameEvent gameEvent)
        {
            var unit = gameEvent.GetParameter<Unit>("unit");
            var isPlayerUnit = gameEvent.GetParameter<bool>("isPlayerUnit");
            
            if (!isPlayerUnit)
            {
                var enemyType = unit?.unitName ?? "Unknown";
                if (!battleAnalytics.enemiesDefeated.ContainsKey(enemyType))
                    battleAnalytics.enemiesDefeated[enemyType] = 0;
                battleAnalytics.enemiesDefeated[enemyType]++;
            }
        }

        void OnUnitAttack(Unit attacker, Unit target, float damage)
        {
            battleAnalytics.totalDamageDealt += (long)damage;
            
            if (attacker != null)
            {
                var unitId = attacker.unitId;
                if (!battleAnalytics.unitDamageContribution.ContainsKey(unitId))
                    battleAnalytics.unitDamageContribution[unitId] = 0;
                battleAnalytics.unitDamageContribution[unitId] += damage;
            }
        }

        void OnUnitHeal(Unit healer, float healAmount)
        {
            battleAnalytics.totalHealingDone += (long)healAmount;
        }

        void OnUnitDeath(Unit unit)
        {
            // Track unit deaths
        }

        void OnQuestCompleted(GameEvent gameEvent)
        {
            var questId = gameEvent.GetParameter<string>("questId");
            
            TrackAction("QuestCompleted", new Dictionary<string, object>
            {
                { "questId", questId }
            });
        }

        void OnAchievementUnlocked(GameEvent gameEvent)
        {
            var achievementId = gameEvent.GetParameter<string>("achievementId");
            var achievementName = gameEvent.GetParameter<string>("achievementName");
            
            if (!progressionAnalytics.achievements.ContainsKey(achievementId))
            {
                progressionAnalytics.achievements[achievementId] = new ProgressionAnalytics.AchievementProgress
                {
                    achievementId = achievementId,
                    achievementName = achievementName,
                    isUnlocked = true,
                    unlockedDate = DateTime.Now,
                    progress = 1f
                };
            }
            
            progressionAnalytics.unlockedAchievements++;
            progressionAnalytics.achievementCompletionRate = 
                (float)progressionAnalytics.unlockedAchievements / progressionAnalytics.totalAchievements;
            
            TrackAction("AchievementUnlocked", new Dictionary<string, object>
            {
                { "achievementId", achievementId },
                { "achievementName", achievementName }
            });
        }

        void OnScreenOpened(GameEvent gameEvent)
        {
            var screenName = gameEvent.GetParameter<string>("screenName");
            currentScreen = screenName;
            screenStartTimes[screenName] = Time.time;
            
            if (!playerBehavior.screenVisits.ContainsKey(screenName))
                playerBehavior.screenVisits[screenName] = 0;
            playerBehavior.screenVisits[screenName]++;
            
            TrackAction("ScreenOpened", new Dictionary<string, object>
            {
                { "screenName", screenName }
            });
        }

        void OnScreenClosed(GameEvent gameEvent)
        {
            var screenName = gameEvent.GetParameter<string>("screenName");
            
            if (screenStartTimes.ContainsKey(screenName))
            {
                var duration = Time.time - screenStartTimes[screenName];
                
                if (!playerBehavior.screenDurations.ContainsKey(screenName))
                    playerBehavior.screenDurations[screenName] = 0;
                playerBehavior.screenDurations[screenName] += duration;
                
                screenStartTimes.Remove(screenName);
            }
            
            TrackAction("ScreenClosed", new Dictionary<string, object>
            {
                { "screenName", screenName }
            });
        }

        void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                var errorLog = new PerformanceMetrics.ErrorLog
                {
                    timestamp = DateTime.Now,
                    errorType = type.ToString(),
                    message = logString,
                    stackTrace = stackTrace,
                    gameState = GameManager.Instance?.CurrentState.ToString() ?? "Unknown"
                };
                
                performanceMetrics.errorLogs.Add(errorLog);
                if (performanceMetrics.errorLogs.Count > maxErrorLogs)
                    performanceMetrics.errorLogs.RemoveAt(0);
                
                performanceMetrics.totalErrors++;
                
                if (!performanceMetrics.errorFrequency.ContainsKey(logString))
                    performanceMetrics.errorFrequency[logString] = 0;
                performanceMetrics.errorFrequency[logString]++;
                
                if (type == LogType.Exception)
                {
                    performanceMetrics.totalCrashes++;
                    if (currentSession != null)
                        currentSession.didCrash = true;
                }
            }
        }

        #endregion

        #region Analytics Tracking

        public void TrackAction(string actionName, Dictionary<string, object> parameters = null)
        {
            if (!enableAnalytics) return;
            
            // Update action counts
            if (!playerBehavior.actionCounts.ContainsKey(actionName))
                playerBehavior.actionCounts[actionName] = 0;
            playerBehavior.actionCounts[actionName]++;
            
            if (currentSession != null)
                currentSession.actionsPerformed++;
            
            // Log detailed analytics if enabled
            if (enableDetailedLogging)
            {
                LogAnalyticsEvent(actionName, parameters);
            }
        }

        public void TrackResourceTransaction(string type, int amount, string source, bool isEarning)
        {
            if (!enableAnalytics || string.IsNullOrEmpty(type)) return;
            
            // Initialize if new resource type
            if (!resourceAnalytics.totalEarned.ContainsKey(type))
            {
                resourceAnalytics.totalEarned[type] = 0;
                resourceAnalytics.totalSpent[type] = 0;
                resourceAnalytics.earningBySources[type] = new Dictionary<string, float>();
                resourceAnalytics.spendingByCategory[type] = new Dictionary<string, float>();
            }
            
            if (isEarning)
            {
                resourceAnalytics.totalEarned[type] += amount;
                if (!resourceAnalytics.earningBySources[type].ContainsKey(source))
                    resourceAnalytics.earningBySources[type][source] = 0;
                resourceAnalytics.earningBySources[type][source] += amount;
            }
            else
            {
                resourceAnalytics.totalSpent[type] += amount;
                if (!resourceAnalytics.spendingByCategory[type].ContainsKey(source))
                    resourceAnalytics.spendingByCategory[type][source] = 0;
                resourceAnalytics.spendingByCategory[type][source] += amount;
            }
        }

        public void TrackBattleParticipation(Unit unit, bool isVictory)
        {
            if (!enableAnalytics || unit == null) return;
            
            var unitId = unit.unitId;
            if (!battleAnalytics.unitUsageCount.ContainsKey(unitId))
            {
                battleAnalytics.unitUsageCount[unitId] = 0;
                battleAnalytics.unitWinRates[unitId] = 0;
            }
            
            battleAnalytics.unitUsageCount[unitId]++;
            
            if (isVictory)
            {
                battleAnalytics.unitWinRates[unitId] = 
                    ((battleAnalytics.unitWinRates[unitId] * (battleAnalytics.unitUsageCount[unitId] - 1)) + 1) / 
                    battleAnalytics.unitUsageCount[unitId];
            }
            
            // Update character analytics
            UpdateCharacterAnalytics(unit);
        }

        public void TrackSkillUsage(string skillId, float effectiveness)
        {
            if (!enableAnalytics) return;
            
            if (!battleAnalytics.skillUsageCounts.ContainsKey(skillId))
            {
                battleAnalytics.skillUsageCounts[skillId] = 0;
                battleAnalytics.skillEffectiveness[skillId] = 0;
            }
            
            battleAnalytics.skillUsageCounts[skillId]++;
            battleAnalytics.skillEffectiveness[skillId] = 
                ((battleAnalytics.skillEffectiveness[skillId] * (battleAnalytics.skillUsageCounts[skillId] - 1)) + effectiveness) / 
                battleAnalytics.skillUsageCounts[skillId];
        }

        void UpdateCharacterAnalytics(Unit unit)
        {
            if (unit == null) return;
            
            var characterId = unit.unitId;
            if (!characterAnalytics.characterStats.ContainsKey(characterId))
            {
                characterAnalytics.characterStats[characterId] = new CharacterAnalytics.CharacterStats
                {
                    characterId = characterId,
                    characterName = unit.unitName,
                    jobClass = unit.jobClass,
                    level = unit.level,
                    awakenLevel = unit.awakenLevel
                };
            }
            
            var stats = characterAnalytics.characterStats[characterId];
            stats.level = unit.level;
            stats.awakenLevel = unit.awakenLevel;
            stats.lastUsed = DateTime.Now;
            
            // Update class and rarity counts
            characterAnalytics.classCounts[unit.jobClass] = 
                characterAnalytics.characterStats.Values.Count(c => c.jobClass == unit.jobClass);
            characterAnalytics.rarityCounts[unit.rarity] = 
                characterAnalytics.characterStats.Values.Count(c => c.jobClass == unit.jobClass);
            
            // Update average level
            characterAnalytics.averageLevel = 
                (float)characterAnalytics.characterStats.Values.Average(c => c.level);
            characterAnalytics.averageAwakenLevel = 
                (float)characterAnalytics.characterStats.Values.Average(c => c.awakenLevel);
        }

        #endregion

        #region Performance Tracking

        IEnumerator PerformanceTrackingCoroutine()
        {
            while (enablePerformanceTracking)
            {
                yield return new WaitForSeconds(memoryCheckInterval);
                UpdateMemoryUsage();
            }
        }

        void UpdateFPS()
        {
            float fps = frameCount / deltaTime;
            performanceMetrics.fpsHistory.Add(fps);
            
            if (performanceMetrics.fpsHistory.Count > maxHistorySize)
                performanceMetrics.fpsHistory.RemoveAt(0);
            
            performanceMetrics.averageFPS = performanceMetrics.fpsHistory.Average();
            performanceMetrics.minFPS = performanceMetrics.fpsHistory.Min();
            performanceMetrics.maxFPS = performanceMetrics.fpsHistory.Max();
            
            // Check for frame drops
            if (fps < 30f) // Threshold for frame drop
            {
                performanceMetrics.frameDropEvents++;
            }
            
            frameCount = 0;
            deltaTime = 0f;
            lastFPSUpdate = Time.time;
        }

        void UpdateMemoryUsage()
        {
            float memoryUsage = GC.GetTotalMemory(false) / (1024f * 1024f); // Convert to MB
            performanceMetrics.memoryHistory.Add(memoryUsage);
            
            if (performanceMetrics.memoryHistory.Count > maxHistorySize)
                performanceMetrics.memoryHistory.RemoveAt(0);
            
            performanceMetrics.averageMemoryUsage = performanceMetrics.memoryHistory.Average();
            performanceMetrics.peakMemoryUsage = Math.Max(performanceMetrics.peakMemoryUsage, memoryUsage);
            
            // Check for memory warnings
            if (memoryUsage > 1024f) // 1GB threshold
            {
                performanceMetrics.memoryWarnings++;
            }
        }

        public void TrackLoadTime(string sceneName, float loadTime)
        {
            if (!enablePerformanceTracking) return;
            
            performanceMetrics.sceneLoadTimes[sceneName] = loadTime;
            performanceMetrics.averageLoadTime = performanceMetrics.sceneLoadTimes.Values.Average();
            performanceMetrics.longestLoadTime = Math.Max(performanceMetrics.longestLoadTime, loadTime);
        }

        #endregion

        #region Economic Analytics

        public void CalculateEconomicMetrics()
        {
            // Calculate inflation rate for Gold
            if (resourceAnalytics.balanceHistory.ContainsKey("Gold"))
            {
                var goldHistory = resourceAnalytics.balanceHistory["Gold"];
                if (goldHistory.Count > 10)
                {
                    var oldValue = goldHistory[goldHistory.Count - 10];
                    var newValue = goldHistory[goldHistory.Count - 1];
                    resourceAnalytics.inflationRate = (newValue - oldValue) / oldValue;
                }
            }
            
            // Calculate resource velocity - commented out ResourceManager usage
            // foreach (var resourceType in resourceAnalytics.totalEarned.Keys)
            // {
            //     var earned = resourceAnalytics.totalEarned[resourceType];
            //     var spent = resourceAnalytics.totalSpent[resourceType];
            //     var current = ResourceManager.Instance?.GetResources()?.GetResource(resourceType) ?? 0;
            //     
            //     if (current > 0)
            //     {
            //         resourceAnalytics.resourceVelocity = (earned + spent) / (2f * current);
            //     }
            //     
            //     // Calculate scarcity
            //     var limit = ResourceManager.Instance?.GetResourceLimits()?.GetLimit(resourceType) ?? 1;
            //     resourceAnalytics.resourceScarcity[resourceType] = 1f - (current / (float)limit);
            // }
        }

        #endregion

        #region Analytics Persistence

        void SaveAnalytics()
        {
            try
            {
                string analyticsPath = Path.Combine(Application.persistentDataPath, exportDirectory);
                if (!Directory.Exists(analyticsPath))
                {
                    Directory.CreateDirectory(analyticsPath);
                }
                
                // Save current analytics data
                var analyticsData = new
                {
                    timestamp = DateTime.Now,
                    playerBehavior,
                    resourceAnalytics,
                    buildingAnalytics,
                    battleAnalytics,
                    characterAnalytics,
                    performanceMetrics,
                    progressionAnalytics
                };
                
                string json = JsonUtility.ToJson(analyticsData, true);
                string fileName = $"analytics_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string fullPath = Path.Combine(analyticsPath, fileName);
                
                File.WriteAllText(fullPath, json);
                
                Debug.Log($"Analytics saved to: {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save analytics: {e.Message}");
            }
        }

        void LoadAnalytics()
        {
            try
            {
                string analyticsPath = Path.Combine(Application.persistentDataPath, exportDirectory);
                if (Directory.Exists(analyticsPath))
                {
                    var files = Directory.GetFiles(analyticsPath, "analytics_*.json");
                    if (files.Length > 0)
                    {
                        // Load the most recent file
                        var mostRecentFile = files.OrderByDescending(f => File.GetCreationTime(f)).First();
                        string json = File.ReadAllText(mostRecentFile);
                        
                        // Parse and restore analytics data
                        // Note: This is a simplified version - you'd need proper deserialization
                        Debug.Log($"Analytics loaded from: {mostRecentFile}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load analytics: {e.Message}");
            }
        }

        IEnumerator AnalyticsUpdateCoroutine()
        {
            while (enableAnalytics)
            {
                yield return new WaitForSeconds(analyticsSaveInterval);
                
                CalculateEconomicMetrics();
                UpdateMostFrequentActions();
                UpdateFeatureEngagement();
                
                if (Time.time - lastAnalyticsSave >= analyticsSaveInterval)
                {
                    SaveAnalytics();
                    lastAnalyticsSave = Time.time;
                }
            }
        }

        void UpdateMostFrequentActions()
        {
            playerBehavior.mostFrequentActions = playerBehavior.actionCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        void UpdateFeatureEngagement()
        {
            // Calculate feature engagement based on screen visits and durations
            foreach (var screen in playerBehavior.screenVisits.Keys)
            {
                var visits = playerBehavior.screenVisits[screen];
                var duration = playerBehavior.screenDurations.ContainsKey(screen) ? 
                    playerBehavior.screenDurations[screen] : 0;
                
                playerBehavior.featureEngagement[screen] = visits * 0.3f + duration * 0.7f;
            }
        }

        #endregion

        #region Data Export

        public void ExportAnalytics(bool includeRawData = false)
        {
            if (!enableAnalytics) return;
            
            try
            {
                string exportPath = Path.Combine(Application.persistentDataPath, exportDirectory, "exports");
                if (!Directory.Exists(exportPath))
                {
                    Directory.CreateDirectory(exportPath);
                }
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                if (exportJSON)
                {
                    ExportToJSON(exportPath, timestamp, includeRawData);
                }
                
                if (exportCSV)
                {
                    ExportToCSV(exportPath, timestamp);
                }
                
                Debug.Log($"Analytics exported to: {exportPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export analytics: {e.Message}");
            }
        }

        void ExportToJSON(string path, string timestamp, bool includeRawData)
        {
            var report = GenerateAnalyticsReport();
            
            if (includeRawData)
            {
                report["rawData"] = new
                {
                    playerBehavior,
                    resourceAnalytics,
                    buildingAnalytics,
                    battleAnalytics,
                    characterAnalytics,
                    performanceMetrics,
                    progressionAnalytics
                };
            }
            
            string json = JsonUtility.ToJson(report, true);
            string fileName = $"analytics_report_{timestamp}.json";
            string fullPath = Path.Combine(path, fileName);
            
            if (compressExports)
            {
                // Compress the JSON data
                byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
                byte[] compressed = CompressData(jsonBytes);
                File.WriteAllBytes(fullPath + ".gz", compressed);
            }
            else
            {
                File.WriteAllText(fullPath, json);
            }
        }

        void ExportToCSV(string path, string timestamp)
        {
            // Export key metrics to CSV files
            ExportResourceAnalyticsCSV(path, timestamp);
            ExportBattleAnalyticsCSV(path, timestamp);
            ExportCharacterAnalyticsCSV(path, timestamp);
            ExportPerformanceMetricsCSV(path, timestamp);
        }

        void ExportResourceAnalyticsCSV(string path, string timestamp)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Resource Type,Total Earned,Total Spent,Peak Balance,Lowest Balance,Current Balance");
            
            foreach (string type in resourceAnalytics.totalEarned.Keys)
            {
                // var current = ResourceManager.Instance?.GetResources()?.GetResource(type) ?? 0; // ResourceManager commented out
                var current = 0; // Default value since ResourceManager is not available
                csv.AppendLine($"{type},{resourceAnalytics.totalEarned[type]},{resourceAnalytics.totalSpent[type]}," +
                             $"{resourceAnalytics.peakBalance[type]},{resourceAnalytics.lowestBalance[type]},{current}");
            }
            
            string fileName = $"resource_analytics_{timestamp}.csv";
            File.WriteAllText(Path.Combine(path, fileName), csv.ToString());
        }

        void ExportBattleAnalyticsCSV(string path, string timestamp)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Total Battles,{battleAnalytics.totalBattles}");
            csv.AppendLine($"Victories,{battleAnalytics.victories}");
            csv.AppendLine($"Defeats,{battleAnalytics.defeats}");
            csv.AppendLine($"Win Rate,{battleAnalytics.winRate:P2}");
            csv.AppendLine($"Average Battle Duration,{battleAnalytics.averageBattleDuration:F2}");
            csv.AppendLine($"Total Damage Dealt,{battleAnalytics.totalDamageDealt}");
            csv.AppendLine($"Total Healing Done,{battleAnalytics.totalHealingDone}");
            
            string fileName = $"battle_analytics_{timestamp}.csv";
            File.WriteAllText(Path.Combine(path, fileName), csv.ToString());
        }

        void ExportCharacterAnalyticsCSV(string path, string timestamp)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Character Name,Class,Level,Awaken Level,Battles,Victories,Last Used");
            
            foreach (var character in characterAnalytics.characterStats.Values)
            {
                csv.AppendLine($"{character.characterName},{character.jobClass},{character.level}," +
                             $"{character.awakenLevel},{character.battlesParticipated},{character.victories}," +
                             $"{character.lastUsed:yyyy-MM-dd HH:mm:ss}");
            }
            
            string fileName = $"character_analytics_{timestamp}.csv";
            File.WriteAllText(Path.Combine(path, fileName), csv.ToString());
        }

        void ExportPerformanceMetricsCSV(string path, string timestamp)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Average FPS,{performanceMetrics.averageFPS:F2}");
            csv.AppendLine($"Min FPS,{performanceMetrics.minFPS:F2}");
            csv.AppendLine($"Max FPS,{performanceMetrics.maxFPS:F2}");
            csv.AppendLine($"Frame Drop Events,{performanceMetrics.frameDropEvents}");
            csv.AppendLine($"Average Memory Usage (MB),{performanceMetrics.averageMemoryUsage:F2}");
            csv.AppendLine($"Peak Memory Usage (MB),{performanceMetrics.peakMemoryUsage:F2}");
            csv.AppendLine($"Memory Warnings,{performanceMetrics.memoryWarnings}");
            csv.AppendLine($"Total Errors,{performanceMetrics.totalErrors}");
            csv.AppendLine($"Total Crashes,{performanceMetrics.totalCrashes}");
            
            string fileName = $"performance_metrics_{timestamp}.csv";
            File.WriteAllText(Path.Combine(path, fileName), csv.ToString());
        }

        byte[] CompressData(byte[] data)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var gzip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
                gzip.Close();
                return ms.ToArray();
            }
        }

        #endregion

        #region Analytics Reporting

        public Dictionary<string, object> GenerateAnalyticsReport()
        {
            var report = new Dictionary<string, object>();
            
            // Player Behavior Summary
            report["playerBehavior"] = new
            {
                totalPlayTime = FormatTime(playerBehavior.totalPlayTime),
                averageSessionLength = FormatTime(playerBehavior.averageSessionLength),
                totalSessions = playerBehavior.totalSessions,
                mostActiveDay = playerBehavior.playTimeByDay.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key,
                mostActiveHour = playerBehavior.playTimeByHour.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key,
                topActions = playerBehavior.mostFrequentActions.Take(5).ToList()
            };
            
            // Resource Summary
            report["resources"] = new
            {
                totalGoldEarned = resourceAnalytics.totalEarned.ContainsKey("Gold") ? resourceAnalytics.totalEarned["Gold"] : 0,
                totalGoldSpent = resourceAnalytics.totalSpent.ContainsKey("Gold") ? resourceAnalytics.totalSpent["Gold"] : 0,
                goldInflationRate = resourceAnalytics.inflationRate,
                resourceVelocity = resourceAnalytics.resourceVelocity,
                topEarningSource = GetTopResourceSource("Gold", true),
                topSpendingCategory = GetTopResourceSource("Gold", false)
            };
            
            // Building Summary
            report["buildings"] = new
            {
                totalBuildings = buildingAnalytics.totalBuildings,
                totalUpgrades = buildingAnalytics.totalUpgrades,
                mostBuiltType = buildingAnalytics.buildingCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key,
                averageConstructionTime = buildingAnalytics.averageConstructionTime
            };
            
            // Battle Summary
            report["battles"] = new
            {
                totalBattles = battleAnalytics.totalBattles,
                winRate = battleAnalytics.winRate,
                averageDuration = battleAnalytics.averageBattleDuration,
                totalDamageDealt = battleAnalytics.totalDamageDealt,
                totalHealingDone = battleAnalytics.totalHealingDone,
                mostUsedUnit = battleAnalytics.unitUsageCount.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key,
                bestPerformingClass = battleAnalytics.classWinRates.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key
            };
            
            // Character Summary
            report["characters"] = new
            {
                totalCharacters = characterAnalytics.characterStats.Count,
                averageLevel = characterAnalytics.averageLevel,
                averageAwakenLevel = characterAnalytics.averageAwakenLevel,
                mostCommonClass = characterAnalytics.classCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key,
                topCharacters = characterAnalytics.mostUsedCharacters.Take(5).ToList()
            };
            
            // Performance Summary
            report["performance"] = new
            {
                averageFPS = performanceMetrics.averageFPS,
                frameDropRate = (float)performanceMetrics.frameDropEvents / playerBehavior.totalPlayTime * 3600f, // per hour
                averageMemoryUsage = performanceMetrics.averageMemoryUsage,
                errorRate = (float)performanceMetrics.totalErrors / playerBehavior.totalPlayTime * 3600f, // per hour
                crashRate = (float)performanceMetrics.totalCrashes / playerBehavior.totalSessions
            };
            
            // Progression Summary
            report["progression"] = new
            {
                currentGuildLevel = progressionAnalytics.currentGuildLevel,
                achievementCompletionRate = progressionAnalytics.achievementCompletionRate,
                overallCompletionRate = progressionAnalytics.overallCompletionRate,
                totalAchievementsUnlocked = progressionAnalytics.unlockedAchievements
            };
            
            return report;
        }

        string GetTopResourceSource(string type, bool isEarning)
        {
            if (!resourceAnalytics.earningBySources.ContainsKey(type)) return "None";
            
            var sources = isEarning ? 
                resourceAnalytics.earningBySources[type] : 
                resourceAnalytics.spendingByCategory[type];
            
            return sources.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "None";
        }

        string FormatTime(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        }

        #endregion

        #region Public API

        public PlayerBehaviorData GetPlayerBehaviorData() => playerBehavior;
        public ResourceAnalytics GetResourceAnalytics() => resourceAnalytics;
        public BuildingAnalytics GetBuildingAnalytics() => buildingAnalytics;
        public BattleAnalytics GetBattleAnalytics() => battleAnalytics;
        public CharacterAnalytics GetCharacterAnalytics() => characterAnalytics;
        public PerformanceMetrics GetPerformanceMetrics() => performanceMetrics;
        public ProgressionAnalytics GetProgressionAnalytics() => progressionAnalytics;
        
        public void SetAnalyticsEnabled(bool enabled)
        {
            enableAnalytics = enabled;
            if (!enabled)
            {
                SaveAnalytics();
            }
        }
        
        public void SetPerformanceTrackingEnabled(bool enabled)
        {
            enablePerformanceTracking = enabled;
        }
        
        public void SetPrivacyMode(bool enabled)
        {
            enablePrivacyMode = enabled;
        }
        
        public void ClearAnalyticsData()
        {
            InitializeAnalytics();
            Debug.Log("Analytics data cleared");
        }

        void LogAnalyticsEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!enableDetailedLogging || enablePrivacyMode) return;
            
            string logMessage = $"[Analytics] {eventName}";
            if (parameters != null && parameters.Count > 0)
            {
                logMessage += " - " + string.Join(", ", parameters.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            }
            
            Debug.Log(logMessage);
        }

        #endregion
    }
}