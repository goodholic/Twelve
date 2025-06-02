using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// ResultScene 전용 매니저: 승리/패배 여부 표시 + 로비로 돌아가기
/// 추가: 웨이브 정보, 획득 캐릭터, 보상 표시
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
    public TextMeshProUGUI waveInfoText;
    public TextMeshProUGUI rewardInfoText;
    public TextMeshProUGUI statsText;

    [Header("로비로 돌아가기 버튼")]
    public Button returnLobbyButton;

    [Header("다시하기 버튼")]
    public Button retryButton;

    [Header("보상 캐릭터 표시")]
    public Transform rewardCharacterContainer;
    public GameObject rewardCharacterPrefab;

    [Header("획득 골드/미네랄 표시")]
    public TextMeshProUGUI goldRewardText;
    public TextMeshProUGUI mineralRewardText;

    private void Start()
    {
        // 씬 진입 시, GameManager에서 승리/패배 여부 읽음
        bool isVictory = false;
        int completedWaves = 0;
        int totalDamageDealt = 0;
        int totalMonsterKilled = 0;

        if (GameManager.Instance != null)
        {
            isVictory = GameManager.Instance.isVictory;
            // GameManager에서 웨이브 정보 등을 가져올 수 있다고 가정
            // completedWaves = GameManager.Instance.GetCompletedWaves();
            // totalDamageDealt = GameManager.Instance.GetTotalDamageDealt();
            // totalMonsterKilled = GameManager.Instance.GetTotalMonsterKilled();
        }

        // 결과 텍스트 설정
        if (resultText != null)
        {
            if (isVictory)
            {
                resultText.text = "축하합니다!\n승리하셨습니다!";
                resultText.color = new Color(1f, 0.843f, 0f); // 금색
            }
            else
            {
                resultText.text = "아쉽네요.\n패배하셨습니다...";
                resultText.color = new Color(0.8f, 0.2f, 0.2f); // 붉은색
            }
        }

        // 웨이브 정보 표시
        if (waveInfoText != null)
        {
            waveInfoText.text = $"클리어한 웨이브: {completedWaves}";
        }

        // 통계 정보 표시
        if (statsText != null)
        {
            statsText.text = $"총 피해량: {totalDamageDealt}\n처치한 몬스터: {totalMonsterKilled}";
        }

        // 보상 정보 표시
        DisplayRewards(isVictory, completedWaves);

        // 버튼 리스너 등록
        if (returnLobbyButton != null)
        {
            returnLobbyButton.onClick.RemoveAllListeners();
            returnLobbyButton.onClick.AddListener(OnClickReturnToLobby);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnClickRetry);
        }
    }

    /// <summary>
    /// 보상 정보 표시
    /// </summary>
    private void DisplayRewards(bool isVictory, int completedWaves)
    {
        if (!isVictory)
        {
            if (rewardInfoText != null)
                rewardInfoText.text = "패배로 인해 보상이 없습니다.";
            return;
        }

        // 골드/미네랄 보상 계산
        int goldReward = 100 + (completedWaves * 50);
        int mineralReward = 50 + (completedWaves * 25);

        if (goldRewardText != null)
            goldRewardText.text = $"+{goldReward}";
        
        if (mineralRewardText != null)
            mineralRewardText.text = $"+{mineralReward}";

        // 5웨이브마다 캐릭터 보상
        int characterRewards = completedWaves / 5;
        if (characterRewards > 0 && rewardInfoText != null)
        {
            rewardInfoText.text = $"획득한 캐릭터: {characterRewards}개";
            
            // 보상 캐릭터 표시 (예시)
            DisplayRewardCharacters(characterRewards);
        }

        // 실제로 보상 지급 (CoreDataManager 연동 필요)
        ApplyRewards(goldReward, mineralReward);
    }

    /// <summary>
    /// 보상 캐릭터 UI 표시
    /// </summary>
    private void DisplayRewardCharacters(int count)
    {
        if (rewardCharacterContainer == null || rewardCharacterPrefab == null) return;

        // 기존 캐릭터 제거
        foreach (Transform child in rewardCharacterContainer)
        {
            Destroy(child.gameObject);
        }

        // 새 캐릭터 표시 (실제 데이터는 GameManager나 다른 곳에서 가져와야 함)
        for (int i = 0; i < count; i++)
        {
            GameObject rewardChar = Instantiate(rewardCharacterPrefab, rewardCharacterContainer);
            // 캐릭터 정보 설정 필요
        }
    }

    /// <summary>
    /// 실제 보상 지급
    /// </summary>
    private void ApplyRewards(int gold, int mineral)
    {
        // CoreDataManager가 있다면 연동
        // if (CoreDataManager.Instance != null)
        // {
        //     CoreDataManager.Instance.AddGold(gold);
        //     CoreDataManager.Instance.AddMineral(mineral);
        // }

        Debug.Log($"[ResultSceneManager] 보상 지급: 골드 +{gold}, 미네랄 +{mineral}");
    }

    /// <summary>
    /// 로비씬으로 돌아가기
    /// </summary>
    public void OnClickReturnToLobby()
    {
        // 게임 상태 초기화
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGameState();
        }

        // LobbyScene 이름으로 전환 (프로젝트에 따라 다를 수 있음)
        SceneManager.LoadScene("LobbyScene");
    }

    /// <summary>
    /// 다시하기
    /// </summary>
    public void OnClickRetry()
    {
        // 게임 상태 초기화
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGameState();
        }

        // 게임씬으로 바로 이동
        SceneManager.LoadScene("GameScene");
    }
}