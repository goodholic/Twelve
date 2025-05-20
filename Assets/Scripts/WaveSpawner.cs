using System.Collections;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    [Header("몬스터 관련 설정")]
    public GameObject monsterPrefab;
    public Transform monsterParent;
    public Transform[] pathWaypoints;

    [Header("웨이브/몬스터 설정")]
    public float timeBetweenWaves = 5f;
    public int monstersPerWave = 5;
    public float spawnInterval = 1f;

    private int aliveMonsters = 0;
    public int currentWave = 0;
    private bool isSpawning = false;

    private bool autoStarted = false;

    [Header("아이템 패널(웨이브 보상용)")]
    [SerializeField] private GameObject itemPanel;

    [Header("ItemRewardPanelManager")]
    [SerializeField] private ItemRewardPanelManager itemRewardPanel;

    [Header("Wave Count Text")]
    [SerializeField] private TextMeshProUGUI waveCountText;

    // =============================
    // [수정추가] 챕터별 몬스터 교체 로직
    // =============================
    [Header("[수정추가] 챕터별 몬스터 교체 옵션")]
    [Tooltip("true면 1챕터~2챕터, 2챕터~3챕터... 등으로 넘어갈 때 5마리 웨이브 중 3번째만 '다음 챕터 몬스터'를 소환")]
    public bool useChapterMonsterLogic = false;

    [Tooltip("현재 챕터 (1이면 1챕터, 2면 2챕터 등)")]
    public int currentChapter = 1;

    [Tooltip("2챕터 몬스터 프리팹(1챕터일 때, 5마리 중 3번째만 이것 사용)")]
    public GameObject chapter2MonsterPrefab;

    [Tooltip("3챕터 몬스터 프리팹(2챕터일 때, 5마리 중 3번째만 이것 사용)")]
    public GameObject chapter3MonsterPrefab;

    // =========================================
    // [수정추가] 1~101 챕터 몬스터 프리팹 배열
    // (인스펙터에서 101개를 세팅)
    // =========================================
    [Header("[수정추가] 1~101 챕터 몬스터 프리팹 배열")]
    [Tooltip("예: index=0 => 1챕터 몬스터, index=1 => 2챕터 몬스터, ..., index=100 => 101챕터 몬스터")]
    public GameObject[] chapterMonsters = new GameObject[101];
    // =========================================

    private void Start()
    {
        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }

        // 최초에 10초 딜레이 후 자동 웨이브 시작
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
        // 최대 200웨이브까지만
        while (currentWave < 200)
        {
            StartNextWave();

            // 현재 웨이브가 끝날 때까지 대기
            while (isSpawning)
            {
                yield return null;
            }
        }
        Debug.Log("[WaveSpawner] 200 wave 전부 완료!");
    }

    /// <summary>
    /// (버튼 등에서) 다음 웨이브 수동 시작
    /// </summary>
    public void StartNextWave()
    {
        // 이미 스폰 중이면 중복 실행 방지
        if (!isSpawning)
        {
            Debug.Log($"[WaveSpawner] StartNextWave() 호출 -> currentWave={currentWave+1}");
            StartCoroutine(SpawnWaveRoutine());
        }
        else
        {
            Debug.Log("[WaveSpawner] 이미 웨이브 스폰 중입니다.");
        }
    }

    /// <summary>
    /// 웨이브 스폰 루틴 (몬스터 n마리 생성 후, 전부 죽어야 끝)
    /// </summary>
    private IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        currentWave++;

        if (waveCountText != null)
        {
            waveCountText.text = $"Wave : {currentWave}";
        }

        // 이번 웨이브에 등장하는 몬스터 수
        aliveMonsters = monstersPerWave;

        Debug.Log($"[WaveSpawner] Wave {currentWave} 시작 - 몬스터 {monstersPerWave}마리 소환 예정");

        // n마리 몬스터 순차 스폰
        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster(i);
            yield return new WaitForSeconds(spawnInterval);
        }

        // 몬스터가 전부 처치될 때까지 대기
        while (aliveMonsters > 0)
        {
            yield return null;
        }

        // 웨이브 클리어 시점
        OnWaveClear();

        yield return new WaitForSeconds(timeBetweenWaves);

        isSpawning = false;
        Debug.Log($"[WaveSpawner] Wave {currentWave} 종료 -> 다음 웨이브로 넘어감");
    }

    /// <summary>
    /// (수정) 몬스터 소환 로직에 indexInWave 추가 + 챕터별 로직
    /// </summary>
    private void SpawnMonster(int indexInWave)
    {
        if (monsterPrefab == null || monsterParent == null)
        {
            Debug.LogError("[WaveSpawner] monsterPrefab/monsterParent가 설정되지 않아 몬스터 소환 불가");
            return;
        }

        // 초기 스폰 위치 (pathWaypoints의 0번지점)
        Vector3 spawnPos = transform.position;
        if (pathWaypoints != null && pathWaypoints.Length > 0 && pathWaypoints[0] != null)
        {
            spawnPos = pathWaypoints[0].position;
        }
        else
        {
            Debug.LogError("[WaveSpawner] pathWaypoints가 제대로 설정되지 않았습니다!");
        }

        // -------------- [수정추가] 챕터 로직 --------------
        GameObject prefabToSpawn = monsterPrefab; // 기본값
        int monsterChapter = currentChapter;      // 현재 챕터

        if (useChapterMonsterLogic)
        {
            // (1) 미리 배열(chapterMonsters)이 101개 설정된 경우
            if (chapterMonsters != null && chapterMonsters.Length >= 101)
            {
                // 웨이브 내 3번째 몬스터(index=2)는 다음 챕터 몬스터 사용(단, currentChapter <101)
                if (indexInWave == 2 && currentChapter < 101 && chapterMonsters[currentChapter] != null)
                {
                    prefabToSpawn = chapterMonsters[currentChapter];
                    monsterChapter = currentChapter + 1; // 다음 챕터 스탯
                }
                else
                {
                    int curIdx = currentChapter - 1;
                    if (curIdx >= 0 && curIdx < chapterMonsters.Length && chapterMonsters[curIdx] != null)
                    {
                        prefabToSpawn = chapterMonsters[curIdx];
                    }
                }
            }
            else
            {
                // (2) 배열이 제대로 세팅 안됐으면, 추가로 아래 2/3챕터 프리팹 대체
                // 웨이브 내 3번째 몬스터만 다음 챕터
                if (indexInWave == 2)
                {
                    if (currentChapter == 1 && chapter2MonsterPrefab != null)
                    {
                        prefabToSpawn = chapter2MonsterPrefab;
                        monsterChapter = 2; // 2챕터
                    }
                    else if (currentChapter == 2 && chapter3MonsterPrefab != null)
                    {
                        prefabToSpawn = chapter3MonsterPrefab;
                        monsterChapter = 3; // 3챕터
                    }
                    else if (currentChapter >= 3 && chapter3MonsterPrefab != null)
                    {
                        prefabToSpawn = chapter3MonsterPrefab;
                        monsterChapter = currentChapter + 1; // 이후 챕터
                    }
                }
            }
        }
        // -------------------------------------

        // 실제 스폰
        GameObject mObj = Instantiate(prefabToSpawn, monsterParent);
        // === [수정] 혹시라도 비활성화된 상태로 생성되면 활성화 ===
        mObj.SetActive(true);

        mObj.transform.position = spawnPos;

        // Monster 컴포넌트 설정
        Monster mComp = mObj.GetComponent<Monster>();
        if (mComp != null)
        {
            mComp.areaIndex = 1; // 지역1
            mComp.pathWaypoints = pathWaypoints;
            mComp.OnDeath += HandleMonsterDeath;

            // 챕터 설정
            mComp.currentChapter = monsterChapter;

            Debug.Log($"[WaveSpawner] Spawned Monster indexInWave={indexInWave}, chapter={monsterChapter}, Wave={currentWave}");
        }
        else
        {
            Debug.LogWarning("[WaveSpawner] 스폰된 프리팹에 Monster 컴포넌트가 없음");
        }
    }

    private void HandleMonsterDeath()
    {
        aliveMonsters--;
        // [디버그용 로그 추가]
        Debug.Log($"[WaveSpawner] Monster 사망 -> aliveMonsters={aliveMonsters}");

        if (aliveMonsters < 0)
        {
            aliveMonsters = 0;
        }
    }

    private void OnWaveClear()
    {
        Debug.Log($"[WaveSpawner] Wave {currentWave} Clear!");

        // 5웨이브마다 보상
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
