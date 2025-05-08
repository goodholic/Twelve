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
    [Tooltip("캐릭터가 속한 상위 패널(예: PlacementManager.tilePanel 등)")]
    public RectTransform parentPanel;  // 캐릭터가 속한 상위 패널

    private Canvas canvas;             // UI Raycast 검사용 (같은 Canvas 찾아 사용)
    private CanvasGroup canvasGroup;   // 드래그 중 Raycast Blocking 해제/복원용
    private RectTransform rectTrans;   // 자기 자신의 RectTransform

    private Character character;       // Character 스크립트(별/공격력/등) 참조

    private Vector2 originalPosition;  // 드래그 시작 시점 위치(실패 시 복귀용)

    private void Awake()
    {
        // 필요한 컴포넌트 할당
        rectTrans = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        character = GetComponent<Character>();

        // 부모 체인에서 가장 가까운 Canvas 찾기
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning($"{nameof(DraggableCharacterUI)}: 부모에 Canvas가 없습니다!");
        }
    }

    /// <summary>
    /// 드래그가 시작될 때 호출되는 메서드
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 중 다른 UI가 Raycast에 걸릴 수 있도록 blocksRaycasts 비활성화
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        // 드래그 시작 시점의 위치를 저장 (드롭 실패 시 복귀용)
        originalPosition = rectTrans.anchoredPosition;
    }

    /// <summary>
    /// 드래그 중 매 프레임 호출되는 메서드
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // Canvas가 없다면 드래그 불가
        if (canvas == null) return;

        // 화면 좌표 -> 상위 패널 내 로컬 좌표로 변환
        Vector2 movePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentPanel,
            eventData.position,
            eventData.pressEventCamera,
            out movePos
        );

        // 실제 UI(RectTransform)의 위치를 이동
        rectTrans.anchoredPosition = movePos;
    }

    /// <summary>
    /// 드래그가 끝났을 때(마우스/터치에서 놓았을 때) 호출되는 메서드
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그가 종료되면 blocksRaycasts 복원
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // 드롭된 위치에 Tile 오브젝트가 있는지 Raycast로 확인
        Tile targetTile = GetTileUnderPointer(eventData);

        // 타일이 있으면 PlacementManager를 통해 캐릭터 드롭 처리 (합성/이동 로직)
        if (targetTile != null)
        {
            PlacementManager.Instance.OnDropCharacter(character, targetTile);
        }
        else
        {
            // 만약 타일이 없으면 -> 원래 위치로 복귀
            rectTrans.anchoredPosition = originalPosition;
        }
    }

    /// <summary>
    /// PointerEventData를 이용하여 하단에 있는 Tile 오브젝트(최상단만) 가져오기
    /// </summary>
    private Tile GetTileUnderPointer(PointerEventData eventData)
    {
        // 모든 레이캐스트 결과를 리스트로 받아옴
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 레이캐스트 결과 중 가장 위에 있는 Tile 반환
        foreach (var r in results)
        {
            Tile t = r.gameObject.GetComponent<Tile>();
            if (t != null)
            {
                return t;
            }
        }

        // 해당되는 Tile이 없으면 null 반환
        return null;
    }
}
