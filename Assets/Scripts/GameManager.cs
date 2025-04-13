using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
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
    [Tooltip("웨이브 스포너")]
    public WaveSpawner waveSpawner;

    [Tooltip("캐릭터 배치 매니저")]
    public PlacementManager placementManager;

    // 예: 게임 상태(승패), 점수, 돈 등의 정보도 여기에 추가 가능
    // public int currentMoney;
    // public int currentScore;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 초기 설정(WaveSpawner, PlacementManager 등을 연결하거나 씬에 배치)
        if (waveSpawner == null)
        {
            waveSpawner = FindObjectOfType<WaveSpawner>();
        }
        if (placementManager == null)
        {
            placementManager = FindObjectOfType<PlacementManager>();
        }
    }

    private void Update()
    {
        // 예시: 게임 흐름 제어, Esc 누르면 게임 종료 등
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// 웨이브 시작(버튼 등에 연결 가능)
    /// </summary>
    public void StartWave()
    {
        if (waveSpawner != null)
        {
            waveSpawner.StartNextWave();
        }
    }
}
