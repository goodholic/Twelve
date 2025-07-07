using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GuildMaster.Core
{
    // 확장된 이벤트 타입
    public enum EventType
    {
        // 자원 관련
        ResourceChanged,
        ResourceCapacityReached,
        
        // 길드 관련
        GuildLevelUp,
        GuildReputationChanged,
        NewAdventurer,
        AdventurerDismissed,
        AdventurerLevelUp,
        
        // 건물 관련
        BuildingConstructed,
        BuildingUpgraded,
        BuildingDestroyed,
        BuildingProductionComplete,
        
        // 전투 관련
        BattleStarted,
        BattleEnded,
        BattleVictory,
        BattleDefeat,
        UnitKilled,
        CriticalHit,
        
        // 퀘스트 관련
        QuestStarted,
        QuestCompleted,
        QuestFailed,
        QuestProgressUpdated,
        
        // 업적 관련
        AchievementUnlocked,
        AchievementProgressUpdated,
        
        // 탐험 관련
        ExplorationComplete,
        // DungeonEntered, // Dungeon 기능 제거됨
        // DungeonCompleted, // Dungeon 기능 제거됨
        TreasureFound,
        
        // 스토리 관련
        StoryChapterStarted,
        StoryChapterCompleted,
        StoryChoiceMade,
        
        // 일일 이벤트
        DayChanged,
        SeasonChanged,
        DailyResetOccurred,
        
        // 특별 이벤트
        SpecialMission,
        ResourceBonus,
        NPCVisit,
        SpecialEventStarted,
        SpecialEventEnded,
        
        // 시스템 이벤트
        GamePaused,
        GameResumed,
        SaveGameCompleted,
        LoadGameCompleted,
        SettingsChanged,
        
        // UI 이벤트
        ScreenOpened,
        ScreenClosed,
        NotificationShown
    }

    [System.Serializable]
    public class GameEvent
    {
        public EventType eventType;
        public string title;
        public string description;
        public float duration;
        public bool isActive;
        public DateTime startTime;
        public Dictionary<string, object> parameters;
        public string source;
        public int priority;

        public GameEvent()
        {
            parameters = new Dictionary<string, object>();
            priority = 0;
        }

        public T GetParameter<T>(string key)
        {
            if (parameters != null && parameters.ContainsKey(key))
            {
                return (T)parameters[key];
            }
            return default(T);
        }

        public void SetParameter(string key, object value)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();
            parameters[key] = value;
        }
    }

    public class EventManager : MonoBehaviour
    {
        private static EventManager _instance;
        public static EventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EventManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("EventManager");
                        _instance = go.AddComponent<EventManager>();
                    }
                }
                return _instance;
            }
        }

        // Active events
        private List<GameEvent> activeEvents = new List<GameEvent>();
        private Queue<GameEvent> pendingEvents = new Queue<GameEvent>();
        private Dictionary<EventType, List<Action<GameEvent>>> eventListeners = new Dictionary<EventType, List<Action<GameEvent>>>();
        private List<GameEvent> eventHistory = new List<GameEvent>();

        // Event settings
        [Header("Event Settings")]
        [SerializeField] private float eventCheckInterval = 10f; // 10초마다 이벤트 체크
        [SerializeField] private int maxHistorySize = 100;
        [SerializeField] private bool enableEventLogging = true;
        private float lastEventCheck;

        // Events
        public event Action<GameEvent> OnEventTriggered;
        public event Action<GameEvent> OnEventCompleted;
        public event Action<EventType, Dictionary<string, object>> OnGenericEvent;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeEventListeners();
        }

        void InitializeEventListeners()
        {
            // 모든 이벤트 타입에 대한 리스너 리스트 초기화
            foreach (EventType eventType in Enum.GetValues(typeof(EventType)))
            {
                eventListeners[eventType] = new List<Action<GameEvent>>();
            }
        }

        public IEnumerator Initialize()
        {
            Debug.Log("이벤트 시스템 초기화 중...");
            
            lastEventCheck = Time.time;
            
            Debug.Log("이벤트 시스템 초기화 완료");
            yield break;
        }

        void Update()
        {
            UpdateActiveEvents();
            CheckForNewEvents();
        }

        void UpdateActiveEvents()
        {
            float currentTime = Time.time;
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                var gameEvent = activeEvents[i];
                
                if ((DateTime.Now - gameEvent.startTime).TotalSeconds >= gameEvent.duration)
                {
                    CompleteEvent(gameEvent);
                }
            }
        }

        void CheckForNewEvents()
        {
            if (Time.time - lastEventCheck >= eventCheckInterval)
            {
                lastEventCheck = Time.time;
                ProcessPendingEvents();
            }
        }

        void ProcessPendingEvents()
        {
            // 우선순위에 따라 정렬
            var sortedEvents = pendingEvents.OrderByDescending(e => e.priority).ToList();
            pendingEvents.Clear();
            
            foreach (var evt in sortedEvents)
            {
                pendingEvents.Enqueue(evt);
            }
            
            // 다음 이벤트 처리
            if (pendingEvents.Count > 0)
            {
                var nextEvent = pendingEvents.Dequeue();
                TriggerEvent(nextEvent);
            }
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        public void Subscribe(EventType eventType, Action<GameEvent> callback)
        {
            if (!eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = new List<Action<GameEvent>>();
            }
            
            if (!eventListeners[eventType].Contains(callback))
            {
                eventListeners[eventType].Add(callback);
                
                if (enableEventLogging)
                {
                    Debug.Log($"[EventManager] Subscribed to {eventType}");
                }
            }
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        public void Unsubscribe(EventType eventType, Action<GameEvent> callback)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType].Remove(callback);
                
                if (enableEventLogging)
                {
                    Debug.Log($"[EventManager] Unsubscribed from {eventType}");
                }
            }
        }

        /// <summary>
        /// 이벤트 즉시 발행
        /// </summary>
        public void TriggerEvent(GameEvent gameEvent)
        {
            if (gameEvent == null) return;

            gameEvent.isActive = true;
            gameEvent.startTime = DateTime.Now;
            activeEvents.Add(gameEvent);

            // 히스토리에 추가
            AddToHistory(gameEvent);

            // 일반 이벤트 핸들러 호출
            OnEventTriggered?.Invoke(gameEvent);
            
            // 타입별 리스너 호출
            if (eventListeners.ContainsKey(gameEvent.eventType))
            {
                var listeners = new List<Action<GameEvent>>(eventListeners[gameEvent.eventType]);
                foreach (var listener in listeners)
                {
                    try
                    {
                        listener?.Invoke(gameEvent);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventManager] Error in listener for {gameEvent.eventType}: {e.Message}");
                    }
                }
            }
            
            // 제네릭 이벤트 호출
            OnGenericEvent?.Invoke(gameEvent.eventType, gameEvent.parameters);
            
            if (enableEventLogging)
            {
                Debug.Log($"이벤트 발생: {gameEvent.title} ({gameEvent.eventType})");
            }
        }

        /// <summary>
        /// 간편한 이벤트 발행
        /// </summary>
        public void TriggerEvent(EventType eventType, string title = "", Dictionary<string, object> parameters = null)
        {
            var gameEvent = new GameEvent
            {
                eventType = eventType,
                title = string.IsNullOrEmpty(title) ? eventType.ToString() : title,
                description = "",
                duration = 0f,
                isActive = false,
                parameters = parameters ?? new Dictionary<string, object>()
            };
            
            TriggerEvent(gameEvent);
        }

        public void CompleteEvent(GameEvent gameEvent)
        {
            if (gameEvent == null) return;

            gameEvent.isActive = false;
            activeEvents.Remove(gameEvent);

            OnEventCompleted?.Invoke(gameEvent);
            
            if (enableEventLogging)
            {
                Debug.Log($"이벤트 완료: {gameEvent.title}");
            }
        }

        public void QueueEvent(EventType eventType, string title, string description, float duration = 30f, int priority = 0)
        {
            var gameEvent = new GameEvent
            {
                eventType = eventType,
                title = title,
                description = description,
                duration = duration,
                isActive = false,
                priority = priority
            };

            pendingEvents.Enqueue(gameEvent);
        }

        void AddToHistory(GameEvent gameEvent)
        {
            eventHistory.Add(gameEvent);
            
            // 히스토리 크기 제한
            while (eventHistory.Count > maxHistorySize)
            {
                eventHistory.RemoveAt(0);
            }
        }

        public List<GameEvent> GetActiveEvents()
        {
            return new List<GameEvent>(activeEvents);
        }

        public bool HasActiveEvent(EventType eventType)
        {
            return activeEvents.Exists(e => e.eventType == eventType);
        }

        public List<GameEvent> GetEventHistory(EventType? eventType = null)
        {
            if (eventType == null)
                return new List<GameEvent>(eventHistory);
            
            return eventHistory.Where(e => e.eventType == eventType).ToList();
        }

        /// <summary>
        /// 특정 이벤트의 마지막 발생 시간
        /// </summary>
        public DateTime? GetLastEventTime(EventType eventType)
        {
            var lastEvent = eventHistory.LastOrDefault(e => e.eventType == eventType);
            return lastEvent?.startTime;
        }

        // 편의 메서드들
        // ResourceType removed - using string instead
        public void TriggerResourceChanged(string resourceType, int amount, int total)
        {
            var parameters = new Dictionary<string, object>
            {
                { "resourceType", resourceType },
                { "amount", amount },
                { "total", total }
            };
            TriggerEvent(EventType.ResourceChanged, $"{resourceType} 변경", parameters);
        }

        public void TriggerBattleResult(bool victory, int goldReward, int expReward)
        {
            var eventType = victory ? EventType.BattleVictory : EventType.BattleDefeat;
            var parameters = new Dictionary<string, object>
            {
                { "goldReward", goldReward },
                { "expReward", expReward }
            };
            TriggerEvent(eventType, victory ? "전투 승리!" : "전투 패배...", parameters);
        }

        public void TriggerQuestCompleted(string questId, int goldReward, int expReward)
        {
            var parameters = new Dictionary<string, object>
            {
                { "questId", questId },
                { "goldReward", goldReward },
                { "expReward", expReward }
            };
            TriggerEvent(EventType.QuestCompleted, "퀘스트 완료!", parameters);
        }

        public void TriggerAchievementUnlocked(string achievementId, string achievementName)
        {
            var parameters = new Dictionary<string, object>
            {
                { "achievementId", achievementId },
                { "achievementName", achievementName }
            };
            TriggerEvent(EventType.AchievementUnlocked, $"업적 달성: {achievementName}", parameters);
        }

        void OnDestroy()
        {
            // 모든 리스너 제거
            foreach (var kvp in eventListeners)
            {
                kvp.Value.Clear();
            }
        }
    }
} 