using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 캐릭터 배치, 합성, 제거 등 전반적인 관리
/// ★★★ 수정: 같은 캐릭터끼리는 한 타일에 최대 3개까지 배치 가능
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

    public bool SummonCharacterOnTile(int characterIndex, Tile tile, bool forceEnemyArea2 = false)
    {
        if (summonManager != null)
        {
            return summonManager.SummonCharacterOnTile(characterIndex, tile, forceEnemyArea2);
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

    /// <summary>
    /// ★★★ 수정: 드래그 드롭 시 합성 체크 및 처리
    /// </summary>
    public void OnDropCharacter(Character droppedChar, Tile targetTile)
    {
        if (droppedChar == null || targetTile == null) return;

        // 타겟 타일에 있는 캐릭터들 확인
        List<Character> targetChars = targetTile.GetOccupyingCharacters();

        if (targetChars.Count == 0)
        {
            // 빈 타일이면 단순 이동
            MoveCharacterToTile(droppedChar, targetTile);
        }
        else
        {
            // 같은 캐릭터인지 확인
            Character firstChar = targetChars[0];
            if (droppedChar.characterName == firstChar.characterName && 
                droppedChar.star == firstChar.star)
            {
                // 3성은 합성 불가
                if (droppedChar.star == CharacterStar.ThreeStar)
                {
                    Debug.Log("[PlacementManager] 3성 캐릭터는 합성할 수 없습니다.");
                    return;
                }

                // 타일에 이미 2개가 있고, 드롭된 캐릭터까지 합치면 3개
                if (targetChars.Count == 2)
                {
                    // 3개 합성 실행
                    MergeThreeCharacters(targetChars[0], targetChars[1], droppedChar);
                }
                else if (targetChars.Count < 3)
                {
                    // 아직 3개가 안되므로 타일에 추가
                    MoveCharacterToTile(droppedChar, targetTile);
                }
                else
                {
                    Debug.Log("[PlacementManager] 타일이 가득 찼습니다.");
                }
            }
            else
            {
                Debug.Log("[PlacementManager] 다른 종류의 캐릭터는 같은 타일에 배치할 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// ★★★ 추가: 같은 타일에 있는 캐릭터들 중 3개 찾아서 합성
    /// </summary>
    private void CheckAndMergeOnTile(Tile tile)
    {
        List<Character> chars = tile.GetOccupyingCharacters();
        
        if (chars.Count >= 3)
        {
            // 같은 종류인지 확인 (이미 타일에 추가될 때 체크했으므로 여기서는 바로 합성)
            MergeThreeCharacters(chars[0], chars[1], chars[2]);
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
        
        // 3개 캐릭터를 타일에서 제거
        foreach (var tile in oldTiles)
        {
            if (tile != null)
            {
                tile.RemoveOccupyingCharacter(char1);
                tile.RemoveOccupyingCharacter(char2);
                tile.RemoveOccupyingCharacter(char3);
            }
        }
        
        // 3개 캐릭터 제거
        DestroyCharacterForMerge(char1);
        DestroyCharacterForMerge(char2);
        DestroyCharacterForMerge(char3);
        
        // 새 캐릭터 생성
        CreateMergedCharacter(char1.characterName, newStar, mergeTargetTile, areaIndex);
        
        // 타일 상태 정리
        foreach (var tile in oldTiles)
        {
            if (tile != null)
            {
                // 타일이 비었으면 원래 상태로
                if (tile.GetOccupyingCharacters().Count == 0)
                {
                    tileManager.OnCharacterRemovedFromTile(tile);
                }
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
        StarMergeDatabaseObject mergeDB = (areaIndex == 2) ?
            coreData.starMergeDatabaseRegion2 : coreData.starMergeDatabase;
            
        if (mergeDB == null)
        {
            Debug.LogError("[PlacementManager] StarMergeDatabase가 설정되지 않았습니다!");
            return;
        }

        CharacterData newCharData = null;
        
        // 원본 캐릭터의 종족 찾기
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

            // ★★★ 추가: 타일에 새 캐릭터 추가
            if (targetTile != null)
            {
                targetTile.AddOccupyingCharacter(mergedChar);
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

    /// <summary>
    /// ★★★ 수정: 캐릭터를 타일로 이동
    /// </summary>
    private void MoveCharacterToTile(Character character, Tile newTile)
    {
        if (character == null || newTile == null) return;
        
        // 이전 타일에서 캐릭터 제거
        if (character.currentTile != null)
        {
            character.currentTile.RemoveOccupyingCharacter(character);
            
            // 이전 타일이 비었으면 정리
            if (character.currentTile.GetOccupyingCharacters().Count == 0)
            {
                tileManager.OnCharacterRemovedFromTile(character.currentTile);
            }
        }
        
        // 새 타일에 캐릭터 추가
        if (!newTile.AddOccupyingCharacter(character))
        {
            Debug.LogError($"[PlacementManager] {character.characterName}을(를) {newTile.name}에 추가할 수 없습니다!");
            return;
        }
        
        // 캐릭터의 타일 참조 업데이트
        character.currentTile = newTile;
        
        // 위치 업데이트는 타일의 UpdateCharacterSizes()에서 처리됨
        
        // 타일 상태 업데이트
        if (!newTile.IsPlaceTile() && !newTile.IsPlaced2())
        {
            tileManager.CreatePlaceTileChild(newTile);
        }
        
        Debug.Log($"[PlacementManager] {character.characterName}을(를) {newTile.name}으로 이동 (타일의 캐릭터 수: {newTile.GetOccupyingCharacters().Count})");

        // 이동 후 3개가 모였는지 확인
        if (newTile.GetOccupyingCharacters().Count == 3)
        {
            CheckAndMergeOnTile(newTile);
        }
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
    }

    /// <summary>
    /// 자동 배치 메서드
    /// </summary>
    public void OnClickAutoPlace()
    {
        if (summonManager != null)
        {
            summonManager.OnClickAutoPlace();
        }
        else
        {
            Debug.LogError("[PlacementManager] SummonManager가 null입니다!");
        }
    }
}