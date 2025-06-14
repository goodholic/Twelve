using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 타일 상태 관리, 타일 참조 정리
/// 게임 기획서: 타일 기반 소환, 3라인 시스템 관리
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
    /// 3라인별로 타일 분류
    /// </summary>
    private void CategorizeRouteTiles()
    {
        leftRouteTiles.Clear();
        centerRouteTiles.Clear();
        rightRouteTiles.Clear();

        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        
        foreach (var tile in allTiles)
        {
            // Walkable 타일들을 라인별로 분류
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
        
        Debug.Log($"[TileManager] 라인별 타일 분류 완료 - 왼쪽: {leftRouteTiles.Count}, 중앙: {centerRouteTiles.Count}, 오른쪽: {rightRouteTiles.Count}");
    }

    /// <summary>
    /// 소환 가능한 타일 분류
    /// </summary>
    private void CategorizeSummonableTiles()
    {
        playerSummonableTiles.Clear();
        aiSummonableTiles.Clear();

        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        
        foreach (var tile in allTiles)
        {
            if (tile.IsPlacable() || tile.IsPlacable2() || tile.IsPlaceTile() || tile.IsPlaced2())
            {
                if (tile.isRegion2)
                {
                    aiSummonableTiles.Add(tile);
                }
                else
                {
                    playerSummonableTiles.Add(tile);
                }
            }
        }
        
        Debug.Log($"[TileManager] 소환 가능 타일 분류 완료 - 플레이어: {playerSummonableTiles.Count}, AI: {aiSummonableTiles.Count}");
    }

    /// <summary>
    /// 랜덤한 빈 소환 가능 타일 찾기 (게임 기획서: 원 버튼 소환)
    /// </summary>
    public Tile GetRandomEmptySummonableTile(bool isAI)
    {
        List<Tile> availableTiles = new List<Tile>();
        List<Tile> targetTiles = isAI ? aiSummonableTiles : playerSummonableTiles;
        
        foreach (var tile in targetTiles)
        {
            if (tile != null && tile.CanPlaceCharacter())
            {
                availableTiles.Add(tile);
            }
        }
        
        if (availableTiles.Count > 0)
        {
            return availableTiles[Random.Range(0, availableTiles.Count)];
        }
        
        Debug.LogWarning($"[TileManager] {(isAI ? "AI" : "플레이어")}의 빈 소환 타일이 없습니다!");
        return null;
    }

    /// <summary>
    /// 특정 라인의 랜덤 타일 찾기 (게임 기획서: 3라인 시스템)
    /// </summary>
    public Tile GetRandomTileFromRoute(RouteType route, bool isRegion2)
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
            var filteredTiles = routeTiles.Where(t => t.isRegion2 == isRegion2).ToList();
            if (filteredTiles.Count > 0)
            {
                return filteredTiles[Random.Range(0, filteredTiles.Count)];
            }
        }
        
        return null;
    }

    /// <summary>
    /// 캐릭터가 타일을 점유하고 있는지 확인
    /// </summary>
    public bool CheckAnyCharacterHasCurrentTile(Tile tile)
    {
        if (tile == null) return false;

        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        List<Character> occupyingChars = new List<Character>();
        
        foreach (var c in allChars)
        {
            if (c != null && c.currentTile == tile)
            {
                occupyingChars.Add(c);
            }
        }
        
        // 중복 참조 정리 - 첫 번째 캐릭터만 유지
        if (occupyingChars.Count > 1)
        {
            Debug.LogWarning($"[TileManager] {tile.name} 타일에 {occupyingChars.Count}개의 중복 참조 감지! 정리합니다.");
            
            // 첫 번째 캐릭터를 제외한 나머지의 currentTile을 null로 설정
            for (int i = 1; i < occupyingChars.Count; i++)
            {
                if (occupyingChars[i] != null)
                {
                    occupyingChars[i].currentTile = null;
                    Debug.Log($"[TileManager] {occupyingChars[i].characterName}의 중복 타일 참조 제거");
                }
            }
            
            return true; // 첫 번째 캐릭터가 점유중
        }
        
        return occupyingChars.Count > 0;
    }

    /// <summary>
    /// 특정 캐릭터의 타일 참조 정리
    /// </summary>
    public void ClearCharacterTileReference(Character character)
    {
        if (character == null) return;
        
        // 이 캐릭터가 참조하는 타일 초기화
        if (character.currentTile != null)
        {
            Debug.Log($"[TileManager] {character.characterName}의 타일 참조 정리: {character.currentTile.name}");
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
    /// 파괴된 캐릭터 참조 정리
    /// </summary>
    private void CleanupDestroyedCharacterReferences()
    {
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        // 모든 타일 검사
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            // 이 타일을 참조하는 캐릭터가 있는지 확인
            bool hasValidCharacter = false;
            foreach (var c in allChars)
            {
                if (c != null && c.currentTile == tile)
                {
                    hasValidCharacter = true;
                    break;
                }
            }
            
            // PlaceTile인데 캐릭터가 없으면 상태 업데이트
            if (!hasValidCharacter && (tile.IsPlaceTile() || tile.IsPlaced2()))
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
        Debug.Log($"[TileManager] {tile.name} 타일에서 캐릭터 제거 후 상태 업데이트");
    }

    /// <summary>
    /// 특정 지역의 빈 소환 가능 타일 찾기
    /// </summary>
    public Tile FindEmptyPlacedOrPlacableTile(bool isRegion2)
    {
        List<Tile> targetTiles = isRegion2 ? aiSummonableTiles : playerSummonableTiles;
        List<Tile> emptyTiles = new List<Tile>();
        
        foreach (var tile in targetTiles)
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

    /// <summary>
    /// 특정 지역의 빈 Walkable 타일 찾기 (게임 기획서: 3라인 시스템)
    /// </summary>
    public Tile FindEmptyWalkableTile(bool isRegion2)
    {
        RouteType selectedRoute = (RouteType)Random.Range(0, 3);
        Debug.Log($"[TileManager] placed/placable 타일이 꽉 찼으므로 {selectedRoute} 루트의 walkable 타일로 배치");
        
        List<Tile> routeTiles = null;
        switch (selectedRoute)
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
            var filteredTiles = routeTiles.Where(t => t.isRegion2 == isRegion2).ToList();
            if (filteredTiles.Count > 0)
            {
                return filteredTiles[Random.Range(0, filteredTiles.Count)];
            }
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
        
        if (newTile != null && newTile.CanPlaceCharacter())
        {
            // 기존 타일에서 캐릭터 제거
            currentTile.RemoveOccupyingCharacter();
            
            // 새 타일로 캐릭터 이동
            character.currentTile = newTile;
            newTile.SetOccupyingCharacter(character);
            
            // 캐릭터 위치 업데이트
            character.transform.position = newTile.transform.position;
            
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