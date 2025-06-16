using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 게임의 핵심 데이터를 관리하는 매니저
/// 싱글톤 패턴 사용
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

    [Header("게임 설정")]
    [Tooltip("호스트 여부 (true: 지역1, false: 지역2)")]
    public bool isHost = true;
    
    [Header("캐릭터 데이터베이스")]
    public CharacterDatabaseObject characterDatabase;
    
    [Header("미네랄 바")]
    public MineralBar region1MineralBar;
    public MineralBar region2MineralBar;
    
    [Header("UI 패널")]
    public RectTransform characterPanel;
    public RectTransform ourMonsterPanel;
    public RectTransform bulletPanel;
    public RectTransform opponentCharacterPanel;
    public RectTransform opponentBulletPanel;
    public RectTransform opponentOurMonsterPanel;
    
    [Header("종족 설정")]
    [Tooltip("플레이 가능한 종족 목록 (게임 기획서: 휴먼, 오크, 엘프)")]
    public CharacterRace[] playableRaces = { CharacterRace.Human, CharacterRace.Orc, CharacterRace.Elf };
    
    [Header("게임 밸런스 설정")]
    [Tooltip("중간성 기본 체력 (게임 기획서: 500)")]
    public int middleCastleHealth = 500;
    
    [Tooltip("최종성 기본 체력 (게임 기획서: 1000)")]
    public int finalCastleHealth = 1000;
    
    [Tooltip("기본 소환 비용")]
    public int defaultSummonCost = 10;
    
    [Header("CSV 설정")]
    [Tooltip("CSV 파일 경로")]
    public string csvFilePath = "Assets/Data/CharacterData.csv";
    
    [Header("총알 프리팹")]
    [Tooltip("기본 총알 프리팹")]
    public GameObject defaultBulletPrefab;
    
    // 현재 선택된 캐릭터 인덱스 (임시로 여기에 저장)
    [HideInInspector] public int currentCharacterIndex = 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeData();
    }

    /// <summary>
    /// 데이터 초기화
    /// </summary>
    private void InitializeData()
    {
        Debug.Log("[CoreDataManager] 데이터 초기화 시작...");
        
        // 캐릭터 데이터베이스 확인
        if (characterDatabase == null)
        {
            characterDatabase = Resources.Load<CharacterDatabaseObject>("CharacterDatabase");
            if (characterDatabase == null)
            {
                // ScriptableObject 자동 검색
                CharacterDatabaseObject[] databases = Resources.LoadAll<CharacterDatabaseObject>("");
                if (databases.Length > 0)
                {
                    characterDatabase = databases[0];
                    Debug.Log($"[CoreDataManager] CharacterDatabase를 자동으로 찾았습니다: {characterDatabase.name}");
                }
                else
                {
                    // 씬에서도 찾아보기
                    CharacterDatabaseObject sceneDatabase = FindFirstObjectByType<CharacterDatabaseObject>();
                    if (sceneDatabase != null)
                    {
                        characterDatabase = sceneDatabase;
                        Debug.Log("[CoreDataManager] CharacterDatabase를 씬에서 찾았습니다.");
                    }
                    else
                    {
                        Debug.LogError("[CoreDataManager] characterDatabase가 null이고 씬에서도 찾을 수 없습니다! CharacterDatabase 프리팹을 씬에 배치해주세요.");
                    }
                }
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
        
        // 기본 총알 프리팹이 없으면 자동으로 찾기
        if (defaultBulletPrefab == null)
        {
            defaultBulletPrefab = Resources.Load<GameObject>("Prefabs/DefaultBullet");
            if (defaultBulletPrefab == null)
            {
                // Resources 폴더에서 Bullet이라는 이름을 가진 프리팹 찾기
                GameObject[] bulletPrefabs = Resources.LoadAll<GameObject>("Prefabs");
                foreach (var prefab in bulletPrefabs)
                {
                    if (prefab.name.Contains("Bullet"))
                    {
                        defaultBulletPrefab = prefab;
                        Debug.Log($"[CoreDataManager] 기본 총알 프리팹을 자동으로 찾았습니다: {defaultBulletPrefab.name}");
                        break;
                    }
                }
            }
        }
        
        // 초기 캐릭터 인덱스 설정
        currentCharacterIndex = -1; // 선택되지 않은 상태로 시작
        
        // CSV 동기화 초기화
        InitializeCSVSync();
        
        // 성 체력 설정
        SetupCastleHealth();
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