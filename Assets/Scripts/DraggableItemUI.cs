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

    // 드래그 시작점에서의 오프셋(마우스 좌표 - UI 로컬 좌표)
    private Vector2 dragOffset;

    // 드래그 시작 시점 위치(실패 시 복귀용)
    private Vector2 originalPosition;

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[DraggableItemUI] 부모 Canvas를 찾지 못했습니다.");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // (1) 만약 currentItem이 null이면(아이템이 없는 빈칸),
        //     드래그 자체를 취소하고 이벤트를 소비합니다.
        if (currentItem == null)
        {
            eventData.Use(); // 즉시 드래그 이벤트 소모(드래그 불가)
            return;
        }

        // (2) 드래그 시작 시, 자기 자신에 대한 Raycast를 비활성
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        // (3) 시작 위치 저장(드롭 실패 시 복귀용)
        originalPosition = rectTrans.anchoredPosition;

        // (4) "마우스 중심"으로 드래그하기 위해
        //     현재 마우스 위치 대비 UI 로컬좌표를 계산하여 dragOffset에 저장
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentPanel != null ? parentPanel : (RectTransform)canvas.transform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint
        );
        dragOffset = rectTrans.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null || parentPanel == null) return;

        // 마우스 스크린 좌표 → parentPanel 기준 로컬 좌표
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentPanel,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        // dragOffset을 더해 "마우스 중심"에 맞춰 이동
        rectTrans.anchoredPosition = localPoint + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // (1) 드래그 종료 -> 다시 Raycast 유효화
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // (2) 마우스 위치 아래에 있는 Character가 있으면 아이템 사용 시도
        Character targetChar = FindCharacterUnderPointer(eventData);
        if (targetChar != null && currentItem != null)
        {
            // 아이템 효과 적용(성공 여부에 따라 처리)
            bool success = currentItem.ApplyEffectToCharacter(targetChar);
            if (success)
            {
                // 효과가 정상적으로 적용된 경우에만 아이템 제거
                var inv = FindFirstObjectByType<ItemInventoryManager>();
                if (inv != null)
                {
                    inv.RemoveItem(currentItem);
                }

                // 인벤토리 패널 갱신
                var panelMgr = FindFirstObjectByType<ItemPanelManager>();
                if (panelMgr != null)
                {
                    panelMgr.RefreshItemPanel();
                }
            }
            else
            {
                // 효과값이 0 이하 등으로 실패 => 원위치로 복귀
                rectTrans.anchoredPosition = originalPosition;
            }
        }
        else
        {
            // 실패 시 원래 위치로 복귀
            rectTrans.anchoredPosition = originalPosition;
        }
    }

    /// <summary>
    /// PointerEventData로 UI RaycastAll을 수행하여
    /// Character 컴포넌트가 있는지 확인
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
