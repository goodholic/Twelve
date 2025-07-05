using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.Core;
using GuildMaster.Systems;
using GuildMaster.Data;

namespace GuildMaster.UI
{
    /// <summary>
    /// 게임 상태 UI - 시간, 날짜, 계절, 속도 조절 등을 표시
    /// </summary>
    public class GameStateUI : MonoBehaviour
    {
        [Header("Time Display")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI seasonText;
        [SerializeField] private Image dayProgressBar;
        [SerializeField] private Image seasonIcon;
        
        [Header("Speed Control")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speed1xButton;
        [SerializeField] private Button speed2xButton;
        [SerializeField] private Button speed4xButton;
        [SerializeField] private Image pauseIcon;
        [SerializeField] private TextMeshProUGUI currentSpeedText;
        
        [Header("Daily Report")]
        [SerializeField] private GameObject dailyReportPanel;
        [SerializeField] private TextMeshProUGUI reportTitleText;
        [SerializeField] private TextMeshProUGUI reportContentText;
        [SerializeField] private Button reportCloseButton;
        [SerializeField] private float reportDisplayDuration = 5f;
        
        [Header("Event Notifications")]
        [SerializeField] private GameObject eventNotificationPrefab;
        [SerializeField] private Transform eventNotificationContainer;
        [SerializeField] private int maxNotifications = 5;
        
        [Header("Resources Display")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI manaStoneText;
        
        [Header("Season Icons")]
        [SerializeField] private Sprite springIcon;
        [SerializeField] private Sprite summerIcon;
        [SerializeField] private Sprite autumnIcon;
        [SerializeField] private Sprite winterIcon;
        
        [Header("Animation")]
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private AnimationCurve dayProgressCurve;
        
        // References
        private GameLoopManager gameLoopManager;
        private ResourceManager resourceManager;
        
        // State
        private bool isInitialized = false;
        private float lastUpdateTime = 0f;
        private Coroutine dailyReportCoroutine;
        
        private void Awake()
        {
            SetupButtons();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            // Get references
            gameLoopManager = GameLoopManager.Instance;
            resourceManager = GameManager.Instance?.ResourceManager;
            
            if (gameLoopManager == null)
            {
                Debug.LogError("GameLoopManager not found!");
                return;
            }
            
            // Subscribe to events
            gameLoopManager.OnDayChanged += OnDayChanged;
            gameLoopManager.OnSeasonChanged += OnSeasonChanged;
            gameLoopManager.OnDayProgressChanged += OnDayProgressChanged;
            gameLoopManager.OnDailyEventTriggered += OnDailyEventTriggered;
            gameLoopManager.OnGameStateChanged += OnGameStateChanged;
            
            // Initial update
            UpdateDisplay();
            isInitialized = true;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (gameLoopManager != null)
            {
                gameLoopManager.OnDayChanged -= OnDayChanged;
                gameLoopManager.OnSeasonChanged -= OnSeasonChanged;
                gameLoopManager.OnDayProgressChanged -= OnDayProgressChanged;
                gameLoopManager.OnDailyEventTriggered -= OnDailyEventTriggered;
                gameLoopManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Update display at intervals
            if (Time.time - lastUpdateTime > updateInterval)
            {
                UpdateDisplay();
                lastUpdateTime = Time.time;
            }
        }
        
        private void SetupButtons()
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
                
            // Report close button
            if (reportCloseButton != null)
                reportCloseButton.onClick.AddListener(CloseDailyReport);
        }
        
        private void UpdateDisplay()
        {
            if (gameLoopManager == null) return;
            
            // Update time
            UpdateTimeDisplay();
            
            // Update resources
            UpdateResourceDisplay();
            
            // Update speed indicator
            UpdateSpeedDisplay();
        }
        
        private void UpdateTimeDisplay()
        {
            // Time of day
            var timeOfDay = gameLoopManager.GetTimeOfDay();
            string timeString = GetTimeString(timeOfDay);
            if (timeText != null)
                timeText.text = timeString;
            
            // Day
            if (dayText != null)
                dayText.text = $"Day {gameLoopManager.CurrentDay}";
            
            // Season
            var season = gameLoopManager.GetCurrentSeason();
            if (seasonText != null)
                seasonText.text = GetSeasonName(season);
            
            // Season icon
            if (seasonIcon != null)
                seasonIcon.sprite = GetSeasonIcon(season);
            
            // Day progress bar
            if (dayProgressBar != null)
            {
                float progress = gameLoopManager.GetDayProgress();
                dayProgressBar.fillAmount = dayProgressCurve.Evaluate(progress);
            }
        }
        
        private void UpdateResourceDisplay()
        {
            if (resourceManager == null) return;
            
            var resources = resourceManager.GetResources();
            
            if (goldText != null)
                goldText.text = FormatNumber(resources.Gold);
                
            if (woodText != null)
                woodText.text = FormatNumber(resources.Wood);
                
            if (stoneText != null)
                stoneText.text = FormatNumber(resources.Stone);
                
            if (manaStoneText != null)
                manaStoneText.text = FormatNumber(resources.ManaStone);
        }
        
        private void UpdateSpeedDisplay()
        {
            if (gameLoopManager == null) return;
            
            float gameSpeed = Time.timeScale;
            bool isPaused = gameLoopManager.CurrentState == GameLoopManager.GameState.Paused;
            
            // Update pause button icon
            if (pauseIcon != null)
            {
                pauseIcon.gameObject.SetActive(isPaused);
            }
            
            // Update current speed text
            if (currentSpeedText != null)
            {
                if (isPaused)
                    currentSpeedText.text = "PAUSED";
                else
                    currentSpeedText.text = $"{gameSpeed:F1}x";
            }
            
            // Update button states
            UpdateSpeedButtonStates(gameSpeed);
        }
        
        private void UpdateSpeedButtonStates(float currentSpeed)
        {
            // Reset all buttons
            SetButtonHighlight(speed1xButton, false);
            SetButtonHighlight(speed2xButton, false);
            SetButtonHighlight(speed4xButton, false);
            
            // Highlight active speed
            if (Mathf.Approximately(currentSpeed, 1f))
                SetButtonHighlight(speed1xButton, true);
            else if (Mathf.Approximately(currentSpeed, 2f))
                SetButtonHighlight(speed2xButton, true);
            else if (Mathf.Approximately(currentSpeed, 4f))
                SetButtonHighlight(speed4xButton, true);
        }
        
        private void SetButtonHighlight(Button button, bool highlighted)
        {
            if (button == null) return;
            
            var colors = button.colors;
            colors.normalColor = highlighted ? Color.yellow : Color.white;
            button.colors = colors;
        }
        
        // Event handlers
        private void OnDayChanged(int newDay)
        {
            // Show daily report
            ShowDailyReport(newDay - 1);
            
            // Update display
            UpdateDisplay();
            
            // Play day change animation
            StartCoroutine(PlayDayChangeAnimation());
        }
        
        private void OnSeasonChanged(int newSeason)
        {
            // Update season display
            UpdateDisplay();
            
            // Show season change notification
            ShowNotification($"{GetSeasonName((GameLoopManager.Season)newSeason)} has arrived!", GuildMaster.Data.NotificationType.System);
            
            // Play season change effect
            StartCoroutine(PlaySeasonChangeEffect());
        }
        
        private void OnDayProgressChanged(float progress)
        {
            // Update progress bar smoothly
            if (dayProgressBar != null)
            {
                dayProgressBar.fillAmount = Mathf.Lerp(
                    dayProgressBar.fillAmount, 
                    dayProgressCurve.Evaluate(progress), 
                    Time.deltaTime * 5f
                );
            }
        }
        
        private void OnDailyEventTriggered(GameLoopManager.DailyEvent dailyEvent)
        {
            // Show event notification
            ShowEventNotification(dailyEvent);
        }
        
        private void OnGameStateChanged(GameLoopManager.GameState newState)
        {
            // Update UI based on game state
            switch (newState)
            {
                case GameLoopManager.GameState.Paused:
                    ShowPausedOverlay();
                    break;
                    
                case GameLoopManager.GameState.Playing:
                    HidePausedOverlay();
                    break;
                    
                case GameLoopManager.GameState.Victory:
                    ShowVictoryScreen();
                    break;
                    
                case GameLoopManager.GameState.GameOver:
                    ShowGameOverScreen();
                    break;
            }
        }
        
        // UI Actions
        private void TogglePause()
        {
            if (gameLoopManager == null) return;
            
            if (gameLoopManager.CurrentState == GameLoopManager.GameState.Playing)
                gameLoopManager.PauseGame();
            else if (gameLoopManager.CurrentState == GameLoopManager.GameState.Paused)
                gameLoopManager.ResumeGame();
        }
        
        private void SetGameSpeed(float speed)
        {
            if (gameLoopManager == null) return;
            
            gameLoopManager.SetGameSpeed(speed);
            UpdateSpeedDisplay();
        }
        
        private void ShowDailyReport(int day)
        {
            if (dailyReportPanel == null) return;
            
            // Stop any existing coroutine
            if (dailyReportCoroutine != null)
                StopCoroutine(dailyReportCoroutine);
            
            // Get daily progress
            var progress = gameLoopManager.GetTodayProgress();
            
            // Set report content
            if (reportTitleText != null)
                reportTitleText.text = $"Day {day} Report";
                
            if (reportContentText != null)
            {
                string report = $"Battles: {progress.battlesCompleted}\n";
                report += $"Dungeons: {progress.dungeonsCompleted}\n";
                report += $"Quests: {progress.questsCompleted}\n";
                report += $"Merchants: {progress.merchantsVisited}\n";
                report += $"Resources: {progress.resourcesGathered:F0}";
                
                reportContentText.text = report;
            }
            
            // Show panel
            dailyReportPanel.SetActive(true);
            
            // Auto-hide after duration
            dailyReportCoroutine = StartCoroutine(AutoHideDailyReport());
        }
        
        private IEnumerator AutoHideDailyReport()
        {
            yield return new WaitForSeconds(reportDisplayDuration);
            CloseDailyReport();
        }
        
        private void CloseDailyReport()
        {
            if (dailyReportPanel != null)
                dailyReportPanel.SetActive(false);
                
            if (dailyReportCoroutine != null)
            {
                StopCoroutine(dailyReportCoroutine);
                dailyReportCoroutine = null;
            }
        }
        
        private void ShowEventNotification(GameLoopManager.DailyEvent dailyEvent)
        {
            if (eventNotificationPrefab == null || eventNotificationContainer == null) return;
            
            // Check notification limit
            if (eventNotificationContainer.childCount >= maxNotifications)
            {
                // Remove oldest notification
                Destroy(eventNotificationContainer.GetChild(0).gameObject);
            }
            
            // Create new notification
            var notification = Instantiate(eventNotificationPrefab, eventNotificationContainer);
            
            // Set notification content
            var titleText = notification.GetComponentInChildren<TextMeshProUGUI>();
            if (titleText != null)
                titleText.text = dailyEvent.eventName;
            
            // Auto-destroy after delay
            Destroy(notification, 5f);
        }
        
        private void ShowNotification(string message, GuildMaster.Data.NotificationType type)
        {
            // This would integrate with a notification system
            Debug.Log($"[{type}] {message}");
        }
        
        private void ShowPausedOverlay()
        {
            // Show pause overlay
            if (pauseIcon != null)
                pauseIcon.gameObject.SetActive(true);
        }
        
        private void HidePausedOverlay()
        {
            // Hide pause overlay
            if (pauseIcon != null)
                pauseIcon.gameObject.SetActive(false);
        }
        
        private void ShowVictoryScreen()
        {
            // This would show the victory UI
            Debug.Log("Victory!");
        }
        
        private void ShowGameOverScreen()
        {
            // This would show the game over UI
            Debug.Log("Game Over!");
        }
        
        // Animation coroutines
        private IEnumerator PlayDayChangeAnimation()
        {
            // Simple fade animation for day text
            if (dayText != null)
            {
                var canvasGroup = dayText.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = dayText.gameObject.AddComponent<CanvasGroup>();
                
                // Fade out
                float t = 0;
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    canvasGroup.alpha = 1f - (t * 2f);
                    yield return null;
                }
                
                // Update text
                dayText.text = $"Day {gameLoopManager.CurrentDay}";
                
                // Fade in
                t = 0;
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    canvasGroup.alpha = t * 2f;
                    yield return null;
                }
                
                canvasGroup.alpha = 1f;
            }
        }
        
        private IEnumerator PlaySeasonChangeEffect()
        {
            // Rotate season icon
            if (seasonIcon != null)
            {
                float duration = 1f;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float rotation = Mathf.Lerp(0, 360, elapsed / duration);
                    seasonIcon.transform.rotation = Quaternion.Euler(0, 0, -rotation);
                    yield return null;
                }
                
                seasonIcon.transform.rotation = Quaternion.identity;
            }
        }
        
        // Utility methods
        private string GetTimeString(GameLoopManager.TimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                GameLoopManager.TimeOfDay.Morning => "Morning",
                GameLoopManager.TimeOfDay.Afternoon => "Afternoon",
                GameLoopManager.TimeOfDay.Evening => "Evening",
                GameLoopManager.TimeOfDay.Night => "Night",
                _ => timeOfDay.ToString()
            };
        }
        
        private string GetSeasonName(GameLoopManager.Season season)
        {
            return season switch
            {
                GameLoopManager.Season.Spring => "Spring",
                GameLoopManager.Season.Summer => "Summer",
                GameLoopManager.Season.Autumn => "Autumn",
                GameLoopManager.Season.Winter => "Winter",
                _ => season.ToString()
            };
        }
        
        private Sprite GetSeasonIcon(GameLoopManager.Season season)
        {
            return season switch
            {
                GameLoopManager.Season.Spring => springIcon,
                GameLoopManager.Season.Summer => summerIcon,
                GameLoopManager.Season.Autumn => autumnIcon,
                GameLoopManager.Season.Winter => winterIcon,
                _ => springIcon
            };
        }
        
        private string FormatNumber(int number)
        {
            if (number >= 1000000)
                return $"{number / 1000000f:F1}M";
            else if (number >= 1000)
                return $"{number / 1000f:F1}K";
            else
                return number.ToString();
        }
    }
}