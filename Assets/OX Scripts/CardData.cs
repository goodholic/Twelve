using UnityEngine;

[System.Serializable]
public class CardData
{
    [Header("카드 이름(예: 슬라임, 드래곤 등)")]
    public string cardName;

    [Header("현재 레벨 (1 ~ 20)")]
    public int level = 1;

    [Header("현재 경험치")]
    public int currentExp = 0;

    [Header("스프라이트(이미지)")]
    public Sprite cardSprite;

    [Header("캐릭터 프리팹(공격 패턴 등 포함)")]
    // 원래 Character -> oxCharacter 로 변경
    public oxCharacter characterPrefab;

    [Header("게임 오브젝트(프리팹)")]
    public GameObject gamePrefab;

    [Header("추가 정보(옵션)")]
    // 필요 시 공격력, 체력, 설명 등 자유롭게 추가 가능

    public bool IsMaxLevel => (level >= 20);

    public void AddExp(int amount)
    {
        if (IsMaxLevel) return;

        currentExp += amount;
        while (!IsMaxLevel && currentExp >= GetRequiredExpForLevel(level))
        {
            currentExp -= GetRequiredExpForLevel(level);
            level++;
            if (IsMaxLevel)
            {
                currentExp = 0;
                break;
            }
        }
    }

    private int GetRequiredExpForLevel(int lv)
    {
        return lv * 5; // 예시: 레벨 * 5
    }
}
