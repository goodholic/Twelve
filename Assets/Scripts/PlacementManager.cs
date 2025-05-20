using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;
using System.Collections.Generic;
using UnityEngine.UI;

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
    }

    /// <summary>
    /// 버튼 클릭으로 캐릭터 인덱스 지정
    /// </summary>
    public void OnClickSelectUnit(int index)
    {
        currentCharacterIndex = index;
        Debug.Log($"[PlacementManager] 선택된 유닛 인덱스: {currentCharacterIndex}");
    }

    // =========================== [추가한 메서드] ===========================
    /// <summary>
    /// "캐릭터 제거 모드"를 토글하는 메서드
    /// UI의 "Remove" 버튼 등에 연결해 사용 가능.
    /// </summary>
    public void ToggleRemoveMode()
    {
        removeMode = !removeMode;
        Debug.Log($"[PlacementManager] removeMode = {removeMode}");
        
        // 제거 모드를 켰을 때만 랜덤 캐릭터 제거 시도
        if (removeMode)
        {
            RemoveRandomCharacter();
        }
    }
    
    /// <summary>
    /// 랜덤으로 placed 타일이나 placable 타일에 있는 캐릭터 중 하나를 선택하여 제거한다.
    /// </summary>
    private void RemoveRandomCharacter()
    {
        // 1. 모든 캐릭터와 그 타일을 찾기
        List<Character> placedCharacters = new List<Character>();
        Character[] allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        // placed 타일이나 placable 타일에 있는 캐릭터만 선택
        foreach (var character in allCharacters)
        {
            if (character != null && character.currentTile != null)
            {
                Tile tile = character.currentTile;
                if (tile.IsPlaceTile() || tile.IsPlaced2() || tile.IsPlacable() || tile.IsPlacable2())
                {
                    placedCharacters.Add(character);
                }
            }
        }
        
        // 제거할 캐릭터가 없는 경우
        if (placedCharacters.Count == 0)
        {
            Debug.Log("[PlacementManager] 제거할 캐릭터가 없습니다.");
            return;
        }
        
        // 2. 랜덤으로 하나 선택
        int randomIndex = Random.Range(0, placedCharacters.Count);
        Character targetCharacter = placedCharacters[randomIndex];
        Tile targetTile = targetCharacter.currentTile;
        
        if (targetTile == null)
        {
            Debug.LogWarning("[PlacementManager] 선택된 캐릭터의 타일이 null입니다.");
            return;
        }
        
        // 3. 선택된 캐릭터 정보 출력
        Debug.Log($"[PlacementManager] 랜덤 제거 대상: {targetCharacter.characterName} (별:{targetCharacter.star}, 타일:{targetTile.name})");
        
        // 4. 캐릭터 제거
        RemoveCharacterOnTile(targetTile);
        
        // 5. 제거 모드 즉시 해제
        removeMode = false;
    }
    // =====================================================================

    // ---------------------------------------------------------------------------
    // (A) "클릭 방식" 배치 (tile.OnClickPlacableTile() 등에서 호출)
    // ---------------------------------------------------------------------------
    public void PlaceCharacterOnTile(Tile tile)
    {
        // removeMode가 true면 제거 로직을 "타일" 측에서 처리하므로,
        // 여기선 배치 로직만 그대로 유지

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
            Debug.LogWarning($"[PlacementManager] 잘못된 인덱스({currentCharacterIndex}) => 배치 불가");
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

        bool isArea2 = (tile.IsWalkable2() || tile.IsPlacable2() || tile.IsPlaced2());
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

        if (tile.IsWalkable())
        {
            WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null && spawner.pathWaypoints != null && spawner.pathWaypoints.Length > 0 && ourMonsterPanel != null)
            {
                Vector3 spawnPos = spawner.pathWaypoints[0].position;
                GameObject allyObj = Instantiate(data.spawnPrefab, ourMonsterPanel);
                if (allyObj != null)
                {
                    RectTransform allyRect = allyObj.GetComponent<RectTransform>();
                    if (allyRect != null)
                    {
                        Vector2 localPos = ourMonsterPanel.InverseTransformPoint(spawnPos);
                        allyRect.anchoredPosition = localPos;
                        allyRect.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        allyObj.transform.position = spawnPos;
                        allyObj.transform.localRotation = Quaternion.identity;
                    }

                    Character allyCharacter = allyObj.GetComponent<Character>();
                    allyCharacter.currentTile = null;
                    allyCharacter.isHero = (currentCharacterIndex == 9);
                    allyCharacter.isCharAttack = !allyCharacter.isHero;

                    allyCharacter.currentWaypointIndex = 0;
                    allyCharacter.maxWaypointIndex = 6;
                    allyCharacter.pathWaypoints = spawner.pathWaypoints;
                    allyCharacter.areaIndex = 1;

                    allyCharacter.attackPower = data.attackPower;
                    allyCharacter.attackSpeed = data.attackSpeed;
                    allyCharacter.attackRange = data.attackRange;
                    allyCharacter.currentHP = data.maxHP;
                    allyCharacter.star = data.initialStar;
                    allyCharacter.ApplyStarVisual();
                    allyCharacter.moveSpeed = data.moveSpeed;

                    Debug.Log($"[PlacementManager] [{data.characterName}] (area1) 몬스터 소환 (cost={data.cost})");

                    var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                    if (selectUI != null)
                    {
                        selectUI.MarkCardAsUsed(currentCharacterIndex);
                    }

                    currentCharacterIndex = -1;
                }
            }
            else
            {
                Debug.LogWarning("[PlacementManager] WaveSpawner/ourMonsterPanel이 없어 소환 실패");
            }
        }
        else if (tile.IsWalkable2())
        {
            WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
            if (spawner2 != null && spawner2.topWaypointsForAI != null && spawner2.topWaypointsForAI.Length > 0 && opponentOurMonsterPanel != null)
            {
                Vector3 spawnPos = spawner2.topWaypointsForAI[0].position;
                GameObject allyObj = Instantiate(data.spawnPrefab, opponentOurMonsterPanel);
                if (allyObj != null)
                {
                    RectTransform allyRect = allyObj.GetComponent<RectTransform>();
                    if (allyRect != null)
                    {
                        Vector2 localPos = opponentOurMonsterPanel.InverseTransformPoint(spawnPos);
                        allyRect.localRotation = Quaternion.identity;
                        allyRect.anchoredPosition = localPos;
                    }
                    else
                    {
                        allyObj.transform.position = spawnPos;
                        allyObj.transform.localRotation = Quaternion.identity;
                    }

                    Character allyCharacter = allyObj.GetComponent<Character>();
                    allyCharacter.currentTile = null;
                    allyCharacter.isHero = (currentCharacterIndex == 9);
                    allyCharacter.isCharAttack = !allyCharacter.isHero;

                    allyCharacter.currentWaypointIndex = 0;
                    allyCharacter.maxWaypointIndex = 6;
                    allyCharacter.pathWaypoints = spawner2.topWaypointsForAI;
                    allyCharacter.areaIndex = 2;

                    allyCharacter.attackPower = data.attackPower;
                    allyCharacter.attackSpeed = data.attackSpeed;
                    allyCharacter.attackRange = data.attackRange;
                    allyCharacter.currentHP = data.maxHP;
                    allyCharacter.star = data.initialStar;
                    allyCharacter.ApplyStarVisual();
                    allyCharacter.moveSpeed = data.moveSpeed;

                    Debug.Log($"[PlacementManager] [{data.characterName}] (지역2) 몬스터 소환 (cost={data.cost})");

                    var selectUI = FindFirstObjectByType<CharacterSelectUI>();
                    if (selectUI != null)
                    {
                        selectUI.MarkCardAsUsed(currentCharacterIndex);
                    }

                    currentCharacterIndex = -1;
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
                currentCharacterIndex = -1;
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
                currentCharacterIndex = -1;
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

            bool tileIsArea2 = tile.IsWalkable2() || tile.IsPlacable2() || tile.IsPlaced2();
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

            if (tile.IsWalkable())
            {
                WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
                if (spawner == null || spawner.pathWaypoints.Length == 0 || ourMonsterPanel == null)
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

                Vector3 spawnPos = spawner.pathWaypoints[0].position;
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
                    allyChar.maxWaypointIndex = 6;
                    allyChar.areaIndex = 1;
                    allyChar.pathWaypoints = spawner.pathWaypoints;

                    allyChar.attackPower = data.attackPower;
                    allyChar.attackSpeed = data.attackSpeed;
                    allyChar.attackRange = data.attackRange;
                    allyChar.currentHP = data.maxHP;
                    allyChar.star = data.initialStar;
                    allyChar.ApplyStarVisual();
                    allyChar.moveSpeed = data.moveSpeed;

                    Debug.Log($"[PlacementManager] 드래그로 [{data.characterName}] (area1) 몬스터 소환 (cost={data.cost})");
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
            else if (tile.IsWalkable2())
            {
                WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                if (spawner2 == null || spawner2.topWaypointsForAI.Length == 0 || opponentOurMonsterPanel == null)
                {
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                    }
                    Debug.LogWarning("[PlacementManager] Walkable2 => 소환 실패");
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

                Vector3 spawnPos2 = spawner2.topWaypointsForAI[0].position;
                GameObject allyObj = Instantiate(prefabToSpawn, opponentOurMonsterPanel);
                if (allyObj != null)
                {
                    allyObj.SetActive(true);
                    RectTransform allyRect = allyObj.GetComponent<RectTransform>();
                    if (allyRect != null)
                    {
                        Vector2 localPos = opponentOurMonsterPanel.InverseTransformPoint(spawnPos2);
                        allyRect.SetParent(opponentOurMonsterPanel, false);
                        allyRect.localRotation = Quaternion.identity;
                        allyRect.anchoredPosition = localPos;
                    }
                    else
                    {
                        allyObj.transform.SetParent(null);
                        allyObj.transform.position = spawnPos2;
                        allyObj.transform.localRotation = Quaternion.identity;
                    }

                    Character allyCharacter = allyObj.GetComponent<Character>();
                    allyCharacter.currentTile = null;
                    allyCharacter.isHero = (summonIndex == 9);
                    allyCharacter.isCharAttack = !allyCharacter.isHero;

                    allyCharacter.currentWaypointIndex = 0;
                    allyCharacter.maxWaypointIndex = 6;
                    allyCharacter.areaIndex = 2;
                    allyCharacter.pathWaypoints = spawner2.topWaypointsForAI;

                    allyCharacter.attackPower = data.attackPower;
                    allyCharacter.attackSpeed = data.attackSpeed;
                    allyCharacter.attackRange = data.attackRange;
                    allyCharacter.currentHP = data.maxHP;
                    allyCharacter.star = data.initialStar;
                    allyCharacter.ApplyStarVisual();
                    allyCharacter.moveSpeed = data.moveSpeed;

                    Debug.Log($"[PlacementManager] 드래그로 [{data.characterName}] (지역2) 몬스터 소환 완료 (cost={data.cost})");
                    success = true;
                }
                else
                {
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                    }
                    Debug.LogError("[PlacementManager] Walkable2 => Instantiate 실패");
                    return false;
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
    // (C) "드래그된 캐릭터"를 새 타일로 옮기거나(이동) / 합성 시도
    // ---------------------------------------------------------------------------
    public void OnDropCharacter(Character movingChar, Tile newTile)
    {
        if (movingChar == null || newTile == null) return;

        Tile oldTile = movingChar.currentTile;
        if (oldTile != null)
        {
            RemovePlaceTileChild(oldTile);
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

            if (newTile.IsWalkable())
            {
                WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
                if (spawner != null && spawner.pathWaypoints != null && spawner.pathWaypoints.Length > 0 && ourMonsterPanel != null)
                {
                    Vector3 spawnPos = spawner.pathWaypoints[0].position;

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
                    movingChar.maxWaypointIndex = 6;
                    movingChar.areaIndex = 1;
                    movingChar.pathWaypoints = spawner.pathWaypoints;
                    movingChar.isCharAttack = !movingChar.isHero;

                    Debug.Log($"[PlacementManager] 드래그로 (placable→walkable) 이동 => waypoint[0]에서 시작");
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
            else if (newTile.IsWalkable2())
            {
                WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                if (spawner2 != null && spawner2.topWaypointsForAI != null && spawner2.topWaypointsForAI.Length > 0 && opponentOurMonsterPanel != null)
                {
                    Vector3 spawnPos2 = spawner2.topWaypointsForAI[0].position;

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
                    movingChar.maxWaypointIndex = 6;
                    movingChar.areaIndex = 2;
                    movingChar.pathWaypoints = spawner2.topWaypointsForAI;
                    movingChar.isCharAttack = !movingChar.isHero;

                    Debug.Log($"[PlacementManager] 드래그로 (placable→walkable2) 이동 => waypoint[0]에서 시작");
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
                movingChar.maxWaypointIndex = 6;
                movingChar.isCharAttack = false;
            }

            CreatePlaceTileChild(newTile);
            Debug.Log("[PlacementManager] 캐릭터가 새 타일로 이동(또는 웨이포인트 모드) 완료");
        }
    }

    private bool CheckAnyCharacterHasCurrentTile(Tile tile)
    {
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var c in allChars)
        {
            if (c != null && c.currentTile == tile)
            {
                return true;
            }
        }
        return false;
    }

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
                    switch (otherChar.star)
                    {
                        case CharacterStar.OneStar:
                            otherChar.star = CharacterStar.TwoStar;
                            RandomizeAppearanceByStarAndRace(otherChar, CharacterStar.TwoStar);
                            UpgradeStats(otherChar);
                            Destroy(movingChar.gameObject);
                            Debug.Log("[PlacementManager] 합성 성공 (1성→2성)");
                            return true;

                        case CharacterStar.TwoStar:
                            otherChar.star = CharacterStar.TwoStar;
                            RandomizeAppearanceByStarAndRace(otherChar, CharacterStar.TwoStar);
                            UpgradeStats(otherChar);
                            Destroy(movingChar.gameObject);
                            Debug.Log("[PlacementManager] 합성 성공 => 2성 풀 중 임의 캐릭터 재탄생");
                            return true;

                        case CharacterStar.ThreeStar:
                            otherChar.star = CharacterStar.ThreeStar;
                            RandomizeAppearanceByStarAndRace(otherChar, CharacterStar.ThreeStar);
                            UpgradeStats(otherChar);
                            Destroy(movingChar.gameObject);
                            Debug.Log("[PlacementManager] 합성 성공 => 3성 풀 중 임의 캐릭터 재탄생");
                            return true;

                        default:
                            Debug.Log("[PlacementManager] 알 수 없는 별 => 합성 불가");
                            return false;
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
                Debug.Log($"[PlacementManager] (area={ch.areaIndex}) {newStar} 변경 => {ch.characterName} (starMergeDB 사용)");
                return;
            }
        }

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

        bool isArea2 = tile.IsWalkable2() || tile.IsPlacable2() || tile.IsPlaced2();

        RectTransform charRect = character.GetComponent<RectTransform>();
        if (charRect != null)
        {
            RectTransform targetParent;
            if ((tile.IsWalkable() || tile.IsWalkable2()) && ourMonsterPanel != null)
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

    private void CreatePlaceTileChild(Tile tile)
    {
        Transform exist = tile.transform.Find("PlaceTile");
        Transform exist2 = tile.transform.Find("Placed2");
        if (exist == null && exist2 == null)
        {
            GameObject placeTileObj = new GameObject("PlaceTile");
            placeTileObj.transform.SetParent(tile.transform, false);
            placeTileObj.transform.localPosition = Vector3.zero;
        }
    }

    private void RemovePlaceTileChild(Tile tile)
    {
        Transform exist = tile.transform.Find("PlaceTile");
        if (exist != null)
        {
            Destroy(exist.gameObject);
        }
        Transform exist2 = tile.transform.Find("Placed2");
        if (exist2 != null)
        {
            Destroy(exist2.gameObject);
        }
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
        if (itemSuccess && itemInventoryManager != null && itemInventoryManager.GetOwnedItems().Count < 9)
        {
            // 아이템 인벤토리 매니저가 있으나, 현재 확인된 유효한 메서드가 없으므로 로그만 출력
            // 실제 구현 시 아이템 추가 로직 필요
            Debug.Log($"[PlacementManager] 별={occupant.star} 캐릭터 제거 -> 아이템 획득! (확률: {itemChance*100}%)");
            Debug.LogWarning("[PlacementManager] 아이템 인벤토리 매니저에 적절한 랜덤 아이템 추가 메서드 구현 필요");
        }
        else
        {
            Debug.Log($"[PlacementManager] 별={occupant.star} 캐릭터 제거 -> 아이템 획득 실패 (확률: {itemChance*100}%)");
        }

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
            Debug.Log($"[PlacementManager] area1 캐릭터 제거 => 미네랄 {finalRefund} 환급 (코스트 {cost}/2 * {ratio*100}%)");
        }
        else if (occupant.areaIndex == 2 && region2MineralBar != null)
        {
            region2MineralBar.RefundMinerals(finalRefund);
            Debug.Log($"[PlacementManager] area2 캐릭터 제거 => 미네랄 {finalRefund} 환급 (코스트 {cost}/2 * {ratio*100}%)");
        }

        // 4) 캐릭터 오브젝트 Destroy
        occupant.currentTile = null;
        Destroy(occupant.gameObject);

        // 5) 타일에서 "PlaceTile"/"Placed2" 자식 제거
        RemovePlaceTileChild(tile);

        Debug.Log($"[PlacementManager] {tile.name} 타일의 캐릭터 제거 완료 (Star={occupant.star})");
    }
    // =====================================================================
}
