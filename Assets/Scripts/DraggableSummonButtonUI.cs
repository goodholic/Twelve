using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

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

    /// <summary>
    /// CharacterSelectUI 참조 (드래그 소환 완료 시 OnDragUseCard를 호출하기 위함)
    /// </summary>
    [HideInInspector]
    public CharacterSelectUI parentSelectUI;

    // =====================================================
    // == (수정 추가) 드래그 시 "공격 범위/형태" 표시용 UI ==
    // =====================================================
    [Header("[수정추가] 공격 범위/형태 표시용 UI")]
    [Tooltip("드래그 시작 시 활성화할 Panel(예: Canvas 아래 Text 등)")]
    public GameObject dragInfoPanel;

    [Tooltip("dragInfoPanel 안에 있는 TextMeshProUGUI")]
    public TextMeshProUGUI dragInfoText;

    // 드래그 중 “버튼”을 축소/복원하기 위한 설정
    [SerializeField] private float dragScaleFactor = 0.5f; // 드래그 시 축소 비율 (0.5=절반크기 등)
    private Vector3 originalScale;     // 드래그 시작 전에 버튼의 원래 스케일

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTrans;

    // "마우스 중심" 오프셋
    private Vector2 dragOffset;

    // 드래그 시작 시점의 anchoredPosition(복귀용)
    private Vector2 originalPos;

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

        // (수정추가) dragInfoPanel이 있으면 기본적으로 비활성
        if (dragInfoPanel != null)
        {
            dragInfoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// CharacterSelectUI에서 이 버튼에 캐릭터 인덱스를 세팅해줄 때 사용
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
            // 이벤트를 소모시켜 드래그를 무효화
            eventData.Use();
            return;
        }

        // (2) 드래그 시작 시 위치 기억
        originalPos = rectTrans.anchoredPosition;

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

        // *** 드래그 시작 시 원래 스케일 저장 후 축소
        originalScale = rectTrans.localScale;
        rectTrans.localScale = originalScale * dragScaleFactor;

        // ================================
        // == (수정 추가) 공격범위 표시 ==
        // ================================
        if (dragInfoPanel != null)
        {
            // 현재 summonCharacterIndex로부터 CharacterData를 찾는다
            if (PlacementManager.Instance != null && 
                PlacementManager.Instance.characterDatabase != null && 
                summonCharacterIndex >= 0)
            {
                // characterDatabase.currentRegisteredCharacters 내에 접근
                var dbArr = PlacementManager.Instance.characterDatabase.currentRegisteredCharacters;
                if (dbArr != null && summonCharacterIndex < dbArr.Length)
                {
                    var cData = dbArr[summonCharacterIndex];
                    if (cData != null)
                    {
                        // Panel 켜고, 텍스트 표시
                        dragInfoPanel.SetActive(true);
                        if (dragInfoText != null)
                        {
                            dragInfoText.text = $"공격 범위: {cData.attackRange}\n" +
                                                $"공격 형태: {cData.rangeType}";
                        }
                    }
                }
            }
        }
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

        // (수정추가) dragInfoPanel도 마우스 근처로 이동시키고 싶다면:
        if (dragInfoPanel != null)
        {
            dragInfoPanel.transform.position = eventData.position; 
            // → UI 좌표계에서 직접 위치 이동 (ex: ScreenPoint)
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // (1) 다시 Raycast 유효화
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // (2) 드래그 종료 시 스케일 복귀
        rectTrans.localScale = originalScale;

        // (3) 타일 찾기
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

        // (4) 버튼은 원래 자리로 복귀
        rectTrans.anchoredPosition = originalPos;

        // ===========================
        // == (수정추가) 표시 종료 ==
        // ===========================
        if (dragInfoPanel != null)
        {
            dragInfoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// PointerEventData로 UI RaycastAll을 수행하여
    /// Tile 컴포넌트가 있는지 확인
    /// </summary>
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
