// ================================================
//  Assets\OX UI Scripts\CharacterInventoryManager.cs
//   - 공용 20칸 데이터배열(sharedSlotData20) 추가
// ================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터(카드) 인벤토리 및 뽑기(gacha) 기능을 관리.
/// (ScriptableObject 기반 CharacterDatabaseObject를 사용하도록 수정)
/// </summary>
public class CharacterInventoryManager : MonoBehaviour
{
    [Header("ScriptableObject DB 참조")]
    [Tooltip("CharacterDatabaseObject(.asset) 파일을 할당하세요.")]
    [SerializeField] private CharacterDatabaseObject characterDatabaseObject;

    // 소유(인벤토리) 중인 캐릭터 목록 (최대 20개라 가정, 실제 제한은 없음)
    [SerializeField] private List<CharacterData> ownedCharacters = new List<CharacterData>();

    // [추가] 덱에 등록된 캐릭터 목록 (인벤토리에서 빼고 별도 관리)
    private List<CharacterData> deckCharacters = new List<CharacterData>();

    // 뽑기(Gacha) 풀
    private List<CharacterData> gachaPool = new List<CharacterData>();

    // =============================
    //  새로 추가된 "공용 20칸" 데이터
    // =============================
    public CharacterData[] sharedSlotData20 = new CharacterData[20];
    // ↑ 덱/업그레이드 패널 모두 이 배열을 참조하여 동기화

    private const string PLAYER_PREFS_OWNED_KEY = "OwnedCharactersJson";

    private void Awake()
    {
        // ScriptableObject 참조 체크
        if (characterDatabaseObject == null)
        {
            Debug.LogError("[CharacterInventoryManager] CharacterDatabaseObject가 연결되지 않음 -> 뽑기 후보 구성 불가");
            return;
        }

        // gachaPool 구성
        gachaPool.Clear();
        foreach (var cData in characterDatabaseObject.characters)
        {
            if (cData != null)
                gachaPool.Add(cData);
        }
        Debug.Log($"[CharacterInventoryManager] {gachaPool.Count}개 캐릭터로 gachaPool 구성 (ScriptableObject)");

        // 이전 저장분 로드
        LoadCharacters();
    }

    /// <summary>
    /// 무작위 캐릭터 뽑기
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

    /// <summary>
    /// 템플릿 CharacterData를 복제 (레벨/경험치 등 필요한 필드 추가 가능)
    /// </summary>
    private CharacterData CreateNewCharacter(CharacterData template)
    {
        // 복제
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

            // [추가] 레벨/경험치도 복제(초기값)
            level         = template.level,
            currentExp    = 0,
            expToNextLevel= template.expToNextLevel,
        };
        return copy;
    }

    /// <summary>
    /// 인벤토리 -> 덱 이동
    /// </summary>
    public void MoveToDeck(CharacterData c)
    {
        if (c == null) return;
        if (ownedCharacters.Contains(c))
        {
            ownedCharacters.Remove(c);
            deckCharacters.Add(c);
            Debug.Log($"[CharacterInventoryManager] 덱으로 이동: {c.characterName}");
        }
        else
        {
            Debug.LogWarning($"[CharacterInventoryManager] 인벤토리에 없는 캐릭터({c.characterName})");
        }
    }

    /// <summary>
    /// (덱 패널 등) 중복 제거된 캐릭터 목록 반환
    /// 인벤토리+덱 전체에서 중복 제거
    /// </summary>
    public List<CharacterData> GetUniqueCharacterList()
    {
        Dictionary<string, CharacterData> dict = new Dictionary<string, CharacterData>();

        // 1) 인벤토리에 있는 캐릭터
        foreach (var c in ownedCharacters)
        {
            if (c != null && !dict.ContainsKey(c.characterName))
            {
                dict.Add(c.characterName, c);
            }
        }
        // 2) 덱에 있는 캐릭터
        foreach (var c in deckCharacters)
        {
            if (c != null && !dict.ContainsKey(c.characterName))
            {
                dict.Add(c.characterName, c);
            }
        }

        return new List<CharacterData>(dict.Values);
    }

    /// <summary>
    /// (업그레이드 패널 등) 중복 포함 전체 캐릭터 목록 반환 (인벤토리 + 덱)
    /// </summary>
    public List<CharacterData> GetAllCharactersWithDuplicates()
    {
        List<CharacterData> all = new List<CharacterData>();
        all.AddRange(ownedCharacters);
        all.AddRange(deckCharacters);
        return all;
    }

    /// <summary>
    /// 업그레이드 등의 이유로 캐릭터 소비(여기서는 인벤토리+덱 모두에서 제거)
    /// </summary>
    public void ConsumeCharactersForUpgrade(List<CharacterData> charsToConsume)
    {
        foreach (var c in charsToConsume)
        {
            if (ownedCharacters.Contains(c))
            {
                ownedCharacters.Remove(c);
                Debug.Log($"[CharacterInventoryManager] 인벤토리 캐릭터 제거: {c.characterName}");
            }
            else if (deckCharacters.Contains(c))
            {
                deckCharacters.Remove(c);
                Debug.Log($"[CharacterInventoryManager] 덱 캐릭터 제거: {c.characterName}");
            }
            else
            {
                Debug.LogWarning($"[CharacterInventoryManager] 목록에 없는 캐릭터: {c.characterName}");
            }
        }
    }

    /// <summary>
    /// 덱이 아닌 인벤토리에 있는 캐릭터만 반환 (DeckPanelManager에서 사용)
    /// </summary>
    public List<CharacterData> GetOwnedCharacters()
    {
        return new List<CharacterData>(ownedCharacters);
    }

    /// <summary>
    /// 보유 캐릭터 정보(PlayerPrefs)에 JSON 형태로 저장
    /// (덱 등록 캐릭터는 여기선 따로 저장 안 하지만 확장 가능)
    /// </summary>
    public void SaveCharacters()
    {
        List<CharacterRecord> recordList = new List<CharacterRecord>();
        foreach (var c in ownedCharacters)
        {
            if (c == null) continue;

            CharacterRecord rec = new CharacterRecord
            {
                characterName = c.characterName
            };
            recordList.Add(rec);
        }

        CharacterRecordWrapper wrapper = new CharacterRecordWrapper { records = recordList };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(PLAYER_PREFS_OWNED_KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"[CharacterInventoryManager] SaveCharacters() 완료. count={recordList.Count}");
    }

    /// <summary>
    /// 보유 캐릭터 로드(PlayerPrefs → JSON → ownedCharacters)
    /// 덱 등록 캐릭터(deckCharacters)는 초기화
    /// </summary>
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
            Debug.LogWarning("[CharacterInventoryManager] LoadCharacters() 파싱 실패");
            return;
        }

        ownedCharacters.Clear();
        deckCharacters.Clear(); // [추가] 불러올 때 덱 리스트는 초기화(혹은 합쳐서 저장했다면 재구현)

        foreach (var rec in wrapper.records)
        {
            // DB(현재 ScriptableObject)에서 이름으로 템플릿 검색
            CharacterData template = FindTemplateByName(rec.characterName);
            if (template == null)
            {
                Debug.LogWarning($"[CharacterInventoryManager] DB에 없는 캐릭터({rec.characterName}) -> 무시");
                continue;
            }

            // 복제 후 인벤토리에 추가
            CharacterData newChar = CreateNewCharacter(template);
            ownedCharacters.Add(newChar);
        }

        Debug.Log($"[CharacterInventoryManager] LoadCharacters() 완료. 개수={ownedCharacters.Count}");
    }

    /// <summary>
    /// ScriptableObject(.asset) 내 캐릭터 배열에서 이름으로 검색
    /// </summary>
    private CharacterData FindTemplateByName(string name)
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

[System.Serializable]
public class CharacterRecord
{
    public string characterName;
    // 필요 시 level, currentExp 등 확장 가능
}

[System.Serializable]
public class CharacterRecordWrapper
{
    public List<CharacterRecord> records;
}
