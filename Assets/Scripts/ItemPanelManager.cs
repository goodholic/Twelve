using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemPanelManager : MonoBehaviour
{
    [Header("ItemInventoryManager")]
    [SerializeField] private ItemInventoryManager itemInventoryManager;

    [Header("아이템 슬롯(버튼) UI들 (총 5개)")]
    [SerializeField] private List<Button> itemSlotButtons;
    [SerializeField] private List<Image> itemSlotImages;
    [SerializeField] private List<TextMeshProUGUI> itemSlotNameTexts;

    [Header("드래그 이동용 Parent Panel")]
    [SerializeField] private RectTransform dragParentPanel;

    [Header("빈 슬롯용 이미지")]
    [SerializeField] private Sprite someEmptySprite;

    // 실제 보유 아이템 목록을 캐싱하기 위한 리스트
    private List<ItemData> itemList = new List<ItemData>();

    /// <summary>
    /// 아이템 인벤토리 패널(5칸)을 새로고침
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

            // i번째 인덱스에 아이템이 있는지 확인
            if (i < itemList.Count && itemList[i] != null)
            {
                // 실제 아이템
                ItemData currentItem = itemList[i];

                // 아이콘 표시
                if (itemSlotImages != null && i < itemSlotImages.Count && itemSlotImages[i] != null)
                {
                    itemSlotImages[i].sprite = currentItem.itemIcon;
                    itemSlotImages[i].gameObject.SetActive(true);
                }

                // 이름 표시
                if (itemSlotNameTexts != null && i < itemSlotNameTexts.Count && itemSlotNameTexts[i] != null)
                {
                    itemSlotNameTexts[i].text = currentItem.itemName;
                }

                // 드래그 스크립트(DraggableItemUI) 연결
                DraggableItemUI dragItem = itemSlotButtons[i].GetComponent<DraggableItemUI>();
                if (dragItem != null)
                {
                    dragItem.currentItem = currentItem; // 여기서 아이템 설정
                    dragItem.parentPanel = dragParentPanel;
                }

                // (선택) 버튼 클릭 시 -> 콘솔 로그
                int copyIndex = i;
                itemSlotButtons[i].onClick.AddListener(() => OnClickItemSlot(copyIndex));
            }
            else
            {
                // 빈 슬롯 처리
                if (itemSlotImages != null && i < itemSlotImages.Count && itemSlotImages[i] != null)
                {
                    itemSlotImages[i].sprite = someEmptySprite;
                }

                if (itemSlotNameTexts != null && i < itemSlotNameTexts.Count && itemSlotNameTexts[i] != null)
                {
                    itemSlotNameTexts[i].text = "";
                }

                // 드래그 스크립트가 있다면 currentItem = null로 처리
                DraggableItemUI dragItem = itemSlotButtons[i].GetComponent<DraggableItemUI>();
                if (dragItem != null)
                {
                    dragItem.currentItem = null;  // 빈칸이므로 null 설정
                    dragItem.parentPanel = dragParentPanel;
                }
            }
        }
    }

    /// <summary>
    /// (선택) 슬롯 클릭 시 로그 출력 등
    /// </summary>
    private void OnClickItemSlot(int index)
    {
        if (index < 0 || index >= itemList.Count) return;
        ItemData clickedItem = itemList[index];
        if (clickedItem == null) return;

        Debug.Log($"[ItemPanelManager] {index}번 슬롯 클릭 => 아이템: {clickedItem.itemName}");
    }
}
