using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// UI 상의 캐릭터를 드래그하여 다른 타일로 옮길 수 있게 하는 스크립트.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggableCharacterUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    public RectTransform parentPanel;  // 캐릭터가 속한 상위 패널(PlacementManager.tilePanel 등)
    
    [Header("시각적 효과 설정")]
    [Tooltip("드래그 중 캐릭터 크기 배율")]
    public float dragScale = 1.1f;
    
    [Tooltip("드래그 중 캐릭터를 위로 올리는 오프셋 (픽셀)")]
    public float dragYOffset = 30f;
    
    [Tooltip("유효한 배치 위치에 있을 때 색상")]
    public Color validDropColor = new Color(0.5f, 1f, 0.5f, 1f);
    
    [Tooltip("유효하지 않은 배치 위치에 있을 때 색상")]
    public Color invalidDropColor = new Color(1f, 0.5f, 0.5f, 1f);

    private Canvas canvas;             // UI Raycast용 (같은 Canvas 찾아 사용)
    private CanvasGroup canvasGroup;   // 드래그 중 Raycast blocking 해제용
    private RectTransform rectTrans;   // 자기 RectTransform
    private Image characterImage;      // 캐릭터 이미지 (색상 변경용)

    private Character character;       // Character 스크립트(별/공격력/등) 참조

    private Vector2 originalPosition;  // 드래그 시작 위치(실패 시 돌아오기 위함)
    private Vector3 originalScale;     // 원래 크기
    private Color originalColor;       // 원래 색상
    
    private Tile currentHoveredTile;   // 현재 마우스가 위치한 타일

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        character = GetComponent<Character>();
        characterImage = GetComponentInChildren<Image>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 혹시 상위에 Canvas가 여러 개면, 가장 근접한 Canvas를 찾는 식으로
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.Log("DraggableCharacterUI: 부모에 Canvas가 없어 최상위 Canvas를 찾거나 생성합니다.");
            
            // 1. 씬에서 Canvas 찾기
            canvas = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay);
                
            // 2. Canvas를 찾지 못했으면 생성
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("AutoCreatedCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("DraggableCharacterUI: 자동으로 Canvas를 생성했습니다.");
            }
            
            // 3. 현재 오브젝트를 Canvas의 자식으로 설정
            transform.SetParent(canvas.transform, false);
            Debug.Log($"DraggableCharacterUI: {canvas.name}에 연결되었습니다.");
        }
        
        // 기본값 설정
        if (dragScale <= 0) dragScale = 1.1f;
        if (validDropColor.a <= 0) validDropColor = new Color(0.5f, 1f, 0.5f, 1f);
        if (invalidDropColor.a <= 0) invalidDropColor = new Color(1f, 0.5f, 0.5f, 1f);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            // 드래그 중 다른 UI Raycast 가능하도록 alphaBlocking 끄기
            canvasGroup.blocksRaycasts = false;
            // 드래그 중 약간 투명하게
            canvasGroup.alpha = 0.8f;
        }

        // 시작 위치 기억
        originalPosition = rectTrans.anchoredPosition;
        originalScale = rectTrans.localScale;
        
        // 드래그 시작 시 크기 확대
        rectTrans.localScale = originalScale * dragScale;
        
        // 드래그 중인 캐릭터를 최상위로 표시
        transform.SetAsLastSibling();
        
        // 원래 색상 저장 (있을 경우)
        if (characterImage != null)
        {
            originalColor = characterImage.color;
        }
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
        
        // 드래그 중인 캐릭터를 손가락/마우스보다 약간 위에 표시 (더 잘 보이도록)
        movePos.y += dragYOffset;
        
        rectTrans.anchoredPosition = movePos;
        
        // 현재 마우스 아래 타일 확인하고 시각적 피드백 제공
        Tile tile = GetTileUnderPointer(eventData);
        UpdateVisualFeedback(tile);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
        
        // 원래 크기로 복귀
        rectTrans.localScale = originalScale;
        
        // 원래 색상으로 복귀
        if (characterImage != null)
        {
            characterImage.color = originalColor;
        }

        // 드롭된 위치에 Tile이 있는지 Raycast
        Tile targetTile = GetTileUnderPointer(eventData);

        if (targetTile != null && CanPlaceOnTile(targetTile))
        {
            // 배치 성공 시 애니메이션 효과 (살짝 커졌다 원래대로)
            StartSuccessAnimation();
            
            // PlacementManager에 드롭 처리 위임 (합성/이동 로직)
            PlacementManager.Instance.OnDropCharacter(character, targetTile);
        }
        else
        {
            // 만약 타일이 없으면 -> 원래 위치로 복귀 (애니메이션)
            StartReturnAnimation();
        }
        
        // 현재 호버 타일 초기화
        currentHoveredTile = null;
    }

    /// <summary>
    /// 이벤트 데이터의 position 아래에 있는 Tile(가장 위의)을 찾는다.
    /// </summary>
    private Tile GetTileUnderPointer(PointerEventData eventData)
    {
        // Raycast
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
    
    /// <summary>
    /// 주어진 타일에 캐릭터가 배치 가능한지 확인
    /// </summary>
    private bool CanPlaceOnTile(Tile tile)
    {
        // 배치 가능한 타일인지 확인 (PlacementManager의 로직 활용)
        return tile.CanPlaceCharacter();
    }
    
    /// <summary>
    /// 타일 위에 있을 때 시각적 피드백 업데이트
    /// </summary>
    private void UpdateVisualFeedback(Tile tile)
    {
        // 이미지 컴포넌트가 없으면 리턴
        if (characterImage == null) return;
        
        // 현재 호버 중인 타일이 변경된 경우
        if (tile != currentHoveredTile)
        {
            currentHoveredTile = tile;
            
            if (tile == null)
            {
                // 타일이 없으면 원래 색상으로
                characterImage.color = originalColor;
            }
            else if (CanPlaceOnTile(tile))
            {
                // 배치 가능한 타일 위에 있으면 초록색 틴트
                characterImage.color = validDropColor;
            }
            else
            {
                // 배치 불가능한 타일 위에 있으면 빨간색 틴트
                characterImage.color = invalidDropColor;
            }
        }
    }
    
    /// <summary>
    /// 배치 성공 시 애니메이션 재생
    /// </summary>
    private void StartSuccessAnimation()
    {
        // 여기서는 간단하게 처리하지만, 실제로는 코루틴이나 애니메이션 시스템을 활용하면 좋습니다
        // 배치 성공 효과음 재생 (구현 시)
        // AudioManager.Instance.PlaySound("place_success");
    }
    
    /// <summary>
    /// 원래 위치로 돌아가는 애니메이션 재생
    /// </summary>
    private void StartReturnAnimation()
    {
        // 간단히 바로 원위치로 (애니메이션은 추후 구현)
        rectTrans.anchoredPosition = originalPosition;
        
        // 실패 효과음 재생 (구현 시)
        // AudioManager.Instance.PlaySound("place_fail");
    }
}
