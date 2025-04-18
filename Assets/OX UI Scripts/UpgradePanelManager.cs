// ================================================
//  Assets\OX UI Scripts\UpgradePanelManager.cs
//    - 12슬롯 제거, 20슬롯으로 통합
//    - set2만 남김 (set1 제거)
//    - 덱/업그레이드 "공용" 배열을 그대로 읽어 UI 반영
// ================================================

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

    // =============================
    //  업그레이드용 20칸 (중복 포함)
    // =============================
    [Header("업그레이드 패널용 20칸 슬롯")]
    [SerializeField] private List<GameObject> upgradeSlots;

    [Header("빈 슬롯용 스프라이트")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("빈 슬롯용 텍스트")]
    [SerializeField] private string emptySlotText = "";

    // =============================
    //  업그레이드용 등록 상태 (set2만)
    // =============================
    [Header("업그레이드 등록 상태 이미지(8칸) - set2")]
    [SerializeField] private List<Image> upgradeRegisteredSlotImages;  // 세트2만

    // (세트2만 유지)
    private CharacterData[] registeredSet2_Up = new CharacterData[8];

    // =============================
    //  업그레이드 버튼 (8개)
    // =============================
    [Header("캐릭터 업그레이드 버튼 (총 8개)")]
    [SerializeField] private List<Button> upgradeButtons;

    // =============================
    //  선택된(토글) 캐릭터들
    // =============================
    private List<CharacterData> selectedCharacters = new List<CharacterData>();

    private void OnEnable()
    {
        // 20칸 표시
        RefreshDisplay();

        // 덱 패널에서 set2 등록 상태 복사
        SetUpgradeRegisteredSlotsFromDeck();

        // 업그레이드 버튼 초기화
        SetupUpgradeButtons();
    }

    // ===========================================
    //  20칸에 "sharedSlotData20" 내용을 표시
    // ===========================================
    public void RefreshDisplay()
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[UpgradePanelManager] characterInventory가 null");
            return;
        }
        if (upgradeSlots == null || upgradeSlots.Count < 20)
        {
            Debug.LogWarning("[UpgradePanelManager] upgradeSlots(20칸) 세팅 안 됨!");
            return;
        }

        // deckPanelManager 쪽에서 이미 sharedSlotData20을 채워줬다고 가정
        CharacterData[] sharedData = characterInventory.sharedSlotData20; // 길이 20

        int slotCount = upgradeSlots.Count; // 20

        // UI 싱크
        for (int i = 0; i < slotCount; i++)
        {
            SetSlotInfo(upgradeSlots[i], sharedData[i]);
        }

        selectedCharacters.Clear();
    }

    /// <summary>
    /// 덱 패널에서 세트2 등록 상태만 복사
    /// </summary>
    public void SetUpgradeRegisteredSlotsFromDeck()
    {
        if (deckPanelManager == null)
        {
            Debug.LogWarning("[UpgradePanelManager] deckPanelManager가 null");
            return;
        }

        // deckPanelManager가 가진 registeredCharactersSet2를 복사
        var deckSet2 = deckPanelManager.registeredCharactersSet2;
        for (int i = 0; i < 8; i++)
        {
            registeredSet2_Up[i] = deckSet2[i];
        }

        // UI에 반영
        for (int i = 0; i < 8; i++)
        {
            UpdateUpgradeRegisteredImage(i);
        }
    }

    private void UpdateUpgradeRegisteredImage(int i)
    {
        if (upgradeRegisteredSlotImages == null || i < 0 || i >= upgradeRegisteredSlotImages.Count) return;
        Image slotImg = upgradeRegisteredSlotImages[i];
        CharacterData cData = registeredSet2_Up[i];

        if (slotImg == null) return;

        if (cData == null)
        {
            slotImg.gameObject.SetActive(false);
        }
        else
        {
            slotImg.gameObject.SetActive(true);
            slotImg.sprite = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : null;
        }
    }

    // =========================================
    //  (20칸) 슬롯 하나에 data 세팅
    // =========================================
    private void SetSlotInfo(GameObject slotObj, CharacterData data)
    {
        if (slotObj == null) return;

        Image cardImg = slotObj.transform.Find("CardImage")?.GetComponent<Image>();
        TextMeshProUGUI cardText = slotObj.transform.Find("CardText")?.GetComponent<TextMeshProUGUI>();
        Toggle toggle = slotObj.transform.Find("SelectToggle")?.GetComponent<Toggle>();
        Button btn = slotObj.GetComponent<Button>();

        if (data == null)
        {
            // 빈 슬롯
            if (cardImg)  cardImg.sprite = emptySlotSprite;
            if (cardText) cardText.text  = emptySlotText;
            if (toggle)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = false;
            }
            if (btn) btn.onClick.RemoveAllListeners();
        }
        else
        {
            // 실제 캐릭터
            if (cardImg)   cardImg.sprite = (data.buttonIcon != null) ? data.buttonIcon.sprite : null;
            if (cardText)  cardText.text  = $"{data.characterName} (Lv={data.level}, Exp={data.currentExp})";

            if (toggle)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = false;
                toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn) SelectCharacter(data);
                    else DeselectCharacter(data);
                });
            }
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnClickCharacterSlot(data));
            }
        }
    }

    /// <summary>
    /// (20칸) 슬롯 클릭
    /// </summary>
    private void OnClickCharacterSlot(CharacterData data)
    {
        Debug.Log($"[UpgradePanelManager] 20칸 슬롯 클릭 => {data.characterName}");
        // 필요하다면 추가 로직
    }

    private void SelectCharacter(CharacterData c)
    {
        if (!selectedCharacters.Contains(c))
        {
            selectedCharacters.Add(c);
        }
    }

    private void DeselectCharacter(CharacterData c)
    {
        if (selectedCharacters.Contains(c))
        {
            selectedCharacters.Remove(c);
        }
    }

    private void SetupUpgradeButtons()
    {
        if (upgradeButtons == null || upgradeButtons.Count < 8)
        {
            Debug.LogWarning("[UpgradePanelManager] upgradeButtons 설정이 부족합니다.");
            return;
        }

        for (int i = 0; i < upgradeButtons.Count; i++)
        {
            int btnIndex = i;
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => OnClickUpgradeButton(btnIndex));
        }
    }

    private void OnClickUpgradeButton(int index)
    {
        if (selectedCharacters.Count == 0)
        {
            Debug.LogWarning($"[UpgradePanel] 선택된 캐릭터 없음 (버튼 {index})");
            return;
        }

        // 첫 번째를 베이스로, 나머지는 재료
        CharacterData baseChar = selectedCharacters[0];
        int feedCount = selectedCharacters.Count - 1;

        if (feedCount > 0)
        {
            List<CharacterData> feedList = new List<CharacterData>(selectedCharacters);
            feedList.RemoveAt(0); // 베이스는 제외
            characterInventory.ConsumeCharactersForUpgrade(feedList);
        }

        // 베이스 캐릭터 경험치 증가
        baseChar.currentExp += feedCount;
        baseChar.CheckLevelUp();

        Debug.Log($"[UpgradePanelManager] {index}번 업그레이드 => 베이스={baseChar.characterName}, 재료={feedCount}장 => Exp={baseChar.currentExp}");

        // 다시 갱신(업그레이드 패널 + 등록슬롯 + 덱 패널도 동기화)
        RefreshDisplay();
        SetUpgradeRegisteredSlotsFromDeck();

        selectedCharacters.Clear();

        // 덱 패널에도 반영(혹시 필요하다면)
        DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        if (dpm != null)
        {
            dpm.RefreshDeckDisplay(); // 다시 sharedSlotData20 반영
        }
    }
}
