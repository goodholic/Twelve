using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 이동, 드래그 처리
/// 기획서: 드래그로 3라인(좌/중/우) 변경 가능
/// ★★★ 수정: 같은 캐릭터끼리는 한 타일에 최대 3개까지 배치 가능
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

    /// <summary>
    /// ★★★ 수정: 캐릭터 드롭 처리
    /// </summary>
    public void OnDropCharacter(Character movingChar, Tile newTile)
    {
        if (movingChar == null || newTile == null) 
        {
            Debug.LogWarning("[CharacterMovementManager] OnDropCharacter: movingChar 또는 newTile이 null");
            return;
        }

        // 히어로는 타 지역으로 이동할 수 없음
        if (movingChar.isHero)
        {
            // 이동하려는 타일이 다른 지역인지 확인
            bool isMovingToDifferentRegion = false;
            
            if (movingChar.areaIndex == 1 && (newTile.isRegion2 || newTile.IsWalkable2() || newTile.IsWalkable2Left() || 
                newTile.IsWalkable2Center() || newTile.IsWalkable2Right() || newTile.IsPlacable2() || newTile.IsPlaced2()))
            {
                isMovingToDifferentRegion = true;
            }
            else if (movingChar.areaIndex == 2 && (!newTile.isRegion2 && (newTile.IsWalkable() || newTile.IsWalkableLeft() || 
                newTile.IsWalkableCenter() || newTile.IsWalkableRight() || newTile.IsPlacable() || newTile.IsPlaceTile())))
            {
                isMovingToDifferentRegion = true;
            }
            
            if (isMovingToDifferentRegion)
            {
                Debug.LogWarning($"[CharacterMovementManager] 히어로 {movingChar.characterName}은(는) 타 지역으로 이동할 수 없습니다!");
                return;
            }
        }

        // 원래 타일 백업
        Tile oldTile = movingChar.currentTile;

        // ★★★ 수정: 새 타일에 캐릭터 배치 가능한지 확인
        if (!newTile.CanPlaceCharacter(movingChar))
        {
            Debug.LogWarning($"[CharacterMovementManager] {newTile.name}에 {movingChar.characterName}을(를) 배치할 수 없습니다.");
            
            // 원래 위치로 복귀
            if (oldTile != null)
            {
                MoveCharacterToTile(movingChar, oldTile);
            }
            return;
        }

        // 타일에 있는 캐릭터들 확인
        List<Character> targetChars = newTile.GetOccupyingCharacters();
        
        // 같은 캐릭터가 2개 있고, 이동 캐릭터까지 합치면 3개가 되는 경우
        if (targetChars.Count == 2 && 
            targetChars[0].characterName == movingChar.characterName && 
            targetChars[0].star == movingChar.star &&
            movingChar.star != CharacterStar.ThreeStar) // 3성은 합성 불가
        {
            // 합성 처리를 PlacementManager에 위임
            if (PlacementManager.Instance != null)
            {
                PlacementManager.Instance.OnDropCharacter(movingChar, newTile);
                return;
            }
        }

        // 일반 이동 처리
        ProcessCharacterMovement(movingChar, oldTile, newTile);
    }

    /// <summary>
    /// 캐릭터 이동 처리
    /// </summary>
    private void ProcessCharacterMovement(Character movingChar, Tile oldTile, Tile newTile)
    {
        // walkable 타일로 이동하는 경우
        if (newTile.IsWalkable() || newTile.IsWalkableLeft() || newTile.IsWalkableCenter() || newTile.IsWalkableRight())
        {
            if (movingChar.areaIndex == 1)
            {
                HandleWalkableMovement(movingChar, oldTile, newTile, false);
            }
            else
            {
                // walkable은 지역1 전용이므로 지역2 캐릭터는 이동 불가
                Debug.LogWarning("[CharacterMovementManager] 지역2 캐릭터는 walkable 타일로 이동할 수 없습니다.");
                RestoreCharacterPosition(movingChar, oldTile);
            }
        }
        // walkable2 타일로 이동하는 경우
        else if (newTile.IsWalkable2() || newTile.IsWalkable2Left() || newTile.IsWalkable2Center() || newTile.IsWalkable2Right())
        {
            if (movingChar.areaIndex == 2)
            {
                HandleWalkableMovement(movingChar, oldTile, newTile, true);
            }
            else
            {
                // walkable2는 지역2 전용이므로 지역1 캐릭터는 이동 불가
                Debug.LogWarning("[CharacterMovementManager] 지역1 캐릭터는 walkable2 타일로 이동할 수 없습니다.");
                RestoreCharacterPosition(movingChar, oldTile);
            }
        }
        else
        {
            // 일반 타일로 이동
            MoveCharacterToTile(movingChar, newTile);
            movingChar.currentWaypointIndex = -1;
            movingChar.maxWaypointIndex = -1;
            movingChar.isCharAttack = false;
        }
    }

    /// <summary>
    /// Walkable 타일로의 이동 처리
    /// </summary>
    private void HandleWalkableMovement(Character movingChar, Tile oldTile, Tile newTile, bool isArea2)
    {
        // 기존 타일에서 캐릭터 제거
        if (oldTile != null)
        {
            oldTile.RemoveOccupyingCharacter(movingChar);
            if (oldTile.GetOccupyingCharacters().Count == 0)
            {
                TileManager.Instance.RemovePlaceTileChild(oldTile);
            }
        }

        // RouteManager를 통해 루트 설정
        if (RouteManager.Instance != null)
        {
            RouteType selectedRoute;
            if (isArea2)
            {
                selectedRoute = RouteManager.Instance.DetermineRouteFromTile(newTile, GameSceneManager.Instance.spawner2);
            }
            else
            {
                selectedRoute = RouteManager.Instance.DetermineRouteFromTile(newTile, GameSceneManager.Instance.spawner);
            }

            GameObject[] waypoints = null;
            if (isArea2)
            {
                waypoints = RouteManager.Instance.GetWaypointsForRegion2(selectedRoute);
                if (waypoints != null && waypoints.Length > 0)
                {
                    movingChar.currentTile = null;
                    movingChar.currentWaypointIndex = 0;
                    movingChar.pathWaypoints = ConvertGameObjectsToTransforms(waypoints);
                    movingChar.maxWaypointIndex = waypoints.Length - 1;
                    movingChar.areaIndex = 2;
                    movingChar.selectedRoute = selectedRoute;
                    movingChar.isCharAttack = !movingChar.isHero;

                    Debug.Log($"[CharacterMovementManager] 지역2 walkable 이동 => {selectedRoute} 루트 선택");
                }
            }
            else
            {
                waypoints = RouteManager.Instance.GetWaypointsForRegion1(selectedRoute);
                if (waypoints != null && waypoints.Length > 0)
                {
                    movingChar.currentTile = null;
                    movingChar.currentWaypointIndex = 0;
                    movingChar.pathWaypoints = ConvertGameObjectsToTransforms(waypoints);
                    movingChar.maxWaypointIndex = waypoints.Length - 1;
                    movingChar.areaIndex = 1;
                    movingChar.selectedRoute = selectedRoute;
                    movingChar.isCharAttack = !movingChar.isHero;

                    Debug.Log($"[CharacterMovementManager] 지역1 walkable 이동 => {selectedRoute} 루트 선택");
                }
            }
        }
        else
        {
            Debug.LogError("[CharacterMovementManager] RouteManager.Instance가 없습니다!");
            RestoreCharacterPosition(movingChar, oldTile);
        }
    }

    /// <summary>
    /// ★★★ 수정: 캐릭터를 타일로 이동
    /// </summary>
    private void MoveCharacterToTile(Character character, Tile newTile)
    {
        if (character == null || newTile == null) return;
        
        // 이전 타일에서 캐릭터 제거
        if (character.currentTile != null)
        {
            character.currentTile.RemoveOccupyingCharacter(character);
            
            // 이전 타일이 비었으면 정리
            if (character.currentTile.GetOccupyingCharacters().Count == 0)
            {
                TileManager.Instance.OnCharacterRemovedFromTile(character.currentTile);
            }
        }
        
        // 새 타일에 캐릭터 추가
        if (!newTile.AddOccupyingCharacter(character))
        {
            Debug.LogError($"[CharacterMovementManager] {character.characterName}을(를) {newTile.name}에 추가할 수 없습니다!");
            return;
        }
        
        // 캐릭터의 타일 참조 업데이트
        character.currentTile = newTile;
        
        // 타일 상태 업데이트
        if (!newTile.IsPlaceTile() && !newTile.IsPlaced2())
        {
            TileManager.Instance.CreatePlaceTileChild(newTile);
        }
        
        Debug.Log($"[CharacterMovementManager] {character.characterName}을(를) {newTile.name}으로 이동");
    }

    /// <summary>
    /// 캐릭터를 원래 위치로 복귀
    /// </summary>
    private void RestoreCharacterPosition(Character character, Tile originalTile)
    {
        if (character == null) return;

        if (originalTile != null)
        {
            MoveCharacterToTile(character, originalTile);
        }
        else
        {
            // 원래 타일이 없으면 캐릭터 패널의 원래 위치로
            RectTransform charRect = character.GetComponent<RectTransform>();
            if (charRect != null)
            {
                charRect.anchoredPosition = Vector2.zero;
            }
        }
    }

    /// <summary>
    /// ★★★ 수정: 타일에 같은 캐릭터가 2개 있고, 이동 캐릭터까지 합치면 3개가 되는지 확인
    /// 기획서: 1성×3 → 2성, 2성×3 → 3성
    /// </summary>
    private bool CheckAndMergeIfPossible(Character movingChar, Tile tile)
    {
        List<Character> sameCharacters = new List<Character>();
        List<Character> tileChars = tile.GetOccupyingCharacters();
        
        // 타일에서 같은 캐릭터 찾기
        foreach (var otherChar in tileChars)
        {
            if (otherChar == null || otherChar == movingChar) continue;
            
            if (otherChar.star == movingChar.star && otherChar.characterName == movingChar.characterName)
            {
                sameCharacters.Add(otherChar);
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
            
            // 합성 처리를 PlacementManager에 위임
            if (PlacementManager.Instance != null)
            {
                PlacementManager.Instance.OnDropCharacter(movingChar, tile);
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// GameObject 배열을 Transform 배열로 변환
    /// </summary>
    private Transform[] ConvertGameObjectsToTransforms(GameObject[] gameObjects)
    {
        if (gameObjects == null) return null;
        
        Transform[] transforms = new Transform[gameObjects.Length];
        for (int i = 0; i < gameObjects.Length; i++)
        {
            transforms[i] = gameObjects[i] != null ? gameObjects[i].transform : null;
        }
        return transforms;
    }
}