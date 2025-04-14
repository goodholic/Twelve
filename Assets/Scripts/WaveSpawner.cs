using System.Collections;
using UnityEngine;

/// <summary>
/// 2D 환경에서 몬스터를 웨이브 단위로 스폰.
/// (10×10 타일은 별도로 TileGridEditor를 통해 설정. 이 스크립트에선 pathWaypoints만 사용)
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("몬스터 프리팹(반드시 UI 오브젝트여야 함, Image 등)")]
    public GameObject monsterPrefab;

    [Tooltip("몬스터를 생성할 부모 Canvas (반드시 Hierarchy 상에 존재)")]
    public Canvas monsterCanvas;

    [Tooltip("몬스터 이동 경로(2D). pathWaypoints가 비어있으면 WaveSpawner 자신의 위치에서 스폰")]
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
        // 예시: Space 키로 웨이브 시작
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartNextWave();
        }
    }

    /// <summary>
    /// 외부에서 호출 가능: 다음 웨이브 시작
    /// </summary>
    public void StartNextWave()
    {
        if (!isSpawning)
        {
            StartCoroutine(SpawnWaveRoutine());
        }
    }

    /// <summary>
    /// 웨이브 진행 코루틴
    /// </summary>
    private IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        currentWave++;

        // monstersPerWave만큼 몬스터를 순차 생성
        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster2D();
            yield return new WaitForSeconds(spawnInterval);
        }

        // 웨이브 간 대기
        yield return new WaitForSeconds(timeBetweenWaves);
        isSpawning = false;
    }

    /// <summary>
    /// 몬스터를 UI(Canvas) 하위로 생성
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
            Debug.LogWarning("WaveSpawner: monsterCanvas가 연결되지 않았습니다! (몬스터가 캔버스 밖에 생성될 수 있음)");
            return;
        }

        // pathWaypoints가 유효하면 첫 Waypoint, 없으면 WaveSpawner 자신의 위치
        Vector3 spawnPos = (pathWaypoints != null && pathWaypoints.Length > 0)
            ? pathWaypoints[0].position
            : transform.position;

        // 1) 몬스터를 'monsterCanvas' 하위로 생성
        GameObject monsterObj = Instantiate(monsterPrefab, monsterCanvas.transform);

        // UI상에서 가장 뒤(마지막)에 오도록 정렬
        monsterObj.transform.SetAsLastSibling();

        // 2) RectTransform 위치를 맞춤 (Canvas 좌표계 기준)
        RectTransform rect = monsterObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector2 localPos = monsterCanvas.transform.InverseTransformPoint(spawnPos);
            rect.anchoredPosition = localPos;
        }

        // 3) Monster 스크립트가 있다면 pathWaypoints 설정
        Monster monster = monsterObj.GetComponent<Monster>();
        if (monster != null && pathWaypoints != null && pathWaypoints.Length > 0)
        {
            monster.pathWaypoints = pathWaypoints;
        }
    }
}
