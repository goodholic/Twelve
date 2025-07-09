using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Core;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 분석 시스템 - 게임 플레이 데이터 수집 및 분석
    /// </summary>
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
                    }
                }
                return _instance;
            }
        }
        
        // Analytics Events
        public event Action<string, Dictionary<string, object>> OnAnalyticsEvent;
        public event Action<SessionData> OnSessionStarted;
        public event Action<SessionData> OnSessionEnded;
        
        [System.Serializable]
        public class SessionData
        {
            public string sessionId;
            public DateTime startTime;
            public DateTime endTime;
            public float totalPlayTime;
            public int actionsPerformed;
            public Dictionary<string, float> timeSpentByScreen;
            public Dictionary<string, float> timeSpentByState;
            public List<string> screenFlow;
            
            public SessionData()
            {
                sessionId = Guid.NewGuid().ToString();
                startTime = DateTime.Now;
                timeSpentByScreen = new Dictionary<string, float>();
                timeSpentByState = new Dictionary<string, float>();
                screenFlow = new List<string>();
            }
        }
        
        [System.Serializable]
        public class PlayerBehavior
        {
            public int totalSessions;
            public float totalPlayTime;
            public float averageSessionLength;
            public Dictionary<DayOfWeek, float> playTimeByDay;
            public Dictionary<int, float> playTimeByHour;
            public Dictionary<string, int> actionCounts;
            public List<DateTime> loginTimes;
            public float retentionRate;
            
            public PlayerBehavior()
            {
                playTimeByDay = new Dictionary<DayOfWeek, float>();
                playTimeByHour = new Dictionary<int, float>();
                actionCounts = new Dictionary<string, int>();
                loginTimes = new List<DateTime>();
            }
        }
        
        [System.Serializable]
        public class ResourceAnalytics
        {
            public Dictionary<string, float> totalEarned;
            public Dictionary<string, float> totalSpent;
            public Dictionary<string, Dictionary<string, float>> earningBySources;
            public Dictionary<string, Dictionary<string, float>> spendingByCategory;
            public Dictionary<string, List<float>> balanceHistory;
            public Dictionary<string, float> peakBalance;
            public Dictionary<string, float> lowestBalance;
            
            public ResourceAnalytics()
            {
                totalEarned = new Dictionary<string, float>();
                totalSpent = new Dictionary<string, float>();
                earningBySources = new Dictionary<string, Dictionary<string, float>>();
                spendingByCategory = new Dictionary<string, Dictionary<string, float>>();
                balanceHistory = new Dictionary<string, List<float>>();
                peakBalance = new Dictionary<string, float>();
                lowestBalance = new Dictionary<string, float>();
            }
        }
        
        [System.Serializable]
        public class BattleAnalytics
        {
            public int totalBattles;
            public int victories;
            public int defeats;
            public float winRate;
            public float averageBattleDuration;
            public Dictionary<string, int> unitsUsedCount;
            public Dictionary<string, int> enemiesDefeated;
            public Dictionary<string, float> damageByUnit;
            public Dictionary<string, float> healingByUnit;
            
            public BattleAnalytics()
            {
                unitsUsedCount = new Dictionary<string, int>();
                enemiesDefeated = new Dictionary<string, int>();
                damageByUnit = new Dictionary<string, float>();
                healingByUnit = new Dictionary<string, float>();
            }
        }
        
        [System.Serializable]
        public class ProgressionAnalytics
        {
            public Dictionary<string, DateTime> milestonesReached;
            public List<LevelProgressData> levelProgressHistory;
            public float averageLevelUpTime;
            public Dictionary<string, int> questCompletionCounts;
            public Dictionary<string, float> questAverageTime;
            public int totalAchievementsUnlocked;
            
            public ProgressionAnalytics()
            {
                milestonesReached = new Dictionary<string, DateTime>();
                levelProgressHistory = new List<LevelProgressData>();
                questCompletionCounts = new Dictionary<string, int>();
                questAverageTime = new Dictionary<string, float>();
            }
        }
        
        [System.Serializable]
        public class LevelProgressData
        {
            public int level;
            public DateTime reachedTime;
            public float timeTaken;
        }
        
        [System.Serializable]
        public class ErrorAnalytics
        {
            public Dictionary<string, int> errorCounts;
            public List<ErrorData> recentErrors;
            public Dictionary<string, DateTime> lastErrorTime;
            
            public ErrorAnalytics()
            {
                errorCounts = new Dictionary<string, int>();
                recentErrors = new List<ErrorData>();
                lastErrorTime = new Dictionary<string, DateTime>();
            }
        }
        
        [System.Serializable]
        public class ErrorData
        {
            public string errorType;
            public string message;
            public string stackTrace;
            public DateTime timestamp;
            public Dictionary<string, object> context;
        }
        
        // Analytics Data
        private SessionData currentSession;
        private PlayerBehavior playerBehavior;
        private ResourceAnalytics resourceAnalytics;
        private BattleAnalytics battleAnalytics;
        private ProgressionAnalytics progressionAnalytics;
        private ErrorAnalytics errorAnalytics;
        
        // Settings
        [Header("Analytics Settings")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enableDetailedLogging = false;
        [SerializeField] private float saveInterval = 60f; // 60초마다 저장
        [SerializeField] private int maxErrorHistory = 100;
        
        private float lastSaveTime;
        private string currentScreen;
        
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
        
        void InitializeAnalytics()
        {
            playerBehavior = new PlayerBehavior();
            resourceAnalytics = new ResourceAnalytics();
            battleAnalytics = new BattleAnalytics();
            progressionAnalytics = new ProgressionAnalytics();
            errorAnalytics = new ErrorAnalytics();
            
            LoadAnalyticsData();
            
            Application.logMessageReceived += OnLogMessageReceived;
        }
        
        public IEnumerator Initialize()
        {
            Debug.Log("분석 시스템 초기화 중...");
            
            StartNewSession();
            
            Debug.Log("분석 시스템 초기화 완료");
            yield break;
        }
        
        void Start()
        {
            StartCoroutine(AutoSaveRoutine());
        }
        
        void OnDestroy()
        {
            EndSession();
            SaveAnalyticsData();
            
            Application.logMessageReceived -= OnLogMessageReceived;
        }
        
        IEnumerator AutoSaveRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(saveInterval);
                SaveAnalyticsData();
            }
        }
        
        #region Session Management
        
        void StartNewSession()
        {
            currentSession = new SessionData();
            playerBehavior.totalSessions++;
            playerBehavior.loginTimes.Add(DateTime.Now);
            
            OnSessionStarted?.Invoke(currentSession);
            
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
                
                OnSessionEnded?.Invoke(currentSession);
                
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
                SaveAnalyticsData();
            }
        }
        
        void ResumeSession()
        {
            // Session continues
        }
        
        #endregion
        
        #region Event Tracking
        
        /// <summary>
        /// 액션 추적
        /// </summary>
        public void TrackAction(string actionName, Dictionary<string, object> parameters = null)
        {
            if (!enableAnalytics) return;
            
            if (currentSession != null)
            {
                currentSession.actionsPerformed++;
            }
            
            if (!playerBehavior.actionCounts.ContainsKey(actionName))
                playerBehavior.actionCounts[actionName] = 0;
            playerBehavior.actionCounts[actionName]++;
            
            LogAnalyticsEvent(actionName, parameters);
        }
        
        /// <summary>
        /// 화면 전환 추적
        /// </summary>
        public void TrackScreenView(string screenName)
        {
            if (!enableAnalytics) return;
            
            if (currentSession != null)
            {
                // 이전 화면의 체류 시간 계산
                if (!string.IsNullOrEmpty(currentScreen))
                {
                    if (!currentSession.timeSpentByScreen.ContainsKey(currentScreen))
                        currentSession.timeSpentByScreen[currentScreen] = 0;
                    
                    currentSession.timeSpentByScreen[currentScreen] += Time.time;
                }
                
                currentSession.screenFlow.Add(screenName);
            }
            
            currentScreen = screenName;
            
            TrackAction("ScreenView", new Dictionary<string, object>
            {
                { "screenName", screenName }
            });
        }
        
        /// <summary>
        /// 리소스 변경 추적
        /// </summary>
        public void TrackResourceChange(string resourceType, float amount, string source)
        {
            if (!enableAnalytics) return;
            
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
                
                if (!resourceAnalytics.earningBySources[resourceType].ContainsKey(source))
                    resourceAnalytics.earningBySources[resourceType][source] = 0;
                resourceAnalytics.earningBySources[resourceType][source] += amount;
            }
            else
            {
                resourceAnalytics.totalSpent[resourceType] += Math.Abs(amount);
                
                if (!resourceAnalytics.spendingByCategory[resourceType].ContainsKey(source))
                    resourceAnalytics.spendingByCategory[resourceType][source] = 0;
                resourceAnalytics.spendingByCategory[resourceType][source] += Math.Abs(amount);
            }
            
            TrackAction("ResourceChange", new Dictionary<string, object>
            {
                { "resourceType", resourceType },
                { "amount", amount },
                { "source", source }
            });
        }
        
        /// <summary>
        /// 전투 결과 추적
        /// </summary>
        public void TrackBattleResult(bool victory, float duration, Dictionary<string, object> battleData = null)
        {
            if (!enableAnalytics) return;
            
            battleAnalytics.totalBattles++;
            
            if (victory)
            {
                battleAnalytics.victories++;
            }
            else
            {
                battleAnalytics.defeats++;
            }
            
            battleAnalytics.winRate = (float)battleAnalytics.victories / battleAnalytics.totalBattles;
            battleAnalytics.averageBattleDuration = 
                ((battleAnalytics.averageBattleDuration * (battleAnalytics.totalBattles - 1)) + duration) / battleAnalytics.totalBattles;
            
            var parameters = new Dictionary<string, object>
            {
                { "victory", victory },
                { "duration", duration }
            };
            
            if (battleData != null)
            {
                foreach (var kvp in battleData)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }
            
            TrackAction("BattleResult", parameters);
        }
        
        /// <summary>
        /// 퀘스트 완료 추적
        /// </summary>
        public void TrackQuestCompleted(string questId, float timeTaken)
        {
            if (!enableAnalytics) return;
            
            if (!progressionAnalytics.questCompletionCounts.ContainsKey(questId))
            {
                progressionAnalytics.questCompletionCounts[questId] = 0;
                progressionAnalytics.questAverageTime[questId] = 0;
            }
            
            progressionAnalytics.questCompletionCounts[questId]++;
            progressionAnalytics.questAverageTime[questId] = 
                ((progressionAnalytics.questAverageTime[questId] * (progressionAnalytics.questCompletionCounts[questId] - 1)) + timeTaken) 
                / progressionAnalytics.questCompletionCounts[questId];
            
            TrackAction("QuestCompleted", new Dictionary<string, object>
            {
                { "questId", questId },
                { "timeTaken", timeTaken }
            });
        }
        
        /// <summary>
        /// 레벨업 추적
        /// </summary>
        public void TrackLevelUp(int newLevel, float timeTaken)
        {
            if (!enableAnalytics) return;
            
            var levelData = new LevelProgressData
            {
                level = newLevel,
                reachedTime = DateTime.Now,
                timeTaken = timeTaken
            };
            
            progressionAnalytics.levelProgressHistory.Add(levelData);
            progressionAnalytics.averageLevelUpTime = 
                progressionAnalytics.levelProgressHistory.Average(l => l.timeTaken);
            
            TrackAction("LevelUp", new Dictionary<string, object>
            {
                { "level", newLevel },
                { "timeTaken", timeTaken }
            });
        }
        
        /// <summary>
        /// 업적 해금 추적
        /// </summary>
        public void TrackAchievementUnlocked(string achievementId)
        {
            if (!enableAnalytics) return;
            
            progressionAnalytics.totalAchievementsUnlocked++;
            
            TrackAction("AchievementUnlocked", new Dictionary<string, object>
            {
                { "achievementId", achievementId },
                { "total", progressionAnalytics.totalAchievementsUnlocked }
            });
        }
        
        /// <summary>
        /// 마일스톤 도달 추적
        /// </summary>
        public void TrackMilestone(string milestone)
        {
            if (!enableAnalytics) return;
            
            progressionAnalytics.milestonesReached[milestone] = DateTime.Now;
            
            TrackAction("MilestoneReached", new Dictionary<string, object>
            {
                { "milestone", milestone }
            });
        }
        
        #endregion
        
        #region Error Tracking
        
        void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                TrackError(type.ToString(), logString, stackTrace);
            }
        }
        
        void TrackError(string errorType, string message, string stackTrace)
        {
            if (!enableAnalytics) return;
            
            if (!errorAnalytics.errorCounts.ContainsKey(errorType))
                errorAnalytics.errorCounts[errorType] = 0;
            errorAnalytics.errorCounts[errorType]++;
            
            errorAnalytics.lastErrorTime[errorType] = DateTime.Now;
            
            var errorData = new ErrorData
            {
                errorType = errorType,
                message = message,
                stackTrace = stackTrace,
                timestamp = DateTime.Now,
                context = new Dictionary<string, object>
                {
                    { "screen", currentScreen },
                    { "sessionId", currentSession?.sessionId }
                }
            };
            
            errorAnalytics.recentErrors.Add(errorData);
            
            // 최대 개수 제한
            while (errorAnalytics.recentErrors.Count > maxErrorHistory)
            {
                errorAnalytics.recentErrors.RemoveAt(0);
            }
            
            LogAnalyticsEvent("Error", new Dictionary<string, object>
            {
                { "errorType", errorType },
                { "message", message }
            });
        }
        
        #endregion
        
        #region Data Access
        
        public SessionData GetCurrentSession() => currentSession;
        public PlayerBehavior GetPlayerBehavior() => playerBehavior;
        public ResourceAnalytics GetResourceAnalytics() => resourceAnalytics;
        public BattleAnalytics GetBattleAnalytics() => battleAnalytics;
        public ProgressionAnalytics GetProgressionAnalytics() => progressionAnalytics;
        public ErrorAnalytics GetErrorAnalytics() => errorAnalytics;
        
        /// <summary>
        /// 가장 많이 수행된 액션 조회
        /// </summary>
        public List<KeyValuePair<string, int>> GetTopActions(int count = 10)
        {
            return playerBehavior.actionCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .ToList();
        }
        
        /// <summary>
        /// 리소스 수입/지출 비율 조회
        /// </summary>
        public float GetResourceIncomeRatio(string resourceType)
        {
            if (!resourceAnalytics.totalEarned.ContainsKey(resourceType) ||
                !resourceAnalytics.totalSpent.ContainsKey(resourceType))
                return 0f;
            
            var earned = resourceAnalytics.totalEarned[resourceType];
            var spent = resourceAnalytics.totalSpent[resourceType];
            
            return spent > 0 ? earned / spent : float.MaxValue;
        }
        
        /// <summary>
        /// 평균 세션 길이 조회 (일별)
        /// </summary>
        public float GetAverageSessionLengthByDay(DayOfWeek day)
        {
            if (!playerBehavior.playTimeByDay.ContainsKey(day))
                return 0f;
            
            var sessionsOnDay = playerBehavior.loginTimes
                .Count(t => t.DayOfWeek == day);
            
            return sessionsOnDay > 0 ? playerBehavior.playTimeByDay[day] / sessionsOnDay : 0f;
        }
        
        #endregion
        
        #region Internal Methods
        
        void LogAnalyticsEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[Analytics] {eventName}: {string.Join(", ", parameters?.Select(kvp => $"{kvp.Key}={kvp.Value}") ?? new string[0])}");
            }
            
            OnAnalyticsEvent?.Invoke(eventName, parameters);
        }
        
        void SaveAnalyticsData()
        {
            // TODO: 실제 저장 구현
            lastSaveTime = Time.time;
        }
        
        void LoadAnalyticsData()
        {
            // TODO: 실제 로드 구현
        }
        
        #endregion
        
        #region Application Events
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseSession();
                TrackAction("ApplicationPaused");
            }
            else
            {
                ResumeSession();
                TrackAction("ApplicationResumed");
            }
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                TrackAction("ApplicationLostFocus");
            }
            else
            {
                TrackAction("ApplicationGainedFocus");
            }
        }
        
        #endregion
    }
}