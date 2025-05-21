#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Photon Fusion의 모든 네트워크 관련 객체를 찾아 HideFlags를 정리하는 자동화 도구입니다.
/// Unity 편집기 시작 시 자동으로 실행되며, 주기적으로 검사를 수행합니다.
/// </summary>
[InitializeOnLoad]
public static class FusionEditorFlagsCleaner
{
    // 마지막으로 전체 검사를 실행한 시간
    private static double _lastFullCheckTime;
    
    // 편집기가 로드될 때마다 실행되는 정적 생성자
    static FusionEditorFlagsCleaner()
    {
        // 주기적으로 업데이트 이벤트 등록 (무거운 검사는 제한된 주기로 실행)
        EditorApplication.update += OnEditorUpdate;
        
        // 객체가 선택될 때 이벤트 등록
        Selection.selectionChanged += OnSelectionChanged;
        
        Debug.Log("[FusionEditorFlagsCleaner] 초기화 완료. 네트워크 컴포넌트 안전 모드 활성화됨.");
    }
    
    /// <summary>
    /// 에디터 업데이트마다 호출되는 메서드
    /// 60초마다 한 번씩 전체 씬 검사 실행
    /// </summary>
    private static void OnEditorUpdate()
    {
        // 주기적으로 전체 씬 검사 (60초마다 한번)
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - _lastFullCheckTime > 60.0)
        {
            _lastFullCheckTime = currentTime;
            CheckSceneForProblematicObjects(silent: true);
        }
    }
    
    /// <summary>
    /// 객체 선택이 변경될 때 호출되는 메서드
    /// </summary>
    private static void OnSelectionChanged()
    {
        // 현재 선택된 객체가 Fusion 관련 객체인지 확인
        if (Selection.activeGameObject != null)
        {
            CheckSelectedObjectForFusionComponents();
        }
    }
    
    /// <summary>
    /// 선택된 객체의 Fusion 컴포넌트 확인
    /// </summary>
    private static void CheckSelectedObjectForFusionComponents()
    {
        if (Selection.activeGameObject == null) return;
        
        GameObject obj = Selection.activeGameObject;
        
        // Fusion 네임스페이스의 컴포넌트 찾기
        List<Component> fusionComponents = new List<Component>();
        
        foreach (var component in obj.GetComponents<Component>())
        {
            if (component == null) continue;
            
            try
            {
                // Fusion 또는 Photon 네임스페이스의 컴포넌트인지 확인
                string ns = component.GetType().Namespace;
                if (ns == "Fusion" || ns?.StartsWith("Fusion.") == true || 
                    ns == "Photon" || ns?.StartsWith("Photon.") == true)
                {
                    fusionComponents.Add(component);
                    
                    // DontSaveInEditor 플래그가 있는지 확인하고 자동으로 제거
                    if ((component.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                        (component.hideFlags & HideFlags.HideAndDontSave) != 0)
                    {
                        HideFlags newFlags = component.hideFlags;
                        newFlags &= ~HideFlags.DontSaveInEditor;
                        newFlags &= ~HideFlags.HideAndDontSave;
                        component.hideFlags = newFlags;
                        
                        EditorUtility.SetDirty(obj);
                        
                        Debug.Log($"[FusionEditorFlagsCleaner] '{obj.name}'의 '{component.GetType().Name}' 컴포넌트에서 DontSaveInEditor 플래그를 자동으로 제거했습니다.");
                    }
                }
            }
            catch (System.Exception)
            {
                // 컴포넌트 타입 접근 중 오류가 발생할 수 있음 (Missing 스크립트 등)
                continue;
            }
        }
    }
    
    /// <summary>
    /// 현재 씬의 모든 Fusion 컴포넌트 검사
    /// </summary>
    private static void CheckSceneForProblematicObjects(bool silent = false)
    {
        if (EditorApplication.isPlaying) return;
        
        // 현재 씬의 모든 게임 오브젝트를 검사
        List<Component> problematicComponents = new List<Component>();
        
        foreach (var rootObj in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var component in rootObj.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                
                try
                {
                    // Fusion 또는 Photon 네임스페이스의 컴포넌트인지 확인
                    string ns = component.GetType().Namespace;
                    if (ns == "Fusion" || ns?.StartsWith("Fusion.") == true || 
                        ns == "Photon" || ns?.StartsWith("Photon.") == true)
                    {
                        // DontSaveInEditor 플래그가 있는지 확인
                        if ((component.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                            (component.hideFlags & HideFlags.HideAndDontSave) != 0)
                        {
                            problematicComponents.Add(component);
                        }
                    }
                }
                catch (System.Exception)
                {
                    continue;
                }
            }
        }
        
        // 문제가 있는 컴포넌트가 발견되면 자동으로 수정
        if (problematicComponents.Count > 0)
        {
            foreach (var component in problematicComponents)
            {
                try
                {
                    // 플래그 제거
                    HideFlags newFlags = component.hideFlags;
                    newFlags &= ~HideFlags.DontSaveInEditor;
                    newFlags &= ~HideFlags.HideAndDontSave;
                    component.hideFlags = newFlags;
                    
                    EditorUtility.SetDirty(component.gameObject);
                }
                catch (System.Exception)
                {
                    continue;
                }
            }
            
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            
            if (!silent)
            {
                Debug.Log($"[FusionEditorFlagsCleaner] {problematicComponents.Count}개의 문제 컴포넌트가 자동으로 수정되었습니다.");
            }
        }
    }
    
    /// <summary>
    /// 메뉴를 통해 강제로 모든 Fusion 관련 컴포넌트 검사 및 수정
    /// </summary>
    [MenuItem("Tools/Fusion/모든 Fusion 컴포넌트 플래그 수정")]
    public static void FixAllFusionComponentFlags()
    {
        // 직접적인 메뉴 호출 시에는 silent 모드 비활성화
        CheckSceneForProblematicObjects(silent: false);
        EditorUtility.DisplayDialog("완료", "모든 Photon Fusion 관련 컴포넌트의 HideFlags가 검사되었습니다.", "확인");
    }
}
#endif 