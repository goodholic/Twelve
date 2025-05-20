// Assets\Scripts\Network\LobbyNetworkManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections;

/// <summary>
/// 로비 씬에서 호스트/클라이언트 진입만 담당하는 간단한 매니저.
/// - Firebase/GoogleSignIn 관련 로직을 전부 삭제함
/// - Photon Fusion 호스트/클라이언트 연결만 제공
/// </summary>
public class LobbyNetworkManager : MonoBehaviour
{
    [Header("UI 관련 (옵션)")]
    [Tooltip("로비화면에서 안내 문구 등을 표시할 TextMeshProUGUI")]
    [SerializeField] private TextMeshProUGUI userInfoText;

    [Header("호스트/조인 버튼")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    // =================== [추가된 부분] ===================
    [Header("로그인 패널 (게임 시작 시 활성화)")]
    [Tooltip("로그인 UI 전체를 담은 패널(게임오브젝트)을 연결하세요.")]
    [SerializeField] private GameObject loginPanel;
    // ===================================================

    // =================== [새로 추가된 부분: 튜토리얼 패널] ===================
    [Header("튜토리얼 패널 (4개 순차적으로 표시)")]
    [Tooltip("튜토리얼 패널들을 순서대로 연결하세요 (인덱스 0~3)")]
    [SerializeField] private GameObject[] tutorialPanels = new GameObject[4];
    
    [Header("튜토리얼 관련 설정")]
    [Tooltip("true면 게임 시작 시 자동으로 튜토리얼을 표시합니다.")]
    [SerializeField] private bool showTutorialOnStart = true;
    
    [Tooltip("현재 표시 중인 튜토리얼 패널 인덱스")]
    private int currentTutorialIndex = 0;
    
    [Tooltip("true면 튜토리얼이 활성화된 상태")]
    private bool isTutorialActive = false;
    // ===================================================================

    private void Start()
    {
        // 1) '호스트'/'조인' 버튼에 클릭 리스너 연결
        if (hostButton != null)
        {
            hostButton.onClick.RemoveAllListeners();
            hostButton.onClick.AddListener(OnClickHostGame);
            // 자동 로그인을 위해 버튼 바로 활성화
            hostButton.interactable = true;
        }
        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnClickJoinGame);
            // 자동 로그인을 위해 버튼 바로 활성화
            joinButton.interactable = true;
        }

        // 2) 안내 문구
        if (userInfoText != null)
        {
            userInfoText.text = "로그인이 완료되었습니다.\n" +
                                "Host/Join 버튼을 눌러 게임에 입장하세요!";
        }

        // 3) 로그인 패널은 비활성화 (자동 로그인)
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }
        
        // 4) 튜토리얼 패널 초기화
        InitializeTutorialPanels();
        
        // 5) 자동으로 튜토리얼 시작 (설정된 경우)
        if (showTutorialOnStart)
        {
            StartTutorial();
        }
    }
    
    /// <summary>
    /// 튜토리얼 패널들을 초기화합니다.
    /// </summary>
    private void InitializeTutorialPanels()
    {
        // 모든 튜토리얼 패널 비활성화
        foreach (var panel in tutorialPanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
        
        // 튜토리얼 상태 초기화
        currentTutorialIndex = 0;
        isTutorialActive = false;
    }
    
    /// <summary>
    /// 튜토리얼을 시작합니다. (첫 번째 패널 표시)
    /// </summary>
    public void StartTutorial()
    {
        if (tutorialPanels.Length == 0)
        {
            Debug.LogWarning("[LobbyNetworkManager] 튜토리얼 패널이 설정되지 않았습니다.");
            return;
        }
        
        isTutorialActive = true;
        currentTutorialIndex = 0;
        
        // 첫 번째 패널 표시
        ShowCurrentTutorialPanel();
    }
    
    /// <summary>
    /// 현재 인덱스의 튜토리얼 패널을 표시합니다.
    /// </summary>
    private void ShowCurrentTutorialPanel()
    {
        // 모든 패널 비활성화
        foreach (var panel in tutorialPanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
        
        // 현재 인덱스 패널만 활성화
        if (currentTutorialIndex >= 0 && currentTutorialIndex < tutorialPanels.Length && 
            tutorialPanels[currentTutorialIndex] != null)
        {
            tutorialPanels[currentTutorialIndex].SetActive(true);
        }
    }
    
    /// <summary>
    /// 다음 튜토리얼 패널로 이동합니다. (마지막이면 튜토리얼 종료)
    /// </summary>
    public void NextTutorialPanel()
    {
        if (!isTutorialActive)
            return;
            
        currentTutorialIndex++;
        
        // 모든 튜토리얼 패널을 다 보여줬으면 종료
        if (currentTutorialIndex >= tutorialPanels.Length)
        {
            EndTutorial();
            return;
        }
        
        // 다음 패널 표시
        ShowCurrentTutorialPanel();
    }
    
    /// <summary>
    /// 튜토리얼을 종료합니다.
    /// </summary>
    private void EndTutorial()
    {
        // 모든 튜토리얼 패널 비활성화
        foreach (var panel in tutorialPanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
        
        isTutorialActive = false;
        
        // 로그인 패널 비활성화 (자동 로그인 상태 유지)
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }
    }

    /// <summary>
    /// (예: 이용약관 '동의' 버튼 등에서 호출) -> 호스트/클라 로그인 버튼 활성화
    /// </summary>
    public void ActivateLoginButtons()
    {
        if (hostButton != null) hostButton.interactable = true;
        if (joinButton != null) joinButton.interactable = true;

        if (userInfoText != null)
        {
            userInfoText.text = "이제 Host/Join 버튼을 눌러 게임에 입장할 수 있습니다!";
        }
    }

    /// <summary>
    /// Host Game (FusionNetworkManager 호출)
    /// </summary>
    public void OnClickHostGame()
    {
        // 로그인 패널 비활성화
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }

        FusionNetworkManager fusionMgr = FindFirstObjectByType<FusionNetworkManager>();
        if (fusionMgr != null)
        {
            fusionMgr.StartGame(GameMode.Host);
        }
        else
        {
            Debug.LogWarning("[LobbyNetworkManager] FusionNetworkManager 오브젝트를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// Join Game (FusionNetworkManager 호출)
    /// </summary>
    public void OnClickJoinGame()
    {
        // 로그인 패널 비활성화
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }

        FusionNetworkManager fusionMgr = FindFirstObjectByType<FusionNetworkManager>();
        if (fusionMgr != null)
        {
            fusionMgr.JoinGame();
        }
        else
        {
            Debug.LogWarning("[LobbyNetworkManager] FusionNetworkManager 오브젝트를 찾을 수 없습니다.");
        }
    }
}
