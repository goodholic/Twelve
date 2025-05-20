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
        // UI 패널 디버그 로그 추가
        Debug.Log($"[PlacementManager] UI 패널 상태: opponentCharacterPanel={opponentCharacterPanel != null}, " +
                 $"opponentOurMonsterPanel={opponentOurMonsterPanel != null}, " +
                 $"opponentBulletPanel={opponentBulletPanel != null}");
        
        // 로그인 패널이 비활성화된 상태이므로 항상 호스트로 설정
        isHost = true;
        Debug.Log("[PlacementManager] 로그인 패널 비활성화 상태: 호스트 모드로 플레이합니다.");
        
        /* 기존 NetworkRunner 검색 로직 주석 처리
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            if (runner.GameMode == GameMode.Host)
            {
                isHost = true;
            }
            else if (runner.GameMode == GameMode.Client)
            {
                isHost = false;
            }
        }
        */
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

    // ---------------------------------------------------------------------------
    // (A) "클릭 방식" 배치 (tile.OnClickPlacableTile() 등에서 호출)
    // ---------------------------------------------------------------------------
    public void PlaceCharacterOnTile(Tile tile)
    {
        // 1) DB 검사
        if (characterDatabase == null
            || characterDatabase.currentRegisteredCharacters == null
            || characterDatabase.currentRegisteredCharacters.Length == 0)
        {
            Debug.LogWarning("[PlacementManager] characterDatabase가 비어있어 배치 불가");
            return;
        }

        // 2) 인덱스 검사
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

        // 지역1/지역2 + 호스트/클라이언트 구분
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

        // 타일 상태 체크
        if (!tile.CanPlaceCharacter())
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} => 배치 불가능한 상태");
            return;
        }

        // =================================
        //  walkable → 이동형(웨이브)
        //  placable → 건물형
        // =================================
        if (tile.IsWalkable())
        {
            // (area1) 이동형(몬스터)
            WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null && spawner.pathWaypoints != null && spawner.pathWaypoints.Length > 0 && ourMonsterPanel != null)
            {
                Vector3 spawnPos = spawner.pathWaypoints[0].position;
                GameObject allyObj = Instantiate(data.spawnPrefab, ourMonsterPanel);
                if (allyObj != null)
                {
                    // 위치
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

                    // 캐릭터 설정
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

                    // (선택) 캐릭터SelectUI
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
            // (area2) 이동형(몬스터)
            WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
            if (spawner2 != null && spawner2.topWaypointsForAI != null && spawner2.topWaypointsForAI.Length > 0 && opponentOurMonsterPanel != null)
            {
                Vector3 spawnPos = spawner2.topWaypointsForAI[0].position;
                GameObject allyObj = Instantiate(data.spawnPrefab, opponentOurMonsterPanel);
                if (allyObj != null)
                {
                    // 위치
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

                    // 캐릭터 설정
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

                    // (선택) 캐릭터SelectUI
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
            // 건물형 배치
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
                    // 건물형 => isCharAttack=false
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

                    if (tile.IsPlaced2() && opponentBulletPanel != null)
                    {
                        characterComp.opponentBulletPanel = opponentBulletPanel;
                    }
                    else
                    {
                        characterComp.SetBulletPanel(bulletPanel);
                    }
                    characterComp.areaIndex = tile.IsPlaced2() ? 2 : 1;
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
    public void SummonCharacterOnTile(int summonIndex, Tile tile, bool forceEnemyArea2 = false)
    {
        Debug.Log($"[PlacementManager] SummonCharacterOnTile: 인덱스={summonIndex}, tile={tile.name}, forceEnemyArea2={forceEnemyArea2}");

        // 1) 어떤 DB에서 불러올지 분기
        CharacterData data;
        if (forceEnemyArea2 && enemyDatabase != null && enemyDatabase.characters != null)
        {
            // enemyDatabase 참조
            if (summonIndex < 0 || summonIndex >= enemyDatabase.characters.Length)
            {
                Debug.LogWarning($"[PlacementManager] 잘못된 소환 인덱스({summonIndex}) => 실패");
                return;
            }
            data = enemyDatabase.characters[summonIndex];
            Debug.Log($"[PlacementManager] enemyDatabase에서 데이터 가져옴: {data.characterName}, spawnPrefab={data.spawnPrefab != null}");
        }
        else
        {
            // 기본 아군 DB
            if (characterDatabase == null || characterDatabase.currentRegisteredCharacters == null
                || characterDatabase.currentRegisteredCharacters.Length == 0)
            {
                Debug.LogWarning("[PlacementManager] characterDatabase가 비어있음 => 소환 불가");
                return;
            }
            if (summonIndex < 0 || summonIndex >= characterDatabase.currentRegisteredCharacters.Length)
            {
                Debug.LogWarning($"[PlacementManager] 잘못된 소환 인덱스({summonIndex}) => 소환 불가");
                return;
            }
            data = characterDatabase.currentRegisteredCharacters[summonIndex];
        }

        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"[PlacementManager] [{summonIndex}]번 캐릭터 spawnPrefab이 null => 소환 불가");
            return;
        }
        if (tile == null)
        {
            Debug.LogWarning("[PlacementManager] tile이 null => 소환 불가");
            return;
        }

        // == 지역2 소환 조건 ==
        bool tileIsArea2 = tile.IsWalkable2() || tile.IsPlacable2() || tile.IsPlaced2();
        if (!forceEnemyArea2)
        {
            if (tileIsArea2 && isHost)
            {
                Debug.LogWarning("[PlacementManager] 지역2에는 호스트 배치 불가");
                return;
            }
            if (!tileIsArea2 && !isHost)
            {
                Debug.LogWarning("[PlacementManager] 지역1에는 클라이언트/AI 배치 불가");
                return;
            }
        }

        // 미네랄 체크 및 차감
        MineralBar targetMineralBar = (forceEnemyArea2 || tileIsArea2) ? region2MineralBar : region1MineralBar;
        bool mineralsSpent = false;
        
        if (targetMineralBar != null)
        {
            mineralsSpent = targetMineralBar.TrySpend(data.cost);
            if (!mineralsSpent)
            {
                Debug.Log($"[PlacementManager] ({(tileIsArea2 ? "지역2" : "지역1")}) 미네랄 부족!(cost={data.cost})");
                return;
            }
        }
        else
        {
            Debug.LogWarning($"[PlacementManager] {(tileIsArea2 ? "region2MineralBar" : "region1MineralBar")}가 null => 소환 불가");
            return;
        }

        try
        {
            // 타일 배치 가능 체크
            if (!tile.CanPlaceCharacter())
            {
                // 미네랄을 이미 소모했지만 소환이 불가능한 경우 미네랄 환불
                if (mineralsSpent && targetMineralBar != null)
                {
                    targetMineralBar.RefundMinerals(data.cost);
                    Debug.Log($"[PlacementManager] 소환 불가로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                }
                
                Debug.LogWarning($"[PlacementManager] {tile.name} => 배치 불가(조건 불충족)");
                return;
            }

            // 소환 직전 추가 디버그
            Debug.Log($"[PlacementManager] 소환 직전 확인: 타일타입=[IsWalkable={tile.IsWalkable()}, IsWalkable2={tile.IsWalkable2()}, " +
                    $"IsPlacable={tile.IsPlacable()}, IsPlacable2={tile.IsPlacable2()}, " +
                    $"IsPlaceTile={tile.IsPlaceTile()}, IsPlaced2={tile.IsPlaced2()}]");

            // (A) 이동형 (walkable/walkable2)
            if (tile.IsWalkable())
            {
                WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
                if (spawner == null || spawner.pathWaypoints.Length == 0 || ourMonsterPanel == null)
                {
                    // 소환 실패 시 미네랄 환불
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                        Debug.Log($"[PlacementManager] Walkable 소환 불가로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                    }
                    
                    Debug.LogWarning("[PlacementManager] Walkable => 소환 실패: WaveSpawner/ourMonsterPanel 미설정");
                    return;
                }

                GameObject prefabToSpawn = data.spawnPrefab;
                Character cc = prefabToSpawn.GetComponent<Character>();
                if (cc == null)
                {
                    // 소환 실패 시 미네랄 환불
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                        Debug.Log($"[PlacementManager] 캐릭터 컴포넌트 없음으로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                    }
                    
                    Debug.LogError($"[PlacementManager] '{prefabToSpawn.name}' 프리팹에 Character 없음 => 실패");
                    return;
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
                }
            }
            else if (tile.IsWalkable2())
            {
                // === Summon 시에도 Walkable2 => opponentOurMonsterPanel로 소환 ===
                WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                if (spawner2 == null || spawner2.topWaypointsForAI.Length == 0 || opponentOurMonsterPanel == null)
                {
                    // 소환 실패 시 미네랄 환불
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                        Debug.Log($"[PlacementManager] Walkable2 소환 실패로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                    }
                    
                    Debug.LogWarning("[PlacementManager] Walkable2 => 소환 실패: " + 
                                    (spawner2 == null ? "WaveSpawnerRegion2 없음" : 
                                    spawner2.topWaypointsForAI.Length == 0 ? "topWaypointsForAI 없음" : 
                                    opponentOurMonsterPanel == null ? "opponentOurMonsterPanel 없음" : "알 수 없는 오류"));
                    return;
                }

                GameObject prefabToSpawn = data.spawnPrefab;
                Character cc = prefabToSpawn.GetComponent<Character>();
                if (cc == null)
                {
                    // 소환 실패 시 미네랄 환불
                    if (mineralsSpent && targetMineralBar != null)
                    {
                        targetMineralBar.RefundMinerals(data.cost);
                        Debug.Log($"[PlacementManager] 캐릭터 컴포넌트 없음으로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불");
                    }
                    
                    Debug.LogError($"[PlacementManager] '{prefabToSpawn.name}' 프리팹에 Character 없음 => 실패");
                    return;
                }

                Debug.Log($"[PlacementManager] Walkable2 소환 시도: prefab={prefabToSpawn.name}, topWaypoints={spawner2.topWaypointsForAI.Length}, opponentOurMonsterPanel={opponentOurMonsterPanel.name}");
                
                Vector3 spawnPos = spawner2.topWaypointsForAI[0].position;
                GameObject allyObj = Instantiate(prefabToSpawn, opponentOurMonsterPanel);
                if (allyObj != null)
                {
                    // 게임 오브젝트가 활성화되어 있는지 확인하고 강제로 활성화
                    allyObj.SetActive(true);
                    
                    RectTransform allyRect = allyObj.GetComponent<RectTransform>();
                    if (allyRect != null)
                    {
                        Vector2 localPos = opponentOurMonsterPanel.InverseTransformPoint(spawnPos);
                        allyRect.SetParent(opponentOurMonsterPanel, false);
                        allyRect.localRotation = Quaternion.identity;
                        allyRect.anchoredPosition = localPos;
                        
                        // UI 요소의 가시성 설정 강화
                        CanvasGroup canvasGroup = allyObj.GetComponent<CanvasGroup>();
                        if (canvasGroup != null)
                        {
                            canvasGroup.alpha = 1f;
                            canvasGroup.blocksRaycasts = true;
                            canvasGroup.interactable = true;
                        }
                        
                        // 이미지 컴포넌트가 있다면 색상 alpha 값 확인
                        Image[] images = allyObj.GetComponentsInChildren<Image>();
                        foreach (Image img in images)
                        {
                            Color c = img.color;
                            img.color = new Color(c.r, c.g, c.b, 1f);
                        }
                        
                        Debug.Log($"[PlacementManager] Walkable2 UI 위치 설정: localPos={localPos}, parent={opponentOurMonsterPanel.name}, scale={allyRect.localScale}");
                    }
                    else
                    {
                        allyObj.transform.SetParent(null);
                        allyObj.transform.position = spawnPos;
                        allyObj.transform.localRotation = Quaternion.identity;
                        Debug.Log($"[PlacementManager] Walkable2 월드 위치 설정: position={spawnPos}");
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
                }
                else
                {
                    Debug.LogError("[PlacementManager] Walkable2 => 캐릭터 생성 실패: Instantiate 결과가 null");
                }
            }
            // (B) placable/placable2
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
                        // 건물형 => isCharAttack=false
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
                }
            }
            // [추가] PlaceTile/Placed2
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

                        // 건물형 => isCharAttack=false
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
                }
            }
            else
            {
                Debug.LogWarning($"[PlacementManager] {tile.name} 상태를 처리할 수 없습니다 (드래그 소환).");
            }
        }
        catch (System.Exception ex)
        {
            // 예외 발생 시 미네랄 환불
            if (mineralsSpent && targetMineralBar != null)
            {
                targetMineralBar.RefundMinerals(data.cost);
                Debug.Log($"[PlacementManager] 예외 발생으로 {(tileIsArea2 ? "지역2" : "지역1")} 미네랄 {data.cost} 환불. 오류: {ex.Message}");
            }
            
            Debug.LogError($"[PlacementManager] 소환 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // ---------------------------------------------------------------------------
    // (C) "드래그된 캐릭터"를 새 타일로 옮기거나(이동) / 합성 시도
    // ---------------------------------------------------------------------------
    public void OnDropCharacter(Character movingChar, Tile newTile)
    {
        if (movingChar == null || newTile == null) return;

        // (1) 기존 타일 Occupied(PlaceTile) 해제
        Tile oldTile = movingChar.currentTile;
        if (oldTile != null)
        {
            RemovePlaceTileChild(oldTile);
        }

        // 새로운 타일에 이미 캐릭터가 있는지 검사
        bool occupantExists = CheckAnyCharacterHasCurrentTile(newTile);

        if (occupantExists)
        {
            // -------------------------------------------
            // (a) 타일에 이미 캐릭터 => "합성" 시도
            // -------------------------------------------
            bool success = TryMergeCharacter(movingChar, newTile);
            if (!success)
            {
                // 합성 실패 => 원래 위치로 복귀
                if (oldTile != null)
                {
                    MoveCharacterToTile(movingChar, oldTile);
                    CreatePlaceTileChild(oldTile);
                }
            }
        }
        else
        {
            // -------------------------------------------
            // (b) 캐릭터가 없음 => "이동 or 소환"
            // -------------------------------------------
            // 배치 가능 여부
            if (!newTile.CanPlaceCharacter())
            {
                // 불가능이면 revert
                Debug.LogWarning($"[PlacementManager] {newTile.name}는 배치 불가 -> 이동 취소");
                if (oldTile != null)
                {
                    MoveCharacterToTile(movingChar, oldTile);
                    CreatePlaceTileChild(oldTile);
                }
                return;
            }

            // [수정] walkable/ walkable2 로 드롭하면 "waypoint[0]에서 새로 생성" 로직 적용
            //        placable 등은 기존처럼 그냥 타일에 두는 식
            // -------------------------------------------
            if (newTile.IsWalkable())
            {
                // area1 웨이브처럼 waypoint[0]에서 다시 시작
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

                    // 웨이포인트 이동값 재설정
                    movingChar.currentTile = null; // 건물형X
                    movingChar.currentWaypointIndex = 0;
                    movingChar.maxWaypointIndex = 6;
                    movingChar.areaIndex = 1;
                    movingChar.pathWaypoints = spawner.pathWaypoints;
                    movingChar.isCharAttack = !movingChar.isHero;

                    Debug.Log($"[PlacementManager] 드래그로 (placable→walkable) 이동 => waypoint[0]에서 시작");
                }
                else
                {
                    // spawner가 없으면 되돌림
                    Debug.LogWarning("[PlacementManager] OnDrop => WaveSpawner 불가 => revert");
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
                // area2 웨이브처럼 waypoint[0]에서 다시 시작
                WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                if (spawner2 != null && spawner2.topWaypointsForAI != null && spawner2.topWaypointsForAI.Length > 0 && opponentOurMonsterPanel != null)
                {
                    Vector3 spawnPos2 = spawner2.topWaypointsForAI[0].position;

                    RectTransform charRect = movingChar.GetComponent<RectTransform>();
                    if (charRect != null)
                    {
                        Vector2 localPos = opponentOurMonsterPanel.InverseTransformPoint(spawnPos2);
                        charRect.SetParent(opponentOurMonsterPanel, false);
                        charRect.anchoredPosition = localPos;
                        charRect.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        movingChar.transform.SetParent(null);
                        movingChar.transform.position = spawnPos2;
                        movingChar.transform.localRotation = Quaternion.identity;
                    }

                    // 웨이포인트 이동값 재설정
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
                    Debug.LogWarning("[PlacementManager] OnDrop => WaveSpawnerRegion2 불가 => revert");
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
                // 건물형 이동
                MoveCharacterToTile(movingChar, newTile);
                movingChar.currentWaypointIndex = -1;
                movingChar.maxWaypointIndex = 6;
                movingChar.isCharAttack = false;
                // ↑ 기존 로직
            }

            // 새 타일 Occupy
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
                // 별+이름이 같아야 합성
                if (otherChar.star == movingChar.star && otherChar.characterName == movingChar.characterName)
                {
                    switch (otherChar.star)
                    {
                        case CharacterStar.OneStar:
                            // 1성 + 1성 => 2성으로 승급
                            otherChar.star = CharacterStar.TwoStar;
                            RandomizeAppearanceByStarAndRace(otherChar, CharacterStar.TwoStar);
                            UpgradeStats(otherChar);
                            Destroy(movingChar.gameObject);
                            Debug.Log("[PlacementManager] 합성 성공 (1성→2성)");
                            return true;

                        case CharacterStar.TwoStar:
                            // 2성 + 2성 => (2성 풀) 무작위 재탄생
                            otherChar.star = CharacterStar.TwoStar;
                            RandomizeAppearanceByStarAndRace(otherChar, CharacterStar.TwoStar);
                            UpgradeStats(otherChar);
                            Destroy(movingChar.gameObject);
                            Debug.Log("[PlacementManager] 합성 성공 => 2성 풀 중 임의 캐릭터 재탄생");
                            return true;

                        case CharacterStar.ThreeStar:
                            // 3성 + 3성 => (3성 풀) 무작위 재탄생
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

        // fallback
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
}
