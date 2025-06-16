using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 월드 좌표 기반 타일 컴포넌트
/// 게임 기획서: 타일 기반 소환 시스템
/// </summary>
public class Tile : MonoBehaviour
{
    // ================================
    // 타일 타입 정의
    // ================================
    public enum TileType
    {
        None,
        Walkable,
        Walkable2,
        WalkableLeft,
        WalkableCenter,
        WalkableRight,
        Walkable2Left,
        Walkable2Center,
        Walkable2Right,
        Placeable,
        Placeable2,
        PlaceTile,
        Placed2
    }

    [Header("타일 설정")]
    [SerializeField] private TileType tileType = TileType.None;
    [SerializeField] private bool isBlocked = false;
    
    [Header("타일 위치 정보")]
    public int row;
    public int column;
    public int tileIndex;
    public int belongingRoute = -1; // 소속 루트 (0: 왼쪽, 1: 중앙, 2: 오른쪽)
    
    [Header("시각적 표현")]
    [SerializeField] private GameObject walkableTilePrefab;
    [SerializeField] private GameObject placeableTilePrefab;
    [SerializeField] public GameObject placeTilePrefab;  // public으로 변경
    [SerializeField] private GameObject placedTilePrefab;
    [SerializeField] private GameObject blockedTilePrefab;
    
    // 캐릭터 관리
    private List<Character> occupyingCharacters = new List<Character>();
    private const int maxCharactersPerTile = 3; // 같은 종류 캐릭터 최대 3개
    
    // 시각적 오브젝트 참조
    private GameObject currentVisual;
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // 타일 기본 설정
        gameObject.layer = LayerMask.NameToLayer("Default");
        
        // 콜라이더 설정
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }
        collider.isTrigger = true;
        
        UpdateTileVisual();
    }

    // ================================
    // 타일 타입 확인 메서드들
    // ================================
    public bool IsWalkable() => tileType == TileType.Walkable;
    public bool IsWalkable2() => tileType == TileType.Walkable2;
    public bool IsWalkableLeft() => tileType == TileType.WalkableLeft;
    public bool IsWalkableCenter() => tileType == TileType.WalkableCenter;
    public bool IsWalkableRight() => tileType == TileType.WalkableRight;
    public bool IsWalkable2Left() => tileType == TileType.Walkable2Left;
    public bool IsWalkable2Center() => tileType == TileType.Walkable2Center;
    public bool IsWalkable2Right() => tileType == TileType.Walkable2Right;
    public bool IsPlaceable() => tileType == TileType.Placeable;
    public bool IsPlaceable2() => tileType == TileType.Placeable2;
    public bool IsPlaceTile() => tileType == TileType.PlaceTile;
    public bool IsPlaced2() => tileType == TileType.Placed2;
    
    public bool IsWalkableType()
    {
        return IsWalkable() || IsWalkable2() || 
               IsWalkableLeft() || IsWalkableCenter() || IsWalkableRight() ||
               IsWalkable2Left() || IsWalkable2Center() || IsWalkable2Right();
    }
    
    public bool IsPlaceableType()
    {
        return IsPlaceable() || IsPlaceable2() || IsPlaceTile() || IsPlaced2();
    }

    // ================================
    // 타일 상태 변경
    // ================================
    public void SetTileType(TileType newType)
    {
        tileType = newType;
        UpdateTileVisual();
    }
    
    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
        UpdateTileVisual();
    }

    // ================================
    // 캐릭터 관리
    // ================================
    public List<Character> GetOccupyingCharacters()
    {
        // null 캐릭터 제거
        occupyingCharacters.RemoveAll(c => c == null);
        return new List<Character>(occupyingCharacters);
    }
    
    public void AddOccupyingCharacter(Character character)
    {
        if (character != null && !occupyingCharacters.Contains(character))
        {
            occupyingCharacters.Add(character);
            UpdateCharacterPositions();
            
            // PlaceTile로 변경
            if (IsPlaceable())
            {
                SetTileType(TileType.PlaceTile);
            }
            else if (IsPlaceable2())
            {
                SetTileType(TileType.Placed2);
            }
            
            Debug.Log($"[Tile] {name}에 {character.characterName} 추가. 현재 캐릭터 수: {occupyingCharacters.Count}");
        }
    }
    
    public bool RemoveOccupyingCharacter(Character character)
    {
        bool removed = occupyingCharacters.Remove(character);
        if (removed)
        {
            UpdateCharacterPositions();
            Debug.Log($"[Tile] {name}에서 {character.characterName} 제거. 남은 캐릭터 수: {occupyingCharacters.Count}");
        }
        return removed;
    }
    
    public void RemoveAllOccupyingCharacters()
    {
        occupyingCharacters.Clear();
        Debug.Log($"[Tile] {name}의 모든 캐릭터 제거");
    }

    /// <summary>
    /// 캐릭터 배치 가능 여부 확인
    /// </summary>
    public bool CanPlaceCharacter(Character newCharacter = null)
    {
        if (isBlocked) return false;
        if (!IsPlaceableType()) return false;
        
        // 같은 종류의 캐릭터는 3개까지 가능
        if (newCharacter != null && occupyingCharacters.Count > 0)
        {
            Character firstChar = occupyingCharacters[0];
            if (firstChar.characterName == newCharacter.characterName && 
                firstChar.star == newCharacter.star)
            {
                return occupyingCharacters.Count < maxCharactersPerTile;
            }
            else
            {
                return false; // 다른 종류의 캐릭터는 배치 불가
            }
        }
        
        // 빈 타일이거나 새 캐릭터가 없으면 배치 가능
        return occupyingCharacters.Count < 1;
    }

    /// <summary>
    /// 타일 위의 캐릭터들 위치 업데이트
    /// </summary>
    private void UpdateCharacterPositions()
    {
        if (occupyingCharacters.Count == 0) return;
        
        float spacing = 0.3f;
        float startX = -(occupyingCharacters.Count - 1) * spacing * 0.5f;
        
        for (int i = 0; i < occupyingCharacters.Count; i++)
        {
            if (occupyingCharacters[i] != null)
            {
                Vector3 pos = transform.position;
                pos.x += startX + (i * spacing);
                pos.z = -1f - (i * 0.1f); // 깊이 조정
                occupyingCharacters[i].transform.position = pos;
                
                // 크기 조정 (여러 개일수록 작게)
                float scale = 1f - (occupyingCharacters.Count - 1) * 0.1f;
                occupyingCharacters[i].transform.localScale = Vector3.one * scale;
            }
        }
    }

    /// <summary>
    /// 타일 시각적 표현 업데이트
    /// </summary>
    private void UpdateTileVisual()
    {
        if (currentVisual != null)
        {
            Destroy(currentVisual);
        }

        GameObject prefabToUse = SelectVisualPrefab();
        if (prefabToUse != null)
        {
            currentVisual = Instantiate(prefabToUse, transform);
            currentVisual.transform.localPosition = Vector3.zero;
            
            // 시각적 오브젝트의 SpriteRenderer는 비활성화 (메인 SpriteRenderer 사용)
            SpriteRenderer visualSprite = currentVisual.GetComponent<SpriteRenderer>();
            if (visualSprite != null)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = visualSprite.sprite;
                }
                visualSprite.enabled = false;
            }
        }
    }

    private GameObject SelectVisualPrefab()
    {
        if (isBlocked && blockedTilePrefab != null)
            return blockedTilePrefab;

        switch (tileType)
        {
            case TileType.PlaceTile:
            case TileType.Placed2:
                return placedTilePrefab;
            case TileType.Placeable:
            case TileType.Placeable2:
                return placeableTilePrefab;
            case TileType.Walkable:
            case TileType.Walkable2:
            case TileType.WalkableLeft:
            case TileType.WalkableCenter:
            case TileType.WalkableRight:
            case TileType.Walkable2Left:
            case TileType.Walkable2Center:
            case TileType.Walkable2Right:
                return walkableTilePrefab;
            default:
                return null;
        }
    }

    /// <summary>
    /// 마우스 클릭 처리
    /// </summary>
    private void OnMouseDown()
    {
        if (isBlocked) return;
        
        // 타일 클릭 시 캐릭터 소환 시도
        if (IsPlaceableType())
        {
            SummonManager summonManager = SummonManager.Instance;
            if (summonManager != null)
            {
                summonManager.PlaceCharacterOnTile(this);
            }
        }
    }

    /// <summary>
    /// 타일 상태 설정 메서드들
    /// </summary>
    public void SetPlacable()
    {
        SetTileType(TileType.Placeable);
    }
    
    public void SetPlacable2()
    {
        SetTileType(TileType.Placeable2);
    }
    
    /// <summary>
    /// 타일 시각 효과 갱신
    /// </summary>
    public void RefreshTileVisual()
    {
        UpdateTileVisual();
        
        // 캐릭터가 있으면 위치 재조정
        if (occupyingCharacters.Count > 0)
        {
            UpdateCharacterPositions();
        }
    }

    // 디버그용
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = IsPlaceableType() ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
        
        if (occupyingCharacters.Count > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
}