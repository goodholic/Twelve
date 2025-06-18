using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using pjy.Characters;

namespace pjy.Managers
{
    /// <summary>
    /// 보스 스폰 관리자 - 특정 웨이브에서 보스 몬스터 소환
    /// </summary>
    public class BossSpawnManager : MonoBehaviour
    {
        private static BossSpawnManager instance;
        public static BossSpawnManager Instance => instance;
        
        [Header("보스 프리팹")]
        [SerializeField] private List<GameObject> bossPrefabs = new List<GameObject>();
        [SerializeField] private GameObject defaultBossPrefab;
        
        [Header("보스 스폰 설정")]
        [SerializeField] private int[] bossWaves = { 5, 10, 15, 20 }; // 보스가 나오는 웨이브
        [SerializeField] private float bossSpawnDelay = 2f;
        [SerializeField] private bool spawnBossAlone = true; // 보스만 단독으로 스폰
        
        [Header("챕터별 보스 설정")]
        [SerializeField] private List<ChapterBossConfig> chapterBossConfigs = new List<ChapterBossConfig>();
        
        [Header("보스 알림 UI")]
        [SerializeField] private GameObject bossWarningUIPrefab;
        [SerializeField] private float warningDuration = 3f;
        [SerializeField] private TextMeshProUGUI bossWarningText;
        
        [Header("보스 보상 배율")]
        [SerializeField] private float bossRewardMultiplier = 5f;
        [SerializeField] private int bossDiamondReward = 5;
        
        // 현재 상태
        private bool isBossWave = false;
        private GameObject currentBoss;
        private int currentChapter = 1;
        
        // 이벤트
        public System.Action<GameObject> OnBossSpawned;
        public System.Action<GameObject> OnBossDefeated;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            InitializeChapterBosses();
        }
        
        /// <summary>
        /// 챕터별 보스 초기화
        /// </summary>
        private void InitializeChapterBosses()
        {
            // 챕터별 기본 보스 설정
            if (chapterBossConfigs.Count == 0)
            {
                for (int i = 1; i <= 10; i++)
                {
                    chapterBossConfigs.Add(new ChapterBossConfig
                    {
                        chapterNumber = i,
                        bossName = $"챕터 {i} 보스",
                        healthMultiplier = 1f + (i * 0.5f),
                        damageMultiplier = 1f + (i * 0.3f),
                        specialAbilities = GetChapterAbilities(i)
                    });
                }
            }
        }
        
        /// <summary>
        /// 챕터별 특수 능력 설정
        /// </summary>
        private List<BossAbilityType> GetChapterAbilities(int chapter)
        {
            var abilities = new List<BossAbilityType>();
            
            // 기본 능력
            abilities.Add(BossAbilityType.BasicAttack);
            
            // 챕터별 추가 능력
            if (chapter >= 2) abilities.Add(BossAbilityType.AreaAttack);
            if (chapter >= 3) abilities.Add(BossAbilityType.SummonMinions);
            if (chapter >= 5) abilities.Add(BossAbilityType.SelfBuff);
            if (chapter >= 7) abilities.Add(BossAbilityType.DebuffArea);
            if (chapter >= 9) abilities.Add(BossAbilityType.SpecialPattern);
            
            return abilities;
        }
        
        /// <summary>
        /// 보스 웨이브인지 확인
        /// </summary>
        public bool IsBossWave(int waveNumber)
        {
            return System.Array.Exists(bossWaves, wave => wave == waveNumber);
        }
        
        /// <summary>
        /// 보스 스폰
        /// </summary>
        public void SpawnBoss(int waveNumber, int chapterIndex, Transform[] spawnPoints, int areaIndex)
        {
            if (!IsBossWave(waveNumber)) return;
            
            isBossWave = true;
            currentChapter = chapterIndex;
            
            StartCoroutine(BossSpawnSequence(waveNumber, chapterIndex, spawnPoints, areaIndex));
        }
        
        /// <summary>
        /// 보스 스폰 시퀀스
        /// </summary>
        private IEnumerator BossSpawnSequence(int waveNumber, int chapterIndex, Transform[] spawnPoints, int areaIndex)
        {
            // 보스 경고 표시
            ShowBossWarning(chapterIndex);
            yield return new WaitForSeconds(warningDuration);
            
            // 스폰 지점 선택 (중앙 우선)
            Transform spawnPoint = spawnPoints.Length > 1 ? spawnPoints[1] : spawnPoints[0];
            
            // 보스 프리팹 선택
            GameObject bossPrefab = GetBossPrefab(chapterIndex);
            
            // 보스 생성
            currentBoss = Instantiate(bossPrefab, spawnPoint.position, Quaternion.identity);
            
            // 보스 설정
            var bossMonster = currentBoss.GetComponent<BossMonster>();
            if (bossMonster != null)
            {
                ConfigureBoss(bossMonster, waveNumber, chapterIndex, areaIndex);
                
                // 웨이포인트 설정 (주석 처리 - WaveSpawner.Instance가 없음)
                // var waveSpawner = WaveSpawner.Instance;
                // if (waveSpawner != null)
                // {
                //     var waypoints = waveSpawner.GetWaypointsForRoute(1); // 중앙 경로
                //     bossMonster.SetWaypoints(waypoints);
                // }
            }
            
            // 보스 스폰 이벤트
            OnBossSpawned?.Invoke(currentBoss);
            
            Debug.Log($"[BossSpawnManager] 챕터 {chapterIndex} 보스 스폰! (웨이브 {waveNumber})");
        }
        
        /// <summary>
        /// 보스 프리팹 선택
        /// </summary>
        private GameObject GetBossPrefab(int chapterIndex)
        {
            // 챕터별 전용 보스가 있으면 사용
            if (chapterIndex <= bossPrefabs.Count && bossPrefabs[chapterIndex - 1] != null)
            {
                return bossPrefabs[chapterIndex - 1];
            }
            
            // 기본 보스 프리팹 사용
            return defaultBossPrefab != null ? defaultBossPrefab : bossPrefabs[0];
        }
        
        /// <summary>
        /// 보스 설정
        /// </summary>
        private void ConfigureBoss(BossMonster boss, int waveNumber, int chapterIndex, int areaIndex)
        {
            boss.chapterIndex = chapterIndex;
            boss.areaIndex = areaIndex;
            
            // 챕터별 설정 적용
            if (chapterIndex <= chapterBossConfigs.Count)
            {
                var config = chapterBossConfigs[chapterIndex - 1];
                
                // 스탯 조정
                boss.health *= config.healthMultiplier;
                boss.damageToCastle = Mathf.RoundToInt(boss.damageToCastle * config.damageMultiplier);
            }
            
            // 웨이브별 추가 강화
            float waveMultiplier = 1f + (waveNumber * 0.05f);
            boss.health *= waveMultiplier;
            boss.damageToCastle = Mathf.RoundToInt(boss.damageToCastle * waveMultiplier);
        }
        
        /// <summary>
        /// 보스 경고 표시
        /// </summary>
        private void ShowBossWarning(int chapterIndex)
        {
            if (bossWarningUIPrefab != null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    GameObject warning = Instantiate(bossWarningUIPrefab, canvas.transform);
                    
                    // 경고 텍스트 설정
                    var warningText = warning.GetComponentInChildren<TextMeshProUGUI>();
                    if (warningText != null)
                    {
                        warningText.text = $"경고!\n챕터 {chapterIndex} 보스 접근 중!";
                    }
                    
                    // 애니메이션 후 제거
                    Destroy(warning, warningDuration);
                }
            }
            
            // 사운드 효과 (구현 시)
            // AudioManager.Instance.PlayBossWarningSound();
        }
        
        /// <summary>
        /// 보스 처치 확인
        /// </summary>
        public void CheckBossDefeat()
        {
            if (currentBoss == null && isBossWave)
            {
                OnBossDefeated?.Invoke(null);
                isBossWave = false;
                
                // 추가 보상 지급
                GiveBossDefeatReward();
            }
        }
        
        /// <summary>
        /// 보스 처치 보상
        /// </summary>
        private void GiveBossDefeatReward()
        {
            Debug.Log("[BossSpawnManager] 보스 처치 보상 지급!");
            
            // 모든 플레이어에게 보상
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (!player.IsAI)
                {
                    // 다이아몬드 보상
                    // player.AddDiamonds(bossDiamondReward);
                    
                    // 추가 골드
                    player.AddMinerals(50 * currentChapter);
                }
            }
        }
        
        /// <summary>
        /// 현재 보스 가져오기
        /// </summary>
        public GameObject GetCurrentBoss()
        {
            return currentBoss;
        }
        
        /// <summary>
        /// 보스 웨이브 진행 중인지 확인
        /// </summary>
        public bool IsBossWaveActive()
        {
            return isBossWave && currentBoss != null;
        }
        
        /// <summary>
        /// 보스 강제 소환 (디버그용)
        /// </summary>
        [ContextMenu("Force Spawn Boss")]
        public void ForceSpawnBoss()
        {
            // WaveSpawner.Instance가 없으므로 기본 위치에서 소환
            Transform[] spawnPoints = new Transform[] { transform };
            SpawnBoss(10, 1, spawnPoints, 1);
        }
    }
    
    // 데이터 구조체
    [System.Serializable]
    public class ChapterBossConfig
    {
        public int chapterNumber;
        public string bossName;
        public GameObject bossPrefab;
        public float healthMultiplier = 1f;
        public float damageMultiplier = 1f;
        public float speedMultiplier = 1f;
        public List<BossAbilityType> specialAbilities = new List<BossAbilityType>();
    }
    
    public enum BossAbilityType
    {
        BasicAttack,
        AreaAttack,
        SummonMinions,
        SelfBuff,
        DebuffArea,
        SpecialPattern
    }
}