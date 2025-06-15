using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 타일 상태 관리, 타일 참조 정리
/// 게임 기획서: 타일 기반 소환, 3라인 시스템 관리
/// ★★★ 수정: 같은 캐릭터끼리는 한 타일에 최대 3개까지 배치 가능
/// </summary>
public class TileManager : MonoBehaviour
{
    private static TileManager instance;
    public static TileManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<TileManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("TileManager");
                    instance = go.AddComponent<TileManager>();
                }
            }
            return instance;
        }
    }

    [Header("타일 그리드 설정")]
    [Tooltip("7x12 그리드의 모든 타일들")]
    public Tile[,] tileGrid = new Tile[7, 12];

    [Header("3라인 시스템")]
    [Tooltip("왼쪽 라인 타일들")]
    public List<Tile> leftRouteTiles = new List<Tile>();
    [Tooltip("중앙 라인 타일들")]
    public List<Tile> centerRouteTiles = new List<Tile>();
    [Tooltip("오른쪽 라인 타일들")]
    public List<Tile> rightRouteTiles = new List<Tile>();

    [Header("소환 가능 타일")]
    [Tooltip("플레이어가 소환 가능한 타일들")]
    public List<Tile> playerSummonableTiles = new List<Tile>();
    [Tooltip("AI가 소환 가능한 타일들")]
    public List<Tile> aiSummonableTiles = new List<Tile>();

    private float lastCleanupTime = 0f;
    private const float CLEANUP_INTERVAL = 1.0f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        InitializeTileGrid();
        CategorizeRouteTiles();
        CategorizeSummonableTiles();
    }

    private void Update()
    {
        if (Time.time - lastCleanupTime > CLEANUP_INTERVAL)
        {
            CleanupDestroyedCharacterReferences();
            UpdatePlacedTileStates();
            lastCleanupTime = Time.time;
        }
    }

    /// <summary>
    /// 타일 그리드 초기화
    /// </summary>
    private void InitializeTileGrid()
    {
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        
        foreach (var tile in allTiles)
        {
            if (tile.row >= 0 && tile.row < 7 && tile.column >= 0 && tile.column < 12)
            {
                tileGrid[tile.row, tile.column] = tile;
            }
        }
        
        Debug.Log($"[TileManager] 타일 그리드 초기화 완료. 총 {allTiles.Length}개 타일 발견");
    }

    /// <summary>
    /// 3라인 시스템을 위한 타일 분류
    /// </summary>
    private void CategorizeRouteTiles()
    {
        leftRouteTiles.Clear();
        centerRouteTiles.Clear();
        rightRouteTiles.Clear();

        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        
        foreach (var tile in allTiles)
        {
            if (tile.IsWalkableLeft() || tile.IsWalkable2Left())
            {
                leftRouteTiles.Add(tile);
                tile.belongingRoute = RouteType.Left;
            }
            else if (tile.IsWalkableCenter() || tile.IsWalkable2Center())
            {
                centerRouteTiles.Add(tile);
                tile.belongingRoute = RouteType.Center;
            }
            else if (tile.IsWalkableRight() || tile.IsWalkable2Right())
            {
                rightRouteTiles.Add(tile);
                tile.belongingRoute = RouteType.Right;
            }
        }
        
        Debug.Log($"[TileManager] 라인별 타일 분류 완료 - 좌:{leftRouteTiles.Count}, 중앙:{centerRouteTiles.Count}, 우:{rightRouteTiles.Count}");
    }

    /// <summary>
    /// 소환 가능 타일 분류
    /// </summary>
    private void CategorizeSummonableTiles()
    {
        playerSummonableTiles.Clear();
        aiSummonableTiles.Clear();

        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        
        foreach (var tile in allTiles)
        {
            if (tile.IsPlacable() || tile.IsPlaceTile())
            {
                playerSummonableTiles.Add(tile);
            }
            else if (tile.IsPlacable2() || tile.IsPlaced2())
            {
                aiSummonableTiles.Add(tile);
            }
        }
        
        Debug.Log($"[TileManager] 소환 가능 타일 분류 완료 - 플레이어:{playerSummonableTiles.Count}, AI:{aiSummonableTiles.Count}");
    }

    /// <summary>
    /// ★★★ 수정: 특정 타일에 캐릭터가 있는지 확인
    /// </summary>
    public bool CheckAnyCharacterHasCurrentTile(Tile tile)
    {
        if (tile == null) return false;
        
        return tile.GetOccupyingCharacters().Count > 0;
    }

    /// <summary>
    /// 특정 캐릭터의 타일 참조 정리
    /// </summary>
    public void ClearCharacterTileReference(Character character)
    {
        if (character == null) return;
        
        // 이 캐릭터가 참조하는 타일에서 제거
        if (character.currentTile != null)
        {
            character.currentTile.RemoveOccupyingCharacter(character);
            Debug.Log($"[TileManager] {character.characterName}의 타일 참조 정리: {character.currentTile.name}");
            
            // 타일이 비었으면 원래 상태로
            if (character.currentTile.GetOccupyingCharacters().Count == 0)
            {
                OnCharacterRemovedFromTile(character.currentTile);
            }
            
            character.currentTile = null;
        }
    }

    /// <summary>
    /// PlaceTile 자식 오브젝트 생성
    /// </summary>
    public void CreatePlaceTileChild(Tile tile)
    {
        if (tile == null) return;
        
        // 이미 PlaceTile 자식이 있는지 확인
        Transform existingChild = tile.transform.Find("PlaceTile(Clone)");
        if (existingChild != null)
        {
            Debug.Log($"[TileManager] {tile.name}에 이미 PlaceTile 자식이 있습니다.");
            return;
        }
        
        // PlaceTile 프리팹 생성
        if (tile.placeTilePrefab != null)
        {
            GameObject placeChild = Instantiate(tile.placeTilePrefab, tile.transform);
            placeChild.transform.localPosition = Vector3.zero;
            placeChild.transform.localScale = Vector3.one;
            
            // RectTransform 설정
            RectTransform placeRect = placeChild.GetComponent<RectTransform>();
            if (placeRect != null)
            {
                placeRect.anchorMin = Vector2.zero;
                placeRect.anchorMax = Vector2.one;
                placeRect.anchoredPosition = Vector2.zero;
                placeRect.sizeDelta = Vector2.zero;
            }
            
            Debug.Log($"[TileManager] {tile.name}에 PlaceTile 자식 생성 완료");
        }
    }

    /// <summary>
    /// PlaceTile 자식 오브젝트 제거
    /// </summary>
    public void RemovePlaceTileChild(Tile tile)
    {
        if (tile == null) return;
        
        Transform placeChild = tile.transform.Find("PlaceTile(Clone)");
        if (placeChild != null)
        {
            Destroy(placeChild.gameObject);
            Debug.Log($"[TileManager] {tile.name}의 PlaceTile 자식 제거");
        }
    }

    /// <summary>
    /// ★★★ 수정: 파괴된 캐릭터 참조 정리
    /// </summary>
    private void CleanupDestroyedCharacterReferences()
    {
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        // 모든 타일 검사
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            // 타일의 캐릭터 리스트 정리
            List<Character> validChars = new List<Character>();
            foreach (var c in tile.GetOccupyingCharacters())
            {
                if (c != null && allChars.Contains(c))
                {
                    validChars.Add(c);
                }
            }
            
            // 유효하지 않은 캐릭터가 있었다면 리스트 재구성
            if (validChars.Count != tile.GetOccupyingCharacters().Count)
            {
                tile.RemoveAllOccupyingCharacters();
                foreach (var c in validChars)
                {
                    tile.AddOccupyingCharacter(c);
                }
                
                // 타일이 비었으면 상태 업데이트
                if (validChars.Count == 0 && (tile.IsPlaceTile() || tile.IsPlaced2()))
                {
                    if (tile.IsPlaceTile())
                    {
                        tile.SetPlacable();
                    }
                    else if (tile.IsPlaced2())
                    {
                        tile.SetPlacable2();
                    }
                    tile.RefreshTileVisual();
                    Debug.Log($"[TileManager] {tile.name} 타일의 캐릭터가 없어져서 상태 업데이트");
                }
            }
        }
    }

    /// <summary>
    /// PlacedTile 상태 업데이트
    /// </summary>
    private void UpdatePlacedTileStates()
    {
        // 모든 소환 가능 타일 업데이트
        List<Tile> allSummonableTiles = new List<Tile>();
        allSummonableTiles.AddRange(playerSummonableTiles);
        allSummonableTiles.AddRange(aiSummonableTiles);
        
        foreach (var tile in allSummonableTiles)
        {
            if (tile == null) continue;
            
            // 타일 위의 캐릭터 확인
            CheckAnyCharacterHasCurrentTile(tile);
        }
    }

    /// <summary>
    /// 캐릭터가 타일에서 제거되었을 때 호출
    /// </summary>
    public void OnCharacterRemovedFromTile(Tile tile)
    {
        if (tile == null) return;
        
        // 타일이 완전히 비었을 때만 상태 변경
        if (tile.GetOccupyingCharacters().Count == 0)
        {
            // 타일 상태 업데이트
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                // PlaceTile/Placed2는 Placable/Placable2로 변경
                if (tile.IsPlaceTile())
                {
                    tile.SetPlacable();
                }
                else if (tile.IsPlaced2())
                {
                    tile.SetPlacable2();
                }
            }
            
            tile.RefreshTileVisual();
            Debug.Log($"[TileManager] {tile.name} 타일에서 모든 캐릭터 제거 후 상태 업데이트");
        }
    }

    /// <summary>
    /// 특정 지역의 빈 소환 가능 타일 찾기
    /// </summary>
    public Tile FindEmptyPlacedOrPlacableTile(bool isRegion2)
    {
        List<Tile> targetTiles = isRegion2 ? aiSummonableTiles : playerSummonableTiles;
        
        foreach (var tile in targetTiles)
        {
            if (tile != null && tile.CanPlaceCharacter())
            {
                return tile;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 특정 지역의 빈 walkable 타일 찾기
    /// </summary>
    public Tile FindEmptyWalkableTile(bool isRegion2)
    {
        List<Tile> allTiles = new List<Tile>();
        allTiles.AddRange(leftRouteTiles);
        allTiles.AddRange(centerRouteTiles);
        allTiles.AddRange(rightRouteTiles);
        
        // 지역별 필터링
        var regionTiles = allTiles.Where(t => t != null && t.isRegion2 == isRegion2).ToList();
        
        // 빈 타일 찾기
        foreach (var tile in regionTiles)
        {
            if (tile.CanPlaceCharacter())
            {
                return tile;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 특정 라인과 지역의 랜덤 walkable 타일 가져오기
    /// </summary>
    public Tile GetRandomWalkableTileFromRoute(RouteType selectedRoute, bool isRegion2)
    {
        List<Tile> candidateTiles = new List<Tile>();
        
        switch (selectedRoute)
        {
            case RouteType.Left:
                candidateTiles = leftRouteTiles.Where(t => t.isRegion2 == isRegion2).ToList();
                break;
            case RouteType.Center:
                candidateTiles = centerRouteTiles.Where(t => t.isRegion2 == isRegion2).ToList();
                break;
            case RouteType.Right:
                candidateTiles = rightRouteTiles.Where(t => t.isRegion2 == isRegion2).ToList();
                break;
            default:
                Debug.LogWarning($"[TileManager] 잘못된 라인 타입: {selectedRoute}");
                return null;
        }
        
        // 배치 가능한 타일만 필터링
        candidateTiles = candidateTiles.Where(t => t.CanPlaceCharacter()).ToList();
        
        if (candidateTiles.Count > 0)
        {
            return candidateTiles[Random.Range(0, candidateTiles.Count)];
        }
        
        Debug.LogWarning($"[TileManager] 지역{(isRegion2 ? 2 : 1)}에 {selectedRoute} 라인의 walkable 타일이 없습니다!");
        return null;
    }

    /// <summary>
    /// 캐릭터를 특정 라인으로 이동 (게임 기획서: 드래그로 라인 변경)
    /// </summary>
    public bool MoveCharacterToRoute(Character character, RouteType newRoute)
    {
        if (character == null) return false;
        
        // 현재 타일 정보
        Tile currentTile = character.currentTile;
        if (currentTile == null) return false;
        
        // 같은 지역의 새로운 라인에서 빈 타일 찾기
        Tile newTile = GetRandomEmptyTileFromRoute(newRoute, currentTile.isRegion2);
        
        if (newTile != null && newTile.CanPlaceCharacter(character))
        {
            // 기존 타일에서 캐릭터 제거
            currentTile.RemoveOccupyingCharacter(character);
            
            // 타일이 비었으면 정리
            if (currentTile.GetOccupyingCharacters().Count == 0)
            {
                OnCharacterRemovedFromTile(currentTile);
            }
            
            // 새 타일에 캐릭터 추가
            newTile.AddOccupyingCharacter(character);
            character.currentTile = newTile;
            
            Debug.Log($"[TileManager] {character.characterName}을 {newRoute} 라인으로 이동 완료");
            return true;
        }
        
        Debug.LogWarning($"[TileManager] {newRoute} 라인에 이동 가능한 타일이 없습니다");
        return false;
    }

    /// <summary>
    /// 특정 라인의 빈 타일 찾기
    /// </summary>
    private Tile GetRandomEmptyTileFromRoute(RouteType route, bool isRegion2)
    {
        List<Tile> routeTiles = null;
        
        switch (route)
        {
            case RouteType.Left:
                routeTiles = leftRouteTiles;
                break;
            case RouteType.Center:
                routeTiles = centerRouteTiles;
                break;
            case RouteType.Right:
                routeTiles = rightRouteTiles;
                break;
        }
        
        if (routeTiles != null && routeTiles.Count > 0)
        {
            var availableTiles = routeTiles.Where(t => t.isRegion2 == isRegion2 && t.CanPlaceCharacter()).ToList();
            if (availableTiles.Count > 0)
            {
                return availableTiles[Random.Range(0, availableTiles.Count)];
            }
        }
        
        return null;
    }
}