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

    private void Start()
    {
        // 혹시 Inspector에서 버튼이 미연결 상태면 여기서 처리
        if (hostButton != null)
        {
            hostButton.onClick.RemoveAllListeners();
            hostButton.onClick.AddListener(OnClickHostGame);
        }
        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnClickJoinGame);
        }

        // 안내 문구
        if (userInfoText != null)
        {
            userInfoText.text = "Photon Fusion 멀티플레이 데모 로비입니다.\n" +
                                "Host / Join 버튼으로 게임에 입장하세요!";
        }
    }

    /// <summary>
    /// Host Game (FusionNetworkManager 호출)
    /// </summary>
    public void OnClickHostGame()
    {
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
