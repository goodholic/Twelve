using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Systems;
using GuildMaster.Core;

namespace GuildMaster.Test
{
    /// <summary>
    /// Test script to verify 6x3 grid configuration across all systems
    /// </summary>
    public class GridConfigurationTest : MonoBehaviour
    {
        [Header("Test Results")]
        [SerializeField] private bool allTestsPassed = false;
        [SerializeField] private string testSummary = "";
        
        void Start()
        {
            RunAllTests();
        }
        
        void RunAllTests()
        {
            Debug.Log("=== 6x3 Grid Configuration Test ===");
            
            bool squadTest = TestSquadConfiguration();
            bool battleSystemTest = TestSquadBattleSystem();
            bool collectionTest = TestCharacterCollection();
            bool battleManagerTest = TestBattleManager();
            
            allTestsPassed = squadTest && battleSystemTest && collectionTest && battleManagerTest;
            
            if (allTestsPassed)
            {
                testSummary = "✅ All systems correctly configured for 6x3 grids (18 units per squad)";
                Debug.Log($"<color=green>{testSummary}</color>");
            }
            else
            {
                testSummary = "❌ Some systems have incorrect configuration";
                Debug.LogError(testSummary);
            }
        }
        
        bool TestSquadConfiguration()
        {
            Debug.Log("\n--- Testing Squad.cs Configuration ---");
            
            bool passed = true;
            
            // Test constants
            if (Squad.ROWS != 3)
            {
                Debug.LogError($"Squad.ROWS = {Squad.ROWS}, expected 3");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ Squad.ROWS = {Squad.ROWS}");
            }
            
            if (Squad.COLS != 6)
            {
                Debug.LogError($"Squad.COLS = {Squad.COLS}, expected 6");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ Squad.COLS = {Squad.COLS}");
            }
            
            if (Squad.MAX_UNITS != 18)
            {
                Debug.LogError($"Squad.MAX_UNITS = {Squad.MAX_UNITS}, expected 18");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ Squad.MAX_UNITS = {Squad.MAX_UNITS}");
            }
            
            // Test grid dimensions
            int expectedGridSize = Squad.ROWS * Squad.COLS;
            if (expectedGridSize != 18)
            {
                Debug.LogError($"Grid size = {expectedGridSize} (3x6), expected 18");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ Grid size = {expectedGridSize} (3x6)");
            }
            
            return passed;
        }
        
        bool TestSquadBattleSystem()
        {
            Debug.Log("\n--- Testing SquadBattleSystem Configuration ---");
            
            bool passed = true;
            
            if (SquadBattleSystem.SQUADS_PER_GUILD != 2)
            {
                Debug.LogError($"SQUADS_PER_GUILD = {SquadBattleSystem.SQUADS_PER_GUILD}, expected 2");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ SQUADS_PER_GUILD = {SquadBattleSystem.SQUADS_PER_GUILD}");
            }
            
            if (SquadBattleSystem.UNITS_PER_SQUAD != 18)
            {
                Debug.LogError($"UNITS_PER_SQUAD = {SquadBattleSystem.UNITS_PER_SQUAD}, expected 18");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ UNITS_PER_SQUAD = {SquadBattleSystem.UNITS_PER_SQUAD}");
            }
            
            if (SquadBattleSystem.SQUAD_WIDTH != 6)
            {
                Debug.LogError($"SQUAD_WIDTH = {SquadBattleSystem.SQUAD_WIDTH}, expected 6");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ SQUAD_WIDTH = {SquadBattleSystem.SQUAD_WIDTH}");
            }
            
            if (SquadBattleSystem.SQUAD_HEIGHT != 3)
            {
                Debug.LogError($"SQUAD_HEIGHT = {SquadBattleSystem.SQUAD_HEIGHT}, expected 3");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ SQUAD_HEIGHT = {SquadBattleSystem.SQUAD_HEIGHT}");
            }
            
            return passed;
        }
        
        bool TestCharacterCollection()
        {
            Debug.Log("\n--- Testing CharacterCollection Configuration ---");
            
            bool passed = true;
            
            if (CharacterCollection.MAX_CHARACTERS != 36)
            {
                Debug.LogError($"MAX_CHARACTERS = {CharacterCollection.MAX_CHARACTERS}, expected 36");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ MAX_CHARACTERS = {CharacterCollection.MAX_CHARACTERS}");
            }
            
            if (CharacterCollection.SQUADS_COUNT != 2)
            {
                Debug.LogError($"SQUADS_COUNT = {CharacterCollection.SQUADS_COUNT}, expected 2");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ SQUADS_COUNT = {CharacterCollection.SQUADS_COUNT}");
            }
            
            if (CharacterCollection.UNITS_PER_SQUAD != 18)
            {
                Debug.LogError($"UNITS_PER_SQUAD = {CharacterCollection.UNITS_PER_SQUAD}, expected 18");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ UNITS_PER_SQUAD = {CharacterCollection.UNITS_PER_SQUAD}");
            }
            
            // Verify total calculation
            int totalUnits = CharacterCollection.SQUADS_COUNT * CharacterCollection.UNITS_PER_SQUAD;
            if (totalUnits != 36)
            {
                Debug.LogError($"Total units = {totalUnits}, expected 36");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ Total units = {totalUnits} (2 squads × 18 units)");
            }
            
            return passed;
        }
        
        bool TestBattleManager()
        {
            Debug.Log("\n--- Testing BattleManager Configuration ---");
            
            bool passed = true;
            
            if (BattleManager.SQUADS_PER_SIDE != 2)
            {
                Debug.LogError($"SQUADS_PER_SIDE = {BattleManager.SQUADS_PER_SIDE}, expected 2");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ SQUADS_PER_SIDE = {BattleManager.SQUADS_PER_SIDE}");
            }
            
            if (BattleManager.SQUAD_ROWS != 3)
            {
                Debug.LogError($"SQUAD_ROWS = {BattleManager.SQUAD_ROWS}, expected 3");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ SQUAD_ROWS = {BattleManager.SQUAD_ROWS}");
            }
            
            if (BattleManager.SQUAD_COLS != 6)
            {
                Debug.LogError($"SQUAD_COLS = {BattleManager.SQUAD_COLS}, expected 6");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ SQUAD_COLS = {BattleManager.SQUAD_COLS}");
            }
            
            if (BattleManager.UNITS_PER_SQUAD != 18)
            {
                Debug.LogError($"UNITS_PER_SQUAD = {BattleManager.UNITS_PER_SQUAD}, expected 18");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ UNITS_PER_SQUAD = {BattleManager.UNITS_PER_SQUAD}");
            }
            
            if (BattleManager.TOTAL_UNITS_PER_SIDE != 36)
            {
                Debug.LogError($"TOTAL_UNITS_PER_SIDE = {BattleManager.TOTAL_UNITS_PER_SIDE}, expected 36");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ TOTAL_UNITS_PER_SIDE = {BattleManager.TOTAL_UNITS_PER_SIDE}");
            }
            
            // Verify grid calculation
            int gridSize = BattleManager.SQUAD_ROWS * BattleManager.SQUAD_COLS;
            if (gridSize != 18)
            {
                Debug.LogError($"Grid size per squad = {gridSize}, expected 18");
                passed = false;
            }
            else
            {
                Debug.Log($"✓ Grid size per squad = {gridSize} (3×6)");
            }
            
            return passed;
        }
        
        public void RunTestInEditor()
        {
            RunAllTests();
        }
    }
}