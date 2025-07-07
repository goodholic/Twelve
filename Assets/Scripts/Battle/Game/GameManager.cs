using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GuildMaster.Data;
using GuildMaster.Systems;
using Unit = GuildMaster.Battle.UnitStatus;

namespace GuildMaster.Core
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        // Core Systems
        public BattleManager BattleManager { get; private set; }
        public Guild.GuildManager GuildManager { get; private set; }
        // public ResourceManager ResourceManager { get; private set; } // Commented out - ResourceManager removed
        // public SaveManager SaveManager { get; private set; } // SaveManager removed - class doesn't exist
        public EventManager EventManager { get; private set; }
        
        // Game Systems
        public Systems.StoryManager StoryManager { get; private set; }
        public Systems.DailyContentManager DailyContentManager { get; private set; }
        public Equipment.EquipmentManager EquipmentManager { get; private set; }
        public Battle.SkillManager SkillManager { get; private set; }
        public Systems.CharacterManager CharacterManager { get; private set; }
        public Systems.GachaSystem GachaSystem { get; private set; }
        public Systems.AchievementSystem AchievementSystem { get; private set; }
        public Systems.GameLoopManager GameLoopManager { get; private set; }
        public Systems.AutoBattleSystem AutoBattleSystem { get; private set; }

        // Game States
        public enum GameState
        {
            MainMenu,
            Guild,
            Battle,
            Story,
            Loading
        }

        private GameState _currentState = GameState.MainMenu;
        public GameState CurrentState
        {
            get => _currentState;
            set
            {
                if (_currentState != value)
                {
                    GameState previousState = _currentState;
                    _currentState = value;
                    OnGameStateChanged?.Invoke(previousState, _currentState);
                }
            }
        }

        // Character Management
        public Battle.UnitStatus[] currentRegisteredCharacters = new Battle.UnitStatus[10];

        // Events
        public event Action<GameState, GameState> OnGameStateChanged;
        public event Action OnGameInitialized;

        // Game Speed
        private float _gameSpeed = 1f;
        public float GameSpeed
        {
            get => _gameSpeed;
            set
            {
                _gameSpeed = Mathf.Clamp(value, 0f, 4f);
                Time.timeScale = _gameSpeed;
            }
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
            
            InitializeSystems();
            InitializeConvenienceSystems();
        }

        void InitializeSystems()
        {
            // Initialize core systems
            BattleManager = GetOrAddComponent<BattleManager>();
            GuildManager = GetOrAddComponent<Guild.GuildManager>();
            // ResourceManager = GetOrAddComponent<ResourceManager>(); // Commented out - ResourceManager removed
            // SaveManager = GetOrAddComponent<SaveManager>(); // SaveManager removed - class doesn't exist
            EventManager = GetOrAddComponent<EventManager>();
            
            // Initialize game systems
            StoryManager = GetOrAddComponent<Systems.StoryManager>();
            DailyContentManager = GetOrAddComponent<Systems.DailyContentManager>();
            EquipmentManager = GetOrAddComponent<Equipment.EquipmentManager>();
            SkillManager = GetOrAddComponent<Battle.SkillManager>();
            CharacterManager = GetOrAddComponent<Systems.CharacterManager>();
            GachaSystem = GetOrAddComponent<Systems.GachaSystem>();
            AchievementSystem = GetOrAddComponent<Systems.AchievementSystem>();
            GameLoopManager = GetOrAddComponent<Systems.GameLoopManager>();
            AutoBattleSystem = GetOrAddComponent<Systems.AutoBattleSystem>();

            StartCoroutine(InitializeGameCoroutine());
        }
        
        void InitializeConvenienceSystems()
        {
            // 편의 기능 시스템들 초기화
            GetOrAddComponent<Systems.GameSpeedSystem>();
            GetOrAddComponent<Systems.ConvenienceSystem>();
            GetOrAddComponent<Systems.SoundSystem>();
            GetOrAddComponent<Systems.ParticleEffectsSystem>();
            GetOrAddComponent<Systems.TutorialSystem>();
            GetOrAddComponent<Systems.SettingsSystem>();
        }

        IEnumerator InitializeGameCoroutine()
        {
            CurrentState = GameState.Loading;
            
            // Load saved data
            // SaveManager.LoadGame(0); // SaveManager removed - class doesn't exist
            
            // Initialize guild
            yield return GuildManager.Initialize();
            
            // Initialize resources
            // yield return ResourceManager.Initialize(); // Commented out - ResourceManager removed
            
            CurrentState = GameState.Guild;
            OnGameInitialized?.Invoke();
        }

        T GetOrAddComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        public void SetGameSpeed(float speed)
        {
            GameSpeed = speed;
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Time.timeScale = _gameSpeed;
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // SaveManager?.SaveGame(0); // SaveManager removed - class doesn't exist
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // SaveManager?.SaveGame(0); // SaveManager removed - class doesn't exist
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void StartNewGame()
        {
            Debug.Log("Starting new game...");
            // 새 게임 초기화 로직
            InitializeNewGame();
        }

        public void LoadGame(int slotIndex)
        {
            Debug.Log($"Loading game from slot {slotIndex}...");
            // 게임 로드 로직
            // SaveManager removed - class doesn't exist
            // var saveData = SaveManager.Instance.LoadGame(slotIndex);
            // if (saveData != null)
            // {
            //     LoadGameData(saveData);
            // }
        }

        private void InitializeNewGame()
        {
            // 새 게임 초기화
            if (GuildManager != null)
            {
                GuildManager.Initialize();
            }
            // if (ResourceManager != null)
            // {
            //     StartCoroutine(ResourceManager.Initialize());
            // } // Commented out - ResourceManager removed
        }

        // SaveData removed - class doesn't exist
        // private void LoadGameData(SaveData saveData)
        // {
        //     // 저장된 데이터로 게임 상태 복원
        //     // if (ResourceManager != null)
        //     // {
        //     //     ResourceManager.GetResources().Gold = saveData.gold;
        //     //     ResourceManager.GetResources().Wood = saveData.wood;
        //     //     ResourceManager.GetResources().Stone = saveData.stone;
        //     //     ResourceManager.GetResources().ManaStone = saveData.manaStone;
        //     // } // Commented out - ResourceManager removed
        // }

        void Start()
        {
            // Start 메서드는 이전에 있었던 초기화 로직을 포함하고 있습니다.
            // 이전 초기화 로직을 유지하면서 새로운 초기화 로직을 추가할 수 있습니다.
        }
    }
}