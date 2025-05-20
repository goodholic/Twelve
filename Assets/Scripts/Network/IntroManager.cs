using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
// [추가] 패널 클릭 이벤트 처리를 위해 필요
using UnityEngine.EventSystems;

/// <summary>
/// 게임 시작 시 튜토리얼 패널을 관리하는 매니저
/// 로비 씬 내에서 4개의 튜토리얼 패널을 순차적으로 보여주고, 완료 후 로비 UI 활성화
/// </summary>
public class IntroManager : MonoBehaviour
{
    [Header("튜토리얼 패널")]
    [Tooltip("튜토리얼 패널들을 순서대로 연결하세요 (인덱스 0~3)")]
    [SerializeField] private GameObject[] tutorialPanels = new GameObject[4];
    
    [Header("패널 이동 버튼")]
    [SerializeField] private Button nextButton;
    
    [Header("패널 설정")]
    [Tooltip("자동으로 다음 패널로 넘어가는 시간 (초)")]
    [SerializeField] private float autoNextTime = 0f;  // 0이면 자동 넘김 비활성화
    
    [Header("텍스트 요소")]
    [SerializeField] private TextMeshProUGUI panelTitleText;
    [SerializeField] private TextMeshProUGUI panelDescriptionText;
    
    [Header("로비 UI 참조")]
    [Tooltip("튜토리얼 완료 후 표시할 로비 UI")]
    [SerializeField] private GameObject lobbyUI;
    
    [Header("자동 시작 설정")]
    [Tooltip("true면 Start() 시 자동으로 튜토리얼을 시작합니다.")]
    [SerializeField] private bool autoStartTutorial = true;

    [Header("패널 전환 설정")]
    [Tooltip("패널 전환 시 페이드 효과 지속 시간 (초)")]
    [SerializeField] private float panelTransitionTime = 0.5f;
    
    // 튜토리얼 완료 시 호출할 이벤트 델리게이트
    public delegate void TutorialCompleteHandler();
    public event TutorialCompleteHandler OnTutorialComplete;
    
    private int currentPanelIndex = 0;
    private Coroutine autoNextCoroutine;
    private Coroutine transitionCoroutine;
    
    private string[] panelTitles = new string[] { 
        "게임 소개", 
        "게임 방법", 
        "캐릭터 소개", 
        "시작하기" 
    };
    
    private string[] panelDescriptions = new string[] {
        "Twelve는 타워 디펜스 게임입니다. 몬스터로부터 성을 지켜내세요!",
        "몬스터를 물리치고 자원을 모아 캐릭터를 강화하세요.",
        "다양한 캐릭터를 수집하고 나만의 덱을 만들어보세요.",
        "이제 게임을 시작할 준비가 되었습니다. 즐거운 시간 되세요!"
    };
    
    private void Awake()
    {
        // 버튼 클릭 이벤트 등록
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClick);
        }
        
        // 모든 패널 초기화
        InitializePanels();
        
        // 로비 UI 비활성화 (튜토리얼 종료 후 활성화)
        if (lobbyUI != null)
        {
            lobbyUI.SetActive(false);
        }

        // [추가] 모든 튜토리얼 패널에 클릭 이벤트를 추가
        // 각 패널 영역을 클릭했을 때 OnNextButtonClick()이 호출되도록 설정
        foreach (GameObject panel in tutorialPanels)
        {
            AddClickEventToPanel(panel);
        }
    }
    
    private void Start()
    {
        // 자동 시작 설정 시 첫 번째 패널 바로 표시
        if (autoStartTutorial)
        {
            ShowFirstPanel();
        }
        else
        {
            // 자동 시작이 아닌 경우 모든 패널 비활성화
            HideAllPanels();
        }
    }
    
    /// <summary>
    /// 패널들을 초기화합니다.
    /// </summary>
    private void InitializePanels()
    {
        // 모든 패널 비활성화
        HideAllPanels();
        currentPanelIndex = 0;
    }
    
    /// <summary>
    /// 모든 패널을 숨깁니다.
    /// </summary>
    private void HideAllPanels()
    {
        foreach (var panel in tutorialPanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 튜토리얼을 시작합니다. (외부에서 호출 가능)
    /// </summary>
    public void StartTutorial()
    {
        currentPanelIndex = 0;
        ShowFirstPanel();
    }
    
    /// <summary>
    /// 첫 번째 패널을 표시합니다.
    /// </summary>
    private void ShowFirstPanel()
    {
        // 모든 패널 비활성화
        HideAllPanels();
        
        // 첫 번째 패널만 활성화
        if (tutorialPanels.Length > 0 && tutorialPanels[0] != null)
        {
            currentPanelIndex = 0;
            tutorialPanels[0].SetActive(true);
            
            // 패널 제목과 설명 업데이트
            UpdatePanelText();
            
            // 자동 넘김 설정된 경우 코루틴 시작
            StartAutoNextTimer();
        }
    }
    
    /// <summary>
    /// 다음 버튼 클릭 시 호출되는 메서드
    /// </summary>
    public void OnNextButtonClick()
    {
        // 자동 넘김 코루틴 중지
        StopAutoNextTimer();
        
        // 전환 코루틴이 실행 중이면 중지
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        
        // 패널 전환 시작
        transitionCoroutine = StartCoroutine(TransitionToNextPanel());
    }
    
    /// <summary>
    /// 다음 패널로 전환하는 코루틴
    /// </summary>
    private IEnumerator TransitionToNextPanel()
    {
        // 현재 패널 페이드 아웃 (비활성화)
        if (currentPanelIndex >= 0 && currentPanelIndex < tutorialPanels.Length && 
            tutorialPanels[currentPanelIndex] != null)
        {
            GameObject currentPanel = tutorialPanels[currentPanelIndex];
            
            // 페이드 아웃 효과 (CanvasGroup 컴포넌트가 있을 경우)
            CanvasGroup canvasGroup = currentPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                float fadeTime = 0f;
                float startAlpha = canvasGroup.alpha;
                
                while (fadeTime < panelTransitionTime)
                {
                    fadeTime += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, fadeTime / panelTransitionTime);
                    yield return null;
                }
                
                canvasGroup.alpha = 0f;
            }
            
            // 패널 비활성화
            currentPanel.SetActive(false);
        }
        
        // 다음 패널 인덱스로 이동
        currentPanelIndex++;
        
        // 모든 패널을 다 보여줬으면 로비 UI 표시
        if (currentPanelIndex >= tutorialPanels.Length)
        {
            FinishTutorial();
            yield break;
        }
        
        // 다음 패널 활성화
        if (currentPanelIndex < tutorialPanels.Length && tutorialPanels[currentPanelIndex] != null)
        {
            GameObject nextPanel = tutorialPanels[currentPanelIndex];
            nextPanel.SetActive(true);
            
            // 패널 제목과 설명 업데이트
            UpdatePanelText();
            
            // 페이드 인 효과 (CanvasGroup 컴포넌트가 있을 경우)
            CanvasGroup canvasGroup = nextPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                
                float fadeTime = 0f;
                while (fadeTime < panelTransitionTime)
                {
                    fadeTime += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeTime / panelTransitionTime);
                    yield return null;
                }
                
                canvasGroup.alpha = 1f;
            }
            
            // 자동 넘김 타이머 시작
            StartAutoNextTimer();
        }
    }
    
    /// <summary>
    /// 자동 넘김 타이머를 시작합니다.
    /// </summary>
    private void StartAutoNextTimer()
    {
        // 자동 넘김 설정된 경우에만 코루틴 시작
        if (autoNextTime > 0)
        {
            StopAutoNextTimer();
            autoNextCoroutine = StartCoroutine(AutoNextPanel());
        }
    }
    
    /// <summary>
    /// 자동 넘김 타이머를 중지합니다.
    /// </summary>
    private void StopAutoNextTimer()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }
    }
    
    /// <summary>
    /// 패널의 텍스트 내용을 업데이트합니다.
    /// </summary>
    private void UpdatePanelText()
    {
        if (panelTitleText != null && currentPanelIndex < panelTitles.Length)
        {
            panelTitleText.text = panelTitles[currentPanelIndex];
        }
        
        if (panelDescriptionText != null && currentPanelIndex < panelDescriptions.Length)
        {
            panelDescriptionText.text = panelDescriptions[currentPanelIndex];
        }
    }
    
    /// <summary>
    /// 일정 시간 후 자동으로 다음 패널로 넘어가는 코루틴
    /// </summary>
    private IEnumerator AutoNextPanel()
    {
        yield return new WaitForSeconds(autoNextTime);
        OnNextButtonClick();
    }
    
    /// <summary>
    /// 튜토리얼을 종료하고 로비 UI를 표시합니다.
    /// </summary>
    private void FinishTutorial()
    {
        Debug.Log("[IntroManager] 모든 튜토리얼 패널을 보여줬습니다. 로비 UI 표시.");
        
        // 모든 튜토리얼 패널 비활성화 (이미 비활성화되어 있을 수 있음)
        HideAllPanels();
        
        // 로비 UI 활성화 (페이드 인 효과 적용)
        if (lobbyUI != null)
        {
            lobbyUI.SetActive(true);
            
            // 페이드 인 효과 (CanvasGroup 컴포넌트가 있을 경우)
            CanvasGroup canvasGroup = lobbyUI.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                StartCoroutine(FadeInLobbyUI(canvasGroup));
            }
        }
        
        // 완료 이벤트 발생
        OnTutorialComplete?.Invoke();
    }
    
    /// <summary>
    /// 로비 UI를 페이드 인 효과로 표시합니다.
    /// </summary>
    private IEnumerator FadeInLobbyUI(CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 0f;
        
        float fadeTime = 0f;
        while (fadeTime < panelTransitionTime)
        {
            fadeTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeTime / panelTransitionTime);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// 튜토리얼을 건너뛰고 바로 로비 UI를 표시합니다. (Skip 버튼 등에서 호출)
    /// </summary>
    public void SkipTutorial()
    {
        // 자동 넘김 코루틴 중지
        StopAutoNextTimer();
        
        // 전환 코루틴이 실행 중이면 중지
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        
        FinishTutorial();
    }

    /// <summary>
    /// [추가] 각 패널에 클릭 이벤트를 할당하여,
    /// 패널을 클릭하면 OnNextButtonClick()이 호출되도록 설정합니다.
    /// </summary>
    private void AddClickEventToPanel(GameObject panel)
    {
        if (panel == null) return;

        // EventTrigger 컴포넌트를 찾거나 추가
        EventTrigger eventTrigger = panel.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = panel.AddComponent<EventTrigger>();
        }

        // 클릭 이벤트 항목 생성
        EventTrigger.Entry clickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };

        // 클릭 시 OnNextButtonClick() 호출
        clickEntry.callback.AddListener((data) => { OnNextButtonClick(); });
        eventTrigger.triggers.Add(clickEntry);
    }
}
