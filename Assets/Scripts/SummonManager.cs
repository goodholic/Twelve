using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 소환, 미네랄 체크, 원 버튼 소환 기능
/// </summary>
public class SummonManager : MonoBehaviour
{
    private static SummonManager instance;
    public static SummonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SummonManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SummonManager");
                    instance = go.AddComponent<SummonManager>();
                }
            }
            return instance;
        }
    }

    [Header("Movement Speed Settings")]
    public float walkableCharSpeed = 1.0f;
    public float walkable2CharSpeed = 1.2f;

    [Header("제거 모드")]
    public bool removeMode = false;

    [Header("원 버튼 소환 설정")]
    [Tooltip("원 버튼 소환 시 소환할 캐릭터 풀 (1성 캐릭터들)")]
    public CharacterData[] oneButtonSummonPool;

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
        // 원 버튼 소환 풀이 비어있으면 기본 캐릭터들로 채우기
        if (oneButtonSummonPool == null || oneButtonSummonPool.Length == 0)
        {
            var coreData = CoreDataManager.Instance;
            if (coreData != null && coreData.characterDatabase != null)
            {
                List<CharacterData> oneStarChars = new List<CharacterData>();
                foreach (var charData in coreData.characterDatabase.currentRegisteredCharacters)
                {
                    if (charData != null && charData.initialStar == CharacterStar.OneStar)
                    {
                        oneStarChars.Add(charData);
                    }
                }
                oneButtonSummonPool = oneStarChars.ToArray();
            }
        }
    }

    private void Update()
    {
        // 숫자키로 캐릭터 인덱스 바꾸기
        if (Input.GetKeyDown(KeyCode.Alpha1)) CoreDataManager.Instance.currentCharacterIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) CoreDataManager.Instance.currentCharacterIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) CoreDataManager.Instance.currentCharacterIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) CoreDataManager.Instance.currentCharacterIndex = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) CoreDataManager.Instance.currentCharacterIndex = 4;
        if (Input.GetKeyDown(KeyCode.Alpha6)) CoreDataManager.Instance.currentCharacterIndex = 5;
        if (Input.GetKeyDown(KeyCode.Alpha7)) CoreDataManager.Instance.currentCharacterIndex = 6;
        if (Input.GetKeyDown(KeyCode.Alpha8)) CoreDataManager.Instance.currentCharacterIndex = 7;
        if (Input.GetKeyDown(KeyCode.Alpha9)) CoreDataManager.Instance.currentCharacterIndex = 8;
        if (Input.GetKeyDown(KeyCode.Alpha0)) CoreDataManager.Instance.currentCharacterIndex = 9;
    }

    /// <summary>
    /// 원 버튼 소환 - 랜덤 캐릭터를 랜덤 위치에 소환
    /// </summary>
    public void OnClickOneButtonSummon(bool isRegion2 = false)
    {
        var coreData = CoreDataManager.Instance;
        
        // 소환 풀 확인
        if (oneButtonSummonPool == null || oneButtonSummonPool.Length == 0)
        {
            Debug.LogWarning("[SummonManager] 원 버튼 소환 풀이 비어있습니다!");
            return;
        }

        // 랜덤 캐릭터 선택
        int randomCharIndex = Random.Range(0, oneButtonSummonPool.Length);
        CharacterData randomCharData = oneButtonSummonPool[randomCharIndex];
        
        if (randomCharData == null || randomCharData.spawnPrefab == null)
        {
            Debug.LogWarning("[SummonManager] 선택된 캐릭터 데이터가 유효하지 않습니다!");
            return;
        }

        // 미네랄 체크
        MineralBar targetMineralBar = isRegion2 ? coreData.region2MineralBar : coreData.region1MineralBar;
        if (targetMineralBar == null)
        {
            Debug.LogWarning($"[SummonManager] {(isRegion2 ? "지역2" : "지역1")} 미네랄 바가 없습니다!");
            return;
        }

        if (!targetMineralBar.TrySpend(randomCharData.cost))
        {
            Debug.Log($"[SummonManager] 미네랄 부족! (필요: {randomCharData.cost})");
            return;
        }

        // 랜덤 타일 찾기
        Tile randomTile = FindRandomEmptyTile(isRegion2);
        if (randomTile == null)
        {
            // 미네랄 환불
            targetMineralBar.RefundMinerals(randomCharData.cost);
            Debug.LogWarning("[SummonManager] 배치 가능한 타일이 없습니다!");
            return;
        }

        // 실제 소환
        bool success = ActualSummonOneButton(randomCharData, randomTile, isRegion2);
        
        if (!success)
        {
            // 실패 시 미네랄 환불
            targetMineralBar.RefundMinerals(randomCharData.cost);
        }
        else
        {
            Debug.Log($"[SummonManager] 원 버튼 소환 성공! {randomCharData.characterName} -> {randomTile.name}");
        }
    }

    /// <summary>
    /// 랜덤으로 빈 타일 찾기
    /// </summary>
    private Tile FindRandomEmptyTile(bool isRegion2)
    {
        List<Tile> availableTiles = new List<Tile>();
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);

        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            if (tile.isRegion2 != isRegion2) continue;
            
            // 배치 가능한 타일인지 확인
            if (!tile.CanPlaceCharacter()) continue;
            
            // 이미 캐릭터가 있는지 확인
            bool occupied = false;
            
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                foreach (var c in allChars)
                {
                    if (c != null && c.currentTile == tile)
                    {
                        occupied = true;
                        break;
                    }
                }
            }
            else
            {
                occupied = TileManager.Instance.CheckAnyCharacterHasCurrentTile(tile);
            }
            
            if (!occupied)
            {
                availableTiles.Add(tile);
            }
        }

        if (availableTiles.Count > 0)
        {
            return availableTiles[Random.Range(0, availableTiles.Count)];
        }

        return null;
    }

    /// <summary>
    /// 원 버튼 소환 실행
    /// </summary>
    private bool ActualSummonOneButton(CharacterData data, Tile tile, bool isRegion2)
    {
        var coreData = CoreDataManager.Instance;
        
        try
        {
            if (tile.IsWalkable() || tile.IsWalkableLeft() || tile.IsWalkableCenter() || tile.IsWalkableRight())
            {
                // 지역1 walkable 타일에 소환
                WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
                if (spawner == null || coreData.ourMonsterPanel == null)
                {
                    Debug.LogWarning("[SummonManager] WaveSpawner/ourMonsterPanel이 없어 소환 실패");
                    return false;
                }

                GameObject allyObj = Instantiate(data.spawnPrefab, coreData.ourMonsterPanel);
                if (allyObj != null)
                {
                    allyObj.transform.position = new Vector3(-1000, -1000, 0);
                    
                    Character allyCharacter = allyObj.GetComponent<Character>();
                    ConfigureCharacterForWalkable(allyCharacter, data, tile, spawner, 1);
                    
                    return true;
                }
            }
            else if (tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right())
            {
                // 지역2 walkable 타일에 소환
                WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                if (spawner2 == null || coreData.opponentOurMonsterPanel == null)
                {
                    Debug.LogWarning("[SummonManager] WaveSpawnerRegion2/opponentOurMonsterPanel이 없어 소환 실패");
                    return false;
                }

                GameObject enemyObj = Instantiate(data.spawnPrefab, coreData.opponentOurMonsterPanel);
                if (enemyObj != null)
                {
                    enemyObj.transform.position = new Vector3(-1000, -1000, 0);
                    
                    Character enemyCharacter = enemyObj.GetComponent<Character>();
                    ConfigureCharacterForWalkable2(enemyCharacter, data, tile, spawner2, 2);
                    
                    return true;
                }
            }
            else if (tile.IsPlacable() || tile.IsPlacable2() || tile.IsPlaceTile() || tile.IsPlaced2())
            {
                // 타워형 타일에 소환
                bool isArea2Tile = tile.IsPlacable2() || tile.IsPlaced2();
                RectTransform targetParent = isArea2Tile
                    ? (coreData.opponentCharacterPanel != null ? coreData.opponentCharacterPanel : coreData.characterPanel)
                    : coreData.characterPanel;

                GameObject charObj = Instantiate(data.spawnPrefab, targetParent);
                if (charObj != null)
                {
                    PositionCharacterOnTile(charObj, tile, targetParent);
                    ConfigureCharacterForTower(charObj.GetComponent<Character>(), data, tile, isArea2Tile);
                    
                    TileManager.Instance.CreatePlaceTileChild(tile);
                    return true;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SummonManager] 원 버튼 소환 중 오류: {ex.Message}");
            return false;
        }

        return false;
    }

    /// <summary>
    /// 지역1 walkable 캐릭터 설정
    /// </summary>
    private void ConfigureCharacterForWalkable(Character character, CharacterData data, Tile tile, WaveSpawner spawner, int areaIndex)
    {
        character.currentTile = null;
        character.isHero = false;
        character.isCharAttack = true;
        character.areaIndex = areaIndex;
        
        character.attackPower = data.attackPower;
        character.attackSpeed = data.attackSpeed;
        character.attackRange = data.attackRange;
        character.currentHP = data.maxHP;
        character.star = data.initialStar;
        character.ApplyStarVisual();
        character.moveSpeed = data.moveSpeed;
        
        RouteType selectedRoute = RouteManager.Instance.DetermineRouteFromTile(tile, spawner);
        RouteManager.Instance.OnRouteSelected(character, tile, selectedRoute, spawner);
    }

    /// <summary>
    /// 지역2 walkable 캐릭터 설정
    /// </summary>
    private void ConfigureCharacterForWalkable2(Character character, CharacterData data, Tile tile, WaveSpawnerRegion2 spawner2, int areaIndex)
    {
        character.currentTile = null;
        character.isHero = false;
        character.isCharAttack = true;
        character.areaIndex = areaIndex;
        
        character.attackPower = data.attackPower;
        character.attackSpeed = data.attackSpeed;
        character.attackRange = data.attackRange;
        character.currentHP = data.maxHP;
        character.star = data.initialStar;
        character.ApplyStarVisual();
        character.moveSpeed = data.moveSpeed;
        
        RouteType selectedRoute = RouteManager.Instance.DetermineRouteFromTile(tile, spawner2);
        RouteManager.Instance.OnRouteSelectedRegion2(character, tile, selectedRoute, spawner2);
    }

    /// <summary>
    /// 타워형 캐릭터 설정
    /// </summary>
    private void ConfigureCharacterForTower(Character character, CharacterData data, Tile tile, bool isArea2)
    {
        var coreData = CoreDataManager.Instance;
        
        character.currentTile = tile;
        character.isHero = false;
        character.isCharAttack = false;
        character.areaIndex = isArea2 ? 2 : 1;
        
        character.currentWaypointIndex = -1;
        character.maxWaypointIndex = 6;
        
        character.attackPower = data.attackPower;
        character.attackSpeed = data.attackSpeed;
        character.attackRange = data.attackRange;
        character.currentHP = data.maxHP;
        character.star = data.initialStar;
        character.ApplyStarVisual();
        
        if (isArea2 && coreData.opponentBulletPanel != null)
        {
            character.opponentBulletPanel = coreData.opponentBulletPanel;
        }
        else
        {
            character.SetBulletPanel(coreData.bulletPanel);
        }
    }

    /// <summary>
    /// 캐릭터를 타일 위치에 배치
    /// </summary>
    private void PositionCharacterOnTile(GameObject charObj, Tile tile, RectTransform targetParent)
    {
        RectTransform tileRect = tile.GetComponent<RectTransform>();
        RectTransform charRect = charObj.GetComponent<RectTransform>();
        
        if (tileRect != null && charRect != null)
        {
            Vector2 localPos = targetParent.InverseTransformPoint(tileRect.transform.position);
            charRect.anchoredPosition = localPos;
            charRect.localRotation = Quaternion.identity;
        }
        else
        {
            charObj.transform.position = tile.transform.position;
            charObj.transform.localRotation = Quaternion.identity;
        }
    }

    // 기존 메서드들은 그대로 유지
    public void OnClickSelectUnit(int index)
    {
        CoreDataManager.Instance.currentCharacterIndex = index;
        Debug.Log($"[SummonManager] 선택된 유닛 인덱스: {index}");
    }

    public void ToggleRemoveMode()
    {
        removeMode = !removeMode;
        Debug.Log($"[SummonManager] removeMode = {removeMode}");

        if (removeMode)
        {
            RemoveRandomCharacter();
        }
    }

    private void RemoveRandomCharacter()
    {
        List<Character> placedCharacters = new List<Character>();
        Character[] allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);

        foreach (var c in allCharacters)
        {
            if (c != null && c.currentTile != null)
            {
                Tile t = c.currentTile;
                if (t.IsPlaceTile() || t.IsPlaced2() || t.IsPlacable() || t.IsPlacable2())
                {
                    placedCharacters.Add(c);
                }
            }
        }

        if (placedCharacters.Count == 0)
        {
            Debug.Log("[SummonManager] 제거할 캐릭터가 없습니다.");
            return;
        }

        int randomIndex = Random.Range(0, placedCharacters.Count);
        Character targetCharacter = placedCharacters[randomIndex];
        Tile targetTile = targetCharacter.currentTile;

        if (targetTile == null)
        {
            Debug.LogWarning("[SummonManager] 선택된 캐릭터의 타일이 null입니다.");
            return;
        }

        Debug.Log($"[SummonManager] 랜덤 제거 대상: {targetCharacter.characterName} (별:{targetCharacter.star}, 타일:{targetTile.name})");
        RemoveCharacterOnTile(targetTile);
        removeMode = false;
    }

    public void PlaceCharacterOnTile(Tile tile)
    {
        var coreData = CoreDataManager.Instance;
        
        Debug.Log($"[SummonManager] PlaceCharacterOnTile 호출 - tile: {tile.name}, currentCharacterIndex: {coreData.currentCharacterIndex}, removeMode: {removeMode}");
        
        if (removeMode)
        {
            RemoveCharacterOnTile(tile);
            return;
        }

        if (coreData.characterDatabase == null
            || coreData.characterDatabase.currentRegisteredCharacters == null
            || coreData.characterDatabase.currentRegisteredCharacters.Length == 0)
        {
            Debug.LogWarning("[SummonManager] characterDatabase가 비어있어 배치 불가");
            return;
        }

        if (coreData.currentCharacterIndex < 0
            || coreData.currentCharacterIndex >= coreData.characterDatabase.currentRegisteredCharacters.Length)
        {
            Debug.LogWarning($"[SummonManager] 잘못된 인덱스({coreData.currentCharacterIndex}) => 배치 불가. 캐릭터를 먼저 선택하세요!");
            return;
        }

        CharacterData data = coreData.characterDatabase.currentRegisteredCharacters[coreData.currentCharacterIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"[SummonManager] [{coreData.currentCharacterIndex}]번 캐릭터 spawnPrefab이 null => 배치 불가");
            return;
        }
        if (tile == null)
        {
            Debug.LogWarning("[SummonManager] tile이 null => 배치 불가");
            return;
        }

        // 타일이 이미 점유되어 있는지 확인
        if (tile.IsPlaceTile() || tile.IsPlaced2() || tile.IsPlacable() || tile.IsPlacable2())
        {
            bool currentTileOccupied = false;
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
                foreach (var c in allChars)
                {
                    if (c != null && c.currentTile == tile)
                    {
                        currentTileOccupied = true;
                        break;
                    }
                }
            }
            else
            {
                currentTileOccupied = TileManager.Instance.CheckAnyCharacterHasCurrentTile(tile);
            }
            
            if (currentTileOccupied)
            {
                Debug.Log($"[SummonManager] {tile.name}에 이미 캐릭터가 있음. 빈 타일을 찾거나 walkable로 전환합니다.");
                
                Tile emptyTile = TileManager.Instance.FindEmptyPlacedOrPlacableTile(tile.isRegion2);
                if (emptyTile != null)
                {
                    Debug.Log($"[SummonManager] 빈 타일 {emptyTile.name}을 찾았습니다. 해당 타일에 배치합니다.");
                    tile = emptyTile;
                }
                else
                {
                    Debug.Log($"[SummonManager] 빈 placed/placable 타일이 없습니다. walkable 타일로 전환합니다.");
                    Tile walkableTile = TileManager.Instance.FindEmptyWalkableTile(tile.isRegion2);
                    if (walkableTile != null)
                    {
                        tile = walkableTile;
                        Debug.Log($"[SummonManager] walkable 타일 {tile.name}에 배치합니다.");
                    }
                    else
                    {
                        Debug.LogWarning("[SummonManager] 배치 가능한 타일이 없습니다!");
                        return;
                    }
                }
            }
        }

        bool isArea2 = (tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right() || tile.IsPlacable2() || tile.IsPlaced2());
        
        MineralBar targetMineralBar = isArea2 ? coreData.region2MineralBar : coreData.region1MineralBar;
        if (targetMineralBar != null)
        {
            Debug.Log($"[SummonManager] 미네랄 체크 - 현재: {targetMineralBar.GetCurrentMinerals()}, 필요: {data.cost}, 충분: {targetMineralBar.GetCurrentMinerals() >= data.cost}");
        }

        // 중복 배치 방지
        bool hasActualCharacter = false;
        if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            foreach (var c in allChars)
            {
                if (c != null && c.currentTile == tile)
                {
                    hasActualCharacter = true;
                    break;
                }
            }
            
            if (hasActualCharacter)
            {
                Debug.LogWarning($"[SummonManager] {tile.name} placed tile에 이미 캐릭터가 있습니다. 배치 불가!");
                return;
            }
        }
        else
        {
            if (TileManager.Instance.CheckAnyCharacterHasCurrentTile(tile))
            {
                Debug.LogWarning($"[SummonManager] {tile.name} 타일은 이미 캐릭터가 점유 중입니다. 배치 불가!");
                return;
            }
        }

        // 모든 placed/placable 타일이 차있으면 walkable 타일로 전환
        if (tile.IsPlaceTile() || tile.IsPlaced2() || tile.IsPlacable() || tile.IsPlacable2())
        {
            bool allPlacedTilesFull = true;
            Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
            Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            
            foreach (var t in allTiles)
            {
                if (t == null) continue;
                if (t.isRegion2 != tile.isRegion2) continue;
                
                if (t.IsPlaceTile() || t.IsPlaced2() || t.IsPlacable() || t.IsPlacable2())
                {
                    bool isEmpty = true;
                    
                    if (t.IsPlaceTile() || t.IsPlaced2())
                    {
                        foreach (var c in allChars)
                        {
                            if (c != null && c.currentTile == t)
                            {
                                isEmpty = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        isEmpty = !TileManager.Instance.CheckAnyCharacterHasCurrentTile(t);
                    }
                    
                    if (isEmpty)
                    {
                        allPlacedTilesFull = false;
                        break;
                    }
                }
            }
            
            if (allPlacedTilesFull)
            {
                Debug.Log($"[SummonManager] 모든 placed/placable 타일이 가득 참. walkable 타일로 자동 전환합니다.");
                Tile walkableTile = TileManager.Instance.FindEmptyWalkableTile(tile.isRegion2);
                if (walkableTile != null)
                {
                    tile = walkableTile;
                    Debug.Log($"[SummonManager] walkable 타일 {tile.name}으로 배치 변경");
                }
                else
                {
                    Debug.LogWarning("[SummonManager] walkable 타일도 없습니다!");
                    return;
                }
            }
        }
        
        if (isArea2 && coreData.isHost)
        {
            Debug.LogWarning("[SummonManager] 지역2에는 (호스트) 배치 불가");
            return;
        }
        if (!isArea2 && !coreData.isHost)
        {
            Debug.LogWarning("[SummonManager] 지역1에는 (클라이언트/AI) 배치 불가");
            return;
        }

        // 미네랄 체크
        if (isArea2)
        {
            if (coreData.region2MineralBar != null)
            {
                if (!coreData.region2MineralBar.TrySpend(data.cost))
                {
                    Debug.Log($"[SummonManager] (지역2) 미네랄 부족! (cost={data.cost})");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[SummonManager] region2MineralBar가 null => 배치 불가");
                return;
            }
        }
        else
        {
            if (coreData.region1MineralBar != null)
            {
                if (!coreData.region1MineralBar.TrySpend(data.cost))
                {
                    Debug.Log($"[SummonManager] (지역1) 미네랄 부족! (cost={data.cost})");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[SummonManager] region1MineralBar가 null => 배치 불가");
                return;
            }
        }

        if (!tile.CanPlaceCharacter())
        {
            Debug.LogWarning($"[SummonManager] {tile.name} => 배치 불가능한 상태");
            return;
        }

        // 실제 소환 로직
        ActualSummon(data, tile, coreData.currentCharacterIndex, isArea2);
    }

    private void ActualSummon(CharacterData data, Tile tile, int characterIndex, bool isArea2)
    {
        var coreData = CoreDataManager.Instance;
        
        if (tile.IsWalkable() || tile.IsWalkableLeft() || tile.IsWalkableCenter() || tile.IsWalkableRight())
        {
            WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null && coreData.ourMonsterPanel != null)
            {
                GameObject allyObj = Instantiate(data.spawnPrefab, coreData.ourMonsterPanel);
                if (allyObj != null)
                {
                    allyObj.transform.position = new Vector3(-1000, -1000, 0);
                    
                    Character allyCharacter = allyObj.GetComponent<Character>();
                    allyCharacter.currentTile = null;
                    allyCharacter.isHero = (characterIndex == 9);
                    allyCharacter.isCharAttack = !allyCharacter.isHero;
                    allyCharacter.areaIndex = 1;
                    
                    allyCharacter.attackPower = data.attackPower;
                    allyCharacter.attackSpeed = data.attackSpeed;
                    allyCharacter.attackRange = data.attackRange;
                    allyCharacter.currentHP = data.maxHP;
                    allyCharacter.star = data.initialStar;
                    allyCharacter.ApplyStarVisual();
                    allyCharacter.moveSpeed = data.moveSpeed;
                    
                    RouteType selectedRoute = RouteManager.Instance.DetermineRouteFromTile(tile, spawner);
                    RouteManager.Instance.OnRouteSelected(allyCharacter, tile, selectedRoute, spawner);
                    
                    var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                    if (selectUI != null)
                    {
                        selectUI.MarkCardAsUsed(characterIndex);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[SummonManager] WaveSpawner/ourMonsterPanel이 없어 소환 실패");
            }
        }
        else if (tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right())
        {
            WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
            if (spawner2 != null && coreData.opponentOurMonsterPanel != null)
            {
                RouteType selectedRoute = RouteManager.Instance.DetermineRouteFromTile(tile, spawner2);
                Transform[] waypoints = RouteManager.Instance.GetWaypointsForRoute(spawner2, selectedRoute);
                
                if (waypoints == null || waypoints.Length == 0)
                {
                    Debug.LogWarning($"[SummonManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
                    return;
                }
                
                Vector3 spawnPos = RouteManager.Instance.GetSpawnPositionForRoute(spawner2, selectedRoute);
                GameObject enemyObj = Instantiate(data.spawnPrefab, coreData.opponentOurMonsterPanel);
                if (enemyObj != null)
                {
                    enemyObj.SetActive(true);
                    RectTransform enemyRect = enemyObj.GetComponent<RectTransform>();
                    if (enemyRect != null)
                    {
                        Vector2 localPos = coreData.opponentOurMonsterPanel.InverseTransformPoint(spawnPos);
                        enemyRect.SetParent(coreData.opponentOurMonsterPanel, false);
                        enemyRect.anchoredPosition = localPos;
                        enemyRect.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        enemyObj.transform.SetParent(null);
                        enemyObj.transform.position = spawnPos;
                        enemyObj.transform.localRotation = Quaternion.identity;
                    }

                    Character enemyCharacter = enemyObj.GetComponent<Character>();
                    enemyCharacter.currentTile = null;
                    enemyCharacter.isHero = false;
                    enemyCharacter.isCharAttack = true;

                    enemyCharacter.currentWaypointIndex = 0;
                    enemyCharacter.pathWaypoints = waypoints;
                    enemyCharacter.maxWaypointIndex = waypoints.Length - 1;
                    enemyCharacter.areaIndex = 2;
                    enemyCharacter.selectedRoute = selectedRoute;

                    enemyCharacter.attackPower = data.attackPower;
                    enemyCharacter.attackSpeed = data.attackSpeed;
                    enemyCharacter.attackRange = data.attackRange;
                    enemyCharacter.currentHP = data.maxHP;
                    enemyCharacter.star = data.initialStar;
                    enemyCharacter.ApplyStarVisual();
                    enemyCharacter.moveSpeed = data.moveSpeed;

                    Debug.Log($"[SummonManager] [{data.characterName}] (지역2) 몬스터 소환 - {selectedRoute} 루트 선택 (cost={data.cost})");

                    var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                    if (selectUI != null)
                    {
                        selectUI.MarkCardAsUsed(coreData.currentCharacterIndex);
                    }
                    return;
                }
                else
                {
                    Debug.LogError("[SummonManager] Walkable2 => Instantiate 실패");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[SummonManager] WaveSpawnerRegion2/enemyMonsterPanel이 없어 소환 실패");
                return;
            }
        }
        else if (tile.IsPlacable() || tile.IsPlacable2())
        {
            RectTransform targetParent = tile.IsPlacable2() && (coreData.opponentCharacterPanel != null)
                ? coreData.opponentCharacterPanel
                : coreData.characterPanel;

            GameObject charObj = Instantiate(data.spawnPrefab, targetParent);
            if (charObj != null)
            {
                RectTransform tileRect = tile.GetComponent<RectTransform>();
                RectTransform charRect = charObj.GetComponent<RectTransform>();
                if (tileRect != null && charRect != null)
                {
                    Vector2 localPos = targetParent.InverseTransformPoint(tileRect.transform.position);
                    charRect.anchoredPosition = localPos;
                    charRect.localRotation = Quaternion.identity;
                }
                else
                {
                    charObj.transform.position = tile.transform.position;
                    charObj.transform.localRotation = Quaternion.identity;
                }

                Character characterComp = charObj.GetComponent<Character>();
                if (characterComp != null)
                {
                    characterComp.currentTile = tile;
                    characterComp.isHero = (characterIndex == 9);
                    characterComp.isCharAttack = false;

                    characterComp.currentWaypointIndex = -1;
                    characterComp.maxWaypointIndex = 6;

                    characterComp.attackPower = data.attackPower;
                    characterComp.attackSpeed = data.attackSpeed;
                    characterComp.attackRange = data.attackRange;
                    characterComp.currentHP = data.maxHP;
                    characterComp.star = data.initialStar;
                    characterComp.ApplyStarVisual();

                    characterComp.areaIndex = tile.IsPlacable2() ? 2 : 1;

                    if (tile.IsPlacable2() && coreData.opponentBulletPanel != null)
                    {
                        characterComp.opponentBulletPanel = coreData.opponentBulletPanel;
                    }
                    else
                    {
                        characterComp.SetBulletPanel(coreData.bulletPanel);
                    }
                }

                TileManager.Instance.CreatePlaceTileChild(tile);

                var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                if (selectUI != null)
                {
                    selectUI.MarkCardAsUsed(characterIndex);
                }

                Debug.Log($"[SummonManager] [{data.characterName}] 배치 완료 (cost={data.cost})");
            }
        }
        else if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            RectTransform targetParent = tile.IsPlaced2() && (coreData.opponentCharacterPanel != null)
                ? coreData.opponentCharacterPanel
                : coreData.characterPanel;

            GameObject charObj = Instantiate(data.spawnPrefab, targetParent);
            if (charObj != null)
            {
                RectTransform tileRect = tile.GetComponent<RectTransform>();
                RectTransform charRect = charObj.GetComponent<RectTransform>();
                if (tileRect != null && charRect != null)
                {
                    Vector2 localPos = targetParent.InverseTransformPoint(tileRect.transform.position);
                    charRect.anchoredPosition = localPos;
                    charRect.localRotation = Quaternion.identity;
                }
                else
                {
                    charObj.transform.position = tile.transform.position;
                    charObj.transform.localRotation = Quaternion.identity;
                }

                Character characterComp = charObj.GetComponent<Character>();
                if (characterComp != null)
                {
                    characterComp.currentTile = tile;
                    characterComp.isHero = (characterIndex == 9);
                    characterComp.isCharAttack = false;

                    characterComp.currentWaypointIndex = -1;
                    characterComp.maxWaypointIndex = 6;

                    characterComp.attackPower = data.attackPower;
                    characterComp.attackSpeed = data.attackSpeed;
                    characterComp.attackRange = data.attackRange;
                    characterComp.currentHP = data.maxHP;
                    characterComp.star = data.initialStar;
                    characterComp.ApplyStarVisual();

                    characterComp.areaIndex = tile.IsPlaced2() ? 2 : 1;

                    if (tile.IsPlaced2() && coreData.opponentBulletPanel != null)
                    {
                        characterComp.opponentBulletPanel = coreData.opponentBulletPanel;
                    }
                    else
                    {
                        characterComp.SetBulletPanel(coreData.bulletPanel);
                    }
                }

                TileManager.Instance.CreatePlaceTileChild(tile);

                var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                if (selectUI != null)
                {
                    selectUI.MarkCardAsUsed(characterIndex);
                }

                Debug.Log($"[SummonManager] [{data.characterName}] 배치 완료 (on PlaceTile/Placed2, cost={data.cost})");
            }
        }
        else
        {
            Debug.LogWarning($"[SummonManager] {tile.name} 상태를 처리할 수 없습니다.");
        }
    }

    public bool SummonCharacterOnTile(int summonIndex, Tile tile, bool forceEnemyArea2 = false)
    {
        var coreData = CoreDataManager.Instance;
        
        Debug.Log($"[SummonManager] SummonCharacterOnTile: 인덱스={summonIndex}, tile={tile.name}, forceEnemyArea2={forceEnemyArea2}");

        bool success = false;

        try
        {
            CharacterData data;
            if (forceEnemyArea2 && coreData.enemyDatabase != null && coreData.enemyDatabase.characters != null)
            {
                if (summonIndex < 0 || summonIndex >= coreData.enemyDatabase.characters.Length)
                {
                    Debug.LogWarning($"[SummonManager] 잘못된 소환 인덱스({summonIndex}) => 실패");
                    return false;
                }
                data = coreData.enemyDatabase.characters[summonIndex];
                Debug.Log($"[SummonManager] enemyDatabase에서 데이터 가져옴: {data.characterName}, spawnPrefab={(data.spawnPrefab != null)}");
            }
            else
            {
                if (coreData.characterDatabase == null || coreData.characterDatabase.currentRegisteredCharacters == null
                    || coreData.characterDatabase.currentRegisteredCharacters.Length == 0)
                {
                    Debug.LogWarning("[SummonManager] characterDatabase가 비어있음 => 소환 불가");
                    return false;
                }
                if (summonIndex < 0 || summonIndex >= coreData.characterDatabase.currentRegisteredCharacters.Length)
                {
                    Debug.LogWarning($"[SummonManager] 잘못된 소환 인덱스({summonIndex}) => 소환 불가");
                    return false;
                }
                data = coreData.characterDatabase.currentRegisteredCharacters[summonIndex];
            }

            if (data == null || data.spawnPrefab == null)
            {
                Debug.LogWarning($"[SummonManager] [{summonIndex}]번 캐릭터 spawnPrefab이 null => 소환 불가");
                return false;
            }
            if (tile == null)
            {
                Debug.LogWarning("[SummonManager] tile이 null => 소환 불가");
                return false;
            }

            bool tileIsArea2 = tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right() || tile.IsPlacable2() || tile.IsPlaced2();
            if (!forceEnemyArea2)
            {
                if (tileIsArea2 && coreData.isHost)
                {
                    Debug.LogWarning("[SummonManager] 지역2에는 호스트 배치 불가");
                    return false;
                }
                if (!tileIsArea2 && !coreData.isHost)
                {
                    Debug.LogWarning("[SummonManager] 지역1에는 클라이언트/AI 배치 불가");
                    return false;
                }
            }

            MineralBar targetMineralBar = (forceEnemyArea2 || tileIsArea2) ? coreData.region2MineralBar : coreData.region1MineralBar;
            bool mineralsSpent = false;

            if (targetMineralBar != null)
            {
                mineralsSpent = targetMineralBar.TrySpend(data.cost);
                if (!mineralsSpent)
                {
                    Debug.Log($"[SummonManager] ({(tileIsArea2 ? "지역2" : "지역1")}) 미네랄 부족!(cost={data.cost})");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"[SummonManager] {(tileIsArea2 ? "region2MineralBar" : "region1MineralBar")}가 null => 소환 불가");
                return false;
            }

            if (!tile.CanPlaceCharacter())
            {
                if (mineralsSpent && targetMineralBar != null)
                {
                    targetMineralBar.RefundMinerals(data.cost);
                    Debug.Log($"[SummonManager] 소환 불가로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                }
                Debug.LogWarning($"[SummonManager] {tile.name} => 배치 불가(조건 불충족)");
                return false;
            }

            // 타일에 이미 캐릭터가 있는지 확인
            bool hasActualCharacter = false;
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
                foreach (var c in allChars)
                {
                    if (c != null && c.currentTile == tile)
                    {
                        hasActualCharacter = true;
                        break;
                    }
                }
                
                if (hasActualCharacter)
                {
                    Debug.LogWarning($"[SummonManager] {tile.name} placed tile에 이미 캐릭터가 있어 소환 불가!");
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                        Debug.Log($"[SummonManager] 소환 불가로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                    }
                    return false;
                }
            }
            else
            {
                if (TileManager.Instance.CheckAnyCharacterHasCurrentTile(tile))
                {
                    Debug.LogWarning($"[SummonManager] {tile.name}는 이미 캐릭터가 있어 소환 불가!");
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                        Debug.Log($"[SummonManager] 소환 불가로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                    }
                    return false;
                }
            }

            // 실제 소환
            success = ActualSummonDrag(data, tile, summonIndex, tileIsArea2, forceEnemyArea2);

            if (!success && mineralsSpent && targetMineralBar != null)
            {
                targetMineralBar.RefundMinerals(data.cost);
                Debug.Log($"[SummonManager] 소환 실패로 미네랄 {data.cost} 환불");
            }

            return success;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SummonManager] 소환 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    private bool ActualSummonDrag(CharacterData data, Tile tile, int summonIndex, bool tileIsArea2, bool forceEnemyArea2)
    {
        var coreData = CoreDataManager.Instance;
        
        if (tile.IsWalkable() || tile.IsWalkableLeft() || tile.IsWalkableCenter() || tile.IsWalkableRight())
        {
            WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner == null || coreData.ourMonsterPanel == null)
            {
                Debug.LogWarning("[SummonManager] Walkable => 소환 실패");
                return false;
            }

            GameObject prefabToSpawn = data.spawnPrefab;
            Character cc = prefabToSpawn.GetComponent<Character>();
            if (cc == null)
            {
                Debug.LogError($"[SummonManager] '{prefabToSpawn.name}' 프리팹에 Character 없음 => 실패");
                return false;
            }

            RouteType selectedRoute = RouteManager.Instance.DetermineRouteFromTile(tile, spawner);
            Transform[] waypoints = RouteManager.Instance.GetWaypointsForRoute(spawner, selectedRoute);
            
            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogWarning($"[SummonManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
                return false;
            }

            Vector3 spawnPos = RouteManager.Instance.GetSpawnPositionForRoute(spawner, selectedRoute);
            GameObject allyMonsterObj = Instantiate(prefabToSpawn, coreData.ourMonsterPanel);
            if (allyMonsterObj != null)
            {
                RectTransform allyRect = allyMonsterObj.GetComponent<RectTransform>();
                if (allyRect != null)
                {
                    Vector2 localPos = coreData.ourMonsterPanel.InverseTransformPoint(spawnPos);
                    allyRect.SetParent(coreData.ourMonsterPanel, false);
                    allyRect.anchoredPosition = localPos;
                    allyRect.localRotation = Quaternion.identity;
                }
                else
                {
                    allyMonsterObj.transform.SetParent(null);
                    allyMonsterObj.transform.position = spawnPos;
                    allyMonsterObj.transform.localRotation = Quaternion.identity;
                }

                Character allyChar = allyMonsterObj.GetComponent<Character>();
                allyChar.currentTile = null;
                allyChar.isHero = (summonIndex == 9);
                allyChar.isCharAttack = !allyChar.isHero;

                allyChar.currentWaypointIndex = 0;
                allyChar.pathWaypoints = waypoints;
                allyChar.maxWaypointIndex = waypoints.Length - 1;
                allyChar.areaIndex = 1;
                allyChar.selectedRoute = selectedRoute;

                allyChar.attackPower = data.attackPower;
                allyChar.attackSpeed = data.attackSpeed;
                allyChar.attackRange = data.attackRange;
                allyChar.currentHP = data.maxHP;
                allyChar.star = data.initialStar;
                allyChar.ApplyStarVisual();
                allyChar.moveSpeed = data.moveSpeed;

                Debug.Log($"[SummonManager] 드래그로 [{data.characterName}] (area1) 몬스터 소환 - {selectedRoute} 루트 선택 (cost={data.cost})");
                return true;
            }
            else
            {
                Debug.LogError("[SummonManager] Walkable => Instantiate 실패");
                return false;
            }
        }
        else if (tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right())
        {
            WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
            if (spawner2 != null && coreData.opponentOurMonsterPanel != null)
            {
                RouteType selectedRoute = RouteManager.Instance.DetermineRouteFromTile(tile, spawner2);
                Transform[] waypoints = RouteManager.Instance.GetWaypointsForRoute(spawner2, selectedRoute);
                
                if (waypoints == null || waypoints.Length == 0)
                {
                    Debug.LogWarning($"[SummonManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
                    return false;
                }
                
                Vector3 spawnPos = RouteManager.Instance.GetSpawnPositionForRoute(spawner2, selectedRoute);
                GameObject enemyObj = Instantiate(data.spawnPrefab, coreData.opponentOurMonsterPanel);
                if (enemyObj != null)
                {
                    enemyObj.SetActive(true);
                    RectTransform enemyRect = enemyObj.GetComponent<RectTransform>();
                    if (enemyRect != null)
                    {
                        Vector2 localPos = coreData.opponentOurMonsterPanel.InverseTransformPoint(spawnPos);
                        enemyRect.SetParent(coreData.opponentOurMonsterPanel, false);
                        enemyRect.anchoredPosition = localPos;
                        enemyRect.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        enemyObj.transform.SetParent(null);
                        enemyObj.transform.position = spawnPos;
                        enemyObj.transform.localRotation = Quaternion.identity;
                    }

                    Character enemyCharacter = enemyObj.GetComponent<Character>();
                    enemyCharacter.currentTile = null;
                    enemyCharacter.isHero = false;
                    enemyCharacter.isCharAttack = true;

                    enemyCharacter.currentWaypointIndex = 0;
                    enemyCharacter.pathWaypoints = waypoints;
                    enemyCharacter.maxWaypointIndex = waypoints.Length - 1;
                    enemyCharacter.areaIndex = 2;
                    enemyCharacter.selectedRoute = selectedRoute;

                    enemyCharacter.attackPower = data.attackPower;
                    enemyCharacter.attackSpeed = data.attackSpeed;
                    enemyCharacter.attackRange = data.attackRange;
                    enemyCharacter.currentHP = data.maxHP;
                    enemyCharacter.star = data.initialStar;
                    enemyCharacter.ApplyStarVisual();
                    enemyCharacter.moveSpeed = data.moveSpeed;

                    Debug.Log($"[SummonManager] [{data.characterName}] (지역2) 몬스터 소환 - {selectedRoute} 루트 선택 (cost={data.cost})");

                    var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                    if (selectUI != null)
                    {
                        selectUI.MarkCardAsUsed(coreData.currentCharacterIndex);
                    }
                    return true;
                }
                else
                {
                    Debug.LogError("[SummonManager] Walkable2 => Instantiate 실패");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("[SummonManager] WaveSpawnerRegion2/enemyMonsterPanel이 없어 소환 실패");
                return false;
            }
        }
        else if (tile.IsPlacable() || tile.IsPlacable2())
        {
            bool isArea2Tile = tile.IsPlacable2();
            RectTransform targetParent = isArea2Tile
                ? (coreData.opponentCharacterPanel != null ? coreData.opponentCharacterPanel : coreData.characterPanel)
                : coreData.characterPanel;

            GameObject newCharObj = Instantiate(data.spawnPrefab, targetParent);
            if (newCharObj != null)
            {
                RectTransform tileRect = tile.GetComponent<RectTransform>();
                RectTransform charRect = newCharObj.GetComponent<RectTransform>();
                if (tileRect != null && charRect != null)
                {
                    Vector2 localPos = targetParent.InverseTransformPoint(tileRect.transform.position);
                    charRect.anchoredPosition = localPos;
                    charRect.localRotation = Quaternion.identity;
                }
                else
                {
                    newCharObj.transform.position = tile.transform.position;
                    newCharObj.transform.localRotation = Quaternion.identity;
                }

                Character cComp = newCharObj.GetComponent<Character>();
                if (cComp != null)
                {
                    cComp.currentTile = tile;
                    cComp.isHero = (summonIndex == 9);
                    cComp.isCharAttack = false;

                    cComp.currentWaypointIndex = -1;
                    cComp.maxWaypointIndex = 6;

                    cComp.attackPower = data.attackPower;
                    cComp.attackSpeed = data.attackSpeed;
                    cComp.attackRange = data.attackRange;
                    cComp.currentHP = data.maxHP;
                    cComp.star = data.initialStar;
                    cComp.ApplyStarVisual();

                    cComp.areaIndex = isArea2Tile ? 2 : 1;

                    if (isArea2Tile && coreData.opponentBulletPanel != null)
                    {
                        cComp.opponentBulletPanel = coreData.opponentBulletPanel;
                    }
                    else
                    {
                        cComp.SetBulletPanel(coreData.bulletPanel);
                    }
                }

                TileManager.Instance.CreatePlaceTileChild(tile);
                Debug.Log($"[SummonManager] [{data.characterName}] 드래그 배치(Placable/Placable2)");
                return true;
            }
            else
            {
                Debug.LogError("[SummonManager] placable => Instantiate 실패");
                return false;
            }
        }
        else if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            bool isArea2Tile = tile.IsPlaced2();
            RectTransform targetParent = isArea2Tile
                ? (coreData.opponentCharacterPanel != null ? coreData.opponentCharacterPanel : coreData.characterPanel)
                : coreData.characterPanel;

            GameObject newCharObj = Instantiate(data.spawnPrefab, targetParent);
            if (newCharObj != null)
            {
                RectTransform tileRect = tile.GetComponent<RectTransform>();
                RectTransform charRect = newCharObj.GetComponent<RectTransform>();

                if (tileRect != null && charRect != null)
                {
                    Vector2 localPos = targetParent.InverseTransformPoint(tileRect.transform.position);
                    charRect.anchoredPosition = localPos;
                    charRect.localRotation = Quaternion.identity;
                }
                else
                {
                    newCharObj.transform.position = tile.transform.position;
                    newCharObj.transform.localRotation = Quaternion.identity;
                }

                Character cComp = newCharObj.GetComponent<Character>();
                if (cComp != null)
                {
                    cComp.currentTile = tile;
                    cComp.isHero = (summonIndex == 9);
                    cComp.isCharAttack = false;

                    cComp.currentWaypointIndex = -1;
                    cComp.maxWaypointIndex = 6;

                    cComp.attackPower = data.attackPower;
                    cComp.attackSpeed = data.attackSpeed;
                    cComp.attackRange = data.attackRange;
                    cComp.currentHP = data.maxHP;
                    cComp.star = data.initialStar;
                    cComp.ApplyStarVisual();

                    cComp.areaIndex = isArea2Tile ? 2 : 1;

                    if (isArea2Tile && coreData.opponentBulletPanel != null)
                    {
                        cComp.opponentBulletPanel = coreData.opponentBulletPanel;
                    }
                    else
                    {
                        cComp.SetBulletPanel(coreData.bulletPanel);
                    }
                }

                TileManager.Instance.CreatePlaceTileChild(tile);
                Debug.Log($"[SummonManager] [{data.characterName}] 드래그 배치(PlaceTile/Placed2)");
                return true;
            }
            else
            {
                Debug.LogError("[SummonManager] PlaceTile/Placed2 => Instantiate 실패");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"[SummonManager] {tile.name} 상태를 처리할 수 없습니다 (드래그 소환).");
            return false;
        }
    }

    public void RemoveCharacterOnTile(Tile tile)
    {
        if (tile == null) return;

        Character occupant = null;
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var c in allChars)
        {
            if (c != null && c.currentTile == tile)
            {
                occupant = c;
                break;
            }
        }

        if (occupant == null)
        {
            Debug.LogWarning($"[SummonManager] {tile.name}에 캐릭터가 없어서 제거 불가");
            return;
        }

        int cost = 10;
        var coreData = CoreDataManager.Instance;

        if (coreData.characterDatabase != null && coreData.characterDatabase.currentRegisteredCharacters != null)
        {
            for (int i = 0; i < coreData.characterDatabase.currentRegisteredCharacters.Length; i++)
            {
                CharacterData data = coreData.characterDatabase.currentRegisteredCharacters[i];
                if (data != null && data.characterName == occupant.characterName)
                {
                    cost = data.cost;
                    Debug.Log($"[SummonManager] '{occupant.characterName}'의 코스트 정보 찾음: {cost}");
                    break;
                }
            }
        }

        if (occupant.areaIndex == 2 && coreData.enemyDatabase != null && coreData.enemyDatabase.characters != null)
        {
            for (int i = 0; i < coreData.enemyDatabase.characters.Length; i++)
            {
                CharacterData data = coreData.enemyDatabase.characters[i];
                if (data != null && data.characterName == occupant.characterName)
                {
                    cost = data.cost;
                    Debug.Log($"[SummonManager] '{occupant.characterName}'의 코스트 정보 찾음 (enemyDB): {cost}");
                    break;
                }
            }
        }

        float ratio = 0f;
        switch (occupant.star)
        {
            case CharacterStar.OneStar:
                ratio = 0.9f;
                break;
            case CharacterStar.TwoStar:
                ratio = 0.5f;
                break;
            case CharacterStar.ThreeStar:
                ratio = 0.1f;
                break;
        }

        int halfCost = cost / 2;
        float refundFloat = halfCost * ratio;
        int finalRefund = Mathf.FloorToInt(refundFloat);

        if (occupant.areaIndex == 1 && coreData.region1MineralBar != null)
        {
            coreData.region1MineralBar.RefundMinerals(finalRefund);
            Debug.Log($"[SummonManager] area1 캐릭터 제거 => 미네랄 {finalRefund} 환급 (코스트 {cost}/2 * {ratio * 100}%)");
        }
        else if (occupant.areaIndex == 2 && coreData.region2MineralBar != null)
        {
            coreData.region2MineralBar.RefundMinerals(finalRefund);
            Debug.Log($"[SummonManager] area2 캐릭터 제거 => 미네랄 {finalRefund} 환급 (코스트 {cost}/2 * {ratio * 100}%)");
        }

        occupant.currentTile = null;
        Destroy(occupant.gameObject);

        if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            tile.RefreshTileVisual();
            Debug.Log($"[SummonManager] placed tile {tile.name}에서 캐릭터 제거 후 비주얼 업데이트");
        }
        else
        {
            TileManager.Instance.RemovePlaceTileChild(tile);
        }
        
        TileManager.Instance.OnCharacterRemovedFromTile(tile);

        Debug.Log($"[SummonManager] {tile.name} 타일의 캐릭터 제거 완료 (Star={occupant.star})");
    }

    public void OnClickAutoPlace()
    {
        var coreData = CoreDataManager.Instance;
        
        if (coreData.currentCharacterIndex < 0)
        {
            Debug.LogWarning("[SummonManager] 캐릭터를 먼저 선택하세요!");
            return;
        }
        
        bool targetRegion2 = !coreData.isHost;
        
        Tile targetTile = TileManager.Instance.FindEmptyPlacedOrPlacableTile(targetRegion2);
        
        if (targetTile == null)
        {
            Debug.Log("[SummonManager] placed/placable 타일이 모두 꽉 찼습니다. walkable 타일로 전환합니다.");
            targetTile = TileManager.Instance.FindEmptyWalkableTile(targetRegion2);
        }
        
        if (targetTile != null)
        {
            PlaceCharacterOnTile(targetTile);
        }
        else
        {
            Debug.LogWarning("[SummonManager] 배치 가능한 타일이 없습니다!");
        }
    }

    public int FindCharacterIndexInEnemyDatabase(CharacterData character)
    {
        var coreData = CoreDataManager.Instance;
        
        if (coreData.enemyDatabase == null || coreData.enemyDatabase.characters == null)
        {
            Debug.LogWarning("[SummonManager] enemyDatabase가 null이거나 비어있습니다.");
            return -1;
        }

        for (int i = 0; i < coreData.enemyDatabase.characters.Length; i++)
        {
            CharacterData data = coreData.enemyDatabase.characters[i];
            if (data != null && data.characterName == character.characterName)
            {
                return i;
            }
        }
        return -1;
    }
}