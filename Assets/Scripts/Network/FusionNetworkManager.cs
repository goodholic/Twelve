using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq; // Count() 확장 메서드를 위해 추가
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Fusion 네트워크 연결/세션 관리를 담당.
/// 1 vs. 1 연결(최대 2명)이 가능하도록 수정.
/// </summary>
public class FusionNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("네트워크 최대 플레이어 수 (1 vs. 1 ⇒ 2로 설정)")]
    [SerializeField] private int maxPlayers = 2;

    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _sceneManager;

    /// <summary>
    /// 호스트(Host)로 방 생성 (혹은 서버 역할).
    /// </summary>
    public async void StartGame(GameMode mode)
    {
        // NetworkRunner 생성
        GameObject runnerObj = new GameObject("NetworkRunner");
        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true; // 호스트/클라 모두 입력 제공 가능

        // 씬 매니저
        GameObject sceneManagerObj = new GameObject("NetworkSceneManager");
        _sceneManager = sceneManagerObj.AddComponent<NetworkSceneManagerDefault>();

        // 콜백 등록 (INetworkRunnerCallbacks 구현)
        _runner.AddCallbacks(this);

        // 1 vs. 1 ⇒ 최대 플레이어 수 설정
        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            PlayerCount = maxPlayers,   // PlayerLimit 대신 PlayerCount 사용
            Scene = null,              // 현재 씬 그대로 사용
            SceneManager = _sceneManager
        };

        // 호스트로 StartGame
        var result = await _runner.StartGame(startGameArgs);
        if (!result.Ok)
        {
            Debug.LogError($"[FusionNetworkManager] StartGame failed: {result.ShutdownReason}");
        }
        else
        {
            Debug.Log($"[FusionNetworkManager] Fusion {mode} started successfully!");
        }
    }

    /// <summary>
    /// 클라이언트 모드로 Join (이미 호스트가 열어둔 같은 SessionName으로 접속)
    /// </summary>
    public async void JoinGame()
    {
        // 클라이언트 러너 생성
        GameObject runnerObj = new GameObject("NetworkRunner");
        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        GameObject sceneManagerObj = new GameObject("NetworkSceneManager");
        _sceneManager = sceneManagerObj.AddComponent<NetworkSceneManagerDefault>();

        // 콜백 등록
        _runner.AddCallbacks(this);

        // 1 vs. 1 ⇒ 최대 플레이어 수 설정
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = "TestRoom",
            PlayerCount = maxPlayers,   // PlayerLimit 대신 PlayerCount 사용
            Scene = null,
            SceneManager = _sceneManager
        };

        var result = await _runner.StartGame(startGameArgs);
        if (!result.Ok)
        {
            Debug.LogError($"[FusionNetworkManager] JoinGame failed: {result.ShutdownReason}");
        }
        else
        {
            Debug.Log("[FusionNetworkManager] Joined room as client.");
        }
    }

    // ----------------------------------------------------------------------------
    //  ↓ INetworkRunnerCallbacks 인터페이스 구현부
    //     (원하는 콜백만 활용 가능. 여기서는 간단히 1~2개만 처리 예시.)
    // ----------------------------------------------------------------------------

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[FusionNetworkManager] OnPlayerJoined -> player={player.PlayerId}");

        // 2명(Host/Client) 모두 들어오면 본격적으로 게임 시작 가능
        // 예: 2명 모였는지 확인
        if (runner.ActivePlayers.Count() == maxPlayers)  // Count 대신 Count() 확장 메서드 사용
        {
            Debug.Log("[FusionNetworkManager] 2명 모두 접속완료 -> 게임 시작 준비");
            // TODO: 필요한 초기화나 씬 로딩, 스폰 등을 여기서 진행
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[FusionNetworkManager] OnPlayerLeft -> player={player.PlayerId}");
        // 1 vs. 1에서 한 명이 나가면 곧바로 게임 종료 처리 가능
    }

    // Unity 메시지 시그니처 수정 (INetworkRunnerCallbacks 구현)
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[FusionNetworkManager] OnConnectedToServer");
    }

    // Unity 메시지 시그니처 수정 (INetworkRunnerCallbacks 구현)
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[FusionNetworkManager] OnDisconnectedFromServer: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // 연결 요청을 처리하는 코드
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[FusionNetworkManager] OnShutdown -> reason={shutdownReason}");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        // 신뢰성 있는 데이터 수신 처리
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        // 신뢰성 있는 데이터 전송 진행 상황 처리
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // 네트워크 입력 처리
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // 누락된, 인터폴레이션된 입력 처리
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // 객체가 관심 영역(Area of Interest)에 들어왔을 때 처리
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // 객체가 관심 영역(Area of Interest)에서 나갔을 때 처리
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        // 호스트 마이그레이션 처리
    }
}

