#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Photon Fusion 네트워크 컴포넌트와 관련된 문제를 해결하기 위한
/// 편리한 메뉴 및 유틸리티 함수들을 제공합니다.
/// </summary>
public static class NetworkSafetyMenu
{
    [MenuItem("Tools/Fusion/안전 모드로 씬 다시 열기")]
    public static void ReopenSceneInSafeMode()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("사용자가 씬 저장을 취소했습니다.");
            return;
        }
        
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(currentScenePath))
        {
            EditorUtility.DisplayDialog("알림", "저장된 씬에서만 사용할 수 있습니다.", "확인");
            return;
        }
        
        // 씬을 다시 열기 전에 선택 해제
        Selection.activeGameObject = null;
        EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        Debug.Log($"[NetworkSafetyMenu] 씬을 안전 모드로 다시 열었습니다: {currentScenePath}");
    }
    
    [MenuItem("Tools/Fusion/모든 Fusion 컴포넌트 검사")]
    public static void CheckAllFusionComponents()
    {
        List<Component> problematicComponents = new List<Component>();
        
        // 현재 씬의 모든 게임 오브젝트를 검사
        foreach (var rootObj in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var component in rootObj.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                
                try
                {
                    // Fusion 네임스페이스의 컴포넌트만 확인
                    if (component.GetType().Namespace == "Fusion" || 
                        component.GetType().Namespace?.StartsWith("Fusion.") == true)
                    {
                        if ((component.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                            (component.hideFlags & HideFlags.HideAndDontSave) != 0)
                        {
                            problematicComponents.Add(component);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"컴포넌트 검사 중 오류: {ex.Message}");
                }
            }
        }
        
        // 결과 보고
        if (problematicComponents.Count > 0)
        {
            Debug.LogWarning($"[NetworkSafetyMenu] {problematicComponents.Count}개의 문제 있는 Fusion 컴포넌트가 발견되었습니다.");
            
            // 첫 번째 문제 컴포넌트 선택
            if (problematicComponents[0] != null && problematicComponents[0].gameObject != null)
            {
                Selection.activeGameObject = problematicComponents[0].gameObject;
                EditorGUIUtility.PingObject(problematicComponents[0].gameObject);
            }
            
            // 자동 수정 제안
            if (EditorUtility.DisplayDialog(
                "문제 발견",
                $"{problematicComponents.Count}개의 Fusion 컴포넌트에 DontSaveInEditor 플래그가 있습니다.\n" +
                "이 문제로 인해 인스펙터에서 오류가 발생할 수 있습니다.\n\n" +
                "모든 문제 컴포넌트의 플래그를 자동으로 수정할까요?",
                "예, 자동 수정",
                "아니오"))
            {
                FixAllProblematicComponents(problematicComponents);
            }
        }
        else
        {
            EditorUtility.DisplayDialog("검사 완료", "문제가 있는 Fusion 컴포넌트가 발견되지 않았습니다.", "확인");
        }
    }
    
    // 문제가 있는 모든 컴포넌트 수정
    private static void FixAllProblematicComponents(List<Component> components)
    {
        int fixedCount = 0;
        
        foreach (var component in components)
        {
            if (component == null) continue;
            
            try
            {
                HideFlags oldFlags = component.hideFlags;
                HideFlags newFlags = oldFlags;
                newFlags &= ~HideFlags.DontSaveInEditor;
                newFlags &= ~HideFlags.HideAndDontSave;
                
                component.hideFlags = newFlags;
                EditorUtility.SetDirty(component.gameObject);
                fixedCount++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"컴포넌트 '{component.name}.{component.GetType().Name}' 수정 중 오류: {ex.Message}");
            }
        }
        
        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[NetworkSafetyMenu] {fixedCount}개 컴포넌트의 플래그가 수정되었습니다.");
            EditorUtility.DisplayDialog("수정 완료", $"{fixedCount}개 컴포넌트의 플래그가 성공적으로 수정되었습니다.", "확인");
        }
    }
    
    [MenuItem("Tools/Fusion/NetworkObject 수정 및 설정 초기화")]
    public static void ResetAndFixNetworkObjects()
    {
        var networkObjects = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(mb => mb.GetType().Name == "NetworkObject")
            .ToList();
            
        if (networkObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("알림", "씬에서 NetworkObject를 찾을 수 없습니다.", "확인");
            return;
        }
        
        if (!EditorUtility.DisplayDialog(
            "NetworkObject 초기화",
            $"씬에서 {networkObjects.Count}개의 NetworkObject를 찾았습니다.\n" +
            "이 작업은 모든 NetworkObject의 HideFlags를 초기화하고 " + 
            "일부 문제를 해결할 수 있습니다.\n\n" +
            "계속하시겠습니까?",
            "예, 초기화",
            "취소"))
        {
            return;
        }
        
        int fixedCount = 0;
        
        foreach (var networkObj in networkObjects)
        {
            try
            {
                // HideFlags 수정
                if ((networkObj.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                    (networkObj.hideFlags & HideFlags.HideAndDontSave) != 0)
                {
                    networkObj.hideFlags &= ~HideFlags.DontSaveInEditor;
                    networkObj.hideFlags &= ~HideFlags.HideAndDontSave;
                    EditorUtility.SetDirty(networkObj.gameObject);
                    fixedCount++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkObject 수정 중 오류: {ex.Message}");
            }
        }
        
        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[NetworkSafetyMenu] {fixedCount}개 NetworkObject의 설정이 초기화되었습니다.");
            EditorUtility.DisplayDialog("초기화 완료", $"{fixedCount}개 NetworkObject의 설정이 초기화되었습니다.", "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("알림", "수정이 필요한 NetworkObject가 없습니다.", "확인");
        }
    }
}
#endif 