// Assets\Editor\EditorCleanupUtility.cs

// Assets\Scripts\EditorCleanupUtility.cs

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 에디터 내에서 문제가 되는 참조들을 정리하기 위한 유틸리티:
/// 1) DontSaveInEditor / HideAndDontSave 플래그 제거
/// 2) Missing Script / Missing Reference 검사
/// 3) Prefab 외부 참조 검사
/// 4) DeepHierarchy 통한 SerializedObject 갱신 등
/// </summary>
public static class EditorCleanupUtility
{
    [MenuItem("Tools/Cleanup/Fix Serialization Issues")]
    public static void FixSerializationIssues()
    {
        // 씬 내 모든 게임오브젝트 스캔
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        Debug.Log($"객체 {allObjects.Length}개 검사 중...");

        int fixedCount = 0;

        // 진행 전 씬 저장 권장 안내
        if (!EditorUtility.DisplayDialog(
            "Fix Serialization Issues",
            "This will attempt to fix serialization issues by checking all objects in the scene.\n\n" +
            "It's recommended to save your scene before proceeding.\n\n" +
            "Continue?",
            "Yes",
            "Cancel"
        ))
        {
            return;
        }

        // 0) 현재 Inspector 선택 해제 (문제 해결 시 충돌 방지)
        Selection.activeGameObject = null;

        // =============================
        // 1) 모든 객체 HideFlags 제거 + Missing Script 검사
        // =============================
        foreach (GameObject obj in allObjects)
        {
            try
            {
                if (obj == null) continue;

                // (A) DontSaveInEditor / HideAndDontSave 플래그 제거
                if ((obj.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                    (obj.hideFlags & HideFlags.HideAndDontSave) != 0)
                {
                    obj.hideFlags &= ~HideFlags.DontSaveInEditor;
                    obj.hideFlags &= ~HideFlags.HideAndDontSave;
                    fixedCount++;
                    Debug.Log($"Fixed hideFlags on GameObject: {obj.name}", obj);
                }

                // (B) 모든 컴포넌트에 대해서도 HideFlags 제거
                Component[] components = obj.GetComponents<Component>();
                foreach (Component comp in components)
                {
                    // Missing Script인 경우 comp == null 이 됨
                    if (comp == null)
                    {
                        // === Missing Script 발견 ===
                        Debug.LogWarning($"[EditorCleanupUtility] Missing Script on '{obj.name}'", obj);
                        continue;
                    }

                    // hideFlags 제거
                    if ((comp.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                        (comp.hideFlags & HideFlags.HideAndDontSave) != 0)
                    {
                        comp.hideFlags &= ~HideFlags.DontSaveInEditor;
                        comp.hideFlags &= ~HideFlags.HideAndDontSave;
                        fixedCount++;
                        Debug.Log($"Fixed hideFlags on component: {comp.GetType().Name} in {obj.name}", obj);
                    }

                    // (C) 컴포넌트 내부 “Missing Reference” 검사
                    //     - SerializedObject를 순회하며, ObjectReference 필드 값이
                    //       완전히 null(=0)인데 실제로는 인스턴스ID가 남았는지 등 확인 가능
                    CheckMissingReferences(obj, comp);
                }

                // (D) 추가적으로, Character의 bulletPanel이 null이면 연결 시도(예: PlacementManager)
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

        // =============================
        // 2) 빈 게임오브젝트 이름 검사
        // =============================
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

        // =============================
        // 3) 현재 Prefab Stage에서 외부 참조 검사
        // =============================
#if UNITY_2018_3_OR_NEWER
        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            Debug.Log("현재 프리팹 편집 모드입니다. 프리팹 내부 참조 검사 중...");
            
            GameObject prefabRoot = prefabStage.prefabContentsRoot;
            CheckPrefabReferences(prefabRoot);
        }
#else
        Debug.Log("프리팹 스테이지 API는 Unity 2018.3 이상에서만 사용 가능합니다.");
#endif

        // =============================
        // 4) DeepHierarchy 통한 SerializedObject 확인
        // =============================
        Debug.Log("깊은 계층 검사 및 정리 중...");
        Object[] deepHierarchy = EditorUtility.CollectDeepHierarchy(new Object[] { });
        foreach (Object obj in deepHierarchy)
        {
            if (obj == null) continue;

            try
            {
                SerializedObject serializedObj = new SerializedObject(obj);
                if (serializedObj != null)
                {
                    serializedObj.Update();
                    serializedObj.ApplyModifiedProperties();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EditorCleanupUtility] {obj.name} ({obj.GetType().Name})의 SerializedObject 생성 불가: {e.Message}", obj);
            }
        }

        // 씬 변경이 있었다면 MarkSceneDirty
        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // 결과 출력
        if (fixedCount > 0)
        {
            Debug.Log($"총 {fixedCount}개의 문제를 수정했습니다!");
            EditorUtility.DisplayDialog(
                "Cleanup Complete",
                $"Fixed {fixedCount} potential serialization issues.\n\n" +
                "You may need to restart Unity if problems persist.",
                "OK"
            );
        }
        else
        {
            Debug.Log("수정이 필요한 문제를 찾지 못했습니다.");
            EditorUtility.DisplayDialog(
                "Cleanup Complete",
                "No immediate issues found that needed fixing.\n\n" +
                "If problems persist, try closing and reopening the scene or restarting Unity.",
                "OK"
            );
        }
    }

    /// <summary>
    /// 컴포넌트 내부 SerializedProperty를 순회하여, ObjectReference가
    /// 완전히 Missing되어 있진 않은지(InstanceID는 있는데 null)를 간단히 검사.
    /// </summary>
    private static void CheckMissingReferences(GameObject obj, Component comp)
    {
        try
        {
            SerializedObject so = new SerializedObject(comp);
            SerializedProperty sp = so.GetIterator();
            bool enterChildren = true;

            while (sp.NextVisible(enterChildren))
            {
                enterChildren = false;

                // ObjectReference 타입의 SerializedProperty만 체크
                if (sp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    // missing reference (objectReferenceInstanceIDValue != 0) but objectReferenceValue == null
                    if (sp.objectReferenceInstanceIDValue != 0 && sp.objectReferenceValue == null)
                    {
                        Debug.LogWarning(
                            $"[EditorCleanupUtility] Missing Reference in {comp.GetType().Name} on '{obj.name}', property={sp.name}",
                            obj
                        );
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"CheckMissingReferences() 실패: {e.Message}", obj);
        }
    }

    /// <summary>
    /// Character 컴포넌트의 bulletPanel이 null이면, PlacementManager에서 가져와 연결 시도
    /// </summary>
    private static void FixCharacterBulletPanel(Character character)
    {
        try
        {
            if (character == null) return;
            if (character.GetComponent<Character>() == null) return;

            if (character != null)
            {
                PlacementManager manager = Object.FindAnyObjectByType<PlacementManager>();
                if (manager != null && manager.bulletPanel != null && character != null)
                {
                    // 이미 연결되어 있지 않으면 할당
                    // (필요 시, 무조건 덮어쓰기를 원하면 if(... == null) 체크 제거)
                    character.SetBulletPanel(manager.bulletPanel);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to fix bulletPanel for character: {e.Message}", character);
        }
    }

#if UNITY_2018_3_OR_NEWER
    /// <summary>
    /// PrefabStageUtility.GetCurrentPrefabStage()일 때, 프리팹 내부에서
    /// 외부 객체를 참조하고 있지 않은지 검사
    /// </summary>
    private static void CheckPrefabReferences(GameObject root)
    {
        if (root == null) return;
        Stack<Transform> stack = new Stack<Transform>();
        stack.Push(root.transform);

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();
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

                    if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                        iterator.objectReferenceValue != null)
                    {
                        GameObject refObj = iterator.objectReferenceValue as GameObject;
                        if (refObj != null && !IsChildOf(refObj.transform, root.transform) &&
                            !PrefabUtility.IsPartOfPrefabAsset(refObj))
                        {
                            Debug.LogWarning(
                                $"[EditorCleanupUtility] 외부 참조 감지: {current.name}의 {comp.GetType().Name}가 프리팹 외부 객체 {refObj.name} 참조",
                                comp
                            );
                        }
                    }
                }
            }

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
            if (current == parent) return true;
            current = current.parent;
        }
        return false;
    }
#endif

    [MenuItem("Tools/Cleanup/Force Refresh Scene")]
    public static void ForceRefreshScene()
    {
        if (EditorUtility.DisplayDialog(
            "Force Refresh Scene",
            "This will close and reopen the current scene to reset all serialization states.\n\n" +
            "Any unsaved changes will be lost. Continue?",
            "Yes, Reload Scene",
            "Cancel"
        ))
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
        // 현재 선택된 항목 해제
        Selection.activeGameObject = null;

        // 인스펙터 창들 새로고침
        System.Type inspectorWindowType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        if (inspectorWindowType != null)
        {
            Object[] inspectorWindows = Resources.FindObjectsOfTypeAll(inspectorWindowType);
            foreach (Object window in inspectorWindows)
            {
                try
                {
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
