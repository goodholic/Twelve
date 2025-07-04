using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // Unit, JobClass를 위해 추가

namespace GuildMaster.Core
{
    using GuildMaster; // Unit 클래스를 위한 using 추가
    
    public class BattleManager : MonoBehaviour
    {
        // Battle Configuration
        public const int SQUADS_PER_SIDE = 2;    // 2부대로 변경
        public const int SQUAD_ROWS = 3;
        public const int SQUAD_COLS = 6;          // 6x3 그리드로 변경
        public const int UNITS_PER_SQUAD = 18;
        public const int TOTAL_UNITS_PER_SIDE = 36;  // 총 36명으로 변경

        // Battle State
        public enum BattleState
        {
            Preparation,
            InProgress,
            Victory,
            Defeat,
            Draw
        }

        private BattleState _currentBattleState = BattleState.Preparation;
        public BattleState CurrentBattleState
        {
            get => _currentBattleState;
            private set
            {
                if (_currentBattleState != value)
                {
                    _currentBattleState = value;
                    OnBattleStateChanged?.Invoke(_currentBattleState);
                }
            }
        }

        // Battle Data
        public class BattleSquad
        {
            public int SquadIndex { get; set; }
            public Unit[,] Units { get; set; } = new Unit[SQUAD_ROWS, SQUAD_COLS];
            public bool IsPlayerSquad { get; set; }
            public float TotalHealth => GetTotalHealth();
            public bool IsDefeated => GetAliveUnitsCount() == 0;
            public float totalPower => GetTotalPower();

            private float GetTotalHealth()
            {
                float total = 0;
                for (int row = 0; row < SQUAD_ROWS; row++)
                {
                    for (int col = 0; col < SQUAD_COLS; col++)
                    {
                        if (Units[row, col] != null && Units[row, col].IsAlive)
                        {
                            total += Units[row, col].CurrentHealth;
                        }
                    }
                }
                return total;
            }

            private float GetTotalPower()
            {
                float total = 0;
                for (int row = 0; row < SQUAD_ROWS; row++)
                {
                    for (int col = 0; col < SQUAD_COLS; col++)
                    {
                        if (Units[row, col] != null && Units[row, col].IsAlive)
                        {
                            var unit = Units[row, col];
                            total += unit.CurrentHealth * 0.5f + unit.AttackModifiable * 2f + unit.Defense * 1.5f + unit.Speed;
                        }
                    }
                }
                return total;
            }

            public int GetAliveUnitsCount()
            {
                int count = 0;
                for (int row = 0; row < SQUAD_ROWS; row++)
                {
                    for (int col = 0; col < SQUAD_COLS; col++)
                    {
                        if (Units[row, col] != null && Units[row, col].IsAlive)
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }

        private BattleSquad[] playerSquads = new BattleSquad[SQUADS_PER_SIDE];
        private BattleSquad[] enemySquads = new BattleSquad[SQUADS_PER_SIDE];
        
        private int currentTurnSquadIndex = 0;
        private bool isPlayerTurn = true;
        
        // Battle Speed
        private float battleSpeed = 1f;
        private float actionDelay = 1f;
        
        // Battle Tactics
        public enum BattleTactic
        {
            Balanced,      // Standard approach
            Aggressive,    // Focus on offense
            Defensive,     // Focus on defense
            Flanking,      // Target back rows
            Focus,         // Target weakest enemies
            Blitz          // All-out assault
        }
        
        private BattleTactic playerTactic = BattleTactic.Balanced;
        private BattleTactic enemyTactic = BattleTactic.Balanced;
        
        // Squad Synergy
        public class SquadSynergy
        {
            public float AttackBonus { get; set; }
            public float DefenseBonus { get; set; }
            public float SpeedBonus { get; set; }
            public float HealingBonus { get; set; }
        }

        // Events
        public event Action<BattleState> OnBattleStateChanged;
        public event Action<BattleSquad, BattleSquad> OnSquadTurnStart;
        public event Action<Unit, Unit, float> OnUnitAttack;
        public event Action<Unit, float> OnUnitHeal;
        public event Action<Unit> OnUnitDeath;
        public event Action<bool> OnBattleEnd;

        // Battle Statistics
        public class BattleStatistics
        {
            public int TotalDamageDealt { get; set; }
            public int TotalHealingDone { get; set; }
            public int UnitsKilled { get; set; }
            public int UnitsLost { get; set; }
            public float BattleDuration { get; set; }
            public Dictionary<int, int> DamageBySquad { get; set; } = new Dictionary<int, int>();
            public Dictionary<int, int> HealingBySquad { get; set; } = new Dictionary<int, int>();
        }

        private BattleStatistics currentBattleStats;
        private float battleStartTime;
        
        // Battle Effects
        private Dictionary<string, float> battlefieldEffects = new Dictionary<string, float>();
        private List<BattleEvent> battleLog = new List<BattleEvent>();
        
        public class BattleEvent
        {
            public float Timestamp { get; set; }
            public string EventType { get; set; }
            public string Description { get; set; }
            public Unit Actor { get; set; }
            public Unit Target { get; set; }
            public float Value { get; set; }
        }

        void Awake()
        {
            InitializeSquads();
        }

        void InitializeSquads()
        {
            for (int i = 0; i < SQUADS_PER_SIDE; i++)
            {
                playerSquads[i] = new BattleSquad { SquadIndex = i, IsPlayerSquad = true };
                enemySquads[i] = new BattleSquad { SquadIndex = i, IsPlayerSquad = false };
            }
        }

        public void StartBattle(List<Unit> playerUnits, List<Unit> enemyUnits)
        {
            if (playerUnits.Count > TOTAL_UNITS_PER_SIDE || enemyUnits.Count > TOTAL_UNITS_PER_SIDE)
            {
                Debug.LogError("Too many units for battle!");
                return;
            }

            CurrentBattleState = BattleState.Preparation;
            currentBattleStats = new BattleStatistics();
            battleStartTime = Time.time;
            battleLog.Clear();
            battlefieldEffects.Clear();

            // Assign units to squads
            AssignUnitsToSquads(playerUnits, playerSquads);
            AssignUnitsToSquads(enemyUnits, enemySquads);
            
            // Calculate squad synergies
            CalculateSquadSynergies();
            
            // Apply pre-battle tactics
            ApplyTacticEffects();
            
            // Log battle start
            AddBattleEvent("BattleStart", "대규모 길드전이 시작되었습니다!", null, null, 0);

            // Start battle
            CurrentBattleState = BattleState.InProgress;
            currentTurnSquadIndex = 0;
            isPlayerTurn = true;

            StartCoroutine(BattleLoop());
        }

        void AssignUnitsToSquads(List<Unit> units, BattleSquad[] squads)
        {
            int unitIndex = 0;
            
            // Smart unit assignment based on job classes
            var sortedUnits = units.OrderBy(u => GetUnitPriority(u.JobClass)).ToList();
            
            for (int squadIndex = 0; squadIndex < SQUADS_PER_SIDE && unitIndex < sortedUnits.Count; squadIndex++)
            {
                // Assign units in optimal positions
                var squadUnits = sortedUnits.Skip(unitIndex).Take(UNITS_PER_SQUAD).ToList();
                AssignUnitsToSquadOptimally(squadUnits, squads[squadIndex]);
                unitIndex += squadUnits.Count;
            }
        }
        
        void AssignUnitsToSquadOptimally(List<Unit> units, BattleSquad squad)
        {
            // Tanks and warriors in front
            var frontLine = units.Where(u => u.JobClass == JobClass.Warrior || u.JobClass == JobClass.Knight).ToList();
            // Damage dealers in middle
            var middleLine = units.Where(u => u.JobClass == JobClass.Assassin || u.JobClass == JobClass.Sage).ToList();
            // Ranged and support in back
            var backLine = units.Where(u => u.JobClass == JobClass.Ranger || u.JobClass == JobClass.Mage || u.JobClass == JobClass.Priest).ToList();
            
            int unitIndex = 0;
            
            // Place front line units
            for (int col = 0; col < SQUAD_COLS && unitIndex < frontLine.Count; col++)
            {
                squad.Units[0, col] = frontLine[unitIndex];
                frontLine[unitIndex].SetPosition(squad.SquadIndex, 0, col);
                unitIndex++;
            }
            
            unitIndex = 0;
            // Place middle line units
            for (int col = 0; col < SQUAD_COLS && unitIndex < middleLine.Count; col++)
            {
                squad.Units[1, col] = middleLine[unitIndex];
                middleLine[unitIndex].SetPosition(squad.SquadIndex, 1, col);
                unitIndex++;
            }
            
            unitIndex = 0;
            // Place back line units
            for (int col = 0; col < SQUAD_COLS && unitIndex < backLine.Count; col++)
            {
                squad.Units[2, col] = backLine[unitIndex];
                backLine[unitIndex].SetPosition(squad.SquadIndex, 2, col);
                unitIndex++;
            }
        }
        
        int GetUnitPriority(JobClass jobClass)
        {
            switch (jobClass)
            {
                case JobClass.Knight: return 0;
                case JobClass.Warrior: return 1;
                case JobClass.Assassin: return 2;
                case JobClass.Sage: return 3;
                case JobClass.Ranger: return 4;
                case JobClass.Mage: return 5;
                case JobClass.Priest: return 6;
                default: return 7;
            }
        }

        IEnumerator BattleLoop()
        {
            // Reset skill cooldowns for new battle
            var skillManager = GameManager.Instance?.SkillManager;
            skillManager?.ResetBattleSkills();
            
            while (CurrentBattleState == BattleState.InProgress)
            {
                // Update skill cooldowns
                if (skillManager != null)
                {
                    skillManager.UpdateSkillCooldowns(Time.deltaTime * battleSpeed);
                    skillManager.UpdateStatusEffects(Time.deltaTime * battleSpeed);
                }
                
                // Get current squad
                BattleSquad currentSquad = isPlayerTurn ? playerSquads[currentTurnSquadIndex] : enemySquads[currentTurnSquadIndex];
                BattleSquad[] targetSquads = isPlayerTurn ? enemySquads : playerSquads;

                if (!currentSquad.IsDefeated)
                {
                    OnSquadTurnStart?.Invoke(currentSquad, targetSquads[0]);
                    yield return StartCoroutine(ExecuteSquadTurn(currentSquad, targetSquads));
                }

                // Move to next squad
                currentTurnSquadIndex++;
                if (currentTurnSquadIndex >= SQUADS_PER_SIDE)
                {
                    currentTurnSquadIndex = 0;
                    isPlayerTurn = !isPlayerTurn;
                }

                // Check battle end conditions
                if (CheckBattleEnd())
                {
                    EndBattle();
                    yield break;
                }

                yield return new WaitForSeconds(actionDelay / battleSpeed);
            }
        }

        IEnumerator ExecuteSquadTurn(BattleSquad squad, BattleSquad[] enemySquads)
        {
            // Execute actions for each alive unit in the squad
            for (int row = 0; row < SQUAD_ROWS; row++)
            {
                for (int col = 0; col < SQUAD_COLS; col++)
                {
                    Unit unit = squad.Units[row, col];
                    if (unit != null && unit.IsAlive)
                    {
                        yield return StartCoroutine(ExecuteUnitAction(unit, enemySquads));
                    }
                }
            }
        }

        IEnumerator ExecuteUnitAction(Unit unit, BattleSquad[] enemySquads)
        {
            var skillManager = GameManager.Instance?.SkillManager;
            
            // Check if unit is silenced
            if (skillManager != null && skillManager.HasStatusEffect(unit, Battle.StatusEffectType.Silence))
            {
                // Can only do basic attacks when silenced
                yield return StartCoroutine(ExecuteBasicAttack(unit, enemySquads));
                yield break;
            }
            
            // Check if unit is stunned
            if (skillManager != null && skillManager.HasStatusEffect(unit, Battle.StatusEffectType.Stun))
            {
                // Skip turn when stunned
                yield return new WaitForSeconds(0.5f / battleSpeed);
                yield break;
            }
            
            // Try to use a skill first
            if (skillManager != null && TryUseSkill(unit, enemySquads))
            {
                yield return new WaitForSeconds(0.5f / battleSpeed);
            }
            else
            {
                // Determine action based on unit job/class
                switch (unit.JobClass)
                {
                    case JobClass.Priest:
                    case JobClass.Sage:
                        // Heal if allies need it
                        Unit healTarget = FindHealTarget(unit);
                        if (healTarget != null)
                        {
                            float healAmount = unit.GetHealPower();
                            healTarget.Heal(healAmount);
                            OnUnitHeal?.Invoke(healTarget, healAmount);
                            
                            if (unit.IsPlayerUnit)
                            {
                                currentBattleStats.TotalHealingDone += (int)healAmount;
                                if (!currentBattleStats.HealingBySquad.ContainsKey(unit.SquadIndex))
                                    currentBattleStats.HealingBySquad[unit.SquadIndex] = 0;
                                currentBattleStats.HealingBySquad[unit.SquadIndex] += (int)healAmount;
                            }
                            
                            yield return new WaitForSeconds(0.5f / battleSpeed);
                            break;
                        }
                        goto default; // If no heal target, attack
                        
                    default:
                        // Attack enemy
                        yield return StartCoroutine(ExecuteBasicAttack(unit, enemySquads));
                        break;
                }
            }
        }

        Unit FindAttackTarget(Unit attacker, BattleSquad[] enemySquads)
        {
            var tactic = attacker.IsPlayerUnit ? playerTactic : enemyTactic;
            List<Unit> potentialTargets = new List<Unit>();
            
            // Gather all alive enemy units
            foreach (var squad in enemySquads.Where(s => !s.IsDefeated))
            {
                for (int row = 0; row < SQUAD_ROWS; row++)
                {
                    for (int col = 0; col < SQUAD_COLS; col++)
                    {
                        Unit target = squad.Units[row, col];
                        if (target != null && target.IsAlive)
                        {
                            potentialTargets.Add(target);
                        }
                    }
                }
            }
            
            if (potentialTargets.Count == 0) return null;
            
            // Select target based on tactic
            switch (tactic)
            {
                case BattleTactic.Aggressive:
                    // Target lowest defense units
                    return potentialTargets.OrderBy(t => t.Defense).First();
                    
                case BattleTactic.Defensive:
                    // Target highest attack units (eliminate threats)
                    return potentialTargets.OrderByDescending(t => t.Attack + t.MagicPower).First();
                    
                case BattleTactic.Flanking:
                    // Target back row units
                    var backRowTargets = potentialTargets.Where(t => t.Row == SQUAD_ROWS - 1).ToList();
                    return backRowTargets.Count > 0 ? backRowTargets[UnityEngine.Random.Range(0, backRowTargets.Count)] : potentialTargets.First();
                    
                case BattleTactic.Focus:
                    // Target lowest health percentage
                    return potentialTargets.OrderBy(t => t.GetHealthPercentage()).First();
                    
                case BattleTactic.Blitz:
                    // Random target for chaos
                    return potentialTargets[UnityEngine.Random.Range(0, potentialTargets.Count)];
                    
                default: // Balanced
                    // Standard front-to-back targeting
                    return potentialTargets.OrderBy(t => t.Row).ThenBy(t => t.Col).First();
            }
        }

        Unit FindHealTarget(Unit healer)
        {
            BattleSquad[] allySquads = healer.IsPlayerUnit ? playerSquads : enemySquads;
            Unit lowestHealthUnit = null;
            float lowestHealthPercentage = 1f;

            foreach (var squad in allySquads)
            {
                for (int row = 0; row < SQUAD_ROWS; row++)
                {
                    for (int col = 0; col < SQUAD_COLS; col++)
                    {
                        Unit unit = squad.Units[row, col];
                        if (unit != null && unit.IsAlive && unit.CurrentHealth < unit.MaxHealth)
                        {
                            float healthPercentage = unit.CurrentHealth / unit.MaxHealth;
                            if (healthPercentage < lowestHealthPercentage)
                            {
                                lowestHealthPercentage = healthPercentage;
                                lowestHealthUnit = unit;
                            }
                        }
                    }
                }
            }

            return lowestHealthPercentage < 0.7f ? lowestHealthUnit : null;
        }

        bool CheckBattleEnd()
        {
            bool allPlayerSquadsDefeated = playerSquads.All(s => s.IsDefeated);
            bool allEnemySquadsDefeated = enemySquads.All(s => s.IsDefeated);

            if (allPlayerSquadsDefeated && allEnemySquadsDefeated)
            {
                CurrentBattleState = BattleState.Draw;
                return true;
            }
            else if (allPlayerSquadsDefeated)
            {
                CurrentBattleState = BattleState.Defeat;
                return true;
            }
            else if (allEnemySquadsDefeated)
            {
                CurrentBattleState = BattleState.Victory;
                return true;
            }

            return false;
        }

        void EndBattle()
        {
            currentBattleStats.BattleDuration = Time.time - battleStartTime;
            
            bool isVictory = CurrentBattleState == BattleState.Victory;
            
            // Log battle end
            string result = isVictory ? "승리!" : (CurrentBattleState == BattleState.Defeat ? "패배..." : "무승부");
            AddBattleEvent("BattleEnd", $"전투 종료 - {result}", null, null, currentBattleStats.BattleDuration);
            
            // Calculate rewards
            if (isVictory)
            {
                CalculateBattleRewards();
            }
            
            // Grant experience to surviving units
            GrantBattleExperience();
            
            OnBattleEnd?.Invoke(isVictory);
        }
        
        void CalculateBattleRewards()
        {
            // Base rewards
            int goldReward = 1000 + (currentBattleStats.UnitsKilled * 50);
            int expReward = 500 + (currentBattleStats.UnitsKilled * 25);
            
            // Bonus for quick victory
            if (currentBattleStats.BattleDuration < 60f)
            {
                goldReward = Mathf.RoundToInt(goldReward * 1.5f);
                expReward = Mathf.RoundToInt(expReward * 1.5f);
            }
            
            // Apply rewards
            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
            {
                resourceManager.AddResource(ResourceType.Gold, goldReward);
            }
            
            Debug.Log($"Battle Rewards - Gold: {goldReward}, Exp: {expReward}");
        }
        
        void GrantBattleExperience()
        {
            int baseExp = 100;
            
            foreach (var squad in playerSquads)
            {
                for (int row = 0; row < SQUAD_ROWS; row++)
                {
                    for (int col = 0; col < SQUAD_COLS; col++)
                    {
                        Unit unit = squad.Units[row, col];
                        if (unit != null && unit.IsAlive)
                        {
                            unit.experience += baseExp;
                            // TODO: Check for level up
                        }
                    }
                }
            }
        }

        public void SetBattleSpeed(float speed)
        {
            battleSpeed = Mathf.Clamp(speed, 1f, 4f);
        }

        public BattleStatistics GetBattleStatistics()
        {
            return currentBattleStats;
        }
        
        IEnumerator ExecuteBasicAttack(Unit unit, BattleSquad[] enemySquads)
        {
            Unit target = FindAttackTarget(unit, enemySquads);
            if (target != null)
            {
                float damage = unit.GetAttackDamage();
                target.TakeDamage(damage);
                OnUnitAttack?.Invoke(unit, target, damage);
                
                if (unit.IsPlayerUnit)
                {
                    currentBattleStats.TotalDamageDealt += (int)damage;
                    if (!currentBattleStats.DamageBySquad.ContainsKey(unit.SquadIndex))
                        currentBattleStats.DamageBySquad[unit.SquadIndex] = 0;
                    currentBattleStats.DamageBySquad[unit.SquadIndex] += (int)damage;
                }
                
                if (!target.IsAlive)
                {
                    OnUnitDeath?.Invoke(target);
                    if (unit.IsPlayerUnit)
                        currentBattleStats.UnitsKilled++;
                    else
                        currentBattleStats.UnitsLost++;
                }
                
                yield return new WaitForSeconds(0.5f / battleSpeed);
            }
        }
        
        bool TryUseSkill(Unit unit, BattleSquad[] enemySquads)
        {
            var skillManager = GameManager.Instance?.SkillManager;
            if (skillManager == null) return false;
            
            var availableSkills = skillManager.GetUnitSkills(unit)
                .Where(s => s.Type == Battle.SkillType.Active && s.CanUse(unit))
                .OrderByDescending(s => s.ManaCost) // Prioritize stronger skills
                .ToList();
            
            foreach (var skill in availableSkills)
            {
                List<Unit> targets = GetSkillTargets(unit, skill, enemySquads);
                if (targets.Count > 0)
                {
                    skillManager.UseSkill(unit, skill.SkillId, targets);
                    return true;
                }
            }
            
            return false;
        }
        
        List<Unit> GetSkillTargets(Unit caster, Battle.Skill skill, BattleSquad[] enemySquads)
        {
            List<Unit> targets = new List<Unit>();
            
            switch (skill.TargetType)
            {
                case Battle.SkillTargetType.Self:
                    targets.Add(caster);
                    break;
                    
                case Battle.SkillTargetType.SingleAlly:
                    var allyTarget = FindHealTarget(caster);
                    if (allyTarget != null) targets.Add(allyTarget);
                    break;
                    
                case Battle.SkillTargetType.SingleEnemy:
                    var enemyTarget = FindAttackTarget(caster, enemySquads);
                    if (enemyTarget != null) targets.Add(enemyTarget);
                    break;
                    
                case Battle.SkillTargetType.AllAllies:
                    var allySquads = caster.IsPlayerUnit ? playerSquads : enemySquads;
                    foreach (var squad in allySquads)
                    {
                        for (int row = 0; row < SQUAD_ROWS; row++)
                        {
                            for (int col = 0; col < SQUAD_COLS; col++)
                            {
                                var unit = squad.Units[row, col];
                                if (unit != null && unit.IsAlive)
                                    targets.Add(unit);
                            }
                        }
                    }
                    break;
                    
                case Battle.SkillTargetType.AllEnemies:
                    foreach (var squad in enemySquads)
                    {
                        for (int row = 0; row < SQUAD_ROWS; row++)
                        {
                            for (int col = 0; col < SQUAD_COLS; col++)
                            {
                                var unit = squad.Units[row, col];
                                if (unit != null && unit.IsAlive)
                                    targets.Add(unit);
                            }
                        }
                    }
                    break;
                    
                case Battle.SkillTargetType.Squad:
                    // Target caster's squad
                    var targetSquad = caster.IsPlayerUnit ? playerSquads[caster.SquadIndex] : enemySquads[caster.SquadIndex];
                    for (int row = 0; row < SQUAD_ROWS; row++)
                    {
                        for (int col = 0; col < SQUAD_COLS; col++)
                        {
                            var unit = targetSquad.Units[row, col];
                            if (unit != null && unit.IsAlive)
                                targets.Add(unit);
                        }
                    }
                    break;
                    
                // TODO: Implement Row, Column, and Area targeting
            }
            
            return targets;
        }
        
        // New methods for enhanced battle system
        void CalculateSquadSynergies()
        {
            // Calculate synergies for player squads
            foreach (var squad in playerSquads)
            {
                CalculateSquadSynergy(squad);
            }
            
            // Calculate synergies for enemy squads
            foreach (var squad in enemySquads)
            {
                CalculateSquadSynergy(squad);
            }
        }
        
        void CalculateSquadSynergy(BattleSquad squad)
        {
            var synergy = new SquadSynergy();
            int warriorCount = 0, knightCount = 0, mageCount = 0, priestCount = 0;
            
            for (int row = 0; row < SQUAD_ROWS; row++)
            {
                for (int col = 0; col < SQUAD_COLS; col++)
                {
                    Unit unit = squad.Units[row, col];
                    if (unit != null)
                    {
                        switch (unit.JobClass)
                        {
                            case JobClass.Warrior: warriorCount++; break;
                            case JobClass.Knight: knightCount++; break;
                            case JobClass.Mage: mageCount++; break;
                            case JobClass.Priest: priestCount++; break;
                        }
                    }
                }
            }
            
            // Apply synergy bonuses
            if (warriorCount >= 3) synergy.AttackBonus = 0.15f;
            if (knightCount >= 2) synergy.DefenseBonus = 0.2f;
            if (mageCount >= 2) synergy.AttackBonus += 0.1f;
            if (priestCount >= 1) synergy.HealingBonus = 0.25f;
            
            // Apply synergy to all units in squad
            ApplySquadSynergy(squad, synergy);
        }
        
        void ApplySquadSynergy(BattleSquad squad, SquadSynergy synergy)
        {
            for (int row = 0; row < SQUAD_ROWS; row++)
            {
                for (int col = 0; col < SQUAD_COLS; col++)
                {
                    Unit unit = squad.Units[row, col];
                    if (unit != null)
                    {
                        unit.attackPower *= (1 + synergy.AttackBonus);
                        unit.defense *= (1 + synergy.DefenseBonus);
                        unit.speed *= (1 + synergy.SpeedBonus);
                    }
                }
            }
        }
        
        void ApplyTacticEffects()
        {
            // Apply player tactic effects
            ApplyTacticToSquads(playerSquads, playerTactic);
            
            // Apply enemy tactic effects
            ApplyTacticToSquads(enemySquads, enemyTactic);
        }
        
        void ApplyTacticToSquads(BattleSquad[] squads, BattleTactic tactic)
        {
            foreach (var squad in squads)
            {
                for (int row = 0; row < SQUAD_ROWS; row++)
                {
                    for (int col = 0; col < SQUAD_COLS; col++)
                    {
                        Unit unit = squad.Units[row, col];
                        if (unit != null)
                        {
                            switch (tactic)
                            {
                                case BattleTactic.Aggressive:
                                    unit.attackPower *= 1.2f;
                                    unit.defense *= 0.9f;
                                    break;
                                case BattleTactic.Defensive:
                                    unit.defense *= 1.3f;
                                    unit.attackPower *= 0.85f;
                                    break;
                                case BattleTactic.Blitz:
                                    unit.speed *= 1.4f;
                                    unit.defense *= 0.8f;
                                    break;
                            }
                        }
                    }
                }
            }
        }
        
        public void SetPlayerTactic(BattleTactic tactic)
        {
            playerTactic = tactic;
        }
        
        public void SetEnemyTactic(BattleTactic tactic)
        {
            enemyTactic = tactic;
        }
        
        void AddBattleEvent(string eventType, string description, Unit actor, Unit target, float value)
        {
            battleLog.Add(new BattleEvent
            {
                Timestamp = Time.time - battleStartTime,
                EventType = eventType,
                Description = description,
                Actor = actor,
                Target = target,
                Value = value
            });
        }
        
        public List<BattleEvent> GetBattleLog()
        {
            return new List<BattleEvent>(battleLog);
        }
        
        public BattleSquad[] GetPlayerSquads()
        {
            return playerSquads;
        }
        
        public BattleSquad[] GetEnemySquads()
        {
            return enemySquads;
        }
    }
}