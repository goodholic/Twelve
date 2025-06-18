using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace pjy.Gameplay
{
    public class HumanPlayer : BasePlayer
    {
        [Header("Human Player Settings")]
        [SerializeField] private bool enableDragAndDrop = true;
        [SerializeField] private bool showPlacementHints = true;
        
        protected override void InitializePlayer()
        {
            base.InitializePlayer();
            isAI = false;
            playerName = "Human Player " + playerID;
        }
        
        protected override void OnGameUpdate()
        {
            base.OnGameUpdate();
            
            HandleInput();
        }
        
        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClick();
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                HandleAutoPlace();
            }
        }
        
        private void HandleMouseClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            
            if (hit.collider != null)
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null && tile.isRegion2 == (areaIndex == 2))
                {
                    OnTileClicked(tile);
                }
                
                Character character = hit.collider.GetComponent<Character>();
                if (character != null && ownedCharacters.Contains(character))
                {
                    OnCharacterClicked(character);
                }
            }
        }
        
        private void OnTileClicked(Tile tile)
        {
            Debug.Log($"[HumanPlayer] Clicked on tile: {tile.name}");
            
            if (showPlacementHints && tile.CanPlaceCharacter())
            {
                ShowPlacementHint(tile);
            }
        }
        
        private void OnCharacterClicked(Character character)
        {
            Debug.Log($"[HumanPlayer] Clicked on character: {character.characterName}");
            
            if (enableDragAndDrop)
            {
                StartDragging(character);
            }
        }
        
        private void ShowPlacementHint(Tile tile)
        {
            
        }
        
        private void StartDragging(Character character)
        {
            DraggableCharacter draggable = character.GetComponent<DraggableCharacter>();
            if (draggable != null)
            {
                draggable.StartDragging();
            }
        }
        
        private void HandleAutoPlace()
        {
            Debug.Log("[HumanPlayer] Auto-place requested");
            
            List<Tile> availableTiles = GetAvailableTiles();
            if (availableTiles.Count > 0 && deck.Count > 0)
            {
                int randomIndex = Random.Range(0, deck.Count);
                CharacterData randomCharacter = deck[randomIndex];
                
                int tileIndex = Random.Range(0, availableTiles.Count);
                Tile randomTile = availableTiles[tileIndex];
                
                TrySummonCharacter(randomCharacter, randomTile);
            }
        }
        
        public void SetDeck(List<CharacterData> newDeck)
        {
            deck = new List<CharacterData>(newDeck);
            Debug.Log($"[HumanPlayer] Deck set with {deck.Count} characters");
        }
        
        protected override void OnCharacterSummoned(Character character)
        {
            base.OnCharacterSummoned(character);
            
            if (enableDragAndDrop)
            {
                DraggableCharacter draggable = character.GetComponent<DraggableCharacter>();
                if (draggable == null)
                {
                    draggable = character.gameObject.AddComponent<DraggableCharacter>();
                }
            }
        }
    }
}