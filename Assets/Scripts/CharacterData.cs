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
    
    [Header("이동 속도")]
    public float moveSpeed = 1.0f;
    
    [Header("경험치")]
    public float currentExp = 0f;
    public float expToNextLevel = 100f;
    
    [Header("특수 속성")]
    public bool isFreeSlotOnly = false;
    
    [Header("스프라이트")]
    public Sprite characterSprite;
    public Sprite frontSprite;
    public Sprite backSprite;
    
    [Header("프리팹")]
    public GameObject spawnPrefab;
    public GameObject motionPrefab;
    
    [Header("UI 아이콘")]
    public Sprite buttonIcon;  // Image에서 Sprite로 변경
    
    [Header("초기 별 등급")]
    public CharacterStar initialStar = CharacterStar.OneStar;
}