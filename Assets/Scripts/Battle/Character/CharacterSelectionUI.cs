using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 전투 시작 전 캐릭터 선택 UI
    /// </summary>
    public class CharacterSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Transform characterListContainer;
        [SerializeField] private Transform selectedCharactersContainer;
        [SerializeField] private GameObject characterCardPrefab;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI selectedCountText;
        
        [Header("Selection Settings")]
        [SerializeField] private int maxCharacters = 10;
        
        private List<Character> availableCharacters = new List<Character>();
        private List<Character> selectedCharacters = new List<Character>();
        private System.Action<List<Character>> onSelectionComplete;
        
        private Dictionary<string, CharacterCard> characterCards = new Dictionary<string, CharacterCard>();
        
        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmSelection);
        }
        
        public void Show(System.Action<List<Character>> callback)
        {
            onSelectionComplete = callback;
            selectionPanel.SetActive(true);
            
            LoadAvailableCharacters();
            UpdateUI();
        }
        
        private void LoadAvailableCharacters()
        {
            // DataManager에서 모든 캐릭터 로드
            availableCharacters = DataManager.Instance.GetAllCharacters();
            
            // 캐릭터 카드 생성
            foreach (var character in availableCharacters)
            {
                CreateCharacterCard(character);
            }
        }
        
        private void CreateCharacterCard(Character character)
        {
            GameObject cardObj = Instantiate(characterCardPrefab, characterListContainer);
            CharacterCard card = cardObj.GetComponent<CharacterCard>();
            
            if (card == null)
                card = cardObj.AddComponent<CharacterCard>();
            
            card.Initialize(character, OnCharacterClicked);
            characterCards[character.characterID] = card;
        }
        
        private void OnCharacterClicked(Character character)
        {
            if (selectedCharacters.Contains(character))
            {
                // 선택 해제
                selectedCharacters.Remove(character);
                characterCards[character.characterID].SetSelected(false);
            }
            else if (selectedCharacters.Count < maxCharacters)
            {
                // 선택
                selectedCharacters.Add(character);
                characterCards[character.characterID].SetSelected(true);
            }
            
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            // 선택된 캐릭터 수 표시
            if (selectedCountText != null)
                selectedCountText.text = $"선택된 캐릭터: {selectedCharacters.Count}/{maxCharacters}";
            
            // 확인 버튼 활성화
            if (confirmButton != null)
                confirmButton.interactable = selectedCharacters.Count == maxCharacters;
            
            // 선택된 캐릭터 미리보기 업데이트
            UpdateSelectedCharactersPreview();
        }
        
        private void UpdateSelectedCharactersPreview()
        {
            // 기존 미리보기 제거
            foreach (Transform child in selectedCharactersContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 직업별로 그룹화
            var groupedByJob = selectedCharacters.GroupBy(c => c.jobClass);
            
            foreach (var group in groupedByJob)
            {
                GameObject jobGroupObj = new GameObject($"JobGroup_{group.Key}");
                jobGroupObj.transform.SetParent(selectedCharactersContainer, false);
                
                TextMeshProUGUI jobText = jobGroupObj.AddComponent<TextMeshProUGUI>();
                jobText.text = $"{JobClassSystem.GetJobClassName(group.Key)} x{group.Count()}";
                jobText.fontSize = 18;
                jobText.color = JobClassSystem.GetJobColor(group.Key);
            }
        }
        
        private void OnConfirmSelection()
        {
            if (selectedCharacters.Count == maxCharacters)
            {
                selectionPanel.SetActive(false);
                onSelectionComplete?.Invoke(selectedCharacters);
            }
        }
        
        public void Hide()
        {
            selectionPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 개별 캐릭터 카드 UI
    /// </summary>
    public class CharacterCard : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image portrait;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI jobText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image rarityBackground;
        [SerializeField] private GameObject selectedOverlay;
        [SerializeField] private Button selectButton;
        
        private Character character;
        private System.Action<Character> onClickCallback;
        private bool isSelected = false;
        
        public void Initialize(Character character, System.Action<Character> onClick)
        {
            this.character = character;
            this.onClickCallback = onClick;
            
            // UI 업데이트
            if (nameText != null)
                nameText.text = character.characterName;
            
            if (jobText != null)
            {
                jobText.text = JobClassSystem.GetJobClassName(character.jobClass);
                jobText.color = JobClassSystem.GetJobColor(character.jobClass);
            }
            
            if (levelText != null)
                levelText.text = $"Lv.{character.level}";
            
            // 레어도 색상
            if (rarityBackground != null)
                rarityBackground.color = GetRarityColor(character.rarity);
            
            // 버튼 이벤트
            if (selectButton != null)
                selectButton.onClick.AddListener(() => onClickCallback?.Invoke(character));
            
            SetSelected(false);
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (selectedOverlay != null)
                selectedOverlay.SetActive(selected);
        }
        
        private Color GetRarityColor(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.Common: return Color.gray;
                case CharacterRarity.Uncommon: return Color.green;
                case CharacterRarity.Rare: return Color.blue;
                case CharacterRarity.Epic: return new Color(0.5f, 0, 0.5f); // 보라
                case CharacterRarity.Legendary: return new Color(1f, 0.84f, 0); // 금색
                default: return Color.white;
            }
        }
    }
}