using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 전투 캐릭터 배치 시스템
    /// A타일과 B타일에 캐릭터를 배치하고 공격 범위를 표시
    /// </summary>
    public class BattleCharacterPlacement : MonoBehaviour
    {
        [Header("타일 설정")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Transform aTileParent; // A 타일 부모 (위쪽)
        [SerializeField] private Transform bTileParent; // B 타일 부모 (아래쪽)
        [SerializeField] private float tileSpacing = 1.1f;
        
        [Header("배치 설정")]
        public GameObject characterPrefab;
        [SerializeField] private LayerMask tileLayerMask;
        
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI currentTurnText;
        [SerializeField] private TextMeshProUGUI allyScoreText;
        [SerializeField] private TextMeshProUGUI enemyScoreText;
        
        // 타일 배열
        private Tile[,] aTiles = new Tile[6, 3]; // A 타일 (위쪽)
        private Tile[,] bTiles = new Tile[6, 3]; // B 타일 (아래쪽)
        
        // 타일 배열 getter
        public Tile[,] ATiles { get { return aTiles; } }
        public Tile[,] BTiles { get { return bTiles; } }
        
        // 게임 상태
        private bool isPlayerTurn = true;
        public int allyCharactersInA = 0;
        public int enemyCharactersInA = 0;
        public int allyCharactersInB = 0;
        public int enemyCharactersInB = 0;
        
        // 현재 선택된 캐릭터
        private CharacterData selectedCharacter;
        private List<Tile> highlightedTiles = new List<Tile>();
        
        private void Start()
        {
            GenerateTiles();
            UpdateUI();
        }
        
        /// <summary>
        /// 타일 생성
        /// </summary>
        private void GenerateTiles()
        {
            // A 타일 생성 (위쪽)
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Vector3 position = new Vector3(x * tileSpacing, y * tileSpacing, 0);
                    GameObject tileObj = Instantiate(tilePrefab, aTileParent);
                    tileObj.transform.localPosition = position;
                    
                    Tile tile = tileObj.GetComponent<Tile>();
                    tile.Initialize(x, y, Tile.TileType.A);
                    aTiles[x, y] = tile;
                }
            }
            
            // B 타일 생성 (아래쪽)
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Vector3 position = new Vector3(x * tileSpacing, y * tileSpacing, 0);
                    GameObject tileObj = Instantiate(tilePrefab, bTileParent);
                    tileObj.transform.localPosition = position;
                    
                    Tile tile = tileObj.GetComponent<Tile>();
                    tile.Initialize(x, y, Tile.TileType.B);
                    bTiles[x, y] = tile;
                }
            }
        }
        
        /// <summary>
        /// 캐릭터 선택
        /// </summary>
        public void SelectCharacter(CharacterData character)
        {
            selectedCharacter = character;
            ClearHighlights();
            
            // 배치 가능한 타일 하이라이트
            HighlightAvailableTiles();
        }
        
        /// <summary>
        /// 배치 가능한 타일 하이라이트
        /// </summary>
        private void HighlightAvailableTiles()
        {
            // A 타일 확인
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (!aTiles[x, y].isOccupied)
                    {
                        aTiles[x, y].SetValidPlacement(true);
                        highlightedTiles.Add(aTiles[x, y]);
                    }
                }
            }
            
            // B 타일 확인
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (!bTiles[x, y].isOccupied)
                    {
                        bTiles[x, y].SetValidPlacement(true);
                        highlightedTiles.Add(bTiles[x, y]);
                    }
                }
            }
        }
        
        /// <summary>
        /// 타일 클릭 처리
        /// </summary>
        private void Update()
        {
            if (selectedCharacter == null) return;
            
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, tileLayerMask);
                
                if (hit.collider != null)
                {
                    Tile clickedTile = hit.collider.GetComponent<Tile>();
                    if (clickedTile != null && !clickedTile.isOccupied)
                    {
                        PlaceCharacter(clickedTile);
                    }
                }
            }
        }
        
        /// <summary>
        /// 캐릭터 배치
        /// </summary>
        private void PlaceCharacter(Tile tile)
        {
            if (selectedCharacter == null || tile.isOccupied) return;
            
            // 캐릭터 생성
            GameObject characterObj = Instantiate(characterPrefab, tile.transform.position, Quaternion.identity);
            BattleCharacter battleChar = characterObj.GetComponent<BattleCharacter>();
            
            // 캐릭터 초기화
            Tile.Team team = isPlayerTurn ? Tile.Team.Ally : Tile.Team.Enemy;
            battleChar.Initialize(selectedCharacter, team);
            
            // 타일에 배치
            tile.PlaceUnit(characterObj, team);
            
            // 공격 범위 표시
            ShowAttackRange(tile, selectedCharacter.GetAttackPattern());
            
            // 점수 업데이트
            UpdateScore(tile, team);
            
            // 턴 전환
            isPlayerTurn = !isPlayerTurn;
            selectedCharacter = null;
            ClearHighlights();
            UpdateUI();
        }
        
        /// <summary>
        /// 공격 범위 표시
        /// </summary>
        public void ShowAttackRange(Tile centerTile, BattleAttackPattern pattern)
        {
            List<Vector2Int> attackPositions = pattern.GetAttackPositions();
            Tile[,] tiles = centerTile.tileType == Tile.TileType.A ? aTiles : bTiles;
            
            foreach (Vector2Int offset in attackPositions)
            {
                int targetX = centerTile.x + offset.x;
                int targetY = centerTile.y + offset.y;
                
                if (targetX >= 0 && targetX < 6 && targetY >= 0 && targetY < 3)
                {
                    tiles[targetX, targetY].ShowAttackEffect();
                }
            }
        }
        
        /// <summary>
        /// 점수 업데이트
        /// </summary>
        public void UpdateScore(Tile tile, Tile.Team team)
        {
            if (tile.tileType == Tile.TileType.A)
            {
                if (team == Tile.Team.Ally)
                    allyCharactersInA++;
                else
                    enemyCharactersInA++;
            }
            else // B 타일
            {
                if (team == Tile.Team.Ally)
                    allyCharactersInB++;
                else
                    enemyCharactersInB++;
            }
        }
        
        /// <summary>
        /// 하이라이트 제거
        /// </summary>
        private void ClearHighlights()
        {
            foreach (Tile tile in highlightedTiles)
            {
                tile.SetValidPlacement(false);
            }
            highlightedTiles.Clear();
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            currentTurnText.text = isPlayerTurn ? "아군 턴" : "적군 턴";
            
            // 점수 계산
            int allyScore = 0;
            int enemyScore = 0;
            
            // A 타일 우위 확인
            if (allyCharactersInA > enemyCharactersInA)
                allyScore++;
            else if (enemyCharactersInA > allyCharactersInA)
                enemyScore++;
            
            // B 타일 우위 확인
            if (allyCharactersInB > enemyCharactersInB)
                allyScore++;
            else if (enemyCharactersInB > allyCharactersInB)
                enemyScore++;
            
            allyScoreText.text = $"아군: {allyScore}점";
            enemyScoreText.text = $"적군: {enemyScore}점";
        }
        
        /// <summary>
        /// 게임 종료 확인
        /// </summary>
        public bool IsGameEnd()
        {
            // 모든 타일이 채워졌는지 확인
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (!aTiles[x, y].isOccupied || !bTiles[x, y].isOccupied)
                        return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// 최종 결과 계산
        /// </summary>
        public string GetGameResult()
        {
            int allyScore = 0;
            int enemyScore = 0;
            
            // A 타일 우위
            if (allyCharactersInA > enemyCharactersInA)
                allyScore++;
            else if (enemyCharactersInA > allyCharactersInA)
                enemyScore++;
            
            // B 타일 우위
            if (allyCharactersInB > enemyCharactersInB)
                allyScore++;
            else if (enemyCharactersInB > allyCharactersInB)
                enemyScore++;
            
            if (allyScore == 2)
                return "승리! (2점)";
            else if (enemyScore == 2)
                return "패배! (0점)";
            else
                return "무승부! (1점)";
        }
    }
    
    /// <summary>
    /// 공격 패턴 클래스
    /// </summary>
    [System.Serializable]
    public class BattleAttackPattern
    {
        public string patternName;
        public List<Vector2Int> attackOffsets = new List<Vector2Int>();
        
        public BattleAttackPattern(string name)
        {
            patternName = name;
        }
        
        public List<Vector2Int> GetAttackPositions()
        {
            return new List<Vector2Int>(attackOffsets);
        }
        
        // 기본 공격 패턴들
        public static BattleAttackPattern GetCrossPattern()
        {
            BattleAttackPattern pattern = new BattleAttackPattern("십자형");
            pattern.attackOffsets.Add(new Vector2Int(0, 1));
            pattern.attackOffsets.Add(new Vector2Int(0, -1));
            pattern.attackOffsets.Add(new Vector2Int(1, 0));
            pattern.attackOffsets.Add(new Vector2Int(-1, 0));
            return pattern;
        }
        
        public static BattleAttackPattern GetDiagonalPattern()
        {
            BattleAttackPattern pattern = new BattleAttackPattern("대각선");
            pattern.attackOffsets.Add(new Vector2Int(1, 1));
            pattern.attackOffsets.Add(new Vector2Int(1, -1));
            pattern.attackOffsets.Add(new Vector2Int(-1, 1));
            pattern.attackOffsets.Add(new Vector2Int(-1, -1));
            return pattern;
        }
        
        public static BattleAttackPattern GetSquarePattern()
        {
            BattleAttackPattern pattern = new BattleAttackPattern("사각형");
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x != 0 || y != 0)
                        pattern.attackOffsets.Add(new Vector2Int(x, y));
                }
            }
            return pattern;
        }
        
        public static BattleAttackPattern GetLinePattern(int range)
        {
            BattleAttackPattern pattern = new BattleAttackPattern($"직선 {range}칸");
            for (int i = 1; i <= range; i++)
            {
                pattern.attackOffsets.Add(new Vector2Int(i, 0));
                pattern.attackOffsets.Add(new Vector2Int(-i, 0));
            }
            return pattern;
        }
    }
}