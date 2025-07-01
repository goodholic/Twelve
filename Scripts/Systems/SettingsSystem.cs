using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.UI;

namespace GuildMaster.Systems
{
    public class SettingsSystem : MonoBehaviour
    {
        private static SettingsSystem _instance;
        public static SettingsSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SettingsSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SettingsSystem");
                        _instance = go.AddComponent<SettingsSystem>();
                    }
                }
                return _instance;
            }
        }
        
        [System.Serializable]
        public class GameSettings
        {
            // 그래픽 설정
            public int resolutionIndex = -1;
            public int qualityLevel = 2;
            public bool fullscreen = true;
            public bool vsync = true;
            public int targetFrameRate = 60;
            public float renderScale = 1.0f;
            
            // 오디오 설정
            public float masterVolume = 1.0f;
            public float musicVolume = 0.7f;
            public float sfxVolume = 1.0f;
            public float voiceVolume = 1.0f;
            public float ambientVolume = 0.5f;
            
            // 게임플레이 설정
            public float battleSpeed = 1.0f;
            public bool autoSave = true;
            public int autoSaveInterval = 300; // 5분
            public bool showDamageNumbers = true;
            public bool showTutorials = true;
            public bool confirmActions = true;
            
            // UI 설정
            public float uiScale = 1.0f;
            public bool showFPS = false;
            public bool showPing = false;
            public int language = 0; // 0: 한국어, 1: English
            
            // 접근성
            public bool colorBlindMode = false;
            public float textSize = 1.0f;
            public bool reduceMotion = false;
            public bool screenShake = true;
            
            // 알림 설정
            public bool questNotifications = true;
            public bool achievementNotifications = true;
            public bool levelUpNotifications = true;
            public bool resourceNotifications = true;
        }
        
        [Header("Settings Data")]
        [SerializeField] private GameSettings currentSettings;
        [SerializeField] private GameSettings defaultSettings;
        
        [Header("Resolution Settings")]
        private Resolution[] availableResolutions;
        private List<Resolution> filteredResolutions;
        
        // 이벤트
        public event Action<GameSettings> OnSettingsChanged;
        public event Action<string> OnSettingsSaved;
        public event Action OnSettingsReset;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Initialize();
        }
        
        void Initialize()
        {
            // 기본 설정 초기화
            defaultSettings = new GameSettings();
            currentSettings = new GameSettings();
            
            // 사용 가능한 해상도 가져오기
            GetAvailableResolutions();
            
            // 저장된 설정 로드
            LoadSettings();
            
            // 설정 적용
            ApplyAllSettings();
        }
        
        void GetAvailableResolutions()
        {
            availableResolutions = Screen.resolutions;
            filteredResolutions = new List<Resolution>();
            
            // 중복 제거 (리프레시 레이트가 다른 동일 해상도)
            HashSet<string> addedResolutions = new HashSet<string>();
            
            for (int i = availableResolutions.Length - 1; i >= 0; i--)
            {
                string resolutionKey = $"{availableResolutions[i].width}x{availableResolutions[i].height}";
                
                if (!addedResolutions.Contains(resolutionKey))
                {
                    filteredResolutions.Add(availableResolutions[i]);
                    addedResolutions.Add(resolutionKey);
                }
            }
            
            filteredResolutions.Reverse();
        }
        
        // ===== 설정 적용 =====
        
        public void ApplyAllSettings()
        {
            ApplyGraphicsSettings();
            ApplyAudioSettings();
            ApplyGameplaySettings();
            ApplyUISettings();
            ApplyAccessibilitySettings();
            
            OnSettingsChanged?.Invoke(currentSettings);
        }
        
        void ApplyGraphicsSettings()
        {
            // 해상도
            if (currentSettings.resolutionIndex >= 0 && currentSettings.resolutionIndex < filteredResolutions.Count)
            {
                Resolution resolution = filteredResolutions[currentSettings.resolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, currentSettings.fullscreen);
            }
            
            // 품질 레벨
            QualitySettings.SetQualityLevel(currentSettings.qualityLevel);
            
            // VSync
            QualitySettings.vSyncCount = currentSettings.vsync ? 1 : 0;
            
            // 프레임레이트
            Application.targetFrameRate = currentSettings.targetFrameRate;
            
            // 렌더 스케일
            // Unity의 Universal Render Pipeline 사용 시
            // UniversalRenderPipelineAsset.renderScale = currentSettings.renderScale;
        }
        
        void ApplyAudioSettings()
        {
            var soundSystem = SoundSystem.Instance;
            if (soundSystem != null)
            {
                soundSystem.SetMasterVolume(currentSettings.masterVolume);
                soundSystem.SetMusicVolume(currentSettings.musicVolume);
                soundSystem.SetSFXVolume(currentSettings.sfxVolume);
                soundSystem.SetVoiceVolume(currentSettings.voiceVolume);
                soundSystem.SetAmbientVolume(currentSettings.ambientVolume);
            }
        }
        
        void ApplyGameplaySettings()
        {
            // 전투 속도
            var battleSystem = FindObjectOfType<MonoBehaviour>(); // BattleAnimationSystem 대신 임시 처리
            if (battleSystem != null && battleSystem.GetType().Name == "BattleAnimationSystem")
            {
                // battleSystem.SetAnimationSpeed(currentSettings.battleSpeed); // 임시 주석 처리
            }
            
            // 자동 저장
            var saveManager = Core.SaveManager.Instance;
            if (saveManager != null)
            {
                // saveManager.SetAutoSave(currentSettings.autoSave, currentSettings.autoSaveInterval); // 메서드가 없으므로 임시 주석 처리
            }
            
            // 데미지 표시
            // TODO: 전투 시스템에 데미지 표시 옵션 적용
        }
        
        void ApplyUISettings()
        {
            // UI 스케일
            var canvasScaler = FindObjectOfType<CanvasScaler>();
            if (canvasScaler != null)
            {
                canvasScaler.scaleFactor = currentSettings.uiScale;
            }
            
            // 언어 설정
            var localization = LocalizationSystem.Instance;
            if (localization != null)
            {
                string[] languages = { "ko", "en" };
                if (currentSettings.language < languages.Length)
                {
                    // localization.SetLanguage(languages[currentSettings.language]); // 타입 불일치로 임시 주석 처리
                }
            }
            
            // FPS 표시
            // TODO: FPS 카운터 UI 토글
        }
        
        void ApplyAccessibilitySettings()
        {
            // 색맹 모드
            if (currentSettings.colorBlindMode)
            {
                // TODO: 색맹 필터 적용
            }
            
            // 텍스트 크기
            var textComponents = FindObjectsOfType<TextMeshProUGUI>();
            foreach (var text in textComponents)
            {
                if (text.CompareTag("ScalableText"))
                {
                    text.fontSize = text.fontSize * currentSettings.textSize;
                }
            }
            
            // 모션 감소
            if (currentSettings.reduceMotion)
            {
                // 애니메이션 속도 감소
                Time.timeScale = 0.7f;
            }
            
            // 화면 흔들림
            // var battleAnimation = BattleAnimationSystem.Instance; // BattleAnimationSystem이 없으므로 임시 주석 처리
            // if (battleAnimation != null)
            // {
            //     battleAnimation.ToggleScreenShake(currentSettings.screenShake);
            // }
        }
        
        // ===== 개별 설정 메서드 =====
        
        // 그래픽 설정
        public void SetResolution(int index)
        {
            if (index >= 0 && index < filteredResolutions.Count)
            {
                currentSettings.resolutionIndex = index;
                ApplyGraphicsSettings();
            }
        }
        
        public void SetQuality(int level)
        {
            currentSettings.qualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            ApplyGraphicsSettings();
        }
        
        public void SetFullscreen(bool fullscreen)
        {
            currentSettings.fullscreen = fullscreen;
            ApplyGraphicsSettings();
        }
        
        public void SetVSync(bool vsync)
        {
            currentSettings.vsync = vsync;
            ApplyGraphicsSettings();
        }
        
        public void SetTargetFrameRate(int fps)
        {
            currentSettings.targetFrameRate = Mathf.Clamp(fps, 30, 240);
            ApplyGraphicsSettings();
        }
        
        // 오디오 설정
        public void SetMasterVolume(float volume)
        {
            currentSettings.masterVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
        }
        
        public void SetMusicVolume(float volume)
        {
            currentSettings.musicVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
        }
        
        public void SetSFXVolume(float volume)
        {
            currentSettings.sfxVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
        }
        
        // 게임플레이 설정
        public void SetBattleSpeed(float speed)
        {
            currentSettings.battleSpeed = Mathf.Clamp(speed, 0.5f, 4f);
            ApplyGameplaySettings();
        }
        
        public void SetAutoSave(bool enabled)
        {
            currentSettings.autoSave = enabled;
            ApplyGameplaySettings();
        }
        
        public void SetShowDamageNumbers(bool show)
        {
            currentSettings.showDamageNumbers = show;
            // TODO: 전투 시스템에 적용
        }
        
        // UI 설정
        public void SetUIScale(float scale)
        {
            currentSettings.uiScale = Mathf.Clamp(scale, 0.8f, 1.5f);
            ApplyUISettings();
        }
        
        public void SetLanguage(int languageIndex)
        {
            currentSettings.language = languageIndex;
            ApplyUISettings();
        }
        
        public void SetShowFPS(bool show)
        {
            currentSettings.showFPS = show;
            // TODO: FPS 카운터 토글
        }
        
        // 접근성 설정
        public void SetColorBlindMode(bool enabled)
        {
            currentSettings.colorBlindMode = enabled;
            ApplyAccessibilitySettings();
        }
        
        public void SetTextSize(float size)
        {
            currentSettings.textSize = Mathf.Clamp(size, 0.8f, 1.5f);
            ApplyAccessibilitySettings();
        }
        
        public void SetScreenShake(bool enabled)
        {
            currentSettings.screenShake = enabled;
            ApplyAccessibilitySettings();
        }
        
        // ===== 저장/로드 =====
        
        public void SaveSettings()
        {
            // PlayerPrefs에 저장
            PlayerPrefs.SetInt("ResolutionIndex", currentSettings.resolutionIndex);
            PlayerPrefs.SetInt("QualityLevel", currentSettings.qualityLevel);
            PlayerPrefs.SetInt("Fullscreen", currentSettings.fullscreen ? 1 : 0);
            PlayerPrefs.SetInt("VSync", currentSettings.vsync ? 1 : 0);
            PlayerPrefs.SetInt("TargetFPS", currentSettings.targetFrameRate);
            PlayerPrefs.SetFloat("RenderScale", currentSettings.renderScale);
            
            PlayerPrefs.SetFloat("MasterVolume", currentSettings.masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", currentSettings.musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", currentSettings.sfxVolume);
            PlayerPrefs.SetFloat("VoiceVolume", currentSettings.voiceVolume);
            PlayerPrefs.SetFloat("AmbientVolume", currentSettings.ambientVolume);
            
            PlayerPrefs.SetFloat("BattleSpeed", currentSettings.battleSpeed);
            PlayerPrefs.SetInt("AutoSave", currentSettings.autoSave ? 1 : 0);
            PlayerPrefs.SetInt("AutoSaveInterval", currentSettings.autoSaveInterval);
            PlayerPrefs.SetInt("ShowDamageNumbers", currentSettings.showDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt("ShowTutorials", currentSettings.showTutorials ? 1 : 0);
            PlayerPrefs.SetInt("ConfirmActions", currentSettings.confirmActions ? 1 : 0);
            
            PlayerPrefs.SetFloat("UIScale", currentSettings.uiScale);
            PlayerPrefs.SetInt("ShowFPS", currentSettings.showFPS ? 1 : 0);
            PlayerPrefs.SetInt("Language", currentSettings.language);
            
            PlayerPrefs.SetInt("ColorBlindMode", currentSettings.colorBlindMode ? 1 : 0);
            PlayerPrefs.SetFloat("TextSize", currentSettings.textSize);
            PlayerPrefs.SetInt("ReduceMotion", currentSettings.reduceMotion ? 1 : 0);
            PlayerPrefs.SetInt("ScreenShake", currentSettings.screenShake ? 1 : 0);
            
            PlayerPrefs.SetInt("QuestNotifications", currentSettings.questNotifications ? 1 : 0);
            PlayerPrefs.SetInt("AchievementNotifications", currentSettings.achievementNotifications ? 1 : 0);
            PlayerPrefs.SetInt("LevelUpNotifications", currentSettings.levelUpNotifications ? 1 : 0);
            PlayerPrefs.SetInt("ResourceNotifications", currentSettings.resourceNotifications ? 1 : 0);
            
            PlayerPrefs.Save();
            
            OnSettingsSaved?.Invoke("설정이 저장되었습니다.");
        }
        
        public void LoadSettings()
        {
            // 현재 해상도 찾기
            Resolution currentResolution = Screen.currentResolution;
            for (int i = 0; i < filteredResolutions.Count; i++)
            {
                if (filteredResolutions[i].width == currentResolution.width &&
                    filteredResolutions[i].height == currentResolution.height)
                {
                    currentSettings.resolutionIndex = i;
                    break;
                }
            }
            
            // PlayerPrefs에서 로드
            currentSettings.resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentSettings.resolutionIndex);
            currentSettings.qualityLevel = PlayerPrefs.GetInt("QualityLevel", 2);
            currentSettings.fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            currentSettings.vsync = PlayerPrefs.GetInt("VSync", 1) == 1;
            currentSettings.targetFrameRate = PlayerPrefs.GetInt("TargetFPS", 60);
            currentSettings.renderScale = PlayerPrefs.GetFloat("RenderScale", 1.0f);
            
            currentSettings.masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            currentSettings.musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            currentSettings.sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            currentSettings.voiceVolume = PlayerPrefs.GetFloat("VoiceVolume", 1.0f);
            currentSettings.ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.5f);
            
            currentSettings.battleSpeed = PlayerPrefs.GetFloat("BattleSpeed", 1.0f);
            currentSettings.autoSave = PlayerPrefs.GetInt("AutoSave", 1) == 1;
            currentSettings.autoSaveInterval = PlayerPrefs.GetInt("AutoSaveInterval", 300);
            currentSettings.showDamageNumbers = PlayerPrefs.GetInt("ShowDamageNumbers", 1) == 1;
            currentSettings.showTutorials = PlayerPrefs.GetInt("ShowTutorials", 1) == 1;
            currentSettings.confirmActions = PlayerPrefs.GetInt("ConfirmActions", 1) == 1;
            
            currentSettings.uiScale = PlayerPrefs.GetFloat("UIScale", 1.0f);
            currentSettings.showFPS = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
            currentSettings.language = PlayerPrefs.GetInt("Language", 0);
            
            currentSettings.colorBlindMode = PlayerPrefs.GetInt("ColorBlindMode", 0) == 1;
            currentSettings.textSize = PlayerPrefs.GetFloat("TextSize", 1.0f);
            currentSettings.reduceMotion = PlayerPrefs.GetInt("ReduceMotion", 0) == 1;
            currentSettings.screenShake = PlayerPrefs.GetInt("ScreenShake", 1) == 1;
            
            currentSettings.questNotifications = PlayerPrefs.GetInt("QuestNotifications", 1) == 1;
            currentSettings.achievementNotifications = PlayerPrefs.GetInt("AchievementNotifications", 1) == 1;
            currentSettings.levelUpNotifications = PlayerPrefs.GetInt("LevelUpNotifications", 1) == 1;
            currentSettings.resourceNotifications = PlayerPrefs.GetInt("ResourceNotifications", 1) == 1;
        }
        
        public void ResetToDefaults()
        {
            currentSettings = new GameSettings();
            ApplyAllSettings();
            SaveSettings();
            OnSettingsReset?.Invoke();
        }
        
        // ===== 게터 =====
        
        public GameSettings GetCurrentSettings()
        {
            return currentSettings;
        }
        
        public List<Resolution> GetFilteredResolutions()
        {
            return filteredResolutions;
        }
        
        public string[] GetQualityLevelNames()
        {
            return QualitySettings.names;
        }
        
        public bool IsAutoSaveEnabled()
        {
            return currentSettings.autoSave;
        }
        
        public int GetAutoSaveInterval()
        {
            return currentSettings.autoSaveInterval;
        }
        
        public float GetBattleSpeed()
        {
            return currentSettings.battleSpeed;
        }
        
        public bool ShouldShowDamageNumbers()
        {
            return currentSettings.showDamageNumbers;
        }
        
        public bool ShouldShowTutorials()
        {
            return currentSettings.showTutorials;
        }
        
        public bool ShouldConfirmActions()
        {
            return currentSettings.confirmActions;
        }
        
        // 알림 설정 확인
        public bool IsNotificationEnabled(string notificationType)
        {
            switch (notificationType)
            {
                case "quest":
                    return currentSettings.questNotifications;
                case "achievement":
                    return currentSettings.achievementNotifications;
                case "levelup":
                    return currentSettings.levelUpNotifications;
                case "resource":
                    return currentSettings.resourceNotifications;
                default:
                    return true;
            }
        }
    }
}