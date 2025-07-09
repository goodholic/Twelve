using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using GuildMaster.Data; // CharacterDataSO를 위해 추가
using GuildMaster.Battle;

namespace GuildMaster.Battle
{
    public class TileBattleManager : MonoBehaviour
    {
        [Header("Battle Setup")]
        public Transform playerDeploymentZone;
        public Transform enemyDeploymentZone;
        public GameObject tilePrefab;
        public int gridWidth = 6;
        public int gridHeight = 3;
        public float tileSpacing = 1.1f;
        
        [Header("UI References")]
        public GameObject battleUI;
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI phaseText;
        public Button endTurnButton;
        public GameObject victoryPanel;
        public GameObject defeatPanel;
        
        [Header("Battle State")]
        public List<UnitState> playerUnits = new List<UnitState>();
        public List<UnitState> enemyUnits = new List<UnitState>();
        public List<UnitState> allUnits = new List<UnitState>();
        private UnitState currentUnit;
        private int currentTurn = 1;
        private BattlePhase currentPhase = BattlePhase.Deployment;
        
        [Header("Grid System")]
        private TileGrid battleGrid;
        private Dictionary<UnitState, Tile> unitPositions = new Dictionary<UnitState, Tile>();
        
        [Header("Battle Settings")]
        public float turnDelay = 1f;
        public float actionDelay = 0.5f;
        public bool autoEndTurn = true;
        
        public enum BattlePhase
        {
            Deployment,
            PlayerTurn,
            EnemyTurn,
            Victory,
            Defeat
        }
        
        [System.Serializable]
        public class TileGrid
        {
            public Tile[,] tiles;
            public int width;
            public int height;
            
            public TileGrid(int w, int h)
            {
                width = w;
                height = h;
                tiles = new Tile[w, h];
            }
            
            public Tile GetTile(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                    return null;
                return tiles[x, y];
            }
            
            public List<Tile> GetTilesInRange(Tile center, int range)
            {
                List<Tile> tilesInRange = new List<Tile>();
                
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Tile tile = tiles[x, y];
                        int distance = Mathf.Abs(tile.x - center.x) + Mathf.Abs(tile.y - center.y);
                        
                        if (distance <= range)
                        {
                            tilesInRange.Add(tile);
                        }
                    }
                }
                
                return tilesInRange;
            }
            
            public List<Tile> GetPath(Tile start, Tile end)
            {
                // Simple A* pathfinding implementation
                List<Tile> path = new List<Tile>();
                // ... pathfinding logic ...
                return path;
            }
        }
        
        [System.Serializable]
        public class Tile
        {
            public int x;
            public int y;
            public GameObject tileObject;
            public UnitState occupyingUnit;
            public bool isWalkable = true;
            public TileType tileType = TileType.Normal;
            
            public enum TileType
            {
                Normal,
                Obstacle,
                Hazard,
                Buff,
                Deployment
            }
            
            public bool IsOccupied => occupyingUnit != null;
            
            public void SetUnit(UnitState unit)
            {
                occupyingUnit = unit;
                if (unit != null)
                {
                    unit.transform.position = tileObject.transform.position;
                }
            }
            
            public void ClearUnit()
            {
                occupyingUnit = null;
            }
        }
        
        void Start()
        {
            InitializeBattle();
        }
        
        void InitializeBattle()
        {
            CreateBattleGrid();
            SetupUI();
            StartCoroutine(BattleFlow());
        }
        
        void CreateBattleGrid()
        {
            battleGrid = new TileGrid(gridWidth, gridHeight * 2); // Double height for both sides
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight * 2; y++)
                {
                    Vector3 position = new Vector3(x * tileSpacing, 0, y * tileSpacing);
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity);
                    tileObj.name = $"Tile_{x}_{y}";
                    
                    Tile tile = new Tile
                    {
                        x = x,
                        y = y,
                        tileObject = tileObj
                    };
                    
                    // Set deployment zones
                    if (y < gridHeight)
                    {
                        tile.tileType = Tile.TileType.Deployment;
                        tileObj.GetComponent<Renderer>().material.color = Color.blue;
                    }
                    else
                    {
                        tile.tileType = Tile.TileType.Normal;
                    }
                    
                    battleGrid.tiles[x, y] = tile;
                }
            }
        }
        
        void SetupUI()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }
            
            UpdateUI();
        }
        
        IEnumerator BattleFlow()
        {
            // Deployment Phase
            currentPhase = BattlePhase.Deployment;
            yield return StartCoroutine(DeploymentPhase());
            
            // Battle Loop
            while (currentPhase != BattlePhase.Victory && currentPhase != BattlePhase.Defeat)
            {
                // Player Turn
                currentPhase = BattlePhase.PlayerTurn;
                UpdateUI();
                yield return StartCoroutine(PlayerTurnPhase());
                
                // Check victory
                if (CheckVictoryCondition())
                {
                    currentPhase = BattlePhase.Victory;
                    break;
                }
                
                // Enemy Turn
                currentPhase = BattlePhase.EnemyTurn;
                UpdateUI();
                yield return StartCoroutine(EnemyTurnPhase());
                
                // Check defeat
                if (CheckDefeatCondition())
                {
                    currentPhase = BattlePhase.Defeat;
                    break;
                }
                
                currentTurn++;
            }
            
            // End Battle
            EndBattle();
        }
        
        IEnumerator DeploymentPhase()
        {
            Debug.Log("Deployment Phase Started");
            
            // Deploy player units
            for (int i = 0; i < playerUnits.Count && i < gridWidth; i++)
            {
                Tile deployTile = battleGrid.GetTile(i, 0);
                if (deployTile != null)
                {
                    PlaceUnit(playerUnits[i], deployTile);
                }
            }
            
            // Deploy enemy units
            for (int i = 0; i < enemyUnits.Count && i < gridWidth; i++)
            {
                Tile deployTile = battleGrid.GetTile(i, gridHeight * 2 - 1);
                if (deployTile != null)
                {
                    PlaceUnit(enemyUnits[i], deployTile);
                }
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        IEnumerator PlayerTurnPhase()
        {
            Debug.Log($"Player Turn {currentTurn} Started");
            
            // Sort units by speed
            var sortedPlayerUnits = playerUnits.OrderByDescending(u => u.speed).ToList();
            
            foreach (var unit in sortedPlayerUnits)
            {
                if (unit.isDead) continue;
                
                currentUnit = unit;
                yield return StartCoroutine(ProcessUnitTurn(unit, true));
            }
            
            yield return new WaitForSeconds(turnDelay);
        }
        
        IEnumerator EnemyTurnPhase()
        {
            Debug.Log($"Enemy Turn {currentTurn} Started");
            
            // Sort units by speed
            var sortedEnemyUnits = enemyUnits.OrderByDescending(u => u.speed).ToList();
            
            foreach (var unit in sortedEnemyUnits)
            {
                if (unit.isDead) continue;
                
                currentUnit = unit;
                yield return StartCoroutine(ProcessUnitTurn(unit, false));
            }
            
            yield return new WaitForSeconds(turnDelay);
        }
        
        IEnumerator ProcessUnitTurn(UnitState unit, bool isPlayerControlled)
        {
            Debug.Log($"{unit.unitName}'s turn");
            
            // Update status effects
            unit.UpdateStatusEffects();
            
            if (isPlayerControlled)
            {
                // Wait for player input
                yield return StartCoroutine(WaitForPlayerAction(unit));
            }
            else
            {
                // AI action
                yield return StartCoroutine(ProcessAIAction(unit));
            }
            
            yield return new WaitForSeconds(actionDelay);
        }
        
        IEnumerator WaitForPlayerAction(UnitState unit)
        {
            // Highlight available actions
            ShowAvailableActions(unit);
            
            // Wait for player to select action
            bool actionCompleted = false;
            while (!actionCompleted && !autoEndTurn)
            {
                // Check for input
                yield return null;
            }
            
            HideAvailableActions();
        }
        
        IEnumerator ProcessAIAction(UnitState unit)
        {
            // Simple AI logic
            UnitState target = GetBestTarget(unit);
            
            if (target != null)
            {
                // Check if in range
                Tile unitTile = GetUnitTile(unit);
                Tile targetTile = GetUnitTile(target);
                
                int distance = Mathf.Abs(unitTile.x - targetTile.x) + Mathf.Abs(unitTile.y - targetTile.y);
                
                if (distance <= 1) // Melee range
                {
                    // Attack
                    yield return StartCoroutine(PerformAttack(unit, target));
                }
                else
                {
                    // Move towards target
                    yield return StartCoroutine(MoveTowardsTarget(unit, target));
                }
            }
        }
        
        void PlaceUnit(UnitState unit, Tile tile)
        {
            if (unitPositions.ContainsKey(unit))
            {
                unitPositions[unit].ClearUnit();
            }
            
            tile.SetUnit(unit);
            unitPositions[unit] = tile;
            allUnits.Add(unit);
        }
        
        Tile GetUnitTile(UnitState unit)
        {
            return unitPositions.ContainsKey(unit) ? unitPositions[unit] : null;
        }
        
        UnitState GetBestTarget(UnitState attacker)
        {
            List<UnitState> targets = attacker.isPlayerUnit ? enemyUnits : playerUnits;
            
            // Find lowest HP target
            UnitState bestTarget = null;
            float lowestHPRatio = float.MaxValue;
            
            foreach (var target in targets)
            {
                if (target.isDead) continue;
                
                float hpRatio = target.currentHP / (float)target.maxHP;
                if (hpRatio < lowestHPRatio)
                {
                    lowestHPRatio = hpRatio;
                    bestTarget = target;
                }
            }
            
            return bestTarget;
        }
        
        IEnumerator PerformAttack(UnitState attacker, UnitState defender)
        {
            Debug.Log($"{attacker.unitName} attacks {defender.unitName}");
            
            // Calculate damage
            int damage = CalculateDamage(attacker, defender);
            
            // Apply damage
            defender.TakeDamage(damage);
            
            // Visual feedback
            // TODO: Add attack animation
            
            yield return new WaitForSeconds(0.5f);
            
            // Check if defender died
            if (defender.isDead)
            {
                HandleUnitDeath(defender);
            }
        }
        
        int CalculateDamage(UnitState attacker, UnitState defender)
        {
            float baseDamage = attacker.attackPower;
            float defense = defender.defense;
            
            float damage = baseDamage * (100f / (100f + defense));
            
            // Critical hit
            if (Random.value < attacker.criticalRate)
            {
                damage *= 2f;
                Debug.Log("Critical Hit!");
            }
            
            return Mathf.RoundToInt(damage);
        }
        
        IEnumerator MoveTowardsTarget(UnitState unit, UnitState target)
        {
            Tile currentTile = GetUnitTile(unit);
            Tile targetTile = GetUnitTile(target);
            
            // Simple movement - move one tile closer
            int dx = Mathf.Clamp(targetTile.x - currentTile.x, -1, 1);
            int dy = Mathf.Clamp(targetTile.y - currentTile.y, -1, 1);
            
            Tile newTile = battleGrid.GetTile(currentTile.x + dx, currentTile.y + dy);
            
            if (newTile != null && !newTile.IsOccupied)
            {
                currentTile.ClearUnit();
                PlaceUnit(unit, newTile);
                
                // Visual movement
                // TODO: Add movement animation
                
                yield return new WaitForSeconds(0.3f);
            }
        }
        
        void HandleUnitDeath(UnitState unit)
        {
            Debug.Log($"{unit.unitName} has been defeated!");
            
            // Remove from tile
            Tile tile = GetUnitTile(unit);
            if (tile != null)
            {
                tile.ClearUnit();
            }
            
            // Remove from lists
            unitPositions.Remove(unit);
            allUnits.Remove(unit);
            
            if (unit.isPlayerUnit)
            {
                playerUnits.Remove(unit);
            }
            else
            {
                enemyUnits.Remove(unit);
            }
            
            // Destroy game object
            Destroy(unit.gameObject);
        }
        
        bool CheckVictoryCondition()
        {
            return enemyUnits.Count(u => !u.isDead) == 0;
        }
        
        bool CheckDefeatCondition()
        {
            return playerUnits.Count(u => !u.isDead) == 0;
        }
        
        void EndBattle()
        {
            Debug.Log($"Battle Ended - {currentPhase}");
            
            if (currentPhase == BattlePhase.Victory)
            {
                ShowVictoryScreen();
            }
            else if (currentPhase == BattlePhase.Defeat)
            {
                ShowDefeatScreen();
            }
        }
        
        void ShowVictoryScreen()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
        }
        
        void ShowDefeatScreen()
        {
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }
        }
        
        void ShowAvailableActions(UnitState unit)
        {
            // Highlight movement range
            Tile unitTile = GetUnitTile(unit);
            List<Tile> movementRange = battleGrid.GetTilesInRange(unitTile, 3);
            
            foreach (var tile in movementRange)
            {
                if (!tile.IsOccupied)
                {
                    tile.tileObject.GetComponent<Renderer>().material.color = Color.green;
                }
            }
        }
        
        void HideAvailableActions()
        {
            // Reset tile colors
            for (int x = 0; x < battleGrid.width; x++)
            {
                for (int y = 0; y < battleGrid.height; y++)
                {
                    Tile tile = battleGrid.GetTile(x, y);
                    tile.tileObject.GetComponent<Renderer>().material.color = Color.white;
                }
            }
        }
        
        void UpdateUI()
        {
            if (turnText != null)
            {
                turnText.text = $"Turn: {currentTurn}";
            }
            
            if (phaseText != null)
            {
                phaseText.text = $"Phase: {currentPhase}";
            }
        }
        
        void OnEndTurnClicked()
        {
            if (currentPhase == BattlePhase.PlayerTurn)
            {
                autoEndTurn = true;
            }
        }
        
        public void SetupBattle(List<UnitState> playerTeam, List<UnitState> enemyTeam)
        {
            playerUnits = new List<UnitState>(playerTeam);
            enemyUnits = new List<UnitState>(enemyTeam);
            
            foreach (var unit in playerUnits)
            {
                unit.isPlayerUnit = true;
            }
            
            foreach (var unit in enemyUnits)
            {
                unit.isPlayerUnit = false;
            }
        }
        
        // TacticalCharacterDataSO를 CharacterDataSO로 변경
        public void SetupBattleFromData(List<CharacterDataSO> playerCharacters, List<CharacterDataSO> enemyCharacters)
        {
            // CharacterDataSO를 사용해서 유닛 생성
            playerUnits.Clear();
            foreach (var characterData in playerCharacters)
            {
                var unit = characterData.CreateUnit();
                unit.isPlayerUnit = true;
                playerUnits.Add(unit);
            }
            
            enemyUnits.Clear();
            foreach (var characterData in enemyCharacters)
            {
                var unit = characterData.CreateUnit();
                unit.isPlayerUnit = false;
                enemyUnits.Add(unit);
            }
        }
    }
}