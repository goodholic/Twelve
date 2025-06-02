using UnityEngine;

/// <summary>
/// GameScene에 존재하는 CharacterDatabase (MonoBehaviour).
/// 게임 기획서에 따라 10개의 캐릭터 슬롯 관리 (각 종족 3명 + 자유 1명)
/// </summary>
public class CharacterDatabase : MonoBehaviour
{
    [Header("현재 등록된 캐릭터(종족별 3명씩 + 자유 1명)")]
    [Tooltip("0-2: 휴먼, 3-5: 오크, 6-8: 엘프, 9: 자유")]
    public CharacterData[] currentRegisteredCharacters = new CharacterData[10];

    [Header("종족별 캐릭터 개수 확인")]
    [SerializeField] private int humanCount = 0;
    [SerializeField] private int orcCount = 0;
    [SerializeField] private int elfCount = 0;

    private void Start()
    {
        // 1) GameManager 인스턴스 확인
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[CharacterDatabase] GameManager.Instance가 없습니다!");
            return;
        }

        // 2) GameManager의 currentRegisteredCharacters(10) 를 가져와 동기화
        for (int i = 0; i < 10; i++)
        {
            currentRegisteredCharacters[i] = gm.currentRegisteredCharacters[i];
        }

        // 3) 종족별 캐릭터 개수 카운트
        CountCharactersByRace();

        Debug.Log($"[CharacterDatabase] 현재 등록된 캐릭터 10개를 GameManager에서 받아왔습니다.");
        Debug.Log($"[CharacterDatabase] 휴먼: {humanCount}명, 오크: {orcCount}명, 엘프: {elfCount}명");
    }

    /// <summary>
    /// 종족별 캐릭터 개수를 카운트하는 메서드
    /// </summary>
    private void CountCharactersByRace()
    {
        humanCount = 0;
        orcCount = 0;
        elfCount = 0;

        foreach (var charData in currentRegisteredCharacters)
        {
            if (charData == null) continue;

            switch (charData.race)
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

    /// <summary>
    /// 특정 종족의 캐릭터만 가져오는 메서드
    /// </summary>
    public CharacterData[] GetCharactersByRace(CharacterRace race)
    {
        System.Collections.Generic.List<CharacterData> raceCharacters = new System.Collections.Generic.List<CharacterData>();
        
        foreach (var charData in currentRegisteredCharacters)
        {
            if (charData != null && charData.race == race)
            {
                raceCharacters.Add(charData);
            }
        }
        
        return raceCharacters.ToArray();
    }

    /// <summary>
    /// 랜덤한 캐릭터 데이터를 반환하는 메서드 (소환 시스템용)
    /// </summary>
    public CharacterData GetRandomCharacter()
    {
        System.Collections.Generic.List<CharacterData> validCharacters = new System.Collections.Generic.List<CharacterData>();
        
        foreach (var charData in currentRegisteredCharacters)
        {
            if (charData != null)
            {
                validCharacters.Add(charData);
            }
        }
        
        if (validCharacters.Count > 0)
        {
            return validCharacters[Random.Range(0, validCharacters.Count)];
        }
        
        Debug.LogWarning("[CharacterDatabase] 등록된 캐릭터가 없습니다!");
        return null;
    }

    /// <summary>
    /// 캐릭터 데이터베이스 갱신 (에디터용)
    /// </summary>
    public void RefreshDatabase()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            for (int i = 0; i < 10; i++)
            {
                currentRegisteredCharacters[i] = gm.currentRegisteredCharacters[i];
            }
            CountCharactersByRace();
            Debug.Log("[CharacterDatabase] 데이터베이스 갱신 완료");
        }
    }
}