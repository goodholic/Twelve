using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CharacterData
{
    [Header("Basic Info")]
    [Tooltip("이 캐릭터를 구분하기 위한 이름 (UI 표시용)")]
    public string characterName;

    [Tooltip("캐릭터 기본 별 등급")]
    public CharacterStar initialStar = CharacterStar.OneStar;

    // [추가] 종족 정보
    [Header("Race Info")]
    public CharacterRace race = CharacterRace.Human;

    // [추가] 덱 구성 시 자유 슬롯 캐릭터인지 여부
    [Tooltip("자유 슬롯(10번째)에만 배치 가능한 캐릭터인지")]
    public bool isFreeSlotOnly = false;

    // ========================
    // (1) 새로 추가된 스탯들
    // ========================
    [Header("Stats (요청에 따른 필드 추가)")]
    [Tooltip("공격력")]
    public float attackPower = 10f;

    [Tooltip("공격속도(초당 공격 횟수)")]
    public float attackSpeed = 1f;

    [Tooltip("공격 사거리(실수값)")]
    public float attackRange = 1.5f;

    [Tooltip("최대 체력(캐릭터가 사용할 체력)")]
    public float maxHP = 100f;

    [Tooltip("이동 속도(웨이포인트 이동 시)")]
    public float moveSpeed = 3f; // 기본값 3f

    // ================
    // (기존) 필드들
    // ================
    [Tooltip("사거리 타입 (근거리/원거리/장거리 구분)")]
    public RangeType rangeType = RangeType.Melee;

    [Tooltip("광역 공격 여부 (true면 AOE 공격)")]
    public bool isAreaAttack = false;

    [Tooltip("버프형 캐릭터 여부 (버프 역할인지)")]
    public bool isBuffSupport = false;

    [Header("레벨/경험치")] 
    [Tooltip("캐릭터 레벨(1~30)")] // 기획서: 최대 30레벨
    public int level = 1;

    [Tooltip("현재 경험치 (0~99)")]
    public int currentExp = 0;

    [Tooltip("다음 레벨업까지 필요한 경험치")]
    public int expToNextLevel = 100;  // 기획서: 100% = 1레벨업

    [Header("Prefab")]
    [Tooltip("실제로 소환/배치할 때 Instantiate할 프리팹 (소환 완료시 보여질 오브젝트)")]
    public GameObject spawnPrefab;

    [Header("UI Display")]
    [Tooltip("버튼 UI에 표시될 캐릭터 아이콘(Image 컴포넌트)")]
    public Image buttonIcon;

    [Tooltip("캐릭터 소환 비용 (UI 표시용)")]
    public int cost = 10;

    // =============================
    // [추가] 캐릭터 앞/뒤 스프라이트
    // =============================
    [Header("캐릭터 방향 스프라이트")]
    [Tooltip("캐릭터가 위쪽을 향할 때 표시할 스프라이트")]
    public Sprite frontSprite;

    [Tooltip("캐릭터가 아래쪽을 향할 때 표시할 스프라이트")]
    public Sprite backSprite;

    [Header("Area Attack Settings (광역공격 범위)")]
    public float areaAttackRadius = 1f;

    [Header("도감용 모션 프리팹 (선택)")]
    public GameObject motionPrefab;

    /// <summary>
    /// [추가] 현재 Exp가 expToNextLevel 이상이면 레벨업
    /// </summary>
    public void CheckLevelUp()
    {
        // 최대 레벨 체크 (기획서: 최대 30레벨)
        if (level >= 30)
        {
            currentExp = 0; // 최대 레벨이면 경험치 초기화
            Debug.Log($"[CharacterData] {characterName}은 최대 레벨(30)입니다.");
            return;
        }

        while (currentExp >= expToNextLevel && level < 30)
        {
            currentExp -= expToNextLevel;
            level++;

            // 레벨업 시 스탯 증가 (레벨당 2% 증가)
            float statIncreaseRate = 1.02f;
            attackPower *= statIncreaseRate;
            attackSpeed *= statIncreaseRate;
            maxHP *= statIncreaseRate;
            moveSpeed *= statIncreaseRate;
            attackRange *= statIncreaseRate;

            Debug.Log($"[CharacterData] {characterName} 레벨업! => Lv.{level}, 남은Exp={currentExp}");
            
            // 최대 레벨 도달 시 경험치 초기화
            if (level >= 30)
            {
                currentExp = 0;
                break;
            }
        }
    }

    /// <summary>
    /// 캐릭터 스탯을 1% 업그레이드합니다.
    /// 공격력, 공격속도, 체력 등 모든 스탯이 증가합니다.
    /// </summary>
    /// <returns>증가된 스탯 정보 문자열</returns>
    public string UpgradeStats()
    {
        // 업그레이드 전 스탯 저장
        float oldAttackPower = attackPower;
        float oldAttackSpeed = attackSpeed;
        float oldMaxHP = maxHP;
        float oldMoveSpeed = moveSpeed;
        float oldAttackRange = attackRange;

        // 각 스탯을 1%씩 증가
        attackPower *= 1.01f;  // 1% 증가
        attackSpeed *= 1.01f;  // 1% 증가
        maxHP *= 1.01f;        // 1% 증가
        moveSpeed *= 1.01f;    // 1% 증가
        attackRange *= 1.01f;  // 1% 증가

        // 업그레이드 결과 메시지 생성
        string upgradeResult = $"{characterName} 스탯 업그레이드:\n" +
            $"공격력: {oldAttackPower:F1} → {attackPower:F1} (+{attackPower - oldAttackPower:F1})\n" +
            $"공격속도: {oldAttackSpeed:F2} → {attackSpeed:F2} (+{attackSpeed - oldAttackSpeed:F2})\n" +
            $"체력: {oldMaxHP:F1} → {maxHP:F1} (+{maxHP - oldMaxHP:F1})\n" +
            $"이동속도: {oldMoveSpeed:F2} → {moveSpeed:F2} (+{moveSpeed - oldMoveSpeed:F2})\n" +
            $"공격범위: {oldAttackRange:F2} → {attackRange:F2} (+{attackRange - oldAttackRange:F2})";

        Debug.Log($"[CharacterData] {upgradeResult}");
        return upgradeResult;
    }

    /// <summary>
    /// 같은 종족/등급 캐릭터를 재료로 경험치를 획득합니다 (기획서: 1% 경험치)
    /// </summary>
    public void AddExperienceFromSameCharacter()
    {
        if (level >= 30)
        {
            Debug.Log($"[CharacterData] {characterName}은 최대 레벨이라 경험치를 획득할 수 없습니다.");
            return;
        }

        currentExp += 1; // 1% 경험치 추가
        Debug.Log($"[CharacterData] {characterName} 경험치 +1% (현재: {currentExp}%)");
        
        CheckLevelUp();
    }
}