using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GuildMaster.Data;
using GuildMaster.Systems;
using GuildMaster.Game;
using TMPro;
using CharacterData = GuildMaster.Data.CharacterData;

namespace GuildMaster.Core
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        // 게임 모드 상태
        public enum GameMode
        {
            CharacterSelection,  // 캐릭터 선택 단계
            Battle,             // 배틀 진행 중
            Result              // 결과 화면
        }

        // 현재 게임 모드
        private GameMode _currentMode = GameMode.CharacterSelection;
        public GameMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    GameMode previousMode = _currentMode;
                    _currentMode = value;
                    OnGameModeChanged?.Invoke(previousMode, _currentMode);
                }
            }
        }

        // 게임 설정
        [Header("Game Settings")]
        public int maxSelectableCharacters = 10;  // 선택 가능한 최대 캐릭터 수
        public float turnTimeLimit = 30f;         // 턴 제한 시간
        public int boardWidth = 6;                // 보드 가로 크기
        public int boardHeight = 3;               // 보드 세로 크기

        // 선택된 캐릭터 목록
        public List<CharacterData> playerSelectedCharacters = new List<CharacterData>();
        public List<CharacterData> enemySelectedCharacters = new List<CharacterData>();

        // 현재 게임 상태
        [Header("Game State")]
        public bool isPlayerTurn = true;
        public int currentTurn = 0;
        public float remainingTurnTime;
        public bool isGameOver = false;

        // 점수 관리
        [Header("Score Management")]
        public int playerScore = 0;
        public int enemyScore = 0;

        // 타일 보드 상태 (A구역: 0, B구역: 1)
        public TileState[,,] boardState = new TileState[2, 6, 3]; // [구역, x, y]

        // 캐릭터 배치 정보
        public Dictionary<Vector3Int, PlacedCharacter> placedCharacters = new Dictionary<Vector3Int, PlacedCharacter>();

        // 이벤트
        public event Action<GameMode, GameMode> OnGameModeChanged;
        public event Action<bool> OnTurnChanged;  // bool: isPlayerTurn
        public event Action<int, int> OnScoreChanged;  // playerScore, enemyScore
        public event Action OnGameOver;
        public event Action OnCharacterPlaced;

        // 타일 상태
        public enum TileState
        {
            Empty,
            PlayerControlled,
            EnemyControlled,
            Contested  // 양쪽이 공격 중인 상태
        }

        // 배치된 캐릭터 정보
        [System.Serializable]
        public class PlacedCharacter
        {
            public CharacterData characterData;
            public bool isPlayerCharacter;
            public Vector3Int position;  // z: 0=A구역, 1=B구역
            public List<Vector3Int> attackRange;

            public PlacedCharacter(CharacterData data, bool isPlayer, Vector3Int pos)
            {
                characterData = data;
                isPlayerCharacter = isPlayer;
                position = pos;
                attackRange = new List<Vector3Int>();
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeGame();
        }

        void InitializeGame()
        {
            // 보드 초기화
            for (int area = 0; area < 2; area++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    for (int y = 0; y < boardHeight; y++)
                    {
                        boardState[area, x, y] = TileState.Empty;
                    }
                }
            }

            // 캐릭터 목록 초기화
            playerSelectedCharacters.Clear();
            enemySelectedCharacters.Clear();
            placedCharacters.Clear();

            // 점수 초기화
            playerScore = 0;
            enemyScore = 0;
            
            // 게임 상태 초기화
            isPlayerTurn = true;
            currentTurn = 0;
            isGameOver = false;
            remainingTurnTime = turnTimeLimit;
        }

        // 캐릭터 선택 추가
        public bool AddSelectedCharacter(CharacterData character, bool isPlayer)
        {
            List<CharacterData> targetList = isPlayer ? playerSelectedCharacters : enemySelectedCharacters;
            
            // 중복 체크
            if (targetList.Contains(character))
            {
                Debug.LogWarning("이미 선택된 캐릭터입니다.");
                return false;
            }

            // 최대 개수 체크
            if (targetList.Count >= maxSelectableCharacters)
            {
                Debug.LogWarning("더 이상 캐릭터를 선택할 수 없습니다.");
                return false;
            }

            targetList.Add(character);
            return true;
        }

        // 캐릭터 선택 제거
        public bool RemoveSelectedCharacter(CharacterData character, bool isPlayer)
        {
            List<CharacterData> targetList = isPlayer ? playerSelectedCharacters : enemySelectedCharacters;
            return targetList.Remove(character);
        }

        // 게임 시작
        public void StartBattle()
        {
            if (playerSelectedCharacters.Count != maxSelectableCharacters ||
                enemySelectedCharacters.Count != maxSelectableCharacters)
            {
                Debug.LogError("양쪽 모두 10개의 캐릭터를 선택해야 합니다.");
                return;
            }

            CurrentMode = GameMode.Battle;
            isPlayerTurn = UnityEngine.Random.Range(0, 2) == 0;  // 랜덤으로 선공 결정
            currentTurn = 1;
            StartCoroutine(TurnTimer());
        }

        // 턴 타이머
        IEnumerator TurnTimer()
        {
            while (!isGameOver)
            {
                remainingTurnTime = turnTimeLimit;
                
                while (remainingTurnTime > 0 && !isGameOver)
                {
                    remainingTurnTime -= Time.deltaTime;
                    yield return null;
                }

                if (!isGameOver)
                {
                    // 시간 초과 시 턴 종료
                    EndTurn();
                }
            }
        }

        // 캐릭터 배치
        public bool PlaceCharacter(CharacterData character, int area, int x, int y)
        {
            if (isGameOver) return false;

            // 유효성 검사
            if (area < 0 || area > 1 || x < 0 || x >= boardWidth || y < 0 || y >= boardHeight)
            {
                Debug.LogError("잘못된 위치입니다.");
                return false;
            }

            Vector3Int position = new Vector3Int(x, y, area);
            
            // 이미 캐릭터가 있는지 확인
            if (placedCharacters.ContainsKey(position))
            {
                Debug.LogWarning("이미 캐릭터가 배치된 위치입니다.");
                return false;
            }

            // 사용 가능한 캐릭터인지 확인
            List<CharacterData> availableCharacters = isPlayerTurn ? playerSelectedCharacters : enemySelectedCharacters;
            bool canUse = false;
            foreach (var c in availableCharacters)
            {
                if (c == character)
                {
                    // 이미 배치된 캐릭터인지 확인
                    bool alreadyPlaced = false;
                    foreach (var placed in placedCharacters.Values)
                    {
                        if (placed.characterData == character && placed.isPlayerCharacter == isPlayerTurn)
                        {
                            alreadyPlaced = true;
                            break;
                        }
                    }
                    if (!alreadyPlaced)
                    {
                        canUse = true;
                        break;
                    }
                }
            }

            if (!canUse)
            {
                Debug.LogWarning("사용할 수 없는 캐릭터입니다.");
                return false;
            }

            // 캐릭터 배치
            PlacedCharacter placedChar = new PlacedCharacter(character, isPlayerTurn, position);
            placedCharacters[position] = placedChar;

            // 공격 범위 계산
            CalculateAttackRange(placedChar);

            // 보드 상태 업데이트
            UpdateBoardState();

            OnCharacterPlaced?.Invoke();
            
            // 턴 종료
            EndTurn();

            return true;
        }

        // 공격 범위 계산 (캐릭터별로 다르게 구현 필요)
        void CalculateAttackRange(PlacedCharacter character)
        {
            character.attackRange.Clear();
            
            // 캐릭터의 공격 패턴에 따라 범위 계산
            // 예시: 십자 패턴, 대각선 패턴, 3x3 패턴 등
            List<Vector2Int> pattern = GetAttackPattern(character.characterData);
            
            foreach (var offset in pattern)
            {
                int newX = character.position.x + offset.x;
                int newY = character.position.y + offset.y;
                
                if (newX >= 0 && newX < boardWidth && newY >= 0 && newY < boardHeight)
                {
                    character.attackRange.Add(new Vector3Int(newX, newY, character.position.z));
                }
            }
        }

        // 캐릭터별 공격 패턴 반환 (예시)
        List<Vector2Int> GetAttackPattern(CharacterData character)
        {
            List<Vector2Int> pattern = new List<Vector2Int>();
            
            // 캐릭터 ID나 타입에 따라 다른 패턴 반환
            // 임시로 기본 십자 패턴
            pattern.Add(new Vector2Int(0, 0));   // 자기 자신
            pattern.Add(new Vector2Int(0, 1));   // 위
            pattern.Add(new Vector2Int(0, -1));  // 아래
            pattern.Add(new Vector2Int(1, 0));   // 오른쪽
            pattern.Add(new Vector2Int(-1, 0));  // 왼쪽
            
            return pattern;
        }

        // 보드 상태 업데이트
        void UpdateBoardState()
        {
            // 모든 타일 초기화
            for (int area = 0; area < 2; area++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    for (int y = 0; y < boardHeight; y++)
                    {
                        boardState[area, x, y] = TileState.Empty;
                    }
                }
            }

            // 각 캐릭터의 공격 범위에 따라 타일 상태 업데이트
            foreach (var placed in placedCharacters.Values)
            {
                foreach (var rangePos in placed.attackRange)
                {
                    TileState currentState = boardState[rangePos.z, rangePos.x, rangePos.y];
                    TileState newState = placed.isPlayerCharacter ? TileState.PlayerControlled : TileState.EnemyControlled;

                    if (currentState == TileState.Empty)
                    {
                        boardState[rangePos.z, rangePos.x, rangePos.y] = newState;
                    }
                    else if (currentState != newState)
                    {
                        boardState[rangePos.z, rangePos.x, rangePos.y] = TileState.Contested;
                    }
                }
            }
        }

        // 턴 종료
        public void EndTurn()
        {
            currentTurn++;
            isPlayerTurn = !isPlayerTurn;
            remainingTurnTime = turnTimeLimit;
            
            OnTurnChanged?.Invoke(isPlayerTurn);

            // 모든 캐릭터가 배치되었는지 확인
            if (placedCharacters.Count >= playerSelectedCharacters.Count + enemySelectedCharacters.Count)
            {
                CalculateFinalScore();
                EndGame();
            }
        }

        // 최종 점수 계산
        void CalculateFinalScore()
        {
            int areaAPlayerTiles = 0;
            int areaAEnemyTiles = 0;
            int areaBPlayerTiles = 0;
            int areaBEnemyTiles = 0;

            // A 구역 (area = 0) 계산
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    if (boardState[0, x, y] == TileState.PlayerControlled)
                        areaAPlayerTiles++;
                    else if (boardState[0, x, y] == TileState.EnemyControlled)
                        areaAEnemyTiles++;
                }
            }

            // B 구역 (area = 1) 계산
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    if (boardState[1, x, y] == TileState.PlayerControlled)
                        areaBPlayerTiles++;
                    else if (boardState[1, x, y] == TileState.EnemyControlled)
                        areaBEnemyTiles++;
                }
            }

            // 점수 계산
            playerScore = 0;
            enemyScore = 0;

            // A 구역 점수
            if (areaAPlayerTiles > areaAEnemyTiles) playerScore++;
            else if (areaAEnemyTiles > areaAPlayerTiles) enemyScore++;

            // B 구역 점수
            if (areaBPlayerTiles > areaBEnemyTiles) playerScore++;
            else if (areaBEnemyTiles > areaBPlayerTiles) enemyScore++;

            OnScoreChanged?.Invoke(playerScore, enemyScore);
        }

        // 게임 종료
        void EndGame()
        {
            isGameOver = true;
            CurrentMode = GameMode.Result;
            OnGameOver?.Invoke();
        }

        // 게임 재시작
        public void RestartGame()
        {
            InitializeGame();
            CurrentMode = GameMode.CharacterSelection;
        }
    }
}