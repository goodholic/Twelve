using System;
using System.Collections.Generic;
using UnityEngine;
using GuildMaster.Core;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 게임 내 사용자 행동과 성과를 추적하는 분석 시스템
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        private static AnalyticsManager _instance;
        public static AnalyticsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AnalyticsManager");
                    _instance = go.AddComponent<AnalyticsManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        [Header("Settings")]
        [SerializeField] private bool enableTracking = true;
        [SerializeField] private bool debugMode = false;
        [SerializeField] private float batchSendInterval = 60f; // 1분마다 배치 전송
        [SerializeField] private int maxBatchSize = 100;
        
        // 세션 데이터
        private SessionData currentSession;
        private List<GameEvent> eventQueue = new List<GameEvent>();
        private float lastBatchSendTime;
        
        // 통계 데이터
        private Dictionary<string, int> actionCounts = new Dictionary<string, int>();
        private Dictionary<string, float> timeSpent = new Dictionary<string, float>();
        private Dictionary<string, object> customMetrics = new Dictionary<string, object>();
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeSession();
        }
        
        void Update()
        {
            if (!enableTracking) return;
            
            // 배치 전송 체크
            if (Time.time - lastBatchSendTime >= batchSendInterval)
            {
                SendBatch();
                lastBatchSendTime = Time.time;
            }
            
            // 세션 시간 업데이트
            if (currentSession != null)
            {
                currentSession.sessionDuration = Time.time - currentSession.sessionStartTime;
            }
        }
        
        void InitializeSession()
        {
            currentSession = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                sessionStartTime = Time.time,
                playerLevel = GetPlayerLevel(),
                guildLevel = GetGuildLevel(),
                platform = Application.platform.ToString(),
                gameVersion = Application.version,
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem
            };
            
            TrackEvent("session_start");
        }
        
        /// <summary>
        /// 게임 이벤트 추적
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!enableTracking) return;
            
            var gameEvent = new GameEvent
            {
                eventName = eventName,
                timestamp = DateTime.Now,
                sessionId = currentSession?.sessionId,
                parameters = parameters ?? new Dictionary<string, object>()
            };
            
            eventQueue.Add(gameEvent);
            
            // 액션 카운트 증가
            if (actionCounts.ContainsKey(eventName))
                actionCounts[eventName]++;
            else
                actionCounts[eventName] = 1;
            
            if (debugMode)
            {
                Debug.Log($"[Analytics] Event: {eventName} | Params: {string.Join(", ", gameEvent.parameters)}");
            }
            
            // 중요 이벤트는 즉시 전송
            if (IsCriticalEvent(eventName))
            {
                SendBatch();
            }
        }
        
        /// <summary>
        /// 사용자 속성 설정
        /// </summary>
        public void SetUserProperty(string propertyName, object value)
        {
            if (!enableTracking) return;
            
            customMetrics[propertyName] = value;
            
            TrackEvent("user_property_set", new Dictionary<string, object>
            {
                { "property_name", propertyName },
                { "property_value", value }
            });
        }
        
        /// <summary>
        /// 시간 측정 시작
        /// </summary>
        public void StartTimer(string timerName)
        {
            if (!enableTracking) return;
            
            timeSpent[timerName + "_start"] = Time.time;
        }
        
        /// <summary>
        /// 시간 측정 종료
        /// </summary>
        public void EndTimer(string timerName)
        {
            if (!enableTracking) return;
            
            string startKey = timerName + "_start";
            if (timeSpent.ContainsKey(startKey))
            {
                float duration = Time.time - timeSpent[startKey];
                timeSpent[timerName] = duration;
                timeSpent.Remove(startKey);
                
                TrackEvent("timer_completed", new Dictionary<string, object>
                {
                    { "timer_name", timerName },
                    { "duration", duration }
                });
            }
        }
        
        /// <summary>
        /// 레벨업 추적
        /// </summary>
        public void TrackLevelUp(int newLevel, string levelType = "player")
        {
            TrackEvent("level_up", new Dictionary<string, object>
            {
                { "new_level", newLevel },
                { "level_type", levelType },
                { "session_time", currentSession?.sessionDuration ?? 0 }
            });
        }
        
        /// <summary>
        /// 구매 추적
        /// </summary>
        public void TrackPurchase(string itemId, int quantity, int cost, string currency = "gold")
        {
            TrackEvent("purchase", new Dictionary<string, object>
            {
                { "item_id", itemId },
                { "quantity", quantity },
                { "cost", cost },
                { "currency", currency },
                { "player_level", GetPlayerLevel() }
            });
        }
        
        /// <summary>
        /// 전투 결과 추적
        /// </summary>
        public void TrackBattleResult(bool victory, string battleType, int duration, Dictionary<string, object> extraData = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "victory", victory },
                { "battle_type", battleType },
                { "duration", duration },
                { "player_level", GetPlayerLevel() },
                { "guild_level", GetGuildLevel() }
            };
            
            if (extraData != null)
            {
                foreach (var kvp in extraData)
                    parameters[kvp.Key] = kvp.Value;
            }
            
            TrackEvent("battle_completed", parameters);
        }
        
        /// <summary>
        /// 퀘스트 완료 추적
        /// </summary>
        public void TrackQuestCompleted(string questId, int rewardGold, int rewardExp)
        {
            TrackEvent("quest_completed", new Dictionary<string, object>
            {
                { "quest_id", questId },
                { "reward_gold", rewardGold },
                { "reward_exp", rewardExp },
                { "player_level", GetPlayerLevel() }
            });
        }
        
        /// <summary>
        /// 건물 건설 추적
        /// </summary>
        public void TrackBuildingConstructed(string buildingType, int level, int cost)
        {
            TrackEvent("building_constructed", new Dictionary<string, object>
            {
                { "building_type", buildingType },
                { "building_level", level },
                { "construction_cost", cost },
                { "guild_level", GetGuildLevel() }
            });
        }
        
        /// <summary>
        /// 자원 소득 추적
        /// </summary>
        public void TrackResourceGained(string resourceType, int amount, string source)
        {
            TrackEvent("resource_gained", new Dictionary<string, object>
            {
                { "resource_type", resourceType },
                { "amount", amount },
                { "source", source }
            });
        }
        
        /// <summary>
        /// 에러 추적
        /// </summary>
        public void TrackError(string errorType, string errorMessage, string stackTrace = null)
        {
            TrackEvent("error_occurred", new Dictionary<string, object>
            {
                { "error_type", errorType },
                { "error_message", errorMessage },
                { "stack_trace", stackTrace },
                { "game_state", GetCurrentGameState() }
            });
        }
        
        /// <summary>
        /// 성과 달성 추적
        /// </summary>
        public void TrackAchievement(string achievementId, int progress = 100)
        {
            TrackEvent("achievement_unlocked", new Dictionary<string, object>
            {
                { "achievement_id", achievementId },
                { "progress", progress },
                { "player_level", GetPlayerLevel() },
                { "session_time", currentSession?.sessionDuration ?? 0 }
            });
        }
        
        /// <summary>
        /// 배치 전송
        /// </summary>
        void SendBatch()
        {
            if (eventQueue.Count == 0) return;
            
            var batch = new AnalyticsBatch
            {
                sessionData = currentSession,
                events = new List<GameEvent>(eventQueue),
                timestamp = DateTime.Now,
                actionCounts = new Dictionary<string, int>(actionCounts),
                customMetrics = new Dictionary<string, object>(customMetrics)
            };
            
            // 실제 서버 전송 (여기서는 로컬 저장)
            SaveBatchLocally(batch);
            
            // 큐 정리
            eventQueue.Clear();
            
            if (debugMode)
            {
                Debug.Log($"[Analytics] Batch sent with {batch.events.Count} events");
            }
        }
        
        /// <summary>
        /// 로컬에 배치 저장
        /// </summary>
        void SaveBatchLocally(AnalyticsBatch batch)
        {
            try
            {
                string json = JsonUtility.ToJson(batch, true);
                string fileName = $"analytics_batch_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, "Analytics", fileName);
                
                // 디렉토리 생성
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
                
                // 파일 저장
                System.IO.File.WriteAllText(filePath, json);
                
                if (debugMode)
                {
                    Debug.Log($"[Analytics] Batch saved to: {filePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Failed to save batch: {e.Message}");
            }
        }
        
        bool IsCriticalEvent(string eventName)
        {
            return eventName == "error_occurred" || 
                   eventName == "session_start" || 
                   eventName == "session_end" ||
                   eventName == "purchase";
        }
        
        int GetPlayerLevel()
        {
            // GameManager에서 플레이어 레벨 가져오기
            if (GameManager.Instance?.GuildManager != null)
                return GameManager.Instance.GuildManager.GetGuildLevel();
            return 1;
        }
        
        int GetGuildLevel()
        {
            // GameManager에서 길드 레벨 가져오기
            if (GameManager.Instance?.GuildManager != null)
                return GameManager.Instance.GuildManager.GetGuildLevel();
            return 1;
        }
        
        string GetCurrentGameState()
        {
            if (GameLoopManager.Instance != null)
                return GameLoopManager.Instance.CurrentState.ToString();
            return "Unknown";
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TrackEvent("game_paused");
                SendBatch(); // 일시정지 시 즉시 전송
            }
            else
            {
                TrackEvent("game_resumed");
            }
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                TrackEvent("game_lost_focus");
                SendBatch();
            }
            else
            {
                TrackEvent("game_gained_focus");
            }
        }
        
        void OnDestroy()
        {
            TrackEvent("session_end");
            SendBatch();
        }
        
        /// <summary>
        /// 분석 데이터 클래스들
        /// </summary>
        [System.Serializable]
        public class SessionData
        {
            public string sessionId;
            public float sessionStartTime;
            public float sessionDuration;
            public int playerLevel;
            public int guildLevel;
            public string platform;
            public string gameVersion;
            public string deviceModel;
            public string operatingSystem;
        }
        
        [System.Serializable]
        public class GameEvent
        {
            public string eventName;
            public DateTime timestamp;
            public string sessionId;
            public Dictionary<string, object> parameters;
        }
        
        [System.Serializable]
        public class AnalyticsBatch
        {
            public SessionData sessionData;
            public List<GameEvent> events;
            public DateTime timestamp;
            public Dictionary<string, int> actionCounts;
            public Dictionary<string, object> customMetrics;
        }
    }
} 