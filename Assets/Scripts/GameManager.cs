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


    // ============================================
    // (A) 기존: 1~9 캐릭터 / 10번(주인공) 보관
    // ============================================
    // 0~8번(9개): 일반 캐릭터, 9번: Hero
    private CharacterData[] deckForGame = new CharacterData[9];
    private CharacterData heroForGame = null;

    // ============================================
    // (B) 추가: currentRegisteredCharacters[10]
    // ============================================
    [Header("현재 등록된 캐릭터(총 10개)")]
    [Tooltip("인덱스 0~8: 일반, 인덱스 9: 히어로")]
    public CharacterData[] currentRegisteredCharacters = new CharacterData[10];


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

        // 씬 전환 시 유지
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
    /// (버튼 등) 웨이브 시작 시 호출
    /// </summary>
    public void StartWave()
    {
        if (waveSpawner != null)
        {
            waveSpawner.StartNextWave();
        }
    }

    // -----------------------------------------------------
    // (1) 1~9번 캐릭터를 세팅 + 반환
    // -----------------------------------------------------
    public void SetDeckForGame(CharacterData[] deck9)
    {
        if (deck9 == null || deck9.Length < 9)
        {
            Debug.LogWarning("[GameManager] SetDeckForGame: 9칸 미만");
            return;
        }
        // 복사
        for (int i = 0; i < 9; i++)
        {
            deckForGame[i] = deck9[i];
        }
        Debug.Log("[GameManager] 1~9 캐릭터(9칸) 세팅 완료");

        // 추가로 currentRegisteredCharacters[0..8]에 동기화
        for (int i = 0; i < 9; i++)
        {
            currentRegisteredCharacters[i] = deckForGame[i];
        }
    }

    public CharacterData[] GetDeckForGame()
    {
        return deckForGame;
    }

    // -----------------------------------------------------
    // (2) 10번(Hero) 캐릭터 세팅 + 반환
    // -----------------------------------------------------
    public void SetHeroCharacter(CharacterData hero)
    {
        heroForGame = hero;
        currentRegisteredCharacters[9] = heroForGame;  // Hero = 인덱스9
        Debug.Log($"[GameManager] 주인공(10번) 세팅: {hero?.characterName}");
    }

    public CharacterData GetHeroCharacter()
    {
        return heroForGame;
    }
}
