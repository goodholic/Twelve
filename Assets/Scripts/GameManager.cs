using UnityEngine;
using System.Collections.Generic;

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

    // =============================
    // (신규) 로비씬 → 게임씬 전달용
    // 9칸 + 주인공(10번째) 분리
    // =============================
    private CharacterData[] deckForGame = new CharacterData[9];

    // (추가) 10번째 덱용 주인공
    private CharacterData heroForGame = null;

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

        // DontDestroyOnLoad
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        DontDestroyOnLoad(gameObject);
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// 웨이브 시작 버튼 등을 눌렀을 때 호출
    /// </summary>
    public void StartWave()
    {
        if (waveSpawner != null)
        {
            waveSpawner.StartNextWave();
        }
    }

    // =========================================================
    //  로비씬 → 게임씬으로 '등록된 9개 캐릭터'를 전달
    // =========================================================
    public void SetDeckForGame(CharacterData[] deck9)
    {
        if (deck9 == null || deck9.Length < 9)
        {
            Debug.LogWarning("[GameManager] SetDeckForGame: 인자가 null이거나 9칸 미만!");
            return;
        }

        for (int i = 0; i < 9; i++)
        {
            deckForGame[i] = deck9[i];
        }
        Debug.Log("[GameManager] SetDeckForGame 완료 (9개 캐릭터 저장)");
    }

    public CharacterData[] GetDeckForGame()
    {
        // 단순 참조 반환
        return deckForGame;
    }

    // =========================================================
    //  (추가) 10번째(주인공) 캐릭터 전달
    // =========================================================
    public void SetHeroCharacter(CharacterData hero)
    {
        heroForGame = hero;
    }

    public CharacterData GetHeroCharacter()
    {
        return heroForGame;
    }
}
