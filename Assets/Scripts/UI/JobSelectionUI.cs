using UnityEngine;
using UnityEngine.UI;
using GuildMaster.Systems;
using GuildMaster.Data;
using GuildMaster.Battle;
using System.Collections.Generic;
using TMPro;
using System;
using System.Collections;

namespace GuildMaster.UI
{
    /// <summary>
    /// 직업 선택 및 자동 레벨업 UI 관리
    /// </summary>
    public class JobSelectionUI : MonoBehaviour
    {
        [Header("Main UI")]
        [SerializeField] private Button mainButton;
        [SerializeField] private GameObject jobSelectionPanel;
        [SerializeField] private Transform jobButtonContainer;
        [SerializeField] private GameObject jobButtonPrefab;
        
        [Header("Info Display")]
        [SerializeField] private TextMeshProUGUI totalGoldText;
        [SerializeField] private TextMeshProUGUI activeJobsText;
        
        [Header("Animation")]
        [SerializeField] private float animationDuration = 0.3f;
        
        private Dictionary<JobClass, JobButtonUI> jobButtons = new Dictionary<JobClass, JobButtonUI>();
        private bool isPanelOpen = false;
        
        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void InitializeUI()
        {
            // 메인 버튼 클릭 이벤트
            if (mainButton != null)
            {
                mainButton.onClick.AddListener(ToggleJobPanel);
            }
            
            // 직업 선택 패널 초기 상태
            if (jobSelectionPanel != null)
            {
                jobSelectionPanel.SetActive(false);
            }
            
            // 직업 버튼들 생성
            CreateJobButtons();
            
            // 초기 텍스트 업데이트
            UpdateInfoDisplay();
        }
        
        private void CreateJobButtons()
        {
            if (jobButtonPrefab == null || jobButtonContainer == null) return;
            
            // 모든 직업에 대해 버튼 생성
            JobClass[] allJobs = (JobClass[])Enum.GetValues(typeof(JobClass));
            
            foreach (JobClass jobClass in allJobs)
            {
                GameObject buttonObj = Instantiate(jobButtonPrefab, jobButtonContainer);
                JobButtonUI jobButton = buttonObj.GetComponent<JobButtonUI>();
                
                if (jobButton == null)
                {
                    jobButton = buttonObj.AddComponent<JobButtonUI>();
                }
                
                jobButton.Initialize(jobClass);
                jobButtons[jobClass] = jobButton;
            }
        }
        
        private void SubscribeToEvents()
        {
            AutoLevelUpManager.OnJobLevelUp += OnJobLevelUp;
            AutoLevelUpManager.OnGoldAccumulated += OnGoldAccumulated;
            AutoLevelUpManager.OnAutoLevelUpToggled += OnAutoLevelUpToggled;
        }
        
        private void UnsubscribeFromEvents()
        {
            AutoLevelUpManager.OnJobLevelUp -= OnJobLevelUp;
            AutoLevelUpManager.OnGoldAccumulated -= OnGoldAccumulated;
            AutoLevelUpManager.OnAutoLevelUpToggled -= OnAutoLevelUpToggled;
        }
        
        private void ToggleJobPanel()
        {
            isPanelOpen = !isPanelOpen;
            
            if (jobSelectionPanel != null)
            {
                if (isPanelOpen)
                {
                    jobSelectionPanel.SetActive(true);
                    AnimatePanel(true);
                }
                else
                {
                    AnimatePanel(false);
                }
            }
        }
        
        private void AnimatePanel(bool open)
        {
            if (jobSelectionPanel == null) return;
            
            CanvasGroup canvasGroup = jobSelectionPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = jobSelectionPanel.AddComponent<CanvasGroup>();
            }
            
            if (open)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                
                StartCoroutine(FadeAlpha(canvasGroup, 0f, 1f, animationDuration, () => {
                    canvasGroup.interactable = true;
                }));
            }
            else
            {
                canvasGroup.interactable = false;
                StartCoroutine(FadeAlpha(canvasGroup, 1f, 0f, animationDuration, () => {
                    jobSelectionPanel.SetActive(false);
                }));
            }
        }
        
        private System.Collections.IEnumerator FadeAlpha(CanvasGroup canvasGroup, float from, float to, float duration, System.Action onComplete = null)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                canvasGroup.alpha = Mathf.Lerp(from, to, progress);
                yield return null;
            }
            canvasGroup.alpha = to;
            onComplete?.Invoke();
        }
        
        private void UpdateInfoDisplay()
        {
            if (AutoLevelUpManager.Instance == null) return;
            
            // 총 골드 표시
            if (totalGoldText != null)
            {
                float totalGold = AutoLevelUpManager.Instance.GetTotalGold();
                totalGoldText.text = $"Total Gold: {totalGold:F2}";
            }
            
            // 활성화된 직업 수 표시
            if (activeJobsText != null)
            {
                int activeCount = 0;
                foreach (var button in jobButtons.Values)
                {
                    if (button.IsAutoLevelUpEnabled)
                    {
                        activeCount++;
                    }
                }
                activeJobsText.text = $"Active Jobs: {activeCount}/7";
            }
        }
        
        private void OnJobLevelUp(JobClass jobClass, int newLevel)
        {
            if (jobButtons.ContainsKey(jobClass))
            {
                jobButtons[jobClass].UpdateLevel(newLevel);
            }
            UpdateInfoDisplay();
        }
        
        private void OnGoldAccumulated(JobClass jobClass, float gold)
        {
            if (jobButtons.ContainsKey(jobClass))
            {
                jobButtons[jobClass].UpdateProgress(gold);
            }
            UpdateInfoDisplay();
        }
        
        private void OnAutoLevelUpToggled(JobClass jobClass, bool enabled)
        {
            if (jobButtons.ContainsKey(jobClass))
            {
                jobButtons[jobClass].UpdateToggleState(enabled);
            }
            UpdateInfoDisplay();
        }
        
        private void Update()
        {
            // ESC 키로 패널 닫기
            if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleJobPanel();
            }
        }
    }
    
    /// <summary>
    /// 개별 직업 버튼 UI
    /// </summary>
    public class JobButtonUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI jobNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image jobIcon;
        [SerializeField] private GameObject activeIndicator;
        [SerializeField] private Toggle autoLevelUpToggle;
        
        private JobClass jobClass;
        private bool isAutoLevelUpEnabled = false;
        
        public bool IsAutoLevelUpEnabled => isAutoLevelUpEnabled;
        
        public void Initialize(JobClass job)
        {
            jobClass = job;
            
            // UI 요소 초기화
            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }
            
            if (autoLevelUpToggle != null)
            {
                autoLevelUpToggle.onValueChanged.AddListener(OnToggleChanged);
            }
            
            // 직업 이름 설정
            if (jobNameText != null)
            {
                jobNameText.text = GetJobDisplayName(jobClass);
            }
            
            // 초기 상태 업데이트
            UpdateDisplay();
        }
        
        private void OnButtonClick()
        {
            if (AutoLevelUpManager.Instance != null)
            {
                AutoLevelUpManager.Instance.ToggleAutoLevelUp(jobClass);
            }
        }
        
        private void OnToggleChanged(bool value)
        {
            if (AutoLevelUpManager.Instance != null)
            {
                AutoLevelUpManager.Instance.SetAutoLevelUp(jobClass, value);
            }
        }
        
        public void UpdateLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"Lv.{level}";
            }
        }
        
        public void UpdateProgress(float accumulatedGold)
        {
            if (AutoLevelUpManager.Instance == null) return;
            
            float nextCost = AutoLevelUpManager.Instance.GetNextLevelUpCost(jobClass);
            
            if (progressSlider != null && nextCost > 0)
            {
                progressSlider.value = accumulatedGold / nextCost;
            }
            
            if (costText != null)
            {
                costText.text = $"{accumulatedGold:F1}/{nextCost:F1}";
            }
        }
        
        public void UpdateToggleState(bool enabled)
        {
            isAutoLevelUpEnabled = enabled;
            
            if (autoLevelUpToggle != null && autoLevelUpToggle.isOn != enabled)
            {
                autoLevelUpToggle.SetIsOnWithoutNotify(enabled);
            }
            
            if (activeIndicator != null)
            {
                activeIndicator.SetActive(enabled);
            }
            
            // 버튼 외관 변경
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = enabled ? Color.green : Color.white;
                button.colors = colors;
            }
        }
        
        private void UpdateDisplay()
        {
            if (AutoLevelUpManager.Instance == null) return;
            
            int level = AutoLevelUpManager.Instance.GetJobLevel(jobClass);
            float gold = AutoLevelUpManager.Instance.GetAccumulatedGold(jobClass);
            
            UpdateLevel(level);
            UpdateProgress(gold);
        }
        
        private string GetJobDisplayName(JobClass job)
        {
            switch (job)
            {
                case JobClass.Warrior: return "전사";
                case JobClass.Knight: return "성기사";
                case JobClass.Mage: return "마법사";
                case JobClass.Priest: return "사제";
                case JobClass.Rogue: return "도적";
                case JobClass.Archer: return "궁수";
                case JobClass.Sage: return "현자";
                default: return job.ToString();
            }
        }
    }
}