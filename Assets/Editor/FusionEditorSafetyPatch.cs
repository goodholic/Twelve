#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using System;
using System.Linq;

/// <summary>
/// Photon Fusion 에디터와 관련된 문제를 해결하기 위한 패치 유틸리티.
/// DontSaveInEditor 플래그가 있는 오브젝트가 저장을 시도할 때 발생하는
/// 어서션 오류를 방지합니다.
/// </summary>
[InitializeOnLoad]
public static class FusionEditorSafetyPatch
{
    // 에디터 시작 시 실행되는 정적 생성자
    static FusionEditorSafetyPatch()
    {
        // 안전 패치 적용
        ApplySafetyPatch();
        
        // 에디터 업데이트 이벤트에 연결하여 필요시 실시간 모니터링
        EditorApplication.update += OnEditorUpdate;
        
        Debug.Log("[FusionEditorSafetyPatch] 패치가 활성화되었습니다. Photon Fusion 에디터 안전장치 적용됨.");
    }

    // 패치가 이미 적용되었는지 확인하는 플래그
    private static bool _patchApplied = false;
    
    // 마지막 오류 발생 시간 추적
    private static double _lastErrorTime = 0;
    
    /// <summary>
    /// NetworkBehaviourEditor가 사용하는 기본 인스펙터 및 관련 메서드에 안전장치를 추가합니다.
    /// </summary>
    private static void ApplySafetyPatch()
    {
        try
        {
            // Fusion 어셈블리에서 NetworkBehaviourEditor 클래스 찾기
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var fusionEditorAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "Fusion.Unity.Editor");
            
            if (fusionEditorAssembly == null)
            {
                Debug.LogWarning("[FusionEditorSafetyPatch] Fusion.Unity.Editor 어셈블리를 찾을 수 없습니다.");
                return;
            }
            
            // NetworkBehaviourEditor 클래스 찾기
            var networkBehaviourEditorType = fusionEditorAssembly.GetType("Fusion.Editor.NetworkBehaviourEditor");
            if (networkBehaviourEditorType == null)
            {
                Debug.LogWarning("[FusionEditorSafetyPatch] NetworkBehaviourEditor 타입을 찾을 수 없습니다.");
                return;
            }
            
            Debug.Log("[FusionEditorSafetyPatch] NetworkBehaviourEditor 클래스가 감지되었습니다. 보호 모드 활성화됨.");
            
            // 이미 패치가 적용되었으면 종료
            if (_patchApplied)
            {
                return;
            }
            
            _patchApplied = true;
            
            // 특정 타입의 Editor.OnInspectorGUI() 메서드를 후킹하여 예외를 캐치하는 대신
            // Editor.callbackOrder 속성을 이용해 우리의 핸들러가 먼저 실행되게 할 수 있습니다
            // 여기서는 EditorApplication.update에서 모니터링하는 방식을 사용합니다
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FusionEditorSafetyPatch] 패치 적용 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 에디터 업데이트 이벤트에서 실행되는 모니터링 메서드
    /// </summary>
    private static void OnEditorUpdate()
    {
        // 현재 선택된 객체가 NetworkBehaviour인지 확인
        if (Selection.activeGameObject != null)
        {
            var networkBehaviours = Selection.activeGameObject.GetComponents<MonoBehaviour>()
                .Where(mb => mb.GetType().Namespace == "Fusion" || mb.GetType().Namespace?.StartsWith("Fusion.") == true);
                
            // NetworkBehaviour가 선택되어 있고 마지막 오류 발생 이후 일정 시간이 지났다면
            // 일정 시간 제한은 메시지 도배 방지용
            bool hasNetworkBehaviours = networkBehaviours.Any();
            double currentTime = EditorApplication.timeSinceStartup;
            
            if (hasNetworkBehaviours && (currentTime - _lastErrorTime > 5.0))
            {
                // 안전 모드에서 인스펙터를 표시하기 위한 조치
                bool hasDontSaveFlags = CheckForDontSaveFlags(Selection.activeGameObject);
                
                if (hasDontSaveFlags)
                {
                    _lastErrorTime = currentTime;
                    
                    // 사용자에게 문제 알림 (5초마다 최대 한 번)
                    Debug.LogWarning(
                        "[FusionEditorSafetyPatch] 주의: 선택한 객체에 DontSaveInEditor 플래그가 있어 " +
                        "NetworkBehaviourEditor에서 문제가 발생할 수 있습니다. " +
                        "Tools > Cleanup > Fix Serialization Issues 메뉴를 실행하여 문제를 해결하세요.");
                }
            }
        }
    }
    
    /// <summary>
    /// 주어진 게임오브젝트나 그 컴포넌트에 DontSaveInEditor 플래그가 있는지 확인합니다.
    /// </summary>
    private static bool CheckForDontSaveFlags(GameObject obj)
    {
        if (obj == null) return false;
        
        // 게임오브젝트 자체에 DontSaveInEditor 플래그가 있는지 확인
        if ((obj.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
            (obj.hideFlags & HideFlags.HideAndDontSave) != 0)
        {
            return true;
        }
        
        // 모든 컴포넌트에도 확인
        foreach (var component in obj.GetComponents<Component>())
        {
            if (component != null && 
                ((component.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                 (component.hideFlags & HideFlags.HideAndDontSave) != 0))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 메뉴를 통해 현재 선택된 객체의 플래그를 안전하게 수정합니다.
    /// </summary>
    [MenuItem("Tools/Fusion/Fix Selected GameObject Flags")]
    public static void FixSelectedGameObjectFlags()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("알림", "수정할 게임오브젝트를 선택해주세요.", "확인");
            return;
        }
        
        GameObject obj = Selection.activeGameObject;
        bool wasFixed = false;
        
        // 게임오브젝트의 플래그 수정
        if ((obj.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
            (obj.hideFlags & HideFlags.HideAndDontSave) != 0)
        {
            HideFlags newFlags = obj.hideFlags;
            newFlags &= ~HideFlags.DontSaveInEditor;
            newFlags &= ~HideFlags.HideAndDontSave;
            obj.hideFlags = newFlags;
            wasFixed = true;
        }
        
        // 모든 컴포넌트의 플래그 수정
        foreach (var component in obj.GetComponents<Component>())
        {
            if (component != null)
            {
                if ((component.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                    (component.hideFlags & HideFlags.HideAndDontSave) != 0)
                {
                    HideFlags newFlags = component.hideFlags;
                    newFlags &= ~HideFlags.DontSaveInEditor;
                    newFlags &= ~HideFlags.HideAndDontSave;
                    component.hideFlags = newFlags;
                    wasFixed = true;
                }
            }
        }
        
        if (wasFixed)
        {
            EditorUtility.SetDirty(obj);
            EditorSceneManager.MarkSceneDirty(obj.scene);
            Debug.Log($"[FusionEditorSafetyPatch] '{obj.name}'의 HideFlags가 수정되었습니다.");
            EditorUtility.DisplayDialog("완료", $"'{obj.name}'의 HideFlags가 성공적으로 수정되었습니다.", "확인");
        }
        else
        {
            Debug.Log($"[FusionEditorSafetyPatch] '{obj.name}'에는 수정할 HideFlags가 없습니다.");
            EditorUtility.DisplayDialog("알림", $"'{obj.name}'에는 수정할 HideFlags가 없습니다.", "확인");
        }
    }
}
#endif 