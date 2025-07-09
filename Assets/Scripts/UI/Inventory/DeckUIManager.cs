using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GuildMaster.Data;
using GuildMaster.Game;
using CharacterData = GuildMaster.Data.CharacterData;

public class DeckUIManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CharacterInventoryManager characterInventory;
    
    [Header("인벤토리 UI")]
    [SerializeField] private Transform inventoryGrid; // 인벤토리 그리드 부모
    [SerializeField] private GameObject inventorySlotPrefab; // 인벤토리 슬롯 프리팹
    
    [Header("덱 UI")]
    [SerializeField] private Transform deckGrid; // 덱 그리드 부모 (10개 슬롯)
    [SerializeField] private GameObject deckSlotPrefab; // 덱 슬롯 프리팹
    
    [Header("UI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI deckCountText; // 현재 덱 개수 표시 (예: 5/10)
    [SerializeField] private Button saveButton; // 덱 저장 버튼
    [SerializeField] private Button clearButton; // 덱 초기화 버튼
    
    [Header("에러 메시지")]
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TextMeshProUGUI errorText;
    
    // 슬롯 관리
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private List<DeckSlot> deckSlots = new List<DeckSlot>();
    
    // 슬롯 클래스들
    private class InventorySlot
    {
        public GameObject gameObject;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI levelText;
        public Button button;
        public CharacterData character;
    }
    
    private class DeckSlot
    {
        public GameObject gameObject;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI levelText;
        public Button removeButton;
        public CharacterData character;
        public int slotIndex;
    }
    
    private void Awake()
    {
        if (characterInventory == null)
        {
            characterInventory = FindFirstObjectByType<CharacterInventoryManager>();
        }
        
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveDeck);
        }
        
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClearDeck);
        }
        
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
        
        // 덱 슬롯 10개 미리 생성
        CreateDeckSlots();
    }
    
    private void OnEnable()
    {
        RefreshUI();
    }
    
    private void CreateDeckSlots()
    {
        // 기존 슬롯 제거
        foreach (var slot in deckSlots)
        {
            if (slot.gameObject != null)
                Destroy(slot.gameObject);
        }
        deckSlots.Clear();
        
        // 10개 슬롯 생성
        for (int i = 0; i < 10; i++)
        {
            GameObject slotObj = Instantiate(deckSlotPrefab, deckGrid);
            DeckSlot slot = new DeckSlot
            {
                gameObject = slotObj,
                iconImage = slotObj.transform.Find("Icon")?.GetComponent<Image>(),
                nameText = slotObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>(),
                levelText = slotObj.transform.Find("Level")?.GetComponent<TextMeshProUGUI>(),
                removeButton = slotObj.transform.Find("RemoveButton")?.GetComponent<Button>(),
                slotIndex = i
            };
            
            if (slot.removeButton != null)
            {
                int index = i;
                slot.removeButton.onClick.AddListener(() => OnRemoveFromDeck(index));
            }
            
            deckSlots.Add(slot);
        }
    }
    
    public void RefreshUI()
    {
        RefreshInventoryUI();
        RefreshDeckUI();
        UpdateDeckCount();
    }
    
    private void RefreshInventoryUI()
    {
        // 기존 인벤토리 슬롯 제거
        foreach (var slot in inventorySlots)
        {
            if (slot.gameObject != null)
                Destroy(slot.gameObject);
        }
        inventorySlots.Clear();
        
        // 보유 캐릭터 가져오기
        List<CharacterData> ownedCharacters = characterInventory.GetOwnedCharacters();
        
        // 각 캐릭터에 대해 슬롯 생성
        foreach (var character in ownedCharacters)
        {
            if (character == null) continue;
            
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryGrid);
            InventorySlot slot = new InventorySlot
            {
                gameObject = slotObj,
                iconImage = slotObj.transform.Find("Icon")?.GetComponent<Image>(),
                nameText = slotObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>(),
                levelText = slotObj.transform.Find("Level")?.GetComponent<TextMeshProUGUI>(),
                button = slotObj.GetComponent<Button>(),
                character = character
            };
            
            // UI 업데이트
            if (slot.iconImage != null && character.buttonIcon != null)
            {
                slot.iconImage.sprite = character.buttonIcon;
            }
            
            if (slot.nameText != null)
            {
                slot.nameText.text = character.characterName;
            }
            
            if (slot.levelText != null)
            {
                slot.levelText.text = $"Lv.{character.level}";
            }
            
            // 클릭 이벤트
            if (slot.button != null)
            {
                CharacterData charCopy = character;
                slot.button.onClick.AddListener(() => OnAddToDeck(charCopy));
            }
            
            inventorySlots.Add(slot);
        }
    }
    
    private void RefreshDeckUI()
    {
        List<CharacterData> deckCharacters = characterInventory.GetDeckCharacters();
        
        // 모든 덱 슬롯 초기화
        for (int i = 0; i < deckSlots.Count; i++)
        {
            DeckSlot slot = deckSlots[i];
            
            if (i < deckCharacters.Count && deckCharacters[i] != null)
            {
                // 캐릭터가 있는 슬롯
                CharacterData character = deckCharacters[i];
                slot.character = character;
                
                if (slot.iconImage != null && character.buttonIcon != null)
                {
                    slot.iconImage.sprite = character.buttonIcon;
                    slot.iconImage.color = Color.white;
                }
                
                if (slot.nameText != null)
                {
                    slot.nameText.text = character.characterName;
                }
                
                if (slot.levelText != null)
                {
                    slot.levelText.text = $"Lv.{character.level}";
                }
                
                if (slot.removeButton != null)
                {
                    slot.removeButton.gameObject.SetActive(true);
                }
            }
            else
            {
                // 빈 슬롯
                slot.character = null;
                
                if (slot.iconImage != null)
                {
                    slot.iconImage.sprite = null;
                    slot.iconImage.color = new Color(1, 1, 1, 0.2f);
                }
                
                if (slot.nameText != null)
                {
                    slot.nameText.text = "빈 슬롯";
                }
                
                if (slot.levelText != null)
                {
                    slot.levelText.text = "";
                }
                
                if (slot.removeButton != null)
                {
                    slot.removeButton.gameObject.SetActive(false);
                }
            }
        }
    }
    
    private void UpdateDeckCount()
    {
        if (deckCountText != null)
        {
            int currentCount = characterInventory.GetDeckCharacters().Count;
            deckCountText.text = $"덱: {currentCount}/10";
        }
    }
    
    private void OnAddToDeck(CharacterData character)
    {
        if (character == null) return;
        
        // 덱이 가득 찼는지 확인
        if (characterInventory.GetDeckCharacters().Count >= 10)
        {
            ShowError("덱이 가득 찼습니다! (최대 10개)");
            return;
        }
        
        // 이미 덱에 있는지 확인 (중복 방지)
        List<CharacterData> deckChars = characterInventory.GetDeckCharacters();
        foreach (var deckChar in deckChars)
        {
            if (deckChar != null && deckChar.characterName == character.characterName)
            {
                ShowError($"{character.characterName}은(는) 이미 덱에 있습니다!");
                return;
            }
        }
        
        // 덱에 추가
        characterInventory.MoveToDeck(character);
        
        // UI 새로고침
        RefreshUI();
        
        // 저장
        characterInventory.SaveCharacters();
    }
    
    private void OnRemoveFromDeck(int slotIndex)
    {
        List<CharacterData> deckChars = characterInventory.GetDeckCharacters();
        if (slotIndex < 0 || slotIndex >= deckChars.Count) return;
        
        CharacterData character = deckChars[slotIndex];
        if (character == null) return;
        
        // 인벤토리로 이동
        characterInventory.MoveToInventory(character);
        
        // UI 새로고침
        RefreshUI();
        
        // 저장
        characterInventory.SaveCharacters();
    }
    
    private void OnSaveDeck()
    {
        characterInventory.SaveCharacters();
        ShowError("덱이 저장되었습니다!");
    }
    
    private void OnClearDeck()
    {
        // 모든 덱 캐릭터를 인벤토리로 이동
        List<CharacterData> deckChars = new List<CharacterData>(characterInventory.GetDeckCharacters());
        foreach (var character in deckChars)
        {
            if (character != null)
            {
                characterInventory.MoveToInventory(character);
            }
        }
        
        // UI 새로고침
        RefreshUI();
        
        // 저장
        characterInventory.SaveCharacters();
        
        ShowError("덱이 초기화되었습니다!");
    }
    
    private void ShowError(string message)
    {
        if (errorPanel != null && errorText != null)
        {
            errorText.text = message;
            errorPanel.SetActive(true);
            
            // 2초 후 자동으로 숨기기
            CancelInvoke(nameof(HideError));
            Invoke(nameof(HideError), 2f);
        }
    }
    
    private void HideError()
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }
}