// Assets\Scripts\GameManager.cs

using UnityEngine;

/// <summary>
/// 2D 게임 전체를 관리하는 GameManager 예시.
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
    public WaveSpawner waveSpawner;
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void StartWave()
    {
        if (waveSpawner != null)
        {
            waveSpawner.StartNextWave();
        }
    }
}
