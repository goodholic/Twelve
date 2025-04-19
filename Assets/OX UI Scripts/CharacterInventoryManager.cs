using System.Collections.Generic;
using UnityEngine;

public class CharacterInventoryManager : MonoBehaviour
{
    [Header("ScriptableObject DB 참조")]
    [SerializeField] private CharacterDatabaseObject characterDatabaseObject;

    // ===========================
    // 인벤토리/덱 캐릭터 목록
    // ===========================
    [SerializeField] private List<CharacterData> ownedCharacters = new List<CharacterData>();
    private List<CharacterData> deckCharacters = new List<CharacterData>();

    // 뽑기(Gacha) 풀
    private List<CharacterData> gachaPool = new List<CharacterData>();

    // =============================
    //  공용 20칸 데이터 배열
    // =============================
    public CharacterData[] sharedSlotData20 = new CharacterData[20];

    // PlayerPrefs 키값
    private const string PLAYER_PREFS_OWNED_KEY = "OwnedCharactersJsonV2";

    private void Awake()
    {
        if (characterDatabaseObject == null)
        {
            Debug.LogError("[CharacterInventoryManager] DB가 연결되지 않았습니다!");
            return;
        }

        // gachaPool 구성
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

    /// <summary>
    /// 현재 덱(Deck)에 들어있는 캐릭터들을 그대로 반환
    /// </summary>
    public List<CharacterData> GetDeckCharacters()
    {
        // (DeckPanelManager가 'SyncRegisteredSet2WithDeck'에서 사용)
        return new List<CharacterData>(deckCharacters);
    }

    /// <summary>
    /// 무작위 뽑기
    /// </summary>
    public CharacterData DrawRandomCharacter()
    {
        if (gachaPool.Count == 0)
        {
            Debug.LogWarning("[CharacterInventoryManager] gachaPool이 비어있음 -> 뽑기 불가");
            return null;
        }

        int randIdx = Random.Range(0, gachaPool.Count);
        CharacterData template = gachaPool[randIdx];
        // 템플릿 복제
        CharacterData newChar = CreateNewCharacter(template);

        // 인벤토리에 추가
        ownedCharacters.Add(newChar);
        Debug.Log($"[CharacterInventoryManager] 뽑기 결과: {newChar.characterName}");

        return newChar;
    }

    private CharacterData CreateNewCharacter(CharacterData template)
    {
        CharacterData copy = new CharacterData
        {
            characterName = template.characterName,
            attackPower   = template.attackPower,
            rangeType     = template.rangeType,
            isAreaAttack  = template.isAreaAttack,
            isBuffSupport = template.isBuffSupport,
            spawnPrefab   = template.spawnPrefab,
            buttonIcon    = template.buttonIcon,
            cost          = template.cost,

            // 레벨/경험치
            level         = template.level,
            currentExp    = 0,
            expToNextLevel= template.expToNextLevel,
        };
        return copy;
    }

    // ===========================
    // 인벤토리 <-> 덱 이동
    // ===========================
    public void MoveToDeck(CharacterData c)
    {
        if (c == null) return;
        if (ownedCharacters.Contains(c))
        {
            ownedCharacters.Remove(c);
            deckCharacters.Add(c);
            Debug.Log($"[CharacterInventoryManager] 인벤토리 -> 덱 이동: {c.characterName}");
        }
        else
        {
            Debug.LogWarning($"[CharacterInventoryManager] 인벤토리에 없는 캐릭터({c.characterName})");
        }
    }

    public void MoveToInventory(CharacterData c)
    {
        if (c == null) return;
        if (deckCharacters.Contains(c))
        {
            deckCharacters.Remove(c);
            ownedCharacters.Add(c);
            Debug.Log($"[CharacterInventoryManager] 덱 -> 인벤토리 이동: {c.characterName}");
        }
        else
        {
            Debug.LogWarning($"[CharacterInventoryManager] 덱에 없는 캐릭터({c.characterName})");
        }
    }

    // ===========================
    // 인벤토리 목록 등
    // ===========================
    public List<CharacterData> GetOwnedCharacters()
    {
        return new List<CharacterData>(ownedCharacters);
    }

    // 전체(인벤토리+덱)
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
            if (ownedCharacters.Remove(c))
            {
                Debug.Log($"[CharacterInventoryManager] 인벤토리에서 제거: {c.characterName}");
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
            ownedCharacters.Add(c);
            Debug.Log($"[CharacterInventoryManager] 인벤토리에 추가: {c.characterName}");
        }
    }

    public void ConsumeCharactersForUpgrade(List<CharacterData> charsToConsume)
    {
        foreach (var c in charsToConsume)
        {
            if (ownedCharacters.Remove(c))
            {
                Debug.Log($"[CharacterInventoryManager] 업그레이드로 인벤토리 제거: {c.characterName}");
            }
            else if (deckCharacters.Remove(c))
            {
                Debug.Log($"[CharacterInventoryManager] 업그레이드로 덱 제거: {c.characterName}");
            }
            else
            {
                Debug.LogWarning($"[CharacterInventoryManager] 없음: {c.characterName}");
            }
        }
    }

    // ===========================
    // JSON 저장/로드
    // ===========================
    public void SaveCharacters()
    {
        List<CharacterRecord> recordList = new List<CharacterRecord>();

        foreach (var c in ownedCharacters)
        {
            recordList.Add(new CharacterRecord
            {
                characterName = c.characterName,
                level         = c.level,
                currentExp    = c.currentExp,
                isInDeck      = false
            });
        }

        foreach (var c in deckCharacters)
        {
            recordList.Add(new CharacterRecord
            {
                characterName = c.characterName,
                level         = c.level,
                currentExp    = c.currentExp,
                isInDeck      = true
            });
        }

        CharacterRecordWrapper wrapper = new CharacterRecordWrapper { records = recordList };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(PLAYER_PREFS_OWNED_KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"[CharacterInventoryManager] SaveCharacters() 완료. totalCount={recordList.Count}");
    }

    public void LoadCharacters()
    {
        string json = PlayerPrefs.GetString(PLAYER_PREFS_OWNED_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[CharacterInventoryManager] 저장된 데이터 없음");
            return;
        }

        CharacterRecordWrapper wrapper = JsonUtility.FromJson<CharacterRecordWrapper>(json);
        if (wrapper == null || wrapper.records == null)
        {
            Debug.LogWarning("[CharacterInventoryManager] LoadCharacters() 실패");
            return;
        }

        ownedCharacters.Clear();
        deckCharacters.Clear();

        foreach (var rec in wrapper.records)
        {
            CharacterData template = FindTemplateByName(rec.characterName);
            if (template == null)
            {
                Debug.LogWarning($"[CharacterInventoryManager] DB에 없는 캐릭터({rec.characterName})");
                continue;
            }

            // 복제
            CharacterData newChar = CreateNewCharacter(template);

            // 레벨/경험치 반영
            newChar.level      = rec.level;
            newChar.currentExp = rec.currentExp;

            // 덱 여부
            if (rec.isInDeck)
                deckCharacters.Add(newChar);
            else
                ownedCharacters.Add(newChar);
        }

        Debug.Log($"[CharacterInventoryManager] LoadCharacters() 완료. "
                + $"인벤토리={ownedCharacters.Count}, 덱={deckCharacters.Count}");
    }

    public CharacterData FindTemplateByName(string name)
    {
        if (characterDatabaseObject == null || characterDatabaseObject.characters == null)
            return null;

        foreach (var c in characterDatabaseObject.characters)
        {
            if (c != null && c.characterName == name)
                return c;
        }
        return null;
    }
}

// 저장용 구조체
[System.Serializable]
public class CharacterRecord
{
    public string characterName;
    public int level;
    public int currentExp;
    public bool isInDeck;
}

[System.Serializable]
public class CharacterRecordWrapper
{
    public List<CharacterRecord> records;
}
