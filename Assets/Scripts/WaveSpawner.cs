using System.Collections;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    [Header("몬스터 프리팹/부모")]
    public GameObject monsterPrefab;
    public Transform monsterParent;

    [Header("웨이포인트(경로)")]
    public Transform[] pathWaypoints;

    [Header("한 웨이브당 몬스터 수")]
    public int monstersPerWave = 5;

    [Header("몬스터 개별 소환 간격(초)")]
    public float spawnInterval = 1f;

    [Header("웨이브 간 딜레이(초)")]
    public float timeBetweenWaves = 10f;

    [Header("최대 웨이브 수 (기본 50)")]
    public int maxWaves = 50;
    [Header("현재 웨이브 (1..maxWaves)")]
    public int currentWave = 0;

    private bool isSpawning = false;

    [Header("첫 웨이브 자동 시작 딜레이(초)")]
    [SerializeField] private float firstWaveDelay = 10f;

    [Header("웨이브 번호 표시 TMP 텍스트(UI)")]
    [SerializeField] private TextMeshProUGUI waveNumberText;

    [Header("아이템 보상 패널 (옵션)")]
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private ItemRewardPanelManager itemRewardPanel;

    // =========================
    // 성 침투(도달) 카운트
    // =========================
    [Header("성 침투 최대 허용값 (기본 10)")]
    public int infiltrationMax = 10;

    [Header("성 침투 횟수(현재까지)")]
    public int infiltrationCount = 0;

    [SerializeField] private TextMeshProUGUI infiltrationCountText;

    [Header("게임오버/승리 판넬")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    private bool isGameOver = false;
    private bool isVictory = false;

    private void Start()
    {
        if (itemPanel != null) itemPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (infiltrationCountText != null) infiltrationCountText.text = "0";

        StartCoroutine(WaitAndStartFirstWave());
    }

    private IEnumerator WaitAndStartFirstWave()
    {
        yield return new WaitForSeconds(firstWaveDelay);
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (isSpawning) return;
        if (isGameOver || isVictory) return;
        if (currentWave >= maxWaves)
        {
            Debug.Log("[WaveSpawner] 최대 웨이브 도달.");
            return;
        }

        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        currentWave++;

        if (currentWave > maxWaves) yield break;

        if (waveNumberText != null)
        {
            waveNumberText.text = $"Wave {currentWave}";
        }

        // (1) 웨이브 몬스터 소환
        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster();
            yield return new WaitForSeconds(spawnInterval);
        }

        // (2) 몬스터가 모두 없어질 때까지 대기
        while (monsterParent.childCount > 0 && !isGameOver && !isVictory)
        {
            yield return null;
        }

        if (isGameOver || isVictory) yield break;

        // (3) 웨이브 클리어 후 일정 딜레이
        yield return new WaitForSeconds(timeBetweenWaves);
        OnWaveClear();

        isSpawning = false;

        // (4) 아직 게임오버/승리가 아니고 웨이브가 남았다면 다음 웨이브 시작
        if (!isGameOver && !isVictory && currentWave < maxWaves)
        {
            StartNextWave();
        }
        else if (!isGameOver && !isVictory && currentWave >= maxWaves)
        {
            // 최종 웨이브까지 클리어 => 승리
            HandleVictory();
        }
    }

    private void SpawnMonster()
    {
        if (monsterPrefab == null || monsterParent == null) return;

        Vector3 spawnPos = transform.position;
        if (pathWaypoints != null && pathWaypoints.Length > 0)
        {
            spawnPos = pathWaypoints[0].position;
        }

        GameObject mObj = Instantiate(monsterPrefab, monsterParent);
        mObj.transform.position = spawnPos;

        Monster mComp = mObj.GetComponent<Monster>();
        if (mComp != null)
        {
            mComp.pathWaypoints = pathWaypoints;
            mComp.OnReachedCastle += HandleMonsterReachedCastle;
            // mComp.OnDeath => 부활 아군 몬스터는 Monster.cs에서 처리
        }
    }

    /// <summary>
    /// 웨이브 클리어 시(몬스터 전멸 후) → 보상 아이템 패널 표시
    /// </summary>
    private void OnWaveClear()
    {
        Debug.Log($"[WaveSpawner] Wave {currentWave} 클리어!");

        if (itemPanel != null) itemPanel.SetActive(true);
        if (itemRewardPanel != null) itemRewardPanel.ShowRewardPanel();

        if (currentWave >= maxWaves && !isGameOver && !isVictory)
        {
            HandleVictory();
        }
    }

    /// <summary>
    /// 몬스터가 성에 도달했을 때 → 아군이 아니면 침투 카운트 증가
    /// </summary>
    private void HandleMonsterReachedCastle(Monster mon)
    {
        if (mon == null) return;

        if (!mon.isAlly) // 적 몬스터만 침투 카운트
        {
            infiltrationCount++;
            if (infiltrationCountText != null)
            {
                infiltrationCountText.text = infiltrationCount.ToString();
            }

            if (infiltrationCount >= infiltrationMax && !isGameOver && !isVictory)
            {
                isGameOver = true;
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                Debug.Log("[WaveSpawner] Game Over! (몬스터가 성에 과도하게 침투)");
            }
        }
    }

    private void HandleVictory()
    {
        if (!isGameOver)
        {
            isVictory = true;
            if (victoryPanel != null) victoryPanel.SetActive(true);
            Debug.Log("[WaveSpawner] *** Victory! ***");
        }
    }
}
