using UnityEngine;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// 캐릭터별 엔딩 영상 관리 시스템
/// </summary>
public class CharacterEndingManager : MonoBehaviour
{
    [Header("엔딩 영상 재생")]
    public VideoPlayer videoPlayer;
    public GameObject endingPanel;
    public TextMeshProUGUI endingTitleText;
    
    [Header("엔딩 영상 파일들")]
    public VideoClip[] characterEndingVideos = new VideoClip[12]; // 12명의 캐릭터 엔딩
    
    [Header("캐릭터 정보")]
    public string[] characterIds = {
        "char_001", "char_002", "char_003", "char_004",
        "char_005", "char_006", "char_007", "char_008", 
        "char_009", "char_010", "char_011", "char_012"
    };
    
    public string[] characterNames = {
        "전사", "마법사", "궁수", "성직자",
        "도적", "기사", "암살자", "드루이드",
        "팔라딘", "워락", "바드", "레인저"
    };

    private static CharacterEndingManager instance;
    public static CharacterEndingManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<CharacterEndingManager>();
            return instance;
        }
    }

    void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 캐릭터 엔딩이 해금되었는지 확인
    /// </summary>
    public bool IsEndingUnlocked(string characterId)
    {
        int wins = PlayerPrefs.GetInt($"CharacterWins_{characterId}", 0);
        return wins >= 100;
    }

    /// <summary>
    /// 캐릭터의 현재 승리 수 반환
    /// </summary>
    public int GetCharacterWins(string characterId)
    {
        return PlayerPrefs.GetInt($"CharacterWins_{characterId}", 0);
    }

    /// <summary>
    /// 캐릭터 엔딩 영상 재생
    /// </summary>
    public void PlayCharacterEnding(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characterIds.Length)
        {
            Debug.LogError($"유효하지 않은 캐릭터 인덱스: {characterIndex}");
            return;
        }

        string characterId = characterIds[characterIndex];
        
        if (!IsEndingUnlocked(characterId))
        {
            Debug.Log($"{characterNames[characterIndex]}의 엔딩이 아직 해금되지 않았습니다.");
            return;
        }

        if (characterIndex < characterEndingVideos.Length && characterEndingVideos[characterIndex] != null)
        {
            PlayEnding(characterEndingVideos[characterIndex], characterNames[characterIndex]);
        }
        else
        {
            // 영상 파일이 없는 경우 텍스트 엔딩 표시
            ShowTextEnding(characterNames[characterIndex]);
        }
    }

    /// <summary>
    /// 영상 엔딩 재생
    /// </summary>
    void PlayEnding(VideoClip endingVideo, string characterName)
    {
        if (videoPlayer != null && endingPanel != null)
        {
            endingPanel.SetActive(true);
            
            if (endingTitleText != null)
                endingTitleText.text = $"{characterName}의 엔딩";
            
            videoPlayer.clip = endingVideo;
            videoPlayer.Play();
            
            Debug.Log($"{characterName}의 엔딩 영상을 재생합니다.");
        }
    }

    /// <summary>
    /// 텍스트 엔딩 표시 (영상이 없는 경우)
    /// </summary>
    void ShowTextEnding(string characterName)
    {
        if (endingPanel != null)
        {
            endingPanel.SetActive(true);
            
            if (endingTitleText != null)
            {
                endingTitleText.text = $"🎉 {characterName}의 엔딩\n\n" +
                                      $"{characterName}은(는) 수많은 전투를 거쳐\n" +
                                      $"100번의 승리를 달성했습니다!\n\n" +
                                      $"전설적인 영웅으로 기억될 것입니다.\n\n" +
                                      $"축하합니다!";
            }
            
            Debug.Log($"{characterName}의 텍스트 엔딩을 표시합니다.");
        }
    }

    /// <summary>
    /// 엔딩 패널 닫기
    /// </summary>
    public void CloseEndingPanel()
    {
        if (endingPanel != null)
            endingPanel.SetActive(false);
            
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();
    }

    /// <summary>
    /// 모든 캐릭터의 승리 수 리셋 (디버그용)
    /// </summary>
    [ContextMenu("Reset All Character Wins")]
    public void ResetAllCharacterWins()
    {
        for (int i = 0; i < characterIds.Length; i++)
        {
            PlayerPrefs.DeleteKey($"CharacterWins_{characterIds[i]}");
        }
        PlayerPrefs.Save();
        Debug.Log("모든 캐릭터의 승리 수가 리셋되었습니다.");
    }

    /// <summary>
    /// 특정 캐릭터에게 100승 부여 (디버그용)
    /// </summary>
    [ContextMenu("Give 100 Wins to First Character")]
    public void Give100WinsToFirstCharacter()
    {
        if (characterIds.Length > 0)
        {
            PlayerPrefs.SetInt($"CharacterWins_{characterIds[0]}", 100);
            PlayerPrefs.Save();
            Debug.Log($"{characterNames[0]}에게 100승이 부여되었습니다.");
        }
    }
} 