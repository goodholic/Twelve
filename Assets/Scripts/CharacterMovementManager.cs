using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 이동, 드래그 처리
/// 기획서: 드래그로 3라인(좌/중/우) 변경 가능
/// </summary>
public class CharacterMovementManager : MonoBehaviour
{
    private static CharacterMovementManager instance;
    public static CharacterMovementManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CharacterMovementManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CharacterMovementManager");
                    instance = go.AddComponent<CharacterMovementManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void OnDropCharacter(Character movingChar, Tile newTile)
    {
        if (movingChar == null || newTile == null) 
        {
            Debug.LogWarning("[CharacterMovementManager] OnDropCharacter: movingChar 또는 newTile이 null");
            return;
        }

        Debug.Log($"[CharacterMovementManager] 드래그 드롭: {movingChar.characterName} -> {newTile.name}");

        Tile oldTile = movingChar.currentTile;
        
        if (oldTile != null)
        {
            Debug.Log($"[CharacterMovementManager] {movingChar.characterName}의 이전 타일 {oldTile.name} 참조 정리");
            
            if (oldTile.IsPlaceTile() || oldTile.IsPlaced2())
            {
                Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
                bool hasOtherCharacter = false;
                foreach (var c in allChars)
                {
                    if (c != null && c != movingChar && c.currentTile == oldTile)
                    {
                        hasOtherCharacter = true;
                        break;
                    }
                }
                
                if (!hasOtherCharacter)
                {
                    oldTile.RefreshTileVisual();
                    Debug.Log($"[CharacterMovementManager] placed tile {oldTile.name}에서 캐릭터 제거됨");
                }
            }
            else
            {
                TileManager.Instance.RemovePlaceTileChild(oldTile);
            }
        }

        bool occupantExists = TileManager.Instance.CheckAnyCharacterHasCurrentTile(newTile);

        if (occupantExists)
        {
            // 기획서: 같은 캐릭터 3개가 모이면 합성
            bool success = CheckAndMergeIfPossible(movingChar, newTile);
            if (!success)
            {
                if (oldTile != null)
                {
                    MoveCharacterToTile(movingChar, oldTile);
                    TileManager.Instance.CreatePlaceTileChild(oldTile);
                }
            }
        }
        else
        {
            if (!newTile.CanPlaceCharacter())
            {
                if (oldTile != null)
                {
                    MoveCharacterToTile(movingChar, oldTile);
                    TileManager.Instance.CreatePlaceTileChild(oldTile);
                }
                return;
            }

            // 기획서: 드래그로 3라인(좌/중/우) 변경 가능
            if (newTile.IsWalkable() || newTile.IsWalkableLeft() || newTile.IsWalkableCenter() || newTile.IsWalkableRight())
            {
                WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
                if (spawner != null && CoreDataManager.Instance.ourMonsterPanel != null)
                {
                    RouteType selectedRoute = RouteManager.Instance.DetermineRouteFromTile(newTile, spawner);
                    Transform[] waypoints = RouteManager.Instance.GetWaypointsForRoute(spawner, selectedRoute);
                    
                    if (waypoints == null || waypoints.Length == 0)
                    {
                        Debug.LogWarning($"[CharacterMovementManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
                        if (oldTile != null)
                        {
                            MoveCharacterToTile(movingChar, oldTile);
                            TileManager.Instance.CreatePlaceTileChild(oldTile);
                        }
                        return;
                    }
                    
                    Vector3 spawnPos = waypoints[0].position;

                    RectTransform charRect = movingChar.GetComponent<RectTransform>();
                    if (charRect != null)
                    {
                        Vector2 localPos = CoreDataManager.Instance.ourMonsterPanel.InverseTransformPoint(spawnPos);
                        charRect.SetParent(CoreDataManager.Instance.ourMonsterPanel, false);
                        charRect.anchoredPosition = localPos;
                        charRect.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        movingChar.transform.SetParent(null);
                        movingChar.transform.position = spawnPos;
                        movingChar.transform.localRotation = Quaternion.identity;
                    }

                    movingChar.currentTile = null;
                    movingChar.currentWaypointIndex = 0;
                    movingChar.pathWaypoints = waypoints;
                    movingChar.maxWaypointIndex = waypoints.Length - 1;
                    movingChar.areaIndex = 1;
                    movingChar.selectedRoute = selectedRoute;
                    movingChar.isCharAttack = !movingChar.isHero;

                    Debug.Log($"[CharacterMovementManager] 드래그로 (placable→walkable) 이동 => {selectedRoute} 루트 선택, waypoint[0]에서 시작");
                }
                else
                {
                    if (oldTile != null)
                    {
                        MoveCharacterToTile(movingChar, oldTile);
                        TileManager.Instance.CreatePlaceTileChild(oldTile);
                    }
                    return;
                }
            }
            else if (newTile.IsWalkable2() || newTile.IsWalkable2Left() || newTile.IsWalkable2Center() || newTile.IsWalkable2Right())
            {
                WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                if (spawner2 != null && CoreDataManager.Instance.opponentOurMonsterPanel != null)
                {
                    RouteType selectedRoute = RouteManager.Instance.DetermineRouteFromTile(newTile, spawner2);
                    Transform[] waypoints = RouteManager.Instance.GetWaypointsForRoute(spawner2, selectedRoute);
                    
                    if (waypoints == null || waypoints.Length == 0)
                    {
                        Debug.LogWarning($"[CharacterMovementManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
                        if (oldTile != null)
                        {
                            MoveCharacterToTile(movingChar, oldTile);
                            TileManager.Instance.CreatePlaceTileChild(oldTile);
                        }
                        return;
                    }
                    
                    Vector3 spawnPos2 = waypoints[0].position;

                    RectTransform charRect = movingChar.GetComponent<RectTransform>();
                    if (charRect != null)
                    {
                        Vector2 localPos = CoreDataManager.Instance.opponentOurMonsterPanel.InverseTransformPoint(spawnPos2);
                        charRect.SetParent(CoreDataManager.Instance.opponentOurMonsterPanel, false);
                        charRect.localRotation = Quaternion.identity;
                        charRect.anchoredPosition = localPos;
                    }
                    else
                    {
                        movingChar.transform.SetParent(null);
                        movingChar.transform.position = spawnPos2;
                        movingChar.transform.localRotation = Quaternion.identity;
                    }

                    movingChar.currentTile = null;
                    movingChar.currentWaypointIndex = 0;
                    movingChar.pathWaypoints = waypoints;
                    movingChar.maxWaypointIndex = waypoints.Length - 1;
                    movingChar.areaIndex = 2;
                    movingChar.selectedRoute = selectedRoute;
                    movingChar.isCharAttack = !movingChar.isHero;

                    Debug.Log($"[CharacterMovementManager] 드래그로 (placable→walkable2) 이동 => {selectedRoute} 루트 선택, waypoint[0]에서 시작");
                }
                else
                {
                    if (oldTile != null)
                    {
                        MoveCharacterToTile(movingChar, oldTile);
                        TileManager.Instance.CreatePlaceTileChild(oldTile);
                    }
                    return;
                }
            }
            else
            {
                MoveCharacterToTile(movingChar, newTile);
                movingChar.currentWaypointIndex = -1;
                movingChar.maxWaypointIndex = -1;
                movingChar.isCharAttack = false;
            }

            TileManager.Instance.CreatePlaceTileChild(newTile);
            Debug.Log("[CharacterMovementManager] 캐릭터가 새 타일로 이동(또는 웨이포인트 모드) 완료");
        }
    }

    /// <summary>
    /// 타일에 같은 캐릭터가 2개 있고, 이동 캐릭터까지 합치면 3개가 되는지 확인
    /// 기획서: 1성×3 → 2성, 2성×3 → 3성
    /// </summary>
    private bool CheckAndMergeIfPossible(Character movingChar, Tile tile)
    {
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        List<Character> sameCharacters = new List<Character>();
        
        // 타일에서 같은 캐릭터 찾기
        foreach (var otherChar in allChars)
        {
            if (otherChar == null || otherChar == movingChar) continue;
            
            if (otherChar.currentTile == tile)
            {
                if (otherChar.star == movingChar.star && otherChar.characterName == movingChar.characterName)
                {
                    sameCharacters.Add(otherChar);
                }
            }
        }
        
        // 이동 캐릭터 포함해서 3개가 되면 합성
        if (sameCharacters.Count >= 2)
        {
            sameCharacters.Add(movingChar);
            
            // 3성은 더 이상 합성 불가
            if (movingChar.star == CharacterStar.ThreeStar)
            {
                Debug.Log("[CharacterMovementManager] 3성은 더 이상 합성할 수 없습니다.");
                return false;
            }
            
            // MergeManager를 통해 합성 실행
            return MergeManager.Instance.TryMergeCharacter(movingChar, tile);
        }
        
        return false;
    }

    private void MoveCharacterToTile(Character character, Tile tile)
    {
        if (tile == null) return;

        bool isArea2 = tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right() || tile.IsPlacable2() || tile.IsPlaced2();

        RectTransform charRect = character.GetComponent<RectTransform>();
        if (charRect != null)
        {
            RectTransform targetParent;
            if ((tile.IsWalkable() || tile.IsWalkableLeft() || tile.IsWalkableCenter() || tile.IsWalkableRight() || 
                 tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right()) && CoreDataManager.Instance.ourMonsterPanel != null)
            {
                targetParent = CoreDataManager.Instance.ourMonsterPanel;
            }
            else
            {
                targetParent = isArea2
                    ? (CoreDataManager.Instance.opponentCharacterPanel != null ? CoreDataManager.Instance.opponentCharacterPanel : CoreDataManager.Instance.characterPanel)
                    : CoreDataManager.Instance.characterPanel;
            }

            RectTransform tileRect = tile.GetComponent<RectTransform>();
            if (tileRect != null)
            {
                Vector2 localPos = targetParent.InverseTransformPoint(tileRect.transform.position);
                charRect.SetParent(targetParent, false);
                charRect.anchoredPosition = localPos;
                charRect.localRotation = Quaternion.identity;
            }
            else
            {
                character.transform.SetParent(targetParent, false);
                character.transform.position = tile.transform.position;
                character.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            character.transform.position = tile.transform.position;
            character.transform.localRotation = Quaternion.identity;
        }

        character.currentTile = tile;
        character.areaIndex = isArea2 ? 2 : 1;
    }
}