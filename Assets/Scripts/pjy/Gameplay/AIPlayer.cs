using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace pjy.Gameplay
{
    public class AIPlayer : BasePlayer
    {
        [Header("AI Settings")]
        [SerializeField] private float decisionInterval = 2f;
        [SerializeField] private float summonChance = 0.7f;
        [SerializeField] private float mergeCheckInterval = 5f;
        
        [Header("AI Strategy")]
        [SerializeField] private AIStrategy strategy = AIStrategy.Balanced;
        [SerializeField] private float aggressiveness = 0.5f;
        [SerializeField] private float defensiveness = 0.5f;
        
        private float nextDecisionTime;
        private float nextMergeCheckTime;
        private AIBrain brain;
        
        public enum AIStrategy
        {
            Aggressive,
            Defensive,
            Balanced,
            Economic
        }
        
        protected override void InitializePlayer()
        {
            base.InitializePlayer();
            isAI = true;
            playerName = "AI Player " + playerID;
            
            brain = GetComponent<AIBrain>();
            if (brain == null)
            {
                brain = gameObject.AddComponent<AIBrain>();
            }
            
            brain.Initialize(this);
        }
        
        protected override void OnGameUpdate()
        {
            base.OnGameUpdate();
            
            if (Time.time >= nextDecisionTime)
            {
                MakeDecision();
                nextDecisionTime = Time.time + decisionInterval;
            }
            
            if (Time.time >= nextMergeCheckTime)
            {
                CheckForMerges();
                nextMergeCheckTime = Time.time + mergeCheckInterval;
            }
        }
        
        private void MakeDecision()
        {
            if (Random.value < summonChance && CanSummonCharacter())
            {
                TrySmartSummon();
            }
            
            if (Random.value < 0.3f)
            {
                TryRearrangeCharacters();
            }
        }
        
        private void TrySmartSummon()
        {
            List<Tile> availableTiles = GetAvailableTiles();
            if (availableTiles.Count == 0 || deck.Count == 0) return;
            
            CharacterData characterToSummon = null;
            Tile targetTile = null;
            
            switch (strategy)
            {
                case AIStrategy.Aggressive:
                    characterToSummon = GetHighestDamageCharacter();
                    targetTile = GetFrontlineTile(availableTiles);
                    break;
                    
                case AIStrategy.Defensive:
                    characterToSummon = GetHighestHealthCharacter();
                    targetTile = GetBacklineTile(availableTiles);
                    break;
                    
                case AIStrategy.Balanced:
                    characterToSummon = GetBalancedCharacter();
                    targetTile = GetStrategicTile(availableTiles);
                    break;
                    
                case AIStrategy.Economic:
                    characterToSummon = GetCheapestCharacter();
                    targetTile = availableTiles[Random.Range(0, availableTiles.Count)];
                    break;
            }
            
            if (characterToSummon != null && targetTile != null)
            {
                TrySummonCharacter(characterToSummon, targetTile);
            }
        }
        
        private CharacterData GetHighestDamageCharacter()
        {
            return deck.Where(c => c.cost <= gold)
                      .OrderByDescending(c => c.attackPower)
                      .FirstOrDefault();
        }
        
        private CharacterData GetHighestHealthCharacter()
        {
            return deck.Where(c => c.cost <= gold)
                      .OrderByDescending(c => c.health)
                      .FirstOrDefault();
        }
        
        private CharacterData GetBalancedCharacter()
        {
            return deck.Where(c => c.cost <= gold)
                      .OrderByDescending(c => c.attackPower * c.health / c.cost)
                      .FirstOrDefault();
        }
        
        private CharacterData GetCheapestCharacter()
        {
            return deck.Where(c => c.cost <= gold)
                      .OrderBy(c => c.cost)
                      .FirstOrDefault();
        }
        
        private Tile GetFrontlineTile(List<Tile> tiles)
        {
            if (areaIndex == 1)
            {
                return tiles.OrderByDescending(t => t.transform.position.x).FirstOrDefault();
            }
            else
            {
                return tiles.OrderBy(t => t.transform.position.x).FirstOrDefault();
            }
        }
        
        private Tile GetBacklineTile(List<Tile> tiles)
        {
            if (areaIndex == 1)
            {
                return tiles.OrderBy(t => t.transform.position.x).FirstOrDefault();
            }
            else
            {
                return tiles.OrderByDescending(t => t.transform.position.x).FirstOrDefault();
            }
        }
        
        private Tile GetStrategicTile(List<Tile> tiles)
        {
            List<Tile> occupiedTiles = GetOccupiedTiles();
            
            foreach (Tile occupied in occupiedTiles)
            {
                List<Character> characters = occupied.GetOccupyingCharacters();
                if (characters.Count > 0 && characters.Count < 3)
                {
                    Character first = characters[0];
                    if (deck.Any(d => d.characterName == first.characterName && d.star == first.star))
                    {
                        return occupied;
                    }
                }
            }
            
            int routeChoice = Random.Range(0, 3);
            List<Tile> routeTiles = tiles.Where(t => GetTileRoute(t) == routeChoice).ToList();
            
            if (routeTiles.Count > 0)
            {
                return routeTiles[Random.Range(0, routeTiles.Count)];
            }
            
            return tiles[Random.Range(0, tiles.Count)];
        }
        
        private int GetTileRoute(Tile tile)
        {
            float y = tile.transform.position.y;
            if (y > 1.5f) return 0;
            if (y < -1.5f) return 2;
            return 1;
        }
        
        private void TryRearrangeCharacters()
        {
            if (ownedCharacters.Count < 2) return;
            
            List<Tile> occupiedTiles = GetOccupiedTiles();
            foreach (Tile tile in occupiedTiles)
            {
                List<Character> characters = tile.GetOccupyingCharacters();
                if (characters.Count == 2)
                {
                    Character matchingChar = ownedCharacters.FirstOrDefault(c => 
                        c.currentTile != tile &&
                        c.characterName == characters[0].characterName &&
                        c.star == characters[0].star);
                        
                    if (matchingChar != null)
                    {
                        Debug.Log($"[AIPlayer] Attempting to stack {matchingChar.characterName} for merge");
                    }
                }
            }
        }
        
        private void CheckForMerges()
        {
            if (brain != null)
            {
                brain.CheckAndPerformMerges();
            }
        }
        
        public void SetStrategy(AIStrategy newStrategy)
        {
            strategy = newStrategy;
            Debug.Log($"[AIPlayer] Strategy changed to: {strategy}");
        }
        
        public void SetDeck(List<CharacterData> newDeck)
        {
            deck = new List<CharacterData>(newDeck);
            Debug.Log($"[AIPlayer] Deck set with {deck.Count} characters");
        }
        
        protected override void OnCharacterSummoned(Character character)
        {
            base.OnCharacterSummoned(character);
            
            if (brain != null)
            {
                brain.OnCharacterSummoned(character);
            }
        }
        
        public override void OnCharacterDeath(Character character)
        {
            base.OnCharacterDeath(character);
            
            if (brain != null)
            {
                brain.OnCharacterDeath(character);
            }
        }
    }
}