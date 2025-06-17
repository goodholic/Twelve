using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UnityMCPServer : MonoBehaviour
{
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private bool isListening = false;

    void Start()
    {
        // MCP 서버 시작
        StartServer();
        Debug.Log("✅ Unity MCP 서버가 시작되었습니다!");
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
                using (TcpClient client = tcpListener.AcceptTcpClient())
                {
                    // 명령 수신
                    NetworkStream stream = client.GetStream();
                    byte[] message = new byte[1024];
                    int bytesRead = stream.Read(message, 0, 1024);

                    string command = Encoding.UTF8.GetString(message, 0, bytesRead);
                    Debug.Log($"📨 받은 명령: {command}");

                    // 응답 전송
                    string response = "OK";
                    byte[] data = Encoding.UTF8.GetBytes(response);
                    stream.Write(data, 0, data.Length);
                }
            }
            catch (SocketException)
            {
                // 정상 종료
            }
        }
    }

    void OnDestroy()
    {
        isListening = false;
        tcpListener?.Stop();
        tcpListenerThread?.Join();
    }
}