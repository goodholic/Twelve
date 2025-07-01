using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildMaster.Battle
{
    public enum SquadRole
    {
        Vanguard,   // Front line, high defense
        Assault,    // Main damage dealers
        Support,    // Healers and buffers
        Reserve     // Backup and special tactics
    }

    [System.Serializable]
    public class Squad
    {
        // Squad Configuration
        public const int ROWS = 3;
        public const int COLS = 6;  // 6x3 그리드로 변경
        public const int MAX_UNITS = 18;
        
        // Squad Properties
        public string Name { get; set; }
        public int Index { get; set; }
        public SquadRole Role { get; set; }
        public bool IsPlayerSquad { get; set; }
        public bool isDefeated => GetAliveUnits().Count == 0;
        public float totalPower => GetTotalCombatPower();
        public List<Unit> aliveUnits => GetAliveUnits();
        
        // Units Grid (6x3)
        private Unit[,] unitsGrid = new Unit[ROWS, COLS];
        private List<Unit> unitsList = new List<Unit>();
        
        // Squad Stats
        public float TotalHealth => CalculateTotalHealth();
        public float TotalMaxHealth => CalculateTotalMaxHealth();
        public int AliveUnitsCount => unitsList.Count(u => u != null && u.isAlive);
        public bool IsDefeated => AliveUnitsCount == 0;
        public float AverageLevel => unitsList.Count > 0 ? (float)unitsList.Average(u => u.level) : 0f;
        
        // 추가 속성들
        public bool IsAlive => !IsDefeated;
        public List<Unit> Units => GetAllUnits();
        public List<Unit> AliveUnits => GetAliveUnits();
        public List<Unit> units => GetAllUnits(); // 호환성을 위한 소문자 속성
        
        // Squad Formation
        public enum Formation
        {
            Standard,   // Balanced formation
            Defensive,  // Tanks in front
            Offensive,  // DPS focused
            Ranged,     // Ranged units protected
            Healing     // Protect healers
        }
        
        public Formation CurrentFormation { get; set; } = Formation.Standard;
        
        // Events
        public event Action<Squad, Unit> OnUnitAdded;
        public event Action<Squad, Unit> OnUnitRemoved;
        public event Action<Squad, Unit> OnUnitDied;
        public event Action<Squad> OnSquadDefeated;
        
        public Squad(string name, int index, bool isPlayerSquad)
        {
            Name = name;
            Index = index;
            IsPlayerSquad = isPlayerSquad;
            Role = SquadRole.Assault; // Default role
        }
        
        // Unit Management
        public bool AddUnit(Unit unit, int row = -1, int col = -1)
        {
            if (unitsList.Count >= MAX_UNITS)
            {
                Debug.LogWarning($"Squad {Name} is full!");
                return false;
            }
            
            // Auto-placement if position not specified
            if (row == -1 || col == -1)
            {
                var position = FindBestPosition(unit);
                if (position.HasValue)
                {
                    row = position.Value.x;
                    col = position.Value.y;
                }
                else
                {
                    Debug.LogWarning($"No suitable position found for unit in squad {Name}");
                    return false;
                }
            }
            
            // Validate position
            if (!IsValidPosition(row, col) || unitsGrid[row, col] != null)
            {
                Debug.LogWarning($"Invalid or occupied position [{row}, {col}] in squad {Name}");
                return false;
            }
            
            // Place unit
            unitsGrid[row, col] = unit;
            unitsList.Add(unit);
            unit.SetPosition(Index, row, col);
            unit.isPlayerUnit = IsPlayerSquad;
            
            // Subscribe to unit events
            unit.OnDeath += HandleUnitDeath;
            
            OnUnitAdded?.Invoke(this, unit);
            return true;
        }
        
        public bool RemoveUnit(Unit unit)
        {
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLS; col++)
                {
                    if (unitsGrid[row, col] == unit)
                    {
                        unitsGrid[row, col] = null;
                        unitsList.Remove(unit);
                        
                        // Unsubscribe from events
                        unit.OnDeath -= HandleUnitDeath;
                        
                        OnUnitRemoved?.Invoke(this, unit);
                        return true;
                    }
                }
            }
            return false;
        }
        
        public Unit GetUnit(int row, int col)
        {
            if (IsValidPosition(row, col))
            {
                return unitsGrid[row, col];
            }
            return null;
        }
        
        public List<Unit> GetAllUnits()
        {
            return new List<Unit>(unitsList);
        }
        
        public List<Unit> GetAliveUnits()
        {
            var aliveUnits = new List<Unit>();
            foreach (var unit in GetAllUnits())
            {
                if (unit != null && unit.IsAlive)
                {
                    aliveUnits.Add(unit);
                }
            }
            return aliveUnits;
        }
        
        public Unit GetUnitAt(int row, int col)
        {
            return GetUnit(row, col);
        }
        
        public int GetUnitCount()
        {
            return unitsList.Count;
        }
        
        public bool IsFull()
        {
            return unitsList.Count >= MAX_UNITS;
        }
        
        public void ClearSquad()
        {
            // 모든 유닛 제거
            var unitsToRemove = new List<Unit>(unitsList);
            foreach (var unit in unitsToRemove)
            {
                RemoveUnit(unit);
            }
            
            // 그리드 초기화
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLS; col++)
                {
                    unitsGrid[row, col] = null;
                }
            }
            
            unitsList.Clear();
        }
        
        // Position Management
        Vector2Int? FindBestPosition(Unit unit)
        {
            // Determine preferred positions based on job class
            List<Vector2Int> preferredPositions = GetPreferredPositions(unit.jobClass);
            
            // Try preferred positions first
            foreach (var pos in preferredPositions)
            {
                if (IsValidPosition(pos.x, pos.y) && unitsGrid[pos.x, pos.y] == null)
                {
                    return pos;
                }
            }
            
            // Try any empty position
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLS; col++)
                {
                    if (unitsGrid[row, col] == null)
                    {
                        return new Vector2Int(row, col);
                    }
                }
            }
            
            return null;
        }
        
        List<Vector2Int> GetPreferredPositions(JobClass jobClass)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            switch (jobClass)
            {
                case JobClass.Warrior:
                case JobClass.Knight:
                    // Front row positions
                    for (int col = 0; col < COLS; col++)
                    {
                        positions.Add(new Vector2Int(0, col));
                    }
                    break;
                    
                case JobClass.Ranger:
                case JobClass.Mage:
                    // Back row positions
                    for (int col = 0; col < COLS; col++)
                    {
                        positions.Add(new Vector2Int(2, col));
                    }
                    break;
                    
                case JobClass.Priest:
                case JobClass.Sage:
                    // Middle-back positions
                    for (int col = 0; col < COLS; col++)
                    {
                        positions.Add(new Vector2Int(2, col));
                        positions.Add(new Vector2Int(1, col));
                    }
                    break;
                    
                case JobClass.Assassin:
                    // Side positions for flanking
                    positions.Add(new Vector2Int(0, 0));
                    positions.Add(new Vector2Int(0, COLS - 1));
                    positions.Add(new Vector2Int(1, 0));
                    positions.Add(new Vector2Int(1, COLS - 1));
                    break;
            }
            
            return positions;
        }
        
        bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < ROWS && col >= 0 && col < COLS;
        }
        
        // Formation Management
        public void ApplyFormation(Formation formation)
        {
            CurrentFormation = formation;
            ReorganizeUnits();
        }
        
        void ReorganizeUnits()
        {
            // Store current units
            List<Unit> units = new List<Unit>(unitsList);
            
            // Clear grid
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLS; col++)
                {
                    unitsGrid[row, col] = null;
                }
            }
            unitsList.Clear();
            
            // Sort units based on formation
            switch (CurrentFormation)
            {
                case Formation.Defensive:
                    units = units.OrderByDescending(u => u.Defense).ToList();
                    break;
                case Formation.Offensive:
                    units = units.OrderByDescending(u => u.Attack + u.MagicPower).ToList();
                    break;
                case Formation.Ranged:
                    units = units.OrderBy(u => 
                        u.JobClass == JobClass.Ranger || u.JobClass == JobClass.Mage ? 0 : 1
                    ).ToList();
                    break;
                case Formation.Healing:
                    units = units.OrderBy(u => 
                        u.JobClass == JobClass.Priest || u.JobClass == JobClass.Sage ? 0 : 1
                    ).ToList();
                    break;
            }
            
            // Re-add units with new positions
            foreach (var unit in units)
            {
                AddUnit(unit);
            }
        }
        
        // Combat Methods
        public Unit GetNextTarget(bool prioritizeFront = true)
        {
            if (prioritizeFront)
            {
                // Check front to back
                for (int row = 0; row < ROWS; row++)
                {
                    for (int col = 0; col < COLS; col++)
                    {
                        Unit unit = unitsGrid[row, col];
                        if (unit != null && unit.IsAlive)
                        {
                            return unit;
                        }
                    }
                }
            }
            else
            {
                // Check back to front (for assassin-type attacks)
                for (int row = ROWS - 1; row >= 0; row--)
                {
                    for (int col = 0; col < COLS; col++)
                    {
                        Unit unit = unitsGrid[row, col];
                        if (unit != null && unit.IsAlive)
                        {
                            return unit;
                        }
                    }
                }
            }
            
            return null;
        }
        
        public List<Unit> GetUnitsInArea(int centerRow, int centerCol, int radius)
        {
            List<Unit> unitsInArea = new List<Unit>();
            
            for (int row = centerRow - radius; row <= centerRow + radius; row++)
            {
                for (int col = centerCol - radius; col <= centerCol + radius; col++)
                {
                    if (IsValidPosition(row, col))
                    {
                        Unit unit = unitsGrid[row, col];
                        if (unit != null && unit.IsAlive)
                        {
                            unitsInArea.Add(unit);
                        }
                    }
                }
            }
            
            return unitsInArea;
        }
        
        // Stats Calculation
        float CalculateTotalHealth()
        {
            return unitsList.Where(u => u != null && u.IsAlive).Sum(u => u.CurrentHealth);
        }
        
        float CalculateTotalMaxHealth()
        {
            return unitsList.Where(u => u != null).Sum(u => u.MaxHealth);
        }
        
        public float GetHealthPercentage()
        {
            float maxHealth = CalculateTotalMaxHealth();
            return maxHealth > 0 ? CalculateTotalHealth() / maxHealth : 0;
        }
        
        public Dictionary<JobClass, int> GetJobComposition()
        {
            return unitsList.GroupBy(u => u.JobClass)
                           .ToDictionary(g => g.Key, g => g.Count());
        }
        
        // Event Handlers
        void HandleUnitDeath(Unit unit)
        {
            OnUnitDied?.Invoke(this, unit);
            
            if (IsDefeated)
            {
                OnSquadDefeated?.Invoke(this);
            }
        }
        
        // Squad Abilities
        public bool CanUseSquadAbility()
        {
            // Check if squad has enough units of certain types
            var composition = GetJobComposition();
            
            switch (Role)
            {
                case SquadRole.Vanguard:
                    return composition.ContainsKey(JobClass.Knight) && composition[JobClass.Knight] >= 2;
                case SquadRole.Assault:
                    return AliveUnitsCount >= 5;
                case SquadRole.Support:
                    return composition.ContainsKey(JobClass.Priest) && composition[JobClass.Priest] >= 1;
                case SquadRole.Reserve:
                    return GetHealthPercentage() < 0.5f;
            }
            
            return false;
        }
        
        public void ExecuteSquadAbility()
        {
            if (!CanUseSquadAbility()) return;
            
            switch (Role)
            {
                case SquadRole.Vanguard:
                    // Shield Wall - Increase defense for all units
                    foreach (var unit in GetAliveUnits())
                    {
                        unit.defense *= 1.5f;
                    }
                    break;
                    
                case SquadRole.Assault:
                    // Battle Cry - Increase attack for all units
                    foreach (var unit in GetAliveUnits())
                    {
                        unit.attackPower *= 1.3f;
                    }
                    break;
                    
                case SquadRole.Support:
                    // Group Heal - Heal all units
                    foreach (var unit in GetAliveUnits())
                    {
                        unit.Heal(unit.MaxHealth * 0.3f);
                    }
                    break;
                    
                case SquadRole.Reserve:
                    // Last Stand - Increase all stats when low health
                    foreach (var unit in GetAliveUnits())
                    {
                        unit.attackPower *= 1.5f;
                        unit.defense *= 1.5f;
                        unit.speed *= 1.5f;
                    }
                    break;
            }
        }

        // Apply formation buffs
        float GetFormationBonus(Vector2Int position)
        {
            float bonus = 1.0f;
            
            if (position.x == 0) // Front row gets defense bonus
            {
                bonus += 0.1f;
            }
            if (position.x == 2) // Back row gets speed bonus  
            {
                bonus += 0.1f;
            }
            
            return bonus;
        }

        public float GetTotalCombatPower()
        {
            float totalPower = 0f;
            foreach (var unit in GetAllUnits())
            {
                if (unit != null && unit.IsAlive)
                {
                    totalPower += unit.GetCombatPower();
                }
            }
            return totalPower;
        }


    }
}