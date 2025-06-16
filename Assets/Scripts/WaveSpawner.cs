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

    [Header("웨이브 스폰 설정")]
    [Tooltip("최초 웨이브 지연 시간")]
    public float firstWaveDelay = 10f;
    [Tooltip("자동 웨이브 시작 여부")]
    public bool autoStartWaves = true;

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
            while (isSpawning || aliveMonsters > 0)
            {
                yield return null;
            }
            
            // 다음 웨이브 전 대기 시간
            yield return new WaitForSeconds(timeBetweenWaves);
        }
        Debug.Log("[WaveSpawner] 200 wave 전부 완료!");
    }

    public void StartNextWave()
    {
        if (!isSpawning && aliveMonsters == 0)
        {
            Debug.Log($"[WaveSpawner] StartNextWave() 호출 -> currentWave={currentWave+1}");
            StartCoroutine(SpawnWaveRoutine());
        }
        else
        {
            Debug.Log($"[WaveSpawner] 웨이브 시작 불가 - isSpawning: {isSpawning}, aliveMonsters: {aliveMonsters}");
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

        isSpawning = false;
        
        Debug.Log($"[WaveSpawner] Wave {currentWave} 스폰 완료");
    }

    /// <summary>
    /// 5마리를 3라인에 분배
    /// </summary>
    private List<int> GetSpawnDistribution(int monsterCount)
    {
        List<int> distribution = new List<int>();
        
        // 기본적으로 균등 분배
        for (int i = 0; i < monsterCount; i++)
        {
            distribution.Add(i % 3); // 0:좌, 1:중, 2:우
        }
        
        // 랜덤하게 섞기
        for (int i = 0; i < distribution.Count; i++)
        {
            int randomIndex = Random.Range(i, distribution.Count);
            int temp = distribution[i];
            distribution[i] = distribution[randomIndex];
            distribution[randomIndex] = temp;
        }
        
        return distribution;
    }

    /// <summary>
    /// 몬스터용 웨이포인트 가져오기
    /// </summary>
    private Transform[] GetMonsterWaypointsForRoute(int route)
    {
        switch (route)
        {
            case 0: // 좌측
                if (monsterWaypointsLeft != null && monsterWaypointsLeft.Length > 0)
                    return monsterWaypointsLeft;
                break;
            case 1: // 중앙
                if (monsterWaypointsCenter != null && monsterWaypointsCenter.Length > 0)
                    return monsterWaypointsCenter;
                break;
            case 2: // 우측
                if (monsterWaypointsRight != null && monsterWaypointsRight.Length > 0)
                    return monsterWaypointsRight;
                break;
        }
        
        Debug.LogWarning($"[WaveSpawner] {route} 루트의 몬스터 웨이포인트가 없습니다!");
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
            aliveMonsters--;  // 생성 실패 시 카운트 감소
            return;
        }

        // 선택된 루트의 웨이포인트 가져오기
        Transform[] selectedWaypoints = GetMonsterWaypointsForRoute(routeIndex);
        if (selectedWaypoints == null || selectedWaypoints.Length == 0)
        {
            Debug.LogError($"[WaveSpawner] 몬스터 {routeIndex} 루트의 웨이포인트가 없습니다!");
            aliveMonsters--;
            return;
        }

        // 스폰 위치
        Vector3 spawnPos = GetMonsterSpawnPosition(routeIndex);

        // 챕터별 몬스터 선택 로직
        GameObject prefabToUse = monsterPrefab;
        int monsterChapter = currentChapter;

        if (useChapterMonsterLogic && indexInWave == 2) // 3번째 몬스터 (인덱스 2)
        {
            if (currentChapter < chapterMonsters.Length && chapterMonsters[currentChapter] != null)
            {
                prefabToUse = chapterMonsters[currentChapter];
                monsterChapter = currentChapter + 1;
                Debug.Log($"[WaveSpawner] 3번째 몬스터는 다음 챕터({monsterChapter}) 몬스터로 소환");
            }
        }

        // 몬스터 생성
        GameObject newMonster = Instantiate(prefabToUse, spawnPos, Quaternion.identity, monsterParent);
        newMonster.name = $"Monster_W{currentWave}_{indexInWave}";

        Monster monster = newMonster.GetComponent<Monster>();
        if (monster != null)
        {
            // 몬스터 설정
            monster.areaIndex = 1;  // 지역1 몬스터
            monster.SetChapter(monsterChapter);
            
            // 라인 정보 설정
            RouteType routeType = RouteType.Center;
            switch (routeIndex)
            {
                case 0: routeType = RouteType.Left; break;
                case 1: routeType = RouteType.Center; break;
                case 2: routeType = RouteType.Right; break;
            }
            
            // 경로 설정
            monster.SetPath(selectedWaypoints, routeType);

            // 이벤트 연결
            monster.OnDeath += HandleMonsterDeath;
            monster.OnReachedCastle += HandleMonsterReachedCastle;
            
            Debug.Log($"[WaveSpawner] 몬스터 생성 완료 - 위치: {spawnPos}, 루트: {routeType}, 챕터: {monsterChapter}");
        }
        else
        {
            Debug.LogError("[WaveSpawner] 생성된 몬스터에 Monster 컴포넌트가 없습니다!");
            aliveMonsters--;  // 생성 실패 시 카운트 감소
        }
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

    private void HandleMonsterReachedCastle(Monster monster)
    {
        Debug.Log($"[WaveSpawner] Monster가 성에 도달함");
        // 성에 도달한 몬스터도 죽은 것으로 처리
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
        
        // 보상 선택 패널 활성화
        rewardSelectionPanel.SetActive(true);
        
        // 기존 보상 버튼들 제거
        foreach (Transform child in rewardButtonParent)
        {
            Destroy(child.gameObject);
        }
        
        // StarMergeDatabaseObject의 GetWaveRewardCandidates 메서드 사용
        CharacterData[] rewardCandidates = starMergeDatabase.GetWaveRewardCandidates();
        
        if (rewardCandidates != null && rewardCandidates.Length >= 3)
        {
            // 3개 보상 캐릭터 표시
            for (int i = 0; i < 3; i++)
            {
                CharacterData selectedChar = rewardCandidates[i];
                if (selectedChar != null)
                {
                    // 보상 버튼 생성 (실제 구현 필요)
                    Debug.Log($"[WaveSpawner] 보상 캐릭터 선택지 {i+1}: {selectedChar.characterName}");
                }
                else
                {
                    Debug.LogWarning($"[WaveSpawner] 보상 캐릭터 {i+1}이 null입니다!");
                }
            }
        }
        else
        {
            Debug.LogWarning("[WaveSpawner] 보상 캐릭터를 가져올 수 없습니다!");
        }
    }

    /// <summary>
    /// 웨이브 스폰너 초기화
    /// </summary>
    public void Initialize()
    {
        Debug.Log("[WaveSpawner] Initialize() 호출");
        
        // 중간성/최종성 체력 설정
        SetupCastleHealth();
        
        // 자동 웨이브 시작이 활성화되어 있으면 시작
        if (autoStartWaves)
        {
            StartCoroutine(DelayFirstWaveRoutine(firstWaveDelay));
        }
    }
    
    /// <summary>
    /// 웨이브 스폰 중지
    /// </summary>
    public void StopSpawning()
    {
        Debug.Log("[WaveSpawner] StopSpawning() 호출");
        
        // 모든 코루틴 중지
        StopAllCoroutines();
        
        // 스폰 상태 초기화
        isSpawning = false;
        autoStarted = false;
    }

    /// <summary>
    /// 몬스터가 죽었을 때 호출되는 메서드
    /// </summary>
    public void OnMonsterKilled(Monster killedMonster)
    {
        if (killedMonster != null)
        {
            Debug.Log($"[WaveSpawner] 몬스터 {killedMonster.monsterName} 처치됨");
            
            // 몬스터 카운트 감소
            aliveMonsters--;
            
            // 웨이브 완료 체크
            CheckWaveCompletion();
        }
    }

    /// <summary>
    /// 웨이브 완료 체크
    /// </summary>
    private void CheckWaveCompletion()
    {
        if (aliveMonsters <= 0 && !isSpawning)
        {
            Debug.Log($"[WaveSpawner] Wave {currentWave} 완료!");
            OnWaveClear();
        }
    }
}