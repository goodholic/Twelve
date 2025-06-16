using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 캐릭터 '소환 버튼'을 드래그 -> 타일 위 드롭하면
/// PlacementManager.SummonCharacterOnTile(...)로 즉시 소환.
/// ★★★ 수정: 같은 캐릭터끼리는 한 타일에 최대 3개까지 배치 가능
/// ★★★ 추가: 50마리 제한 체크
/// 
/// => "마우스 중심"을 맞추기 위해
///    드래그 시작 시점에 offset( rectPos - pointerPos )을 계산하여 사용
///
/// 드래그 소환 성공 시, CharacterSelectUI.OnDragUseCard(...)도 호출하여
/// next unit 로직을 동일하게 유지함.
///
/// (추가) 드래그 중 크기를 축소하여 버튼이 작게 보이도록 처리.
/// 
/// [게임 기획서 주석]
/// 기획서에는 "원 버튼 소환: 미네랄 소모하여 랜덤 캐릭터, 랜덤 위치 소환"이 명시되어 있음.
/// 현재는 드래그 소환 방식으로 구현되어 있으나, 원 버튼 소환을 추가하려면:
/// 1. 버튼 클릭 이벤트 추가 (OnPointerClick 구현)
/// 2. 클릭 시 SummonManager의 랜덤 소환 메서드 호출
/// 3. 랜덤으로 빈 타일 선택하여 캐릭터 배치
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggableSummonButtonUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
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

    [Header("[게임 기획서] 원 버튼 소환 설정")]
    [Tooltip("true면 클릭 시 랜덤 위치에 소환, false면 드래그로만 소환")]
    public bool enableOneClickSummon = false;

    // 드래그 중 "버튼"을 축소/복원하기 위한 설정
    [SerializeField] private float dragScaleFactor = 0.5f; // 드래그 시 축소 비율 (0.5=절반크기 등)
    private Vector3 originalScale;     // 드래그 시작 전에 버튼의 원래 스케일

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTrans;

    // "마우스 중심" 오프셋
    private Vector2 dragOffset;

    // 드래그 시작 시점의 anchoredPosition(복귀용)
    private Vector2 originalPos;

    // 드래그 진행 중인지 체크
    private bool isDragging = false;

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

        if (dragParent == null)
        {
            dragParent = canvas?.transform as RectTransform;
        }

        // 원래 스케일 저장
        originalScale = transform.localScale;
    }

    /// <summary>
    /// OnPointerClick: 원 버튼 소환 구현 (옵션)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 드래그 중이면 클릭 이벤트 무시
        if (isDragging) return;
        
        if (enableOneClickSummon && summonCharacterIndex >= 0)
        {
            Debug.Log($"[DraggableSummonButtonUI] 원 버튼 소환 - 캐릭터 인덱스: {summonCharacterIndex}");
            
            // ★★★ 50마리 제한 체크
            bool isHost = CoreDataManager.Instance?.isHost ?? true;
            if (PlacementManager.Instance != null && !PlacementManager.Instance.CanSummonCharacter(!isHost))
            {
                int currentCount = PlacementManager.Instance.GetCharacterCount(!isHost);
                Debug.LogWarning($"[DraggableSummonButtonUI] 캐릭터 수 제한 도달! 현재: {currentCount}/50");
                return;
            }
            
            // SummonManager를 통해 랜덤 위치에 소환
            SummonManager summonManager = SummonManager.Instance;
            if (summonManager != null)
            {
                // 현재 선택된 캐릭터로 자동 배치
                CoreDataManager.Instance.currentCharacterIndex = summonCharacterIndex;
                summonManager.OnClickAutoPlace();
                
                // 카드 사용 처리
                if (parentSelectUI != null)
                {
                    parentSelectUI.OnDragUseCard(summonCharacterIndex);
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        Debug.Log($"[DraggableSummonButtonUI] 드래그 시작 - 캐릭터 인덱스: {summonCharacterIndex}");

        // ★★★ 50마리 제한 체크
        bool isHost = CoreDataManager.Instance?.isHost ?? true;
        if (PlacementManager.Instance != null && !PlacementManager.Instance.CanSummonCharacter(!isHost))
        {
            int currentCount = PlacementManager.Instance.GetCharacterCount(!isHost);
            Debug.LogWarning($"[DraggableSummonButtonUI] 캐릭터 수 제한 도달! 현재: {currentCount}/50");
            isDragging = false;
            return;
        }

        // 원래 위치 저장
        originalPos = rectTrans.anchoredPosition;

        // 오프셋 계산
        Vector2 localPointerPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragParent,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPos
        );
        dragOffset = rectTrans.anchoredPosition - localPointerPos;

        // 레이캐스트 무시
        canvasGroup.blocksRaycasts = false;

        // 드래그 시 크기 축소
        transform.localScale = originalScale * dragScaleFactor;

        // 부모를 dragParent로 변경
        transform.SetParent(dragParent, false);

        // =====================================
        // == 공격 정보 표시 ==
        // =====================================
        if (dragInfoPanel != null && dragInfoText != null)
        {
            var coreData = CoreDataManager.Instance;
            if (coreData != null && coreData.characterDatabase != null)
            {
                var allChars = coreData.characterDatabase.currentRegisteredCharacters;
                if (summonCharacterIndex >= 0 && summonCharacterIndex < allChars.Length)
                {
                    var charData = allChars[summonCharacterIndex];
                    if (charData != null)
                    {
                        dragInfoPanel.SetActive(true);
                        dragInfoText.text = $"{charData.characterName}\n" +
                                          $"공격력: {charData.attackPower}\n" +
                                          $"사거리: {charData.attackRange}\n" +
                                          $"타입: {charData.rangeType}\n" +
                                          $"범위공격: {(charData.isAreaAttack ? "O" : "X")}";
                    }
                }
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // 마우스 중심으로 버튼 이동
        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragParent,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPos))
        {
            rectTrans.anchoredPosition = localPointerPos + dragOffset;
        }

        // (수정) 마우스 아래의 타일 체크
        CheckTileUnderPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        Debug.Log("[DraggableSummonButtonUI] 드래그 종료");

        // 레이캐스트 복원
        canvasGroup.blocksRaycasts = true;

        // 크기 복원
        transform.localScale = originalScale;

        // 드래그 정보 패널 숨기기
        if (dragInfoPanel != null)
        {
            dragInfoPanel.SetActive(false);
        }

        // 드롭한 위치 체크 (UI + 월드 타일)
        bool droppedOnTile = false;

        // 1) 월드 타일 체크
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

        foreach (var hit in hits)
        {
            Tile tile = hit.GetComponent<Tile>();
            if (tile != null)
            {
                Debug.Log($"[DraggableSummonButtonUI] 타일 감지: {tile.name}, CanPlace: {tile.CanPlaceCharacter()}");

                if (tile.CanPlaceCharacter())
                {
                    TrySummonOnTile(tile);
                    droppedOnTile = true;
                    break;
                }
            }
        }

        // 2) UI 레이캐스트로도 체크
        if (!droppedOnTile)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            eventData.module.GetComponent<GraphicRaycaster>().Raycast(eventData, results);

            foreach (var result in results)
            {
                GameObject go = result.gameObject;
                Tile tile = go.GetComponent<Tile>();
                
                if (tile != null && tile.CanPlaceCharacter())
                {
                    Debug.Log($"[DraggableSummonButtonUI] UI 타일 감지: {tile.name}");
                    TrySummonOnTile(tile);
                    droppedOnTile = true;
                    break;
                }
            }
        }

        // 타일에 드롭하지 않은 경우 원위치로
        if (!droppedOnTile)
        {
            Debug.Log("[DraggableSummonButtonUI] 유효한 타일이 아님 - 원위치로 복귀");
            if (parentSelectUI != null)
            {
                transform.SetParent(parentSelectUI.selectButtonParent.transform, false);
            }
            rectTrans.anchoredPosition = originalPos;
        }
    }

    /// <summary>
    /// (수정 추가) 드래그 중 마우스 아래의 타일을 체크
    /// </summary>
    private void CheckTileUnderPointer(PointerEventData eventData)
    {
        // 월드 좌표로 변환
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

        bool foundValidTile = false;
        foreach (var hit in hits)
        {
            Tile tile = hit.GetComponent<Tile>();
            if (tile != null && tile.CanPlaceCharacter())
            {
                foundValidTile = true;
                break;
            }
        }

        // 유효한 타일 위에 있으면 시각적 피드백 (예: 색상 변경)
        Image buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = foundValidTile ? Color.green : Color.white;
        }
    }

    /// <summary>
    /// 타일에 캐릭터 소환 시도
    /// </summary>
    private void TrySummonOnTile(Tile tile)
    {
        if (summonCharacterIndex < 0)
        {
            Debug.LogError("[DraggableSummonButtonUI] summonCharacterIndex가 유효하지 않습니다!");
            return;
        }

        var coreData = CoreDataManager.Instance;
        if (coreData == null || coreData.characterDatabase == null)
        {
            Debug.LogError("[DraggableSummonButtonUI] CoreDataManager 또는 characterDatabase가 null입니다!");
            return;
        }

        var allChars = coreData.characterDatabase.currentRegisteredCharacters;
        if (summonCharacterIndex >= allChars.Length)
        {
            Debug.LogError($"[DraggableSummonButtonUI] 인덱스 초과: {summonCharacterIndex} >= {allChars.Length}");
            return;
        }

        CharacterData charData = allChars[summonCharacterIndex];
        if (charData == null)
        {
            Debug.LogError($"[DraggableSummonButtonUI] 캐릭터 데이터[{summonCharacterIndex}]가 null입니다!");
            return;
        }

        Debug.Log($"[DraggableSummonButtonUI] {charData.characterName}을(를) {tile.name}에 소환 시도");

        // 미네랄 확인
        bool isHost = coreData.isHost;
        MineralBar mineralBar = isHost ? coreData.region1MineralBar : coreData.region2MineralBar;
        
        if (mineralBar == null || mineralBar.GetMineral() < charData.cost)
        {
            Debug.LogWarning($"[DraggableSummonButtonUI] 미네랄 부족! 필요: {charData.cost}, 현재: {mineralBar?.GetMineral() ?? 0}");
            return;
        }

        // PlacementManager를 통해 소환
        if (PlacementManager.Instance != null)
        {
            // 미네랄 소모
            mineralBar.UseMineral(charData.cost);
            
            Character newChar = PlacementManager.Instance.SummonCharacterOnTile(charData, tile, !isHost);
            
            if (newChar != null)
            {
                Debug.Log($"[DraggableSummonButtonUI] {charData.characterName} 소환 성공!");
                
                // CharacterSelectUI에 드래그 사용 알림
                if (parentSelectUI != null)
                {
                    parentSelectUI.OnDragUseCard(summonCharacterIndex);
                }
                
                // 버튼 제거
                Destroy(gameObject);
            }
            else
            {
                // 소환 실패 시 미네랄 환불
                mineralBar.AddMineral(charData.cost);
                Debug.LogError("[DraggableSummonButtonUI] 캐릭터 소환 실패!");
            }
        }
    }
}