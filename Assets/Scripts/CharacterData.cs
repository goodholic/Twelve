using UnityEngine;
using UnityEngine.UI;

public enum RangeType
{
    Melee,      // 근거리
    Ranged,     // 원거리
    LongRange   // 장거리
}

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
    [Tooltip("캐릭터 레벨(1~N)")]
    public int level = 1;

    [Tooltip("현재 경험치")]
    public int currentExp = 0;

    [Tooltip("다음 레벨업까지 필요한 경험치")]
    public int expToNextLevel = 5;  // 예시로 5

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
        if (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            level++;

            expToNextLevel += 5; // 예: 레벨업마다 +5씩 증가
            Debug.Log($"[CharacterData] {characterName} 레벨업! => Lv.{level}, 남은Exp={currentExp}");
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
}


/* =======================================
   [예시] 10개 유닛(1성 기준) 밸런스 수치
   =======================================

1) 검(=Swordsman)
   - attackPower = 12
   - attackSpeed = 1.3  // 초당 1.3회 공격
   - attackRange = 1.1  // 근접
   - maxHP = 200
   - isAreaAttack = false
   - rangeType = Melee

2) 남 법사(=Male Wizard)
   - attackPower = 15
   - attackSpeed = 1.0
   - attackRange = 3.0
   - maxHP = 120
   - isAreaAttack = true
   - rangeType = Ranged

3) 캐논병
   - attackPower = 20
   - attackSpeed = 0.8
   - attackRange = 3.5
   - maxHP = 150
   - isAreaAttack = true
   - rangeType = Ranged

4) 쉴드병
   - attackPower = 8
   - attackSpeed = 0.9
   - attackRange = 1.0
   - maxHP = 300
   - isAreaAttack = false
   - rangeType = Melee

5) 여법사1
   - attackPower = 13
   - attackSpeed = 1.1
   - attackRange = 2.5
   - maxHP = 100
   - isAreaAttack = false
   - rangeType = Ranged

6) 여법사2
   - attackPower = 18
   - attackSpeed = 0.9
   - attackRange = 3.0
   - maxHP = 100
   - isAreaAttack = true
   - rangeType = Ranged

7) 거중기병
   - attackPower = 25
   - attackSpeed = 0.7
   - attackRange = 1.5
   - maxHP = 250
   - isAreaAttack = false
   - rangeType = Melee

8) 완드법사
   - attackPower = 16
   - attackSpeed = 1.0
   - attackRange = 2.8
   - maxHP = 120
   - isAreaAttack = false
   - rangeType = Ranged

9) 우는 병사
   - attackPower = 9
   - attackSpeed = 1.5
   - attackRange = 1.1
   - maxHP = 180
   - isAreaAttack = false
   - rangeType = Melee

10) 레이저 병사
   - attackPower = 22
   - attackSpeed = 0.6
   - attackRange = 4.0
   - maxHP = 130
   - isAreaAttack = true
   - rangeType = LongRange

*/
