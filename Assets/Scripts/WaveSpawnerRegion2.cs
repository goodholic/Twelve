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
    public bool autoStartWaves = true;  // 수정: false에서 true로 변경하여 자동 시작
    public int maxWaveCount2 = 200;  // 수정: 5에서 200으로 변경

    private int currentWave2 = 0;
    private int aliveMonsters2 = 0;
    private bool isSpawning2 = false;  // 수정: 문법 오류 수정
    private bool autoStarted2 = false;

    [Header("중간성/최종성 참조 - Region2")]
    [Tooltip("좌측 중간성(체력 500)")]
    public GameObject leftMiddleCastle2;
    [Tooltip("중앙 중간성(체력 500)")]
    public GameObject centerMiddleCastle2;
    [Tooltip("우측 중간성(체력 500)")]
    public GameObject rightMiddleCastle2;
    [Tooltip("최종성(체력 1000)")]
    public GameObject finalCastle2;

    [Header("지역2 성 체력 UI")]
    [SerializeField] private TextMeshProUGUI region2LifeText;
    public int region2Life = 10;

    [Header("Wave Count Text - Region2")]
    [SerializeField] private TextMeshProUGUI waveCountText2;

    private void Start()
    {
        if (enemyMonsterPrefab == null)
        {
            Debug.LogError("[WaveSpawnerRegion2] enemyMonsterPrefab이 설정되지 않았습니다!");
            return;
        }

        // 중간성/최종성 체력 설정
        SetupCastleHealth();

        // 최초에 firstWaveDelay2 딜레이 후 자동 웨이브 시작
        StartCoroutine(DelayFirstWaveRoutine2(firstWaveDelay2));
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
        if (!autoStarted2 && autoStartWaves)
        {
            autoStarted2 = true;
            StartCoroutine(AutoWaveRoutine2());
        }
    }

    private IEnumerator AutoWaveRoutine2()
    {
        // 최대 웨이브 수까지만 실행
        while (currentWave2 < maxWaveCount2)
        {
            StartNextWave2();

            // 현재 웨이브가 끝날 때까지 대기
            while (isSpawning2 || aliveMonsters2 > 0)
            {
                yield return null;
            }
            
            // 다음 웨이브 시작 전 대기
            yield return new WaitForSeconds(timeBetweenWaves2);
        }

        Debug.Log($"[WaveSpawnerRegion2] {maxWaveCount2} wave 전부 완료!");
    }

    public void StartNextWave2()
    {
        if (!isSpawning2 && currentWave2 < maxWaveCount2)
        {
            Debug.Log($"[WaveSpawnerRegion2] StartNextWave2() 호출 -> currentWave2={currentWave2+1}");
            StartCoroutine(SpawnWaveRoutine2());
        }
        else if (currentWave2 >= maxWaveCount2)
        {
            Debug.Log($"[WaveSpawnerRegion2] 최대 웨이브 수({maxWaveCount2})에 도달했습니다.");
        }
        else
        {
            Debug.Log("[WaveSpawnerRegion2] 이미 웨이브 스폰 중입니다.");
        }
    }

    private IEnumerator SpawnWaveRoutine2()
    {
        isSpawning2 = true;
        currentWave2++;

        aliveMonsters2 = monstersPerWave2;
        
        Debug.Log($"[WaveSpawnerRegion2] Wave {currentWave2} 시작 - 몬스터 {monstersPerWave2}마리 소환 예정");
        
        if (waveCountText2 != null)
        {
            waveCountText2.text = $"Wave : {currentWave2}";
        }
        
        // 기획서: 5마리 동시 생성을 위해 3라인에 분배
        List<int> spawnRoutes = GetSpawnDistribution(monstersPerWave2);
        
        for (int i = 0; i < monstersPerWave2; i++)
        {
            SpawnEnemyMonster(i, spawnRoutes[i]);
            yield return new WaitForSeconds(spawnInterval2);
        }

        isSpawning2 = false;
        
        Debug.Log($"[WaveSpawnerRegion2] Wave {currentWave2} 스폰 완료");
    }

    /// <summary>
    /// 5마리를 3라인에 분배하는 로직
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
                if (monsterWaypoints2Left != null && monsterWaypoints2Left.Length > 0)
                    return monsterWaypoints2Left;
                break;
            case 1: // 중앙
                if (monsterWaypoints2Center != null && monsterWaypoints2Center.Length > 0)
                    return monsterWaypoints2Center;
                break;
            case 2: // 우측
                if (monsterWaypoints2Right != null && monsterWaypoints2Right.Length > 0)
                    return monsterWaypoints2Right;
                break;
        }
        
        Debug.LogWarning($"[WaveSpawnerRegion2] {route} 루트의 몬스터 웨이포인트가 없습니다!");
        return null;
    }

    /// <summary>
    /// 선택된 루트에 따른 웨이포인트 반환 (캐릭터용)
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
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    Debug.LogWarning($"[WaveSpawnerRegion2] {route} 루트의 웨이포인트[{i}]가 null입니다!");
                    return null;
                }
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
    /// 몬스터 생성 (라인 지정)
    /// </summary>
    private void SpawnEnemyMonster(int indexInWave, int routeIndex)
    {
        if (enemyMonsterPrefab == null || enemyMonsterParent == null)
        {
            Debug.LogError("[WaveSpawnerRegion2] 몬스터 프리팹/Parent 미설정");
            aliveMonsters2--;  // 생성 실패 시 카운트 감소
            return;
        }

        // 몬스터용 웨이포인트 가져오기
        Transform[] selectedWaypoints = GetMonsterWaypointsForRoute(routeIndex);
        if (selectedWaypoints == null || selectedWaypoints.Length == 0)
        {
            Debug.LogError($"[WaveSpawnerRegion2] 몬스터 {routeIndex} 루트의 웨이포인트가 없습니다!");
            aliveMonsters2--;
            return;
        }

        // 스폰 위치
        Vector3 spawnPos = GetMonsterSpawnPosition(routeIndex);

        // 몬스터 생성
        GameObject newMonster = Instantiate(enemyMonsterPrefab, spawnPos, Quaternion.identity, enemyMonsterParent);
        newMonster.name = $"Enemy_Monster_W{currentWave2}_{indexInWave}";

        Monster monster = newMonster.GetComponent<Monster>();
        if (monster != null)
        {
            // 몬스터 설정
            monster.pathWaypoints = selectedWaypoints;
            monster.areaIndex = 2;  // 지역2 몬스터
            
            // 라인 정보 설정
            RouteType routeType = RouteType.Center;
            switch (routeIndex)
            {
                case 0: routeType = RouteType.Left; break;
                case 1: routeType = RouteType.Center; break;
                case 2: routeType = RouteType.Right; break;
            }
            monster.SetMonsterRoute(routeType);  // 몬스터에 라인 정보 전달

            // 이벤트 연결
            monster.OnDeath += HandleMonsterDeath2;
            monster.OnReachedCastle += HandleMonsterReachedCastle2;
            
            Debug.Log($"[WaveSpawnerRegion2] AI 몬스터 생성 완료 - 위치: {spawnPos}, 루트: {routeType}");
        }
        else
        {
            Debug.LogError("[WaveSpawnerRegion2] 생성된 몬스터에 Monster 컴포넌트가 없습니다!");
            aliveMonsters2--;  // 생성 실패 시 카운트 감소
        }
    }

    private void HandleMonsterDeath2()
    {
        aliveMonsters2--;
        Debug.Log($"[WaveSpawnerRegion2] Monster 사망 -> aliveMonsters2={aliveMonsters2}");

        if (aliveMonsters2 < 0)
        {
            aliveMonsters2 = 0;
        }
    }

    private void HandleMonsterReachedCastle2(Monster monster)
    {
        Debug.Log($"[WaveSpawnerRegion2] Monster가 성에 도달함");
        // 성에 도달한 몬스터도 죽은 것으로 처리
    }

    /// <summary>
    /// Region2의 체력을 감소시키는 메서드
    /// (몬스터가 성에 도달했을 때 호출)
    /// </summary>
    public void TakeDamageToRegion2(int damage)
    {
        region2Life -= damage;
        Debug.Log($"[WaveSpawnerRegion2] Region2 성 피격! 남은 체력: {region2Life}");

        if (region2LifeText != null)
        {
            region2LifeText.text = $"Life : {region2Life}";
        }

        if (region2Life <= 0)
        {
            region2Life = 0;
            OnRegion2Defeated();
        }
    }

    /// <summary>
    /// Region2가 패배했을 때
    /// </summary>
    private void OnRegion2Defeated()
    {
        Debug.Log("[WaveSpawnerRegion2] Region2 패배!");
        // 게임 매니저에 승리 알림
        GameManager.Instance?.SetGameOver(true);  // 플레이어 승리
    }
}