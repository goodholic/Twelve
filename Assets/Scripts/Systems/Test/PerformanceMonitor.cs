using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GuildMaster.Core
{
    /// <summary>
    /// 게임 성능을 실시간으로 모니터링하는 성능 분석 시스템
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        private static PerformanceMonitor _instance;
        public static PerformanceMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PerformanceMonitor");
                    _instance = go.AddComponent<PerformanceMonitor>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        [Header("Settings")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private bool showOnScreenDisplay = false;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private int historySize = 60;
        
        [Header("Warning Thresholds")]
        [SerializeField] private float fpsWarningThreshold = 30f;
        [SerializeField] private long memoryWarningThreshold = 100 * 1024 * 1024; // 100MB
        [SerializeField] private float frameTimeWarningThreshold = 33.33f; // 30fps
        
        // Performance Metrics
        private PerformanceData currentData = new PerformanceData();
        private Queue<PerformanceData> performanceHistory = new Queue<PerformanceData>();
        
        // FPS Calculation
        private float fpsTimer;
        private int frameCount;
        private float currentFPS;
        
        // Frame Time Tracking
        private float lastFrameTime;
        private float maxFrameTime;
        private float minFrameTime = float.MaxValue;
        
        // Memory Tracking
        private long lastGCMemory;
        private int gcCount;
        
        // Events
        public event Action<PerformanceData> OnPerformanceUpdated;
        public event Action<PerformanceWarning> OnPerformanceWarning;
        
        // GUI Display
        private GUIStyle textStyle;
        private bool showDetailedStats = false;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeMonitoring();
        }
        
        void InitializeMonitoring()
        {
            lastFrameTime = Time.realtimeSinceStartup;
            lastGCMemory = GC.GetTotalMemory(false);
            
            // GUI 스타일 설정
            textStyle = new GUIStyle();
            textStyle.fontSize = 12;
            textStyle.normal.textColor = Color.white;
        }
        
        void Update()
        {
            if (!enableMonitoring) return;
            
            UpdateFPS();
            UpdateFrameTime();
            UpdateMemoryUsage();
            
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= updateInterval)
            {
                CollectPerformanceData();
                CheckPerformanceWarnings();
                fpsTimer = 0f;
                frameCount = 0;
                maxFrameTime = 0f;
                minFrameTime = float.MaxValue;
            }
            
            // 키보드 단축키 처리
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showOnScreenDisplay = !showOnScreenDisplay;
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                showDetailedStats = !showDetailedStats;
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                ForceGarbageCollection();
            }
        }
        
        void UpdateFPS()
        {
            frameCount++;
            if (fpsTimer > 0)
            {
                currentFPS = frameCount / fpsTimer;
            }
        }
        
        void UpdateFrameTime()
        {
            float currentTime = Time.realtimeSinceStartup;
            float frameTime = (currentTime - lastFrameTime) * 1000f; // milliseconds
            
            maxFrameTime = Mathf.Max(maxFrameTime, frameTime);
            minFrameTime = Mathf.Min(minFrameTime, frameTime);
            
            lastFrameTime = currentTime;
        }
        
        void UpdateMemoryUsage()
        {
            long currentMemory = GC.GetTotalMemory(false);
            if (currentMemory < lastGCMemory)
            {
                gcCount++;
            }
            lastGCMemory = currentMemory;
        }
        
        void CollectPerformanceData()
        {
            currentData = new PerformanceData
            {
                timestamp = Time.realtimeSinceStartup,
                fps = currentFPS,
                frameTimeMs = Time.deltaTime * 1000f,
                maxFrameTimeMs = maxFrameTime,
                minFrameTimeMs = minFrameTime == float.MaxValue ? 0 : minFrameTime,
                
                // Memory
                totalMemoryMB = GC.GetTotalMemory(false) / (1024f * 1024f),
                unityReservedMemoryMB = Profiler.GetTotalReservedMemory() / (1024f * 1024f),
                unityUsedMemoryMB = Profiler.GetTotalAllocatedMemory() / (1024f * 1024f),
                gcCount = gcCount,
                
                // System
                batteryLevel = SystemInfo.batteryLevel,
                thermalState = "Normal", // SystemInfo.thermalState is iOS only
                
                // Rendering - Using simplified values since DebugUI is not available in standard Unity
                drawCalls = 0,
                triangles = 0,
                vertices = 0
            };
            
            // Add to history
            performanceHistory.Enqueue(currentData);
            while (performanceHistory.Count > historySize)
            {
                performanceHistory.Dequeue();
            }
            
            OnPerformanceUpdated?.Invoke(currentData);
        }
        
        void CheckPerformanceWarnings()
        {
            var warnings = new List<PerformanceWarning>();
            
            // FPS Warning
            if (currentData.fps < fpsWarningThreshold)
            {
                warnings.Add(new PerformanceWarning
                {
                    type = WarningType.LowFPS,
                    message = $"Low FPS detected: {currentData.fps:F1}",
                    severity = GetSeverity(currentData.fps, fpsWarningThreshold, 15f),
                    value = currentData.fps
                });
            }
            
            // Frame Time Warning
            if (currentData.maxFrameTimeMs > frameTimeWarningThreshold)
            {
                warnings.Add(new PerformanceWarning
                {
                    type = WarningType.HighFrameTime,
                    message = $"High frame time: {currentData.maxFrameTimeMs:F1}ms",
                    severity = GetSeverity(currentData.maxFrameTimeMs, frameTimeWarningThreshold, 100f),
                    value = currentData.maxFrameTimeMs
                });
            }
            
            // Memory Warning
            if (currentData.totalMemoryMB * 1024 * 1024 > memoryWarningThreshold)
            {
                warnings.Add(new PerformanceWarning
                {
                    type = WarningType.HighMemoryUsage,
                    message = $"High memory usage: {currentData.totalMemoryMB:F1}MB",
                    severity = GetSeverity(currentData.totalMemoryMB, memoryWarningThreshold / (1024f * 1024f), 500f),
                    value = currentData.totalMemoryMB
                });
            }
            
            // Battery Warning (모바일 전용)
            #if UNITY_MOBILE
            if (currentData.batteryLevel < 0.2f && currentData.batteryLevel > 0)
            {
                warnings.Add(new PerformanceWarning
                {
                    type = WarningType.LowBattery,
                    message = $"Low battery: {currentData.batteryLevel * 100:F0}%",
                    severity = WarningSeverity.Warning,
                    value = currentData.batteryLevel
                });
            }
            #endif
            
            // 경고 발생
            foreach (var warning in warnings)
            {
                OnPerformanceWarning?.Invoke(warning);
                if (enableMonitoring)
                {
                    Debug.LogWarning($"[Performance] {warning.message}");
                }
            }
        }
        
        WarningSeverity GetSeverity(float value, float warningThreshold, float criticalThreshold)
        {
            if (value >= criticalThreshold)
                return WarningSeverity.Critical;
            else if (value >= warningThreshold)
                return WarningSeverity.Warning;
            else
                return WarningSeverity.Info;
        }
        
        /// <summary>
        /// 성능 통계 가져오기
        /// </summary>
        public PerformanceStats GetPerformanceStats()
        {
            if (performanceHistory.Count == 0)
                return new PerformanceStats();
            
            var history = new List<PerformanceData>(performanceHistory);
            
            return new PerformanceStats
            {
                averageFPS = CalculateAverage(history, d => d.fps),
                minimumFPS = CalculateMinimum(history, d => d.fps),
                maximumFPS = CalculateMaximum(history, d => d.fps),
                
                averageFrameTime = CalculateAverage(history, d => d.frameTimeMs),
                maximumFrameTime = CalculateMaximum(history, d => d.maxFrameTimeMs),
                
                averageMemoryUsage = CalculateAverage(history, d => d.totalMemoryMB),
                maximumMemoryUsage = CalculateMaximum(history, d => d.totalMemoryMB),
                
                totalGCCount = history[history.Count - 1].gcCount,
                dataPoints = history.Count
            };
        }
        
        float CalculateAverage(List<PerformanceData> data, Func<PerformanceData, float> selector)
        {
            float sum = 0;
            foreach (var item in data)
                sum += selector(item);
            return sum / data.Count;
        }
        
        float CalculateMinimum(List<PerformanceData> data, Func<PerformanceData, float> selector)
        {
            float min = float.MaxValue;
            foreach (var item in data)
                min = Mathf.Min(min, selector(item));
            return min;
        }
        
        float CalculateMaximum(List<PerformanceData> data, Func<PerformanceData, float> selector)
        {
            float max = float.MinValue;
            foreach (var item in data)
                max = Mathf.Max(max, selector(item));
            return max;
        }
        
        /// <summary>
        /// 성능 데이터 리셋
        /// </summary>
        public void ResetPerformanceData()
        {
            performanceHistory.Clear();
            gcCount = 0;
            maxFrameTime = 0;
            minFrameTime = float.MaxValue;
        }
        
        /// <summary>
        /// 메모리 강제 정리
        /// </summary>
        public void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            Debug.Log("[Performance] Forced garbage collection completed");
        }
        
        void OnGUI()
        {
            if (!showOnScreenDisplay || !enableMonitoring) return;
            
            const int padding = 10;
            const int lineHeight = 20;
            int yOffset = padding;
            
            // 기본 정보
            GUI.Label(new Rect(padding, yOffset, 300, lineHeight), 
                     $"FPS: {currentData.fps:F1}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(padding, yOffset, 300, lineHeight), 
                     $"Frame: {currentData.frameTimeMs:F1}ms", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(padding, yOffset, 300, lineHeight), 
                     $"Memory: {currentData.totalMemoryMB:F1}MB", textStyle);
            yOffset += lineHeight;
            
            // 토글 버튼
            if (GUI.Button(new Rect(padding, yOffset, 100, 25), showDetailedStats ? "Hide Details" : "Show Details"))
            {
                showDetailedStats = !showDetailedStats;
            }
            yOffset += 30;
            
            // 상세 정보
            if (showDetailedStats)
            {
                GUI.Label(new Rect(padding, yOffset, 300, lineHeight), 
                         $"Max Frame: {currentData.maxFrameTimeMs:F1}ms", textStyle);
                yOffset += lineHeight;
                
                GUI.Label(new Rect(padding, yOffset, 300, lineHeight), 
                         $"Min Frame: {currentData.minFrameTimeMs:F1}ms", textStyle);
                yOffset += lineHeight;
                
                GUI.Label(new Rect(padding, yOffset, 300, lineHeight), 
                         $"Unity Memory: {currentData.unityUsedMemoryMB:F1}MB", textStyle);
                yOffset += lineHeight;
                
                GUI.Label(new Rect(padding, yOffset, 300, lineHeight), 
                         $"GC Count: {currentData.gcCount}", textStyle);
                yOffset += lineHeight;
                
                #if UNITY_MOBILE
                GUI.Label(new Rect(padding, yOffset, 300, lineHeight), 
                         $"Battery: {currentData.batteryLevel * 100:F0}%", textStyle);
                yOffset += lineHeight;
                
                GUI.Label(new Rect(padding, yOffset, 300, lineHeight), 
                         $"Thermal: {currentData.thermalState}", textStyle);
                yOffset += lineHeight;
                #endif
                
                if (GUI.Button(new Rect(padding, yOffset, 120, 25), "Force GC"))
                {
                    ForceGarbageCollection();
                }
            }
        }
        
        /// <summary>
        /// 성능 데이터 구조체
        /// </summary>
        [System.Serializable]
        public struct PerformanceData
        {
            public float timestamp;
            public float fps;
            public float frameTimeMs;
            public float maxFrameTimeMs;
            public float minFrameTimeMs;
            public float totalMemoryMB;
            public float unityReservedMemoryMB;
            public float unityUsedMemoryMB;
            public int gcCount;
            public float batteryLevel;
            public string thermalState;
            public int drawCalls;
            public int triangles;
            public int vertices;
        }
        
        /// <summary>
        /// 성능 통계 구조체
        /// </summary>
        [System.Serializable]
        public struct PerformanceStats
        {
            public float averageFPS;
            public float minimumFPS;
            public float maximumFPS;
            public float averageFrameTime;
            public float maximumFrameTime;
            public float averageMemoryUsage;
            public float maximumMemoryUsage;
            public int totalGCCount;
            public int dataPoints;
        }
        
        /// <summary>
        /// 성능 경고 클래스
        /// </summary>
        public class PerformanceWarning
        {
            public WarningType type;
            public string message;
            public WarningSeverity severity;
            public float value;
        }
        
        public enum WarningType
        {
            LowFPS,
            HighFrameTime,
            HighMemoryUsage,
            LowBattery,
            ThermalThrottling
        }
        
        public enum WarningSeverity
        {
            Info,
            Warning,
            Critical
        }
    }
} 