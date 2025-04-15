using System.Collections;
using UnityEngine;

/// <summary>
/// 2D 환경에서 몬스터를 웨이브 단위로 스폰하는 예시
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    public GameObject monsterPrefab;
    public Canvas monsterCanvas;
    public Transform[] pathWaypoints;
    public float timeBetweenWaves = 5f;
    public int monstersPerWave = 5;
    public float spawnInterval = 1f;
    public int currentWave = 0;

    private bool isSpawning = false;

    private void Update()
    {
        // Space 키로 웨이브 시작
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartNextWave();
        }
    }

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
            SpawnMonster2D();
            yield return new WaitForSeconds(spawnInterval);
        }

        yield return new WaitForSeconds(timeBetweenWaves);
        isSpawning = false;
    }

    private void SpawnMonster2D()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("WaveSpawner: monsterPrefab이 없습니다.");
            return;
        }
        if (monsterCanvas == null)
        {
            Debug.LogWarning("WaveSpawner: monsterCanvas가 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPos = (pathWaypoints != null && pathWaypoints.Length > 0)
            ? pathWaypoints[0].position
            : transform.position;

        GameObject monsterObj = Instantiate(monsterPrefab, monsterCanvas.transform);
        monsterObj.transform.SetAsLastSibling();

        RectTransform rect = monsterObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector2 localPos = monsterCanvas.transform.InverseTransformPoint(spawnPos);
            rect.anchoredPosition = localPos;
        }

        Monster monster = monsterObj.GetComponent<Monster>();
        if (monster != null && pathWaypoints != null && pathWaypoints.Length > 0)
        {
            monster.pathWaypoints = pathWaypoints;
        }
    }
}
