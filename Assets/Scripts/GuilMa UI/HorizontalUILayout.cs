using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GuildMaster.Systems;

namespace GuildMaster.UI
{
    public class HorizontalUILayout : MonoBehaviour
    {
        [Header("Main Layout Areas")]
        [SerializeField] private RectTransform leftPanel;      // 길드 시설 3D 뷰
        [SerializeField] private RectTransform centerPanel;    // 주요 콘텐츠
        [SerializeField] private RectTransform rightPanel;     // 부대 편성
        [SerializeField] private RectTransform topBar;         // 자원 및 정보
        [SerializeField] private RectTransform bottomBar;      // 하단 메뉴
        
        [Header("Left Panel - Guild View")]
        [SerializeField] private Camera isometricCamera;
        [SerializeField] private RectTransform guildViewport;
        [SerializeField] private GameObject buildingMenuButton;
        
        [Header("Center Panel - Main Content")]
        [SerializeField] private GameObject adventureButton;
        [SerializeField] private GameObject battleButton;
        [SerializeField] private GameObject researchButton;
        [SerializeField] private GameObject constructButton;
        
        [Header("Right Panel - Squad Formation")]
        [SerializeField] private Transform squad1Container;
        [SerializeField] private Transform squad2Container;
        [SerializeField] private Transform squad3Container;
        [SerializeField] private Transform squad4Container;
        [SerializeField] private Text totalPowerText;
        
        [Header("Top Bar - Resources")]
        [SerializeField] private Text goldText;
        [SerializeField] private Text woodText;
        [SerializeField] private Text stoneText;
        [SerializeField] private Text manaStoneText;
        [SerializeField] private Text reputationText;
        [SerializeField] private Text guildLevelText;
        [SerializeField] private Button speedButton;
        [SerializeField] private Text speedText;
        
        [Header("Bottom Bar - Quick Access")]
        [SerializeField] private Button dailyTasksButton;
        [SerializeField] private Button npcTradeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button bulkRewardButton;
        [SerializeField] private GameObject notificationBadge;
        
        [Header("UI Prefabs")]
        [SerializeField] private GameObject unitSlotPrefab;
        [SerializeField] private GameObject buildingSlotPrefab;
        
        // 화면 비율 설정 (가로 모드 1920x1080 기준)
        private const float ASPECT_RATIO = 16f / 9f;
        private const float LEFT_PANEL_WIDTH = 0.35f;    // 35%
        private const float CENTER_PANEL_WIDTH = 0.35f;  // 35%
        private const float RIGHT_PANEL_WIDTH = 0.3f;    // 30%
        private const float TOP_BAR_HEIGHT = 0.08f;      // 8%
        private const float BOTTOM_BAR_HEIGHT = 0.08f;   // 8%
        
        void Start()
        {
            SetupLayout();
            SubscribeToEvents();
            UpdateResourceDisplay();
        }
        
        void SetupLayout()
        {
            // 화면 크기 가져오기
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            // 가로 모드 강제
            if (screenHeight > screenWidth)
            {
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            }
            
            // 레이아웃 설정
            SetupLeftPanel();
            SetupCenterPanel();
            SetupRightPanel();
            SetupTopBar();
            SetupBottomBar();
        }
        
        void SetupLeftPanel()
        {
            if (leftPanel == null) return;
            
            // 왼쪽 패널 크기 설정
            leftPanel.anchorMin = new Vector2(0, BOTTOM_BAR_HEIGHT);
            leftPanel.anchorMax = new Vector2(LEFT_PANEL_WIDTH, 1 - TOP_BAR_HEIGHT);
            leftPanel.offsetMin = Vector2.zero;
            leftPanel.offsetMax = Vector2.zero;
            
            // 3D 아이소메트릭 카메라 뷰포트 설정
            if (isometricCamera != null)
            {
                float viewportX = 0;
                float viewportY = BOTTOM_BAR_HEIGHT;
                float viewportWidth = LEFT_PANEL_WIDTH;
                float viewportHeight = 1 - TOP_BAR_HEIGHT - BOTTOM_BAR_HEIGHT;
                
                isometricCamera.rect = new Rect(viewportX, viewportY, viewportWidth, viewportHeight);
            }
        }
        
        void SetupCenterPanel()
        {
            if (centerPanel == null) return;
            
            // 중앙 패널 크기 설정
            centerPanel.anchorMin = new Vector2(LEFT_PANEL_WIDTH, BOTTOM_BAR_HEIGHT);
            centerPanel.anchorMax = new Vector2(LEFT_PANEL_WIDTH + CENTER_PANEL_WIDTH, 1 - TOP_BAR_HEIGHT);
            centerPanel.offsetMin = Vector2.zero;
            centerPanel.offsetMax = Vector2.zero;
            
            // 주요 버튼들 그리드 배치
            SetupMainContentButtons();
        }
        
        void SetupRightPanel()
        {
            if (rightPanel == null) return;
            
            // 오른쪽 패널 크기 설정
            rightPanel.anchorMin = new Vector2(1 - RIGHT_PANEL_WIDTH, BOTTOM_BAR_HEIGHT);
            rightPanel.anchorMax = new Vector2(1, 1 - TOP_BAR_HEIGHT);
            rightPanel.offsetMin = Vector2.zero;
            rightPanel.offsetMax = Vector2.zero;
            
            // 4개 부대 컨테이너 설정
            SetupSquadContainers();
        }
        
        void SetupTopBar()
        {
            if (topBar == null) return;
            
            // 상단 바 크기 설정
            topBar.anchorMin = new Vector2(0, 1 - TOP_BAR_HEIGHT);
            topBar.anchorMax = new Vector2(1, 1);
            topBar.offsetMin = Vector2.zero;
            topBar.offsetMax = Vector2.zero;
        }
        
        void SetupBottomBar()
        {
            if (bottomBar == null) return;
            
            // 하단 바 크기 설정
            bottomBar.anchorMin = new Vector2(0, 0);
            bottomBar.anchorMax = new Vector2(1, BOTTOM_BAR_HEIGHT);
            bottomBar.offsetMin = Vector2.zero;
            bottomBar.offsetMax = Vector2.zero;
        }
        
        void SetupMainContentButtons()
        {
            // 2x2 그리드로 주요 버튼 배치
            float buttonSize = 0.4f;
            float spacing = 0.1f;
            
            if (adventureButton != null)
            {
                SetButtonPosition(adventureButton, 0, 0, buttonSize, spacing);
            }
            
            if (battleButton != null)
            {
                SetButtonPosition(battleButton, 1, 0, buttonSize, spacing);
            }
            
            if (researchButton != null)
            {
                SetButtonPosition(researchButton, 0, 1, buttonSize, spacing);
            }
            
            if (constructButton != null)
            {
                SetButtonPosition(constructButton, 1, 1, buttonSize, spacing);
            }
        }
        
        void SetButtonPosition(GameObject button, int x, int y, float size, float spacing)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null) return;
            
            float xPos = spacing + x * (size + spacing);
            float yPos = 1 - (spacing + (y + 1) * (size + spacing));
            
            rect.anchorMin = new Vector2(xPos, yPos);
            rect.anchorMax = new Vector2(xPos + size, yPos + size);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        
        void SetupSquadContainers()
        {
            // 4개 부대를 세로로 배치
            Transform[] squadContainers = { squad1Container, squad2Container, squad3Container, squad4Container };
            float squadHeight = 0.22f;
            float spacing = 0.02f;
            
            for (int i = 0; i < squadContainers.Length; i++)
            {
                if (squadContainers[i] == null) continue;
                
                RectTransform rect = squadContainers[i].GetComponent<RectTransform>();
                if (rect == null) continue;
                
                float yPos = 1 - ((i + 1) * (squadHeight + spacing));
                
                rect.anchorMin = new Vector2(0.05f, yPos);
                rect.anchorMax = new Vector2(0.95f, yPos + squadHeight);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                
                // 6x3 그리드 생성
                CreateSquadGrid(squadContainers[i], i);
            }
        }
        
        void CreateSquadGrid(Transform container, int squadIndex)
        {
            // 6x3 그리드 생성
            GridLayoutGroup grid = container.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.cellSize = new Vector2(40, 40);
            grid.spacing = new Vector2(5, 5);
            grid.padding = new RectOffset(10, 10, 10, 10);
            
            // 9개 슬롯 생성 (6x3 그리드에서 9명만 배치)
            for (int i = 0; i < 9; i++)
            {
                if (unitSlotPrefab != null)
                {
                    GameObject slot = Instantiate(unitSlotPrefab, container);
                    slot.name = $"Squad{squadIndex + 1}_Slot{i + 1}";
                }
            }
        }
        
        void SubscribeToEvents()
        {
            // 자원 업데이트 이벤트
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
            {
                resourceManager.OnGoldChanged += (amount) => UpdateGoldDisplay();
                resourceManager.OnWoodChanged += (amount) => UpdateResourceDisplay();
                resourceManager.OnStoneChanged += (amount) => UpdateResourceDisplay();
                resourceManager.OnManaStoneChanged += (amount) => UpdateResourceDisplay();
                resourceManager.OnReputationChanged += (amount) => UpdateResourceDisplay();
            }
            
            // 게임 속도 버튼
            if (speedButton != null)
            {
                speedButton.onClick.AddListener(OnSpeedButtonClicked);
            }
            
            // 편의 기능 버튼들
            if (bulkRewardButton != null)
            {
                bulkRewardButton.onClick.AddListener(OnBulkRewardClicked);
            }
            
            if (dailyTasksButton != null)
            {
                dailyTasksButton.onClick.AddListener(OnDailyTasksClicked);
            }
        }
        
        void UpdateResourceDisplay()
        {
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager == null) return;
            
            if (goldText != null)
                goldText.text = FormatNumber(resourceManager.GetGold());
                
            if (woodText != null)
                woodText.text = FormatNumber(resourceManager.GetWood());
                
            if (stoneText != null)
                stoneText.text = FormatNumber(resourceManager.GetStone());
                
            if (manaStoneText != null)
                manaStoneText.text = FormatNumber(resourceManager.GetManaStone());
                
            if (reputationText != null)
                reputationText.text = FormatNumber(resourceManager.GetReputation());
        }
        
        void UpdateGoldDisplay()
        {
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager != null && goldText != null)
            {
                goldText.text = FormatNumber(resourceManager.GetGold());
            }
        }
        
        void OnSpeedButtonClicked()
        {
            var speedSystem = GameSpeedSystem.Instance;
            if (speedSystem != null)
            {
                speedSystem.CycleSpeed();
                UpdateSpeedDisplay();
            }
        }
        
        void UpdateSpeedDisplay()
        {
            var speedSystem = GameSpeedSystem.Instance;
            if (speedSystem != null && speedText != null)
            {
                speedText.text = $"{(int)speedSystem.GetCurrentSpeed()}x";
            }
        }
        
        void OnBulkRewardClicked()
        {
            var convenienceSystem = ConvenienceSystem.Instance;
            if (convenienceSystem != null)
            {
                var rewards = convenienceSystem.CollectAllRewards();
                UpdateNotificationBadge();
            }
        }
        
        void OnDailyTasksClicked()
        {
            var convenienceSystem = ConvenienceSystem.Instance;
            if (convenienceSystem != null)
            {
                convenienceSystem.CompleteAllDailies();
            }
        }
        
        void UpdateNotificationBadge()
        {
            var convenienceSystem = ConvenienceSystem.Instance;
            if (convenienceSystem != null && notificationBadge != null)
            {
                int pendingCount = convenienceSystem.GetPendingRewardCount();
                notificationBadge.SetActive(pendingCount > 0);
                
                Text badgeText = notificationBadge.GetComponentInChildren<Text>();
                if (badgeText != null)
                {
                    badgeText.text = pendingCount.ToString();
                }
            }
        }
        
        string FormatNumber(int number)
        {
            if (number >= 1000000)
                return $"{number / 1000000f:F1}M";
            else if (number >= 1000)
                return $"{number / 1000f:F1}K";
            else
                return number.ToString();
        }
        
        void Update()
        {
            // 알림 배지 주기적 업데이트
            if (Time.frameCount % 60 == 0)
            {
                UpdateNotificationBadge();
            }
        }
    }
}