// Assets\Scripts\WaveSpawner.cs

using System.Collections;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    [Header("몬스터 관련 설정")]
    public GameObject monsterPrefab;
    public Transform monsterParent;
    public Transform[] pathWaypoints;

    // ▼▼ [삭제] 텔레포트 스폰 위치(지역2) 관련 필드/코드 제거
    // [Header("텔레포트 스폰 위치(지역2)")]
    // public Transform region2TeleportSpawn;

    [Header("웨이브/몬스터 설정")]
    public float timeBetweenWaves = 5f;
    public int monstersPerWave = 5;
    public float spawnInterval = 1f;

    private int aliveMonsters = 0;
    public int currentWave = 0;
    private bool isSpawning = false;

    [Header("아이템 패널(웨이브 보상용)")]
    [SerializeField] private GameObject itemPanel;

    [Header("ItemRewardPanelManager")]
    [SerializeField] private ItemRewardPanelManager itemRewardPanel;

    private bool autoStarted = false;

    [Header("Wave Count Text")]
    [SerializeField] private TextMeshProUGUI waveCountText;

    private void Start()
    {
        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }

        StartCoroutine(DelayFirstWaveRoutine(10f));
    }

    private IEnumerator DelayFirstWaveRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartAutoWaveSpawn();
    }

    private void StartAutoWaveSpawn()
    {
        if (!autoStarted)
        {
            autoStarted = true;
            StartCoroutine(AutoWaveRoutine());
        }
    }

    private IEnumerator AutoWaveRoutine()
    {
        while (currentWave < 200)
        {
            StartNextWave();

            while (isSpawning)
            {
                yield return null;
            }
        }
        Debug.Log("[WaveSpawner] 200 wave 전부 완료!");
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

        if (waveCountText != null)
        {
            waveCountText.text = $"Wave : {currentWave}";
        }

        aliveMonsters = monstersPerWave;

        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster();
            yield return new WaitForSeconds(spawnInterval);
        }

        while (aliveMonsters > 0)
        {
            yield return null;
        }

        OnWaveClear();

        yield return new WaitForSeconds(timeBetweenWaves);

        isSpawning = false;
    }

    private void SpawnMonster()
    {
        if (monsterPrefab == null || monsterParent == null)
        {
            Debug.LogError("[WaveSpawner] Monster Prefab/Parent가 설정되지 않음!");
            return;
        }

        Vector3 spawnPos = transform.position;
        if (pathWaypoints != null && pathWaypoints.Length > 0 && pathWaypoints[0] != null)
        {
            spawnPos = pathWaypoints[0].position;
        }
        else
        {
            Debug.LogError("[WaveSpawner] pathWaypoints가 올바르지 않습니다!");
        }

        GameObject mObj = Instantiate(monsterPrefab, monsterParent);
        mObj.transform.position = spawnPos;

        Monster mComp = mObj.GetComponent<Monster>();
        if (mComp != null)
        {
            mComp.areaIndex = 1;
            mComp.pathWaypoints = pathWaypoints;

            // ▼▼ [삭제] 텔레포트 관련 설정 제거
            // Character charComp = mObj.GetComponent<Character>();
            // if (charComp != null)
            // {
            //     charComp.region2TeleportSpawn = region2TeleportSpawn;
            // }

            mComp.OnDeath += HandleMonsterDeath;
        }
    }

    private void HandleMonsterDeath()
    {
        aliveMonsters--;
        if (aliveMonsters < 0) aliveMonsters = 0;
    }

    private void OnWaveClear()
    {
        Debug.Log($"Wave {currentWave} 클리어!");

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
