using UnityEngine;

/// <summary>
/// 중앙 데이터 관리자 - 모든 매니저가 공유하는 데이터베이스와 UI 패널 참조 관리
/// </summary>
public class CoreDataManager : MonoBehaviour
{
    private static CoreDataManager instance;
    public static CoreDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CoreDataManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CoreDataManager");
                    instance = go.AddComponent<CoreDataManager>();
                }
            }
            return instance;
        }
    }

    [Header("Split Database - Ally / Enemy")]
    [Tooltip("내가 다루는(아군) Database (ScriptableObject)")]
    public CharacterDatabaseObject allyDatabase;

    [Tooltip("적(상대) Database (ScriptableObject)")]
    public CharacterDatabaseObject enemyDatabase;

    [Tooltip("내가 다루는(아군) 캐릭터 프리팹 (총 10개)")]
    public GameObject[] allyCharacterPrefabs = new GameObject[10];

    [Tooltip("적(상대) 캐릭터 프리팹 (총 10개)")]
    public GameObject[] enemyCharacterPrefabs = new GameObject[10];

    [Tooltip("내가 다루는(아군) 탄환 프리팹 (총 10개)")]
    public GameObject[] allyBulletPrefabs = new GameObject[10];

    [Tooltip("적(상대) 탄환 프리팹 (총 10개)")]
    public GameObject[] enemyBulletPrefabs = new GameObject[10];

    [Tooltip("내가 다루는(아군) 몬스터 프리팹 (총 101개)")]
    public GameObject[] allyMonsterPrefabs = new GameObject[101];

    [Tooltip("적(상대) 몬스터 프리팹 (총 101개)")]
    public GameObject[] enemyMonsterPrefabs = new GameObject[101];

    [Header("Character Database")]
    [Tooltip("(구) 단일 DB (주로 아군용)")]
    public CharacterDatabase characterDatabase;

    [Header("2D Camera")]
    [Tooltip("2D 카메라 (Orthographic)")]
    public Camera mainCamera;

    [Header("UI Panels")]
    [Tooltip("타일들이 모인 패널")]
    public RectTransform tilePanel;

    [Tooltip("아군 건물/캐릭터가 들어갈 패널")]
    public RectTransform characterPanel;

    [Tooltip("아군 탄환이 들어갈 패널")]
    public RectTransform bulletPanel;

    [Tooltip("아군 몬스터(웨이브 소환)가 들어갈 패널")]
    public RectTransform ourMonsterPanel;

    [Tooltip("VFX(이펙트) 프리팹이 생성될 부모 패널 (없으면 월드)")]
    public RectTransform vfxPanel;

    [Header("Mineral Bars for Region1 / Region2")]
    public MineralBar region1MineralBar;
    public MineralBar region2MineralBar;

    [Header("Opponent Panels for Region2")]
    [Tooltip("상대(지역2) 건물/캐릭터 패널")]
    public RectTransform opponentCharacterPanel;

    [Tooltip("상대(지역2) 탄환 패널")]
    public RectTransform opponentBulletPanel;

    [Tooltip("상대(지역2) 몬스터 패널")]
    public RectTransform opponentOurMonsterPanel;

    [Header("Star Merge DB")]
    public StarMergeDatabaseObject starMergeDatabase;
    public StarMergeDatabaseObject starMergeDatabaseRegion2;

    [Header("Network")]
    public bool isHost = true;

    [Header("게임 기획서 - 종족 시스템")]
    [Tooltip("3종족: 휴먼, 오크, 엘프")]
    public CharacterRace[] playableRaces = { CharacterRace.Human, CharacterRace.Orc, CharacterRace.Elf };
    
    [Header("게임 기획서 - 성 체력 설정")]
    [Tooltip("중간성 기본 체력")]
    public int middleCastleHealth = 500;
    [Tooltip("최종성 기본 체력")]
    public int finalCastleHealth = 1000;

    [Header("CSV 연동")]
    [Tooltip("CSV 파일 경로")]
    public string csvFilePath = "Assets/Data/CharacterData.csv";
    [Tooltip("자동 CSV 동기화 활성화")]
    public bool enableCSVSync = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // VFX 패널 초기 설정
        if (vfxPanel == null)
        {
            GameObject panelObj = GameObject.Find("VFX Panel");
            if (panelObj != null)
            {
                vfxPanel = panelObj.GetComponent<RectTransform>();
            }
        }
        Bullet.SetVfxPanel(vfxPanel);

        // EventSystem 체크
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            Debug.LogWarning("<color=red>씬에 EventSystem이 없습니다! UI 클릭/드래그가 제대로 안 될 수 있음.</color>");
        }
    }

    // CoreDataManager.cs의 Start 메서드에 다음 코드를 추가하세요:

private void Start()
{
    // UI 패널 디버그 로그
    Debug.Log($"[CoreDataManager] UI 패널 상태 확인 - " +
              $"characterPanel={characterPanel != null}, " +
              $"opponentCharacterPanel={opponentCharacterPanel != null}, " +
              $"opponentOurMonsterPanel={opponentOurMonsterPanel != null}, " +
              $"opponentBulletPanel={opponentBulletPanel != null}");

    // 로그인 패널이 비활성화된 상태이므로 항상 호스트로 설정
    isHost = true;
    Debug.Log("[CoreDataManager] 로그인 패널 비활성화 상태: 호스트 모드로 플레이합니다.");

    // CSV 동기화 초기화
    if (enableCSVSync)
    {
        InitializeCSVSync();
    }

    // 성 체력 초기화
    SetupCastleHealth();
    
    // ========== 추가된 코드 시작 ==========
    // characterDatabase 초기화 확인
    if (characterDatabase == null)
    {
        // 씬에서 CharacterDatabase 컴포넌트를 찾아서 자동 설정
        CharacterDatabase foundDB = FindFirstObjectByType<CharacterDatabase>();
        if (foundDB != null)
        {
            characterDatabase = foundDB;
            Debug.Log("[CoreDataManager] CharacterDatabase를 자동으로 찾아서 설정했습니다.");
        }
        else
        {
            Debug.LogError("[CoreDataManager] characterDatabase가 null이고 씬에서도 찾을 수 없습니다! CharacterDatabase 프리팹을 씬에 배치해주세요.");
        }
    }
    
    if (characterDatabase != null)
    {
        if (characterDatabase.currentRegisteredCharacters == null || characterDatabase.currentRegisteredCharacters.Length == 0)
        {
            Debug.LogWarning("[CoreDataManager] characterDatabase.currentRegisteredCharacters가 비어있습니다!");
            
            // GameManager에서 데이터 가져오기 시도
            if (GameManager.Instance != null && GameManager.Instance.currentRegisteredCharacters != null)
            {
                characterDatabase.currentRegisteredCharacters = new CharacterData[GameManager.Instance.currentRegisteredCharacters.Length];
                for (int i = 0; i < GameManager.Instance.currentRegisteredCharacters.Length; i++)
                {
                    characterDatabase.currentRegisteredCharacters[i] = GameManager.Instance.currentRegisteredCharacters[i];
                }
                Debug.Log($"[CoreDataManager] GameManager에서 {characterDatabase.currentRegisteredCharacters.Length}개 캐릭터 데이터를 가져왔습니다.");
            }
        }
        else
        {
            Debug.Log($"[CoreDataManager] characterDatabase에 {characterDatabase.currentRegisteredCharacters.Length}개 캐릭터가 등록되어 있습니다.");
        }
    }
    
    // MineralBar 초기화 확인
    if (region1MineralBar == null)
    {
        GameObject mineralBarObj = GameObject.Find("Region1MineralBar");
        if (mineralBarObj != null)
        {
            region1MineralBar = mineralBarObj.GetComponent<MineralBar>();
            Debug.Log("[CoreDataManager] Region1MineralBar를 자동으로 찾았습니다.");
        }
        else
        {
            Debug.LogError("[CoreDataManager] region1MineralBar가 null입니다! Inspector에서 설정해주세요.");
        }
    }
    
    if (region2MineralBar == null)
    {
        GameObject mineralBarObj = GameObject.Find("Region2MineralBar");
        if (mineralBarObj != null)
        {
            region2MineralBar = mineralBarObj.GetComponent<MineralBar>();
            Debug.Log("[CoreDataManager] Region2MineralBar를 자동으로 찾았습니다.");
        }
        else
        {
            Debug.LogError("[CoreDataManager] region2MineralBar가 null입니다! Inspector에서 설정해주세요.");
        }
    }
    
    // UI 패널들이 없으면 자동으로 찾기
    if (characterPanel == null)
    {
        GameObject panelObj = GameObject.Find("CharacterPanel");
        if (panelObj != null)
        {
            characterPanel = panelObj.GetComponent<RectTransform>();
            Debug.Log("[CoreDataManager] CharacterPanel을 자동으로 찾았습니다.");
        }
    }
    
    if (ourMonsterPanel == null)
    {
        GameObject panelObj = GameObject.Find("OurMonsterPanel");
        if (panelObj != null)
        {
            ourMonsterPanel = panelObj.GetComponent<RectTransform>();
            Debug.Log("[CoreDataManager] OurMonsterPanel을 자동으로 찾았습니다.");
        }
    }
    
    if (bulletPanel == null)
    {
        GameObject panelObj = GameObject.Find("BulletPanel");
        if (panelObj != null)
        {
            bulletPanel = panelObj.GetComponent<RectTransform>();
            Debug.Log("[CoreDataManager] BulletPanel을 자동으로 찾았습니다.");
        }
    }
    
    // 초기 캐릭터 인덱스 설정
    currentCharacterIndex = -1; // 선택되지 않은 상태로 시작
    // ========== 추가된 코드 끝 ==========
}

    /// <summary>
    /// CSV 동기화 초기화
    /// </summary>
    private void InitializeCSVSync()
    {
        Debug.Log("[CoreDataManager] CSV 동기화 초기화 중...");
        // CSV 파일이 존재하는지 확인
        if (System.IO.File.Exists(csvFilePath))
        {
            Debug.Log($"[CoreDataManager] CSV 파일 발견: {csvFilePath}");
            // CSV 로드 로직은 별도 매니저에서 처리
        }
        else
        {
            Debug.LogWarning($"[CoreDataManager] CSV 파일을 찾을 수 없습니다: {csvFilePath}");
        }
    }

    /// <summary>
    /// 성 체력 초기 설정
    /// </summary>
    private void SetupCastleHealth()
    {
        // 중간성 체력 설정
        MiddleCastle[] middleCastles = FindObjectsByType<MiddleCastle>(FindObjectsSortMode.None);
        foreach (var castle in middleCastles)
        {
            if (castle != null)
            {
                castle.maxHealth = middleCastleHealth;
                castle.currentHealth = middleCastleHealth;
                Debug.Log($"[CoreDataManager] 중간성 {castle.name} 체력 {middleCastleHealth}으로 설정");
            }
        }

        // 최종성 체력 설정
        FinalCastle[] finalCastles = FindObjectsByType<FinalCastle>(FindObjectsSortMode.None);
        foreach (var castle in finalCastles)
        {
            if (castle != null)
            {
                castle.maxHealth = finalCastleHealth;
                castle.currentHealth = finalCastleHealth;
                Debug.Log($"[CoreDataManager] 최종성 {castle.name} 체력 {finalCastleHealth}으로 설정");
            }
        }
    }

    /// <summary>
    /// 현재 선택된 캐릭터 인덱스 (임시로 여기에 저장)
    /// </summary>
    [HideInInspector] public int currentCharacterIndex = 0;

    /// <summary>
    /// 종족이 플레이 가능한지 확인
    /// </summary>
    public bool IsPlayableRace(CharacterRace race)
    {
        foreach (var playableRace in playableRaces)
        {
            if (playableRace == race)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 랜덤한 플레이 가능 종족 반환
    /// </summary>
    public CharacterRace GetRandomPlayableRace()
    {
        if (playableRaces.Length > 0)
        {
            return playableRaces[Random.Range(0, playableRaces.Length)];
        }
        return CharacterRace.Human;
    }
}