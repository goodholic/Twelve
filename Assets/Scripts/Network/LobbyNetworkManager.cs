// Assets\Scripts\Network\LobbyNetworkManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

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

    private void Start()
    {
        // 1) '호스트'/'조인' 버튼에 클릭 리스너 연결
        if (hostButton != null)
        {
            hostButton.onClick.RemoveAllListeners();
            hostButton.onClick.AddListener(OnClickHostGame);
            // 시작 시에는 버튼을 비활성화
            hostButton.interactable = false;
        }
        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnClickJoinGame);
            // 시작 시에는 버튼을 비활성화
            joinButton.interactable = false;
        }

        // 2) 안내 문구
        if (userInfoText != null)
        {
            userInfoText.text = "Photon Fusion 멀티플레이 데모 로비입니다.\n" +
                                "로그인 버튼은 아직 비활성화되어 있습니다.";
        }

        // 3) 로그인 패널은 기본으로 활성화
        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
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
