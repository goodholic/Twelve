using UnityEngine;

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

    [Header("Prefab")]
    [Tooltip("실제로 소환/배치할 때 Instantiate할 프리팹 (소환 완료시 보여질 오브젝트)")]
    public GameObject spawnPrefab;

    [Header("UI Display")]
    [Tooltip("버튼 UI에 표시될 캐릭터 아이콘 (예: Sprite)")]
    public Sprite buttonIcon;

    [Tooltip("캐릭터 소환 비용 (UI 표시용)")]
    public int cost = 10;
}
