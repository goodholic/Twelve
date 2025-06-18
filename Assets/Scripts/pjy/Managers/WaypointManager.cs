using UnityEngine;

/// <summary>
/// 웨이포인트 관리자
/// 게임 기획서: 3라인 시스템 (왼쪽/중앙/오른쪽 웨이포인트)
/// </summary>
public class WaypointManager : MonoBehaviour
{
    [Header("Region 1 웨이포인트")]
    public GameObject[] region1RouteLeft_Waypoints;
    public GameObject[] region1RouteCenter_Waypoints;
    public GameObject[] region1RouteRight_Waypoints;
    
    [Header("Region 2 웨이포인트")]
    public GameObject[] region2RouteLeft_Waypoints;
    public GameObject[] region2RouteCenter_Waypoints;
    public GameObject[] region2RouteRight_Waypoints;
    
    private static WaypointManager instance;
    public static WaypointManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<WaypointManager>();
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
    /// 특정 지역과 라인의 웨이포인트 가져오기
    /// </summary>
    public Transform[] GetWaypoints(int areaIndex, int routeIndex)
    {
        GameObject[] waypoints = null;
        
        if (areaIndex == 1)
        {
            switch (routeIndex)
            {
                case 0: waypoints = region1RouteLeft_Waypoints; break;
                case 1: waypoints = region1RouteCenter_Waypoints; break;
                case 2: waypoints = region1RouteRight_Waypoints; break;
            }
        }
        else if (areaIndex == 2)
        {
            switch (routeIndex)
            {
                case 0: waypoints = region2RouteLeft_Waypoints; break;
                case 1: waypoints = region2RouteCenter_Waypoints; break;
                case 2: waypoints = region2RouteRight_Waypoints; break;
            }
        }
        
        if (waypoints == null) return null;
        
        // GameObject 배열을 Transform 배열로 변환
        Transform[] transforms = new Transform[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
                transforms[i] = waypoints[i].transform;
        }
        
        return transforms;
    }
    
    /// <summary>
    /// 특정 지역과 RouteType의 웨이포인트 가져오기
    /// </summary>
    public Transform[] GetWaypointsForRoute(int areaIndex, RouteType route)
    {
        GameObject[] waypoints = null;
        
        if (areaIndex == 1)
        {
            switch (route)
            {
                case RouteType.Left: 
                    waypoints = region1RouteLeft_Waypoints; 
                    break;
                case RouteType.Center: 
                    waypoints = region1RouteCenter_Waypoints; 
                    break;
                case RouteType.Right: 
                    waypoints = region1RouteRight_Waypoints; 
                    break;
            }
        }
        else if (areaIndex == 2)
        {
            switch (route)
            {
                case RouteType.Left: 
                    waypoints = region2RouteLeft_Waypoints; 
                    break;
                case RouteType.Center: 
                    waypoints = region2RouteCenter_Waypoints; 
                    break;
                case RouteType.Right: 
                    waypoints = region2RouteRight_Waypoints; 
                    break;
            }
        }
        
        if (waypoints == null) return null;
        
        // GameObject 배열을 Transform 배열로 변환
        Transform[] transforms = new Transform[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
                transforms[i] = waypoints[i].transform;
        }
        
        return transforms;
    }
    
    /// <summary>
    /// 타일 타입에 따른 라우트 인덱스 가져오기
    /// </summary>
    public int GetRouteIndexFromTile(Tile tile)
    {
        if (tile == null) return -1;
        
        if (tile.IsWalkableLeft() || tile.IsWalkable2Left())
            return 0; // 왼쪽 라인
        else if (tile.IsWalkableCenter() || tile.IsWalkable2Center())
            return 1; // 중앙 라인
        else if (tile.IsWalkableRight() || tile.IsWalkable2Right())
            return 2; // 오른쪽 라인
            
        return -1;
    }
    
    /// <summary>
    /// 웨이포인트 유효성 검사
    /// </summary>
    public bool ValidateWaypoints()
    {
        bool isValid = true;
        
        // Region 1 검사
        if (region1RouteLeft_Waypoints == null || region1RouteLeft_Waypoints.Length == 0)
        {
            Debug.LogWarning("[WaypointManager] Region1 왼쪽 웨이포인트가 설정되지 않았습니다!");
            isValid = false;
        }
        if (region1RouteCenter_Waypoints == null || region1RouteCenter_Waypoints.Length == 0)
        {
            Debug.LogWarning("[WaypointManager] Region1 중앙 웨이포인트가 설정되지 않았습니다!");
            isValid = false;
        }
        if (region1RouteRight_Waypoints == null || region1RouteRight_Waypoints.Length == 0)
        {
            Debug.LogWarning("[WaypointManager] Region1 오른쪽 웨이포인트가 설정되지 않았습니다!");
            isValid = false;
        }
        
        // Region 2 검사
        if (region2RouteLeft_Waypoints == null || region2RouteLeft_Waypoints.Length == 0)
        {
            Debug.LogWarning("[WaypointManager] Region2 왼쪽 웨이포인트가 설정되지 않았습니다!");
            isValid = false;
        }
        if (region2RouteCenter_Waypoints == null || region2RouteCenter_Waypoints.Length == 0)
        {
            Debug.LogWarning("[WaypointManager] Region2 중앙 웨이포인트가 설정되지 않았습니다!");
            isValid = false;
        }
        if (region2RouteRight_Waypoints == null || region2RouteRight_Waypoints.Length == 0)
        {
            Debug.LogWarning("[WaypointManager] Region2 오른쪽 웨이포인트가 설정되지 않았습니다!");
            isValid = false;
        }
        
        return isValid;
    }
}