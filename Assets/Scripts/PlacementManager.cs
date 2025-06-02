using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 캐릭터 배치, 합성, 제거 등 전반적인 관리
/// </summary>
public class PlacementManager : MonoBehaviour
{
    private static PlacementManager instance;
    public static PlacementManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PlacementManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PlacementManager");
                    instance = go.AddComponent<PlacementManager>();
                }
            }
            return instance;
        }
    }

    [Header("Character Database")]
    public CharacterDatabase characterDatabase;

    [Header("UI Panels")]
    public RectTransform tilePanel;
    public RectTransform characterPanel;
    public RectTransform bulletPanel;
    public RectTransform ourMonsterPanel;
    public RectTransform opponentCharacterPanel;
    public RectTransform opponentBulletPanel;
    public RectTransform opponentOurMonsterPanel;

    [Header("Camera")]
    public Camera mainCamera;

    [Header("제거 모드")]
    public bool removeMode = false;

    private SummonManager summonManager;
    private TileManager tileManager;
    
    // 현재 선택된 캐릭터 인덱스 추적
    private int currentCharacterIndex = -1;

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
        // CoreDataManager에서 참조 가져오기
        var coreData = CoreDataManager.Instance;
        if (coreData != null)
        {
            if (characterDatabase == null) characterDatabase = coreData.characterDatabase;
            if (tilePanel == null) tilePanel = coreData.tilePanel;
            if (characterPanel == null) characterPanel = coreData.characterPanel;
            if (bulletPanel == null) bulletPanel = coreData.bulletPanel;
            if (ourMonsterPanel == null) ourMonsterPanel = coreData.ourMonsterPanel;
            if (opponentCharacterPanel == null) opponentCharacterPanel = coreData.opponentCharacterPanel;
            if (opponentBulletPanel == null) opponentBulletPanel = coreData.opponentBulletPanel;
            if (opponentOurMonsterPanel == null) opponentOurMonsterPanel = coreData.opponentOurMonsterPanel;
            if (mainCamera == null) mainCamera = coreData.mainCamera;
        }

        summonManager = SummonManager.Instance;
        tileManager = TileManager.Instance;
    }

    public void PlaceCharacterOnTile(Tile tile)
    {
        if (summonManager != null)
        {
            summonManager.PlaceCharacterOnTile(tile);
        }
    }

    public bool SummonCharacterOnTile(int characterIndex, Tile tile)
    {
        if (summonManager != null)
        {
            return summonManager.SummonCharacterOnTile(characterIndex, tile);
        }
        return false;
    }

    public void RemoveCharacterOnTile(Tile tile)
    {
        if (summonManager != null)
        {
            summonManager.RemoveCharacterOnTile(tile);
        }
    }

    // 드래그 드롭 시 합성 체크 및 처리
    public void OnDropCharacter(Character droppedChar, Tile targetTile)
    {
        if (droppedChar == null || targetTile == null) return;

        // 타겟 타일에 이미 캐릭터가 있는지 확인
        Character targetChar = null;
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var c in allChars)
        {
            if (c != null && c != droppedChar && c.currentTile == targetTile)
            {
                targetChar = c;
                break;
            }
        }

        if (targetChar != null)
        {
            // 같은 캐릭터인지 확인 (이름과 별 등급으로 판단)
            if (droppedChar.characterName == targetChar.characterName && 
                droppedChar.star == targetChar.star)
            {
                // 3성은 합성 불가
                if (droppedChar.star == CharacterStar.ThreeStar)
                {
                    Debug.Log("[PlacementManager] 3성 캐릭터는 합성할 수 없습니다.");
                    return;
                }

                // 자동으로 3개째 찾기
                Character thirdChar = FindThirdCharacterForMerge(droppedChar.characterName, droppedChar.star, droppedChar, targetChar);
                
                if (thirdChar != null)
                {
                    // 3개 합성 실행
                    MergeThreeCharacters(droppedChar, targetChar, thirdChar);
                }
                else
                {
                    Debug.Log("[PlacementManager] 같은 종류의 세 번째 캐릭터를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.Log("[PlacementManager] 다른 종류의 캐릭터는 합성할 수 없습니다.");
            }
        }
        else
        {
            // 빈 타일이면 이동
            MoveCharacterToTile(droppedChar, targetTile);
        }
    }

    private Character FindThirdCharacterForMerge(string charName, CharacterStar star, Character char1, Character char2)
    {
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var c in allChars)
        {
            if (c != null && c != char1 && c != char2 &&
                c.characterName == charName && c.star == star &&
                c.currentTile != null)
            {
                return c;
            }
        }
        
        return null;
    }

    private void MergeThreeCharacters(Character char1, Character char2, Character char3)
    {
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
        CreateMergedCharacter(char1.characterName, newStar, mergeTargetTile, areaIndex);
        
        // 타일 상태 정리
        foreach (var tile in oldTiles)
        {
            if (tile != null && tile != mergeTargetTile)
            {
                tileManager.OnCharacterRemovedFromTile(tile);
            }
        }
    }

    private Tile GetRearMostTile(Character char1, Character char2, Character char3)
    {
        // Y 좌표가 가장 낮은 (화면상 가장 아래쪽) 캐릭터의 타일 반환
        Character rearMost = char1;
        
        if (char2.transform.position.y < rearMost.transform.position.y)
            rearMost = char2;
        if (char3.transform.position.y < rearMost.transform.position.y)
            rearMost = char3;
            
        return rearMost.currentTile;
    }

    private void DestroyCharacterForMerge(Character character)
    {
        if (character.currentTile != null)
        {
            character.currentTile = null;
        }
        Destroy(character.gameObject);
    }

    private void CreateMergedCharacter(string baseName, CharacterStar newStar, Tile targetTile, int areaIndex)
    {
        // StarMergeDatabase에서 새 캐릭터 데이터 가져오기
        var coreData = CoreDataManager.Instance;
        StarMergeDatabaseObject mergeDB = (areaIndex == 2) ? coreData.starMergeDatabaseRegion2 : coreData.starMergeDatabase;
        
        if (mergeDB == null)
        {
            Debug.LogError("[PlacementManager] StarMergeDatabase가 설정되지 않았습니다!");
            return;
        }

        CharacterData newCharData = null;
        
        // 원본 캐릭터의 종족 찾기
        CharacterRace originalRace = CharacterRace.Human;
        if (characterDatabase != null && characterDatabase.currentRegisteredCharacters != null)
        {
            foreach (var data in characterDatabase.currentRegisteredCharacters)
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
            Debug.LogError($"[PlacementManager] {newStar} 캐릭터 데이터를 찾을 수 없습니다!");
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

        Debug.Log($"[PlacementManager] 합성 성공! {baseName} -> {newCharData.characterName} ({newStar})");
    }

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

    private void MoveCharacterToTile(Character character, Tile newTile)
    {
        if (character == null || newTile == null) return;
        
        // 이전 타일 정리
        if (character.currentTile != null)
        {
            tileManager.OnCharacterRemovedFromTile(character.currentTile);
        }
        
        // 새 타일로 이동
        character.currentTile = newTile;
        
        RectTransform charRect = character.GetComponent<RectTransform>();
        RectTransform tileRect = newTile.GetComponent<RectTransform>();
        
        if (charRect != null && tileRect != null)
        {
            // 부모 패널 확인
            RectTransform targetParent = charRect.parent as RectTransform;
            Vector2 localPos = targetParent.InverseTransformPoint(tileRect.transform.position);
            charRect.anchoredPosition = localPos;
        }
        
        // 타일 상태 업데이트
        if (!newTile.IsPlaceTile() && !newTile.IsPlaced2())
        {
            tileManager.CreatePlaceTileChild(newTile);
        }
        
        Debug.Log($"[PlacementManager] {character.characterName}을(를) {newTile.name}으로 이동");
    }

    public void ClearCharacterTileReference(Character character)
    {
        if (tileManager != null)
        {
            tileManager.ClearCharacterTileReference(character);
        }
    }

    public void OnCharacterRemovedFromTile(Tile tile)
    {
        if (tileManager != null)
        {
            tileManager.OnCharacterRemovedFromTile(tile);
        }
    }

    // 현재 선택된 캐릭터 인덱스 관리
    public void SetCurrentCharacterIndex(int index)
    {
        currentCharacterIndex = index;
        CoreDataManager.Instance.currentCharacterIndex = index;
    }

    public int GetCurrentCharacterIndex()
    {
        return currentCharacterIndex;
    }

    /// <summary>
    /// 캐릭터 선택 (CharacterSelectUI에서 호출)
    /// </summary>
    public void OnClickSelectUnit(int index)
    {
        currentCharacterIndex = index;
        if (summonManager != null)
        {
            summonManager.OnClickSelectUnit(index);
        }
        Debug.Log($"[PlacementManager] 캐릭터 {index}번 선택됨");
    }
    
    /// <summary>
    /// 자동 배치 (CharacterSelectUI에서 호출)
    /// </summary>
    public void OnClickAutoPlace()
    {
        if (summonManager != null)
        {
            summonManager.OnClickAutoPlace();
        }
    }
    
    /// <summary>
    /// 보상 캐릭터 배치 (WaveSpawner에서 호출)
    /// </summary>
    public void PlaceRewardCharacterOnTile(CharacterData characterData, Tile tile)
    {
        if (characterData == null || tile == null) return;
        
        // 캐릭터 데이터베이스에서 인덱스 찾기
        int characterIndex = -1;
        if (characterDatabase != null && characterDatabase.currentRegisteredCharacters != null)
        {
            for (int i = 0; i < characterDatabase.currentRegisteredCharacters.Length; i++)
            {
                if (characterDatabase.currentRegisteredCharacters[i] == characterData)
                {
                    characterIndex = i;
                    break;
                }
            }
        }
        
        if (characterIndex >= 0)
        {
            SummonCharacterOnTile(characterIndex, tile);
        }
        else
        {
            Debug.LogWarning($"[PlacementManager] 보상 캐릭터 {characterData.characterName}의 인덱스를 찾을 수 없습니다.");
        }
    }
}