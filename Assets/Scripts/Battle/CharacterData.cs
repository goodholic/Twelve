using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Data;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "OX Game/Character Data")]
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
    public int baseMagicPower = 10; // 기본 마법력
    public int baseSpeed = 10; // 기본 속도
    public float critRate = 0.1f; // 크리티컬 확률
    public float critDamage = 150.0f; // 크리티컬 데미지
    public float accuracy = 95.0f; // 명중률
    public float evasion = 5.0f; // 회피율
    public string skill1Id = ""; // 스킬 1 ID
    public string skill2Id = ""; // 스킬 2 ID  
    public string skill3Id = ""; // 스킬 3 ID
    public string description = ""; // 캐릭터 설명

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

// 공격 패턴 관리자
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

    // 건너편 보드 공격 여부 확인
    public static bool IsCrossBoardAttack(AttackPattern pattern)
    {
        return pattern == AttackPattern.CrossBoard;
    }
}