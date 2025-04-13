// Assets\Scripts\WaveSpawner.cs

using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("몬스터 프리팹")]
    public GameObject monsterPrefab;

    [Tooltip("몬스터가 이동할 웨이포인트(씬에서 참조)")]
    public Transform[] pathWaypoints;

    [Tooltip("웨이브 간 간격(초)")]
    public float timeBetweenWaves = 5f;

    [Tooltip("한 웨이브당 생성할 몬스터 수")]
    public int monstersPerWave = 5;

    [Tooltip("몬스터 생성 간격(초)")]
    public float spawnInterval = 1f;

    [Tooltip("현재 웨이브 번호")]
    public int currentWave = 0;

    private bool isSpawning = false;

    private void Update()
    {
        // 예: Space 키로 다음 웨이브 스폰
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartNextWave();
        }
    }

    /// <summary>
    /// 웨이브 시작
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

        // 웨이포인트가 비었으면 경고
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogWarning("WaveSpawner: pathWaypoints가 비어있습니다!");
        }

        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster();
            yield return new WaitForSeconds(spawnInterval);
        }

        // 한 웨이브 끝
        yield return new WaitForSeconds(timeBetweenWaves);
        isSpawning = false;
    }

    /// <summary>
    /// 몬스터 생성
    /// </summary>
    private void SpawnMonster()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("WaveSpawner: monsterPrefab이 설정되지 않았습니다.");
            return;
        }

        // 웨이포인트[0] 위치에서 스폰 (없으면 자기 transform 위치)
        Vector3 spawnPos = transform.position;
        if (pathWaypoints != null && pathWaypoints.Length > 0)
        {
            spawnPos = pathWaypoints[0].position;
        }

        // 몬스터 인스턴스화
        GameObject monsterObj = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);

        // Monster 스크립트에 웨이포인트 배열 할당
        Monster monster = monsterObj.GetComponent<Monster>();
        if (monster != null && pathWaypoints != null && pathWaypoints.Length > 0)
        {
            monster.pathWaypoints = pathWaypoints;
        }
    }
}
