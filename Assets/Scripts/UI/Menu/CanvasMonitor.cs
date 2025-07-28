using UnityEngine;
using System.Collections;

/// <summary>
/// Canvas 상태를 실시간으로 모니터링하여 비활성화되는 시점과 원인을 추적하는 디버깅 도구
/// </summary>
public class CanvasMonitor : MonoBehaviour
{
    [Header("모니터링 설정")]
    [Tooltip("모니터링할 Canvas들을 자동으로 찾습니다")]
    public bool autoFindCanvases = true;
    
    [Tooltip("수동으로 모니터링할 Canvas들")]
    public Canvas[] canvasesToMonitor;
    
    [Tooltip("모니터링 간격 (초)")]
    public float monitorInterval = 0.1f;
    
    private Canvas[] allCanvases;
    private bool[] lastCanvasStates;
    private string[] canvasNames;
    
    void Start()
    {
        Debug.Log("[CanvasMonitor] 🔍 Canvas 모니터링을 시작합니다...");
        
        if (autoFindCanvases)
        {
            FindAllCanvases();
        }
        else
        {
            SetupManualCanvases();
        }
        
        StartCoroutine(MonitorCanvases());
    }
    
    void FindAllCanvases()
    {
        // 씬의 모든 Canvas 찾기 (비활성화된 것도 포함)
        allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        Debug.Log($"[CanvasMonitor] 🎯 총 {allCanvases.Length}개의 Canvas 발견");
        
        SetupMonitoring();
    }
    
    void SetupManualCanvases()
    {
        allCanvases = canvasesToMonitor;
        Debug.Log($"[CanvasMonitor] 📋 수동으로 {allCanvases.Length}개의 Canvas 모니터링 설정");
        
        SetupMonitoring();
    }
    
    void SetupMonitoring()
    {
        if (allCanvases == null || allCanvases.Length == 0)
        {
            Debug.LogWarning("[CanvasMonitor] ⚠️ 모니터링할 Canvas가 없습니다!");
            return;
        }
        
        lastCanvasStates = new bool[allCanvases.Length];
        canvasNames = new string[allCanvases.Length];
        
        for (int i = 0; i < allCanvases.Length; i++)
        {
            if (allCanvases[i] != null)
            {
                lastCanvasStates[i] = allCanvases[i].enabled;
                canvasNames[i] = allCanvases[i].gameObject.name;
                
                Debug.Log($"[CanvasMonitor] 📌 Canvas '{canvasNames[i]}' 초기 상태: {(lastCanvasStates[i] ? "활성화" : "비활성화")}");
            }
        }
    }
    
    IEnumerator MonitorCanvases()
    {
        while (true)
        {
            yield return new WaitForSeconds(monitorInterval);
            
            if (allCanvases == null) continue;
            
            for (int i = 0; i < allCanvases.Length; i++)
            {
                if (allCanvases[i] == null) continue;
                
                bool currentState = allCanvases[i].enabled;
                
                // 상태 변화 감지
                if (currentState != lastCanvasStates[i])
                {
                    string stateChange = currentState ? "❇️ 활성화됨" : "❌ 비활성화됨";
                    
                    Debug.LogWarning($"[CanvasMonitor] 🚨 Canvas '{canvasNames[i]}' 상태 변화: {stateChange}");
                    Debug.LogWarning($"[CanvasMonitor] 📍 프레임: {Time.frameCount}, 시간: {Time.time:F2}초");
                    
                    // 스택 트레이스로 어디서 변경되었는지 추적
                    Debug.LogWarning($"[CanvasMonitor] 📊 호출 스택:\n{System.Environment.StackTrace}");
                    
                    // GameObject 활성화 상태도 확인
                    Debug.LogWarning($"[CanvasMonitor] 🎮 GameObject '{canvasNames[i]}' 활성화 상태: {allCanvases[i].gameObject.activeInHierarchy}");
                    
                    lastCanvasStates[i] = currentState;
                    
                    // 비활성화된 경우 원인 추가 분석
                    if (!currentState)
                    {
                        AnalyzeCanvasDeactivation(allCanvases[i], i);
                    }
                }
            }
        }
    }
    
    void AnalyzeCanvasDeactivation(Canvas canvas, int index)
    {
        Debug.LogError($"[CanvasMonitor] 🔬 Canvas '{canvasNames[index]}' 비활성화 원인 분석:");
        
        // 부모 오브젝트 확인
        Transform parent = canvas.transform.parent;
        if (parent != null)
        {
            Debug.LogError($"[CanvasMonitor] 👨‍👩‍👧‍👦 부모 오브젝트: {parent.name}, 활성화: {parent.gameObject.activeInHierarchy}");
        }
        
        // 컴포넌트 상태 확인
        Debug.LogError($"[CanvasMonitor] 🧩 Canvas 컴포넌트 활성화: {canvas.enabled}");
        Debug.LogError($"[CanvasMonitor] 🎮 GameObject 활성화: {canvas.gameObject.activeInHierarchy}");
        Debug.LogError($"[CanvasMonitor] 🏠 GameObject 자체 활성화: {canvas.gameObject.activeSelf}");
        
        // 다른 컴포넌트들 확인
        Component[] components = canvas.GetComponents<Component>();
        Debug.LogError($"[CanvasMonitor] 📦 총 {components.Length}개 컴포넌트:");
        foreach (var comp in components)
        {
            if (comp == null)
            {
                Debug.LogError($"[CanvasMonitor] ❌ Missing 컴포넌트 발견!");
            }
            else
            {
                Debug.LogError($"[CanvasMonitor] ✅ {comp.GetType().Name}");
            }
        }
    }
    
    // 수동으로 Canvas 상태 확인하는 public 메서드
    [ContextMenu("현재 Canvas 상태 출력")]
    public void PrintCurrentCanvasStates()
    {
        Debug.Log("[CanvasMonitor] 📊 현재 모든 Canvas 상태:");
        
        if (allCanvases == null)
        {
            Debug.LogWarning("[CanvasMonitor] ⚠️ 모니터링 설정이 되지 않았습니다!");
            return;
        }
        
        for (int i = 0; i < allCanvases.Length; i++)
        {
            if (allCanvases[i] != null)
            {
                string status = allCanvases[i].enabled ? "✅ 활성화" : "❌ 비활성화";
                Debug.Log($"[CanvasMonitor] Canvas '{canvasNames[i]}': {status}");
            }
            else
            {
                Debug.LogWarning($"[CanvasMonitor] Canvas {i}가 null입니다!");
            }
        }
    }
    
    void OnDestroy()
    {
        Debug.Log("[CanvasMonitor] 🔚 Canvas 모니터링이 종료되었습니다.");
    }
} 