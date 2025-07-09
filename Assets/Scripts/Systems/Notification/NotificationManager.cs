using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 알림 시스템 매니저 - 게임 내 알림 표시
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
        
        [System.Serializable]
        public class NotificationData
        {
            public string id;
            public string title;
            public string message;
            public NotificationType type;
            public float duration;
            public DateTime timestamp;
            public bool isRead;
            public Dictionary<string, object> metadata;
            
            public NotificationData()
            {
                id = Guid.NewGuid().ToString();
                timestamp = DateTime.Now;
                metadata = new Dictionary<string, object>();
                duration = 3f;
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
        
        // UI References
        [Header("UI Components")]
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private GameObject toastPrefab;
        [SerializeField] private Transform toastContainer;
        [SerializeField] private GameObject floatingTextPrefab;
        
        // Audio
        [Header("Audio")]
        [SerializeField] private AudioClip notificationSound;
        [SerializeField] private AudioClip achievementSound;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioClip errorSound;
        
        // Settings
        [Header("Settings")]
        [SerializeField] private NotificationSettings settings = new NotificationSettings();
        [SerializeField] private int maxNotifications = 50;
        [SerializeField] private float defaultDuration = 3f;
        [SerializeField] private float toastDuration = 2f;
        
        // Notification queue
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private List<NotificationData> notificationHistory = new List<NotificationData>();
        private List<GameObject> activeNotifications = new List<GameObject>();
        private bool isProcessingNotifications = false;
        
        // Events
        public event Action<NotificationData> OnNotificationShown;
        public event Action<NotificationData> OnNotificationClicked;
        public event Action<NotificationData> OnNotificationDismissed;
        
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
        }
        
        void Start()
        {
            StartCoroutine(ProcessNotificationQueue());
        }
        
        public IEnumerator Initialize()
        {
            Debug.Log("알림 시스템 초기화 중...");
            
            // UI 컴포넌트 찾기
            if (notificationContainer == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    GameObject container = new GameObject("NotificationContainer");
                    container.transform.SetParent(canvas.transform, false);
                    notificationContainer = container.transform;
                    
                    // RectTransform 설정
                    RectTransform rt = container.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    rt.anchoredPosition = new Vector2(-10, -10);
                    rt.sizeDelta = new Vector2(300, 600);
                    
                    // Vertical Layout Group 추가
                    VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
                    vlg.spacing = 10;
                    vlg.childAlignment = TextAnchor.UpperRight;
                    vlg.childControlHeight = false;
                    vlg.childControlWidth = true;
                    vlg.childForceExpandHeight = false;
                    vlg.childForceExpandWidth = true;
                }
            }
            
            Debug.Log("알림 시스템 초기화 완료");
            yield break;
        }
        
        IEnumerator ProcessNotificationQueue()
        {
            while (true)
            {
                if (notificationQueue.Count > 0 && !isProcessingNotifications)
                {
                    isProcessingNotifications = true;
                    var notification = notificationQueue.Dequeue();
                    ShowNotificationInternal(notification);
                    yield return new WaitForSeconds(0.5f);
                    isProcessingNotifications = false;
                }
                yield return null;
            }
        }
        
        // Public Methods
        /// <summary>
        /// 알림 표시
        /// </summary>
        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info, float duration = 0f)
        {
            if (!settings.enableNotifications) return;
            
            // 타입별 필터링
            if (!ShouldShowNotificationType(type)) return;
            
            var notification = new NotificationData
            {
                title = title,
                message = message,
                type = type,
                duration = duration > 0 ? duration : defaultDuration
            };
            
            notificationQueue.Enqueue(notification);
            AddToHistory(notification);
        }
        
        /// <summary>
        /// 토스트 메시지 표시
        /// </summary>
        public void ShowToast(string message, float duration = 0f)
        {
            if (toastContainer == null || toastPrefab == null) return;
            
            GameObject toast = Instantiate(toastPrefab, toastContainer);
            TextMeshProUGUI text = toast.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
            }
            
            // 애니메이션
            StartCoroutine(AnimateToast(toast, duration > 0 ? duration : toastDuration));
        }
        
        /// <summary>
        /// 플로팅 텍스트 표시
        /// </summary>
        public void ShowFloatingText(string text, Vector3 worldPosition, Color color, float duration = 1f)
        {
            if (floatingTextPrefab == null) return;
            
            GameObject floatingText = Instantiate(floatingTextPrefab);
            floatingText.transform.position = worldPosition;
            
            TextMeshProUGUI tmp = floatingText.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
                tmp.color = color;
            }
            
            StartCoroutine(AnimateFloatingText(floatingText, duration));
        }
        
        void ShowNotificationInternal(NotificationData notification)
        {
            if (notificationContainer == null || notificationPrefab == null) return;
            
            GameObject notificationObj = Instantiate(notificationPrefab, notificationContainer);
            
            // UI 설정
            SetupNotificationUI(notificationObj, notification);
            
            // 사운드 재생
            PlayNotificationSound(notification.type);
            
            // 활성 알림 목록에 추가
            activeNotifications.Add(notificationObj);
            
            // 애니메이션
            StartCoroutine(AnimateNotification(notificationObj, notification));
            
            // 이벤트 발생
            OnNotificationShown?.Invoke(notification);
        }
        
        void SetupNotificationUI(GameObject notificationObj, NotificationData notification)
        {
            // 제목 설정
            Transform titleTransform = notificationObj.transform.Find("Title");
            if (titleTransform != null)
            {
                TextMeshProUGUI titleText = titleTransform.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.text = notification.title;
                }
            }
            
            // 메시지 설정
            Transform messageTransform = notificationObj.transform.Find("Message");
            if (messageTransform != null)
            {
                TextMeshProUGUI messageText = messageTransform.GetComponent<TextMeshProUGUI>();
                if (messageText != null)
                {
                    messageText.text = notification.message;
                }
            }
            
            // 아이콘 설정
            Transform iconTransform = notificationObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = GetIconForType(notification.type);
                    iconImage.color = GetColorForType(notification.type);
                }
            }
            
            // 클릭 이벤트
            Button button = notificationObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnNotificationClick(notification, notificationObj));
            }
        }
        
        void OnNotificationClick(NotificationData notification, GameObject notificationObj)
        {
            notification.isRead = true;
            OnNotificationClicked?.Invoke(notification);
            
            // 알림 제거
            RemoveNotification(notificationObj, notification);
        }
        
        void RemoveNotification(GameObject notificationObj, NotificationData notification)
        {
            activeNotifications.Remove(notificationObj);
            Destroy(notificationObj);
            OnNotificationDismissed?.Invoke(notification);
        }
        
        IEnumerator AnimateNotification(GameObject notificationObj, NotificationData notification)
        {
            // 페이드 인
            CanvasGroup canvasGroup = notificationObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = notificationObj.AddComponent<CanvasGroup>();
            }
            
            float elapsed = 0f;
            float fadeInDuration = 0.3f;
            
            canvasGroup.alpha = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            
            // 대기
            yield return new WaitForSeconds(notification.duration);
            
            // 페이드 아웃
            elapsed = 0f;
            float fadeOutDuration = 0.3f;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            
            // 제거
            RemoveNotification(notificationObj, notification);
        }
        
        IEnumerator AnimateToast(GameObject toast, float duration)
        {
            CanvasGroup canvasGroup = toast.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = toast.AddComponent<CanvasGroup>();
            }
            
            RectTransform rt = toast.GetComponent<RectTransform>();
            Vector2 startPos = rt.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0, 50);
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                
                yield return null;
            }
            
            Destroy(toast);
        }
        
        IEnumerator AnimateFloatingText(GameObject floatingText, float duration)
        {
            float elapsed = 0f;
            Vector3 startPos = floatingText.transform.position;
            Vector3 endPos = startPos + Vector3.up * 2f;
            
            TextMeshProUGUI tmp = floatingText.GetComponentInChildren<TextMeshProUGUI>();
            Color startColor = tmp.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                floatingText.transform.position = Vector3.Lerp(startPos, endPos, t);
                if (tmp != null)
                {
                    tmp.color = Color.Lerp(startColor, endColor, t);
                }
                
                yield return null;
            }
            
            Destroy(floatingText);
        }
        
        bool ShouldShowNotificationType(NotificationType type)
        {
            return type switch
            {
                NotificationType.Quest => settings.enableQuestNotifications,
                NotificationType.Achievement => settings.enableAchievementNotifications,
                NotificationType.LevelUp => settings.enableLevelUpNotifications,
                NotificationType.Resource => settings.enableResourceNotifications,
                NotificationType.Battle => settings.enableBattleNotifications,
                NotificationType.System => settings.enableSystemNotifications,
                _ => true
            };
        }
        
        void PlayNotificationSound(NotificationType type)
        {
            if (!settings.playSounds) return;
            
            AudioClip clip = type switch
            {
                NotificationType.Achievement => achievementSound,
                NotificationType.Warning => warningSound,
                NotificationType.Error => errorSound,
                _ => notificationSound
            };
            
            if (clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clip);
            }
        }
        
        Sprite GetIconForType(NotificationType type)
        {
            // TODO: 실제 아이콘 리소스 로드
            return null;
        }
        
        Color GetColorForType(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => Color.green,
                NotificationType.Warning => Color.yellow,
                NotificationType.Error => Color.red,
                NotificationType.Achievement => new Color(1f, 0.8f, 0f), // Gold
                NotificationType.LevelUp => Color.cyan,
                _ => Color.white
            };
        }
        
        void AddToHistory(NotificationData notification)
        {
            notificationHistory.Add(notification);
            
            // 최대 개수 제한
            while (notificationHistory.Count > maxNotifications)
            {
                notificationHistory.RemoveAt(0);
            }
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
        }
        
        void LoadSettings()
        {
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
        
        // 히스토리 관련 메서드
        public List<NotificationData> GetNotificationHistory()
        {
            return new List<NotificationData>(notificationHistory);
        }
        
        public List<NotificationData> GetUnreadNotifications()
        {
            return notificationHistory.FindAll(n => !n.isRead);
        }
        
        public void MarkAllAsRead()
        {
            foreach (var notification in notificationHistory)
            {
                notification.isRead = true;
            }
        }
        
        public void ClearHistory()
        {
            notificationHistory.Clear();
        }
    }
}