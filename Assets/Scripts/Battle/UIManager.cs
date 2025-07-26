using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<UIManager>();
            return instance;
        }
    }

    [Header("게임 상태 UI")]
    public TextMeshProUGUI currentTurnText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gameStateText;

    [Header("캐릭터 선택 버튼")]
    public GameObject[] characterButtons = new GameObject[4];
    public Image[] buttonImages;
    public TextMeshProUGUI[] buttonTexts;

    [Header("게임 준비 UI")]
    public GameObject preparationPanel;
    public Button startBattleButton;
    public TextMeshProUGUI xTeamCountText;
    public TextMeshProUGUI oTeamCountText;

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
        InitializeUI();
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

        if (startBattleButton != null)
            startBattleButton.onClick.AddListener(OnStartBattleClick);

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

        // 게임 상태 표시
        if (gameStateText != null)
        {
            switch (GameManager.Instance.currentState)
            {
                case GameManager.GameState.Preparation:
                    gameStateText.text = "준비 단계";
                    break;
                case GameManager.GameState.Battle:
                    gameStateText.text = "배틀 진행 중";
                    break;
                case GameManager.GameState.GameOver:
                    gameStateText.text = "게임 종료";
                    break;
            }
        }

        // 패널 활성화/비활성화
        UpdatePanels();
    }

    void UpdatePanels()
    {
        bool isPreparation = GameManager.Instance.currentState == GameManager.GameState.Preparation;
        bool isBattle = GameManager.Instance.currentState == GameManager.GameState.Battle;
        bool isGameOver = GameManager.Instance.currentState == GameManager.GameState.GameOver;

        if (preparationPanel != null)
            preparationPanel.SetActive(isPreparation);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(isGameOver);

        // 캐릭터 버튼 활성화
        foreach (GameObject btn in characterButtons)
        {
            if (btn != null)
                btn.SetActive(isBattle);
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

    void OnStartBattleClick()
    {
        // 준비 완료 확인
        if (GameManager.Instance.xTeamPool.Count >= 10 && GameManager.Instance.oTeamPool.Count >= 10)
        {
            GameManager.Instance.currentState = GameManager.GameState.Battle;
            UpdateUI();
        }
        else
        {
            Debug.Log("양 팀 모두 10개의 캐릭터를 선택해야 합니다!");
        }
    }

    void OnRestartClick()
    {
        // 게임 재시작
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel == null || winnerText == null) return;

        gameOverPanel.SetActive(true);
        
        string result = "";
        if (GameManager.Instance.xTeamScore > GameManager.Instance.oTeamScore)
        {
            result = "X팀 승리! (2-0)";
        }
        else if (GameManager.Instance.oTeamScore > GameManager.Instance.xTeamScore)
        {
            result = "O팀 승리! (0-2)";
        }
        else
        {
            result = "무승부! (1-1)";
        }
        
        winnerText.text = result;
    }

    // 캐릭터 버튼에 랜덤 캐릭터 표시
    public void UpdateCharacterButtons()
    {
        List<CharacterData> currentPool = GameManager.Instance.currentTurn == GameManager.Team.X ? 
                                          GameManager.Instance.xTeamPool : 
                                          GameManager.Instance.oTeamPool;

        for (int i = 0; i < characterButtons.Length && i < buttonImages.Length; i++)
        {
            if (currentPool.Count > 0)
            {
                // 랜덤 캐릭터 선택 (실제로는 버튼 클릭 시 선택)
                CharacterData randomChar = currentPool[Random.Range(0, currentPool.Count)];
                
                if (buttonImages[i] != null && randomChar.characterIcon != null)
                    buttonImages[i].sprite = randomChar.characterIcon;
                    
                if (buttonTexts[i] != null)
                    buttonTexts[i].text = "?"; // 클릭 전까지는 물음표
            }
        }
    }
}