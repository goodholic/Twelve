using UnityEngine;
using System.Collections;

/// <summary>
/// Canvas가 비활성화되는 것을 강력하게 방지하는 보호 스크립트
/// Missing Script 문제나 다른 스크립트에 의한 Canvas 비활성화를 막습니다
/// </summary>
public class CanvasProtector : MonoBehaviour
{
    [Header("보호 설정")]
    [Tooltip("보호할 Canvas들을 자동으로 찾습니다")]
    public bool autoProtectAllCanvases = true;
    
    [Tooltip("수동으로 보호할 특정 Canvas들")]
    public Canvas[] canvasesToProtect;
    
    [Tooltip("보호 체크 간격 (초) - 더 빠른 반응을 위해 낮게 설정")]
    public float protectionInterval = 0.05f; // 매우 빠른 체크
    
    [Tooltip("강제 활성화 로그 출력")]
    public bool enableDebugLogs = true;
    
    [Header("고급 보호 설정")]
    [Tooltip("MMTouchControls 등의 외부 스크립트로부터 보호")]
    public bool ignoreExternalDeactivation = true;
    
    [Tooltip("보호 강도 (낮을수록 더 자주 체크)")]
    [Range(0.01f, 1.0f)]
    public float advancedProtectionInterval = 0.05f;
    
    private Canvas[] protectedCanvases;
    private string[] canvasNames;
    private bool isProtectionActive = false;
    
    void Awake()
    {
        // 가능한 한 빨리 보호 시작
        InitializeProtection();
        StartProtection();
    }
    
    void Start()
    {
        if (!isProtectionActive)
        {
            StartProtection();
        }
    }
    
    void InitializeProtection()
    {
        if (enableDebugLogs)
            Debug.Log("[CanvasProtector] 🛡️ Canvas 보호 시스템을 초기화합니다...");
        
        if (autoProtectAllCanvases)
        {
            // 모든 Canvas 찾기 (비활성화된 것 포함)
            protectedCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
        else
        {
            protectedCanvases = canvasesToProtect;
        }
        
        if (protectedCanvases == null || protectedCanvases.Length == 0)
        {
            Debug.LogWarning("[CanvasProtector] ⚠️ 보호할 Canvas가 없습니다!");
            return;
        }
        
        canvasNames = new string[protectedCanvases.Length];
        for (int i = 0; i < protectedCanvases.Length; i++)
        {
            if (protectedCanvases[i] != null)
            {
                canvasNames[i] = protectedCanvases[i].gameObject.name;
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[CanvasProtector] 🎯 {protectedCanvases.Length}개의 Canvas를 보호합니다: {string.Join(", ", canvasNames)}");
    }
    
    void StartProtection()
    {
        if (isProtectionActive) return;
        
        isProtectionActive = true;
        StartCoroutine(ProtectCanvases());
        
        if (enableDebugLogs)
            Debug.Log("[CanvasProtector] ✅ Canvas 보호가 활성화되었습니다!");
    }
    
    IEnumerator ProtectCanvases()
    {
        while (isProtectionActive)
        {
            yield return new WaitForSeconds(ignoreExternalDeactivation ? advancedProtectionInterval : protectionInterval);
            
            if (protectedCanvases == null) continue;
            
            for (int i = 0; i < protectedCanvases.Length; i++)
            {
                if (protectedCanvases[i] == null) continue;
                
                // 고급 보호: MMTouchControls 등의 외부 비활성화 무시
                if (ignoreExternalDeactivation)
                {
                    // GameObject 자체가 비활성화되었다면 즉시 재활성화
                    if (!protectedCanvases[i].gameObject.activeSelf)
                    {
                        protectedCanvases[i].gameObject.SetActive(true);
                        
                        if (enableDebugLogs)
                            Debug.LogWarning($"[CanvasProtector] 🚨 GameObject '{canvasNames[i]}' 강제 재활성화!");
                    }
                    
                    // Canvas 컴포넌트가 비활성화되었다면 즉시 재활성화
                    if (!protectedCanvases[i].enabled)
                    {
                        protectedCanvases[i].enabled = true;
                        
                        if (enableDebugLogs)
                            Debug.LogWarning($"[CanvasProtector] 🚨 Canvas '{canvasNames[i]}' 강제 재활성화!");
                    }
                    
                    // CanvasGroup alpha 값도 보호
                    CanvasGroup canvasGroup = protectedCanvases[i].GetComponent<CanvasGroup>();
                    if (canvasGroup != null && canvasGroup.alpha < 0.1f)
                    {
                        canvasGroup.alpha = 1.0f;
                        
                        if (enableDebugLogs)
                            Debug.LogWarning($"[CanvasProtector] 🚨 CanvasGroup '{canvasNames[i]}' alpha 복원!");
                    }
                }
                else
                {
                    // 기본 보호
                    if (!protectedCanvases[i].enabled)
                    {
                        protectedCanvases[i].enabled = true;
                        
                        if (enableDebugLogs)
                            Debug.LogWarning($"[CanvasProtector] 🚨 Canvas '{canvasNames[i]}' 강제 재활성화!");
                    }
                    
                    if (!protectedCanvases[i].gameObject.activeSelf)
                    {
                        protectedCanvases[i].gameObject.SetActive(true);
                        
                        if (enableDebugLogs)
                            Debug.LogWarning($"[CanvasProtector] 🚨 GameObject '{canvasNames[i]}' 강제 재활성화!");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 특정 Canvas에 특별 보호 적용
    /// </summary>
    public void ForceActivateCanvas(string canvasName)
    {
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas != null && canvas.gameObject.name.Contains(canvasName))
            {
                canvas.enabled = true;
                canvas.gameObject.SetActive(true);
                
                if (enableDebugLogs)
                    Debug.LogWarning($"[CanvasProtector] 🔧 Canvas '{canvasName}' 수동 활성화!");
            }
        }
    }
    
    /// <summary>
    /// ToonFXSceneSelect 같은 문제 스크립트들을 비활성화
    /// </summary>
    [ContextMenu("문제 스크립트 비활성화")]
    public void DisableProblematicScripts()
    {
        Debug.Log("[CanvasProtector] 🔍 문제가 될 수 있는 스크립트들을 찾아 비활성화합니다...");
        
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int disabledCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null)
                {
                    string scriptName = script.GetType().Name;
                    
                    // Canvas를 비활성화할 수 있는 알려진 문제 스크립트들
                    if (scriptName == "ToonFXSceneSelect" || 
                        scriptName.Contains("SceneSelect") ||
                        scriptName.Contains("CanvasToggle"))
                    {
                        if (script.enabled)
                        {
                            script.enabled = false;
                            disabledCount++;
                            
                            if (enableDebugLogs)
                                Debug.LogWarning($"[CanvasProtector] 🚫 문제 스크립트 비활성화: {scriptName} on {obj.name}");
                        }
                    }
                }
            }
        }
        
        Debug.Log($"[CanvasProtector] ✅ {disabledCount}개의 문제 스크립트를 비활성화했습니다.");
    }
    
    /// <summary>
    /// 모든 Canvas 즉시 활성화
    /// </summary>
    [ContextMenu("모든 Canvas 강제 활성화")]
    public void ForceActivateAllCanvases()
    {
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int activatedCount = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas != null)
            {
                bool wasInactive = !canvas.enabled || !canvas.gameObject.activeSelf;
                
                canvas.enabled = true;
                canvas.gameObject.SetActive(true);
                
                if (wasInactive)
                {
                    activatedCount++;
                    if (enableDebugLogs)
                        Debug.LogWarning($"[CanvasProtector] 🔧 Canvas '{canvas.gameObject.name}' 강제 활성화!");
                }
            }
        }
        
        Debug.Log($"[CanvasProtector] ✅ {activatedCount}개의 Canvas를 활성화했습니다.");
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && isProtectionActive)
        {
            // 포커스를 되찾았을 때 모든 Canvas 재활성화
            Invoke("ForceActivateAllCanvases", 0.1f);
        }
    }
    
    void OnDestroy()
    {
        isProtectionActive = false;
        
        if (enableDebugLogs)
            Debug.Log("[CanvasProtector] 🔚 Canvas 보호가 종료되었습니다.");
    }
    
    // 에디터에서 정보 표시
    void OnDrawGizmos()
    {
        if (protectedCanvases != null && protectedCanvases.Length > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
} 