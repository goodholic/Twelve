// Assets\Scripts\WaveSpawner.cs

using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro 사용을 위해

public class WaveSpawner : MonoBehaviour
{
    public GameObject monsterPrefab;
    public Transform monsterParent;
    public Transform[] pathWaypoints;

    public float timeBetweenWaves = 5f;
    public int monstersPerWave = 5;
    public float spawnInterval = 1f;

    public int currentWave = 0;
    private bool isSpawning = false;
    private int aliveMonsters = 0;

    // ================================
    //    보상 패널 + 아이템 로직
    // ================================
    [Header("아이템 패널(보상용)")]
    [SerializeField] private GameObject itemPanel;  // ← 이전에는 LobbySceneManager가 가지고 있던 필드를 WaveSpawner로 이동
    
    [Header("ItemRewardPanelManager 스크립트")]
    [SerializeField] private ItemRewardPanelManager itemRewardPanel; 
    // (만약 itemRewardPanel에 Awake()에서 gameObject.SetActive(false) 하는 코드를 사용)

    // (추가) 자동 웨이브 스타트 플래그
    private bool autoStarted = false;

    // (추가) 현재 웨이브 번호를 표시할 TextMeshProUGUI
    [Header("Wave Count Text (현재 웨이브 번호 표시용)")]
    [SerializeField] private TextMeshProUGUI waveCountText;

    private void Start()
    {
        // 아이템 패널을 초기에 비활성화
        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }

        // (추가) 10초 뒤부터 자동 웨이브 스폰 시작
        Invoke(nameof(StartAutoWaveSpawn), 10f);
    }

    private void Update()
    {
        // 테스트용 : Space 키로 웨이브 수동 시작(디버그)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartNextWave();
        }
    }

    // (추가) 10초 후에 실행될 메서드 → 200웨이브까지만 자동으로 돌린다
    private void StartAutoWaveSpawn()
    {
        if (!autoStarted)
        {
            autoStarted = true;
            StartCoroutine(AutoWaveRoutine());
        }
    }

    // (추가) 200웨이브까지만 반복
    private IEnumerator AutoWaveRoutine()
    {
        while (currentWave < 200)
        {
            StartNextWave();

            // StartNextWave() 내에서 isSpawning이 true가 되고, 
            // SpawnWaveRoutine이 끝나면 다시 false.
            // => 그 사이(웨이브 진행 중)는 대기
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

        // (추가) 웨이브 번호 텍스트 갱신
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

        // 전부 죽을 때까지 대기
        while (aliveMonsters > 0)
        {
            yield return null;
        }

        // 웨이브 클리어 시점
        OnWaveClear();

        yield return new WaitForSeconds(timeBetweenWaves);
        isSpawning = false;
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
            // 몬스터 사망 시 이벤트
            mComp.OnDeath += HandleMonsterDeath;
            mComp.pathWaypoints = pathWaypoints;
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

        // ------------------------------
        //   아이템 패널 활성화
        // ------------------------------
        if (itemPanel != null)
        {
            itemPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[WaveSpawner] itemPanel이 null이어서 보상창을 열 수 없음!");
        }

        // itemRewardPanel(스크립트)도 ShowRewardPanel() 호출
        if (itemRewardPanel != null)
        {
            itemRewardPanel.ShowRewardPanel();
        }
        else
        {
            Debug.LogWarning("[WaveSpawner] itemRewardPanel이 null!");
        }
    }
}
