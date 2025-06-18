using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.IO;

public class UnityMCPServer : MonoBehaviour
{
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private bool isListening = false;
    private ConcurrentQueue<LogMessage> logQueue = new ConcurrentQueue<LogMessage>();
    private List<TcpClient> connectedClients = new List<TcpClient>();
    
    [System.Serializable]
    public class LogMessage
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public System.DateTime timestamp;
        
        public LogMessage(string msg, string stack, LogType logType)
        {
            message = msg;
            stackTrace = stack;
            type = logType;
            timestamp = System.DateTime.Now;
        }
    }

    void Start()
    {
        // Unity 콘솔 로그 캡처 시작
        Application.logMessageReceived += OnLogMessageReceived;
        
        // MCP 서버 시작
        StartServer();
        Debug.Log("✅ Unity MCP 서버가 시작되었습니다!");
    }

    void OnLogMessageReceived(string logString, string stackTrace, LogType type)
    {
        // 로그 메시지를 큐에 추가
        LogMessage logMsg = new LogMessage(logString, stackTrace, type);
        logQueue.Enqueue(logMsg);
        
        // 연결된 클라이언트들에게 로그 전송
        BroadcastLogToClients(logMsg);
    }

    void StartServer()
    {
        tcpListenerThread = new Thread(new ThreadStart(ListenForClients));
        tcpListenerThread.Start();
    }

    void ListenForClients()
    {
        tcpListener = new TcpListener(IPAddress.Any, 8080);
        tcpListener.Start();
        isListening = true;

        while (isListening)
        {
            try
            {
                // 클라이언트 연결 대기
                TcpClient client = tcpListener.AcceptTcpClient();
                lock (connectedClients)
                {
                    connectedClients.Add(client);
                }
                
                // 클라이언트 처리를 별도 스레드에서 실행
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
            catch (SocketException)
            {
                // 정상 종료
            }
        }
    }

    void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        
        try
        {
            while (client.Connected && isListening)
            {
                if (stream.DataAvailable)
                {
                    byte[] message = new byte[1024];
                    int bytesRead = stream.Read(message, 0, 1024);
                    string command = Encoding.UTF8.GetString(message, 0, bytesRead);
                    
                    Debug.Log($"📨 받은 명령: {command}");
                    
                    // 명령 처리
                    string response = ProcessCommand(command);
                    byte[] data = Encoding.UTF8.GetBytes(response);
                    stream.Write(data, 0, data.Length);
                }
                
                Thread.Sleep(10); // CPU 사용률 줄이기
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"클라이언트 연결 오류: {e.Message}");
        }
        finally
        {
            lock (connectedClients)
            {
                connectedClients.Remove(client);
            }
            client.Close();
        }
    }

    string ProcessCommand(string command)
    {
        switch (command.Trim().ToLower())
        {
            case "get_logs":
                return GetRecentLogs();
            case "clear_logs":
                ClearLogQueue();
                return "로그가 지워졌습니다.";
            case "ping":
                return "pong";
            default:
                return "알 수 없는 명령입니다.";
        }
    }

    string GetRecentLogs()
    {
        List<LogMessage> logs = new List<LogMessage>();
        while (logQueue.TryDequeue(out LogMessage log))
        {
            logs.Add(log);
        }
        
        if (logs.Count == 0)
        {
            return "새로운 로그가 없습니다.";
        }
        
        StringBuilder sb = new StringBuilder();
        foreach (var log in logs)
        {
            string logLevel = GetLogLevelString(log.type);
            sb.AppendLine($"[{log.timestamp:HH:mm:ss}] {logLevel}: {log.message}");
            
            if (!string.IsNullOrEmpty(log.stackTrace) && log.type == LogType.Error)
            {
                sb.AppendLine($"스택 트레이스: {log.stackTrace}");
            }
        }
        
        return sb.ToString();
    }

    void BroadcastLogToClients(LogMessage logMsg)
    {
        string logData = FormatLogForTransmission(logMsg);
        byte[] data = Encoding.UTF8.GetBytes(logData);
        
        lock (connectedClients)
        {
            List<TcpClient> disconnectedClients = new List<TcpClient>();
            
            foreach (var client in connectedClients)
            {
                try
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                    else
                    {
                        disconnectedClients.Add(client);
                    }
                }
                catch
                {
                    disconnectedClients.Add(client);
                }
            }
            
            // 연결이 끊어진 클라이언트 제거
            foreach (var client in disconnectedClients)
            {
                connectedClients.Remove(client);
                client.Close();
            }
        }
    }

    string FormatLogForTransmission(LogMessage logMsg)
    {
        string logLevel = GetLogLevelString(logMsg.type);
        string formattedLog = $"UNITY_LOG|{logMsg.timestamp:yyyy-MM-dd HH:mm:ss}|{logLevel}|{logMsg.message}";
        
        if (!string.IsNullOrEmpty(logMsg.stackTrace) && logMsg.type == LogType.Error)
        {
            formattedLog += $"|{logMsg.stackTrace}";
        }
        
        return formattedLog + "\n";
    }

    string GetLogLevelString(LogType type)
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

    void ClearLogQueue()
    {
        while (logQueue.TryDequeue(out _)) { }
    }

    void OnDestroy()
    {
        // 콘솔 로그 캡처 중단
        Application.logMessageReceived -= OnLogMessageReceived;
        
        isListening = false;
        
        lock (connectedClients)
        {
            foreach (var client in connectedClients)
            {
                client.Close();
            }
            connectedClients.Clear();
        }
        
        tcpListener?.Stop();
        tcpListenerThread?.Join();
    }
}