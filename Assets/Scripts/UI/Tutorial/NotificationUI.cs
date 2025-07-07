using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using NotificationType = GuildMaster.Data.NotificationType;

namespace GuildMaster.UI
{
    public class NotificationUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject notificationPanel;
        public Transform notificationParent;
        public GameObject notificationPrefab;
        public ScrollRect scrollRect;
        
        [Header("Settings")]
        public int maxNotifications = 50;
        public float autoCloseTime = 5f;
        public bool autoScroll = true;
        
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private List<GameObject> activeNotifications = new List<GameObject>();
        
        private static NotificationUI _instance;
        public static NotificationUI Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<NotificationUI>();
                return _instance;
            }
        }
        
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
        }
        
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = -1f)
        {
            var notificationData = new NotificationData
            {
                message = message,
                type = type,
                timestamp = DateTime.Now,
                duration = duration > 0 ? duration : autoCloseTime
            };
            
            notificationQueue.Enqueue(notificationData);
            ProcessNextNotification();
        }
        
        public void ShowNotification(NotificationData notification)
        {
            notificationQueue.Enqueue(notification);
            ProcessNextNotification();
        }
        
        // Overload for simple text notification
        public void ShowNotification(string message)
        {
            ShowNotification(message, NotificationType.Info);
        }
        
        void ProcessNextNotification()
        {
            if (notificationQueue.Count == 0)
                return;
                
            var notification = notificationQueue.Dequeue();
            CreateNotificationUI(notification);
        }
        
        void CreateNotificationUI(NotificationData notification)
        {
            if (notificationPrefab == null || notificationParent == null)
                return;
                
            GameObject notificationObj = Instantiate(notificationPrefab, notificationParent);
            activeNotifications.Add(notificationObj);
            
            // 설정
            var notificationComponent = notificationObj.GetComponent<NotificationItem>();
            if (notificationComponent != null)
            {
                notificationComponent.Setup(notification, OnNotificationClosed);
            }
            else
            {
                // 기본 설정
                var messageText = notificationObj.GetComponentInChildren<TextMeshProUGUI>();
                if (messageText != null)
                    messageText.text = notification.message;
                    
                var image = notificationObj.GetComponent<Image>();
                if (image != null)
                    image.color = GetNotificationColor(notification.type);
            }
            
            // 패널 표시
            if (notificationPanel != null)
                notificationPanel.SetActive(true);
                
            // 자동 스크롤
            if (autoScroll && scrollRect != null)
            {
                StartCoroutine(ScrollToBottom());
            }
            
            // 자동 닫기
            if (notification.duration > 0)
            {
                StartCoroutine(AutoCloseNotification(notificationObj, notification.duration));
            }
            
            // 최대 알림 수 제한
            while (activeNotifications.Count > maxNotifications)
            {
                CloseNotification(activeNotifications[0]);
            }
        }
        
        IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            scrollRect.verticalNormalizedPosition = 0f;
        }
        
        IEnumerator AutoCloseNotification(GameObject notificationObj, float delay)
        {
            yield return new WaitForSeconds(delay);
            CloseNotification(notificationObj);
        }
        
        void OnNotificationClosed(GameObject notificationObj)
        {
            CloseNotification(notificationObj);
        }
        
        void CloseNotification(GameObject notificationObj)
        {
            if (notificationObj != null && activeNotifications.Contains(notificationObj))
            {
                activeNotifications.Remove(notificationObj);
                Destroy(notificationObj);
                
                // 모든 알림이 닫혔으면 패널 숨기기
                if (activeNotifications.Count == 0 && notificationPanel != null)
                {
                    notificationPanel.SetActive(false);
                }
            }
        }
        
        public void ClearAllNotifications()
        {
            foreach (var notification in activeNotifications)
            {
                if (notification != null)
                    Destroy(notification);
            }
            activeNotifications.Clear();
            notificationQueue.Clear();
            
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
        }
        
        Color GetNotificationColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                NotificationType.Warning => new Color(1f, 0.8f, 0.2f, 0.8f),
                NotificationType.Error => new Color(0.8f, 0.2f, 0.2f, 0.8f),
                NotificationType.Info => new Color(0.2f, 0.6f, 1f, 0.8f),
                NotificationType.Achievement => new Color(1f, 0.8f, 0f, 0.8f),
                NotificationType.Quest => new Color(0.6f, 0.4f, 1f, 0.8f),
                NotificationType.Battle => new Color(0.8f, 0.2f, 0.2f, 0.8f),
                NotificationType.System => new Color(0.5f, 0.5f, 0.5f, 0.8f),
                NotificationType.LevelUp => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                NotificationType.ItemReceived => new Color(1f, 0.8f, 0.2f, 0.8f),
                // ResourcesGained 및 BuildingComplete 타입이 제거되어 주석 처리
                // NotificationType.ResourcesGained => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                // NotificationType.BuildingComplete => new Color(0.4f, 0.6f, 1f, 0.8f),
                NotificationType.Emergency => new Color(1f, 0.2f, 0.2f, 0.8f),
                _ => new Color(0.5f, 0.5f, 0.5f, 0.8f)
            };
        }
        
        /// <summary>
        /// NotificationManager에서 호출하는 간단한 Setup 메서드
        /// </summary>
        public void Setup(string message, NotificationType type, float duration)
        {
            // 최신 알림 표시
            ShowNotification(message, type);
        }
    }
    
    [System.Serializable]
    public class NotificationData
    {
        public string message;
        public NotificationType type;
        public DateTime timestamp;
        public float duration;
        public Sprite icon;
        public string title;
        public Action onClicked;
    }
    
    public class NotificationItem : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI messageText;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI timestampText;
        public Image iconImage;
        public Image backgroundImage;
        public Button closeButton;
        public Button mainButton;
        
        private NotificationData data;
        private Action<GameObject> onClosed;
        
        void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseNotification);
                
            if (mainButton != null)
                mainButton.onClick.AddListener(OnNotificationClicked);
        }
        
        public void Setup(NotificationData notificationData, Action<GameObject> closeCallback)
        {
            data = notificationData;
            onClosed = closeCallback;
            
            if (messageText != null)
                messageText.text = data.message;
                
            if (titleText != null)
                titleText.text = data.title ?? "";
                
            if (timestampText != null)
                timestampText.text = data.timestamp.ToString("HH:mm");
                
            if (iconImage != null && data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.gameObject.SetActive(true);
            }
            else if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = GetNotificationColor(data.type);
            }
        }
        
        void CloseNotification()
        {
            onClosed?.Invoke(gameObject);
        }
        
        void OnNotificationClicked()
        {
            data.onClicked?.Invoke();
            CloseNotification();
        }
        
        Color GetNotificationColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                NotificationType.Warning => new Color(1f, 0.8f, 0.2f, 0.8f),
                NotificationType.Error => new Color(0.8f, 0.2f, 0.2f, 0.8f),
                NotificationType.Info => new Color(0.2f, 0.6f, 1f, 0.8f),
                NotificationType.Achievement => new Color(1f, 0.8f, 0f, 0.8f),
                NotificationType.Quest => new Color(0.6f, 0.4f, 1f, 0.8f),
                NotificationType.Battle => new Color(0.8f, 0.2f, 0.2f, 0.8f),
                NotificationType.System => new Color(0.5f, 0.5f, 0.5f, 0.8f),
                NotificationType.LevelUp => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                NotificationType.ItemReceived => new Color(1f, 0.8f, 0.2f, 0.8f),
                // ResourcesGained 및 BuildingComplete 타입이 제거되어 주석 처리
                // NotificationType.ResourcesGained => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                // NotificationType.BuildingComplete => new Color(0.4f, 0.6f, 1f, 0.8f),
                NotificationType.Emergency => new Color(1f, 0.2f, 0.2f, 0.8f),
                _ => new Color(0.5f, 0.5f, 0.5f, 0.8f)
            };
        }
    }
} 