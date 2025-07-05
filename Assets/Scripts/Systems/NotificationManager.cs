using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GuildMaster.Core;
using GuildMaster.UI;
using GuildMaster.Data;

namespace GuildMaster.Systems
{

    /// <summary>
    /// 알림 관리 시스템
    /// 인게임 알림, 팝업, 토스트 메시지 등을 관리
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        private static NotificationManager _instance;
        public static NotificationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NotificationManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("NotificationManager");
                        _instance = go.AddComponent<NotificationManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Notification Settings")]
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private int maxVisibleNotifications = 5;
        [SerializeField] private float notificationDuration = 3f;
        [SerializeField] private float notificationSpacing = 10f;
        
        [Header("Popup Settings")]
        [SerializeField] private GameObject popupPrefab;
        [SerializeField] private Transform popupContainer;
        
        [Header("Toast Settings")]
        [SerializeField] private GameObject toastPrefab;
        [SerializeField] private Transform toastContainer;
        [SerializeField] private float toastDuration = 2f;
        
        [Header("Sound Settings")]
        [SerializeField] private string defaultNotificationSound = "notification_default";
        [SerializeField] private string achievementSound = "achievement_unlock";
        [SerializeField] private string questCompleteSound = "quest_complete";
        [SerializeField] private string errorSound = "error";
        
        // 알림 큐
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private List<GameObject> activeNotifications = new List<GameObject>();
        private Dictionary<string, GameObject> popups = new Dictionary<string, GameObject>();
        
        // 설정
        private NotificationSettings settings;
        
        [System.Serializable]
        public class NotificationData
        {
            public string id;
            public string title;
            public string message;
            public GuildMaster.Data.NotificationType type;
            public float duration;
            public Sprite icon;
            public Action onClick;
            public bool autoClose;
            public int priority;
            public DateTime timestamp;
            
            public NotificationData()
            {
                id = Guid.NewGuid().ToString();
                duration = 3f;
                autoClose = true;
                priority = 0;
                timestamp = DateTime.Now;
            }
        }
        
        [System.Serializable]
        public class NotificationSettings
        {
            public bool enableNotifications = true;
            public bool enableQuestNotifications = true;
            public bool enableAchievementNotifications = true;
            public bool enableLevelUpNotifications = true;
            public bool enableResourceNotifications = true;
            public bool enableBattleNotifications = true;
            public bool enableSystemNotifications = true;
            public bool playSounds = true;
            public float notificationScale = 1f;
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
            
            LoadSettings();
            SubscribeToEvents();
        }
        
        void LoadSettings()
        {
            settings = new NotificationSettings();
            
            // PlayerPrefs에서 설정 로드
            settings.enableNotifications = PlayerPrefs.GetInt("NotificationsEnabled", 1) == 1;
            settings.enableQuestNotifications = PlayerPrefs.GetInt("QuestNotifications", 1) == 1;
            settings.enableAchievementNotifications = PlayerPrefs.GetInt("AchievementNotifications", 1) == 1;
            settings.enableLevelUpNotifications = PlayerPrefs.GetInt("LevelUpNotifications", 1) == 1;
            settings.enableResourceNotifications = PlayerPrefs.GetInt("ResourceNotifications", 1) == 1;
            settings.enableBattleNotifications = PlayerPrefs.GetInt("BattleNotifications", 1) == 1;
            settings.enableSystemNotifications = PlayerPrefs.GetInt("SystemNotifications", 1) == 1;
            settings.playSounds = PlayerPrefs.GetInt("NotificationSounds", 1) == 1;
            settings.notificationScale = PlayerPrefs.GetFloat("NotificationScale", 1f);
        }
        
        void SubscribeToEvents()
        {
            var eventManager = EventManager.Instance;
            if (eventManager == null) return;
            
            // 퀘스트 관련
            eventManager.Subscribe(GuildMaster.Core.EventType.QuestCompleted, OnQuestCompleted);
            eventManager.Subscribe(GuildMaster.Core.EventType.QuestStarted, OnQuestStarted);
            eventManager.Subscribe(GuildMaster.Core.EventType.QuestFailed, OnQuestFailed);
            
            // 업적 관련
            eventManager.Subscribe(GuildMaster.Core.EventType.AchievementUnlocked, OnAchievementUnlocked);
            
            // 레벨업 관련
            eventManager.Subscribe(GuildMaster.Core.EventType.GuildLevelUp, OnGuildLevelUp);
            eventManager.Subscribe(GuildMaster.Core.EventType.AdventurerLevelUp, OnAdventurerLevelUp);
            
            // 자원 관련
            eventManager.Subscribe(GuildMaster.Core.EventType.ResourceCapacityReached, OnResourceCapacityReached);
            
            // 전투 관련
            eventManager.Subscribe(GuildMaster.Core.EventType.BattleVictory, OnBattleVictory);
            eventManager.Subscribe(GuildMaster.Core.EventType.BattleDefeat, OnBattleDefeat);
            
            // 건물 관련
            eventManager.Subscribe(GuildMaster.Core.EventType.BuildingConstructed, OnBuildingConstructed);
            eventManager.Subscribe(GuildMaster.Core.EventType.BuildingUpgraded, OnBuildingUpgraded);
            
            // 특별 이벤트
            eventManager.Subscribe(GuildMaster.Core.EventType.SpecialEventStarted, OnSpecialEventStarted);
        }
        
        /// <summary>
        /// 일반 알림 표시
        /// </summary>
        public void ShowNotification(string title, string message, GuildMaster.Data.NotificationType type = GuildMaster.Data.NotificationType.Info, float duration = 0f)
        {
            if (!settings.enableNotifications) return;
            
            var notification = new NotificationData
            {
                title = title,
                message = message,
                type = type,
                duration = duration > 0 ? duration : notificationDuration,
                autoClose = true
            };
            
            ShowNotification(notification);
        }
        
        /// <summary>
        /// 상세 알림 표시
        /// </summary>
        public void ShowNotification(NotificationData data)
        {
            if (!settings.enableNotifications) return;
            
            // 우선순위에 따라 큐에 추가
            notificationQueue.Enqueue(data);
            
            // 즉시 표시 시도
            TryShowNextNotification();
            
            // 사운드 재생
            if (settings.playSounds)
            {
                PlayNotificationSound(data.type);
            }
        }
        
        void TryShowNextNotification()
        {
            if (activeNotifications.Count >= maxVisibleNotifications || notificationQueue.Count == 0)
                return;
            
            var data = notificationQueue.Dequeue();
            StartCoroutine(ShowNotificationCoroutine(data));
        }
        
        IEnumerator ShowNotificationCoroutine(NotificationData data)
        {
            GameObject notificationObj = null;
            
            if (notificationPrefab != null && notificationContainer != null)
            {
                notificationObj = Instantiate(notificationPrefab, notificationContainer);
                var notificationUI = notificationObj.GetComponent<NotificationUI>();
                
                if (notificationUI != null)
                {
                    notificationUI.Setup(data.message, data.type, data.duration);
                }
                
                // 스케일 적용
                notificationObj.transform.localScale = Vector3.one * settings.notificationScale;
                
                // 위치 조정
                UpdateNotificationPositions();
                
                activeNotifications.Add(notificationObj);
                
                // 클릭 이벤트
                if (data.onClick != null)
                {
                    var button = notificationObj.GetComponent<UnityEngine.UI.Button>();
                    if (button != null)
                    {
                        button.onClick.AddListener(() => data.onClick.Invoke());
                    }
                }
            }
            
            // 자동 닫기
            if (data.autoClose && data.duration > 0)
            {
                yield return new WaitForSeconds(data.duration);
                
                if (notificationObj != null)
                {
                    activeNotifications.Remove(notificationObj);
                    Destroy(notificationObj);
                    UpdateNotificationPositions();
                    
                    // 다음 알림 표시
                    TryShowNextNotification();
                }
            }
        }
        
        void UpdateNotificationPositions()
        {
            for (int i = 0; i < activeNotifications.Count; i++)
            {
                if (activeNotifications[i] != null)
                {
                    var rectTransform = activeNotifications[i].GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        float yPos = -i * (rectTransform.sizeDelta.y + notificationSpacing);
                        rectTransform.anchoredPosition = new Vector2(0, yPos);
                    }
                }
            }
        }
        
        /// <summary>
        /// 팝업 표시
        /// </summary>
        public void ShowPopup(string id, string title, string message, Action onConfirm = null, Action onCancel = null)
        {
            if (popups.ContainsKey(id))
            {
                // 이미 표시 중인 팝업
                return;
            }
            
            if (popupPrefab != null && popupContainer != null)
            {
                var popupObj = Instantiate(popupPrefab, popupContainer);
                popups[id] = popupObj;
                
                // 팝업 설정 (구현에 따라 수정 필요)
                // var popup = popupObj.GetComponent<PopupUI>();
                // if (popup != null)
                // {
                //     popup.Setup(title, message, onConfirm, onCancel);
                // }
            }
        }
        
        /// <summary>
        /// 팝업 닫기
        /// </summary>
        public void ClosePopup(string id)
        {
            if (popups.ContainsKey(id))
            {
                Destroy(popups[id]);
                popups.Remove(id);
            }
        }
        
        /// <summary>
        /// 토스트 메시지 표시
        /// </summary>
        public void ShowToast(string message, float duration = 0f)
        {
            if (!settings.enableNotifications) return;
            
            StartCoroutine(ShowToastCoroutine(message, duration > 0 ? duration : toastDuration));
        }
        
        IEnumerator ShowToastCoroutine(string message, float duration)
        {
            if (toastPrefab != null && toastContainer != null)
            {
                var toastObj = Instantiate(toastPrefab, toastContainer);
                
                // 토스트 메시지 설정
                var text = toastObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = message;
                }
                
                yield return new WaitForSeconds(duration);
                
                Destroy(toastObj);
            }
        }
        
        void PlayNotificationSound(GuildMaster.Data.NotificationType type)
        {
            string soundName = defaultNotificationSound;
            
            switch (type)
            {
                case GuildMaster.Data.NotificationType.Success:
                    soundName = questCompleteSound;
                    break;
                case GuildMaster.Data.NotificationType.Warning:
                    soundName = defaultNotificationSound;
                    break;
                case GuildMaster.Data.NotificationType.Error:
                    soundName = errorSound;
                    break;
            }
            
            SoundSystem.Instance?.PlaySound(soundName);
        }
        
        // 이벤트 핸들러들
        void OnQuestCompleted(GameEvent gameEvent)
        {
            if (!settings.enableQuestNotifications) return;
            
            var questName = gameEvent.GetParameter<string>("questName");
            var goldReward = gameEvent.GetParameter<int>("goldReward");
            var expReward = gameEvent.GetParameter<int>("expReward");
            
            ShowNotification(
                "퀘스트 완료!",
                $"{questName}\n보상: {goldReward} 골드, {expReward} 경험치",
                GuildMaster.Data.NotificationType.Success
            );
        }
        
        void OnQuestStarted(GameEvent gameEvent)
        {
            if (!settings.enableQuestNotifications) return;
            
            var questName = gameEvent.GetParameter<string>("questName");
            ShowToast($"새로운 퀘스트: {questName}");
        }
        
        void OnQuestFailed(GameEvent gameEvent)
        {
            if (!settings.enableQuestNotifications) return;
            
            var questName = gameEvent.GetParameter<string>("questName");
            ShowNotification(
                "퀘스트 실패",
                questName,
                GuildMaster.Data.NotificationType.Error
            );
        }
        
        void OnAchievementUnlocked(GameEvent gameEvent)
        {
            if (!settings.enableAchievementNotifications) return;
            
            var achievementName = gameEvent.GetParameter<string>("achievementName");
            
            ShowNotification(
                "업적 달성!",
                achievementName,
                GuildMaster.Data.NotificationType.Success,
                5f
            );
            
            if (settings.playSounds)
            {
                SoundSystem.Instance?.PlaySound(achievementSound);
            }
        }
        
        void OnGuildLevelUp(GameEvent gameEvent)
        {
            if (!settings.enableLevelUpNotifications) return;
            
            var newLevel = gameEvent.GetParameter<int>("newLevel");
            ShowNotification(
                "길드 레벨업!",
                $"길드가 레벨 {newLevel}이 되었습니다!",
                GuildMaster.Data.NotificationType.Success,
                5f
            );
        }
        
        void OnAdventurerLevelUp(GameEvent gameEvent)
        {
            if (!settings.enableLevelUpNotifications) return;
            
            var adventurerName = gameEvent.GetParameter<string>("adventurerName");
            var newLevel = gameEvent.GetParameter<int>("newLevel");
            
            ShowToast($"{adventurerName}이(가) 레벨 {newLevel} 달성!");
        }
        
        void OnResourceCapacityReached(GameEvent gameEvent)
        {
            if (!settings.enableResourceNotifications) return;
            
            var resourceType = gameEvent.GetParameter<Data.ResourceType>("resourceType");
            ShowNotification(
                "창고 가득!",
                $"{resourceType} 창고가 가득 찼습니다!",
                GuildMaster.Data.NotificationType.Warning
            );
        }
        
        void OnBattleVictory(GameEvent gameEvent)
        {
            if (!settings.enableBattleNotifications) return;
            
            var goldReward = gameEvent.GetParameter<int>("goldReward");
            var expReward = gameEvent.GetParameter<int>("expReward");
            
            ShowToast($"전투 승리! +{goldReward} 골드, +{expReward} 경험치");
        }
        
        void OnBattleDefeat(GameEvent gameEvent)
        {
            if (!settings.enableBattleNotifications) return;
            
            ShowNotification(
                "전투 패배",
                "부대가 패배했습니다...",
                GuildMaster.Data.NotificationType.Error
            );
        }
        
        void OnBuildingConstructed(GameEvent gameEvent)
        {
            var buildingName = gameEvent.GetParameter<string>("buildingName");
            ShowToast($"{buildingName} 건설 완료!");
        }
        
        void OnBuildingUpgraded(GameEvent gameEvent)
        {
            var buildingName = gameEvent.GetParameter<string>("buildingName");
            var newLevel = gameEvent.GetParameter<int>("newLevel");
            ShowToast($"{buildingName}이(가) 레벨 {newLevel}로 업그레이드!");
        }
        
        void OnSpecialEventStarted(GameEvent gameEvent)
        {
            var eventName = gameEvent.GetParameter<string>("eventName");
            ShowNotification(
                "특별 이벤트!",
                eventName,
                GuildMaster.Data.NotificationType.Info,
                5f
            );
        }
        
        // 설정 메서드
        public void UpdateSettings(NotificationSettings newSettings)
        {
            settings = newSettings;
            SaveSettings();
        }
        
        void SaveSettings()
        {
            PlayerPrefs.SetInt("NotificationsEnabled", settings.enableNotifications ? 1 : 0);
            PlayerPrefs.SetInt("QuestNotifications", settings.enableQuestNotifications ? 1 : 0);
            PlayerPrefs.SetInt("AchievementNotifications", settings.enableAchievementNotifications ? 1 : 0);
            PlayerPrefs.SetInt("LevelUpNotifications", settings.enableLevelUpNotifications ? 1 : 0);
            PlayerPrefs.SetInt("ResourceNotifications", settings.enableResourceNotifications ? 1 : 0);
            PlayerPrefs.SetInt("BattleNotifications", settings.enableBattleNotifications ? 1 : 0);
            PlayerPrefs.SetInt("SystemNotifications", settings.enableSystemNotifications ? 1 : 0);
            PlayerPrefs.SetInt("NotificationSounds", settings.playSounds ? 1 : 0);
            PlayerPrefs.SetFloat("NotificationScale", settings.notificationScale);
            PlayerPrefs.Save();
        }
        
        public NotificationSettings GetSettings() => settings;
        
        void OnDestroy()
        {
            var eventManager = EventManager.Instance;
            if (eventManager != null)
            {
                eventManager.Unsubscribe(GuildMaster.Core.EventType.QuestCompleted, OnQuestCompleted);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.QuestStarted, OnQuestStarted);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.QuestFailed, OnQuestFailed);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.AchievementUnlocked, OnAchievementUnlocked);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.GuildLevelUp, OnGuildLevelUp);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.AdventurerLevelUp, OnAdventurerLevelUp);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.ResourceCapacityReached, OnResourceCapacityReached);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.BattleVictory, OnBattleVictory);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.BattleDefeat, OnBattleDefeat);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.BuildingConstructed, OnBuildingConstructed);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.BuildingUpgraded, OnBuildingUpgraded);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.SpecialEventStarted, OnSpecialEventStarted);
            }
        }
    }
}