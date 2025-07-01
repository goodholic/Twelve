using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Systems;
using GuildMaster.Battle;

namespace GuildMaster.UI
{
    /// <summary>
    /// 전투 시뮬레이션 UI
    /// 시뮬레이션 실행, 결과 분석, 통계 표시
    /// </summary>
    public class BattleSimulationUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject simulationPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        
        [Header("Simulation Controls")]
        [SerializeField] private TMP_Dropdown simulationTypeDropdown;
        [SerializeField] private TMP_InputField simulationRunsInput;
        [SerializeField] private Slider simulationSpeedSlider;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private Toggle detailedLoggingToggle;
        [SerializeField] private Button startSimulationButton;
        [SerializeField] private Button stopSimulationButton;
        
        [Header("Progress Display")]
        [SerializeField] private GameObject progressPanel;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI currentBattleText;
        [SerializeField] private TextMeshProUGUI winRateText;
        [SerializeField] private TextMeshProUGUI timeRemainingText;
        
        [Header("Results Overview")]
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI totalBattlesText;
        [SerializeField] private TextMeshProUGUI victoriesText;
        [SerializeField] private TextMeshProUGUI defeatsText;
        [SerializeField] private TextMeshProUGUI finalWinRateText;
        [SerializeField] private TextMeshProUGUI simulationTimeText;
        [SerializeField] private Button viewDetailsButton;
        
        [Header("Detailed Results")]
        [SerializeField] private GameObject detailsPanel;
        [SerializeField] private GameObject unitStatsTab;
        [SerializeField] private GameObject jobStatsTab;
        [SerializeField] private GameObject battleLogTab;
        [SerializeField] private Button unitStatsButton;
        [SerializeField] private Button jobStatsButton;
        [SerializeField] private Button battleLogButton;
        
        [Header("Unit Statistics")]
        [SerializeField] private Transform unitStatsContainer;
        [SerializeField] private GameObject unitStatEntryPrefab;
        [SerializeField] private TMP_Dropdown unitSortDropdown;
        [SerializeField] private Toggle showOnlyActiveToggle;
        
        [Header("Job Class Statistics")]
        [SerializeField] private Transform jobStatsContainer;
        [SerializeField] private GameObject jobStatEntryPrefab;
        [SerializeField] private GameObject jobComparisonChart;
        
        [Header("Battle Log")]
        [SerializeField] private Transform battleLogContainer;
        [SerializeField] private GameObject battleLogEntryPrefab;
        [SerializeField] private ScrollRect battleLogScrollRect;
        [SerializeField] private Toggle showOnlyKeyBattlesToggle;
        
        [Header("Visualization")]
        [SerializeField] private GameObject winRateChart;
        [SerializeField] private Image winRateChartFill;
        [SerializeField] private LineRenderer winRateTrend;
        [SerializeField] private GameObject damageDistributionChart;
        [SerializeField] private Transform damageBarContainer;
        
        [Header("Recommendations")]
        [SerializeField] private GameObject recommendationsPanel;
        [SerializeField] private TextMeshProUGUI squadRecommendationText;
        [SerializeField] private TextMeshProUGUI unitRecommendationText;
        [SerializeField] private TextMeshProUGUI strategyRecommendationText;
        [SerializeField] private Button applyRecommendationsButton;
        
        // System references
        private BattleSimulationSystem simulationSystem;
        
        // UI State
        private BattleSimulationSystem.SimulationResult currentResult;
        private List<GameObject> unitStatEntries = new List<GameObject>();
        private List<GameObject> jobStatEntries = new List<GameObject>();
        private List<GameObject> battleLogEntries = new List<GameObject>();
        private Coroutine updateCoroutine;
        private float simulationStartTime;
        private string currentTab = "units";
        
        // Sorting options
        private enum UnitSortOption
        {
            KDRatio,
            TotalDamage,
            Kills,
            SurvivalRate,
            Name
        }
        
        private UnitSortOption currentSortOption = UnitSortOption.KDRatio;
        
        void Start()
        {
            simulationSystem = BattleSimulationSystem.Instance;
            
            if (simulationSystem == null)
            {
                Debug.LogError("BattleSimulationSystem not found!");
                enabled = false;
                return;
            }
            
            SetupUI();
            SubscribeToEvents();
            
            // 초기 UI 업데이트
            UpdateSimulationHistory();
            UpdateControlsState(false);
        }
        
        void SetupUI()
        {
            // 메인 버튼
            if (openButton != null)
                openButton.onClick.AddListener(() => ShowPanel(true));
            if (closeButton != null)
                closeButton.onClick.AddListener(() => ShowPanel(false));
            
            // 시뮬레이션 컨트롤
            if (startSimulationButton != null)
                startSimulationButton.onClick.AddListener(StartSimulation);
            if (stopSimulationButton != null)
                stopSimulationButton.onClick.AddListener(StopSimulation);
            
            // 시뮬레이션 타입 드롭다운
            if (simulationTypeDropdown != null)
            {
                simulationTypeDropdown.ClearOptions();
                var options = System.Enum.GetNames(typeof(BattleSimulationSystem.SimulationType)).ToList();
                simulationTypeDropdown.AddOptions(options);
            }
            
            // 속도 슬라이더
            if (simulationSpeedSlider != null)
            {
                simulationSpeedSlider.onValueChanged.AddListener(OnSpeedChanged);
                simulationSpeedSlider.value = 1f;
            }
            
            // 상세 결과 탭
            if (unitStatsButton != null)
                unitStatsButton.onClick.AddListener(() => ShowDetailTab("units"));
            if (jobStatsButton != null)
                jobStatsButton.onClick.AddListener(() => ShowDetailTab("jobs"));
            if (battleLogButton != null)
                battleLogButton.onClick.AddListener(() => ShowDetailTab("log"));
            
            // 정렬 옵션
            if (unitSortDropdown != null)
            {
                unitSortDropdown.ClearOptions();
                var sortOptions = System.Enum.GetNames(typeof(UnitSortOption)).ToList();
                unitSortDropdown.AddOptions(sortOptions);
                unitSortDropdown.onValueChanged.AddListener(OnSortOptionChanged);
            }
            
            // 기타 버튼
            if (viewDetailsButton != null)
                viewDetailsButton.onClick.AddListener(ShowDetailedResults);
            if (applyRecommendationsButton != null)
                applyRecommendationsButton.onClick.AddListener(ApplyRecommendations);
            
            // 토글
            if (showOnlyActiveToggle != null)
                showOnlyActiveToggle.onValueChanged.AddListener(_ => RefreshUnitStats());
            if (showOnlyKeyBattlesToggle != null)
                showOnlyKeyBattlesToggle.onValueChanged.AddListener(_ => RefreshBattleLog());
        }
        
        void SubscribeToEvents()
        {
            if (simulationSystem != null)
            {
                simulationSystem.OnSimulationComplete += OnSimulationComplete;
                simulationSystem.OnSimulationProgress += OnSimulationProgress;
                simulationSystem.OnBattleSimulated += OnBattleSimulated;
            }
        }
        
        void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            
            // 이벤트 구독 해제
            if (simulationSystem != null)
            {
                simulationSystem.OnSimulationComplete -= OnSimulationComplete;
                simulationSystem.OnSimulationProgress -= OnSimulationProgress;
                simulationSystem.OnBattleSimulated -= OnBattleSimulated;
            }
        }
        
        void ShowPanel(bool show)
        {
            if (simulationPanel != null)
            {
                simulationPanel.SetActive(show);
                
                if (show)
                {
                    UpdateSimulationHistory();
                }
            }
        }
        
        void StartSimulation()
        {
            if (simulationSystem.IsSimulating)
            {
                Debug.LogWarning("Simulation already in progress");
                return;
            }
            
            // Get simulation parameters
            var type = (BattleSimulationSystem.SimulationType)simulationTypeDropdown.value;
            int runs = 100;
            
            if (int.TryParse(simulationRunsInput.text, out int inputRuns) && inputRuns > 0)
            {
                runs = inputRuns;
            }
            
            // Start simulation
            simulationStartTime = Time.time;
            simulationSystem.StartSimulation(type, runs);
            
            // Update UI
            UpdateControlsState(true);
            ShowProgressPanel(true);
            
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            updateCoroutine = StartCoroutine(UpdateProgressCoroutine());
        }
        
        void StopSimulation()
        {
            // TODO: Implement stop simulation in BattleSimulationSystem
            Debug.Log("Stopping simulation...");
            
            UpdateControlsState(false);
            ShowProgressPanel(false);
            
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }
        }
        
        void UpdateControlsState(bool isSimulating)
        {
            if (startSimulationButton != null)
                startSimulationButton.interactable = !isSimulating;
            
            if (stopSimulationButton != null)
                stopSimulationButton.interactable = isSimulating;
            
            if (simulationTypeDropdown != null)
                simulationTypeDropdown.interactable = !isSimulating;
            
            if (simulationRunsInput != null)
                simulationRunsInput.interactable = !isSimulating;
        }
        
        void ShowProgressPanel(bool show)
        {
            if (progressPanel != null)
                progressPanel.SetActive(show);
            
            if (resultsPanel != null && show)
                resultsPanel.SetActive(false);
        }
        
        IEnumerator UpdateProgressCoroutine()
        {
            while (simulationSystem.IsSimulating)
            {
                float progress = simulationSystem.SimulationProgress;
                
                if (progressBar != null)
                    progressBar.value = progress;
                
                if (progressText != null)
                    progressText.text = $"{progress:P0}";
                
                // Update time remaining
                if (timeRemainingText != null)
                {
                    float elapsed = Time.time - simulationStartTime;
                    float estimated = progress > 0 ? elapsed / progress : 0;
                    float remaining = estimated - elapsed;
                    
                    if (remaining > 0)
                    {
                        int minutes = Mathf.FloorToInt(remaining / 60f);
                        int seconds = Mathf.FloorToInt(remaining % 60f);
                        timeRemainingText.text = $"남은 시간: {minutes:00}:{seconds:00}";
                    }
                    else
                    {
                        timeRemainingText.text = "계산 중...";
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        void OnSpeedChanged(float value)
        {
            if (speedText != null)
            {
                speedText.text = $"속도: {value:F1}x";
            }
            
            // TODO: Apply speed to simulation system
        }
        
        void OnSortOptionChanged(int index)
        {
            currentSortOption = (UnitSortOption)index;
            RefreshUnitStats();
        }
        
        void ShowDetailTab(string tab)
        {
            currentTab = tab;
            
            if (unitStatsTab != null)
                unitStatsTab.SetActive(tab == "units");
            if (jobStatsTab != null)
                jobStatsTab.SetActive(tab == "jobs");
            if (battleLogTab != null)
                battleLogTab.SetActive(tab == "log");
            
            // Update button highlights
            UpdateTabButtons();
            
            // Refresh content
            switch (tab)
            {
                case "units":
                    RefreshUnitStats();
                    break;
                case "jobs":
                    RefreshJobStats();
                    break;
                case "log":
                    RefreshBattleLog();
                    break;
            }
        }
        
        void UpdateTabButtons()
        {
            Color normalColor = new Color(0.8f, 0.8f, 0.8f);
            Color selectedColor = Color.white;
            
            if (unitStatsButton != null)
                unitStatsButton.image.color = currentTab == "units" ? selectedColor : normalColor;
            if (jobStatsButton != null)
                jobStatsButton.image.color = currentTab == "jobs" ? selectedColor : normalColor;
            if (battleLogButton != null)
                battleLogButton.image.color = currentTab == "log" ? selectedColor : normalColor;
        }
        
        void ShowDetailedResults()
        {
            if (detailsPanel != null)
            {
                detailsPanel.SetActive(true);
                ShowDetailTab("units");
            }
        }
        
        void RefreshUnitStats()
        {
            if (currentResult == null) return;
            
            // Clear existing entries
            foreach (var entry in unitStatEntries)
            {
                Destroy(entry);
            }
            unitStatEntries.Clear();
            
            // Sort units
            var sortedUnits = SortUnitStats(currentResult.unitStats.Values);
            
            // Filter if needed
            if (showOnlyActiveToggle != null && showOnlyActiveToggle.isOn)
            {
                sortedUnits = sortedUnits.Where(u => u.battlesParticipated > 0).ToList();
            }
            
            // Create entries
            foreach (var unitStat in sortedUnits)
            {
                CreateUnitStatEntry(unitStat);
            }
        }
        
        List<BattleSimulationSystem.BattleStatistics> SortUnitStats(IEnumerable<BattleSimulationSystem.BattleStatistics> stats)
        {
            return currentSortOption switch
            {
                UnitSortOption.KDRatio => stats.OrderByDescending(s => s.kdRatio).ToList(),
                UnitSortOption.TotalDamage => stats.OrderByDescending(s => s.totalDamageDealt).ToList(),
                UnitSortOption.Kills => stats.OrderByDescending(s => s.kills).ToList(),
                UnitSortOption.SurvivalRate => stats.OrderByDescending(s => s.averageSurvivalTime).ToList(),
                UnitSortOption.Name => stats.OrderBy(s => s.unitName).ToList(),
                _ => stats.ToList()
            };
        }
        
        void CreateUnitStatEntry(BattleSimulationSystem.BattleStatistics stats)
        {
            if (unitStatEntryPrefab == null || unitStatsContainer == null) return;
            
            var entryObj = Instantiate(unitStatEntryPrefab, unitStatsContainer);
            unitStatEntries.Add(entryObj);
            
            // Find UI elements
            var nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var kdText = entryObj.transform.Find("KDText")?.GetComponent<TextMeshProUGUI>();
            var damageText = entryObj.transform.Find("DamageText")?.GetComponent<TextMeshProUGUI>();
            var survivalText = entryObj.transform.Find("SurvivalText")?.GetComponent<TextMeshProUGUI>();
            var battlesText = entryObj.transform.Find("BattlesText")?.GetComponent<TextMeshProUGUI>();
            
            // Set values
            if (nameText != null)
                nameText.text = stats.unitName;
            
            if (kdText != null)
                kdText.text = $"K/D: {stats.kills}/{stats.deaths} ({stats.kdRatio:F2})";
            
            if (damageText != null)
            {
                float avgDamage = stats.battlesParticipated > 0 ? stats.totalDamageDealt / stats.battlesParticipated : 0;
                damageText.text = $"평균 피해: {avgDamage:F0}";
            }
            
            if (survivalText != null)
                survivalText.text = $"생존: {stats.averageSurvivalTime:F1}초";
            
            if (battlesText != null)
                battlesText.text = $"전투: {stats.battlesParticipated}";
        }
        
        void RefreshJobStats()
        {
            if (currentResult == null) return;
            
            // Clear existing entries
            foreach (var entry in jobStatEntries)
            {
                Destroy(entry);
            }
            jobStatEntries.Clear();
            
            // Create entries for each job class
            foreach (var kvp in currentResult.jobStats.OrderByDescending(j => j.Value.winRateContribution))
            {
                CreateJobStatEntry(kvp.Value);
            }
            
            // Update comparison chart
            UpdateJobComparisonChart();
        }
        
        void CreateJobStatEntry(BattleSimulationSystem.JobClassStatistics stats)
        {
            if (jobStatEntryPrefab == null || jobStatsContainer == null) return;
            
            var entryObj = Instantiate(jobStatEntryPrefab, jobStatsContainer);
            jobStatEntries.Add(entryObj);
            
            // Find UI elements
            var classText = entryObj.transform.Find("ClassText")?.GetComponent<TextMeshProUGUI>();
            var winRateText = entryObj.transform.Find("WinRateText")?.GetComponent<TextMeshProUGUI>();
            var kdText = entryObj.transform.Find("KDText")?.GetComponent<TextMeshProUGUI>();
            var damageText = entryObj.transform.Find("DamageText")?.GetComponent<TextMeshProUGUI>();
            var unitsText = entryObj.transform.Find("UnitsText")?.GetComponent<TextMeshProUGUI>();
            var progressBar = entryObj.transform.Find("ProgressBar")?.GetComponent<Slider>();
            
            // Set values
            if (classText != null)
                classText.text = GetJobClassName(stats.jobClass);
            
            if (winRateText != null)
                winRateText.text = $"승률 기여도: {stats.winRateContribution:P0}";
            
            if (kdText != null)
                kdText.text = $"평균 K/D: {stats.averageKD:F2}";
            
            if (damageText != null)
                damageText.text = $"평균 피해: {stats.averageDamage:F0}";
            
            if (unitsText != null)
                unitsText.text = $"유닛 수: {stats.totalUnits}";
            
            if (progressBar != null)
                progressBar.value = stats.winRateContribution;
        }
        
        string GetJobClassName(JobClass jobClass)
        {
            return jobClass switch
            {
                JobClass.Warrior => "전사",
                JobClass.Mage => "마법사",
                JobClass.Archer => "궁수",
                JobClass.Priest => "사제",
                JobClass.Knight => "기사",
                JobClass.Ranger => "레인저",
                _ => jobClass.ToString()
            };
        }
        
        void UpdateJobComparisonChart()
        {
            if (jobComparisonChart == null || currentResult == null) return;
            
            // Create bar chart comparing job classes
            // This would be implemented with actual chart visualization
            
            // For now, just log the comparison
            Debug.Log("Job Class Performance Comparison:");
            foreach (var kvp in currentResult.jobStats.OrderByDescending(j => j.Value.winRateContribution))
            {
                Debug.Log($"{kvp.Key}: {kvp.Value.winRateContribution:P0} win rate contribution");
            }
        }
        
        void RefreshBattleLog()
        {
            if (currentResult == null) return;
            
            // Clear existing entries
            foreach (var entry in battleLogEntries)
            {
                Destroy(entry);
            }
            battleLogEntries.Clear();
            
            // Filter battles if needed
            var battles = currentResult.battleResults;
            if (showOnlyKeyBattlesToggle != null && showOnlyKeyBattlesToggle.isOn)
            {
                // Show only close battles or special battles
                battles = battles.Where(b => 
                    (b.isVictory && b.roundsPlayed > 15) || 
                    (!b.isVictory) ||
                    b.keyMoments.Count > 5
                ).ToList();
            }
            
            // Create entries
            foreach (var battle in battles.TakeLast(50)) // Show last 50 battles
            {
                CreateBattleLogEntry(battle);
            }
            
            // Scroll to bottom
            if (battleLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                battleLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        void CreateBattleLogEntry(BattleSimulationSystem.BattleResult battle)
        {
            if (battleLogEntryPrefab == null || battleLogContainer == null) return;
            
            var entryObj = Instantiate(battleLogEntryPrefab, battleLogContainer);
            battleLogEntries.Add(entryObj);
            
            // Find UI elements
            var resultIcon = entryObj.transform.Find("ResultIcon")?.GetComponent<Image>();
            var battleIdText = entryObj.transform.Find("BattleIdText")?.GetComponent<TextMeshProUGUI>();
            var typeText = entryObj.transform.Find("TypeText")?.GetComponent<TextMeshProUGUI>();
            var durationText = entryObj.transform.Find("DurationText")?.GetComponent<TextMeshProUGUI>();
            var detailsButton = entryObj.transform.Find("DetailsButton")?.GetComponent<Button>();
            
            // Set values
            if (resultIcon != null)
            {
                resultIcon.color = battle.isVictory ? Color.green : Color.red;
            }
            
            if (battleIdText != null)
                battleIdText.text = $"Battle #{battle.battleId.Substring(0, 8)}";
            
            if (typeText != null)
                typeText.text = $"{battle.type} - {(battle.isVictory ? "승리" : "패배")}";
            
            if (durationText != null)
                durationText.text = $"{battle.duration:F1}초 ({battle.roundsPlayed}라운드)";
            
            if (detailsButton != null)
            {
                detailsButton.onClick.AddListener(() => ShowBattleDetails(battle));
            }
        }
        
        void ShowBattleDetails(BattleSimulationSystem.BattleResult battle)
        {
            Debug.Log($"Battle Details: {battle.battleId}");
            Debug.Log($"Type: {battle.type}, Victory: {battle.isVictory}");
            Debug.Log($"Duration: {battle.duration:F1}s, Rounds: {battle.roundsPlayed}");
            
            Debug.Log("Key Moments:");
            foreach (var moment in battle.keyMoments)
            {
                Debug.Log($"  {moment}");
            }
            
            // TODO: Show detailed battle view in UI
        }
        
        void UpdateSimulationHistory()
        {
            var history = simulationSystem.GetSimulationHistory(5);
            
            if (history.Count > 0)
            {
                // Update win rate trend
                UpdateWinRateTrend(history);
                
                // Show overall statistics
                UpdateOverallStats();
            }
        }
        
        void UpdateWinRateTrend(List<BattleSimulationSystem.SimulationResult> history)
        {
            if (winRateTrend == null) return;
            
            winRateTrend.positionCount = history.Count;
            
            for (int i = 0; i < history.Count; i++)
            {
                float x = (float)i / (history.Count - 1);
                float y = history[i].winRate;
                winRateTrend.SetPosition(i, new Vector3(x, y, 0));
            }
        }
        
        void UpdateOverallStats()
        {
            float overallWinRate = simulationSystem.GetOverallWinRate();
            
            if (winRateChartFill != null)
            {
                winRateChartFill.fillAmount = overallWinRate;
                winRateChartFill.color = Color.Lerp(Color.red, Color.green, overallWinRate);
            }
        }
        
        void GenerateRecommendations()
        {
            if (currentResult == null || recommendationsPanel == null) return;
            
            recommendationsPanel.SetActive(true);
            
            // Squad recommendations
            if (squadRecommendationText != null)
            {
                var bestJobCombo = currentResult.jobStats
                    .OrderByDescending(j => j.Value.winRateContribution)
                    .Take(3)
                    .Select(j => GetJobClassName(j.Key));
                
                squadRecommendationText.text = $"추천 조합: {string.Join(", ", bestJobCombo)} 중심의 부대 구성";
            }
            
            // Unit recommendations
            if (unitRecommendationText != null)
            {
                var topPerformers = currentResult.unitStats.Values
                    .OrderByDescending(u => u.kdRatio)
                    .Take(5)
                    .Select(u => u.unitName);
                
                unitRecommendationText.text = $"주력 유닛: {string.Join(", ", topPerformers)}";
            }
            
            // Strategy recommendations
            if (strategyRecommendationText != null)
            {
                string strategy = currentResult.winRate > 0.7f ? "현재 전략을 유지하세요" :
                                 currentResult.winRate > 0.5f ? "일부 조정이 필요합니다" :
                                 "전략을 재검토해야 합니다";
                
                strategyRecommendationText.text = $"전략 제안: {strategy}";
            }
        }
        
        void ApplyRecommendations()
        {
            Debug.Log("Applying recommendations...");
            // TODO: Actually apply the recommendations to squad compositions
        }
        
        // Event handlers
        void OnSimulationComplete(BattleSimulationSystem.SimulationResult result)
        {
            currentResult = result;
            
            // Stop update coroutine
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }
            
            // Update UI
            UpdateControlsState(false);
            ShowProgressPanel(false);
            ShowResultsPanel(true);
            
            // Generate recommendations
            GenerateRecommendations();
        }
        
        void ShowResultsPanel(bool show)
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(show);
                
                if (show && currentResult != null)
                {
                    // Update result texts
                    if (totalBattlesText != null)
                        totalBattlesText.text = $"총 전투: {currentResult.totalBattles}";
                    
                    if (victoriesText != null)
                        victoriesText.text = $"승리: {currentResult.victories}";
                    
                    if (defeatsText != null)
                        defeatsText.text = $"패배: {currentResult.defeats}";
                    
                    if (finalWinRateText != null)
                    {
                        finalWinRateText.text = $"승률: {currentResult.winRate:P0}";
                        finalWinRateText.color = Color.Lerp(Color.red, Color.green, currentResult.winRate);
                    }
                    
                    if (simulationTimeText != null)
                    {
                        var duration = currentResult.endTime - currentResult.startTime;
                        simulationTimeText.text = $"소요 시간: {duration.TotalSeconds:F1}초";
                    }
                }
            }
        }
        
        void OnSimulationProgress(float progress)
        {
            // Progress is updated in the coroutine
        }
        
        void OnBattleSimulated(BattleSimulationSystem.BattleResult battle)
        {
            if (currentBattleText != null)
            {
                currentBattleText.text = $"현재 전투: {battle.type} - {(battle.isVictory ? "승리" : "패배")}";
            }
            
            if (winRateText != null && currentResult != null)
            {
                float currentWinRate = currentResult.totalBattles > 0 ? 
                    (float)currentResult.victories / currentResult.totalBattles : 0f;
                winRateText.text = $"현재 승률: {currentWinRate:P0}";
            }
        }
    }
}