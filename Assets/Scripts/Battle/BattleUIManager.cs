using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GuildMaster.UI;

namespace TwelveGame.Battle
{
    /// <summary>
    /// Twelve Game의 전투 시스템 UI 관리자
    /// 게임 상태, 캐릭터 선택, 점수 등의 UI를 담당합니다
    /// </summary>
    public class BattleUIManager : MonoBehaviour
{
    private static BattleUIManager instance;
    public static BattleUIManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<BattleUIManager>();
            return instance;
        }
    }

    [Header("게임 상태 UI")]
    public TextMeshProUGUI currentTurnText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gameStateText;

    [Header("캐릭터 선택 버튼")]
    public GameObject[] characterButtons = new GameObject[4]; // 현재 선택 가능한 4개 캐릭터
    public Image[] buttonImages;
    public TextMeshProUGUI[] buttonTexts;
    
    [Header("다음 캐릭터 미리보기")]
    public GameObject nextCharacterCard; // 다음에 나올 캐릭터 미리보기 카드
    public Image nextCharacterImage; // 다음 캐릭터 이미지
    public TextMeshProUGUI nextCharacterText; // 다음 캐릭터 이름

    [Header("게임 준비 UI")]
    public GameObject preparationPanel; // 더 이상 사용되지 않음 (로비에서 바로 배틀 시작)
    public TextMeshProUGUI aTileCountText; // A타일 X:O 비율 표시
    public TextMeshProUGUI bTileCountText; // B타일 X:O 비율 표시

    [Header("게임 종료 UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI winnerText;
    public Button restartButton;

    [Header("캐릭터 정보 UI")]
    public GameObject characterInfoPanel;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterDescText;
    public Image attackPatternPreview;

    [Header("전투 미리보기")]
    public GameObject battlePreviewPanel;
    public TextMeshProUGUI previewText;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Debug.Log("🚀 BattleUIManager Start() 호출됨");
        InitializeUI();
        
        // 2초 후에 강제로 업데이트 (GameManager 초기화 완료 대기)
        StartCoroutine(DelayedForceUpdate());
    }
    
    System.Collections.IEnumerator DelayedForceUpdate()
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("⏰ BattleUIManager 지연 업데이트 시작");
        UpdateCharacterButtons();
        UpdateUI();
    }

    void InitializeUI()
    {
        // 버튼 이벤트 연결
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int buttonIndex = i;
            Button btn = characterButtons[i].GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnCharacterButtonClick(buttonIndex));
            }
        }

        // 배틀 시작 버튼 제거됨 - 로비에서 바로 배틀씬 진입 시 자동 시작

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClick);

        UpdateUI();
    }

    public void UpdateUI()
    {
        // 턴 표시
        if (currentTurnText != null)
        {
            string turnTeam = GameManager.Instance.currentTurn == GameManager.Team.X ? "X" : "O";
            currentTurnText.text = $"현재 턴: {turnTeam} 팀";
        }

        // 점수 표시
        if (scoreText != null)
        {
            scoreText.text = $"X팀: {GameManager.Instance.xTeamScore} - O팀: {GameManager.Instance.oTeamScore}";
        }

        // 타일별 X:O 비율 표시
        UpdateTileCountTexts();

        // 다음 캐릭터 미리보기 업데이트
        UpdateNextCharacterPreview();

        // 게임 상태 표시 (턴 수 중심)
        if (gameStateText != null)
        {
            switch (GameManager.Instance.currentState)
            {
                case GameManager.GameState.Preparation:
                    gameStateText.text = $"{GameManager.Instance.currentTurnNumber}턴 (준비)";
                    break;
                case GameManager.GameState.Battle:
                    gameStateText.text = $"{GameManager.Instance.currentTurnNumber}턴 / {GameManager.Instance.maxTurns}턴";
                    break;
                case GameManager.GameState.GameOver:
                    gameStateText.text = "게임 종료";
                    break;
            }
        }

        // 패널 활성화/비활성화
        UpdatePanels();
    }

    void UpdateTileCountTexts()
    {
        // A타일과 B타일별 X:O 비율 계산
        var aTileCount = GameManager.Instance.GetTileTeamCount(0); // A타일 (보드 인덱스 0)
        var bTileCount = GameManager.Instance.GetTileTeamCount(1); // B타일 (보드 인덱스 1)

        // A타일 X:O 표시
        if (aTileCountText != null)
        {
            aTileCountText.text = $"A타일 {aTileCount.xCount}:{aTileCount.oCount}";
        }

        // B타일 X:O 표시
        if (bTileCountText != null)
        {
            bTileCountText.text = $"B타일 {bTileCount.xCount}:{bTileCount.oCount}";
        }
    }

    void UpdateNextCharacterPreview()
    {
        // 다음 캐릭터 미리보기 업데이트
        CharacterData nextCharacter = GameManager.Instance.GetNextCharacter();
        
        if (nextCharacterCard != null)
        {
            nextCharacterCard.SetActive(nextCharacter != null);
            
            if (nextCharacter != null)
            {
                // 다음 캐릭터 이미지 설정
                if (nextCharacterImage != null)
                {
                    if (nextCharacter.characterIcon != null)
                    {
                        nextCharacterImage.sprite = nextCharacter.characterIcon;
                    }
                    else
                    {
                        // 캐릭터 아이콘이 없을 때 기본 스프라이트 생성
                        nextCharacterImage.sprite = CreateDefaultCharacterSprite(nextCharacter.characterName);
                    }
                }
                
                // 다음 캐릭터 이름 설정
                if (nextCharacterText != null)
                {
                    nextCharacterText.text = nextCharacter.characterName;
                }
            }
            else if (nextCharacterImage != null)
            {
                // 다음 캐릭터가 없으면 이미지를 null로 설정
                nextCharacterImage.sprite = null;
            }
        }
    }

    void UpdatePanels()
    {
        bool isPreparation = GameManager.Instance.currentState == GameManager.GameState.Preparation;
        bool isBattle = GameManager.Instance.currentState == GameManager.GameState.Battle;
        bool isGameOver = GameManager.Instance.currentState == GameManager.GameState.GameOver;

        // 준비 패널은 더 이상 사용하지 않음 (로비에서 바로 배틀 시작)
        if (preparationPanel != null)
            preparationPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(isGameOver);

        // 캐릭터 버튼은 준비 단계와 배틀 단계 모두에서 활성화
        foreach (GameObject btn in characterButtons)
        {
            if (btn != null)
                btn.SetActive(isPreparation || isBattle);
        }
    }

    void OnCharacterButtonClick(int buttonIndex)
    {
        GameManager.Instance.OnCharacterButtonClick(buttonIndex);
        
        // 버튼 하이라이트
        HighlightButton(buttonIndex);
        
        // 캐릭터 정보 표시
        if (GameManager.Instance.selectedCharacter != null)
        {
            ShowCharacterInfo(GameManager.Instance.selectedCharacter);
        }
    }

    public void HighlightButton(int buttonIndex)
    {
        // 모든 버튼 하이라이트 제거
        for (int i = 0; i < characterButtons.Length; i++)
        {
            Image img = characterButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = Color.white;
        }

        // 선택된 버튼 하이라이트
        if (buttonIndex >= 0 && buttonIndex < characterButtons.Length)
        {
            Image img = characterButtons[buttonIndex].GetComponent<Image>();
            if (img != null)
                img.color = Color.yellow;
        }
    }

    public void ShowCharacterInfo(CharacterData character)
    {
        if (characterInfoPanel == null) return;

        characterInfoPanel.SetActive(true);
        
        if (characterNameText != null)
            characterNameText.text = character.characterName;
            
        if (characterDescText != null)
            characterDescText.text = $"HP: {character.hp}\n공격력: {character.attackPower}\n공격 패턴: {character.attackPattern}";
    }

    public void ShowBattlePreview(PreviewResult preview)
    {
        if (battlePreviewPanel == null || previewText == null) return;

        battlePreviewPanel.SetActive(true);
        
        string previewMessage = "";
        if (!preview.canPlace)
        {
            previewMessage = "이곳에 배치할 수 없습니다!";
        }
        else if (preview.willKillDefender && preview.willDie)
        {
            previewMessage = "서로 제거됩니다!";
        }
        else if (preview.willKillDefender)
        {
            previewMessage = "적을 제거하고 배치됩니다!";
        }
        else if (preview.willDie)
        {
            previewMessage = "배치되지만 제거됩니다!";
        }
        else
        {
            previewMessage = "안전하게 배치됩니다!";
        }
        
        previewText.text = previewMessage;
    }

    public void HideBattlePreview()
    {
        if (battlePreviewPanel != null)
            battlePreviewPanel.SetActive(false);
    }

    // 배틀 시작 버튼 제거됨 - 로비에서 바로 배틀씬 진입 시 자동 시작
    // void OnStartBattleClick() - 더 이상 사용되지 않음

    void OnRestartClick()
    {
        // 게임 재시작
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel == null || winnerText == null) return;

        gameOverPanel.SetActive(true);
        
        // 승자 결정
        string winner = "";
        string resultIcon = "";
        
        if (GameManager.Instance.xTeamScore > GameManager.Instance.oTeamScore)
        {
            winner = "X팀 승리!";
            resultIcon = "🏆";
        }
        else if (GameManager.Instance.oTeamScore > GameManager.Instance.xTeamScore)
        {
            winner = "O팀 승리!";
            resultIcon = "🏆";
        }
        else
        {
            winner = "무승부!";
            resultIcon = "🤝";
        }

        // A타일과 B타일 점유 현황 표시
        var aTileCount = GameManager.Instance.GetTileTeamCount(0);
        var bTileCount = GameManager.Instance.GetTileTeamCount(1);

        winnerText.text = $"{resultIcon} {winner}\n\n" +
                         $"최종 점수: X팀 {GameManager.Instance.xTeamScore}점 - O팀 {GameManager.Instance.oTeamScore}점\n\n" +
                         $"A타일: {aTileCount.xCount} vs {aTileCount.oCount}\n" +
                         $"B타일: {bTileCount.xCount} vs {bTileCount.oCount}\n\n" +
                         $"총 {GameManager.Instance.currentTurnNumber-1}턴 진행";
    }

    // 캐릭터 버튼에 랜덤 캐릭터 표시
    public void UpdateCharacterButtons()
    {
        Debug.Log("🔄 UpdateCharacterButtons 호출됨");
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ GameManager.Instance가 null입니다!");
            return;
        }
        
        List<CharacterData> currentHand = GameManager.Instance.GetCurrentHand();
        Debug.Log($"📋 현재 손패: {(currentHand != null ? currentHand.Count : 0)}장");

        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (characterButtons[i] != null)
            {
                bool hasCharacter = i < currentHand.Count;
                characterButtons[i].SetActive(hasCharacter);
                
                if (hasCharacter && buttonImages != null && i < buttonImages.Length && buttonImages[i] != null)
                {
                    CharacterData character = currentHand[i];
                    
                    // 캐릭터 이미지 설정
                    if (character.characterIcon != null)
                    {
                        buttonImages[i].sprite = character.characterIcon;
                        Debug.Log($"✅ 캐릭터 '{character.characterName}' 아이콘 설정됨");
                    }
                    else
                    {
                        // 캐릭터 아이콘이 없을 때 기본 스프라이트 생성
                        buttonImages[i].sprite = CreateDefaultCharacterSprite(character.characterName);
                        Debug.Log($"🎨 캐릭터 '{character.characterName}' 기본 스프라이트 생성됨");
                    }
                    
                    // 캐릭터 이름 설정
                    if (buttonTexts != null && i < buttonTexts.Length && buttonTexts[i] != null)
                    {
                        buttonTexts[i].text = character.characterName;
                    }
                }
                else if (buttonImages != null && i < buttonImages.Length && buttonImages[i] != null)
                {
                    // 캐릭터가 없는 버튼은 이미지를 null로 설정
                    buttonImages[i].sprite = null;
                }
            }
        }
    }
    
    /// <summary>
    /// 캐릭터 아이콘이 없을 때 사용할 기본 스프라이트 생성
    /// </summary>
    private Sprite CreateDefaultCharacterSprite(string characterName)
    {
        // 캐릭터 이름에 따라 다른 색상 사용
        Color color = GetCharacterColor(characterName);
        
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        // 테두리가 있는 단색 스프라이트 생성
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                if (x < 2 || x >= 62 || y < 2 || y >= 62)
                {
                    pixels[x + y * 64] = Color.black; // 테두리
                }
                else
                {
                    pixels[x + y * 64] = color; // 내부 색상
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
    }
    
    /// <summary>
    /// 캐릭터 이름에 따라 고유한 색상 반환
    /// </summary>
    private Color GetCharacterColor(string characterName)
    {
        if (string.IsNullOrEmpty(characterName)) return Color.gray;
        
        // 캐릭터 이름의 해시코드를 기반으로 색상 생성
        int hash = characterName.GetHashCode();
        float hue = (hash % 360) / 360f;
        return Color.HSVToRGB(hue, 0.7f, 0.9f);
    }
}
}