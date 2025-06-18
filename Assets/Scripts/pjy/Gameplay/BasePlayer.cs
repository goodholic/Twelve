using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace pjy.Gameplay
{
    public abstract class BasePlayer : MonoBehaviour
    {
        [Header("Player Info")]
        [SerializeField] protected string playerName = "Player";
        [SerializeField] protected int playerID = 0;
        [SerializeField] protected bool isAI = false;
        [SerializeField] protected int areaIndex = 1;
        
        [Header("Resources")]
        [SerializeField] protected int gold = 100;
        [SerializeField] protected int mana = 50;
        
        [Header("Character Management")]
        [SerializeField] protected List<Character> ownedCharacters = new List<Character>();
        [SerializeField] protected int maxCharacters = 50;
        [SerializeField] protected List<CharacterData> deck = new List<CharacterData>();
        
        [Header("Game State")]
        [SerializeField] protected bool isPlaying = false;
        [SerializeField] protected float gameTime = 0f;
        
        public string PlayerName => playerName;
        public int PlayerID => playerID;
        public bool IsAI => isAI;
        public int AreaIndex => areaIndex;
        public int Gold => gold;
        public int Mana => mana;
        public List<Character> OwnedCharacters => ownedCharacters;
        public int CharacterCount => ownedCharacters.Count;
        
        protected virtual void Start()
        {
            InitializePlayer();
        }
        
        protected virtual void Update()
        {
            if (isPlaying)
            {
                gameTime += Time.deltaTime;
                OnGameUpdate();
            }
        }
        
        protected virtual void InitializePlayer()
        {
            Debug.Log($"[BasePlayer] {playerName} initialized - AI: {isAI}, Area: {areaIndex}");
        }
        
        protected virtual void OnGameUpdate()
        {
            
        }
        
        public virtual bool CanSummonCharacter()
        {
            return ownedCharacters.Count < maxCharacters;
        }
        
        public virtual bool TrySummonCharacter(CharacterData characterData, Tile targetTile)
        {
            if (!CanSummonCharacter())
            {
                Debug.LogWarning($"[BasePlayer] {playerName} cannot summon - character limit reached ({ownedCharacters.Count}/{maxCharacters})");
                return false;
            }
            
            if (gold < characterData.cost)
            {
                Debug.LogWarning($"[BasePlayer] {playerName} cannot summon - not enough gold ({gold}/{characterData.cost})");
                return false;
            }
            
            PlacementManager placementManager = PlacementManager.Instance;
            if (placementManager == null)
            {
                Debug.LogError("[BasePlayer] PlacementManager not found!");
                return false;
            }
            
            Character newCharacter = placementManager.SummonCharacterOnTile(characterData, targetTile, areaIndex == 2);
            if (newCharacter != null)
            {
                ownedCharacters.Add(newCharacter);
                gold -= characterData.cost;
                OnCharacterSummoned(newCharacter);
                return true;
            }
            
            return false;
        }
        
        protected virtual void OnCharacterSummoned(Character character)
        {
            Debug.Log($"[BasePlayer] {playerName} summoned {character.characterName} at {character.currentTile.name}");
        }
        
        public virtual void OnCharacterDeath(Character character)
        {
            if (ownedCharacters.Contains(character))
            {
                ownedCharacters.Remove(character);
                Debug.Log($"[BasePlayer] {playerName}'s {character.characterName} died. Remaining: {ownedCharacters.Count}");
            }
        }
        
        public virtual void AddGold(int amount)
        {
            gold += amount;
            Debug.Log($"[BasePlayer] {playerName} gained {amount} gold. Total: {gold}");
        }
        
        public virtual void AddMana(int amount)
        {
            mana += amount;
            Debug.Log($"[BasePlayer] {playerName} gained {amount} mana. Total: {mana}");
        }
        
        public virtual void StartGame()
        {
            isPlaying = true;
            gameTime = 0f;
            OnGameStart();
        }
        
        public virtual void EndGame()
        {
            isPlaying = false;
            OnGameEnd();
        }
        
        protected virtual void OnGameStart()
        {
            Debug.Log($"[BasePlayer] {playerName} started the game");
        }
        
        protected virtual void OnGameEnd()
        {
            Debug.Log($"[BasePlayer] {playerName} ended the game");
        }
        
        public List<Tile> GetAvailableTiles()
        {
            List<Tile> availableTiles = new List<Tile>();
            Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
            
            foreach (Tile tile in allTiles)
            {
                if (tile.isRegion2 == (areaIndex == 2) && tile.CanPlaceCharacter())
                {
                    availableTiles.Add(tile);
                }
            }
            
            return availableTiles;
        }
        
        public List<Tile> GetOccupiedTiles()
        {
            List<Tile> occupiedTiles = new List<Tile>();
            
            foreach (Character character in ownedCharacters)
            {
                if (character != null && character.currentTile != null)
                {
                    if (!occupiedTiles.Contains(character.currentTile))
                    {
                        occupiedTiles.Add(character.currentTile);
                    }
                }
            }
            
            return occupiedTiles;
        }
    }
}