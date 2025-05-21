#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Linq;

/// <summary>
/// NetworkBehaviourEditor에서 발생하는 예외를 방지하기 위한 가드 클래스입니다.
/// </summary>
[InitializeOnLoad]
public static class FusionInspectorGuard
{
    // 어셈블리에서 타입을 찾을 수 없을 때를 대비한 기본 문자열 식별자
    private const string NETWORK_BEHAVIOUR_EDITOR_TYPE_NAME = "Fusion.Editor.NetworkBehaviourEditor";
    private const string NETWORK_OBJECT_TYPE_NAME = "Fusion.NetworkObject";
    private const string NETWORK_BEHAVIOUR_TYPE_NAME = "Fusion.NetworkBehaviour";
    
    // 초기화 시 실행되는 정적 생성자
    static FusionInspectorGuard()
    {
        // 에디터 업데이트에 대한 콜백 등록
        EditorApplication.update += OnEditorUpdate;
        
        // 인스펙터가 표시되기 전 콜백 등록 (가능한 경우)
        TryRegisterInspectorCallback();
        
        Debug.Log("[FusionInspectorGuard] Fusion NetworkBehaviourEditor 예외 방지 가드가 활성화되었습니다.");
    }
    
    // 마지막 경고 표시 시간
    private static double _lastWarningTime = 0;
    
    /// <summary>
    /// Editor.finishedDefaultHeaderGUI 콜백을 등록 시도합니다.
    /// </summary>
    private static void TryRegisterInspectorCallback()
    {
        try
        {
            // 리플렉션을 통해 Fusion 어셈블리 찾기
            var fusionEditorAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Fusion.Unity.Editor");
                
            if (fusionEditorAssembly != null)
            {
                var networkBehaviourEditorType = fusionEditorAssembly.GetType(NETWORK_BEHAVIOUR_EDITOR_TYPE_NAME);
                if (networkBehaviourEditorType != null)
                {
                    Debug.Log("[FusionInspectorGuard] NetworkBehaviourEditor 타입을 성공적으로 찾았습니다.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FusionInspectorGuard] 초기화 중 오류: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 에디터 업데이트마다 호출되는 메서드
    /// </summary>
    private static void OnEditorUpdate()
    {
        // 현재 선택된 오브젝트가 NetworkBehaviour를 가지고 있는지 확인
        if (Selection.activeGameObject != null && UnityEngine.Time.realtimeSinceStartup - _lastWarningTime > 5.0f)
        {
            var components = Selection.activeGameObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                
                try
                {
                    Type compType = comp.GetType();
                    if (compType.Namespace == "Fusion" || compType.Namespace?.StartsWith("Fusion.") == true)
                    {
                        // DontSaveInEditor 플래그 확인
                        if ((comp.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                            (comp.hideFlags & HideFlags.HideAndDontSave) != 0)
                        {
                            // 플래그 자동 수정
                            comp.hideFlags &= ~HideFlags.DontSaveInEditor;
                            comp.hideFlags &= ~HideFlags.HideAndDontSave;
                            EditorUtility.SetDirty(comp);
                            
                            _lastWarningTime = UnityEngine.Time.realtimeSinceStartup;
                            Debug.Log($"[FusionInspectorGuard] '{comp.name}'의 '{comp.GetType().Name}' 컴포넌트에서 DontSaveInEditor 플래그를 자동으로 제거했습니다.");
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    // 컴포넌트 타입에 접근하는 도중 오류가 발생할 수 있습니다.
                    continue;
                }
            }
        }
    }
    
    /// <summary>
    /// Photon Fusion의 모든 NetworkBehaviour 컴포넌트 인스턴스를 찾아 DontSaveInEditor 플래그를 제거합니다.
    /// </summary>
    [MenuItem("Tools/Fusion/모든 NetworkBehaviour 플래그 정리")]
    public static void CleanAllNetworkBehaviourFlags()
    {
        var allNetworkBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>()
            .Where(mb => {
                if (mb == null) return false;
                try {
                    Type mbType = mb.GetType();
                    return mbType.Namespace == "Fusion" || mbType.Namespace?.StartsWith("Fusion.") == true;
                }
                catch {
                    return false;
                }
            })
            .ToList();
            
        int fixedCount = 0;
        
        foreach (var behaviour in allNetworkBehaviours)
        {
            if ((behaviour.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                (behaviour.hideFlags & HideFlags.HideAndDontSave) != 0)
            {
                behaviour.hideFlags &= ~HideFlags.DontSaveInEditor;
                behaviour.hideFlags &= ~HideFlags.HideAndDontSave;
                EditorUtility.SetDirty(behaviour);
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            EditorUtility.DisplayDialog("완료", $"{fixedCount}개의 NetworkBehaviour의 플래그가 정리되었습니다.", "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("알림", "수정이 필요한 NetworkBehaviour가 없습니다.", "확인");
        }
    }
}
#endif 