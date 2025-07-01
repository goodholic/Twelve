#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class MissingPrefabFixer : EditorWindow
{
    [MenuItem("Tools/Missing Prefab Fixer")]
    public static void ShowWindow()
    {
        GetWindow<MissingPrefabFixer>("Missing Prefab Fixer");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Missing Prefab Fixer", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Find Missing Prefabs in Current Scene"))
        {
            FindMissingPrefabsInCurrentScene();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Find Missing Prefabs in All Scenes"))
        {
            FindMissingPrefabsInAllScenes();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Clean Up Missing Prefab References"))
        {
            CleanUpMissingPrefabReferences();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("GUID 검색", EditorStyles.boldLabel);
        GUILayout.Label("찾는 GUID: 83009eb1999cbcc489d24fc3e770b5e7");
        
        if (GUILayout.Button("Search for GUID in Project"))
        {
            SearchForGUID("83009eb1999cbcc489d24fc3e770b5e7");
        }
    }
    
    private void FindMissingPrefabsInCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"[MissingPrefabFixer] Checking scene: {currentScene.name}");
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int missingCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (PrefabUtility.IsPrefabAssetMissing(obj))
            {
                Debug.LogWarning($"[MissingPrefabFixer] Missing prefab found: {obj.name}", obj);
                missingCount++;
            }
        }
        
        Debug.Log($"[MissingPrefabFixer] Found {missingCount} missing prefabs in current scene.");
    }
    
    private void FindMissingPrefabsInAllScenes()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        int totalMissing = 0;
        
        foreach (string sceneGuid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            int missingCount = 0;
            
            foreach (GameObject obj in allObjects)
            {
                if (PrefabUtility.IsPrefabAssetMissing(obj))
                {
                    Debug.LogWarning($"[MissingPrefabFixer] Missing prefab in {scene.name}: {obj.name}", obj);
                    missingCount++;
                }
            }
            
            if (missingCount > 0)
            {
                Debug.Log($"[MissingPrefabFixer] Scene {scene.name}: {missingCount} missing prefabs");
                totalMissing += missingCount;
            }
        }
        
        Debug.Log($"[MissingPrefabFixer] Total missing prefabs found: {totalMissing}");
    }
    
    private void CleanUpMissingPrefabReferences()
    {
        if (EditorUtility.DisplayDialog("Clean Up Missing Prefabs", 
            "이 작업은 현재 씬의 모든 누락된 프리팹 참조를 제거합니다. 계속하시겠습니까?", 
            "예", "아니오"))
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            int cleanedCount = 0;
            
            foreach (GameObject obj in allObjects)
            {
                if (PrefabUtility.IsPrefabAssetMissing(obj))
                {
                    Debug.Log($"[MissingPrefabFixer] Cleaning up: {obj.name}");
                    DestroyImmediate(obj);
                    cleanedCount++;
                }
            }
            
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[MissingPrefabFixer] Cleaned up {cleanedCount} missing prefab references.");
        }
    }
    
    private void SearchForGUID(string guid)
    {
        string[] allAssets = AssetDatabase.FindAssets("");
        bool found = false;
        
        foreach (string assetGuid in allAssets)
        {
            if (assetGuid == guid)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"[MissingPrefabFixer] GUID {guid} found at: {path}");
                found = true;
                break;
            }
        }
        
        if (!found)
        {
            Debug.LogWarning($"[MissingPrefabFixer] GUID {guid} not found in project. This asset may have been deleted.");
            
            // .meta 파일에서 검색
            string[] metaFiles = System.IO.Directory.GetFiles(Application.dataPath, "*.meta", System.IO.SearchOption.AllDirectories);
            foreach (string metaFile in metaFiles)
            {
                string content = System.IO.File.ReadAllText(metaFile);
                if (content.Contains(guid))
                {
                    Debug.Log($"[MissingPrefabFixer] GUID reference found in meta file: {metaFile}");
                }
            }
        }
    }
}
#endif 