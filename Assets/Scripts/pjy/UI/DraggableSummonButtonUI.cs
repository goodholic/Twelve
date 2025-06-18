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

        // dragParent 설정
        if (dragParent == null && canvas != null)
        {
            dragParent = canvas.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        originalScale = transform.localScale;
    }

    /// <summary>
    /// 클릭 이벤트 처리 (원 버튼 소환)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enableOneClickSummon || isDragging) return;

        Debug.Log($"[DraggableSummonButtonUI] 원 버튼 소환 클릭! 캐릭터 인덱스: {summonCharacterIndex}");

        // SummonManager를 통해 랜덤 위치에 소환
        SummonManager summonManager = SummonManager.Instance;
        if (summonManager != null && summonCharacterIndex >= 0)
        {
            summonManager.AutoPlaceCharacter(summonCharacterIndex);
            
            // 소환 성공 시 카드 사용 처리
            if (parentSelectUI != null)
            {
                parentSelectUI.OnDragUseCard(summonCharacterIndex);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        
        // 원래 위치 저장
        originalPos = rectTrans.anchoredPosition;

        // 마우스 중심으로 계산
        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragParent,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPos))
        {
            dragOffset = rectTrans.anchoredPosition - localPointerPos;
        }

        // 레이캐스트 차단 해제
        canvasGroup.blocksRaycasts = false;

        // 크기 축소
        transform.localScale = originalScale * dragScaleFactor;

        // (추가) 드래그 정보 패널 활성화
        if (dragInfoPanel != null && dragInfoText != null)
        {
            dragInfoPanel.SetActive(true);

            // 캐릭터 정보 가져오기
            CharacterDatabaseObject db = CoreDataManager.Instance?.characterDatabase;
            if (db != null && summonCharacterIndex >= 0 && summonCharacterIndex < db.currentRegisteredCharacters.Length)
            {
                CharacterData charData = db.currentRegisteredCharacters[summonCharacterIndex];
                if (charData != null)
                {
                    // 공격 형태와 사거리 표시
                    string shapeStr = charData.attackShapeType == AttackShapeType.Sector ? "부채꼴" :
                                    charData.attackShapeType == AttackShapeType.Circle ? "원형" :
                                    charData.attackShapeType == AttackShapeType.Rectangle ? "사각형" : "단일";
                    
                    dragInfoText.text = $"{charData.characterName}\n공격범위: {shapeStr}\n사거리: {charData.range:F1}";
                    
                    // ★★★ 추가: 같은 캐릭터가 있는 타일 표시
                    Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
                    int sameCharCount = 0;
                    foreach (var tile in allTiles)
                    {
                        var chars = tile.GetOccupyingCharacters();
                        if (chars.Count > 0 && chars[0].characterName == charData.characterName)
                        {
                            sameCharCount++;
                        }
                    }
                    
                    if (sameCharCount > 0)
                    {
                        dragInfoText.text += $"\n\n같은 캐릭터 타일: {sameCharCount}개";
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

    /// <summary>
    /// ★★★ 수정: 마우스 아래의 타일 체크 및 배치 가능 여부 표시
    /// </summary>
    private void CheckTileUnderPointer(PointerEventData eventData)
    {
        if (dragInfoText == null) return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

        bool foundTile = false;
        foreach (var hit in hits)
        {
            Tile tile = hit.GetComponent<Tile>();
            if (tile != null && tile.IsPlaceableType())
            {
                foundTile = true;
                
                // 캐릭터 데이터 가져오기
                CharacterDatabaseObject db = CoreDataManager.Instance?.characterDatabase;
                if (db != null && summonCharacterIndex >= 0 && summonCharacterIndex < db.currentRegisteredCharacters.Length)
                {
                    CharacterData charData = db.currentRegisteredCharacters[summonCharacterIndex];
                    
                    // 타일의 현재 캐릭터 확인
                    var occupyingChars = tile.GetOccupyingCharacters();
                    
                    if (occupyingChars.Count == 0)
                    {
                        dragInfoText.text += $"\n\n타일: {tile.name} (배치 가능)";
                        dragInfoText.color = Color.green;
                    }
                    else
                    {
                        Character first = occupyingChars[0];
                        if (first.characterName == charData.characterName && first.star == charData.star)
                        {
                            if (occupyingChars.Count < 3)
                            {
                                dragInfoText.text += $"\n\n타일: {tile.name}\n같은 캐릭터 {occupyingChars.Count}/3개";
                                dragInfoText.color = Color.yellow;
                            }
                            else
                            {
                                dragInfoText.text += $"\n\n타일: {tile.name}\n최대 3개 도달!";
                                dragInfoText.color = Color.red;
                            }
                        }
                        else
                        {
                            dragInfoText.text += $"\n\n타일: {tile.name}\n다른 캐릭터 존재!";
                            dragInfoText.color = Color.red;
                        }
                    }
                }
                break;
            }
        }

        if (!foundTile)
        {
            dragInfoText.color = Color.white;
        }
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

                // ★★★ 수정: 캐릭터 데이터를 가져와서 같은 캐릭터인지 확인
                CharacterDatabaseObject db = CoreDataManager.Instance?.characterDatabase;
                if (db != null && summonCharacterIndex >= 0 && summonCharacterIndex < db.currentRegisteredCharacters.Length)
                {
                    CharacterData charData = db.currentRegisteredCharacters[summonCharacterIndex];
                    
                    // 가상의 캐릭터 생성 (실제 생성은 아님)
                    GameObject tempObj = new GameObject("TempChar");
                    Character tempChar = tempObj.AddComponent<Character>();
                    tempChar.characterName = charData.characterName;
                    tempChar.star = charData.star;
                    
                    bool canPlace = tile.CanPlaceCharacter(tempChar);
                    
                    // 임시 오브젝트 제거
                    Destroy(tempObj);
                    
                    if (canPlace)
                    {
                        TrySummonOnTile(tile);
                        droppedOnTile = true;
                        break;
                    }
                }
            }
        }

        // 2) UI 레이캐스트로도 체크
        if (!droppedOnTile)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.Raycast(eventData, results);
            }

            foreach (var result in results)
            {
                GameObject go = result.gameObject;
                Tile tile = go.GetComponent<Tile>();
                
                if (tile != null)
                {
                    // 위와 동일한 로직
                    CharacterDatabaseObject db = CoreDataManager.Instance?.characterDatabase;
                    if (db != null && summonCharacterIndex >= 0 && summonCharacterIndex < db.currentRegisteredCharacters.Length)
                    {
                        CharacterData charData = db.currentRegisteredCharacters[summonCharacterIndex];
                        
                        GameObject tempObj = new GameObject("TempChar");
                        Character tempChar = tempObj.AddComponent<Character>();
                        tempChar.characterName = charData.characterName;
                        tempChar.star = charData.star;
                        
                        bool canPlace = tile.CanPlaceCharacter(tempChar);
                        
                        Destroy(tempObj);
                        
                        if (canPlace)
                        {
                            Debug.Log($"[DraggableSummonButtonUI] UI 타일 감지: {tile.name}");
                            TrySummonOnTile(tile);
                            droppedOnTile = true;
                            break;
                        }
                    }
                }
            }
        }

        // 3) 드롭 실패 시 원위치로
        if (!droppedOnTile)
        {
            Debug.Log("[DraggableSummonButtonUI] 유효한 타일에 드롭하지 않아 원위치로 복귀");
            rectTrans.anchoredPosition = originalPos;
        }
    }

    /// <summary>
    /// 타일에 소환 시도
    /// </summary>
    private void TrySummonOnTile(Tile tile)
    {
        if (summonCharacterIndex < 0)
        {
            Debug.LogError("[DraggableSummonButtonUI] summonCharacterIndex가 설정되지 않았습니다!");
            return;
        }

        SummonManager summonManager = SummonManager.Instance;
        if (summonManager == null)
        {
            Debug.LogError("[DraggableSummonButtonUI] SummonManager를 찾을 수 없습니다!");
            return;
        }

        // SummonManager를 통해 소환
        bool success = summonManager.SummonOnTile(summonCharacterIndex, tile, false);

        if (success)
        {
            Debug.Log($"[DraggableSummonButtonUI] 소환 성공! 인덱스: {summonCharacterIndex}, 타일: {tile.name}");

            // CharacterSelectUI에게 알림 (next unit 로직)
            if (parentSelectUI != null)
            {
                parentSelectUI.OnDragUseCard(summonCharacterIndex);
            }

            // 버튼 원위치로
            rectTrans.anchoredPosition = originalPos;
        }
        else
        {
            Debug.LogWarning($"[DraggableSummonButtonUI] 소환 실패! 타일: {tile.name}");
            rectTrans.anchoredPosition = originalPos;
        }
    }
    
    /// <summary>
    /// 소환 데이터 설정 (CharacterSelectUI에서 호출)
    /// </summary>
    public void SetSummonData(int characterIndex)
    {
        summonCharacterIndex = characterIndex;
        Debug.Log($"[DraggableSummonButtonUI] 소환 데이터 설정됨: {characterIndex}");
    }
}