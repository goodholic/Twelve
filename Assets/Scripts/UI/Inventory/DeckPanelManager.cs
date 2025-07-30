using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
using GuildMaster.Data;

namespace GuildMaster.UI
{
    /// <summary>
    /// 덱 구성 패널을 관리하는 매니저
    /// 캐릭터 인벤토리에서 덱으로 캐릭터를 등록/해제하는 기능을 담당
    /// </summary>
    public class DeckPanelManager : MonoBehaviour
    {
        [Header("Character Inventory")]
        [SerializeField] private CharacterInventoryManager characterInventory;
        
        [Header("Character Database")]
        [SerializeField] private CharacterDatabaseSO characterDB;
        [SerializeField] private CharacterDatabaseSO characterDBObject;
        
        [Header("덱 슬롯 이미지들 (최대 10개)")]
        [SerializeField] private List<Image> deckSlotImages = new List<Image>();
        
        [Header("덱 슬롯 레벨 텍스트들")]
        [SerializeField] private List<TextMeshProUGUI> deckSlotLevelTexts = new List<TextMeshProUGUI>();
        
        [Header("인벤토리 슬롯 이미지들")]
        [SerializeField] private List<Image> inventorySlotImages = new List<Image>();
        
        [Header("UI 설정")]
        [SerializeField] private Sprite emptySlotSprite;
        [SerializeField] private Color ownedCharacterColor = Color.white;
        [SerializeField] private Color emptySlotColor = Color.gray;
        
        [Header("업그레이드 모드")]
        public bool isUpgradeMode = false;
        
        // 현재 덱에 등록된 캐릭터들 (최대 10개)
        private List<CharacterData> deckCharacters = new List<CharacterData>();
        
        // 현재 선택된 슬롯 인덱스
        private int selectedSlotIndex = -1;
        
        void Start()
        {
            InitializeDeckPanel();
        }
        
        void OnEnable()
        {
            RefreshDeckDisplay();
        }
        
        /// <summary>
        /// 덱 패널 초기화
        /// </summary>
        void InitializeDeckPanel()
        {
            // CharacterInventoryManager 참조 확인
            if (characterInventory == null)
            {
                characterInventory = CharacterInventoryManager.Instance;
            }
            
            // CharacterDatabase 참조 확인
            if (characterDB == null && characterDBObject != null)
            {
                characterDB = characterDBObject;
            }
            
            // 빈 슬롯 스프라이트 설정
            if (emptySlotSprite == null)
            {
                // 기본 빈 슬롯 스프라이트 생성 또는 로드
                Debug.LogWarning("[DeckPanelManager] emptySlotSprite가 설정되지 않았습니다.");
            }
            
            RefreshDeckDisplay();
        }
        
        /// <summary>
        /// 덱 디스플레이 새로고침
        /// </summary>
        public void RefreshDeckDisplay()
        {
            if (characterInventory == null) return;
            
            // 현재 보유 중인 캐릭터들 가져오기
            var ownedCharacters = characterInventory.GetOwnedCharacters();
            
            // 덱 슬롯 업데이트 (최대 10개)
            for (int i = 0; i < deckSlotImages.Count && i < 10; i++)
            {
                if (i < ownedCharacters.Count)
                {
                    var character = ownedCharacters[i];
                    if (deckSlotImages[i] != null)
                    {
                        deckSlotImages[i].sprite = character.buttonIcon;
                        deckSlotImages[i].color = ownedCharacterColor;
                    }
                    
                    if (deckSlotLevelTexts[i] != null)
                    {
                        deckSlotLevelTexts[i].text = $"Lv.{character.level}";
                    }
                }
                else
                {
                    // 빈 슬롯
                    if (deckSlotImages[i] != null)
                    {
                        deckSlotImages[i].sprite = emptySlotSprite;
                        deckSlotImages[i].color = emptySlotColor;
                    }
                    
                    if (deckSlotLevelTexts[i] != null)
                    {
                        deckSlotLevelTexts[i].text = "";
                    }
                }
            }
            
            // 인벤토리 슬롯 업데이트
            RefreshInventoryDisplay();
        }
        
        /// <summary>
        /// 인벤토리 디스플레이 새로고침
        /// </summary>
        void RefreshInventoryDisplay()
        {
            if (characterInventory == null) return;
            
            var allOwnedCharacters = characterInventory.GetOwnedCharacters();
            
            for (int i = 0; i < inventorySlotImages.Count; i++)
            {
                if (i < allOwnedCharacters.Count)
                {
                    var character = allOwnedCharacters[i];
                    if (inventorySlotImages[i] != null)
                    {
                        inventorySlotImages[i].sprite = character.buttonIcon;
                        inventorySlotImages[i].color = ownedCharacterColor;
                    }
                }
                else
                {
                    if (inventorySlotImages[i] != null)
                    {
                        inventorySlotImages[i].sprite = emptySlotSprite;
                        inventorySlotImages[i].color = emptySlotColor;
                    }
                }
            }
        }
        
        /// <summary>
        /// 덱 슬롯 클릭 처리
        /// </summary>
        /// <param name="slotIndex">클릭된 슬롯 인덱스</param>
        public void OnDeckSlotClicked(int slotIndex)
        {
            selectedSlotIndex = slotIndex;
            Debug.Log($"[DeckPanelManager] 덱 슬롯 {slotIndex} 선택됨");
            
            // 슬롯 선택 표시 UI 업데이트 (구현 필요시)
        }
        
        /// <summary>
        /// 인벤토리 슬롯 클릭 처리
        /// </summary>
        /// <param name="slotIndex">클릭된 인벤토리 슬롯 인덱스</param>
        public void OnInventorySlotClicked(int slotIndex)
        {
            if (characterInventory == null) return;
            
            var ownedCharacters = characterInventory.GetOwnedCharacters();
            if (slotIndex < ownedCharacters.Count)
            {
                var selectedCharacter = ownedCharacters[slotIndex];
                Debug.Log($"[DeckPanelManager] 인벤토리 캐릭터 {selectedCharacter.characterName} 선택됨");
                
                // 덱에 캐릭터 추가/변경 로직 (구현 필요시)
            }
        }
        
        /// <summary>
        /// 덱에서 캐릭터 제거
        /// </summary>
        /// <param name="slotIndex">제거할 슬롯 인덱스</param>
        public void RemoveFromDeck(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < deckCharacters.Count)
            {
                deckCharacters.RemoveAt(slotIndex);
                RefreshDeckDisplay();
                Debug.Log($"[DeckPanelManager] 덱 슬롯 {slotIndex}에서 캐릭터 제거됨");
            }
        }
        
        /// <summary>
        /// 덱에 캐릭터 추가
        /// </summary>
        /// <param name="character">추가할 캐릭터</param>
        public void AddToDeck(CharacterData character)
        {
            if (deckCharacters.Count < 10)
            {
                deckCharacters.Add(character);
                RefreshDeckDisplay();
                Debug.Log($"[DeckPanelManager] 덱에 {character.characterName} 추가됨");
            }
            else
            {
                Debug.LogWarning("[DeckPanelManager] 덱이 가득참 (최대 10개)");
            }
        }
        
        /// <summary>
        /// 현재 덱 구성 가져오기
        /// </summary>
        /// <returns>덱에 등록된 캐릭터 리스트</returns>
        public List<CharacterData> GetDeckCharacters()
        {
            return new List<CharacterData>(deckCharacters);
        }
        
        /// <summary>
        /// 덱이 업그레이드 모드인지 확인
        /// </summary>
        /// <returns>업그레이드 모드 여부</returns>
        public bool IsUpgradeMode()
        {
            return isUpgradeMode;
        }
        
        /// <summary>
        /// 업그레이드 모드 설정
        /// </summary>
        /// <param name="upgradeMode">업그레이드 모드 활성화 여부</param>
        public void SetUpgradeMode(bool upgradeMode)
        {
            isUpgradeMode = upgradeMode;
            Debug.Log($"[DeckPanelManager] 업그레이드 모드: {upgradeMode}");
        }
    }
} 