using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("스폰할 몬스터 프리팹")]
    public GameObject monsterPrefab;

    [Tooltip("몬스터가 이동할 웨이포인트(씬에서 참조)")]
    public Transform[] pathWaypoints;

    [Tooltip("웨이브 간 간격(초)")]
    public float timeBetweenWaves = 5f;

    [Tooltip("한 웨이브에 생성할 몬스터 수")]
    public int monstersPerWave = 5;

    [Tooltip("몬스터 생성 간격(초)")]
    public float spawnInterval = 1f;

    [Tooltip("현재 진행 중인 웨이브 번호")]
    public int currentWave = 0;

    private bool isSpawning = false;

    private void Update()
    {
        // 예시: 키보드 입력으로 다음 웨이브 강제 시작
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartNextWave();
        }
    }

    /// <summary>
    /// 다음 웨이브 스폰 시작
    /// </summary>
    public void StartNextWave()
    {
        if (!isSpawning)
        {
            StartCoroutine(SpawnWaveRoutine());
        }
    }

    private IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        currentWave++;

        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster();
            yield return new WaitForSeconds(spawnInterval);
        }

        // 웨이브 1회 스폰 완료 후 대기
        yield return new WaitForSeconds(timeBetweenWaves);
        isSpawning = false;
    }

    /// <summary>
    /// 몬스터 생성
    /// </summary>
    private void SpawnMonster()
    {
        if (monsterPrefab != null)
        {
            // 스폰 위치는 웨이포인트 첫 번째 지점 등으로 설정(가장 앞 웨이포인트)
            GameObject monsterObj = Instantiate(monsterPrefab, pathWaypoints[0].position, Quaternion.identity);
            Monster monster = monsterObj.GetComponent<Monster>();
            if (monster != null)
            {
                monster.pathWaypoints = pathWaypoints;
            }
        }
    }
}
