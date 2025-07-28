using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using GuildMaster.Data; // CharacterDatabaseSO 사용을 위해 추가

namespace TwelveGame.Battle
{
    /// <summary>
    /// Twelve Game의 핵심 게임 관리자
    /// 게임 상태, 턴 관리, 보드 상태, 점수 등을 총괄 관리합니다
    /// </summary>
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
    
    [Header("턴 관리")]
    public int currentTurnNumber = 1; // 현재 턴 수 (1턴부터 시작)
    public int maxTurns = 20; // 최대 턴 수 (밸런싱 조정)
    
    [Header("시작 설정")]
    [Tooltip("배틀씬 진입 시 자동으로 게임을 시작할지 여부")]
    public bool autoStartGame = true; // 로비에서 배틀씬 전환 시 바로 시작
    [Tooltip("게임 시작 전 대기 시간 (초)")]
    public float startDelay = 0f;

    [Header("보드 설정")]
    public const int BOARD_WIDTH = 6;
    public const int BOARD_HEIGHT = 3;
    public const int BOARD_COUNT = 2; // A, B 두 개의 보드

    // 보드 상태 [보드인덱스(0=A, 1=B), x, y]
    public Character[,,] boardState = new Character[BOARD_COUNT, BOARD_WIDTH, BOARD_HEIGHT];

    [Header("캐릭터 데이터베이스")]
    [SerializeField] private CharacterDatabaseSO characterDatabase;
    [SerializeField] private bool loadAllCharactersAtStart = true; // 배틀 시작 시 모든 캐릭터 제공
    
    [Header("캐릭터 풀")]
    public List<CharacterData> xTeamPool = new List<CharacterData>();
    public List<CharacterData> oTeamPool = new List<CharacterData>();
    
    [Header("캐릭터 덱 시스템")]
    private List<CharacterData> xTeamDeck = new List<CharacterData>(); // X팀 덱 (무작위 순서)
    private List<CharacterData> oTeamDeck = new List<CharacterData>(); // O팀 덱 (무작위 순서)
    private List<CharacterData> xTeamHand = new List<CharacterData>(); // X팀 현재 손패 (4장)
    private List<CharacterData> oTeamHand = new List<CharacterData>(); // O팀 현재 손패 (4장)
    private CharacterData xTeamNextCard; // X팀 다음 카드
    private CharacterData oTeamNextCard; // O팀 다음 카드
    
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
        // CharacterDatabaseSO 로드 및 초기화
        LoadCharacterDatabase();
        
        if (autoStartGame)
        {
            // 자동 시작 모드
            if (startDelay > 0)
            {
                StartCoroutine(DelayedGameStart());
            }
            else
            {
                StartGameImmediately();
            }
        }
        else
        {
            // 수동 시작 모드 - 대기 상태
            currentState = GameState.Preparation;
            Debug.Log("게임이 대기 상태입니다. StartGameManually()를 호출하여 시작하세요.");
            UpdateUI();
        }
    }
    
    System.Collections.IEnumerator DelayedGameStart()
    {
        Debug.Log($"게임이 {startDelay}초 후에 자동으로 시작됩니다...");
        yield return new WaitForSeconds(startDelay);
        StartGameImmediately();
    }
    
    void StartGameImmediately()
    {
        InitializeGame();
        currentState = GameState.Preparation;
        UpdateUI();
        Debug.Log("게임이 자동으로 시작되었습니다 (Preparation 상태)");
    }
    
    [ContextMenu("게임 수동 시작")]
    public void StartGameManually()
    {
        if (currentState == GameState.Preparation && !autoStartGame)
        {
            InitializeGame();
            currentState = GameState.Preparation;
            UpdateUI();
            Debug.Log("게임이 수동으로 시작되었습니다 (Preparation 상태)");
        }
        else
        {
            Debug.LogWarning("게임을 시작할 수 없습니다. 현재 상태: " + currentState);
        }
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

        // 캐릭터 풀 초기화 (CharacterDatabaseSO에서 로드)
        InitializeCharacterPools();

        // UI 업데이트
        UpdateUI();
    }

    /// <summary>
    /// CharacterDatabaseSO에서 캐릭터 데이터 로드
    /// </summary>
    private void LoadCharacterDatabase()
    {
        // 1. Inspector에서 할당된 데이터베이스 확인
        if (characterDatabase == null)
        {
            Debug.Log("🔍 CharacterDatabaseSO를 자동으로 찾는 중...");
            
            // 2. Resources 폴더에서 로드 시도
            characterDatabase = Resources.Load<CharacterDatabaseSO>("Data/CharacterDatabase");
            
            if (characterDatabase == null)
            {
                // 3. 기본 경로에서 로드 시도
                #if UNITY_EDITOR
                characterDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>("Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset");
                
                // 4. Generated 폴더에서도 시도
                if (characterDatabase == null)
                {
                    characterDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>("Assets/Characters/Generated/CharacterDatabaseSO.asset");
                }
                #endif
            }
        }

        if (characterDatabase == null)
        {
            Debug.LogError("❌ CharacterDatabaseSO를 찾을 수 없습니다!");
            Debug.LogError("🔧 해결 방법: Unity 메뉴 → Twelve → ⚡ Quick Tools → 🎮 배틀용 캐릭터 DB 생성");
            CreateEmptyCharacterPools();
            return;
        }
        else
        {
            Debug.Log($"✅ CharacterDatabaseSO 발견: {characterDatabase.name}");
        }

        // 데이터베이스 초기화
        characterDatabase.Initialize();

        // 배틀 시작 시 모든 캐릭터 제공
        if (loadAllCharactersAtStart)
        {
            LoadAllCharactersForBattle();
        }

        Debug.Log($"✅ 캐릭터 데이터베이스 로드 완료: {characterDatabase.GetAllTacticalCharacters().Count}개 캐릭터");
    }

    /// <summary>
    /// 배틀용으로 모든 캐릭터를 양 팀에 제공
    /// </summary>
    private void LoadAllCharactersForBattle()
    {
        if (characterDatabase == null) return;

        var allCharacters = characterDatabase.GetAllTacticalCharacters();
        
        if (allCharacters.Count == 0)
        {
            Debug.LogWarning("⚠️ CharacterDatabaseSO에 캐릭터가 없습니다. 자동으로 생성합니다...");
            CreateTestCharacters(); // 테스트용 캐릭터 생성
            
            // 생성 후 다시 로드 시도
            if (characterDatabase != null)
            {
                characterDatabase.Initialize();
                var newCharacters = characterDatabase.GetAllTacticalCharacters();
                if (newCharacters.Count > 0)
                {
                    xTeamPool.Clear();
                    oTeamPool.Clear();
                    xTeamPool.AddRange(newCharacters);
                    oTeamPool.AddRange(newCharacters);
                    Debug.Log($"🎮 자동 생성 후 캐릭터 풀 초기화 완료: 각 팀 {newCharacters.Count}개 캐릭터");
                    return;
                }
            }
            
            Debug.LogError("❌ 캐릭터 자동 생성 실패. 수동으로 생성해주세요.");
            return;
        }

        // 양 팀 모두에게 동일한 캐릭터 풀 제공
        xTeamPool.Clear();
        oTeamPool.Clear();
        
        xTeamPool.AddRange(allCharacters);
        oTeamPool.AddRange(allCharacters);

        Debug.Log($"🎮 배틀 캐릭터 풀 초기화 완료: 각 팀 {allCharacters.Count}개 캐릭터");
    }

    /// <summary>
    /// 빈 캐릭터 풀 생성 (데이터베이스가 없을 때)
    /// </summary>
    private void CreateEmptyCharacterPools()
    {
        xTeamPool.Clear();
        oTeamPool.Clear();
        Debug.LogWarning("⚠️ 캐릭터 데이터베이스가 없어서 빈 풀로 시작합니다.");
    }

    /// <summary>
    /// 테스트용 캐릭터 생성 (데이터가 없을 때)
    /// </summary>
    private void CreateTestCharacters()
    {
        Debug.Log("🔧 테스트용 캐릭터 생성 중...");
        
        // RuntimeCharacterGenerator 사용하여 테스트 캐릭터 생성
        var generator = FindObjectOfType<RuntimeCharacterGenerator>();
        if (generator != null)
        {
            #if UNITY_EDITOR
            generator.GenerateTestCharacters();
            // 생성 후 다시 로드 시도
            if (characterDatabase != null)
            {
                LoadAllCharactersForBattle();
            }
            #endif
        }
        else
        {
            Debug.LogWarning("⚠️ RuntimeCharacterGenerator를 찾을 수 없습니다. 수동으로 캐릭터를 생성하거나 CSV 데이터를 임포트하세요.");
        }
    }

    void InitializeCharacterPools()
    {
        // CharacterDatabaseSO에서 이미 로드됨
        if (xTeamPool.Count == 0 || oTeamPool.Count == 0)
        {
            Debug.LogWarning("⚠️ 캐릭터 풀이 비어있습니다. CharacterDatabaseSO를 확인하세요.");
            return;
        }

        // 덱과 손패 초기화
        InitializeDecksAndHands();
    }

    void InitializeDecksAndHands()
    {
        // X팀 덱 생성 및 셔플
        xTeamDeck.Clear();
        xTeamDeck.AddRange(xTeamPool);
        ShuffleDeck(xTeamDeck);

        // O팀 덱 생성 및 셔플
        oTeamDeck.Clear();
        oTeamDeck.AddRange(oTeamPool);
        ShuffleDeck(oTeamDeck);

        // 초기 손패 뽑기 (각 팀당 4장)
        DrawInitialHands();

        Debug.Log($"덱 초기화 완료 - X팀 덱: {xTeamDeck.Count}장, O팀 덱: {oTeamDeck.Count}장");
    }

    void ShuffleDeck(List<CharacterData> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CharacterData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    void DrawInitialHands()
    {
        // X팀 손패 뽑기
        xTeamHand.Clear();
        for (int i = 0; i < 4 && xTeamDeck.Count > 0; i++)
        {
            xTeamHand.Add(xTeamDeck[0]);
            xTeamDeck.RemoveAt(0);
        }

        // X팀 다음 카드 뽑기
        if (xTeamDeck.Count > 0)
        {
            xTeamNextCard = xTeamDeck[0];
            xTeamDeck.RemoveAt(0);
        }

        // O팀 손패 뽑기
        oTeamHand.Clear();
        for (int i = 0; i < 4 && oTeamDeck.Count > 0; i++)
        {
            oTeamHand.Add(oTeamDeck[0]);
            oTeamDeck.RemoveAt(0);
        }

        // O팀 다음 카드 뽑기
        if (oTeamDeck.Count > 0)
        {
            oTeamNextCard = oTeamDeck[0];
            oTeamDeck.RemoveAt(0);
        }

        Debug.Log($"초기 손패 뽑기 완료 - X팀: {xTeamHand.Count}장, O팀: {oTeamHand.Count}장");
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
            
            // 사용한 캐릭터를 손패에서 제거하고 다음 카드 뽑기
            UseCharacterFromHand(selectedButtonIndex);
        }

        // 선택 초기화
        selectedCharacter = null;
        selectedButtonIndex = -1;

        // UI 업데이트
        BattleUIManager.Instance.UpdateCharacterButtons();
        BattleUIManager.Instance.HighlightButton(-1);
        
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
        
        // X팀 턴이 시작될 때마다 턴 수 증가 (한 라운드 완료)
        if (currentTurn == Team.X)
        {
            currentTurnNumber++;
            Debug.Log($"=== {currentTurnNumber}턴 시작 ===");
        }

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

    // StartBattle 메서드는 더 이상 사용되지 않음 (자동 시작으로 변경)
    // public void StartBattle() - 배틀은 씬 진입 시 자동으로 시작됨

    [System.Serializable]
    public struct TileTeamCount
    {
        public int xCount;
        public int oCount;
        
        public TileTeamCount(int x, int o)
        {
            xCount = x;
            oCount = o;
        }
    }

    public TileTeamCount GetTileTeamCount(int boardIndex)
    {
        int xCount = 0;
        int oCount = 0;

        // 해당 보드의 모든 타일을 확인
        for (int x = 0; x < BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                Character character = boardState[boardIndex, x, y];
                if (character != null)
                {
                    if (character.team == Team.X)
                        xCount++;
                    else if (character.team == Team.O)
                        oCount++;
                }
            }
        }

        return new TileTeamCount(xCount, oCount);
    }

    public CharacterData GetNextCharacter()
    {
        // 현재 턴의 다음 캐릭터 반환
        return currentTurn == Team.X ? xTeamNextCard : oTeamNextCard;
    }

    public List<CharacterData> GetCurrentHand()
    {
        // 현재 턴의 손패 반환
        return currentTurn == Team.X ? xTeamHand : oTeamHand;
    }

    public CharacterData GetCharacterFromHand(int index)
    {
        List<CharacterData> currentHand = GetCurrentHand();
        if (index >= 0 && index < currentHand.Count)
        {
            return currentHand[index];
        }
        return null;
    }

    void UseCharacterFromHand(int handIndex)
    {
        List<CharacterData> currentHand = GetCurrentHand();
        List<CharacterData> currentDeck = currentTurn == Team.X ? xTeamDeck : oTeamDeck;
        
        if (handIndex < 0 || handIndex >= currentHand.Count) return;

        // 손패에서 사용한 캐릭터 제거
        CharacterData usedCharacter = currentHand[handIndex];
        currentHand.RemoveAt(handIndex);

        // 다음 카드를 손패로 이동
        if (currentTurn == Team.X && xTeamNextCard != null)
        {
            currentHand.Add(xTeamNextCard);
            
            // 새로운 다음 카드 뽑기
            if (xTeamDeck.Count > 0)
            {
                xTeamNextCard = xTeamDeck[0];
                xTeamDeck.RemoveAt(0);
            }
            else
            {
                xTeamNextCard = null;
            }
        }
        else if (currentTurn == Team.O && oTeamNextCard != null)
        {
            currentHand.Add(oTeamNextCard);
            
            // 새로운 다음 카드 뽑기
            if (oTeamDeck.Count > 0)
            {
                oTeamNextCard = oTeamDeck[0];
                oTeamDeck.RemoveAt(0);
            }
            else
            {
                oTeamNextCard = null;
            }
        }

        Debug.Log($"{currentTurn}팀이 {usedCharacter.characterName} 사용. 손패: {currentHand.Count}장, 덱: {currentDeck.Count}장");
    }

    public void OnCharacterButtonClick(int buttonIndex)
    {
        if (currentState != GameState.Battle && currentState != GameState.Preparation) return;

        // 현재 손패에서 캐릭터 선택
        CharacterData character = GetCharacterFromHand(buttonIndex);
        
        if (character != null)
        {
            selectedCharacter = character;
            selectedButtonIndex = buttonIndex;
            
            // UI 업데이트
                    if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.HighlightButton(buttonIndex);
            BattleUIManager.Instance.ShowCharacterInfo(selectedCharacter);
        }
            
            Debug.Log($"{currentTurn} 팀이 {selectedCharacter.characterName} 선택 (손패 {buttonIndex}번째)");
        }
        else
        {
            Debug.LogWarning($"유효하지 않은 손패 인덱스: {buttonIndex}");
        }
    }
    
    bool CheckGameEnd()
    {
        // 35턴 도달 시 게임 종료
        if (currentTurnNumber > maxTurns)
        {
            Debug.Log($"최대 턴 수({maxTurns}턴) 도달로 게임 종료");
            return true;
        }
        
        // 더 이상 배치할 수 없는 상황이면 종료
        if (!CanAnyonePlaceCharacter())
        {
            Debug.Log("더 이상 배치할 수 없어 게임 종료");
            return true;
        }
        
        return false;
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
        
        // 20턴까지 살아남은 캐릭터들의 승리 카운트 증가
        RecordSurvivorVictories();
        
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.ShowGameOver();
        }
        
        Debug.Log($"게임 종료! 최종 점수 - X팀: {xTeamScore}, O팀: {oTeamScore}");
    }

    void RecordSurvivorVictories()
    {
        // 20턴까지 도달한 경우에만 승리 기록
        if (currentTurnNumber >= maxTurns)
        {
            // 보드에 살아남은 모든 캐릭터들의 승리 카운트 증가
            for (int b = 0; b < BOARD_COUNT; b++)
            {
                for (int x = 0; x < BOARD_WIDTH; x++)
                {
                    for (int y = 0; y < BOARD_HEIGHT; y++)
                    {
                        Character survivor = boardState[b, x, y];
                        if (survivor != null && survivor.characterData != null)
                        {
                            string characterId = survivor.characterData.characterId;
                            if (!string.IsNullOrEmpty(characterId))
                            {
                                int currentWins = PlayerPrefs.GetInt($"CharacterWins_{characterId}", 0);
                                currentWins++;
                                PlayerPrefs.SetInt($"CharacterWins_{characterId}", currentWins);
                                
                                Debug.Log($"{survivor.characterData.characterName} 승리 카운트: {currentWins}/100");
                                
                                // 100승 달성 시 특별 메시지
                                if (currentWins == 100)
                                {
                                    Debug.Log($"🎉 {survivor.characterData.characterName}이(가) 100승을 달성했습니다! 엔딩을 확인해보세요!");
                                }
                            }
                        }
                    }
                }
            }
            PlayerPrefs.Save();
        }
    }

    void CalculateFinalScore()
    {
        // A타일과 B타일별 점유율 계산
        var aTileCount = GetTileTeamCount(0); // A타일
        var bTileCount = GetTileTeamCount(1); // B타일
        
        int xTeamPoints = 0;
        int oTeamPoints = 0;
        
        // A타일 승부 판정
        if (aTileCount.xCount > aTileCount.oCount)
        {
            xTeamPoints++;
            Debug.Log($"A타일: X팀 승리 ({aTileCount.xCount} vs {aTileCount.oCount})");
        }
        else if (aTileCount.oCount > aTileCount.xCount)
        {
            oTeamPoints++;
            Debug.Log($"A타일: O팀 승리 ({aTileCount.oCount} vs {aTileCount.xCount})");
        }
        else
        {
            Debug.Log($"A타일: 무승부 ({aTileCount.xCount} vs {aTileCount.oCount})");
        }
        
        // B타일 승부 판정
        if (bTileCount.xCount > bTileCount.oCount)
        {
            xTeamPoints++;
            Debug.Log($"B타일: X팀 승리 ({bTileCount.xCount} vs {bTileCount.oCount})");
        }
        else if (bTileCount.oCount > bTileCount.xCount)
        {
            oTeamPoints++;
            Debug.Log($"B타일: O팀 승리 ({bTileCount.oCount} vs {bTileCount.xCount})");
        }
        else
        {
            Debug.Log($"B타일: 무승부 ({bTileCount.xCount} vs {bTileCount.oCount})");
        }
        
        // 최종 점수 설정
        xTeamScore = xTeamPoints;
        oTeamScore = oTeamPoints;
        
        Debug.Log($"=== 최종 결과 ===");
        Debug.Log($"X팀: {xTeamPoints}점, O팀: {oTeamPoints}점");
        
        if (xTeamPoints > oTeamPoints)
        {
            Debug.Log("🏆 X팀 승리!");
        }
        else if (oTeamPoints > xTeamPoints)
        {
            Debug.Log("🏆 O팀 승리!");
        }
        else
        {
            Debug.Log("🤝 무승부!");
        }

        // 기존 코드는 이미 위에서 새로운 방식으로 처리됨
    }

    void UpdateUI()
    {
        if (turnText != null)
            turnText.text = $"현재 턴: {currentTurn} 팀";

        if (scoreText != null)
            scoreText.text = $"X: {xTeamScore} - O: {oTeamScore}";
    }
}

    // 캐릭터 인스턴스 클래스 (TwelveGame.Battle 네임스페이스 내부)
    [System.Serializable]
    public class Character : MonoBehaviour
{
    [Header("캐릭터 기본 정보")]
    public CharacterData characterData;
    public GameManager.Team team;
    public int boardIndex;
    public int x;
    public int y;
    public int currentHP;
    
    [Header("애니메이션 컨트롤러")]
    public Animator spriteAnimator; // 기존 스프라이트 애니메이션용
            public GuildMaster.Battle.PNGSequenceController pngSequenceController; // PNG 시퀀스 애니메이션용
    
    private void Awake()
    {
        InitializeAnimationControllers();
    }
    
    private void Start()
    {
        if (characterData != null)
        {
            currentHP = characterData.maxHP;
            SetupAnimation();
        }
    }
    
    /// <summary>
    /// 애니메이션 컨트롤러 초기화
    /// </summary>
    private void InitializeAnimationControllers()
    {
        // 기존 Animator 컴포넌트 찾기
        if (spriteAnimator == null)
        {
            spriteAnimator = GetComponent<Animator>();
        }
        
        // PNGSequenceController 컴포넌트 찾기 또는 생성
        if (pngSequenceController == null)
        {
            pngSequenceController = GetComponent<GuildMaster.Battle.PNGSequenceController>();
            if (pngSequenceController == null && characterData != null && characterData.animationType == AnimationType.PNGSequence)
            {
                pngSequenceController = gameObject.AddComponent<GuildMaster.Battle.PNGSequenceController>();
            }
        }
    }
    
    /// <summary>
    /// 캐릭터 데이터에 따른 애니메이션 설정
    /// </summary>
    private void SetupAnimation()
    {
        if (characterData == null) return;
        
        switch (characterData.animationType)
        {
            case AnimationType.Sprite:
                // 스프라이트 애니메이션 사용
                if (spriteAnimator != null)
                {
                    spriteAnimator.enabled = true;
                }
                            if (pngSequenceController != null)
            {
                pngSequenceController.enabled = false;
            }
                break;
                

                
            case AnimationType.PNGSequence:
                // PNG 시퀀스 애니메이션 사용 (권장!)
                if (spriteAnimator != null)
                {
                    spriteAnimator.enabled = false;
                }
                            if (pngSequenceController != null)
            {
                pngSequenceController.enabled = true;
                pngSequenceController.SetCharacterData(characterData);
            }
                break;
        }
    }
    
    /// <summary>
    /// 공격 애니메이션 재생 (attack.mp4)
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (characterData == null) return;
        
        switch (characterData.animationType)
        {
            case AnimationType.Sprite:
                if (spriteAnimator != null)
                {
                    spriteAnimator.SetTrigger("Attack");
                }
                break;
                
            case AnimationType.PNGSequence:
                        if (pngSequenceController != null)
        {
            pngSequenceController.PlayAttackAnimation();
        }
                break;
        }
    }
    
    /// <summary>
    /// 스킬 애니메이션 재생 (attack과 동일)
    /// </summary>
    public void PlaySkillAnimation()
    {
        // 스킬은 attack 애니메이션과 동일하게 처리
        PlayAttackAnimation();
    }
    
    /// <summary>
    /// 이동 애니메이션 재생 (idle 재생)
    /// </summary>
    public void PlayWalkAnimation()
    {
        // 이동도 idle 동영상으로 처리
        PlayIdleAnimation();
    }
    
    /// <summary>
    /// 대기 애니메이션 재생 (idle.mp4)
    /// </summary>
    public void PlayIdleAnimation()
    {
        if (characterData == null) return;
        
        switch (characterData.animationType)
        {
            case AnimationType.Sprite:
                if (spriteAnimator != null)
                {
                    spriteAnimator.SetBool("IsWalking", false);
                }
                break;
                
            case AnimationType.PNGSequence:
                        if (pngSequenceController != null)
        {
            pngSequenceController.PlayAnimation(GuildMaster.Battle.PNGSequenceController.CharacterAnimationState.Idle);
        }
                break;
        }
    }
    
    /// <summary>
    /// 죽음 효과 재생 (연기로 사라짐)
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (characterData == null) return;
        
        switch (characterData.animationType)
        {
            case AnimationType.Sprite:
                if (spriteAnimator != null)
                {
                    spriteAnimator.SetTrigger("Death");
                }
                // 스프라이트 캐릭터도 연기 효과 추가 가능
                StartCoroutine(PlaySpriteDeathEffect());
                break;
                
            case AnimationType.PNGSequence:
                        if (pngSequenceController != null)
        {
            pngSequenceController.PlayDeathEffect(); // 연기 효과로 사라짐
        }
                break;
        }
    }
    
    /// <summary>
    /// 스프라이트 캐릭터용 죽음 효과
    /// </summary>
    private IEnumerator PlaySpriteDeathEffect()
    {
        yield return new WaitForSeconds(1.0f); // 죽음 애니메이션 재생 시간
        
        // 연기 파티클 효과 (있다면)
        ParticleSystem smokeParticle = GetComponentInChildren<ParticleSystem>();
        if (smokeParticle != null)
        {
            smokeParticle.Play();
            yield return new WaitForSeconds(1.0f);
        }
        
        // 페이드 아웃
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            float fadeTime = 1.0f;
            float elapsed = 0;
            Color originalColor = spriteRenderer.color;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1.0f, 0.0f, elapsed / fadeTime);
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
        }
        
        gameObject.SetActive(false);
        Debug.Log($"[Character] {gameObject.name}: Sprite death effect completed");
    }
    
    /// <summary>
    /// 캐릭터 데이터 설정 (런타임에서 변경 시 사용)
    /// </summary>
    public void SetCharacterData(CharacterData newData)
    {
        characterData = newData;
        if (characterData != null)
        {
            currentHP = characterData.maxHP;
            SetupAnimation();
        }
    }
}
}