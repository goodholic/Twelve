using System.Collections;
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

    [Header("위쪽(상단) 지역 웨이포인트들(Region2)")]
    public Transform[] topWaypointsForAI;

    [Header("텔레포트 스폰 위치(지역1)")]
    [Tooltip("AI가 소환한 지역2 유닛이, 이동 후 지역1로 넘어갈 때 도착하는 위치")]
    public Transform region1TeleportSpawn;

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

    // ================== [수정 추가] 지역2 전용 생명력 ==================
    [Header("지역2 전용 생명력(텍스트)")]
    [SerializeField] private TextMeshProUGUI region2LifeText;
    public int region2Life = 10;

    private void Start()
    {
        // 첫 웨이브 시작 지연
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

            // 스폰이 진행 중이면 대기
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
            SpawnEnemyMonster();
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

    private void SpawnEnemyMonster()
    {
        if (enemyMonsterPrefab == null || enemyMonsterParent == null)
        {
            Debug.LogError("[WaveSpawnerRegion2] 몬스터 프리팹/Parent 미설정");
            return;
        }

        Vector3 spawnPos = transform.position;
        if (topWaypointsForAI != null && topWaypointsForAI.Length > 0 && topWaypointsForAI[0] != null)
        {
            spawnPos = topWaypointsForAI[0].position;
        }

        GameObject enemyObj = Instantiate(enemyMonsterPrefab, enemyMonsterParent);
        enemyObj.transform.position = spawnPos;

        Monster enemyComp = enemyObj.GetComponent<Monster>();
        if (enemyComp != null)
        {
            enemyComp.pathWaypoints = topWaypointsForAI;
            enemyComp.areaIndex = 2;

            Character charComp = enemyObj.GetComponent<Character>();
            if (charComp != null)
            {
                // [수정] AI 몬스터 -> isCharAttack=false, isHero=false
                charComp.isHero = false;
                charComp.isCharAttack = false;

                // 텔레포트 -> region1TeleportSpawn
                charComp.region1TeleportSpawn = region1TeleportSpawn;
            }

            enemyComp.OnDeath += HandleEnemyMonsterDeath;
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
