using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace GuildMaster.UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                }
                return _instance;
            }
        }
        
        // UI Panels
        [Header("Main Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject guildPanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject explorationPanel;
        [SerializeField] private GameObject storyPanel;
        
        // Guild UI Elements
        [Header("Guild UI")]
        [SerializeField] private TextMeshProUGUI guildNameText;
        [SerializeField] private TextMeshProUGUI guildLevelText;
        [SerializeField] private TextMeshProUGUI guildReputationText;
        [SerializeField] private Transform buildingGridContainer;
        [SerializeField] private GameObject buildingSlotPrefab;
        
        // Resource UI Elements
        [Header("Resource UI")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI manaStoneText;
        
        // Battle UI Elements
        [Header("Battle UI")]
        [SerializeField] private Transform playerSquadsContainer;
        [SerializeField] private Transform enemySquadsContainer;
        [SerializeField] private GameObject squadUIPrefab;
        [SerializeField] private Button battleSpeedButton;
        [SerializeField] private TextMeshProUGUI battleSpeedText;
        
        // Squad Management UI
        [Header("Squad Management")]
        [SerializeField] private GameObject squadManagementPanel;
        [SerializeField] private Transform squadSlotsContainer;
        [SerializeField] private Transform adventurerListContainer;
        [SerializeField] private GameObject adventurerCardPrefab;
        
        // Notification System
        [Header("Notifications")]
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private float notificationDuration = 3f;
        
        // Dialog System
        [Header("Dialog")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TextMeshProUGUI dialogSpeakerText;
        [SerializeField] private TextMeshProUGUI dialogContentText;
        [SerializeField] private Button dialogContinueButton;
        
        // Idle Rewards UI
        [Header("Idle Rewards")]
        [SerializeField] private GameObject idleRewardsPanel;
        [SerializeField] private TextMeshProUGUI idleTimeText;
        [SerializeField] private TextMeshProUGUI idleRewardsText;
        [SerializeField] private Button collectIdleRewardsButton;
        
        // Current State
        private Core.GameManager.GameState currentUIState;
        private float currentBattleSpeed = 1f;
        
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
            
            InitializeUI();
        }
        
        void Start()
        {
            // Subscribe to game events
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                Core.GameManager.Instance.OnGameInitialized += OnGameInitialized;
            }
            
            // Subscribe to resource events
            if (Core.GameManager.Instance?.ResourceManager != null)
            {
                Core.GameManager.Instance.ResourceManager.OnResourcesChanged += UpdateResourceUI;
            }
            
            // IdleManager가 제거되었으므로 관련 코드도 제거
        }
        
        void InitializeUI()
        {
            // Hide all panels initially
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (guildPanel) guildPanel.SetActive(false);
            if (battlePanel) battlePanel.SetActive(false);
            if (explorationPanel) explorationPanel.SetActive(false);
            if (storyPanel) storyPanel.SetActive(false);
            if (squadManagementPanel) squadManagementPanel.SetActive(false);
            if (dialogPanel) dialogPanel.SetActive(false);
            if (idleRewardsPanel) idleRewardsPanel.SetActive(false);
            
            // Setup button listeners
            if (battleSpeedButton) battleSpeedButton.onClick.AddListener(ToggleBattleSpeed);
            if (collectIdleRewardsButton) collectIdleRewardsButton.onClick.AddListener(CollectIdleRewards);
            if (dialogContinueButton) dialogContinueButton.onClick.AddListener(ContinueDialog);
        }
        
        void OnGameInitialized()
        {
            UpdateGuildUI();
            UpdateResourceUI(Core.GameManager.Instance.ResourceManager.GetResources());
        }
        
        void OnGameStateChanged(Core.GameManager.GameState previousState, Core.GameManager.GameState newState)
        {
            currentUIState = newState;
            UpdateUIState();
        }
        
        void UpdateUIState()
        {
            // Hide all panels
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (guildPanel) guildPanel.SetActive(false);
            if (battlePanel) battlePanel.SetActive(false);
            if (explorationPanel) explorationPanel.SetActive(false);
            if (storyPanel) storyPanel.SetActive(false);
            
            // Show appropriate panel
            switch (currentUIState)
            {
                case Core.GameManager.GameState.MainMenu:
                    if (mainMenuPanel) mainMenuPanel.SetActive(true);
                    break;
                case Core.GameManager.GameState.Guild:
                    if (guildPanel) guildPanel.SetActive(true);
                    UpdateGuildUI();
                    break;
                case Core.GameManager.GameState.Battle:
                    if (battlePanel) battlePanel.SetActive(true);
                    break;
                case Core.GameManager.GameState.Exploration:
                    if (explorationPanel) explorationPanel.SetActive(true);
                    break;
                case Core.GameManager.GameState.Story:
                    if (storyPanel) storyPanel.SetActive(true);
                    break;
            }
        }
        
        // Guild UI Methods
        void UpdateGuildUI()
        {
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager == null) return;
            
            var guildData = guildManager.GetGuildData();
            
            if (guildNameText) guildNameText.text = guildData.GuildName;
            if (guildLevelText) guildLevelText.text = $"Level {guildData.GuildLevel}";
            if (guildReputationText) guildReputationText.text = $"Reputation: {guildData.GuildReputation}";
            
            UpdateBuildingGrid();
        }
        
        void UpdateBuildingGrid()
        {
            if (!buildingGridContainer || !buildingSlotPrefab) return;
            
            // Clear existing slots
            foreach (Transform child in buildingGridContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create grid slots
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager == null) return;
            
            var buildingGrid = guildManager.GetGuildGrid();
            
            for (int x = 0; x < Core.GuildManager.GUILD_GRID_SIZE; x++)
            {
                for (int y = 0; y < Core.GuildManager.GUILD_GRID_SIZE; y++)
                {
                    GameObject slot = Instantiate(buildingSlotPrefab, buildingGridContainer);
                    // TODO: Setup building slot UI
                }
            }
        }
        
        // Resource UI Methods
        void UpdateResourceUI(Core.ResourceManager.Resources resources)
        {
            if (goldText) goldText.text = $"{resources.Gold}";
            if (woodText) woodText.text = $"{resources.Wood}";
            if (stoneText) stoneText.text = $"{resources.Stone}";
            if (manaStoneText) manaStoneText.text = $"{resources.ManaStone}";
        }
        
        // Battle UI Methods
        public void SetupBattleUI(List<Battle.Squad> playerSquads, List<Battle.Squad> enemySquads)
        {
            if (!playerSquadsContainer || !enemySquadsContainer || !squadUIPrefab) return;
            
            // Clear existing UI
            foreach (Transform child in playerSquadsContainer)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in enemySquadsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create player squad UI
            foreach (var squad in playerSquads)
            {
                GameObject squadUI = Instantiate(squadUIPrefab, playerSquadsContainer);
                // TODO: Setup squad UI component
            }
            
            // Create enemy squad UI
            foreach (var squad in enemySquads)
            {
                GameObject squadUI = Instantiate(squadUIPrefab, enemySquadsContainer);
                // TODO: Setup squad UI component
            }
        }
        
        void ToggleBattleSpeed()
        {
            // Cycle through speed options: 1x -> 2x -> 4x -> 1x
            if (currentBattleSpeed >= 4f)
            {
                currentBattleSpeed = 1f;
            }
            else
            {
                currentBattleSpeed *= 2f;
            }
            
            Core.GameManager.Instance?.SetGameSpeed(currentBattleSpeed);
            
            if (battleSpeedText)
            {
                battleSpeedText.text = $"{currentBattleSpeed}x";
            }
        }
        
        // Squad Management Methods
        public void ShowSquadManagement()
        {
            if (squadManagementPanel)
            {
                squadManagementPanel.SetActive(true);
                RefreshSquadManagementUI();
            }
        }
        
        public void HideSquadManagement()
        {
            if (squadManagementPanel)
            {
                squadManagementPanel.SetActive(false);
            }
        }
        
        void RefreshSquadManagementUI()
        {
            // TODO: Implement squad management UI refresh
        }
        
        // Notification Methods
        public void ShowNotification(string message)
        {
            Debug.Log($"Notification: {message}");
        }
        
        // Dialog Methods
        public void ShowDialog(string speaker, string content, Action onContinue = null)
        {
            if (!dialogPanel) return;
            
            dialogPanel.SetActive(true);
            
            if (dialogSpeakerText) dialogSpeakerText.text = speaker;
            if (dialogContentText) dialogContentText.text = content;
            
            // Store continue action
            if (dialogContinueButton)
            {
                dialogContinueButton.onClick.RemoveAllListeners();
                dialogContinueButton.onClick.AddListener(() =>
                {
                    HideDialog();
                    onContinue?.Invoke();
                });
            }
        }
        
        public void HideDialog()
        {
            if (dialogPanel)
            {
                dialogPanel.SetActive(false);
            }
        }
        
        void ContinueDialog()
        {
            // Handled by ShowDialog
        }
        
        // Idle Rewards Methods (IdleManager 제거로 인해 단순화)
        void ShowIdleRewards(string rewardText)
        {
            if (!idleRewardsPanel) return;
            
            idleRewardsPanel.SetActive(true);
            
            if (idleRewardsText)
            {
                idleRewardsText.text = rewardText;
            }
        }
        
        void CollectIdleRewards()
        {
            if (idleRewardsPanel)
            {
                idleRewardsPanel.SetActive(false);
            }
        }
        
        // Helper Methods
        public void ShowLoadingScreen(bool show)
        {
            // TODO: Implement loading screen
        }
        
        public void UpdateBattleProgress(float progress)
        {
            // TODO: Update battle progress bar
        }
        
        public void SetUIScale(float scale)
        {
            // UI 스케일 설정 로직
        }
        
        public void ShowConfirmDialog(string title, string message, Action onConfirm, Action onCancel = null)
        {
            Debug.Log($"Confirm Dialog: {title} - {message}");
            onConfirm?.Invoke();
        }
    }
}