// =========================================
// UpgradePanelManager.cs (수정본)
// =========================================

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
    //  (1) 업그레이드 패널(10칸)에서
    //      "빈칸 전용" 스프라이트
    // ===========================================
    [Header("빈 슬롯용 스프라이트(업그레이드 10칸)")]
    [SerializeField] private Sprite emptyUpgradeSlotSprite;

    // ===========================================
    //  "등록 상태 (set2만)" 10칸
    // ===========================================
    [Header("업그레이드 등록 상태 이미지(10칸) - set2")]
    [SerializeField] private List<Image> upgradeRegisteredSlotImages; // 등록된 캐릭터 이미지 (10칸)

    [Header("업그레이드 슬롯(10칸) 레벨 텍스트")]
    [SerializeField] private List<TextMeshProUGUI> upgradeRegisteredSlotLevelTexts;

    // -----------------------------------------------------------
    //  deckPanelManager.registeredCharactersSet2와 공유할 배열
    // -----------------------------------------------------------
    private CharacterData[] registeredSet2_Up = new CharacterData[10];

    // ===========================================
    //  업그레이드 버튼 (10개)
    // ===========================================
    [Header("캐릭터 업그레이드 버튼 (총 10개)")]
    [SerializeField] private List<Button> upgradeButtons;

    // ===========================================
    // "재료(feed)"로 사용될 인벤토리 캐릭터
    // ===========================================
    private CharacterData feedCharacter = null;
    private int feedSlotIndex = -1;

    // (과거에는 "재료 캐릭터가 덱에 있어야 한다"는 로직으로 deckIndex를 보관)
    // 현재는 인벤토리에만 있어도 업그레이드 허용하도록 바꾸므로 -1로 두거나 사용 X.
    private int feedDeckSlotIndex = -1; 

    private void OnEnable()
    {
        // 업그레이드 패널 활성화 시 -> 덱 패널매니저 업그레이드모드=true
        if (deckPanelManager != null)
        {
            deckPanelManager.isUpgradeMode = true;
        }

        // 덱 패널에서 등록 상태 복사
        SetUpgradeRegisteredSlotsFromDeck();

        // 업그레이드 버튼 초기화
        SetupUpgradeButtons();
    }

    public void RefreshDisplay()
    {
        SetUpgradeRegisteredSlotsFromDeck();
    }

    /// <summary>
    /// 덱 패널에 등록된 10칸 정보 -> 업그레이드 패널에도 반영
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

        // 시각적 갱신
        for (int i = 0; i < 10; i++)
        {
            UpdateUpgradeRegisteredImage(i);
        }
    }

    private void UpdateUpgradeRegisteredImage(int i)
    {
        if (upgradeRegisteredSlotImages == null) return;
        if (i < 0 || i >= upgradeRegisteredSlotImages.Count) return;

        Image slotImg = upgradeRegisteredSlotImages[i];
        TextMeshProUGUI lvlText = (upgradeRegisteredSlotLevelTexts != null && i < upgradeRegisteredSlotLevelTexts.Count)
            ? upgradeRegisteredSlotLevelTexts[i]
            : null;

        CharacterData cData = registeredSet2_Up[i];
        if (cData == null)
        {
            // 빈 슬롯 -> emptyUpgradeSlotSprite
            if (slotImg)
            {
                slotImg.gameObject.SetActive(true);
                slotImg.sprite = emptyUpgradeSlotSprite;
            }
            if (lvlText)
            {
                lvlText.gameObject.SetActive(false);
            }

            // 해당 업그레이드 버튼 비활성화
            if (upgradeButtons != null && i < upgradeButtons.Count)
            {
                upgradeButtons[i].interactable = false;  // 빈칸이면 버튼 불가
            }
        }
        else
        {
            // 캐릭터 있음
            if (slotImg)
            {
                slotImg.gameObject.SetActive(true);
                if (cData.buttonIcon != null)
                    slotImg.sprite = cData.buttonIcon.sprite;
                else
                    slotImg.sprite = emptyUpgradeSlotSprite;
            }
            if (lvlText)
            {
                lvlText.gameObject.SetActive(true);
                lvlText.text = $"Lv.{cData.level}";
            }

            // 업그레이드 버튼 활성화
            if (upgradeButtons != null && i < upgradeButtons.Count)
            {
                upgradeButtons[i].interactable = true;
            }
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
            int idx = i;
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => OnClickUpgradeButton(idx));
        }
    }

    /// <summary>
    /// 인벤토리 20칸 중 어떤 것을 "재료"로 지정 (isUpgradeMode가 true일 때 DeckPanelManager에서 호출)
    /// 기존에는 "덱에 등록된 캐릭터만" 재료로 쓸 수 있게 막았으나,
    /// 이제는 인벤토리에만 있어도 허용하도록 수정함.
    /// </summary>
    public void SetFeedFromInventory(int slotIndex, CharacterData feedData)
    {
        if (feedData == null)
        {
            Debug.LogWarning("[UpgradePanel] 재료가 null => 지정 취소");
            feedCharacter = null;
            feedSlotIndex = -1;
            feedDeckSlotIndex = -1; 
            return;
        }

        // =========================
        // [수정] "덱에 있지 않으면 return" 로직 삭제
        // =========================
        // 예전 코드:
        //   int foundDeckIndex = ~~~
        //   if (foundDeckIndex == -1) { Debug.LogWarning(...); return; }
        // → 제거

        // 이제는 인벤토리에만 있어도 재료 사용 OK
        feedDeckSlotIndex = -1; // 덱 인덱스는 사용하지 않음
        feedCharacter = feedData;
        feedSlotIndex = slotIndex;

        Debug.Log($"[UpgradePanel] 재료로 선택됨: {feedData.characterName}, 인벤토리슬롯={slotIndex}");
    }

    /// <summary>
    /// i번 업그레이드 버튼 클릭 -> registeredSet2_Up[i] 캐릭터(=목표)를 레벨업 시도
    /// </summary>
    private void OnClickUpgradeButton(int index)
    {
        if (index < 0 || index >= 10)
        {
            Debug.LogWarning($"[UpgradePanel] 잘못된 인덱스: {index}");
            return;
        }

        // 업그레이드 대상
        CharacterData targetChar = registeredSet2_Up[index];
        if (targetChar == null)
        {
            Debug.LogWarning($"[UpgradePanel] {index}번 슬롯에 캐릭터가 없어서 업그레이드 불가");
            return;
        }

        // (1) 재료 캐릭터 존재 여부
        if (feedCharacter == null)
        {
            Debug.LogWarning("[UpgradePanel] 재료 캐릭터가 선택되지 않음!");
            return;
        }

        // =========================
        // [수정] 기존에는 "재료 캐릭터와 대상 슬롯 index가 같아야 함" 체크를 했지만 제거
        //        인벤토리에만 있어도 되도록 변경
        // =========================
        // 예전 코드:
        //   if (feedDeckSlotIndex != index) { ... return; }

        // (2) feedSlotIndex 유효성 검사
        if (feedSlotIndex < 0
            || feedSlotIndex >= characterInventory.sharedSlotData20.Length
            || characterInventory.sharedSlotData20[feedSlotIndex] == null)
        {
            Debug.LogWarning("[UpgradePanel] 재료 슬롯 인덱스가 유효하지 않거나 이미 빈칸임 => 업그레이드 불가");
            return;
        }

        // (3) 재료 소모 (인벤토리에서 제거)
        characterInventory.sharedSlotData20[feedSlotIndex] = null;
        characterInventory.RemoveFromInventory(feedCharacter); // 인벤토리 List에서도 제거

        // (4) 대상 Exp+1
        targetChar.currentExp += 1;
        targetChar.CheckLevelUp();

        Debug.Log($"[UpgradePanel] [{targetChar.characterName}] 업그레이드 (재료={feedCharacter.characterName} 소모)");

        // 업그레이드 후 재료 해제
        feedCharacter = null;
        feedSlotIndex = -1;
        feedDeckSlotIndex = -1; 

        // 슬롯 UI 갱신
        UpdateUpgradeRegisteredImage(index);

        // 인벤토리 다시 표시(20칸)
        if (deckPanelManager != null)
        {
            deckPanelManager.RefreshInventoryUI();
        }

        // 저장
        if (characterInventory != null)
        {
            characterInventory.SaveCharacters();
        }
    }
}
