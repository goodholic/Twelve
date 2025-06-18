using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 개선된 캐릭터 인벤토리 매니저 - 초기화 문제 해결
/// 안정적인 초기화 순서와 null 체크 강화
/// </summary>
public class ImprovedCharacterInventoryManager : MonoBehaviour
{
    [Header("ScriptableObject DB 참조")]
    [SerializeField] private CharacterDatabaseObject characterDatabaseObject;
    
    [Header("초기화 설정")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private float initializationDelay = 0.1f;
    [SerializeField] private int maxInitRetries = 3;
    
    [Header("기본 캐릭터 설정")]
    [SerializeField] private bool createDefaultCharactersIfEmpty = true;
    [SerializeField] private int defaultCharacterCount = 8;
    
    [Header("디버그")]
    [SerializeField] private bool enableDetailedLogs = false;
    
    // 인벤토리 데이터
    private List<CharacterData> ownedCharacters = new List<CharacterData>();
    private List<CharacterData> deckCharacters = new List<CharacterData>();
    private List<CharacterData> gachaPool = new List<CharacterData>();
    
    // 200칸 공유 배열
    public CharacterData[] sharedSlotData200 = new CharacterData[200];
    
    // 초기화 상태
    private bool isInitialized = false;
    private int initRetryCount = 0;
    
    // 싱글톤
    private static ImprovedCharacterInventoryManager instance;
    public static ImprovedCharacterInventoryManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ImprovedCharacterInventoryManager>();
            }
            return instance;
        }
    }
    
    private const string PLAYER_PREFS_OWNED_KEY = "ImprovedOwnedCharactersJsonV3";
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        LogDebug("=== ImprovedCharacterInventoryManager Awake ===");
        
        if (autoInitialize)
        {
            TryInitialize();
        }
    }
    
    private void Start()
    {
        // Awake에서 초기화되지 않았다면 Start에서 재시도
        if (!isInitialized)
        {
            LogDebug("Start에서 초기화 재시도");
            StartCoroutine(DelayedInitialization());
        }
    }
    
    /// <summary>
    /// 지연된 초기화 - 다른 매니저들이 준비될 때까지 대기
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialization()
    {
        yield return new WaitForSeconds(initializationDelay);
        
        while (!isInitialized && initRetryCount < maxInitRetries)
        {
            LogDebug($"초기화 시도 {initRetryCount + 1}/{maxInitRetries}");
            TryInitialize();
            
            if (!isInitialized)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        if (!isInitialized)
        {
            LogError("최대 재시도 횟수 초과 - 강제 초기화 진행");
            ForceInitialize();
        }
    }
    
    /// <summary>
    /// 초기화 시도
    /// </summary>
    private void TryInitialize()
    {
        initRetryCount++;
        
        // 1. 데이터베이스 확인
        if (!ValidateDatabase())
        {
            LogWarning("데이터베이스 검증 실패");
            return;
        }
        
        // 2. 가챠풀 구성
        if (!BuildGachaPool())
        {
            LogWarning("가챠풀 구성 실패");
            return;
        }
        
        // 3. 저장된 데이터 로드
        LoadCharacters();
        
        // 4. sharedSlotData200 동기화
        SyncSharedSlots();
        
        isInitialized = true;
        LogDebug($"초기화 성공! 인벤토리: {ownedCharacters.Count}개, 가챠풀: {gachaPool.Count}개");
    }
    
    /// <summary>
    /// 데이터베이스 검증
    /// </summary>
    private bool ValidateDatabase()
    {
        // Inspector에서 설정된 DB 확인
        if (characterDatabaseObject != null && ValidateDatabaseContent(characterDatabaseObject))
        {
            LogDebug("Inspector DB 검증 성공");
            return true;
        }
        
        // Resources 폴더에서 로드
        var resourceDB = Resources.Load<CharacterDatabaseObject>("CharacterDatabase");
        if (resourceDB != null && ValidateDatabaseContent(resourceDB))
        {
            characterDatabaseObject = resourceDB;
            LogDebug("Resources DB 검증 성공");
            return true;
        }
        
        // 모든 Resources에서 검색
        var allDBs = Resources.LoadAll<CharacterDatabaseObject>("");
        foreach (var db in allDBs)
        {
            if (ValidateDatabaseContent(db))
            {
                characterDatabaseObject = db;
                LogDebug($"대체 DB 발견: {db.name}");
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 데이터베이스 내용 검증
    /// </summary>
    private bool ValidateDatabaseContent(CharacterDatabaseObject db)
    {
        if (db == null) return false;
        if (db.characters == null || db.characters.Length == 0) return false;
        
        // null이 아닌 캐릭터가 하나라도 있는지 확인
        return db.characters.Any(c => c != null);
    }
    
    /// <summary>
    /// 가챠풀 구성
    /// </summary>
    private bool BuildGachaPool()
    {
        gachaPool.Clear();
        
        if (characterDatabaseObject == null || characterDatabaseObject.characters == null)
        {
            return false;
        }
        
        foreach (var charData in characterDatabaseObject.characters)
        {
            if (charData != null)
            {
                gachaPool.Add(charData);
                LogDebug($"가챠풀 추가: {charData.characterName}");
            }
        }
        
        return gachaPool.Count > 0;
    }
    
    /// <summary>
    /// 강제 초기화 - 모든 방법이 실패했을 때
    /// </summary>
    private void ForceInitialize()
    {
        LogWarning("강제 초기화 시작");
        
        // 기본 캐릭터 생성
        if (createDefaultCharactersIfEmpty)
        {
            CreateDefaultCharacters();
        }
        
        // 초기 캐릭터 제공
        ProvideInitialCharacters();
        
        isInitialized = true;
    }
    
    /// <summary>
    /// 기본 캐릭터 생성
    /// </summary>
    private void CreateDefaultCharacters()
    {
        LogDebug("기본 캐릭터 생성 시작");
        
        for (int i = 0; i < defaultCharacterCount; i++)
        {
            CharacterData defaultChar = ScriptableObject.CreateInstance<CharacterData>();
            
            // 기본 속성 설정
            defaultChar.characterName = $"DefaultChar_{i}";
            defaultChar.characterIndex = i;
            defaultChar.attackPower = 10 + (i * 2);
            defaultChar.attackSpeed = 1f;
            defaultChar.attackRange = 1.5f;
            defaultChar.maxHP = 100;
            defaultChar.health = 100;
            defaultChar.moveSpeed = 1f;
            defaultChar.rangeType = (RangeType)(i % 3);
            defaultChar.star = CharacterStar.OneStar;
            defaultChar.level = 1;
            defaultChar.currentExp = 0;
            defaultChar.expToNextLevel = 100;
            defaultChar.race = (CharacterRace)(i % 3);
            defaultChar.cost = 1 + (i % 3);
            defaultChar.tribe = (RaceType)(i % 3);
            
            gachaPool.Add(defaultChar);
            LogDebug($"기본 캐릭터 생성: {defaultChar.characterName}");
        }
    }
    
    /// <summary>
    /// 초기 캐릭터 제공
    /// </summary>
    private void ProvideInitialCharacters()
    {
        if (ownedCharacters.Count == 0 && gachaPool.Count > 0)
        {
            // 처음 시작하는 플레이어에게 기본 캐릭터 제공
            int initialCount = Mathf.Min(8, gachaPool.Count);
            for (int i = 0; i < initialCount; i++)
            {
                var template = gachaPool[i % gachaPool.Count];
                var newChar = CreateCharacterCopy(template);
                ownedCharacters.Add(newChar);
                
                if (i < sharedSlotData200.Length)
                {
                    sharedSlotData200[i] = newChar;
                }
            }
            
            LogDebug($"초기 캐릭터 {initialCount}개 제공");
            SaveCharacters();
        }
    }
    
    /// <summary>
    /// 캐릭터 복사본 생성
    /// </summary>
    private CharacterData CreateCharacterCopy(CharacterData template)
    {
        if (template == null) return null;
        
        // ScriptableObject 인스턴스 생성
        CharacterData copy = ScriptableObject.CreateInstance<CharacterData>();
        
        // 모든 속성 복사
        copy.characterName = template.characterName;
        copy.characterIndex = template.characterIndex;
        copy.attackPower = template.attackPower;
        copy.attackSpeed = template.attackSpeed;
        copy.attackRange = template.attackRange;
        copy.maxHP = template.maxHP;
        copy.health = template.health;
        copy.moveSpeed = template.moveSpeed;
        copy.rangeType = template.rangeType;
        copy.isAreaAttack = template.isAreaAttack;
        copy.isBuffSupport = template.isBuffSupport;
        copy.spawnPrefab = template.spawnPrefab;
        copy.buttonIcon = template.buttonIcon;
        copy.cost = template.cost;
        copy.level = template.level;
        copy.currentExp = template.currentExp;
        copy.expToNextLevel = template.expToNextLevel;
        copy.star = template.star;
        copy.race = template.race;
        copy.tribe = template.tribe;
        copy.isFreeSlotOnly = template.isFreeSlotOnly;
        copy.characterSprite = template.characterSprite;
        copy.frontSprite = template.frontSprite;
        copy.backSprite = template.backSprite;
        copy.areaAttackRadius = template.areaAttackRadius;
        copy.attackShapeType = template.attackShapeType;
        
        return copy;
    }
    
    /// <summary>
    /// sharedSlotData200 동기화
    /// </summary>
    private void SyncSharedSlots()
    {
        // 기존 데이터 정리
        for (int i = 0; i < sharedSlotData200.Length; i++)
        {
            sharedSlotData200[i] = null;
        }
        
        // ownedCharacters를 sharedSlotData200에 복사
        for (int i = 0; i < ownedCharacters.Count && i < sharedSlotData200.Length; i++)
        {
            sharedSlotData200[i] = ownedCharacters[i];
        }
    }
    
    /// <summary>
    /// 캐릭터 로드
    /// </summary>
    private void LoadCharacters()
    {
        ownedCharacters.Clear();
        deckCharacters.Clear();
        
        if (!PlayerPrefs.HasKey(PLAYER_PREFS_OWNED_KEY))
        {
            LogDebug("저장된 데이터 없음");
            return;
        }
        
        try
        {
            string json = PlayerPrefs.GetString(PLAYER_PREFS_OWNED_KEY);
            var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);
            
            if (wrapper != null && wrapper.characters != null)
            {
                foreach (var saveData in wrapper.characters)
                {
                    var template = FindTemplateByName(saveData.name);
                    if (template != null)
                    {
                        var loadedChar = CreateCharacterCopy(template);
                        loadedChar.level = saveData.level;
                        loadedChar.currentExp = saveData.currentExp;
                        
                        if (saveData.isInDeck)
                        {
                            deckCharacters.Add(loadedChar);
                        }
                        else
                        {
                            ownedCharacters.Add(loadedChar);
                        }
                    }
                }
                
                LogDebug($"캐릭터 로드 완료: 인벤토리 {ownedCharacters.Count}개, 덱 {deckCharacters.Count}개");
            }
        }
        catch (System.Exception e)
        {
            LogError($"캐릭터 로드 실패: {e.Message}");
        }
    }
    
    /// <summary>
    /// 캐릭터 저장
    /// </summary>
    public void SaveCharacters()
    {
        var saveDataList = new List<SaveData>();
        
        // ownedCharacters 저장
        foreach (var c in ownedCharacters)
        {
            if (c != null)
            {
                saveDataList.Add(new SaveData
                {
                    name = c.characterName,
                    level = c.level,
                    currentExp = (int)c.currentExp,
                    isInDeck = false
                });
            }
        }
        
        // deckCharacters 저장
        foreach (var c in deckCharacters)
        {
            if (c != null)
            {
                saveDataList.Add(new SaveData
                {
                    name = c.characterName,
                    level = c.level,
                    currentExp = (int)c.currentExp,
                    isInDeck = true
                });
            }
        }
        
        string json = JsonUtility.ToJson(new SerializationWrapper { characters = saveDataList });
        PlayerPrefs.SetString(PLAYER_PREFS_OWNED_KEY, json);
        PlayerPrefs.Save();
        
        LogDebug($"캐릭터 저장 완료: {saveDataList.Count}개");
    }
    
    /// <summary>
    /// 템플릿 찾기
    /// </summary>
    private CharacterData FindTemplateByName(string name)
    {
        return gachaPool.FirstOrDefault(c => c != null && c.characterName == name);
    }
    
    /// <summary>
    /// 초기화 상태 확인
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
    
    /// <summary>
    /// 소유 캐릭터 목록 가져오기
    /// </summary>
    public List<CharacterData> GetOwnedCharacters()
    {
        EnsureInitialized();
        return new List<CharacterData>(ownedCharacters);
    }
    
    /// <summary>
    /// 덱 캐릭터 목록 가져오기
    /// </summary>
    public List<CharacterData> GetDeckCharacters()
    {
        EnsureInitialized();
        return new List<CharacterData>(deckCharacters);
    }
    
    /// <summary>
    /// 랜덤 캐릭터 뽑기
    /// </summary>
    public CharacterData DrawRandomCharacter()
    {
        EnsureInitialized();
        
        if (gachaPool.Count == 0)
        {
            LogWarning("가챠풀이 비어있음");
            return null;
        }
        
        int randIdx = Random.Range(0, gachaPool.Count);
        var template = gachaPool[randIdx];
        var newChar = CreateCharacterCopy(template);
        
        // 인벤토리에 추가
        ownedCharacters.Add(newChar);
        
        // sharedSlotData200에 추가
        for (int i = 0; i < sharedSlotData200.Length; i++)
        {
            if (sharedSlotData200[i] == null)
            {
                sharedSlotData200[i] = newChar;
                break;
            }
        }
        
        SaveCharacters();
        return newChar;
    }
    
    /// <summary>
    /// 초기화 확인
    /// </summary>
    private void EnsureInitialized()
    {
        if (!isInitialized)
        {
            LogWarning("매니저가 초기화되지 않음 - 강제 초기화");
            ForceInitialize();
        }
    }
    
    /// <summary>
    /// 데이터 초기화
    /// </summary>
    public void ClearAllData()
    {
        ownedCharacters.Clear();
        deckCharacters.Clear();
        
        for (int i = 0; i < sharedSlotData200.Length; i++)
        {
            sharedSlotData200[i] = null;
        }
        
        PlayerPrefs.DeleteKey(PLAYER_PREFS_OWNED_KEY);
        PlayerPrefs.Save();
        
        LogDebug("모든 데이터 초기화됨");
    }
    
    // 로그 헬퍼 메서드
    private void LogDebug(string message)
    {
        if (enableDetailedLogs)
        {
            Debug.Log($"[ImprovedCharInventory] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[ImprovedCharInventory] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[ImprovedCharInventory] {message}");
    }
    
    // 직렬화 클래스
    [System.Serializable]
    private class SaveData
    {
        public string name;
        public int level;
        public int currentExp;
        public bool isInDeck;
    }
    
    [System.Serializable]
    private class SerializationWrapper
    {
        public List<SaveData> characters;
    }
}