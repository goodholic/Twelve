using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GuildMaster.Data;
using GuildMaster.Systems;
using GuildMaster.Battle;
using GuildMaster.Guild;
using GuildMaster.UI;

namespace GuildMaster.Core
{
    /// <summary>
    /// README 기반 길드 키우기 게임 코어 매니저
    /// - 싱글플레이어 방치형 JRPG
    /// - 2:2 부대 전투 (18명 vs 18명)
    /// - 18명 캐릭터 수집
    /// - 길드 시뮬레이션
    /// - 완전 자동화
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Core Systems
        public BattleManager BattleManager { get; private set; }
        public Guild.GuildManager GuildManager { get; private set; }
        public ResourceManager ResourceManager { get; private set; }
        public SaveManager SaveManager { get; private set; }
        public EventManager EventManager { get; private set; }
        
        // Additional Systems (필요한 시스템들만 유지)
        public NPC.MerchantManager MerchantManager { get; private set; }
        // 삭제된 시스템들: DungeonManager, TerritoryManager
        
        public Systems.StoryManager StoryManager { get; private set; }
        public Systems.DailyContentManager DailyContentManager { get; private set; }
        public Equipment.EquipmentManager EquipmentManager { get; private set; }
        public Systems.ResearchManager ResearchManager { get; private set; }
        public Battle.SkillManager SkillManager { get; private set; }
        // public GuildMaster.Systems.AnalyticsSystem AnalyticsSystem { get; private set; }

        // Game States
        public enum GameState
        {
            MainMenu,
            Guild,
            Battle,
            Exploration,
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

        [Header("등록된 캐릭터들")]
        public CharacterData[] currentRegisteredCharacters = new CharacterData[10]; // 0-8: 일반 슬롯, 9: 히어로 슬롯
        
        [Header("게임 설정")]
        public int maxCharacterSlots = 10;
        public bool allowDuplicateCharacters = false;

        [Header("게임 시스템들")]
        [SerializeField] private GuildBattleCore battleCore;
        [SerializeField] private GuildSimulationCore guildCore;
        [SerializeField] private IdleGameCore idleCore;
        [SerializeField] private CSVDataManager dataManager;
        
        [Header("UI 매니저")]
        [SerializeField] private GuildMasterUI uiManager;
        [SerializeField] private GameSceneManager sceneManager;
        
        // 게임 상태
        public bool IsGameInitialized { get; private set; }
        public float TotalPlayTime { get; private set; }

        private void Awake()
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
            ResourceManager = GetOrAddComponent<ResourceManager>();
            SaveManager = GetOrAddComponent<SaveManager>();
            EventManager = GetOrAddComponent<EventManager>();
            
            // Initialize additional systems
            MerchantManager = GetOrAddComponent<NPC.MerchantManager>();
            // 삭제된 시스템들 제거: DungeonManager, TerritoryManager
            
            StoryManager = GetOrAddComponent<Systems.StoryManager>();
            DailyContentManager = GetOrAddComponent<Systems.DailyContentManager>();
            EquipmentManager = GetOrAddComponent<Equipment.EquipmentManager>();
            ResearchManager = GetOrAddComponent<Systems.ResearchManager>();
            SkillManager = GetOrAddComponent<Battle.SkillManager>();
            // AnalyticsSystem = GetOrAddComponent<GuildMaster.Systems.AnalyticsSystem>();

            StartCoroutine(InitializeGameCoroutine());
        }
        
        void InitializeConvenienceSystems()
        {
            // 편의 기능 시스템들 초기화
            GetOrAddComponent<Systems.GameSpeedSystem>();
            GetOrAddComponent<Systems.AutoBattleSystem>();
            GetOrAddComponent<Systems.ConvenienceSystem>();
            GetOrAddComponent<Systems.GachaSystem>();
            GetOrAddComponent<Systems.AdventurerGrowthSystem>();
        }

        IEnumerator InitializeGameCoroutine()
        {
            CurrentState = GameState.Loading;
            
            // Load saved data
            SaveManager.LoadGame(0);
            
            // Initialize guild
            // yield return GuildManager.Initialize();
            
            // Initialize resources
            yield return ResourceManager.Initialize();
            
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
                SaveManager?.SaveGame(0);
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveManager?.SaveGame(0);
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
            var saveData = SaveManager.Instance.LoadGame(slotIndex);
            if (saveData != null)
            {
                LoadGameData(saveData);
            }
        }

        private void InitializeNewGame()
        {
            // 새 게임 초기화
            if (GuildManager != null)
            {
                GuildManager.Initialize();
            }
            if (ResourceManager != null)
            {
                StartCoroutine(ResourceManager.Initialize());
            }
        }

        private void LoadGameData(SaveData saveData)
        {
            // 저장된 데이터로 게임 상태 복원
            if (ResourceManager != null)
            {
                ResourceManager.GetResources().Gold = saveData.gold;
                ResourceManager.GetResources().Wood = saveData.wood;
                ResourceManager.GetResources().Stone = saveData.stone;
                ResourceManager.GetResources().ManaStone = saveData.manaStone;
            }
        }

        void Start()
        {
            // Start 메서드는 이전에 있었던 초기화 로직을 포함하고 있습니다.
            // 이전 초기화 로직을 유지하면서 새로운 초기화 로직을 추가할 수 있습니다.
        }

        public void RegisterCharacter(int slotIndex, CharacterData character)
        {
            if (slotIndex >= 0 && slotIndex < currentRegisteredCharacters.Length)
            {
                currentRegisteredCharacters[slotIndex] = character;
                Debug.Log($"캐릭터 등록됨: 슬롯 {slotIndex}, {character?.characterName}");
            }
        }
        
        public void UnregisterCharacter(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < currentRegisteredCharacters.Length)
            {
                currentRegisteredCharacters[slotIndex] = null;
                Debug.Log($"캐릭터 등록 해제됨: 슬롯 {slotIndex}");
            }
        }
        
        public CharacterData GetRegisteredCharacter(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < currentRegisteredCharacters.Length)
            {
                return currentRegisteredCharacters[slotIndex];
            }
            return null;
        }
        
        public bool IsSlotEmpty(int slotIndex)
        {
            return GetRegisteredCharacter(slotIndex) == null;
        }
        
        public int GetEmptySlotCount()
        {
            int count = 0;
            for (int i = 0; i < currentRegisteredCharacters.Length; i++)
            {
                if (currentRegisteredCharacters[i] == null)
                    count++;
            }
            return count;
        }
        
        public void ClearAllSlots()
        {
            for (int i = 0; i < currentRegisteredCharacters.Length; i++)
            {
                currentRegisteredCharacters[i] = null;
            }
            Debug.Log("모든 캐릭터 슬롯이 초기화되었습니다.");
        }

        /// <summary>
        /// 게임 초기화
        /// </summary>
        private void InitializeGame()
        {
            Debug.Log("길드마스터 게임 초기화 시작...");
            
            // 데이터 시스템 초기화
            if (dataManager != null)
            {
                dataManager.Initialize();
            }
            
            // 직업 시스템은 static 클래스이므로 별도 초기화 불필요
            
            // 핵심 시스템들 초기화
            if (guildCore != null)
            {
                guildCore.Initialize();
            }
            
            if (battleCore != null)
            {
                battleCore.Initialize();
            }
            
            if (idleCore != null)
            {
                idleCore.Initialize();
            }
            
            // UI 초기화
            if (uiManager != null)
            {
                uiManager.Initialize();
            }
            
            IsGameInitialized = true;
            Debug.Log("길드마스터 게임 초기화 완료!");
        }
        
        private void Update()
        {
            if (!IsGameInitialized) return;
            
            TotalPlayTime += Time.deltaTime;
            
            // 핵심 시스템 업데이트
            UpdateCoreSystems();
        }
        
        /// <summary>
        /// 핵심 시스템들 업데이트
        /// </summary>
        private void UpdateCoreSystems()
        {
            if (guildCore != null)
            {
                guildCore.UpdateGuildSystems(Time.deltaTime);
            }
            
            if (battleCore != null)
            {
                battleCore.UpdateBattleSystems(Time.deltaTime);
            }
            
            if (idleCore != null)
            {
                idleCore.UpdateIdleSystems(Time.deltaTime);
            }
        }
        
        /// <summary>
        /// 게임 저장
        /// </summary>
        public void SaveGame()
        {
            PlayerPrefs.SetFloat("TotalPlayTime", TotalPlayTime);
            
            if (guildCore != null)
            {
                guildCore.SaveGuildData();
            }
            
            if (battleCore != null)
            {
                battleCore.SaveBattleData();
            }
            
            if (idleCore != null)
            {
                idleCore.SaveIdleData();
            }
            
            PlayerPrefs.Save();
            Debug.Log("게임 저장 완료!");
        }
        
        /// <summary>
        /// 게임 로드
        /// </summary>
        public void LoadGame()
        {
            TotalPlayTime = PlayerPrefs.GetFloat("TotalPlayTime", 0f);
            
            if (guildCore != null)
            {
                guildCore.LoadGuildData();
            }
            
            if (battleCore != null)
            {
                battleCore.LoadBattleData();
            }
            
            if (idleCore != null)
            {
                idleCore.LoadIdleData();
            }
            
            Debug.Log("게임 로드 완료!");
        }
        
        /// <summary>
        /// 게임 리셋
        /// </summary>
        public void ResetGame()
        {
            PlayerPrefs.DeleteAll();
            
            if (guildCore != null)
            {
                guildCore.ResetGuildData();
            }
            
            if (battleCore != null)
            {
                battleCore.ResetBattleData();
            }
            
            if (idleCore != null)
            {
                idleCore.ResetIdleData();
            }
            
            Debug.Log("게임 리셋 완료!");
        }
        
        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
            SaveGame();
            Application.Quit();
        }
    }
}