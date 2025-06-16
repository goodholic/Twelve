using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 타일 관리자 - 게임 기획서: 타일 기반 소환 시스템
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
    
    [Header("타일 분류")]
    private List<Tile> leftRouteTiles = new List<Tile>();
    private List<Tile> centerRouteTiles = new List<Tile>();
    private List<Tile> rightRouteTiles = new List<Tile>();
    
    private List<Tile> playerSummonableTiles = new List<Tile>();
    private List<Tile> aiSummonableTiles = new List<Tile>();
    
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
        CollectAllTiles();
        CategorizeRouteTiles();
        CategorizeSummonableTiles();
    }
    
    /// <summary>
    /// 모든 타일 수집
    /// </summary>
    private void CollectAllTiles()
    {
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        Debug.Log($"[TileManager] 총 {allTiles.Length}개 타일 발견");
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
        
        // 모든 타일 검사
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            // 타일의 캐릭터 리스트 정리
            List<Character> validChars = new List<Character>();
            foreach (var c in tile.GetOccupyingCharacters())
            {
                if (c != null && c != character)
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
    /// 타일 정리 및 상태 업데이트
    /// </summary>
    public void CleanupTiles()
    {
        // 모든 캐릭터 수집
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        HashSet<Character> validCharSet = new HashSet<Character>(allChars);
        
        // 모든 타일 검사
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            // 타일의 캐릭터 리스트 정리
            List<Character> validChars = new List<Character>();
            foreach (var c in tile.GetOccupyingCharacters())
            {
                if (c != null && validCharSet.Contains(c))
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