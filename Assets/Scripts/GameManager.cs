// Assets\Scripts\GameManager.cs

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
        // 초기 설정
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
        // 예: Esc 키로 게임 종료
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
