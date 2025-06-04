using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    [Header("몬스터 관련 설정")]
    public GameObject monsterPrefab;
    public Transform monsterParent;
    
    [Header("몬스터 이동 경로 (3라인)")]
    [Tooltip("좌측 루트 몬스터 웨이포인트")]
    public Transform[] monsterWaypointsLeft;
    [Tooltip("중앙 루트 몬스터 웨이포인트")]
    public Transform[] monsterWaypointsCenter;
    [Tooltip("우측 루트 몬스터 웨이포인트")]
    public Transform[] monsterWaypointsRight;
    
    [Header("몬스터 루트별 스폰 위치")]
    [Tooltip("좌측 루트 몬스터 스폰 위치")]
    public Transform monsterLeftSpawnPoint;
    [Tooltip("중앙 루트 몬스터 스폰 위치")]
    public Transform monsterCenterSpawnPoint;
    [Tooltip("우측 루트 몬스터 스폰 위치")]
    public Transform monsterRightSpawnPoint;
    
    [Header("캐릭터 공격 루트 설정 (좌/중/우)")]
    [Tooltip("좌측 루트 웨이포인트")]
    public Transform[] walkableLeft;
    [Tooltip("중앙 루트 웨이포인트")]
    public Transform[] walkableCenter;
    [Tooltip("우측 루트 웨이포인트")]
    public Transform[] walkableRight;
    
    [Header("캐릭터 루트별 스폰 위치")]
    [Tooltip("좌측 루트 스폰 위치")]
    public Transform leftSpawnPoint;
    [Tooltip("중앙 루트 스폰 위치")]
    public Transform centerSpawnPoint;
    [Tooltip("우측 루트 스폰 위치")]
    public Transform rightSpawnPoint;

    [Header("중간성/최종성 참조")]
    [Tooltip("좌측 중간성(체력 500)")]
    public GameObject leftMiddleCastle;
    [Tooltip("중앙 중간성(체력 500)")]
    public GameObject centerMiddleCastle;
    [Tooltip("우측 중간성(체력 500)")]
    public GameObject rightMiddleCastle;
    [Tooltip("최종성(체력 1000)")]
    public GameObject finalCastle;

    [Header("웨이브/몬스터 설정")]
    public float timeBetweenWaves = 5f;
    public int monstersPerWave = 5;
    public float spawnInterval = 0.2f; // 동시 생성을 위해 간격 축소

    private int aliveMonsters = 0;
    public int currentWave = 0;
    private bool isSpawning = false;

    private bool autoStarted = false;

    [Header("아이템 패널(웨이브 보상용)")]
    [SerializeField] private GameObject itemPanel;

    [Header("Wave Count Text")]
    [SerializeField] private TextMeshProUGUI waveCountText;
    
    [Header("5웨이브 보상 시스템")]
    [SerializeField] private GameObject rewardSelectionPanel;
    [SerializeField] private Transform rewardButtonParent;
    [SerializeField] private StarMergeDatabaseObject starMergeDatabase;

    [Header("[수정추가] 챕터별 몬스터 교체 옵션")]
    [Tooltip("true면 1챕터~2챕터, 2챕터~3챕터... 등으로 넘어갈 때 5마리 웨이브 중 3번째만 '다음 챕터 몬스터'를 소환")]
    public bool useChapterMonsterLogic = true;

    [Tooltip("현재 챕터 (1이면 1챕터, 2면 2챕터 등)")]
    public int currentChapter = 1;

    [Tooltip("2챕터 몬스터 프리팹(1챕터일 때, 5마리 중 3번째만 이것 사용)")]
    public GameObject chapter2MonsterPrefab;

    [Tooltip("3챕터 몬스터 프리팹(2챕터일 때, 5마리 중 3번째만 이것 사용)")]
    public GameObject chapter3MonsterPrefab;

    [Header("[수정추가] 1~101 챕터 몬스터 프리팹 배열")]
    [Tooltip("예: index=0 => 1챕터 몬스터, index=1 => 2챕터 몬스터, ..., index=100 => 101챕터 몬스터")]
    public GameObject[] chapterMonsters = new GameObject[101];

    private void Start()
    {
        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }
        
        if (rewardSelectionPanel != null)
        {
            rewardSelectionPanel.SetActive(false);
        }

        // 중간성/최종성 체력 설정
        SetupCastleHealth();

        // 최초에 10초 딜레이 후 자동 웨이브 시작
        StartCoroutine(DelayFirstWaveRoutine(10f));
    }

    /// <summary>
    /// 중간성 체력 500, 최종성 체력 1000 설정
    /// </summary>
    private void SetupCastleHealth()
    {
        // 중간성 체력 설정
        if (leftMiddleCastle != null)
        {
            var middleCastle = leftMiddleCastle.GetComponent<MiddleCastle>();
            if (middleCastle != null)
            {
                middleCastle.maxHealth = 500;
                middleCastle.currentHealth = 500;
                Debug.Log($"[WaveSpawner] 좌측 중간성 체력 500으로 설정");
            }
        }
        
        if (centerMiddleCastle != null)
        {
            var middleCastle = centerMiddleCastle.GetComponent<MiddleCastle>();
            if (middleCastle != null)
            {
                middleCastle.maxHealth = 500;
                middleCastle.currentHealth = 500;
                Debug.Log($"[WaveSpawner] 중앙 중간성 체력 500으로 설정");
            }
        }
        
        if (rightMiddleCastle != null)
        {
            var middleCastle = rightMiddleCastle.GetComponent<MiddleCastle>();
            if (middleCastle != null)
            {
                middleCastle.maxHealth = 500;
                middleCastle.currentHealth = 500;
                Debug.Log($"[WaveSpawner] 우측 중간성 체력 500으로 설정");
            }
        }
        
        // 최종성 체력 설정
        if (finalCastle != null)
        {
            var finalCastleComp = finalCastle.GetComponent<FinalCastle>();
            if (finalCastleComp != null)
            {
                finalCastleComp.maxHealth = 1000;
                finalCastleComp.currentHealth = 1000;
                Debug.Log("[WaveSpawner] 최종성 체력 1000으로 설정");
            }
        }
    }

    private IEnumerator DelayFirstWaveRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartAutoWaveSpawn();
    }

    private void StartAutoWaveSpawn()
    {
        if (!autoStarted)
        {
            autoStarted = true;
            StartCoroutine(AutoWaveRoutine());
        }
    }

    private IEnumerator AutoWaveRoutine()
    {
        // 최대 200웨이브까지만
        while (currentWave < 200)
        {
            StartNextWave();

            // 현재 웨이브가 끝날 때까지 대기
            while (isSpawning)
            {
                yield return null;
            }
        }
        Debug.Log("[WaveSpawner] 200 wave 전부 완료!");
    }

    public void StartNextWave()
    {
        if (!isSpawning)
        {
            Debug.Log($"[WaveSpawner] StartNextWave() 호출 -> currentWave={currentWave+1}");
            StartCoroutine(SpawnWaveRoutine());
        }
        else
        {
            Debug.Log("[WaveSpawner] 이미 웨이브 스폰 중입니다.");
        }
    }

    private IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        currentWave++;

        if (waveCountText != null)
        {
            waveCountText.text = $"Wave : {currentWave}";
        }

        aliveMonsters = monstersPerWave;

        Debug.Log($"[WaveSpawner] Wave {currentWave} 시작 - 몬스터 {monstersPerWave}마리 소환 예정");

        // 5마리를 3라인에 분배
        List<int> spawnRoutes = GetSpawnDistribution(monstersPerWave);

        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster(i, spawnRoutes[i]);
            yield return new WaitForSeconds(spawnInterval);
        }

        while (aliveMonsters > 0)
        {
            yield return null;
        }

        OnWaveClear();

        yield return new WaitForSeconds(timeBetweenWaves);

        isSpawning = false;
        Debug.Log($"[WaveSpawner] Wave {currentWave} 종료 -> 다음 웨이브로 넘어감");
    }

    /// <summary>
    /// 5마리를 3라인에 분배하는 로직
    /// </summary>
    private List<int> GetSpawnDistribution(int monsterCount)
    {
        List<int> distribution = new List<int>();
        
        // 기본 분배: 2-1-2 (좌측2, 중앙1, 우측2)
        if (monsterCount == 5)
        {
            distribution.Add(0); // 좌측
            distribution.Add(0); // 좌측
            distribution.Add(1); // 중앙
            distribution.Add(2); // 우측
            distribution.Add(2); // 우측
        }
        else
        {
            // 다른 경우는 균등 분배
            for (int i = 0; i < monsterCount; i++)
            {
                distribution.Add(i % 3);
            }
        }
        
        return distribution;
    }

    /// <summary>
    /// 몬스터용 루트에 따른 웨이포인트 배열 반환
    /// </summary>
    private Transform[] GetMonsterWaypointsForRoute(int route)
    {
        Transform[] waypoints = null;
        
        switch (route)
        {
            case 0:
                waypoints = monsterWaypointsLeft;
                break;
            case 1:
                waypoints = monsterWaypointsCenter;
                break;
            case 2:
                waypoints = monsterWaypointsRight;
                break;
            default:
                waypoints = monsterWaypointsCenter;
                break;
        }
        
        if (waypoints != null && waypoints.Length > 0)
        {
            List<Transform> validWaypoints = new List<Transform>();
            
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    validWaypoints.Add(waypoints[i]);
                }
            }
            
            if (validWaypoints.Count > 0)
            {
                return validWaypoints.ToArray();
            }
        }
        
        Debug.LogWarning($"[WaveSpawner] 몬스터 {route} 루트의 웨이포인트가 없습니다!");
        return null;
    }

    /// <summary>
    /// 몬스터용 스폰 위치 반환
    /// </summary>
    private Vector3 GetMonsterSpawnPosition(int route)
    {
        Transform spawnPoint = null;
        
        switch (route)
        {
            case 0:
                spawnPoint = monsterLeftSpawnPoint;
                break;
            case 1:
                spawnPoint = monsterCenterSpawnPoint;
                break;
            case 2:
                spawnPoint = monsterRightSpawnPoint;
                break;
        }

        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        
        // 스폰 포인트가 없으면 해당 루트의 첫 웨이포인트 사용
        Transform[] waypoints = GetMonsterWaypointsForRoute(route);
        if (waypoints != null && waypoints.Length > 0)
        {
            return waypoints[0].position;
        }

        return transform.position;
    }

    private void SpawnMonster(int indexInWave, int routeIndex)
    {
        if (monsterPrefab == null || monsterParent == null)
        {
            Debug.LogError("[WaveSpawner] monsterPrefab/monsterParent가 설정되지 않아 몬스터 소환 불가");
            return;
        }

        // 선택된 루트의 웨이포인트 가져오기
        Transform[] selectedWaypoints = GetMonsterWaypointsForRoute(routeIndex);
        if (selectedWaypoints == null || selectedWaypoints.Length == 0)
        {
            Debug.LogError($"[WaveSpawner] 몬스터 {routeIndex} 루트의 웨이포인트가 없습니다!");
            return;
        }

        // 스폰 위치
        Vector3 spawnPos = GetMonsterSpawnPosition(routeIndex);

        // 챕터별 몬스터 선택 로직
        GameObject prefabToSpawn = monsterPrefab;
        int monsterChapter = currentChapter;

        if (useChapterMonsterLogic)
        {
            if (chapterMonsters != null && chapterMonsters.Length >= 101)
            {
                if (indexInWave == 2 && currentChapter < 101 && chapterMonsters[currentChapter] != null)
                {
                    prefabToSpawn = chapterMonsters[currentChapter];
                    monsterChapter = currentChapter + 1;
                }
                else
                {
                    int curIdx = currentChapter - 1;
                    if (curIdx >= 0 && curIdx < chapterMonsters.Length && chapterMonsters[curIdx] != null)
                    {
                        prefabToSpawn = chapterMonsters[curIdx];
                    }
                }
            }
            else
            {
                if (indexInWave == 2)
                {
                    if (currentChapter == 1 && chapter2MonsterPrefab != null)
                    {
                        prefabToSpawn = chapter2MonsterPrefab;
                        monsterChapter = 2;
                    }
                    else if (currentChapter == 2 && chapter3MonsterPrefab != null)
                    {
                        prefabToSpawn = chapter3MonsterPrefab;
                        monsterChapter = 3;
                    }
                    else if (currentChapter >= 3 && chapter3MonsterPrefab != null)
                    {
                        prefabToSpawn = chapter3MonsterPrefab;
                        monsterChapter = currentChapter + 1;
                    }
                }
            }
        }

        // 실제 스폰
        GameObject mObj = Instantiate(prefabToSpawn, monsterParent);
        mObj.SetActive(true);
        mObj.transform.position = spawnPos;

        Monster mComp = mObj.GetComponent<Monster>();
        if (mComp != null)
        {
            mComp.areaIndex = 1;
            mComp.pathWaypoints = selectedWaypoints;
            mComp.OnDeath += HandleMonsterDeath;
            mComp.currentChapter = monsterChapter;

            // 몬스터가 어느 루트인지 설정
            switch (routeIndex)
            {
                case 0:
                    mComp.SetMonsterRoute(RouteType.Left);
                    break;
                case 1:
                    mComp.SetMonsterRoute(RouteType.Center);
                    break;
                case 2:
                    mComp.SetMonsterRoute(RouteType.Right);
                    break;
            }

            Debug.Log($"[WaveSpawner] Spawned Monster on route {routeIndex}, indexInWave={indexInWave}, chapter={monsterChapter}, Wave={currentWave}");
        }
        else
        {
            Debug.LogWarning("[WaveSpawner] 스폰된 프리팹에 Monster 컴포넌트가 없음");
        }
    }

    /// <summary>
    /// 선택된 루트에 따른 웨이포인트 배열 반환 (캐릭터용)
    /// </summary>
    private Transform[] GetWaypointsForRoute(RouteType route)
    {
        Transform[] waypoints = null;
        
        switch (route)
        {
            case RouteType.Left:
                waypoints = walkableLeft;
                break;
            case RouteType.Center:
                waypoints = walkableCenter;
                break;
            case RouteType.Right:
                waypoints = walkableRight;
                break;
            default:
                waypoints = walkableCenter;
                break;
        }
        
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
                    Debug.LogWarning($"[WaveSpawner] {route} 루트의 웨이포인트[{i}]가 null입니다!");
                }
            }
            
            if (validWaypoints.Count > 0)
            {
                for (int i = 0; i < validWaypoints.Count - 1; i++)
                {
                    float distance = Vector2.Distance(validWaypoints[i].position, validWaypoints[i + 1].position);
                    if (distance > 12f)
                    {
                        Debug.LogWarning($"[WaveSpawner] {route} 루트 웨이포인트[{i}]→[{i+1}] 거리가 너무 멉니다: {distance:F2}");
                    }
                }
                
                Debug.Log($"[WaveSpawner] {route} 루트 웨이포인트 검증 완료: {validWaypoints.Count}개");
                return validWaypoints.ToArray();
            }
            else
            {
                Debug.LogError($"[WaveSpawner] {route} 루트에 유효한 웨이포인트가 없습니다!");
                return null;
            }
        }
        
        Debug.LogWarning($"[WaveSpawner] {route} 루트의 웨이포인트 배열이 null이거나 비어있습니다!");
        return null;
    }

    private void HandleMonsterDeath()
    {
        aliveMonsters--;
        Debug.Log($"[WaveSpawner] Monster 사망 -> aliveMonsters={aliveMonsters}");

        if (aliveMonsters < 0)
        {
            aliveMonsters = 0;
        }
    }

    private void OnWaveClear()
    {
        Debug.Log($"[WaveSpawner] Wave {currentWave} Clear!");

        // 5웨이브마다 보상
        if (currentWave % 5 == 0)
        {
            ShowRewardSelection();
        }
    }
    
    private void ShowRewardSelection()
    {
        if (rewardSelectionPanel == null)
        {
            Debug.LogWarning("[WaveSpawner] 보상 선택 패널이 설정되지 않았습니다!");
            return;
        }
        
        if (starMergeDatabase == null)
        {
            Debug.LogWarning("[WaveSpawner] StarMergeDatabase가 설정되지 않았습니다!");
            return;
        }
        
        // 기존 보상 버튼들 찾기 (이미 씬에 있는 버튼들)
        UnityEngine.UI.Button[] existingButtons = rewardButtonParent.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        
        // 랜덤 2성 캐릭터 3개 선택
        List<CharacterData> twoStarCharacters = new List<CharacterData>();
        
        // 모든 종족의 2성 캐릭터 수집
        RaceType[] allRaces = { RaceType.Human, RaceType.Orc, RaceType.Elf };
        foreach (var race in allRaces)
        {
            var twoStarData = starMergeDatabase.GetRandom2Star(race);
            if (twoStarData != null)
            {
                twoStarCharacters.Add(twoStarData);
            }
        }
        
        // 중복 없이 3개 선택
        List<CharacterData> selectedRewards = new List<CharacterData>();
        while (selectedRewards.Count < 3 && twoStarCharacters.Count > 0)
        {
            int randomIndex = Random.Range(0, twoStarCharacters.Count);
            selectedRewards.Add(twoStarCharacters[randomIndex]);
            twoStarCharacters.RemoveAt(randomIndex);
        }
        
        // 기존 버튼들에 보상 데이터 설정
        for (int i = 0; i < existingButtons.Length && i < selectedRewards.Count; i++)
        {
            CharacterData reward = selectedRewards[i];
            var button = existingButtons[i];
            
            // 버튼 활성화
            button.gameObject.SetActive(true);
            
            // 버튼 UI 설정
            var image = button.GetComponentInChildren<UnityEngine.UI.Image>();
            var text = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            
            if (image != null && reward.buttonIcon != null)
            {
                image.sprite = reward.buttonIcon.sprite;
            }
            
            if (text != null)
            {
                text.text = $"{reward.characterName}\n★★";
            }
            
            // 기존 리스너 제거 후 새 리스너 추가
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                OnRewardSelected(reward);
            });
        }
        
        // 남은 버튼들은 비활성화
        for (int i = selectedRewards.Count; i < existingButtons.Length; i++)
        {
            existingButtons[i].gameObject.SetActive(false);
        }
        
        // 패널 표시
        rewardSelectionPanel.SetActive(true);
        Time.timeScale = 0f; // 게임 일시정지
    }
    
    private void OnRewardSelected(CharacterData selectedCharacter)
    {
        Debug.Log($"[WaveSpawner] 보상 캐릭터 선택: {selectedCharacter.characterName}");
        
        // 빈 타일 찾기
        Tile emptyTile = TileManager.Instance.FindEmptyPlacedOrPlacableTile(false);
        
        if (emptyTile == null)
        {
            // placable 타일이 없으면 walkable 타일로
            emptyTile = TileManager.Instance.FindEmptyWalkableTile(false);
        }
        
        if (emptyTile != null && selectedCharacter.spawnPrefab != null)
        {
            // PlacementManager를 통해 보상 캐릭터 배치
            PlacementManager.Instance.PlaceRewardCharacterOnTile(selectedCharacter, emptyTile);
        }
        else
        {
            Debug.LogWarning("[WaveSpawner] 보상 캐릭터를 배치할 타일이 없습니다!");
        }
        
        // 패널 닫기
        rewardSelectionPanel.SetActive(false);
        Time.timeScale = 1f; // 게임 재개
    }
}