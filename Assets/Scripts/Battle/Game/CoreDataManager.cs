using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CoreDataManager : MonoBehaviour
{
    private static CoreDataManager instance;
    public static CoreDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CoreDataManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CoreDataManager");
                    instance = go.AddComponent<CoreDataManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    [Header("캐릭터 데이터베이스")]
    public List<CharacterDefinition> allCharacters = new List<CharacterDefinition>();
    
    [Header("공격 패턴 정의")]
    public List<AttackPatternData> attackPatterns = new List<AttackPatternData>();
    
    [Header("게임 설정")]
    public GameConfiguration gameConfig;
    
    [Header("현재 게임 세션")]
    public GameSession currentSession;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 기본 데이터 초기화
        InitializeDefaultData();
    }
    
    // 캐릭터 정의
    [System.Serializable]
    public class CharacterDefinition
    {
        public int characterId;
        public string characterName;
        public Sprite characterIcon;
        public AttackPatternType attackType;
        public int attackRange;  // 패턴의 최대 범위
        public string description;
        public CharacterRarity rarity;
        
        // 커스텀 공격 패턴 (attackType이 Custom일 때 사용)
        public List<Vector2Int> customPattern = new List<Vector2Int>();
    }
    
    // 캐릭터 희귀도
    public enum CharacterRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }
    
    // 공격 패턴 타입
    public enum AttackPatternType
    {
        Cross,          // 십자 (상하좌우)
        Diagonal,       // 대각선 (X자)
        Square3x3,      // 3x3 정사각형
        Square5x5,      // 5x5 정사각형
        Line,           // 직선 (전방)
        Knight,         // 체스 나이트 패턴
        Circle,         // 원형 패턴
        Plus,           // + 모양 (십자 + 중앙)
        Star,           // 별 모양
        Custom          // 커스텀 패턴
    }
    
    // 공격 패턴 데이터
    [System.Serializable]
    public class AttackPatternData
    {
        public AttackPatternType patternType;
        public List<Vector2Int> patternOffsets = new List<Vector2Int>();
    }
    
    // 게임 설정
    [System.Serializable]
    public class GameConfiguration
    {
        public int maxSelectableCharacters = 10;
        public float turnTimeLimit = 30f;
        public int boardWidth = 6;
        public int boardHeight = 3;
        public int totalAreas = 2;
        
        // 점수 설정
        public int pointsPerAreaWin = 1;
        public int pointsForTotalWin = 2;
        public int pointsForTotalLoss = 0;
    }
    
    // 현재 게임 세션 정보
    [System.Serializable]
    public class GameSession
    {
        public string sessionId;
        public System.DateTime startTime;
        public int totalTurns;
        public float totalPlayTime;
        public bool isMultiplayer;
        public string opponentName;
    }
    
    // 기본 데이터 초기화
    void InitializeDefaultData()
    {
        // 기본 게임 설정
        if (gameConfig == null)
        {
            gameConfig = new GameConfiguration();
        }
        
        // 기본 공격 패턴 초기화
        InitializeAttackPatterns();
        
        // 기본 캐릭터 생성
        if (allCharacters.Count == 0)
        {
            CreateDefaultCharacters();
        }
    }
    
    // 공격 패턴 초기화
    void InitializeAttackPatterns()
    {
        attackPatterns.Clear();
        
        // 십자 패턴
        AttackPatternData cross = new AttackPatternData();
        cross.patternType = AttackPatternType.Cross;
        cross.patternOffsets.Add(new Vector2Int(0, 0));    // 중앙
        cross.patternOffsets.Add(new Vector2Int(0, 1));    // 위
        cross.patternOffsets.Add(new Vector2Int(0, -1));   // 아래
        cross.patternOffsets.Add(new Vector2Int(1, 0));    // 오른쪽
        cross.patternOffsets.Add(new Vector2Int(-1, 0));   // 왼쪽
        attackPatterns.Add(cross);
        
        // 대각선 패턴
        AttackPatternData diagonal = new AttackPatternData();
        diagonal.patternType = AttackPatternType.Diagonal;
        diagonal.patternOffsets.Add(new Vector2Int(0, 0));    // 중앙
        diagonal.patternOffsets.Add(new Vector2Int(1, 1));    // 우상
        diagonal.patternOffsets.Add(new Vector2Int(1, -1));   // 우하
        diagonal.patternOffsets.Add(new Vector2Int(-1, 1));   // 좌상
        diagonal.patternOffsets.Add(new Vector2Int(-1, -1));  // 좌하
        attackPatterns.Add(diagonal);
        
        // 3x3 정사각형 패턴
        AttackPatternData square3x3 = new AttackPatternData();
        square3x3.patternType = AttackPatternType.Square3x3;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                square3x3.patternOffsets.Add(new Vector2Int(x, y));
            }
        }
        attackPatterns.Add(square3x3);
        
        // 나이트 패턴
        AttackPatternData knight = new AttackPatternData();
        knight.patternType = AttackPatternType.Knight;
        knight.patternOffsets.Add(new Vector2Int(0, 0));    // 중앙
        knight.patternOffsets.Add(new Vector2Int(2, 1));
        knight.patternOffsets.Add(new Vector2Int(2, -1));
        knight.patternOffsets.Add(new Vector2Int(-2, 1));
        knight.patternOffsets.Add(new Vector2Int(-2, -1));
        knight.patternOffsets.Add(new Vector2Int(1, 2));
        knight.patternOffsets.Add(new Vector2Int(1, -2));
        knight.patternOffsets.Add(new Vector2Int(-1, 2));
        knight.patternOffsets.Add(new Vector2Int(-1, -2));
        attackPatterns.Add(knight);
        
        // 직선 패턴 (전방 3칸)
        AttackPatternData line = new AttackPatternData();
        line.patternType = AttackPatternType.Line;
        line.patternOffsets.Add(new Vector2Int(0, 0));
        line.patternOffsets.Add(new Vector2Int(1, 0));
        line.patternOffsets.Add(new Vector2Int(2, 0));
        attackPatterns.Add(line);
    }
    
    // 기본 캐릭터 생성
    void CreateDefaultCharacters()
    {
        // 십자 공격 캐릭터
        CharacterDefinition crossChar = new CharacterDefinition();
        crossChar.characterId = 1;
        crossChar.characterName = "십자 전사";
        crossChar.attackType = AttackPatternType.Cross;
        crossChar.attackRange = 1;
        crossChar.description = "십자 모양으로 공격합니다.";
        crossChar.rarity = CharacterRarity.Common;
        allCharacters.Add(crossChar);
        
        // 대각선 공격 캐릭터
        CharacterDefinition diagonalChar = new CharacterDefinition();
        diagonalChar.characterId = 2;
        diagonalChar.characterName = "대각선 마법사";
        diagonalChar.attackType = AttackPatternType.Diagonal;
        diagonalChar.attackRange = 1;
        diagonalChar.description = "대각선 방향으로 공격합니다.";
        diagonalChar.rarity = CharacterRarity.Common;
        allCharacters.Add(diagonalChar);
        
        // 3x3 범위 공격 캐릭터
        CharacterDefinition aoeChar = new CharacterDefinition();
        aoeChar.characterId = 3;
        aoeChar.characterName = "폭발 전문가";
        aoeChar.attackType = AttackPatternType.Square3x3;
        aoeChar.attackRange = 1;
        aoeChar.description = "3x3 범위를 공격합니다.";
        aoeChar.rarity = CharacterRarity.Rare;
        allCharacters.Add(aoeChar);
        
        // 나이트 패턴 캐릭터
        CharacterDefinition knightChar = new CharacterDefinition();
        knightChar.characterId = 4;
        knightChar.characterName = "기마병";
        knightChar.attackType = AttackPatternType.Knight;
        knightChar.attackRange = 2;
        knightChar.description = "체스의 나이트처럼 이동하며 공격합니다.";
        knightChar.rarity = CharacterRarity.Epic;
        allCharacters.Add(knightChar);
        
        // 직선 공격 캐릭터
        CharacterDefinition lineChar = new CharacterDefinition();
        lineChar.characterId = 5;
        lineChar.characterName = "저격수";
        lineChar.attackType = AttackPatternType.Line;
        lineChar.attackRange = 2;
        lineChar.description = "전방 직선으로 공격합니다.";
        lineChar.rarity = CharacterRarity.Common;
        allCharacters.Add(lineChar);
        
        // 추가 캐릭터들...
        
        // 원형 패턴 캐릭터
        CharacterDefinition circleChar = new CharacterDefinition();
        circleChar.characterId = 6;
        circleChar.characterName = "원소술사";
        circleChar.attackType = AttackPatternType.Circle;
        circleChar.attackRange = 2;
        circleChar.description = "원형 범위로 공격합니다.";
        circleChar.rarity = CharacterRarity.Rare;
        allCharacters.Add(circleChar);
        
        // 별 모양 패턴 캐릭터
        CharacterDefinition starChar = new CharacterDefinition();
        starChar.characterId = 7;
        starChar.characterName = "별의 수호자";
        starChar.attackType = AttackPatternType.Star;
        starChar.attackRange = 2;
        starChar.description = "별 모양으로 공격합니다.";
        starChar.rarity = CharacterRarity.Legendary;
        allCharacters.Add(starChar);
        
        // + 모양 패턴 캐릭터
        CharacterDefinition plusChar = new CharacterDefinition();
        plusChar.characterId = 8;
        plusChar.characterName = "성기사";
        plusChar.attackType = AttackPatternType.Plus;
        plusChar.attackRange = 2;
        plusChar.description = "+ 모양으로 넓게 공격합니다.";
        plusChar.rarity = CharacterRarity.Epic;
        allCharacters.Add(plusChar);
        
        // 커스텀 패턴 캐릭터 1
        CharacterDefinition customChar1 = new CharacterDefinition();
        customChar1.characterId = 9;
        customChar1.characterName = "암살자";
        customChar1.attackType = AttackPatternType.Custom;
        customChar1.attackRange = 3;
        customChar1.description = "특수한 패턴으로 공격합니다.";
        customChar1.rarity = CharacterRarity.Epic;
        customChar1.customPattern.Add(new Vector2Int(0, 0));
        customChar1.customPattern.Add(new Vector2Int(2, 0));
        customChar1.customPattern.Add(new Vector2Int(-2, 0));
        customChar1.customPattern.Add(new Vector2Int(0, 2));
        customChar1.customPattern.Add(new Vector2Int(0, -2));
        allCharacters.Add(customChar1);
        
        // 커스텀 패턴 캐릭터 2
        CharacterDefinition customChar2 = new CharacterDefinition();
        customChar2.characterId = 10;
        customChar2.characterName = "시공술사";
        customChar2.attackType = AttackPatternType.Custom;
        customChar2.attackRange = 3;
        customChar2.description = "시공간을 뒤틀어 공격합니다.";
        customChar2.rarity = CharacterRarity.Legendary;
        customChar2.customPattern.Add(new Vector2Int(0, 0));
        customChar2.customPattern.Add(new Vector2Int(3, 0));
        customChar2.customPattern.Add(new Vector2Int(-3, 0));
        customChar2.customPattern.Add(new Vector2Int(0, 3));
        customChar2.customPattern.Add(new Vector2Int(0, -3));
        customChar2.customPattern.Add(new Vector2Int(2, 2));
        customChar2.customPattern.Add(new Vector2Int(-2, -2));
        allCharacters.Add(customChar2);
    }
    
    // 캐릭터 ID로 캐릭터 데이터 가져오기
    public CharacterDefinition GetCharacterById(int id)
    {
        return allCharacters.Find(c => c.characterId == id);
    }
    
    // 공격 패턴 가져오기
    public List<Vector2Int> GetAttackPattern(CharacterDefinition character)
    {
        if (character.attackType == AttackPatternType.Custom)
        {
            return character.customPattern;
        }
        
        AttackPatternData pattern = attackPatterns.Find(p => p.patternType == character.attackType);
        return pattern?.patternOffsets ?? new List<Vector2Int>();
    }
    
    // 새 게임 세션 시작
    public void StartNewSession(bool isMultiplayer = false, string opponentName = "")
    {
        currentSession = new GameSession();
        currentSession.sessionId = System.Guid.NewGuid().ToString();
        currentSession.startTime = System.DateTime.Now;
        currentSession.totalTurns = 0;
        currentSession.totalPlayTime = 0f;
        currentSession.isMultiplayer = isMultiplayer;
        currentSession.opponentName = opponentName;
    }
    
    // 세션 업데이트
    public void UpdateSession(float deltaTime)
    {
        if (currentSession != null)
        {
            currentSession.totalPlayTime += deltaTime;
        }
    }
}