using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.TileBattle;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 전투 매니저 - 전체 전투 시스템 관리
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        private static BattleManager instance;
        public static BattleManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<BattleManager>();
                }
                return instance;
            }
        }
        
        [Header("전투 설정")]
        [SerializeField] private int maxTurns = 36; // 최대 턴 수 (6x3x2 = 36)
        [SerializeField] private float turnTime = 30f; // 턴당 시간 제한
        
        [Header("시스템 참조")]
        [SerializeField] private BattleCharacterPlacement placementSystem;
        [SerializeField] private CharacterSelectUI selectionUI;
        [SerializeField] private GameObject battleUICanvas;
        
        [Header("전투 UI")]
        [SerializeField] private TextMeshProUGUI turnCountText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        
        [Header("AI 설정")]
        [SerializeField] private bool enableAI = true;
        [SerializeField] private float aiThinkTime = 1.5f;
        
        // 전투 상태
        private BattleState currentState = BattleState.Preparation;
        private int currentTurn = 0;
        private float currentTurnTime = 0f;
        private bool isPlayerTurn = true;
        private bool isBattleEnded = false;
        
        // AI용 캐릭터 풀
        private List<CharacterData> aiCharacterPool = new List<CharacterData>();
        
        public enum BattleState
        {
            Preparation,    // 준비 단계
            PlayerTurn,     // 플레이어 턴
            EnemyTurn,      // 적 턴
            BattleEnd       // 전투 종료
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeBattle();
            
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartBattle);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        
        private void Update()
        {
            if (currentState == BattleState.PlayerTurn || currentState == BattleState.EnemyTurn)
            {
                UpdateTimer();
            }
        }
        
        /// <summary>
        /// 전투 초기화
        /// </summary>
        private void InitializeBattle()
        {
            currentTurn = 0;
            isBattleEnded = false;
            isPlayerTurn = true;
            
            // AI 캐릭터 풀 생성
            CreateAICharacterPool();
            
            // UI 초기화
            if (victoryPanel != null)
                victoryPanel.SetActive(false);
            
            UpdateUI();
            
            // 전투 시작
            StartBattle();
        }
        
        /// <summary>
        /// AI 캐릭터 풀 생성
        /// </summary>
        private void CreateAICharacterPool()
        {
            aiCharacterPool.Clear();
            
            // 각 직업별로 AI 캐릭터 생성
            string[] aiNames = { "적군", "몬스터", "마왕군", "악당" };
            JobClass[] jobs = { JobClass.Warrior, JobClass.Knight, JobClass.Wizard, 
                               JobClass.Priest, JobClass.Rogue, JobClass.Sage, 
                               JobClass.Archer, JobClass.Gunner };
            
            foreach (JobClass job in jobs)
            {
                for (int i = 0; i < 3; i++)
                {
                    string name = $"{aiNames[Random.Range(0, aiNames.Length)]} {job}";
                    CharacterData aiChar = new CharacterData(
                        System.Guid.NewGuid().ToString(),
                        name,
                        job,
                        Random.Range(3, 8),
                        CharacterRarity.Common
                    );
                    
                    aiCharacterPool.Add(aiChar);
                }
            }
        }
        
        /// <summary>
        /// 전투 시작
        /// </summary>
        public void StartBattle()
        {
            currentState = BattleState.PlayerTurn;
            currentTurnTime = turnTime;
            Debug.Log("전투 시작! 플레이어 턴입니다.");
        }
        
        /// <summary>
        /// 턴 종료
        /// </summary>
        public void EndTurn()
        {
            currentTurn++;
            
            // 게임 종료 체크
            if (placementSystem.IsGameEnd() || currentTurn >= maxTurns)
            {
                EndBattle();
                return;
            }
            
            // 턴 전환
            isPlayerTurn = !isPlayerTurn;
            currentTurnTime = turnTime;
            
            if (isPlayerTurn)
            {
                currentState = BattleState.PlayerTurn;
                Debug.Log("플레이어 턴입니다.");
            }
            else
            {
                currentState = BattleState.EnemyTurn;
                Debug.Log("적 턴입니다.");
                
                if (enableAI)
                {
                    StartCoroutine(AITurn());
                }
            }
            
            UpdateUI();
        }
        
        /// <summary>
        /// AI 턴 처리
        /// </summary>
        private IEnumerator AITurn()
        {
            yield return new WaitForSeconds(aiThinkTime);
            
            if (currentState != BattleState.EnemyTurn) yield break;
            
            // AI 캐릭터 선택
            CharacterData aiCharacter = aiCharacterPool[Random.Range(0, aiCharacterPool.Count)];
            
            // 배치 가능한 타일 찾기
            List<Tile> availableTiles = GetAvailableTiles();
            
            if (availableTiles.Count > 0)
            {
                // 전략적 타일 선택 (간단한 AI)
                Tile selectedTile = SelectBestTile(availableTiles, aiCharacter);
                
                // 캐릭터 배치
                GameObject characterObj = Instantiate(placementSystem.characterPrefab, 
                    selectedTile.transform.position, Quaternion.identity);
                BattleCharacter battleChar = characterObj.GetComponent<BattleCharacter>();
                battleChar.Initialize(aiCharacter, Tile.Team.Enemy);
                
                selectedTile.PlaceUnit(characterObj, Tile.Team.Enemy);
                
                // 공격 범위 표시
                placementSystem.ShowAttackRange(selectedTile, aiCharacter.GetAttackPattern());
                
                // 점수 업데이트
                placementSystem.UpdateScore(selectedTile, Tile.Team.Enemy);
            }
            
            // 턴 종료
            EndTurn();
        }
        
        /// <summary>
        /// 사용 가능한 타일 목록 가져오기
        /// </summary>
        private List<Tile> GetAvailableTiles()
        {
            List<Tile> tiles = new List<Tile>();
            
            // A 타일 확인
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (!placementSystem.ATiles[x, y].isOccupied)
                        tiles.Add(placementSystem.ATiles[x, y]);
                }
            }
            
            // B 타일 확인
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (!placementSystem.BTiles[x, y].isOccupied)
                        tiles.Add(placementSystem.BTiles[x, y]);
                }
            }
            
            return tiles;
        }
        
        /// <summary>
        /// AI가 최적의 타일 선택 (간단한 전략)
        /// </summary>
        private Tile SelectBestTile(List<Tile> availableTiles, CharacterData character)
        {
            // 간단한 전략: 중앙에 가까운 타일 선호
            Tile bestTile = availableTiles[0];
            float bestScore = float.MinValue;
            
            foreach (Tile tile in availableTiles)
            {
                float score = 0f;
                
                // 중앙 선호 (x = 2.5, y = 1)
                float distanceToCenter = Mathf.Abs(tile.x - 2.5f) + Mathf.Abs(tile.y - 1f);
                score -= distanceToCenter;
                
                // 같은 타일 그룹에 아군이 많으면 가산점
                if (tile.tileType == Tile.TileType.A)
                {
                    score += placementSystem.enemyCharactersInA * 0.5f;
                }
                else
                {
                    score += placementSystem.enemyCharactersInB * 0.5f;
                }
                
                // 원거리 캐릭터는 뒤쪽 선호
                if (character.GetAttackRange() >= 3)
                {
                    score += tile.y * 0.3f;
                }
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }
            
            return bestTile;
        }
        
        /// <summary>
        /// 타이머 업데이트
        /// </summary>
        private void UpdateTimer()
        {
            currentTurnTime -= Time.deltaTime;
            
            if (currentTurnTime <= 0)
            {
                currentTurnTime = 0;
                
                // 시간 초과 시 턴 종료
                if (currentState == BattleState.PlayerTurn)
                {
                    Debug.Log("시간 초과! 턴이 종료됩니다.");
                    EndTurn();
                }
            }
            
            UpdateTimerUI();
        }
        
        /// <summary>
        /// 전투 종료
        /// </summary>
        private void EndBattle()
        {
            if (isBattleEnded) return;
            
            isBattleEnded = true;
            currentState = BattleState.BattleEnd;
            
            string result = placementSystem.GetGameResult();
            ShowResult(result);
        }
        
        /// <summary>
        /// 결과 표시
        /// </summary>
        private void ShowResult(string result)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                
                if (resultText != null)
                {
                    resultText.text = result;
                }
            }
            
            Debug.Log($"전투 종료! 결과: {result}");
        }
        
        /// <summary>
        /// 전투 재시작
        /// </summary>
        private void RestartBattle()
        {
            // 씬 리로드 또는 전투 초기화
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        
        /// <summary>
        /// 메인 메뉴로 돌아가기
        /// </summary>
        private void ReturnToMainMenu()
        {
            // 메인 메뉴 씬으로 이동
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            if (turnCountText != null)
            {
                turnCountText.text = $"턴: {currentTurn + 1}/{maxTurns}";
            }
            
            UpdateTimerUI();
        }
        
        /// <summary>
        /// 타이머 UI 업데이트
        /// </summary>
        private void UpdateTimerUI()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(currentTurnTime / 60);
                int seconds = Mathf.FloorToInt(currentTurnTime % 60);
                timerText.text = $"{minutes:00}:{seconds:00}";
                
                // 시간이 적으면 빨간색으로
                if (currentTurnTime <= 10f)
                {
                    timerText.color = Color.red;
                }
                else
                {
                    timerText.color = Color.white;
                }
            }
        }
        
        /// <summary>
        /// 현재 상태 반환
        /// </summary>
        public BattleState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// 플레이어 턴인지 확인
        /// </summary>
        public bool IsPlayerTurn()
        {
            return currentState == BattleState.PlayerTurn;
        }
    }
}