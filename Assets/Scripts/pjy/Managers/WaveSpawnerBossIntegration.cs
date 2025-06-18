using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using pjy.UI;
using pjy.Characters;

namespace pjy.Managers
{
    /// <summary>
    /// WaveSpawner와 BossSpawnManager를 연동하는 확장 클래스
    /// 기존 WaveSpawner의 기능을 유지하면서 보스 시스템 추가
    /// </summary>
    public class WaveSpawnerBossIntegration : MonoBehaviour
    {
        [Header("보스 시스템")]
        [SerializeField] private bool enableBossSystem = true;
        [SerializeField] private BossSpawnManager bossSpawnManager;
        [SerializeField] private GameObject bossHealthBarUIPrefab;
        [SerializeField] private Transform bossHealthBarParent;
        
        // 참조
        private WaveSpawner waveSpawner;
        private GameManager gameManager;
        private BossHealthBarUI currentBossHealthBar;
        
        // 상태
        private bool isBossWave = false;
        private bool bossDefeated = false;
        
        private void Awake()
        {
            // WaveSpawner 컴포넌트 찾기
            waveSpawner = GetComponent<WaveSpawner>();
            if (waveSpawner == null)
            {
                Debug.LogError("[WaveSpawnerBossIntegration] WaveSpawner 컴포넌트를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
            
            // BossSpawnManager 찾기
            if (bossSpawnManager == null)
            {
                bossSpawnManager = FindFirstObjectByType<BossSpawnManager>();
                if (bossSpawnManager == null && enableBossSystem)
                {
                    // BossSpawnManager가 없으면 생성
                    GameObject bossManagerObj = new GameObject("BossSpawnManager");
                    bossSpawnManager = bossManagerObj.AddComponent<BossSpawnManager>();
                }
            }
        }
        
        private void Start()
        {
            gameManager = GameManager.Instance;
            
            if (bossSpawnManager != null)
            {
                // 보스 이벤트 구독
                bossSpawnManager.OnBossSpawned += OnBossSpawned;
                bossSpawnManager.OnBossDefeated += OnBossDefeated;
            }
        }
        
        private void OnDestroy()
        {
            if (bossSpawnManager != null)
            {
                // 이벤트 구독 해제
                bossSpawnManager.OnBossSpawned -= OnBossSpawned;
                bossSpawnManager.OnBossDefeated -= OnBossDefeated;
            }
        }
        
        /// <summary>
        /// 웨이브 시작 전 보스 웨이브 체크
        /// </summary>
        public bool CheckAndHandleBossWave(int waveNumber)
        {
            if (!enableBossSystem || bossSpawnManager == null) return false;
            
            if (bossSpawnManager.IsBossWave(waveNumber))
            {
                isBossWave = true;
                bossDefeated = false;
                StartCoroutine(BossWaveSequence(waveNumber));
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 보스 웨이브 시퀀스
        /// </summary>
        private IEnumerator BossWaveSequence(int waveNumber)
        {
            Debug.Log($"[WaveSpawnerBossIntegration] 보스 웨이브 {waveNumber} 시작!");
            
            // 보스 스폰 위치 설정
            Transform[] spawnPoints = GetBossSpawnPoints();
            int areaIndex = 1; // 기본값 사용
            int chapterIndex = gameManager != null ? 1 : 1; // 기본값 사용
            
            // 보스 스폰
            bossSpawnManager.SpawnBoss(waveNumber, chapterIndex, spawnPoints, areaIndex);
            
            // 보스가 처치될 때까지 대기
            while (!bossDefeated && bossSpawnManager.IsBossWaveActive())
            {
                yield return new WaitForSeconds(0.5f);
                
                // 보스 체력 체크
                var currentBoss = bossSpawnManager.GetCurrentBoss();
                if (currentBoss == null)
                {
                    bossDefeated = true;
                }
            }
            
            // 보스 처치 완료
            Debug.Log($"[WaveSpawnerBossIntegration] 보스 웨이브 {waveNumber} 완료!");
            isBossWave = false;
            
            // 다음 웨이브로 진행 (주석 처리 - EndWave 메서드가 없음)
            // if (gameManager != null)
            // {
            //     gameManager.EndWave();
            // }
        }
        
        /// <summary>
        /// 보스 스폰 위치 가져오기
        /// </summary>
        private Transform[] GetBossSpawnPoints()
        {
            List<Transform> spawnPoints = new List<Transform>();
            
            // WaveSpawner의 스폰 포인트 사용
            if (waveSpawner.monsterLeftSpawnPoint != null)
                spawnPoints.Add(waveSpawner.monsterLeftSpawnPoint);
                
            if (waveSpawner.monsterCenterSpawnPoint != null)
                spawnPoints.Add(waveSpawner.monsterCenterSpawnPoint);
                
            if (waveSpawner.monsterRightSpawnPoint != null)
                spawnPoints.Add(waveSpawner.monsterRightSpawnPoint);
            
            // 스폰 포인트가 없으면 WaveSpawner 위치 사용
            if (spawnPoints.Count == 0)
            {
                spawnPoints.Add(waveSpawner.transform);
            }
            
            return spawnPoints.ToArray();
        }
        
        /// <summary>
        /// 보스 스폰 시 호출
        /// </summary>
        private void OnBossSpawned(GameObject boss)
        {
            if (boss == null) return;
            
            // 보스 체력바 UI 생성
            CreateBossHealthBar(boss);
            
            // 보스 웨이포인트 설정
            var bossMonster = boss.GetComponent<BossMonster>();
            if (bossMonster != null)
            {
                // 중앙 경로 웨이포인트 사용
                Transform[] waypoints = GetWaypointsForBoss();
                bossMonster.SetWaypoints(waypoints);
            }
            
            // 화면 효과 (선택사항)
            // CameraShake.Instance.Shake(0.5f, 0.3f);
        }
        
        /// <summary>
        /// 보스 처치 시 호출
        /// </summary>
        private void OnBossDefeated(GameObject boss)
        {
            bossDefeated = true;
            
            // 보스 체력바 제거
            if (currentBossHealthBar != null)
            {
                Destroy(currentBossHealthBar.gameObject);
                currentBossHealthBar = null;
            }
            
            // 축하 메시지
            ShowBossDefeatMessage();
        }
        
        /// <summary>
        /// 보스 체력바 생성
        /// </summary>
        private void CreateBossHealthBar(GameObject boss)
        {
            if (bossHealthBarUIPrefab == null) return;
            
            // UI 부모 찾기
            if (bossHealthBarParent == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    bossHealthBarParent = canvas.transform;
                }
            }
            
            if (bossHealthBarParent != null)
            {
                GameObject healthBarObj = Instantiate(bossHealthBarUIPrefab, bossHealthBarParent);
                currentBossHealthBar = healthBarObj.GetComponent<BossHealthBarUI>();
                
                if (currentBossHealthBar != null)
                {
                    var bossMonster = boss.GetComponent<BossMonster>();
                    currentBossHealthBar.SetBoss(bossMonster);
                }
            }
        }
        
        /// <summary>
        /// 보스용 웨이포인트 가져오기
        /// </summary>
        private Transform[] GetWaypointsForBoss()
        {
            // 중앙 경로 웨이포인트 사용
            if (waveSpawner.monsterWaypointsCenter != null && waveSpawner.monsterWaypointsCenter.Length > 0)
            {
                return waveSpawner.monsterWaypointsCenter;
            }
            
            // 대체 경로
            if (waveSpawner.monsterWaypointsLeft != null && waveSpawner.monsterWaypointsLeft.Length > 0)
            {
                return waveSpawner.monsterWaypointsLeft;
            }
            
            return new Transform[0];
        }
        
        /// <summary>
        /// 보스 처치 메시지 표시
        /// </summary>
        private void ShowBossDefeatMessage()
        {
            // TODO: 화면 중앙에 축하 메시지 표시
            Debug.Log("[WaveSpawnerBossIntegration] 보스 처치! 축하합니다!");
        }
        
        /// <summary>
        /// 일반 몬스터 스폰 가능 여부
        /// </summary>
        public bool CanSpawnRegularMonsters()
        {
            return !isBossWave || !enableBossSystem;
        }
        
        /// <summary>
        /// 보스 웨이브 진행 중인지 확인
        /// </summary>
        public bool IsBossWaveActive()
        {
            return isBossWave && !bossDefeated;
        }
    }
}