using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 3개 루트 관리, 웨이포인트 설정
/// </summary>
public class RouteManager : MonoBehaviour
{
    private static RouteManager instance;
    public static RouteManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<RouteManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("RouteManager");
                    instance = go.AddComponent<RouteManager>();
                }
            }
            return instance;
        }
    }

    [Header("지역1 중간성/최종성 참조")]
    [Tooltip("지역1 왼쪽 중간성 (체력 500)")]
    public GameObject region1LeftMiddleCastle;
    [Tooltip("지역1 중앙 중간성 (체력 500)")]
    public GameObject region1CenterMiddleCastle;
    [Tooltip("지역1 오른쪽 중간성 (체력 500)")]
    public GameObject region1RightMiddleCastle;
    [Tooltip("지역1 최종성 (체력 1000)")]
    public GameObject region1FinalCastle;
    
    [Header("지역2 중간성/최종성 참조")]
    [Tooltip("지역2 왼쪽 중간성 (체력 500)")]
    public GameObject region2LeftMiddleCastle;
    [Tooltip("지역2 중앙 중간성 (체력 500)")]
    public GameObject region2CenterMiddleCastle;
    [Tooltip("지역2 오른쪽 중간성 (체력 500)")]
    public GameObject region2RightMiddleCastle;
    [Tooltip("지역2 최종성 (체력 1000)")]
    public GameObject region2FinalCastle;

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
    /// 타일 위치에 따른 루트 결정 (게임 기획서: 3라인 시스템)
    /// </summary>
    public RouteType DetermineRouteFromTile(Tile tile, WaveSpawner spawner)
    {
        if (tile == null || spawner == null) return RouteType.Center; // 기본값 중앙
        
        // 타워용 타일은 타일 위치에 따라 가장 가까운 루트 선택
        if (tile.IsPlacable() || tile.IsPlaceTile())
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 타워용 타일 - 위치 기반 루트 선택");
            return GetNearestRouteByPosition(tile, spawner);
        }
        
        // Walkable 타일의 경우 명시적 루트 확인
        if (tile.IsWalkableLeft())
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 WalkableLeft 타일이므로 Left 루트 선택");
            return RouteType.Left;
        }
        if (tile.IsWalkableCenter())
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 WalkableCenter 타일이므로 Center 루트 선택");
            return RouteType.Center;
        }
        if (tile.IsWalkableRight())
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 WalkableRight 타일이므로 Right 루트 선택");
            return RouteType.Right;
        }
        
        // 그 외의 경우 위치 기반으로 결정
        return GetNearestRouteByPosition(tile, spawner);
    }

    /// <summary>
    /// 타일 위치에 따른 가장 가까운 루트 선택
    /// </summary>
    private RouteType GetNearestRouteByPosition(Tile tile, WaveSpawner spawner)
    {
        float tileX = tile.transform.position.x;
        
        float leftMinX = float.MaxValue, leftMaxX = float.MinValue;
        float centerMinX = float.MaxValue, centerMaxX = float.MinValue;
        float rightMinX = float.MaxValue, rightMaxX = float.MinValue;
        
        // 각 루트의 X 범위 계산
        if (spawner.walkableLeft != null && spawner.walkableLeft.Length > 0)
        {
            foreach (var waypoint in spawner.walkableLeft)
            {
                if (waypoint != null)
                {
                    float x = waypoint.position.x;
                    if (x < leftMinX) leftMinX = x;
                    if (x > leftMaxX) leftMaxX = x;
                }
            }
        }
        
        if (spawner.walkableCenter != null && spawner.walkableCenter.Length > 0)
        {
            foreach (var waypoint in spawner.walkableCenter)
            {
                if (waypoint != null)
                {
                    float x = waypoint.position.x;
                    if (x < centerMinX) centerMinX = x;
                    if (x > centerMaxX) centerMaxX = x;
                }
            }
        }
        
        if (spawner.walkableRight != null && spawner.walkableRight.Length > 0)
        {
            foreach (var waypoint in spawner.walkableRight)
            {
                if (waypoint != null)
                {
                    float x = waypoint.position.x;
                    if (x < rightMinX) rightMinX = x;
                    if (x > rightMaxX) rightMaxX = x;
                }
            }
        }
        
        // 각 루트의 중심점
        float leftCenterX = (leftMinX + leftMaxX) / 2;
        float centerCenterX = (centerMinX + centerMaxX) / 2;
        float rightCenterX = (rightMinX + rightMaxX) / 2;
        
        // 타일이 어느 루트 범위에 있는지 확인
        if (leftMinX != float.MaxValue && tileX >= leftMinX && tileX <= leftMaxX)
        {
            Debug.Log($"[RouteManager] 타일 {tile.name} (X: {tileX})은 좌측 루트 범위에 있음");
            return RouteType.Left;
        }
        else if (rightMinX != float.MaxValue && tileX >= rightMinX && tileX <= rightMaxX)
        {
            Debug.Log($"[RouteManager] 타일 {tile.name} (X: {tileX})은 우측 루트 범위에 있음");
            return RouteType.Right;
        }
        else if (centerMinX != float.MaxValue && tileX >= centerMinX && tileX <= centerMaxX)
        {
            Debug.Log($"[RouteManager] 타일 {tile.name} (X: {tileX})은 중앙 루트 범위에 있음");
            return RouteType.Center;
        }
        
        // 범위에 없으면 가장 가까운 루트 선택
        float distToLeft = (leftCenterX != float.MaxValue) ? Mathf.Abs(tileX - leftCenterX) : float.MaxValue;
        float distToCenter = (centerCenterX != float.MaxValue) ? Mathf.Abs(tileX - centerCenterX) : float.MaxValue;
        float distToRight = (rightCenterX != float.MaxValue) ? Mathf.Abs(tileX - rightCenterX) : float.MaxValue;
        
        Debug.Log($"[RouteManager] 타일 {tile.name} (X: {tileX}) - 좌측 거리: {distToLeft}, 중앙 거리: {distToCenter}, 우측 거리: {distToRight}");
        
        if (distToLeft < distToCenter && distToLeft < distToRight)
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 좌측 루트 선택 (가장 가까움)");
            return RouteType.Left;
        }
        else if (distToRight < distToCenter && distToRight < distToLeft)
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 우측 루트 선택 (가장 가까움)");
            return RouteType.Right;
        }
        else
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 중앙 루트 선택 (기본값)");
            return RouteType.Center;
        }
    }

    public RouteType DetermineRouteFromTile(Tile tile, WaveSpawnerRegion2 spawner)
    {
        if (tile == null || spawner == null) return RouteType.Center;
        
        if (tile.IsPlacable2() || tile.IsPlaced2())
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 타워용 타일 - 위치 기반 루트 선택");
            return GetNearestRouteByPositionRegion2(tile, spawner);
        }
        
        if (tile.IsWalkable2Left())
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 Walkable2Left 타일이므로 Left 루트 선택");
            return RouteType.Left;
        }
        if (tile.IsWalkable2Center())
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 Walkable2Center 타일이므로 Center 루트 선택");
            return RouteType.Center;
        }
        if (tile.IsWalkable2Right())
        {
            Debug.Log($"[RouteManager] 타일 {tile.name}은 Walkable2Right 타일이므로 Right 루트 선택");
            return RouteType.Right;
        }
        
        return GetNearestRouteByPositionRegion2(tile, spawner);
    }

    private RouteType GetNearestRouteByPositionRegion2(Tile tile, WaveSpawnerRegion2 spawner)
    {
        float tileX = tile.transform.position.x;
        
        float leftAvgX = 0f, centerAvgX = 0f, rightAvgX = 0f;
        int leftCount = 0, centerCount = 0, rightCount = 0;
        
        if (spawner.walkableLeft2 != null)
        {
            foreach (var waypoint in spawner.walkableLeft2)
            {
                if (waypoint != null)
                {
                    leftAvgX += waypoint.position.x;
                    leftCount++;
                }
            }
            if (leftCount > 0) leftAvgX /= leftCount;
        }
        
        if (spawner.walkableCenter2 != null)
        {
            foreach (var waypoint in spawner.walkableCenter2)
            {
                if (waypoint != null)
                {
                    centerAvgX += waypoint.position.x;
                    centerCount++;
                }
            }
            if (centerCount > 0) centerAvgX /= centerCount;
        }
        
        if (spawner.walkableRight2 != null)
        {
            foreach (var waypoint in spawner.walkableRight2)
            {
                if (waypoint != null)
                {
                    rightAvgX += waypoint.position.x;
                    rightCount++;
                }
            }
            if (rightCount > 0) rightAvgX /= rightCount;
        }
        
        float distToLeft = (leftCount > 0) ? Mathf.Abs(tileX - leftAvgX) : float.MaxValue;
        float distToCenter = (centerCount > 0) ? Mathf.Abs(tileX - centerAvgX) : float.MaxValue;
        float distToRight = (rightCount > 0) ? Mathf.Abs(tileX - rightAvgX) : float.MaxValue;
        
        if (distToLeft <= distToCenter && distToLeft <= distToRight)
        {
            return RouteType.Left;
        }
        else if (distToRight <= distToCenter && distToRight <= distToLeft)
        {
            return RouteType.Right;
        }
        
        return RouteType.Center;
    }

    /// <summary>
    /// 웨이포인트 상실 시 중간성/최종성 목표 설정
    /// </summary>
    public Transform[] GetWaypointsForRoute(WaveSpawner spawner, RouteType route)
    {
        Transform[] waypoints = null;
        
        switch (route)
        {
            case RouteType.Left:
                waypoints = spawner.walkableLeft;
                break;
            case RouteType.Center:
                waypoints = spawner.walkableCenter;
                break;
            case RouteType.Right:
                waypoints = spawner.walkableRight;
                break;
            default:
                waypoints = spawner.walkableCenter;
                break;
        }
        
        if (waypoints != null && waypoints.Length > 0)
        {
            List<Transform> validWaypoints = new List<Transform>();
            
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    validWaypoints.Add(waypoints[i]);
                }
                else
                {
                    Debug.LogWarning($"[RouteManager] {route} 루트의 웨이포인트[{i}]가 null입니다!");
                }
            }
            
            if (validWaypoints.Count > 0)
            {
                // 웨이포인트 간 거리 검사
                for (int i = 0; i < validWaypoints.Count - 1; i++)
                {
                    float distance = Vector2.Distance(validWaypoints[i].position, validWaypoints[i + 1].position);
                    if (distance > 15f)
                    {
                        Debug.LogWarning($"[RouteManager] {route} 루트 웨이포인트[{i}]→[{i+1}] 거리가 너무 멉니다: {distance}");
                    }
                }
                
                Debug.Log($"[RouteManager] {route} 루트 웨이포인트 검증 완료: {validWaypoints.Count}개 (원본: {waypoints.Length}개)");
                return validWaypoints.ToArray();
            }
            else
            {
                Debug.LogError($"[RouteManager] {route} 루트에 유효한 웨이포인트가 없습니다!");
                return GetFallbackWaypoints(route, 1); // 지역1용 fallback
            }
        }
        
        Debug.LogWarning($"[RouteManager] {route} 루트의 웨이포인트 배열이 null이거나 비어있습니다!");
        return GetFallbackWaypoints(route, 1); // 지역1용 fallback
    }

    /// <summary>
    /// 웨이포인트 상실 시 중간성/최종성으로 가는 경로 반환
    /// </summary>
    private Transform[] GetFallbackWaypoints(RouteType route, int areaIndex)
    {
        List<Transform> fallbackWaypoints = new List<Transform>();
        
        if (areaIndex == 1)
        {
            // 게임 기획서: 웨이포인트 상실 시 중간성 목표
            switch (route)
            {
                case RouteType.Left:
                    if (region1LeftMiddleCastle != null)
                    {
                        fallbackWaypoints.Add(region1LeftMiddleCastle.transform);
                        Debug.Log("[RouteManager] 지역1 왼쪽 루트 웨이포인트 상실 → 왼쪽 중간성(체력 500) 목표");
                    }
                    break;
                case RouteType.Right:
                    if (region1RightMiddleCastle != null)
                    {
                        fallbackWaypoints.Add(region1RightMiddleCastle.transform);
                        Debug.Log("[RouteManager] 지역1 오른쪽 루트 웨이포인트 상실 → 오른쪽 중간성(체력 500) 목표");
                    }
                    break;
                case RouteType.Center:
                    if (region1CenterMiddleCastle != null)
                    {
                        fallbackWaypoints.Add(region1CenterMiddleCastle.transform);
                        Debug.Log("[RouteManager] 지역1 중앙 루트 웨이포인트 상실 → 중앙 중간성(체력 500) 목표");
                    }
                    break;
            }
            
            // 중간성이 파괴되었거나 없으면 최종성으로
            if (fallbackWaypoints.Count == 0 && region1FinalCastle != null)
            {
                fallbackWaypoints.Add(region1FinalCastle.transform);
                Debug.Log("[RouteManager] 지역1 중간성이 파괴됨 → 최종성(체력 1000) 목표");
            }
        }
        else if (areaIndex == 2)
        {
            // 지역2용 fallback
            switch (route)
            {
                case RouteType.Left:
                    if (region2LeftMiddleCastle != null)
                    {
                        fallbackWaypoints.Add(region2LeftMiddleCastle.transform);
                        Debug.Log("[RouteManager] 지역2 왼쪽 루트 웨이포인트 상실 → 왼쪽 중간성(체력 500) 목표");
                    }
                    break;
                case RouteType.Right:
                    if (region2RightMiddleCastle != null)
                    {
                        fallbackWaypoints.Add(region2RightMiddleCastle.transform);
                        Debug.Log("[RouteManager] 지역2 오른쪽 루트 웨이포인트 상실 → 오른쪽 중간성(체력 500) 목표");
                    }
                    break;
                case RouteType.Center:
                    if (region2CenterMiddleCastle != null)
                    {
                        fallbackWaypoints.Add(region2CenterMiddleCastle.transform);
                        Debug.Log("[RouteManager] 지역2 중앙 루트 웨이포인트 상실 → 중앙 중간성(체력 500) 목표");
                    }
                    break;
            }
            
            // 중간성이 파괴되었거나 없으면 최종성으로
            if (fallbackWaypoints.Count == 0 && region2FinalCastle != null)
            {
                fallbackWaypoints.Add(region2FinalCastle.transform);
                Debug.Log("[RouteManager] 지역2 중간성이 파괴됨 → 최종성(체력 1000) 목표");
            }
        }
        
        return fallbackWaypoints.ToArray();
    }

    /// <summary>
    /// 지역1용 웨이포인트 반환 (GameObject 배열)
    /// </summary>
    public GameObject[] GetWaypointsForRegion1(RouteType route)
    {
        WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
        if (spawner == null)
        {
            Debug.LogError("[RouteManager] WaveSpawner를 찾을 수 없습니다!");
            return new GameObject[0];
        }

        Transform[] waypoints = GetWaypointsForRoute(spawner, route);
        if (waypoints == null || waypoints.Length == 0)
        {
            return new GameObject[0];
        }

        GameObject[] gameObjects = new GameObject[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            gameObjects[i] = waypoints[i] != null ? waypoints[i].gameObject : null;
        }

        return gameObjects;
    }

    /// <summary>
    /// 지역2용 웨이포인트 반환 (GameObject 배열)
    /// </summary>
    public GameObject[] GetWaypointsForRegion2(RouteType route)
    {
        WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
        if (spawner2 == null)
        {
            Debug.LogError("[RouteManager] WaveSpawnerRegion2를 찾을 수 없습니다!");
            return new GameObject[0];
        }

        Transform[] waypoints = GetWaypointsForRoute(spawner2, route);
        if (waypoints == null || waypoints.Length == 0)
        {
            return new GameObject[0];
        }

        GameObject[] gameObjects = new GameObject[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            gameObjects[i] = waypoints[i] != null ? waypoints[i].gameObject : null;
        }

        return gameObjects;
    }

    public Transform[] GetWaypointsForRoute(WaveSpawnerRegion2 spawner, RouteType route)
    {
        Transform[] waypoints = null;
        
        switch (route)
        {
            case RouteType.Left:
                waypoints = spawner.walkableLeft2;
                break;
            case RouteType.Center:
                waypoints = spawner.walkableCenter2;
                break;
            case RouteType.Right:
                waypoints = spawner.walkableRight2;
                break;
            default:
                waypoints = spawner.walkableCenter2;
                break;
        }
        
        if (waypoints != null && waypoints.Length > 0)
        {
            List<Transform> validWaypoints = new List<Transform>();
            
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    validWaypoints.Add(waypoints[i]);
                }
                else
                {
                    Debug.LogWarning($"[RouteManager] 지역2 {route} 루트의 웨이포인트[{i}]가 null입니다!");
                }
            }
            
            if (validWaypoints.Count > 0)
            {
                for (int i = 0; i < validWaypoints.Count - 1; i++)
                {
                    float distance = Vector2.Distance(validWaypoints[i].position, validWaypoints[i + 1].position);
                    if (distance > 15f)
                    {
                        Debug.LogWarning($"[RouteManager] 지역2 {route} 루트 웨이포인트[{i}]→[{i+1}] 거리가 너무 멉니다: {distance}");
                    }
                }
                
                Debug.Log($"[RouteManager] 지역2 {route} 루트 웨이포인트 검증 완료: {validWaypoints.Count}개 (원본: {waypoints.Length}개)");
                return validWaypoints.ToArray();
            }
            else
            {
                Debug.LogError($"[RouteManager] 지역2 {route} 루트에 유효한 웨이포인트가 없습니다!");
                return GetFallbackWaypoints(route, 2); // 지역2용 fallback
            }
        }
        
        Debug.LogWarning($"[RouteManager] 지역2 {route} 루트의 웨이포인트 배열이 null이거나 비어있습니다!");
        return GetFallbackWaypoints(route, 2); // 지역2용 fallback
    }

    public Vector3 GetSpawnPositionForRoute(WaveSpawner spawner, RouteType route)
    {
        Transform spawnPoint = null;
        
        switch (route)
        {
            case RouteType.Left:
                spawnPoint = spawner.leftSpawnPoint;
                break;
            case RouteType.Center:
                spawnPoint = spawner.centerSpawnPoint;
                break;
            case RouteType.Right:
                spawnPoint = spawner.rightSpawnPoint;
                break;
        }
        
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        
        Transform[] waypoints = GetWaypointsForRoute(spawner, route);
        if (waypoints != null && waypoints.Length > 0)
        {
            return waypoints[0].position;
        }
        
        return Vector3.zero;
    }

    public Vector3 GetSpawnPositionForRoute(WaveSpawnerRegion2 spawner, RouteType route)
    {
        Transform spawnPoint = null;
        
        switch (route)
        {
            case RouteType.Left:
                spawnPoint = spawner.leftSpawnPoint2;
                break;
            case RouteType.Center:
                spawnPoint = spawner.centerSpawnPoint2;
                break;
            case RouteType.Right:
                spawnPoint = spawner.rightSpawnPoint2;
                break;
        }
        
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        
        Transform[] waypoints = GetWaypointsForRoute(spawner, route);
        if (waypoints != null && waypoints.Length > 0)
        {
            return waypoints[0].position;
        }
        
        return Vector3.zero;
    }

    public void OnRouteSelected(Character character, Tile tile, RouteType selectedRoute, WaveSpawner spawner)
    {
        var coreData = CoreDataManager.Instance;
        
        Transform[] waypoints = GetWaypointsForRoute(spawner, selectedRoute);
        
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"[RouteManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
            Destroy(character.gameObject);
            return;
        }
        
        Vector3 spawnPos = GetSpawnPositionForRoute(spawner, selectedRoute);
        
        RectTransform allyRect = character.GetComponent<RectTransform>();
        if (allyRect != null)
        {
            Vector2 localPos = coreData.ourMonsterPanel.InverseTransformPoint(spawnPos);
            allyRect.anchoredPosition = localPos;
            allyRect.localRotation = Quaternion.identity;
        }
        else
        {
            character.transform.position = spawnPos;
            character.transform.localRotation = Quaternion.identity;
        }
        
        character.currentWaypointIndex = 0;
        character.pathWaypoints = waypoints;
        character.maxWaypointIndex = waypoints.Length - 1;
        character.selectedRoute = selectedRoute;
        
        Debug.Log($"[RouteManager] 캐릭터 {character.characterName}에게 {selectedRoute} 루트 설정 완료. 웨이포인트 개수: {waypoints.Length}");
    }

    public void OnRouteSelectedRegion2(Character character, Tile tile, RouteType selectedRoute, WaveSpawnerRegion2 spawner2)
    {
        var coreData = CoreDataManager.Instance;
        
        Transform[] waypoints = GetWaypointsForRoute(spawner2, selectedRoute);
        
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"[RouteManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
            Destroy(character.gameObject);
            return;
        }
        
        Vector3 spawnPos = GetSpawnPositionForRoute(spawner2, selectedRoute);
        
        RectTransform enemyRect = character.GetComponent<RectTransform>();
        if (enemyRect != null)
        {
            Vector2 localPos = coreData.opponentOurMonsterPanel.InverseTransformPoint(spawnPos);
            enemyRect.anchoredPosition = localPos;
            enemyRect.localRotation = Quaternion.identity;
        }
        else
        {
            character.transform.position = spawnPos;
            character.transform.localRotation = Quaternion.identity;
        }
        
        character.currentWaypointIndex = 0;
        character.pathWaypoints = waypoints;
        character.maxWaypointIndex = waypoints.Length - 1;
        character.selectedRoute = selectedRoute;
        
        Debug.Log($"[RouteManager] 캐릭터 {character.characterName}에게 {selectedRoute} 루트 설정 완료. 웨이포인트 개수: {waypoints.Length}");
    }
    
    /// <summary>
    /// 지역별 중간성 반환
    /// </summary>
    public GameObject GetMiddleCastle(int areaIndex, RouteType route)
    {
        if (areaIndex == 1)
        {
            switch (route)
            {
                case RouteType.Left:
                    return region1LeftMiddleCastle;
                case RouteType.Center:
                    return region1CenterMiddleCastle;
                case RouteType.Right:
                    return region1RightMiddleCastle;
            }
        }
        else if (areaIndex == 2)
        {
            switch (route)
            {
                case RouteType.Left:
                    return region2LeftMiddleCastle;
                case RouteType.Center:
                    return region2CenterMiddleCastle;
                case RouteType.Right:
                    return region2RightMiddleCastle;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 지역별 최종성 반환
    /// </summary>
    public GameObject GetFinalCastle(int areaIndex)
    {
        if (areaIndex == 1)
            return region1FinalCastle;
        else if (areaIndex == 2)
            return region2FinalCastle;
        return null;
    }
}