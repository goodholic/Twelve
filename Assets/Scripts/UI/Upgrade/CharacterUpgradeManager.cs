using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using GuildMaster.Data;
using GuildMaster.Game;

namespace GuildMaster.UI
{
    /// <summary>
    /// 캐릭터 업그레이드 시스템 매니저 (인게임 전 로비에서 사용)
    /// 같은 종족/등급 캐릭터를 재료로 경험치 획득 및 레벨업
    /// </summary>
    public class CharacterUpgradeManager : MonoBehaviour
    {
        [Header("업그레이드 설정")]
        [SerializeField] private int goldPerUpgrade = 10;
        [SerializeField] private float expPerUpgrade = 1f;
        [SerializeField] private int maxLevel = 30;
        
        [Header("UI 요소")]
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private Transform targetSlot;
        [SerializeField] private Transform materialSlots;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;
        
        [Header("정보 표시")]
        [SerializeField] private TextMeshProUGUI targetNameText;
        [SerializeField] private TextMeshProUGUI targetLevelText;
        [SerializeField] private TextMeshProUGUI expBarText;
        [SerializeField] private Slider expBar;
        [SerializeField] private TextMeshProUGUI goldCostText;
        [SerializeField] private TextMeshProUGUI materialCountText;
        
        [Header("매니저 참조")]
        [SerializeField] private CharacterInventoryManager inventoryManager;
        [SerializeField] private LobbySceneManager lobbyManager;
        
        private CharacterData targetCharacter;
        private List<CharacterData> materialCharacters = new List<CharacterData>();
        private bool isUpgrading = false;
        
        private void Start()
        {
            if (upgradePanel != null)
                upgradePanel.SetActive(false);
                
            // 버튼 이벤트 설정
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(PerformUpgrade);
                
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseUpgradePanel);
                
            // 매니저 참조 찾기
            if (inventoryManager == null)
                inventoryManager = FindObjectOfType<CharacterInventoryManager>();
                
            if (lobbyManager == null)
                lobbyManager = FindObjectOfType<LobbySceneManager>();
        }
        
        /// <summary>
        /// 업그레이드 패널 열기
        /// </summary>
        public void OpenUpgradePanel()
        {
            if (upgradePanel != null)
            {
                upgradePanel.SetActive(true);
                ClearSlots();
                UpdateUI();
            }
        }
        
        /// <summary>
        /// 업그레이드 패널 닫기
        /// </summary>
        public void CloseUpgradePanel()
        {
            if (upgradePanel != null)
            {
                upgradePanel.SetActive(false);
                targetCharacter = null;
                materialCharacters.Clear();
                ClearSlots();
            }
        }
        
        /// <summary>
        /// 대상 캐릭터 설정 (드래그 앤 드롭)
        /// </summary>
        public void SetTargetCharacter(CharacterData character)
        {
            if (character == null || isUpgrading) return;
            
            targetCharacter = character;
            UpdateTargetDisplay();
            UpdateUI();
        }
        
        /// <summary>
        /// 재료 캐릭터 추가 (드래그 앤 드롭)
        /// </summary>
        public void AddMaterialCharacter(CharacterData character)
        {
            if (character == null || isUpgrading) return;
            if (targetCharacter == null)
            {
                Debug.Log("[CharacterUpgradeManager] 먼저 업그레이드할 대상 캐릭터를 설정하세요!");
                return;
            }
            
            // 같은 종족/등급 확인
            if (character.race != targetCharacter.race || character.star != targetCharacter.star)
            {
                Debug.Log("[CharacterUpgradeManager] 같은 종족과 등급의 캐릭터만 재료로 사용할 수 있습니다!");
                ShowMessage("같은 종족과 등급만 가능합니다!");
                return;
            }
            
            // 자기 자신은 재료로 사용 불가
            if (character == targetCharacter)
            {
                Debug.Log("[CharacterUpgradeManager] 자기 자신은 재료로 사용할 수 없습니다!");
                return;
            }
            
            // 이미 추가된 재료인지 확인
            if (materialCharacters.Contains(character))
            {
                Debug.Log("[CharacterUpgradeManager] 이미 추가된 재료입니다!");
                return;
            }
            
            // 재료 추가
            materialCharacters.Add(character);
            UpdateMaterialDisplay();
            UpdateUI();
        }
        
        /// <summary>
        /// 재료 캐릭터 제거
        /// </summary>
        public void RemoveMaterialCharacter(CharacterData character)
        {
            if (materialCharacters.Remove(character))
            {
                UpdateMaterialDisplay();
                UpdateUI();
            }
        }
        
        /// <summary>
        /// 업그레이드 실행
        /// </summary>
        private void PerformUpgrade()
        {
            if (targetCharacter == null || materialCharacters.Count == 0 || isUpgrading)
                return;
                
            // 레벨 체크
            if (targetCharacter.level >= maxLevel)
            {
                ShowMessage($"최대 레벨({maxLevel})에 도달했습니다!");
                return;
            }
            
            // 골드 체크
            int totalCost = goldPerUpgrade * materialCharacters.Count;
            int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);
            
            if (currentGold < totalCost)
            {
                ShowMessage("골드가 부족합니다!");
                return;
            }
            
            StartCoroutine(UpgradeRoutine(totalCost));
        }
        
        /// <summary>
        /// 업그레이드 처리 코루틴
        /// </summary>
        private IEnumerator UpgradeRoutine(int goldCost)
        {
            isUpgrading = true;
            
            // 골드 차감
            int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);
            PlayerPrefs.SetInt("PlayerGold", currentGold - goldCost);
            
            // 경험치 증가
            float totalExp = expPerUpgrade * materialCharacters.Count;
            float previousExp = targetCharacter.currentExp;
            targetCharacter.currentExp += (int)totalExp;
            
            // 애니메이션 효과
            yield return StartCoroutine(AnimateExpGain(previousExp, targetCharacter.currentExp));
            
            // 레벨업 체크
            int levelUps = 0;
            while (targetCharacter.currentExp >= targetCharacter.expToNextLevel && targetCharacter.level < maxLevel)
            {
                targetCharacter.currentExp -= targetCharacter.expToNextLevel;
                targetCharacter.level++;
                levelUps++;
                
                // 다음 레벨 필요 경험치 증가
                targetCharacter.expToNextLevel = (int)CalculateExpToNextLevel(targetCharacter.level);
                
                // 스탯 증가
                ApplyLevelUpBonus(targetCharacter);
            }
            
            // 재료 캐릭터 제거
            if (inventoryManager != null)
            {
                inventoryManager.ConsumeCharactersForUpgrade(materialCharacters);
            }
            
            // 효과 표시
            if (levelUps > 0)
            {
                ShowLevelUpEffect(levelUps);
                yield return new WaitForSeconds(1f);
            }
            
            // UI 업데이트
            materialCharacters.Clear();
            UpdateTargetDisplay();
            UpdateMaterialDisplay();
            UpdateUI();
            
            // 로비 UI 업데이트
            if (lobbyManager != null)
            {
                lobbyManager.UpdateCurrencyDisplay();
                lobbyManager.RefreshPanel();
            }
            
            ShowMessage($"업그레이드 완료! (+{totalExp}% 경험치)");
            
            isUpgrading = false;
        }
        
        /// <summary>
        /// 경험치 획득 애니메이션
        /// </summary>
        private IEnumerator AnimateExpGain(float startExp, float endExp)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float currentExp = Mathf.Lerp(startExp, endExp, t);
                
                UpdateExpBar(currentExp);
                yield return null;
            }
            
            UpdateExpBar(endExp);
        }
        
        /// <summary>
        /// 다음 레벨 필요 경험치 계산
        /// </summary>
        private float CalculateExpToNextLevel(int level)
        {
            // 레벨이 올라갈수록 필요 경험치 증가
            return 100f + (level - 1) * 20f;
        }
        
        /// <summary>
        /// 레벨업 시 스탯 증가
        /// </summary>
        private void ApplyLevelUpBonus(CharacterData character)
        {
            // 레벨당 스탯 증가율
            float statIncreaseRate = 0.05f; // 5%
            
            character.attackPower = (int)(character.attackPower * (1f + statIncreaseRate));
            character.maxHP = (int)(character.maxHP * (1f + statIncreaseRate));
            character.health = character.maxHP;
            
            Debug.Log($"[CharacterUpgradeManager] {character.characterName} 레벨업! Lv.{character.level} (공격력: {character.attackPower}, 체력: {character.maxHP})");
        }
        
        /// <summary>
        /// UI 업데이트 메서드들
        /// </summary>
        private void UpdateTargetDisplay()
        {
            if (targetCharacter == null) return;
            
            if (targetNameText != null)
                targetNameText.text = targetCharacter.characterName;
                
            if (targetLevelText != null)
                targetLevelText.text = $"Lv.{targetCharacter.level}";
                
            UpdateExpBar(targetCharacter.currentExp);
            
            // 대상 슬롯에 캐릭터 이미지 표시
            if (targetSlot != null)
            {
                Image slotImage = targetSlot.GetComponent<Image>();
                if (slotImage != null && targetCharacter.characterSprite != null)
                {
                    slotImage.sprite = targetCharacter.characterSprite;
                    slotImage.color = Color.white;
                }
            }
        }
        
        private void UpdateExpBar(float currentExp)
        {
            if (targetCharacter == null) return;
            
            float expPercent = currentExp / targetCharacter.expToNextLevel;
            
            if (expBar != null)
                expBar.value = expPercent;
                
            if (expBarText != null)
                expBarText.text = $"{currentExp:F0}/{targetCharacter.expToNextLevel:F0} ({expPercent * 100:F0}%)";
        }
        
        private void UpdateMaterialDisplay()
        {
            if (materialSlots == null) return;
            
            // 모든 슬롯 초기화
            foreach (Transform slot in materialSlots)
            {
                Image slotImage = slot.GetComponent<Image>();
                if (slotImage != null)
                {
                    slotImage.sprite = null;
                    slotImage.color = new Color(1, 1, 1, 0);
                }
            }
            
            // 재료 캐릭터 표시
            for (int i = 0; i < materialCharacters.Count && i < materialSlots.childCount; i++)
            {
                Transform slot = materialSlots.GetChild(i);
                Image slotImage = slot.GetComponent<Image>();
                
                if (slotImage != null && materialCharacters[i].characterSprite != null)
                {
                    slotImage.sprite = materialCharacters[i].characterSprite;
                    slotImage.color = Color.white;
                }
            }
        }
        
        private void UpdateUI()
        {
            // 비용 표시
            int totalCost = goldPerUpgrade * materialCharacters.Count;
            if (goldCostText != null)
                goldCostText.text = $"비용: {totalCost} 골드";
                
            // 재료 수 표시
            if (materialCountText != null)
                materialCountText.text = $"재료: {materialCharacters.Count}개";
                
            // 업그레이드 버튼 활성화
            if (upgradeButton != null)
            {
                bool canUpgrade = targetCharacter != null && 
                                 materialCharacters.Count > 0 && 
                                 targetCharacter.level < maxLevel &&
                                 PlayerPrefs.GetInt("PlayerGold", 0) >= totalCost;
                                 
                upgradeButton.interactable = canUpgrade;
            }
        }
        
        private void ClearSlots()
        {
            // 대상 슬롯 초기화
            if (targetSlot != null)
            {
                Image slotImage = targetSlot.GetComponent<Image>();
                if (slotImage != null)
                {
                    slotImage.sprite = null;
                    slotImage.color = new Color(1, 1, 1, 0);
                }
            }
            
            // 재료 슬롯 초기화
            UpdateMaterialDisplay();
        }
        
        /// <summary>
        /// 레벨업 효과 표시
        /// </summary>
        private void ShowLevelUpEffect(int levelUps)
        {
            GameObject effectObj = new GameObject("LevelUpEffect");
            effectObj.transform.SetParent(transform, false);
            
            TextMeshProUGUI effectText = effectObj.AddComponent<TextMeshProUGUI>();
            effectText.text = $"LEVEL UP! x{levelUps}";
            effectText.fontSize = 48;
            effectText.color = Color.yellow;
            effectText.alignment = TextAlignmentOptions.Center;
            
            RectTransform rect = effectObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(400, 100);
            
            // 애니메이션
            StartCoroutine(LevelUpAnimation(rect, effectText));
        }
        
        private IEnumerator LevelUpAnimation(RectTransform rect, TextMeshProUGUI text)
        {
            float duration = 2f;
            float elapsed = 0f;
            Vector2 startPos = rect.anchoredPosition;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 위로 이동 + 페이드 아웃
                rect.anchoredPosition = startPos + Vector2.up * (100f * t);
                text.color = new Color(1, 1, 0, 1f - t);
                
                // 크기 변화
                float scale = 1f + (0.5f * Mathf.Sin(t * Mathf.PI));
                rect.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            Destroy(rect.gameObject);
        }
        
        /// <summary>
        /// 메시지 표시
        /// </summary>
        private void ShowMessage(string message)
        {
            StartCoroutine(ShowTemporaryMessage(message, 2f));
        }
        
        private IEnumerator ShowTemporaryMessage(string message, float duration)
        {
            GameObject messageObj = new GameObject("TempMessage");
            messageObj.transform.SetParent(transform, false);
            
            TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
            messageText.text = message;
            messageText.fontSize = 24;
            messageText.alignment = TextAlignmentOptions.Center;
            
            RectTransform rect = messageObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.7f);
            rect.anchorMax = new Vector2(0.5f, 0.7f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(400, 50);
            
            // 배경 추가
            Image bg = messageObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);
            
            yield return new WaitForSeconds(duration);
            
            Destroy(messageObj);
        }
        
        /// <summary>
        /// 드래그 앤 드롭 지원 메서드
        /// </summary>
        public void OnCharacterDroppedOnTargetSlot(CharacterData character)
        {
            SetTargetCharacter(character);
        }
        
        public void OnCharacterDroppedOnMaterialSlot(CharacterData character)
        {
            AddMaterialCharacter(character);
        }
    }
}