using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.Core;
using GuildMaster.Battle;
using GuildMaster.Systems;
using GuildMaster.Exploration;
using GuildMaster.Guild;

namespace GuildMaster.UI
{
    /// <summary>
    /// 던전 탐험 UI
    /// 던전 목록, 입장, 진행 상황 표시
    /// </summary>
    public class DungeonExplorationUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject dungeonPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        
        [Header("Dungeon Tabs")]
        [SerializeField] private GameObject tabContainer;
        [SerializeField] private Button normalDungeonsTab;
        [SerializeField] private Button eliteDungeonsTab;
        [SerializeField] private Button specialDungeonsTab;
        [SerializeField] private Button infiniteDungeonTab;
        
        [Header("Dungeon List")]
        [SerializeField] private Transform dungeonListContainer;
        [SerializeField] private GameObject dungeonEntryPrefab;
        [SerializeField] private ScrollRect dungeonScrollView;
        
        [Header("Dungeon Details")]
        [SerializeField] private GameObject dungeonDetailsPanel;
        [SerializeField] private TextMeshProUGUI dungeonNameText;
        [SerializeField] private TextMeshProUGUI dungeonDescriptionText;
        [SerializeField] private TextMeshProUGUI dungeonLevelText;
        [SerializeField] private TextMeshProUGUI recommendedPowerText;
        [SerializeField] private TextMeshProUGUI floorsText;
        [SerializeField] private TextMeshProUGUI entryCostText;
        [SerializeField] private Image dungeonTypeIcon;
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private GameObject rewardItemPrefab;
        
        [Header("Entry Requirements")]
        [SerializeField] private GameObject requirementsPanel;
        [SerializeField] private TextMeshProUGUI guildLevelRequirement;
        [SerializeField] private TextMeshProUGUI reputationRequirement;
        [SerializeField] private TextMeshProUGUI dailyLimitText;
        [SerializeField] private Image requirementCheckIcon;
        
        [Header("Squad Selection")]
        [SerializeField] private GameObject squadSelectionPanel;
        [SerializeField] private Transform squadSlotsContainer;
        [SerializeField] private GameObject squadSlotPrefab;
        [SerializeField] private Button autoSelectButton;
        [SerializeField] private TextMeshProUGUI totalPowerText;
        [SerializeField] private Button enterDungeonButton;
        
        [Header("Exploration Progress")]
        [SerializeField] private GameObject explorationPanel;
        [SerializeField] private TextMeshProUGUI currentFloorText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Transform explorationLogContainer;
        [SerializeField] private GameObject logEntryPrefab;
        [SerializeField] private Button speedUpButton;
        [SerializeField] private Button retreatButton;
        
        [Header("Results Panel")]
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI floorsCompletedText;
        [SerializeField] private TextMeshProUGUI explorationTimeText;
        [SerializeField] private Transform resultRewardsContainer;
        [SerializeField] private Button claimRewardsButton;
        
        [Header("Infinite Dungeon")]
        [SerializeField] private GameObject infiniteProgressPanel;
        [SerializeField] private TextMeshProUGUI currentInfiniteFloorText;
        [SerializeField] private TextMeshProUGUI highestFloorText;
        [SerializeField] private Button continueInfiniteButton;
        [SerializeField] private Button exitInfiniteButton;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem discoveryEffect;
        [SerializeField] private ParticleSystem completionEffect;
        [SerializeField] private AnimationCurve panelAnimCurve;
        
        // System references
        private DungeonSystem dungeonSystem;
        private GuildManager guildManager;
        private BattleManager battleManager;
        
        // UI State
        private DungeonSystem.DungeonType currentTabType = DungeonSystem.DungeonType.Normal;
        private DungeonSystem.Dungeon selectedDungeon;
        private List<Squad> selectedSquads = new List<Squad>();
        private List<GameObject> dungeonEntries = new List<GameObject>();
        private List<GameObject> logEntries = new List<GameObject>();
        private Coroutine explorationCoroutine;
        
        [System.Serializable]
        public class DungeonEntry
        {
            public GameObject entryObject;
            public DungeonSystem.Dungeon dungeon;
            public Button selectButton;
            public Image completionIcon;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI levelText;
            public TextMeshProUGUI statusText;
        }
        
        void Start()
        {
            dungeonSystem = DungeonSystem.Instance;
            guildManager = Core.GameManager.Instance?.GuildManager;
            battleManager = Core.GameManager.Instance?.BattleManager;
            
            if (dungeonSystem == null)
            {
                Debug.LogError("DungeonSystem not found!");
                enabled = false;
                return;
            }
            
            SetupUI();
            SubscribeToEvents();
            
            // 초기 UI 업데이트
            RefreshDungeonList();
            ShowTab(DungeonSystem.DungeonType.Normal);
        }
        
        void SetupUI()
        {
            // 메인 버튼
            if (openButton != null)
                openButton.onClick.AddListener(() => ShowPanel(true));
            if (closeButton != null)
                closeButton.onClick.AddListener(() => ShowPanel(false));
            
            // 탭 버튼
            if (normalDungeonsTab != null)
                normalDungeonsTab.onClick.AddListener(() => ShowTab(DungeonSystem.DungeonType.Normal));
            if (eliteDungeonsTab != null)
                eliteDungeonsTab.onClick.AddListener(() => ShowTab(DungeonSystem.DungeonType.Elite));
            if (specialDungeonsTab != null)
                specialDungeonsTab.onClick.AddListener(() => ShowTab(DungeonSystem.DungeonType.Event));
            if (infiniteDungeonTab != null)
                infiniteDungeonTab.onClick.AddListener(() => ShowTab(DungeonSystem.DungeonType.Infinite));
            
            // 기타 버튼
            if (autoSelectButton != null)
                autoSelectButton.onClick.AddListener(AutoSelectSquads);
            if (enterDungeonButton != null)
                enterDungeonButton.onClick.AddListener(EnterDungeon);
            if (speedUpButton != null)
                speedUpButton.onClick.AddListener(SpeedUpExploration);
            if (retreatButton != null)
                retreatButton.onClick.AddListener(RetreatFromDungeon);
            if (claimRewardsButton != null)
                claimRewardsButton.onClick.AddListener(ClaimRewards);
            if (continueInfiniteButton != null)
                continueInfiniteButton.onClick.AddListener(ContinueInfiniteDungeon);
            if (exitInfiniteButton != null)
                exitInfiniteButton.onClick.AddListener(ExitInfiniteDungeon);
        }
        
        void SubscribeToEvents()
        {
            if (dungeonSystem != null)
            {
                dungeonSystem.OnDungeonUnlocked += OnDungeonUnlocked;
                dungeonSystem.OnDungeonEntered += OnDungeonEntered;
                dungeonSystem.OnFloorCleared += OnFloorCleared;
                dungeonSystem.OnExplorationComplete += OnExplorationComplete;
                dungeonSystem.OnEncounterStart += OnEncounterStart;
                dungeonSystem.OnRewardReceived += OnRewardReceived;
            }
        }
        
        void OnDestroy()
        {
            if (explorationCoroutine != null)
            {
                StopCoroutine(explorationCoroutine);
            }
            
            // 이벤트 구독 해제
            if (dungeonSystem != null)
            {
                dungeonSystem.OnDungeonUnlocked -= OnDungeonUnlocked;
                dungeonSystem.OnDungeonEntered -= OnDungeonEntered;
                dungeonSystem.OnFloorCleared -= OnFloorCleared;
                dungeonSystem.OnExplorationComplete -= OnExplorationComplete;
                dungeonSystem.OnEncounterStart -= OnEncounterStart;
                dungeonSystem.OnRewardReceived -= OnRewardReceived;
            }
        }
        
        void ShowPanel(bool show)
        {
            if (dungeonPanel != null)
            {
                dungeonPanel.SetActive(show);
                
                if (show)
                {
                    RefreshDungeonList();
                    StartCoroutine(AnimatePanel(true));
                }
                else
                {
                    StartCoroutine(AnimatePanel(false));
                }
            }
        }
        
        IEnumerator AnimatePanel(bool show)
        {
            if (dungeonPanel == null) yield break;
            
            var canvasGroup = dungeonPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dungeonPanel.AddComponent<CanvasGroup>();
            }
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                if (panelAnimCurve != null)
                    t = panelAnimCurve.Evaluate(t);
                
                canvasGroup.alpha = show ? t : 1f - t;
                dungeonPanel.transform.localScale = Vector3.one * (show ? 0.8f + 0.2f * t : 1f - 0.2f * t);
                
                yield return null;
            }
            
            if (!show)
            {
                dungeonPanel.SetActive(false);
            }
        }
        
        void ShowTab(DungeonSystem.DungeonType dungeonType)
        {
            currentTabType = dungeonType;
            RefreshDungeonList();
            
            // 탭 하이라이트
            UpdateTabHighlight();
        }
        
        void UpdateTabHighlight()
        {
            Color normalColor = new Color(0.8f, 0.8f, 0.8f);
            Color selectedColor = Color.white;
            
            if (normalDungeonsTab != null)
                normalDungeonsTab.image.color = currentTabType == DungeonSystem.DungeonType.Normal ? selectedColor : normalColor;
            if (eliteDungeonsTab != null)
                eliteDungeonsTab.image.color = currentTabType == DungeonSystem.DungeonType.Elite ? selectedColor : normalColor;
            if (specialDungeonsTab != null)
                specialDungeonsTab.image.color = currentTabType == DungeonSystem.DungeonType.Event ? selectedColor : normalColor;
            if (infiniteDungeonTab != null)
                infiniteDungeonTab.image.color = currentTabType == DungeonSystem.DungeonType.Infinite ? selectedColor : normalColor;
        }
        
        void RefreshDungeonList()
        {
            // 기존 엔트리 제거
            foreach (var entry in dungeonEntries)
            {
                Destroy(entry);
            }
            dungeonEntries.Clear();
            
            // 던전 목록 가져오기
            var dungeons = dungeonSystem.GetDungeonsByType(currentTabType);
            
            // 던전 엔트리 생성
            foreach (var dungeon in dungeons)
            {
                CreateDungeonEntry(dungeon);
            }
            
            // 레이아웃 업데이트
            LayoutRebuilder.ForceRebuildLayoutImmediate(dungeonListContainer as RectTransform);
        }
        
        void CreateDungeonEntry(DungeonSystem.Dungeon dungeon)
        {
            if (dungeonEntryPrefab == null || dungeonListContainer == null) return;
            
            var entryObj = Instantiate(dungeonEntryPrefab, dungeonListContainer);
            dungeonEntries.Add(entryObj);
            
            var entry = new DungeonEntry
            {
                entryObject = entryObj,
                dungeon = dungeon
            };
            
            // UI 요소 찾기
            entry.nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            entry.levelText = entryObj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            entry.statusText = entryObj.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            entry.completionIcon = entryObj.transform.Find("CompletionIcon")?.GetComponent<Image>();
            entry.selectButton = entryObj.GetComponent<Button>();
            
            // 정보 설정
            if (entry.nameText != null)
                entry.nameText.text = dungeon.dungeonName;
            
            if (entry.levelText != null)
                entry.levelText.text = $"Lv.{dungeon.recommendedLevel}";
            
            if (entry.statusText != null)
            {
                if (dungeon.isCompleted)
                    entry.statusText.text = "클리어";
                else if (dungeon.todayEntries >= dungeon.dailyEntryLimit)
                    entry.statusText.text = "입장 제한";
                else
                    entry.statusText.text = $"입장 가능 ({dungeon.dailyEntryLimit - dungeon.todayEntries}회)";
            }
            
            // 완료 아이콘
            if (entry.completionIcon != null)
            {
                entry.completionIcon.gameObject.SetActive(dungeon.isCompleted);
            }
            
            // 선택 버튼
            if (entry.selectButton != null)
            {
                entry.selectButton.onClick.AddListener(() => SelectDungeon(dungeon));
                entry.selectButton.interactable = dungeon.todayEntries < dungeon.dailyEntryLimit;
            }
        }
        
        void SelectDungeon(DungeonSystem.Dungeon dungeon)
        {
            selectedDungeon = dungeon;
            ShowDungeonDetails(dungeon);
            ShowSquadSelection();
        }
        
        void ShowDungeonDetails(DungeonSystem.Dungeon dungeon)
        {
            if (dungeonDetailsPanel != null)
            {
                dungeonDetailsPanel.SetActive(true);
                
                if (dungeonNameText != null)
                    dungeonNameText.text = dungeon.dungeonName;
                
                if (dungeonDescriptionText != null)
                    dungeonDescriptionText.text = dungeon.description;
                
                if (dungeonLevelText != null)
                    dungeonLevelText.text = $"권장 레벨: {dungeon.recommendedLevel}";
                
                if (recommendedPowerText != null)
                    recommendedPowerText.text = $"권장 전투력: {dungeon.recommendedPower:N0}";
                
                if (floorsText != null)
                    floorsText.text = $"층수: {dungeon.maxFloors}층";
                
                if (entryCostText != null)
                    entryCostText.text = $"입장료: {dungeon.entryCost} 골드";
                
                // 보상 표시
                ShowDungeonRewards(dungeon);
                
                // 요구사항 표시
                ShowDungeonRequirements(dungeon);
            }
        }
        
        void ShowDungeonRewards(DungeonSystem.Dungeon dungeon)
        {
            // 기존 보상 아이템 제거
            foreach (Transform child in rewardsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 첫 클리어 보상
            if (!dungeon.isCompleted && dungeon.firstClearReward != null)
            {
                CreateRewardItem("첫 클리어 보상", Color.yellow);
                
                if (dungeon.firstClearReward.goldReward > 0)
                    CreateRewardItem($"골드: {dungeon.firstClearReward.goldReward}");
                
                if (dungeon.firstClearReward.expReward > 0)
                    CreateRewardItem($"경험치: {dungeon.firstClearReward.expReward}");
                
                if (dungeon.firstClearReward.unitRewardChance > 0)
                    CreateRewardItem($"유닛 획득 확률: {dungeon.firstClearReward.unitRewardChance:P0}");
            }
            
            // 반복 보상
            if (dungeon.repeatReward != null)
            {
                CreateRewardItem("기본 보상", Color.white);
                
                if (dungeon.repeatReward.goldReward > 0)
                    CreateRewardItem($"골드: {dungeon.repeatReward.goldReward}");
                
                if (dungeon.repeatReward.expReward > 0)
                    CreateRewardItem($"경험치: {dungeon.repeatReward.expReward}");
            }
        }
        
        void CreateRewardItem(string text, Color? color = null)
        {
            if (rewardItemPrefab != null && rewardsContainer != null)
            {
                var item = Instantiate(rewardItemPrefab, rewardsContainer);
                var textComp = item.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                {
                    textComp.text = text;
                    if (color.HasValue)
                        textComp.color = color.Value;
                }
            }
        }
        
        void ShowDungeonRequirements(DungeonSystem.Dungeon dungeon)
        {
            if (requirementsPanel == null) return;
            
            requirementsPanel.SetActive(true);
            
            bool canEnter = true;
            
            // 길드 레벨 요구사항
            if (guildLevelRequirement != null)
            {
                int currentLevel = guildManager?.GetGuildLevel() ?? 1;
                guildLevelRequirement.text = $"길드 레벨: {currentLevel}/{dungeon.recommendedLevel}";
                
                if (currentLevel < dungeon.recommendedLevel)
                {
                    guildLevelRequirement.color = Color.red;
                    canEnter = false;
                }
                else
                {
                    guildLevelRequirement.color = Color.green;
                }
            }
            
            // 명성 요구사항
            if (reputationRequirement != null)
            {
                int currentRep = 0; // TODO: Get from guild manager
                reputationRequirement.text = $"명성: {currentRep}/{dungeon.recommendedLevel * 50}";
            }
            
            // 일일 제한
            if (dailyLimitText != null)
            {
                dailyLimitText.text = $"오늘 입장: {dungeon.todayEntries}/{dungeon.dailyEntryLimit}";
                
                if (dungeon.todayEntries >= dungeon.dailyEntryLimit)
                {
                    dailyLimitText.color = Color.red;
                    canEnter = false;
                }
                else
                {
                    dailyLimitText.color = Color.white;
                }
            }
            
            // 체크 아이콘
            if (requirementCheckIcon != null)
            {
                requirementCheckIcon.color = canEnter ? Color.green : Color.red;
            }
        }
        
        void ShowSquadSelection()
        {
            if (squadSelectionPanel != null)
            {
                squadSelectionPanel.SetActive(true);
                
                // 스쿼드 슬롯 생성
                CreateSquadSlots();
                
                // 입장 버튼 상태
                UpdateEnterButton();
            }
        }
        
        void CreateSquadSlots()
        {
            // 기존 슬롯 제거
            foreach (Transform child in squadSlotsContainer)
            {
                Destroy(child.gameObject);
            }
            
            selectedSquads.Clear();
            
            // 4개 스쿼드 슬롯 생성
            for (int i = 0; i < 4; i++)
            {
                if (squadSlotPrefab != null)
                {
                    var slot = Instantiate(squadSlotPrefab, squadSlotsContainer);
                    SetupSquadSlot(slot, i);
                }
            }
        }
        
        void SetupSquadSlot(GameObject slot, int index)
        {
            var nameText = slot.transform.Find("SquadNameText")?.GetComponent<TextMeshProUGUI>();
            var powerText = slot.transform.Find("PowerText")?.GetComponent<TextMeshProUGUI>();
            var selectButton = slot.GetComponent<Button>();
            
            if (nameText != null)
                nameText.text = $"부대 {index + 1} 선택";
            
            if (selectButton != null)
            {
                int slotIndex = index;
                selectButton.onClick.AddListener(() => SelectSquadForSlot(slotIndex));
            }
        }
        
        void SelectSquadForSlot(int slotIndex)
        {
            // TODO: 스쿼드 선택 UI 표시
            Debug.Log($"Select squad for slot {slotIndex}");
        }
        
        void AutoSelectSquads()
        {
            if (battleManager != null)
            {
                // BattleManager에서 BattleSquad 가져오기
                var allBattleSquads = battleManager.GetPlayerSquads();
                
                // BattleSquad를 Squad로 변환
                var allSquads = new List<Squad>();
                foreach (var battleSquad in allBattleSquads)
                {
                    if (battleSquad != null && !battleSquad.IsDefeated)
                    {
                        var squad = ConvertBattleSquadToSquad(battleSquad);
                        if (squad != null)
                        {
                            allSquads.Add(squad);
                        }
                    }
                }
                
                // 전투력 순으로 정렬
                var sortedSquads = allSquads.OrderByDescending(s => s.GetTotalCombatPower()).Take(4).ToList();
                
                selectedSquads.Clear();
                selectedSquads.AddRange(sortedSquads);
                
                UpdateSquadDisplay();
                UpdateEnterButton();
            }
        }
        
        private Squad ConvertBattleSquadToSquad(BattleManager.BattleSquad battleSquad)
        {
            // BattleSquad의 Units를 Squad로 변환
            var units = new List<Unit>();
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    if (battleSquad.Units[row, col] != null)
                    {
                        units.Add(battleSquad.Units[row, col]);
                    }
                }
            }
            
            if (units.Count == 0) return null;
            
            var squad = new Squad($"Squad_{battleSquad.SquadIndex}", battleSquad.SquadIndex, battleSquad.IsPlayerSquad);
            
            // 유닛들을 부대에 추가
            foreach (var unit in units)
            {
                squad.AddUnit(unit);
            }
            
            return squad;
        }
        
        void UpdateSquadDisplay()
        {
            float totalPower = selectedSquads.Sum(s => s.GetTotalCombatPower());
            
            if (totalPowerText != null)
            {
                totalPowerText.text = $"총 전투력: {totalPower:N0}";
                
                // 권장 전투력과 비교
                if (selectedDungeon != null)
                {
                    if (totalPower < selectedDungeon.recommendedPower)
                    {
                        totalPowerText.color = Color.red;
                    }
                    else if (totalPower < selectedDungeon.recommendedPower * 1.2f)
                    {
                        totalPowerText.color = Color.yellow;
                    }
                    else
                    {
                        totalPowerText.color = Color.green;
                    }
                }
            }
        }
        
        void UpdateEnterButton()
        {
            if (enterDungeonButton != null)
            {
                bool canEnter = selectedDungeon != null && 
                               selectedSquads.Count > 0 &&
                               !dungeonSystem.IsExploring();
                
                enterDungeonButton.interactable = canEnter;
            }
        }
        
        void EnterDungeon()
        {
            if (selectedDungeon == null || selectedSquads.Count == 0) return;
            
            bool success = dungeonSystem.EnterDungeon(selectedDungeon.dungeonId, selectedSquads);
            
            if (success)
            {
                ShowExplorationPanel();
                explorationCoroutine = StartCoroutine(UpdateExplorationProgress());
            }
            else
            {
                Debug.LogWarning("Failed to enter dungeon");
            }
        }
        
        void ShowExplorationPanel()
        {
            if (squadSelectionPanel != null)
                squadSelectionPanel.SetActive(false);
            
            if (explorationPanel != null)
            {
                explorationPanel.SetActive(true);
                
                // 로그 초기화
                foreach (var entry in logEntries)
                {
                    Destroy(entry);
                }
                logEntries.Clear();
            }
        }
        
        IEnumerator UpdateExplorationProgress()
        {
            while (dungeonSystem.IsExploring())
            {
                float progress = dungeonSystem.GetExplorationProgress();
                
                if (progressBar != null)
                    progressBar.value = progress;
                
                if (progressText != null)
                    progressText.text = $"{progress:P0}";
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        void SpeedUpExploration()
        {
            // TODO: 속도 증가 구현 (재화 소모)
            Debug.Log("Speed up exploration");
        }
        
        void RetreatFromDungeon()
        {
            // TODO: 후퇴 구현
            Debug.Log("Retreat from dungeon");
        }
        
        void ContinueInfiniteDungeon()
        {
            dungeonSystem.ProgressInfiniteDungeon();
        }
        
        void ExitInfiniteDungeon()
        {
            // TODO: 무한 던전 종료
            Debug.Log("Exit infinite dungeon");
        }
        
        void ClaimRewards()
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }
            
            ShowPanel(false);
        }
        
        // 이벤트 핸들러
        void OnDungeonUnlocked(DungeonSystem.Dungeon dungeon)
        {
            // 발견 효과
            if (discoveryEffect != null)
            {
                discoveryEffect.Play();
            }
            
            // UI 갱신
            RefreshDungeonList();
            
            // 알림
            ShowNotification($"새로운 던전 발견: {dungeon.dungeonName}!");
        }
        
        void OnDungeonEntered(DungeonSystem.Dungeon dungeon)
        {
            AddLogEntry($"=== {dungeon.dungeonName} 입장 ===");
        }
        
        void OnFloorCleared(DungeonSystem.DungeonFloor floor)
        {
            if (currentFloorText != null)
                currentFloorText.text = $"현재 층: {floor.floorNumber}";
            
            AddLogEntry($"✓ {floor.floorName} 클리어!");
        }
        
        void OnExplorationComplete(DungeonSystem.ExplorationResult result)
        {
            if (explorationCoroutine != null)
            {
                StopCoroutine(explorationCoroutine);
            }
            
            ShowExplorationResult(result);
        }
        
        void OnEncounterStart(DungeonSystem.DungeonEncounter encounter)
        {
            string icon = encounter.type switch
            {
                DungeonSystem.DungeonEncounter.EncounterType.Battle => "⚔️",
                DungeonSystem.DungeonEncounter.EncounterType.Treasure => "💎",
                DungeonSystem.DungeonEncounter.EncounterType.Trap => "⚠️",
                DungeonSystem.DungeonEncounter.EncounterType.Rest => "🏕️",
                DungeonSystem.DungeonEncounter.EncounterType.Event => "❓",
                DungeonSystem.DungeonEncounter.EncounterType.Boss => "👹",
                _ => "•"
            };
            
            AddLogEntry($"{icon} {encounter.encounterName}");
        }
        
        void OnRewardReceived(DungeonSystem.DungeonReward reward)
        {
            // 보상 획득 효과
            if (reward.goldReward > 0)
                AddLogEntry($"💰 골드 +{reward.goldReward}");
            
            if (reward.expReward > 0)
                AddLogEntry($"⭐ 경험치 +{reward.expReward}");
        }
        
        void AddLogEntry(string text)
        {
            if (logEntryPrefab != null && explorationLogContainer != null)
            {
                var entry = Instantiate(logEntryPrefab, explorationLogContainer);
                var textComp = entry.GetComponentInChildren<TextMeshProUGUI>();
                
                if (textComp != null)
                {
                    textComp.text = text;
                }
                
                logEntries.Add(entry);
                
                // 스크롤 하단으로
                Canvas.ForceUpdateCanvases();
                var scrollRect = explorationLogContainer.GetComponentInParent<ScrollRect>();
                if (scrollRect != null)
                {
                    scrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }
        
        void ShowExplorationResult(DungeonSystem.ExplorationResult result)
        {
            if (explorationPanel != null)
                explorationPanel.SetActive(false);
            
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
                
                if (resultTitleText != null)
                    resultTitleText.text = result.isSuccess ? "탐험 성공!" : "탐험 실패";
                
                if (floorsCompletedText != null)
                    floorsCompletedText.text = $"클리어한 층: {result.floorsCleared}";
                
                if (explorationTimeText != null)
                {
                    int minutes = Mathf.FloorToInt(result.explorationTime / 60f);
                    int seconds = Mathf.FloorToInt(result.explorationTime % 60f);
                    explorationTimeText.text = $"소요 시간: {minutes:00}:{seconds:00}";
                }
                
                // 보상 표시
                ShowResultRewards(result.totalRewards);
                
                // 완료 효과
                if (result.isSuccess && completionEffect != null)
                {
                    completionEffect.Play();
                }
            }
        }
        
        void ShowResultRewards(DungeonSystem.DungeonReward rewards)
        {
            // 기존 보상 제거
            foreach (Transform child in resultRewardsContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (rewards.goldReward > 0)
                CreateRewardItem($"골드: +{rewards.goldReward}");
            
            if (rewards.expReward > 0)
                CreateRewardItem($"경험치: +{rewards.expReward}");
            
            foreach (var resource in rewards.resourceRewards)
            {
                CreateRewardItem($"{resource.Key}: +{resource.Value}");
            }
            
            foreach (var item in rewards.itemRewards)
            {
                CreateRewardItem($"아이템: {item}");
            }
            
            if (rewards.unitRewardChance >= 1f)
            {
                CreateRewardItem("새로운 유닛 획득!", Color.yellow);
            }
        }
        
        void ShowNotification(string message)
        {
            Debug.Log($"Dungeon Notification: {message}");
            // TODO: 실제 알림 UI 구현
        }
        
        // 무한 던전 UI 업데이트
        public void UpdateInfiniteProgress()
        {
            if (infiniteProgressPanel == null) return;
            
            int currentFloor = dungeonSystem.GetInfiniteDungeonCurrentFloor();
            int highestFloor = dungeonSystem.GetInfiniteHighScore();
            
            if (currentInfiniteFloorText != null)
                currentInfiniteFloorText.text = $"현재 층: {currentFloor}";
            
            if (highestFloorText != null)
                highestFloorText.text = $"최고 기록: {highestFloor}층";
        }
    }

    // 확장 메서드
    public static class DungeonSystemExtensions
    {
        public static int GetInfiniteDungeonCurrentFloor(this DungeonSystem system)
        {
            // TODO: DungeonSystem에서 현재 무한 던전 층 반환
            return 1;
        }
    }
}