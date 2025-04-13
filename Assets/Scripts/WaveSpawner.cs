// Assets\Scripts\WaveSpawner.cs

using System.Collections;
using UnityEngine;

/// <summary>
/// 2D 환경에서 몬스터를 웨이브 단위로 스폰.
/// (캔버스 안에서 UI로 표시되길 원하는 경우)
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("몬스터 프리팹(반드시 UI 오브젝트여야 함, Image 등)")]
    public GameObject monsterPrefab;

    [Tooltip("몬스터를 생성할 부모 Canvas (반드시 Hierarchy 상에 존재)")]
    public Canvas monsterCanvas;

    [Tooltip("몬스터 이동 경로(2D) - 실제 이동 로직이 필요하다면 Monster.cs에서 처리")]
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
        // Space 키로 웨이브 시작
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartNextWave();
        }
    }

    /// <summary>
    /// 다음 웨이브 시작
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

        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogWarning("WaveSpawner: pathWaypoints가 비어있습니다! 2D 이동이 불가능합니다.");
        }

        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster2D();
            yield return new WaitForSeconds(spawnInterval);
        }

        yield return new WaitForSeconds(timeBetweenWaves);
        isSpawning = false;
    }

    /// <summary>
    /// 몬스터를 UI 캔버스 하위로 생성(2D UI)
    /// </summary>
    private void SpawnMonster2D()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("WaveSpawner: monsterPrefab이 없습니다.");
            return;
        }

        if (monsterCanvas == null)
        {
            Debug.LogWarning("WaveSpawner: monsterCanvas가 연결되지 않았습니다! (몬스터가 캔버스 밖에 생김)");
            return;
        }

        // 스폰 위치는 pathWaypoints[0] 또는 본인 transform 위치
        Vector3 spawnPos = transform.position;
        if (pathWaypoints != null && pathWaypoints.Length > 0)
        {
            spawnPos = pathWaypoints[0].position;
        }

        // 1) 몬스터를 'monsterCanvas' 하위로 생성
        GameObject monsterObj = Instantiate(monsterPrefab, monsterCanvas.transform);

        // ========== 추가된 코드: 생성된 몬스터를 자식 중 "제일 하단(마지막 순번)"으로 이동 ==========
        monsterObj.transform.SetAsLastSibling();

        // 2) RectTransform 위치를 맞춤 (WorldPosition → Canvas 좌표)
        RectTransform rect = monsterObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            // Canvas가 WorldSpace나 CameraSpace라면 아래처럼 InverseTransformPoint를 통해 위치 설정
            Vector2 localPos = monsterCanvas.transform.InverseTransformPoint(spawnPos);
            rect.anchoredPosition = localPos;
        }

        // 3) Monster 스크립트 연결 (웨이포인트 등)
        Monster monster = monsterObj.GetComponent<Monster>();
        if (monster != null && pathWaypoints != null && pathWaypoints.Length > 0)
        {
            monster.pathWaypoints = pathWaypoints;
        }
    }
}
