using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GuildMaster.Systems;
using GuildMaster.UI;
using GuildMaster.Data;

namespace GuildMaster.Core
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Game Configuration")]
        [SerializeField] private GameConfiguration gameConfig;
        [SerializeField] private bool isDebugMode = false;
        [SerializeField] private bool skipIntro = false;
        
        [Header("Loading Screen")]
        [SerializeField] private GameObject loadingScreenPrefab;
        [SerializeField] private UnityEngine.UI.Image loadingBar;
        [SerializeField] private TMPro.TextMeshProUGUI loadingText;
        [SerializeField] private TMPro.TextMeshProUGUI versionText;
        
        [Header("Initial Scene Setup")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string gameplayScene = "Gameplay";
        [SerializeField] private float minimumLoadTime = 2f;
        
        private LoadingScreen currentLoadingScreen;
        private float loadProgress = 0f;
        private List<string> loadingSteps = new List<string>();
        
        // 게임 버전
        private const string GAME_VERSION = "1.0.0";
        private const string BUILD_NUMBER = "20250127";
        
        void Awake()
        {
            // 프레임레이트 설정
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            
            // 화면 설정
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            
            // 디버그 설정
            Debug.unityLogger.logEnabled = isDebugMode;
            
            StartCoroutine(InitializeGame());
        }
        
        IEnumerator InitializeGame()
        {
            // 로딩 화면 생성
            ShowLoadingScreen();
            
            float startTime = Time.time;
            
            // 1. 핵심 시스템 초기화
            UpdateLoadingStatus("Initializing Core Systems...", 0.1f);
            yield return InitializeCoreSystems();
            
            // 2. 데이터 시스템 초기화
            UpdateLoadingStatus("Loading Game Data...", 0.2f);
            yield return InitializeDataSystems();
            
            // 3. 리소스 로드
            UpdateLoadingStatus("Loading Resources...", 0.3f);
            yield return LoadResources();
            
            // 4. 오디오 시스템 초기화
            UpdateLoadingStatus("Initializing Audio...", 0.4f);
            yield return InitializeAudio();
            
            // 5. 그래픽 시스템 초기화
            UpdateLoadingStatus("Initializing Graphics...", 0.5f);
            yield return InitializeGraphics();
            
            // 6. UI 시스템 초기화
            UpdateLoadingStatus("Initializing UI...", 0.6f);
            yield return InitializeUI();
            
            // 7. 게임플레이 시스템 초기화
            UpdateLoadingStatus("Initializing Gameplay Systems...", 0.7f);
            yield return InitializeGameplaySystems();
            
            // 8. 네트워크 시스템 초기화 (멀티플레이어용)
            UpdateLoadingStatus("Checking Network...", 0.8f);
            yield return InitializeNetworking();
            
            // 9. 세이브 데이터 확인
            UpdateLoadingStatus("Checking Save Data...", 0.9f);
            yield return CheckSaveData();
            
            // 최소 로딩 시간 보장
            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minimumLoadTime)
            {
                yield return new WaitForSeconds(minimumLoadTime - elapsedTime);
            }
            
            // 10. 완료
            UpdateLoadingStatus("Loading Complete!", 1.0f);
            yield return new WaitForSeconds(0.5f);
            
            // 메인 메뉴로 전환
            LoadMainMenu();
        }
        
        void ShowLoadingScreen()
        {
            if (loadingScreenPrefab != null)
            {
                GameObject loadingGO = Instantiate(loadingScreenPrefab);
                currentLoadingScreen = loadingGO.GetComponent<LoadingScreen>();
                DontDestroyOnLoad(loadingGO);
                
                if (versionText != null)
                {
                    versionText.text = $"v{GAME_VERSION} (Build {BUILD_NUMBER})";
                }
            }
        }
        
        void UpdateLoadingStatus(string status, float progress)
        {
            loadProgress = progress;
            
            if (currentLoadingScreen != null)
            {
                currentLoadingScreen.UpdateProgress(progress, status);
            }
            else
            {
                if (loadingBar != null)
                    loadingBar.fillAmount = progress;
                    
                if (loadingText != null)
                    loadingText.text = status;
            }
            
            Debug.Log($"[GameInit] {status} ({(int)(progress * 100)}%)");
        }
        
        IEnumerator InitializeCoreSystems()
        {
            // GameManager 생성
            if (GameManager.Instance == null)
            {
                GameObject gmObject = new GameObject("GameManager");
                gmObject.AddComponent<GameManager>();
                DontDestroyOnLoad(gmObject);
            }
            
            yield return null;
            
            // 필수 시스템 매니저 생성
            CreateSystemManager<SaveManager>("SaveManager");
            CreateSystemManager<ResourceManager>("ResourceManager");
            CreateSystemManager<EventManager>("EventManager");
            
            yield return null;
        }
        
        IEnumerator InitializeDataSystems()
        {
            // 데이터 매니저 초기화
            var dataManager = CreateSystemManager<DataManager>("DataManager");
            if (dataManager != null)
            {
                dataManager.LoadAllData();
            }
            
            yield return null;
            
            // 게임 구성 로드
            if (gameConfig == null)
            {
                gameConfig = Resources.Load<GameConfiguration>("GameConfiguration");
            }
            
            yield return null;
        }
        
        IEnumerator LoadResources()
        {
            // 리소스 프리로드
            yield return PreloadResources();
            
            // 에셋 번들 로드 (있는 경우)
            yield return LoadAssetBundles();
        }
        
        IEnumerator PreloadResources()
        {
            // UI 프리팹 로드
            Resources.LoadAsync<GameObject>("Prefabs/UI/MainMenu");
            Resources.LoadAsync<GameObject>("Prefabs/UI/GameHUD");
            yield return null;
            
            // 캐릭터 프리팹 로드
            Resources.LoadAsync<GameObject>("Prefabs/Characters/AdventurerBase");
            yield return null;
            
            // 이펙트 프리팹 로드
            Resources.LoadAsync<GameObject>("Prefabs/Effects/CommonEffects");
            yield return null;
        }
        
        IEnumerator LoadAssetBundles()
        {
            // 에셋 번들이 있다면 여기서 로드
            yield return null;
        }
        
        IEnumerator InitializeAudio()
        {
            // 사운드 시스템 초기화
            var soundSystem = CreateSystemManager<SoundSystem>("SoundSystem");
            
            yield return null;
            
            // 오디오 믹서 설정
            if (soundSystem != null)
            {
                soundSystem.SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
                soundSystem.SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 0.7f));
                soundSystem.SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
            }
            
            yield return null;
        }
        
        IEnumerator InitializeGraphics()
        {
            // 파티클 시스템 초기화
            CreateSystemManager<ParticleEffectsSystem>("ParticleEffectsSystem");
            
            yield return null;
            
            // 그래픽 설정 적용
            ApplyGraphicsSettings();
            
            yield return null;
        }
        
        void ApplyGraphicsSettings()
        {
            // 저장된 그래픽 설정 적용
            int qualityLevel = PlayerPrefs.GetInt("GraphicsQuality", 2); // 0: Low, 1: Medium, 2: High
            QualitySettings.SetQualityLevel(qualityLevel);
            
            // 해상도 설정
            int resolutionIndex = PlayerPrefs.GetInt("Resolution", -1);
            if (resolutionIndex >= 0 && resolutionIndex < Screen.resolutions.Length)
            {
                Resolution resolution = Screen.resolutions[resolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            }
        }
        
        IEnumerator InitializeUI()
        {
            // UI 매니저 초기화
            var uiManager = CreateSystemManager<GuildMaster.UI.UIManager>("UIManager");
            
            yield return null;
            
            // 로컬라이제이션 시스템 초기화
            var localizationSystem = CreateSystemManager<LocalizationSystem>("LocalizationSystem");
            
            yield return null;
            
            // UI 스케일 설정
            if (uiManager != null)
            {
                float uiScale = PlayerPrefs.GetFloat("UIScale", 1f);
                uiManager.SetUIScale(uiScale);
            }
        }
        
        IEnumerator InitializeGameplaySystems()
        {
            // 전투 시스템
            CreateSystemManager<BattleManager>("BattleManager");
            yield return null;
            
            // 길드 시스템
            CreateSystemManager<GuildManager>("GuildManager");
            yield return null;
            
            // 튜토리얼 시스템
            CreateSystemManager<TutorialSystem>("TutorialSystem");
            yield return null;
            
            // 업적 시스템
            CreateSystemManager<AchievementSystem>("AchievementSystem");
            yield return null;
            
            // 일일 콘텐츠 매니저
            CreateSystemManager<DailyContentManager>("DailyContentManager");
            yield return null;
            
            // 시즌 패스 시스템
            CreateSystemManager<SeasonPassSystem>("SeasonPassSystem");
            yield return null;
            
            // 자동 전투 시스템
            CreateSystemManager<AutoBattleSystem>("AutoBattleSystem");
            yield return null;
            
            // 편의 시스템
            CreateSystemManager<ConvenienceSystem>("ConvenienceSystem");
            yield return null;
            
            // 게임 속도 시스템
            CreateSystemManager<GameSpeedSystem>("GameSpeedSystem");
            yield return null;
        }
        
        IEnumerator InitializeNetworking()
        {
            // 네트워크 매니저 초기화 (멀티플레이어 지원 시)
            // 현재는 싱글플레이어 전용이므로 스킵
            yield return null;
        }
        
        IEnumerator CheckSaveData()
        {
            var saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                saveManager.Initialize();
                
                // 자동 저장 파일 확인
                if (saveManager.HasAutoSave())
                {
                    // 자동 저장 복구 옵션 제공
                    Debug.Log("Auto-save found. Recovery option available.");
                }
            }
            
            yield return null;
        }
        
        T CreateSystemManager<T>(string objectName) where T : MonoBehaviour
        {
            // 이미 존재하는지 확인
            T existing = FindObjectOfType<T>();
            if (existing != null)
                return existing;
            
            GameObject managerObject = new GameObject(objectName);
            T component = managerObject.AddComponent<T>();
            DontDestroyOnLoad(managerObject);
            
            return component;
        }
        
        void LoadMainMenu()
        {
            if (currentLoadingScreen != null)
            {
                currentLoadingScreen.FadeOut(() =>
                {
                    Destroy(currentLoadingScreen.gameObject);
                    SceneManager.LoadScene(mainMenuScene);
                });
            }
            else
            {
                SceneManager.LoadScene(mainMenuScene);
            }
        }
        
        // 게임 시작 (메인 메뉴에서 호출)
        public void StartNewGame()
        {
            StartCoroutine(LoadGameplayScene(true));
        }
        
        public void ContinueGame()
        {
            StartCoroutine(LoadGameplayScene(false));
        }
        
        IEnumerator LoadGameplayScene(bool isNewGame)
        {
            // 로딩 화면 표시
            ShowLoadingScreen();
            UpdateLoadingStatus("Loading Game World...", 0f);
            
            // 씬 로드
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameplayScene);
            asyncLoad.allowSceneActivation = false;
            
            while (asyncLoad.progress < 0.9f)
            {
                UpdateLoadingStatus("Loading Game World...", asyncLoad.progress);
                yield return null;
            }
            
            UpdateLoadingStatus("Preparing Game...", 0.9f);
            
            // 게임 데이터 준비
            if (isNewGame)
            {
                GameManager.Instance.StartNewGame();
            }
            else
            {
                GameManager.Instance.LoadGame(0); // 슬롯 0에서 로드
            }
            
            UpdateLoadingStatus("Ready!", 1f);
            yield return new WaitForSeconds(0.5f);
            
            // 씬 활성화
            asyncLoad.allowSceneActivation = true;
            
            // 로딩 화면 제거
            if (currentLoadingScreen != null)
            {
                currentLoadingScreen.FadeOut(() =>
                {
                    Destroy(currentLoadingScreen.gameObject);
                });
            }
        }
    }
    
    [System.Serializable]
    public class GameConfiguration : ScriptableObject
    {
        [Header("Game Settings")]
        public string gameName = "Guild Master";
        public string companyName = "YourCompany";
        public string gameVersion = "1.0.0";
        
        [Header("Gameplay Configuration")]
        public int maxGuildLevel = 50;
        public int maxAdventurerLevel = 100;
        public int maxSquadSize = 36;
        public int maxBuildings = 10;
        
        [Header("Economy Settings")]
        public int startingGold = 1000;
        public int startingWood = 500;
        public int startingStone = 500;
        public float taxRate = 0.1f;
        
        [Header("Battle Settings")]
        public float defaultBattleSpeed = 1f;
        public int maxBattleTurns = 100;
        public float criticalHitMultiplier = 2f;
        
        [Header("Save Settings")]
        public float autoSaveInterval = 300f; // 5 minutes
        public int maxSaveSlots = 3;
        public bool enableCloudSave = false;
    }
}