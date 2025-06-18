using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 몬스터 소환 관리자 - 최대 50마리 제한
/// </summary>
public class MonsterSpawnManager : MonoBehaviour
{
    private static MonsterSpawnManager instance;
    public static MonsterSpawnManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<MonsterSpawnManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MonsterSpawnManager");
                    instance = go.AddComponent<MonsterSpawnManager>();
                }
            }
            return instance;
        }
    }
    
    [Header("소환 설정")]
    [SerializeField] private int maxMonsterCount = 50;
    [SerializeField] private float spawnCooldown = 2f;
    [SerializeField] private int monstersPerWave = 5;
    
    [Header("스폰 위치")]
    [SerializeField] private Transform[] leftSpawnPoints;
    [SerializeField] private Transform[] centerSpawnPoints;
    [SerializeField] private Transform[] rightSpawnPoints;
    
    [Header("몬스터 프리팹")]
    [SerializeField] private GameObject[] monsterPrefabs;
    
    private List<Monster> activeMonsters = new List<Monster>();
    private Queue<MonsterSpawnRequest> spawnQueue = new Queue<MonsterSpawnRequest>();
    private bool isSpawning = false;
    private float lastSpawnTime = 0f;
    
    // 플레이어별 소환 제한
    private Dictionary<PlayerController, int> playerMonsterCounts = new Dictionary<PlayerController, int>();
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    
    /// <summary>
    /// 몬스터 소환 요청
    /// </summary>
    public bool RequestMonsterSpawn(PlayerController player, int monsterIndex, int routeIndex, int count = 1)
    {
        // 플레이어별 소환 수 체크
        if (!playerMonsterCounts.ContainsKey(player))
        {
            playerMonsterCounts[player] = 0;
        }
        
        int currentCount = GetActiveMonsterCount(player);
        if (currentCount + count > maxMonsterCount)
        {
            int availableSlots = maxMonsterCount - currentCount;
            Debug.LogWarning($"[MonsterSpawnManager] {player.name}의 몬스터 소환 제한 초과! " +
                           $"현재: {currentCount}/{maxMonsterCount}, 요청: {count}, 가능: {availableSlots}");
            
            if (availableSlots <= 0)
                return false;
                
            count = availableSlots; // 가능한 만큼만 소환
        }
        
        // 소환 요청 큐에 추가
        MonsterSpawnRequest request = new MonsterSpawnRequest
        {
            player = player,
            monsterIndex = monsterIndex,
            routeIndex = routeIndex,
            count = count
        };
        
        spawnQueue.Enqueue(request);
        
        // 소환 프로세스 시작
        if (!isSpawning)
        {
            StartCoroutine(ProcessSpawnQueue());
        }
        
        return true;
    }
    
    /// <summary>
    /// 소환 큐 처리
    /// </summary>
    private IEnumerator ProcessSpawnQueue()
    {
        isSpawning = true;
        
        while (spawnQueue.Count > 0)
        {
            // 쿨다운 체크
            float timeSinceLastSpawn = Time.time - lastSpawnTime;
            if (timeSinceLastSpawn < spawnCooldown)
            {
                yield return new WaitForSeconds(spawnCooldown - timeSinceLastSpawn);
            }
            
            MonsterSpawnRequest request = spawnQueue.Dequeue();
            
            // 실제 소환 처리
            for (int i = 0; i < request.count; i++)
            {
                SpawnMonster(request.player, request.monsterIndex, request.routeIndex);
                yield return new WaitForSeconds(0.2f); // 연속 소환 간격
            }
            
            lastSpawnTime = Time.time;
        }
        
        isSpawning = false;
    }
    
    /// <summary>
    /// 실제 몬스터 소환
    /// </summary>
    private void SpawnMonster(PlayerController player, int monsterIndex, int routeIndex)
    {
        if (monsterIndex < 0 || monsterIndex >= monsterPrefabs.Length)
        {
            Debug.LogError($"[MonsterSpawnManager] 잘못된 몬스터 인덱스: {monsterIndex}");
            return;
        }
        
        // 스폰 위치 결정
        Transform spawnPoint = GetSpawnPoint(player, routeIndex);
        if (spawnPoint == null)
        {
            Debug.LogError($"[MonsterSpawnManager] 스폰 포인트를 찾을 수 없습니다. Route: {routeIndex}");
            return;
        }
        
        // 몬스터 생성
        GameObject monsterObj = Instantiate(monsterPrefabs[monsterIndex], spawnPoint.position, Quaternion.identity);
        Monster monster = monsterObj.GetComponent<Monster>();
        
        if (monster != null)
        {
            // 몬스터 초기화
            monster.SetOwnerPlayer(player);
            monster.selectedRoute = routeIndex;
            
            // 웨이포인트 설정
            Transform[] waypoints = GetWaypoints(player, routeIndex);
            if (waypoints != null)
            {
                monster.SetWaypoints(waypoints);
            }
            
            // 활성 몬스터 목록에 추가
            activeMonsters.Add(monster);
            
            // 플레이어별 카운트 증가
            if (!playerMonsterCounts.ContainsKey(player))
                playerMonsterCounts[player] = 0;
            playerMonsterCounts[player]++;
            
            Debug.Log($"[MonsterSpawnManager] {player.name}의 몬스터 소환 완료 - " +
                     $"현재 몬스터 수: {playerMonsterCounts[player]}/{maxMonsterCount}");
        }
    }
    
    /// <summary>
    /// 스폰 포인트 가져오기
    /// </summary>
    private Transform GetSpawnPoint(PlayerController player, int routeIndex)
    {
        Transform[] spawnPoints = null;
        
        switch (routeIndex)
        {
            case 0: // 좌측
                spawnPoints = leftSpawnPoints;
                break;
            case 1: // 중앙
                spawnPoints = centerSpawnPoints;
                break;
            case 2: // 우측
                spawnPoints = rightSpawnPoints;
                break;
        }
        
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // 플레이어 지역에 따른 스폰 포인트 선택
            int spawnIndex = player.AreaIndex == 1 ? 0 : spawnPoints.Length - 1;
            if (spawnIndex < spawnPoints.Length)
                return spawnPoints[spawnIndex];
        }
        
        return null;
    }
    
    /// <summary>
    /// 웨이포인트 가져오기
    /// </summary>
    private Transform[] GetWaypoints(PlayerController player, int routeIndex)
    {
        // WaypointManager에서 웨이포인트 가져오기
        if (WaypointManager.Instance != null)
        {
            return WaypointManager.Instance.GetWaypoints(player.AreaIndex, routeIndex);
        }
        
        return null;
    }
    
    /// <summary>
    /// 몬스터 제거 시 호출
    /// </summary>
    public void OnMonsterRemoved(Monster monster)
    {
        if (activeMonsters.Remove(monster))
        {
            PlayerController owner = monster.GetOwnerPlayer();
            if (owner != null && playerMonsterCounts.ContainsKey(owner))
            {
                playerMonsterCounts[owner]--;
                Debug.Log($"[MonsterSpawnManager] {owner.name}의 몬스터 제거 - " +
                         $"남은 몬스터: {playerMonsterCounts[owner]}/{maxMonsterCount}");
            }
        }
    }
    
    /// <summary>
    /// 특정 플레이어의 활성 몬스터 수 가져오기
    /// </summary>
    public int GetActiveMonsterCount(PlayerController player)
    {
        if (playerMonsterCounts.ContainsKey(player))
            return playerMonsterCounts[player];
        return 0;
    }
    
    /// <summary>
    /// 전체 활성 몬스터 수
    /// </summary>
    public int GetTotalActiveMonsterCount()
    {
        return activeMonsters.Count;
    }
    
    /// <summary>
    /// 남은 소환 가능 수
    /// </summary>
    public int GetAvailableSpawnCount(PlayerController player)
    {
        int currentCount = GetActiveMonsterCount(player);
        return Mathf.Max(0, maxMonsterCount - currentCount);
    }
    
    /// <summary>
    /// 모든 몬스터 제거
    /// </summary>
    public void ClearAllMonsters()
    {
        foreach (Monster monster in activeMonsters)
        {
            if (monster != null)
                Destroy(monster.gameObject);
        }
        
        activeMonsters.Clear();
        playerMonsterCounts.Clear();
        spawnQueue.Clear();
    }
    
    /// <summary>
    /// 특정 플레이어의 몬스터만 제거
    /// </summary>
    public void ClearPlayerMonsters(PlayerController player)
    {
        List<Monster> toRemove = new List<Monster>();
        
        foreach (Monster monster in activeMonsters)
        {
            if (monster != null && monster.GetOwnerPlayer() == player)
            {
                toRemove.Add(monster);
            }
        }
        
        foreach (Monster monster in toRemove)
        {
            activeMonsters.Remove(monster);
            Destroy(monster.gameObject);
        }
        
        if (playerMonsterCounts.ContainsKey(player))
            playerMonsterCounts[player] = 0;
    }
    
    // 몬스터 소환 요청 구조체
    private struct MonsterSpawnRequest
    {
        public PlayerController player;
        public int monsterIndex;
        public int routeIndex;
        public int count;
    }
    
    // 디버그 UI
    private void OnGUI()
    {
        if (!Debug.isDebugBuild) return;
        
        GUILayout.BeginArea(new Rect(10, 200, 300, 200));
        GUILayout.Label($"=== 몬스터 소환 현황 ===");
        GUILayout.Label($"전체 활성 몬스터: {activeMonsters.Count}");
        
        foreach (var kvp in playerMonsterCounts)
        {
            if (kvp.Key != null)
            {
                GUILayout.Label($"{kvp.Key.name}: {kvp.Value}/{maxMonsterCount}");
            }
        }
        
        GUILayout.Label($"대기중인 소환: {spawnQueue.Count}");
        GUILayout.EndArea();
    }
}