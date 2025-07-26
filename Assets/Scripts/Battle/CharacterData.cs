using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Data;
using TacticalTileGame.Data;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Twelve Game/Characters/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("기본 정보")]
    public string characterName;
    public Sprite characterIcon;
    public Sprite buttonIcon; // UI용 버튼 아이콘
    public int hp = 100; // 단순화된 HP
    public int attackPower = 50; // 단순화된 공격력
    
    [Header("게임 오브젝트")]
    public GameObject spawnPrefab; // 스폰할 프리팹
    public GameObject motionPrefab; // 모션용 프리팹

    [Header("캐릭터 스탯")]
    public float moveSpeed = 1.0f; // 이동 속도
    public int cost = 1; // 배치 비용
    public int level = 1; // 레벨
    public int currentExp = 0; // 현재 경험치
    public int initialStar = 1; // 초기 별 등급
    
    [Header("전투 특성")]
    public RangeType rangeType = RangeType.Melee; // 사거리 타입
    public bool isAreaAttack = false; // 범위 공격 여부
    public bool isBuffSupport = false; // 버프 지원 여부
    public int maxHP = 100; // 최대 HP
    public int health = 100; // 현재 체력
    public int star = 1; // 별 등급
    public int starLevel = 1; // 별 레벨
    public string race = "Human"; // 종족
    public int expToNextLevel = 100; // 다음 레벨까지 필요 경험치
    public float attackRange = 1.0f; // 공격 범위
    public float attackSpeed = 1.0f; // 공격 속도
    public float areaAttackRadius = 1.0f; // 범위 공격 반경
    public bool isFreeSlotOnly = false; // 무료 슬롯 전용
    public Sprite frontSprite; // 앞면 스프라이트
    public Sprite backSprite; // 뒷면 스프라이트
    public int characterIndex = 0; // 캐릭터 인덱스
    public Sprite characterSprite; // 캐릭터 스프라이트
    
    // CSV Data Editor에서 사용하는 속성들
    public string id = ""; // 캐릭터 ID
    public JobClass jobClass = JobClass.Warrior; // 직업
    public Rarity rarity = Rarity.Common; // 레어도
    public int baseHP = 100; // 기본 HP
    public int baseMP = 50; // 기본 MP
    public int baseAttack = 10; // 기본 공격력
    public int baseDefense = 5; // 기본 방어력
    public int baseMagicPower = 0; // 기본 마법력
    // baseSpeed 제거됨 - 게임에서 사용하지 않음
    
    // TacticalCharacterDataSO에서 통합된 속성들
    public string characterId = ""; // 전술 게임용 ID (id와 동기화)  
    public string description = ""; // 캐릭터 설명
    public GameObject characterPrefab; // 전술 게임용 프리팹
    public float baseCritRate = 0.1f; // 기본 크리티컬 확률
    public string skillId = ""; // 스킬 ID (캐릭터당 하나)
    public SkillDataSO skill; // 스킬 객체 (캐릭터당 하나)
    public string attackPatternCSV = ""; // CSV 패턴 문자열
    public int maxLevel = 50; // 최대 레벨
    public CharacterRarity tacticalRarity = CharacterRarity.Common; // 전술 게임용 레어도
    public float critRate = 0.1f; // 크리티컬 확률
    public float critDamage = 150.0f; // 크리티컬 데미지
    public float accuracy = 95.0f; // 명중률
    public float evasion = 5.0f; // 회피율
    // 개별 스킬 ID들 제거됨 - 이제 skillId 하나만 사용
    [Header("공격 패턴")]
    public AttackPattern attackPattern;
    
    [Header("커스텀 패턴 (Custom 선택 시)")]
    public List<Vector2Int> customPattern = new List<Vector2Int>();

    // 공격 범위를 Vector2Int 리스트로 저장 (상대 위치)
    public List<Vector2Int> GetAttackPositions()
    {
        if (attackPattern == AttackPattern.Custom)
        {
            return customPattern;
        }
        return AttackPatternManager.GetPattern(attackPattern);
    }
}

// 공격 패턴 종류
public enum AttackPattern
{
    Cross,          // 십자 (상하좌우)
    Diagonal,       // 대각선 (X자)
    Line,           // 직선 (전방 3칸)
    Knight,         // ㄹ자 (체스 나이트)
    CrossBoard,     // 건너편 타일 공격
    Custom          // 커스텀 패턴
}

// 사거리 타입
public enum RangeType
{
    Melee,          // 근접
    Ranged,         // 원거리
    Magic           // 마법
}

// 런타임 공격 패턴 관리자 (에디터용은 CharacterAttackPatternEditor.cs에 별도 존재)
public static class AttackPatternManager
{
    public static List<Vector2Int> GetPattern(AttackPattern pattern)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        switch (pattern)
        {
            case AttackPattern.Cross:
                // 십자 패턴
                positions.Add(new Vector2Int(0, 1));   // 위
                positions.Add(new Vector2Int(0, -1));  // 아래
                positions.Add(new Vector2Int(1, 0));   // 오른쪽
                positions.Add(new Vector2Int(-1, 0));  // 왼쪽
                break;

            case AttackPattern.Diagonal:
                // 대각선 패턴
                positions.Add(new Vector2Int(1, 1));    // 우상
                positions.Add(new Vector2Int(1, -1));   // 우하
                positions.Add(new Vector2Int(-1, 1));   // 좌상
                positions.Add(new Vector2Int(-1, -1));  // 좌하
                break;

            case AttackPattern.Line:
                // 직선 패턴 (전방 3칸)
                positions.Add(new Vector2Int(1, 0));
                positions.Add(new Vector2Int(2, 0));
                positions.Add(new Vector2Int(3, 0));
                break;

            case AttackPattern.Knight:
                // ㄹ자 패턴 (체스 나이트)
                positions.Add(new Vector2Int(2, 1));
                positions.Add(new Vector2Int(2, -1));
                positions.Add(new Vector2Int(-2, 1));
                positions.Add(new Vector2Int(-2, -1));
                positions.Add(new Vector2Int(1, 2));
                positions.Add(new Vector2Int(1, -2));
                positions.Add(new Vector2Int(-1, 2));
                positions.Add(new Vector2Int(-1, -2));
                break;

            case AttackPattern.CrossBoard:
                // 건너편 타일 공격은 별도 처리 필요
                // 일단 빈 리스트 반환
                break;

            case AttackPattern.Custom:
                // 커스텀 패턴은 CharacterData에서 직접 정의
                break;
        }

        return positions;
    }

    public static bool IsCrossBoardAttack(AttackPattern pattern)
    {
        return pattern == AttackPattern.CrossBoard;
    }
}

/// <summary>
/// 통합된 CharacterData용 확장 메서드들
/// </summary>
public static class CharacterDataExtensions
{
    /// <summary>
    /// CSV 데이터로부터 캐릭터 데이터 초기화 (TacticalCharacterDataSO에서 통합)
    /// </summary>
    public static void InitializeFromCSV(this CharacterData character, Dictionary<string, string> csvData)
    {
        if (csvData.ContainsKey("id"))
        {
            character.id = csvData["id"];
            character.characterId = csvData["id"]; // 동기화
        }
        if (csvData.ContainsKey("name"))
        {
            character.characterName = csvData["name"];
        }
        if (csvData.ContainsKey("description"))
        {
            character.description = csvData["description"];
        }
        
        // 스탯 파싱
        if (csvData.ContainsKey("hp") && int.TryParse(csvData["hp"], out int hp))
        {
            character.baseHP = hp;
            character.maxHP = hp;
        }
        if (csvData.ContainsKey("attack") && int.TryParse(csvData["attack"], out int atk))
        {
            character.baseAttack = atk;
            character.attackPower = atk; // 동기화
        }
        if (csvData.ContainsKey("defense") && int.TryParse(csvData["defense"], out int def))
            character.baseDefense = def;
        if (csvData.ContainsKey("magic") && int.TryParse(csvData["magic"], out int mag))
            character.baseMagicPower = mag;
        if (csvData.ContainsKey("speed") && int.TryParse(csvData["speed"], out int spd))
            // baseSpeed 제거됨
        if (csvData.ContainsKey("critRate") && float.TryParse(csvData["critRate"], out float crit))
            character.baseCritRate = crit;
        
        // 공격 패턴 파싱
        if (csvData.ContainsKey("attackPattern"))
        {
            character.attackPatternCSV = csvData["attackPattern"];
            character.customPattern = ParseAttackPatternFromString(character.attackPatternCSV);
            if (character.customPattern.Count > 0)
            {
                character.attackPattern = AttackPattern.Custom;
            }
        }
        
        // 스킬 ID 파싱 (첫 번째 스킬만 사용)
        if (csvData.ContainsKey("skills"))
        {
            string[] skills = csvData["skills"].Split(',');
            if (skills.Length > 0 && !string.IsNullOrEmpty(skills[0].Trim()))
            {
                character.skillId = skills[0].Trim();
            }
        }
        
        // 클래스와 레어도 파싱
        if (csvData.ContainsKey("class") && System.Enum.TryParse<JobClass>(csvData["class"], out JobClass charClass))
            character.jobClass = charClass;
        if (csvData.ContainsKey("rarity") && System.Enum.TryParse<CharacterRarity>(csvData["rarity"], out CharacterRarity charRarity))
            character.tacticalRarity = charRarity;
    }
    
    /// <summary>
    /// 실제 공격 가능한 타일 위치 계산 (TacticalCharacterDataSO에서 통합)
    /// </summary>
    public static List<Vector2Int> GetAttackableTiles(this CharacterData character, Vector2Int characterPosition)
    {
        List<Vector2Int> attackableTiles = new List<Vector2Int>();
        
        List<Vector2Int> pattern = character.GetAttackPositions();
        if (pattern != null)
        {
            foreach (var offset in pattern)
            {
                attackableTiles.Add(characterPosition + offset);
            }
        }
        
        return attackableTiles;
    }
    
    /// <summary>
    /// 공격 패턴 문자열 파싱 (TacticalCharacterDataSO에서 통합)
    /// </summary>
    private static List<Vector2Int> ParseAttackPatternFromString(string patternString)
    {
        List<Vector2Int> pattern = new List<Vector2Int>();
        if (string.IsNullOrEmpty(patternString)) return pattern;
        
        string[] positions = patternString.Split(';');
        foreach (string pos in positions)
        {
            string[] coords = pos.Split(',');
            if (coords.Length == 2)
            {
                if (int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y))
                {
                    pattern.Add(new Vector2Int(x, y));
                }
            }
        }
        return pattern;
    }
}