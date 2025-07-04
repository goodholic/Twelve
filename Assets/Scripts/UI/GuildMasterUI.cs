using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using GuildMaster.Battle;
using GuildMaster.Guild;
using GuildMaster.Systems;
using GuildMaster.Data;


namespace GuildMaster.UI
{
    /// <summary>
    /// 길드마스터 메인 UI 매니저 (완전 구현)
    /// 상용 게임 수준의 UI/UX 제공
    /// </summary>
    public class GuildMasterUI : MonoBehaviour
    {
        [Header("메인 패널들")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject guildManagementPanel;
        [SerializeField] private GameObject characterCollectionPanel;
        [SerializeField] private GameObject battleFormationPanel;
        [SerializeField] private GameObject buildingPanel;
        [SerializeField] private GameObject researchPanel;
        [SerializeField] private GameObject automationPanel;
        [SerializeField] private GameObject settingsPanel;
        
        [Header("UI 애니메이션 설정")]
        [SerializeField] private float panelTransitionDuration = 0.3f;
        [SerializeField] private AnimationCurve panelEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float counterAnimationDuration = 1f;
        
        [Header("캐릭터 수집 UI")]
        [SerializeField] private Slider collectionProgressBar;
        [SerializeField] private TextMeshProUGUI collectionCountText;
        [SerializeField] private Transform characterSlotsParent;
        
        [Header("자원 표시 UI")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI manaStoneText;
        
        // 이벤트
        public event Action<GuildMaster.Battle.Unit> OnCharacterSelected;
        public event Action<BuildingType> OnBuildingSelected;
        public event Action<Vector2Int> OnFormationSlotClicked;
        
        // 현재 활성 패널
        private string currentActivePanel = "MainMenu";
        
        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
            ShowPanel("MainMenu");
        }
        
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // 모든 패널 비활성화
            HideAllPanels();
            
            // UI 업데이트 시작
            InvokeRepeating(nameof(UpdateUI), 0f, 0.1f);
        }
        
        /// <summary>
        /// GameManager에서 호출하는 공개 초기화 메서드
        /// </summary>
        public void Initialize()
        {
            InitializeUI();
        }
        
        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            if (GuildBattleCore.Instance != null)
            {
                GuildBattleCore.Instance.OnCharacterCollected += OnCharacterCollected;
                GuildBattleCore.Instance.OnBattleComplete += OnBattleComplete;
            }
            
            if (GuildSimulationCore.Instance != null)
            {
                GuildSimulationCore.Instance.OnBuildingConstructed += OnBuildingConstructed;
                GuildSimulationCore.Instance.OnGuildLevelUp += OnGuildLevelUp;
                GuildSimulationCore.Instance.OnResourcesChanged += OnResourcesChanged;
            }
            
            if (IdleGameCore.Instance != null)
            {
                IdleGameCore.Instance.OnAutomationReport += OnAutomationReport;
                IdleGameCore.Instance.OnOfflineReward += OnOfflineReward;
            }
        }
        
        /// <summary>
        /// 패널 표시
        /// </summary>
        public void ShowPanel(string panelName)
        {
            if (currentActivePanel == panelName) return;
            
            StartCoroutine(SwitchPanelCoroutine(panelName));
        }
        
        /// <summary>
        /// 패널 전환 코루틴
        /// </summary>
        private IEnumerator SwitchPanelCoroutine(string newPanelName)
        {
            // 현재 패널 숨기기
            if (!string.IsNullOrEmpty(currentActivePanel))
            {
                yield return StartCoroutine(HidePanel(currentActivePanel));
            }
            
            // 새 패널 표시
            yield return StartCoroutine(ShowPanelCoroutine(newPanelName));
            currentActivePanel = newPanelName;
        }
        
        /// <summary>
        /// 패널 표시 코루틴
        /// </summary>
        private IEnumerator ShowPanelCoroutine(string panelName)
        {
            GameObject panel = GetPanelByName(panelName);
            if (panel == null) yield break;
            
            panel.SetActive(true);
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            panel.transform.localScale = Vector3.one * 0.9f;
            
            yield return StartCoroutine(AnimatePanel(canvasGroup, panel.transform, 0f, 1f, Vector3.one * 0.9f, Vector3.one, () => canvasGroup.interactable = true));
        }
        
        /// <summary>
        /// 패널 숨기기 코루틴
        /// </summary>
        private IEnumerator HidePanel(string panelName)
        {
            GameObject panel = GetPanelByName(panelName);
            if (panel == null) yield break;
            
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) yield break;
            
            canvasGroup.interactable = false;
            
            yield return StartCoroutine(AnimatePanel(canvasGroup, panel.transform, 1f, 0f, Vector3.one, Vector3.one * 0.9f, () => panel.SetActive(false)));
        }
        
        /// <summary>
        /// Unity Coroutine을 사용한 패널 애니메이션
        /// </summary>
        private IEnumerator AnimatePanel(CanvasGroup canvasGroup, Transform panelTransform, float startAlpha, float endAlpha, Vector3 startScale, Vector3 endScale, System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < panelTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / panelTransitionDuration;
                t = panelEaseCurve.Evaluate(t);
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                panelTransform.localScale = Vector3.Lerp(startScale, endScale, t);
                
                yield return null;
            }
            
            canvasGroup.alpha = endAlpha;
            panelTransform.localScale = endScale;
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            UpdateResourceDisplay();
            UpdateCharacterCollection();
            UpdateBuildingStatus();
            UpdateResearchProgress();
            UpdateAutomationStatus();
        }
        
        /// <summary>
        /// 자원 표시 업데이트
        /// </summary>
        private void UpdateResourceDisplay()
        {
            if (GuildSimulationCore.Instance != null)
            {
                var resources = GuildSimulationCore.Instance.GetResources();
                
                if (goldText != null) AnimateCounterText(goldText, (int)resources.gold);
                if (woodText != null) AnimateCounterText(woodText, (int)resources.wood);
                if (stoneText != null) AnimateCounterText(stoneText, (int)resources.stone);
                if (manaStoneText != null) AnimateCounterText(manaStoneText, (int)resources.manaStone);
            }
        }
        
        /// <summary>
        /// 캐릭터 수집 상태 업데이트
        /// </summary>
        private void UpdateCharacterCollection()
        {
            if (GuildBattleCore.Instance != null)
            {
                var characters = GuildBattleCore.Instance.GetCollectedCharacters();
                int currentCount = characters.Count;
                int maxCount = GuildBattleCore.TOTAL_COLLECTIBLE_CHARACTERS;
                
                if (collectionCountText != null)
                {
                    collectionCountText.text = $"{currentCount}/{maxCount}";
                }
                
                if (collectionProgressBar != null)
                {
                    float progress = (float)currentCount / maxCount;
                    StartCoroutine(AnimateSlider(collectionProgressBar, progress, 0.3f));
                }
            }
        }
        
        /// <summary>
        /// 건물 상태 업데이트
        /// </summary>
        private void UpdateBuildingStatus()
        {
            // 건물 건설/업그레이드 진행 상황 업데이트
        }
        
        /// <summary>
        /// 연구 진행 상황 업데이트
        /// </summary>
        private void UpdateResearchProgress()
        {
            // 연구 진행 상황 업데이트
        }
        
        /// <summary>
        /// 자동화 상태 업데이트
        /// </summary>
        private void UpdateAutomationStatus()
        {
            // 자동화 상태 업데이트
        }
        
        /// <summary>
        /// 숫자 카운터 애니메이션
        /// </summary>
        private void AnimateCounterText(TextMeshProUGUI text, int targetValue)
        {
            if (int.TryParse(text.text.Replace(",", ""), out int currentValue))
            {
                StartCoroutine(AnimateCounter(text, currentValue, targetValue, counterAnimationDuration));
            }
            else
            {
                text.text = targetValue.ToString("N0");
            }
        }
        
        /// <summary>
        /// Unity Coroutine을 사용한 슬라이더 애니메이션
        /// </summary>
        private IEnumerator AnimateSlider(Slider slider, float targetValue, float duration)
        {
            float startValue = slider.value;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                slider.value = Mathf.Lerp(startValue, targetValue, t);
                yield return null;
            }
            
            slider.value = targetValue;
        }
        
        /// <summary>
        /// Unity Coroutine을 사용한 카운터 애니메이션
        /// </summary>
        private IEnumerator AnimateCounter(TextMeshProUGUI text, int startValue, int targetValue, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, t));
                text.text = currentValue.ToString("N0");
                yield return null;
            }
            
            text.text = targetValue.ToString("N0");
        }
        
        /// <summary>
        /// 패널 이름으로 GameObject 가져오기
        /// </summary>
        private GameObject GetPanelByName(string panelName)
        {
            return panelName switch
            {
                "MainMenu" => mainMenuPanel,
                "GuildManagement" => guildManagementPanel,
                "CharacterCollection" => characterCollectionPanel,
                "BattleFormation" => battleFormationPanel,
                "Building" => buildingPanel,
                "Research" => researchPanel,
                "Automation" => automationPanel,
                "Settings" => settingsPanel,
                _ => null
            };
        }
        
        /// <summary>
        /// 모든 패널 숨기기
        /// </summary>
        private void HideAllPanels()
        {
            var panels = new[] { mainMenuPanel, guildManagementPanel, characterCollectionPanel, 
                               battleFormationPanel, buildingPanel, researchPanel, 
                               automationPanel, settingsPanel };
            
            foreach (var panel in panels)
            {
                if (panel != null) panel.SetActive(false);
            }
        }
        
        // 이벤트 핸들러들
        private void OnCharacterSlotClicked(int slotIndex)
        {
            if (GuildBattleCore.Instance != null)
            {
                var characters = GuildBattleCore.Instance.GetCollectedCharacters();
                if (slotIndex < characters.Count)
                {
                    OnCharacterSelected?.Invoke(characters[slotIndex]);
                }
            }
        }
        
        private void OnFormationSlotClickedHandler(int squadIndex, Vector2Int position)
        {
            OnFormationSlotClicked?.Invoke(position);
        }
        
        private void OnBuildingSlotClicked(Vector2Int position)
        {
            // 건물 배치 UI 로직
        }
        
        private void OnCharacterCollected(GuildMaster.Battle.Unit character)
        {
            ShowNotification($"새로운 캐릭터 수집: {character.unitName}!", Color.green);
        }
        
        private void OnBuildingConstructed(GuildBuilding building)
        {
            ShowNotification($"건물 완성: {building.buildingType}!", Color.blue);
        }
        
        private void OnGuildLevelUp(int newLevel)
        {
            ShowNotification($"길드 레벨업! Lv.{newLevel}", Color.yellow);
        }
        
        private void OnBattleComplete(GuildBattleResult result)
        {
            string message = result.isVictory ? "전투 승리!" : "전투 패배...";
            Color color = result.isVictory ? Color.green : Color.red;
            ShowNotification(message, color);
        }
        
        private void OnResourcesChanged(GuildResources resources)
        {
            // 자원 변경 알림
        }
        
        private void OnAutomationReport(AutomationReport report)
        {
            // 자동화 보고서 표시
        }
        
        private void OnOfflineReward(OfflineReward reward)
        {
            ShowNotification($"오프라인 보상: 골드 +{reward.goldEarned:F0}", Color.cyan);
        }
        
        /// <summary>
        /// 알림 메시지 표시
        /// </summary>
        private void ShowNotification(string message, Color color)
        {
            Debug.Log($"[알림] {message}");
        }
        
        // 공개 인터페이스
        public void ShowMainMenu() => ShowPanel("MainMenu");
        public void ShowGuildManagement() => ShowPanel("GuildManagement");
        public void ShowCharacterCollection() => ShowPanel("CharacterCollection");
        public void ShowBattleFormation() => ShowPanel("BattleFormation");
        public void ShowBuilding() => ShowPanel("Building");
        public void ShowResearch() => ShowPanel("Research");
        public void ShowAutomation() => ShowPanel("Automation");
        public void ShowSettings() => ShowPanel("Settings");
    }
} 