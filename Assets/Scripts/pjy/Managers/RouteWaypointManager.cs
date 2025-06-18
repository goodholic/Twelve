using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace pjy.Managers
{
    [System.Serializable]
    public class WaypointPath
    {
        public string pathName;
        public RouteType routeType;
        public Transform[] waypoints;
        public Color gizmoColor = Color.white;
        
        public bool IsValid()
        {
            return waypoints != null && waypoints.Length > 0 && waypoints.All(w => w != null);
        }
    }
    
    public class RouteWaypointManager : MonoBehaviour
    {
        private static RouteWaypointManager instance;
        public static RouteWaypointManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<RouteWaypointManager>();
                }
                return instance;
            }
        }
        
        [Header("Player 1 Waypoint Paths")]
        [SerializeField] private WaypointPath player1LeftPath;
        [SerializeField] private WaypointPath player1CenterPath;
        [SerializeField] private WaypointPath player1RightPath;
        
        [Header("Player 2 Waypoint Paths")]
        [SerializeField] private WaypointPath player2LeftPath;
        [SerializeField] private WaypointPath player2CenterPath;
        [SerializeField] private WaypointPath player2RightPath;
        
        [Header("Dynamic Waypoint Settings")]
        [SerializeField] private float waypointSpacing = 2f;
        [SerializeField] private bool autoGenerateWaypoints = false;
        [SerializeField] private LayerMask walkableLayer = -1;
        
        [Header("Debug Settings")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private float gizmoSize = 0.5f;
        
        private Dictionary<string, WaypointPath> pathDictionary = new Dictionary<string, WaypointPath>();
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            InitializePaths();
        }
        
        private void InitializePaths()
        {
            // Initialize path colors
            if (player1LeftPath == null) player1LeftPath = new WaypointPath { pathName = "P1_Left", routeType = RouteType.Left, gizmoColor = Color.red };
            if (player1CenterPath == null) player1CenterPath = new WaypointPath { pathName = "P1_Center", routeType = RouteType.Center, gizmoColor = Color.green };
            if (player1RightPath == null) player1RightPath = new WaypointPath { pathName = "P1_Right", routeType = RouteType.Right, gizmoColor = Color.blue };
            
            if (player2LeftPath == null) player2LeftPath = new WaypointPath { pathName = "P2_Left", routeType = RouteType.Left, gizmoColor = Color.magenta };
            if (player2CenterPath == null) player2CenterPath = new WaypointPath { pathName = "P2_Center", routeType = RouteType.Center, gizmoColor = Color.yellow };
            if (player2RightPath == null) player2RightPath = new WaypointPath { pathName = "P2_Right", routeType = RouteType.Right, gizmoColor = Color.cyan };
            
            // Build dictionary
            pathDictionary.Clear();
            pathDictionary["P1_Left"] = player1LeftPath;
            pathDictionary["P1_Center"] = player1CenterPath;
            pathDictionary["P1_Right"] = player1RightPath;
            pathDictionary["P2_Left"] = player2LeftPath;
            pathDictionary["P2_Center"] = player2CenterPath;
            pathDictionary["P2_Right"] = player2RightPath;
        }
        
        public Transform[] GetWaypointsForCharacter(Character character, Tile placementTile)
        {
            int playerIndex = character.areaIndex;
            RouteType route = DetermineRouteFromTilePosition(placementTile);
            
            return GetWaypoints(playerIndex, route);
        }
        
        public Transform[] GetWaypoints(int playerIndex, RouteType route)
        {
            WaypointPath path = GetPath(playerIndex, route);
            
            if (path != null && path.IsValid())
            {
                return path.waypoints;
            }
            
            Debug.LogWarning($"[RouteWaypointManager] No valid waypoints for Player {playerIndex}, Route {route}");
            return null;
        }
        
        private WaypointPath GetPath(int playerIndex, RouteType route)
        {
            string key = $"P{playerIndex}_{route}";
            
            if (pathDictionary.ContainsKey(key))
            {
                return pathDictionary[key];
            }
            
            return null;
        }
        
        public RouteType DetermineRouteFromTilePosition(Tile tile)
        {
            if (tile == null) return RouteType.Center;
            
            float yPos = tile.transform.position.y;
            
            // Y 좌표 기반 경로 결정
            if (yPos > 1.5f)
            {
                return RouteType.Left;
            }
            else if (yPos < -1.5f)
            {
                return RouteType.Right;
            }
            else
            {
                return RouteType.Center;
            }
        }
        
        public void SetupCharacterRoute(Character character, Tile tile)
        {
            if (character == null || tile == null) return;
            
            Transform[] waypoints = GetWaypointsForCharacter(character, tile);
            
            if (waypoints != null && waypoints.Length > 0)
            {
                CharacterMovement movement = character.GetComponent<CharacterMovement>();
                if (movement != null)
                {
                    movement.SetWaypoints(waypoints, 0);
                    movement.StartMoving();
                    
                    Debug.Log($"[RouteWaypointManager] {character.characterName} set to follow {waypoints.Length} waypoints");
                }
            }
        }
        
        public void GenerateWaypointsAutomatically()
        {
            if (!autoGenerateWaypoints) return;
            
            // Find all walkable tiles
            Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
            
            // Group tiles by route type
            Dictionary<string, List<Tile>> tilePaths = new Dictionary<string, List<Tile>>();
            
            foreach (Tile tile in allTiles)
            {
                if (tile.IsWalkable())
                {
                    string key = $"P{(tile.isRegion2 ? 2 : 1)}_{DetermineRouteFromTilePosition(tile)}";
                    
                    if (!tilePaths.ContainsKey(key))
                    {
                        tilePaths[key] = new List<Tile>();
                    }
                    
                    tilePaths[key].Add(tile);
                }
            }
            
            // Generate waypoints for each path
            foreach (var kvp in tilePaths)
            {
                if (pathDictionary.ContainsKey(kvp.Key))
                {
                    List<Transform> waypoints = GenerateWaypointsFromTiles(kvp.Value);
                    pathDictionary[kvp.Key].waypoints = waypoints.ToArray();
                }
            }
        }
        
        private List<Transform> GenerateWaypointsFromTiles(List<Tile> tiles)
        {
            // Sort tiles by X position (left to right movement)
            tiles.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            
            List<Transform> waypoints = new List<Transform>();
            Vector3 lastWaypointPos = Vector3.zero;
            
            foreach (Tile tile in tiles)
            {
                Vector3 tilePos = tile.transform.position;
                
                // Add waypoint if distance from last is sufficient
                if (waypoints.Count == 0 || Vector3.Distance(tilePos, lastWaypointPos) >= waypointSpacing)
                {
                    GameObject waypointObj = new GameObject($"Waypoint_{waypoints.Count}");
                    waypointObj.transform.position = tilePos;
                    waypointObj.transform.parent = transform;
                    
                    waypoints.Add(waypointObj.transform);
                    lastWaypointPos = tilePos;
                }
            }
            
            return waypoints;
        }
        
        public Transform GetNearestWaypoint(Vector3 position, int playerIndex, RouteType route)
        {
            Transform[] waypoints = GetWaypoints(playerIndex, route);
            
            if (waypoints == null || waypoints.Length == 0) return null;
            
            Transform nearest = null;
            float minDistance = float.MaxValue;
            
            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    float distance = Vector3.Distance(position, waypoint.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = waypoint;
                    }
                }
            }
            
            return nearest;
        }
        
        public int GetNearestWaypointIndex(Vector3 position, Transform[] waypoints)
        {
            if (waypoints == null || waypoints.Length == 0) return -1;
            
            int nearestIndex = 0;
            float minDistance = float.MaxValue;
            
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    float distance = Vector3.Distance(position, waypoints[i].position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestIndex = i;
                    }
                }
            }
            
            return nearestIndex;
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            DrawPathGizmos(player1LeftPath);
            DrawPathGizmos(player1CenterPath);
            DrawPathGizmos(player1RightPath);
            DrawPathGizmos(player2LeftPath);
            DrawPathGizmos(player2CenterPath);
            DrawPathGizmos(player2RightPath);
        }
        
        private void DrawPathGizmos(WaypointPath path)
        {
            if (path == null || !path.IsValid()) return;
            
            Gizmos.color = path.gizmoColor;
            
            // Draw waypoints
            foreach (Transform waypoint in path.waypoints)
            {
                if (waypoint != null)
                {
                    Gizmos.DrawWireSphere(waypoint.position, gizmoSize);
                }
            }
            
            // Draw path lines
            for (int i = 0; i < path.waypoints.Length - 1; i++)
            {
                if (path.waypoints[i] != null && path.waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(path.waypoints[i].position, path.waypoints[i + 1].position);
                }
            }
            
            // Draw path name
            if (path.waypoints.Length > 0 && path.waypoints[0] != null)
            {
                UnityEditor.Handles.Label(path.waypoints[0].position + Vector3.up, path.pathName);
            }
        }
        #endif
    }
}