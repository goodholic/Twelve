using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // JobClass를 위해 추가

namespace GuildMaster.Data
{
    [CreateAssetMenu(fileName = "TileConquestDatabase", menuName = "GuildMaster/Data/TileConquestDatabase")]
    public class TileConquestDatabaseSO : ScriptableObject
    {
        [Header("Tile Conquest Data")]
        public List<TileData> tiles = new List<TileData>();
        public List<ConquestReward> conquestRewards = new List<ConquestReward>();
        
        [Header("Difficulty Settings")]
        public List<DifficultyLevel> difficultyLevels = new List<DifficultyLevel>();
        
        [System.Serializable]
        public class TileData
        {
            public string tileId;
            public string tileName;
            public int x;
            public int y;
            public TileType tileType;
            public int difficulty;
            public bool isConquered;
            public List<string> requiredTiles = new List<string>();
            public List<EnemySquad> enemySquads = new List<EnemySquad>();
            public ConquestReward reward;
            
            public enum TileType
            {
                Normal,
                Elite,
                Boss,
                Treasure,
                Event
            }
        }
        
        [System.Serializable]
        public class EnemySquad
        {
            public string squadName;
            public List<EnemyUnit> units = new List<EnemyUnit>();
            public AIBehaviorSO aiBehavior;
        }
        
        [System.Serializable]
        public class EnemyUnit
        {
            public string unitId;
            public int level;
            public JobClass jobClass;
            public Rarity rarity;
            public Vector2Int position;
        }
        
        [System.Serializable]
        public class ConquestReward
        {
            public int gold;
            public int experience;
            public List<ItemReward> items = new List<ItemReward>();
            public List<string> unlockedTiles = new List<string>();
        }
        
        [System.Serializable]
        public class ItemReward
        {
            public string itemId;
            public int quantity;
            public float dropChance = 1f;
        }
        
        [System.Serializable]
        public class DifficultyLevel
        {
            public int difficulty;
            public string difficultyName;
            public float enemyStatMultiplier = 1f;
            public float rewardMultiplier = 1f;
            public int recommendedLevel;
        }
        
        public TileData GetTile(string tileId)
        {
            return tiles.FirstOrDefault(t => t.tileId == tileId);
        }
        
        public TileData GetTileAt(int x, int y)
        {
            return tiles.FirstOrDefault(t => t.x == x && t.y == y);
        }
        
        public List<TileData> GetAvailableTiles()
        {
            return tiles.Where(t => !t.isConquered && AreRequirementsMet(t)).ToList();
        }
        
        public bool AreRequirementsMet(TileData tile)
        {
            if (tile.requiredTiles == null || tile.requiredTiles.Count == 0)
                return true;
                
            foreach (var requiredTileId in tile.requiredTiles)
            {
                var requiredTile = GetTile(requiredTileId);
                if (requiredTile == null || !requiredTile.isConquered)
                    return false;
            }
            
            return true;
        }
        
        public void ConquerTile(string tileId)
        {
            var tile = GetTile(tileId);
            if (tile != null)
            {
                tile.isConquered = true;
                EditorUtility.SetDirty(this);
            }
        }
        
        public DifficultyLevel GetDifficultyLevel(int difficulty)
        {
            return difficultyLevels.FirstOrDefault(d => d.difficulty == difficulty) ?? 
                   new DifficultyLevel { difficulty = difficulty, difficultyName = "Unknown" };
        }
        
        public List<EnemyUnit> GenerateEnemyUnits(TileData tile)
        {
            var allUnits = new List<EnemyUnit>();
            var difficultyLevel = GetDifficultyLevel(tile.difficulty);
            
            foreach (var squad in tile.enemySquads)
            {
                foreach (var unit in squad.units)
                {
                    // Apply difficulty scaling
                    var scaledUnit = new EnemyUnit
                    {
                        unitId = unit.unitId,
                        level = Mathf.RoundToInt(unit.level * difficultyLevel.enemyStatMultiplier),
                        jobClass = unit.jobClass,
                        rarity = unit.rarity,
                        position = unit.position
                    };
                    
                    allUnits.Add(scaledUnit);
                }
            }
            
            return allUnits;
        }
        
        public void ResetConquest()
        {
            foreach (var tile in tiles)
            {
                tile.isConquered = false;
            }
            EditorUtility.SetDirty(this);
        }
        
        public float GetConquestProgress()
        {
            if (tiles.Count == 0) return 0f;
            
            int conqueredCount = tiles.Count(t => t.isConquered);
            return (float)conqueredCount / tiles.Count;
        }
        
        public List<TileData> GetTilesByType(TileData.TileType type)
        {
            return tiles.Where(t => t.tileType == type).ToList();
        }
    }
}