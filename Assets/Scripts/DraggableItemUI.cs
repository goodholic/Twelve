// Assets/Scripts/DraggableItemUI.cs

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("현재 슬롯에 어떤 아이템인지 (ItemData)")]
    public ItemData currentItem;

    [Header("드래그 시 이동할 부모 패널 (RectTransform)")]
    public RectTransform parentPanel;

    private Canvas canvas;           // 드래그 좌표 계산용
    private CanvasGroup canvasGroup; // 드래그 중 Raycast 막기/허용
    private RectTransform rectTrans; // 자신의 RectTransform

    private Vector2 originalPosition; // 드래그 시작 시점 위치 저장

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>(); // ★ 필수
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[DraggableItemUI] 부모 Canvas를 찾지 못했습니다.");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 시작 시, 자기 자신에 대한 Raycast를 비활성
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        // 시작 위치 저장(드롭 실패 시 복귀용)
        originalPosition = rectTrans.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null || parentPanel == null) return;

        // 마우스 스크린 좌표 -> parentPanel 기준 로컬 좌표
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentPanel,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        rectTrans.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그 종료 -> 다시 Raycast 유효화
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // 지점 아래에 있는 Character 찾기
        Character targetChar = FindCharacterUnderPointer(eventData);
        if (targetChar != null && currentItem != null)
        {
            // 아이템 효과 적용
            currentItem.ApplyEffectToCharacter(targetChar);

            // 아이템 인벤토리에서 제거(소비 가정)
            var inv = FindFirstObjectByType<ItemInventoryManager>();
            if (inv != null)
            {
                inv.RemoveItem(currentItem);
            }

            // (선택) 인벤토리 새로고침
            var panelMgr = FindFirstObjectByType<ItemPanelManager>();
            if (panelMgr != null)
            {
                panelMgr.RefreshItemPanel();
            }
        }
        else
        {
            // 실패 시 원래 위치로 복귀
            rectTrans.anchoredPosition = originalPosition;
        }
    }

    /// <summary>
    /// PointerEventData로 UI RaycastAll 수행 → Character 컴포넌트 있는지 확인
    /// </summary>
    private Character FindCharacterUnderPointer(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            Character c = r.gameObject.GetComponent<Character>();
            if (c != null)
            {
                return c;
            }
        }
        return null;
    }
}
