// Assets/Scripts/ItemPanelManager.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemPanelManager : MonoBehaviour
{
    [Header("ItemInventoryManager")]
    [SerializeField] private ItemInventoryManager itemInventoryManager;

    [Header("아이템 슬롯(버튼) UI들 (총 9개)")]
    [SerializeField] private List<Button> itemSlotButtons;
    [SerializeField] private List<Image> itemSlotImages;
    [SerializeField] private List<TextMeshProUGUI> itemSlotNameTexts;

    // ---------------------------------------------------------
    // (중요) 드래그 시 이동할 부모 패널
    // ---------------------------------------------------------
    [Header("드래그 이동용 Parent Panel")]
    [SerializeField] private RectTransform dragParentPanel;

    [Header("빈 슬롯용 이미지")]
    [SerializeField] private Sprite someEmptySprite;

    // 실제 보유 아이템 목록을 캐싱하기 위한 리스트
    private List<ItemData> itemList = new List<ItemData>();

    /// <summary>
    /// 로비씬 등에서 아이템 인벤토리를 새로고침한다.
    /// </summary>
    public void RefreshItemPanel()
    {
        // 1) 실제 보유 아이템 목록 가져오기
        if (itemInventoryManager == null)
        {
            Debug.LogWarning("[ItemPanelManager] itemInventoryManager가 null -> 빈 목록 처리");
            itemList = new List<ItemData>();
        }
        else
        {
            itemList = itemInventoryManager.GetOwnedItems();
        }

        int slotCount = itemSlotButtons.Count;

        // 2) 슬롯별로 UI 표시
        for (int i = 0; i < slotCount; i++)
        {
            // 버튼 클릭 리스너 초기화
            if (itemSlotButtons[i] != null)
            {
                itemSlotButtons[i].onClick.RemoveAllListeners();
            }

            // i번째 인덱스에 아이템이 있다면:
            if (i < itemList.Count && itemList[i] != null)
            {
                ItemData currentItem = itemList[i];

                // 아이콘 이미지 설정
                if (itemSlotImages != null && i < itemSlotImages.Count && itemSlotImages[i] != null)
                {
                    itemSlotImages[i].sprite = currentItem.itemIcon;
                    itemSlotImages[i].gameObject.SetActive(true); // 아이콘 오브젝트 활성
                }

                // 이름 텍스트 설정
                if (itemSlotNameTexts != null && i < itemSlotNameTexts.Count && itemSlotNameTexts[i] != null)
                {
                    itemSlotNameTexts[i].text = currentItem.itemName;
                }

                // 드래그 스크립트 연결
                DraggableItemUI dragItem = itemSlotButtons[i].GetComponent<DraggableItemUI>();
                if (dragItem != null)
                {
                    dragItem.currentItem  = currentItem;
                    dragItem.parentPanel  = dragParentPanel;
                }

                // 버튼 클릭 시 로직 (※ 여기서는 예시로 콘솔 로그만)
                int copyIndex = i;
                itemSlotButtons[i].onClick.AddListener(() => OnClickItemSlot(copyIndex));
                // ★ 버튼 비활성화 코드를 제거했습니다 (interactable = false X)
            }
            else
            {
                itemSlotImages[i].sprite = someEmptySprite;
                // ------------------------------
                // 아이템이 없는 "빈 칸" 처리:
                // "버튼 이미지는 그대로" 유지
                // => sprite/text/interactable 변경 X
                // ------------------------------
                // 아무것도 하지 않습니다.
            }
        }
    }

    /// <summary>
    /// (선택) 슬롯 클릭 시 로직 예시
    /// </summary>
    private void OnClickItemSlot(int index)
    {
        if (index < 0 || index >= itemList.Count) return;
        ItemData clickedItem = itemList[index];
        if (clickedItem == null) return;

        Debug.Log($"[ItemPanelManager] {index}번 슬롯 클릭 -> 아이템: {clickedItem.itemName}");
        // 여기서 팝업 표시 등 추가 로직을 구현 가능
    }
}
