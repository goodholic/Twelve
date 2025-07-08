using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 유닛 배치 관리 시스템
    /// A타일과 B타일에 턴제로 유닛을 배치하고 승부를 판정
    /// </summary>
    public class Placement : MonoBehaviour
    {
        private static Placement instance;
        public static Placement Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<Placement>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("Placement");
                        instance = go.AddComponent<Placement>();
                    }
                }
                return instance;
            }
        }
        
        [Header("그리드 설정")]
        public const int GRID_WIDTH = 6;   // 가로 6칸
        public const int GRID_HEIGHT = 3;  // 세로 3칸
        public float tileSize = 1.2f;
        public float tileSpacing = 0.1f;
        public float groupSpacing = 2.0f;  // A타일과 B타일 사이 간격
        
        [Header("타일 프리팹")]
        public GameObject tilePrefab;
        public Transform tileContainer;
        
        [Header("배치 설정")]
        private bool isPlacementMode = false;
        private GameObject selectedUnit;
        private Tile.Team currentTeam = Tile.Team.Ally;
        private int currentTurn = 0;
        
        [Header("유닛 관리")]
        private Dictionary<Tile.TileType, Dictionary<Tile.Team, List<GameObject>>> unitsOnTiles;
        
        // 타일 그리드
        private Dictionary<Tile.TileType, List<Tile>> tileGroups;
        
        // 이벤트
        public System.Action<GameObject, Tile> OnUnitPlaced;
        public System.Action<Tile.Team> OnTurnChanged;
        public System.Action<int, int, GameResult> OnGameEnd;  // A점수, B점수, 결과
        
        public enum GameResult
        {
            Victory,    // 2점 - 승리
            Draw,       // 1점 - 무승부
            Defeat      // 0점 - 패배
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            InitializeDataStructures();
        }
        
        private void Start()
        {
            // 타일이 프리팹으로 미리 배치되어 있다면 수집
            CollectExistingTiles();
        }
        
        /// <summary>
        /// 데이터 구조 초기화
        /// </summary>
        private void InitializeDataStructures()
        {
            tileGroups = new Dictionary<Tile.TileType, List<Tile>>()
            {
                { Tile.TileType.A, new List<Tile>() },
                { Tile.TileType.B, new List<Tile>() }
            };
            
            unitsOnTiles = new Dictionary<Tile.TileType, Dictionary<Tile.Team, List<GameObject>>>()
            {
                { 
                    Tile.TileType.A, new Dictionary<Tile.Team, List<GameObject>>()
                    {
                        { Tile.Team.Ally, new List<GameObject>() },
                        { Tile.Team.Enemy, new List<GameObject>() }
                    }
                },
                { 
                    Tile.TileType.B, new Dictionary<Tile.Team, List<GameObject>>()
                    {
                        { Tile.Team.Ally, new List<GameObject>() },
                        { Tile.Team.Enemy, new List<GameObject>() }
                    }
                }
            };
        }
        
        /// <summary>
        /// 이미 배치된 타일 수집
        /// </summary>
        private void CollectExistingTiles()
        {
            Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
            
            foreach (Tile tile in allTiles)
            {
                if (tileGroups.ContainsKey(tile.tileType))
                {
                    tileGroups[tile.tileType].Add(tile);
                }
            }
            
            Debug.Log($"수집된 타일 - A: {tileGroups[Tile.TileType.A].Count}개, B: {tileGroups[Tile.TileType.B].Count}개");
        }
        
        /// <summary>
        /// 배치 모드 시작
        /// </summary>
        public void StartPlacementMode(GameObject unit)
        {
            isPlacementMode = true;
            selectedUnit = unit;
            Debug.Log($"{currentTeam} 팀의 배치 모드 시작");
        }
        
        /// <summary>
        /// 배치 모드 종료
        /// </summary>
        public void EndPlacementMode()
        {
            isPlacementMode = false;
            selectedUnit = null;
            
            // 모든 타일의 표시 제거
            foreach (var tiles in tileGroups.Values)
            {
                foreach (var tile in tiles)
                {
                    tile.ShowPlacementIndicator(false);
                }
            }
        }
        
        /// <summary>
        /// 배치 모드 확인
        /// </summary>
        public bool IsPlacementMode()
        {
            return isPlacementMode;
        }
        
        /// <summary>
        /// 타일에 배치 가능한지 확인
        /// </summary>
        public bool CanPlaceOnTile(Tile tile)
        {
            if (tile == null || !tile.CanPlaceUnit())
                return false;
                
            // 현재 턴의 팀만 배치 가능
            return true;
        }
        
        /// <summary>
        /// 유닛 배치 시도
        /// </summary>
        public bool TryPlaceUnit(Tile tile)
        {
            if (!isPlacementMode || selectedUnit == null || tile == null)
                return false;
                
            if (!CanPlaceOnTile(tile))
            {
                Debug.Log("이 타일에는 배치할 수 없습니다.");
                return false;
            }
            
            // 유닛 배치
            tile.PlaceUnit(selectedUnit, currentTeam);
            unitsOnTiles[tile.tileType][currentTeam].Add(selectedUnit);
            
            // 이벤트 발생
            OnUnitPlaced?.Invoke(selectedUnit, tile);
            
            // 배치 모드 종료
            EndPlacementMode();
            
            // 턴 종료
            EndTurn();
            
            return true;
        }
        
        /// <summary>
        /// 턴 종료
        /// </summary>
        private void EndTurn()
        {
            currentTurn++;
            
            // 팀 전환
            currentTeam = currentTeam == Tile.Team.Ally ? Tile.Team.Enemy : Tile.Team.Ally;
            OnTurnChanged?.Invoke(currentTeam);
            
            Debug.Log($"턴 {currentTurn}: {currentTeam} 팀의 차례");
            
            // 게임 종료 조건 체크 (예: 모든 타일이 채워졌을 때)
            CheckGameEnd();
        }
        
        /// <summary>
        /// 게임 종료 확인
        /// </summary>
        private void CheckGameEnd()
        {
            // 모든 타일이 채워졌는지 확인
            bool allTilesFilled = true;
            foreach (var tiles in tileGroups.Values)
            {
                foreach (var tile in tiles)
                {
                    if (!tile.isOccupied)
                    {
                        allTilesFilled = false;
                        break;
                    }
                }
                if (!allTilesFilled) break;
            }
            
            if (allTilesFilled)
            {
                CalculateGameResult();
            }
        }
        
        /// <summary>
        /// 게임 결과 계산
        /// </summary>
        private void CalculateGameResult()
        {
            int allyScore = 0;
            int enemyScore = 0;
            
            // A타일 판정
            int allyCountA = unitsOnTiles[Tile.TileType.A][Tile.Team.Ally].Count;
            int enemyCountA = unitsOnTiles[Tile.TileType.A][Tile.Team.Enemy].Count;
            
            if (allyCountA > enemyCountA)
                allyScore++;
            else if (enemyCountA > allyCountA)
                enemyScore++;
            
            Debug.Log($"A타일: 아군 {allyCountA} vs 적군 {enemyCountA}");
            
            // B타일 판정
            int allyCountB = unitsOnTiles[Tile.TileType.B][Tile.Team.Ally].Count;
            int enemyCountB = unitsOnTiles[Tile.TileType.B][Tile.Team.Enemy].Count;
            
            if (allyCountB > enemyCountB)
                allyScore++;
            else if (enemyCountB > allyCountB)
                enemyScore++;
                
            Debug.Log($"B타일: 아군 {allyCountB} vs 적군 {enemyCountB}");
            
            // 최종 결과
            GameResult result;
            if (allyScore == 2)
            {
                result = GameResult.Victory;
                Debug.Log($"승리! (아군 {allyScore}점)");
            }
            else if (allyScore == 1)
            {
                result = GameResult.Draw;
                Debug.Log($"무승부! (아군 {allyScore}점, 적군 {enemyScore}점)");
            }
            else
            {
                result = GameResult.Defeat;
                Debug.Log($"패배! (아군 {allyScore}점, 적군 {enemyScore}점)");
            }
            
            OnGameEnd?.Invoke(allyScore, enemyScore, result);
        }
        
        /// <summary>
        /// 특정 타일 그룹의 유닛 수 가져오기
        /// </summary>
        public int GetUnitCount(Tile.TileType tileType, Tile.Team team)
        {
            return unitsOnTiles[tileType][team].Count;
        }
        
        /// <summary>
        /// 게임 리셋
        /// </summary>
        public void ResetGame()
        {
            // 모든 타일 리셋
            foreach (var tiles in tileGroups.Values)
            {
                foreach (var tile in tiles)
                {
                    tile.ResetTile();
                }
            }
            
            // 유닛 리스트 클리어
            foreach (var tileDict in unitsOnTiles.Values)
            {
                foreach (var teamList in tileDict.Values)
                {
                    teamList.Clear();
                }
            }
            
            // 게임 상태 리셋
            currentTurn = 0;
            currentTeam = Tile.Team.Ally;
            EndPlacementMode();
            
            Debug.Log("게임이 리셋되었습니다.");
        }
        
        /// <summary>
        /// 현재 턴 정보
        /// </summary>
        public Tile.Team GetCurrentTeam()
        {
            return currentTeam;
        }
        
        public int GetCurrentTurn()
        {
            return currentTurn;
        }
    }
}
