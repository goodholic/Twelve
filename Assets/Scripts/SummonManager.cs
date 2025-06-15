using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 캐릭터 소환 관리
/// 게임 기획서: 원 버튼 소환, 타일 기반 배치
/// ★★★ 수정: 같은 캐릭터끼리는 한 타일에 최대 3개까지 배치 가능
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
        // CoreDataManager 초기화 확인
        StartCoroutine(InitializeDelayed());
    }

    private System.Collections.IEnumerator InitializeDelayed()
    {
        // CoreDataManager가 초기화될 때까지 대기
        int waitCount = 0;
        while (CoreDataManager.Instance == null && waitCount < 30)
        {
            yield return null;
            waitCount++;
        }

        if (CoreDataManager.Instance == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager를 찾을 수 없습니다!");
            yield break;
        }

        var coreData = CoreDataManager.Instance;
        
        // characterDatabase가 초기화될 때까지 추가 대기
        int dbWaitCount = 0;
        while (coreData.characterDatabase == null && dbWaitCount < 60) // 최대 60프레임 대기
        {
            yield return null;
            dbWaitCount++;
        }
        
        // characterDatabase 확인
        if (coreData.characterDatabase == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager.characterDatabase가 null입니다!");
            Debug.LogError("[SummonManager] CoreDataManager Inspector에서 characterDatabase를 설정하거나, 씬에 CharacterDatabase 프리팹을 배치해주세요.");
            
            // 추가 디버그 정보
            CharacterDatabase foundDB = FindFirstObjectByType<CharacterDatabase>();
            if (foundDB != null)
            {
                Debug.Log("[SummonManager] 씬에서 CharacterDatabase를 찾았지만 CoreDataManager에 연결되지 않았습니다.");
                Debug.Log("[SummonManager] CoreDataManager.Start()가 실행되면 자동으로 연결될 예정입니다.");
                
                // 직접 연결 시도
                coreData.characterDatabase = foundDB;
                Debug.Log("[SummonManager] CharacterDatabase를 직접 연결했습니다.");
            }
            else
            {
                Debug.LogError("[SummonManager] 씬에 CharacterDatabase가 없습니다! CharacterDatabase 프리팹을 씬에 배치해주세요.");
                yield break;
            }
        }

        // 원 버튼 소환 풀이 비어있으면 기본 캐릭터들로 채우기
        if (oneButtonSummonPool == null || oneButtonSummonPool.Length == 0)
        {
            if (coreData.characterDatabase.currentRegisteredCharacters != null)
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
                Debug.Log($"[SummonManager] 원 버튼 소환 풀 초기화 완료: {oneButtonSummonPool.Length}개 캐릭터");
            }
        }
    }

    private void Update()
    {
        // 숫자키로 캐릭터 인덱스 바꾸기
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetCharacterIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetCharacterIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetCharacterIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetCharacterIndex(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetCharacterIndex(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetCharacterIndex(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SetCharacterIndex(6);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SetCharacterIndex(7);
        if (Input.GetKeyDown(KeyCode.Alpha9)) SetCharacterIndex(8);
        if (Input.GetKeyDown(KeyCode.Alpha0)) SetCharacterIndex(9);
    }

    private void SetCharacterIndex(int index)
    {
        if (CoreDataManager.Instance != null)
        {
            CoreDataManager.Instance.currentCharacterIndex = index;
            Debug.Log($"[SummonManager] 캐릭터 인덱스 {index}로 설정");
        }
    }

    /// <summary>
    /// 원 버튼 소환 - 랜덤 캐릭터를 랜덤 위치에 소환
    /// </summary>
    public void OnClickOneButtonSummon(bool isRegion2 = false)
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager가 없습니다!");
            return;
        }
        
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
            Debug.Log($"[SummonManager] 원 버튼 소환 성공! {randomCharData.characterName}을(를) {randomTile.name}에 소환");
        }
    }

    /// <summary>
    /// 타일에 캐릭터 배치 시도 (랜덤 선택)
    /// </summary>
    public void PlaceCharacterOnTile(Tile tile)
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager.Instance가 null");
            return;
        }
        
        if (coreData.currentCharacterIndex < 0)
        {
            Debug.LogWarning("[SummonManager] 캐릭터를 먼저 선택하세요!");
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
                if (TileManager.Instance != null)
                {
                    currentTileOccupied = TileManager.Instance.CheckAnyCharacterHasCurrentTile(tile);
                }
            }
            
            if (currentTileOccupied)
            {
                Debug.Log($"[SummonManager] {tile.name}에 이미 캐릭터가 있음. 빈 타일을 찾거나 walkable로 전환합니다.");
                
                Tile emptyTile = null;
                if (TileManager.Instance != null)
                {
                    emptyTile = TileManager.Instance.FindEmptyPlacedOrPlacableTile(tile.isRegion2);
                }
                
                if (emptyTile != null)
                {
                    Debug.Log($"[SummonManager] 빈 타일 {emptyTile.name}을 찾았습니다. 해당 타일에 배치합니다.");
                    tile = emptyTile;
                }
                else
                {
                    Debug.Log($"[SummonManager] 빈 placed/placable 타일이 없습니다. walkable 타일로 전환합니다.");
                    Tile walkableTile = null;
                    if (TileManager.Instance != null)
                    {
                        walkableTile = TileManager.Instance.FindEmptyWalkableTile(tile.isRegion2);
                    }
                    
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

        bool success = SummonCharacterOnTile(coreData.currentCharacterIndex, tile, false);
        
        if (success)
        {
            Debug.Log($"[SummonManager] [{coreData.currentCharacterIndex}] {data.characterName} 소환 성공");
        }
        else
        {
            Debug.LogWarning($"[SummonManager] 소환 실패");
        }
    }

    /// <summary>
    /// ★★★ 수정: 특정 캐릭터를 타일에 소환
    /// </summary>
    public bool SummonCharacterOnTile(int summonIndex, Tile tile, bool forceEnemyArea2 = false)
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager가 없습니다!");
            return false;
        }
        
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
            if (targetMineralBar == null)
            {
                Debug.LogWarning($"[SummonManager] {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 바가 없습니다!");
                return false;
            }

            bool mineralsSpent = targetMineralBar.TrySpend(data.cost);
            if (!mineralsSpent)
            {
                Debug.Log($"[SummonManager] {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 부족! (필요: {data.cost}, 현재: {targetMineralBar.GetCurrentMinerals()})");
                return false;
            }

            // ★★★ 추가: 같은 캐릭터 체크
            if (tile.GetOccupyingCharacters().Count > 0)
            {
                Character firstChar = tile.GetOccupyingCharacters()[0];
                if (firstChar.characterName != data.characterName || firstChar.star != data.initialStar)
                {
                    Debug.Log($"[SummonManager] 다른 종류의 캐릭터가 이미 있음: {firstChar.characterName} vs {data.characterName}");
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

    /// <summary>
    /// 드래그 소환 실제 처리
    /// </summary>
    private bool ActualSummonDrag(CharacterData data, Tile tile, int summonIndex, bool tileIsArea2, bool forceEnemyArea2)
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null) return false;
        
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

            RouteType selectedRoute = RouteType.Center;
            if (RouteManager.Instance != null)
            {
                selectedRoute = RouteManager.Instance.DetermineRouteFromTile(tile, spawner);
            }
            
            Transform[] waypoints = null;
            if (RouteManager.Instance != null)
            {
                waypoints = RouteManager.Instance.GetWaypointsForRoute(spawner, selectedRoute);
            }
            
            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogWarning($"[SummonManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
                return false;
            }

            GameObject allyObj = Instantiate(data.spawnPrefab, coreData.ourMonsterPanel);
            if (allyObj != null)
            {
                allyObj.transform.position = new Vector3(-1000, -1000, 0);
                
                Character allyCharacter = allyObj.GetComponent<Character>();
                ConfigureCharacterForWalkable(allyCharacter, data, tile, spawner, 1);
                
                Debug.Log($"[SummonManager] [{data.characterName}] (지역1) 몬스터 소환 - {selectedRoute} 루트 선택 (cost={data.cost})");

                var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                if (selectUI != null)
                {
                    selectUI.MarkCardAsUsed(summonIndex);
                }
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
                GameObject enemyObj = Instantiate(data.spawnPrefab, coreData.opponentOurMonsterPanel);
                if (enemyObj != null)
                {
                    enemyObj.transform.position = new Vector3(-1000, -1000, 0);
                    
                    Character enemyCharacter = enemyObj.GetComponent<Character>();
                    ConfigureCharacterForWalkable2(enemyCharacter, data, tile, spawner2, 2);
                    
                    Debug.Log($"[SummonManager] [{data.characterName}] (지역2) 몬스터 소환 완료 (cost={data.cost})");

                    var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                    if (selectUI != null)
                    {
                        selectUI.MarkCardAsUsed(summonIndex);
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
        else if (tile.IsPlacable() || tile.IsPlacable2() || tile.IsPlaceTile() || tile.IsPlaced2())
        {
            bool isArea2Tile = tile.IsPlacable2() || tile.IsPlaced2();
            RectTransform targetParent = isArea2Tile
                ? (coreData.opponentCharacterPanel != null ? coreData.opponentCharacterPanel : coreData.characterPanel)
                : coreData.characterPanel;

            GameObject newCharObj = Instantiate(data.spawnPrefab, targetParent);
            if (newCharObj != null)
            {
                PositionCharacterOnTile(newCharObj, tile, targetParent);
                
                Character cComp = newCharObj.GetComponent<Character>();
                ConfigureCharacterForTower(cComp, data, tile, isArea2Tile);
                
                // ★★★ 추가: 타일에 캐릭터 추가
                if (!tile.AddOccupyingCharacter(cComp))
                {
                    Destroy(newCharObj);
                    Debug.LogError($"[SummonManager] 타일에 캐릭터 추가 실패!");
                    return false;
                }
                
                if (TileManager.Instance != null)
                {
                    TileManager.Instance.CreatePlaceTileChild(tile);
                }
                
                Debug.Log($"[SummonManager] [{data.characterName}] 드래그 배치 완료 (cost={data.cost})");
                
                var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                if (selectUI != null)
                {
                    selectUI.MarkCardAsUsed(summonIndex);
                }
                return true;
            }
            else
            {
                Debug.LogError("[SummonManager] Instantiate 실패");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"[SummonManager] {tile.name} 상태를 처리할 수 없습니다 (드래그 소환).");
            return false;
        }
    }

    /// <summary>
    /// 원 버튼 소환 실제 처리
    /// </summary>
    private bool ActualSummonOneButton(CharacterData data, Tile tile, bool isRegion2)
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null) return false;
        
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
                    
                    // ★★★ 추가: 타일에 캐릭터 추가
                    Character cComp = charObj.GetComponent<Character>();
                    if (!tile.AddOccupyingCharacter(cComp))
                    {
                        Destroy(charObj);
                        Debug.LogError($"[SummonManager] 타일에 캐릭터 추가 실패!");
                        return false;
                    }
                    
                    if (TileManager.Instance != null)
                    {
                        TileManager.Instance.CreatePlaceTileChild(tile);
                    }
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
    /// 캐릭터를 타일 위치에 배치
    /// </summary>
    private void PositionCharacterOnTile(GameObject charObj, Tile tile, RectTransform targetParent)
    {
        RectTransform charRect = charObj.GetComponent<RectTransform>();
        RectTransform tileRect = tile.GetComponent<RectTransform>();
        
        if (charRect != null && tileRect != null)
        {
            Vector2 localPos = targetParent.InverseTransformPoint(tileRect.transform.position);
            charRect.anchoredPosition = localPos;
            charRect.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// 지역1 walkable 캐릭터 설정
    /// </summary>
    private void ConfigureCharacterForWalkable(Character character, CharacterData data, Tile tile, WaveSpawner spawner, int areaIndex)
    {
        if (character == null || data == null) return;
        
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
        
        if (RouteManager.Instance != null)
        {
            RouteType selectedRoute = RouteManager.Instance.DetermineRouteFromTile(tile, spawner);
            RouteManager.Instance.OnRouteSelected(character, tile, selectedRoute, spawner);
        }
    }

    /// <summary>
    /// 지역2 walkable 캐릭터 설정
    /// </summary>
    private void ConfigureCharacterForWalkable2(Character character, CharacterData data, Tile tile, WaveSpawnerRegion2 spawner2, int areaIndex)
    {
        if (character == null || data == null) return;
        
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
        
        if (RouteManager.Instance != null)
        {
            RouteType selectedRoute = RouteManager.Instance.DetermineRouteFromTile(tile, spawner2);
            RouteManager.Instance.OnRouteSelectedRegion2(character, tile, selectedRoute, spawner2);
        }
    }

    /// <summary>
    /// 타워형 캐릭터 설정
    /// </summary>
    private void ConfigureCharacterForTower(Character character, CharacterData data, Tile tile, bool isArea2)
    {
        if (character == null || data == null) return;
        
        var coreData = CoreDataManager.Instance;
        
        character.currentTile = tile;
        character.isHero = false;
        character.isCharAttack = false;
        character.areaIndex = isArea2 ? 2 : 1;
        
        character.characterName = data.characterName;
        character.attackPower = data.attackPower;
        character.attackSpeed = data.attackSpeed;
        character.attackRange = data.attackRange;
        character.currentHP = data.maxHP;
        character.star = data.initialStar;
        character.ApplyStarVisual();
        character.moveSpeed = data.moveSpeed;
        
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
    /// 제거 모드 토글
    /// </summary>
    public void ToggleRemoveMode()
    {
        removeMode = !removeMode;
        Debug.Log($"[SummonManager] removeMode = {removeMode}");

        if (removeMode)
        {
            RemoveRandomCharacter();
        }
    }

    /// <summary>
    /// 랜덤 캐릭터 제거
    /// </summary>
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

    /// <summary>
    /// ★★★ 수정: 타일의 캐릭터 제거
    /// </summary>
    public void RemoveCharacterOnTile(Tile tile)
    {
        if (tile == null) return;

        List<Character> occupants = tile.GetOccupyingCharacters();
        
        if (occupants.Count == 0)
        {
            Debug.LogWarning($"[SummonManager] {tile.name}에 캐릭터가 없어서 제거 불가");
            return;
        }

        // 마지막에 추가된 캐릭터를 제거 (또는 첫 번째 캐릭터)
        Character toRemove = occupants[occupants.Count - 1];
        
        if (toRemove == null) return;

        // 미네랄 환불
        int refundAmount = 10; // 기본값
        var coreData = CoreDataManager.Instance;
        
        if (coreData != null && coreData.characterDatabase != null)
        {
            foreach (var data in coreData.characterDatabase.currentRegisteredCharacters)
            {
                if (data != null && data.characterName == toRemove.characterName)
                {
                    refundAmount = data.cost;
                    break;
                }
            }
        }

        // 캐릭터 제거
        tile.RemoveOccupyingCharacter(toRemove);
        
        // 미네랄 환불
        if (toRemove.areaIndex == 2)
        {
            if (coreData.region2MineralBar != null)
            {
                coreData.region2MineralBar.RefundMinerals(refundAmount);
            }
        }
        else
        {
            if (coreData.region1MineralBar != null)
            {
                coreData.region1MineralBar.RefundMinerals(refundAmount);
            }
        }

        // 캐릭터 오브젝트 파괴
        Destroy(toRemove.gameObject);
        
        Debug.Log($"[SummonManager] {toRemove.characterName}을(를) 제거하고 {refundAmount} 미네랄 환불 (남은 캐릭터: {tile.GetOccupyingCharacters().Count})");

        // 타일이 비었으면 원래 상태로 복구
        if (tile.GetOccupyingCharacters().Count == 0)
        {
            if (tile.IsPlaceTile())
            {
                tile.SetPlacable();
            }
            else if (tile.IsPlaced2())
            {
                tile.SetPlacable2();
            }
            
            if (TileManager.Instance != null)
            {
                TileManager.Instance.OnCharacterRemovedFromTile(tile);
            }
        }
    }

    /// <summary>
    /// 캐릭터 선택
    /// </summary>
    public void OnClickSelectUnit(int index)
    {
        var coreData = CoreDataManager.Instance;
        
        if (coreData != null)
        {
            coreData.currentCharacterIndex = index;
            
            if (index >= 0 && index < coreData.characterDatabase.currentRegisteredCharacters.Length)
            {
                CharacterData data = coreData.characterDatabase.currentRegisteredCharacters[index];
                if (data != null)
                {
                    Debug.Log($"[SummonManager] 캐릭터 선택: [{index}] {data.characterName}");
                }
            }
        }
    }

    /// <summary>
    /// 자동 배치 메서드 (PlacementManager에서 호출)
    /// </summary>
    public void OnClickAutoPlace()
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager가 없습니다!");
            return;
        }
        
        if (coreData.currentCharacterIndex < 0)
        {
            Debug.LogWarning("[SummonManager] 캐릭터를 먼저 선택하세요!");
            return;
        }
        
        bool targetRegion2 = !coreData.isHost;
        
        Tile targetTile = null;
        if (TileManager.Instance != null)
        {
            targetTile = TileManager.Instance.FindEmptyPlacedOrPlacableTile(targetRegion2);
        }
        
        if (targetTile != null)
        {
            bool success = SummonCharacterOnTile(coreData.currentCharacterIndex, targetTile, targetRegion2);
            if (success)
            {
                Debug.Log($"[SummonManager] 자동 배치 성공: 캐릭터 인덱스 {coreData.currentCharacterIndex}를 {targetTile.name}에 배치");
            }
            else
            {
                Debug.LogWarning("[SummonManager] 자동 배치 실패");
            }
        }
        else
        {
            Debug.LogWarning("[SummonManager] 자동 배치할 빈 타일을 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 원 버튼 소환을 위한 메서드
    /// </summary>
    public void OnClickOneButtonPlace()
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager가 없습니다!");
            return;
        }
        
        if (coreData.currentCharacterIndex < 0)
        {
            Debug.LogWarning("[SummonManager] 캐릭터를 먼저 선택하세요!");
            return;
        }
        
        bool targetRegion2 = !coreData.isHost;
        
        Tile targetTile = null;
        if (TileManager.Instance != null)
        {
            targetTile = TileManager.Instance.FindEmptyPlacedOrPlacableTile(targetRegion2);
        }
        
        if (targetTile == null)
        {
            Debug.Log("[SummonManager] placed/placable 타일이 모두 꽉 찼습니다. walkable 타일로 전환합니다.");
            if (TileManager.Instance != null)
            {
                targetTile = TileManager.Instance.FindEmptyWalkableTile(targetRegion2);
            }
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

    /// <summary>
    /// 특정 지역의 빈 타일 찾기
    /// </summary>
    private Tile FindRandomEmptyTile(bool isRegion2 = false)
    {
        List<Tile> availableTiles = new List<Tile>();
        
        if (TileManager.Instance != null)
        {
            var tiles = isRegion2 ? TileManager.Instance.aiSummonableTiles : TileManager.Instance.playerSummonableTiles;
            availableTiles = tiles.Where(t => t.CanPlaceCharacter()).ToList();
        }
        
        if (availableTiles.Count > 0)
        {
            return availableTiles[Random.Range(0, availableTiles.Count)];
        }
        
        return null;
    }

    /// <summary>
    /// enemyDatabase에서 캐릭터 인덱스 찾기
    /// </summary>
    public int FindCharacterIndexInEnemyDatabase(CharacterData character)
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null) return -1;
        
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