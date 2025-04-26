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

    [Header("웨이브 딜레이(몬스터 전멸/퇴장 후 + 몇 초 뒤에 다음 웨이브)")]
    [Tooltip("기본 10초 (유저 요구사항)")]
    public float timeBetweenWaves = 10f;

    [Header("최대 웨이브 수 (기본 50)")]
    public int maxWaves = 50;  // 1~50웨이브까지만

    [Header("현재 웨이브 (1..maxWaves)")]
    public int currentWave = 0;

    // 이미 웨이브를 스폰 중인 상태인지 여부
    private bool isSpawning = false;

    [Header("첫 웨이브 자동 시작 딜레이(초)")]
    [SerializeField] private float firstWaveDelay = 10f;

    [Header("웨이브 번호 표시 TMP 텍스트(UI)")]
    [SerializeField] private TextMeshProUGUI waveNumberText;

    [Header("아이템 패널(웨이브 보상용) + 보상매니저")]
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private ItemRewardPanelManager itemRewardPanel;

    // ==========================
    // 성 침투 시 게임오버 처리
    // ==========================
    [Header("성 침투(도달) 카운트 관련")]
    [Tooltip("성에 들어온(마지막 웨이포인트 도달) 몬스터 수")]
    public int infiltrationCount = 0;

    [Tooltip("몇 마리가 들어오면 게임오버인지 (기본 10)")]
    public int infiltrationMax = 10;

    [Tooltip("성 침투수를 표시할 TextMeshProUGUI (옵션)")]
    [SerializeField] private TextMeshProUGUI infiltrationCountText;

    [Header("게임오버 UI(예: Panel)")]
    [SerializeField] private GameObject gameOverPanel;

    // ==========================
    // (추가) 승리 UI
    // ==========================
    [Header("승리 UI(예: Panel)")]
    [SerializeField] private GameObject victoryPanel;

    // 내부 상태
    private bool isGameOver = false;
    private bool isVictory = false;  // 승리 시 true

    private void Start()
    {
        // 아이템 패널 초기에 비활성
        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }

        // 게임오버 / 승리 UI도 초기 비활성
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        // 성 침투 카운트 UI 초기화
        if (infiltrationCountText != null)
        {
            infiltrationCountText.text = "0";
        }

        // 첫 웨이브 자동 시작
        StartCoroutine(WaitAndStartFirstWave());
    }

    /// <summary>
    /// 첫 웨이브 시작 전, firstWaveDelay초 대기
    /// </summary>
    private IEnumerator WaitAndStartFirstWave()
    {
        yield return new WaitForSeconds(firstWaveDelay);
        StartNextWave();
    }

    /// <summary>
    /// 외부(버튼 등)에서 "다음 웨이브 시작"을 요청
    /// </summary>
    public void StartNextWave()
    {
        if (isSpawning) return;    // 이미 스폰 중이면 무시
        if (isGameOver || isVictory) return;    // 게임오버 or 승리 상태면 중단
        if (currentWave >= maxWaves)
        {
            Debug.Log("[WaveSpawner] 더 이상 웨이브가 없습니다 (최대 웨이브 도달).");
            return;
        }

        StartCoroutine(SpawnWaveRoutine());
    }

    /// <summary>
    /// 실제 웨이브를 스폰하고, 몬스터가 없어진 뒤 10초 뒤( timeBetweenWaves ) 다음 웨이브로 이동
    /// </summary>
    private IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        currentWave++;

        // 혹시 현재웨이브가 maxWaves 초과면 종료
        if (currentWave > maxWaves)
        {
            yield break;
        }

        // 웨이브 번호 텍스트 갱신
        if (waveNumberText != null)
        {
            waveNumberText.text = $"Wave {currentWave}";
        }

        // (1) 몬스터를 monstersPerWave만큼 순차 소환
        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster();
            yield return new WaitForSeconds(spawnInterval);
        }

        // (2) 몬스터 패널( monsterParent )의 자식이 없어질 때까지 대기
        //     (모두 죽였든, 침투로 사라졌든, childCount == 0 이면 다음 단계)
        while (monsterParent.childCount > 0 && !isGameOver && !isVictory)
        {
            yield return null;
        }

        // 게임오버 or 승리 시 중단
        if (isGameOver || isVictory)
        {
            yield break;
        }

        // (3) 몬스터가 0마리가 된 시점에서 10초 뒤 -> OnWaveClear() 처리
        yield return new WaitForSeconds(timeBetweenWaves);
        OnWaveClear();

        // (4) 스폰 종료
        isSpawning = false;

        // (5) 아직 게임오버/승리가 아니고, 웨이브 최대치도 아니면 다음 웨이브 시작
        if (!isGameOver && !isVictory && currentWave < maxWaves)
        {
            StartNextWave();
        }
        else if (!isGameOver && !isVictory && currentWave >= maxWaves)
        {
            // 여기까지 오면 사실상 최종 웨이브 클리어
            HandleVictory();
        }
    }

    /// <summary>
    /// 몬스터 1마리 스폰
    /// </summary>
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
            // 성 도달 시 이벤트
            mComp.OnReachedCastle += HandleMonsterReachedCastle;

            // 사망 시 이벤트
            mComp.OnDeath += HandleMonsterDeath;

            // 웨이포인트 연결
            mComp.pathWaypoints = pathWaypoints;

            // 웨이브가 올라갈수록 체력 1.2배씩
            mComp.health *= Mathf.Pow(1.2f, (currentWave - 1));
        }
    }

    /// <summary>
    /// 몬스터가 죽었을 때(코드상 별도 처리 필요 없지만, 혹시 로그용)
    /// </summary>
    private void HandleMonsterDeath()
    {
        // 여기서는 별도 처리 없음 (코루틴에서 monsterParent.childCount로만 감시)
    }

    /// <summary>
    /// 몬스터가 성(마지막 웨이포인트)에 도달 => infiltrationCount++
    /// => infiltrationMax 이상이면 게임오버
    /// </summary>
    private void HandleMonsterReachedCastle()
    {
        infiltrationCount++;
        if (infiltrationCountText != null)
        {
            infiltrationCountText.text = infiltrationCount.ToString();
        }

        if (infiltrationCount >= infiltrationMax && !isGameOver && !isVictory)
        {
            // 패배
            isGameOver = true;
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            Debug.Log("[WaveSpawner] Game Over! 몬스터가 성에 너무 많이 도달함.");
        }
    }

    /// <summary>
    /// 웨이브 클리어 처리 -> 아이템 보상 패널 열기 등
    /// </summary>
    private void OnWaveClear()
    {
        Debug.Log($"[WaveSpawner] Wave {currentWave} 클리어!");

        // 아이템 보상
        if (itemPanel != null)
        {
            itemPanel.SetActive(true);
        }
        if (itemRewardPanel != null)
        {
            itemRewardPanel.ShowRewardPanel();
        }

        // 만약 지금이 마지막 웨이브였다면 승리 처리
        if (currentWave >= maxWaves && !isGameOver && !isVictory)
        {
            HandleVictory();
        }
    }

    /// <summary>
    /// (추가) 최종 승리 처리
    /// </summary>
    private void HandleVictory()
    {
        // 이미 GameOver 상태가 아닌 경우 -> 승리
        if (!isGameOver)
        {
            isVictory = true;
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
            Debug.Log("[WaveSpawner] *** Victory! ***");
        }
    }
}
