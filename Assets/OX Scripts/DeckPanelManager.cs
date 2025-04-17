using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 덱 패널: 중복 제거(유니크)된 카드 목록을 표시.
/// 12칸(4×3)에 순서대로 배치.
/// </summary>
public class DeckPanelManager : MonoBehaviour
{
    [Header("CardInventoryManager (카드 인벤토리)")]
    [SerializeField] private CardInventoryManager cardInventory;

    [Header("덱 패널용 12슬롯 (Button 등)")]
    [SerializeField] private List<GameObject> deckSlots;

    // (추가) 빈 슬롯 표시용 스프라이트(선택사항)
    [Header("빈 슬롯용 스프라이트 (선택)")]
    [SerializeField] private Sprite emptySlotSprite;

    // (추가) 빈 슬롯 시 표시할 텍스트 (원한다면)
    [SerializeField] private string emptySlotText = "";

    private void OnEnable()
    {
        RefreshDeckDisplay();
    }

    /// <summary>
    /// 유니크 카드 목록을 12칸에 표시
    /// </summary>
    public void RefreshDeckDisplay()
    {
        if (cardInventory == null)
        {
            Debug.LogWarning("[DeckPanelManager] cardInventory가 연결되지 않음!");
            return;
        }
        if (deckSlots == null || deckSlots.Count == 0)
        {
            Debug.LogWarning("[DeckPanelManager] deckSlots가 세팅되지 않음!");
            return;
        }

        // 1) 중복 제거된 카드 목록
        List<CardData> uniqueCards = cardInventory.GetUniqueCardList();

        // 2) 슬롯 수, 카드 수 비교
        int slotCount = deckSlots.Count; // 예: 12
        int cardCount = uniqueCards.Count;
        int displayCount = Mathf.Min(slotCount, cardCount);

        // 3) 모든 슬롯을 일단 '빈 슬롯'으로 초기화
        for (int i = 0; i < slotCount; i++)
        {
            SetSlotInfo(deckSlots[i], null);
        }

        // 4) 실제 카드 수만큼 슬롯 채우기
        for (int i = 0; i < displayCount; i++)
        {
            CardData card = uniqueCards[i];
            SetSlotInfo(deckSlots[i], card);
        }
    }

    /// <summary>
    /// 특정 슬롯에 CardData를 배치(이미지/텍스트), 
    /// card == null이면 "빈 슬롯"으로 표시
    /// </summary>
    private void SetSlotInfo(GameObject slotObj, CardData card)
    {
        // 만약 슬롯 오브젝트 자체가 null이면 건너뛰기
        if (slotObj == null)
        {
            Debug.LogWarning("[DeckPanelManager] 슬롯 오브젝트가 null입니다.");
            return;
        }

        Image cardImg = slotObj.transform.Find("CardImage")?.GetComponent<Image>();
        TextMeshProUGUI cardText = slotObj.transform.Find("CardText")?.GetComponent<TextMeshProUGUI>();
        Button button = slotObj.GetComponent<Button>();

        // ★ card가 null이면 "빈 슬롯" 표시
        if (card == null)
        {
            if (cardImg) cardImg.sprite = emptySlotSprite;   // 없으면 null
            if (cardText) cardText.text = emptySlotText;     // 기본값 ""

            // 버튼 리스너 초기화
            if (button)
            {
                button.onClick.RemoveAllListeners();
            }
        }
        else
        {
            // --- 카드가 존재할 때 ---
            if (cardImg) cardImg.sprite = card.cardSprite;
            if (cardText) cardText.text = $"{card.cardName}\nLv.{card.level}";

            // 버튼 누르면 상세 정보 등 처리
            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClickDeckCard(card));
            }
        }
    }

    /// <summary>
    /// 덱 카드 클릭 시 상세 표시 등
    /// </summary>
    private void OnClickDeckCard(CardData card)
    {
        Debug.Log($"[DeckPanelManager] 덱 카드 클릭: {card.cardName} (Lv.{card.level})");
        // 필요 시 별도 UI 표시 등
    }
}
