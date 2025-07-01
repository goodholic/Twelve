using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.Systems;

namespace GuildMaster.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Settings Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject graphicsPanel;
        [SerializeField] private GameObject audioPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private GameObject accessibilityPanel;
        
        [Header("Tab Buttons")]
        [SerializeField] private Button graphicsTabButton;
        [SerializeField] private Button audioTabButton;
        [SerializeField] private Button gameplayTabButton;
        [SerializeField] private Button controlsTabButton;
        [SerializeField] private Button accessibilityTabButton;
        
        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Slider fpsSlider;
        [SerializeField] private TextMeshProUGUI fpsValueText;
        
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider voiceVolumeSlider;
        [SerializeField] private Slider ambientVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeText;
        [SerializeField] private TextMeshProUGUI musicVolumeText;
        [SerializeField] private TextMeshProUGUI sfxVolumeText;
        [SerializeField] private TextMeshProUGUI voiceVolumeText;
        [SerializeField] private TextMeshProUGUI ambientVolumeText;
        [SerializeField] private Button testSoundButton;
        
        [Header("Gameplay Settings")]
        [SerializeField] private Slider battleSpeedSlider;
        [SerializeField] private TextMeshProUGUI battleSpeedText;
        [SerializeField] private Toggle autoSaveToggle;
        [SerializeField] private TMP_Dropdown autoSaveIntervalDropdown;
        [SerializeField] private Toggle showDamageToggle;
        [SerializeField] private Toggle showTutorialsToggle;
        [SerializeField] private Toggle confirmActionsToggle;
        [SerializeField] private TMP_Dropdown languageDropdown;
        
        [Header("Accessibility Settings")]
        [SerializeField] private Toggle colorBlindToggle;
        [SerializeField] private Slider textSizeSlider;
        [SerializeField] private TextMeshProUGUI textSizeText;
        [SerializeField] private Toggle reduceMotionToggle;
        [SerializeField] private Toggle screenShakeToggle;
        [SerializeField] private Toggle subtitlesToggle;
        
        [Header("Notification Settings")]
        [SerializeField] private Toggle questNotificationToggle;
        [SerializeField] private Toggle achievementNotificationToggle;
        [SerializeField] private Toggle levelUpNotificationToggle;
        [SerializeField] private Toggle resourceNotificationToggle;
        
        [Header("Bottom Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;
        
        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        
        private SettingsSystem settingsSystem;
        private SettingsSystem.GameSettings tempSettings;
        private string currentTab = "graphics";
        
        void Start()
        {
            settingsSystem = SettingsSystem.Instance;
            if (settingsSystem == null)
            {
                Debug.LogError("SettingsSystem not found!");
                return;
            }
            
            SetupUI();
            LoadCurrentSettings();
            ShowTab("graphics");
        }
        
        void SetupUI()
        {
            // 탭 버튼 설정
            if (graphicsTabButton != null)
                graphicsTabButton.onClick.AddListener(() => ShowTab("graphics"));
            if (audioTabButton != null)
                audioTabButton.onClick.AddListener(() => ShowTab("audio"));
            if (gameplayTabButton != null)
                gameplayTabButton.onClick.AddListener(() => ShowTab("gameplay"));
            if (controlsTabButton != null)
                controlsTabButton.onClick.AddListener(() => ShowTab("controls"));
            if (accessibilityTabButton != null)
                accessibilityTabButton.onClick.AddListener(() => ShowTab("accessibility"));
            
            // 그래픽 설정
            SetupGraphicsUI();
            
            // 오디오 설정
            SetupAudioUI();
            
            // 게임플레이 설정
            SetupGameplayUI();
            
            // 접근성 설정
            SetupAccessibilityUI();
            
            // 알림 설정
            SetupNotificationUI();
            
            // 하단 버튼
            if (applyButton != null)
                applyButton.onClick.AddListener(ApplySettings);
            if (saveButton != null)
                saveButton.onClick.AddListener(SaveSettings);
            if (resetButton != null)
                resetButton.onClick.AddListener(ShowResetConfirmation);
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSettings);
        }
        
        void SetupGraphicsUI()
        {
            // 해상도 드롭다운
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                var resolutions = settingsSystem.GetFilteredResolutions();
                List<string> options = new List<string>();
                
                foreach (var res in resolutions)
                {
                    options.Add($"{res.width} x {res.height}");
                }
                
                resolutionDropdown.AddOptions(options);
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }
            
            // 품질 드롭다운
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new List<string>(settingsSystem.GetQualityLevelNames()));
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }
            
            // 토글들
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            
            // FPS 슬라이더
            if (fpsSlider != null)
            {
                fpsSlider.minValue = 30;
                fpsSlider.maxValue = 240;
                fpsSlider.wholeNumbers = true;
                fpsSlider.onValueChanged.AddListener(OnFPSChanged);
            }
        }
        
        void SetupAudioUI()
        {
            // 볼륨 슬라이더들
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.minValue = 0;
                masterVolumeSlider.maxValue = 1;
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0;
                musicVolumeSlider.maxValue = 1;
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0;
                sfxVolumeSlider.maxValue = 1;
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
            
            if (voiceVolumeSlider != null)
            {
                voiceVolumeSlider.minValue = 0;
                voiceVolumeSlider.maxValue = 1;
                voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
            }
            
            if (ambientVolumeSlider != null)
            {
                ambientVolumeSlider.minValue = 0;
                ambientVolumeSlider.maxValue = 1;
                ambientVolumeSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);
            }
            
            // 테스트 사운드 버튼
            if (testSoundButton != null)
                testSoundButton.onClick.AddListener(PlayTestSound);
        }
        
        void SetupGameplayUI()
        {
            // 전투 속도
            if (battleSpeedSlider != null)
            {
                battleSpeedSlider.minValue = 0.5f;
                battleSpeedSlider.maxValue = 4f;
                battleSpeedSlider.onValueChanged.AddListener(OnBattleSpeedChanged);
            }
            
            // 자동 저장
            if (autoSaveToggle != null)
                autoSaveToggle.onValueChanged.AddListener(OnAutoSaveChanged);
            
            if (autoSaveIntervalDropdown != null)
            {
                autoSaveIntervalDropdown.ClearOptions();
                autoSaveIntervalDropdown.AddOptions(new List<string> { "1분", "3분", "5분", "10분", "15분" });
                autoSaveIntervalDropdown.onValueChanged.AddListener(OnAutoSaveIntervalChanged);
            }
            
            // 기타 토글
            if (showDamageToggle != null)
                showDamageToggle.onValueChanged.AddListener(OnShowDamageChanged);
            if (showTutorialsToggle != null)
                showTutorialsToggle.onValueChanged.AddListener(OnShowTutorialsChanged);
            if (confirmActionsToggle != null)
                confirmActionsToggle.onValueChanged.AddListener(OnConfirmActionsChanged);
            
            // 언어 드롭다운
            if (languageDropdown != null)
            {
                languageDropdown.ClearOptions();
                languageDropdown.AddOptions(new List<string> { "한국어", "English" });
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            }
        }
        
        void SetupAccessibilityUI()
        {
            // 색맹 모드
            if (colorBlindToggle != null)
                colorBlindToggle.onValueChanged.AddListener(OnColorBlindChanged);
            
            // 텍스트 크기
            if (textSizeSlider != null)
            {
                textSizeSlider.minValue = 0.8f;
                textSizeSlider.maxValue = 1.5f;
                textSizeSlider.onValueChanged.AddListener(OnTextSizeChanged);
            }
            
            // 기타 토글
            if (reduceMotionToggle != null)
                reduceMotionToggle.onValueChanged.AddListener(OnReduceMotionChanged);
            if (screenShakeToggle != null)
                screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
        }
        
        void SetupNotificationUI()
        {
            if (questNotificationToggle != null)
                questNotificationToggle.onValueChanged.AddListener(OnQuestNotificationChanged);
            if (achievementNotificationToggle != null)
                achievementNotificationToggle.onValueChanged.AddListener(OnAchievementNotificationChanged);
            if (levelUpNotificationToggle != null)
                levelUpNotificationToggle.onValueChanged.AddListener(OnLevelUpNotificationChanged);
            if (resourceNotificationToggle != null)
                resourceNotificationToggle.onValueChanged.AddListener(OnResourceNotificationChanged);
        }
        
        void LoadCurrentSettings()
        {
            var settings = settingsSystem.GetCurrentSettings();
            tempSettings = JsonUtility.FromJson<SettingsSystem.GameSettings>(JsonUtility.ToJson(settings));
            
            // UI에 현재 설정 반영
            UpdateUIFromSettings(tempSettings);
        }
        
        void UpdateUIFromSettings(SettingsSystem.GameSettings settings)
        {
            // 그래픽 설정
            if (resolutionDropdown != null)
                resolutionDropdown.value = settings.resolutionIndex;
            if (qualityDropdown != null)
                qualityDropdown.value = settings.qualityLevel;
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = settings.fullscreen;
            if (vsyncToggle != null)
                vsyncToggle.isOn = settings.vsync;
            if (fpsSlider != null)
            {
                fpsSlider.value = settings.targetFrameRate;
                if (fpsValueText != null)
                    fpsValueText.text = settings.targetFrameRate.ToString();
            }
            
            // 오디오 설정
            UpdateVolumeSliders(settings);
            
            // 게임플레이 설정
            if (battleSpeedSlider != null)
            {
                battleSpeedSlider.value = settings.battleSpeed;
                if (battleSpeedText != null)
                    battleSpeedText.text = $"{settings.battleSpeed:F1}x";
            }
            
            if (autoSaveToggle != null)
                autoSaveToggle.isOn = settings.autoSave;
            if (autoSaveIntervalDropdown != null)
            {
                int[] intervals = { 60, 180, 300, 600, 900 };
                for (int i = 0; i < intervals.Length; i++)
                {
                    if (intervals[i] == settings.autoSaveInterval)
                    {
                        autoSaveIntervalDropdown.value = i;
                        break;
                    }
                }
            }
            
            if (showDamageToggle != null)
                showDamageToggle.isOn = settings.showDamageNumbers;
            if (showTutorialsToggle != null)
                showTutorialsToggle.isOn = settings.showTutorials;
            if (confirmActionsToggle != null)
                confirmActionsToggle.isOn = settings.confirmActions;
            if (languageDropdown != null)
                languageDropdown.value = settings.language;
            
            // 접근성 설정
            if (colorBlindToggle != null)
                colorBlindToggle.isOn = settings.colorBlindMode;
            if (textSizeSlider != null)
            {
                textSizeSlider.value = settings.textSize;
                if (textSizeText != null)
                    textSizeText.text = $"{(int)(settings.textSize * 100)}%";
            }
            if (reduceMotionToggle != null)
                reduceMotionToggle.isOn = settings.reduceMotion;
            if (screenShakeToggle != null)
                screenShakeToggle.isOn = settings.screenShake;
            
            // 알림 설정
            if (questNotificationToggle != null)
                questNotificationToggle.isOn = settings.questNotifications;
            if (achievementNotificationToggle != null)
                achievementNotificationToggle.isOn = settings.achievementNotifications;
            if (levelUpNotificationToggle != null)
                levelUpNotificationToggle.isOn = settings.levelUpNotifications;
            if (resourceNotificationToggle != null)
                resourceNotificationToggle.isOn = settings.resourceNotifications;
        }
        
        void UpdateVolumeSliders(SettingsSystem.GameSettings settings)
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = settings.masterVolume;
                if (masterVolumeText != null)
                    masterVolumeText.text = $"{(int)(settings.masterVolume * 100)}%";
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = settings.musicVolume;
                if (musicVolumeText != null)
                    musicVolumeText.text = $"{(int)(settings.musicVolume * 100)}%";
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = settings.sfxVolume;
                if (sfxVolumeText != null)
                    sfxVolumeText.text = $"{(int)(settings.sfxVolume * 100)}%";
            }
            
            if (voiceVolumeSlider != null)
            {
                voiceVolumeSlider.value = settings.voiceVolume;
                if (voiceVolumeText != null)
                    voiceVolumeText.text = $"{(int)(settings.voiceVolume * 100)}%";
            }
            
            if (ambientVolumeSlider != null)
            {
                ambientVolumeSlider.value = settings.ambientVolume;
                if (ambientVolumeText != null)
                    ambientVolumeText.text = $"{(int)(settings.ambientVolume * 100)}%";
            }
        }
        
        void ShowTab(string tabName)
        {
            currentTab = tabName;
            
            // 모든 패널 숨기기
            if (graphicsPanel != null) graphicsPanel.SetActive(false);
            if (audioPanel != null) audioPanel.SetActive(false);
            if (gameplayPanel != null) gameplayPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (accessibilityPanel != null) accessibilityPanel.SetActive(false);
            
            // 선택된 패널 표시
            switch (tabName)
            {
                case "graphics":
                    if (graphicsPanel != null) graphicsPanel.SetActive(true);
                    break;
                case "audio":
                    if (audioPanel != null) audioPanel.SetActive(true);
                    break;
                case "gameplay":
                    if (gameplayPanel != null) gameplayPanel.SetActive(true);
                    break;
                case "controls":
                    if (controlsPanel != null) controlsPanel.SetActive(true);
                    break;
                case "accessibility":
                    if (accessibilityPanel != null) accessibilityPanel.SetActive(true);
                    break;
            }
            
            // 탭 버튼 하이라이트
            UpdateTabButtons(tabName);
        }
        
        void UpdateTabButtons(string activeTab)
        {
            // 모든 탭 버튼 비활성 상태로
            Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            Color activeColor = Color.white;
            
            if (graphicsTabButton != null)
                graphicsTabButton.image.color = activeTab == "graphics" ? activeColor : normalColor;
            if (audioTabButton != null)
                audioTabButton.image.color = activeTab == "audio" ? activeColor : normalColor;
            if (gameplayTabButton != null)
                gameplayTabButton.image.color = activeTab == "gameplay" ? activeColor : normalColor;
            if (controlsTabButton != null)
                controlsTabButton.image.color = activeTab == "controls" ? activeColor : normalColor;
            if (accessibilityTabButton != null)
                accessibilityTabButton.image.color = activeTab == "accessibility" ? activeColor : normalColor;
        }
        
        // ===== 설정 변경 핸들러 =====
        
        // 그래픽 설정
        void OnResolutionChanged(int index)
        {
            tempSettings.resolutionIndex = index;
        }
        
        void OnQualityChanged(int level)
        {
            tempSettings.qualityLevel = level;
        }
        
        void OnFullscreenChanged(bool value)
        {
            tempSettings.fullscreen = value;
        }
        
        void OnVSyncChanged(bool value)
        {
            tempSettings.vsync = value;
        }
        
        void OnFPSChanged(float value)
        {
            tempSettings.targetFrameRate = (int)value;
            if (fpsValueText != null)
                fpsValueText.text = ((int)value).ToString();
        }
        
        // 오디오 설정
        void OnMasterVolumeChanged(float value)
        {
            tempSettings.masterVolume = value;
            if (masterVolumeText != null)
                masterVolumeText.text = $"{(int)(value * 100)}%";
            
            // 실시간 적용
            settingsSystem.SetMasterVolume(value);
        }
        
        void OnMusicVolumeChanged(float value)
        {
            tempSettings.musicVolume = value;
            if (musicVolumeText != null)
                musicVolumeText.text = $"{(int)(value * 100)}%";
            
            // 실시간 적용
            settingsSystem.SetMusicVolume(value);
        }
        
        void OnSFXVolumeChanged(float value)
        {
            tempSettings.sfxVolume = value;
            if (sfxVolumeText != null)
                sfxVolumeText.text = $"{(int)(value * 100)}%";
            
            // 실시간 적용
            settingsSystem.SetSFXVolume(value);
        }
        
        void OnVoiceVolumeChanged(float value)
        {
            tempSettings.voiceVolume = value;
            if (voiceVolumeText != null)
                voiceVolumeText.text = $"{(int)(value * 100)}%";
        }
        
        void OnAmbientVolumeChanged(float value)
        {
            tempSettings.ambientVolume = value;
            if (ambientVolumeText != null)
                ambientVolumeText.text = $"{(int)(value * 100)}%";
        }
        
        void PlayTestSound()
        {
            SoundSystem.Instance?.PlaySound("ui_click");
        }
        
        // 게임플레이 설정
        void OnBattleSpeedChanged(float value)
        {
            tempSettings.battleSpeed = value;
            if (battleSpeedText != null)
                battleSpeedText.text = $"{value:F1}x";
        }
        
        void OnAutoSaveChanged(bool value)
        {
            tempSettings.autoSave = value;
            if (autoSaveIntervalDropdown != null)
                autoSaveIntervalDropdown.interactable = value;
        }
        
        void OnAutoSaveIntervalChanged(int index)
        {
            int[] intervals = { 60, 180, 300, 600, 900 };
            if (index >= 0 && index < intervals.Length)
            {
                tempSettings.autoSaveInterval = intervals[index];
            }
        }
        
        void OnShowDamageChanged(bool value)
        {
            tempSettings.showDamageNumbers = value;
        }
        
        void OnShowTutorialsChanged(bool value)
        {
            tempSettings.showTutorials = value;
        }
        
        void OnConfirmActionsChanged(bool value)
        {
            tempSettings.confirmActions = value;
        }
        
        void OnLanguageChanged(int index)
        {
            tempSettings.language = index;
        }
        
        // 접근성 설정
        void OnColorBlindChanged(bool value)
        {
            tempSettings.colorBlindMode = value;
        }
        
        void OnTextSizeChanged(float value)
        {
            tempSettings.textSize = value;
            if (textSizeText != null)
                textSizeText.text = $"{(int)(value * 100)}%";
        }
        
        void OnReduceMotionChanged(bool value)
        {
            tempSettings.reduceMotion = value;
        }
        
        void OnScreenShakeChanged(bool value)
        {
            tempSettings.screenShake = value;
        }
        
        // 알림 설정
        void OnQuestNotificationChanged(bool value)
        {
            tempSettings.questNotifications = value;
        }
        
        void OnAchievementNotificationChanged(bool value)
        {
            tempSettings.achievementNotifications = value;
        }
        
        void OnLevelUpNotificationChanged(bool value)
        {
            tempSettings.levelUpNotifications = value;
        }
        
        void OnResourceNotificationChanged(bool value)
        {
            tempSettings.resourceNotifications = value;
        }
        
        // ===== 버튼 액션 =====
        
        void ApplySettings()
        {
            // 임시 설정을 실제 설정에 적용
            var currentSettings = settingsSystem.GetCurrentSettings();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tempSettings), currentSettings);
            
            settingsSystem.ApplyAllSettings();
            
            ShowNotification("설정이 적용되었습니다.");
        }
        
        void SaveSettings()
        {
            ApplySettings();
            settingsSystem.SaveSettings();
            ShowNotification("설정이 저장되었습니다.");
        }
        
        void ShowResetConfirmation()
        {
            ShowConfirmationDialog(
                "모든 설정을 기본값으로 초기화하시겠습니까?",
                () => 
                {
                    settingsSystem.ResetToDefaults();
                    LoadCurrentSettings();
                    ShowNotification("설정이 초기화되었습니다.");
                }
            );
        }
        
        void CloseSettings()
        {
            // 변경사항 확인
            var currentSettings = settingsSystem.GetCurrentSettings();
            bool hasChanges = JsonUtility.ToJson(currentSettings) != JsonUtility.ToJson(tempSettings);
            
            if (hasChanges)
            {
                ShowConfirmationDialog(
                    "변경사항이 있습니다. 저장하지 않고 닫으시겠습니까?",
                    () => 
                    {
                        if (settingsPanel != null)
                            settingsPanel.SetActive(false);
                    }
                );
            }
            else
            {
                if (settingsPanel != null)
                    settingsPanel.SetActive(false);
            }
        }
        
        void ShowConfirmationDialog(string message, Action onConfirm)
        {
            if (confirmationDialog != null)
            {
                confirmationDialog.SetActive(true);
                
                if (confirmationText != null)
                    confirmationText.text = message;
                
                if (confirmYesButton != null)
                {
                    confirmYesButton.onClick.RemoveAllListeners();
                    confirmYesButton.onClick.AddListener(() =>
                    {
                        onConfirm?.Invoke();
                        confirmationDialog.SetActive(false);
                    });
                }
                
                if (confirmNoButton != null)
                {
                    confirmNoButton.onClick.RemoveAllListeners();
                    confirmNoButton.onClick.AddListener(() =>
                    {
                        confirmationDialog.SetActive(false);
                    });
                }
            }
        }
        
        void ShowNotification(string message)
        {
            // TODO: 알림 UI 표시
            Debug.Log($"Settings: {message}");
        }
        
        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                LoadCurrentSettings();
                ShowTab("graphics");
            }
        }
    }
}