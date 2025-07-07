using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;

namespace GuildMaster.Battle
{
    /// <summary>
    /// Extension methods for Unit and UnitComponent to make them easier to work with
    /// </summary>
    public static class UnitExtensions
    {
        /// <summary>
        /// Get the Unit data from a GameObject
        /// </summary>
        public static Unit GetUnit(this GameObject go)
        {
            if (go == null) return null;
            var component = go.GetComponent<UnitComponent>();
            return component?.UnitData;
        }
        
        /// <summary>
        /// Get the Unit data from a Transform
        /// </summary>
        public static Unit GetUnit(this Transform transform)
        {
            if (transform == null) return null;
            return transform.gameObject.GetUnit();
        }
        
        /// <summary>
        /// Check if a GameObject has a Unit
        /// </summary>
        public static bool HasUnit(this GameObject go)
        {
            if (go == null) return false;
            var component = go.GetComponent<UnitComponent>();
            return component != null && component.UnitData != null;
        }
        
        /// <summary>
        /// Try to get Unit data from a GameObject
        /// </summary>
        public static bool TryGetUnit(this GameObject go, out Unit unit)
        {
            unit = null;
            if (go == null) return false;
            
            var component = go.GetComponent<UnitComponent>();
            if (component != null && component.UnitData != null)
            {
                unit = component.UnitData;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Add or get UnitComponent from a GameObject
        /// </summary>
        public static UnitComponent GetOrAddUnitComponent(this GameObject go)
        {
            if (go == null) return null;
            
            var component = go.GetComponent<UnitComponent>();
            if (component == null)
            {
                component = go.AddComponent<UnitComponent>();
            }
            return component;
        }
        
        /// <summary>
        /// Set Unit data on a GameObject
        /// </summary>
        public static void SetUnit(this GameObject go, Unit unit)
        {
            if (go == null || unit == null) return;
            
            var component = go.GetOrAddUnitComponent();
            component.SetUnit(unit);
        }
        
        /// <summary>
        /// Create and set a new Unit on a GameObject
        /// </summary>
        public static Unit CreateUnit(this GameObject go, string name, int level, JobClass jobClass, Rarity rarity = Rarity.Common)
        {
            if (go == null) return null;
            
            var component = go.GetOrAddUnitComponent();
            component.CreateUnit(name, level, jobClass, rarity);
            return component.UnitData;
        }
        
        /// <summary>
        /// Find all Units in children
        /// </summary>
        public static List<Unit> GetUnitsInChildren(this GameObject go, bool includeInactive = false)
        {
            if (go == null) return new List<Unit>();
            
            var units = new List<Unit>();
            var components = go.GetComponentsInChildren<UnitComponent>(includeInactive);
            
            foreach (var comp in components)
            {
                if (comp.UnitData != null)
                {
                    units.Add(comp.UnitData);
                }
            }
            
            return units;
        }
        
        /// <summary>
        /// Find all enemy Units
        /// </summary>
        public static List<Unit> GetEnemyUnits(this Unit unit)
        {
            if (unit == null) return new List<Unit>();
            
            var allUnits = GameObject.FindObjectsOfType<UnitComponent>()
                .Select(c => c.UnitData)
                .Where(u => u != null && u.isAlive);
            
            return allUnits.Where(u => u.isPlayerUnit != unit.isPlayerUnit).ToList();
        }
        
        /// <summary>
        /// Find all ally Units
        /// </summary>
        public static List<Unit> GetAllyUnits(this Unit unit)
        {
            if (unit == null) return new List<Unit>();
            
            var allUnits = GameObject.FindObjectsOfType<UnitComponent>()
                .Select(c => c.UnitData)
                .Where(u => u != null && u.isAlive);
            
            return allUnits.Where(u => u.isPlayerUnit == unit.isPlayerUnit && u != unit).ToList();
        }
        
        /// <summary>
        /// Get the closest enemy Unit
        /// </summary>
        public static Unit GetClosestEnemy(this Unit unit)
        {
            if (unit == null || unit.transform == null) return null;
            
            var enemies = unit.GetEnemyUnits();
            if (enemies.Count == 0) return null;
            
            Unit closest = null;
            float closestDistance = float.MaxValue;
            
            foreach (var enemy in enemies)
            {
                if (enemy.transform == null) continue;
                
                float distance = Vector3.Distance(unit.transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = enemy;
                }
            }
            
            return closest;
        }
        
        /// <summary>
        /// Get Units within range
        /// </summary>
        public static List<Unit> GetUnitsInRange(this Unit unit, float range, bool includeEnemies = true, bool includeAllies = false)
        {
            if (unit == null || unit.transform == null) return new List<Unit>();
            
            var result = new List<Unit>();
            var allUnits = GameObject.FindObjectsOfType<UnitComponent>()
                .Select(c => c.UnitData)
                .Where(u => u != null && u.isAlive && u != unit);
            
            foreach (var other in allUnits)
            {
                if (other.transform == null) continue;
                
                bool isEnemy = other.isPlayerUnit != unit.isPlayerUnit;
                if ((isEnemy && !includeEnemies) || (!isEnemy && !includeAllies))
                    continue;
                
                float distance = Vector3.Distance(unit.transform.position, other.transform.position);
                if (distance <= range)
                {
                    result.Add(other);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Check if Unit can attack target
        /// </summary>
        public static bool CanAttack(this Unit unit, Unit target)
        {
            if (unit == null || target == null) return false;
            if (!unit.isAlive || !target.isAlive) return false;
            if (unit.isPlayerUnit == target.isPlayerUnit) return false; // Can't attack allies
            
            // Add more conditions as needed (range, line of sight, etc.)
            return true;
        }
        
        /// <summary>
        /// Apply damage with full calculation
        /// </summary>
        public static float DealDamage(this Unit attacker, Unit target, float baseDamage, bool isMagical = false, bool canCrit = true)
        {
            if (attacker == null || target == null || !target.isAlive) return 0f;
            
            float damage = baseDamage;
            
            // Apply attacker's power
            if (isMagical)
            {
                damage *= attacker.effectiveMagicPower / 100f;
            }
            else
            {
                damage *= attacker.effectiveAttackPower / 100f;
            }
            
            // Apply critical hit
            if (canCrit && attacker.GetCriticalHit())
            {
                damage *= attacker.criticalDamage;
            }
            
            // Apply to target
            target.TakeDamage(damage, isMagical);
            
            return damage;
        }
        
        /// <summary>
        /// Quick unit info for debugging
        /// </summary>
        public static string GetDebugInfo(this Unit unit)
        {
            if (unit == null) return "null";
            
            return $"{unit.unitName} Lv.{unit.level} {unit.jobClass} " +
                   $"HP:{unit.currentHP:F0}/{unit.maxHP:F0} " +
                   $"ATK:{unit.attackPower:F0} DEF:{unit.defense:F0}";
        }
    }
    
    /// <summary>
    /// Helper class for Unit-related utilities
    /// </summary>
    public static class UnitUtility
    {
        /// <summary>
        /// Find all Units in the scene
        /// </summary>
        public static List<Unit> FindAllUnits()
        {
            return GameObject.FindObjectsOfType<UnitComponent>()
                .Select(c => c.UnitData)
                .Where(u => u != null)
                .ToList();
        }
        
        /// <summary>
        /// Find all alive Units
        /// </summary>
        public static List<Unit> FindAllAliveUnits()
        {
            return FindAllUnits().Where(u => u.isAlive).ToList();
        }
        
        /// <summary>
        /// Find Units by team
        /// </summary>
        public static List<Unit> FindUnitsByTeam(bool isPlayerTeam)
        {
            return FindAllAliveUnits().Where(u => u.isPlayerUnit == isPlayerTeam).ToList();
        }
        
        /// <summary>
        /// Find Units by job class
        /// </summary>
        public static List<Unit> FindUnitsByJobClass(JobClass jobClass)
        {
            return FindAllAliveUnits().Where(u => u.jobClass == jobClass).ToList();
        }
        
        /// <summary>
        /// Create a Unit GameObject from data
        /// </summary>
        public static GameObject CreateUnitGameObject(Unit unitData, GameObject prefab = null)
        {
            GameObject go;
            
            if (prefab != null)
            {
                go = GameObject.Instantiate(prefab);
            }
            else
            {
                go = new GameObject($"Unit_{unitData.unitName}");
            }
            
            go.SetUnit(unitData);
            return go;
        }
        
        /// <summary>
        /// Sort Units by initiative (for turn order)
        /// </summary>
        public static List<Unit> SortByInitiative(List<Unit> units)
        {
            return units.OrderByDescending(u => u.speed).ToList();
        }
        
        /// <summary>
        /// Calculate team power
        /// </summary>
        public static int CalculateTeamPower(List<Unit> units)
        {
            return units.Sum(u => u.GetCombatPower());
        }
    }
}