using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DrawPanelManager : MonoBehaviour
{
    [Header("CardInventoryManager 참조")]
    [SerializeField] private CardInventoryManager cardInventory;

    [Header("뽑기 결과 텍스트")]
    [SerializeField] private TextMeshProUGUI drawResultText;

    [Header("뽑기 결과 이미지 (새로 추가)")]
    [SerializeField] private Image drawResultImage;

    [Header("뽑기 버튼")]
    [SerializeField] private Button drawButton;

    private void Awake()
    {
        if (drawButton) drawButton.onClick.AddListener(OnClickDraw);
    }

    private void OnClickDraw()
    {
        if (!cardInventory)
        {
            Debug.LogWarning("[DrawPanelManager] cardInventory가 없음");
            return;
        }

        CardData newCard = cardInventory.DrawRandomCard();
        if (newCard != null)
        {
            // 뽑은 카드 즉시 저장
            cardInventory.SaveOwnedCards();

            if (drawResultText)
            {
                drawResultText.text = $"뽑기 성공: {newCard.cardName} (Lv.{newCard.level})";
            }
            if (drawResultImage)
            {
                drawResultImage.sprite = newCard.cardSprite;
            }

            // ====== [추가 코드] 패널 UI 즉시 갱신 ======
            oxGameManager gm = FindFirstObjectByType<oxGameManager>();
            if (gm)
            {
                gm.RefreshInventoryDisplay(); // 임시로 호출(인벤토리 UI 갱신)
            }

            UpgradePanelManager upm = FindFirstObjectByType<UpgradePanelManager>();
            if (upm)
            {
                upm.RefreshUpgradeDisplay();
            }

            DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
            if (dpm)
            {
                dpm.RefreshDeckDisplay();
            }
            // =========================================
        }
        else
        {
            if (drawResultText) drawResultText.text = "뽑기 실패(풀 비어있음)";
            if (drawResultImage) drawResultImage.sprite = null;
        }
    }
}
