using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 타일 클래스 - 게임 기획서: 타일 기반 소환 시스템
/// ★★★ 수정: 같은 캐릭터끼리는 한 타일에 최대 3개까지 배치 가능
/// </summary>
public class Tile : MonoBehaviour
{
    [Header("타일 상태")]
    public TileType tileType = TileType.None;
    public bool isBlocked = false;
    public bool isRegion2 = false; // 지역2 타일인지 여부

    [Header("타일 인덱스")]
    public int row;
    public int column;
    public int tileIndex;

    [Header("캐릭터 정보")]
    [SerializeField] private List<Character> occupyingCharacters = new List<Character>();
    
    // 이전 버전 호환성을 위한 속성들
    public bool isOccupied => occupyingCharacters.Count > 0;
    public Character occupyingCharacter => occupyingCharacters.Count > 0 ? occupyingCharacters[0] : null;
    
    [Header("라우트 정보")]
    public RouteType belongingRoute = RouteType.Center;

    [Header("타일 프리팹")]
    public GameObject walkableTilePrefab;
    public GameObject placeableTilePrefab;
    public GameObject placedTilePrefab;
    public GameObject blockedTilePrefab;

    private GameObject currentVisual;
    private SpriteRenderer spriteRenderer;
    
    // 하이라이트 효과용
    private Color originalColor;
    private bool isHighlighted = false;

    /// <summary>
    /// 타일 타입 열거형
    /// </summary>
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
        Placed,
        Placed2,
        Blocked
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        UpdateTileVisual();
    }

    // ================================
    // 타일 타입 확인 메서드들
    // ================================
    
    // 지역1 타일 타입
    public bool IsWalkable() => tileType == TileType.Walkable;
    public bool IsWalkableLeft() => tileType == TileType.WalkableLeft;
    public bool IsWalkableCenter() => tileType == TileType.WalkableCenter;
    public bool IsWalkableRight() => tileType == TileType.WalkableRight;
    public bool IsPlacable() => tileType == TileType.Placeable;
    public bool IsPlaceable() => tileType == TileType.Placeable;
    public bool IsPlaceTile() => tileType == TileType.PlaceTile;
    
    // 지역2 타일 타입
    public bool IsWalkable2() => tileType == TileType.Walkable2;
    public bool IsWalkable2Left() => tileType == TileType.Walkable2Left;
    public bool IsWalkable2Center() => tileType == TileType.Walkable2Center;
    public bool IsWalkable2Right() => tileType == TileType.Walkable2Right;
    public bool IsPlacable2() => tileType == TileType.Placeable2;
    public bool IsPlaceable2() => tileType == TileType.Placeable2;
    public bool IsPlaced2() => tileType == TileType.Placed2;
    
    // 추가 메서드들
    public bool IsTowerPlaceable() => IsPlacable() || IsPlaceTile();
    public bool IsTower2Placeable() => IsPlacable2() || IsPlaced2();
    
    // 통합 확인 메서드
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
    
    public void SetPlacable()
    {
        SetTileType(TileType.Placeable);
    }
    
    public void SetPlacable2()
    {
        SetTileType(TileType.Placeable2);
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
        UpdateCharacterPositions();
    }
    
    /// <summary>
    /// 캐릭터 배치 가능 여부 확인
    /// ★★★ 수정: 같은 캐릭터끼리는 3개까지 가능
    /// </summary>
    public bool CanPlaceCharacter(Character character = null)
    {
        // 블록된 타일은 배치 불가
        if (isBlocked) return false;
        
        // 타워 배치 가능 타일
        if (IsPlaceableType())
        {
            // 같은 캐릭터끼리는 3개까지 가능
            if (character != null && occupyingCharacters.Count > 0)
            {
                // 첫 번째 캐릭터와 같은 종류인지 확인
                Character first = occupyingCharacters[0];
                if (first.characterName == character.characterName && first.star == character.star)
                {
                    return occupyingCharacters.Count < 3; // 같은 종류는 3개까지
                }
                return false; // 다른 종류는 불가
            }
            return occupyingCharacters.Count < 3; // 빈 타일이거나 3개 미만
        }
        
        // Walkable 타일은 1개만
        if (IsWalkableType())
        {
            return occupyingCharacters.Count == 0;
        }
        
        return false;
    }

    /// <summary>
    /// 타일에 캐릭터가 있는지 확인
    /// </summary>
    public bool IsOccupiedByCharacter()
    {
        return occupyingCharacters.Count > 0;
    }

    /// <summary>
    /// 단일 캐릭터 설정 (이전 버전 호환성)
    /// </summary>
    public void SetOccupyingCharacter(Character character)
    {
        occupyingCharacters.Clear();
        if (character != null)
        {
            AddOccupyingCharacter(character);
        }
    }

    // ================================
    // 타일 하이라이트
    // ================================
    public void HighlightTile()
    {
        if (!isHighlighted && spriteRenderer != null)
        {
            isHighlighted = true;
            spriteRenderer.color = new Color(originalColor.r * 1.5f, originalColor.g * 1.5f, originalColor.b * 1.5f, 1f);
        }
    }
    
    public void UnhighlightTile()
    {
        if (isHighlighted && spriteRenderer != null)
        {
            isHighlighted = false;
            spriteRenderer.color = originalColor;
        }
    }

    // ================================
    // 시각적 업데이트
    // ================================
    /// <summary>
    /// ★★★ 수정: 캐릭터 위치와 크기 조정
    /// 같은 종류의 캐릭터가 한 타일에 여러 개 있을 때 시각적으로 구분되도록 배치
    /// 1개: 100% 크기로 중앙 배치
    /// 2개: 80% 크기로 좌우 배치
    /// 3개: 70% 크기로 삼각형 배치
    /// </summary>
    private void UpdateCharacterPositions()
    {
        // 캐릭터가 여러 개일 때 위치 조정
        int count = occupyingCharacters.Count;
        if (count == 0) return;
        
        for (int i = 0; i < count; i++)
        {
            if (occupyingCharacters[i] != null)
            {
                Vector3 pos = transform.position;
                float scale = 1f;
                
                // 캐릭터 수에 따른 위치와 크기 조정
                switch (count)
                {
                    case 1:
                        // 1개일 때: 타일 중앙, 원래 크기
                        pos = transform.position;
                        scale = 1f;
                        break;
                        
                    case 2:
                        // 2개일 때: 좌우로 배치, 80% 크기
                        if (i == 0)
                            pos = transform.position + new Vector3(-0.2f, 0, 0);
                        else
                            pos = transform.position + new Vector3(0.2f, 0, 0);
                        scale = 0.8f;
                        break;
                        
                    case 3:
                        // 3개일 때: 삼각형 배치, 70% 크기
                        if (i == 0)
                            pos = transform.position + new Vector3(0, 0.2f, 0);
                        else if (i == 1)
                            pos = transform.position + new Vector3(-0.2f, -0.2f, 0);
                        else
                            pos = transform.position + new Vector3(0.2f, -0.2f, 0);
                        scale = 0.7f;
                        break;
                }
                
                // Z 위치 조정 (깊이)
                pos.z = -1f - (i * 0.1f);
                
                // 위치와 크기 적용
                occupyingCharacters[i].transform.position = pos;
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
            var coreData = CoreDataManager.Instance;
            if (coreData != null && coreData.currentCharacterIndex >= 0)
            {
                SummonManager summonManager = SummonManager.Instance;
                if (summonManager != null && coreData.characterDatabase != null)
                {
                    var allChars = coreData.characterDatabase.currentRegisteredCharacters;
                    if (coreData.currentCharacterIndex < allChars.Length)
                    {
                        CharacterData selectedChar = allChars[coreData.currentCharacterIndex];
                        if (selectedChar != null)
                        {
                            bool isOpponent = !coreData.isHost;
                            summonManager.PlaceCharacterOnTile(selectedChar, this, isOpponent);
                        }
                    }
                }
            }
        }
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

    // 디버그용 - 에디터에서만 실행
    #if UNITY_EDITOR
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
    #endif
}