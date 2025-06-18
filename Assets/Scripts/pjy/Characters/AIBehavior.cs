using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIBehavior : MonoBehaviour
{
    private bool isDestroyed = false;
    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard,
        Expert
    }
    
    public enum AIStrategy
    {
        Rush,      // Early aggression with cheap units
        Turtle,    // Defensive buildup
        Economy,   // Focus on resource generation
        Counter,   // React to player's strategy
        Balanced   // Mix of all strategies
    }
    
    private PlayerController playerController;
    private AIDifficulty difficulty = AIDifficulty.Normal;
    private AIStrategy currentStrategy = AIStrategy.Balanced;
    
    [Header("AI Decision Making")]
    private float summonCooldown = 3f;
    private float lastSummonTime;
    private float strategyEvaluationInterval = 10f;
    private float lastStrategyEvaluation;
    
    [Header("AI Parameters by Difficulty")]
    private Dictionary<AIDifficulty, AIParameters> difficultyParameters;
    
    [System.Serializable]
    private class AIParameters
    {
        public float decisionSpeed = 2f;
        public float summonEfficiency = 0.7f;
        public float placementAccuracy = 0.8f;
        public float mergeThreshold = 0.85f;
        public float resourceManagement = 0.75f;
    }
    
    public void Initialize(PlayerController controller, AIDifficulty diff)
    {
        playerController = controller;
        difficulty = diff;
        InitializeDifficultyParameters();
        DetermineInitialStrategy();
    }
    
    private void InitializeDifficultyParameters()
    {
        difficultyParameters = new Dictionary<AIDifficulty, AIParameters>
        {
            { AIDifficulty.Easy, new AIParameters { 
                decisionSpeed = 4f, summonEfficiency = 0.5f, 
                placementAccuracy = 0.6f, mergeThreshold = 0.6f, 
                resourceManagement = 0.5f 
            }},
            { AIDifficulty.Normal, new AIParameters { 
                decisionSpeed = 2f, summonEfficiency = 0.7f, 
                placementAccuracy = 0.8f, mergeThreshold = 0.85f, 
                resourceManagement = 0.75f 
            }},
            { AIDifficulty.Hard, new AIParameters { 
                decisionSpeed = 1f, summonEfficiency = 0.9f, 
                placementAccuracy = 0.95f, mergeThreshold = 0.95f, 
                resourceManagement = 0.9f 
            }},
            { AIDifficulty.Expert, new AIParameters { 
                decisionSpeed = 0.5f, summonEfficiency = 1f, 
                placementAccuracy = 1f, mergeThreshold = 1f, 
                resourceManagement = 1f 
            }}
        };
        
        UpdateAIParameters();
    }
    
    private void UpdateAIParameters()
    {
        var parameters = difficultyParameters[difficulty];
        summonCooldown = parameters.decisionSpeed;
    }
    
    public void SetDifficulty(AIDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
        UpdateAIParameters();
    }
    
    private void OnDestroy()
    {
        isDestroyed = true;
        
        // Clean up references
        playerController = null;
        
        // Clear dictionary
        if (difficultyParameters != null)
        {
            difficultyParameters.Clear();
            difficultyParameters = null;
        }
        
        // Stop all coroutines
        StopAllCoroutines();
    }
    
    public void MakeDecision()
    {
        // Check if component is being destroyed
        if (isDestroyed || this == null)
        {
            return;
        }
        
        try
        {
            if (playerController == null)
            {
                Debug.LogError("[AIBehavior] PlayerController가 null입니다!");
                return;
            }
            
            // Evaluate strategy periodically
            if (Time.time - lastStrategyEvaluation >= strategyEvaluationInterval)
            {
                lastStrategyEvaluation = Time.time;
                EvaluateAndUpdateStrategy();
            }
            
            // Execute current strategy
            switch (currentStrategy)
            {
                case AIStrategy.Rush:
                    ExecuteRushStrategy();
                    break;
                case AIStrategy.Turtle:
                    ExecuteTurtleStrategy();
                    break;
                case AIStrategy.Economy:
                    ExecuteEconomyStrategy();
                    break;
                case AIStrategy.Counter:
                    ExecuteCounterStrategy();
                    break;
                case AIStrategy.Balanced:
                    ExecuteBalancedStrategy();
                    break;
            }
            
            // Check for merge opportunities
            CheckAndPerformMerges();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIBehavior] 의사결정 중 오류: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void DetermineInitialStrategy()
    {
        // Analyze game state and opponent
        if (GameManager.Instance != null)
        {
            int currentWave = GameManager.Instance.GetCurrentWave();
            
            if (currentWave <= 5)
            {
                currentStrategy = AIStrategy.Economy;
            }
            else if (currentWave <= 10)
            {
                currentStrategy = AIStrategy.Balanced;
            }
            else
            {
                currentStrategy = AIStrategy.Rush;
            }
        }
    }
    
    /// <summary>
    /// AI 전략 평가 및 업데이트
    /// 주기적으로 게임 상황을 분석하여 최적의 전략으로 전환
    /// </summary>
    private void EvaluateAndUpdateStrategy()
    {
        if (GameManager.Instance == null) return;
        
        // Get opponent player
        PlayerController opponent = GetOpponentPlayer();
        if (opponent == null) return;
        
        // Analyze opponent's army composition
        var opponentCharacters = opponent.OwnedCharacters;
        int opponentStrength = CalculateArmyStrength(opponentCharacters);
        int myStrength = CalculateArmyStrength(playerController.OwnedCharacters);
        
        // 전략 결정 로직:
        // 1. 상대가 1.5배 이상 강함 → 방어(Turtle): 강한 유닛으로 방어 구축
        // 2. 내가 1.2배 이상 강함 → 공격(Rush): 우세를 활용한 빠른 마무리
        // 3. 자원 부족(50 미만) → 경제(Economy): 자원 절약 및 효율적 운영
        // 4. 그 외 → 균형(Balanced): 상황에 맞는 유연한 대응
        if (opponentStrength > myStrength * 1.5f)
        {
            currentStrategy = AIStrategy.Turtle;
        }
        else if (myStrength > opponentStrength * 1.2f)
        {
            currentStrategy = AIStrategy.Rush;
        }
        else if (playerController.CurrentMinerals < 50)
        {
            currentStrategy = AIStrategy.Economy;
        }
        else
        {
            currentStrategy = AIStrategy.Balanced;
        }
    }
    
    private void ExecuteRushStrategy()
    {
        // Focus on cheap, fast units
        if (CanSummon())
        {
            var cheapCharacters = GetAvailableCharacters()
                .Where(c => c.cost <= 30)
                .OrderBy(c => c.cost)
                .ToList();
                
            if (cheapCharacters.Count > 0)
            {
                SummonAndPlaceCharacter(cheapCharacters[0]);
            }
        }
    }
    
    private void ExecuteTurtleStrategy()
    {
        // Focus on defensive positioning and strong units
        if (CanSummon() && playerController.CurrentMinerals >= 50)
        {
                    var strongCharacters = GetAvailableCharacters()
            .Where(c => c.maxHP > 100)
            .OrderByDescending(c => c.maxHP)
            .ToList();
                
            if (strongCharacters.Count > 0)
            {
                SummonAndPlaceCharacter(strongCharacters[0], true);
            }
        }
    }
    
    private void ExecuteEconomyStrategy()
    {
        // Save resources, only summon when necessary
        if (CanSummon() && playerController.CurrentMinerals >= 100)
        {
            var efficientCharacters = GetAvailableCharacters()
                .Where(c => c.cost <= 40)
                .OrderByDescending(c => (c.attackPower + c.maxHP) / (float)c.cost)
                .ToList();
                
            if (efficientCharacters.Count > 0)
            {
                SummonAndPlaceCharacter(efficientCharacters[0]);
            }
        }
    }
    
    private void ExecuteCounterStrategy()
    {
        // Analyze opponent and counter their units
        PlayerController opponent = GetOpponentPlayer();
        if (opponent == null || !CanSummon()) return;
        
        // Get opponent's strongest unit type
        var opponentUnits = opponent.OwnedCharacters;
        if (opponentUnits.Count > 0)
        {
            // Find counter units
            var counterCharacters = GetAvailableCharacters()
                .OrderByDescending(c => c.attackPower)
                .ToList();
                
            if (counterCharacters.Count > 0)
            {
                SummonAndPlaceCharacter(counterCharacters[0]);
            }
        }
    }
    
    private void ExecuteBalancedStrategy()
    {
        // Mix of all strategies
        if (CanSummon())
        {
            var allCharacters = GetAvailableCharacters()
                .OrderByDescending(c => CalculateCharacterValue(c))
                .ToList();
                
            if (allCharacters.Count > 0)
            {
                SummonAndPlaceCharacter(allCharacters[0]);
            }
        }
    }
    
    private bool CanSummon()
    {
        return Time.time - lastSummonTime >= summonCooldown && 
               playerController.CanSummonCharacter();
    }
    
    private List<CharacterData> GetAvailableCharacters()
    {
        if (CharacterDatabase.Instance == null)
        {
            Debug.LogWarning("[AIBehavior] CharacterDatabase.Instance is null");
            return new List<CharacterData>();
        }
        
        // Get characters that AI can afford
        return CharacterDatabase.Instance.GetAllCharacters()
            .Where(c => c.cost <= playerController.CurrentMinerals)
            .ToList();
    }
    
    private void SummonAndPlaceCharacter(CharacterData characterData, bool defensive = false)
    {
        if (characterData == null)
        {
            Debug.LogError("[AIBehavior] SummonAndPlaceCharacter called with null characterData");
            return;
        }
        
        if (playerController == null)
        {
            Debug.LogError("[AIBehavior] playerController is null in SummonAndPlaceCharacter");
            return;
        }
        
        // Get placement position based on strategy
        Tile targetTile = GetOptimalPlacementTile(defensive);
        if (targetTile == null) return;
        
        // Summon character
        Character newCharacter = playerController.SummonCharacter(characterData, targetTile.transform.position);
        if (newCharacter != null)
        {
            // Place on tile
            playerController.PlaceCharacter(newCharacter, targetTile);
            lastSummonTime = Time.time;
        }
    }
    
    private Tile GetOptimalPlacementTile(bool defensive)
    {
        if (TileManager.Instance == null) return null;
        
        var availableTiles = TileManager.Instance.GetAvailableTilesForPlayer(playerController.PlayerID);
        if (availableTiles.Count == 0) return null;
        
        // Add placement accuracy based on difficulty
        float accuracy = difficultyParameters[difficulty].placementAccuracy;
        
        if (Random.value > accuracy)
        {
            // Random placement for lower difficulties
            return availableTiles[Random.Range(0, availableTiles.Count)];
        }
        
        // Strategic placement
        if (defensive)
        {
            // Place near castles
            return availableTiles.OrderBy(t => Vector3.Distance(t.transform.position, GetNearestCastle())).FirstOrDefault();
        }
        else
        {
            // Place forward
            return availableTiles.OrderByDescending(t => t.transform.position.z).FirstOrDefault();
        }
    }
    
    private void CheckAndPerformMerges()
    {
        if (difficultyParameters == null || !difficultyParameters.ContainsKey(difficulty))
        {
            Debug.LogError("[AIBehavior] difficultyParameters not properly initialized");
            return;
        }
        
        var mergeThreshold = difficultyParameters[difficulty].mergeThreshold;
        if (Random.value > mergeThreshold) return;
        
        // Group characters by type
        var characterGroups = playerController.OwnedCharacters
            .GroupBy(c => c.GetCharacterData())
            .Where(g => g.Count() >= 3);
            
        foreach (var group in characterGroups)
        {
            var charactersToMerge = group.Take(3).ToList();
            if (charactersToMerge.Count == 3)
            {
                // Trigger merge through MergeManager
                if (MergeManager.Instance != null)
                {
                    MergeManager.Instance.TryMergeCharacters(charactersToMerge);
                }
            }
        }
    }
    
    private int CalculateArmyStrength(List<Character> characters)
    {
        return characters.Sum(c => (int)(c.GetAttackDamage() + c.GetMaxHealth() / 10));
    }
    
    private float CalculateCharacterValue(CharacterData character)
    {
        return (character.attackPower * 2 + character.maxHP + character.attackRange * 10) / (float)character.cost;
    }
    
    private PlayerController GetOpponentPlayer()
    {
        if (GameManager.Instance == null) return null;
        
        var allPlayers = GameManager.Instance.GetAllPlayers();
        return allPlayers.FirstOrDefault(p => p != playerController);
    }
    
    private Vector3 GetNearestCastle()
    {
        // Find nearest friendly castle position
        GameObject castle = GameObject.FindGameObjectWithTag("Castle");
        if (castle != null)
        {
            return castle.transform.position;
        }
        return Vector3.zero;
    }
}