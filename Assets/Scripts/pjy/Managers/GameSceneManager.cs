// Assets\Scripts\GameSceneManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameSceneManager : MonoBehaviour
{
    private static GameSceneManager instance;
    public static GameSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameSceneManager>();
            }
            return instance;
        }
    }

    [Header("스포너 참조")]
    public WaveSpawner spawner;
    public WaveSpawnerRegion2 spawner2;
    [Header("게임씬의 9개 캐릭터 슬롯(이미지/텍스트)")]
    [SerializeField] private Image[] slotImages9;          
    [SerializeField] private TextMeshProUGUI[] slotTexts9; 

    [Header("게임씬 UI: 나가기 버튼")]
    [SerializeField] private Button exitButton;

    [Header("게임 진행 시간 텍스트")]
    [SerializeField] private TextMeshProUGUI gameTimeText;

    [Header("Hero Panel (주인공 캐릭터 전용)")]
    public GameObject heroPanel;  

    [Header("아이템 인벤토리 패널 (게임씬)")]
    [SerializeField] private GameObject itemInventoryPanel;

    [Header("종족별 캐릭터 표시 (기획서: 각 종족 3명 + 자유 1명)")]
    [SerializeField] private TextMeshProUGUI humanCountText;
    [SerializeField] private TextMeshProUGUI orcCountText;
    [SerializeField] private TextMeshProUGUI elfCountText;

    [Header("종족 버튼 (리사이클용)")]
    [SerializeField] private Button humanRaceButton;
    [SerializeField] private Button orcRaceButton;
    [SerializeField] private Button elfRaceButton;
    [SerializeField] private Button recycleButton;

    [Header("리사이클 매니저")]
    [SerializeField] private RaceRecycleManager raceRecycleManager;

    [Header("★★★ 캐릭터 수 표시")]
    [SerializeField] private TextMeshProUGUI playerCharacterCountText;
    [SerializeField] private TextMeshProUGUI aiCharacterCountText;

    private CharacterData[] deckFromLobby = new CharacterData[9]; 
    private CharacterData heroCharacter = null; 

    private float elapsedTime = 0f;

    // 종족별 카운트
    private int humanCount = 0;
    private int orcCount = 0;
    private int elfCount = 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnEnable()
    {
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }
    }

    private void Start()
    {
        // CoreDataManager 초기화 확인
        if (CoreDataManager.Instance == null)
        {
            Debug.LogError("[GameSceneManager] CoreDataManager가 없습니다!");
            return;
        }

        // (기존 코드) GameManager에서 1~9 캐릭터 정보 가져오기
        if (GameManager.Instance != null &&
            GameManager.Instance.currentRegisteredCharacters != null &&
            GameManager.Instance.currentRegisteredCharacters.Length >= 10)
        {
            for (int i = 0; i < 9; i++)
            {
                deckFromLobby[i] = GameManager.Instance.currentRegisteredCharacters[i];
            }

            // 10번째(인덱스 9) 캐릭터가 있으면 히어로로 설정
            if (GameManager.Instance.currentRegisteredCharacters[9] != null)
            {
                heroCharacter = GameManager.Instance.currentRegisteredCharacters[9];
                Debug.Log($"[GameSceneManager] 히어로 캐릭터 설정: {heroCharacter.characterName}");
            }
        }

        // CoreDataManager에 슬롯 정보 업데이트
        if (CoreDataManager.Instance.characterDatabase != null)
        {
            for (int i = 0; i < 9 && i < deckFromLobby.Length; i++)
            {
                if (deckFromLobby[i] != null)
                {
                    CoreDataManager.Instance.characterDatabase.currentRegisteredCharacters[i] = deckFromLobby[i];
                }
            }
        }

        // 미네랄 바 참조
        if (CoreDataManager.Instance.region1MineralBar == null)
        {
            GameObject mineralBar1 = GameObject.Find("MineralBar");
            if (mineralBar1 != null)
            {
                CoreDataManager.Instance.region1MineralBar = mineralBar1.GetComponent<MineralBar>();
            }
        }

        if (CoreDataManager.Instance.region2MineralBar == null)
        {
            GameObject mineralBar2 = GameObject.Find("Region2MineralBar");
            if (mineralBar2 != null)
            {
                CoreDataManager.Instance.region2MineralBar = mineralBar2.GetComponent<MineralBar>();
            }
        }

        // UI 업데이트
        if (slotImages9 != null && slotTexts9 != null)
        {
            for (int i = 0; i < 9; i++)
            {
                if (i < deckFromLobby.Length && deckFromLobby[i] != null)
                {
                    CharacterData c = deckFromLobby[i];
                    if (slotImages9[i] != null)
                    {
                        slotImages9[i].gameObject.SetActive(true);
                        slotImages9[i].sprite = c.buttonIcon != null ? c.buttonIcon : null;

                        slotTexts9[i].gameObject.SetActive(true);
                        slotTexts9[i].text = $"{c.characterName}\nLv.{c.level}\n{GetRaceString(c.race)}";
                    }
                    else
                    {
                        slotImages9[i].gameObject.SetActive(false);
                        slotTexts9[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        // 종족별 카운트 UI 업데이트
        UpdateRaceCountUI();

        // 4) Hero(인덱스 9) 자동 소환 - 지연 실행으로 변경
        StartCoroutine(SpawnHeroDelayed());

        // 나가기 버튼
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnClickExitGame);
        }

        // 게임 시간 초기
        if (gameTimeText != null)
        {
            gameTimeText.text = "Time: 0.0s";
        }

        // 아이템 인벤토리 패널
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }

        // 리사이클 매니저 초기화
        InitializeRaceRecycleManager();
    }

    /// <summary>
    /// 히어로 소환을 지연시켜 다른 컴포넌트들이 먼저 초기화되도록 함
    /// </summary>
    private System.Collections.IEnumerator SpawnHeroDelayed()
    {
        // 1프레임 대기
        yield return null;
        
        // CoreDataManager가 초기화될 때까지 대기
        int waitCount = 0;
        while (CoreDataManager.Instance == null && waitCount < 30)
        {
            yield return null;
            waitCount++;
        }

        if (CoreDataManager.Instance == null)
        {
            Debug.LogError("[GameSceneManager] CoreDataManager 초기화 실패!");
            yield break;
        }

        // Hero 소환
        SpawnHero();
    }

    /// <summary>
    /// 히어로 캐릭터 소환
    /// </summary>
    private void SpawnHero()
    {
        if (heroCharacter == null || heroCharacter.spawnPrefab == null)
        {
            Debug.LogWarning("[GameSceneManager] 히어로 캐릭터 데이터가 없습니다.");
            return;
        }

        // 프리팹 유효성 검사
        if (!IsPrefabValid(heroCharacter.spawnPrefab))
        {
            Debug.LogError($"[GameSceneManager] 히어로 프리팹 '{heroCharacter.spawnPrefab.name}'에 Missing Script 참조가 있습니다. 프리팹을 확인해주세요.");
            return;
        }

        // heroPanel이 없으면 찾거나 생성
        if (heroPanel == null)
        {
            // 기존 heroPanel 찾기
            GameObject existingHeroPanel = GameObject.Find("HeroPanel");
            if (existingHeroPanel != null)
            {
                heroPanel = existingHeroPanel;
                Debug.Log("[GameSceneManager] 기존 HeroPanel을 찾았습니다.");
            }
            else
            {
                // Canvas 찾기
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("[GameSceneManager] Canvas를 찾을 수 없습니다!");
                    return;
                }

                // HeroPanel 생성
                heroPanel = new GameObject("HeroPanel");
                heroPanel.transform.SetParent(canvas.transform, false);
                
                RectTransform heroPanelRect = heroPanel.AddComponent<RectTransform>();
                heroPanelRect.anchorMin = new Vector2(0, 0);
                heroPanelRect.anchorMax = new Vector2(0, 0);
                heroPanelRect.pivot = new Vector2(0.5f, 0.5f);
                heroPanelRect.anchoredPosition = new Vector2(200, 100);
                heroPanelRect.sizeDelta = new Vector2(100, 100);
                
                Debug.Log("[GameSceneManager] HeroPanel을 새로 생성했습니다.");
            }
        }

        // 히어로 캐릭터 생성
        GameObject heroObj = Instantiate(heroCharacter.spawnPrefab, heroPanel.transform);
        heroObj.name = $"Hero_{heroCharacter.characterName}";
        
        // RectTransform 설정
        RectTransform heroRect = heroObj.GetComponent<RectTransform>();
        if (heroRect == null)
        {
            heroRect = heroObj.AddComponent<RectTransform>();
        }
        heroRect.anchorMin = new Vector2(0.5f, 0.5f);
        heroRect.anchorMax = new Vector2(0.5f, 0.5f);
        heroRect.pivot = new Vector2(0.5f, 0.5f);
        heroRect.anchoredPosition = Vector2.zero;
        heroRect.localScale = Vector3.one;

        // Character 컴포넌트 설정
        Character heroComp = heroObj.GetComponent<Character>();
        if (heroComp != null)
        {
            // 히어로로 강제 지정 + HP바 비활성화
            heroComp.isHero = true;
            if (heroComp.hpBarCanvas != null)
            {
                heroComp.hpBarCanvas.gameObject.SetActive(false);
            }

            heroComp.attackPower = heroCharacter.attackPower;
            switch (heroCharacter.rangeType)
            {
                case RangeType.Melee:    heroComp.attackRange = 1.2f; break;
                case RangeType.Ranged:   heroComp.attackRange = 2.5f; break;
                case RangeType.LongRange:heroComp.attackRange = 4.0f; break;
            }

            // 총알 패널 연결
            var pMan = PlacementManager.Instance;
            if (pMan != null && pMan.bulletPanel != null)
            {
                heroComp.SetBulletPanel(pMan.bulletPanel);
            }
        }

        Debug.Log("[GameSceneManager] Hero(인덱스9) 자동 소환 완료");
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        if (gameTimeText != null)
        {
            gameTimeText.text = $"{elapsedTime:F1}s";
        }

        // ★★★ 캐릭터 수 업데이트
        UpdateCharacterCountUI();
    }

    /// <summary>
    /// ★★★ 캐릭터 수 UI 업데이트
    /// </summary>
    private void UpdateCharacterCountUI()
    {
        if (PlacementManager.Instance != null)
        {
            int playerCount = PlacementManager.Instance.GetCharacterCount(false);
            int aiCount = PlacementManager.Instance.GetCharacterCount(true);

            if (playerCharacterCountText != null)
            {
                playerCharacterCountText.text = $"플레이어: {playerCount}/50";
                
                // 제한에 가까워지면 색상 변경
                if (playerCount >= 45)
                    playerCharacterCountText.color = Color.red;
                else if (playerCount >= 40)
                    playerCharacterCountText.color = Color.yellow;
                else
                    playerCharacterCountText.color = Color.white;
            }

            if (aiCharacterCountText != null)
            {
                aiCharacterCountText.text = $"AI: {aiCount}/50";
                
                // 제한에 가까워지면 색상 변경
                if (aiCount >= 45)
                    aiCharacterCountText.color = Color.red;
                else if (aiCount >= 40)
                    aiCharacterCountText.color = Color.yellow;
                else
                    aiCharacterCountText.color = Color.white;
            }
        }
    }

    /// <summary>
    /// 리사이클 매니저 초기화
    /// </summary>
    private void InitializeRaceRecycleManager()
    {
        // RaceRecycleManager가 없으면 자동 생성
        if (raceRecycleManager == null)
        {
            raceRecycleManager = FindFirstObjectByType<RaceRecycleManager>();
            
            if (raceRecycleManager == null)
            {
                GameObject recycleManagerObj = new GameObject("RaceRecycleManager");
                raceRecycleManager = recycleManagerObj.AddComponent<RaceRecycleManager>();
                Debug.Log("[GameSceneManager] RaceRecycleManager 자동 생성");
            }
        }

        // 종족 버튼들이 설정되어 있는지 확인
        if (humanRaceButton == null || orcRaceButton == null || elfRaceButton == null)
        {
            Debug.LogWarning("[GameSceneManager] 종족 버튼들이 설정되지 않았습니다. Inspector에서 설정해주세요.");
        }
        
        if (recycleButton == null)
        {
            Debug.LogWarning("[GameSceneManager] 리사이클 버튼이 설정되지 않았습니다. Inspector에서 설정해주세요.");
        }
    }

    private void OnClickExitGame()
    {
        Debug.Log("[GameSceneManager] 나가기 버튼 클릭 -> 로비씬");
        SceneManager.LoadScene("LobbyScene");
    }

    private bool IsPrefabValid(GameObject prefab)
    {
        if (prefab == null) 
        {
            Debug.LogError("[GameSceneManager] 프리팹이 null입니다.");
            return false;
        }
        
        Component[] components = prefab.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null)
            {
                Debug.LogError($"[GameSceneManager] 프리팹 '{prefab.name}'에 Missing Script가 발견되었습니다!");
                return false;
            }
        }
        
        Component[] childComponents = prefab.GetComponentsInChildren<Component>(true);
        foreach (var comp in childComponents)
        {
            if (comp == null)
            {
                Debug.LogError($"[GameSceneManager] 프리팹 '{prefab.name}'의 자식에 Missing Script가 발견되었습니다!");
                return false;
            }
        }
        
        return true;
    }

    private string GetRaceString(CharacterRace race)
    {
        switch (race)
        {
            case CharacterRace.Human: return "휴먼";
            case CharacterRace.Orc: return "오크";
            case CharacterRace.Elf: return "엘프";
            case CharacterRace.Undead: return "언데드";
            default: return "기타";
        }
    }

    /// <summary>
    /// 종족별 카운트 UI 업데이트
    /// </summary>
    private void UpdateRaceCountUI()
    {
        // 종족별 카운트 초기화
        humanCount = 0;
        orcCount = 0;
        elfCount = 0;

        // 현재 덱에서 종족별 카운트
        for (int i = 0; i < 9 && i < deckFromLobby.Length; i++)
        {
            if (deckFromLobby[i] != null)
            {
                switch (deckFromLobby[i].race)
                {
                    case CharacterRace.Human:
                        humanCount++;
                        break;
                    case CharacterRace.Orc:
                        orcCount++;
                        break;
                    case CharacterRace.Elf:
                        elfCount++;
                        break;
                }
            }
        }

        // UI 텍스트 업데이트
        if (humanCountText != null)
            humanCountText.text = $"휴먼: {humanCount}";
        if (orcCountText != null)
            orcCountText.text = $"오크: {orcCount}";
        if (elfCountText != null)
            elfCountText.text = $"엘프: {elfCount}";
    }
}