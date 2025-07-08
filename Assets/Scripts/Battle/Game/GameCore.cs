using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

namespace GuildMaster.Game
{
    /// <summary>
    /// 턴제 범위 공격 게임의 핵심 로직 관리
    /// </summary>
    public class GameCore : MonoBehaviour
    {
        [Header("Board Settings")]
        public GameObject tilePrefab;
        public Transform areaAParent;  // 위쪽 영역
        public Transform areaBParent;  // 아래쪽 영역
        public float tileSpacing = 1.1f;
        
        [Header("Visual Settings")]
        public Color emptyTileColor = Color.gray;
        public Color playerTileColor = Color.blue;
        public Color enemyTileColor = Color.red;
        public Color contestedTileColor = Color.yellow;
        
        [Header("UI References")]
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI playerScoreText;
        public TextMeshProUGUI enemyScoreText;
        public GameObject characterSelectionPanel;
        public GameObject battlePanel;
        public GameObject resultPanel;
        public TextMeshProUGUI resultText;
        
        [Header("Character Display")]
        public Transform characterSlotParent;
        public GameObject characterSlotPrefab;
        
        // 타일 게임오브젝트 배열
        private GameObject[,,] tileObjects = new GameObject[2, 6, 3];
        private Dictionary<GameObject, Vector3Int> tilePositions = new Dictionary<GameObject, Vector3Int>();
        
        // 캐릭터 게임오브젝트 관리
        private Dictionary<Vector3Int, GameObject> characterObjects = new Dictionary<Vector3Int, GameObject>();
        
        // Singleton
        private static GameCore _instance;
        public static GameCore Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameCore>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameCore");
                        _instance = go.AddComponent<GameCore>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
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
        }

        void Start()
        {
            InitializeBoard();
            SubscribeToEvents();
            UpdateUI();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        // 이벤트 구독
        void SubscribeToEvents()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnGameModeChanged += OnGameModeChanged;
                Core.GameManager.Instance.OnTurnChanged += OnTurnChanged;
                Core.GameManager.Instance.OnScoreChanged += OnScoreChanged;
                Core.GameManager.Instance.OnGameOver += OnGameOver;
                Core.GameManager.Instance.OnCharacterPlaced += OnCharacterPlaced;
            }
        }

        // 이벤트 구독 해제
        void UnsubscribeFromEvents()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnGameModeChanged -= OnGameModeChanged;
                Core.GameManager.Instance.OnTurnChanged -= OnTurnChanged;
                Core.GameManager.Instance.OnScoreChanged -= OnScoreChanged;
                Core.GameManager.Instance.OnGameOver -= OnGameOver;
                Core.GameManager.Instance.OnCharacterPlaced -= OnCharacterPlaced;
            }
        }

        // 보드 초기화
        void InitializeBoard()
        {
            // 기존 타일 제거
            foreach (var tile in tileObjects)
            {
                if (tile != null) Destroy(tile);
            }
            tilePositions.Clear();

            // A 영역 (위쪽) 타일 생성
            CreateTilesForArea(0, areaAParent);

            // B 영역 (아래쪽) 타일 생성
            CreateTilesForArea(1, areaBParent);
        }

        // 영역별 타일 생성
        void CreateTilesForArea(int area, Transform parent)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    Vector3 position = new Vector3(x * tileSpacing, -y * tileSpacing, 0);
                    GameObject tile = Instantiate(tilePrefab, parent);
                    tile.transform.localPosition = position;
                    tile.name = $"Tile_Area{area}_X{x}_Y{y}";
                    
                    tileObjects[area, x, y] = tile;
                    Vector3Int tilePos = new Vector3Int(x, y, area);
                    tilePositions[tile] = tilePos;

                    // 타일 클릭 이벤트 추가
                    TileClickHandler clickHandler = tile.AddComponent<TileClickHandler>();
                    clickHandler.Initialize(tilePos, this);

                    // 초기 색상 설정
                    SetTileColor(tile, emptyTileColor);
                }
            }
        }

        // 타일 색상 설정
        void SetTileColor(GameObject tile, Color color)
        {
            SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
            }
        }

        // 타일 클릭 처리
        public void OnTileClicked(Vector3Int position)
        {
            if (Core.GameManager.Instance == null) return;
            if (Core.GameManager.Instance.CurrentMode != Core.GameManager.GameMode.Battle) return;
            if (Core.GameManager.Instance.isGameOver) return;

            // 현재 선택된 캐릭터 가져오기 (UI에서 선택된 캐릭터)
            CharacterData selectedCharacter = GetCurrentSelectedCharacter();
            if (selectedCharacter != null)
            {
                Core.GameManager.Instance.PlaceCharacter(selectedCharacter, position.z, position.x, position.y);
            }
        }

        // 현재 선택된 캐릭터 가져오기 (구현 필요)
        CharacterData GetCurrentSelectedCharacter()
        {
            // UI에서 선택된 캐릭터 반환
            // 임시 구현
            return null;
        }

        // 게임 모드 변경 시
        void OnGameModeChanged(Core.GameManager.GameMode previousMode, Core.GameManager.GameMode newMode)
        {
            // UI 패널 전환
            if (characterSelectionPanel != null)
                characterSelectionPanel.SetActive(newMode == Core.GameManager.GameMode.CharacterSelection);
            
            if (battlePanel != null)
                battlePanel.SetActive(newMode == Core.GameManager.GameMode.Battle);
            
            if (resultPanel != null)
                resultPanel.SetActive(newMode == Core.GameManager.GameMode.Result);
        }

        // 턴 변경 시
        void OnTurnChanged(bool isPlayerTurn)
        {
            UpdateUI();
        }

        // 점수 변경 시
        void OnScoreChanged(int playerScore, int enemyScore)
        {
            if (playerScoreText != null)
                playerScoreText.text = $"Player: {playerScore}";
            
            if (enemyScoreText != null)
                enemyScoreText.text = $"Enemy: {enemyScore}";
        }

        // 게임 종료 시
        void OnGameOver()
        {
            if (resultText != null && Core.GameManager.Instance != null)
            {
                int playerScore = Core.GameManager.Instance.playerScore;
                int enemyScore = Core.GameManager.Instance.enemyScore;
                
                string result = "";
                if (playerScore > enemyScore)
                    result = "승리!";
                else if (playerScore < enemyScore)
                    result = "패배...";
                else
                    result = "무승부";
                
                resultText.text = $"{result}\n플레이어: {playerScore}점\n적: {enemyScore}점";
            }
        }

        // 캐릭터 배치 시
        void OnCharacterPlaced()
        {
            UpdateBoardVisuals();
            UpdateCharacterVisuals();
        }

        // 보드 시각 효과 업데이트
        void UpdateBoardVisuals()
        {
            if (Core.GameManager.Instance == null) return;

            for (int area = 0; area < 2; area++)
            {
                for (int x = 0; x < 6; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        GameObject tile = tileObjects[area, x, y];
                        if (tile != null)
                        {
                            Core.GameManager.TileState state = Core.GameManager.Instance.boardState[area, x, y];
                            Color color = emptyTileColor;
                            
                            switch (state)
                            {
                                case Core.GameManager.TileState.PlayerControlled:
                                    color = playerTileColor;
                                    break;
                                case Core.GameManager.TileState.EnemyControlled:
                                    color = enemyTileColor;
                                    break;
                                case Core.GameManager.TileState.Contested:
                                    color = contestedTileColor;
                                    break;
                            }
                            
                            SetTileColor(tile, color);
                        }
                    }
                }
            }
        }

        // 캐릭터 시각 효과 업데이트
        void UpdateCharacterVisuals()
        {
            if (Core.GameManager.Instance == null) return;

            // 기존 캐릭터 오브젝트 제거
            foreach (var charObj in characterObjects.Values)
            {
                if (charObj != null) Destroy(charObj);
            }
            characterObjects.Clear();

            // 배치된 캐릭터 표시
            foreach (var kvp in Core.GameManager.Instance.placedCharacters)
            {
                Vector3Int position = kvp.Key;
                Core.GameManager.PlacedCharacter placedChar = kvp.Value;
                
                GameObject tile = tileObjects[position.z, position.x, position.y];
                if (tile != null)
                {
                    // 캐릭터 스프라이트 생성 (임시)
                    GameObject charObj = new GameObject($"Character_{position}");
                    charObj.transform.SetParent(tile.transform);
                    charObj.transform.localPosition = Vector3.zero;
                    charObj.transform.localScale = Vector3.one * 0.8f;
                    
                    SpriteRenderer renderer = charObj.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 10;
                    
                    // 임시 색상 (플레이어: 파랑, 적: 빨강)
                    renderer.color = placedChar.isPlayerCharacter ? Color.blue : Color.red;
                    
                    characterObjects[position] = charObj;
                }
            }
        }

        // UI 업데이트
        void UpdateUI()
        {
            if (Core.GameManager.Instance == null) return;

            // 턴 표시
            if (turnText != null)
            {
                string turnOwner = Core.GameManager.Instance.isPlayerTurn ? "플레이어" : "적";
                turnText.text = $"턴 {Core.GameManager.Instance.currentTurn}: {turnOwner}";
            }

            // 타이머 표시
            if (timerText != null)
            {
                timerText.text = $"남은 시간: {Core.GameManager.Instance.remainingTurnTime:F1}초";
            }
        }

        void Update()
        {
            // 타이머 업데이트
            if (Core.GameManager.Instance != null && 
                Core.GameManager.Instance.CurrentMode == Core.GameManager.GameMode.Battle &&
                !Core.GameManager.Instance.isGameOver)
            {
                UpdateUI();
            }
        }

        // 타일 클릭 핸들러 컴포넌트
        public class TileClickHandler : MonoBehaviour
        {
            private Vector3Int tilePosition;
            private GameCore gameCore;

            public void Initialize(Vector3Int position, GameCore core)
            {
                tilePosition = position;
                gameCore = core;
            }

            void OnMouseDown()
            {
                if (gameCore != null)
                {
                    gameCore.OnTileClicked(tilePosition);
                }
            }
        }
    }

    // 캐릭터 데이터 (임시)
    [System.Serializable]
    public class CharacterData
    {
        public string characterName;
        public int characterId;
        public Sprite characterSprite;
        public AttackPattern attackPattern;
    }

    // 공격 패턴 타입
    public enum AttackPattern
    {
        Cross,      // 십자
        Diagonal,   // 대각선
        Square3x3,  // 3x3 정사각형
        Line,       // 직선
        Knight,     // 체스 나이트 패턴
        Custom      // 커스텀 패턴
    }
}