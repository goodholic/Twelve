using UnityEngine;
using System;
using System.Collections.Generic;

namespace GuildMaster.Game
{
    /// <summary>
    /// 게임 전반의 핵심 기능을 관리하는 클래스
    /// </summary>
    public class GameCore : MonoBehaviour
    {
        [Header("Game Settings")]
        public GameState currentState = GameState.MainMenu;
        public float gameSpeed = 1f;
        public bool isPaused = false;
        public string gameVersion = "1.0.0";

        [Header("Game Data")]
        public GameSettings gameSettings;
        public SaveData currentSaveData;

        // Singleton pattern
        private static GameCore _instance;
        public static GameCore Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameCore>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameCore");
                        _instance = go.AddComponent<GameCore>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public enum GameState
        {
            MainMenu,
            Loading,
            Playing,
            Paused,
            Settings,
            Battle,
            Guild,
            Exploration,
            Shop,
            Inventory,
            Character,
            GameOver
        }

        [System.Serializable]
        public class GameSettings
        {
            public float masterVolume = 1f;
            public float musicVolume = 1f;
            public float sfxVolume = 1f;
            public int targetFrameRate = 60;
            public bool autoSave = true;
            public float autoSaveInterval = 300f; // 5분
            public string language = "Korean";
            public int graphicsQuality = 3;
            public bool fullScreen = true;
            public Vector2Int resolution = new Vector2Int(1920, 1080);
        }

        [System.Serializable]
        public class SaveData
        {
            public string saveId;
            public DateTime createdTime;
            public DateTime lastPlayedTime;
            public int playTimeSeconds;
            public string playerName;
            public int playerLevel;
            public long playerGold;
            public string sceneName;
            public Vector3 playerPosition;
            public Dictionary<string, object> gameData = new Dictionary<string, object>();
        }

        // Events
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<bool> OnGamePaused;
        public static event Action OnGameSaved;
        public static event Action OnGameLoaded;

        private float lastAutoSaveTime;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            LoadGameSettings();
            ApplyGameSettings();
        }

        void Update()
        {
            HandleInput();
            
            if (gameSettings != null && gameSettings.autoSave)
            {
                CheckAutoSave();
            }
        }

        void InitializeGame()
        {
            // 기본 게임 설정 초기화
            if (gameSettings == null)
            {
                gameSettings = new GameSettings();
            }

            // 기본 저장 데이터 초기화
            if (currentSaveData == null)
            {
                currentSaveData = new SaveData();
            }

            Application.targetFrameRate = gameSettings.targetFrameRate;
            lastAutoSaveTime = Time.time;
        }

        void HandleInput()
        {
            // ESC 키로 일시정지
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (currentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }

            // F1 키로 설정 화면
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleSettings();
            }

            // F5 키로 퀵세이브
            if (Input.GetKeyDown(KeyCode.F5))
            {
                QuickSave();
            }

            // F9 키로 퀵로드
            if (Input.GetKeyDown(KeyCode.F9))
            {
                QuickLoad();
            }
        }

        void CheckAutoSave()
        {
            if (Time.time - lastAutoSaveTime >= gameSettings.autoSaveInterval)
            {
                AutoSave();
                lastAutoSaveTime = Time.time;
            }
        }

        public void ChangeGameState(GameState newState)
        {
            if (currentState == newState) return;

            GameState previousState = currentState;
            currentState = newState;

            Debug.Log($"Game state changed from {previousState} to {newState}");
            OnGameStateChanged?.Invoke(newState);
        }

        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                isPaused = true;
                Time.timeScale = 0f;
                ChangeGameState(GameState.Paused);
                OnGamePaused?.Invoke(true);
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                isPaused = false;
                Time.timeScale = gameSpeed;
                ChangeGameState(GameState.Playing);
                OnGamePaused?.Invoke(false);
            }
        }

        public void ToggleSettings()
        {
            if (currentState == GameState.Settings)
            {
                ChangeGameState(GameState.Playing);
            }
            else
            {
                ChangeGameState(GameState.Settings);
            }
        }

        public void SetGameSpeed(float speed)
        {
            gameSpeed = Mathf.Clamp(speed, 0.1f, 5f);
            if (!isPaused)
            {
                Time.timeScale = gameSpeed;
            }
        }

        public void QuickSave()
        {
            SaveGame("quicksave");
        }

        public void QuickLoad()
        {
            LoadGame("quicksave");
        }

        public void AutoSave()
        {
            SaveGame("autosave");
        }

        public bool SaveGame(string saveSlot = "default")
        {
            try
            {
                currentSaveData.saveId = saveSlot;
                currentSaveData.lastPlayedTime = DateTime.Now;
                currentSaveData.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                // 플레이어 위치 저장
                if (GameObject.FindWithTag("Player") != null)
                {
                    currentSaveData.playerPosition = GameObject.FindWithTag("Player").transform.position;
                }

                // 게임 데이터를 JSON으로 변환하여 저장
                string jsonData = JsonUtility.ToJson(currentSaveData, true);
                string filePath = GetSaveFilePath(saveSlot);
                
                System.IO.File.WriteAllText(filePath, jsonData);

                Debug.Log($"Game saved to {saveSlot}");
                OnGameSaved?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
                return false;
            }
        }

        public bool LoadGame(string saveSlot = "default")
        {
            try
            {
                string filePath = GetSaveFilePath(saveSlot);
                
                if (!System.IO.File.Exists(filePath))
                {
                    Debug.LogWarning($"Save file not found: {saveSlot}");
                    return false;
                }

                string jsonData = System.IO.File.ReadAllText(filePath);
                currentSaveData = JsonUtility.FromJson<SaveData>(jsonData);

                Debug.Log($"Game loaded from {saveSlot}");
                OnGameLoaded?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                return false;
            }
        }

        public void LoadGameSettings()
        {
            try
            {
                string settingsPath = GetSettingsFilePath();
                
                if (System.IO.File.Exists(settingsPath))
                {
                    string jsonData = System.IO.File.ReadAllText(settingsPath);
                    gameSettings = JsonUtility.FromJson<GameSettings>(jsonData);
                }
                else
                {
                    // 기본 설정 생성
                    gameSettings = new GameSettings();
                    SaveGameSettings();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load settings: {e.Message}");
                gameSettings = new GameSettings();
            }
        }

        public void SaveGameSettings()
        {
            try
            {
                string jsonData = JsonUtility.ToJson(gameSettings, true);
                string settingsPath = GetSettingsFilePath();
                System.IO.File.WriteAllText(settingsPath, jsonData);
                
                ApplyGameSettings();
                Debug.Log("Game settings saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save settings: {e.Message}");
            }
        }

        void ApplyGameSettings()
        {
            if (gameSettings == null) return;

            // 오디오 설정 적용
            AudioListener.volume = gameSettings.masterVolume;
            
            // 프레임레이트 설정 적용
            Application.targetFrameRate = gameSettings.targetFrameRate;
            
            // 화면 설정 적용
            if (gameSettings.fullScreen != Screen.fullScreen)
            {
                Screen.SetResolution(gameSettings.resolution.x, gameSettings.resolution.y, gameSettings.fullScreen);
            }
            
            // 품질 설정 적용
            QualitySettings.SetQualityLevel(gameSettings.graphicsQuality);
        }

        string GetSaveFilePath(string saveSlot)
        {
            string saveDirectory = Application.persistentDataPath + "/Saves";
            if (!System.IO.Directory.Exists(saveDirectory))
            {
                System.IO.Directory.CreateDirectory(saveDirectory);
            }
            return saveDirectory + $"/{saveSlot}.json";
        }

        string GetSettingsFilePath()
        {
            return Application.persistentDataPath + "/settings.json";
        }

        public List<string> GetAvailableSaves()
        {
            List<string> saves = new List<string>();
            string saveDirectory = Application.persistentDataPath + "/Saves";
            
            if (System.IO.Directory.Exists(saveDirectory))
            {
                string[] files = System.IO.Directory.GetFiles(saveDirectory, "*.json");
                foreach (string file in files)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                    saves.Add(fileName);
                }
            }
            
            return saves;
        }

        public bool DeleteSave(string saveSlot)
        {
            try
            {
                string filePath = GetSaveFilePath(saveSlot);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Debug.Log($"Save deleted: {saveSlot}");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save: {e.Message}");
                return false;
            }
        }

        public void QuitGame()
        {
            SaveGameSettings();
            
            if (gameSettings.autoSave && currentState == GameState.Playing)
            {
                AutoSave();
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && currentState == GameState.Playing)
            {
                PauseGame();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && currentState == GameState.Playing)
            {
                PauseGame();
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
} 