using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.Core;
using GuildMaster.Data;
using GuildMaster.Guild;

namespace GuildMaster.UI
{
    /// <summary>
    /// 건물 건설 UI
    /// </summary>
    public class BuildingConstructionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject constructionPanel;
        [SerializeField] private Transform categoryButtonContainer;
        [SerializeField] private Transform buildingGridContainer;
        [SerializeField] private GameObject categoryButtonPrefab;
        [SerializeField] private GameObject buildingItemPrefab;
        
        [Header("Building Info")]
        [SerializeField] private GameObject buildingInfoPanel;
        [SerializeField] private Image buildingIcon;
        [SerializeField] private TextMeshProUGUI buildingName;
        [SerializeField] private TextMeshProUGUI buildingDescription;
        [SerializeField] private TextMeshProUGUI buildingSize;
        [SerializeField] private Transform effectsContainer;
        [SerializeField] private GameObject effectItemPrefab;
        
        [Header("Cost Display")]
        [SerializeField] private TextMeshProUGUI goldCost;
        [SerializeField] private TextMeshProUGUI woodCost;
        [SerializeField] private TextMeshProUGUI stoneCost;
        [SerializeField] private TextMeshProUGUI manaCost;
        [SerializeField] private TextMeshProUGUI buildTime;
        
        [Header("Buttons")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button rotateButton;
        
        [Header("Selected Building")]
        [SerializeField] private GameObject selectedBuildingPanel;
        [SerializeField] private TextMeshProUGUI selectedName;
        [SerializeField] private TextMeshProUGUI selectedLevel;
        [SerializeField] private Transform selectedStatsContainer;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button demolishButton;
        [SerializeField] private Button toggleActiveButton;
        
        // 카테고리별 건물 목록
        private Dictionary<GuildMaster.Data.BuildingCategory, List<BuildingDataSO>> buildingsByCategory;
        private GuildMaster.Data.BuildingCategory currentCategory = GuildMaster.Data.BuildingCategory.Core;
        private BuildingDataSO selectedBuildingData;
        private PlacedBuilding selectedPlacedBuilding;
        
        // UI 상태
        private bool isConstructionMode = false;
        
        void Start()
        {
            InitializeUI();
            LoadBuildingData();
            SetupEventListeners();
        }
        
        void InitializeUI()
        {
            // 버튼 이벤트 설정
            buildButton.onClick.AddListener(OnBuildButtonClicked);
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
            rotateButton.onClick.AddListener(OnRotateButtonClicked);
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
            demolishButton.onClick.AddListener(OnDemolishButtonClicked);
            toggleActiveButton.onClick.AddListener(OnToggleActiveButtonClicked);
            
            // 초기 상태
            constructionPanel.SetActive(false);
            buildingInfoPanel.SetActive(false);
            selectedBuildingPanel.SetActive(false);
        }
        
        void LoadBuildingData()
        {
            buildingsByCategory = new Dictionary<BuildingCategory, List<BuildingDataSO>>();
            
            // 모든 건물 데이터 로드
            var allBuildings = DataManager.Instance.GetAllBuildingData();
            
            foreach (var building in allBuildings)
            {
                if (!buildingsByCategory.ContainsKey(building.category))
                {
                    buildingsByCategory[building.category] = new List<BuildingDataSO>();
                }
                
                buildingsByCategory[building.category].Add(building);
            }
            
            // 카테고리 버튼 생성
            CreateCategoryButtons();
            
            // 첫 번째 카테고리 선택
            SelectCategory(BuildingCategory.Core);
        }
        
        void CreateCategoryButtons()
        {
            // 기존 버튼 제거
            foreach (Transform child in categoryButtonContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 카테고리별 버튼 생성
            foreach (var category in System.Enum.GetValues(typeof(BuildingCategory)))
            {
                BuildingCategory cat = (BuildingCategory)category;
                
                if (buildingsByCategory.ContainsKey(cat) && buildingsByCategory[cat].Count > 0)
                {
                    GameObject buttonObj = Instantiate(categoryButtonPrefab, categoryButtonContainer);
                    
                    var button = buttonObj.GetComponent<Button>();
                    var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                    
                    text.text = GetCategoryName(cat);
                    button.onClick.AddListener(() => SelectCategory(cat));
                }
            }
        }
        
        string GetCategoryName(GuildMaster.Data.BuildingCategory category)
        {
            switch (category)
            {
                case GuildMaster.Data.BuildingCategory.Core: return "핵심 시설";
                case GuildMaster.Data.BuildingCategory.Production: return "생산 시설";
                case GuildMaster.Data.BuildingCategory.Training: return "훈련 시설";
                case GuildMaster.Data.BuildingCategory.Research: return "연구 시설";
                case GuildMaster.Data.BuildingCategory.Defense: return "방어 시설";
                case GuildMaster.Data.BuildingCategory.Decoration: return "장식 시설";
                default: return category.ToString();
            }
        }
        
        void SelectCategory(GuildMaster.Data.BuildingCategory category)
        {
            currentCategory = category;
            DisplayBuildingsInCategory(category);
        }
        
        void DisplayBuildingsInCategory(GuildMaster.Data.BuildingCategory category)
        {
            // 기존 아이템 제거
            foreach (Transform child in buildingGridContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (!buildingsByCategory.ContainsKey(category))
                return;
                
            // 건물 아이템 생성
            foreach (var building in buildingsByCategory[category])
            {
                CreateBuildingItem(building);
            }
        }
        
        void CreateBuildingItem(BuildingDataSO buildingData)
        {
            GameObject itemObj = Instantiate(buildingItemPrefab, buildingGridContainer);
            
            // 아이콘
            var icon = itemObj.transform.Find("Icon").GetComponent<Image>();
            icon.sprite = buildingData.buildingIcon;
            
            // 이름
            var nameText = itemObj.transform.Find("Name").GetComponent<TextMeshProUGUI>();
            nameText.text = buildingData.buildingName;
            
            // 비용
            var costText = itemObj.transform.Find("Cost").GetComponent<TextMeshProUGUI>();
            costText.text = FormatCost(buildingData.buildCost);
            
            // 버튼 이벤트
            var button = itemObj.GetComponent<Button>();
            button.onClick.AddListener(() => SelectBuilding(buildingData));
            
            // 건설 가능 여부 체크
            bool canBuild = CheckBuildRequirements(buildingData);
            button.interactable = canBuild;
            
            if (!canBuild)
            {
                // 비활성화 표시
                var canvasGroup = itemObj.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = itemObj.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0.5f;
            }
        }
        
        string FormatCost(ResourceCost cost)
        {
            List<string> parts = new List<string>();
            
            if (cost.gold > 0) parts.Add($"골드 {cost.gold}");
            if (cost.wood > 0) parts.Add($"목재 {cost.wood}");
            if (cost.stone > 0) parts.Add($"석재 {cost.stone}");
            if (cost.mana > 0) parts.Add($"마나 {cost.mana}");
            
            return string.Join(", ", parts);
        }
        
        bool CheckBuildRequirements(BuildingDataSO buildingData)
        {
            // 길드 레벨 확인
            if (GuildManager.Instance.GetGuildLevel() < buildingData.requiredGuildLevel)
                return false;
                
            // 자원 확인
            if (!ResourceManager.Instance.CanAfford(buildingData.buildCost.gold, buildingData.buildCost.wood, buildingData.buildCost.stone, buildingData.buildCost.mana))
                return false;
                
            return true;
        }
        
        void SelectBuilding(BuildingDataSO buildingData)
        {
            selectedBuildingData = buildingData;
            ShowBuildingInfo(buildingData);
        }
        
        void ShowBuildingInfo(BuildingDataSO buildingData)
        {
            buildingInfoPanel.SetActive(true);
            
            // 기본 정보
            buildingIcon.sprite = buildingData.buildingIcon;
            buildingName.text = buildingData.buildingName;
            buildingDescription.text = buildingData.description;
            buildingSize.text = $"크기: {buildingData.sizeX}x{buildingData.sizeY}";
            
            // 비용
            goldCost.text = buildingData.buildCost.gold.ToString();
            woodCost.text = buildingData.buildCost.wood.ToString();
            stoneCost.text = buildingData.buildCost.stone.ToString();
            manaCost.text = buildingData.buildCost.mana.ToString();
            buildTime.text = FormatTime(buildingData.buildTime);
            
            // 효과
            DisplayBuildingEffects(buildingData);
            
            // 건설 버튼 상태
            bool canBuild = CheckBuildRequirements(buildingData);
            buildButton.interactable = canBuild;
        }
        
        void DisplayBuildingEffects(BuildingDataSO buildingData)
        {
            // 기존 효과 제거
            foreach (Transform child in effectsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 효과 표시
            foreach (var effect in buildingData.effects)
            {
                GameObject effectObj = Instantiate(effectItemPrefab, effectsContainer);
                var text = effectObj.GetComponent<TextMeshProUGUI>();
                text.text = $"• {effect.description}";
            }
            
            // 생산 효과
            if (buildingData.isProducer)
            {
                var production = buildingData.production;
                
                if (production.goldPerHour > 0)
                    CreateEffectItem($"• 시간당 골드 +{production.goldPerHour}");
                if (production.woodPerHour > 0)
                    CreateEffectItem($"• 시간당 목재 +{production.woodPerHour}");
                if (production.stonePerHour > 0)
                    CreateEffectItem($"• 시간당 석재 +{production.stonePerHour}");
                if (production.manaPerHour > 0)
                    CreateEffectItem($"• 시간당 마나 +{production.manaPerHour}");
            }
        }
        
        void CreateEffectItem(string text)
        {
            GameObject effectObj = Instantiate(effectItemPrefab, effectsContainer);
            var textComp = effectObj.GetComponent<TextMeshProUGUI>();
            textComp.text = text;
        }
        
        string FormatTime(float seconds)
        {
            if (seconds < 60)
                return $"{seconds:0}초";
            else if (seconds < 3600)
                return $"{seconds / 60:0}분";
            else
                return $"{seconds / 3600:0.0}시간";
        }
        
        void OnBuildButtonClicked()
        {
            if (selectedBuildingData == null) return;
            
            // 건설 모드 시작
            GuildBuildingSystem.Instance.StartBuildingMode(selectedBuildingData);
            
            // UI 닫기
            HideConstructionUI();
        }
        
        void OnCancelButtonClicked()
        {
            HideConstructionUI();
        }
        
        void OnRotateButtonClicked()
        {
            // 건물 회전 (추후 구현)
        }
        
        void OnUpgradeButtonClicked()
        {
            if (selectedPlacedBuilding == null) return;
            
            GuildBuildingSystem.Instance.UpgradeBuilding(selectedPlacedBuilding);
            UpdateSelectedBuildingInfo();
        }
        
        void OnDemolishButtonClicked()
        {
            if (selectedPlacedBuilding == null) return;
            
            // 확인 대화상자
            UIManager.Instance.ShowConfirmDialog(
                "건물 철거",
                "정말로 이 건물을 철거하시겠습니까?\n자원의 50%가 환급됩니다.",
                () => {
                    GuildBuildingSystem.Instance.RemoveBuilding(selectedPlacedBuilding);
                    HideSelectedBuildingPanel();
                }
            );
        }
        
        void OnToggleActiveButtonClicked()
        {
            if (selectedPlacedBuilding == null) return;
            
            selectedPlacedBuilding.SetActive(!selectedPlacedBuilding.isActive);
            UpdateSelectedBuildingInfo();
        }
        
        void SetupEventListeners()
        {
            // 건물 선택 이벤트
            GuildBuildingSystem.Instance.OnBuildingSelected += OnBuildingSelected;
            
            // 건물 업그레이드 이벤트
            GuildBuildingSystem.Instance.OnBuildingUpgraded += OnBuildingUpgraded;
        }
        
        void OnBuildingSelected(PlacedBuilding building)
        {
            selectedPlacedBuilding = building;
            ShowSelectedBuildingPanel(building);
        }
        
        void OnBuildingUpgraded(PlacedBuilding building)
        {
            if (selectedPlacedBuilding == building)
            {
                UpdateSelectedBuildingInfo();
            }
        }
        
        void ShowSelectedBuildingPanel(PlacedBuilding building)
        {
            selectedBuildingPanel.SetActive(true);
            UpdateSelectedBuildingInfo();
        }
        
        void UpdateSelectedBuildingInfo()
        {
            if (selectedPlacedBuilding == null) return;
            
            selectedName.text = selectedPlacedBuilding.buildingData.name;
            selectedLevel.text = $"레벨 {selectedPlacedBuilding.currentLevel}";
            
            // 스탯 표시
            var stats = selectedPlacedBuilding.GetBuildingStats();
            DisplaySelectedBuildingStats(stats);
            
            // 버튼 상태
            upgradeButton.interactable = selectedPlacedBuilding.CanUpgrade();
            
            var toggleText = toggleActiveButton.GetComponentInChildren<TextMeshProUGUI>();
            toggleText.text = selectedPlacedBuilding.isActive ? "비활성화" : "활성화";
        }
        
        void DisplaySelectedBuildingStats(GuildMaster.Guild.BuildingStats stats)
        {
            // 기존 스탯 제거
            foreach (Transform child in selectedStatsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 스탯 표시
            if (stats.maxAdventurers > 0)
                CreateStatItem($"최대 모험가: +{stats.maxAdventurers}");
            if (stats.trainingSpeed > 0)
                CreateStatItem($"훈련 속도: +{stats.trainingSpeed:0}%");
            if (stats.researchSpeed > 0)
                CreateStatItem($"연구 속도: +{stats.researchSpeed:0}%");
            if (stats.storageCapacity > 0)
                CreateStatItem($"저장 용량: +{stats.storageCapacity}");
            if (stats.goldProduction > 0)
                CreateStatItem($"골드 생산: +{stats.goldProduction:0}/시간");
            if (stats.woodProduction > 0)
                CreateStatItem($"목재 생산: +{stats.woodProduction:0}/시간");
            if (stats.stoneProduction > 0)
                CreateStatItem($"석재 생산: +{stats.stoneProduction:0}/시간");
            if (stats.manaProduction > 0)
                CreateStatItem($"마나 생산: +{stats.manaProduction:0}/시간");
        }
        
        void CreateStatItem(string text)
        {
            GameObject statObj = new GameObject("StatItem");
            statObj.transform.SetParent(selectedStatsContainer);
            
            var textComp = statObj.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 14;
        }
        
        void HideSelectedBuildingPanel()
        {
            selectedBuildingPanel.SetActive(false);
            selectedPlacedBuilding = null;
        }
        
        public void ShowConstructionUI()
        {
            constructionPanel.SetActive(true);
            isConstructionMode = true;
        }
        
        public void HideConstructionUI()
        {
            constructionPanel.SetActive(false);
            buildingInfoPanel.SetActive(false);
            isConstructionMode = false;
        }
        
        void OnDestroy()
        {
            // 이벤트 리스너 제거
            if (GuildBuildingSystem.Instance != null)
            {
                GuildBuildingSystem.Instance.OnBuildingSelected -= OnBuildingSelected;
                GuildBuildingSystem.Instance.OnBuildingUpgraded -= OnBuildingUpgraded;
            }
        }
    }
}