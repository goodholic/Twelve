using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // [수정추가]

/// <summary>
/// 지역2(적 AI) 웨이브 스포너
/// </summary>
public class WaveSpawnerRegion2 : MonoBehaviour
{
    [Header("적 AI 몬스터 프리팹 (Region2 전용)")]
    public GameObject enemyMonsterPrefab;

    [Header("몬스터 부모 오브젝트(Region2)")]
    public Transform enemyMonsterParent;

    [Header("몬스터 이동 경로 - Region2")]
    [Tooltip("몬스터가 따라갈 웨이포인트")]
    public Transform[] monsterWaypoints2;
    
    // [수정] 기존 단일 경로를 제거하고 3개 루트로 변경
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
    public int monstersPerWave2 = 5;
    public float spawnInterval2 = 1f;
    public float firstWaveDelay2 = 10f;
    public int maxWaveCount2 = 200;

    private int currentWave2 = 0;
    private int aliveMonsters2 = 0;
    private bool isSpawning2 = false;
    private bool autoStarted2 = false;

    [Header("UI: 지역2 웨이브번호 표시용")]
    [SerializeField] private TextMeshProUGUI waveCountText2;

    // ===============================
    // [수정추가] 챕터별 몬스터 교체 로직
    // ===============================
    [Header("[수정추가] 지역2용 챕터 몬스터 교체 옵션")]    
    [Tooltip("true면 2챕터일 때 5마리 중 3번째는 '3챕터 몬스터'로 소환")]    
    public bool useChapterMonsterLogic = true;

    [Tooltip("현재 챕터(2라면 5마리 중 3번째는 3챕터 몬스터)")]
    public int currentChapter = 2;

    [Tooltip("3챕터 몬스터 프리팹(2챕터일 때, 5마리 중 3번째만 이것 사용)")]
    public GameObject chapter3MonsterPrefab;

    // ============================================
    // [수정추가] Region2 전용 1~101 챕터 몬스터 배열
    // ============================================
    [Header("[수정추가] Region2 전용 1~101 챕터 몬스터 프리팹 배열")]
    [Tooltip("예: index=0 => 1챕터 몬스터, index=1 => 2챕터 몬스터, ..., index=100 => 101챕터 몬스터")]
    public GameObject[] allChapterMonsterPrefabsRegion2 = new GameObject[101];
    // ============================================

    // ================== [수정 추가] 지역2 전용 생명력 ==================
    [Header("지역2 전용 생명력(텍스트)")]
    [SerializeField] private TextMeshProUGUI region2LifeText;
    public int region2Life = 10;

    // 루트 타입 열거형
    public enum RouteType
    {
        Left,
        Center,
        Right
    }

    private void Start()
    {
        StartCoroutine(DelayFirstWaveRoutine2(firstWaveDelay2));
        UpdateRegion2LifeText();
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

        if (waveCountText2 != null)
        {
            waveCountText2.text = $"{currentWave2} 웨이브";
        }

        aliveMonsters2 = monstersPerWave2;
        for (int i = 0; i < monstersPerWave2; i++)
        {
            SpawnEnemyMonster(i);
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
    /// 랜덤 루트 선택
    /// </summary>
    private RouteType GetRandomRoute()
    {
        int randomIndex = Random.Range(0, 3);
        return (RouteType)randomIndex;
    }

    /// <summary>
    /// 선택된 루트에 따른 웨이포인트 배열 반환
    /// </summary>
    private Transform[] GetWaypointsForRoute(RouteType route)
    {
        Transform[] waypoints = null;
        
        switch (route)
        {
            case RouteType.Left:
                waypoints = walkableLeft2;
                break;
            case RouteType.Center:
                waypoints = walkableCenter2;
                break;
            case RouteType.Right:
                waypoints = walkableRight2;
                break;
            default:
                waypoints = walkableCenter2; // 기본값은 중앙 루트
                break;
        }
        
        // ▼▼ [추가] 웨이포인트 유효성 검사 ▼▼
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
                // ▼▼ [추가] 웨이포인트 간 거리 검사 ▼▼
                for (int i = 0; i < validWaypoints.Count - 1; i++)
                {
                    float distance = Vector2.Distance(validWaypoints[i].position, validWaypoints[i + 1].position);
                    if (distance > 12f) // UI 좌표계에서 너무 먼 거리
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
    /// 모든 walkable2 타일의 웨이포인트를 하나의 배열로 반환
    /// </summary>
    private Transform[] GetAllWalkableWaypoints()
    {
        List<Transform> allWaypoints = new List<Transform>();
        
        // 좌측 경로 추가
        if (walkableLeft2 != null)
        {
            allWaypoints.AddRange(walkableLeft2);
        }
        
        // 중앙 경로 추가
        if (walkableCenter2 != null)
        {
            allWaypoints.AddRange(walkableCenter2);
        }
        
        // 우측 경로 추가
        if (walkableRight2 != null)
        {
            allWaypoints.AddRange(walkableRight2);
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
    /// 선택된 루트에 따른 스폰 위치 반환
    /// </summary>
    private Vector3 GetSpawnPositionForRoute(RouteType route)
    {
        Transform spawnPoint = null;
        
        switch (route)
        {
            case RouteType.Left:
                spawnPoint = leftSpawnPoint2;
                break;
            case RouteType.Center:
                spawnPoint = centerSpawnPoint2;
                break;
            case RouteType.Right:
                spawnPoint = rightSpawnPoint2;
                break;
            default:
                spawnPoint = centerSpawnPoint2; // 기본값은 중앙 스폰 포인트
                break;
        }

        // 스폰 포인트가 설정되어 있으면 그 위치 사용
        if (spawnPoint != null)
        {
            Debug.Log($"[WaveSpawnerRegion2] {route} 루트 스폰 포인트 사용: {spawnPoint.position}");
            return spawnPoint.position;
        }
        
        // 스폰 포인트가 없으면 해당 루트의 첫 번째 웨이포인트 위치 사용
        Transform[] waypoints = GetWaypointsForRoute(route);
        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            Debug.Log($"[WaveSpawnerRegion2] {route} 루트 첫 번째 웨이포인트 사용: {waypoints[0].position}");
            return waypoints[0].position;
        }

        // 모두 실패하면 기본 위치 사용
        Debug.LogWarning($"[WaveSpawnerRegion2] {route} 루트의 스폰 위치를 찾을 수 없어 기본 위치 사용");
        return transform.position;
    }



    // ======================================================
    // [수정] SpawnEnemyMonster → 인자 (int indexInWave) + 루트 선택
    // ======================================================
    private void SpawnEnemyMonster(int indexInWave)
    {
        if (enemyMonsterPrefab == null || enemyMonsterParent == null)
        {
            Debug.LogError("[WaveSpawnerRegion2] 몬스터 프리팹/Parent 미설정");
            return;
        }

        // 몬스터는 단일 경로 사용
        if (monsterWaypoints2 == null || monsterWaypoints2.Length == 0)
        {
            Debug.LogError($"[WaveSpawnerRegion2] 몬스터 웨이포인트가 설정되지 않았습니다!");
            return;
        }

        // 스폰 위치는 첫 번째 웨이포인트
        Vector3 spawnPos = monsterWaypoints2[0].position;

        // -----------------------------------------------
        // [수정추가] 챕터별 3번째 몬스터만 다음 챕터 몬스터
        // + 101챕터까지 배열로 확장
        // -----------------------------------------------
        GameObject prefabToSpawn = enemyMonsterPrefab; // 기본값
        int monsterChapter = currentChapter; // 기본적으로는 현재 챕터 적용

        if (useChapterMonsterLogic)
        {
            // 1) 배열이 101개 세팅되어 있는 경우 => 배열 기반 로직 우선
            if (allChapterMonsterPrefabsRegion2 != null && allChapterMonsterPrefabsRegion2.Length >= 101)
            {
                // (1) indexInWave == 2 -> 다음 챕터 몬스터
                if (indexInWave == 2 && currentChapter < 101 && allChapterMonsterPrefabsRegion2[currentChapter] != null)
                {
                    prefabToSpawn = allChapterMonsterPrefabsRegion2[currentChapter];
                    monsterChapter = currentChapter + 1; // 다음 챕터 스탯 적용
                }
                // (2) 나머지는 현재 챕터 몬스터
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
                // 2) 배열이 제대로 설정되지 않은 경우: 챕터별 개별 로직 사용
                // 모든 챕터에서 웨이브의 3번째 몬스터(indexInWave == 2)만 다음 챕터 몬스터 사용
                if (indexInWave == 2)
                {
                    // 2챕터에서는 3챕터 몬스터 사용
                    if (currentChapter == 2 && chapter3MonsterPrefab != null)
                    {
                        prefabToSpawn = chapter3MonsterPrefab;
                        monsterChapter = 3; // 3챕터 스탯 적용
                    }
                    // 3챕터 이상에서는 chapter3MonsterPrefab을 재사용하되 스탯은 다음 챕터로 증가
                    else if (currentChapter >= 3 && chapter3MonsterPrefab != null)
                    {
                        prefabToSpawn = chapter3MonsterPrefab;
                        monsterChapter = currentChapter + 1; // 다음 챕터 스탯 적용
                        Debug.Log($"[WaveSpawnerRegion2] 다음 챕터 몬스터 프리팹이 없어 chapter3MonsterPrefab 사용. 능력치는 {monsterChapter}챕터 적용");
                    }
                }
                // 나머지는 기본 enemyMonsterPrefab
            }
        }

        GameObject enemyObj = Instantiate(prefabToSpawn, enemyMonsterParent);
        // === [수정] 혹시라도 비활성화된 상태로 생성되면 활성화 ===
        enemyObj.SetActive(true);

        enemyObj.transform.position = spawnPos;

        Monster enemyComp = enemyObj.GetComponent<Monster>();
        if (enemyComp != null)
        {
            enemyComp.pathWaypoints = monsterWaypoints2; // 몬스터 단일 경로 설정
            enemyComp.areaIndex = 2;
            enemyComp.OnDeath += HandleEnemyMonsterDeath;
            
            // 챕터에 따른 몬스터 스탯 설정
            enemyComp.currentChapter = monsterChapter;
            
            // 몬스터 정보 로그 출력
            Debug.Log($"[WaveSpawnerRegion2] Spawned Monster, indexInWave={indexInWave}, chapter={monsterChapter}, Wave={currentWave2}");
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
