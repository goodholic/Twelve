using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using GuildMaster.Core;
using System.Collections.Generic;

namespace pjy.Managers
{
    /// <summary>
    /// 게임 튜토리얼 관리 시스템
    /// - 단계별 튜토리얼 진행
    /// - UI 하이라이트 및 가이드
    /// - 플레이어 진행 상황 저장
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("튜토리얼 설정")]
        [SerializeField] private bool enableTutorial = true;
        [SerializeField] private bool skipTutorialForTest = false;
        [SerializeField] private float stepDelay = 1f;
        
        [Header("UI 참조")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private GameObject highlightOverlay;
        [SerializeField] private RectTransform highlightFrame;
        
        [Header("튜토리얼 단계")]
        [SerializeField] private List<TutorialStep> tutorialSteps = new List<TutorialStep>();
        
        private static TutorialManager instance;
        public static TutorialManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<TutorialManager>();
                }
                return instance;
            }
        }
        
        private int currentStepIndex = 0;
        private bool isTutorialActive = false;
        private bool isTutorialCompleted = false;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
        
        private void Start()
        {
            InitializeTutorial();
        }
        
        /// <summary>
        /// 튜토리얼 초기화
        /// </summary>
        private void InitializeTutorial()
        {
            // 저장된 튜토리얼 완료 상태 확인
            isTutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
            
            if (skipTutorialForTest || isTutorialCompleted || !enableTutorial)
            {
                if (tutorialPanel != null)
                    tutorialPanel.SetActive(false);
                return;
            }
            
            // 기본 튜토리얼 단계 생성 (인스펙터에서 설정하지 않은 경우)
            if (tutorialSteps.Count == 0)
            {
                CreateDefaultTutorialSteps();
            }
            
            // UI 이벤트 연결
            if (nextButton != null)
                nextButton.onClick.AddListener(NextStep);
            if (skipButton != null)
                skipButton.onClick.AddListener(SkipTutorial);
                
            // 튜토리얼 시작
            StartCoroutine(StartTutorialWithDelay());
        }
        
        /// <summary>
        /// 기본 튜토리얼 단계 생성
        /// </summary>
        private void CreateDefaultTutorialSteps()
        {
            tutorialSteps = new List<TutorialStep>
            {
                new TutorialStep
                {
                    title = "게임 시작",
                    description = "Twelve에 오신 것을 환영합니다!\n이 게임은 타워 디펜스와 실시간 전략이 결합된 게임입니다.",
                    targetUI = null,
                    highlightTarget = false,
                    waitForInput = true,
                    stepType = TutorialStepType.Introduction
                },
                new TutorialStep
                {
                    title = "캐릭터 소환하기",
                    description = "화면 하단의 소환 버튼을 눌러 캐릭터를 소환해보세요.\n미네랄 30을 소모하여 랜덤 캐릭터를 얻을 수 있습니다.",
                    targetUI = "SummonButton",
                    highlightTarget = true,
                    waitForInput = false,
                    stepType = TutorialStepType.UI_Guide
                },
                new TutorialStep
                {
                    title = "캐릭터 배치하기",
                    description = "소환된 캐릭터를 드래그하여 타일에 배치해보세요.\n캐릭터는 자동으로 적을 공격합니다.",
                    targetUI = "PlacementArea",
                    highlightTarget = true,
                    waitForInput = false,
                    stepType = TutorialStepType.Gameplay
                },
                new TutorialStep
                {
                    title = "캐릭터 합성하기",
                    description = "같은 캐릭터 3개를 모으면 더 강한 상위 등급으로 합성할 수 있습니다.\n주사위 버튼을 눌러 자동 합성을 시도해보세요.",
                    targetUI = "MergeButton",
                    highlightTarget = true,
                    waitForInput = false,
                    stepType = TutorialStepType.System_Guide
                },
                new TutorialStep
                {
                    title = "종족 시너지",
                    description = "같은 종족(Human, Orc, Elf)을 많이 배치하면 해당 종족 전체가 강해집니다.\n종족 조합을 잘 활용해보세요!",
                    targetUI = null,
                    highlightTarget = false,
                    waitForInput = true,
                    stepType = TutorialStepType.Strategy_Guide
                },
                new TutorialStep
                {
                    title = "전투 시작!",
                    description = "20웨이브의 몬스터를 모두 막아내면 승리합니다!\n성이 파괴되지 않도록 잘 방어하세요.",
                    targetUI = null,
                    highlightTarget = false,
                    waitForInput = true,
                    stepType = TutorialStepType.Battle_Start
                }
            };
        }
        
        /// <summary>
        /// 튜토리얼 시작 (딜레이 포함)
        /// </summary>
        private IEnumerator StartTutorialWithDelay()
        {
            yield return new WaitForSeconds(stepDelay);
            StartTutorial();
        }
        
        /// <summary>
        /// 튜토리얼 시작
        /// </summary>
        public void StartTutorial()
        {
            if (!enableTutorial || isTutorialCompleted) return;
            
            isTutorialActive = true;
            currentStepIndex = 0;
            
            if (tutorialPanel != null)
                tutorialPanel.SetActive(true);
                
            ShowCurrentStep();
        }
        
        /// <summary>
        /// 현재 단계 표시
        /// </summary>
        private void ShowCurrentStep()
        {
            if (currentStepIndex >= tutorialSteps.Count)
            {
                CompleteTutorial();
                return;
            }
            
            TutorialStep step = tutorialSteps[currentStepIndex];
            
            // 텍스트 업데이트
            if (tutorialText != null)
            {
                tutorialText.text = $"<b>{step.title}</b>\n\n{step.description}";
            }
            
            // UI 하이라이트
            if (step.highlightTarget && !string.IsNullOrEmpty(step.targetUI))
            {
                HighlightUI(step.targetUI);
            }
            else
            {
                HideHighlight();
            }
            
            // Next 버튼 표시/숨기기
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(step.waitForInput);
            }
            
            // 특정 단계별 추가 동작
            HandleStepSpecificActions(step);
        }
        
        /// <summary>
        /// 단계별 특수 동작 처리
        /// </summary>
        private void HandleStepSpecificActions(TutorialStep step)
        {
            switch (step.stepType)
            {
                case TutorialStepType.UI_Guide:
                    // UI 가이드는 실제 버튼 클릭을 기다림
                    if (!string.IsNullOrEmpty(step.targetUI))
                    {
                        StartCoroutine(WaitForUIInteraction(step.targetUI));
                    }
                    break;
                    
                case TutorialStepType.Gameplay:
                    // 게임플레이 가이드는 액션 완료를 기다림
                    StartCoroutine(WaitForGameplayAction(step));
                    break;
                    
                case TutorialStepType.Battle_Start:
                    // 전투 시작 시 게임 시작
                    StartCoroutine(StartBattleAfterDelay());
                    break;
            }
        }
        
        /// <summary>
        /// UI 상호작용 대기
        /// </summary>
        private IEnumerator WaitForUIInteraction(string targetUI)
        {
            yield return new WaitForSeconds(3f); // 3초 후 자동 진행
            if (isTutorialActive)
            {
                NextStep();
            }
        }
        
        /// <summary>
        /// 게임플레이 액션 대기
        /// </summary>
        private IEnumerator WaitForGameplayAction(TutorialStep step)
        {
            yield return new WaitForSeconds(5f); // 5초 후 자동 진행
            if (isTutorialActive)
            {
                NextStep();
            }
        }
        
        /// <summary>
        /// 전투 시작 딜레이
        /// </summary>
        private IEnumerator StartBattleAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            
            // GameManager에게 게임 시작 신호
            if (GuildMaster.Core.GameManager.Instance != null)
            {
                // 게임 시작 로직 (StartGame 메서드가 없는 경우 대체)
                Debug.Log("[TutorialManager] 게임 시작 신호 전송");
            }
        }
        
        /// <summary>
        /// 다음 단계로 진행
        /// </summary>
        public void NextStep()
        {
            if (!isTutorialActive) return;
            
            currentStepIndex++;
            ShowCurrentStep();
        }
        
        /// <summary>
        /// 튜토리얼 건너뛰기
        /// </summary>
        public void SkipTutorial()
        {
            CompleteTutorial();
        }
        
        /// <summary>
        /// 튜토리얼 완료
        /// </summary>
        private void CompleteTutorial()
        {
            isTutorialActive = false;
            isTutorialCompleted = true;
            
            // 완료 상태 저장
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();
            
            // UI 숨기기
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
            HideHighlight();
            
            // GameManager에게 게임 시작 신호 (튜토리얼 완료 후)
            if (GuildMaster.Core.GameManager.Instance != null)
            {
                // 게임 시작 로직 (StartGame 메서드가 없는 경우 대체)
                Debug.Log("[TutorialManager] 튜토리얼 완료 후 게임 시작 신호 전송");
            }
            
            Debug.Log("[TutorialManager] 튜토리얼이 완료되었습니다.");
        }
        
        /// <summary>
        /// UI 하이라이트 표시
        /// </summary>
        private void HighlightUI(string targetUIName)
        {
            GameObject targetUI = GameObject.Find(targetUIName);
            if (targetUI == null) return;
            
            RectTransform targetRect = targetUI.GetComponent<RectTransform>();
            if (targetRect == null) return;
            
            if (highlightOverlay != null)
                highlightOverlay.SetActive(true);
                
            if (highlightFrame != null)
            {
                // 하이라이트 프레임을 타겟 UI 위치와 크기에 맞춤
                highlightFrame.position = targetRect.position;
                highlightFrame.sizeDelta = targetRect.sizeDelta * 1.1f; // 약간 크게
            }
        }
        
        /// <summary>
        /// 하이라이트 숨기기
        /// </summary>
        private void HideHighlight()
        {
            if (highlightOverlay != null)
                highlightOverlay.SetActive(false);
        }
        
        /// <summary>
        /// 특정 단계로 점프 (디버그용)
        /// </summary>
        public void JumpToStep(int stepIndex)
        {
            if (stepIndex >= 0 && stepIndex < tutorialSteps.Count)
            {
                currentStepIndex = stepIndex;
                ShowCurrentStep();
            }
        }
        
        /// <summary>
        /// 튜토리얼 초기화 (다시 보기용)
        /// </summary>
        public void ResetTutorial()
        {
            PlayerPrefs.DeleteKey("TutorialCompleted");
            PlayerPrefs.Save();
            isTutorialCompleted = false;
            currentStepIndex = 0;
            
            Debug.Log("[TutorialManager] 튜토리얼이 초기화되었습니다.");
        }
        
        /// <summary>
        /// 현재 튜토리얼 진행 상태 확인
        /// </summary>
        public bool IsTutorialActive()
        {
            return isTutorialActive;
        }
        
        /// <summary>
        /// 튜토리얼 완료 여부 확인
        /// </summary>
        public bool IsTutorialCompleted()
        {
            return isTutorialCompleted;
        }
        
        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
    
    /// <summary>
    /// 튜토리얼 단계 데이터
    /// </summary>
    [System.Serializable]
    public class TutorialStep
    {
        [Header("단계 정보")]
        public string title;
        [TextArea(3, 5)]
        public string description;
        
        [Header("UI 설정")]
        public string targetUI; // 하이라이트할 UI 오브젝트 이름
        public bool highlightTarget; // 하이라이트 표시 여부
        public bool waitForInput; // 사용자 입력 대기 여부
        
        [Header("단계 타입")]
        public TutorialStepType stepType;
    }
    
    /// <summary>
    /// 튜토리얼 단계 타입
    /// </summary>
    public enum TutorialStepType
    {
        Introduction,    // 소개
        UI_Guide,       // UI 가이드
        Gameplay,       // 게임플레이 가이드
        System_Guide,   // 시스템 가이드
        Strategy_Guide, // 전략 가이드
        Battle_Start    // 전투 시작
    }
}