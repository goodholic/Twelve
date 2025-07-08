using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 전투 캐릭터 선택 UI
    /// </summary>
    public class CharacterSelectionUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Transform characterButtonContainer;
        [SerializeField] private GameObject characterButtonPrefab;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI selectedCharacterText;
        [SerializeField] private TextMeshProUGUI characterInfoText;
        
        [Header("시스템 참조")]
        [SerializeField] private BattleCharacterPlacement placementSystem;
        [SerializeField] private CharacterCollection characterCollection;
        
        private CharacterData selectedCharacter;
        private List<CharacterButton> characterButtons = new List<CharacterButton>();
        
        private void Start()
        {
            InitializeCharacterButtons();
            confirmButton.onClick.AddListener(OnConfirmSelection);
            UpdateUI();
        }
        
        /// <summary>
        /// 캐릭터 버튼 초기화
        /// </summary>
        private void InitializeCharacterButtons()
        {
            // 기존 버튼 제거
            foreach (Transform child in characterButtonContainer)
            {
                Destroy(child.gameObject);
            }
            characterButtons.Clear();
            
            // 컬렉션에서 캐릭터 가져오기
            List<CharacterData> availableCharacters = GetAvailableCharacters();
            
            foreach (CharacterData character in availableCharacters)
            {
                GameObject buttonObj = Instantiate(characterButtonPrefab, characterButtonContainer);
                CharacterButton charButton = buttonObj.GetComponent<CharacterButton>();
                
                if (charButton == null)
                {
                    charButton = buttonObj.AddComponent<CharacterButton>();
                }
                
                charButton.Initialize(character, this);
                characterButtons.Add(charButton);
            }
        }
        
        /// <summary>
        /// 사용 가능한 캐릭터 목록 가져오기
        /// </summary>
        private List<CharacterData> GetAvailableCharacters()
        {
            List<CharacterData> characters = new List<CharacterData>();
            
            // 테스트용 캐릭터 생성 (실제로는 CharacterCollection에서 가져옴)
            characters.Add(CreateTestCharacter("전사 철수", JobClass.Warrior, 5));
            characters.Add(CreateTestCharacter("기사 영희", JobClass.Knight, 4));
            characters.Add(CreateTestCharacter("마법사 민수", JobClass.Wizard, 6));
            characters.Add(CreateTestCharacter("성직자 지영", JobClass.Priest, 3));
            characters.Add(CreateTestCharacter("도적 현우", JobClass.Rogue, 4));
            characters.Add(CreateTestCharacter("현자 서연", JobClass.Sage, 5));
            characters.Add(CreateTestCharacter("궁수 준호", JobClass.Archer, 4));
            characters.Add(CreateTestCharacter("총사 민지", JobClass.Gunner, 5));
            
            return characters;
        }
        
        /// <summary>
        /// 테스트용 캐릭터 생성
        /// </summary>
        private CharacterData CreateTestCharacter(string name, JobClass job, int level)
        {
            CharacterData character = new CharacterData(
                System.Guid.NewGuid().ToString(),
                name,
                job,
                level,
                CharacterRarity.Common
            );
            
            // 직업별 기본 스탯 설정
            switch (job)
            {
                case JobClass.Warrior:
                    character.baseHP = 150;
                    character.baseAttack = 20;
                    character.baseDefense = 15;
                    break;
                case JobClass.Knight:
                    character.baseHP = 180;
                    character.baseAttack = 15;
                    character.baseDefense = 25;
                    break;
                case JobClass.Wizard:
                    character.baseHP = 80;
                    character.baseMagicPower = 30;
                    character.baseMP = 100;
                    break;
                case JobClass.Priest:
                    character.baseHP = 100;
                    character.baseMagicPower = 20;
                    character.baseMP = 120;
                    break;
                case JobClass.Rogue:
                    character.baseHP = 100;
                    character.baseAttack = 25;
                    character.baseSpeed = 20;
                    break;
                case JobClass.Sage:
                    character.baseHP = 120;
                    character.baseMagicPower = 25;
                    character.baseAttack = 15;
                    break;
                case JobClass.Archer:
                    character.baseHP = 110;
                    character.baseAttack = 22;
                    character.baseSpeed = 15;
                    break;
                case JobClass.Gunner:
                    character.baseHP = 100;
                    character.baseAttack = 28;
                    character.baseSpeed = 12;
                    break;
            }
            
            return character;
        }
        
        /// <summary>
        /// 캐릭터 선택
        /// </summary>
        public void SelectCharacter(CharacterData character)
        {
            selectedCharacter = character;
            UpdateUI();
        }
        
        /// <summary>
        /// 선택 확인
        /// </summary>
        private void OnConfirmSelection()
        {
            if (selectedCharacter != null && placementSystem != null)
            {
                placementSystem.SelectCharacter(selectedCharacter);
                
                // 선택 후 초기화
                selectedCharacter = null;
                UpdateUI();
            }
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            if (selectedCharacter != null)
            {
                selectedCharacterText.text = $"선택된 캐릭터: {selectedCharacter.characterName}";
                
                // 캐릭터 정보 표시
                string info = $"직업: {selectedCharacter.jobClass}\n";
                info += $"레벨: {selectedCharacter.level}\n";
                info += $"공격 범위: {selectedCharacter.GetAttackRange()} 타일\n";
                info += $"공격 타입: {selectedCharacter.GetAttackType()}\n";
                info += $"HP: {selectedCharacter.currentHP}\n";
                info += $"공격력: {selectedCharacter.totalAttack}";
                
                characterInfoText.text = info;
                confirmButton.interactable = true;
            }
            else
            {
                selectedCharacterText.text = "캐릭터를 선택하세요";
                characterInfoText.text = "";
                confirmButton.interactable = false;
            }
            
            // 버튼 상태 업데이트
            foreach (CharacterButton button in characterButtons)
            {
                button.UpdateSelection(selectedCharacter);
            }
        }
    }
    
    /// <summary>
    /// 캐릭터 버튼 컴포넌트
    /// </summary>
    public class CharacterButton : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI jobText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image selectionFrame;
        [SerializeField] private Image characterIcon;
        
        private CharacterData characterData;
        private CharacterSelectionUI selectionUI;
        
        /// <summary>
        /// 버튼 초기화
        /// </summary>
        public void Initialize(CharacterData character, CharacterSelectionUI ui)
        {
            characterData = character;
            selectionUI = ui;
            
            // UI 요소가 없으면 자동으로 찾기
            if (button == null) button = GetComponent<Button>();
            if (nameText == null) nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (jobText == null) jobText = transform.Find("JobText")?.GetComponent<TextMeshProUGUI>();
            if (levelText == null) levelText = transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            if (selectionFrame == null) selectionFrame = transform.Find("SelectionFrame")?.GetComponent<Image>();
            if (characterIcon == null) characterIcon = transform.Find("Icon")?.GetComponent<Image>();
            
            // 텍스트 설정
            if (nameText != null) nameText.text = character.characterName;
            if (jobText != null) jobText.text = character.jobClass.ToString();
            if (levelText != null) levelText.text = $"Lv.{character.level}";
            
            // 아이콘 설정 (있다면)
            if (characterIcon != null && character.characterSprite != null)
            {
                characterIcon.sprite = character.characterSprite;
            }
            
            // 버튼 클릭 이벤트
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClick);
            
            UpdateSelection(null);
        }
        
        /// <summary>
        /// 버튼 클릭 처리
        /// </summary>
        private void OnButtonClick()
        {
            selectionUI.SelectCharacter(characterData);
        }
        
        /// <summary>
        /// 선택 상태 업데이트
        /// </summary>
        public void UpdateSelection(CharacterData selectedCharacter)
        {
            bool isSelected = characterData == selectedCharacter;
            
            if (selectionFrame != null)
            {
                selectionFrame.gameObject.SetActive(isSelected);
            }
            
            // 선택된 버튼 강조
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = isSelected ? new Color(0.9f, 0.9f, 1f) : Color.white;
                button.colors = colors;
            }
        }
    }
}