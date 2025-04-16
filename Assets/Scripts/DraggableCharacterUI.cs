using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 상의 캐릭터를 드래그하여 다른 타일로 옮길 수 있게 하는 스크립트.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggableCharacterUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    public RectTransform parentPanel;  // 캐릭터가 속한 상위 패널(PlacementManager.tilePanel 등)

    private Canvas canvas;             // UI Raycast용 (같은 Canvas 찾아 사용)
    private CanvasGroup canvasGroup;   // 드래그 중 Raycast blocking 해제용
    private RectTransform rectTrans;   // 자기 RectTransform

    private Character character;       // Character 스크립트(별/공격력/등) 참조

    private Vector2 originalPosition;  // 드래그 시작 위치(실패 시 돌아오기 위함)

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        character = GetComponent<Character>();

        // 혹시 상위에 Canvas가 여러 개면, 가장 근접한 Canvas를 찾는 식으로
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("DraggableCharacterUI: 부모에 Canvas가 없습니다!");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            // 드래그 중 다른 UI Raycast 가능하도록 alphaBlocking 끄기
            canvasGroup.blocksRaycasts = false;
        }

        // 시작 위치 기억
        originalPosition = rectTrans.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // 화면 좌표에서 로컬 좌표로 변환
        Vector2 movePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentPanel,
            eventData.position,
            eventData.pressEventCamera,
            out movePos
        );
        rectTrans.anchoredPosition = movePos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // 드롭된 위치에 Tile이 있는지 Raycast
        Tile targetTile = GetTileUnderPointer(eventData);

        if (targetTile != null)
        {
            // PlacementManager에 드롭 처리 위임 (합성/이동 로직)
            PlacementManager.Instance.OnDropCharacter(character, targetTile);
        }
        else
        {
            // 만약 타일이 없으면 -> 원래 위치로 복귀
            rectTrans.anchoredPosition = originalPosition;
        }
    }

    /// <summary>
    /// 이벤트 데이터의 position 아래에 있는 Tile(가장 위의)을 찾는다.
    /// </summary>
    private Tile GetTileUnderPointer(PointerEventData eventData)
    {
        // Raycast
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            Tile t = r.gameObject.GetComponent<Tile>();
            if (t != null)
            {
                return t;
            }
        }
        return null;
    }
}
