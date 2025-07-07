# Unit Component System Documentation

## Overview
The Unit class cannot be used directly as a MonoBehaviour component due to Unity's ExtensionOfNativeClass restrictions. Instead, use the `UnitComponent` wrapper class.

## Problem
- `Unit` is a pure C# class (not derived from MonoBehaviour)
- Unity cannot serialize or use non-MonoBehaviour classes as components
- Attempting to use `GetComponent<Unit>()` or `AddComponent<Unit>()` causes errors

## Solution
Use `UnitComponent` as a MonoBehaviour wrapper that contains Unit data.

### Basic Usage

```csharp
// WRONG - This will cause errors
Unit unit = gameObject.GetComponent<Unit>();
gameObject.AddComponent<Unit>();

// CORRECT - Use UnitComponent
UnitComponent unitComp = gameObject.GetComponent<UnitComponent>();
Unit unit = unitComp.UnitData;

// Or use extension methods
Unit unit = gameObject.GetUnit();
```

### Creating Units

```csharp
// Method 1: Using UnitComponent directly
UnitComponent unitComp = gameObject.AddComponent<UnitComponent>();
unitComp.CreateUnit("Warrior", 5, JobClass.Warrior, Rarity.Rare);

// Method 2: Using extension methods
Unit unit = gameObject.CreateUnit("Mage", 10, JobClass.Mage, Rarity.Epic);

// Method 3: Setting existing Unit data
Unit myUnit = new Unit("Priest", 15, JobClass.Priest);
gameObject.SetUnit(myUnit);
```

### Finding Units

```csharp
// Find all units in scene
List<Unit> allUnits = UnitUtility.FindAllUnits();

// Find units by team
List<Unit> playerUnits = UnitUtility.FindUnitsByTeam(true);
List<Unit> enemyUnits = UnitUtility.FindUnitsByTeam(false);

// Find units by job class
List<Unit> warriors = UnitUtility.FindUnitsByJobClass(JobClass.Warrior);

// Using extension methods
List<Unit> enemies = myUnit.GetEnemyUnits();
Unit closestEnemy = myUnit.GetClosestEnemy();
List<Unit> unitsInRange = myUnit.GetUnitsInRange(10f);
```

## Available Tools

### 1. Unit Component Fixer (Menu: GuildMaster/Tools/Fix Unit Component Errors)
- Scans for missing script references
- Removes null components
- Converts GameObjects to use UnitComponent
- Creates unit prefab templates

### 2. Unit Reference Converter (Menu: GuildMaster/Tools/Convert Unit References)
- Scans code for problematic Unit usage
- Automatically converts `GetComponent<Unit>()` to `GetComponent<UnitComponent>().UnitData`
- Updates AddComponent and FindObjectOfType calls
- Adds necessary using statements

### 3. Unit Component Validator (Menu: GuildMaster/Validate/...)
- Validates Unit components in current scene
- Checks all scenes for issues
- Auto-fixes common problems
- Optional auto-validation on scene load

## Extension Methods

The `UnitExtensions` class provides helpful methods:

```csharp
// GameObject extensions
unit = gameObject.GetUnit();
bool hasUnit = gameObject.HasUnit();
gameObject.TryGetUnit(out Unit unit);
List<Unit> units = gameObject.GetUnitsInChildren();

// Unit extensions
List<Unit> enemies = unit.GetEnemyUnits();
List<Unit> allies = unit.GetAllyUnits();
Unit closest = unit.GetClosestEnemy();
bool canAttack = unit.CanAttack(target);
float damage = attacker.DealDamage(target, 100f);
string info = unit.GetDebugInfo();
```

## Migration Guide

If you have existing code using Unit as a component:

1. Run "GuildMaster/Tools/Convert Unit References" to automatically update your code
2. Run "GuildMaster/Tools/Fix Unit Component Errors" to fix scene objects
3. Test your game thoroughly
4. Save all modified scenes and scripts

## Best Practices

1. Always use `UnitComponent` for GameObjects, never `Unit` directly
2. Use the provided extension methods for cleaner code
3. Run the validator tools after major changes
4. Keep Unit as a data class, put MonoBehaviour logic in separate components

## Common Patterns

### Battle System Integration
```csharp
public class BattleController : MonoBehaviour
{
    void Start()
    {
        // Find all units
        var allUnits = UnitUtility.FindAllAliveUnits();
        
        // Sort by speed for turn order
        var turnOrder = UnitUtility.SortByInitiative(allUnits);
        
        // Process turns
        foreach (var unit in turnOrder)
        {
            ProcessUnitTurn(unit);
        }
    }
    
    void ProcessUnitTurn(Unit unit)
    {
        if (!unit.isAlive) return;
        
        var target = unit.GetClosestEnemy();
        if (target != null && unit.CanAttack(target))
        {
            unit.DealDamage(target, unit.attackPower);
        }
    }
}
```

### Unit Spawning
```csharp
public class UnitSpawner : MonoBehaviour
{
    public GameObject unitPrefab;
    
    public void SpawnUnit(string name, int level, JobClass job)
    {
        GameObject go = Instantiate(unitPrefab);
        Unit unit = go.CreateUnit(name, level, job);
        
        // Position the unit
        unit.transform.position = GetSpawnPosition();
    }
}
```

## Troubleshooting

### "ExtensionOfNativeClass" Error
- You're trying to use Unit as a component
- Solution: Use UnitComponent instead

### "Missing Script" on GameObjects
- The Unit script was previously attached
- Solution: Run the Unit Component Fixer tool

### GetComponent<Unit>() returns null
- The GameObject has UnitComponent, not Unit
- Solution: Use GetComponent<UnitComponent>().UnitData or gameObject.GetUnit()

### Prefab has missing components
- Unit was saved as a component in the prefab
- Solution: Open prefab, remove missing scripts, add UnitComponent