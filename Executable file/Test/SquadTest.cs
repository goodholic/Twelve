using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Systems;

namespace GuildMaster.Test
{
    /// <summary>
    /// Test script to verify 6x3 squad configuration
    /// </summary>
    public class SquadTest : MonoBehaviour
    {
        void Start()
        {
            TestSquadConfiguration();
            TestCharacterCollection();
            TestBattleManager();
        }

        void TestSquadConfiguration()
        {
            Debug.Log("=== Testing Squad Configuration ===");
            
            // Test Squad constants
            Debug.Log($"Squad Rows: {Squad.ROWS} (Expected: 3)");
            Debug.Log($"Squad Cols: {Squad.COLS} (Expected: 6)");
            Debug.Log($"Max Units per Squad: {Squad.MAX_UNITS} (Expected: 18)");
            
            // Create test squad
            Squad testSquad = new Squad("Test Squad", 0, true);
            
            // Try to add 18 units
            for (int i = 0; i < 20; i++) // Try to add more than max
            {
                Unit testUnit = new GameObject($"TestUnit_{i}").AddComponent<Unit>();
                testUnit.characterId = i;
                testUnit.Name = $"Unit {i}";
                testUnit.jobClass = (JobClass)(i % 7);
                
                bool added = testSquad.AddUnit(testUnit);
                if (i < 18)
                {
                    Debug.Log($"Added unit {i}: {added} (Should be true)");
                }
                else
                {
                    Debug.Log($"Added unit {i}: {added} (Should be false - squad full)");
                }
            }
            
            Debug.Log($"Squad unit count: {testSquad.GetUnitCount()} (Should be 18)");
            Debug.Log($"Squad is full: {testSquad.IsFull()} (Should be true)");
            
            // Cleanup
            testSquad.ClearSquad();
            Debug.Log($"After clear - Unit count: {testSquad.GetUnitCount()} (Should be 0)");
        }

        void TestCharacterCollection()
        {
            Debug.Log("\n=== Testing Character Collection ===");
            
            Debug.Log($"Max Characters: {CharacterCollection.MAX_CHARACTERS} (Expected: 36)");
            Debug.Log($"Squads Count: {CharacterCollection.SQUADS_COUNT} (Expected: 2)");
            Debug.Log($"Units per Squad: {CharacterCollection.UNITS_PER_SQUAD} (Expected: 18)");
            
            var collection = CharacterCollection.Instance;
            Debug.Log($"Collection initialized: {collection != null}");
        }

        void TestBattleManager()
        {
            Debug.Log("\n=== Testing Battle Manager ===");
            
            Debug.Log($"Squads per Side: {BattleManager.SQUADS_PER_SIDE} (Expected: 2)");
            Debug.Log($"Squad Rows: {BattleManager.SQUAD_ROWS} (Expected: 3)");
            Debug.Log($"Squad Cols: {BattleManager.SQUAD_COLS} (Expected: 6)");
            Debug.Log($"Units per Squad: {BattleManager.UNITS_PER_SQUAD} (Expected: 18)");
            Debug.Log($"Total Units per Side: {BattleManager.TOTAL_UNITS_PER_SIDE} (Expected: 36)");
        }

        void TestSquadBattleSystem()
        {
            Debug.Log("\n=== Testing Squad Battle System ===");
            
            Debug.Log($"Squads per Guild: {SquadBattleSystem.SQUADS_PER_GUILD} (Expected: 2)");
            Debug.Log($"Units per Squad: {SquadBattleSystem.UNITS_PER_SQUAD} (Expected: 18)");
            Debug.Log($"Squad Width: {SquadBattleSystem.SQUAD_WIDTH} (Expected: 6)");
            Debug.Log($"Squad Height: {SquadBattleSystem.SQUAD_HEIGHT} (Expected: 3)");
        }
    }
}