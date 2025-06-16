using System.Collections.Generic;
using UnityEngine;

public class CharacterInventoryManager : MonoBehaviour
{
    [Header("ScriptableObject DB 참조")]
    [SerializeField] public CharacterDatabaseObject characterDatabaseObject;

    [SerializeField] private List<CharacterData> ownedCharacters = new List<CharacterData>();
    private List<CharacterData> deckCharacters = new List<CharacterData>();

    private List<CharacterData> gachaPool = new List<CharacterData>();

    // ===============================
    // 200칸짜리 공유 배열 (인벤토리)
    // ===============================
    public CharacterData[] sharedSlotData200 = new CharacterData[200];

    private const string PLAYER_PREFS_OWNED_KEY = "OwnedCharactersJsonV2";

    private void Awake()
    {
        if (characterDatabaseObject == null)
        {
            Debug.LogError("[CharacterInventoryManager] DB가 연결되지 않았습니다!");
            return;
        }

        // 가챠 풀 구성
        gachaPool.Clear();
        foreach (var cData in characterDatabaseObject.characters)
        {
            if (cData != null)
                gachaPool.Add(cData);
        }
        Debug.Log($"[CharacterInventoryManager] gachaPool 구성 완료: {gachaPool.Count}개");

        // 저장된 데이터 로드
        LoadCharacters();
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
        CharacterData copy = new CharacterData
        {
            characterName = template.characterName,
            attackPower = template.attackPower,
            attackSpeed = template.attackSpeed,
            attackRange = template.attackRange,
            maxHP = template.maxHP,
            moveSpeed = template.moveSpeed,
            rangeType = template.rangeType,
            isAreaAttack = template.isAreaAttack,
            isBuffSupport = template.isBuffSupport,
            spawnPrefab = template.spawnPrefab,
            buttonIcon = template.buttonIcon,
            cost = template.cost,
            level = template.level,
            currentExp = 0,
            expToNextLevel = template.expToNextLevel,
            initialStar = template.initialStar,
            race = template.race,
            isFreeSlotOnly = template.isFreeSlotOnly,
            frontSprite = template.frontSprite,
            backSprite = template.backSprite,
            areaAttackRadius = template.areaAttackRadius,
            motionPrefab = template.motionPrefab
        };
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
        return new List<CharacterData>(ownedCharacters);
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
        ownedCharacters.Clear();
        deckCharacters.Clear();

        if (!PlayerPrefs.HasKey(PLAYER_PREFS_OWNED_KEY))
        {
            Debug.Log("[CharacterInventoryManager] 저장된 데이터 없음. 기본 로드 실행.");
            
            // 기본 캐릭터 추가 (8개의 RandomChar)
            int addedCount = 0;
            for (int i = 1; i <= 8; i++)
            {
                string charName = $"RandomChar_{i}";
                var template = FindTemplateByName(charName);
                
                if (template != null)
                {
                    CharacterData newChar = CreateNewCharacter(template);
                    ownedCharacters.Add(newChar);
                    addedCount++;
                    Debug.Log($"[CharacterInventoryManager] 기본 캐릭터 추가: {charName}");
                }
                else
                {
                    Debug.LogWarning($"[CharacterInventoryManager] 템플릿을 찾을 수 없음: {charName}");
                }
            }
            
            Debug.Log($"[CharacterInventoryManager] 총 {addedCount}개의 기본 캐릭터 추가됨");
            
            // sharedSlotData200 초기화
            for (int i = 0; i < ownedCharacters.Count && i < sharedSlotData200.Length; i++)
            {
                sharedSlotData200[i] = ownedCharacters[i];
            }
            
            // 초기 데이터 저장
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
                loadedChar.currentExp = (float)sd.currentExp;

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