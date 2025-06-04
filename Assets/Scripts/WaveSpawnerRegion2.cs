using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 지역2(적 AI) 웨이브 스포너
/// </summary>
public class WaveSpawnerRegion2 : MonoBehaviour
{
    [Header("적 AI 몬스터 프리팹 (Region2 전용)")]
    public GameObject enemyMonsterPrefab;

    [Header("몬스터 부모 오브젝트(Region2)")]
    public Transform enemyMonsterParent;

    [Header("몬스터 이동 경로 (3라인) - Region2")]
    [Tooltip("좌측 루트 몬스터 웨이포인트")]
    public Transform[] monsterWaypoints2Left;
    [Tooltip("중앙 루트 몬스터 웨이포인트")]
    public Transform[] monsterWaypoints2Center;
    [Tooltip("우측 루트 몬스터 웨이포인트")]
    public Transform[] monsterWaypoints2Right;
    
    [Header("몬스터 루트별 스폰 위치 - Region2")]
    [Tooltip("좌측 루트 몬스터 스폰 위치")]
    public Transform monsterLeftSpawnPoint2;
    [Tooltip("중앙 루트 몬스터 스폰 위치")]
    public Transform monsterCenterSpawnPoint2;
    [Tooltip("우측 루트 몬스터 스폰 위치")]
    public Transform monsterRightSpawnPoint2;
    
    [Header("캐릭터 공격 루트 설정 (좌/중/우) - Region2")]
    [Tooltip("좌측 루트 웨이포인트")]
    public Transform[] walkableLeft2;
    [Tooltip("중앙 루트 웨이포인트")]
    public Transform[] walkableCenter2;
    [Tooltip("우측 루트 웨이포인트")]
    public Transform[] walkableRight2;
    
    [Header("캐릭터 루트별 스폰 위치 - Region2")]
    [Tooltip("좌측 루트 스폰 위치")]
    public Transform leftSpawnPoint2;
    [Tooltip("중앙 루트 스폰 위치")]
    public Transform centerSpawnPoint2;
    [Tooltip("우측 루트 스폰 위치")]
    public Transform rightSpawnPoint2;

    [Header("웨이브 설정 (Region2)")]
    public float timeBetweenWaves2 = 5f;
    public int monstersPerWave2 = 5; // 기획서: 5마리 동시 생성
    public float spawnInterval2 = 0.2f; // 동시 생성을 위해 간격 축소
    public float firstWaveDelay2 = 10f;
    public int maxWaveCount2 = 200;

    private int currentWave2 = 0;
    private int aliveMonsters2 = 0;
    private bool isSpawning2 = false;
    private bool autoStarted2 = false;

    [Header("챕터별 몬스터 교체 옵션")]    
    [Tooltip("true면 2챕터일 때 5마리 중 3번째는 '3챕터 몬스터'로 소환")]    
    public bool useChapterMonsterLogic = true;

    [Tooltip("현재 챕터(2라면 5마리 중 3번째는 3챕터 몬스터)")]
    public int currentChapter = 2;

    [Tooltip("3챕터 몬스터 프리팹(2챕터일 때, 5마리 중 3번째만 이것 사용)")]
    public GameObject chapter3MonsterPrefab;

    [Header("Region2 전용 1~101 챕터 몬스터 프리팹 배열")]
    [Tooltip("예: index=0 => 1챕터 몬스터, index=1 => 2챕터 몬스터, ..., index=100 => 101챕터 몬스터")]
    public GameObject[] allChapterMonsterPrefabsRegion2 = new GameObject[101];

    [Header("지역2 전용 생명력(텍스트)")]
    [SerializeField] private TextMeshProUGUI region2LifeText;
    public int region2Life = 10;

    [Header("중간성/최종성 참조")]
    [Tooltip("좌측 중간성(체력 500)")]
    public GameObject leftMiddleCastle2;
    [Tooltip("중앙 중간성(체력 500)")]
    public GameObject centerMiddleCastle2;
    [Tooltip("우측 중간성(체력 500)")]
    public GameObject rightMiddleCastle2;
    [Tooltip("최종성(체력 1000)")]
    public GameObject finalCastle2;

    private void Start()
    {
        StartCoroutine(DelayFirstWaveRoutine2(firstWaveDelay2));
        UpdateRegion2LifeText();
        
        // 중간성/최종성 체력 설정
        SetupCastleHealth();
    }

    /// <summary>
    /// 중간성 체력 500, 최종성 체력 1000 설정
    /// </summary>
    private void SetupCastleHealth()
    {
        // 중간성 체력 설정
        if (leftMiddleCastle2 != null)
        {
            var middleCastle = leftMiddleCastle2.GetComponent<MiddleCastle>();
            if (middleCastle != null)
            {
                middleCastle.maxHealth = 500;
                middleCastle.currentHealth = 500;
                Debug.Log($"[WaveSpawnerRegion2] 좌측 중간성 체력 500으로 설정");
            }
        }
        
        if (centerMiddleCastle2 != null)
        {
            var middleCastle = centerMiddleCastle2.GetComponent<MiddleCastle>();
            if (middleCastle != null)
            {
                middleCastle.maxHealth = 500;
                middleCastle.currentHealth = 500;
                Debug.Log($"[WaveSpawnerRegion2] 중앙 중간성 체력 500으로 설정");
            }
        }
        
        if (rightMiddleCastle2 != null)
        {
            var middleCastle = rightMiddleCastle2.GetComponent<MiddleCastle>();
            if (middleCastle != null)
            {
                middleCastle.maxHealth = 500;
                middleCastle.currentHealth = 500;
                Debug.Log($"[WaveSpawnerRegion2] 우측 중간성 체력 500으로 설정");
            }
        }
        
        // 최종성 체력 설정
        if (finalCastle2 != null)
        {
            var finalCastleComp = finalCastle2.GetComponent<FinalCastle>();
            if (finalCastleComp != null)
            {
                finalCastleComp.maxHealth = 1000;
                finalCastleComp.currentHealth = 1000;
                Debug.Log("[WaveSpawnerRegion2] 최종성 체력 1000으로 설정");
            }
        }
    }

    private IEnumerator DelayFirstWaveRoutine2(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartAutoWaveSpawn2();
    }

    private void StartAutoWaveSpawn2()
    {
        if (!autoStarted2)
        {
            autoStarted2 = true;
            StartCoroutine(AutoWaveRoutine2());
        }
    }

    private IEnumerator AutoWaveRoutine2()
    {
        while (currentWave2 < maxWaveCount2)
        {
            StartNextWave2();

            while (isSpawning2)
            {
                yield return null;
            }
        }

        Debug.Log("[WaveSpawnerRegion2] 모든 웨이브 종료 or 최대치 도달");
    }

    public void StartNextWave2()
    {
        if (!isSpawning2)
        {
            StartCoroutine(SpawnWaveRoutine2());
        }
    }

    private IEnumerator SpawnWaveRoutine2()
    {
        isSpawning2 = true;
        currentWave2++;

        aliveMonsters2 = monstersPerWave2;
        
        // 기획서: 5마리 동시 생성을 위해 3라인에 분배
        List<int> spawnRoutes = GetSpawnDistribution(monstersPerWave2);
        
        for (int i = 0; i < monstersPerWave2; i++)
        {
            SpawnEnemyMonster(i, spawnRoutes[i]);
            yield return new WaitForSeconds(spawnInterval2);
        }

        while (aliveMonsters2 > 0)
        {
            yield return null;
        }

        Debug.Log($"[WaveSpawnerRegion2] 웨이브 {currentWave2} 클리어!");
        yield return new WaitForSeconds(timeBetweenWaves2);

        isSpawning2 = false;
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
                waypoints = monsterWaypoints2Left;
                break;
            case 1:
                waypoints = monsterWaypoints2Center;
                break;
            case 2:
                waypoints = monsterWaypoints2Right;
                break;
            default:
                waypoints = monsterWaypoints2Center;
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
        
        Debug.LogWarning($"[WaveSpawnerRegion2] 몬스터 {route} 루트의 웨이포인트가 없습니다!");
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
                spawnPoint = monsterLeftSpawnPoint2;
                break;
            case 1:
                spawnPoint = monsterCenterSpawnPoint2;
                break;
            case 2:
                spawnPoint = monsterRightSpawnPoint2;
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

    /// <summary>
    /// 선택된 루트에 따른 웨이포인트 배열 반환 (캐릭터용)
    /// </summary>
    private Transform[] GetWaypointsForRoute(int route)
    {
        Transform[] waypoints = null;
        
        switch (route)
        {
            case 0:
                waypoints = walkableLeft2;
                break;
            case 1:
                waypoints = walkableCenter2;
                break;
            case 2:
                waypoints = walkableRight2;
                break;
            default:
                waypoints = walkableCenter2;
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
                    Debug.LogWarning($"[WaveSpawnerRegion2] {route} 루트의 웨이포인트[{i}]가 null입니다!");
                }
            }
            
            if (validWaypoints.Count > 0)
            {
                for (int i = 0; i < validWaypoints.Count - 1; i++)
                {
                    float distance = Vector2.Distance(validWaypoints[i].position, validWaypoints[i + 1].position);
                    if (distance > 15f)
                    {
                        Debug.LogWarning($"[WaveSpawnerRegion2] {route} 루트 웨이포인트[{i}]→[{i+1}] 거리가 너무 멉니다: {distance:F2}");
                    }
                }
                
                Debug.Log($"[WaveSpawnerRegion2] {route} 루트 웨이포인트 검증 완료: {validWaypoints.Count}개");
                return validWaypoints.ToArray();
            }
            else
            {
                Debug.LogError($"[WaveSpawnerRegion2] {route} 루트에 유효한 웨이포인트가 없습니다!");
                return null;
            }
        }
        
        Debug.LogWarning($"[WaveSpawnerRegion2] {route} 루트의 웨이포인트 배열이 null이거나 비어있습니다!");
        return null;
    }

    /// <summary>
    /// 선택된 루트에 따른 스폰 위치 반환 (캐릭터용)
    /// </summary>
    private Vector3 GetSpawnPositionForRoute(int route)
    {
        Transform spawnPoint = null;
        
        switch (route)
        {
            case 0:
                spawnPoint = leftSpawnPoint2;
                break;
            case 1:
                spawnPoint = centerSpawnPoint2;
                break;
            case 2:
                spawnPoint = rightSpawnPoint2;
                break;
            default:
                spawnPoint = centerSpawnPoint2;
                break;
        }

        if (spawnPoint != null)
        {
            Debug.Log($"[WaveSpawnerRegion2] {route} 루트 스폰 포인트 사용: {spawnPoint.position}");
            return spawnPoint.position;
        }
        
        Transform[] waypoints = GetWaypointsForRoute(route);
        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            Debug.Log($"[WaveSpawnerRegion2] {route} 루트 첫 번째 웨이포인트 사용: {waypoints[0].position}");
            return waypoints[0].position;
        }

        Debug.LogWarning($"[WaveSpawnerRegion2] {route} 루트의 스폰 위치를 찾을 수 없어 기본 위치 사용");
        return transform.position;
    }

    /// <summary>
    /// 몬스터 생성 (라인 지정)
    /// </summary>
    private void SpawnEnemyMonster(int indexInWave, int routeIndex)
    {
        if (enemyMonsterPrefab == null || enemyMonsterParent == null)
        {
            Debug.LogError("[WaveSpawnerRegion2] 몬스터 프리팹/Parent 미설정");
            return;
        }

        // 몬스터용 웨이포인트 가져오기
        Transform[] selectedWaypoints = GetMonsterWaypointsForRoute(routeIndex);
        if (selectedWaypoints == null || selectedWaypoints.Length == 0)
        {
            Debug.LogError($"[WaveSpawnerRegion2] 몬스터 {routeIndex} 루트의 웨이포인트가 없습니다!");
            return;
        }

        // 스폰 위치
        Vector3 spawnPos = GetMonsterSpawnPosition(routeIndex);

        // 챕터별 몬스터 선택 로직
        GameObject prefabToSpawn = enemyMonsterPrefab;
        int monsterChapter = currentChapter;

        if (useChapterMonsterLogic)
        {
            if (allChapterMonsterPrefabsRegion2 != null && allChapterMonsterPrefabsRegion2.Length >= 101)
            {
                if (indexInWave == 2 && currentChapter < 101 && allChapterMonsterPrefabsRegion2[currentChapter] != null)
                {
                    prefabToSpawn = allChapterMonsterPrefabsRegion2[currentChapter];
                    monsterChapter = currentChapter + 1;
                }
                else
                {
                    int curIdx = currentChapter - 1;
                    if (curIdx >= 0 && curIdx < allChapterMonsterPrefabsRegion2.Length 
                        && allChapterMonsterPrefabsRegion2[curIdx] != null)
                    {
                        prefabToSpawn = allChapterMonsterPrefabsRegion2[curIdx];
                    }
                }
            }
            else
            {
                if (indexInWave == 2)
                {
                    if (currentChapter == 2 && chapter3MonsterPrefab != null)
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

        GameObject enemyObj = Instantiate(prefabToSpawn, enemyMonsterParent);
        enemyObj.SetActive(true);
        enemyObj.transform.position = spawnPos;

        Monster enemyComp = enemyObj.GetComponent<Monster>();
        if (enemyComp != null)
        {
            enemyComp.pathWaypoints = selectedWaypoints;
            enemyComp.areaIndex = 2;
            enemyComp.OnDeath += HandleEnemyMonsterDeath;
            enemyComp.currentChapter = monsterChapter;
            
            // 몬스터가 어느 루트인지 설정
            switch (routeIndex)
            {
                case 0:
                    enemyComp.SetMonsterRoute(RouteType.Left);
                    break;
                case 1:
                    enemyComp.SetMonsterRoute(RouteType.Center);
                    break;
                case 2:
                    enemyComp.SetMonsterRoute(RouteType.Right);
                    break;
            }
            
            Debug.Log($"[WaveSpawnerRegion2] Spawned Monster on route {routeIndex}, indexInWave={indexInWave}, chapter={monsterChapter}, Wave={currentWave2}");
        }
    }

    private void HandleEnemyMonsterDeath()
    {
        aliveMonsters2--;
        if (aliveMonsters2 < 0) aliveMonsters2 = 0;
    }

    /// <summary>
    /// (지역2 체력 감소) -> 0 이하가 되면 플레이어 승리 처리
    /// </summary>
    public void TakeDamageToRegion2(int amount)
    {
        region2Life -= amount;
        if (region2Life < 0)
            region2Life = 0;

        UpdateRegion2LifeText();
        Debug.Log($"[WaveSpawnerRegion2] 지역2 체력 감소: {amount} => 남은 HP={region2Life}");

        if (region2Life <= 0)
        {
            Debug.LogWarning("[WaveSpawnerRegion2] 지역2 체력=0 => 플레이어 승리!");
            GameManager.Instance.SetGameOver(true);
        }
    }

    private void UpdateRegion2LifeText()
    {
        if (region2LifeText != null)
        {
            region2LifeText.text = $"HP: {region2Life}";
        }
    }
}