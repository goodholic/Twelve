using UnityEngine;

/// <summary>
/// 캐릭터 데이터 클래스
/// 게임 기획서: 캐릭터의 모든 정보를 담는 데이터 구조
/// </summary>
[System.Serializable]
public class CharacterData
{
    [Header("기본 정보")]
    public string characterName = "Unknown";
    public int characterIndex = -1;
    public CharacterRace race = CharacterRace.Human;
    public CharacterStar star = CharacterStar.OneStar;
    public int level = 1;
    
    [Header("전투 스탯")]
    public float attackPower = 10f;
    public float attackRange = 3f;
    public float attackSpeed = 1f;
    public float health = 100f;
    public float maxHP = 100f;
    public AttackTargetType attackTargetType = AttackTargetType.Both;
    public RangeType rangeType = RangeType.Melee;
    public bool isAreaAttack = false;
    public float areaAttackRadius = 1.5f;
    public bool isBuffSupport = false;
    
    [Header("비용")]
    public int cost = 10;
    
    [Header("스프라이트")]
    public Sprite characterSprite;
    public Sprite frontSprite;
    public Sprite backSprite;
    
    [Header("프리팹")]
    public GameObject spawnPrefab;
    
    [Header("UI 아이콘")]
    public Sprite buttonIcon;  // Image에서 Sprite로 변경
    
    [Header("초기 별 등급")]
    public CharacterStar initialStar = CharacterStar.OneStar;
}

/// <summary>
/// 캐릭터 종족
/// 게임 기획서: 휴먼, 오크, 엘프 (각 종족 3명 + 자유 1명)
/// </summary>
[System.Serializable]
public enum CharacterRace
{
    Human,
    Orc,
    Elf,
    Undead, // 추가 종족
    Etc     // 기타
}

/// <summary>
/// 캐릭터 별 등급
/// 게임 기획서: 1성×3 → 2성, 2성×3 → 3성
/// </summary>
[System.Serializable]
public enum CharacterStar
{
    OneStar = 1,
    TwoStar = 2,
    ThreeStar = 3
}

/// <summary>
/// 공격 타겟 타입
/// </summary>
[System.Serializable]
public enum AttackTargetType
{
    Character,  // 캐릭터만
    Monster,    // 몬스터만
    Both        // 둘 다
}

/// <summary>
/// 공격 사거리 타입
/// </summary>
[System.Serializable]
public enum RangeType
{
    Melee,      // 근접
    Ranged      // 원거리
}