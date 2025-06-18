using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System;
using System.Threading;

/// <summary>
/// Unity에서 TCP 서버를 실행하여 외부 MCP 서버에 로그와 게임 정보를 제공하는 컴포넌트
/// </summary>
public class UnityMCPServer : MonoBehaviour
{
    [Header("MCP 서버 설정")]
    public bool enableMCPServer = true;
    public int serverPort = 8081;
    public bool enableLogging = true;
    
    [Header("로그 필터")]
    public bool sendInfoLogs = true;
    public bool sendWarningLogs = true;
    public bool sendErrorLogs = true;
    
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private List<TcpClient> connectedClients = new List<TcpClient>();
    private Queue<string> logQueue = new Queue<string>();
    private bool isServerRunning = false;

    void Start()
    {
        if (enableMCPServer)
        {
            StartTCPServer();
            
            // Unity 콘솔 로그 이벤트 구독
            Application.logMessageReceived += OnLogMessageReceived;
            
            if (enableLogging)
                Debug.Log($"🎮 Unity MCP 서버가 포트 {serverPort}에서 시작되었습니다!");
        }
    }

    void StartTCPServer()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, serverPort);
            tcpListener.Start();
            isServerRunning = true;
            
            tcpListenerThread = new Thread(new ThreadStart(ListenForClients));
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Start();
            
            if (enableLogging)
                Debug.Log($"🔗 Unity TCP 서버가 포트 {serverPort}에서 시작되었습니다");
        }
        catch (Exception ex)
        {
            if (enableLogging)
                Debug.LogError($"❌ TCP 서버 시작 실패: {ex.Message}");
            isServerRunning = false;
        }
    }

    void ListenForClients()
    {
        while (isServerRunning)
        {
            try
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                connectedClients.Add(client);
                
                if (enableLogging)
                    Debug.Log($"🔗 새 클라이언트가 연결되었습니다. 총 {connectedClients.Count}개 연결");
                
                // 각 클라이언트를 별도 스레드에서 처리
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.IsBackground = true;
                clientThread.Start();
            }
            catch (ObjectDisposedException)
            {
                // 서버가 정상적으로 종료됨
                break;
            }
            catch (Exception ex)
            {
                if (isServerRunning && enableLogging)
                    Debug.LogError($"❌ 클라이언트 연결 오류: {ex.Message}");
            }
        }
    }

    void HandleClient(TcpClient client)
    {
        NetworkStream stream = null;

        try
        {
            stream = client.GetStream();
            
            // 연결 성공 메시지 전송
            SendToClient(stream, "UNITY_CONNECT|Unity MCP 서버에 연결되었습니다");

            byte[] bytes = new byte[1024];
            int bytesRead;

            while ((bytesRead = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                string request = Encoding.UTF8.GetString(bytes, 0, bytesRead);
                
                if (enableLogging)
                    Debug.Log($"📨 받은 요청: {request}");

                // 요청 처리
                if (request.Contains("clear_logs"))
                {
                    logQueue.Clear();
                    SendToClient(stream, "UNITY_RESPONSE|로그가 지워졌습니다");
                }
            }
        }
        catch (Exception ex)
        {
            if (enableLogging)
                Debug.LogWarning($"❌ 클라이언트 처리 오류: {ex.Message}");
        }
        finally
        {
            try
            {
                stream?.Close();
                client?.Close();
                connectedClients.Remove(client);
                
                if (enableLogging)
                    Debug.Log($"🔌 클라이언트 연결 해제. 남은 연결: {connectedClients.Count}개");
            }
            catch { }
        }
    }

    void OnLogMessageReceived(string logString, string stackTrace, LogType logType)
    {
        if (!isServerRunning) return;

        // 로그 타입별 필터링
        bool shouldSend = false;
        string level = "";

        switch (logType)
        {
            case LogType.Log:
                shouldSend = sendInfoLogs;
                level = "INFO";
                break;
            case LogType.Warning:
                shouldSend = sendWarningLogs;
                level = "WARNING";
                break;
            case LogType.Error:
            case LogType.Exception:
                shouldSend = sendErrorLogs;
                level = "ERROR";
                break;
            case LogType.Assert:
                shouldSend = sendErrorLogs;
                level = "ASSERT";
                break;
        }

        if (shouldSend)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string message = $"UNITY_LOG|{timestamp}|{level}|{logString}|{stackTrace}";
            
            logQueue.Enqueue(message);
        }
    }

    void Update()
    {
        // 로그 큐에서 메시지를 모든 연결된 클라이언트에게 전송
        if (isServerRunning && logQueue.Count > 0 && connectedClients.Count > 0)
        {
            while (logQueue.Count > 0)
            {
                string message = logQueue.Dequeue();
                BroadcastToAllClients(message);
            }
        }
    }

    void SendToClient(NetworkStream stream, string message)
    {
        if (stream == null) return;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }
        catch (Exception ex)
        {
            if (enableLogging)
                Debug.LogWarning($"❌ 클라이언트에게 메시지 전송 실패: {ex.Message}");
        }
    }

    void BroadcastToAllClients(string message)
    {
        var clientsToRemove = new List<TcpClient>();

        foreach (var client in connectedClients)
        {
            try
            {
                if (client.Connected)
                {
                    NetworkStream stream = client.GetStream();
                    SendToClient(stream, message);
                }
                else
                {
                    clientsToRemove.Add(client);
                }
            }
            catch
            {
                clientsToRemove.Add(client);
            }
        }

        // 연결이 끊어진 클라이언트 제거
        foreach (var client in clientsToRemove)
        {
            connectedClients.Remove(client);
            try { client.Close(); } catch { }
        }
    }

    /// <summary>
    /// 게임 상태 정보를 모든 클라이언트에게 전송
    /// </summary>
    public void SendGameState()
    {
        if (!isServerRunning) return;

        try
        {
            var gameState = new
            {
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                isHost = CoreDataManager.Instance?.isHost ?? false,
                characterCount = CoreDataManager.Instance?.characterDatabase?.currentRegisteredCharacters?.Length ?? 0,
                gameManagerExists = GameManager.Instance != null,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            string json = JsonUtility.ToJson(gameState);
            string message = $"UNITY_GAME_STATE|{DateTime.Now:HH:mm:ss}|{json}";
            BroadcastToAllClients(message);
        }
        catch (Exception ex)
        {
            if (enableLogging)
                Debug.LogWarning($"❌ 게임 상태 전송 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 캐릭터 정보를 모든 클라이언트에게 전송
    /// </summary>
    public void SendCharacterInfo()
    {
        if (!isServerRunning) return;

        try
        {
            var characters = new List<object>();
            
            if (CoreDataManager.Instance?.characterDatabase?.currentRegisteredCharacters != null)
            {
                foreach (var character in CoreDataManager.Instance.characterDatabase.currentRegisteredCharacters)
                {
                    if (character != null)
                    {
                        characters.Add(new
                        {
                            name = character.characterName,
                            race = character.race.ToString(),
                            star = character.star.ToString(),
                            attack = character.attackPower,
                            hp = character.health,
                            cost = character.cost,
                            attackSpeed = character.attackSpeed,
                            range = character.attackRange,
                            moveSpeed = character.moveSpeed
                        });
                    }
                }
            }

            var characterData = new
            {
                characters = characters.ToArray(),
                count = characters.Count,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            string json = JsonUtility.ToJson(characterData);
            string message = $"UNITY_CHARACTERS|{DateTime.Now:HH:mm:ss}|{json}";
            BroadcastToAllClients(message);
        }
        catch (Exception ex)
        {
            if (enableLogging)
                Debug.LogWarning($"❌ 캐릭터 정보 전송 실패: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        Application.logMessageReceived -= OnLogMessageReceived;
        
        // 서버 종료
        isServerRunning = false;
        
        // 모든 클라이언트에게 종료 메시지 전송
        if (connectedClients.Count > 0)
        {
            BroadcastToAllClients("UNITY_DISCONNECT|Unity MCP 서버가 종료됩니다");
        }
        
        // 모든 클라이언트 연결 해제
        foreach (var client in connectedClients)
        {
            try { client.Close(); } catch { }
        }
        connectedClients.Clear();
        
        // TCP 리스너 종료
        try
        {
            tcpListener?.Stop();
            tcpListenerThread?.Join(1000);
        }
        catch { }
        
        if (enableLogging)
            Debug.Log("🛑 Unity MCP 서버가 종료되었습니다");
    }

    void OnApplicationQuit()
    {
        OnDestroy();
    }

    // 에디터에서 테스트용 메서드들
    [ContextMenu("게임 상태 전송")]
    public void TestSendGameState()
    {
        SendGameState();
    }

    [ContextMenu("캐릭터 정보 전송")]
    public void TestSendCharacterInfo()
    {
        SendCharacterInfo();
    }

    [ContextMenu("테스트 로그 전송")]
    public void TestSendLog()
    {
        Debug.Log("🧪 테스트 Info 로그");
        Debug.LogWarning("⚠️ 테스트 Warning 로그");
        Debug.LogError("❌ 테스트 Error 로그");
    }

    [ContextMenu("서버 재시작")]
    public void RestartServer()
    {
        OnDestroy();
        if (enableMCPServer)
        {
            StartTCPServer();
        }
    }
} 