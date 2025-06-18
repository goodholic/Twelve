using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace pjy.Gameplay
{
    public class AIBrain : MonoBehaviour
    {
        [Header("AI Brain Settings")]
        [SerializeField] private float thinkingSpeed = 1f;
        [SerializeField] private float learningRate = 0.1f;
        
        [Header("Decision Weights")]
        [SerializeField] private float summonWeight = 0.4f;
        [SerializeField] private float mergeWeight = 0.3f;
        [SerializeField] private float positioningWeight = 0.3f;
        
        [Header("State Analysis")]
        [SerializeField] private int enemyCharacterCount;
        [SerializeField] private int allyCharacterCount;
        [SerializeField] private float threatLevel;
        [SerializeField] private float economicAdvantage;
        
        private AIPlayer aiPlayer;
        private Dictionary<string, float> characterEffectiveness = new Dictionary<string, float>();
        private List<DecisionRecord> decisionHistory = new List<DecisionRecord>();
        
        [System.Serializable]
        private class DecisionRecord
        {
            public float timestamp;
            public string decision;
            public float successScore;
        }
        
        public void Initialize(AIPlayer player)
        {
            aiPlayer = player;
            StartCoroutine(AnalyzeGameState());
        }
        
        private IEnumerator AnalyzeGameState()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f / thinkingSpeed);
                
                UpdateGameStateAnalysis();
                EvaluateThreats();
                UpdateDecisionWeights();
            }
        }
        
        private void UpdateGameStateAnalysis()
        {
            Character[] allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
            
            enemyCharacterCount = 0;
            allyCharacterCount = 0;
            
            foreach (Character character in allCharacters)
            {
                if (character.areaIndex == aiPlayer.AreaIndex)
                {
                    allyCharacterCount++;
                }
                else
                {
                    enemyCharacterCount++;
                }
            }
            
            economicAdvantage = (float)aiPlayer.Gold / 100f;
        }
        
        private void EvaluateThreats()
        {
            threatLevel = 0f;
            
            if (enemyCharacterCount > allyCharacterCount)
            {
                threatLevel += (enemyCharacterCount - allyCharacterCount) * 0.1f;
            }
            
            Monster[] monsters = FindObjectsByType<Monster>(FindObjectsSortMode.None);
            int nearbyMonsters = monsters.Count(m => m.areaIndex != aiPlayer.AreaIndex);
            threatLevel += nearbyMonsters * 0.05f;
            
            FinalCastle castle = FindObjectsByType<FinalCastle>(FindObjectsSortMode.None)
                .FirstOrDefault(c => c.areaIndex == aiPlayer.AreaIndex);
                
            if (castle != null)
            {
                float healthPercent = castle.currentHealth / castle.maxHealth;
                if (healthPercent < 0.5f)
                {
                    threatLevel += (0.5f - healthPercent) * 2f;
                }
            }
            
            threatLevel = Mathf.Clamp01(threatLevel);
        }
        
        private void UpdateDecisionWeights()
        {
            if (threatLevel > 0.7f)
            {
                summonWeight = 0.6f;
                mergeWeight = 0.1f;
                positioningWeight = 0.3f;
            }
            else if (threatLevel < 0.3f)
            {
                summonWeight = 0.3f;
                mergeWeight = 0.4f;
                positioningWeight = 0.3f;
            }
            else
            {
                summonWeight = 0.4f;
                mergeWeight = 0.3f;
                positioningWeight = 0.3f;
            }
        }
        
        public void CheckAndPerformMerges()
        {
            List<Tile> occupiedTiles = aiPlayer.GetOccupiedTiles();
            
            foreach (Tile tile in occupiedTiles)
            {
                List<Character> characters = tile.GetOccupyingCharacters();
                if (characters.Count >= 3)
                {
                    Character first = characters[0];
                    bool canMerge = characters.All(c => 
                        c.characterName == first.characterName && 
                        c.star == first.star &&
                        first.star != CharacterStar.ThreeStar);
                        
                    if (canMerge)
                    {
                        AutoMergeManager mergeManager = FindFirstObjectByType<AutoMergeManager>();
                        if (mergeManager != null)
                        {
                            Character[] mergeTargets = characters.Take(3).ToArray();
                            mergeManager.MergeCharacters(mergeTargets);
                            
                            RecordDecision("Merge", 1f);
                            Debug.Log($"[AIBrain] Successfully merged 3x {first.characterName} {first.star}");
                        }
                    }
                }
            }
        }
        
        public CharacterData SelectBestCharacterToSummon(List<CharacterData> availableCharacters)
        {
            if (availableCharacters.Count == 0) return null;
            
            CharacterData bestChoice = null;
            float bestScore = -1f;
            
            foreach (CharacterData character in availableCharacters)
            {
                if (character.cost > aiPlayer.Gold) continue;
                
                float score = EvaluateCharacter(character);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestChoice = character;
                }
            }
            
            return bestChoice;
        }
        
        private float EvaluateCharacter(CharacterData character)
        {
            float score = 0f;
            
            float damageScore = character.attackPower / 100f;
            float healthScore = character.health / 1000f;
            float costEfficiency = (damageScore + healthScore) / (character.cost / 10f);
            score += costEfficiency * 0.4f;
            
            if (characterEffectiveness.ContainsKey(character.characterName))
            {
                score += characterEffectiveness[character.characterName] * 0.3f;
            }
            
            if (threatLevel > 0.5f)
            {
                score += damageScore * 0.3f;
            }
            else
            {
                score += healthScore * 0.3f;
            }
            
            return score;
        }
        
        public Tile SelectBestTile(List<Tile> availableTiles, CharacterData characterToPlace)
        {
            if (availableTiles.Count == 0) return null;
            
            Tile bestTile = null;
            float bestScore = -1f;
            
            foreach (Tile tile in availableTiles)
            {
                float score = EvaluateTile(tile, characterToPlace);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }
            
            return bestTile;
        }
        
        private float EvaluateTile(Tile tile, CharacterData character)
        {
            float score = 0f;
            
            List<Character> occupyingChars = tile.GetOccupyingCharacters();
            if (occupyingChars.Count > 0 && occupyingChars.Count < 3)
            {
                Character first = occupyingChars[0];
                if (first.characterName == character.characterName && first.star == character.star)
                {
                    score += 0.5f;
                }
            }
            
            float xPos = tile.transform.position.x;
            if (aiPlayer.AreaIndex == 1)
            {
                if (threatLevel > 0.5f)
                {
                    score += (10f - xPos) / 20f;
                }
                else
                {
                    score += xPos / 20f;
                }
            }
            else
            {
                if (threatLevel > 0.5f)
                {
                    score += (xPos + 10f) / 20f;
                }
                else
                {
                    score += (10f - xPos) / 20f;
                }
            }
            
            return score;
        }
        
        public void OnCharacterSummoned(Character character)
        {
            string key = character.characterName;
            if (!characterEffectiveness.ContainsKey(key))
            {
                characterEffectiveness[key] = 0.5f;
            }
        }
        
        public void OnCharacterDeath(Character character)
        {
            string key = character.characterName;
            if (characterEffectiveness.ContainsKey(key))
            {
                float lifespan = character.gameObject ? Time.time : 0f;
                float effectiveness = Mathf.Clamp01(lifespan / 60f);
                
                characterEffectiveness[key] = Mathf.Lerp(
                    characterEffectiveness[key],
                    effectiveness,
                    learningRate
                );
            }
        }
        
        private void RecordDecision(string decision, float success)
        {
            DecisionRecord record = new DecisionRecord
            {
                timestamp = Time.time,
                decision = decision,
                successScore = success
            };
            
            decisionHistory.Add(record);
            
            if (decisionHistory.Count > 100)
            {
                decisionHistory.RemoveAt(0);
            }
        }
        
        public float GetThreatLevel() => threatLevel;
        public float GetEconomicAdvantage() => economicAdvantage;
        public int GetEnemyCount() => enemyCharacterCount;
        public int GetAllyCount() => allyCharacterCount;
    }
}