using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 주사위 버튼 클릭 시 자동으로 같은 등급 3개씩 합성하는 매니저
/// </summary>
public class AutoMergeManager : MonoBehaviour
{
    private static AutoMergeManager instance;
    public static AutoMergeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AutoMergeManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("AutoMergeManager");
                    instance = go.AddComponent<AutoMergeManager>();
                }
            }
            return instance;
        }
    }

    [Header("합성 설정")]
    [Tooltip("한 번에 처리할 최대 합성 횟수")]
    public int maxMergesPerClick = 10;
    
    [Tooltip("합성 간 딜레이 (초)")]
    public float mergeDelay = 0.3f; // 빠른 합성을 위해 딜레이 축소
    
    [Header("효과")]
    [Tooltip("합성 시 이펙트 프리팹")]
    public GameObject mergeEffectPrefab;
    
    [Tooltip("합성 시 사운드")]
    public AudioClip mergeSound;
    
    [Header("주사위 버튼")]
    [Tooltip("주사위 버튼 UI")]
    public UnityEngine.UI.Button diceButton;
    
    private bool isProcessingMerge = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        // 주사위 버튼 이벤트 연결
        if (diceButton != null)
        {
            diceButton.onClick.RemoveAllListeners();
            diceButton.onClick.AddListener(() => OnDiceButtonClick(1)); // 기본 지역1
        }
    }

    /// <summary>
    /// 주사위 버튼 클릭 시 호출 - 자동 합성 실행
    /// </summary>
    public void OnDiceButtonClick(int areaIndex = 1)
    {
        if (isProcessingMerge)
        {
            Debug.Log("[AutoMergeManager] 이미 합성 처리 중입니다.");
            return;
        }

        StartCoroutine(ProcessAutoMerge(areaIndex));
    }

    /// <summary>
    /// 자동 합성 처리 코루틴
    /// </summary>
    private System.Collections.IEnumerator ProcessAutoMerge(int areaIndex)
    {
        isProcessingMerge = true;
        int mergeCount = 0;

        Debug.Log($"[AutoMergeManager] 지역 {areaIndex} 자동 합성 시작! (주사위 버튼 클릭)");

        // 합성 가능한 그룹 찾기
        List<MergeGroup> mergeGroups = FindMergeableGroups(areaIndex);
        
        while (mergeGroups.Count > 0 && mergeCount < maxMergesPerClick)
        {
            // 첫 번째 그룹 합성
            MergeGroup group = mergeGroups[0];
            
            // 합성 효과 표시
            ShowMergeEffect(group);
            
            // 실제 합성 수행
            PerformMerge(group);
            
            mergeCount++;
            
            // 딜레이
            yield return new WaitForSeconds(mergeDelay);
            
            // 다시 합성 가능한 그룹 찾기
            mergeGroups = FindMergeableGroups(areaIndex);
        }

        if (mergeCount == 0)
        {
            Debug.Log("[AutoMergeManager] 합성 가능한 캐릭터가 없습니다. (같은 등급 3개 이상 필요)");
        }
        else
        {
            Debug.Log($"[AutoMergeManager] 총 {mergeCount}번의 합성을 완료했습니다!");
        }

        isProcessingMerge = false;
    }

    /// <summary>
    /// 합성 가능한 그룹 찾기 (게임 기획서: 같은 등급 3개)
    /// </summary>
    private List<MergeGroup> FindMergeableGroups(int areaIndex)
    {
        List<MergeGroup> groups = new List<MergeGroup>();
        Character[] allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        // 지역별로 필터링
        var areaCharacters = allCharacters.Where(c => 
            c != null && 
            c.areaIndex == areaIndex && 
            c.currentTile != null &&
            c.star != CharacterStar.ThreeStar && // 3성은 합성 불가
            !c.isHero // 히어로는 합성 제외
        ).ToList();

        // 캐릭터를 이름과 별 등급으로 그룹화
        var groupedChars = areaCharacters
            .GroupBy(c => new { c.characterName, c.star })
            .Where(g => g.Count() >= 3); // 3개 이상인 그룹만

        foreach (var group in groupedChars)
        {
            var chars = group.Take(3).ToList(); // 3개씩만 가져오기
            
            MergeGroup mergeGroup = new MergeGroup
            {
                characterName = group.Key.characterName,
                star = group.Key.star,
                characters = chars
            };
            
            groups.Add(mergeGroup);
        }

        // 우선순위 정렬: 1성 > 2성 순으로
        groups = groups.OrderBy(g => g.star).ToList();

        return groups;
    }

    /// <summary>
    /// 합성 수행 (게임 기획서: 가장 뒤쪽 캐릭터 위치에 생성)
    /// </summary>
    private void PerformMerge(MergeGroup group)
    {
        if (group.characters.Count < 3) return;

        Character char1 = group.characters[0];
        Character char2 = group.characters[1];
        Character char3 = group.characters[2];

        // 위치 결정 (가장 뒤쪽 캐릭터 위치)
        Tile mergeTargetTile = GetRearMostTile(char1, char2, char3);
        int areaIndex = char1.areaIndex;
        
        // 새로운 별 등급
        CharacterStar newStar = CharacterStar.OneStar;
        if (char1.star == CharacterStar.OneStar) newStar = CharacterStar.TwoStar;
        else if (char1.star == CharacterStar.TwoStar) newStar = CharacterStar.ThreeStar;
        
        // 합성 전 타일 정보 백업
        Tile[] oldTiles = new Tile[] { char1.currentTile, char2.currentTile, char3.currentTile };
        
        // 3개 캐릭터 제거
        DestroyCharacterForMerge(char1);
        DestroyCharacterForMerge(char2);
        DestroyCharacterForMerge(char3);
        
        // 새 캐릭터 생성
        CreateMergedCharacter(group.characterName, newStar, mergeTargetTile, areaIndex);
        
        // 타일 상태 정리
        foreach (var tile in oldTiles)
        {
            if (tile != null && tile != mergeTargetTile)
            {
                TileManager.Instance.OnCharacterRemovedFromTile(tile);
            }
        }

        Debug.Log($"[AutoMergeManager] {group.characterName} ({group.star}) 3개를 합성하여 {newStar} 생성!");
    }

    /// <summary>
    /// 가장 뒤쪽 타일 찾기 (게임 기획서: 가장 뒤쪽 = Y 좌표가 가장 낮은)
    /// </summary>
    private Tile GetRearMostTile(Character char1, Character char2, Character char3)
    {
        Character rearMost = char1;
        
        // Y 좌표가 가장 낮은 (화면상 가장 아래쪽) 캐릭터 찾기
        if (char2.transform.position.y < rearMost.transform.position.y)
            rearMost = char2;
        if (char3.transform.position.y < rearMost.transform.position.y)
            rearMost = char3;
            
        return rearMost.currentTile;
    }

    /// <summary>
    /// 캐릭터 제거
    /// </summary>
    private void DestroyCharacterForMerge(Character character)
    {
        if (character.currentTile != null)
        {
            character.currentTile = null;
        }
        Destroy(character.gameObject);
    }

    /// <summary>
    /// 합성된 새 캐릭터 생성 (게임 기획서: 종족 시스템 고려)
    /// </summary>
    private void CreateMergedCharacter(string baseName, CharacterStar newStar, Tile targetTile, int areaIndex)
    {
        var coreData = CoreDataManager.Instance;
        StarMergeDatabaseObject mergeDB = (areaIndex == 2) ? coreData.starMergeDatabaseRegion2 : coreData.starMergeDatabase;
        
        if (mergeDB == null)
        {
            Debug.LogError("[AutoMergeManager] StarMergeDatabase가 설정되지 않았습니다!");
            return;
        }

        CharacterData newCharData = null;
        
        // 원본 캐릭터의 종족 찾기 (게임 기획서: 휴먼, 오크, 엘프)
        CharacterRace originalRace = CharacterRace.Human;
        if (coreData.characterDatabase != null && coreData.characterDatabase.currentRegisteredCharacters != null)
        {
            foreach (var data in coreData.characterDatabase.currentRegisteredCharacters)
            {
                if (data != null && data.characterName == baseName)
                {
                    originalRace = data.race;
                    break;
                }
            }
        }

        // 종족에 따른 새 캐릭터 데이터 가져오기
        RaceType raceType = ConvertCharacterRaceToRaceType(originalRace);
        
        if (newStar == CharacterStar.TwoStar)
        {
            newCharData = mergeDB.GetRandom2Star(raceType);
        }
        else if (newStar == CharacterStar.ThreeStar)
        {
            newCharData = mergeDB.GetRandom3Star(raceType);
        }

        if (newCharData == null || newCharData.spawnPrefab == null)
        {
            Debug.LogError($"[AutoMergeManager] {newStar} 캐릭터 데이터를 찾을 수 없습니다!");
            return;
        }

        // 새 캐릭터 생성
        RectTransform targetParent = (areaIndex == 2 && coreData.opponentCharacterPanel != null) 
            ? coreData.opponentCharacterPanel : coreData.characterPanel;
            
        GameObject mergedObj = Instantiate(newCharData.spawnPrefab, targetParent);
        
        // 위치 설정
        RectTransform mergedRect = mergedObj.GetComponent<RectTransform>();
        if (mergedRect != null && targetTile != null)
        {
            RectTransform tileRect = targetTile.GetComponent<RectTransform>();
            if (tileRect != null)
            {
                Vector2 localPos = targetParent.InverseTransformPoint(tileRect.transform.position);
                mergedRect.anchoredPosition = localPos;
                mergedRect.localRotation = Quaternion.identity;
            }
        }

        // Character 컴포넌트 설정
        Character mergedChar = mergedObj.GetComponent<Character>();
        if (mergedChar != null)
        {
            mergedChar.currentTile = targetTile;
            mergedChar.areaIndex = areaIndex;
            mergedChar.star = newStar;
            mergedChar.characterName = newCharData.characterName;
            mergedChar.attackPower = newCharData.attackPower;
            mergedChar.attackSpeed = newCharData.attackSpeed;
            mergedChar.attackRange = newCharData.attackRange;
            mergedChar.currentHP = newCharData.maxHP;
            mergedChar.ApplyStarVisual();
            
            if (areaIndex == 2 && coreData.opponentBulletPanel != null)
            {
                mergedChar.opponentBulletPanel = coreData.opponentBulletPanel;
            }
            else
            {
                mergedChar.SetBulletPanel(coreData.bulletPanel);
            }
        }
    }

    /// <summary>
    /// 종족 변환 (게임 기획서: 휴먼, 오크, 엘프, 언데드)
    /// </summary>
    private RaceType ConvertCharacterRaceToRaceType(CharacterRace charRace)
    {
        switch (charRace)
        {
            case CharacterRace.Human: return RaceType.Human;
            case CharacterRace.Orc: return RaceType.Orc;
            case CharacterRace.Elf: return RaceType.Elf;
            case CharacterRace.Undead: return RaceType.Undead;
            default: return RaceType.Etc;
        }
    }

    /// <summary>
    /// 합성 효과 표시
    /// </summary>
    private void ShowMergeEffect(MergeGroup group)
    {
        // 이펙트 프리팹이 있으면 생성
        if (mergeEffectPrefab != null)
        {
            foreach (var character in group.characters)
            {
                if (character != null && character.transform != null)
                {
                    GameObject effect = Instantiate(mergeEffectPrefab, character.transform.position, Quaternion.identity);
                    Destroy(effect, 2f); // 2초 후 제거
                }
            }
        }

        // 사운드 재생
        if (mergeSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.PlayOneShot(mergeSound);
        }
    }

    /// <summary>
    /// 합성 그룹 데이터 구조
    /// </summary>
    private class MergeGroup
    {
        public string characterName;
        public CharacterStar star;
        public List<Character> characters;
    }
}