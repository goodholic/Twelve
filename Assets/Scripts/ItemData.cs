// Assets/Scripts/ItemData.cs

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

    // =========================
    // 실제로 아이템을 사용해
    // 캐릭터에게 효과를 부여할 로직
    // =========================
    public void ApplyEffectToCharacter(Character target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[ItemData] {itemName} 아이템 효과 적용 실패: Character가 null");
            return;
        }

        switch (effectType)
        {
            case ItemEffectType.IncreaseAttack:
                target.attackPower += effectValue;
                Debug.Log($"[ItemData] {itemName} 사용 -> 공격력 +{effectValue}, 결과={target.attackPower}");
                break;

            case ItemEffectType.IncreaseHP:
                // 체력(Hp)이 따로 있다면, 별도 HP 속성 증가
                // 여기서는 Monster처럼 HP가 없을 수 있으므로, 별도 구현 필요
                // 예: target.AddAdditionalHP(effectValue);
                Debug.Log($"[ItemData] {itemName} 사용 -> HP +{effectValue}");
                break;

            case ItemEffectType.IncreaseRange:
                target.attackRange += effectValue;
                Debug.Log($"[ItemData] {itemName} 사용 -> 사거리 +{effectValue}, 결과={target.attackRange}");
                break;
        }
    }
}
