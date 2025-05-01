// Assets\Scripts\Network\WaveSpawner.cs

using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro 사용을 위해

public class WaveSpawner : MonoBehaviour
{
    [Header("몬스터 관련 설정")]
    public GameObject monsterPrefab;
    public Transform monsterParent;
    public Transform[] pathWaypoints;

    [Header("웨이브/몬스터 설정")]
    public float timeBetweenWaves = 5f;  // 웨이브 간 대기 시간(두 번째 웨이브부터)
    public int monstersPerWave = 5;      // 웨이브 당 몬스터 수
    public float spawnInterval = 1f;     // 몬스터 개별 소환 간격

    private int aliveMonsters = 0;
    public int currentWave = 0;
    private bool isSpawning = false;

    // ================================
    //  아이템 보상 패널 관련
    // ================================
    [Header("아이템 패널(웨이브 보상용)")]
    [SerializeField] private GameObject itemPanel;

    [Header("ItemRewardPanelManager")]
    [SerializeField] private ItemRewardPanelManager itemRewardPanel;

    // ================================
    //  자동 웨이브 제어
    // ================================
    private bool autoStarted = false;

    [Header("Wave Count Text (현재 웨이브 번호 표시용)")]
    [SerializeField] private TextMeshProUGUI waveCountText;

    private void Start()
    {
        // 아이템 패널 초기 비활성화
        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }

        // 10초 대기 후 첫 웨이브 자동 시작
        StartCoroutine(DelayFirstWaveRoutine(10f));
    }

    /// <summary>
    /// 10초 기다렸다가 자동 웨이브를 시작
    /// </summary>
    private IEnumerator DelayFirstWaveRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartAutoWaveSpawn();
    }

    /// <summary>
    /// 자동 웨이브 루틴 시작
    /// </summary>
    private void StartAutoWaveSpawn()
    {
        if (!autoStarted)
        {
            autoStarted = true;
            StartCoroutine(AutoWaveRoutine());
        }
    }

    /// <summary>
    /// 최대 200웨이브까지 자동 진행
    /// </summary>
    private IEnumerator AutoWaveRoutine()
    {
        while (currentWave < 200)
        {
            StartNextWave();

            // 현재 웨이브의 스폰이 끝날 때까지 대기
            while (isSpawning)
            {
                yield return null;
            }
        }

        Debug.Log("[WaveSpawner] 200 wave 전부 완료!");
    }

    /// <summary>
    /// 외부(버튼)에서 수동으로도 웨이브 시작 가능
    /// </summary>
    public void StartNextWave()
    {
        if (!isSpawning)
        {
            StartCoroutine(SpawnWaveRoutine());
        }
    }

    /// <summary>
    /// 웨이브 소환 루틴
    /// </summary>
    private IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        currentWave++;

        if (waveCountText != null)
        {
            waveCountText.text = $"Wave : {currentWave}";
        }
        else
        {
            Debug.LogWarning("[WaveSpawner] waveCountText가 null이라 Wave 번호 표시 불가!");
        }

        aliveMonsters = monstersPerWave;

        // 몬스터 소환
        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster();
            yield return new WaitForSeconds(spawnInterval);
        }

        // 해당 웨이브의 모든 몬스터가 죽을 때까지 대기
        while (aliveMonsters > 0)
        {
            yield return null;
        }

        // 웨이브 클리어 시점
        OnWaveClear();

        // 다음 웨이브 전 대기
        yield return new WaitForSeconds(timeBetweenWaves);

        isSpawning = false;
    }

    /// <summary>
    /// 몬스터 1마리를 소환
    /// </summary>
    private void SpawnMonster()
    {
        if (monsterPrefab == null || monsterParent == null)
        {
            Debug.LogError("[WaveSpawner] Monster Prefab 또는 Parent가 설정되지 않음!");
            return;
        }

        Vector3 spawnPos = transform.position;
        if (pathWaypoints != null && pathWaypoints.Length > 0 && pathWaypoints[0] != null)
        {
            spawnPos = pathWaypoints[0].position;
        }
        else
        {
            Debug.LogError("[WaveSpawner] pathWaypoints가 올바르게 설정되지 않았습니다!");
        }

        GameObject mObj = Instantiate(monsterPrefab, monsterParent);
        mObj.transform.position = spawnPos;

        Monster mComp = mObj.GetComponent<Monster>();
        if (mComp != null)
        {
            // 적 몬스터로 간주
            mComp.isAlly = false;
            mComp.OnDeath += HandleMonsterDeath;
            mComp.pathWaypoints = pathWaypoints;
        }
    }

    private void HandleMonsterDeath()
    {
        aliveMonsters--;
        if (aliveMonsters < 0) aliveMonsters = 0;
    }

    /// <summary>
    /// 웨이브 클리어 처리
    /// </summary>
    private void OnWaveClear()
    {
        Debug.Log($"Wave {currentWave} 클리어!");

        // === [수정] 5웨이브마다 아이템 패널 표시 ===
        if (currentWave % 5 == 0)
        {
            if (itemPanel != null)
            {
                itemPanel.SetActive(true);
            }

            if (itemRewardPanel != null)
            {
                itemRewardPanel.ShowRewardPanel();
            }
        }
    }
}
