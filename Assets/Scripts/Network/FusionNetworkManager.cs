// Assets\Scripts\Network\FusionNetworkManager.cs

using Fusion;
using Fusion.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class FusionNetworkManager : MonoBehaviour
{
    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _sceneManager;

    public async void StartGame(GameMode mode)
    {
        // 네트워크 러너 생성
        GameObject runnerObj = new GameObject("NetworkRunner");
        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true; // 호스트/클라 입력 가능

        // 씬 매니저
        GameObject sceneManagerObj = new GameObject("NetworkSceneManager");
        _sceneManager = sceneManagerObj.AddComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = null, // 현재 씬 사용
            SceneManager = _sceneManager
        };

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

    public async void JoinGame()
    {
        // 클라이언트 모드
        GameObject runnerObj = new GameObject("NetworkRunner");
        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        GameObject sceneManagerObj = new GameObject("NetworkSceneManager");
        _sceneManager = sceneManagerObj.AddComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = "TestRoom",
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
}
