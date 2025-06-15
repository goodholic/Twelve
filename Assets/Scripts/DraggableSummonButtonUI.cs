using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 캐릭터 '소환 버튼'을 드래그 -> 타일 위 드롭하면
/// PlacementManager.SummonCharacterOnTile(...)로 즉시 소환.
/// ★★★ 수정: 같은 캐릭터끼리는 한 타일에 최대 3개까지 배치 가능
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

    /// <summary>
    /// [게임 기획서] 원 버튼 소환 구현
    /// 클릭 시 랜덤 위치에 캐릭터 소환
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 드래그 중이었다면 클릭 이벤트 무시
        if (isDragging)
        {
            isDragging = false;
            return;
        }

        // 원 버튼 소환이 활성화되어 있고, 유효한 캐릭터 인덱스인 경우
        if (enableOneClickSummon && summonCharacterIndex >= 0)
        {
            Debug.Log($"[DraggableSummonButtonUI] 원 버튼 소환 시도: 캐릭터 인덱스 {summonCharacterIndex}");
            
            // PlacementManager를 통해 랜덤 위치에 소환
            if (PlacementManager.Instance != null)
            {
                // 랜덤 타일 찾기
                Tile randomTile = FindRandomEmptyTile();
                if (randomTile != null)
                {
                    PlacementManager.Instance.SummonCharacterOnTile(summonCharacterIndex, randomTile);
                    
                    // next unit 로직
                    if (parentSelectUI != null)
                    {
                        parentSelectUI.OnDragUseCard(summonCharacterIndex);
                    }
                }
                else
                {
                    Debug.LogWarning("[DraggableSummonButtonUI] 빈 타일을 찾을 수 없습니다!");
                }
            }
        }
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

        isDragging = true;

        // (2) 드래그 시작 시 위치 기억
        originalPos = rectTrans.anchoredPosition;

        // (3) 드래그 중 자기 자신에 대한 Raycast 차단
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        // (4) "마우스 중심" 계산
        RectTransform basePanel = (dragParent != null) ? dragParent : (canvas != null ? canvas.transform as RectTransform : null);
        if (basePanel != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(basePanel, eventData.position, eventData.pressEventCamera, out Vector2 pointerLocalPos);
            dragOffset = rectTrans.anchoredPosition - pointerLocalPos;
        }
        else
        {
            dragOffset = Vector2.zero;
        }

        // (5) 드래그 시작 시 버튼 크기 축소 (효과)
        originalScale = rectTrans.localScale;
        rectTrans.localScale = originalScale * dragScaleFactor;

        // =====================================================
        // == (수정추가) 드래그 시작 시 dragInfoPanel 활성화 ==
        // =====================================================
        if (dragInfoPanel != null)
        {
            dragInfoPanel.SetActive(true);

            // 캐릭터 데이터 가져와서 정보 표시
            ShowCharacterInfo();
        }
    }

    /// <summary>
    /// ★★★ 추가: 드래그 중인 캐릭터 정보 표시
    /// </summary>
    private void ShowCharacterInfo()
    {
        if (dragInfoText == null) return;

        var coreData = CoreDataManager.Instance;
        if (coreData == null || coreData.characterDatabase == null) return;

        if (summonCharacterIndex >= 0 && summonCharacterIndex < coreData.characterDatabase.currentRegisteredCharacters.Length)
        {
            CharacterData charData = coreData.characterDatabase.currentRegisteredCharacters[summonCharacterIndex];
            if (charData != null)
            {
                string info = $"<b>{charData.characterName}</b>\n";
                info += $"공격력: {charData.attackPower}\n";
                info += $"사거리: {charData.attackRange}\n";
                info += $"비용: {charData.cost} 미네랄\n";
                
                if (charData.isAreaAttack)
                {
                    info += $"<color=yellow>범위 공격 (반경: {charData.areaAttackRadius})</color>";
                }
                else
                {
                    info += "단일 공격";
                }
                
                dragInfoText.text = info;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null || summonCharacterIndex < 0) return;

        // dragParent 또는 canvas 기준으로 마우스 위치 얻기
        RectTransform basePanel = (dragParent != null) ? dragParent : canvas.transform as RectTransform;
        if (basePanel != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(basePanel, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPos);
            rectTrans.anchoredPosition = localPointerPos + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (summonCharacterIndex < 0) return;

        // (1) Raycast로 드롭 타겟 찾기
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        Tile droppedTile = null;
        foreach (var result in raycastResults)
        {
            Tile tile = result.gameObject.GetComponent<Tile>();
            if (tile != null)
            {
                droppedTile = tile;
                break;
            }
        }

        // (2) 타일에 드롭했을 때
        if (droppedTile != null)
        {
            // ★★★ 수정: 같은 캐릭터 체크를 위해 PlacementManager가 처리하도록 함
            bool summonSuccess = PlacementManager.Instance.SummonCharacterOnTile(summonCharacterIndex, droppedTile);
            
            if (summonSuccess)
            {
                // next unit 로직
                if (parentSelectUI != null)
                {
                    parentSelectUI.OnDragUseCard(summonCharacterIndex);
                }
            }
        }

        // (3) 원래 위치로 복귀
        rectTrans.anchoredPosition = originalPos;

        // (4) 크기 복원
        rectTrans.localScale = originalScale;

        // (5) Raycast 다시 활성화
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // =====================================================
        // == (수정추가) 드래그 종료 시 dragInfoPanel 비활성화 ==
        // =====================================================
        if (dragInfoPanel != null)
        {
            dragInfoPanel.SetActive(false);
        }

        isDragging = false;
    }

    /// <summary>
    /// 랜덤 빈 타일 찾기 (원 버튼 소환용)
    /// </summary>
    private Tile FindRandomEmptyTile()
    {
        if (TileManager.Instance == null) return null;
        
        // 플레이어 소환 가능 타일 중에서 빈 타일 찾기
        List<Tile> emptyTiles = new List<Tile>();
        
        foreach (var tile in TileManager.Instance.playerSummonableTiles)
        {
            if (tile != null && tile.CanPlaceCharacter())
            {
                emptyTiles.Add(tile);
            }
        }
        
        if (emptyTiles.Count > 0)
        {
            return emptyTiles[Random.Range(0, emptyTiles.Count)];
        }
        
        return null;
    }
}