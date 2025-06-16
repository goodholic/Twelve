using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 월드 좌표 기반으로 캐릭터를 드래그하여 다른 타일로 옮기거나 라인을 변경할 수 있게 하는 스크립트.
/// UI 기반이 아닌 월드 좌표 기반으로 작동합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DraggableCharacter : MonoBehaviour
{
    [Header("Drag Settings")]
    [Tooltip("드래그 중 캐릭터 크기 배율")]
    public float dragScale = 1.1f;
    
    [Tooltip("드래그 중 캐릭터를 위로 올리는 오프셋")]
    public float dragYOffset = 0.3f;
    
    [Tooltip("유효한 배치 위치에 있을 때 색상")]
    public Color validDropColor = new Color(0.5f, 1f, 0.5f, 1f);
    
    [Tooltip("유효하지 않은 배치 위치에 있을 때 색상")]
    public Color invalidDropColor = new Color(1f, 0.5f, 0.5f, 1f);
    
    [Header("라인 변경 효과")]
    [Tooltip("라인 변경 시 보여줄 화살표 프리팹")]
    public GameObject lineChangeArrowPrefab;
    
    [Tooltip("드래그 중 현재 라인 하이라이트 색상")]
    public Color currentLineHighlight = new Color(1f, 1f, 0f, 0.5f);

    private Camera mainCamera;
    private Collider2D col2D;
    private SpriteRenderer spriteRenderer;
    private Character character;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Color originalColor;
    private int originalSortingOrder;
    
    private bool isDragging = false;
    private Vector3 dragOffset;
    
    private Tile originalTile;
    private Tile currentHoveredTile;
    private GameObject lineChangeIndicator;
    
    // 라인 변경 감지용
    private RouteType? originalRoute;
    private RouteType? targetRoute;

    private void Awake()
    {
        col2D = GetComponent<Collider2D>();
        if (col2D == null)
        {
            col2D = gameObject.AddComponent<BoxCollider2D>();
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        character = GetComponent<Character>();
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("[DraggableCharacter] Main Camera를 찾을 수 없습니다!");
        }

        originalScale = transform.localScale;
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalSortingOrder = spriteRenderer.sortingOrder;
        }
    }

    private void OnMouseDown()
    {
        if (character == null || mainCamera == null) return;
        
        // 제거 모드인 경우 드래그 대신 제거
        if (PlacementManager.Instance != null && PlacementManager.Instance.removeMode)
        {
            RemoveCharacter();
            return;
        }

        // 드래그 시작
        isDragging = true;
        originalPosition = transform.position;
        originalTile = character.currentTile;
        
        // 원래 라우트 저장
        if (originalTile != null)
        {
            originalRoute = GetTileRoute(originalTile);
        }

        // 마우스와 오브젝트 간의 오프셋 계산
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        dragOffset = transform.position - mouseWorldPos;

        // 드래그 중 시각 효과
        transform.localScale = originalScale * dragScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder + 100; // 드래그 중에는 최상위 표시
        }

        Debug.Log($"[DraggableCharacter] {character.characterName} 드래그 시작");
    }

    private void OnMouseDrag()
    {
        if (!isDragging || mainCamera == null) return;

        // 마우스 위치로 캐릭터 이동
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        transform.position = mouseWorldPos + dragOffset + (Vector3.up * dragYOffset);

        // 마우스 위치의 타일 확인
        CheckHoveredTile(mouseWorldPos);
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;
        
        // 원래 크기와 정렬 순서 복원
        transform.localScale = originalScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.sortingOrder = originalSortingOrder;
        }

        // 라인 변경 표시 제거
        if (lineChangeIndicator != null)
        {
            Destroy(lineChangeIndicator);
        }

        // 드롭 처리
        HandleDrop();

        Debug.Log($"[DraggableCharacter] {character.characterName} 드래그 종료");
    }

    private void CheckHoveredTile(Vector3 mouseWorldPos)
    {
        // Raycast로 타일 확인
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0f, LayerMask.GetMask("Tile"));
        
        Tile hoveredTile = null;
        if (hit.collider != null)
        {
            hoveredTile = hit.collider.GetComponent<Tile>();
        }

        if (hoveredTile != currentHoveredTile)
        {
            // 이전 타일 하이라이트 해제
            if (currentHoveredTile != null)
            {
                currentHoveredTile.UnhighlightTile();
            }

            currentHoveredTile = hoveredTile;

            // 새 타일 하이라이트
            if (currentHoveredTile != null)
            {
                // 유효한 배치 위치인지 확인
                bool canPlace = CanPlaceOnTile(currentHoveredTile);
                
                if (canPlace)
                {
                    currentHoveredTile.HighlightTile();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = validDropColor;
                    }
                }
                else
                {
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = invalidDropColor;
                    }
                }
                
                // 라인 변경 확인
                CheckLineChange(currentHoveredTile);
            }
        }
    }

    private bool CanPlaceOnTile(Tile tile)
    {
        if (tile == null) return false;
        
        // 같은 타일인 경우 가능
        if (tile == originalTile) return true;
        
        // 타일이 비어있고 이동 가능한 경로인지 확인
        if (!tile.IsOccupiedByCharacter())
        {
            // 타워형 캐릭터는 타워 배치 가능한 타일에만
            if (character.isTower)
            {
                return tile.IsTowerPlaceable() || tile.IsTower2Placeable();
            }
            else
            {
                // 이동형 캐릭터는 경로 타일에만
                return tile.IsWalkable() || tile.IsWalkable2() ||
                       tile.IsWalkableLeft() || tile.IsWalkable2Left() ||
                       tile.IsWalkableCenter() || tile.IsWalkable2Center() ||
                       tile.IsWalkableRight() || tile.IsWalkable2Right();
            }
        }
        
        return false;
    }

    private void CheckLineChange(Tile tile)
    {
        if (tile == null) return;
        
        RouteType? newRoute = GetTileRoute(tile);
        
        if (newRoute != targetRoute)
        {
            targetRoute = newRoute;
            
            // 라인 변경 표시 업데이트
            if (lineChangeIndicator != null)
            {
                Destroy(lineChangeIndicator);
            }

            if (originalRoute.HasValue && targetRoute.HasValue && originalRoute != targetRoute)
            {
                // 라인 변경 화살표 표시
                if (lineChangeArrowPrefab != null)
                {
                    lineChangeIndicator = Instantiate(lineChangeArrowPrefab, transform.position, Quaternion.identity);
                    
                    // 화살표 방향 설정
                    if (originalRoute == RouteType.Left && targetRoute == RouteType.Center ||
                        originalRoute == RouteType.Center && targetRoute == RouteType.Right)
                    {
                        lineChangeIndicator.transform.rotation = Quaternion.Euler(0, 0, -90); // 오른쪽
                    }
                    else if (originalRoute == RouteType.Right && targetRoute == RouteType.Center ||
                             originalRoute == RouteType.Center && targetRoute == RouteType.Left)
                    {
                        lineChangeIndicator.transform.rotation = Quaternion.Euler(0, 0, 90); // 왼쪽
                    }
                }
            }
        }
    }

    private void HandleDrop()
    {
        bool dropSuccess = false;

        if (currentHoveredTile != null && CanPlaceOnTile(currentHoveredTile))
        {
            // CharacterMovementManager를 통해 이동 처리
            if (CharacterMovementManager.Instance != null)
            {
                CharacterMovementManager.Instance.OnDropCharacter(character, currentHoveredTile);
                dropSuccess = true;
                
                Debug.Log($"[DraggableCharacter] {character.characterName}을(를) {currentHoveredTile.name}으로 이동 성공");
            }
        }

        if (!dropSuccess)
        {
            // 원래 위치로 복귀
            transform.position = originalPosition;
            Debug.Log($"[DraggableCharacter] {character.characterName} 드롭 실패, 원래 위치로 복귀");
        }
        
        // 타일 하이라이트 해제
        if (currentHoveredTile != null)
        {
            currentHoveredTile.UnhighlightTile();
            currentHoveredTile = null;
        }
    }

    private void RemoveCharacter()
    {
        Debug.Log($"[DraggableCharacter] {character.characterName} 제거");
        
        // 타일에서 제거
        if (character.currentTile != null)
        {
            character.currentTile.RemoveOccupyingCharacter(character);
        }

        // 캐릭터 제거
        Destroy(gameObject);
    }

    private RouteType? GetTileRoute(Tile tile)
    {
        if (tile == null) return null;

        if (tile.IsWalkableLeft() || tile.IsWalkable2Left())
            return RouteType.Left;
        else if (tile.IsWalkableCenter() || tile.IsWalkable2Center())
            return RouteType.Center;
        else if (tile.IsWalkableRight() || tile.IsWalkable2Right())
            return RouteType.Right;
        else if (tile.IsWalkable() || tile.IsWalkable2())
        {
            // 일반 Walkable 타일의 경우 위치로 판단
            if (tile.transform.position.x < -2f)
                return RouteType.Left;
            else if (tile.transform.position.x > 2f)
                return RouteType.Right;
            else
                return RouteType.Center;
        }

        return null;
    }

    private void OnDestroy()
    {
        if (lineChangeIndicator != null)
        {
            Destroy(lineChangeIndicator);
        }
    }
}

// LookAtCamera 클래스는 Character.cs에 이미 정의되어 있으므로 여기서는 제거했습니다.