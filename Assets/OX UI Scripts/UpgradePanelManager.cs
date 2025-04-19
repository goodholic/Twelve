// Assets\OX UI Scripts\UpgradePanelManager.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class UpgradePanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("DeckPanelManager (등록상태 복사)")]
    [SerializeField] private DeckPanelManager deckPanelManager;

    // ===========================================
    //  "등록 상태 (set2만)" 10칸
    // ===========================================
    [Header("업그레이드 등록 상태 이미지(10칸) - set2")]
    [SerializeField] private List<Image> upgradeRegisteredSlotImages; // 세트2만

    // 덱 패널의 registeredCharactersSet2와 공유할 배열 (10칸)
    private CharacterData[] registeredSet2_Up = new CharacterData[10];

    // ===========================================
    //  업그레이드 버튼 (10개)
    // ===========================================
    [Header("캐릭터 업그레이드 버튼 (총 10개)")]
    [SerializeField] private List<Button> upgradeButtons;

    // ===========================================
    //   (추가) "재료(feed)"로 사용될 인벤토리 캐릭터
    // ===========================================
    private CharacterData feedCharacter = null;
    private int feedSlotIndex = -1;  // 20칸 중 어떤 슬롯이었나

    private void OnEnable()
    {
        // 덱 패널에서 현재 등록 상태(10칸) 가져오기
        SetUpgradeRegisteredSlotsFromDeck();

        // 업그레이드 버튼(10개) 초기화
        SetupUpgradeButtons();
    }

    /// <summary>
    /// 호환성을 위해 남겨둠 - 20칸 슬롯 시각은 제거되었지만, 등록슬롯만 다시 세팅
    /// </summary>
    public void RefreshDisplay()
    {
        SetUpgradeRegisteredSlotsFromDeck();
    }

    /// <summary>
    /// 덱 패널에서 set2 등록 상태(10칸)를 복사해와서
    /// 업그레이드 패널에도 동일하게 표시
    /// </summary>
    public void SetUpgradeRegisteredSlotsFromDeck()
    {
        if (deckPanelManager == null)
        {
            Debug.LogWarning("[UpgradePanelManager] deckPanelManager가 null임");
            return;
        }

        var deckSet2 = deckPanelManager.registeredCharactersSet2; // 10칸
        for (int i = 0; i < 10; i++)
        {
            registeredSet2_Up[i] = deckSet2[i];
        }

        // 시각적으로 이미지 갱신
        for (int i = 0; i < 10; i++)
        {
            UpdateUpgradeRegisteredImage(i);
        }
    }

    private void UpdateUpgradeRegisteredImage(int i)
    {
        if (upgradeRegisteredSlotImages == null
            || i < 0 || i >= upgradeRegisteredSlotImages.Count)
            return;

        Image slotImg = upgradeRegisteredSlotImages[i];
        if (slotImg == null) return;

        CharacterData cData = registeredSet2_Up[i];
        if (cData == null)
        {
            slotImg.gameObject.SetActive(false);
        }
        else
        {
            slotImg.gameObject.SetActive(true);
            slotImg.sprite = (cData.buttonIcon != null)
                ? cData.buttonIcon.sprite
                : null;
        }
    }

    private void SetupUpgradeButtons()
    {
        if (upgradeButtons == null || upgradeButtons.Count < 10)
        {
            Debug.LogWarning("[UpgradePanelManager] upgradeButtons(10개) 부족");
            return;
        }

        for (int i = 0; i < upgradeButtons.Count; i++)
        {
            int btnIndex = i;
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => OnClickUpgradeButton(btnIndex));
        }
    }

    /// <summary>
    /// (추가) 인벤토리 20칸 중 어떤 슬롯을 "재료"로 쓸지 지정
    /// </summary>
    public void SetFeedFromInventory(int slotIndex, CharacterData feedData)
    {
        if (feedData == null)
        {
            Debug.LogWarning("[UpgradePanel] 재료가 null임 - 지정 취소");
            feedCharacter = null;
            feedSlotIndex = -1;
            return;
        }
        feedCharacter = feedData;
        feedSlotIndex = slotIndex;

        Debug.Log($"[UpgradePanel] 업그레이드 재료 지정 => slot={slotIndex}, char={feedData.characterName}");
    }

    /// <summary>
    /// i번 업그레이드 버튼 -> registeredSet2_Up[i] 캐릭터 업그레이드
    /// 재료(feedCharacter)가 있다면, 그것을 소비하고 경험치 +1
    /// </summary>
    private void OnClickUpgradeButton(int index)
    {
        if (index < 0 || index >= 10)
        {
            Debug.LogWarning($"[UpgradePanel] 잘못된 업그레이드 버튼 인덱스: {index}");
            return;
        }

        CharacterData targetChar = registeredSet2_Up[index];
        if (targetChar == null)
        {
            Debug.LogWarning($"[UpgradePanel] {index}번 슬롯에 캐릭터가 없습니다.");
            return;
        }

        // -------------------------
        // (1) 재료가 있는지 확인
        // -------------------------
        if (feedCharacter == null)
        {
            Debug.LogWarning("[UpgradePanel] 업그레이드할 재료(feedCharacter)가 선택되지 않음!");
            return;
        }
        // 혹시 feedSlotIndex도 유효해야 함
        if (feedSlotIndex < 0
            || feedSlotIndex >= characterInventory.sharedSlotData20.Length
            || characterInventory.sharedSlotData20[feedSlotIndex] == null)
        {
            Debug.LogWarning("[UpgradePanel] 재료 슬롯 인덱스가 잘못되었거나 빈칸임 - 업그레이드 불가");
            return;
        }

        // -------------------------
        // (2) 재료 소모: 인벤토리에서 제거
        // -------------------------
        characterInventory.sharedSlotData20[feedSlotIndex] = null;
        characterInventory.RemoveFromInventory(feedCharacter);

        // -------------------------
        // (3) 대상 캐릭터 Exp +1
        // -------------------------
        targetChar.currentExp += 1;
        targetChar.CheckLevelUp();

        Debug.Log($"[UpgradePanel] {index}번 업그레이드 => [{targetChar.characterName}] Exp+1 (재료={feedCharacter.characterName} 소모)");

        // 재료 참조 해제
        feedCharacter = null;
        feedSlotIndex = -1;

        // UI 갱신
        UpdateUpgradeRegisteredImage(index);

        // 덱 패널 쪽 20칸도 새로고침
        deckPanelManager.RefreshDeckDisplay();

        // 저장
        if (characterInventory != null)
        {
            characterInventory.SaveCharacters();
        }
    }
}
