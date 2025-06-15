using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    [Header("몬스터 관련 설정")]
    public GameObject monsterPrefab;
    public Transform monsterParent;
    
    [Header("몬스터 이동 경로 (3라인)")]
    [Tooltip("좌측 루트 몬스터 웨이포인트")]
    public Transform[] monsterWaypointsLeft;
    [Tooltip("중앙 루트 몬스터 웨이포인트")]
    public Transform[] monsterWaypointsCenter;
    [Tooltip("우측 루트 몬스터 웨이포인트")]
    public Transform[] monsterWaypointsRight;
    
    [Header("몬스터 루트별 스폰 위치")]
    [Tooltip("좌측 루트 몬스터 스폰 위치")]
    public Transform monsterLeftSpawnPoint;
    [Tooltip("중앙 루트 몬스터 스폰 위치")]
    public Transform monsterCenterSpawnPoint;
    [Tooltip("우측 루트 몬스터 스폰 위치")]
    public Transform monsterRightSpawnPoint;
    
    [Header("캐릭터 공격 루트 설정 (좌/중/우)")]
    [Tooltip("좌측 루트 웨이포인트")]
    public Transform[] walkableLeft;
    [Tooltip("중앙 루트 웨이포인트")]
    public Transform[] walkableCenter;
    [Tooltip("우측 루트 웨이포인트")]
    public Transform[] walkableRight;
    
    [Header("캐릭터 루트별 스폰 위치")]
    [Tooltip("좌측 루트 스폰 위치")]
    public Transform leftSpawnPoint;
    [Tooltip("중앙 루트 스폰 위치")]
    public Transform centerSpawnPoint;
    [Tooltip("우측 루트 스폰 위치")]
    public Transform rightSpawnPoint;

    [Header("중간성/최종성 참조")]
    [Tooltip("좌측 중간성(체력 500)")]
    public GameObject leftMiddleCastle;
    [Tooltip("중앙 중간성(체력 500)")]
    public GameObject centerMiddleCastle;
    [Tooltip("우측 중간성(체력 500)")]
    public GameObject rightMiddleCastle;
    [Tooltip("최종성(체력 1000)")]
    public GameObject finalCastle;

    [Header("웨이브/몬스터 설정")]
    public float timeBetweenWaves = 5f;
    public int monstersPerWave = 5;
    public float spawnInterval = 0.2f; // 동시 생성을 위해 간격 축소

    private int aliveMonsters = 0;
    public int currentWave = 0;
    private bool isSpawning = false;

    private bool autoStarted = false;

    [Header("아이템 패널(웨이브 보상용)")]
    [SerializeField] private GameObject itemPanel;

    [Header("Wave Count Text")]
    [SerializeField] private TextMeshProUGUI waveCountText;
    
    [Header("5웨이브 보상 시스템")]
    [SerializeField] private GameObject rewardSelectionPanel;
    [SerializeField] private Transform rewardButtonParent;
    [SerializeField] private StarMergeDatabaseObject starMergeDatabase;

    [Header("[수정추가] 챕터별 몬스터 교체 옵션")]
    [Tooltip("true면 1챕터~2챕터, 2챕터~3챕터... 등으로 넘어갈 때 5마리 웨이브 중 3번째만 '다음 챕터 몬스터'를 소환")]
    public bool useChapterMonsterLogic = true;

    [Tooltip("현재 챕터 (1이면 1챕터, 2면 2챕터 등)")]
    public int currentChapter = 1;

    [Tooltip("2챕터 몬스터 프리팹(1챕터일 때, 5마리 중 3번째만 이것 사용)")]
    public GameObject chapter2MonsterPrefab;

    [Tooltip("3챕터 몬스터 프리팹(2챕터일 때, 5마리 중 3번째만 이것 사용)")]
    public GameObject chapter3MonsterPrefab;

    [Header("[수정추가] 1~101 챕터 몬스터 프리팹 배열")]
    [Tooltip("예: index=0 => 1챕터 몬스터, index=1 => 2챕터 몬스터, ..., index=100 => 101챕터 몬스터")]
    public GameObject[] chapterMonsters = new GameObject[101];

    private void Start()
    {
        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }
        
        if (rewardSelectionPanel != null)
        {
            rewardSelectionPanel.SetActive(false);
        }

        // 중간성/최종성 체력 설정
        SetupCastleHealth();

        // 최초에 10초 딜레이 후 자동 웨이브 시작
        StartCoroutine(DelayFirstWaveRoutine(10f));
    }

    /// <summary>
    /// 중간성 체력 500, 최종성 체력 1000 설정
    /// </summary>
    private void SetupCastleHealth()
    {
        // 중간성 체력 설정
        if (leftMiddleCastle != null)
        {
            var middleCastle = leftMiddleCastle.GetComponent<MiddleCastle>();
            if (middleCastle != null)
            {
                middleCastle.maxHealth = 500;
                middleCastle.currentHealth = 500;
                Debug.Log($"[WaveSpawner] 좌측 중간성 체력 500으로 설정");
            }
        }
        
        if (centerMiddleCastle != null)
        {
            var middleCastle = centerMiddleCastle.GetComponent<MiddleCastle>();
            if (middleCastle != null)
            {
                middleCastle.maxHealth = 500;
                middleCastle.currentHealth = 500;
                Debug.Log($"[WaveSpawner] 중앙 중간성 체력 500으로 설정");
            }
        }
        
        if (rightMiddleCastle != null)
        {
            var middleCastle = rightMiddleCastle.GetComponent<MiddleCastle>();
            if (middleCastle != null)
            {
                middleCastle.maxHealth = 500;
                middleCastle.currentHealth = 500;
                Debug.Log($"[WaveSpawner] 우측 중간성 체력 500으로 설정");
            }
        }
        
        // 최종성 체력 설정
        if (finalCastle != null)
        {
            var finalCastleComp = finalCastle.GetComponent<FinalCastle>();
            if (finalCastleComp != null)
            {
                finalCastleComp.maxHealth = 1000;
                finalCastleComp.currentHealth = 1000;
                Debug.Log("[WaveSpawner] 최종성 체력 1000으로 설정");
            }
        }
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
            while (isSpawning || aliveMonsters > 0)
            {
                yield return null;
            }
            
            // 다음 웨이브 전 대기 시간
            yield return new WaitForSeconds(timeBetweenWaves);
        }
        Debug.Log("[WaveSpawner] 200 wave 전부 완료!");
    }

    public void StartNextWave()
    {
        if (!isSpawning && aliveMonsters == 0)
        {
            Debug.Log($"[WaveSpawner] StartNextWave() 호출 -> currentWave={currentWave+1}");
            StartCoroutine(SpawnWaveRoutine());
        }
        else
        {
            Debug.Log($"[WaveSpawner] 웨이브 시작 불가 - isSpawning: {isSpawning}, aliveMonsters: {aliveMonsters}");
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

        Debug.Log($"[WaveSpawner] Wave {currentWave} 시작 - 몬스터 {monstersPerWave}마리 소환 예정");

        // 5마리를 3라인에 분배
        List<int> spawnRoutes = GetSpawnDistribution(monstersPerWave);

        for (int i = 0; i < monstersPerWave; i++)
        {
            SpawnMonster(i, spawnRoutes[i]);
            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;
        
        // 몬스터가 모두 죽을 때까지 대기
        while (aliveMonsters > 0)
        {
            yield return null;
        }

        OnWaveClear();
        
        Debug.Log($"[WaveSpawner] Wave {currentWave} 완전 종료");
    }

    /// <summary>
    /// 5마리를 3라인에 분배하는 로직
    /// </summary>
    private List<int> GetSpawnDistribution(int monsterCount)
    {
        List<int> distribution = new List<int>();
        
        // 기본 분배: 2-1-2 (좌측2, 중앙1, 우측2)
        if (monsterCount == 5)
        {
            distribution.Add(0); // 좌측
            distribution.Add(0); // 좌측
            distribution.Add(1); // 중앙
            distribution.Add(2); // 우측
            distribution.Add(2); // 우측
        }
        else
        {
            // 다른 경우는 균등 분배
            for (int i = 0; i < monsterCount; i++)
            {
                distribution.Add(i % 3);
            }
        }
        
        return distribution;
    }

    /// <summary>
    /// 몬스터용 루트에 따른 웨이포인트 배열 반환
    /// </summary>
    private Transform[] GetMonsterWaypointsForRoute(int route)
    {
        Transform[] waypoints = null;
        
        switch (route)
        {
            case 0:
                waypoints = monsterWaypointsLeft;
                break;
            case 1:
                waypoints = monsterWaypointsCenter;
                break;
            case 2:
                waypoints = monsterWaypointsRight;
                break;
            default:
                waypoints = monsterWaypointsCenter;
                break;
        }
        
        if (waypoints != null && waypoints.Length > 0)
        {
            List<Transform> validWaypoints = new List<Transform>();
            
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    validWaypoints.Add(waypoints[i]);
                }
            }
            
            if (validWaypoints.Count > 0)
            {
                return validWaypoints.ToArray();
            }
        }
        
        Debug.LogWarning($"[WaveSpawner] 몬스터 {route} 루트의 웨이포인트가 없습니다!");
        return null;
    }

    /// <summary>
    /// 몬스터용 스폰 위치 반환
    /// </summary>
    private Vector3 GetMonsterSpawnPosition(int route)
    {
        Transform spawnPoint = null;
        
        switch (route)
        {
            case 0:
                spawnPoint = monsterLeftSpawnPoint;
                break;
            case 1:
                spawnPoint = monsterCenterSpawnPoint;
                break;
            case 2:
                spawnPoint = monsterRightSpawnPoint;
                break;
        }

        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        
        // 스폰 포인트가 없으면 해당 루트의 첫 웨이포인트 사용
        Transform[] waypoints = GetMonsterWaypointsForRoute(route);
        if (waypoints != null && waypoints.Length > 0)
        {
            return waypoints[0].position;
        }

        return transform.position;
    }

    private void SpawnMonster(int indexInWave, int routeIndex)
    {
        if (monsterPrefab == null || monsterParent == null)
        {
            Debug.LogError("[WaveSpawner] monsterPrefab/monsterParent가 설정되지 않아 몬스터 소환 불가");
            aliveMonsters--;  // 생성 실패 시 카운트 감소
            return;
        }

        // 선택된 루트의 웨이포인트 가져오기
        Transform[] selectedWaypoints = GetMonsterWaypointsForRoute(routeIndex);
        if (selectedWaypoints == null || selectedWaypoints.Length == 0)
        {
            Debug.LogError($"[WaveSpawner] 몬스터 {routeIndex} 루트의 웨이포인트가 없습니다!");
            aliveMonsters--;  // 생성 실패 시 카운트 감소
            return;
        }

        // 스폰 위치
        Vector3 spawnPos = GetMonsterSpawnPosition(routeIndex);

        // 챕터별 몬스터 선택 로직
        GameObject prefabToSpawn = monsterPrefab;
        int monsterChapter = currentChapter;

        if (useChapterMonsterLogic)
        {
            if (chapterMonsters != null && chapterMonsters.Length >= 101)
            {
                if (indexInWave == 2 && currentChapter < 101 && chapterMonsters[currentChapter] != null)
                {
                    prefabToSpawn = chapterMonsters[currentChapter];
                    monsterChapter = currentChapter + 1;
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
                if (indexInWave == 2)
                {
                    if (currentChapter == 1 && chapter2MonsterPrefab != null)
                    {
                        prefabToSpawn = chapter2MonsterPrefab;
                        monsterChapter = 2;
                    }
                    else if (currentChapter == 2 && chapter3MonsterPrefab != null)
                    {
                        prefabToSpawn = chapter3MonsterPrefab;
                        monsterChapter = 3;
                    }
                }
            }
        }

        // 몬스터 생성
        GameObject monsterObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, monsterParent);
        Monster monster = monsterObj.GetComponent<Monster>();

        if (monster != null)
        {
            // 몬스터 설정
            monster.pathWaypoints = selectedWaypoints;
            monster.currentChapter = monsterChapter;
            monster.areaIndex = 1;  // Area 1로 설정
            
            // 라인 정보 설정
            RouteType routeType = RouteType.Center;
            switch (routeIndex)
            {
                case 0: routeType = RouteType.Left; break;
                case 1: routeType = RouteType.Center; break;
                case 2: routeType = RouteType.Right; break;
            }
            monster.SetMonsterRoute(routeType);
            
            // 이벤트 구독 - 이것이 누락되어 있었음!
            monster.OnDeath += HandleMonsterDeath;
            monster.OnReachedCastle += HandleMonsterReachedCastle;
            
            Debug.Log($"[WaveSpawner] 몬스터 생성 완료 - 위치: {spawnPos}, 루트: {routeType}, 챕터: {monsterChapter}");
        }
        else
        {
            Debug.LogError("[WaveSpawner] 생성된 몬스터에 Monster 컴포넌트가 없습니다!");
            aliveMonsters--;  // 생성 실패 시 카운트 감소
        }
    }

    private void HandleMonsterDeath()
    {
        aliveMonsters--;
        Debug.Log($"[WaveSpawner] Monster 사망 -> aliveMonsters={aliveMonsters}");

        if (aliveMonsters < 0)
        {
            aliveMonsters = 0;
        }
    }

    private void HandleMonsterReachedCastle(Monster monster)
    {
        Debug.Log($"[WaveSpawner] Monster가 성에 도달함");
        // 성에 도달한 몬스터도 죽은 것으로 처리되므로 별도 처리 불필요
    }

    private void OnWaveClear()
    {
        Debug.Log($"[WaveSpawner] Wave {currentWave} Clear!");

        // 5웨이브마다 보상
        if (currentWave % 5 == 0)
        {
            ShowRewardSelection();
        }
    }
    
    private void ShowRewardSelection()
    {
        if (rewardSelectionPanel == null)
        {
            Debug.LogWarning("[WaveSpawner] 보상 선택 패널이 설정되지 않았습니다!");
            return;
        }
        
        if (starMergeDatabase == null)
        {
            Debug.LogWarning("[WaveSpawner] StarMergeDatabase가 설정되지 않았습니다!");
            return;
        }
        
        // 기존 보상 버튼들 찾기 (이미 씬에 있는 버튼들)
        UnityEngine.UI.Button[] existingButtons = rewardButtonParent.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        
        // 랜덤 2성 캐릭터 3개 선택
        List<CharacterData> twoStarCharacters = new List<CharacterData>();
        
        // 모든 종족의 2성 캐릭터 수집
        RaceType[] allRaces = { RaceType.Human, RaceType.Orc, RaceType.Elf };
        foreach (var race in allRaces)
        {
            var twoStarData = starMergeDatabase.GetRandom2Star(race);
            if (twoStarData != null)
            {
                twoStarCharacters.Add(twoStarData);
            }
        }
        
        // 중복 없이 3개 선택
        List<CharacterData> selectedRewards = new List<CharacterData>();
        while (selectedRewards.Count < 3 && twoStarCharacters.Count > 0)
        {
            int randomIndex = Random.Range(0, twoStarCharacters.Count);
            selectedRewards.Add(twoStarCharacters[randomIndex]);
            twoStarCharacters.RemoveAt(randomIndex);
        }
        
        // 버튼에 보상 정보 설정
        for (int i = 0; i < existingButtons.Length && i < selectedRewards.Count; i++)
        {
            int index = i;  // 클로저를 위한 로컬 변수
            CharacterData reward = selectedRewards[i];
            
            // 버튼 이벤트 설정
            existingButtons[i].onClick.RemoveAllListeners();
            existingButtons[i].onClick.AddListener(() => OnRewardSelected(reward));
            
            // 버튼 텍스트 업데이트 (버튼 하위에 Text 컴포넌트가 있다고 가정)
            var buttonText = existingButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"{reward.characterName} (2★)";
            }
            
            existingButtons[i].gameObject.SetActive(true);
        }
        
        // 사용하지 않는 버튼은 비활성화
        for (int i = selectedRewards.Count; i < existingButtons.Length; i++)
        {
            existingButtons[i].gameObject.SetActive(false);
        }
        
        // 보상 패널 표시
        rewardSelectionPanel.SetActive(true);
        
        // 게임 일시정지 (선택적)
        Time.timeScale = 0f;
    }
    
    private void OnRewardSelected(CharacterData selectedCharacter)
    {
        Debug.Log($"[WaveSpawner] 보상 선택됨: {selectedCharacter.characterName}");
        
        // TODO: 선택된 캐릭터를 플레이어의 인벤토리나 덱에 추가하는 로직
        // 예: PlayerInventory.Instance.AddCharacter(selectedCharacter);
        
        // 보상 패널 숨기기
        if (rewardSelectionPanel != null)
        {
            rewardSelectionPanel.SetActive(false);
        }
        
        // 게임 재개
        Time.timeScale = 1f;
    }
}