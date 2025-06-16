using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 월드 좌표 기반 타일 클래스
/// 캐릭터 배치, 이동 경로 등을 관리합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Tile : MonoBehaviour
{
    [Header("타일 기본 설정")]
    [SerializeField] private TileType tileType = TileType.None;
    [SerializeField] private int tileIndex;
    [SerializeField] public bool isBlocked = false;
    [SerializeField] public bool isRegion2 = false;

    [Header("시각적 프리팹")]
    [SerializeField] private GameObject placeTilePrefab;
    [SerializeField] private GameObject placedTilePrefab;
    [SerializeField] private GameObject placeableTilePrefab;
    [SerializeField] private GameObject walkableTilePrefab;
    [SerializeField] private GameObject blockedTilePrefab;

    [Header("색상 설정")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color blockedColor = Color.red;
    [SerializeField] private Color validPlacementColor = new Color(0.5f, 1f, 0.5f, 0.8f);

    [Header("캐릭터 배치")]
    [SerializeField] private int maxCharactersPerTile = 3;
    private List<Character> occupyingCharacters = new List<Character>();

    // 컴포넌트
    private SpriteRenderer spriteRenderer;
    private Collider2D col2D;
    private GameObject currentVisual;

    // 상태
    private bool isHighlighted = false;
    private Color currentColor;

    public enum TileType
    {
        None,
        PlaceTile,
        Placed,
        Placeable,
        Walkable,
        WalkableLeft,
        WalkableCenter,
        WalkableRight,
        Place2Tile,
        Placed2,
        Placeable2,
        Walkable2,
        Walkable2Left,
        Walkable2Center,
        Walkable2Right
    }

    private void Awake()
    {
        col2D = GetComponent<Collider2D>();
        if (col2D == null)
        {
            col2D = gameObject.AddComponent<BoxCollider2D>();
        }
        col2D.isTrigger = true;

        // 스프라이트 렌더러 찾기 또는 생성
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            GameObject spriteObj = new GameObject("TileSprite");
            spriteObj.transform.SetParent(transform);
            spriteObj.transform.localPosition = Vector3.zero;
            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "Tiles";
            spriteRenderer.sortingOrder = 0;
        }

        currentColor = defaultColor;
        
        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer("Tile");
    }

    private void Start()
    {
        UpdateTileVisual();
        UpdateTileColor();
    }

    /// <summary>
    /// 타일 타입 설정
    /// </summary>
    public void SetTileType(TileType newType)
    {
        tileType = newType;
        UpdateTileVisual();
        UpdateTileColor();
    }

    /// <summary>
    /// 타일 타입 확인 메서드들
    /// </summary>
    public bool IsPlaceTile() => tileType == TileType.PlaceTile;
    public bool IsPlaced() => tileType == TileType.Placed;
    public bool IsPlacable() => tileType == TileType.Placeable;
    public bool IsWalkable() => tileType == TileType.Walkable;
    public bool IsWalkableLeft() => tileType == TileType.WalkableLeft;
    public bool IsWalkableCenter() => tileType == TileType.WalkableCenter;
    public bool IsWalkableRight() => tileType == TileType.WalkableRight;
    public bool IsPlaced2() => tileType == TileType.Placed2;
    public bool IsPlacable2() => tileType == TileType.Placeable2;
    public bool IsWalkable2() => tileType == TileType.Walkable2;
    public bool IsWalkable2Left() => tileType == TileType.Walkable2Left;
    public bool IsWalkable2Center() => tileType == TileType.Walkable2Center;
    public bool IsWalkable2Right() => tileType == TileType.Walkable2Right;

    /// <summary>
    /// 타일이 이동 가능한 경로인지 확인
    /// </summary>
    public bool IsWalkablePath()
    {
        return IsWalkable() || IsWalkableLeft() || IsWalkableCenter() || IsWalkableRight() ||
               IsWalkable2() || IsWalkable2Left() || IsWalkable2Center() || IsWalkable2Right();
    }

    /// <summary>
    /// 타일이 배치 가능한지 확인
    /// </summary>
    public bool IsPlaceableType()
    {
        return IsPlaceTile() || IsPlaced() || IsPlacable() || 
               IsPlaced2() || IsPlacable2();
    }

    /// <summary>
    /// 캐릭터를 타일에 추가
    /// </summary>
    public bool AddOccupyingCharacter(Character character)
    {
        if (character == null) return false;
        
        if (!occupyingCharacters.Contains(character))
        {
            occupyingCharacters.Add(character);
            UpdateCharacterPositions();
            
            // PlaceTile로 변경
            if (IsPlacable())
            {
                SetTileType(TileType.PlaceTile);
            }
            else if (IsPlacable2())
            {
                SetTileType(TileType.Place2Tile);
            }
            
            Debug.Log($"[Tile] {name}에 {character.characterName} 추가됨. 현재 캐릭터 수: {occupyingCharacters.Count}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 캐릭터를 타일에서 제거
    /// </summary>
    public bool RemoveOccupyingCharacter(Character character)
    {
        if (character == null) return false;
        
        bool removed = occupyingCharacters.Remove(character);
        if (removed)
        {
            UpdateCharacterPositions();
            
            // 타일이 비었으면 Placeable로 변경
            if (occupyingCharacters.Count == 0)
            {
                if (IsPlaceTile())
                {
                    SetTileType(TileType.Placeable);
                }
                else if (IsPlaced2())
                {
                    SetTileType(TileType.Placeable2);
                }
            }
            
            Debug.Log($"[Tile] {name}에서 {character.characterName} 제거됨. 남은 캐릭터 수: {occupyingCharacters.Count}");
        }
        return removed;
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
            case TileType.Place2Tile:
                return placedTilePrefab;
            case TileType.Placed:
            case TileType.Placed2:
                return placedTilePrefab;
            case TileType.Placeable:
            case TileType.Placeable2:
                return placeableTilePrefab;
            case TileType.Walkable:
            case TileType.WalkableLeft:
            case TileType.WalkableCenter:
            case TileType.WalkableRight:
            case TileType.Walkable2:
            case TileType.Walkable2Left:
            case TileType.Walkable2Center:
            case TileType.Walkable2Right:
                return walkableTilePrefab;
            default:
                return null;
        }
    }

    /// <summary>
    /// 타일 색상 업데이트
    /// </summary>
    private void UpdateTileColor()
    {
        if (spriteRenderer == null) return;

        if (isBlocked)
        {
            currentColor = blockedColor;
        }
        else if (isHighlighted)
        {
            currentColor = highlightColor;
        }
        else if (IsPlaceableType() && occupyingCharacters.Count == 0)
        {
            currentColor = validPlacementColor;
        }
        else
        {
            currentColor = defaultColor;
        }

        spriteRenderer.color = currentColor;
    }

    /// <summary>
    /// 타일 하이라이트
    /// </summary>
    public void HighlightTile(bool canPlace = true)
    {
        isHighlighted = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = canPlace ? highlightColor : blockedColor;
        }
    }

    public void UnhighlightTile()
    {
        isHighlighted = false;
        UpdateTileColor();
    }

    /// <summary>
    /// 점유 중인 캐릭터 목록 반환
    /// </summary>
    public List<Character> GetOccupyingCharacters()
    {
        // null 제거
        occupyingCharacters.RemoveAll(c => c == null);
        return new List<Character>(occupyingCharacters);
    }

    /// <summary>
    /// 타일 정리 (null 캐릭터 제거)
    /// </summary>
    public void CleanupNullCharacters()
    {
        occupyingCharacters.RemoveAll(c => c == null);
        UpdateCharacterPositions();
        
        if (occupyingCharacters.Count == 0 && (IsPlaceTile() || IsPlaced2()))
        {
            SetTileType(isRegion2 ? TileType.Placeable2 : TileType.Placeable);
        }
    }

#if UNITY_EDITOR
    public void RefreshInEditor()
    {
        if (Application.isPlaying)
        {
            UpdateTileVisual();
            UpdateTileColor();
        }
    }

    private void OnDrawGizmos()
    {
        // 타일 타입에 따른 기즈모 표시
        Gizmos.color = isBlocked ? Color.red : Color.green;
        
        if (IsWalkablePath())
        {
            Gizmos.color = Color.blue;
        }
        else if (IsPlaceableType())
        {
            Gizmos.color = Color.yellow;
        }
        
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
    }
#endif
}