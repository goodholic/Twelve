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
        InitializeLogging();
        Application.logMessageReceived += OnLogMessageReceived;
        EditorApplication.quitting += OnEditorQuitting;
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

            logFilePath = Path.Combine(logDirectory, "unity-console.log");
            
            // 기존 로그 파일이 있으면 백업
            if (File.Exists(logFilePath))
            {
                string backupPath = logFilePath + ".backup." + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Move(logFilePath, backupPath);
            }

            logWriter = new StreamWriter(logFilePath, false)
            {
                AutoFlush = true // 실시간 업데이트를 위해
            };

            string startMessage = $"=== Unity Log Session Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===";
            logWriter.WriteLine(startMessage);
            Debug.Log("[LogToTerminal] 로그 파일이 생성되었습니다: " + logFilePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[LogToTerminal] 로그 파일 초기화 실패: " + ex.Message);
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
