using UnityEngine;
using System.Collections.Generic;
using Fusion;
// ▼ 아래 2줄은 필요하다면 제거하거나 주석처리 가능합니다.
// using UnityEngine.SceneManagement; // (씬 전환 미사용 시 주석처리 가능)
using TMPro;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
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

    // =======================================================================
    // == 기존에는 resultSceneName 필드 + 씬 이동 로직이 있었으나 제거했습니다. ==
    // =======================================================================
    // public string resultSceneName = "ResultScene"; // (사용 안 함)

    [HideInInspector] public bool isGameOver = false;  // 게임이 끝났는지 여부
    [HideInInspector] public bool isVictory = false;   // true면 승리, false면 패배

    // =================== (추가) 결과 패널/텍스트 연결 ===================
    [Header("결과 패널(씬 전환 대신 사용)")]
    public GameObject resultPanel;          // 인스펙터에서 연결 (기본비활성)
    public TextMeshProUGUI resultPanelText; // 승/패 문구 표시용 TMP

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
        // ESC로 종료
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

    /// <summary>
    /// 게임 오버(승/패) 처리.  
    /// 기존에는 `SceneManager.LoadScene(resultSceneName);`를 호출했으나 제거하고,  
    /// **같은 씬**에서 `resultPanel.SetActive(true)`로 대체합니다.
    /// </summary>
    public void SetGameOver(bool victory)
    {
        if (isGameOver) return; // 이미 끝났다면 중복 처리 방지
        isGameOver = true;
        isVictory = victory;

        Debug.Log($"[GameManager] GameOver!! isVictory={victory}");

        // 승리 시 100골드 지급
        if (victory)
        {
            // ShopManager를 통해 골드 지급
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddGold(100);
                Debug.Log("[GameManager] 승리 보상으로 100골드가 지급되었습니다.");
            }
            else
            {
                Debug.LogWarning("[GameManager] ShopManager.Instance가 null - 골드를 지급할 수 없습니다.");
            }
        }

        // === 씬 이동 대신, 결과 패널을 켬 ===
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            // 승리냐 패배냐에 따라 텍스트 변경
            if (resultPanelText != null)
            {
                resultPanelText.text = victory ? "승리!" : "패배...";
            }
        }
        // else: 혹시라도 resultPanel이 null이면 메세지만 출력
        else
        {
            Debug.LogWarning("[GameManager] resultPanel이 null -> 결과 UI를 표시할 수 없습니다.");
        }
    }
}
