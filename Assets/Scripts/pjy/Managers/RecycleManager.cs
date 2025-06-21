using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace pjy.Managers
{
    /// <summary>
    /// 리사이클 시스템 매니저
    /// 특정 종족 캐릭터를 다른 종족으로 변환하는 시스템
    /// </summary>
    public class RecycleManager : MonoBehaviour
    {
        [Header("리사이클 설정")]
        [SerializeField] private int recycleGoldCost = 50;
        [SerializeField] private GameObject recyclePanel;
        [SerializeField] private Button recycleButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        [Header("UI 요소")]
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI currentRaceText;
        [SerializeField] private TextMeshProUGUI newRaceText;
        [SerializeField] private Image characterImage;
        [SerializeField] private Transform characterSlot;
        
        [Header("종족 선택 버튼")]
        [SerializeField] private Button humanButton;
        [SerializeField] private Button orcButton;
        [SerializeField] private Button elfButton;
        
        [Header("매니저 참조")]
        [SerializeField] private CharacterInventoryManager inventoryManager;
        [SerializeField] private LobbySceneManager lobbyManager;
        
        private CharacterData selectedCharacter;
        private CharacterRace targetRace;
        private bool isRecycling = false;
        
        private void Start()
        {
            if (recyclePanel != null)
                recyclePanel.SetActive(false);
                
            // 버튼 이벤트 설정
            if (recycleButton != null)
                recycleButton.onClick.AddListener(OpenRecyclePanel);
                
            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmRecycle);
                
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CloseRecyclePanel);
                
            // 종족 선택 버튼 이벤트
            if (humanButton != null)
                humanButton.onClick.AddListener(() => SelectTargetRace(CharacterRace.Human));
                
            if (orcButton != null)
                orcButton.onClick.AddListener(() => SelectTargetRace(CharacterRace.Orc));
                
            if (elfButton != null)
                elfButton.onClick.AddListener(() => SelectTargetRace(CharacterRace.Elf));
                
            // 매니저 참조 찾기
            if (inventoryManager == null)
                inventoryManager = FindObjectOfType<CharacterInventoryManager>();
                
            if (lobbyManager == null)
                lobbyManager = FindObjectOfType<LobbySceneManager>();
        }
        
        /// <summary>
        /// 리사이클 패널 열기
        /// </summary>
        public void OpenRecyclePanel()
        {
            if (recyclePanel != null)
            {
                recyclePanel.SetActive(true);
                UpdateCostDisplay();
                ClearSelection();
            }
        }
        
        /// <summary>
        /// 리사이클 패널 닫기
        /// </summary>
        public void CloseRecyclePanel()
        {
            if (recyclePanel != null)
            {
                recyclePanel.SetActive(false);
                selectedCharacter = null;
                targetRace = CharacterRace.Human;
                isRecycling = false;
            }
        }
        
        /// <summary>
        /// 캐릭터 선택 (인벤토리에서 호출)
        /// </summary>
        public void SelectCharacterForRecycle(CharacterData character)
        {
            if (character == null || isRecycling) return;
            
            selectedCharacter = character;
            UpdateCharacterDisplay();
            UpdateRaceButtons();
            
            // 선택된 캐릭터와 다른 종족만 선택 가능하게 설정
            SetRaceButtonsInteractable();
        }
        
        /// <summary>
        /// 대상 종족 선택
        /// </summary>
        private void SelectTargetRace(CharacterRace race)
        {
            if (selectedCharacter == null || selectedCharacter.race == race) return;
            
            targetRace = race;
            UpdateNewRaceDisplay();
            
            // 확인 버튼 활성화
            if (confirmButton != null)
                confirmButton.interactable = true;
        }
        
        /// <summary>
        /// 리사이클 실행
        /// </summary>
        private void ConfirmRecycle()
        {
            if (selectedCharacter == null || isRecycling) return;
            
            // 골드 확인
            int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);
            if (currentGold < recycleGoldCost)
            {
                Debug.Log("[RecycleManager] 골드가 부족합니다!");
                ShowInsufficientGoldMessage();
                return;
            }
            
            StartCoroutine(PerformRecycle());
        }
        
        /// <summary>
        /// 리사이클 처리 코루틴
        /// </summary>
        private IEnumerator PerformRecycle()
        {
            isRecycling = true;
            
            // 골드 차감
            int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);
            PlayerPrefs.SetInt("PlayerGold", currentGold - recycleGoldCost);
            
            // 로비 매니저 UI 업데이트
            if (lobbyManager != null)
                lobbyManager.UpdateCurrencyDisplay();
            
            // 애니메이션 효과 (선택사항)
            if (characterImage != null)
            {
                // 회전 애니메이션
                float duration = 1f;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float rotation = Mathf.Lerp(0, 360, elapsed / duration);
                    characterImage.transform.rotation = Quaternion.Euler(0, 0, rotation);
                    yield return null;
                }
            }
            
            // 종족 변경
            CharacterRace oldRace = selectedCharacter.race;
            selectedCharacter.race = targetRace;
            selectedCharacter.tribe = ConvertToRaceType(targetRace);
            
            // 스프라이트 업데이트 (종족별 스프라이트가 있다면)
            UpdateCharacterSprite(selectedCharacter);
            
            // 인벤토리 업데이트
            if (inventoryManager != null)
            {
                inventoryManager.RefreshInventoryDisplay();
            }
            
            Debug.Log($"[RecycleManager] {selectedCharacter.characterName}의 종족이 {oldRace}에서 {targetRace}로 변경되었습니다!");
            
            // 성공 메시지 표시
            ShowSuccessMessage();
            
            yield return new WaitForSeconds(1f);
            
            // 패널 닫기
            CloseRecyclePanel();
            isRecycling = false;
        }
        
        /// <summary>
        /// CharacterRace를 RaceType으로 변환
        /// </summary>
        private RaceType ConvertToRaceType(CharacterRace race)
        {
            switch (race)
            {
                case CharacterRace.Human:
                    return RaceType.Human;
                case CharacterRace.Orc:
                    return RaceType.Orc;
                case CharacterRace.Elf:
                    return RaceType.Elf;
                default:
                    return RaceType.Human;
            }
        }
        
        /// <summary>
        /// 종족별 스프라이트 업데이트
        /// </summary>
        private void UpdateCharacterSprite(CharacterData character)
        {
            // 종족별 스프라이트가 있다면 여기서 변경
            // 예: character.characterSprite = GetRaceSprite(character.race, character.star);
        }
        
        /// <summary>
        /// UI 업데이트 메서드들
        /// </summary>
        private void UpdateCostDisplay()
        {
            if (costText != null)
                costText.text = $"비용: {recycleGoldCost} 골드";
        }
        
        private void UpdateCharacterDisplay()
        {
            if (selectedCharacter == null) return;
            
            if (characterImage != null && selectedCharacter.characterSprite != null)
                characterImage.sprite = selectedCharacter.characterSprite;
                
            if (currentRaceText != null)
                currentRaceText.text = $"현재 종족: {selectedCharacter.race}";
        }
        
        private void UpdateNewRaceDisplay()
        {
            if (newRaceText != null)
                newRaceText.text = $"변경될 종족: {targetRace}";
        }
        
        private void SetRaceButtonsInteractable()
        {
            if (selectedCharacter == null) return;
            
            if (humanButton != null)
                humanButton.interactable = (selectedCharacter.race != CharacterRace.Human);
                
            if (orcButton != null)
                orcButton.interactable = (selectedCharacter.race != CharacterRace.Orc);
                
            if (elfButton != null)
                elfButton.interactable = (selectedCharacter.race != CharacterRace.Elf);
        }
        
        private void UpdateRaceButtons()
        {
            // 종족 버튼 하이라이트 업데이트
            if (humanButton != null)
            {
                var colors = humanButton.colors;
                colors.normalColor = (targetRace == CharacterRace.Human) ? Color.yellow : Color.white;
                humanButton.colors = colors;
            }
            
            if (orcButton != null)
            {
                var colors = orcButton.colors;
                colors.normalColor = (targetRace == CharacterRace.Orc) ? Color.yellow : Color.white;
                orcButton.colors = colors;
            }
            
            if (elfButton != null)
            {
                var colors = elfButton.colors;
                colors.normalColor = (targetRace == CharacterRace.Elf) ? Color.yellow : Color.white;
                elfButton.colors = colors;
            }
        }
        
        private void ClearSelection()
        {
            selectedCharacter = null;
            targetRace = CharacterRace.Human;
            
            if (characterImage != null)
                characterImage.sprite = null;
                
            if (currentRaceText != null)
                currentRaceText.text = "캐릭터를 선택하세요";
                
            if (newRaceText != null)
                newRaceText.text = "";
                
            if (confirmButton != null)
                confirmButton.interactable = false;
        }
        
        private void ShowInsufficientGoldMessage()
        {
            // 골드 부족 메시지 표시
            StartCoroutine(ShowTemporaryMessage("골드가 부족합니다!", 2f));
        }
        
        private void ShowSuccessMessage()
        {
            // 성공 메시지 표시
            StartCoroutine(ShowTemporaryMessage("종족 변환 성공!", 1.5f));
        }
        
        private IEnumerator ShowTemporaryMessage(string message, float duration)
        {
            // 임시 메시지 표시 (Toast 형태)
            GameObject messageObj = new GameObject("TempMessage");
            messageObj.transform.SetParent(transform, false);
            
            TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
            messageText.text = message;
            messageText.fontSize = 24;
            messageText.alignment = TextAlignmentOptions.Center;
            
            RectTransform rect = messageObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(300, 50);
            
            yield return new WaitForSeconds(duration);
            
            Destroy(messageObj);
        }
        
        /// <summary>
        /// 인벤토리에서 캐릭터 드래그 시작 시 호출
        /// </summary>
        public void OnCharacterDragStart(CharacterData character)
        {
            if (recyclePanel != null && recyclePanel.activeSelf)
            {
                SelectCharacterForRecycle(character);
            }
        }
    }
}