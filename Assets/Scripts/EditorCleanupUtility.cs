#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 에디터 내에서 문제가 되는 참조를 정리하기 위한 유틸리티
/// </summary>
public static class EditorCleanupUtility
{
    [MenuItem("Tools/Cleanup/Fix Serialization Issues")]
    public static void FixSerializationIssues()
    {
        // 선택된 모든 게임오브젝트에 대해 처리
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        Debug.Log($"객체 {allObjects.Length}개 검사 중...");

        int fixedCount = 0;

        // 시작 전 씬 저장 권장
        if (!EditorUtility.DisplayDialog("Fix Serialization Issues",
            "This will attempt to fix serialization issues by checking all objects in the scene.\n\n" +
            "It's recommended to save your scene before proceeding.\n\n" +
            "Continue?", "Yes", "Cancel"))
        {
            return;
        }

        // 0. Inspector에 선택된 객체 해제 (에디터 명령)
        Selection.activeGameObject = null;

        // 첫 번째 단계: 모든 객체의 HideFlags 확인 및 수정
        foreach (GameObject obj in allObjects)
        {
            try
            {
                if (obj == null) continue;

                // 문제가 될 수 있는 HideFlags 조합 확인
                if ((obj.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                    (obj.hideFlags & HideFlags.HideAndDontSave) != 0)
                {
                    // DontSaveInEditor, HideAndDontSave 플래그 제거
                    obj.hideFlags &= ~HideFlags.DontSaveInEditor;
                    obj.hideFlags &= ~HideFlags.HideAndDontSave;
                    fixedCount++;
                    Debug.Log($"Fixed hideFlags on: {obj.name}", obj);
                }

                // 모든 컴포넌트에 대해서도 확인
                Component[] components = obj.GetComponents<Component>();
                foreach (Component comp in components)
                {
                    if (comp == null) continue;

                    if ((comp.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                        (comp.hideFlags & HideFlags.HideAndDontSave) != 0)
                    {
                        comp.hideFlags &= ~HideFlags.DontSaveInEditor;
                        comp.hideFlags &= ~HideFlags.HideAndDontSave;
                        fixedCount++;
                        Debug.Log($"Fixed hideFlags on component: {comp.GetType().Name} in {obj.name}", obj);
                    }
                }

                // Character 컴포넌트가 있고 bulletPanel이 null이면 찾아서 연결
                Character character = obj.GetComponent<Character>();
                if (character != null)
                {
                    FixCharacterBulletPanel(character);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing {obj.name}: {e.Message}", obj);
            }
        }

        // 두 번째 단계: 빈 게임오브젝트 이름이 있는지 검사
        List<GameObject> emptyNameObjects = new List<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;

            if (string.IsNullOrEmpty(obj.name))
            {
                emptyNameObjects.Add(obj);
                obj.name = "FixedEmptyName_" + System.Guid.NewGuid().ToString().Substring(0, 8);
                fixedCount++;
            }
        }

        // 세 번째 단계: 프리팹 참조 문제 수정
        #if UNITY_2018_3_OR_NEWER
        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            Debug.Log("현재 프리팹 편집 모드입니다. 프리팹 내부 참조 검사 중...");
            
            // 프리팹 내 모든 객체에 대해 외부 참조 검사
            GameObject prefabRoot = prefabStage.prefabContentsRoot;
            CheckPrefabReferences(prefabRoot);
        }
        #else
        Debug.Log("프리팹 스테이지 API는 Unity 2018.3 이상에서만 사용 가능합니다.");
        #endif

        // 네 번째 단계: EditorUtility.CollectDeepHierarchy로 모든 SerializedObjects 확인
        Debug.Log("깊은 계층 검사 및 정리 중...");
        Object[] deepHierarchy = EditorUtility.CollectDeepHierarchy(new Object[] { });
        foreach (Object obj in deepHierarchy)
        {
            if (obj == null) continue;

            // SerializedObject가 생성 가능한지 테스트
            try
            {
                SerializedObject serializedObj = new SerializedObject(obj);
                if (serializedObj != null)
                {
                    // 성공적으로 SerializedObject 생성됨
                    serializedObj.Update();
                    serializedObj.ApplyModifiedProperties();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"객체 {obj.name} ({obj.GetType().Name})의 SerializedObject를 생성할 수 없습니다: {e.Message}", obj);
            }
        }

        // 다섯 번째 단계: 씬 변경이 있다면 마킹해주기
        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // 정리 완료 메시지
        if (fixedCount > 0)
        {
            Debug.Log($"총 {fixedCount}개의 문제 수정됨!");
            EditorUtility.DisplayDialog("Cleanup Complete", 
                $"Fixed {fixedCount} potential serialization issues.\n\n" +
                "You may need to restart Unity if problems persist.", "OK");
        }
        else
        {
            Debug.Log("수정이 필요한 문제를 찾지 못했습니다.");
            EditorUtility.DisplayDialog("Cleanup Complete", 
                "No immediate issues found that needed fixing.\n\n" +
                "If problems persist, try closing and reopening the scene or restarting Unity.", "OK");
        }
    }

    private static void FixCharacterBulletPanel(Character character)
    {
        try
        {
            // PlacementManager 찾기
            PlacementManager manager = Object.FindAnyObjectByType<PlacementManager>();
            if (manager != null && manager.bulletPanel != null)
            {
                character.SetBulletPanel(manager.bulletPanel);
                Debug.Log($"Set bulletPanel for character: {character.name}", character);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to fix bulletPanel for character: {e.Message}", character);
        }
    }

    private static void CheckPrefabReferences(GameObject root)
    {
        Stack<Transform> stack = new Stack<Transform>();
        stack.Push(root.transform);

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();
            
            // 이 게임오브젝트의 모든 컴포넌트 체크
            Component[] components = current.GetComponents<Component>();
            
            foreach (Component comp in components)
            {
                if (comp == null) continue;
                
                SerializedObject serializedObject = new SerializedObject(comp);
                SerializedProperty iterator = serializedObject.GetIterator();
                
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    
                    // Object 참조 타입만 확인
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && 
                        iterator.objectReferenceValue != null)
                    {
                        GameObject refObj = iterator.objectReferenceValue as GameObject;
                        if (refObj != null && !IsChildOf(refObj.transform, root.transform) && 
                            !PrefabUtility.IsPartOfPrefabAsset(refObj))
                        {
                            Debug.LogWarning($"외부 참조 감지: {current.name}의 {comp.GetType().Name} 컴포넌트가 " +
                                            $"프리팹 외부 객체 {refObj.name}을 참조합니다.", comp);
                        }
                    }
                }
            }
            
            // 자식 추가
            foreach (Transform child in current)
            {
                stack.Push(child);
            }
        }
    }

    private static bool IsChildOf(Transform child, Transform parent)
    {
        Transform current = child;
        while (current != null)
        {
            if (current == parent)
                return true;
            current = current.parent;
        }
        return false;
    }

    [MenuItem("Tools/Cleanup/Force Refresh Scene")]
    public static void ForceRefreshScene()
    {
        if (EditorUtility.DisplayDialog("Force Refresh Scene",
            "This will close and reopen the current scene to reset all serialization states.\n\n" +
            "Any unsaved changes will be lost. Continue?",
            "Yes, Reload Scene", "Cancel"))
        {
            string currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            if (!string.IsNullOrEmpty(currentScenePath))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
                    Debug.Log("씬을 다시 로드했습니다: " + currentScenePath);
                }
            }
            else
            {
                Debug.LogWarning("현재 씬이 저장되지 않아 다시 로드할 수 없습니다.");
            }
        }
    }

    [MenuItem("Tools/Cleanup/Clear Selection and Reload Inspectors")]
    public static void ClearSelectionAndReloadInspectors()
    {
        // 현재 선택된 항목 해제 (Inspector에 표시될 객체 없게)
        Selection.activeGameObject = null;
        
        // 인스펙터 새로고침 (리플렉션을 통해 접근)
        System.Type inspectorWindowType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        if (inspectorWindowType != null)
        {
            // 열린 모든 인스펙터 창 가져오기
            Object[] inspectorWindows = Resources.FindObjectsOfTypeAll(inspectorWindowType);
            foreach (Object window in inspectorWindows)
            {
                try
                {
                    // 리플렉션으로 Repaint 메소드 호출 시도
                    var method = inspectorWindowType.GetMethod("Repaint");
                    if (method != null)
                    {
                        method.Invoke(window, null);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"인스펙터 새로고침 중 오류 발생: {e.Message}");
                }
            }
        }
        
        Debug.Log("선택이 해제되고 인스펙터가 새로고침되었습니다.");
    }
}
#endif 