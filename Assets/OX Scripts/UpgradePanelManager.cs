using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class UpgradePanelManager : MonoBehaviour
{
    [Header("CardInventoryManager")]
    [SerializeField] private CardInventoryManager cardInventory;

    [Header("중복 카드도 전부 보여줄 12개 슬롯 (업그레이드 창)")]
    [SerializeField] private List<GameObject> upgradeSlots;

    [Header("캐릭터(1~4) 업그레이드 버튼")]
    [SerializeField] private Button upgradeChar1Button;
    [SerializeField] private Button upgradeChar2Button;
    [SerializeField] private Button upgradeChar3Button;
    [SerializeField] private Button upgradeChar4Button;

    [Header("빈 슬롯용 스프라이트 (선택)")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("빈 슬롯용 텍스트 (선택)")]
    [SerializeField] private string emptySlotText = "";

    private List<CardData> selectedCards = new List<CardData>();

    private void OnEnable()
    {
        RefreshUpgradeDisplay();
        SetupUpgradeButtons();
    }

    public void RefreshUpgradeDisplay()
    {
        if (cardInventory == null)
        {
            Debug.LogWarning("[UpgradePanelManager] cardInventory가 없음");
            return;
        }
        List<CardData> allCards = cardInventory.GetAllCardsWithDuplicates();

        int slotCount = upgradeSlots.Count; 
        int cardCount = allCards.Count;
        int displayCount = Mathf.Min(slotCount, cardCount);

        for (int i = 0; i < slotCount; i++)
        {
            SetSlotInfo(upgradeSlots[i], null);
        }

        for (int i = 0; i < displayCount; i++)
        {
            CardData card = allCards[i];
            SetSlotInfo(upgradeSlots[i], card);
        }

        selectedCards.Clear();
    }

    private void SetSlotInfo(GameObject slotObj, CardData card)
    {
        if (slotObj == null)
        {
            Debug.LogWarning("[UpgradePanelManager] 슬롯 오브젝트가 null입니다.");
            return;
        }

        Image cardImg = slotObj.transform.Find("CardImage")?.GetComponent<Image>();
        TextMeshProUGUI cardText = slotObj.transform.Find("CardText")?.GetComponent<TextMeshProUGUI>();
        Toggle toggle = slotObj.transform.Find("SelectToggle")?.GetComponent<Toggle>();
        Button button = slotObj.GetComponent<Button>();

        if (card == null)
        {
            if (cardImg) cardImg.sprite = emptySlotSprite;
            if (cardText) cardText.text = emptySlotText;

            if (toggle)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = false;
            }

            if (button)
            {
                button.onClick.RemoveAllListeners();
            }
        }
        else
        {
            if (cardImg) cardImg.sprite = card.cardSprite;
            if (cardText) cardText.text = $"{card.cardName}\nLv.{card.level}";

            if (toggle)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = false;
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn) SelectCard(card);
                    else DeselectCard(card);
                });
            }

            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClickCardSlot(card));
            }
        }
    }

    private void OnClickCardSlot(CardData card)
    {
        Debug.Log($"[UpgradePanelManager] 카드 슬롯 클릭: {card.cardName} (Lv.{card.level})");
    }

    private void SelectCard(CardData c)
    {
        if (!selectedCards.Contains(c))
        {
            selectedCards.Add(c);
        }
    }

    private void DeselectCard(CardData c)
    {
        if (selectedCards.Contains(c))
        {
            selectedCards.Remove(c);
        }
    }

    private void SetupUpgradeButtons()
    {
        if (upgradeChar1Button)
            upgradeChar1Button.onClick.AddListener(() => OnClickUpgradeCharacter(1));
        if (upgradeChar2Button)
            upgradeChar2Button.onClick.AddListener(() => OnClickUpgradeCharacter(2));
        if (upgradeChar3Button)
            upgradeChar3Button.onClick.AddListener(() => OnClickUpgradeCharacter(3));
        if (upgradeChar4Button)
            upgradeChar4Button.onClick.AddListener(() => OnClickUpgradeCharacter(4));
    }

    private void OnClickUpgradeCharacter(int charIndex)
    {
        if (selectedCards.Count == 0)
        {
            Debug.LogWarning("[UpgradePanel] 선택된 카드가 없습니다");
            return;
        }

        int consumeCount = selectedCards.Count;
        cardInventory.ConsumeCardsForUpgrade(selectedCards);

        oxGameManager gm = FindFirstObjectByType<oxGameManager>();
        if (!gm)
        {
            Debug.LogWarning("[UpgradePanel] oxGameManager가 없음");
            return;
        }

        SummonButton targetButton = null;
        switch (charIndex)
        {
            case 1: targetButton = gm.GetSummonButton1(); break;
            case 2: targetButton = gm.GetSummonButton2(); break;
            case 3: targetButton = gm.GetSummonButton3(); break;
            case 4: targetButton = gm.GetSummonButton4(); break;
        }

        if (targetButton == null)
        {
            Debug.LogWarning($"[UpgradePanel] SummonButton {charIndex}가 연결 안됨");
            return;
        }

        Debug.Log($"[UpgradePanel] 캐릭터 {charIndex}에 {consumeCount}장 소모 -> 예시 Exp 증가");

        // (실제 업그레이드/경험치 반영 로직은 임의로 작성)
        // ...

        // 다시 갱신
        RefreshUpgradeDisplay();
    }
}
