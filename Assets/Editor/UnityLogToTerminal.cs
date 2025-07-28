using System;
using System.IO;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class UnityLogToTerminal
{
    private static StreamWriter logWriter;
    private static string logFilePath;
    private static readonly object lockObject = new object();

    static UnityLogToTerminal()
    {
        // LogToTerminal 기능 완전 비활성화
        Debug.Log("[LogToTerminal] 기능이 비활성화되었습니다.");
        return;
        
        // 아래 코드는 실행되지 않음
        /*
        // Play Mode에서는 실행하지 않음
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }
        
        // 환경 변수로 로깅 활성화 여부 확인
        if (System.Environment.GetEnvironmentVariable("UNITY_ENABLE_LOG_TO_TERMINAL") != "1")
        {
            Debug.Log("[LogToTerminal] 로그 파일 기능이 비활성화되어 있습니다. UNITY_ENABLE_LOG_TO_TERMINAL=1로 설정하여 활성화할 수 있습니다.");
            return;
        }
        
        InitializeLogging();
        
        // logWriter가 성공적으로 초기화된 경우에만 이벤트 등록
        if (logWriter != null)
        {
            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.quitting += OnEditorQuitting;
        }
        */
    }

    private static void InitializeLogging()
    {
        try
        {
            string logDirectory = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // 고유한 파일명 생성 (시간 기반)
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            logFilePath = Path.Combine(logDirectory, $"unity-console_{timestamp}.log");
            
            // 기존 파일과 충돌 방지를 위해 고유한 이름 사용
            int counter = 0;
            string baseFilePath = logFilePath;
            while (File.Exists(logFilePath))
            {
                counter++;
                logFilePath = baseFilePath.Replace(".log", $"_{counter}.log");
            }

            // 안전한 파일 생성
            logWriter = new StreamWriter(logFilePath, false, System.Text.Encoding.UTF8)
            {
                AutoFlush = true
            };

            string startMessage = $"=== Unity Log Session Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===";
            logWriter.WriteLine(startMessage);
            
            // Console 대신 Debug.Log만 사용
            Debug.Log("[LogToTerminal] 로그 파일이 생성되었습니다: " + logFilePath);
            
            // 오래된 로그 파일들 정리 (7일 이상된 파일들)
            CleanupOldLogFiles(logDirectory);
        }
        catch (Exception ex)
        {
            // 로그 초기화가 실패해도 Unity는 계속 작동해야 함
            Debug.Log($"[LogToTerminal] 로그 파일 초기화 건너뜀: {ex.Message}");
            logWriter = null; // null로 설정하여 로깅 비활성화
        }
    }
    
    private static void CleanupOldLogFiles(string logDirectory)
    {
        try
        {
            string[] logFiles = Directory.GetFiles(logDirectory, "unity-console_*.log");
            DateTime cutoffDate = DateTime.Now.AddDays(-7);
            
            foreach (string file in logFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"[LogToTerminal] 오래된 로그 파일 정리 실패: {ex.Message}");
        }
    }

    private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
    {
        if (logWriter == null) return;

        lock (lockObject)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string logLevel = GetLogLevel(type);
                string formattedMessage = $"[{timestamp}] {logLevel}: {logString}";

                logWriter.WriteLine(formattedMessage);

                // 스택 트레이스가 있고 에러/예외인 경우 추가
                if (!string.IsNullOrEmpty(stackTrace) && (type == LogType.Error || type == LogType.Exception))
                {
                    string[] stackLines = stackTrace.Split('\n');
                    foreach (string line in stackLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            logWriter.WriteLine($"[{timestamp}] STACK: {line.Trim()}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[LogToTerminal] 로그 쓰기 실패: " + ex.Message);
            }
        }
    }

    private static string GetLogLevel(LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                return "ERROR";
            case LogType.Assert:
                return "ASSERT";
            case LogType.Warning:
                return "WARNING";
            case LogType.Log:
                return "INFO";
            case LogType.Exception:
                return "EXCEPTION";
            default:
                return "LOG";
        }
    }

    private static void OnEditorQuitting()
    {
        if (logWriter != null)
        {
            lock (lockObject)
            {
                try
                {
                    string endMessage = $"=== Unity Log Session Ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===";
                    logWriter.WriteLine(endMessage);
                    logWriter.Close();
                    logWriter.Dispose();
                    logWriter = null;
                }
                catch (Exception ex)
                {
                    Debug.LogError("[LogToTerminal] 로그 파일 종료 실패: " + ex.Message);
                }
            }
        }
    }

    [MenuItem("Unity Log/로그 파일 열기")]
    private static void OpenLogFile()
    {
        if (File.Exists(logFilePath))
        {
            System.Diagnostics.Process.Start(logFilePath);
        }
        else
        {
            Debug.LogWarning("로그 파일이 존재하지 않습니다: " + logFilePath);
        }
    }

    [MenuItem("Unity Log/로그 파일 경로 복사")]
    private static void CopyLogPath()
    {
        if (!string.IsNullOrEmpty(logFilePath))
        {
            EditorGUIUtility.systemCopyBuffer = logFilePath;
            Debug.Log("로그 파일 경로가 클립보드에 복사되었습니다: " + logFilePath);
        }
    }

    [MenuItem("Unity Log/모니터링 명령어 생성")]
    private static void GenerateMonitoringCommand()
    {
        string command = $"PowerShell -ExecutionPolicy Bypass -File \"{Application.dataPath}\\..\\Scripts\\Watch-UnityLogs.ps1\"";
        EditorGUIUtility.systemCopyBuffer = command;
        Debug.Log("PowerShell 모니터링 명령어가 클립보드에 복사되었습니다:\n" + command);
    }
}
