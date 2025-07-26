using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<GameManager>();
            return instance;
        }
    }

    // 게임 상태
    public enum GameState
    {
        Preparation,    // 캐릭터 선택 단계
        Battle,         // 배틀 진행 중
        GameOver        // 게임 종료
    }

    // 팀 정의
    public enum Team
    {
        X,
        O
    }

    [Header("게임 설정")]
    public GameState currentState = GameState.Preparation;
    public Team currentTurn;
    public bool isFirstPlayer = true; // 첫 턴 랜덤 결정용

    [Header("보드 설정")]
    public const int BOARD_WIDTH = 6;
    public const int BOARD_HEIGHT = 3;
    public const int BOARD_COUNT = 2; // A, B 두 개의 보드

    // 보드 상태 [보드인덱스(0=A, 1=B), x, y]
    public Character[,,] boardState = new Character[BOARD_COUNT, BOARD_WIDTH, BOARD_HEIGHT];

    [Header("캐릭터 풀")]
    public List<CharacterData> xTeamPool = new List<CharacterData>();
    public List<CharacterData> oTeamPool = new List<CharacterData>();
    
    // 사용된 캐릭터 추적
    public Dictionary<CharacterData, int> xTeamUsageCount = new Dictionary<CharacterData, int>();
    public Dictionary<CharacterData, int> oTeamUsageCount = new Dictionary<CharacterData, int>();
    public int maxUsagePerCharacter = 1; // 각 캐릭터당 최대 사용 횟수

    [Header("점수")]
    public int xTeamScore = 0;
    public int oTeamScore = 0;

    [Header("UI 참조")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI scoreText;
    public GameObject[] characterButtons; // 4개의 캐릭터 선택 버튼

    // 현재 선택된 캐릭터
    public CharacterData selectedCharacter;
    private int selectedButtonIndex = -1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        InitializeGame();
        
        // 준비 단계로 시작
        currentState = GameState.Preparation;
        UpdateUI();
    }

    void InitializeGame()
    {
        // 첫 턴 랜덤 결정
        currentTurn = Random.Range(0, 2) == 0 ? Team.X : Team.O;
        
        // 보드 초기화
        for (int b = 0; b < BOARD_COUNT; b++)
        {
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                for (int y = 0; y < BOARD_HEIGHT; y++)
                {
                    boardState[b, x, y] = null;
                }
            }
        }

        // 캐릭터 풀 초기화 (나중에 CharacterData로 채움)
        InitializeCharacterPools();

        // UI 업데이트
        UpdateUI();
    }

    void InitializeCharacterPools()
    {
        // 캐릭터 데이터는 별도로 정의하고 여기서 풀에 추가
        // Resources 폴더나 ScriptableObject로 관리하는 것을 권장
        
        // 임시로 기본 캐릭터 생성 (실제로는 에디터에서 할당)
        if (xTeamPool.Count == 0)
        {
            Debug.LogWarning("X팀 캐릭터 풀이 비어있습니다. 에디터에서 캐릭터를 할당하세요.");
        }
        
        if (oTeamPool.Count == 0)
        {
            Debug.LogWarning("O팀 캐릭터 풀이 비어있습니다. 에디터에서 캐릭터를 할당하세요.");
        }
    }



    public void OnTileClick(int boardIndex, int x, int y)
    {
        if (currentState != GameState.Battle || selectedCharacter == null) return;

        // 배치 가능 여부 확인
        if (CanPlaceCharacter(boardIndex, x, y))
        {
            PlaceCharacter(boardIndex, x, y);
        }
    }

    public bool CanPlaceCharacter(int boardIndex, int x, int y)
    {
        if (selectedCharacter == null) return false;
        
        Character existingChar = boardState[boardIndex, x, y];
        
        // 빈 자리면 배치 가능
        if (existingChar == null) return true;

        // 같은 팀이면 배치 불가
        if (existingChar.team == currentTurn) return false;

        // 전투 시뮬레이션
        BattleResult result = BattleSystem.Instance.SimulateBattle(selectedCharacter, existingChar);
        return result.canPlace;
    }

    void PlaceCharacter(int boardIndex, int x, int y)
    {
        // 전투 결과 확인
        Character existingChar = boardState[boardIndex, x, y];
        BattleResult battleResult = null;
        
        if (existingChar != null)
        {
            battleResult = BattleSystem.Instance.SimulateBattle(selectedCharacter, existingChar);
            if (!battleResult.canPlace) return;
            
            // 기존 캐릭터 제거
            if (!battleResult.defenderSurvives)
            {
                Destroy(existingChar.gameObject);
                boardState[boardIndex, x, y] = null;
            }
        }
        
        // 새 캐릭터 배치 (공격자가 생존하는 경우만)
        if (battleResult == null || battleResult.attackerSurvives)
        {
            // 캐릭터 오브젝트 생성
            GameObject charObj = new GameObject($"Character_{selectedCharacter.characterName}");
            charObj.transform.position = GetWorldPosition(boardIndex, x, y);
            
            // Character 컴포넌트 추가 및 초기화
            Character newChar = charObj.AddComponent<Character>();
            newChar.characterData = selectedCharacter;
            newChar.team = currentTurn;
            newChar.boardIndex = boardIndex;
            newChar.x = x;
            newChar.y = y;
            newChar.currentHP = selectedCharacter.hp;
            
            boardState[boardIndex, x, y] = newChar;
            
            // 캐릭터 사용 횟수 업데이트
            Dictionary<CharacterData, int> usageCount = currentTurn == Team.X ? xTeamUsageCount : oTeamUsageCount;
            if (usageCount.ContainsKey(selectedCharacter))
                usageCount[selectedCharacter]++;
            else
                usageCount[selectedCharacter] = 1;
            
            // 시각적 표현 추가
            CreateCharacterVisualComponents(charObj, newChar);
            
            // 공격 처리
            BattleSystem.Instance.ProcessCharacterAttack(newChar);
        }

        // 선택 초기화
        selectedCharacter = null;
        selectedButtonIndex = -1;

        // UI 업데이트
        UIManager.Instance.UpdateCharacterButtons();
        UIManager.Instance.HighlightButton(-1);
        
        // 턴 종료
        EndTurn();
    }
    
    void CreateCharacterVisualComponents(GameObject charObj, Character character)
    {
        // 스프라이트 렌더러 추가
        SpriteRenderer sprite = charObj.AddComponent<SpriteRenderer>();
        if (character.characterData.characterIcon != null)
        {
            sprite.sprite = character.characterData.characterIcon;
        }
        else
        {
            // 임시 스프라이트 생성
            sprite.sprite = CreateTempSprite(character.team == Team.X ? Color.blue : Color.red);
        }
        sprite.sortingOrder = 1;
        
        // 팀 텍스트 표시
        GameObject textObj = new GameObject("TeamText");
        textObj.transform.parent = charObj.transform;
        textObj.transform.localPosition = new Vector3(0, 0, -0.1f);
        
        TextMeshPro teamText = textObj.AddComponent<TextMeshPro>();
        teamText.text = character.team == Team.X ? "X" : "O";
        teamText.fontSize = 3;
        teamText.alignment = TextAlignmentOptions.Center;
        teamText.color = Color.white;
        teamText.sortingOrder = 2;
        
        // HP 표시
        GameObject hpObj = new GameObject("HPText");
        hpObj.transform.parent = charObj.transform;
        hpObj.transform.localPosition = new Vector3(0, -0.5f, -0.1f);
        
        TextMeshPro hpText = hpObj.AddComponent<TextMeshPro>();
        hpText.text = character.currentHP.ToString();
        hpText.fontSize = 2;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.color = Color.yellow;
        hpText.sortingOrder = 2;
    }

    void CreateCharacterVisual(Character character)
    {
        // 캐릭터 오브젝트 생성
        GameObject charObj = new GameObject($"Character_{character.characterData.characterName}");
        charObj.transform.position = GetWorldPosition(character.boardIndex, character.x, character.y);
        
        // Character 컴포넌트 추가
        character = charObj.AddComponent<Character>();
        
        // 스프라이트 렌더러 추가
        SpriteRenderer sprite = charObj.AddComponent<SpriteRenderer>();
        if (character.characterData.characterIcon != null)
        {
            sprite.sprite = character.characterData.characterIcon;
        }
        else
        {
            // 임시 스프라이트 생성
            sprite.sprite = CreateTempSprite(character.team == Team.X ? Color.blue : Color.red);
        }
        sprite.sortingOrder = 1;
        
        // 팀 텍스트 표시
        GameObject textObj = new GameObject("TeamText");
        textObj.transform.parent = charObj.transform;
        textObj.transform.localPosition = Vector3.zero;
        
        TextMesh teamText = textObj.AddComponent<TextMesh>();
        teamText.text = character.team == Team.X ? "X" : "O";
        teamText.fontSize = 20;
        teamText.anchor = TextAnchor.MiddleCenter;
        teamText.color = Color.white;
        
        // 보드 매니저에 업데이트 요청
        BoardManager boardManager = FindObjectOfType<BoardManager>();
        if (boardManager != null)
        {
            boardManager.RefreshBoard();
        }
    }
    
    Vector3 GetWorldPosition(int boardIndex, int x, int y)
    {
        // 보드 매니저의 타일 위치 기준으로 계산
        float tileSize = 1.0f;
        float boardSpacing = 2.0f;
        
        float xPos = (x - GameManager.BOARD_WIDTH / 2f + 0.5f) * tileSize;
        float yPos = boardIndex == 0 ? 
            boardSpacing + (y - GameManager.BOARD_HEIGHT / 2f + 0.5f) * tileSize :
            -boardSpacing - GameManager.BOARD_HEIGHT * tileSize + (y - GameManager.BOARD_HEIGHT / 2f + 0.5f) * tileSize;
            
        return new Vector3(xPos, yPos, 0);
    }
    
    Sprite CreateTempSprite(Color color)
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
    }

    void EndTurn()
    {
        // 턴 변경
        currentTurn = currentTurn == Team.X ? Team.O : Team.X;

        // 게임 종료 확인
        if (CheckGameEnd())
        {
            EndGame();
        }
        else
        {
            UpdateUI();
        }
    }

    public void StartBattle()
    {
        if (xTeamPool.Count < 10 || oTeamPool.Count < 10)
        {
            Debug.LogError("양쪽 모두 10개의 캐릭터를 선택해야 합니다.");
            return;
        }
        
        currentState = GameState.Battle;
        
        // 첫 턴 랜덤 결정
        currentTurn = Random.Range(0, 2) == 0 ? Team.X : Team.O;
        
        UpdateUI();
        
        Debug.Log($"배틀 시작! 첫 턴: {currentTurn} 팀");
    }
    

    
    public void OnCharacterButtonClick(int buttonIndex)
    {
        if (currentState != GameState.Battle) return;

        // 버튼에서 랜덤 캐릭터 선택
        List<CharacterData> currentPool = currentTurn == Team.X ? xTeamPool : oTeamPool;
        Dictionary<CharacterData, int> usageCount = currentTurn == Team.X ? xTeamUsageCount : oTeamUsageCount;
        
        // 사용 가능한 캐릭터 필터링
        List<CharacterData> availableCharacters = new List<CharacterData>();
        foreach (var character in currentPool)
        {
            int count = usageCount.ContainsKey(character) ? usageCount[character] : 0;
            if (count < maxUsagePerCharacter)
            {
                availableCharacters.Add(character);
            }
        }
        
        if (availableCharacters.Count == 0)
        {
            Debug.Log("사용 가능한 캐릭터가 없습니다!");
            return;
        }

        selectedCharacter = availableCharacters[Random.Range(0, availableCharacters.Count)];
        selectedButtonIndex = buttonIndex;

        // UI 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HighlightButton(buttonIndex);
            UIManager.Instance.ShowCharacterInfo(selectedCharacter);
        }

        Debug.Log($"선택된 캐릭터: {selectedCharacter.characterName}");
    }
    
    bool CheckGameEnd()
    {
        // 모든 캐릭터가 배치되었는지 확인
        int totalCharacters = 0;
        for (int b = 0; b < BOARD_COUNT; b++)
        {
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                for (int y = 0; y < BOARD_HEIGHT; y++)
                {
                    if (boardState[b, x, y] != null)
                        totalCharacters++;
                }
            }
        }
        
        // 각 팀당 10개씩, 총 20개 캐릭터가 배치되면 종료
        // 또는 더 이상 배치할 수 없는 상황이면 종료
        return totalCharacters >= 20 || !CanAnyonePlaceCharacter();
    }
    
    bool CanAnyonePlaceCharacter()
    {
        // 현재 턴의 팀이 어디든 배치할 수 있는지 확인
        for (int b = 0; b < BOARD_COUNT; b++)
        {
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                for (int y = 0; y < BOARD_HEIGHT; y++)
                {
                    if (boardState[b, x, y] == null || boardState[b, x, y].team != currentTurn)
                        return true;
                }
            }
        }
        return false;
    }
    
    void EndGame()
    {
        currentState = GameState.GameOver;
        CalculateFinalScore();
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver();
        }
        
        Debug.Log($"게임 종료! 최종 점수 - X팀: {xTeamScore}, O팀: {oTeamScore}");
    }

    void CalculateFinalScore()
    {
        // 각 보드별 점수 계산
        int boardAXCount = 0, boardAOCount = 0;
        int boardBXCount = 0, boardBOCount = 0;

        // 보드 A (위쪽) 계산
        for (int x = 0; x < BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                if (boardState[0, x, y] != null)
                {
                    if (boardState[0, x, y].team == Team.X)
                        boardAXCount++;
                    else
                        boardAOCount++;
                }
            }
        }

        // 보드 B (아래쪽) 계산
        for (int x = 0; x < BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                if (boardState[1, x, y] != null)
                {
                    if (boardState[1, x, y].team == Team.X)
                        boardBXCount++;
                    else
                        boardBOCount++;
                }
            }
        }

        // 점수 계산
        xTeamScore = 0;
        oTeamScore = 0;

        // 보드 A 점수
        if (boardAXCount > boardAOCount) xTeamScore++;
        else if (boardAOCount > boardAXCount) oTeamScore++;

        // 보드 B 점수
        if (boardBXCount > boardBOCount) xTeamScore++;
        else if (boardBOCount > boardBXCount) oTeamScore++;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (turnText != null)
            turnText.text = $"현재 턴: {currentTurn} 팀";

        if (scoreText != null)
            scoreText.text = $"X: {xTeamScore} - O: {oTeamScore}";
    }
}

// 캐릭터 인스턴스 클래스
[System.Serializable]
public class Character : MonoBehaviour
{
    public CharacterData characterData;
    public GameManager.Team team;
    public int boardIndex;
    public int x;
    public int y;
    public int currentHP;
}