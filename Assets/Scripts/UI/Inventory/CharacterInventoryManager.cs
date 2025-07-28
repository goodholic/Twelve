using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using GuildMaster.Data;

public class CharacterInventoryManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static CharacterInventoryManager instance;
    public static CharacterInventoryManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CharacterInventoryManager>();
                if (instance == null)
                {
                    Debug.LogWarning("[CharacterInventoryManager] Scene에 인스턴스가 없어 자동 생성합니다.");
                    
                    // 자동으로 GameObject 생성
                    GameObject go = new GameObject("CharacterInventoryManager");
                    instance = go.AddComponent<CharacterInventoryManager>();
                    
                    // DontDestroyOnLoad 설정으로 Scene 간 유지
                    DontDestroyOnLoad(go);
                    
                    Debug.Log("[CharacterInventoryManager] 자동 생성 완료 및 DontDestroyOnLoad 설정됨");
                }
            }
            return instance;
        }
    }

    [Header("ScriptableObject DB 참조")]
    [SerializeField] public CharacterDatabaseSO characterDatabaseObject;
    
    /// <summary>
    /// 다른 매니저들이 데이터베이스에 접근할 수 있도록 하는 public 프로퍼티
    /// </summary>
    public CharacterDatabaseSO GetCharacterDatabase()
    {
        return characterDatabaseObject;
    }

    [SerializeField] private List<CharacterData> ownedCharacters = new List<CharacterData>();
    private List<CharacterData> deckCharacters = new List<CharacterData>();

    private List<CharacterData> gachaPool = new List<CharacterData>();
    
    // 초기화 상태 확인 플래그
    private bool isInitialized = false;

    // ===============================
    // 200칸짜리 공유 배열 (인벤토리)
    // ===============================
    public CharacterData[] sharedSlotData200 = new CharacterData[200];

    private const string PLAYER_PREFS_OWNED_KEY = "OwnedCharactersJsonV2";

    private void Awake()
    {
        // 싱글톤 초기화
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        // Scene 간 유지 설정
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("[CharacterInventoryManager] ===== AWAKE CALLED =====");
        
        Debug.Log($"[CharacterInventoryManager] characterDatabaseObject null 체크: {characterDatabaseObject == null}");
        
        if (characterDatabaseObject == null)
        {
            Debug.LogWarning("[CharacterInventoryManager] characterDatabaseObject가 null입니다. 자동 할당을 시도합니다.");
            
            // 1. Resources에서 CharacterDatabase 찾기
            characterDatabaseObject = Resources.Load<CharacterDatabaseSO>("Data/CharacterDatabase");
            
            if (characterDatabaseObject == null)
            {
                // 2. 기본 경로에서 찾기
                #if UNITY_EDITOR
                characterDatabaseObject = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>("Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset");
                #endif
            }
            
            if (characterDatabaseObject == null)
            {
                // 3. FindObjectOfType으로 Scene에서 찾기
                characterDatabaseObject = FindFirstObjectByType<CharacterDatabaseSO>();
            }
            
            if (characterDatabaseObject == null)
            {
                Debug.LogError("[CharacterInventoryManager] 모든 방법으로 CharacterDatabaseSO를 찾을 수 없습니다!");
                Debug.LogError("[CharacterInventoryManager] characterDatabaseObject가 null이므로 Awake() 종료");
                return;
            }
            else
            {
                Debug.Log($"[CharacterInventoryManager] CharacterDatabaseSO 자동 할당 성공: {characterDatabaseObject.name}");
            }
        }
        
        Debug.Log("[CharacterInventoryManager] DB 연결 확인됨, 계속 진행...");

        Debug.Log($"[CharacterInventoryManager] 데이터베이스 연결됨: {characterDatabaseObject.name}");
        Debug.Log($"[CharacterInventoryManager] 데이터베이스 캐릭터 수: {characterDatabaseObject.characters.Count}");
        Debug.Log($"[CharacterInventoryManager] characterDatabaseObject.characters가 null인가: {characterDatabaseObject.characters == null}");

        // 가챠 풀 구성
        gachaPool.Clear();
        
        // 먼저 characters 배열 확인
        bool foundCharacters = false;
        if (characterDatabaseObject.characters != null && characterDatabaseObject.characters.Count > 0)
        {
            foreach (var cData in characterDatabaseObject.characters)
            {
                if (cData != null)
                {
                    gachaPool.Add(ConvertToCharacterData(cData));
                    Debug.LogError($"[CharacterInventoryManager] 가챠풀에 추가 (characters): {cData.characterName}");
                    foundCharacters = true;
                }
                else
                {
                    Debug.LogError($"[CharacterInventoryManager] null 캐릭터 데이터 발견");
                }
            }
        }
        
        // characters 배열이 비어있다면 tacticalCharacters 배열 확인
        if (!foundCharacters && characterDatabaseObject.tacticalCharacters != null && characterDatabaseObject.tacticalCharacters.Count > 0)
        {
            Debug.Log($"[CharacterInventoryManager] characters 배열이 비어있음, tacticalCharacters 배열 사용: {characterDatabaseObject.tacticalCharacters.Count}개");
            foreach (var tacticalData in characterDatabaseObject.tacticalCharacters)
            {
                if (tacticalData != null)
                {
                    // TacticalCharacterData를 CharacterData로 변환하여 가챠풀에 추가
                    var convertedData = ConvertTacticalToCharacterData(tacticalData);
                    if (convertedData != null)
                    {
                        gachaPool.Add(convertedData);
                        Debug.Log($"[CharacterInventoryManager] 가챠풀에 추가 (tacticalCharacters): {convertedData.characterName}");
                        foundCharacters = true;
                    }
                }
            }
        }
        
        if (!foundCharacters)
        {
            Debug.LogError($"[CharacterInventoryManager] 모든 캐릭터 배열이 비어있거나 null입니다!");
            Debug.LogError($"[CharacterInventoryManager] characters 길이: {(characterDatabaseObject.characters != null ? characterDatabaseObject.characters.Count : -1)}");
            Debug.LogError($"[CharacterInventoryManager] tacticalCharacters 길이: {(characterDatabaseObject.tacticalCharacters != null ? characterDatabaseObject.tacticalCharacters.Count : -1)}");
        }
        
        // 가챠풀이 비어있다면 대안 로딩 방법들 시도 (배열이 존재하든 안하든 관계없이)
        if (gachaPool.Count == 0)
        {
            Debug.LogError("[CharacterInventoryManager] 가챠풀이 비어있음 - 대안 로딩 방법들 시도 중...");
            
            // 1) Resources에서 로드 시도
            var resourceDB = Resources.Load<CharacterDatabaseSO>("CharacterDatabase");
            if (resourceDB != null && resourceDB.characters != null && resourceDB.characters.Count > 0)
            {
                Debug.LogError($"[CharacterInventoryManager] Resources에서 발견됨: {resourceDB.characters.Count}개 캐릭터");
                characterDatabaseObject = resourceDB;
                
                foreach (var cData in resourceDB.characters)
                {
                    if (cData != null)
                    {
                        gachaPool.Add(ConvertToCharacterData(cData));
                        Debug.LogError($"[CharacterInventoryManager] 리소스에서 가챠풀에 추가: {cData.characterName}");
                    }
                }
            }
            
            // 여전히 비어있다면 2) 직접 에셋 경로로 로드 시도
            if (gachaPool.Count == 0)
            {
                Debug.LogError("[CharacterInventoryManager] 직접 에셋 로드 시도...");
                var directDB = UnityEngine.Resources.LoadAll<CharacterDatabaseSO>("");
                if (directDB != null && directDB.Length > 0)
                {
                    foreach (var db in directDB)
                    {
                        if (db != null && db.characters != null && db.characters.Count > 0)
                        {
                            bool hasValidCharacters = false;
                            foreach (var char_ in db.characters)
                            {
                                if (char_ != null) hasValidCharacters = true;
                            }
                            
                            if (hasValidCharacters)
                            {
                                Debug.LogError($"[CharacterInventoryManager] 유효한 DB 발견: {db.name}");
                                characterDatabaseObject = db;
                                
                                                    foreach (var cData in db.characters)
                    {
                        if (cData != null)
                        {
                            gachaPool.Add(ConvertToCharacterData(cData));
                                        Debug.LogError($"[CharacterInventoryManager] 직접로드에서 가챠풀에 추가: {cData.characterName}");
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            
            // 3) 최후의 수단: 기본 캐릭터 생성
            if (gachaPool.Count == 0)
            {
                Debug.LogError("[CharacterInventoryManager] 모든 로드 방법 실패 - 기본 캐릭터 생성");
                CreateDefaultCharacters();
            }
        }
        Debug.Log($"[CharacterInventoryManager] gachaPool 구성 완료: {gachaPool.Count}개");

        // 저장된 데이터 로드
        Debug.Log("[CharacterInventoryManager] LoadCharacters() 호출");
        LoadCharacters();
        
        // 초기화 완료 플래그 설정
        isInitialized = true;
        
        Debug.Log($"[CharacterInventoryManager] Awake() 완료 - 인벤토리: {ownedCharacters.Count}개, 초기화 상태: {isInitialized}");
    }

    // --------------------------------------
    //  모든 데이터 초기화 메서드
    // --------------------------------------
    public void ClearAllData()
    {
        Debug.Log("[CharacterInventoryManager] ClearAllData() 호출 -> 인벤토리 및 덱 초기화 & PlayerPrefs Key 삭제");

        // 1) 현재 메모리상 데이터 초기화
        ownedCharacters.Clear();
        deckCharacters.Clear();
        for (int i = 0; i < sharedSlotData200.Length; i++)
        {
            sharedSlotData200[i] = null;
        }

        // 2) PlayerPrefs 에 저장된 OwnedCharactersJsonV2 키 삭제
        PlayerPrefs.DeleteKey(PLAYER_PREFS_OWNED_KEY);
        PlayerPrefs.Save();
    }

    // ========================================================
    // (1) sharedSlotData200에서 
    //     "이름이 빈칸이고 spawnPrefab도 없고 buttonIcon도 없으면 null 취급" + 재정렬
    // ========================================================
    /// <summary>
    /// sharedSlotData200을 "null이 아니고, (캐릭터 이름/프리팹/아이콘) 조건을 만족하지 않는" 캐릭터만
    /// 순서대로 추출하여 앞으로 모으고, 나머지는 null로 채운다.
    /// 
    /// - '이름이 ""(빈 문자열)이고 spawnPrefab==null 이고 buttonIcon==null' 이면 => null 취급(제거).
    /// </summary>
    public void CondenseAndReorderSharedSlots()
    {
        if (sharedSlotData200 == null)
        {
            Debug.LogError("[CharacterInventoryManager] sharedSlotData200이 null입니다!");
            return;
        }

        List<CharacterData> temp = new List<CharacterData>();

        // 1) 현재 sharedSlotData200 스캔
        for (int i = 0; i < sharedSlotData200.Length; i++)
        {
            if (i >= 0 && i < sharedSlotData200.Length)
            {
                CharacterData cd = sharedSlotData200[i];
                if (cd != null)
                {
                    // ---------------------------------------------------------
                    // 새 로직: 캐릭터 이름이 빈칸 + spawnPrefab==null + buttonIcon==null
                    // ---------------------------------------------------------
                    bool allEmpty = (string.IsNullOrEmpty(cd.characterName) || cd.characterName == "")
                                    && cd.spawnPrefab == null
                                    && cd.buttonIcon == null;

                    if (!allEmpty)
                    {
                        // 세 조건이 동시에 만족하지 않으면 => 유효한 캐릭터로 간주
                        temp.Add(cd);
                    }
                    else
                    {
                        // allEmpty==true => "null 취급"해서 버림
                        Debug.Log($"[CharacterInventoryManager] '{cd.characterName}' 은(는) 이름/프리팹/아이콘 다 없으므로 제거됨");
                    }
                }
            }
        }

        // 2) sharedSlotData200을 앞에서부터 temp 내용으로 채우고 나머진 null
        for (int i = 0; i < sharedSlotData200.Length; i++)
        {
            if (i < temp.Count)
                sharedSlotData200[i] = temp[i];
            else
                sharedSlotData200[i] = null;
        }
    }

    /// <summary>
    /// sharedSlotData200 내용을 기반으로 ownedCharacters를 재구성(동기화).
    /// => "UI 순서"가 곧 인벤토리 순서가 되도록 맞춘다.
    /// </summary>
    public void SyncOwnedFromSharedSlots()
    {
        // 먼저 ownedCharacters를 싹 비운 뒤,
        // sharedSlotData200에 들어있는(=null아닌) 캐릭터만 다시 담는다.
        // => 이 과정을 통해 "sharedSlotData200"의 순서 = ownedCharacters의 순서
        ownedCharacters.Clear();
        
        if (sharedSlotData200 == null)
        {
            Debug.LogError("[CharacterInventoryManager] sharedSlotData200이 null입니다!");
            return;
        }
        
        for (int i = 0; i < sharedSlotData200.Length; i++)
        {
            if (i >= 0 && i < sharedSlotData200.Length)
            {
                CharacterData cd = sharedSlotData200[i];
                if (cd != null)
                {
                    ownedCharacters.Add(cd);
                }
            }
        }
    }

    public List<CharacterData> GetDeckCharacters()
    {
        return new List<CharacterData>(deckCharacters);
    }

    // ===================================================================
    // (2) DrawRandomCharacter(): 뽑힌 캐릭터를 sharedSlotData200에도 삽입
    // ===================================================================
    public CharacterData DrawRandomCharacter()
    {
        if (gachaPool.Count == 0)
        {
            Debug.LogWarning("[CharacterInventoryManager] gachaPool이 비어있음 -> 뽑기 불가");
            return null;
        }

        int randIdx = Random.Range(0, gachaPool.Count);
        CharacterData template = gachaPool[randIdx];
        CharacterData newChar = CreateNewCharacter(template);

        // 1) 뽑힌 캐릭터를 ownedCharacters에 추가
        ownedCharacters.Add(newChar);

        // 2) sharedSlotData200에서 첫 번째로 null인 자리에 삽입
        int firstEmptyIndex = -1;
        
        if (sharedSlotData200 == null)
        {
            Debug.LogError("[CharacterInventoryManager] sharedSlotData200이 null입니다!");
            return newChar;
        }
        
        // 전체 200개 슬롯을 검사
        for (int i = 0; i < sharedSlotData200.Length; i++)
        {
            if (i < sharedSlotData200.Length && sharedSlotData200[i] == null)
            {
                firstEmptyIndex = i;
                break;
            }
        }
        
        if (firstEmptyIndex >= 0 && firstEmptyIndex < sharedSlotData200.Length)
        {
            sharedSlotData200[firstEmptyIndex] = newChar;
            Debug.Log($"[CharacterInventoryManager] 뽑기 결과: {newChar.characterName} => sharedSlotData200[{firstEmptyIndex}]에 배치");
        }
        else
        {
            Debug.LogWarning($"[CharacterInventoryManager] 인벤토리 200칸이 모두 가득 찼습니다! (sharedSlotData200에 삽입 실패)");
        }

        return newChar;
    }

    private CharacterData CreateNewCharacter(CharacterData template)
    {
        CharacterData copy = ScriptableObject.CreateInstance<CharacterData>();
        copy.characterName = template.characterName;
        copy.rangeType = template.rangeType;
        copy.isAreaAttack = template.isAreaAttack;
        copy.isBuffSupport = template.isBuffSupport;
        copy.spawnPrefab = template.spawnPrefab;
        copy.buttonIcon = template.buttonIcon;
        copy.level = template.level;
        copy.currentExp = 0;
        copy.initialStar = template.initialStar;
        copy.race = template.race;
        copy.isFreeSlotOnly = template.isFreeSlotOnly;
        copy.frontSprite = template.frontSprite;
        copy.backSprite = template.backSprite;
        copy.areaAttackRadius = template.areaAttackRadius;
        copy.motionPrefab = template.motionPrefab;
        
        // Setter가 있는 속성들을 나중에 설정
        copy.attackPower = template.attackPower;
        copy.attackSpeed = template.attackSpeed;
        copy.attackRange = template.attackRange;
        copy.maxHP = template.maxHP;
        copy.expToNextLevel = template.expToNextLevel;
        return copy;
    }

    public void MoveToDeck(CharacterData c)
    {
        if (c == null) return;
        
        // 1) ownedCharacters에서 제거 (참조가 다를 수 있으므로 이름으로도 검사)
        bool removed = false;
        for (int i = ownedCharacters.Count - 1; i >= 0; i--)
        {
            if (ownedCharacters[i] == c || 
                (ownedCharacters[i] != null && ownedCharacters[i].characterName == c.characterName))
            {
                ownedCharacters.RemoveAt(i);
                removed = true;
                
                // 2) sharedSlotData200에서도 제거 (중요!)
                for (int j = 0; j < sharedSlotData200.Length; j++)
                {
                    if (sharedSlotData200[j] == c || 
                        (sharedSlotData200[j] != null && sharedSlotData200[j].characterName == c.characterName))
                    {
                        sharedSlotData200[j] = null;
                        Debug.Log($"[CharacterInventoryManager] sharedSlotData200[{j}]에서 {c.characterName} 제거");
                        break;
                    }
                }
                
                // 3) deckCharacters에 추가
                deckCharacters.Add(c);
                Debug.Log($"[CharacterInventoryManager] 인벤토리 -> 덱 이동: {c.characterName}");
                
                // 4) 자동 정렬 호출 (★ 추가된 부분 ★)
                CondenseAndReorderSharedSlots();
                SyncOwnedFromSharedSlots();
                break;
            }
        }
        
        if (!removed)
        {
            Debug.LogWarning($"[CharacterInventoryManager] 인벤토리에 없는 캐릭터({c.characterName})");
        }
    }

    public void MoveToInventory(CharacterData c)
    {
        if (c == null) return;
        
        if (deckCharacters.Contains(c))
        {
            // 1) deckCharacters에서 제거
            deckCharacters.Remove(c);
            
            // 2) ownedCharacters에 추가
            ownedCharacters.Add(c);
            
            // 3) sharedSlotData200의 첫 번째 빈 공간에 추가
            int firstEmptyIndex = -1;
            for (int i = 0; i < sharedSlotData200.Length; i++)
            {
                if (sharedSlotData200[i] == null)
                {
                    firstEmptyIndex = i;
                    break;
                }
            }
            
            if (firstEmptyIndex >= 0 && firstEmptyIndex < sharedSlotData200.Length)
            {
                sharedSlotData200[firstEmptyIndex] = c;
                Debug.Log($"[CharacterInventoryManager] {c.characterName} => sharedSlotData200[{firstEmptyIndex}]에 복원");
            }
            else
            {
                Debug.LogWarning($"[CharacterInventoryManager] 인벤토리가 가득 차서 {c.characterName}을 sharedSlotData200에 넣을 수 없음");
            }
            
            Debug.Log($"[CharacterInventoryManager] 덱 -> 인벤토리 이동: {c.characterName}");
            
            // 4) 자동 정렬 호출
            CondenseAndReorderSharedSlots();
            SyncOwnedFromSharedSlots();
        }
        else
        {
            Debug.LogWarning($"[CharacterInventoryManager] 덱에 없는 캐릭터({c.characterName})");
        }
    }

    public List<CharacterData> GetOwnedCharacters()
    {
        Debug.Log($"[CharacterInventoryManager] ===== GetOwnedCharacters() 호출 =====");
        
        // 완전 초기화 강제 실행
        if (!isInitialized)
        {
            Debug.LogWarning("[CharacterInventoryManager] 초기화되지 않은 상태 - 강제 완전 초기화 실행");
            ForceCompleteInitialization();
        }
        
        Debug.Log($"[CharacterInventoryManager] 현재 ownedCharacters.Count: {ownedCharacters.Count}");
        Debug.Log($"[CharacterInventoryManager] 현재 gachaPool.Count: {gachaPool.Count}");
        
        // 여전히 소유 캐릭터가 없다면 기본 캐릭터 생성
        if (ownedCharacters.Count == 0 && gachaPool.Count > 0)
        {
            Debug.LogWarning("[CharacterInventoryManager] 소유 캐릭터가 없어 기본 캐릭터 생성");
            CreateDefaultOwnedCharacters();
        }
        
        for (int i = 0; i < ownedCharacters.Count && i < 5; i++)
        {
            Debug.Log($"[CharacterInventoryManager] ownedCharacters[{i}]: {ownedCharacters[i]?.characterName ?? "null"}");
        }
        return new List<CharacterData>(ownedCharacters);
    }
    
    /// <summary>
    /// 강제 완전 초기화 - Awake()가 호출되지 않은 경우 사용
    /// </summary>
    private void ForceCompleteInitialization()
    {
        Debug.LogWarning("[CharacterInventoryManager] ForceCompleteInitialization() 시작");
        
        try
        {
            // 1. Database 자동 할당
            if (characterDatabaseObject == null)
            {
                Debug.LogWarning("[CharacterInventoryManager] Database 자동 할당 시도");
                
                // Resources에서 찾기
                characterDatabaseObject = Resources.Load<CharacterDatabaseSO>("Data/CharacterDatabase");
                
                if (characterDatabaseObject == null)
                {
                    // 기본 경로에서 찾기
                    #if UNITY_EDITOR
                    characterDatabaseObject = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>("Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset");
                    #endif
                }
                
                if (characterDatabaseObject != null)
                {
                    Debug.Log($"[CharacterInventoryManager] Database 자동 할당 성공: {characterDatabaseObject.name}");
                }
                else
                {
                    Debug.LogError("[CharacterInventoryManager] Database 자동 할당 실패");
                    return;
                }
            }
            
            // 2. 가챠풀 구성
            gachaPool.Clear();
            if (characterDatabaseObject.characters != null && characterDatabaseObject.characters.Count > 0)
            {
                foreach (var cData in characterDatabaseObject.characters)
                {
                    if (cData != null)
                    {
                        gachaPool.Add(ConvertToCharacterData(cData));
                    }
                }
            }
            else if (characterDatabaseObject.tacticalCharacters != null && characterDatabaseObject.tacticalCharacters.Count > 0)
            {
                foreach (var tacticalData in characterDatabaseObject.tacticalCharacters)
                {
                    if (tacticalData != null)
                    {
                        gachaPool.Add(tacticalData);
                    }
                }
            }
            
            Debug.Log($"[CharacterInventoryManager] 강제 초기화 - gachaPool 구성 완료: {gachaPool.Count}개");
            
            // 3. 캐릭터 로드
            LoadCharacters();
            
            // 4. 초기화 완료 표시
            isInitialized = true;
            
            Debug.Log($"[CharacterInventoryManager] 강제 완전 초기화 완료 - 소유 캐릭터: {ownedCharacters.Count}개");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CharacterInventoryManager] 강제 초기화 중 오류: {e.Message}");
        }
    }
    
    /// <summary>
    /// 기본 소유 캐릭터 생성 (gachaPool에서 처음 8개)
    /// </summary>
    private void CreateDefaultOwnedCharacters()
    {
        if (gachaPool.Count == 0)
        {
            Debug.LogWarning("[CharacterInventoryManager] gachaPool이 비어있어 기본 캐릭터 생성 불가");
            return;
        }
        
        Debug.Log($"[CharacterInventoryManager] 기본 소유 캐릭터 생성 시작 - gachaPool: {gachaPool.Count}개");
        
        int maxCharacters = Mathf.Min(8, gachaPool.Count);
        for (int i = 0; i < maxCharacters; i++)
        {
            if (gachaPool[i] != null)
            {
                CharacterData newChar = CreateNewCharacter(gachaPool[i]);
                ownedCharacters.Add(newChar);
                Debug.Log($"[CharacterInventoryManager] 기본 캐릭터 추가: {gachaPool[i].characterName}");
            }
        }
        
        // sharedSlotData200 업데이트
        for (int i = 0; i < ownedCharacters.Count && i < sharedSlotData200.Length; i++)
        {
            sharedSlotData200[i] = ownedCharacters[i];
        }
        
        // 저장
        SaveCharacters();
        
        Debug.Log($"[CharacterInventoryManager] 기본 소유 캐릭터 생성 완료: {ownedCharacters.Count}개");
    }

    public List<CharacterData> GetAllCharactersWithDuplicates()
    {
        List<CharacterData> all = new List<CharacterData>();
        all.AddRange(ownedCharacters);
        all.AddRange(deckCharacters);
        return all;
    }

    public void RemoveFromInventory(CharacterData c)
    {
        if (c != null)
        {
            // 1) ownedCharacters에서 제거
            bool removed = false;
            
            // 참조가 다를 수 있으므로 이름으로도 검사
            for (int i = ownedCharacters.Count - 1; i >= 0; i--)
            {
                if (ownedCharacters[i] == c || 
                    (ownedCharacters[i] != null && ownedCharacters[i].characterName == c.characterName))
                {
                    ownedCharacters.RemoveAt(i);
                    removed = true;
                    Debug.Log($"[CharacterInventoryManager] 인벤토리에서 제거: {c.characterName}");
                    break;
                }
            }
            
            if (removed)
            {
                // 2) sharedSlotData200에서도 제거 (중요!)
                for (int i = 0; i < sharedSlotData200.Length; i++)
                {
                    if (sharedSlotData200[i] == c || 
                        (sharedSlotData200[i] != null && sharedSlotData200[i].characterName == c.characterName))
                    {
                        sharedSlotData200[i] = null;
                        Debug.Log($"[CharacterInventoryManager] sharedSlotData200[{i}]에서 {c.characterName} 제거");
                        break;
                    }
                }
                
                // 3) 자동 정렬 호출
                CondenseAndReorderSharedSlots();
                SyncOwnedFromSharedSlots();
            }
            else
            {
                Debug.LogWarning($"[CharacterInventoryManager] 인벤토리에 없는 캐릭터({c.characterName})");
            }
        }
    }

    public void RemoveFromDeck(CharacterData c)
    {
        if (c != null)
        {
            if (deckCharacters.Remove(c))
            {
                Debug.Log($"[CharacterInventoryManager] 덱에서 제거: {c.characterName}");
            }
            else
            {
                Debug.LogWarning($"[CharacterInventoryManager] 덱에 없는 캐릭터({c.characterName})");
            }
        }
    }

    public void AddToInventory(CharacterData c)
    {
        if (c != null)
        {
            // 1) ownedCharacters에 추가
            ownedCharacters.Add(c);
            
            // 2) sharedSlotData200의 첫 번째 빈 공간에 추가
            int firstEmptyIndex = -1;
            for (int i = 0; i < sharedSlotData200.Length; i++)
            {
                if (sharedSlotData200[i] == null)
                {
                    firstEmptyIndex = i;
                    break;
                }
            }
            
            if (firstEmptyIndex >= 0 && firstEmptyIndex < sharedSlotData200.Length)
            {
                sharedSlotData200[firstEmptyIndex] = c;
                Debug.Log($"[CharacterInventoryManager] {c.characterName} => sharedSlotData200[{firstEmptyIndex}]에 추가");
            }
            else
            {
                Debug.LogWarning($"[CharacterInventoryManager] 인벤토리가 가득 차서 {c.characterName}을 sharedSlotData200에 넣을 수 없음");
            }
            
            Debug.Log($"[CharacterInventoryManager] 인벤토리에 추가: {c.characterName}");
            
            // 3) 자동 정렬 호출
            CondenseAndReorderSharedSlots();
            SyncOwnedFromSharedSlots();
        }
    }

    public void ConsumeCharactersForUpgrade(List<CharacterData> charsToConsume)
    {
        foreach (var c in charsToConsume)
        {
            // ownedCharacters에서 제거 시도
            if (ownedCharacters.Remove(c))
            {
                Debug.Log($"[CharacterInventoryManager] 업그레이드로 인벤토리 제거: {c.characterName}");
                
                // sharedSlotData200에서도 제거
                for (int i = 0; i < sharedSlotData200.Length; i++)
                {
                    if (sharedSlotData200[i] == c)
                    {
                        sharedSlotData200[i] = null;
                        break;
                    }
                }
            }
            else if (deckCharacters.Remove(c))
            {
                Debug.Log($"[CharacterInventoryManager] 업그레이드로 덱 제거: {c.characterName}");
            }
        }
        
        // 업그레이드 후 자동 정렬
        CondenseAndReorderSharedSlots();
        SyncOwnedFromSharedSlots();
    }

    // ===================================================================
    // 저장/불러오기 관련
    // ===================================================================
    public void SaveCharacters()
    {
        var saveDataList = new List<SaveData>();

        // ownedCharacters 저장
        foreach (var c in ownedCharacters)
        {
            var sd = new SaveData
            {
                name = c.characterName,
                level = c.level,
                currentExp = (int)c.currentExp,
                isInDeck = false
            };
            saveDataList.Add(sd);
        }

        // deckCharacters 저장
        foreach (var c in deckCharacters)
        {
            var sd = new SaveData
            {
                name = c.characterName,
                level = c.level,
                currentExp = (int)c.currentExp,
                isInDeck = true
            };
            saveDataList.Add(sd);
        }

        string json = JsonUtility.ToJson(new SerializationWrapper { characters = saveDataList });
        PlayerPrefs.SetString(PLAYER_PREFS_OWNED_KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"[CharacterInventoryManager] 캐릭터 저장 완료. 인벤토리: {ownedCharacters.Count}, 덱: {deckCharacters.Count}");
    }

    private void LoadCharacters()
    {
        Debug.Log("[CharacterInventoryManager] ===== LoadCharacters() 시작 =====");
        ownedCharacters.Clear();
        deckCharacters.Clear();

        bool hasKey = PlayerPrefs.HasKey(PLAYER_PREFS_OWNED_KEY);
        Debug.Log($"[CharacterInventoryManager] PlayerPrefs 키 '{PLAYER_PREFS_OWNED_KEY}' 존재: {hasKey}");

        if (!hasKey)
        {
            Debug.LogError("[CharacterInventoryManager] 저장된 데이터 없음. 기본 로드 실행.");
            Debug.LogError($"[CharacterInventoryManager] gachaPool 개수: {gachaPool.Count}");
            
            // gachaPool에서 기본 캐릭터 추가 (최대 8개)
            int addedCount = 0;
            int maxCharacters = Mathf.Min(8, gachaPool.Count);
            
            for (int i = 0; i < maxCharacters; i++)
            {
                if (gachaPool[i] != null)
                {
                    CharacterData newChar = CreateNewCharacter(gachaPool[i]);
                    ownedCharacters.Add(newChar);
                    addedCount++;
                    Debug.Log($"[CharacterInventoryManager] 기본 캐릭터 추가 성공: {gachaPool[i].characterName}");
                }
                else
                {
                    Debug.LogWarning($"[CharacterInventoryManager] gachaPool[{i}]가 null입니다");
                }
            }
            
            // gachaPool이 비어있다면 이전 방식 시도 (RandomChar_ 형태)
            if (addedCount == 0 && gachaPool.Count == 0)
            {
                Debug.LogWarning("[CharacterInventoryManager] gachaPool이 비어있어 RandomChar_ 형태로 시도");
                for (int i = 0; i < 8; i++)
                {
                    string charName = $"RandomChar_{i}";
                    var template = FindTemplateByName(charName);
                    
                    if (template != null)
                    {
                        CharacterData newChar = CreateNewCharacter(template);
                        ownedCharacters.Add(newChar);
                        addedCount++;
                        Debug.Log($"[CharacterInventoryManager] RandomChar 추가 성공: {charName}");
                    }
                }
            }
            
            Debug.LogError($"[CharacterInventoryManager] 총 {addedCount}개의 기본 캐릭터 추가됨");
            Debug.LogError($"[CharacterInventoryManager] ownedCharacters.Count: {ownedCharacters.Count}");
            
            // sharedSlotData200 초기화
            for (int i = 0; i < ownedCharacters.Count && i < sharedSlotData200.Length; i++)
            {
                sharedSlotData200[i] = ownedCharacters[i];
                Debug.Log($"[CharacterInventoryManager] sharedSlotData200[{i}] = {ownedCharacters[i].characterName}");
            }
            
            // 초기 데이터 저장
            Debug.Log("[CharacterInventoryManager] 초기 데이터 저장 중...");
            SaveCharacters();
            
            return;
        }

        string json = PlayerPrefs.GetString(PLAYER_PREFS_OWNED_KEY);
        var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);

        if (wrapper != null && wrapper.characters != null)
        {
            foreach (var sd in wrapper.characters)
            {
                var template = FindTemplateByName(sd.name);
                if (template == null) 
                {
                    Debug.LogWarning($"[CharacterInventoryManager] 템플릿을 찾을 수 없음: {sd.name}");
                    continue;
                }

                var loadedChar = CreateNewCharacter(template);
                loadedChar.level = sd.level;
                loadedChar.currentExp = sd.currentExp;

                if (sd.isInDeck)
                {
                    deckCharacters.Add(loadedChar);
                }
                else
                {
                    ownedCharacters.Add(loadedChar);
                }
            }
        }

        // sharedSlotData200 재구성
        for (int i = 0; i < sharedSlotData200.Length; i++)
        {
            if (i < ownedCharacters.Count)
                sharedSlotData200[i] = ownedCharacters[i];
            else
                sharedSlotData200[i] = null;
        }

        Debug.Log($"[CharacterInventoryManager] 캐릭터 로드 완료. 인벤토리: {ownedCharacters.Count}, 덱: {deckCharacters.Count}");
    }

    private CharacterData FindTemplateByName(string name)
    {
        foreach (var c in gachaPool)
        {
            if (c.characterName == name)
                return c;
        }
        return null;
    }

    // CharacterDataSO를 CharacterData로 변환하는 헬퍼 메서드
    private CharacterData ConvertToCharacterData(CharacterDataSO characterDataSO)
    {
        if (characterDataSO == null) return null;
        
        CharacterData characterData = ScriptableObject.CreateInstance<CharacterData>();
        characterData.characterName = characterDataSO.characterName;
        characterData.characterIcon = characterDataSO.portrait;
        characterData.buttonIcon = characterDataSO.portrait;
        characterData.hp = characterDataSO.baseHP;
        characterData.attackPower = characterDataSO.baseAttack;
        characterData.level = characterDataSO.baseLevel;
        characterData.maxHP = characterDataSO.baseHP;
        characterData.health = characterDataSO.baseHP;
        
        return characterData;
    }

    // TacticalCharacterData를 CharacterData로 변환하는 헬퍼 메서드
    private CharacterData ConvertTacticalToCharacterData(CharacterData tacticalData)
    {
        // tacticalCharacters는 이미 CharacterData 타입이므로 그대로 반환
        return tacticalData;
    }

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

    /// <summary>
    /// 최후의 수단: 모든 데이터베이스 로드가 실패했을 때 기본 캐릭터들을 프로그래밍적으로 생성
    /// </summary>
    private void CreateDefaultCharacters()
    {
        Debug.LogError("[CharacterInventoryManager] CreateDefaultCharacters() 실행 - 기본 캐릭터들을 생성합니다");
        
        // 기본 캐릭터 8개 생성 (RandomChar_0 ~ RandomChar_7)
        for (int i = 0; i < 8; i++)
        {
            CharacterData defaultChar = new CharacterData();
            defaultChar.characterName = $"RandomChar_{i}";
            string rangeTypeString = i % 3 == 0 ? "Melee" : (i % 3 == 1 ? "Ranged" : "Magic");
            if (System.Enum.TryParse<RangeType>(rangeTypeString, out RangeType parsedRangeType))
                defaultChar.rangeType = parsedRangeType;
            else
                defaultChar.rangeType = RangeType.Melee;
            defaultChar.isAreaAttack = (i % 4 == 0);
            defaultChar.isBuffSupport = (i % 5 == 0);
            defaultChar.level = 1;
            defaultChar.currentExp = 0;
            defaultChar.initialStar = 1;
            defaultChar.race = (i % 3 == 0) ? "Human" : (i % 3 == 1) ? "Elf" : "Dwarf";
            defaultChar.isFreeSlotOnly = false;
            defaultChar.spawnPrefab = null;
            defaultChar.buttonIcon = null;
            defaultChar.frontSprite = null;
            defaultChar.backSprite = null;
            defaultChar.areaAttackRadius = 0f;
            defaultChar.motionPrefab = null;
            
            // Setter가 있는 속성들을 별도로 설정
            defaultChar.attackPower = 10 + (i * 2);
            defaultChar.attackSpeed = 1;
            defaultChar.attackRange = 1.5f;
            defaultChar.maxHP = 100;
            defaultChar.expToNextLevel = 100;
            
            gachaPool.Add(defaultChar);
            Debug.LogError($"[CharacterInventoryManager] 기본 캐릭터 생성됨: {defaultChar.characterName} (공격력: {defaultChar.attackPower}, 종족: {defaultChar.race})");
        }
        
        Debug.LogError($"[CharacterInventoryManager] 기본 캐릭터 {gachaPool.Count}개 생성 완료");
    }
    
    /// <summary>
    /// 인벤토리 디스플레이를 새로고침 (리사이클 등 외부 변경 시 호출)
    /// </summary>
    public void RefreshInventoryDisplay()
    {
        Debug.Log("[CharacterInventoryManager] RefreshInventoryDisplay() 호출됨");
        
        // sharedSlotData200 재정렬
        CondenseAndReorderSharedSlots();
        SyncOwnedFromSharedSlots();
        
        // 저장
        SaveCharacters();
        
        // UI 업데이트 이벤트 발생 (필요한 경우)
        // 예: OnInventoryUpdated?.Invoke();
    }
    
    /// <summary>
    /// 캐릭터 추가 (WaveRewardUI에서 사용)
    /// </summary>
    public void AddCharacter(CharacterData character)
    {
        if (character != null)
        {
            AddToInventory(character);
        }
    }
    
    /// <summary>
    /// 캐릭터 보유 여부 확인
    /// </summary>
    public bool IsCharacterOwned(int characterID)
    {
        foreach (var character in ownedCharacters)
        {
            if (character != null && character.characterIndex == characterID)
            {
                return true;
            }
        }
        
        foreach (var character in deckCharacters)
        {
            if (character != null && character.characterIndex == characterID)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 캐릭터 조각 추가 (중복 캐릭터 처리용)
    /// </summary>
    public void AddCharacterFragments(int characterID, int fragmentCount)
    {
        Debug.Log($"[CharacterInventoryManager] 캐릭터 ID {characterID}의 조각 {fragmentCount}개 추가");
        
        // 현재는 단순히 로그만 출력하고 나중에 조각 시스템 구현 시 확장 가능
        // TODO: 실제 조각 시스템 구현 시 여기에 조각 저장 로직 추가
        
        // 임시로 경험치나 다른 보상으로 대체할 수 있음
        // 예: 조각 1개당 골드 100 지급 등
    }
}