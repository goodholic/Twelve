#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 씬 저장 전에 문제가 있는 객체의 HideFlags를 자동으로 수정하는 스크립트
/// </summary>
[InitializeOnLoad]
public class PreSaveSceneProcessor
{
    // 에디터 시작 시 이벤트 등록
    static PreSaveSceneProcessor()
    {
        EditorSceneManager.sceneSaving += OnSceneSaving;
        Debug.Log("[PreSaveSceneProcessor] 씬 저장 전 처리기가 초기화되었습니다.");
    }
    
    // 씬 저장 전에 호출되는 메서드
    private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
    {
        // 씬의 모든 게임 오브젝트 검사
        Debug.Log($"[PreSaveSceneProcessor] '{scene.name}' 씬이 저장되기 전에 문제 있는 플래그를 검사합니다...");
        
        int fixedCount = 0;
        List<GameObject> problematicObjects = new List<GameObject>();
        
        // 모든 루트 게임 오브젝트 검사
        foreach (var rootObj in scene.GetRootGameObjects())
        {
            // 각 게임 오브젝트와 그 자식들 검사
            CheckAndFixRecursive(rootObj, ref fixedCount, problematicObjects);
        }
        
        // 결과 로그
        if (fixedCount > 0)
        {
            Debug.Log($"[PreSaveSceneProcessor] '{scene.name}' 씬 저장 전 {fixedCount}개 객체/컴포넌트의 플래그가 수정되었습니다.");
            
            // 문제가 있었던 게임 오브젝트 목록 (최대 5개까지만 표시)
            if (problematicObjects.Count > 0)
            {
                string objNames = string.Join(", ", problematicObjects
                    .Take(5)
                    .Select(o => o.name));
                    
                if (problematicObjects.Count > 5)
                {
                    objNames += $" 외 {problematicObjects.Count - 5}개";
                }
                
                Debug.Log($"[PreSaveSceneProcessor] 수정된 객체: {objNames}");
            }
        }
    }
    
    // 게임 오브젝트와 그 자식들을 재귀적으로 검사하고 수정
    private static void CheckAndFixRecursive(GameObject obj, ref int fixedCount, List<GameObject> problematicObjects)
    {
        bool wasObjectFixed = false;
        
        // 게임 오브젝트 자체 검사
        if ((obj.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
            (obj.hideFlags & HideFlags.HideAndDontSave) != 0)
        {
            try
            {
                HideFlags newFlags = obj.hideFlags;
                newFlags &= ~HideFlags.DontSaveInEditor;
                newFlags &= ~HideFlags.HideAndDontSave;
                obj.hideFlags = newFlags;
                wasObjectFixed = true;
                fixedCount++;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PreSaveSceneProcessor] '{obj.name}' 플래그 수정 중 오류: {ex.Message}");
            }
        }
        
        // 모든 컴포넌트 검사
        foreach (var component in obj.GetComponents<Component>())
        {
            if (component == null) continue;
            
            try
            {
                if ((component.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                    (component.hideFlags & HideFlags.HideAndDontSave) != 0)
                {
                    HideFlags newFlags = component.hideFlags;
                    newFlags &= ~HideFlags.DontSaveInEditor;
                    newFlags &= ~HideFlags.HideAndDontSave;
                    component.hideFlags = newFlags;
                    
                    // 컴포넌트 변경 사항 저장
                    EditorUtility.SetDirty(component);
                    wasObjectFixed = true;
                    fixedCount++;
                }
            }
            catch (Exception)
            {
                // 컴포넌트 처리 중 오류 발생 시 무시
            }
        }
        
        // 객체가 수정되었으면 리스트에 추가
        if (wasObjectFixed)
        {
            problematicObjects.Add(obj);
            EditorUtility.SetDirty(obj);
        }
        
        // 모든 자식 객체 검사
        foreach (Transform child in obj.transform)
        {
            CheckAndFixRecursive(child.gameObject, ref fixedCount, problematicObjects);
        }
    }
    
    // 수동으로 실행할 수 있는 메뉴 항목
    [MenuItem("Tools/Fusion/저장 전 처리 수동 실행")]
    public static void ManuallyProcessScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int fixedCount = 0;
        List<GameObject> problematicObjects = new List<GameObject>();
        
        foreach (var rootObj in scene.GetRootGameObjects())
        {
            CheckAndFixRecursive(rootObj, ref fixedCount, problematicObjects);
        }
        
        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog("수정 완료", $"{fixedCount}개의 객체/컴포넌트 플래그가 수정되었습니다.", "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("검사 완료", "수정이 필요한 객체가 없습니다.", "확인");
        }
    }
}
#endif 