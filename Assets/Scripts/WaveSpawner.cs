using System.Collections;
using UnityEngine;

/// <summary>
/// 2D 환경에서 몬스터를 웨이브 단위로 스폰하는 예시
/// (캔버스가 아닌, 지정된 "몬스터 부모" 오브젝트(Transform) 아래로 생성되도록 수정)
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("스폰할 몬스터 프리팹")]
    public GameObject monsterPrefab;

    [Header("Monster Parent (몬스터를 이 아래로 생성)")]
    [Tooltip("몬스터를 담을 게임오브젝트 패널(Transform)")]
    public Transform monsterParent;

    [Header("Path Settings")]
    [Tooltip("몬스터가 이동할 웨이포인트들 (2D)")]
    public Transform[] pathWaypoints;

    [Header("Spawn Timing")]
    [Tooltip("웨이브 간 간격(초)")]
    public float timeBetweenWaves = 5f;

    [Tooltip("한 웨이브당 몬스터 수")]
    public int monstersPerWave = 5;

    [Tooltip("몬스터 간 스폰 간격(초)")]
    public float spawnInterval = 1f;

    [Header("Runtime Info")]
    public int currentWave = 0;
    private bool isSpawning = false;

    private void Update()
    {
        // (예시) Space 키로 웨이브 시작
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

    /// <summary>
    /// monstersPerWave만큼 몬스터를 순차적으로 스폰 후, timeBetweenWaves 대기
    /// </summary>
    private IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        currentWave++;

        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster2D();
            yield return new WaitForSeconds(spawnInterval);
        }

        // 한 웨이브 스폰 끝 -> 대기
        yield return new WaitForSeconds(timeBetweenWaves);
        isSpawning = false;
    }

    /// <summary>
    /// 몬스터 한 마리를 스폰
    /// (캔버스 대신 monsterParent(Transform) 밑에 생성 + 웨이포인트 경로 설정)
    /// </summary>
    private void SpawnMonster2D()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("WaveSpawner: monsterPrefab이 없습니다.");
            return;
        }
        if (monsterParent == null)
        {
            Debug.LogWarning("WaveSpawner: monsterParent가 지정되지 않았습니다.");
            return;
        }

        // 첫 웨이포인트 위치를 스폰 지점으로 사용 (없다면 WaveSpawner 자신의 위치)
        Vector3 spawnPos = (pathWaypoints != null && pathWaypoints.Length > 0)
            ? pathWaypoints[0].position
            : transform.position;

        // 몬스터 생성 -> monsterParent 아래로
        GameObject monsterObj = Instantiate(monsterPrefab, monsterParent);
        monsterObj.transform.position = spawnPos;

        // 웨이포인트 경로 설정
        Monster monster = monsterObj.GetComponent<Monster>();
        if (monster != null && pathWaypoints != null && pathWaypoints.Length > 0)
        {
            monster.pathWaypoints = pathWaypoints;
        }
    }
}
