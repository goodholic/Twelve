using UnityEngine;
using UnityEditor;
using pjy.Managers;

[CustomEditor(typeof(RouteWaypointManager))]
public class RouteWaypointEditor : UnityEditor.Editor
{
    private RouteWaypointManager manager;
    private bool showPlayer1Paths = true;
    private bool showPlayer2Paths = true;
    
    private void OnEnable()
    {
        manager = (RouteWaypointManager)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Waypoint Path Setup", EditorStyles.boldLabel);
        
        showPlayer1Paths = EditorGUILayout.Foldout(showPlayer1Paths, "Player 1 Paths");
        if (showPlayer1Paths)
        {
            EditorGUI.indentLevel++;
            DrawPathButtons("Player 1 Left", RouteType.Left, 1);
            DrawPathButtons("Player 1 Center", RouteType.Center, 1);
            DrawPathButtons("Player 1 Right", RouteType.Right, 1);
            EditorGUI.indentLevel--;
        }
        
        showPlayer2Paths = EditorGUILayout.Foldout(showPlayer2Paths, "Player 2 Paths");
        if (showPlayer2Paths)
        {
            EditorGUI.indentLevel++;
            DrawPathButtons("Player 2 Left", RouteType.Left, 2);
            DrawPathButtons("Player 2 Center", RouteType.Center, 2);
            DrawPathButtons("Player 2 Right", RouteType.Right, 2);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Auto Generate All Waypoints", GUILayout.Height(30)))
        {
            manager.GenerateWaypointsAutomatically();
            EditorUtility.SetDirty(manager);
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("Create Waypoint at Scene View Center"))
        {
            CreateWaypointAtSceneCenter();
        }
        
        if (GUILayout.Button("Align Selected Waypoints"))
        {
            AlignSelectedWaypoints();
        }
    }
    
    private void DrawPathButtons(string label, RouteType route, int playerIndex)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        
        if (GUILayout.Button("Add Waypoint", GUILayout.Width(100)))
        {
            AddWaypointToPath(playerIndex, route);
        }
        
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            ClearPath(playerIndex, route);
        }
        
        if (GUILayout.Button("Auto", GUILayout.Width(60)))
        {
            AutoGeneratePath(playerIndex, route);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void AddWaypointToPath(int playerIndex, RouteType route)
    {
        GameObject waypointObj = new GameObject($"Waypoint_P{playerIndex}_{route}_{System.DateTime.Now.Ticks}");
        waypointObj.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
        waypointObj.transform.parent = manager.transform;
        
        // Add to appropriate path
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty pathProp = GetPathProperty(so, playerIndex, route);
        
        if (pathProp != null)
        {
            SerializedProperty waypointsProp = pathProp.FindPropertyRelative("waypoints");
            waypointsProp.arraySize++;
            waypointsProp.GetArrayElementAtIndex(waypointsProp.arraySize - 1).objectReferenceValue = waypointObj.transform;
            so.ApplyModifiedProperties();
        }
        
        Selection.activeGameObject = waypointObj;
        EditorUtility.SetDirty(manager);
    }
    
    private void ClearPath(int playerIndex, RouteType route)
    {
        if (EditorUtility.DisplayDialog("Clear Path", 
            $"Are you sure you want to clear all waypoints for Player {playerIndex} {route} path?", 
            "Yes", "No"))
        {
            SerializedObject so = new SerializedObject(manager);
            SerializedProperty pathProp = GetPathProperty(so, playerIndex, route);
            
            if (pathProp != null)
            {
                SerializedProperty waypointsProp = pathProp.FindPropertyRelative("waypoints");
                
                // Delete waypoint GameObjects
                for (int i = 0; i < waypointsProp.arraySize; i++)
                {
                    Transform waypoint = waypointsProp.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    if (waypoint != null)
                    {
                        DestroyImmediate(waypoint.gameObject);
                    }
                }
                
                waypointsProp.ClearArray();
                so.ApplyModifiedProperties();
            }
            
            EditorUtility.SetDirty(manager);
        }
    }
    
    private void AutoGeneratePath(int playerIndex, RouteType route)
    {
        // Find walkable tiles for this route
        Tile[] allTiles = FindObjectsOfType<Tile>();
        System.Collections.Generic.List<Tile> routeTiles = new System.Collections.Generic.List<Tile>();
        
        foreach (Tile tile in allTiles)
        {
            if (tile.isRegion2 == (playerIndex == 2) && tile.IsWalkable())
            {
                RouteType tileRoute = manager.DetermineRouteFromTilePosition(tile);
                if (tileRoute == route)
                {
                    routeTiles.Add(tile);
                }
            }
        }
        
        if (routeTiles.Count == 0)
        {
            EditorUtility.DisplayDialog("No Tiles Found", 
                $"No walkable tiles found for Player {playerIndex} {route} path", "OK");
            return;
        }
        
        // Sort tiles by X position
        routeTiles.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        
        // Clear existing path
        ClearPath(playerIndex, route);
        
        // Create waypoints
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty pathProp = GetPathProperty(so, playerIndex, route);
        
        if (pathProp != null)
        {
            SerializedProperty waypointsProp = pathProp.FindPropertyRelative("waypoints");
            
            foreach (Tile tile in routeTiles)
            {
                GameObject waypointObj = new GameObject($"Auto_Waypoint_P{playerIndex}_{route}");
                waypointObj.transform.position = tile.transform.position;
                waypointObj.transform.parent = manager.transform;
                
                waypointsProp.arraySize++;
                waypointsProp.GetArrayElementAtIndex(waypointsProp.arraySize - 1).objectReferenceValue = waypointObj.transform;
            }
            
            so.ApplyModifiedProperties();
        }
        
        EditorUtility.SetDirty(manager);
    }
    
    private SerializedProperty GetPathProperty(SerializedObject so, int playerIndex, RouteType route)
    {
        string propName = $"player{playerIndex}{route}Path";
        return so.FindProperty(propName);
    }
    
    private void CreateWaypointAtSceneCenter()
    {
        GameObject waypointObj = new GameObject($"Waypoint_{System.DateTime.Now.Ticks}");
        
        if (SceneView.lastActiveSceneView != null)
        {
            waypointObj.transform.position = SceneView.lastActiveSceneView.pivot;
        }
        
        waypointObj.transform.parent = manager.transform;
        Selection.activeGameObject = waypointObj;
        
        EditorUtility.SetDirty(manager);
    }
    
    private void AlignSelectedWaypoints()
    {
        Transform[] selected = Selection.transforms;
        if (selected.Length < 2)
        {
            EditorUtility.DisplayDialog("Not Enough Selection", 
                "Please select at least 2 waypoints to align", "OK");
            return;
        }
        
        // Find average Y and Z
        float avgY = 0f;
        float avgZ = 0f;
        
        foreach (Transform t in selected)
        {
            avgY += t.position.y;
            avgZ += t.position.z;
        }
        
        avgY /= selected.Length;
        avgZ /= selected.Length;
        
        // Apply to all selected
        Undo.RecordObjects(selected, "Align Waypoints");
        
        foreach (Transform t in selected)
        {
            Vector3 pos = t.position;
            pos.y = avgY;
            pos.z = avgZ;
            t.position = pos;
        }
        
        EditorUtility.SetDirty(manager);
    }
    
    private void OnSceneGUI()
    {
        // Draw waypoint handles in scene view
        DrawPathHandles();
    }
    
    private void DrawPathHandles()
    {
        SerializedObject so = new SerializedObject(manager);
        
        DrawPathWithHandles(so, "player1LeftPath", Color.red);
        DrawPathWithHandles(so, "player1CenterPath", Color.green);
        DrawPathWithHandles(so, "player1RightPath", Color.blue);
        DrawPathWithHandles(so, "player2LeftPath", Color.magenta);
        DrawPathWithHandles(so, "player2CenterPath", Color.yellow);
        DrawPathWithHandles(so, "player2RightPath", Color.cyan);
    }
    
    private void DrawPathWithHandles(SerializedObject so, string pathName, Color color)
    {
        SerializedProperty pathProp = so.FindProperty(pathName);
        if (pathProp == null) return;
        
        SerializedProperty waypointsProp = pathProp.FindPropertyRelative("waypoints");
        if (waypointsProp == null) return;
        
        Handles.color = color;
        
        for (int i = 0; i < waypointsProp.arraySize; i++)
        {
            Transform waypoint = waypointsProp.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            if (waypoint != null)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(waypoint.position, Quaternion.identity);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(waypoint, "Move Waypoint");
                    waypoint.position = newPos;
                }
                
                // Draw waypoint number
                Handles.Label(waypoint.position + Vector3.up * 0.5f, $"{i}", EditorStyles.whiteBoldLabel);
            }
        }
    }
}