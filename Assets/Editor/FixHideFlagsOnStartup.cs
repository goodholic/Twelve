#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System;

/// <summary>
/// Unity 에디터 시작 시 자동으로 DontSaveInEditor 플래그 문제를 수정하는 스크립트
/// </summary>
[InitializeOnLoad]
public class FixHideFlagsOnStartup
{
    // 생성자는 에디터 시작 시 자동으로 호출됨
    static FixHideFlagsOnStartup()
    {
        // 에디터 초기화 후 실행되도록 콜백 등록
        EditorApplication.delayCall += DelayedStartupCheck;
    }
    
    private static void DelayedStartupCheck()
    {
        // 플레이 모드일 때는 실행하지 않음
        if (EditorApplication.isPlaying)
            return;
            
        // 씬이 로드되었는지 확인
        if (!EditorSceneManager.GetActiveScene().isLoaded)
            return;
            
        Debug.Log("[FixHideFlagsOnStartup] 자동 플래그 검사 및 수정을 시작합니다...");
        
        // 모든 문제가 있는 컴포넌트 찾기
        FixAllProblematicObjects();
    }
    
    /// <summary>
    /// 씬의 모든 문제 있는 객체 수정
    /// </summary>
    private static void FixAllProblematicObjects()
    {
        int fixedCount = 0;
        
        // 모든 게임 오브젝트 검사
        foreach (var rootObj in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            fixedCount += FixObjectAndChildren(rootObj);
        }
        
        // 수정된 것이 있으면 씬 저장
        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[FixHideFlagsOnStartup] 총 {fixedCount}개의 객체/컴포넌트 플래그가 자동으로 수정되었습니다.");
        }
    }
    
    /// <summary>
    /// 객체와 모든 자식 객체의 플래그 수정
    /// </summary>
    private static int FixObjectAndChildren(GameObject obj)
    {
        int fixedCount = 0;
        
        // 객체 자체 수정
        fixedCount += FixObjectFlags(obj);
        
        // 모든 자식 객체 수정
        foreach (Transform child in obj.transform)
        {
            fixedCount += FixObjectAndChildren(child.gameObject);
        }
        
        return fixedCount;
    }
    
    /// <summary>
    /// 객체와 컴포넌트의 플래그 수정
    /// </summary>
    private static int FixObjectFlags(GameObject obj)
    {
        int fixedCount = 0;
        bool wasObjectFixed = false;
        
        // 게임 오브젝트 플래그 수정
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
                Debug.LogError($"객체 '{obj.name}' 플래그 수정 중 오류: {ex.Message}");
            }
        }
        
        // 모든 컴포넌트 수정
        foreach (var component in obj.GetComponents<Component>())
        {
            if (component == null) continue;
            
            try
            {
                // 컴포넌트에 DontSaveInEditor 플래그가 있는지 확인
                if ((component.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                    (component.hideFlags & HideFlags.HideAndDontSave) != 0)
                {
                    // 플래그 제거
                    HideFlags newFlags = component.hideFlags;
                    newFlags &= ~HideFlags.DontSaveInEditor;
                    newFlags &= ~HideFlags.HideAndDontSave;
                    component.hideFlags = newFlags;
                    
                    EditorUtility.SetDirty(component);
                    fixedCount++;
                }
            }
            catch (Exception)
            {
                // 컴포넌트 처리 중 오류 발생 시 무시하고 계속 진행
            }
        }
        
        // 게임 오브젝트 변경사항 저장
        if (wasObjectFixed)
        {
            EditorUtility.SetDirty(obj);
        }
        
        return fixedCount;
    }
    
    // 수동으로 실행할 수 있는 메뉴 항목
    [MenuItem("Tools/Fusion/시작 시 자동 수정 수동 실행")]
    public static void ManuallyFixAllObjects()
    {
        FixAllProblematicObjects();
        EditorUtility.DisplayDialog("수정 완료", "모든 객체와 컴포넌트의 HideFlags가 검사 및 수정되었습니다.", "확인");
    }
}
#endif 