#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

/// <summary>
/// Unity 콘솔 로그를 터미널에서 읽을 수 있는 파일로 출력하는 스크립트
/// </summary>
[InitializeOnLoad]
public class UnityLogToTerminal
{
    private static string logFilePath;
    private static StreamWriter logWriter;
    
    static UnityLogToTerminal()
    {
        // 로그 파일 경로 설정
        string projectPath = Application.dataPath.Replace("/Assets", "");
        logFilePath = Path.Combine(projectPath, "Logs", "unity-console.log");
        
        // 로그 디렉토리 생성
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
        
        // 로그 파일 초기화
        InitializeLogFile();
        
        // Unity 콘솔 로그 캡처 시작
        Application.logMessageReceived += OnLogMessageReceived;
        
        Debug.Log("🔍 Unity 로그가 터미널용 파일로 출력됩니다: " + logFilePath);
    }
    
    private static void InitializeLogFile()
    {
        try
        {
            // 기존 로그 파일 백업
            if (File.Exists(logFilePath))
            {
                string backupPath = logFilePath + ".backup";
                File.Copy(logFilePath, backupPath, true);
            }
            
            // 새 로그 파일 생성
            logWriter = new StreamWriter(logFilePath, false, System.Text.Encoding.UTF8);
            logWriter.AutoFlush = true;
            
            // 헤더 작성
            logWriter.WriteLine($"# Unity Console Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logWriter.WriteLine($"# Project: {Application.productName}");
            logWriter.WriteLine($"# Unity Version: {Application.unityVersion}");
            logWriter.WriteLine("# Format: [TIMESTAMP] LEVEL: MESSAGE");
            logWriter.WriteLine("");
        }
        catch (Exception e)
        {
            Debug.LogError($"로그 파일 초기화 실패: {e.Message}");
        }
    }
    
    private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
    {
        if (logWriter == null) return;
        
        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logLevel = GetLogLevelString(type);
            
            // 기본 로그 라인
            string logLine = $"[{timestamp}] {logLevel}: {logString}";
            logWriter.WriteLine(logLine);
            
            // 에러나 예외인 경우 스택 트레이스 추가
            if ((type == LogType.Error || type == LogType.Exception) && !string.IsNullOrEmpty(stackTrace))
            {
                logWriter.WriteLine($"[{timestamp}] STACK: {stackTrace}");
            }
            
            // 터미널에서 읽기 쉽도록 구분선 추가 (에러/경고만)
            if (type == LogType.Error || type == LogType.Warning)
            {
                logWriter.WriteLine("---");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"로그 파일 쓰기 실패: {e.Message}");
        }
    }
    
    private static string GetLogLevelString(LogType type)
    {
        switch (type)
        {
            case LogType.Error: return "ERROR";
            case LogType.Assert: return "ASSERT";
            case LogType.Warning: return "WARNING";
            case LogType.Log: return "INFO";
            case LogType.Exception: return "EXCEPTION";
            default: return "INFO";
        }
    }
    
    /// <summary>
    /// Unity 에디터 종료 시 로그 파일 정리
    /// </summary>
    [MenuItem("Unity Log/로그 파일 열기", false, 1)]
    public static void OpenLogFile()
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
    
    [MenuItem("Unity Log/로그 파일 경로 복사", false, 2)]
    public static void CopyLogFilePath()
    {
        EditorGUIUtility.systemCopyBuffer = logFilePath;
        Debug.Log("로그 파일 경로가 클립보드에 복사되었습니다: " + logFilePath);
    }
    
    [MenuItem("Unity Log/터미널 모니터링 명령어 생성", false, 3)]
    public static void GenerateTerminalCommand()
    {
        string command = $"Get-Content -Path \"{logFilePath}\" -Wait -Tail 10";
        EditorGUIUtility.systemCopyBuffer = command;
        
        Debug.Log("터미널 모니터링 명령어가 클립보드에 복사되었습니다:");
        Debug.Log(command);
        Debug.Log("\n사용법: PowerShell에서 위 명령어를 실행하면 Unity 로그를 실시간으로 볼 수 있습니다.");
    }
    
    // Unity 에디터 종료 시 정리
    static void OnApplicationQuit()
    {
        if (logWriter != null)
        {
            logWriter.WriteLine($"# Unity Editor Closed - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logWriter.Close();
            logWriter = null;
        }
    }
}
#endif 