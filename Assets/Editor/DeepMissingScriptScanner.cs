using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.UI;
using GuildMaster.Data;
using TwelveGame.Battle;

/// <summary>
/// 전체 프로젝트에서 Missing Script를 감지하고 자동으로 수정하는 강력한 도구입니다.
/// 씬과 프리팹의 모든 Missing Script를 찾아 올바른 스크립트로 교체합니다.
/// TwelveToolsManager를 통해서만 접근 가능합니다.
/// </summary>
public class DeepMissingScriptScanner : EditorWindow
{
    // TwelveToolsManager를 통해서만 접근 - 직접 메뉴 항목 제거
    // [MenuItem("Twelve/🔍 Deep Missing Script Scanner")]
    public static void ShowWindow()
    {
        var window = GetWindow<DeepMissingScriptScanner>("Deep Missing Script Scanner");
        window.titleContent = new GUIContent("🔍 Deep Scanner");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }
    
    private Vector2 scrollPosition;
    private List<MissingScriptInfo> missingScripts = new List<MissingScriptInfo>();
    
    private class MissingScriptInfo
    {
        public GameObject gameObject;
        public string gameObjectPath;
        public int componentIndex;
        public bool isPrefab;
        public string suggestedScript;
    }
    
    void OnGUI()
    {
        GUILayout.Label("깊이 Missing Script 스캔 및 복구", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 전체 스캔 (Prefab 포함)", GUILayout.Height(40)))
        {
            ScanAllMissingScripts();
        }
        
        if (GUILayout.Button("🚀 자동 복구", GUILayout.Height(40)))
        {
            AutoFixAllMissingScripts();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        if (missingScripts.Count > 0)
        {
            GUILayout.Label($"발견된 Missing Scripts: {missingScripts.Count}개", EditorStyles.boldLabel);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            foreach (var missing in missingScripts)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label($"🎯 {missing.gameObjectPath}", EditorStyles.boldLabel);
                GUILayout.Label($"   컴포넌트 위치: {missing.componentIndex}");
                GUILayout.Label($"   Prefab: {(missing.isPrefab ? "예" : "아니오")}");
                if (!string.IsNullOrEmpty(missing.suggestedScript))
                {
                    GUILayout.Label($"   추천 스크립트: {missing.suggestedScript}", EditorStyles.helpBox);
                }
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("선택"))
                {
                    Selection.activeGameObject = missing.gameObject;
                    EditorGUIUtility.PingObject(missing.gameObject);
                }
                
                if (GUILayout.Button("개별 수정"))
                {
                    FixIndividualMissingScript(missing);
                }
                GUILayout.EndHorizontal();
                
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
            
            GUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("Missing Script가 발견되지 않았습니다.", EditorStyles.helpBox);
        }
    }
    
    void ScanAllMissingScripts()
    {
        Debug.Log("=== 깊이 Missing Script 스캔 시작 ===");
        missingScripts.Clear();
        
        // 1. 현재 Scene의 모든 GameObject 스캔 (비활성 포함)
        ScanGameObjectsInScene();
        
        // 2. Prefab 스캔
        ScanPrefabs();
        
        Debug.Log($"=== 스캔 완료: {missingScripts.Count}개 Missing Script 발견 ===");
        
        // 각 Missing Script에 추천 스크립트 제안
        SuggestScriptsForMissingComponents();
    }
    
    void ScanGameObjectsInScene()
    {
        // Resources.FindObjectsOfTypeAll을 사용해서 비활성 GameObject도 포함
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        foreach (GameObject go in allObjects)
        {
            // Scene 객체만 처리 (Prefab은 따로 처리)
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
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                ScanGameObjectForMissingScripts(prefab, true);
                
                // Prefab의 하위 객체도 스캔
                Transform[] children = prefab.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.gameObject != prefab)
                    {
                        ScanGameObjectForMissingScripts(child.gameObject, true);
                    }
                }
            }
        }
    }
    
    void ScanGameObjectForMissingScripts(GameObject go, bool isPrefab)
    {
        Component[] components = go.GetComponents<Component>();
        
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                string path = isPrefab ? AssetDatabase.GetAssetPath(go) : GetGameObjectPath(go);
                
                var missingInfo = new MissingScriptInfo
                {
                    gameObject = go,
                    gameObjectPath = path,
                    componentIndex = i,
                    isPrefab = isPrefab
                };
                
                missingScripts.Add(missingInfo);
                
                Debug.LogWarning($"Missing Script 발견: {path} (컴포넌트 {i})");
            }
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
    
    void SuggestScriptsForMissingComponents()
    {
        // GameObject 이름을 기반으로 스크립트 추천
        var scriptSuggestions = new Dictionary<string, System.Type>
        {
            { "LobbySceneManager", typeof(LobbySceneManager) },
            { "BookPanelManager", typeof(BookPanelManager) },
            { "ShopUI", typeof(ShopUI) },
            { "GameResolutionManager", typeof(GameResolutionManager) },
            { "IntroManager", typeof(IntroManager) },
            { "ShopManager", typeof(ShopManager) },
            { "UpgradePanelManager", typeof(UpgradePanelManager) },
            { "DrawPanelManager", typeof(DrawPanelManager) },
            { "CharacterInventoryManager", typeof(CharacterInventoryManager) }
        };
        
        foreach (var missing in missingScripts)
        {
            string objectName = missing.gameObject.name;
            
            foreach (var suggestion in scriptSuggestions)
            {
                if (objectName.Contains(suggestion.Key))
                {
                    missing.suggestedScript = suggestion.Key;
                    break;
                }
            }
        }
    }
    
    void AutoFixAllMissingScripts()
    {
        Debug.Log("=== 자동 Missing Script 복구 시작 ===");
        int fixedCount = 0;
        
        var scriptMappings = new Dictionary<string, System.Type>
        {
            { "LobbySceneManager", typeof(LobbySceneManager) },
            { "BookPanelManager", typeof(BookPanelManager) },
            { "ShopUI", typeof(ShopUI) },
            { "GameResolutionManager", typeof(GameResolutionManager) },
            { "IntroManager", typeof(IntroManager) },
            { "ShopManager", typeof(ShopManager) },
            { "UpgradePanelManager", typeof(UpgradePanelManager) },
            { "DrawPanelManager", typeof(DrawPanelManager) },
            { "CharacterInventoryManager", typeof(CharacterInventoryManager) },
            { "DeckPanelManager", typeof(DeckPanelManager) },
            { "GameManager", typeof(TwelveGame.Battle.GameManager) },

        };
        
        foreach (var missing in missingScripts)
        {
            if (FixIndividualMissingScript(missing, scriptMappings))
            {
                fixedCount++;
            }
        }
        
        Debug.Log($"=== 자동 복구 완료: {fixedCount}개 수정됨 ===");
        
        // Asset Database 저장 및 새로고침
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // 스캔 결과 새로고침
        Debug.Log("🔄 복구 결과 확인을 위해 다시 스캔합니다...");
        ScanAllMissingScripts();
    }
    
    void FixIndividualMissingScript(MissingScriptInfo missing)
    {
        var scriptMappings = new Dictionary<string, System.Type>
        {
            { "LobbySceneManager", typeof(LobbySceneManager) },
            { "BookPanelManager", typeof(BookPanelManager) },
            { "ShopUI", typeof(ShopUI) },
            { "GameResolutionManager", typeof(GameResolutionManager) },
            { "IntroManager", typeof(IntroManager) },
            { "ShopManager", typeof(ShopManager) },
            { "UpgradePanelManager", typeof(UpgradePanelManager) },
            { "DrawPanelManager", typeof(DrawPanelManager) },
            { "CharacterInventoryManager", typeof(CharacterInventoryManager) },
            { "DeckPanelManager", typeof(DeckPanelManager) },
            { "GameManager", typeof(TwelveGame.Battle.GameManager) },

        };
        
        FixIndividualMissingScript(missing, scriptMappings);
    }
    
    bool FixIndividualMissingScript(MissingScriptInfo missing, Dictionary<string, System.Type> scriptMappings)
    {
        string objectName = missing.gameObject.name;
        
        foreach (var mapping in scriptMappings)
        {
            if (objectName.Contains(mapping.Key))
            {
                try
                {
                    if (missing.isPrefab)
                    {
                        // Prefab은 AssetDatabase를 통해 수정
                        string prefabPath = AssetDatabase.GetAssetPath(missing.gameObject);
                        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                        
                        // Prefab 내에서 해당 GameObject 찾기
                        GameObject targetInPrefab = FindGameObjectInPrefab(prefabRoot, missing.gameObject.name);
                        if (targetInPrefab != null)
                        {
                            // 중요: Missing Script를 먼저 제거해야 함!
                            Debug.Log($"🧹 Missing Script 제거 중: {prefabPath} ({targetInPrefab.name})");
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(targetInPrefab);
                            
                            // 그 다음에 올바른 컴포넌트 추가
                            Component newComponent = targetInPrefab.AddComponent(mapping.Value);
                            Debug.Log($"➕ 새 컴포넌트 추가: {mapping.Key}");
                            
                            // Prefab 저장
                            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                            Debug.Log($"✅ Prefab 복구 성공: {prefabPath} -> {mapping.Key}");
                        }
                        
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                    else
                    {
                        // Scene 객체는 직접 수정
                        Debug.Log($"🧹 Scene 객체에서 Missing Script 제거 중: {missing.gameObjectPath}");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(missing.gameObject);
                        
                        Component newComponent = missing.gameObject.AddComponent(mapping.Value);
                        Debug.Log($"✅ Scene 객체 복구 성공: {missing.gameObjectPath} -> {mapping.Key}");
                    }
                    
                    return true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ 복구 실패: {missing.gameObjectPath} -> {mapping.Key}: {e.Message}");
                    Debug.LogError($"   상세 오류: {e.StackTrace}");
                }
                
                break;
            }
        }
        
        return false;
    }
    
    GameObject FindGameObjectInPrefab(GameObject prefabRoot, string targetName)
    {
        if (prefabRoot.name == targetName)
            return prefabRoot;
        
        Transform[] children = prefabRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.gameObject.name == targetName)
                return child.gameObject;
        }
        
        return null;
    }
} 