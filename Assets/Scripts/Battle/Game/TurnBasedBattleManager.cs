using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 턴제 타일 배치 전투를 관리하는 메인 매니저
    /// </summary>
    public class TurnBasedBattleManager : MonoBehaviour
    {
        private static TurnBasedBattleManager _instance;
        public static TurnBasedBattleManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TurnBasedBattleManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("TurnBasedBattleManager");
                        _instance = go.AddComponent<TurnBasedBattleManager>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("전투 설정")]
        [SerializeField] private int maxCharactersPerTeam = 10; // 각 팀당 최대 10개 캐릭터
        [SerializeField] private float turnChangeDelay = 1.5f;
        [SerializeField] private int maxTurns = 20; // 최대 턴 수
        
        [Header("팀 캐릭터 목록")]
        [SerializeField] private List<CharacterData> playerCharacterPool = new List<CharacterData>();
        [SerializeField] private List<CharacterData> enemyCharacterPool = new List<CharacterData>();
        
        [Header("현재 전투 상태")]
        private List<CharacterUnit> playerCharacters = new List<CharacterUnit>();
        private List<CharacterUnit> enemyCharacters = new List<CharacterUnit>();
        private Queue<CharacterUnit> playerDeployQueue = new Queue<CharacterUnit>();
        private Queue<CharacterUnit> enemyDeployQueue = new Queue<CharacterUnit>();
        
        [Header("턴 관리")]
        private int currentTurn = 1;
        private bool isPlayerTurn = true;
        private bool isBattleActive = false;
        private bool isDeploymentPhase = true;
        
        [Header("컴포넌트 참조")]
        [SerializeField] private TileGridManager tileGridManager;
        [SerializeField] private AITurnController aiController;
        [SerializeField] private BattleUIController battleUI;
        
        [Header("캐릭터 프리팹")]
        [SerializeField] private GameObject characterUnitPrefab;
        
        // 이벤트
        public System.Action<int> OnTurnChanged;
        public System.Action<bool> OnTurnSwitched; // true: 플레이어 턴, false: AI 턴
        public System.Action<GameResult, int, int> OnBattleEnd; // 결과, 아군 점수, 적군 점수
        public System.Action OnDeploymentPhaseEnd;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // 컴포넌트 찾기
            if (tileGridManager == null)
                tileGridManager = FindObjectOfType<TileGridManager>();
                
            if (aiController == null)
                aiController = FindObjectOfType<AITurnController>();
                
            if (battleUI == null)
                battleUI = FindObjectOfType<BattleUIController>();
        }
        
        /// <summary>
        /// 전투 시작
        /// </summary>
        public void StartBattle(List<CharacterData> playerTeam, List<CharacterData> enemyTeam)
        {
            if (isBattleActive) return;
            
            // 팀 데이터 설정
            playerCharacterPool = playerTeam.Take(maxCharactersPerTeam).ToList();
            enemyCharacterPool = enemyTeam.Take(maxCharactersPerTeam).ToList();
            
            // 전투 초기화
            InitializeBattle();
        }
        
        /// <summary>
        /// 전투 초기화
        /// </summary>
        void InitializeBattle()
        {
            isBattleActive = true;
            isDeploymentPhase = true;
            currentTurn = 1;
            isPlayerTurn = true;
            
            // 기존 캐릭터 정리
            ClearAllCharacters();
            
            // 타일 그리드 초기화
            if (tileGridManager != null)
            {
                tileGridManager.ResetAllTiles();
            }
            
            // 캐릭터 생성
            CreateCharacters();
            
            // UI 업데이트
            if (battleUI != null)
            {
                battleUI.SetupBattle(playerCharacters, enemyCharacters);
                battleUI.UpdateTurnInfo(currentTurn, isPlayerTurn);
            }
            
            // 배치 페이즈 시작
            StartDeploymentPhase();
        }
        
        /// <summary>
        /// 캐릭터 생성
        /// </summary>
        void CreateCharacters()
        {
            // 플레이어 캐릭터 생성
            foreach (var data in playerCharacterPool)
            {
                GameObject charGO = Instantiate(characterUnitPrefab);
                CharacterUnit unit = charGO.GetComponent<CharacterUnit>();
                
                if (unit == null)
                {
                    unit = charGO.AddComponent<CharacterUnit>();
                }
                
                unit.Initialize(data, Tile.Team.Ally);
                playerCharacters.Add(unit);
                playerDeployQueue.Enqueue(unit);
                
                // 대기 위치로 이동
                charGO.transform.position = GetWaitingPosition(true, playerCharacters.Count - 1);
            }
            
            // AI 캐릭터 생성
            foreach (var data in enemyCharacterPool)
            {
                GameObject charGO = Instantiate(characterUnitPrefab);
                CharacterUnit unit = charGO.GetComponent<CharacterUnit>();
                
                if (unit == null)
                {
                    unit = charGO.AddComponent<CharacterUnit>();
                }
                
                unit.Initialize(data, Tile.Team.Enemy);
                enemyCharacters.Add(unit);
                enemyDeployQueue.Enqueue(unit);
                
                // 대기 위치로 이동
                charGO.transform.position = GetWaitingPosition(false, enemyCharacters.Count - 1);
                
                // AI 컨트롤러에 등록
                if (aiController != null)
                {
                    aiController.RegisterAICharacter(unit);
                }
            }
        }
        
        /// <summary>
        /// 대기 위치 계산
        /// </summary>
        Vector3 GetWaitingPosition(bool isPlayer, int index)
        {
            float x = isPlayer ? -10f : 10f;
            float z = (index - 4.5f) * 1.5f;
            return new Vector3(x, 0, z);
        }
        
        /// <summary>
        /// 배치 페이즈 시작
        /// </summary>
        void StartDeploymentPhase()
        {
            isDeploymentPhase = true;
            
            if (battleUI != null)
            {
                battleUI.ShowDeploymentUI(true);
            }
            
            // 플레이어 턴부터 시작
            StartPlayerTurn();
        }
        
        /// <summary>
        /// 플레이어 턴 시작
        /// </summary>
        void StartPlayerTurn()
        {
            isPlayerTurn = true;
            OnTurnSwitched?.Invoke(true);
            
            if (isDeploymentPhase)
            {
                // 배치할 캐릭터가 있는지 확인
                if (playerDeployQueue.Count > 0)
                {
                    CharacterUnit nextChar = playerDeployQueue.Peek();
                    
                    // UI에 현재 배치할 캐릭터 표시
                    if (battleUI != null)
                    {
                        battleUI.ShowCurrentCharacterToPlace(nextChar);
                    }
                    
                    // 플레이어 입력 대기
                    EnablePlayerInput(true);
                }
                else
                {
                    // 플레이어 배치 완료
                    EndPlayerTurn();
                }
            }
        }
        
        /// <summary>
        /// AI 턴 시작
        /// </summary>
        void StartAITurn()
        {
            isPlayerTurn = false;
            OnTurnSwitched?.Invoke(false);
            
            if (isDeploymentPhase)
            {
                // AI가 배치할 캐릭터 목록
                List<CharacterUnit> charactersToPlace = new List<CharacterUnit>();
                
                while (enemyDeployQueue.Count > 0 && charactersToPlace.Count < 1) // AI는 한 번에 1개씩 배치
                {
                    charactersToPlace.Add(enemyDeployQueue.Dequeue());
                }
                
                if (charactersToPlace.Count > 0)
                {
                    // AI 배치 시작
                    if (aiController != null)
                    {
                        aiController.StartAITurn(charactersToPlace);
                        aiController.OnAITurnEnd += OnAITurnComplete;
                    }
                }
                else
                {
                    // AI 배치 완료
                    EndAITurn();
                }
            }
        }
        
        /// <summary>
        /// 플레이어 입력 활성화
        /// </summary>
        void EnablePlayerInput(bool enable)
        {
            // PlacementManager나 입력 시스템에 신호 전달
            // 타일 클릭 이벤트 활성화/비활성화
        }
        
        /// <summary>
        /// 플레이어가 타일을 선택했을 때
        /// </summary>
        public void OnPlayerSelectTile(Tile selectedTile)
        {
            if (!isPlayerTurn || !isDeploymentPhase) return;
            
            if (playerDeployQueue.Count > 0 && selectedTile.CanPlaceUnit())
            {
                CharacterUnit character = playerDeployQueue.Dequeue();
                
                bool placed = tileGridManager.PlaceCharacter(character, selectedTile);
                
                if (placed)
                {
                    // 배치 성공
                    EnablePlayerInput(false);
                    
                    // 다음 캐릭터가 있으면 계속, 없으면 턴 종료
                    if (playerDeployQueue.Count > 0)
                    {
                        StartCoroutine(NextCharacterDelay());
                    }
                    else
                    {
                        StartCoroutine(EndTurnDelay());
                    }
                }
                else
                {
                    // 배치 실패 - 캐릭터를 큐에 다시 넣기
                    playerDeployQueue.Enqueue(character);
                }
            }
        }
        
        /// <summary>
        /// 다음 캐릭터 배치 대기
        /// </summary>
        IEnumerator NextCharacterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            StartPlayerTurn();
        }
        
        /// <summary>
        /// 턴 종료 대기
        /// </summary>
        IEnumerator EndTurnDelay()
        {
            yield return new WaitForSeconds(turnChangeDelay);
            EndPlayerTurn();
        }
        
        /// <summary>
        /// 플레이어 턴 종료
        /// </summary>
        void EndPlayerTurn()
        {
            EnablePlayerInput(false);
            
            // 배치 페이즈 종료 체크
            if (CheckDeploymentComplete())
            {
                EndDeploymentPhase();
            }
            else
            {
                // AI 턴으로 전환
                StartAITurn();
            }
        }
        
        /// <summary>
        /// AI 턴 종료
        /// </summary>
        void EndAITurn()
        {
            // 배치 페이즈 종료 체크
            if (CheckDeploymentComplete())
            {
                EndDeploymentPhase();
            }
            else
            {
                // 다음 턴으로
                NextTurn();
            }
        }
        
        /// <summary>
        /// AI 턴 완료 콜백
        /// </summary>
        void OnAITurnComplete()
        {
            if (aiController != null)
            {
                aiController.OnAITurnEnd -= OnAITurnComplete;
            }
            
            EndAITurn();
        }
        
        /// <summary>
        /// 배치 완료 체크
        /// </summary>
        bool CheckDeploymentComplete()
        {
            return playerDeployQueue.Count == 0 && enemyDeployQueue.Count == 0;
        }
        
        /// <summary>
        /// 배치 페이즈 종료
        /// </summary>
        void EndDeploymentPhase()
        {
            isDeploymentPhase = false;
            OnDeploymentPhaseEnd?.Invoke();
            
            if (battleUI != null)
            {
                battleUI.ShowDeploymentUI(false);
            }
            
            // 전투 결과 확인
            CheckBattleResult();
        }
        
        /// <summary>
        /// 다음 턴
        /// </summary>
        void NextTurn()
        {
            currentTurn++;
            OnTurnChanged?.Invoke(currentTurn);
            
            if (battleUI != null)
            {
                battleUI.UpdateTurnInfo(currentTurn, !isPlayerTurn);
            }
            
            // 턴 제한 체크
            if (currentTurn > maxTurns)
            {
                // 시간 초과로 전투 종료
                CheckBattleResult();
                return;
            }
            
            // 플레이어 턴으로 시작
            StartPlayerTurn();
        }
        
        /// <summary>
        /// 전투 결과 확인
        /// </summary>
        void CheckBattleResult()
        {
            GameResult result = tileGridManager.CheckVictoryCondition();
            
            // 각 타일의 점수 계산
            int allyScoreA = tileGridManager.GetCharacterCount(Tile.TileType.A, Tile.Team.Ally);
            int enemyScoreA = tileGridManager.GetCharacterCount(Tile.TileType.A, Tile.Team.Enemy);
            int allyScoreB = tileGridManager.GetCharacterCount(Tile.TileType.B, Tile.Team.Ally);
            int enemyScoreB = tileGridManager.GetCharacterCount(Tile.TileType.B, Tile.Team.Enemy);
            
            int allyTotalScore = 0;
            int enemyTotalScore = 0;
            
            // A 타일 점수
            if (allyScoreA > enemyScoreA) allyTotalScore++;
            else if (enemyScoreA > allyScoreA) enemyTotalScore++;
            
            // B 타일 점수
            if (allyScoreB > enemyScoreB) allyTotalScore++;
            else if (enemyScoreB > allyScoreB) enemyTotalScore++;
            
            // 전투 종료
            EndBattle(result, allyTotalScore, enemyTotalScore);
        }
        
        /// <summary>
        /// 전투 종료
        /// </summary>
        void EndBattle(GameResult result, int allyScore, int enemyScore)
        {
            isBattleActive = false;
            
            // UI 업데이트
            if (battleUI != null)
            {
                battleUI.ShowBattleResult(result, allyScore, enemyScore);
            }
            
            // 이벤트 발생
            OnBattleEnd?.Invoke(result, allyScore, enemyScore);
            
            // 보상 처리 등
            ProcessBattleRewards(result);
        }
        
        /// <summary>
        /// 전투 보상 처리
        /// </summary>
        void ProcessBattleRewards(GameResult result)
        {
            switch (result)
            {
                case GameResult.Victory:
                    // 승리 보상
                    Debug.Log("승리! 보상을 획득했습니다.");
                    break;
                    
                case GameResult.Draw:
                    // 무승부 보상
                    Debug.Log("무승부! 일부 보상을 획득했습니다.");
                    break;
                    
                case GameResult.Defeat:
                    // 패배
                    Debug.Log("패배... 다시 도전하세요!");
                    break;
            }
        }
        
        /// <summary>
        /// 모든 캐릭터 제거
        /// </summary>
        void ClearAllCharacters()
        {
            foreach (var character in playerCharacters)
            {
                if (character != null)
                    Destroy(character.gameObject);
            }
            playerCharacters.Clear();
            playerDeployQueue.Clear();
            
            foreach (var character in enemyCharacters)
            {
                if (character != null)
                    Destroy(character.gameObject);
            }
            enemyCharacters.Clear();
            enemyDeployQueue.Clear();
        }
        
        /// <summary>
        /// 현재 턴 가져오기
        /// </summary>
        public int GetCurrentTurn()
        {
            return currentTurn;
        }
        
        /// <summary>
        /// 플레이어 턴인지 확인
        /// </summary>
        public bool IsPlayerTurn()
        {
            return isPlayerTurn;
        }
        
        /// <summary>
        /// 전투 중인지 확인
        /// </summary>
        public bool IsBattleActive()
        {
            return isBattleActive;
        }
        
        /// <summary>
        /// 배치 페이즈인지 확인
        /// </summary>
        public bool IsDeploymentPhase()
        {
            return isDeploymentPhase;
        }
    }
}