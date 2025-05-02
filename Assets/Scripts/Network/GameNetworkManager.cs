using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour
{
    [SerializeField] private NetworkRunner networkRunnerPrefab;
    private NetworkRunner _runner;
    
    // 예: 방 생성(또는 입장)
    public async void StartGame()
    {
        // NetworkRunner 인스턴스가 없다면 새로 생성
        if (_runner == null)
        {
            _runner = Instantiate(networkRunnerPrefab);
        }

        // SceneRef 및 NetworkSceneInfo 생성 및 설정
        var sceneRef = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (sceneRef.IsValid)
        {
            sceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Single); // 현재 씬 하나만 로드
        }

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,   // 방 없으면 생성, 있으면 참가
            SessionName = "TestRoom",               // 방(세션) 이름
            Scene = sceneInfo,                      // 생성한 sceneInfo 할당
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>() // 기본 씬 매니저
        };

        // 실제로 Runner 시작
        await _runner.StartGame(startGameArgs);
    }
}
