using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터의 기본 정보를 담는 데이터 구조 (에디터에서 일괄 관리용)
/// </summary>
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

    [Header("Stats")]
    [Tooltip("이 캐릭터의 공격력(Placement 시 Character.cs로 전달)")]
    public float attackPower = 10f;

    [Tooltip("사거리 타입 (근거리/원거리/장거리 구분)")]
    public RangeType rangeType = RangeType.Melee;

    [Tooltip("광역 공격 여부")]
    public bool isAreaAttack = false;

    [Tooltip("버프형 캐릭터 여부 (버프 역할인지)")]
    public bool isBuffSupport = false;

    [Header("레벨/경험치")] // [추가]
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

    /// <summary>
    /// [추가] 현재 Exp가 expToNextLevel 이상이면 레벨업
    /// </summary>
    public void CheckLevelUp()
    {
        // 필요하다면 반복(while)로 여러 레벨업도 가능
        if (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            level++;

            // 레벨이 오르면 expToNextLevel도 증가시키거나,
            // 공격력, 코스트 등을 올릴 수 있음
            expToNextLevel += 5; // 예: 다음 레벨로 갈 때마다 +5
            Debug.Log($"[CharacterData] {characterName} 레벨업! => Lv.{level}, 남은Exp={currentExp}");
        }
    }
}
