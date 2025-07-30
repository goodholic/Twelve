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
    
    [Tooltip("모니터링 간격 (초) - 매우 빠른 감지를 위해 낮게 설정")]
    public float monitorInterval = 0.01f; // 훨씬 더 빠른 체크
    
    [Header("고급 추적 설정")]
    [Tooltip("Simulation Canvas만 집중 모니터링")]
    public bool focusOnSimulationCanvas = true;
    
    [Tooltip("GameObject 상태 변화도 추적")]
    public bool trackGameObjectState = true;
    
    [Tooltip("CanvasGroup 상태도 추적")]
    public bool trackCanvasGroup = true;
    
    private Canvas[] allCanvases;
    private bool[] lastCanvasStates;
    private bool[] lastGameObjectStates;
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
        lastGameObjectStates = new bool[allCanvases.Length];
        canvasNames = new string[allCanvases.Length];
        
        for (int i = 0; i < allCanvases.Length; i++)
        {
            if (allCanvases[i] != null)
            {
                lastCanvasStates[i] = allCanvases[i].enabled;
                lastGameObjectStates[i] = allCanvases[i].gameObject.activeInHierarchy;
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
                bool currentGameObjectState = allCanvases[i].gameObject.activeInHierarchy;
                
                // 비활성화 감지 - 이전 상태가 활성화였는데 현재 비활성화인 경우
                bool wasActive = lastCanvasStates[i] && lastGameObjectStates[i];
                bool isActive = currentState && currentGameObjectState;
                bool justDeactivated = wasActive && !isActive;
                
                // 상태 변화 감지
                if (currentState != lastCanvasStates[i] || currentGameObjectState != lastGameObjectStates[i])
                {
                    string canvasStateChange = currentState ? "❇️ Canvas 활성화됨" : "❌ Canvas 비활성화됨";
                    string gameObjectStateChange = currentGameObjectState ? "❇️ GameObject 활성화됨" : "❌ GameObject 비활성화됨";
                    
                    // Simulation Canvas인 경우 특별히 강조 표시
                    bool isSimulationCanvas = canvasNames[i].Contains("Simulation");
                    string prefix = isSimulationCanvas ? "🚨🚨🚨 [SIMULATION CANVAS] 🚨🚨🚨" : "";
                    
                    // 비활성화는 에러, 활성화는 경고로 구분
                    bool isDeactivation = !currentState || !currentGameObjectState;
                    
                    if (justDeactivated)
                    {
                        // 방금 비활성화된 경우 - 가장 중요한 정보!
                        Debug.LogError($"{prefix}[CanvasMonitor] ⚠️⚠️⚠️ CANVAS 비활성화 감지! ⚠️⚠️⚠️");
                        Debug.LogError($"[CanvasMonitor] Canvas '{canvasNames[i]}' 상태 변화:");
                        Debug.LogError($"  ├─ {canvasStateChange}");
                        Debug.LogError($"  ├─ {gameObjectStateChange}");
                        Debug.LogError($"  ├─ 프레임: {Time.frameCount}, 시간: {Time.time:F2}초");
                        Debug.LogError($"  ├─ ⚠️ Canvas 비활성화 감지! 원인 분석 중...");
                        Debug.LogError($"  ├─ 📍 비활성화 시점 스택 트레이스:");
                        Debug.LogError(System.Environment.StackTrace);
                        
                        // Unity 내부 스택 정보도 출력
                        Debug.LogError($"  ├─ 🔍 더 많은 정보:");
                        Debug.LogError($"  ├─   Canvas.enabled: {allCanvases[i].enabled}");
                        Debug.LogError($"  ├─   GameObject.activeSelf: {allCanvases[i].gameObject.activeSelf}");
                        Debug.LogError($"  ├─   GameObject.activeInHierarchy: {allCanvases[i].gameObject.activeInHierarchy}");
                    }
                    else if (isDeactivation)
                    {
                        // 비활성화 상태 - 진짜 문제
                        Debug.LogError($"{prefix}[CanvasMonitor] Canvas '{canvasNames[i]}' 상태 변화:");
                        Debug.LogError($"  ├─ {canvasStateChange}");
                        Debug.LogError($"  ├─ {gameObjectStateChange}");
                        Debug.LogError($"  ├─ 프레임: {Time.frameCount}, 시간: {Time.time:F2}초");
                        Debug.LogError($"  ├─ ⚠️ Canvas 비활성화 감지! 원인 분석 중...");
                        Debug.LogError($"  ├─ 📍 스택 트레이스:");
                        Debug.LogError(System.Environment.StackTrace);
                    }
                    else
                    {
                        // 활성화는 복구 - LogWarning 사용  
                        Debug.LogWarning($"{prefix}[CanvasMonitor] Canvas '{canvasNames[i]}' 상태 변화:");
                        Debug.LogWarning($"  ├─ {canvasStateChange}");
                        Debug.LogWarning($"  ├─ {gameObjectStateChange}");
                        Debug.LogWarning($"  ├─ 프레임: {Time.frameCount}, 시간: {Time.time:F2}초");
                        Debug.LogWarning($"  ├─ ✅ Canvas 복구됨!");
                    }
                    
                    // CanvasGroup 상태도 확인
                    if (trackCanvasGroup)
                    {
                        CanvasGroup canvasGroup = allCanvases[i].GetComponent<CanvasGroup>();
                        if (canvasGroup != null)
                        {
                            if (isDeactivation)
                            {
                                Debug.LogError($"  ├─ CanvasGroup alpha: {canvasGroup.alpha}");
                                Debug.LogError($"  ├─ CanvasGroup interactable: {canvasGroup.interactable}");
                            }
                            else
                            {
                                Debug.LogWarning($"  ├─ CanvasGroup alpha: {canvasGroup.alpha}");
                                Debug.LogWarning($"  ├─ CanvasGroup interactable: {canvasGroup.interactable}");
                            }
                        }
                    }
                    
                    // MMTouchControls 컴포넌트 확인
                    Component mmTouchControls = null;
                    try 
                    {
                        // 동적으로 MMTouchControls 타입 검색
                        var mmTouchControlsType = System.Type.GetType("MoreMountains.Tools.MMTouchControls, Assembly-CSharp");
                        if (mmTouchControlsType != null)
                        {
                            mmTouchControls = allCanvases[i].GetComponent(mmTouchControlsType);
                        }
                    }
                    catch (System.Exception)
                    {
                        // MoreMountains 네임스페이스가 없는 경우 무시
                        mmTouchControls = null;
                    }
                    
                    if (mmTouchControls != null)
                    {
                        if (isDeactivation)
                        {
                            Debug.LogError($"  ├─ ⚠️ MMTouchControls 발견! 이것이 비활성화 원인일 수 있습니다!");
                        }
                        else
                        {
                            Debug.LogWarning($"  ├─ ✅ MMTouchControls 발견했지만 Canvas는 활성화 상태입니다.");
                        }
                        
                        // 리플렉션으로 프로퍼티 값 가져오기 (안전함)
                        try
                        {
                            var type = mmTouchControls.GetType();
                            var isMobileProp = type.GetProperty("IsMobile");
                            var forcedModeProp = type.GetProperty("ForcedMode");
                            var autoDetectionField = type.GetField("AutoMobileDetection");
                            
                            if (isMobileProp != null)
                            {
                                string logMsg = $"  ├─ MMTouchControls.IsMobile: {isMobileProp.GetValue(mmTouchControls)}";
                                if (isDeactivation) Debug.LogError(logMsg);
                                else Debug.LogWarning(logMsg);
                            }
                            if (forcedModeProp != null)
                            {
                                string logMsg = $"  ├─ MMTouchControls.ForcedMode: {forcedModeProp.GetValue(mmTouchControls)}";
                                if (isDeactivation) Debug.LogError(logMsg);
                                else Debug.LogWarning(logMsg);
                            }
                            if (autoDetectionField != null)
                            {
                                string logMsg = $"  ├─ MMTouchControls.AutoMobileDetection: {autoDetectionField.GetValue(mmTouchControls)}";
                                if (isDeactivation) Debug.LogError(logMsg);
                                else Debug.LogWarning(logMsg);
                            }
                        }
                        catch (System.Exception e)
                        {
                            string logMsg = $"  ├─ MMTouchControls 속성 읽기 실패: {e.Message}";
                            if (isDeactivation) Debug.LogError(logMsg);
                            else Debug.LogWarning(logMsg);
                        }
                    }
                      
                    // 스택 트레이스로 어디서 변경되었는지 추적
                    string stackMsg = $"  ├─ 📊 호출 스택:\n{System.Environment.StackTrace}";
                    if (isDeactivation) Debug.LogError(stackMsg);
                    else Debug.LogWarning(stackMsg);
                    
                    lastCanvasStates[i] = currentState;
                    lastGameObjectStates[i] = currentGameObjectState;
                    
                    // 비활성화된 경우 원인 추가 분석
                    if (!currentState || !currentGameObjectState)
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