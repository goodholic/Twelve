using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 써드파티 에셋(Feel, Layer Lab 등)의 Missing Script를 전문적으로 정리하는 도구입니다.
/// 핵심 게임 스크립트와 써드파티 스크립트를 구분하여 안전하게 처리합니다.
/// TwelveToolsManager를 통해서만 접근 가능합니다.
/// </summary>
public class ThirdPartyMissingScriptCleaner : EditorWindow
{
    private Vector2 scrollPosition;
    private List<MissingScriptInfo> thirdPartyMissing = new List<MissingScriptInfo>();
    private List<MissingScriptInfo> coreMissing = new List<MissingScriptInfo>();
    private bool showThirdParty = true;
    private bool showCore = true;
    
    [System.Serializable]
    public class MissingScriptInfo
    {
        public GameObject gameObject;
        public string gameObjectPath;
        public string prefabPath;
        public bool isPrefab;
        public bool isThirdParty;
        public string assetSource;
    }
    
    // 써드파티 에셋 경로들
    private static readonly string[] ThirdPartyPaths = {
        "Feel/",
        "Layer Lab/",
        "ToonFX/",
        "D.A. Assets/",
        "GifToUnity/",
        "TextMesh Pro/",
        "StreamingAssets/"
    };
    
    // TwelveToolsManager를 통해서만 접근 - 직접 메뉴 항목 제거
    // [MenuItem("Twelve/🧹 Third Party Missing Script Cleaner")]
    public static void ShowWindow()
    {
        var window = GetWindow<ThirdPartyMissingScriptCleaner>("Third Party Missing Script Cleaner");
        window.titleContent = new GUIContent("🧹 Third Party Cleaner");
        window.minSize = new Vector2(600, 500);
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("써드파티 Missing Script 정리 도구", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("스캔 시작", GUILayout.Height(30)))
        {
            ScanAndCategorize();
        }
        
        if (GUILayout.Button("써드파티 Missing Scripts 제거", GUILayout.Height(30)))
        {
            RemoveThirdPartyMissingScripts();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // 통계 표시
        if (thirdPartyMissing.Count > 0 || coreMissing.Count > 0)
        {
            EditorGUILayout.HelpBox(
                $"전체 Missing Scripts: {thirdPartyMissing.Count + coreMissing.Count}개\n" +
                $"• 써드파티 에셋: {thirdPartyMissing.Count}개\n" +
                $"• 핵심 게임 스크립트: {coreMissing.Count}개", 
                MessageType.Info);
        }
        
        GUILayout.Space(10);
        
        // 표시 옵션
        GUILayout.BeginHorizontal();
        showThirdParty = EditorGUILayout.Toggle("써드파티 표시", showThirdParty);
        showCore = EditorGUILayout.Toggle("핵심 스크립트 표시", showCore);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
        // 써드파티 Missing Scripts 표시
        if (showThirdParty && thirdPartyMissing.Count > 0)
        {
            GUILayout.Label($"써드파티 Missing Scripts ({thirdPartyMissing.Count}개)", EditorStyles.boldLabel);
            
            foreach (var missing in thirdPartyMissing)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"🔧 {missing.gameObject.name}", EditorStyles.miniLabel);
                GUILayout.Label($"[{missing.assetSource}]", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(missing.gameObject);
                }
                
                if (GUILayout.Button("제거", GUILayout.Width(50)))
                {
                    RemoveIndividualMissingScript(missing);
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Label($"경로: {missing.gameObjectPath}", EditorStyles.miniLabel);
                if (missing.isPrefab)
                {
                    GUILayout.Label($"프리팹: {missing.prefabPath}", EditorStyles.miniLabel);
                }
                
                GUILayout.EndVertical();
                GUILayout.Space(2);
            }
        }
        
        // 핵심 Missing Scripts 표시  
        if (showCore && coreMissing.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"핵심 게임 Missing Scripts ({coreMissing.Count}개)", EditorStyles.boldLabel);
            
            foreach (var missing in coreMissing)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"⚠️ {missing.gameObject.name}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(missing.gameObject);
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Label($"경로: {missing.gameObjectPath}", EditorStyles.miniLabel);
                if (missing.isPrefab)
                {
                    GUILayout.Label($"프리팹: {missing.prefabPath}", EditorStyles.miniLabel);
                }
                
                GUILayout.EndVertical();
                GUILayout.Space(2);
            }
        }
        
        GUILayout.EndScrollView();
    }
    
    void ScanAndCategorize()
    {
        Debug.Log("=== 써드파티 Missing Script 스캔 시작 ===");
        thirdPartyMissing.Clear();
        coreMissing.Clear();
        
        // Scene 객체들 스캔
        ScanGameObjectsInScene();
        
        // Prefab 스캔
        ScanPrefabs();
        
        Debug.Log($"=== 스캔 완료: 써드파티 {thirdPartyMissing.Count}개, 핵심 {coreMissing.Count}개 ===");
    }
    
    void ScanGameObjectsInScene()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        foreach (GameObject go in allObjects)
        {
            if (go.scene.IsValid())
            {
                ScanGameObjectForMissingScripts(go, false);
            }
        }
    }
    
    void ScanPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        
        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            
            if (prefab != null)
            {
                ScanGameObjectForMissingScripts(prefab, true, assetPath);
            }
        }
    }
    
    void ScanGameObjectForMissingScripts(GameObject go, bool isPrefab, string prefabPath = "")
    {
        Component[] components = go.GetComponents<Component>();
        
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                var missingInfo = new MissingScriptInfo
                {
                    gameObject = go,
                    gameObjectPath = GetGameObjectPath(go),
                    prefabPath = prefabPath,
                    isPrefab = isPrefab
                };
                
                // 써드파티 에셋인지 확인
                bool isThirdParty = false;
                string assetSource = "Unknown";
                
                if (isPrefab)
                {
                    foreach (string thirdPartyPath in ThirdPartyPaths)
                    {
                        if (prefabPath.Contains(thirdPartyPath))
                        {
                            isThirdParty = true;
                            assetSource = thirdPartyPath.TrimEnd('/');
                            break;
                        }
                    }
                }
                
                missingInfo.isThirdParty = isThirdParty;
                missingInfo.assetSource = assetSource;
                
                if (isThirdParty)
                {
                    thirdPartyMissing.Add(missingInfo);
                }
                else
                {
                    coreMissing.Add(missingInfo);
                }
            }
        }
        
        // 자식 객체들도 재귀적으로 스캔
        foreach (Transform child in go.transform)
        {
            ScanGameObjectForMissingScripts(child.gameObject, isPrefab, prefabPath);
        }
    }
    
    string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    void RemoveThirdPartyMissingScripts()
    {
        if (thirdPartyMissing.Count == 0)
        {
            EditorUtility.DisplayDialog("알림", "제거할 써드파티 Missing Script가 없습니다.", "확인");
            return;
        }
        
        bool confirm = EditorUtility.DisplayDialog(
            "써드파티 Missing Script 제거", 
            $"총 {thirdPartyMissing.Count}개의 써드파티 Missing Script를 제거하시겠습니까?\n\n" +
            "이 작업은 되돌릴 수 없습니다.", 
            "제거", "취소");
        
        if (!confirm) return;
        
        int removedCount = 0;
        
        foreach (var missing in thirdPartyMissing)
        {
            if (RemoveIndividualMissingScript(missing))
            {
                removedCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"=== 써드파티 Missing Script 제거 완료: {removedCount}개 ===");
        EditorUtility.DisplayDialog("완료", $"{removedCount}개의 써드파티 Missing Script를 제거했습니다.", "확인");
        
        // 다시 스캔
        ScanAndCategorize();
    }
    
    bool RemoveIndividualMissingScript(MissingScriptInfo missing)
    {
        try
        {
            if (missing.isPrefab)
            {
                string prefabPath = AssetDatabase.GetAssetPath(missing.gameObject);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                
                GameObject targetInPrefab = FindGameObjectInPrefab(prefabRoot, missing.gameObject.name);
                if (targetInPrefab != null)
                {
                    Debug.Log($"🧹 써드파티 Missing Script 제거: {prefabPath} ({targetInPrefab.name})");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(targetInPrefab);
                    
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    Debug.Log($"✅ 써드파티 프리팹 정리 완료: {prefabPath}");
                }
                
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            else
            {
                Debug.Log($"🧹 Scene 객체에서 써드파티 Missing Script 제거: {missing.gameObjectPath}");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(missing.gameObject);
                Debug.Log($"✅ Scene 객체 정리 완료: {missing.gameObjectPath}");
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 써드파티 Missing Script 제거 실패: {missing.gameObjectPath}: {e.Message}");
            return false;
        }
    }
    
    GameObject FindGameObjectInPrefab(GameObject prefabRoot, string targetName)
    {
        if (prefabRoot.name == targetName)
            return prefabRoot;
        
        foreach (Transform child in prefabRoot.transform)
        {
            GameObject found = FindGameObjectInPrefabRecursive(child.gameObject, targetName);
            if (found != null)
                return found;
        }
        
        return null;
    }
    
    GameObject FindGameObjectInPrefabRecursive(GameObject obj, string targetName)
    {
        if (obj.name == targetName)
            return obj;
        
        foreach (Transform child in obj.transform)
        {
            GameObject found = FindGameObjectInPrefabRecursive(child.gameObject, targetName);
            if (found != null)
                return found;
        }
        
        return null;
    }
} 