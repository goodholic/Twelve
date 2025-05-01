using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// 호스트와 클라이언트가 서로 다른 덱(캐릭터 세트)을 사용하도록 수정.
/// - hostDeck / clientDeck 두 세트를 Inspector에 할당.
/// - Awake()에서 NetworkRunner 검사하여, host면 hostDeck을, client면 clientDeck을 currentRegisteredCharacters에 복사.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    [Header("Managers")]
    public WaveSpawner waveSpawner;
    public PlacementManager placementManager;

    // ------------------ 새로 추가된 부분 ------------------
    [Header("[호스트 전용 9칸 덱]")]
    public CharacterData[] hostDeck = new CharacterData[9];

    [Header("[클라이언트 전용 9칸 덱]")]
    public CharacterData[] clientDeck = new CharacterData[9];
    // -----------------------------------------------------

    [Header("현재 등록된 캐릭터(총 10개)")]
    [Tooltip("인덱스 0~8: 일반, 인덱스 9: 히어로 (옵션)")]
    public CharacterData[] currentRegisteredCharacters = new CharacterData[10];

    private NetworkRunner runner;

    private void Awake()
    {
        // 싱글톤 중복 방지
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // 네트워크 런너 찾기
        runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            if (runner.GameMode == GameMode.Host)
            {
                // 호스트면 hostDeck을 currentRegisteredCharacters[0..8]에 복사
                for (int i = 0; i < 9; i++)
                {
                    currentRegisteredCharacters[i] = (hostDeck != null && i < hostDeck.Length)
                        ? hostDeck[i]
                        : null;
                }
                Debug.Log("[GameManager] Host => hostDeck 사용");
            }
            else if (runner.GameMode == GameMode.Client)
            {
                // 클라이언트면 clientDeck을 currentRegisteredCharacters[0..8]에 복사
                for (int i = 0; i < 9; i++)
                {
                    currentRegisteredCharacters[i] = (clientDeck != null && i < clientDeck.Length)
                        ? clientDeck[i]
                        : null;
                }
                Debug.Log("[GameManager] Client => clientDeck 사용");
            }
        }
        else
        {
            // 싱글플레이(네트워크 없음) => 호스트 덱 사용
            for (int i = 0; i < 9; i++)
            {
                currentRegisteredCharacters[i] = (hostDeck != null && i < hostDeck.Length)
                    ? hostDeck[i]
                    : null;
            }
            Debug.Log("[GameManager] 싱글플레이 => hostDeck 사용");
        }
    }

    private void Start()
    {
        if (waveSpawner == null)
        {
            waveSpawner = FindFirstObjectByType<WaveSpawner>();
        }
        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<PlacementManager>();
        }
    }

    private void Update()
    {
        // ESC로 종료 테스트
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// (버튼) 웨이브 시작
    /// </summary>
    public void StartWave()
    {
        if (waveSpawner != null)
        {
            waveSpawner.StartNextWave();
        }
    }
}
