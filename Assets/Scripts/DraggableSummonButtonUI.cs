using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 '소환 버튼'을 드래그 -> 타일 위 드롭하면
/// PlacementManager.SummonCharacterOnTile(...)로 즉시 소환.
///
/// => "마우스 중심"을 맞추기 위해
///    드래그 시작 시점에 offset( rectPos - pointerPos )을 계산하여 사용
///
/// 드래그 소환 성공 시, CharacterSelectUI.OnDragUseCard(...)도 호출하여
/// next unit 로직을 동일하게 유지함.
/// 
/// (추가) 드래그 중 크기를 축소하여 버튼이 작게 보이도록 처리.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggableSummonButtonUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("드래그로 소환할 캐릭터 인덱스")]
    public int summonCharacterIndex = -1;

    [Header("드래그 이동 기준 패널 (없으면 Canvas 기준)")]
    public RectTransform dragParent;

    // === (추가) CharacterSelectUI 참조
    [HideInInspector]
    public CharacterSelectUI parentSelectUI;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTrans;

    // "마우스 중심" 오프셋
    private Vector2 dragOffset;

    // 드래그 시작 시점의 anchoredPosition(복귀용)
    private Vector2 originalPos;

    // *** 변경점: 드래그 중 축소/복원을 위한 변수
    private Vector3 originalScale;     // 드래그 시작 전에 버튼의 원래 스케일
    [SerializeField] private float dragScaleFactor = 0.5f; // 드래그 시 축소 비율 (0.5=절반크기 등)

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 부모 Canvas 찾기
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[DraggableSummonButtonUI] 부모 Canvas가 없습니다!");
        }
    }

    /// <summary>
    /// 캐릭터 인덱스 세팅(SelectButton에서 전달)
    /// </summary>
    public void SetSummonData(int index)
    {
        summonCharacterIndex = index;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // (1) 만약 인덱스가 -1이면 "빈 버튼" 취급 → 드래그 취소
        if (summonCharacterIndex < 0)
        {
            eventData.Use();
            return;
        }

        // (2) 드래그 시작 시 위치 기억
        originalPos = rectTrans.anchoredPosition;

        // *** 변경점: 드래그 시작 시 원래 스케일 저장 후 축소
        originalScale = rectTrans.localScale;
        rectTrans.localScale = originalScale * dragScaleFactor;

        // (3) 드래그 중 자기 자신에 대한 Raycast 차단
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        // (4) "마우스 중심" 계산
        RectTransform basePanel = (dragParent != null) ? dragParent : (RectTransform)canvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            basePanel,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint
        );
        dragOffset = rectTrans.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        RectTransform basePanel = (dragParent != null) ? dragParent : (RectTransform)canvas.transform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            basePanel,
            eventData.position,
            eventData.pressEventCamera,
            out var localPos))
        {
            // "마우스 중심"을 맞추기 위해 offset 적용
            rectTrans.anchoredPosition = localPos + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // (1) 다시 Raycast 유효화
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // *** 변경점: 드래그 종료 시 스케일 복귀
        rectTrans.localScale = originalScale;

        // (2) 타일 찾기
        Tile tile = FindTileUnderPointer(eventData);
        if (tile != null && summonCharacterIndex >= 0)
        {
            // PlacementManager로 즉시 소환
            PlacementManager.Instance.SummonCharacterOnTile(summonCharacterIndex, tile);

            // next unit 로직
            if (parentSelectUI != null)
            {
                parentSelectUI.OnDragUseCard(summonCharacterIndex);
            }
        }

        // (3) 버튼은 원래 자리로 복귀
        rectTrans.anchoredPosition = originalPos;
    }

    private Tile FindTileUnderPointer(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
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
