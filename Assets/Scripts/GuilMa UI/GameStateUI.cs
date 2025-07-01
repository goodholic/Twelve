using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Systems;

namespace GuildMaster.UI
{
    /// <summary>
    /// 게임 상태 UI
    /// 일/시간, 시즌, 이벤트 표시
    /// </summary>
    public class GameStateUI : MonoBehaviour
    {
        [Header("Time Display")]
        [SerializeField] private GameObject timePanel;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI timeOfDayText;
        [SerializeField] private TextMeshProUGUI seasonText;
        [SerializeField] private Image dayProgressBar;
        [SerializeField] private Image timeOfDayIcon;
        [SerializeField] private Gradient dayProgressGradient;
        
        [Header("Time Icons")]
        [SerializeField] private Sprite morningIcon;
        [SerializeField] private Sprite afternoonIcon;
        [SerializeField] private Sprite eveningIcon;
        [SerializeField] private Sprite nightIcon;
        
        [Header("Season Display")]
        [SerializeField] private Image seasonIcon;
        [SerializeField] private Sprite springIcon;
        [SerializeField] private Sprite summerIcon;
        [SerializeField] private Sprite autumnIcon;
        [SerializeField] private Sprite winterIcon;
        [SerializeField] private ParticleSystem seasonParticles;
        
        [Header("Speed Control")]
        [SerializeField] private GameObject speedControlPanel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speed1xButton;
        [SerializeField] private Button speed2xButton;
        [SerializeField] private Button speed4xButton;
        [SerializeField] private TextMeshProUGUI currentSpeedText;
        [SerializeField] private Image pauseIcon;
        
        [Header("Daily Progress")]
        [SerializeField] private GameObject dailyProgressPanel;
        [SerializeField] private TextMeshProUGUI battlesText;
        [SerializeField] private TextMeshProUGUI dungeonsText;
        [SerializeField] private TextMeshProUGUI questsText;
        [SerializeField] private TextMeshProUGUI resourcesText;
        [SerializeField] private Slider battlesProgressBar;
        [SerializeField] private Slider dungeonsProgressBar;
        
        [Header("Event Notifications")]
        [SerializeField] private GameObject eventNotificationPrefab;
        [SerializeField] private Transform eventNotificationContainer;
        [SerializeField] private float notificationDuration = 5f;
        [SerializeField] private int maxNotifications = 5;
        
        [Header("Daily Report")]
        [SerializeField] private GameObject dailyReportPanel;
        [SerializeField] private TextMeshProUGUI reportDayText;
        [SerializeField] private Transform reportContentContainer;
        [SerializeField] private GameObject reportItemPrefab;
        [SerializeField] private Button closeDailyReportButton;
        
        [Header("Game State Indicators")]
        [SerializeField] private GameObject pausedIndicator;
        [SerializeField] private GameObject battleIndicator;
        [SerializeField] private GameObject storyIndicator;
        [SerializeField] private GameObject maintenanceWarning;
        
        [Header("Victory/GameOver")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private TextMeshProUGUI victoryScoreText;
        [SerializeField] private TextMeshProUGUI victoryDaysText;
        [SerializeField] private Button victoryMainMenuButton;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverReasonText;
        [SerializeField] private TextMeshProUGUI gameOverDaysText;
        [SerializeField] private Button gameOverMainMenuButton;
        
        // System references
        private GameLoopManager gameLoopManager;
        private Core.ResourceManager resourceManager;
        
        // UI State
        private List<GameObject> activeNotifications = new List<GameObject>();
        private float currentGameSpeed = 1f;
        private Coroutine timeUpdateCoroutine;
        
        void Start()
        {
            gameLoopManager = GameLoopManager.Instance;
            resourceManager = Core.GameManager.Instance?.ResourceManager;
            
            if (gameLoopManager == null)
            {
                Debug.LogError("GameLoopManager not found!");
                enabled = false;
                return;
            }
            
            SetupUI();
            SubscribeToEvents();
            
            // 초기 UI 업데이트
            UpdateTimeDisplay();
            UpdateDailyProgress();
            UpdateSpeedDisplay();
            
            timeUpdateCoroutine = StartCoroutine(UpdateTimeCoroutine());
        }
        
        void SetupUI()
        {
            // Speed control buttons
            if (pauseButton != null)
                pauseButton.onClick.AddListener(TogglePause);
            if (speed1xButton != null)
                speed1xButton.onClick.AddListener(() => SetGameSpeed(1f));
            if (speed2xButton != null)
                speed2xButton.onClick.AddListener(() => SetGameSpeed(2f));
            if (speed4xButton != null)
                speed4xButton.onClick.AddListener(() => SetGameSpeed(4f));
            
            // Report buttons
            if (closeDailyReportButton != null)
                closeDailyReportButton.onClick.AddListener(() => dailyReportPanel.SetActive(false));
            
            // Victory/GameOver buttons
            if (victoryMainMenuButton != null)
                victoryMainMenuButton.onClick.AddListener(ReturnToMainMenu);
            if (gameOverMainMenuButton != null)
                gameOverMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        
        void SubscribeToEvents()
        {
            if (gameLoopManager != null)
            {
                gameLoopManager.OnDayChanged += OnDayChanged;
                gameLoopManager.OnSeasonChanged += OnSeasonChanged;
                gameLoopManager.OnGameStateChanged += OnGameStateChanged;
                gameLoopManager.OnDailyEventTriggered += OnDailyEventTriggered;
                gameLoopManager.OnDayProgressChanged += OnDayProgressChanged;
            }
        }
        
        void OnDestroy()
        {
            if (timeUpdateCoroutine != null)
            {
                StopCoroutine(timeUpdateCoroutine);
            }
            
            // 이벤트 구독 해제
            if (gameLoopManager != null)
            {
                gameLoopManager.OnDayChanged -= OnDayChanged;
                gameLoopManager.OnSeasonChanged -= OnSeasonChanged;
                gameLoopManager.OnGameStateChanged -= OnGameStateChanged;
                gameLoopManager.OnDailyEventTriggered -= OnDailyEventTriggered;
                gameLoopManager.OnDayProgressChanged -= OnDayProgressChanged;
            }
        }
        
        IEnumerator UpdateTimeCoroutine()
        {
            while (true)
            {
                UpdateTimeDisplay();
                UpdateDailyProgress();
                CheckMaintenanceWarning();
                
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }
        
        void UpdateTimeDisplay()
        {
            if (gameLoopManager == null) return;
            
            // Day display
            if (dayText != null)
            {
                dayText.text = $"Day {gameLoopManager.CurrentDay}";
            }
            
            // Time of day
            var timeOfDay = gameLoopManager.GetTimeOfDay();
            if (timeOfDayText != null)
            {
                timeOfDayText.text = GetTimeOfDayText(timeOfDay);
            }
            
            // Time icon
            if (timeOfDayIcon != null)
            {
                timeOfDayIcon.sprite = GetTimeOfDayIcon(timeOfDay);
                timeOfDayIcon.color = GetTimeOfDayColor(timeOfDay);
            }
            
            // Day progress bar
            float dayProgress = gameLoopManager.GetDayProgress();
            if (dayProgressBar != null)
            {
                dayProgressBar.fillAmount = dayProgress;
                if (dayProgressGradient != null)
                {
                    dayProgressBar.color = dayProgressGradient.Evaluate(dayProgress);
                }
            }
            
            // Season
            var season = gameLoopManager.GetCurrentSeason();
            if (seasonText != null)
            {
                seasonText.text = GetSeasonText(season);
            }
            
            if (seasonIcon != null)
            {
                seasonIcon.sprite = GetSeasonIcon(season);
            }
            
            // Season particles
            UpdateSeasonParticles(season);
        }
        
        string GetTimeOfDayText(GameLoopManager.TimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                GameLoopManager.TimeOfDay.Morning => "아침",
                GameLoopManager.TimeOfDay.Afternoon => "오후",
                GameLoopManager.TimeOfDay.Evening => "저녁",
                GameLoopManager.TimeOfDay.Night => "밤",
                _ => timeOfDay.ToString()
            };
        }
        
        Sprite GetTimeOfDayIcon(GameLoopManager.TimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                GameLoopManager.TimeOfDay.Morning => morningIcon,
                GameLoopManager.TimeOfDay.Afternoon => afternoonIcon,
                GameLoopManager.TimeOfDay.Evening => eveningIcon,
                GameLoopManager.TimeOfDay.Night => nightIcon,
                _ => null
            };
        }
        
        Color GetTimeOfDayColor(GameLoopManager.TimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                GameLoopManager.TimeOfDay.Morning => new Color(1f, 0.9f, 0.5f), // Warm yellow
                GameLoopManager.TimeOfDay.Afternoon => new Color(1f, 1f, 0.8f), // Bright
                GameLoopManager.TimeOfDay.Evening => new Color(1f, 0.5f, 0.3f), // Orange
                GameLoopManager.TimeOfDay.Night => new Color(0.3f, 0.3f, 0.8f), // Dark blue
                _ => Color.white
            };
        }
        
        string GetSeasonText(GameLoopManager.Season season)
        {
            return season switch
            {
                GameLoopManager.Season.Spring => "봄",
                GameLoopManager.Season.Summer => "여름",
                GameLoopManager.Season.Autumn => "가을",
                GameLoopManager.Season.Winter => "겨울",
                _ => season.ToString()
            };
        }
        
        Sprite GetSeasonIcon(GameLoopManager.Season season)
        {
            return season switch
            {
                GameLoopManager.Season.Spring => springIcon,
                GameLoopManager.Season.Summer => summerIcon,
                GameLoopManager.Season.Autumn => autumnIcon,
                GameLoopManager.Season.Winter => winterIcon,
                _ => null
            };
        }
        
        void UpdateSeasonParticles(GameLoopManager.Season season)
        {
            if (seasonParticles == null) return;
            
            var main = seasonParticles.main;
            var emission = seasonParticles.emission;
            
            switch (season)
            {
                case GameLoopManager.Season.Spring:
                    // Cherry blossoms
                    main.startColor = new Color(1f, 0.7f, 0.8f);
                    emission.rateOverTime = 5f;
                    break;
                    
                case GameLoopManager.Season.Summer:
                    // Minimal particles
                    emission.rateOverTime = 0f;
                    break;
                    
                case GameLoopManager.Season.Autumn:
                    // Falling leaves
                    main.startColor = new Color(1f, 0.5f, 0f);
                    emission.rateOverTime = 10f;
                    break;
                    
                case GameLoopManager.Season.Winter:
                    // Snow
                    main.startColor = Color.white;
                    emission.rateOverTime = 20f;
                    break;
            }
        }
        
        void UpdateDailyProgress()
        {
            var progress = gameLoopManager?.GetTodayProgress();
            if (progress == null) return;
            
            // Battle progress
            if (battlesText != null)
                battlesText.text = $"전투: {progress.battlesCompleted}/5";
            
            if (battlesProgressBar != null)
                battlesProgressBar.value = progress.battlesCompleted / 5f;
            
            // Dungeon progress
            if (dungeonsText != null)
                dungeonsText.text = $"던전: {progress.dungeonsCompleted}/3";
            
            if (dungeonsProgressBar != null)
                dungeonsProgressBar.value = progress.dungeonsCompleted / 3f;
            
            // Quest progress
            if (questsText != null)
                questsText.text = $"퀘스트: {progress.questsCompleted}";
            
            // Resources gathered
            if (resourcesText != null)
                resourcesText.text = $"자원: {progress.resourcesGathered:F0}";
        }
        
        void UpdateSpeedDisplay()
        {
            if (currentSpeedText != null)
            {
                currentSpeedText.text = gameLoopManager.CurrentState == GameLoopManager.GameState.Paused 
                    ? "일시정지" 
                    : $"{currentGameSpeed:F0}x";
            }
            
            // Update button highlights
            UpdateSpeedButtonHighlights();
        }
        
        void UpdateSpeedButtonHighlights()
        {
            Color normalColor = new Color(0.8f, 0.8f, 0.8f);
            Color selectedColor = Color.white;
            
            if (speed1xButton != null)
                speed1xButton.image.color = currentGameSpeed == 1f ? selectedColor : normalColor;
            if (speed2xButton != null)
                speed2xButton.image.color = currentGameSpeed == 2f ? selectedColor : normalColor;
            if (speed4xButton != null)
                speed4xButton.image.color = currentGameSpeed == 4f ? selectedColor : normalColor;
        }
        
        void CheckMaintenanceWarning()
        {
            if (maintenanceWarning == null || resourceManager == null) return;
            
            // Calculate expected maintenance cost
            int maintenanceCost = 100; // Base cost, would be calculated properly
            bool canAfford = resourceManager.GetGold() >= maintenanceCost;
            
            // Show warning if low on gold and approaching end of day
            float dayProgress = gameLoopManager.GetDayProgress();
            bool showWarning = !canAfford && dayProgress > 0.8f;
            
            maintenanceWarning.SetActive(showWarning);
        }
        
        void TogglePause()
        {
            if (gameLoopManager.CurrentState == GameLoopManager.GameState.Playing)
            {
                gameLoopManager.PauseGame();
                if (pauseIcon != null)
                {
                    // Change to play icon
                }
            }
            else if (gameLoopManager.CurrentState == GameLoopManager.GameState.Paused)
            {
                gameLoopManager.ResumeGame();
                if (pauseIcon != null)
                {
                    // Change to pause icon
                }
            }
        }
        
        void SetGameSpeed(float speed)
        {
            currentGameSpeed = speed;
            gameLoopManager.SetGameSpeed(speed);
            UpdateSpeedDisplay();
        }
        
        void ShowDailyReport(int day)
        {
            if (dailyReportPanel == null) return;
            
            dailyReportPanel.SetActive(true);
            
            if (reportDayText != null)
                reportDayText.text = $"Day {day} 보고서";
            
            // Clear previous items
            foreach (Transform child in reportContentContainer)
            {
                Destroy(child.gameObject);
            }
            
            var progress = gameLoopManager.GetTodayProgress();
            var events = gameLoopManager.GetTodayEvents();
            
            // Add report items
            CreateReportItem("전투", $"{progress.battlesCompleted}회 완료");
            CreateReportItem("던전", $"{progress.dungeonsCompleted}회 탐험");
            CreateReportItem("퀘스트", $"{progress.questsCompleted}개 완료");
            CreateReportItem("상인", $"{progress.merchantsVisited}명 방문");
            CreateReportItem("수집 자원", $"{progress.resourcesGathered:F0}");
            
            // Add events
            if (events.Count > 0)
            {
                CreateReportItem("===== 이벤트 =====", "");
                foreach (var evt in events)
                {
                    CreateReportItem(evt.eventName, evt.isCompleted ? "완료" : "진행중");
                }
            }
        }
        
        void CreateReportItem(string label, string value)
        {
            if (reportItemPrefab == null || reportContentContainer == null) return;
            
            var item = Instantiate(reportItemPrefab, reportContentContainer);
            
            var labelText = item.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            var valueText = item.transform.Find("Value")?.GetComponent<TextMeshProUGUI>();
            
            if (labelText != null) labelText.text = label;
            if (valueText != null) valueText.text = value;
        }
        
        void ShowEventNotification(GameLoopManager.DailyEvent dailyEvent)
        {
            if (eventNotificationPrefab == null || eventNotificationContainer == null) return;
            
            // Remove old notifications if at max
            while (activeNotifications.Count >= maxNotifications)
            {
                var oldest = activeNotifications[0];
                activeNotifications.RemoveAt(0);
                Destroy(oldest);
            }
            
            // Create new notification
            var notification = Instantiate(eventNotificationPrefab, eventNotificationContainer);
            activeNotifications.Add(notification);
            
            // Set notification content
            var titleText = notification.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var descText = notification.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            var icon = notification.transform.Find("Icon")?.GetComponent<Image>();
            
            if (titleText != null) titleText.text = dailyEvent.eventName;
            if (descText != null) descText.text = dailyEvent.description;
            if (icon != null) icon.color = GetEventTypeColor(dailyEvent.type);
            
            // Animate in
            StartCoroutine(AnimateNotification(notification));
            
            // Auto remove after duration
            StartCoroutine(RemoveNotificationAfterDelay(notification, notificationDuration));
        }
        
        Color GetEventTypeColor(GameLoopManager.DailyEvent.EventType type)
        {
            return type switch
            {
                GameLoopManager.DailyEvent.EventType.MerchantVisit => new Color(1f, 0.8f, 0.2f), // Gold
                GameLoopManager.DailyEvent.EventType.GuildChallenge => new Color(1f, 0.3f, 0.3f), // Red
                GameLoopManager.DailyEvent.EventType.StoryEvent => new Color(0.8f, 0.5f, 1f), // Purple
                GameLoopManager.DailyEvent.EventType.SpecialDungeon => new Color(0.3f, 0.8f, 1f), // Cyan
                GameLoopManager.DailyEvent.EventType.ResourceBonus => new Color(0.3f, 1f, 0.3f), // Green
                GameLoopManager.DailyEvent.EventType.SeasonalEvent => new Color(1f, 0.5f, 0f), // Orange
                _ => Color.white
            };
        }
        
        IEnumerator AnimateNotification(GameObject notification)
        {
            var rectTransform = notification.GetComponent<RectTransform>();
            if (rectTransform == null) yield break;
            
            // Slide in from right
            Vector2 startPos = rectTransform.anchoredPosition;
            startPos.x += 300f;
            Vector2 endPos = rectTransform.anchoredPosition;
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }
        }
        
        IEnumerator RemoveNotificationAfterDelay(GameObject notification, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (notification != null && activeNotifications.Contains(notification))
            {
                activeNotifications.Remove(notification);
                
                // Fade out
                var canvasGroup = notification.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = notification.AddComponent<CanvasGroup>();
                
                float fadeDuration = 0.3f;
                float elapsed = 0f;
                
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = 1f - (elapsed / fadeDuration);
                    yield return null;
                }
                
                Destroy(notification);
            }
        }
        
        void ShowVictory(int score)
        {
            if (victoryPanel == null) return;
            
            victoryPanel.SetActive(true);
            
            if (victoryScoreText != null)
                victoryScoreText.text = $"최종 점수: {score:N0}";
            
            if (victoryDaysText != null)
                victoryDaysText.text = $"{gameLoopManager.CurrentDay}일 만에 달성!";
            
            // Play victory effects
            // TODO: Add particle effects, sound, etc.
        }
        
        void ShowGameOver(string reason)
        {
            if (gameOverPanel == null) return;
            
            gameOverPanel.SetActive(true);
            
            if (gameOverReasonText != null)
                gameOverReasonText.text = reason;
            
            if (gameOverDaysText != null)
                gameOverDaysText.text = $"{gameLoopManager.CurrentDay}일 동안 버텼습니다.";
        }
        
        void ReturnToMainMenu()
        {
            // TODO: Implement return to main menu
            Debug.Log("Returning to main menu...");
        }
        
        // Event handlers
        void OnDayChanged(int newDay)
        {
            // Show daily report for previous day
            if (newDay > 1)
            {
                ShowDailyReport(newDay - 1);
            }
            
            // Update display
            UpdateTimeDisplay();
        }
        
        void OnSeasonChanged(int newSeason)
        {
            UpdateTimeDisplay();
            
            // Season change notification
            var season = (GameLoopManager.Season)newSeason;
            ShowNotification($"{GetSeasonText(season)} 시즌 시작!", GameLoopManager.NotificationType.Important);
        }
        
        void OnGameStateChanged(GameLoopManager.GameState newState)
        {
            // Update state indicators
            if (pausedIndicator != null)
                pausedIndicator.SetActive(newState == GameLoopManager.GameState.Paused);
            
            if (battleIndicator != null)
                battleIndicator.SetActive(newState == GameLoopManager.GameState.Battle);
            
            if (storyIndicator != null)
                storyIndicator.SetActive(newState == GameLoopManager.GameState.Story);
            
            // Handle victory/game over
            switch (newState)
            {
                case GameLoopManager.GameState.Victory:
                    int score = PlayerPrefs.GetInt("VictoryScore", 0);
                    ShowVictory(score);
                    break;
                    
                case GameLoopManager.GameState.GameOver:
                    string reason = DetermineGameOverReason();
                    ShowGameOver(reason);
                    break;
            }
            
            UpdateSpeedDisplay();
        }
        
        string DetermineGameOverReason()
        {
            if (resourceManager != null && resourceManager.GetGold() < -1000)
                return "파산: 길드가 빚에 허덕이다 문을 닫았습니다.";
            
            var guildData = Core.GameManager.Instance?.GuildManager?.GetGuildData();
            if (guildData != null)
            {
                if (guildData.Adventurers.Count == 0)
                    return "인력 부족: 모든 모험가가 길드를 떠났습니다.";
                
                if (guildData.GuildReputation <= 0)
                    return "명성 실추: 길드의 명성이 바닥을 쳤습니다.";
            }
            
            return "길드 운영에 실패했습니다.";
        }
        
        void OnDailyEventTriggered(GameLoopManager.DailyEvent dailyEvent)
        {
            ShowEventNotification(dailyEvent);
        }
        
        void OnDayProgressChanged(float progress)
        {
            // Day progress is updated in UpdateTimeDisplay
        }
        
        void ShowNotification(string message, GameLoopManager.NotificationType type)
        {
            // Create a simple daily event for the notification
            var notificationEvent = new GameLoopManager.DailyEvent
            {
                eventId = $"notification_{System.Guid.NewGuid()}",
                eventName = type.ToString(),
                description = message,
                type = GameLoopManager.DailyEvent.EventType.ResourceBonus // Default type
            };
            
            ShowEventNotification(notificationEvent);
        }
    }
}