using UnityEngine;
// using UnityEngine.Video; // PNG 시퀀스로 교체됨
using UnityEngine.UI;
using TMPro;
using System.Collections;
using GuildMaster.Battle;

/// <summary>
/// 캐릭터별 엔딩 PNG 시퀀스 관리 시스템
/// VideoPlayer 대신 PNG 시퀀스를 사용하여 완벽한 투명배경 지원
/// </summary>
public class CharacterEndingManager : MonoBehaviour
{
    [Header("🖼️ PNG 시퀀스 엔딩 재생")]
    [SerializeField, Tooltip("PNG 시퀀스를 표시할 RawImage")]
    public RawImage endingDisplayImage;
    [SerializeField, Tooltip("엔딩 패널 GameObject")]
    public GameObject endingPanel;
    [SerializeField, Tooltip("엔딩 제목 텍스트")]
    public TextMeshProUGUI endingTitleText;
    
    [Header("🎬 PNG 시퀀스 설정")]
    [SerializeField, Tooltip("PNG 시퀀스 프레임레이트")]
    public float endingFrameRate = 24f; // 엔딩은 부드럽게
    [SerializeField, Tooltip("PNG 시퀀스 반복 재생 여부")]
    public bool loopEndingSequences = false; // 엔딩은 한 번만 재생
    [SerializeField, Tooltip("PNG 시퀀스 크기 조절")]
    public float endingSequenceScale = 1.0f;
    
    [Header("🖼️ 캐릭터별 엔딩 PNG 시퀀스")]
    [Space(10)]
    [SerializeField, Tooltip("12명 캐릭터의 엔딩 PNG 시퀀스 배열")]
    public CharacterEndingSequence[] characterEndingSequences = new CharacterEndingSequence[12];
    
    [System.Serializable]
    public class CharacterEndingSequence
    {
        [Header("캐릭터 정보")]
        public string characterName;
        public string characterId;
        
        [Header("PNG 시퀀스")]
        [Tooltip("엔딩 PNG 시퀀스 프레임들")]
        public Texture2D[] endingPNGFrames;
        
        [Header("엔딩 설정")]
        [Tooltip("이 캐릭터만의 특별한 프레임레이트 (0이면 기본값 사용)")]
        public float customFrameRate = 0f;
        [Tooltip("특별한 엔딩 메시지")]
        public string customEndingMessage = "";
    }
    
    [Header("📋 캐릭터 기본 정보")]
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

    // PNG 시퀀스 재생 관련
    private Coroutine endingSequenceCoroutine;
    private bool isPlayingEndingSequence = false;
    private int currentFrameIndex = 0;
    private Texture2D[] currentEndingSequence;
    
    private static CharacterEndingManager instance;
    public static CharacterEndingManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<CharacterEndingManager>();
            return instance;
        }
    }

    void Awake()
    {
        instance = this;
        InitializeCharacterSequences();
    }
    
    /// <summary>
    /// 캐릭터 시퀀스 배열 초기화
    /// </summary>
    private void InitializeCharacterSequences()
    {
        // 기본 정보가 없는 시퀀스들에 기본값 설정
        for (int i = 0; i < characterEndingSequences.Length && i < characterIds.Length; i++)
        {
            if (characterEndingSequences[i] == null)
            {
                characterEndingSequences[i] = new CharacterEndingSequence();
            }
            
            if (string.IsNullOrEmpty(characterEndingSequences[i].characterId))
            {
                characterEndingSequences[i].characterId = characterIds[i];
            }
            
            if (string.IsNullOrEmpty(characterEndingSequences[i].characterName))
            {
                characterEndingSequences[i].characterName = characterNames[i];
            }
        }
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
    /// 캐릭터 엔딩 PNG 시퀀스 재생
    /// </summary>
    public void PlayCharacterEnding(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characterIds.Length)
        {
            Debug.LogError($"[Ending Manager] 유효하지 않은 캐릭터 인덱스: {characterIndex}");
            return;
        }

        string characterId = characterIds[characterIndex];
        string characterName = characterNames[characterIndex];
        
        if (!IsEndingUnlocked(characterId))
        {
            Debug.Log($"[Ending Manager] {characterName}의 엔딩이 아직 해금되지 않았습니다. (현재 승리: {GetCharacterWins(characterId)}/100)");
            ShowUnlockRequirement(characterName, GetCharacterWins(characterId));
            return;
        }

        if (characterIndex < characterEndingSequences.Length && 
            characterEndingSequences[characterIndex] != null &&
            characterEndingSequences[characterIndex].endingPNGFrames != null &&
            characterEndingSequences[characterIndex].endingPNGFrames.Length > 0)
        {
            PlayPNGSequenceEnding(characterEndingSequences[characterIndex]);
        }
        else
        {
            // PNG 시퀀스가 없는 경우 텍스트 엔딩 표시
            ShowTextEnding(characterName, characterIndex < characterEndingSequences.Length ? 
                         characterEndingSequences[characterIndex]?.customEndingMessage : "");
        }
    }

    /// <summary>
    /// PNG 시퀀스 엔딩 재생
    /// </summary>
    private void PlayPNGSequenceEnding(CharacterEndingSequence endingSequence)
    {
        if (endingDisplayImage == null || endingPanel == null)
        {
            Debug.LogError("[Ending Manager] PNG 시퀀스 표시용 UI 컴포넌트가 설정되지 않았습니다!");
            ShowTextEnding(endingSequence.characterName, endingSequence.customEndingMessage);
            return;
        }
        
        // 이전 재생 중단
        StopEndingSequence();
        
        endingPanel.SetActive(true);
        
        if (endingTitleText != null)
        {
            endingTitleText.text = $"🎉 {endingSequence.characterName}의 엔딩";
        }
        
        // PNG 시퀀스 설정
        currentEndingSequence = endingSequence.endingPNGFrames;
        currentFrameIndex = 0;
        isPlayingEndingSequence = true;
        
        // 프레임레이트 설정 (커스텀 > 기본값)
        float frameRate = endingSequence.customFrameRate > 0 ? 
                         endingSequence.customFrameRate : endingFrameRate;
        
        // PNG 시퀀스 재생 시작
        endingSequenceCoroutine = StartCoroutine(PlayEndingSequenceCoroutine(frameRate));
        
        Debug.Log($"[Ending Manager] {endingSequence.characterName}의 PNG 시퀀스 엔딩 재생 시작 ({currentEndingSequence.Length} frames, {frameRate} FPS)");
    }
    
    /// <summary>
    /// PNG 시퀀스 재생 코루틴
    /// </summary>
    private IEnumerator PlayEndingSequenceCoroutine(float frameRate)
    {
        float frameDelay = 1f / frameRate;
        
        while (isPlayingEndingSequence && currentEndingSequence != null)
        {
            if (currentFrameIndex < currentEndingSequence.Length)
            {
                // 현재 프레임 표시
                if (currentEndingSequence[currentFrameIndex] != null)
                {
                    endingDisplayImage.texture = currentEndingSequence[currentFrameIndex];
                    
                    // 크기 조절 적용
                    if (endingSequenceScale != 1.0f)
                    {
                        Vector3 scale = endingDisplayImage.transform.localScale;
                        scale *= endingSequenceScale;
                        endingDisplayImage.transform.localScale = scale;
                    }
                }
                
                currentFrameIndex++;
                yield return new WaitForSeconds(frameDelay);
            }
            else
            {
                // 시퀀스 완료
                if (loopEndingSequences)
                {
                    currentFrameIndex = 0; // 루프
                }
                else
                {
                    // 엔딩 완료
                    isPlayingEndingSequence = false;
                    Debug.Log("[Ending Manager] PNG 시퀀스 엔딩 재생 완료");
                    yield return new WaitForSeconds(3f); // 3초 대기 후 자동 종료
                    CloseEndingPanel();
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// PNG 시퀀스 재생 중단
    /// </summary>
    private void StopEndingSequence()
    {
        if (endingSequenceCoroutine != null)
        {
            StopCoroutine(endingSequenceCoroutine);
            endingSequenceCoroutine = null;
        }
        isPlayingEndingSequence = false;
        currentEndingSequence = null;
        currentFrameIndex = 0;
    }

    /// <summary>
    /// 해금 조건 표시
    /// </summary>
    private void ShowUnlockRequirement(string characterName, int currentWins)
    {
        if (endingPanel != null)
        {
            endingPanel.SetActive(true);
            
            if (endingTitleText != null)
            {
                endingTitleText.text = $"🔒 {characterName}의 엔딩\n\n" +
                                      $"현재 승리 횟수: {currentWins}/100\n\n" +
                                      $"엔딩을 해금하려면\n" +
                                      $"{100 - currentWins}번 더 승리하세요!\n\n" +
                                      $"화이팅! 💪";
            }
            
            Debug.Log($"[Ending Manager] {characterName} 엔딩 해금 조건 표시: {currentWins}/100 승리");
        }
    }

    /// <summary>
    /// 텍스트 엔딩 표시 (PNG 시퀀스가 없는 경우)
    /// </summary>
    private void ShowTextEnding(string characterName, string customMessage = "")
    {
        if (endingPanel != null)
        {
            endingPanel.SetActive(true);
            
            if (endingTitleText != null)
            {
                string message = !string.IsNullOrEmpty(customMessage) ? customMessage :
                                $"{characterName}은(는) 수많은 전투를 거쳐\n" +
                                $"100번의 승리를 달성했습니다!\n\n" +
                                $"전설적인 영웅으로 기억될 것입니다.\n\n" +
                                $"축하합니다!";
                
                endingTitleText.text = $"🎉 {characterName}의 엔딩\n\n{message}";
            }
            
            Debug.Log($"[Ending Manager] {characterName}의 텍스트 엔딩 표시");
        }
    }

    /// <summary>
    /// 엔딩 패널 닫기
    /// </summary>
    public void CloseEndingPanel()
    {
        // PNG 시퀀스 재생 중단
        StopEndingSequence();
        
        if (endingPanel != null)
            endingPanel.SetActive(false);
            
        Debug.Log("[Ending Manager] 엔딩 패널 닫기");
    }
    
    /// <summary>
    /// 특정 캐릭터의 PNG 시퀀스가 설정되어 있는지 확인
    /// </summary>
    public bool HasPNGSequenceEnding(int characterIndex)
    {
        return characterIndex >= 0 && 
               characterIndex < characterEndingSequences.Length &&
               characterEndingSequences[characterIndex] != null &&
               characterEndingSequences[characterIndex].endingPNGFrames != null &&
               characterEndingSequences[characterIndex].endingPNGFrames.Length > 0;
    }
    
    /// <summary>
    /// 현재 PNG 시퀀스가 재생 중인지 확인
    /// </summary>
    public bool IsPlayingPNGSequence()
    {
        return isPlayingEndingSequence;
    }

    /// <summary>
    /// 모든 캐릭터의 승리 수 리셋 (디버그용)
    /// </summary>
    [ContextMenu("🔄 Reset All Character Wins")]
    public void ResetAllCharacterWins()
    {
        for (int i = 0; i < characterIds.Length; i++)
        {
            PlayerPrefs.DeleteKey($"CharacterWins_{characterIds[i]}");
        }
        PlayerPrefs.Save();
        Debug.Log("[Ending Manager] 모든 캐릭터의 승리 수가 리셋되었습니다.");
    }

    /// <summary>
    /// 특정 캐릭터에게 100승 부여 (디버그용)
    /// </summary>
    [ContextMenu("🎉 Give 100 Wins to First Character")]
    public void Give100WinsToFirstCharacter()
    {
        if (characterIds.Length > 0)
        {
            PlayerPrefs.SetInt($"CharacterWins_{characterIds[0]}", 100);
            PlayerPrefs.Save();
            Debug.Log($"[Ending Manager] {characterNames[0]}에게 100승이 부여되었습니다.");
        }
    }
    
    /// <summary>
    /// 모든 캐릭터에게 100승 부여 (디버그용)
    /// </summary>
    [ContextMenu("🚀 Give 100 Wins to All Characters")]
    public void Give100WinsToAllCharacters()
    {
        for (int i = 0; i < characterIds.Length; i++)
        {
            PlayerPrefs.SetInt($"CharacterWins_{characterIds[i]}", 100);
        }
        PlayerPrefs.Save();
        Debug.Log("[Ending Manager] 모든 캐릭터에게 100승이 부여되었습니다!");
    }
    
    /// <summary>
    /// PNG 시퀀스 엔딩 시스템 상태 출력 (디버그용)
    /// </summary>
    [ContextMenu("🔍 Show PNG Sequence Status")]
    public void ShowPNGSequenceStatus()
    {
        Debug.Log("=== 🖼️ PNG 시퀀스 엔딩 시스템 상태 ===");
        Debug.Log($"📊 기본 프레임레이트: {endingFrameRate} FPS");
        Debug.Log($"🔄 루프 재생: {loopEndingSequences}");
        Debug.Log($"📏 스케일: {endingSequenceScale}");
        Debug.Log("");
        
        for (int i = 0; i < characterEndingSequences.Length; i++)
        {
            if (characterEndingSequences[i] != null)
            {
                var seq = characterEndingSequences[i];
                bool hasFrames = seq.endingPNGFrames != null && seq.endingPNGFrames.Length > 0;
                string status = hasFrames ? $"✅ {seq.endingPNGFrames.Length} frames" : "❌ 없음";
                string customRate = seq.customFrameRate > 0 ? $" ({seq.customFrameRate} FPS)" : "";
                
                Debug.Log($"{i+1:00}. {seq.characterName} ({seq.characterId}): {status}{customRate}");
            }
            else
            {
                Debug.Log($"{i+1:00}. [설정되지 않음]");
            }
        }
        Debug.Log("==========================================");
    }
    
    private void OnDestroy()
    {
        // PNG 시퀀스 재생 정리
        StopEndingSequence();
    }
} 