using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private bool isAI = false;
    [SerializeField] private int playerID = 0;
    [SerializeField] private string playerName = "Player";
    
    [Header("AI Settings")]
    [SerializeField] private AIBehavior.AIDifficulty aiDifficulty = AIBehavior.AIDifficulty.Normal;
    [SerializeField] private float aiDecisionInterval = 2f;
    
    [Header("Resources")]
    [SerializeField] private int currentMinerals = 100;
    [SerializeField] private int maxCharacters = 50;
    
    [Header("Game Area")]
    [SerializeField] private int areaIndex = 1; // 1: 플레이어 지역, 2: AI 지역
    
    private AIBehavior aiBehavior;
    private List<Character> ownedCharacters = new List<Character>();
    private float lastAIDecisionTime;
    
    public bool IsAI => isAI;
    public int PlayerID => playerID;
    public string PlayerName => playerName;
    public int CurrentMinerals => currentMinerals;
    public int CharacterCount => ownedCharacters.Count;
    public List<Character> OwnedCharacters => ownedCharacters;
    public int AreaIndex => areaIndex;
    
    private void Awake()
    {
        if (isAI)
        {
            aiBehavior = gameObject.AddComponent<AIBehavior>();
            aiBehavior.Initialize(this, aiDifficulty);
        }
    }
    
    private void Start()
    {
        // Register this player with GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }
        else
        {
            Debug.LogWarning($"[PlayerController] GameManager.Instance is null at Start for {playerName}");
        }
    }
    
    private void OnDestroy()
    {
        // Unregister from GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPlayer(this);
        }
        
        // Clean up owned characters list
        ownedCharacters.Clear();
        
        // Clean up AI behavior
        if (aiBehavior != null)
        {
            Destroy(aiBehavior);
            aiBehavior = null;
        }
    }
    
    private void Update()
    {
        if (isAI && aiBehavior != null)
        {
            if (Time.time - lastAIDecisionTime >= aiDecisionInterval)
            {
                lastAIDecisionTime = Time.time;
                aiBehavior.MakeDecision();
            }
        }
    }
    
    public bool CanSummonCharacter()
    {
        return ownedCharacters.Count < maxCharacters;
    }
    
    public bool TrySpendMinerals(int amount)
    {
        if (currentMinerals >= amount)
        {
            currentMinerals -= amount;
            return true;
        }
        return false;
    }
    
    public void AddMinerals(int amount)
    {
        currentMinerals += amount;
    }
    
    public void AddCharacter(Character character)
    {
        if (!ownedCharacters.Contains(character))
        {
            ownedCharacters.Add(character);
            character.SetOwnerPlayer(this);
        }
    }
    
    public void RemoveCharacter(Character character)
    {
        ownedCharacters.Remove(character);
    }
    
    public Character SummonCharacter(CharacterData characterData, Vector3 position)
    {
        if (characterData == null)
        {
            Debug.LogError($"[PlayerController] SummonCharacter called with null characterData for {playerName}");
            return null;
        }
        
        if (!CanSummonCharacter())
        {
            Debug.LogWarning($"Player {playerName} cannot summon more characters. Current: {CharacterCount}/{maxCharacters}");
            return null;
        }
        
        if (!TrySpendMinerals(characterData.cost))
        {
            Debug.LogWarning($"Player {playerName} doesn't have enough minerals. Need: {characterData.cost}, Have: {currentMinerals}");
            return null;
        }
        
        // Use SummonManager to create character
        if (SummonManager.Instance != null)
        {
            Character newCharacter = SummonManager.Instance.SummonCharacterForPlayer(characterData, position, this);
            if (newCharacter != null)
            {
                AddCharacter(newCharacter);
                return newCharacter;
            }
        }
        else
        {
            Debug.LogError($"[PlayerController] SummonManager.Instance is null for {playerName}");
        }
        
        // Refund minerals if summon failed
        AddMinerals(characterData.cost);
        return null;
    }
    
    public bool PlaceCharacter(Character character, Tile targetTile)
    {
        if (!ownedCharacters.Contains(character))
        {
            Debug.LogWarning($"Player {playerName} doesn't own this character!");
            return false;
        }
        
        // Use PlacementManager to place character
        if (PlacementManager.Instance != null)
        {
            return PlacementManager.Instance.PlaceCharacterForPlayer(character, targetTile, this);
        }
        
        return false;
    }
    
    public void SetAIDifficulty(AIBehavior.AIDifficulty difficulty)
    {
        aiDifficulty = difficulty;
        if (aiBehavior != null)
        {
            aiBehavior.SetDifficulty(difficulty);
        }
    }
    
    public void EnableAI(bool enable)
    {
        isAI = enable;
        
        if (enable && aiBehavior == null)
        {
            aiBehavior = gameObject.AddComponent<AIBehavior>();
            aiBehavior.Initialize(this, aiDifficulty);
        }
        else if (!enable && aiBehavior != null)
        {
            // Destroy component and immediately null the reference
            AIBehavior tempBehavior = aiBehavior;
            aiBehavior = null;
            Destroy(tempBehavior);
        }
    }
}