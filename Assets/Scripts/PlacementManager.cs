using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;
using System.Collections.Generic;
using UnityEngine.UI;

// RouteType enum 추가
public enum RouteType
{
    Default,  // 타워용 기본값
    Left,
    Center,
    Right
}

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("Split Database - Ally / Enemy")]
    [Tooltip("내가 다루는(아군) Database (ScriptableObject)")]
    public CharacterDatabaseObject allyDatabase;

    [Tooltip("적(상대) Database (ScriptableObject)")]
    public CharacterDatabaseObject enemyDatabase;

    [Tooltip("내가 다루는(아군) 캐릭터 프리팹 (총 10개)")]
    public GameObject[] allyCharacterPrefabs = new GameObject[10];

    [Tooltip("적(상대) 캐릭터 프리팹 (총 10개)")]
    public GameObject[] enemyCharacterPrefabs = new GameObject[10];

    [Tooltip("내가 다루는(아군) 탄환 프리팹 (총 10개)")]
    public GameObject[] allyBulletPrefabs = new GameObject[10];

    [Tooltip("적(상대) 탄환 프리팹 (총 10개)")]
    public GameObject[] enemyBulletPrefabs = new GameObject[10];

    [Tooltip("내가 다루는(아군) 몬스터 프리팹 (총 101개)")]
    public GameObject[] allyMonsterPrefabs = new GameObject[101];

    [Tooltip("적(상대) 몬스터 프리팹 (총 101개)")]
    public GameObject[] enemyMonsterPrefabs = new GameObject[101];

    [Header("Placement Settings")]
    [Tooltip("(구) 단일 DB (주로 아군용)")]
    public CharacterDatabase characterDatabase;

    [Tooltip("현재 선택된 캐릭터 인덱스")]
    public int currentCharacterIndex = 0;

    [Tooltip("2D 카메라 (Orthographic)")]
    public Camera mainCamera;

    [Header("UI Panels")]
    [Tooltip("타일들이 모인 패널")]
    public RectTransform tilePanel;

    [Tooltip("아군 건물/캐릭터가 들어갈 패널")]
    public RectTransform characterPanel;

    [Tooltip("아군 탄환이 들어갈 패널")]
    public RectTransform bulletPanel;

    [Tooltip("아군 몬스터(웨이브 소환)가 들어갈 패널")]
    public RectTransform ourMonsterPanel;

    [Tooltip("VFX(이펙트) 프리팹이 생성될 부모 패널 (없으면 월드)")]
    public RectTransform vfxPanel;

    [Header("Mineral Bars for Region1 / Region2")]
    public MineralBar region1MineralBar;
    public MineralBar region2MineralBar;

    [Header("Opponent Panels for Region2")]
    [Tooltip("상대(지역2) 건물/캐릭터 패널")]
    public RectTransform opponentCharacterPanel;

    [Tooltip("상대(지역2) 탄환 패널")]
    public RectTransform opponentBulletPanel;

    [Tooltip("상대(지역2) 몬스터 패널")]
    public RectTransform opponentOurMonsterPanel;

    [Header("Movement Speed Settings (for Player Units)")]
    public float walkableCharSpeed = 1.0f;   // region1: walkable
    public float walkable2CharSpeed = 1.2f;  // region2: walkable2

    private bool isHost = true;

    // ======================== [추가] 2성/3성 합성용 별도 DB ========================
    [Header("Star Merge DB (기본 지역1용)")]
    public StarMergeDatabaseObject starMergeDatabase;

    [Header("Star Merge DB for Region2 (별도의 DB)")]
    public StarMergeDatabaseObject starMergeDatabaseRegion2;
    // =============================================================================

    // ======================== [추가한 필드 1] ========================
    [Header("[추가] 아이템 인벤토리 매니저 참조 (캐릭터 제거 시 아이템 지급)")]
    public ItemInventoryManager itemInventoryManager;

    // ======================== [추가한 필드 2] ========================
    [Header("[추가] 캐릭터 제거 모드 (true면 타일 클릭 시 캐릭터 제거)")]
    public bool removeMode = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // VFX 패널 연결
        if (vfxPanel == null)
        {
            GameObject panelObj = GameObject.Find("VFX Panel");
            if (panelObj != null)
            {
                vfxPanel = panelObj.GetComponent<RectTransform>();
            }
        }
        Bullet.SetVfxPanel(vfxPanel);

        // EventSystem 체크
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            Debug.LogWarning("<color=red>씬에 EventSystem이 없습니다! UI 클릭/드래그가 제대로 안 될 수 있음.</color>");
        }

        if (characterPanel == null)
        {
            Debug.LogWarning("[PlacementManager] characterPanel이 null입니다. (Area1 건물 배치용 패널)");
        }
    }

    private void Start()
    {
        // UI 패널 디버그 로그
        Debug.Log($"[PlacementManager] UI 패널 상태: opponentCharacterPanel={opponentCharacterPanel != null}, " +
                  $"opponentOurMonsterPanel={opponentOurMonsterPanel != null}, " +
                  $"opponentBulletPanel={opponentBulletPanel != null}");

        // 로그인 패널이 비활성화된 상태이므로 항상 호스트로 설정
        isHost = true;
        Debug.Log("[PlacementManager] 로그인 패널 비활성화 상태: 호스트 모드로 플레이합니다.");
    }

    private void Update()
    {
        // 숫자키로 캐릭터 인덱스 바꾸기 (테스트용)
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentCharacterIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentCharacterIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentCharacterIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentCharacterIndex = 3;
        
        // ▼▼ [수정] 더 빠른 주기로 타일 참조 정리 (1초마다) ▼▼
        if (Time.time - lastCleanupTime > 1.0f) // 5초에서 1초로 변경
        {
            CleanupDestroyedCharacterReferences();
            UpdatePlacedTileStates(); // placed tile 상태 업데이트 추가
            lastCleanupTime = Time.time;
        }
    }

    /// <summary>
    /// 버튼 클릭으로 캐릭터 인덱스 지정
    /// </summary>
    public void OnClickSelectUnit(int index)
    {
        currentCharacterIndex = index;
        Debug.Log($"[PlacementManager] 선택된 유닛 인덱스: {currentCharacterIndex}");
    }
    
    /// <summary>
    /// 현재 선택된 캐릭터 인덱스 반환
    /// </summary>
    public int GetCurrentCharacterIndex()
    {
        return currentCharacterIndex;
    }
    
    /// <summary>
    /// 캐릭터 선택 해제
    /// </summary>
    public void ClearCharacterSelection()
    {
        currentCharacterIndex = -1;
        Debug.Log("[PlacementManager] 캐릭터 선택 해제됨");
    }

    /// <summary>
    /// "캐릭터 제거 모드"를 토글하는 메서드 (UI 버튼 등에 연결)
    /// </summary>
    public void ToggleRemoveMode()
    {
        removeMode = !removeMode;
        Debug.Log($"[PlacementManager] removeMode = {removeMode}");

        // (테스트) 제거 모드를 켰을 때, 임의의 캐릭터를 즉시 제거 시도할 수도 있음
        // 이 부분은 필요하다면 실제 사용시 주석 처리
        if (removeMode)
        {
            RemoveRandomCharacter();
        }
    }

    /// <summary>
    /// 랜덤으로 placed/placable 타일 위 캐릭터를 찾아 제거
    /// </summary>
    private void RemoveRandomCharacter()
    {
        // 1) 모든 캐릭터 + 타일 중에서 '배치된' 캐릭터만 찾기
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

        // 제거할 캐릭터가 없는 경우
        if (placedCharacters.Count == 0)
        {
            Debug.Log("[PlacementManager] 제거할 캐릭터가 없습니다.");
            return;
        }

        // 2) 랜덤 선택
        int randomIndex = Random.Range(0, placedCharacters.Count);
        Character targetCharacter = placedCharacters[randomIndex];
        Tile targetTile = targetCharacter.currentTile;

        if (targetTile == null)
        {
            Debug.LogWarning("[PlacementManager] 선택된 캐릭터의 타일이 null입니다.");
            return;
        }

        Debug.Log($"[PlacementManager] 랜덤 제거 대상: {targetCharacter.characterName} (별:{targetCharacter.star}, 타일:{targetTile.name})");

        // 3) 캐릭터 제거
        RemoveCharacterOnTile(targetTile);

        // 4) 제거 모드 즉시 해제
        removeMode = false;
    }

    // ---------------------------------------------------------------------------
    // (A) "클릭 방식" 배치 (tile.OnClickPlacableTile() 등에서 호출)
    // ---------------------------------------------------------------------------
    public void PlaceCharacterOnTile(Tile tile)
    {
        // ▼▼ [추가] 디버그 로그 강화 ▼▼
        Debug.Log($"[PlacementManager] PlaceCharacterOnTile 호출 - tile: {tile.name}, currentCharacterIndex: {currentCharacterIndex}, removeMode: {removeMode}");
        
        // removeMode가 true면 제거 로직을 "타일" 측에서 처리하므로,
        // 여기서는 배치 로직만 그대로 유지

        if (characterDatabase == null
            || characterDatabase.currentRegisteredCharacters == null
            || characterDatabase.currentRegisteredCharacters.Length == 0)
        {
            Debug.LogWarning("[PlacementManager] characterDatabase가 비어있어 배치 불가");
            return;
        }

        if (currentCharacterIndex < 0
            || currentCharacterIndex >= characterDatabase.currentRegisteredCharacters.Length)
        {
            Debug.LogWarning($"[PlacementManager] 잘못된 인덱스({currentCharacterIndex}) => 배치 불가. 캐릭터를 먼저 선택하세요!");
            return;
        }

        CharacterData data = characterDatabase.currentRegisteredCharacters[currentCharacterIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"[PlacementManager] [{currentCharacterIndex}]번 캐릭터 spawnPrefab이 null => 배치 불가");
            return;
        }
        if (tile == null)
        {
            Debug.LogWarning("[PlacementManager] tile이 null => 배치 불가");
            return;
        }
        
        // ▼▼ [추가] 타일이 placed/placable인 경우, 빈 타일이 있는지 확인 ▼▼
        if (tile.IsPlaceTile() || tile.IsPlaced2() || tile.IsPlacable() || tile.IsPlacable2())
        {
            // 현재 타일에 캐릭터가 있는지 확인
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
                currentTileOccupied = CheckAnyCharacterHasCurrentTile(tile);
            }
            
            // 현재 타일이 점유되어 있으면, 빈 placed/placable 타일을 찾거나 walkable로 전환
            if (currentTileOccupied)
            {
                Debug.Log($"[PlacementManager] {tile.name}에 이미 캐릭터가 있음. 빈 타일을 찾거나 walkable로 전환합니다.");
                
                // 먼저 빈 placed/placable 타일 찾기
                Tile emptyTile = FindEmptyPlacedOrPlacableTile(tile.isRegion2);
                if (emptyTile != null)
                {
                    Debug.Log($"[PlacementManager] 빈 타일 {emptyTile.name}을 찾았습니다. 해당 타일에 배치합니다.");
                    tile = emptyTile;
                }
                else
                {
                    // 빈 placed/placable 타일이 없으면 walkable 타일 찾기
                    Debug.Log($"[PlacementManager] 빈 placed/placable 타일이 없습니다. walkable 타일로 전환합니다.");
                    Tile walkableTile = FindEmptyWalkableTile(tile.isRegion2);
                    if (walkableTile != null)
                    {
                        tile = walkableTile;
                        Debug.Log($"[PlacementManager] walkable 타일 {tile.name}에 배치합니다.");
                    }
                    else
                    {
                        Debug.LogWarning("[PlacementManager] 배치 가능한 타일이 없습니다!");
                        return;
                    }
                }
            }
        }

        bool isArea2 = (tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right() || tile.IsPlacable2() || tile.IsPlaced2());
        
        // ▼▼ [추가] 미네랄 상태 확인 로그 ▼▼
        MineralBar targetMineralBar = isArea2 ? region2MineralBar : region1MineralBar;
        if (targetMineralBar != null)
        {
            Debug.Log($"[PlacementManager] 미네랄 체크 - 현재: {targetMineralBar.GetCurrentMinerals()}, 필요: {data.cost}, 충분: {targetMineralBar.GetCurrentMinerals() >= data.cost}");
        }
        
        // === [수정 추가] 한 타일에는 한 캐릭터만 있어야 하므로, 이미 다른 캐릭터가 있는지 확인 ===
        // placed tile의 경우 특별 처리: 실제 캐릭터가 있는지만 확인
        bool hasActualCharacter = false;
        if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            // placed tile은 PlaceTile/Placed2 자식이 아닌 실제 캐릭터만 확인
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
                Debug.LogWarning($"[PlacementManager] {tile.name} placed tile에 이미 캐릭터가 있습니다. 배치 불가!");
                return;
            }
        }
        else
        {
            // placable/walkable 타일은 기존 로직 사용
            if (CheckAnyCharacterHasCurrentTile(tile))
            {
                Debug.LogWarning($"[PlacementManager] {tile.name} 타일은 이미 캐릭터가 점유 중입니다. 배치 불가!");
                return;
            }
        }
        // ================================================================================
        
        // ▼▼ [추가] 타일이 placed/placable인 경우, 모든 타일이 차있으면 walkable로 자동 전환 ▼▼
        if (tile.IsPlaceTile() || tile.IsPlaced2() || tile.IsPlacable() || tile.IsPlacable2())
        {
            // 모든 placed/placable 타일이 차있는지 확인
            bool allPlacedTilesFull = true;
            Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
            Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            
            foreach (var t in allTiles)
            {
                if (t == null) continue;
                if (t.isRegion2 != tile.isRegion2) continue;
                
                // placed 또는 placable 타일인지 확인
                if (t.IsPlaceTile() || t.IsPlaced2() || t.IsPlacable() || t.IsPlacable2())
                {
                    // 타일이 비어있는지 확인
                    bool isEmpty = true;
                    
                    if (t.IsPlaceTile() || t.IsPlaced2())
                    {
                        // placed tile의 경우
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
                        // placable tile의 경우
                        isEmpty = !CheckAnyCharacterHasCurrentTile(t);
                    }
                    
                    if (isEmpty)
                    {
                        allPlacedTilesFull = false;
                        break;
                    }
                }
            }
            
            // 모든 placed/placable 타일이 차있으면 walkable 타일로 전환
            if (allPlacedTilesFull)
            {
                Debug.Log($"[PlacementManager] 모든 placed/placable 타일이 가득 참. walkable 타일로 자동 전환합니다.");
                Tile walkableTile = FindEmptyWalkableTile(tile.isRegion2);
                if (walkableTile != null)
                {
                    tile = walkableTile;
                    Debug.Log($"[PlacementManager] walkable 타일 {tile.name}으로 배치 변경");
                }
                else
                {
                    Debug.LogWarning("[PlacementManager] walkable 타일도 없습니다!");
                    return;
                }
            }
        }
        
        if (isArea2 && isHost)
        {
            Debug.LogWarning("[PlacementManager] 지역2에는 (호스트) 배치 불가");
            return;
        }
        if (!isArea2 && !isHost)
        {
            Debug.LogWarning("[PlacementManager] 지역1에는 (클라이언트/AI) 배치 불가");
            return;
        }

        // 미네랄 체크
        if (isArea2)
        {
            if (region2MineralBar != null)
            {
                if (!region2MineralBar.TrySpend(data.cost))
                {
                    Debug.Log($"[PlacementManager] (지역2) 미네랄 부족! (cost={data.cost})");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[PlacementManager] region2MineralBar가 null => 배치 불가");
                return;
            }
        }
        else
        {
            if (region1MineralBar != null)
            {
                if (!region1MineralBar.TrySpend(data.cost))
                {
                    Debug.Log($"[PlacementManager] (지역1) 미네랄 부족! (cost={data.cost})");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[PlacementManager] region1MineralBar가 null => 배치 불가");
                return;
            }
        }

        if (!tile.CanPlaceCharacter())
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} => 배치 불가능한 상태");
            return;
        }

        if (tile.IsWalkable() || tile.IsWalkableLeft() || tile.IsWalkableCenter() || tile.IsWalkableRight())
        {
            WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null && ourMonsterPanel != null)
            {
                // ▼▼ [수정] 먼저 캐릭터를 생성하고 루트 선택 UI 표시
                GameObject allyObj = Instantiate(data.spawnPrefab, ourMonsterPanel);
                if (allyObj != null)
                {
                    // 임시로 화면 밖에 배치
                    allyObj.transform.position = new Vector3(-1000, -1000, 0);
                    
                    Character allyCharacter = allyObj.GetComponent<Character>();
                    allyCharacter.currentTile = null;
                    allyCharacter.isHero = (currentCharacterIndex == 9);
                    allyCharacter.isCharAttack = !allyCharacter.isHero;
                    allyCharacter.areaIndex = 1;
                    
                    // 기본 스탯 설정
                    allyCharacter.attackPower = data.attackPower;
                    allyCharacter.attackSpeed = data.attackSpeed;
                    allyCharacter.attackRange = data.attackRange;
                    allyCharacter.currentHP = data.maxHP;
                    allyCharacter.star = data.initialStar;
                    allyCharacter.ApplyStarVisual();
                    allyCharacter.moveSpeed = data.moveSpeed;
                    
                    // ▼▼ [수정] 자동으로 루트 결정 (UI 없이)
                    RouteType selectedRoute = DetermineRouteFromTile(tile, spawner);
                    OnRouteSelected(allyCharacter, tile, selectedRoute, spawner);
                    
                    var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                    if (selectUI != null)
                    {
                        selectUI.MarkCardAsUsed(currentCharacterIndex);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[PlacementManager] WaveSpawner/ourMonsterPanel이 없어 소환 실패");
            }
        }
        else if (tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right())
        {
            WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
            if (spawner2 != null && opponentOurMonsterPanel != null)
            {
                // ▼▼ [수정] 먼저 캐릭터를 생성하고 루트 선택 UI 표시
                GameObject enemyObj = Instantiate(data.spawnPrefab, opponentOurMonsterPanel);
                if (enemyObj != null)
                {
                    // 임시로 화면 밖에 배치
                    enemyObj.transform.position = new Vector3(-1000, -1000, 0);
                    
                    Character enemyCharacter = enemyObj.GetComponent<Character>();
                    enemyCharacter.currentTile = null;
                    enemyCharacter.isHero = false;
                    enemyCharacter.isCharAttack = true;
                    enemyCharacter.areaIndex = 2;
                    
                    // 기본 스탯 설정
                    enemyCharacter.attackPower = data.attackPower;
                    enemyCharacter.attackSpeed = data.attackSpeed;
                    enemyCharacter.attackRange = data.attackRange;
                    enemyCharacter.currentHP = data.maxHP;
                    enemyCharacter.star = data.initialStar;
                    enemyCharacter.ApplyStarVisual();
                    enemyCharacter.moveSpeed = data.moveSpeed;
                    
                    // ▼▼ [수정] 자동으로 루트 결정 (UI 없이)
                    RouteType selectedRoute = DetermineRouteFromTile(tile, spawner2);
                    OnRouteSelectedRegion2(enemyCharacter, tile, selectedRoute, spawner2);
                    
                    var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                    if (selectUI != null)
                    {
                        selectUI.MarkCardAsUsed(currentCharacterIndex);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[PlacementManager] WaveSpawnerRegion2/opponentOurMonsterPanel이 없어 소환 실패");
            }
        }
        else if (tile.IsPlacable() || tile.IsPlacable2())
        {
            RectTransform targetParent = tile.IsPlacable2() && (opponentCharacterPanel != null)
                ? opponentCharacterPanel
                : characterPanel;

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
                    characterComp.isHero = (currentCharacterIndex == 9);
                    characterComp.isCharAttack = false;

                    characterComp.currentWaypointIndex = -1;
                    characterComp.maxWaypointIndex = 6;

                    characterComp.attackPower = data.attackPower;
                    characterComp.attackSpeed = data.attackSpeed;
                    characterComp.attackRange = data.attackRange;
                    characterComp.currentHP = data.maxHP;
                    characterComp.star = data.initialStar;
                    characterComp.ApplyStarVisual();

                    if (tile.IsPlacable2() && opponentBulletPanel != null)
                    {
                        characterComp.opponentBulletPanel = opponentBulletPanel;
                    }
                    else
                    {
                        characterComp.SetBulletPanel(bulletPanel);
                    }
                    characterComp.areaIndex = tile.IsPlacable2() ? 2 : 1;
                }

                CreatePlaceTileChild(tile);

                var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                if (selectUI != null)
                {
                    selectUI.MarkCardAsUsed(currentCharacterIndex);
                }

                Debug.Log($"[PlacementManager] [{data.characterName}] 배치 완료 (cost={data.cost})");
                // ▼▼ [수정] 자동으로 -1로 설정하지 않고, CharacterSelectUI에서 관리하도록 함 ▼▼
                // currentCharacterIndex = -1;
            }
        }
        else if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            RectTransform targetParent = tile.IsPlaced2() && (opponentCharacterPanel != null)
                ? opponentCharacterPanel
                : characterPanel;

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
                    characterComp.isHero = (currentCharacterIndex == 9);
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

                    if (tile.IsPlaced2() && opponentBulletPanel != null)
                    {
                        characterComp.opponentBulletPanel = opponentBulletPanel;
                    }
                    else
                    {
                        characterComp.SetBulletPanel(bulletPanel);
                    }
                }

                CreatePlaceTileChild(tile);

                var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                if (selectUI != null)
                {
                    selectUI.MarkCardAsUsed(currentCharacterIndex);
                }

                Debug.Log($"[PlacementManager] [{data.characterName}] 배치 완료 (on PlaceTile/Placed2, cost={data.cost})");
                // ▼▼ [수정] 자동으로 -1로 설정하지 않고, CharacterSelectUI에서 관리하도록 함 ▼▼
                // currentCharacterIndex = -1;
            }
        }
        else
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} 상태를 처리할 수 없습니다.");
        }
    }

    // ---------------------------------------------------------------------------
    // (B) "드래그 방식" 배치 (DraggableSummonButtonUI → tile 드롭 시)
    // ---------------------------------------------------------------------------
    public bool SummonCharacterOnTile(int summonIndex, Tile tile, bool forceEnemyArea2 = false)
    {
        Debug.Log($"[PlacementManager] SummonCharacterOnTile: 인덱스={summonIndex}, tile={tile.name}, forceEnemyArea2={forceEnemyArea2}");

        bool success = false;

        try
        {
            CharacterData data;
            if (forceEnemyArea2 && enemyDatabase != null && enemyDatabase.characters != null)
            {
                if (summonIndex < 0 || summonIndex >= enemyDatabase.characters.Length)
                {
                    Debug.LogWarning($"[PlacementManager] 잘못된 소환 인덱스({summonIndex}) => 실패");
                    return false;
                }
                data = enemyDatabase.characters[summonIndex];
                Debug.Log($"[PlacementManager] enemyDatabase에서 데이터 가져옴: {data.characterName}, spawnPrefab={(data.spawnPrefab != null)}");
            }
            else
            {
                if (characterDatabase == null || characterDatabase.currentRegisteredCharacters == null
                    || characterDatabase.currentRegisteredCharacters.Length == 0)
                {
                    Debug.LogWarning("[PlacementManager] characterDatabase가 비어있음 => 소환 불가");
                    return false;
                }
                if (summonIndex < 0 || summonIndex >= characterDatabase.currentRegisteredCharacters.Length)
                {
                    Debug.LogWarning($"[PlacementManager] 잘못된 소환 인덱스({summonIndex}) => 소환 불가");
                    return false;
                }
                data = characterDatabase.currentRegisteredCharacters[summonIndex];
            }

            if (data == null || data.spawnPrefab == null)
            {
                Debug.LogWarning($"[PlacementManager] [{summonIndex}]번 캐릭터 spawnPrefab이 null => 소환 불가");
                return false;
            }
            if (tile == null)
            {
                Debug.LogWarning("[PlacementManager] tile이 null => 소환 불가");
                return false;
            }

            bool tileIsArea2 = tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right() || tile.IsPlacable2() || tile.IsPlaced2();
            if (!forceEnemyArea2)
            {
                if (tileIsArea2 && isHost)
                {
                    Debug.LogWarning("[PlacementManager] 지역2에는 호스트 배치 불가");
                    return false;
                }
                if (!tileIsArea2 && !isHost)
                {
                    Debug.LogWarning("[PlacementManager] 지역1에는 클라이언트/AI 배치 불가");
                    return false;
                }
            }

            MineralBar targetMineralBar = (forceEnemyArea2 || tileIsArea2) ? region2MineralBar : region1MineralBar;
            bool mineralsSpent = false;

            if (targetMineralBar != null)
            {
                mineralsSpent = targetMineralBar.TrySpend(data.cost);
                if (!mineralsSpent)
                {
                    Debug.Log($"[PlacementManager] ({(tileIsArea2 ? "지역2" : "지역1")}) 미네랄 부족!(cost={data.cost})");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"[PlacementManager] {(tileIsArea2 ? "region2MineralBar" : "region1MineralBar")}가 null => 소환 불가");
                return false;
            }

            if (!tile.CanPlaceCharacter())
            {
                if (mineralsSpent && targetMineralBar != null)
                {
                    targetMineralBar.RefundMinerals(data.cost);
                    Debug.Log($"[PlacementManager] 소환 불가로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                }
                Debug.LogWarning($"[PlacementManager] {tile.name} => 배치 불가(조건 불충족)");
                return false;
            }

            // === [수정 추가] 타일에 이미 캐릭터가 있는지 확인 => 중복 배치 방지 ===
            // placed tile의 경우 특별 처리: 실제 캐릭터가 있는지만 확인
            bool hasActualCharacter = false;
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                // placed tile은 PlaceTile/Placed2 자식이 아닌 실제 캐릭터만 확인
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
                    Debug.LogWarning($"[PlacementManager] {tile.name} placed tile에 이미 캐릭터가 있어 소환 불가!");
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                        Debug.Log($"[PlacementManager] 소환 불가로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                    }
                    return false;
                }
            }
            else
            {
                // placable/walkable 타일은 기존 로직 사용
                if (CheckAnyCharacterHasCurrentTile(tile))
                {
                    Debug.LogWarning($"[PlacementManager] {tile.name}는 이미 캐릭터가 있어 소환 불가!");
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                        Debug.Log($"[PlacementManager] 소환 불가로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                    }
                    return false;
                }
            }
            // ==================================================================

            if (tile.IsWalkable() || tile.IsWalkableLeft() || tile.IsWalkableCenter() || tile.IsWalkableRight())
            {
                WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
                if (spawner == null || ourMonsterPanel == null)
                {
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                    }
                    Debug.LogWarning("[PlacementManager] Walkable => 소환 실패");
                    return false;
                }

                GameObject prefabToSpawn = data.spawnPrefab;
                Character cc = prefabToSpawn.GetComponent<Character>();
                if (cc == null)
                {
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                    }
                    Debug.LogError($"[PlacementManager] '{prefabToSpawn.name}' 프리팹에 Character 없음 => 실패");
                    return false;
                }

                // 타일 위치에 따른 루트 선택
                RouteType selectedRoute = DetermineRouteFromTile(tile, spawner);
                Transform[] waypoints = GetWaypointsForRoute(spawner, selectedRoute);
                
                if (waypoints == null || waypoints.Length == 0)
                {
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                    }
                    Debug.LogWarning($"[PlacementManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
                    return false;
                }

                Vector3 spawnPos = GetSpawnPositionForRoute(spawner, selectedRoute);
                GameObject allyMonsterObj = Instantiate(prefabToSpawn, ourMonsterPanel);
                if (allyMonsterObj != null)
                {
                    RectTransform allyRect = allyMonsterObj.GetComponent<RectTransform>();
                    if (allyRect != null)
                    {
                        Vector2 localPos = ourMonsterPanel.InverseTransformPoint(spawnPos);
                        allyRect.SetParent(ourMonsterPanel, false);
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
                    allyChar.maxWaypointIndex = waypoints.Length - 1; // 실제 웨이포인트 길이에 맞게 설정
                    allyChar.areaIndex = 1;
                    allyChar.selectedRoute = selectedRoute; // 선택된 루트 저장



                    allyChar.attackPower = data.attackPower;
                    allyChar.attackSpeed = data.attackSpeed;
                    allyChar.attackRange = data.attackRange;
                    allyChar.currentHP = data.maxHP;
                    allyChar.star = data.initialStar;
                    allyChar.ApplyStarVisual();
                    allyChar.moveSpeed = data.moveSpeed;

                    Debug.Log($"[PlacementManager] 드래그로 [{data.characterName}] (area1) 몬스터 소환 - {selectedRoute} 루트 선택 (cost={data.cost})");
                    success = true;
                }
                else
                {
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                    }
                    Debug.LogError("[PlacementManager] Walkable => Instantiate 실패");
                    return false;
                }
            }
            else if (tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right())
            {
                WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                if (spawner2 != null && opponentOurMonsterPanel != null)
                {
                    // 타일 위치에 따른 루트 선택
                    RouteType selectedRoute = DetermineRouteFromTile(tile, spawner2);
                    Transform[] waypoints = GetWaypointsForRoute(spawner2, selectedRoute);
                    
                    if (waypoints == null || waypoints.Length == 0)
                    {
                        Debug.LogWarning($"[PlacementManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
                        return false;
                    }
                    
                                    Vector3 spawnPos = GetSpawnPositionForRoute(spawner2, selectedRoute);
                GameObject enemyObj = Instantiate(data.spawnPrefab, opponentOurMonsterPanel);
                if (enemyObj != null)
                {
                    enemyObj.SetActive(true);
                    RectTransform enemyRect = enemyObj.GetComponent<RectTransform>();
                    if (enemyRect != null)
                    {
                        Vector2 localPos = opponentOurMonsterPanel.InverseTransformPoint(spawnPos);
                        enemyRect.SetParent(opponentOurMonsterPanel, false);
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
                        enemyCharacter.maxWaypointIndex = waypoints.Length - 1; // 실제 웨이포인트 길이에 맞게 설정
                        enemyCharacter.areaIndex = 2;
                        enemyCharacter.selectedRoute = selectedRoute; // 선택된 루트 저장



                        enemyCharacter.attackPower = data.attackPower;
                        enemyCharacter.attackSpeed = data.attackSpeed;
                        enemyCharacter.attackRange = data.attackRange;
                        enemyCharacter.currentHP = data.maxHP;
                        enemyCharacter.star = data.initialStar;
                        enemyCharacter.ApplyStarVisual();
                        enemyCharacter.moveSpeed = data.moveSpeed;

                        Debug.Log($"[PlacementManager] [{data.characterName}] (지역2) 몬스터 소환 - {selectedRoute} 루트 선택 (cost={data.cost})");

                        var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                        if (selectUI != null)
                        {
                            selectUI.MarkCardAsUsed(currentCharacterIndex);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[PlacementManager] WaveSpawnerRegion2/enemyMonsterPanel이 없어 소환 실패");
                }
            }
            else if (tile.IsPlacable() || tile.IsPlacable2())
            {
                bool isArea2Tile = tile.IsPlacable2();
                RectTransform targetParent = isArea2Tile
                    ? (opponentCharacterPanel != null ? opponentCharacterPanel : characterPanel)
                    : characterPanel;

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

                        if (isArea2Tile && opponentBulletPanel != null)
                        {
                            cComp.opponentBulletPanel = opponentBulletPanel;
                        }
                        else
                        {
                            cComp.SetBulletPanel(bulletPanel);
                        }
                    }

                    CreatePlaceTileChild(tile);
                    Debug.Log($"[PlacementManager] [{data.characterName}] 드래그 배치(Placable/Placable2)");
                    success = true;
                }
                else
                {
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                    }
                    Debug.LogError("[PlacementManager] placable => Instantiate 실패");
                    return false;
                }
            }
            else if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                bool isArea2Tile = tile.IsPlaced2();
                RectTransform targetParent = isArea2Tile
                    ? (opponentCharacterPanel != null ? opponentCharacterPanel : characterPanel)
                    : characterPanel;

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

                        if (isArea2Tile && opponentBulletPanel != null)
                        {
                            cComp.opponentBulletPanel = opponentBulletPanel;
                        }
                        else
                        {
                            cComp.SetBulletPanel(bulletPanel);
                        }
                    }

                    CreatePlaceTileChild(tile);
                    Debug.Log($"[PlacementManager] [{data.characterName}] 드래그 배치(PlaceTile/Placed2)");
                    success = true;
                }
                else
                {
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                    }
                    Debug.LogError("[PlacementManager] PlaceTile/Placed2 => Instantiate 실패");
                    return false;
                }
            }
            else
            {
                if (mineralsSpent && targetMineralBar != null)
                {
                    targetMineralBar.RefundMinerals(data.cost);
                    Debug.Log($"[PlacementManager] {tile.name} 상태를 처리할 수 없음 => 미네랄 {data.cost} 환불");
                }
                Debug.LogWarning($"[PlacementManager] {tile.name} 상태를 처리할 수 없습니다 (드래그 소환).");
                return false;
            }

            return success;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlacementManager] 소환 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    // (C) "드래그된 캐릭터"를 새 타일로 옮기거나(이동) / 합성 시도 [강화됨]
    // ---------------------------------------------------------------------------
    public void OnDropCharacter(Character movingChar, Tile newTile)
    {
        if (movingChar == null || newTile == null) 
        {
            Debug.LogWarning("[PlacementManager] OnDropCharacter: movingChar 또는 newTile이 null");
            return;
        }

        Debug.Log($"[PlacementManager] 드래그 드롭: {movingChar.characterName} -> {newTile.name}");

        Tile oldTile = movingChar.currentTile;
        
        // ▼▼ [강화] 이전 타일 참조 완전히 정리 ▼▼
        if (oldTile != null)
        {
            Debug.Log($"[PlacementManager] {movingChar.characterName}의 이전 타일 {oldTile.name} 참조 정리");
            
            // placed tile인 경우 상태 재확인
            if (oldTile.IsPlaceTile() || oldTile.IsPlaced2())
            {
                // 다른 캐릭터가 있는지 확인
                Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
                bool hasOtherCharacter = false;
                foreach (var c in allChars)
                {
                    if (c != null && c != movingChar && c.currentTile == oldTile)
                    {
                        hasOtherCharacter = true;
                        break;
                    }
                }
                
                // placed tile은 자식 관리가 아닌 비주얼 업데이트만 필요
                if (!hasOtherCharacter)
                {
                    // placed tile에서 캐릭터가 없어졌으므로 시각적 업데이트
                    oldTile.RefreshTileVisual();
                    Debug.Log($"[PlacementManager] placed tile {oldTile.name}에서 캐릭터 제거됨");
                }
            }
            else
            {
                // placable tile의 경우 기존 로직
                RemovePlaceTileChild(oldTile);
            }
        }

        bool occupantExists = CheckAnyCharacterHasCurrentTile(newTile);

        if (occupantExists)
        {
            bool success = TryMergeCharacter(movingChar, newTile);
            if (!success)
            {
                if (oldTile != null)
                {
                    MoveCharacterToTile(movingChar, oldTile);
                    CreatePlaceTileChild(oldTile);
                }
            }
        }
        else
        {
            if (!newTile.CanPlaceCharacter())
            {
                if (oldTile != null)
                {
                    MoveCharacterToTile(movingChar, oldTile);
                    CreatePlaceTileChild(oldTile);
                }
                return;
            }

            if (newTile.IsWalkable() || newTile.IsWalkableLeft() || newTile.IsWalkableCenter() || newTile.IsWalkableRight())
            {
                WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
                if (spawner != null && ourMonsterPanel != null)
                {
                    // 타일 위치에 따른 루트 선택
                    RouteType selectedRoute = DetermineRouteFromTile(newTile, spawner);
                    Transform[] waypoints = GetWaypointsForRoute(spawner, selectedRoute);
                    
                    if (waypoints == null || waypoints.Length == 0)
                    {
                        Debug.LogWarning($"[PlacementManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
                        if (oldTile != null)
                        {
                            MoveCharacterToTile(movingChar, oldTile);
                            CreatePlaceTileChild(oldTile);
                        }
                        return;
                    }
                    
                    Vector3 spawnPos = waypoints[0].position;

                    RectTransform charRect = movingChar.GetComponent<RectTransform>();
                    if (charRect != null)
                    {
                        Vector2 localPos = ourMonsterPanel.InverseTransformPoint(spawnPos);
                        charRect.SetParent(ourMonsterPanel, false);
                        charRect.anchoredPosition = localPos;
                        charRect.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        movingChar.transform.SetParent(null);
                        movingChar.transform.position = spawnPos;
                        movingChar.transform.localRotation = Quaternion.identity;
                    }

                    movingChar.currentTile = null;
                    movingChar.currentWaypointIndex = 0;
                    movingChar.pathWaypoints = waypoints;
                    movingChar.maxWaypointIndex = waypoints.Length - 1; // 실제 웨이포인트 길이에 맞게 설정
                    movingChar.areaIndex = 1;
                    movingChar.selectedRoute = selectedRoute; // 선택된 루트 저장
                    movingChar.isCharAttack = !movingChar.isHero;

                    Debug.Log($"[PlacementManager] 드래그로 (placable→walkable) 이동 => {selectedRoute} 루트 선택, waypoint[0]에서 시작");
                }
                else
                {
                    if (oldTile != null)
                    {
                        MoveCharacterToTile(movingChar, oldTile);
                        CreatePlaceTileChild(oldTile);
                    }
                    return;
                }
            }
            else if (newTile.IsWalkable2() || newTile.IsWalkable2Left() || newTile.IsWalkable2Center() || newTile.IsWalkable2Right())
            {
                WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                if (spawner2 != null && opponentOurMonsterPanel != null)
                {
                    // 타일 위치에 따른 루트 선택
                    RouteType selectedRoute = DetermineRouteFromTile(newTile, spawner2);
                    Transform[] waypoints = GetWaypointsForRoute(spawner2, selectedRoute);
                    
                    if (waypoints == null || waypoints.Length == 0)
                    {
                        Debug.LogWarning($"[PlacementManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
                        if (oldTile != null)
                        {
                            MoveCharacterToTile(movingChar, oldTile);
                            CreatePlaceTileChild(oldTile);
                        }
                        return;
                    }
                    
                    Vector3 spawnPos2 = waypoints[0].position;

                    RectTransform charRect = movingChar.GetComponent<RectTransform>();
                    if (charRect != null)
                    {
                        Vector2 localPos = opponentOurMonsterPanel.InverseTransformPoint(spawnPos2);
                        charRect.SetParent(opponentOurMonsterPanel, false);
                        charRect.localRotation = Quaternion.identity;
                        charRect.anchoredPosition = localPos;
                    }
                    else
                    {
                        movingChar.transform.SetParent(null);
                        movingChar.transform.position = spawnPos2;
                        movingChar.transform.localRotation = Quaternion.identity;
                    }

                    movingChar.currentTile = null;
                    movingChar.currentWaypointIndex = 0;
                    movingChar.pathWaypoints = waypoints;
                    movingChar.maxWaypointIndex = waypoints.Length - 1; // 실제 웨이포인트 길이에 맞게 설정
                    movingChar.areaIndex = 2;
                    movingChar.selectedRoute = selectedRoute; // 선택된 루트 저장
                    movingChar.isCharAttack = !movingChar.isHero;

                    Debug.Log($"[PlacementManager] 드래그로 (placable→walkable2) 이동 => {selectedRoute} 루트 선택, waypoint[0]에서 시작");
                }
                else
                {
                    if (oldTile != null)
                    {
                        MoveCharacterToTile(movingChar, oldTile);
                        CreatePlaceTileChild(oldTile);
                    }
                    return;
                }
            }
            else
            {
                MoveCharacterToTile(movingChar, newTile);
                movingChar.currentWaypointIndex = -1;
                movingChar.maxWaypointIndex = -1; // placable/placed 타일에서는 웨이포인트 사용 안함
                movingChar.isCharAttack = false;
            }

            CreatePlaceTileChild(newTile);
            Debug.Log("[PlacementManager] 캐릭터가 새 타일로 이동(또는 웨이포인트 모드) 완료");
        }
    }

    // =========================== [강화된 타일 참조 관리 시스템] ===========================
    /// <summary>
    /// [강화] 캐릭터가 타일을 점유하고 있는지 확인하며, 중복 참조 감지 및 자동 정리도 수행
    /// </summary>
    private bool CheckAnyCharacterHasCurrentTile(Tile tile)
    {
        if (tile == null) return false;

        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        List<Character> occupyingChars = new List<Character>();
        
        // 해당 타일을 참조하는 모든 캐릭터 찾기
        foreach (var c in allChars)
        {
            if (c != null && c.currentTile == tile)
            {
                occupyingChars.Add(c);
            }
        }
        
        // 중복 참조 감지 및 정리
        if (occupyingChars.Count > 1)
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} 타일에 {occupyingChars.Count}개의 중복 참조 감지! 자동 정리 수행");
            CleanupDuplicateReferences(tile, occupyingChars);
            return occupyingChars.Count > 0;
        }
        
        // 유효하지 않은 참조 정리 (캐릭터가 실제로는 다른 위치에 있는 경우)
        for (int i = occupyingChars.Count - 1; i >= 0; i--)
        {
            Character c = occupyingChars[i];
            if (Vector3.Distance(c.transform.position, tile.transform.position) > 2.0f) // 임계값 설정
            {
                Debug.LogWarning($"[PlacementManager] {c.characterName}이 {tile.name}을 참조하지만 실제 위치가 다름. 참조 정리");
                c.currentTile = null;
                occupyingChars.RemoveAt(i);
            }
        }
        
        // ▼▼ [추가] PlaceTile/Placed2 상태 실시간 반영 ▼▼
        bool hasOccupant = occupyingChars.Count > 0;
        
        // placed tile은 이미 그 자체가 PlaceTile/Placed2 타입이므로 자식 관리 불필요
        if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            // ▼▼ [추가] placed tile의 경우 실시간 상태 업데이트 ▼▼
            if (!hasOccupant)
            {
                // placed tile이 비어있으면 즉시 비주얼 업데이트
                tile.RefreshTileVisual();
            }
            return hasOccupant;
        }
        
        // placable tile의 경우만 자식 관리
        bool hasPlaceTileChild = (tile.transform.Find("PlaceTile") != null || tile.transform.Find("Placed2") != null);
        
        // 캐릭터가 있는데 PlaceTile 자식이 없으면 생성
        if (hasOccupant && !hasPlaceTileChild)
        {
            Debug.Log($"[PlacementManager] {tile.name}에 캐릭터가 있지만 PlaceTile 자식이 없음. 생성");
            CreatePlaceTileChild(tile);
        }
        // 캐릭터가 없는데 PlaceTile 자식이 있으면 제거
        else if (!hasOccupant && hasPlaceTileChild)
        {
            Debug.Log($"[PlacementManager] {tile.name}에 캐릭터가 없지만 PlaceTile 자식이 있음. 제거");
            RemovePlaceTileChild(tile);
        }
        
        return hasOccupant;
    }

    private void CleanupDuplicateReferences(Tile tile, List<Character> duplicateChars)
    {
        if (duplicateChars.Count <= 1) return;
        
        Character closestChar = null;
        float closestDistance = float.MaxValue;
        
        foreach (var c in duplicateChars)
        {
            float distance = Vector3.Distance(c.transform.position, tile.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestChar = c;
            }
        }
        
        foreach (var c in duplicateChars)
        {
            if (c != closestChar)
            {
                Debug.Log($"[PlacementManager] {c.characterName}의 {tile.name} 참조 해제 (중복 정리)");
                c.currentTile = null;
            }
        }
    }

    public void CleanupDestroyedCharacterReferences()
    {
        // ▼▼ [수정] 안전한 참조 정리를 위한 null 체크 강화 ▼▼
        if (this == null || gameObject == null || !gameObject.activeInHierarchy) return;
        
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var c in allChars)
        {
            // 캐릭터 오브젝트가 유효한지 확인
            if (c == null || c.gameObject == null) continue;
            
            // 캐릭터가 활성화되어 있고 파괴 중이 아닌지 확인
            if (!c.gameObject.activeInHierarchy) continue;
            
            // 타일 참조가 유효한지 확인
            if (c.currentTile != null && c.currentTile.gameObject != null)
            {
                // 거리 계산 전에 transform이 유효한지 확인
                if (c.transform != null && c.currentTile.transform != null)
                {
                    float distance = Vector3.Distance(c.transform.position, c.currentTile.transform.position);
                    if (distance > 5.0f)
                    {
                        Debug.LogWarning($"[PlacementManager] {c.characterName}의 타일 참조가 잘못됨 (거리: {distance}). 참조 정리");
                        c.currentTile = null;
                    }
                }
            }
        }
    }

    public void CleanupAllTileReferences()
    {
        Debug.Log("[PlacementManager] 전체 타일 참조 정리 시작");
        
        CleanupDestroyedCharacterReferences();
        
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        int cleanedCount = 0;
        
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            List<Character> tileOccupants = new List<Character>();
            
            foreach (var c in allChars)
            {
                if (c != null && c.currentTile == tile)
                {
                    tileOccupants.Add(c);
                }
            }
            
            if (tileOccupants.Count > 1)
            {
                CleanupDuplicateReferences(tile, tileOccupants);
                cleanedCount++;
            }
        }
        
        Debug.Log($"[PlacementManager] 전체 타일 참조 정리 완료 - {cleanedCount}개 타일에서 중복 참조 정리됨");
    }

    public void ClearCharacterTileReference(Character character)
    {
        if (character == null) return;
        
        Tile oldTile = character.currentTile;
        character.currentTile = null;
        
        if (oldTile != null)
        {
            // placed tile과 placable tile 구분 처리
            if (oldTile.IsPlaceTile() || oldTile.IsPlaced2())
            {
                // placed tile은 비주얼만 업데이트
                oldTile.RefreshTileVisual();
                Debug.Log($"[PlacementManager] {character.characterName}의 placed tile {oldTile.name} 비주얼 업데이트");
            }
            else
            {
                // placable tile은 자식 제거
                RemovePlaceTileChild(oldTile);
                Debug.Log($"[PlacementManager] {character.characterName}의 {oldTile.name} 참조 정리 완료");
            }
        }
    }

    private float lastCleanupTime = 0f;
    private const float CLEANUP_INTERVAL = 5f;

    private bool TryMergeCharacter(Character movingChar, Tile newTile)
    {
        Debug.Log($"[PlacementManager] TryMergeCharacter: movingChar={movingChar.characterName}, star={movingChar.star}, tile={newTile.name}");

        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var otherChar in allChars)
        {
            if (otherChar == null || otherChar == movingChar) continue;

            if (otherChar.currentTile == newTile)
            {
                if (otherChar.star == movingChar.star && otherChar.characterName == movingChar.characterName)
                {
                    // ▼▼ [수정] 프리팹 교체를 위한 준비 ▼▼
                    CharacterStar targetStar = CharacterStar.OneStar;
                    bool shouldReplace = false;
                    
                    switch (otherChar.star)
                    {
                        case CharacterStar.OneStar:
                            targetStar = CharacterStar.TwoStar;
                            shouldReplace = true;
                            break;

                        case CharacterStar.TwoStar:
                            targetStar = CharacterStar.TwoStar; // 2성끼리 합성해도 2성
                            shouldReplace = true;
                            break;

                        case CharacterStar.ThreeStar:
                            targetStar = CharacterStar.ThreeStar; // 3성끼리 합성해도 3성
                            shouldReplace = true;
                            break;

                        default:
                            Debug.Log("[PlacementManager] 알 수 없는 별 => 합성 불가");
                            return false;
                    }
                    
                    if (shouldReplace)
                    {
                        // ▼▼ [수정] 프리팹 교체 로직 ▼▼
                        return ReplaceCharacterWithNewStar(otherChar, movingChar, targetStar);
                    }
                }
                else
                {
                    Debug.Log("[PlacementManager] 별이 다르거나 다른 캐릭터명 => 합성 불가");
                    return false;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// 캐릭터를 새로운 별 등급의 프리팹으로 교체
    /// </summary>
    private bool ReplaceCharacterWithNewStar(Character targetChar, Character movingChar, CharacterStar newStar)
    {
        Debug.Log($"[PlacementManager] 프리팹 교체 시작: {targetChar.characterName} -> {newStar}");
        
        // 기존 캐릭터 정보 저장
        Tile currentTile = targetChar.currentTile;
        int areaIndex = targetChar.areaIndex;
        Vector3 position = targetChar.transform.position;
        Transform parent = targetChar.transform.parent;
        
        // StarMergeDatabase에서 새로운 캐릭터 데이터 선택
        StarMergeDatabaseObject targetDB = (areaIndex == 2 && starMergeDatabaseRegion2 != null) 
            ? starMergeDatabaseRegion2 : starMergeDatabase;
            
        if (targetDB == null)
        {
            Debug.LogWarning("[PlacementManager] StarMergeDatabase가 null입니다.");
            RandomizeAppearanceByStarAndRace(targetChar, newStar);
            UpgradeStats(targetChar);
            Destroy(movingChar.gameObject);
            return true;
        }
        
        // 새로운 캐릭터 데이터 선택
        RaceType raceType = (RaceType)targetChar.race;
        CharacterData newCharData = null;
        
        if (newStar == CharacterStar.TwoStar)
        {
            newCharData = targetDB.GetRandom2Star(raceType);
        }
        else if (newStar == CharacterStar.ThreeStar)
        {
            newCharData = targetDB.GetRandom3Star(raceType);
        }
        
        if (newCharData == null || newCharData.spawnPrefab == null)
        {
            Debug.LogWarning($"[PlacementManager] {newStar} 프리팹을 찾을 수 없습니다. 기존 방식으로 처리");
            RandomizeAppearanceByStarAndRace(targetChar, newStar);
            UpgradeStats(targetChar);
            Destroy(movingChar.gameObject);
            return true;
        }
        
        // 새로운 프리팹으로 캐릭터 생성
        GameObject newCharObj = Instantiate(newCharData.spawnPrefab, parent);
        if (newCharObj == null)
        {
            Debug.LogError("[PlacementManager] 새 프리팹 생성 실패");
            return false;
        }
        
        // 위치 설정
        RectTransform newCharRect = newCharObj.GetComponent<RectTransform>();
        if (newCharRect != null)
        {
            RectTransform oldRect = targetChar.GetComponent<RectTransform>();
            if (oldRect != null)
            {
                newCharRect.anchoredPosition = oldRect.anchoredPosition;
                newCharRect.localRotation = oldRect.localRotation;
            }
        }
        else
        {
            newCharObj.transform.position = position;
            newCharObj.transform.localRotation = targetChar.transform.localRotation;
        }
        
        // Character 컴포넌트 설정
        Character newCharacter = newCharObj.GetComponent<Character>();
        if (newCharacter != null)
        {
            // 기본 정보 복사
            newCharacter.currentTile = currentTile;
            newCharacter.areaIndex = areaIndex;
            newCharacter.isHero = targetChar.isHero;
            newCharacter.isCharAttack = targetChar.isCharAttack;
            newCharacter.currentWaypointIndex = targetChar.currentWaypointIndex;
            newCharacter.maxWaypointIndex = targetChar.maxWaypointIndex;
            newCharacter.pathWaypoints = targetChar.pathWaypoints;
            
            // 새로운 데이터 적용
            newCharacter.characterName = newCharData.characterName;
            newCharacter.race = newCharData.race;
            newCharacter.star = newStar;
            
            // 스탯 설정 (업그레이드된 값)
            float statMultiplier = 1.0f;
            switch (newStar)
            {
                case CharacterStar.TwoStar:
                    statMultiplier = 1.3f;
                    break;
                case CharacterStar.ThreeStar:
                    statMultiplier = 1.6f;
                    break;
            }
            
            newCharacter.attackPower = newCharData.attackPower * statMultiplier;
            newCharacter.attackSpeed = newCharData.attackSpeed * 1.1f;
            newCharacter.attackRange = newCharData.attackRange * 1.1f;
            newCharacter.currentHP = newCharData.maxHP * statMultiplier;
            newCharacter.moveSpeed = newCharData.moveSpeed;
            
            // 별 비주얼 적용
            newCharacter.ApplyStarVisual();
            
            // 패널 설정
            if (areaIndex == 2 && opponentBulletPanel != null)
            {
                newCharacter.opponentBulletPanel = opponentBulletPanel;
            }
            else
            {
                newCharacter.SetBulletPanel(bulletPanel);
            }
            
            // ▼▼ [핵심] 앞뒤 이미지 적용 ▼▼
            if (newCharData.frontSprite != null || newCharData.backSprite != null)
            {
                ApplyFrontBackImages(newCharacter, newCharData);
            }
        }
        
        // 기존 캐릭터들 제거
        Destroy(targetChar.gameObject);
        Destroy(movingChar.gameObject);
        
        Debug.Log($"[PlacementManager] 합성 성공! 새로운 {newStar} 캐릭터 '{newCharData.characterName}' 생성 완료");
        return true;
    }
    
    /// <summary>
    /// 캐릭터에 앞뒤 이미지 적용
    /// </summary>
    private void ApplyFrontBackImages(Character character, CharacterData data)
    {
        // 기존 이미지 컴포넌트 찾기 또는 생성
        Transform frontImageObj = character.transform.Find("FrontImage");
        Transform backImageObj = character.transform.Find("BackImage");
        
        // FrontImage 생성 또는 업데이트
        if (data.frontSprite != null)
        {
            if (frontImageObj == null)
            {
                GameObject frontGO = new GameObject("FrontImage");
                frontGO.transform.SetParent(character.transform, false);
                frontImageObj = frontGO.transform;
                
                Image frontImg = frontGO.AddComponent<Image>();
                RectTransform frontRect = frontGO.GetComponent<RectTransform>();
                frontRect.anchorMin = new Vector2(0.5f, 0.5f);
                frontRect.anchorMax = new Vector2(0.5f, 0.5f);
                frontRect.pivot = new Vector2(0.5f, 0.5f);
                frontRect.sizeDelta = new Vector2(100, 100); // 크기 조정 필요시 수정
                frontRect.anchoredPosition = Vector2.zero;
            }
            
            Image frontImage = frontImageObj.GetComponent<Image>();
            if (frontImage != null)
            {
                frontImage.sprite = data.frontSprite;
                frontImage.preserveAspect = true;
            }
            
            // 처음에는 숨김 (Character 스크립트에서 방향에 따라 표시)
            frontImageObj.gameObject.SetActive(false);
        }
        
        // BackImage 생성 또는 업데이트
        if (data.backSprite != null)
        {
            if (backImageObj == null)
            {
                GameObject backGO = new GameObject("BackImage");
                backGO.transform.SetParent(character.transform, false);
                backImageObj = backGO.transform;
                
                Image backImg = backGO.AddComponent<Image>();
                RectTransform backRect = backGO.GetComponent<RectTransform>();
                backRect.anchorMin = new Vector2(0.5f, 0.5f);
                backRect.anchorMax = new Vector2(0.5f, 0.5f);
                backRect.pivot = new Vector2(0.5f, 0.5f);
                backRect.sizeDelta = new Vector2(100, 100); // 크기 조정 필요시 수정
                backRect.anchoredPosition = Vector2.zero;
            }
            
            Image backImage = backImageObj.GetComponent<Image>();
            if (backImage != null)
            {
                backImage.sprite = data.backSprite;
                backImage.preserveAspect = true;
            }
            
            // 기본적으로 뒷모습 표시
            backImageObj.gameObject.SetActive(true);
        }
        
        Debug.Log($"[PlacementManager] {character.characterName}에 앞뒤 이미지 적용 완료");
    }

    private void RandomizeAppearanceByStarAndRace(Character ch, CharacterStar newStar)
    {
        StarMergeDatabaseObject targetDB;
        if (ch.areaIndex == 2 && starMergeDatabaseRegion2 != null)
        {
            targetDB = starMergeDatabaseRegion2;
        }
        else
        {
            targetDB = starMergeDatabase;
        }

        if (targetDB != null)
        {
            RaceType raceType = (RaceType)ch.race;
            CharacterData chosenData = null;

            if (newStar == CharacterStar.TwoStar)
            {
                chosenData = targetDB.GetRandom2Star(raceType);
            }
            else if (newStar == CharacterStar.ThreeStar)
            {
                chosenData = targetDB.GetRandom3Star(raceType);
            }

            if (chosenData != null)
            {
                ch.characterName = chosenData.characterName;
                ch.race = chosenData.race;

                if (chosenData.buttonIcon != null && ch.GetComponentInChildren<SpriteRenderer>() == null)
                {
                    var uiImg = ch.GetComponentInChildren<UnityEngine.UI.Image>();
                    if (uiImg != null)
                    {
                        uiImg.sprite = chosenData.buttonIcon.sprite;
                    }
                }

                if (newStar == CharacterStar.TwoStar || newStar == CharacterStar.ThreeStar)
                {
                    if (chosenData.frontSprite != null)
                    {
                        SetFrontImage(ch, chosenData.frontSprite);
                    }
                    if (chosenData.backSprite != null)
                    {
                        SetBackImage(ch, chosenData.backSprite);
                    }
                    Debug.Log($"[PlacementManager] {newStar}에서 앞뒤 이미지 적용 완료 (starMergeDB)");
                }

                Debug.Log($"[PlacementManager] (area={ch.areaIndex}) {newStar} 변경 => {ch.characterName} (starMergeDB 사용)");
                return;
            }
        }

        // starMergeDB가 없거나 선택 실패 -> fallback: characterDatabase
        if (characterDatabase == null || characterDatabase.currentRegisteredCharacters == null)
        {
            Debug.LogWarning("[PlacementManager] characterDatabase가 없어 임의 외형 변경 불가(fallback도 실패)");
            return;
        }

        List<CharacterData> candidateList = new List<CharacterData>();
        foreach (var cd in characterDatabase.currentRegisteredCharacters)
        {
            if (cd == null) continue;
            if (cd.initialStar == newStar)
            {
                candidateList.Add(cd);
            }
        }
        if (candidateList.Count == 0)
        {
            Debug.LogWarning($"[PlacementManager] fallback: {newStar} 캐릭터가 DB에 없음 => 외형 교체 불가");
            return;
        }

        List<CharacterData> sameRace = new List<CharacterData>();
        List<CharacterData> otherRace = new List<CharacterData>();

        foreach (var cd in candidateList)
        {
            if (cd.race == ch.race) sameRace.Add(cd);
            else otherRace.Add(cd);
        }

        float randVal = Random.value;
        const float SAME_RACE_PROB = 0.7f;
        CharacterData chosenAlt = null;

        if (randVal < SAME_RACE_PROB && sameRace.Count > 0)
        {
            chosenAlt = sameRace[Random.Range(0, sameRace.Count)];
        }
        else
        {
            if (otherRace.Count > 0)
            {
                chosenAlt = otherRace[Random.Range(0, otherRace.Count)];
            }
            else
            {
                chosenAlt = sameRace[Random.Range(0, sameRace.Count)];
            }
        }

        if (chosenAlt == null)
        {
            Debug.LogWarning("[PlacementManager] 랜덤 선택 실패 => 외형 교체 불가");
            return;
        }

        ch.characterName = chosenAlt.characterName;
        ch.race = chosenAlt.race;

        if (chosenAlt.buttonIcon != null && ch.GetComponentInChildren<SpriteRenderer>() == null)
        {
            var uiImg = ch.GetComponentInChildren<UnityEngine.UI.Image>();
            if (uiImg != null)
            {
                uiImg.sprite = chosenAlt.buttonIcon.sprite;
            }
        }

        if (newStar == CharacterStar.TwoStar || newStar == CharacterStar.ThreeStar)
        {
            if (chosenAlt.frontSprite != null)
            {
                SetFrontImage(ch, chosenAlt.frontSprite);
            }
            if (chosenAlt.backSprite != null)
            {
                SetBackImage(ch, chosenAlt.backSprite);
            }
            Debug.Log($"[PlacementManager] (fallback) {newStar} => '{ch.characterName}' 앞뒤 이미지 적용 (70% sameRace)");
        }

        Debug.Log($"[PlacementManager] (fallback) {newStar} => '{ch.characterName}'로 종족 변경 (70% sameRace)");
    }

    private void UpgradeStats(Character ch)
    {
        float baseAtk = ch.attackPower / 1.6f;
        float baseRange = ch.attackRange / 1.2f;
        float baseSpeed = ch.attackSpeed / 1.2f;

        switch (ch.star)
        {
            case CharacterStar.OneStar:
                ch.attackPower = baseAtk;
                ch.attackRange = baseRange;
                ch.attackSpeed = baseSpeed;
                break;
            case CharacterStar.TwoStar:
                ch.attackPower = baseAtk * 1.3f;
                ch.attackRange = baseRange * 1.1f;
                ch.attackSpeed = baseSpeed * 1.1f;
                break;
            case CharacterStar.ThreeStar:
                ch.attackPower = baseAtk * 1.6f;
                ch.attackRange = baseRange * 1.2f;
                ch.attackSpeed = baseSpeed * 1.2f;
                break;
        }
        ch.ApplyStarVisual();
    }

    private void MoveCharacterToTile(Character character, Tile tile)
    {
        if (tile == null) return;

        bool isArea2 = tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right() || tile.IsPlacable2() || tile.IsPlaced2();

        RectTransform charRect = character.GetComponent<RectTransform>();
        if (charRect != null)
        {
            RectTransform targetParent;
            if ((tile.IsWalkable() || tile.IsWalkableLeft() || tile.IsWalkableCenter() || tile.IsWalkableRight() || 
                 tile.IsWalkable2() || tile.IsWalkable2Left() || tile.IsWalkable2Center() || tile.IsWalkable2Right()) && ourMonsterPanel != null)
            {
                targetParent = ourMonsterPanel;
            }
            else
            {
                targetParent = isArea2
                    ? (opponentCharacterPanel != null ? opponentCharacterPanel : characterPanel)
                    : characterPanel;
            }

            RectTransform tileRect = tile.GetComponent<RectTransform>();
            if (tileRect != null)
            {
                Vector2 localPos = targetParent.InverseTransformPoint(tileRect.transform.position);
                charRect.SetParent(targetParent, false);
                charRect.anchoredPosition = localPos;
                charRect.localRotation = Quaternion.identity;
            }
            else
            {
                character.transform.SetParent(targetParent, false);
                character.transform.position = tile.transform.position;
                character.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            character.transform.position = tile.transform.position;
            character.transform.localRotation = Quaternion.identity;
        }

        character.currentTile = tile;
        character.areaIndex = isArea2 ? 2 : 1;
    }

    public void CreatePlaceTileChild(Tile tile)
    {
        if (tile == null) return;
        
        // ▼▼ [수정] placed tile은 이미 PlaceTile/Placed2 타입이므로 자식 생성 불필요 ▼▼
        if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            Debug.Log($"[PlacementManager] {tile.name}은 이미 placed tile이므로 자식 생성 생략");
            tile.RefreshTileVisual();
            return;
        }
        
        // 기존 PlaceTile/Placed2 자식 제거
        Transform exist = tile.transform.Find("PlaceTile");
        Transform exist2 = tile.transform.Find("Placed2");
        if (exist != null) Destroy(exist.gameObject);
        if (exist2 != null) Destroy(exist2.gameObject);
        
        // 새로운 PlaceTile 자식 생성 (placable tile용)
        GameObject placeTileObj = new GameObject("PlaceTile");
        placeTileObj.transform.SetParent(tile.transform, false);
        placeTileObj.transform.localPosition = Vector3.zero;
        
        // ▼▼ [추가] 타일 비주얼 업데이트 ▼▼
        tile.RefreshTileVisual();
        
        Debug.Log($"[PlacementManager] {tile.name}에 {placeTileObj.name} 자식 생성 완료");
    }

    public void RemovePlaceTileChild(Tile tile)
    {
        if (tile == null) return;
        
        Transform exist = tile.transform.Find("PlaceTile");
        if (exist != null)
        {
            Destroy(exist.gameObject);
            Debug.Log($"[PlacementManager] {tile.name}의 PlaceTile 자식 제거");
        }
        Transform exist2 = tile.transform.Find("Placed2");
        if (exist2 != null)
        {
            Destroy(exist2.gameObject);
            Debug.Log($"[PlacementManager] {tile.name}의 Placed2 자식 제거");
        }
        
        // ▼▼ [추가] 타일 비주얼 업데이트 ▼▼
        tile.RefreshTileVisual();
    }

    public int FindCharacterIndexInEnemyDatabase(CharacterData character)
    {
        if (enemyDatabase == null || enemyDatabase.characters == null)
        {
            Debug.LogWarning("[PlacementManager] enemyDatabase가 null이거나 비어있습니다.");
            return -1;
        }

        for (int i = 0; i < enemyDatabase.characters.Length; i++)
        {
            CharacterData data = enemyDatabase.characters[i];
            if (data != null && data.characterName == character.characterName)
            {
                return i;
            }
        }
        return -1;
    }

    private int FindCharacterIndex(Character character)
    {
        if (characterDatabase == null || characterDatabase.currentRegisteredCharacters == null) return -1;

        for (int i = 0; i < characterDatabase.currentRegisteredCharacters.Length; i++)
        {
            if (characterDatabase.currentRegisteredCharacters[i] != null &&
                characterDatabase.currentRegisteredCharacters[i].characterName == character.characterName)
            {
                return i;
            }
        }
        return -1;
    }

    // =========================== [새로 추가한 메서드] ===========================
    /// <summary>
    /// 특정 타일에 있는 캐릭터를 제거(파괴)한다.
    /// 1) 별에 따른 아이템 획득 확률 (1성=10%, 2성=50%, 3성=90%)
    /// 2) 별에 따른 미네랄 환급 (cost/2 * {1성=0.9, 2성=0.5, 3성=0.1})
    /// 3) 실제 오브젝트 Destroy + 타일 자식 제거
    /// </summary>
    public void RemoveCharacterOnTile(Tile tile)
    {
        if (tile == null) return;

        // 1) 타일에 배치된 캐릭터 찾기
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
            Debug.LogWarning($"[PlacementManager] {tile.name}에 캐릭터가 없어서 제거 불가");
            return;
        }

        // 캐릭터의 코스트 찾기
        int cost = 10; // 기본 코스트 값

        // 데이터베이스에서 해당 캐릭터 찾기
        if (characterDatabase != null && characterDatabase.currentRegisteredCharacters != null)
        {
            for (int i = 0; i < characterDatabase.currentRegisteredCharacters.Length; i++)
            {
                CharacterData data = characterDatabase.currentRegisteredCharacters[i];
                if (data != null && data.characterName == occupant.characterName)
                {
                    cost = data.cost;
                    Debug.Log($"[PlacementManager] '{occupant.characterName}'의 코스트 정보 찾음: {cost}");
                    break;
                }
            }
        }

        // 지역2 캐릭터라면 enemyDatabase에서도 찾아보기
        if (occupant.areaIndex == 2 && enemyDatabase != null && enemyDatabase.characters != null)
        {
            for (int i = 0; i < enemyDatabase.characters.Length; i++)
            {
                CharacterData data = enemyDatabase.characters[i];
                if (data != null && data.characterName == occupant.characterName)
                {
                    cost = data.cost;
                    Debug.Log($"[PlacementManager] '{occupant.characterName}'의 코스트 정보 찾음 (enemyDB): {cost}");
                    break;
                }
            }
        }

        // 2) 별에 따른 아이템 획득 시도
        float itemChance = 0f;
        switch (occupant.star)
        {
            case CharacterStar.OneStar:
                itemChance = 0.10f; // 10%
                break;
            case CharacterStar.TwoStar:
                itemChance = 0.50f; // 50%
                break;
            case CharacterStar.ThreeStar:
                itemChance = 0.90f; // 90%
                break;
        }

        bool itemSuccess = (Random.value < itemChance);

        // ▼▼ [수정] 여기서 실제로 itemDatabase에서 랜덤 아이템을 찾아 인벤토리에 추가 ▼▼        if (itemSuccess)        {            if (itemInventoryManager == null)            {                Debug.LogWarning("[PlacementManager] itemInventoryManager가 null입니다. Inspector에서 연결해주세요!");            }            else if (itemInventoryManager.GetOwnedItems().Count < 9)            {                if (itemInventoryManager.ItemDatabase != null && itemInventoryManager.ItemDatabase.items != null                    && itemInventoryManager.ItemDatabase.items.Length > 0)                {                    int randomIndex = Random.Range(0, itemInventoryManager.ItemDatabase.items.Length);                    ItemData rewardItem = itemInventoryManager.ItemDatabase.items[randomIndex];                    // 인벤토리에 추가                    itemInventoryManager.AddItem(rewardItem);                    // 아이템 패널 새로고침                    ItemPanelManager panelMgr = FindFirstObjectByType<ItemPanelManager>();                    if (panelMgr != null)                    {                        panelMgr.RefreshItemPanel();                    }                    Debug.Log($"[PlacementManager] 별={occupant.star} 캐릭터 제거 -> 아이템 획득! '{rewardItem.itemName}'");                }                else                {                    Debug.LogWarning("[PlacementManager] itemDatabase가 비었거나 items가 0개임 => 아이템 실제 지급 불가");                }            }            else            {                Debug.Log($"[PlacementManager] 인벤토리가 가득 참 (9개) => 아이템 획득 불가");            }        }        else        {            Debug.Log($"[PlacementManager] 별={occupant.star} 캐릭터 제거 -> 아이템 획득 실패 (확률: {itemChance*100}%)");        }
        // ▲▲ [수정 끝] ▲▲

        // 3) 미네랄 환급
        float ratio = 0f;
        switch (occupant.star)
        {
            case CharacterStar.OneStar:
                ratio = 0.9f; // 90%
                break;
            case CharacterStar.TwoStar:
                ratio = 0.5f; // 50%
                break;
            case CharacterStar.ThreeStar:
                ratio = 0.1f; // 10%
                break;
        }

        int halfCost = cost / 2;
        float refundFloat = halfCost * ratio;
        int finalRefund = Mathf.FloorToInt(refundFloat);

        if (occupant.areaIndex == 1 && region1MineralBar != null)
        {
            region1MineralBar.RefundMinerals(finalRefund);
            Debug.Log($"[PlacementManager] area1 캐릭터 제거 => 미네랄 {finalRefund} 환급 (코스트 {cost}/2 * {ratio * 100}%)");
        }
        else if (occupant.areaIndex == 2 && region2MineralBar != null)
        {
            region2MineralBar.RefundMinerals(finalRefund);
            Debug.Log($"[PlacementManager] area2 캐릭터 제거 => 미네랄 {finalRefund} 환급 (코스트 {cost}/2 * {ratio * 100}%)");
        }

        // 4) 캐릭터 오브젝트 Destroy
        occupant.currentTile = null;
        Destroy(occupant.gameObject);

        // 5) 타일에서 "PlaceTile"/"Placed2" 자식 제거
        // placed tile인 경우 자식 제거가 아닌 비주얼 업데이트만
        if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            tile.RefreshTileVisual();
            Debug.Log($"[PlacementManager] placed tile {tile.name}에서 캐릭터 제거 후 비주얼 업데이트");
        }
        else
        {
            RemovePlaceTileChild(tile);
        }
        
        // ▼▼ [추가] 타일 상태 즉시 업데이트 ▼▼
        OnCharacterRemovedFromTile(tile);

        Debug.Log($"[PlacementManager] {tile.name} 타일의 캐릭터 제거 완료 (Star={occupant.star})");
    }
    // =====================================================================

    private void SetFrontImage(Character ch, Sprite frontSprite)
    {
        Transform frontObj = ch.transform.Find("FrontImage");
        if (frontObj != null)
        {
            var img = frontObj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.sprite = frontSprite;
            }
        }
    }

    private void SetBackImage(Character ch, Sprite backSprite)
    {
        Transform backObj = ch.transform.Find("BackImage");
        if (backObj != null)
        {
            var img = backObj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.sprite = backSprite;
            }
        }
    }

    private void UpdatePlacedTileStates()
    {
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            // placed tile만 처리
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                // 해당 타일에 캐릭터가 있는지 확인
                bool hasCharacter = false;
                foreach (var c in allChars)
                {
                    if (c != null && c.currentTile == tile)
                    {
                        hasCharacter = true;
                        break;
                    }
                }
                
                // 캐릭터 존재 여부에 따라 비주얼 업데이트
                // placed tile은 자식 관리가 아닌 비주얼 업데이트로 상태 반영
                tile.RefreshTileVisual();
                
                if (!hasCharacter)
                {
                    Debug.Log($"[PlacementManager] placed tile {tile.name}이 비어있음");
                }
            }
        }
    }

    // ▼▼ [추가] 캐릭터 사망/제거 시 즉시 호출되는 타일 상태 업데이트 메서드 ▼▼
    public void OnCharacterRemovedFromTile(Tile tile)
    {
        if (tile == null) return;
        
        Debug.Log($"[PlacementManager] {tile.name} 타일에서 캐릭터 제거됨. 즉시 상태 업데이트");
        
        // placed tile인 경우
        if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            // 즉시 비주얼 업데이트로 빈 타일임을 표시
            tile.RefreshTileVisual();
            
            // 추가로 타일이 비어있음을 확실히 하기 위해 모든 캐릭터 확인
            Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            bool stillHasCharacter = false;
            foreach (var c in allChars)
            {
                if (c != null && c.currentTile == tile)
                {
                    stillHasCharacter = true;
                    break;
                }
            }
            
            if (!stillHasCharacter)
            {
                Debug.Log($"[PlacementManager] {tile.name} placed tile이 완전히 비어있음 확인");
            }
        }
        else
        {
            // placable tile인 경우 PlaceTile 자식 제거
            RemovePlaceTileChild(tile);
        }
        
        // 타일의 배치 가능 상태 즉시 업데이트
        tile.ResetHighlight();
    }

    // ▼▼ [추가] 빈 placed/placable 타일 찾기 메서드 ▼▼
    private Tile FindEmptyPlacedOrPlacableTile(bool isRegion2)
    {
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        List<Tile> emptyTiles = new List<Tile>();
        
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            if (tile.isRegion2 != isRegion2) continue;
            
            // placed 또는 placable 타일인지 확인
            if (tile.IsPlaceTile() || tile.IsPlaced2() || tile.IsPlacable() || tile.IsPlacable2())
            {
                // 타일이 비어있는지 확인
                bool isEmpty = true;
                
                if (tile.IsPlaceTile() || tile.IsPlaced2())
                {
                    // placed tile의 경우
                    Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
                    foreach (var c in allChars)
                    {
                        if (c != null && c.currentTile == tile)
                        {
                            isEmpty = false;
                            break;
                        }
                    }
                }
                else
                {
                    // placable tile의 경우
                    isEmpty = !CheckAnyCharacterHasCurrentTile(tile);
                }
                
                if (isEmpty)
                {
                    emptyTiles.Add(tile);
                }
            }
        }
        
        // 랜덤하게 하나 선택
        if (emptyTiles.Count > 0)
        {
            return emptyTiles[Random.Range(0, emptyTiles.Count)];
        }
        
        return null;
    }
    
    // ▼▼ [수정] 빈 walkable 타일 찾기 메서드 - 랜덤 루트 선택 ▼▼
    private Tile FindEmptyWalkableTile(bool isRegion2)
    {
        // 먼저 랜덤하게 루트를 선택
        RouteType selectedRoute = GetRandomRoute();
        Debug.Log($"[PlacementManager] placed/placable 타일이 꽉 찼으므로 {selectedRoute} 루트의 walkable 타일로 배치");
        
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        List<Tile> leftTiles = new List<Tile>();
        List<Tile> centerTiles = new List<Tile>();
        List<Tile> rightTiles = new List<Tile>();
        List<Tile> generalWalkableTiles = new List<Tile>();
        
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            if (tile.isRegion2 != isRegion2) continue;
            
            // 루트별로 타일 분류
            if (isRegion2)
            {
                if (tile.IsWalkable2Left())
                {
                    leftTiles.Add(tile);
                }
                else if (tile.IsWalkable2Center())
                {
                    centerTiles.Add(tile);
                }
                else if (tile.IsWalkable2Right())
                {
                    rightTiles.Add(tile);
                }
                else if (tile.IsWalkable2())
                {
                    generalWalkableTiles.Add(tile);
                }
            }
            else
            {
                if (tile.IsWalkableLeft())
                {
                    leftTiles.Add(tile);
                }
                else if (tile.IsWalkableCenter())
                {
                    centerTiles.Add(tile);
                }
                else if (tile.IsWalkableRight())
                {
                    rightTiles.Add(tile);
                }
                else if (tile.IsWalkable())
                {
                    generalWalkableTiles.Add(tile);
                }
            }
        }
        
        // 선택된 루트에 해당하는 타일 반환
        List<Tile> targetTiles = null;
        switch (selectedRoute)
        {
            case RouteType.Left:
                targetTiles = leftTiles;
                break;
            case RouteType.Center:
                targetTiles = centerTiles;
                break;
            case RouteType.Right:
                targetTiles = rightTiles;
                break;
            case RouteType.Default:
                targetTiles = centerTiles; // Default는 Center 사용
                break;
        }
        
        // 선택된 루트에 타일이 있으면 랜덤 선택
        if (targetTiles != null && targetTiles.Count > 0)
        {
            Tile selectedTile = targetTiles[Random.Range(0, targetTiles.Count)];
            Debug.Log($"[PlacementManager] {selectedRoute} 루트의 타일 {selectedTile.name} 선택");
            return selectedTile;
        }
        
        // 선택된 루트에 타일이 없으면 일반 walkable 타일 사용
        if (generalWalkableTiles.Count > 0)
        {
            Tile selectedTile = generalWalkableTiles[Random.Range(0, generalWalkableTiles.Count)];
            Debug.Log($"[PlacementManager] {selectedRoute} 루트 타일이 없어 일반 walkable 타일 {selectedTile.name} 선택");
            return selectedTile;
        }
        
        // 모든 루트에서 타일 찾기 (fallback)
        List<Tile> allWalkableTiles = new List<Tile>();
        allWalkableTiles.AddRange(leftTiles);
        allWalkableTiles.AddRange(centerTiles);
        allWalkableTiles.AddRange(rightTiles);
        allWalkableTiles.AddRange(generalWalkableTiles);
        
        if (allWalkableTiles.Count > 0)
        {
            Tile selectedTile = allWalkableTiles[Random.Range(0, allWalkableTiles.Count)];
            Debug.Log($"[PlacementManager] fallback으로 타일 {selectedTile.name} 선택");
            return selectedTile;
        }
        
        Debug.LogWarning($"[PlacementManager] 지역{(isRegion2 ? 2 : 1)}에 walkable 타일이 없습니다!");
        return null;
    }

    /// <summary>
    /// ▼▼ [추가] 버튼 클릭으로 자동 배치 (placed/placable 꽉 차면 walkable로) ▼▼
    /// </summary>
    public void OnClickAutoPlace()
    {
        if (currentCharacterIndex < 0)
        {
            Debug.LogWarning("[PlacementManager] 캐릭터를 먼저 선택하세요!");
            return;
        }
        
        // 지역1(호스트 기준)의 빈 타일 찾기
        bool targetRegion2 = !isHost;
        
        // 1. 먼저 빈 placed/placable 타일 찾기
        Tile targetTile = FindEmptyPlacedOrPlacableTile(targetRegion2);
        
        // 2. placed/placable이 모두 꽉 찼으면 walkable 타일 찾기
        if (targetTile == null)
        {
            Debug.Log("[PlacementManager] placed/placable 타일이 모두 꽉 찼습니다. walkable 타일로 전환합니다.");
            targetTile = FindEmptyWalkableTile(targetRegion2);
        }
        
        // 3. 찾은 타일에 배치
        if (targetTile != null)
        {
            PlaceCharacterOnTile(targetTile);
        }
        else
        {
            Debug.LogWarning("[PlacementManager] 배치 가능한 타일이 없습니다!");
        }
    }

    // ======================== [추가] 3개 루트 시스템 헬퍼 메서드들 ========================
    /// <summary>
    /// 타일의 위치에 따라 적절한 루트를 결정합니다.
    /// </summary>
    private RouteType DetermineRouteFromTile(Tile tile, WaveSpawner spawner)
    {
        if (tile == null || spawner == null) return RouteType.Default;
        
        // ▼▼ [추가] placable/placed 타일인 경우 Default 루트 사용 (타워용)
        if (tile.IsPlacable() || tile.IsPlaceTile())
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 타워용 타일이므로 Default 루트 선택");
            return RouteType.Default;
        }
        
        // ▼▼ [추가] 새로운 타일 타입별 루트 직접 결정
        if (tile.IsWalkableLeft())
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 WalkableLeft 타일이므로 Left 루트 선택");
            return RouteType.Left;
        }
        if (tile.IsWalkableCenter())
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 WalkableCenter 타일이므로 Center 루트 선택");
            return RouteType.Center;
        }
        if (tile.IsWalkableRight())
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 WalkableRight 타일이므로 Right 루트 선택");
            return RouteType.Right;
        }
        
        float tileX = tile.transform.position.x;
        
        // ▼▼ [수정] 각 루트의 중심점을 더 정확하게 계산
        float leftMinX = float.MaxValue, leftMaxX = float.MinValue;
        float centerMinX = float.MaxValue, centerMaxX = float.MinValue;
        float rightMinX = float.MaxValue, rightMaxX = float.MinValue;
        
        // 좌측 루트의 X 범위
        if (spawner.walkableLeft != null && spawner.walkableLeft.Length > 0)
        {
            foreach (var waypoint in spawner.walkableLeft)
            {
                if (waypoint != null)
                {
                    float x = waypoint.position.x;
                    if (x < leftMinX) leftMinX = x;
                    if (x > leftMaxX) leftMaxX = x;
                }
            }
        }
        
        // 중앙 루트의 X 범위
        if (spawner.walkableCenter != null && spawner.walkableCenter.Length > 0)
        {
            foreach (var waypoint in spawner.walkableCenter)
            {
                if (waypoint != null)
                {
                    float x = waypoint.position.x;
                    if (x < centerMinX) centerMinX = x;
                    if (x > centerMaxX) centerMaxX = x;
                }
            }
        }
        
        // 우측 루트의 X 범위
        if (spawner.walkableRight != null && spawner.walkableRight.Length > 0)
        {
            foreach (var waypoint in spawner.walkableRight)
            {
                if (waypoint != null)
                {
                    float x = waypoint.position.x;
                    if (x < rightMinX) rightMinX = x;
                    if (x > rightMaxX) rightMaxX = x;
                }
            }
        }
        
        // 각 루트의 중심점 계산
        float leftCenterX = (leftMinX + leftMaxX) / 2;
        float centerCenterX = (centerMinX + centerMaxX) / 2;
        float rightCenterX = (rightMinX + rightMaxX) / 2;
        
        // 타일이 어느 범위에 속하는지 확인
        if (leftMinX != float.MaxValue && tileX >= leftMinX && tileX <= leftMaxX)
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name} (X: {tileX})은 좌측 루트 범위에 있음");
            return RouteType.Left;
        }
        else if (rightMinX != float.MaxValue && tileX >= rightMinX && tileX <= rightMaxX)
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name} (X: {tileX})은 우측 루트 범위에 있음");
            return RouteType.Right;
        }
        else if (centerMinX != float.MaxValue && tileX >= centerMinX && tileX <= centerMaxX)
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name} (X: {tileX})은 중앙 루트 범위에 있음");
            return RouteType.Center;
        }
        
        // 범위 밖이면 가장 가까운 중심점으로
        float distToLeft = (leftCenterX != float.MaxValue) ? Mathf.Abs(tileX - leftCenterX) : float.MaxValue;
        float distToCenter = (centerCenterX != float.MaxValue) ? Mathf.Abs(tileX - centerCenterX) : float.MaxValue;
        float distToRight = (rightCenterX != float.MaxValue) ? Mathf.Abs(tileX - rightCenterX) : float.MaxValue;
        
        Debug.Log($"[PlacementManager] 타일 {tile.name} (X: {tileX}) - 좌측 거리: {distToLeft}, 중앙 거리: {distToCenter}, 우측 거리: {distToRight}");
        
        if (distToLeft < distToCenter && distToLeft < distToRight)
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 좌측 루트 선택 (가장 가까움)");
            return RouteType.Left;
        }
        else if (distToRight < distToCenter && distToRight < distToLeft)
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 우측 루트 선택 (가장 가까움)");
            return RouteType.Right;
        }
        else
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 중앙 루트 선택 (기본값)");
            return RouteType.Default; // ▼▼ [수정] Center → Default로 변경
        }
    }
    
    /// <summary>
    /// 타일의 위치에 따라 적절한 루트를 결정합니다. (지역2용)
    /// </summary>
    private RouteType DetermineRouteFromTile(Tile tile, WaveSpawnerRegion2 spawner)
    {
        if (tile == null || spawner == null) return RouteType.Default;
        
        // ▼▼ [추가] placable2/placed2 타일인 경우 Default 루트 사용 (타워용)
        if (tile.IsPlacable2() || tile.IsPlaced2())
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 타워용 타일이므로 Default 루트 선택");
            return RouteType.Default;
        }
        
        // ▼▼ [추가] 새로운 타일 타입별 루트 직접 결정
        if (tile.IsWalkable2Left())
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 Walkable2Left 타일이므로 Left 루트 선택");
            return RouteType.Left;
        }
        if (tile.IsWalkable2Center())
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 Walkable2Center 타일이므로 Center 루트 선택");
            return RouteType.Center;
        }
        if (tile.IsWalkable2Right())
        {
            Debug.Log($"[PlacementManager] 타일 {tile.name}은 Walkable2Right 타일이므로 Right 루트 선택");
            return RouteType.Right;
        }
        
        float tileX = tile.transform.position.x;
        
        // 좌측 루트의 X 범위 확인
        if (spawner.walkableLeft2 != null && spawner.walkableLeft2.Length > 0)
        {
            float leftAvgX = 0f;
            foreach (var waypoint in spawner.walkableLeft2)
            {
                if (waypoint != null) leftAvgX += waypoint.position.x;
            }
            leftAvgX /= spawner.walkableLeft2.Length;
            
            // 중앙 루트의 X 평균
            float centerAvgX = 0f;
            if (spawner.walkableCenter2 != null && spawner.walkableCenter2.Length > 0)
            {
                foreach (var waypoint in spawner.walkableCenter2)
                {
                    if (waypoint != null) centerAvgX += waypoint.position.x;
                }
                centerAvgX /= spawner.walkableCenter2.Length;
            }
            
            // 우측 루트의 X 평균
            float rightAvgX = 0f;
            if (spawner.walkableRight2 != null && spawner.walkableRight2.Length > 0)
            {
                foreach (var waypoint in spawner.walkableRight2)
                {
                    if (waypoint != null) rightAvgX += waypoint.position.x;
                }
                rightAvgX /= spawner.walkableRight2.Length;
            }
            
            // 타일이 어느 루트에 가장 가까운지 판단
            float distToLeft = Mathf.Abs(tileX - leftAvgX);
            float distToCenter = Mathf.Abs(tileX - centerAvgX);
            float distToRight = Mathf.Abs(tileX - rightAvgX);
            
            if (distToLeft <= distToCenter && distToLeft <= distToRight)
            {
                return RouteType.Left;
            }
            else if (distToRight <= distToCenter && distToRight <= distToLeft)
            {
                return RouteType.Right;
            }
        }
        
        return RouteType.Default; // ▼▼ [수정] Center → Default로 변경
    }
    
    /// <summary>
    /// 랜덤하게 루트(좌/중/우)를 선택합니다.
    /// </summary>
    private RouteType GetRandomRoute()
    {
        int randomValue = Random.Range(0, 3);
        return (RouteType)randomValue;
    }

    /// <summary>
    /// WaveSpawner에서 선택된 루트에 대한 웨이포인트 배열을 반환합니다.
    /// 캐릭터는 선택된 루트만 사용합니다.
    /// </summary>
    private Transform[] GetWaypointsForRoute(WaveSpawner spawner, RouteType route)
    {
        Transform[] waypoints = null;
        
        switch (route)
        {
            case RouteType.Default:
                waypoints = spawner.walkableCenter; // Default는 Center 루트 사용
                break;
            case RouteType.Left:
                waypoints = spawner.walkableLeft;
                break;
            case RouteType.Center:
                waypoints = spawner.walkableCenter;
                break;
            case RouteType.Right:
                waypoints = spawner.walkableRight;
                break;
            default:
                waypoints = spawner.walkableCenter;
                break;
        }
        
        // ▼▼ [추가] 웨이포인트 유효성 검사 및 정리 ▼▼
        if (waypoints != null && waypoints.Length > 0)
        {
            List<Transform> validWaypoints = new List<Transform>();
            
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    validWaypoints.Add(waypoints[i]);
                }
                else
                {
                    Debug.LogWarning($"[PlacementManager] {route} 루트의 웨이포인트[{i}]가 null입니다!");
                }
            }
            
            if (validWaypoints.Count > 0)
            {
                // ▼▼ [추가] 웨이포인트 간 거리 검사 ▼▼
                for (int i = 0; i < validWaypoints.Count - 1; i++)
                {
                    float distance = Vector2.Distance(validWaypoints[i].position, validWaypoints[i + 1].position);
                    if (distance > 15f) // UI 좌표계에서 너무 먼 거리
                    {
                        Debug.LogWarning($"[PlacementManager] {route} 루트 웨이포인트[{i}]→[{i+1}] 거리가 너무 멉니다: {distance}");
                    }
                }
                
                Debug.Log($"[PlacementManager] {route} 루트 웨이포인트 검증 완료: {validWaypoints.Count}개 (원본: {waypoints.Length}개)");
                return validWaypoints.ToArray();
            }
            else
            {
                Debug.LogError($"[PlacementManager] {route} 루트에 유효한 웨이포인트가 없습니다!");
                return null;
            }
        }
        
        Debug.LogWarning($"[PlacementManager] {route} 루트의 웨이포인트 배열이 null이거나 비어있습니다!");
        return null;
    }
    
    /// <summary>
    /// WaveSpawner의 모든 walkable 타일 웨이포인트를 하나의 배열로 반환
    /// </summary>
    private Transform[] GetAllWalkableWaypointsFromSpawner(WaveSpawner spawner)
    {
        List<Transform> allWaypoints = new List<Transform>();
        
        // 좌측 경로 추가
        if (spawner.walkableLeft != null)
        {
            allWaypoints.AddRange(spawner.walkableLeft);
        }
        
        // 중앙 경로 추가
        if (spawner.walkableCenter != null)
        {
            allWaypoints.AddRange(spawner.walkableCenter);
        }
        
        // 우측 경로 추가
        if (spawner.walkableRight != null)
        {
            allWaypoints.AddRange(spawner.walkableRight);
        }
        
        // 중복 제거 및 거리 기준 정렬
        List<Transform> uniqueWaypoints = new List<Transform>();
        foreach (var waypoint in allWaypoints)
        {
            if (waypoint != null && !uniqueWaypoints.Contains(waypoint))
            {
                uniqueWaypoints.Add(waypoint);
            }
        }
        
        // Y 좌표 기준으로 정렬 (아래에서 위로)
        uniqueWaypoints.Sort((a, b) => a.position.y.CompareTo(b.position.y));
        
        return uniqueWaypoints.ToArray();
    }

    /// <summary>
    /// WaveSpawnerRegion2에서 선택된 루트에 대한 웨이포인트 배열을 반환합니다.
    /// 캐릭터는 선택된 루트만 사용합니다.
    /// </summary>
    private Transform[] GetWaypointsForRoute(WaveSpawnerRegion2 spawner, RouteType route)
    {
        Transform[] waypoints = null;
        
        switch (route)
        {
            case RouteType.Default:
                waypoints = spawner.walkableCenter2; // Default는 Center 루트 사용
                break;
            case RouteType.Left:
                waypoints = spawner.walkableLeft2;
                break;
            case RouteType.Center:
                waypoints = spawner.walkableCenter2;
                break;
            case RouteType.Right:
                waypoints = spawner.walkableRight2;
                break;
            default:
                waypoints = spawner.walkableCenter2;
                break;
        }
        
        // ▼▼ [추가] 웨이포인트 유효성 검사 및 정리 ▼▼
        if (waypoints != null && waypoints.Length > 0)
        {
            List<Transform> validWaypoints = new List<Transform>();
            
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    validWaypoints.Add(waypoints[i]);
                }
                else
                {
                    Debug.LogWarning($"[PlacementManager] 지역2 {route} 루트의 웨이포인트[{i}]가 null입니다!");
                }
            }
            
            if (validWaypoints.Count > 0)
            {
                // ▼▼ [추가] 웨이포인트 간 거리 검사 ▼▼
                for (int i = 0; i < validWaypoints.Count - 1; i++)
                {
                    float distance = Vector2.Distance(validWaypoints[i].position, validWaypoints[i + 1].position);
                    if (distance > 15f) // UI 좌표계에서 너무 먼 거리
                    {
                        Debug.LogWarning($"[PlacementManager] 지역2 {route} 루트 웨이포인트[{i}]→[{i+1}] 거리가 너무 멉니다: {distance}");
                    }
                }
                
                Debug.Log($"[PlacementManager] 지역2 {route} 루트 웨이포인트 검증 완료: {validWaypoints.Count}개 (원본: {waypoints.Length}개)");
                return validWaypoints.ToArray();
            }
            else
            {
                Debug.LogError($"[PlacementManager] 지역2 {route} 루트에 유효한 웨이포인트가 없습니다!");
                return null;
            }
        }
        
        Debug.LogWarning($"[PlacementManager] 지역2 {route} 루트의 웨이포인트 배열이 null이거나 비어있습니다!");
        return null;
    }
    
    /// <summary>
    /// WaveSpawnerRegion2의 모든 walkable2 타일 웨이포인트를 하나의 배열로 반환
    /// </summary>
    private Transform[] GetAllWalkableWaypointsFromSpawner(WaveSpawnerRegion2 spawner)
    {
        List<Transform> allWaypoints = new List<Transform>();
        
        // 좌측 경로 추가
        if (spawner.walkableLeft2 != null)
        {
            allWaypoints.AddRange(spawner.walkableLeft2);
        }
        
        // 중앙 경로 추가
        if (spawner.walkableCenter2 != null)
        {
            allWaypoints.AddRange(spawner.walkableCenter2);
        }
        
        // 우측 경로 추가
        if (spawner.walkableRight2 != null)
        {
            allWaypoints.AddRange(spawner.walkableRight2);
        }
        
        // 중복 제거 및 거리 기준 정렬
        List<Transform> uniqueWaypoints = new List<Transform>();
        foreach (var waypoint in allWaypoints)
        {
            if (waypoint != null && !uniqueWaypoints.Contains(waypoint))
            {
                uniqueWaypoints.Add(waypoint);
            }
        }
        
        // Y 좌표 기준으로 정렬 (위에서 아래로 - 지역2는 반대)
        uniqueWaypoints.Sort((a, b) => b.position.y.CompareTo(a.position.y));
        
        return uniqueWaypoints.ToArray();
    }

    /// <summary>
    /// WaveSpawner에서 선택된 루트에 대한 스폰 위치를 반환합니다.
    /// </summary>
    private Vector3 GetSpawnPositionForRoute(WaveSpawner spawner, RouteType route)
    {
        Transform spawnPoint = null;
        
        switch (route)
        {
            case RouteType.Default:
                spawnPoint = spawner.centerSpawnPoint; // Default는 Center 스폰 포인트 사용
                break;
            case RouteType.Left:
                spawnPoint = spawner.leftSpawnPoint;
                break;
            case RouteType.Center:
                spawnPoint = spawner.centerSpawnPoint;
                break;
            case RouteType.Right:
                spawnPoint = spawner.rightSpawnPoint;
                break;
        }
        
        // 스폰 포인트가 설정되어 있으면 그 위치 사용
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        
        // 스폰 포인트가 없으면 해당 루트의 첫 번째 웨이포인트 위치 사용
        Transform[] waypoints = GetWaypointsForRoute(spawner, route);
        if (waypoints != null && waypoints.Length > 0)
        {
            return waypoints[0].position;
        }
        
        return Vector3.zero;
    }

    /// <summary>
    /// WaveSpawnerRegion2에서 선택된 루트에 대한 스폰 위치를 반환합니다.
    /// </summary>
    private Vector3 GetSpawnPositionForRoute(WaveSpawnerRegion2 spawner, RouteType route)
    {
        Transform spawnPoint = null;
        
        switch (route)
        {
            case RouteType.Default:
                spawnPoint = spawner.centerSpawnPoint2; // Default는 Center 스폰 포인트 사용
                break;
            case RouteType.Left:
                spawnPoint = spawner.leftSpawnPoint2;
                break;
            case RouteType.Center:
                spawnPoint = spawner.centerSpawnPoint2;
                break;
            case RouteType.Right:
                spawnPoint = spawner.rightSpawnPoint2;
                break;
        }
        
        // 스폰 포인트가 설정되어 있으면 그 위치 사용
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        
        // 스폰 포인트가 없으면 해당 루트의 첫 번째 웨이포인트 위치 사용
        Transform[] waypoints = GetWaypointsForRoute(spawner, route);
        if (waypoints != null && waypoints.Length > 0)
        {
            return waypoints[0].position;
        }
        
        return Vector3.zero;
    }

    /// <summary>
    /// 루트가 선택되었을 때 호출되는 메서드
    /// </summary>
    private void OnRouteSelected(Character character, Tile tile, RouteType selectedRoute, WaveSpawner spawner)
    {
        Transform[] waypoints = GetWaypointsForRoute(spawner, selectedRoute);
        
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"[PlacementManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
            Destroy(character.gameObject);
            return;
        }
        
        Vector3 spawnPos = GetSpawnPositionForRoute(spawner, selectedRoute);
        
        // 캐릭터를 실제 위치로 이동
        RectTransform allyRect = character.GetComponent<RectTransform>();
        if (allyRect != null)
        {
            Vector2 localPos = ourMonsterPanel.InverseTransformPoint(spawnPos);
            allyRect.anchoredPosition = localPos;
            allyRect.localRotation = Quaternion.identity;
        }
        else
        {
            character.transform.position = spawnPos;
            character.transform.localRotation = Quaternion.identity;
        }
        
        // 웨이포인트 설정
        character.currentWaypointIndex = 0;
        character.pathWaypoints = waypoints;
        character.maxWaypointIndex = waypoints.Length - 1;
        character.selectedRoute = selectedRoute;
        
        Debug.Log($"[PlacementManager] 캐릭터 {character.characterName}에게 {selectedRoute} 루트 설정 완료. 웨이포인트 개수: {waypoints.Length}");
        
        // 타일을 사용된 것으로 표시 - Tile 클래스에 메서드가 없으므로 제거
        // tile.SetAsUsed();
    }
    
    /// <summary>
    /// 루트 선택이 취소되었을 때 호출되는 메서드
    /// </summary>
    private void OnRouteCancelled(Character character)
    {
        Debug.Log($"[PlacementManager] 루트 선택 취소 - 캐릭터 제거: {character.characterName}");
        // RouteSelectionUI에서 이미 캐릭터를 제거하고 미네랄을 환불함
    }

    /// <summary>
    /// 지역2에서 루트가 선택되었을 때 호출되는 메서드
    /// </summary>
    private void OnRouteSelectedRegion2(Character character, Tile tile, RouteType selectedRoute, WaveSpawnerRegion2 spawner2)
    {
        Transform[] waypoints = GetWaypointsForRoute(spawner2, selectedRoute);
        
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"[PlacementManager] {selectedRoute} 루트의 웨이포인트가 없습니다.");
            Destroy(character.gameObject);
            return;
        }
        
        Vector3 spawnPos = GetSpawnPositionForRoute(spawner2, selectedRoute);
        
        // 캐릭터를 실제 위치로 이동
        RectTransform enemyRect = character.GetComponent<RectTransform>();
        if (enemyRect != null)
        {
            Vector2 localPos = opponentOurMonsterPanel.InverseTransformPoint(spawnPos);
            enemyRect.anchoredPosition = localPos;
            enemyRect.localRotation = Quaternion.identity;
        }
        else
        {
            character.transform.position = spawnPos;
            character.transform.localRotation = Quaternion.identity;
        }
        
        // 웨이포인트 설정
        character.currentWaypointIndex = 0;
        character.pathWaypoints = waypoints;
        character.maxWaypointIndex = waypoints.Length - 1;
        character.selectedRoute = selectedRoute;
        
        Debug.Log($"[PlacementManager] 캐릭터 {character.characterName}에게 {selectedRoute} 루트 설정 완료. 웨이포인트 개수: {waypoints.Length}");
        
        // 타일을 사용된 것으로 표시 - Tile 클래스에 메서드가 없으므로 제거
        // tile.SetAsUsed();
    }
}
        