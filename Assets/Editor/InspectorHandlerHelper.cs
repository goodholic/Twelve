#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// Unity 인스펙터에서 안전하게 객체를 편집할 수 있도록 지원하는 헬퍼 클래스입니다.
/// 이 클래스는 Photon Fusion의 네트워크 컴포넌트에서 발생하는 인스펙터 오류를 방지합니다.
/// </summary>
[InitializeOnLoad]
public static class InspectorHandlerHelper
{
    // 인스펙터 문제가 검출된 인스턴스 추적
    private static readonly HashSet<int> _flaggedInstances = new HashSet<int>();
    
    // 마지막 클린업 수행 시간
    private static double _lastCleanupTime;
    
    // 2초마다 자동 정리 수행 여부
    private static bool _autoCleanEnabled = true;
    
    /// <summary>
    /// 에디터 시작 시 호출되는 정적 생성자
    /// </summary>
    static InspectorHandlerHelper()
    {
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.delayCall += OnEditorStart;
        
        // 인스펙터 정리 메뉴 항목을 Dynamic으로 설정
        Menu.SetChecked("Tools/Fusion/자동 인스펙터 정리 활성화", _autoCleanEnabled);
    }
    
    /// <summary>
    /// 에디터 시작 시 호출되는 메서드
    /// </summary>
    private static void OnEditorStart()
    {
        Debug.Log("[InspectorHandlerHelper] 인스펙터 핸들러가 초기화되었습니다.");
        
        // 시작 시 모든 NetworkBehaviour 플래그 검사
        if (_autoCleanEnabled)
        {
            CleanAllNetworkBehaviourFlags(silent: true);
        }
    }
    
    /// <summary>
    /// 에디터 업데이트마다 호출되는 메서드
    /// </summary>
    private static void OnEditorUpdate()
    {
        // 2초마다 한 번씩 자동 정리 수행
        double currentTime = EditorApplication.timeSinceStartup;
        if (_autoCleanEnabled && currentTime - _lastCleanupTime > 2.0)
        {
            _lastCleanupTime = currentTime;
            CleanAllNetworkBehaviourFlags(silent: true);
        }
    }
    
    /// <summary>
    /// Fusion의 모든 NetworkBehaviour 컴포넌트 인스턴스를 찾아 DontSaveInEditor 플래그를 제거합니다.
    /// </summary>
    private static void CleanAllNetworkBehaviourFlags(bool silent = false)
    {
        if (EditorApplication.isPlaying) return;
        
        int fixedCount = 0;
        
        // 현재 씬의 모든 게임오브젝트 검사
        foreach (var obj in UnityEngine.Object.FindObjectsOfType<GameObject>())
        {
            if (obj == null) continue;
            
            // 오브젝트 자체 플래그 수정
            bool wasObjectFixed = false;
            if ((obj.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                (obj.hideFlags & HideFlags.HideAndDontSave) != 0)
            {
                HideFlags newFlags = obj.hideFlags;
                newFlags &= ~HideFlags.DontSaveInEditor;
                newFlags &= ~HideFlags.HideAndDontSave;
                
                try
                {
                    obj.hideFlags = newFlags;
                    wasObjectFixed = true;
                    fixedCount++;
                }
                catch (Exception) { /* 객체 플래그 수정 중 오류 발생 시 무시 */ }
            }
            
            // 모든 컴포넌트 검사
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp == null) continue;
                
                try
                {
                    // Fusion 네임스페이스인지 확인
                    Type compType = comp.GetType();
                    string ns = compType.Namespace;
                    
                    bool isFusionComponent = ns == "Fusion" || ns?.StartsWith("Fusion.") == true || 
                                            ns == "Photon" || ns?.StartsWith("Photon.") == true;
                                            
                    // Fusion 컴포넌트이고 DontSaveInEditor 플래그가 있는지 확인
                    if (isFusionComponent)
                    {
                        // 플래그 있는지 확인
                        if ((comp.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                            (comp.hideFlags & HideFlags.HideAndDontSave) != 0)
                        {
                            _flaggedInstances.Add(comp.GetInstanceID());
                            
                            // 플래그 제거
                            HideFlags newFlags = comp.hideFlags;
                            newFlags &= ~HideFlags.DontSaveInEditor;
                            newFlags &= ~HideFlags.HideAndDontSave;
                            
                            comp.hideFlags = newFlags;
                            
                            // 변경 사항 표시
                            EditorUtility.SetDirty(comp);
                            fixedCount++;
                            
                            if (!silent)
                            {
                                Debug.Log($"[InspectorHandlerHelper] '{obj.name}'의 '{compType.Name}' 컴포넌트 플래그가 정리되었습니다.");
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // 컴포넌트 처리 중 오류 발생 시 무시
                }
            }
            
            // 게임오브젝트 수정 사항 적용
            if (wasObjectFixed)
            {
                EditorUtility.SetDirty(obj);
            }
        }
        
        // 씬 변경 사항 표시
        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            
            if (!silent)
            {
                Debug.Log($"[InspectorHandlerHelper] 총 {fixedCount}개의 오브젝트/컴포넌트 플래그가 정리되었습니다.");
                EditorUtility.DisplayDialog("정리 완료", $"총 {fixedCount}개의 오브젝트/컴포넌트 플래그가 정리되었습니다.", "확인");
            }
        }
    }
    
    /// <summary>
    /// 메뉴를 통해 모든 네트워크 컴포넌트 플래그 정리
    /// </summary>
    [MenuItem("Tools/Fusion/인스펙터 정리 실행")]
    public static void CleanupAllNetworkInspectorFlags()
    {
        CleanAllNetworkBehaviourFlags(silent: false);
    }
    
    /// <summary>
    /// 자동 인스펙터 정리 활성화/비활성화 메뉴
    /// </summary>
    [MenuItem("Tools/Fusion/자동 인스펙터 정리 활성화")]
    public static void ToggleAutoClean()
    {
        _autoCleanEnabled = !_autoCleanEnabled;
        Menu.SetChecked("Tools/Fusion/자동 인스펙터 정리 활성화", _autoCleanEnabled);
        
        EditorPrefs.SetBool("FusionAutoCleanEnabled", _autoCleanEnabled);
        
        Debug.Log($"[InspectorHandlerHelper] 자동 인스펙터 정리가 {(_autoCleanEnabled ? "활성화" : "비활성화")}되었습니다.");
    }
}
#endif 