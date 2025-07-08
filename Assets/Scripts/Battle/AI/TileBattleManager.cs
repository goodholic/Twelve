using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GuildMaster.TileBattle
{
    /// <summary>
    /// 타일 배틀 게임 매니저
    /// 게임 진행, 턴 관리, 승리 조건 체크
    /// </summary>
    public class TileBattleManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private TileBattleAI.AILevel aiDifficulty = TileBattleAI.AILevel.Normal;
        [SerializeField] private float turnDelay = 0.5f;
        
        [Header("UI References")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Transform tileAParent; // A타일 부모
        [SerializeField] private Transform tileBParent; // B타일 부모
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private GameObject characterSelectPanel;
        [SerializeField] private GameObject gameResultPanel;
        
        // 게임 상태
        private TileBattleAI battleAI;
        private TileBattleAI.TileInfo[,] tileA;
        private TileBattleAI.TileInfo[,] tileB;
        private GameObject[,] tileObjectsA;
        private GameObject[,] tileObjectsB;
        
        private List<TileBattleAI.Character> playerCharacters;
        private List<TileBattleAI.Character> aiCharacters;
        private List<TileBattleAI.Character> playerSelectedCharacters;
        private List<TileBattleAI.Character> aiSelectedCharacters;
        
        private int currentTurn = 0; // 0 = 플레이어, 1 = AI
        private int turnCount = 0;
        private bool isGameActive = false;
        private int playerDeployedCount = 0;
        private int aiDeployedCount = 0;
        
        // 상수
        private const int TILE_WIDTH = 6;
        private const int TILE_HEIGHT = 3;
        private const int MAX_CHARACTERS = 10;

        void Start()
        {
            InitializeGame();
        }

        /// <summary>
        /// 게임 초기화
        /// </summary>
        void InitializeGame()
        {
            // AI 초기화
            battleAI = gameObject.AddComponent<TileBattleAI>();
            
            // 타일 초기화
            InitializeTiles();
            
            // 캐릭터 풀 초기화
            InitializeCharacterPools();
            
            // UI 초기화
            UpdateUI();
            
            // 캐릭터 선택 단계 시작
            StartCharacterSelection();
        }

        /// <summary>
        /// 타일 초기화 및 생성
        /// </summary>
        void InitializeTiles()
        {
            tileA = new TileBattleAI.TileInfo[TILE_WIDTH, TILE_HEIGHT];
            tileB = new TileBattleAI.TileInfo[TILE_WIDTH, TILE_HEIGHT];
            tileObjectsA = new GameObject[TILE_WIDTH, TILE_HEIGHT];
            tileObjectsB = new GameObject[TILE_WIDTH, TILE_HEIGHT];
            
            // A타일 생성 (위쪽)
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    tileA[x, y] = new TileBattleAI.TileInfo { 
                        tileIndex = 0, 
                        position = new Vector2Int(x, y) 
                    };
                    
                    Vector3 position = new Vector3(x * 1.1f, 0, y * 1.1f);
                    GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, tileAParent);
                    tile.name = $"TileA_{x}_{y}";
                    tileObjectsA[x, y] = tile;
                    
                    // 타일 클릭 이벤트 추가
                    int capturedX = x;
                    int capturedY = y;
                    tile.GetComponent<TileClickHandler>()?.SetCallback(() => OnTileClicked(0, capturedX, capturedY));
                }
            }
            
            // B타일 생성 (아래쪽)
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    tileB[x, y] = new TileBattleAI.TileInfo { 
                        tileIndex = 1, 
                        position = new Vector2Int(x, y) 
                    };
                    
                    Vector3 position = new Vector3(x * 1.1f, -4, y * 1.1f);
                    GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, tileBParent);
                    tile.name = $"TileB_{x}_{y}";
                    tileObjectsB[x, y] = tile;
                    
                    // 타일 클릭 이벤트 추가
                    int capturedX = x;
                    int capturedY = y;
                    tile.GetComponent<TileClickHandler>()?.SetCallback(() => OnTileClicked(1, capturedX, capturedY));
                }
            }
        }

        /// <summary>
        /// 캐릭터 풀 초기화
        /// </summary>
        void InitializeCharacterPools()
        {
            playerCharacters = CreateCharacterPool(0); // 플레이어 팀
            aiCharacters = CreateCharacterPool(1);     // AI 팀
        }

        /// <summary>
        /// 캐릭터 풀 생성
        /// </summary>
        List<TileBattleAI.Character> CreateCharacterPool(int team)
        {
            var characters = new List<TileBattleAI.Character>();
            
            // 다양한 공격 패턴을 가진 캐릭터들
            var attackPatterns = new Dictionary<string, List<Vector2Int>>
            {
                ["cross"] = new List<Vector2Int> { 
                    new Vector2Int(0, 1), new Vector2Int(0, -1), 
                    new Vector2Int(1, 0), new Vector2Int(-1, 0) 
                },
                ["diagonal"] = new List<Vector2Int> { 
                    new Vector2Int(1, 1), new Vector2Int(1, -1), 
                    new Vector2Int(-1, 1), new Vector2Int(-1, -1) 
                },
                ["surrounding"] = new List<Vector2Int> { 
                    new Vector2Int(0, 1), new Vector2Int(0, -1), 
                    new Vector2Int(1, 0), new Vector2Int(-1, 0),
                    new Vector2Int(1, 1), new Vector2Int(1, -1), 
                    new Vector2Int(-1, 1), new Vector2Int(-1, -1) 
                },
                ["line2"] = new List<Vector2Int> { 
                    new Vector2Int(0, 1), new Vector2Int(0, 2) 
                },
                ["bigCross"] = new List<Vector2Int> { 
                    new Vector2Int(0, 1), new Vector2Int(0, 2),
                    new Vector2Int(0, -1), new Vector2Int(0, -2),
                    new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(-1, 0), new Vector2Int(-2, 0)
                }
            };
            
            // 캐릭터 생성
            characters.Add(new TileBattleAI.Character { 
                id = $"warrior_{team}", name = "전사", 
                attackPower = 10, attackRange = attackPatterns["cross"], team = team 
            });
            
            characters.Add(new TileBattleAI.Character { 
                id = $"mage_{team}", name = "마법사", 
                attackPower = 8, attackRange = attackPatterns["surrounding"], team = team 
            });
            
            characters.Add(new TileBattleAI.Character { 
                id = $"archer_{team}", name = "궁수", 
                attackPower = 7, attackRange = attackPatterns["line2"], team = team 
            });
            
            characters.Add(new TileBattleAI.Character { 
                id = $"knight_{team}", name = "기사", 
                attackPower = 9, attackRange = attackPatterns["cross"], team = team 
            });
            
            characters.Add(new TileBattleAI.Character { 
                id = $"assassin_{team}", name = "암살자", 
                attackPower = 11, attackRange = attackPatterns["diagonal"], team = team 
            });
            
            characters.Add(new TileBattleAI.Character { 
                id = $"priest_{team}", name = "성직자", 
                attackPower = 5, attackRange = attackPatterns["surrounding"], team = team 
            });
            
            characters.Add(new TileBattleAI.Character { 
                id = $"paladin_{team}", name = "성기사", 
                attackPower = 9, attackRange = attackPatterns["bigCross"], team = team 
            });
            
            // 추가 캐릭터들
            for (int i = 2; i <= 5; i++)
            {
                characters.Add(new TileBattleAI.Character { 
                    id = $"soldier_{team}_{i}", name = $"병사{i}", 
                    attackPower = 6 + i, attackRange = attackPatterns["cross"], team = team 
                });
            }
            
            return characters;
        }

        /// <summary>
        /// 캐릭터 선택 단계 시작
        /// </summary>
        void StartCharacterSelection()
        {
            characterSelectPanel.SetActive(true);
            
            // AI 캐릭터 선택
            aiSelectedCharacters = battleAI.SelectCharacters();
            
            // 플레이어 캐릭터 선택 UI 표시
            DisplayCharacterSelection();
        }

        /// <summary>
        /// 캐릭터 선택 UI 표시
        /// </summary>
        void DisplayCharacterSelection()
        {
            // 플레이어가 선택할 수 있는 캐릭터 목록 표시
            // (실제 구현시 UI 프리팹과 연동)
            Debug.Log("플레이어는 10개의 캐릭터를 선택하세요.");
        }

        /// <summary>
        /// 플레이어 캐릭터 선택 완료
        /// </summary>
        public void OnPlayerCharacterSelectionComplete(List<TileBattleAI.Character> selected)
        {
            playerSelectedCharacters = selected;
            characterSelectPanel.SetActive(false);
            
            // 게임 시작
            StartBattle();
        }

        /// <summary>
        /// 전투 시작
        /// </summary>
        void StartBattle()
        {
            isGameActive = true;
            currentTurn = 0; // 플레이어 선공
            turnCount = 0;
            playerDeployedCount = 0;
            aiDeployedCount = 0;
            
            UpdateUI();
            
            // 첫 턴 시작
            if (currentTurn == 0)
            {
                Debug.Log("플레이어 턴입니다. 캐릭터를 배치하세요.");
            }
            else
            {
                StartCoroutine(AITurn());
            }
        }

        /// <summary>
        /// 타일 클릭 처리
        /// </summary>
        void OnTileClicked(int tileIndex, int x, int y)
        {
            if (!isGameActive || currentTurn != 0) return;
            
            var tile = (tileIndex == 0) ? tileA : tileB;
            
            // 빈 타일인지 확인
            if (tile[x, y].occupant != null)
            {
                Debug.Log("이미 캐릭터가 배치된 타일입니다.");
                return;
            }
            
            // 플레이어 캐릭터 배치
            if (playerDeployedCount < playerSelectedCharacters.Count)
            {
                var character = playerSelectedCharacters[playerDeployedCount];
                PlaceCharacter(character, tileIndex, x, y);
                playerDeployedCount++;
                
                // 턴 종료
                EndTurn();
            }
        }

        /// <summary>
        /// 캐릭터 배치
        /// </summary>
        void PlaceCharacter(TileBattleAI.Character character, int tileIndex, int x, int y)
        {
            var tile = (tileIndex == 0) ? tileA : tileB;
            var tileObject = (tileIndex == 0) ? tileObjectsA[x, y] : tileObjectsB[x, y];
            
            tile[x, y].occupant = character;
            
            // 시각적 표시 (캐릭터 스프라이트 또는 3D 모델)
            var visual = tileObject.transform.Find("CharacterVisual");
            if (visual != null)
            {
                visual.gameObject.SetActive(true);
                // 팀 색상 설정
                var renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = (character.team == 0) ? Color.blue : Color.red;
                }
            }
            
            Debug.Log($"{(character.team == 0 ? "플레이어" : "AI")}가 {character.name}을(를) " +
                     $"{(tileIndex == 0 ? "A" : "B")}타일 ({x}, {y})에 배치했습니다.");
        }

        /// <summary>
        /// AI 턴 처리
        /// </summary>
        IEnumerator AITurn()
        {
            yield return new WaitForSeconds(turnDelay);
            
            if (aiDeployedCount < aiSelectedCharacters.Count)
            {
                var character = aiSelectedCharacters[aiDeployedCount];
                
                // AI 결정 대기
                var moveEnumerator = battleAI.MakeMove(character, tileA, tileB);
                while (moveEnumerator.MoveNext())
                {
                    yield return moveEnumerator.Current;
                }
                
                var position = moveEnumerator.Current;
                
                // 위치 디코딩
                int tileIndex = position.x >= 100 ? 1 : 0;
                int x = position.x % 100;
                int y = position.y;
                
                PlaceCharacter(character, tileIndex, x, y);
                aiDeployedCount++;
                
                EndTurn();
            }
        }

        /// <summary>
        /// 턴 종료
        /// </summary>
        void EndTurn()
        {
            turnCount++;
            
            // 모든 캐릭터 배치 완료 확인
            if (playerDeployedCount >= MAX_CHARACTERS && aiDeployedCount >= MAX_CHARACTERS)
            {
                // 전투 단계로 진행
                StartCombatPhase();
                return;
            }
            
            // 턴 교체
            currentTurn = 1 - currentTurn;
            UpdateUI();
            
            if (currentTurn == 1)
            {
                StartCoroutine(AITurn());
            }
            else
            {
                Debug.Log("플레이어 턴입니다. 캐릭터를 배치하세요.");
            }
        }

        /// <summary>
        /// 전투 단계 시작
        /// </summary>
        void StartCombatPhase()
        {
            Debug.Log("배치 완료! 전투를 시작합니다.");
            StartCoroutine(ExecuteCombat());
        }

        /// <summary>
        /// 전투 실행
        /// </summary>
        IEnumerator ExecuteCombat()
        {
            // 모든 캐릭터가 동시에 공격
            yield return new WaitForSeconds(1f);
            
            // A타일 전투
            ExecuteTileCombat(tileA);
            
            // B타일 전투
            ExecuteTileCombat(tileB);
            
            // 전투 애니메이션 대기
            yield return new WaitForSeconds(2f);
            
            // 승리 조건 체크
            CheckVictoryCondition();
        }

        /// <summary>
        /// 타일별 전투 실행
        /// </summary>
        void ExecuteTileCombat(TileBattleAI.TileInfo[,] tile)
        {
            // 모든 캐릭터가 자신의 공격 범위 내 적을 공격
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    var attacker = tile[x, y].occupant;
                    if (attacker != null && attacker.isAlive)
                    {
                        // 공격 범위 내 적 공격
                        foreach (var offset in attacker.attackRange)
                        {
                            var targetPos = new Vector2Int(x, y) + offset;
                            if (IsValidPosition(targetPos))
                            {
                                var target = tile[targetPos.x, targetPos.y].occupant;
                                if (target != null && target.team != attacker.team && target.isAlive)
                                {
                                    // 데미지 처리 (간단한 버전)
                                    Debug.Log($"{attacker.name}이(가) {target.name}을(를) 공격!");
                                    target.isAlive = false;
                                    
                                    // 시각적 효과
                                    UpdateCharacterVisual(tile == tileA ? 0 : 1, targetPos.x, targetPos.y, false);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 캐릭터 시각 효과 업데이트
        /// </summary>
        void UpdateCharacterVisual(int tileIndex, int x, int y, bool isAlive)
        {
            var tileObject = (tileIndex == 0) ? tileObjectsA[x, y] : tileObjectsB[x, y];
            var visual = tileObject.transform.Find("CharacterVisual");
            
            if (visual != null)
            {
                if (!isAlive)
                {
                    // 사망 효과
                    visual.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 승리 조건 체크
        /// </summary>
        void CheckVictoryCondition()
        {
            int playerScore = 0;
            int aiScore = 0;
            
            // A타일 점수 계산
            int playerCountA = 0, aiCountA = 0;
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    var occupant = tileA[x, y].occupant;
                    if (occupant != null && occupant.isAlive)
                    {
                        if (occupant.team == 0) playerCountA++;
                        else aiCountA++;
                    }
                }
            }
            if (playerCountA > aiCountA) playerScore++;
            else if (aiCountA > playerCountA) aiScore++;
            
            // B타일 점수 계산
            int playerCountB = 0, aiCountB = 0;
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    var occupant = tileB[x, y].occupant;
                    if (occupant != null && occupant.isAlive)
                    {
                        if (occupant.team == 0) playerCountB++;
                        else aiCountB++;
                    }
                }
            }
            if (playerCountB > aiCountB) playerScore++;
            else if (aiCountB > playerCountB) aiScore++;
            
            // 게임 종료
            EndGame(playerScore, aiScore);
        }

        /// <summary>
        /// 게임 종료
        /// </summary>
        void EndGame(int playerScore, int aiScore)
        {
            isGameActive = false;
            
            string result;
            if (playerScore > aiScore)
            {
                result = $"플레이어 승리! ({playerScore} vs {aiScore})";
            }
            else if (aiScore > playerScore)
            {
                result = $"AI 승리! ({aiScore} vs {playerScore})";
            }
            else
            {
                result = $"무승부! ({playerScore} vs {aiScore})";
            }
            
            Debug.Log(result);
            
            // 결과 UI 표시
            gameResultPanel.SetActive(true);
            var resultText = gameResultPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (resultText != null)
            {
                resultText.text = result;
            }
            
            // AI 평가 출력
            battleAI.EvaluateGameState();
        }

        /// <summary>
        /// UI 업데이트
        /// </summary>
        void UpdateUI()
        {
            if (turnText != null)
            {
                turnText.text = $"턴: {(currentTurn == 0 ? "플레이어" : "AI")} (턴 {turnCount + 1})";
            }
            
            if (scoreText != null)
            {
                scoreText.text = $"배치: 플레이어 {playerDeployedCount}/{MAX_CHARACTERS} | AI {aiDeployedCount}/{MAX_CHARACTERS}";
            }
        }

        /// <summary>
        /// 유효한 위치 확인
        /// </summary>
        bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < TILE_WIDTH && 
                   position.y >= 0 && position.y < TILE_HEIGHT;
        }

        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            // 기존 타일 제거
            foreach (Transform child in tileAParent)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in tileBParent)
            {
                Destroy(child.gameObject);
            }
            
            // AI 리셋
            battleAI.ResetAI();
            
            // 게임 재초기화
            InitializeGame();
        }
    }

    /// <summary>
    /// 타일 클릭 핸들러
    /// </summary>
    public class TileClickHandler : MonoBehaviour
    {
        private System.Action onClickCallback;
        
        public void SetCallback(System.Action callback)
        {
            onClickCallback = callback;
        }
        
        void OnMouseDown()
        {
            onClickCallback?.Invoke();
        }
    }
}