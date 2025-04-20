using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이템 고유 정보(이름, 효과 종류, UI 아이콘 등).
/// </summary>
public enum ItemEffectType
{
    IncreaseAttack,   // 공격력 증가
    IncreaseHP,       // 체력 증가
    IncreaseRange,    // 사거리 증가
    // 필요한 만큼 추가 가능
}

[System.Serializable]
public class ItemData
{
    [Header("기본 정보")]
    public string itemName;
    public Sprite itemIcon;       // 카드나 인벤토리에 표시할 아이콘
    public ItemEffectType effectType;
    public float effectValue;     // 효과값 (예: 공격력+10, 체력+50 등)
    
    [TextArea]
    public string description;    // (선택) 아이템 설명

    /// <summary>
    /// 아이템을 캐릭터에게 적용하는 메서드.
    /// effectValue가 0 이하라면 적용하지 않고 false 반환.
    /// 적용 성공 시 true 반환.
    /// </summary>
    public bool ApplyEffectToCharacter(Character target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[ItemData] {itemName} 아이템 효과 적용 실패: Character가 null");
            return false;
        }

        // 만약 효과값이 0 이하라면 "적용하지 않는다"
        if (effectValue <= 0f)
        {
            Debug.LogWarning($"[ItemData] {itemName} 은(는) 효과값이 0 이하이므로 적용 불가.");
            return false;
        }

        // 여기까지 왔으면 효과를 적용
        switch (effectType)
        {
            case ItemEffectType.IncreaseAttack:
                target.attackPower += effectValue;
                Debug.Log($"[ItemData] {itemName} 사용 -> 공격력 +{effectValue}, 결과={target.attackPower}");
                break;

            case ItemEffectType.IncreaseHP:
                // 체력이 따로 있다면 이 부분에서 증가시켜야 함 (예: target.AddHp(effectValue))
                Debug.Log($"[ItemData] {itemName} 사용 -> HP +{effectValue}");
                break;

            case ItemEffectType.IncreaseRange:
                target.attackRange += effectValue;
                Debug.Log($"[ItemData] {itemName} 사용 -> 사거리 +{effectValue}, 결과={target.attackRange}");
                break;
        }

        return true; // 적용 성공
    }
}
