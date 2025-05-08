using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ResultScene 전용 매니저: 승리/패배 여부 표시 + 로비로 돌아가기
/// 
/// (사용법)
/// 1) ResultScene에 이 스크립트를 빈 오브젝트에 붙이고,
/// 2) resultText, returnLobbyButton 등을 인스펙터에 연결.
/// 3) GameManager.Instance.isVictory 를 확인하여 "승리/패배" 텍스트 반영.
/// </summary>
public class ResultSceneManager : MonoBehaviour
{
    [Header("결과 표시용 텍스트 (TMP)")]
    public TextMeshProUGUI resultText;

    [Header("로비로 돌아가기 버튼")]
    public Button returnLobbyButton;

    private void Start()
    {
        // 씬 진입 시, GameManager에서 승리/패배 여부 읽음
        if (GameManager.Instance != null && GameManager.Instance.isVictory)
        {
            if (resultText != null) resultText.text = "축하합니다!\n승리하셨습니다!";
        }
        else
        {
            if (resultText != null) resultText.text = "아쉽네요.\n패배하셨습니다...";
        }

        // 버튼 리스너 등록
        if (returnLobbyButton != null)
        {
            returnLobbyButton.onClick.RemoveAllListeners();
            returnLobbyButton.onClick.AddListener(OnClickReturnToLobby);
        }
    }

    /// <summary>
    /// 로비씬으로 돌아가기
    /// </summary>
    public void OnClickReturnToLobby()
    {
        // LobbyScene 이름으로 전환 (프로젝트에 따라 다를 수 있음)
        SceneManager.LoadScene("LobbyScene");
    }
}
