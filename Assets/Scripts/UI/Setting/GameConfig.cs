using UnityEngine;

namespace GuildMaster.Core
{
    /// <summary>
    /// 게임 전체 설정을 관리하는 중앙 집중식 설정 클래스
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "GuildMaster/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Game Version")]
        public string gameVersion = "1.0.0";
        public string buildNumber = "1";
        public bool isDebugBuild = false;
        
        [Header("Economy")]
        public int startingGold = 1000;
        public int startingWood = 500;
        public int startingStone = 300;
        public int startingFood = 200;
        public float goldMultiplier = 1.0f;
        public float experienceMultiplier = 1.0f;
        
        [Header("Gameplay")]
        public int maxAdventurers = 20;
        public int maxBuildings = 50;
        public int maxQueuedActions = 10;
        public float dayDuration = 300f; // 5 minutes
        public bool allowPause = true;
        public bool autoSave = true;
        public float autoSaveInterval = 300f; // 5 minutes
        
        [Header("Difficulty")]
        public float enemyHealthMultiplier = 1.0f;
        public float enemyDamageMultiplier = 1.0f;
        public float resourceProductionMultiplier = 1.0f;
        public float buildingCostMultiplier = 1.0f;
        
        [Header("UI Settings")]
        public bool enableNotifications = true;
        public bool enableSounds = true;
        public bool enableMusic = true;
        public float defaultVolume = 0.7f;
        public string defaultLanguage = "ko";
        
        [Header("Network")]
        public string serverUrl = "https://api.guildmaster.com";
        public int connectionTimeout = 30;
        public bool enableCloudSave = false;
        public bool enableLeaderboards = false;
        
        [Header("Tutorial")]
        public bool showTutorial = true;
        public bool skipIntro = false;
        public string tutorialVersion = "1.0";
        
        [Header("Debug")]
        public bool enableDebugUI = false;
        public bool enableCheats = false;
        public bool logAllEvents = false;
        public bool showFPS = false;
        
        private static GameConfig _instance;
        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfig>("GameConfig");
                    if (_instance == null)
                    {
                        Debug.LogError("GameConfig not found in Resources folder!");
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 게임 시작 시 초기화
        /// </summary>
        public void Initialize()
        {
            Debug.Log($"Guild Master v{gameVersion} Build {buildNumber}");
            
            // 디버그 빌드 확인
            if (isDebugBuild)
            {
                Debug.Log("Debug build detected - enabling debug features");
                Application.logMessageReceived += OnLogMessageReceived;
            }
            
            // 플랫폼별 설정
            ApplyPlatformSettings();
            
            // 저장된 설정 로드
            LoadPlayerPreferences();
        }
        
        void ApplyPlatformSettings()
        {
            #if UNITY_MOBILE
            // 모바일 플랫폼 최적화
            dayDuration *= 0.5f; // 모바일에서는 더 빠른 게임플레이
            autoSaveInterval = 60f; // 더 자주 자동저장
            #endif
            
            #if UNITY_WEBGL
            // WebGL 플랫폼 최적화
            enableCloudSave = false;
            autoSaveInterval = 120f;
            #endif
            
            #if UNITY_STANDALONE
            // PC 플랫폼 설정
            enableDebugUI = isDebugBuild;
            showFPS = isDebugBuild;
            #endif
        }
        
        void LoadPlayerPreferences()
        {
            enableSounds = PlayerPrefs.GetInt("EnableSounds", 1) == 1;
            enableMusic = PlayerPrefs.GetInt("EnableMusic", 1) == 1;
            defaultVolume = PlayerPrefs.GetFloat("Volume", 0.7f);
            defaultLanguage = PlayerPrefs.GetString("Language", "ko");
            showTutorial = PlayerPrefs.GetInt("ShowTutorial", 1) == 1;
        }
        
        public void SavePlayerPreferences()
        {
            PlayerPrefs.SetInt("EnableSounds", enableSounds ? 1 : 0);
            PlayerPrefs.SetInt("EnableMusic", enableMusic ? 1 : 0);
            PlayerPrefs.SetFloat("Volume", defaultVolume);
            PlayerPrefs.SetString("Language", defaultLanguage);
            PlayerPrefs.SetInt("ShowTutorial", showTutorial ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            if (logAllEvents && type == LogType.Log)
            {
                // 로그를 파일에 저장하거나 서버로 전송
                Debug.Log($"[LOG] {message}");
            }
        }
        
        /// <summary>
        /// 치트 기능 활성화 여부 확인
        /// </summary>
        public bool CanUseCheats()
        {
            return isDebugBuild && enableCheats;
        }
        
        /// <summary>
        /// 네트워크 기능 사용 가능 여부
        /// </summary>
        public bool IsNetworkEnabled()
        {
            return !string.IsNullOrEmpty(serverUrl) && (enableCloudSave || enableLeaderboards);
        }
        
        /// <summary>
        /// 현재 빌드 정보 문자열
        /// </summary>
        public string GetBuildInfo()
        {
            return $"Guild Master v{gameVersion} Build {buildNumber} ({(isDebugBuild ? "Debug" : "Release")})";
        }
        
        /// <summary>
        /// 설정을 기본값으로 리셋
        /// </summary>
        public void ResetToDefaults()
        {
            goldMultiplier = 1.0f;
            experienceMultiplier = 1.0f;
            enemyHealthMultiplier = 1.0f;
            enemyDamageMultiplier = 1.0f;
            resourceProductionMultiplier = 1.0f;
            buildingCostMultiplier = 1.0f;
            enableNotifications = true;
            enableSounds = true;
            enableMusic = true;
            defaultVolume = 0.7f;
            defaultLanguage = "ko";
            showTutorial = true;
            
            SavePlayerPreferences();
        }
    }
} 