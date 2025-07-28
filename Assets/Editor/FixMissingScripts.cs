using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GuildMaster.UI; // UIManager namespace

namespace TwelveGame.Editor
{
    /// <summary>
    /// Missing Script 문제를 해결하기 위한 유틸리티
    /// </summary>
    public class FixMissingScripts : EditorWindow
    {
        [MenuItem("Twelve/🔧 Fix Missing Scripts", false, 1)]
        public static void ShowWindow()
        {
            FixMissingScripts window = GetWindow<FixMissingScripts>();
            window.titleContent = new GUIContent("Fix Missing Scripts");
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Missing Script 복구 도구", EditorStyles.boldLabel);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("현재 Scene의 Missing Scripts 찾기", GUILayout.Height(30)))
            {
                FindMissingScriptsInCurrentScene();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("모든 Scene의 Missing Scripts 찾기", GUILayout.Height(30)))
            {
                FindMissingScriptsInAllScenes();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Missing Scripts 자동 복구 시도", GUILayout.Height(30)))
            {
                AutoFixMissingScripts();
            }
            
            GUILayout.Space(20);
            
            EditorGUILayout.HelpBox(
                "이 도구는 Missing Script 문제를 찾고 복구를 시도합니다.\n" +
                "먼저 '찾기' 버튼으로 문제를 확인한 후 '자동 복구' 버튼을 사용하세요.",
                MessageType.Info
            );
        }

        static void FindMissingScriptsInCurrentScene()
        {
            int missingCount = 0;
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            Debug.Log("=== 현재 Scene의 Missing Scripts 검사 시작 ===");
            
            foreach (GameObject go in allObjects)
            {
                Component[] components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        missingCount++;
                        Debug.LogWarning($"Missing Script 발견: {go.name} (컴포넌트 {i})", go);
                    }
                }
            }
            
            Debug.Log($"=== 검사 완료: {missingCount}개의 Missing Script 발견 ===");
        }

        static void FindMissingScriptsInAllScenes()
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            int totalMissingCount = 0;
            
            Debug.Log("=== 모든 Scene의 Missing Scripts 검사 시작 ===");
            
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                EditorUtility.DisplayProgressBar("Scene 검사 중", scenePath, (float)totalMissingCount / sceneGuids.Length);
                
                UnityEngine.SceneManagement.Scene originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                
                int missingInThisScene = 0;
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                
                foreach (GameObject go in allObjects)
                {
                    Component[] components = go.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            missingInThisScene++;
                            Debug.LogWarning($"Missing Script 발견: {scenePath} - {go.name} (컴포넌트 {i})");
                        }
                    }
                }
                
                totalMissingCount += missingInThisScene;
                Debug.Log($"Scene '{scenePath}': {missingInThisScene}개 Missing Script");
            }
            
            EditorUtility.ClearProgressBar();
            Debug.Log($"=== 전체 검사 완료: {totalMissingCount}개의 Missing Script 발견 ===");
        }

        static void AutoFixMissingScripts()
        {
            int fixedCount = 0;
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            Debug.Log("=== Missing Scripts 자동 복구 시작 ===");
            
            // 스크립트 타입 매핑
            Dictionary<string, System.Type> scriptMapping = new Dictionary<string, System.Type>
            {
                {"LobbySceneManager", typeof(LobbySceneManager)},
                {"CharacterInventoryManager", typeof(CharacterInventoryManager)},
                {"UpgradePanelManager", typeof(UpgradePanelManager)},
                {"BookPanelManager", typeof(BookPanelManager)},
                {"DrawPanelManager", typeof(DrawPanelManager)},
                {"GameResolutionManager", typeof(GameResolutionManager)},
                {"ShopManager", typeof(ShopManager)},
                {"ShopUI", typeof(ShopUI)},
                {"IntroManager", typeof(IntroManager)},
                {"GachaManager", typeof(GachaManager)},
                {"UIManager", typeof(UIManager)}
            };
            
            foreach (GameObject go in allObjects)
            {
                // SerializedObject를 사용하여 Missing Script 복구 시도
                SerializedObject serializedObject = new SerializedObject(go);
                SerializedProperty components = serializedObject.FindProperty("m_Component");
                
                for (int i = 0; i < components.arraySize; i++)
                {
                    SerializedProperty component = components.GetArrayElementAtIndex(i);
                    SerializedProperty componentRef = component.FindPropertyRelative("component");
                    
                    if (componentRef.objectReferenceValue == null)
                    {
                        Debug.Log($"Missing Script 복구 시도: {go.name}");
                        
                        // 게임오브젝트 이름으로 스크립트 타입 추정
                        foreach (var mapping in scriptMapping)
                        {
                            if (go.name.Contains(mapping.Key))
                            {
                                var newComponent = go.AddComponent(mapping.Value);
                                Debug.Log($"✅ {go.name}에 {mapping.Key} 컴포넌트 추가됨");
                                fixedCount++;
                                break;
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"=== 자동 복구 완료: {fixedCount}개 수정됨 ===");
            
            if (fixedCount > 0)
            {
                EditorUtility.DisplayDialog("복구 완료", 
                    $"{fixedCount}개의 Missing Script가 복구되었습니다.\n" +
                    "Inspector에서 필요한 참조들을 다시 할당해주세요.", 
                    "확인");
            }
        }
    }
} 