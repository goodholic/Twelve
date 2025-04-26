using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 서버 데이터베이스를 사용하므로, PlayerPrefs를 통한 로컬 저장/로드 로직은 전부 제거됨.
/// </summary>
public class CharacterInventoryManager : MonoBehaviour
{
    [Header("ScriptableObject DB 참조")]
    [SerializeField] public CharacterDatabaseObject characterDatabaseObject;

    private List<CharacterData> ownedCharacters = new List<CharacterData>();
    private List<CharacterData> deckCharacters = new List<CharacterData>();
    private List<CharacterData> gachaPool = new List<CharacterData>();

    // 공유 슬롯 20칸 (UI용)
    public CharacterData[] sharedSlotData20 = new CharacterData[20];

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

        // 이전에는 LoadCharacters() 같은 메서드가 있었으나, 서버 사용으로 로컬 로드는 제거
    }

    /// <summary>
    /// sharedSlotData20에서
    /// '이름/프리팹/아이콘이 전부 없는 캐릭터'는 null 취급 + 앞으로 땡겨 재정렬.
    /// </summary>
    public void CondenseAndReorderSharedSlots()
    {
        List<CharacterData> temp = new List<CharacterData>();

        for (int i = 0; i < 20; i++)
        {
            CharacterData cd = sharedSlotData20[i];
            if (cd != null)
            {
                bool allEmpty = (string.IsNullOrEmpty(cd.characterName) || cd.characterName == "")
                                && cd.spawnPrefab == null
                                && cd.buttonIcon == null;

                if (!allEmpty)
                {
                    temp.Add(cd);
                }
                else
                {
                    Debug.Log($"[CharacterInventoryManager] '{cd.characterName}' 은(는) 빈 데이터로 간주되어 제거됨");
                }
            }
        }

        for (int i = 0; i < 20; i++)
        {
            if (i < temp.Count)
                sharedSlotData20[i] = temp[i];
            else
                sharedSlotData20[i] = null;
        }
    }

    /// <summary>
    /// sharedSlotData20의 순서대로 ownedCharacters를 재구성.
    /// </summary>
    public void SyncOwnedFromSharedSlots()
    {
        ownedCharacters.Clear();
        for (int i = 0; i < 20; i++)
        {
            CharacterData cd = sharedSlotData20[i];
            if (cd != null)
            {
                ownedCharacters.Add(cd);
            }
        }
    }

    /// <summary>
    /// 현재 덱(10칸) 리스트 반환
    /// </summary>
    public List<CharacterData> GetDeckCharacters()
    {
        return new List<CharacterData>(deckCharacters);
    }

    /// <summary>
    /// 가챠 풀에서 무작위로 한 캐릭터를 뽑아서 인벤토리에 추가, 공유슬롯에 배치
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
        CharacterData newChar = CreateNewCharacter(template);

        ownedCharacters.Add(newChar);

        // 20칸 중 첫 null 슬롯에 배치
        int firstEmptyIndex = -1;
        for (int i = 0; i < 20; i++)
        {
            if (sharedSlotData20[i] == null)
            {
                firstEmptyIndex = i;
                break;
            }
        }
        if (firstEmptyIndex >= 0)
        {
            sharedSlotData20[firstEmptyIndex] = newChar;
            Debug.Log($"[CharacterInventoryManager] 뽑기 결과: {newChar.characterName} => 인덱스[{firstEmptyIndex}]에 배치");
        }
        else
        {
            Debug.LogWarning("[CharacterInventoryManager] 인벤토리 20칸이 모두 찼습니다! (배치 실패)");
        }

        return newChar;
    }

    private CharacterData CreateNewCharacter(CharacterData template)
    {
        CharacterData copy = new CharacterData
        {
            characterName  = template.characterName,
            attackPower    = template.attackPower,
            rangeType      = template.rangeType,
            isAreaAttack   = template.isAreaAttack,
            isBuffSupport  = template.isBuffSupport,
            spawnPrefab    = template.spawnPrefab,
            buttonIcon     = template.buttonIcon,
            cost           = template.cost,
            level          = template.level,
            currentExp     = 0,
            expToNextLevel = template.expToNextLevel,
        };
        return copy;
    }

    public void MoveToDeck(CharacterData c)
    {
        if (c == null) return;
        if (ownedCharacters.Remove(c))
        {
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
        if (deckCharacters.Remove(c))
        {
            ownedCharacters.Add(c);
            Debug.Log($"[CharacterInventoryManager] 덱 -> 인벤토리 이동: {c.characterName}");
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
        // 인벤토리(owned) + 덱(deck) 통합
        List<CharacterData> all = new List<CharacterData>();
        all.AddRange(ownedCharacters);
        all.AddRange(deckCharacters);
        return all;
    }

    public void RemoveFromInventory(CharacterData c)
    {
        if (c != null && ownedCharacters.Remove(c))
        {
            Debug.Log($"[CharacterInventoryManager] 인벤토리에서 제거: {c.characterName}");
        }
    }

    public void RemoveFromDeck(CharacterData c)
    {
        if (c != null && deckCharacters.Remove(c))
        {
            Debug.Log($"[CharacterInventoryManager] 덱에서 제거: {c.characterName}");
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

    /// <summary>
    /// 업그레이드에 사용될 재료 캐릭터 소모
    /// </summary>
    public void ConsumeCharactersForUpgrade(List<CharacterData> charsToConsume)
    {
        foreach (var c in charsToConsume)
        {
            if (ownedCharacters.Remove(c))
            {
                Debug.Log($"[CharacterInventoryManager] 업그레이드 재료로 인벤토리 제거: {c.characterName}");
            }
            else if (deckCharacters.Remove(c))
            {
                Debug.Log($"[CharacterInventoryManager] 업그레이드 재료로 덱 제거: {c.characterName}");
            }
            else
            {
                Debug.LogWarning($"[CharacterInventoryManager] 소모 대상에 없음: {c.characterName}");
            }
        }
    }

    // ==============
    // 데이터 세이브/로드 로직 제거됨 (서버 DB 사용 예정)
    // ==============

    /// <summary>
    /// 서버 데이터베이스를 사용하므로 실제 로직은 제거되었으나, 
    /// 다른 클래스에서 참조하기 위해 임시로 비어있는 메서드로 제공합니다.
    /// </summary>
    public void SaveCharacters()
    {
        Debug.Log("[CharacterInventoryManager] 캐릭터 데이터 저장 호출됨 (서버 사용으로 실제 로컬 저장은 생략)");
        
        // 지금은 빈 구현이지만, 서버 연동 시 여기에 서버 저장 로직이 들어갈 예정입니다.
        // 예: ServerDataManager.Instance.SaveCharacterData(ownedCharacters, deckCharacters);
    }
}
