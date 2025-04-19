using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DeckPanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager (인벤토리)")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("빈 슬롯용 스프라이트")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("빈 슬롯 시 표시할 텍스트")]
    [SerializeField] private string emptySlotText = "";

    // ============================================
    //  "20칸 인벤토리" 표시용 슬롯 (덱 패널)
    // ============================================
    [Header("덱 패널용 20칸 인벤토리 슬롯")]
    [SerializeField] private List<GameObject> deckSlots;  // (기존 deckSlots20 → deckSlots로 변경)

    // ============================================
    //  등록 버튼 & 슬롯(이미지) - 세트2만 유지
    // ============================================
    [Header("등록 버튼(10개) + 이미지 슬롯(10개) - (세트2만)")]
    [SerializeField] private List<Button> registerButtons;      // set2 버튼
    [SerializeField] private List<Image> registerSlotImages;    // set2 이미지

    [Header("등록 슬롯(10칸) 레벨 텍스트")]
    [SerializeField] private List<TextMeshProUGUI> registerSlotLevelTexts;  // 레벨 표시용 텍스트(10칸)

    // ============================================
    // 실제 등록 완료 캐릭터 (세트2만) - 10칸
    // ============================================
    public CharacterData[] registeredCharactersSet2 = new CharacterData[10];

    // ============================================
    // 20칸 슬롯 중 "선택된" 캐릭터 및 그 슬롯 인덱스
    // ============================================
    private CharacterData selectedCharacterForRegistration = null;
    private int selectedInventorySlotIndex = -1; // 20칸 중 몇 번째였는지

    private void OnEnable()
    {
        // 1) 패널 켜질 때마다 인벤토리(20칸) 새로고침
        RefreshDeckDisplay();

        // 2) 등록 버튼 초기화 (10개)
        SetupRegisterButtons();

        // 3) 등록 슬롯(10개) 시각 갱신
        InitRegisterSlotsVisual();
    }

    // ============================================
    //  (20칸) 인벤토리 목록 표시
    // ============================================
    public void RefreshDeckDisplay()
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[DeckPanelManager] characterInventory가 연결되지 않음!");
            return;
        }
        if (deckSlots == null || deckSlots.Count < 20)
        {
            Debug.LogWarning("[DeckPanelManager] deckSlots(20칸) 세팅이 안 됨!");
            return;
        }

        // 인벤토리(덱 제외)에서 가져온 목록
        List<CharacterData> inventoryList = characterInventory.GetOwnedCharacters();

        // -----------------------------------------------------
        // "클릭 불가능한 빈칸(null)"은 순서에서 제외하기 위해,
        // 실제로 유효한 캐릭터만 골라내서 listFiltered에 담음
        // -----------------------------------------------------
        List<CharacterData> listFiltered = new List<CharacterData>();
        foreach (var c in inventoryList)
        {
            if (c != null)
            {
                // DB 상에서도 존재해야 실제로 사용 가능한 캐릭터
                var foundTemplate = characterInventory.FindTemplateByName(c.characterName);
                if (foundTemplate != null)
                {
                    listFiltered.Add(c);
                }
                // 만약 DB에 없는 캐릭터면 굳이 표시할 필요 없음
            }
        }

        int slotCount = deckSlots.Count; // 20
        int validCount = listFiltered.Count;
        int displayCount = Mathf.Min(slotCount, validCount);

        // ----------------------------------------------
        // 0번 슬롯부터 차곡차곡 넣고, 남는 칸은 null 처리
        // ----------------------------------------------
        for (int i = 0; i < slotCount; i++)
        {
            if (i < displayCount)
            {
                characterInventory.sharedSlotData20[i] = listFiltered[i];
            }
            else
            {
                characterInventory.sharedSlotData20[i] = null;
            }
        }

        // 슬롯 UI 갱신
        for (int i = 0; i < slotCount; i++)
        {
            SetSlotInfo(deckSlots[i], characterInventory.sharedSlotData20[i], i);
        }
    }

    /// <summary>
    /// 20칸 슬롯 UI 설정
    /// (data == null이거나 DB에 존재하지 않는 캐릭터이면 -> 빈 슬롯 처리)
    /// </summary>
    private void SetSlotInfo(GameObject slotObj, CharacterData data, int slotIndex)
    {
        if (slotObj == null) return;
        if (characterInventory == null) return;

        // 혹시 DB에 없는 캐릭터라면 null 처리(안전장치)
        if (data != null)
        {
            var foundTemplate = characterInventory.FindTemplateByName(data.characterName);
            if (foundTemplate == null)
            {
                data = null;
            }
        }

        Image cardImg = slotObj.transform.Find("CardImage")?.GetComponent<Image>();
        TextMeshProUGUI cardText = slotObj.transform.Find("CardText")?.GetComponent<TextMeshProUGUI>();
        Button btn = slotObj.GetComponent<Button>();

        if (data == null)
        {
            // 빈 슬롯
            if (cardImg)  cardImg.sprite = emptySlotSprite;
            if (cardText) cardText.text  = emptySlotText;
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.interactable = false; // 클릭 불가
            }
        }
        else
        {
            // 실제 캐릭터 표시
            if (cardImg)
                cardImg.sprite = (data.buttonIcon != null) ? data.buttonIcon.sprite : emptySlotSprite;

            if (cardText)
                cardText.text = $"{data.characterName} (Lv={data.level}, Exp={data.currentExp})";

            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnClickInventoryCharacter(data, slotIndex));
                btn.interactable = true; // 클릭 가능
            }
        }
    }

    /// <summary>
    /// (20칸) 슬롯 클릭 -> 등록 후보 지정
    /// </summary>
    private void OnClickInventoryCharacter(CharacterData data, int inventorySlotIndex)
    {
        selectedCharacterForRegistration = data;
        selectedInventorySlotIndex = inventorySlotIndex;
        Debug.Log($"[DeckPanelManager] 20칸 {inventorySlotIndex}번 슬롯 클릭 => {data.characterName}");
    }

    // ============================================
    //  등록 버튼(10개) - 세트2만
    // ============================================
    private void SetupRegisterButtons()
    {
        if (registerButtons == null || registerButtons.Count < 10)
        {
            Debug.LogWarning("[DeckPanelManager] registerButtons(세트2) 설정이 부족합니다. (10개 필요)");
            return;
        }

        for (int i = 0; i < registerButtons.Count; i++)
        {
            if (registerButtons[i] != null)
            {
                registerButtons[i].onClick.RemoveAllListeners();
                registerButtons[i].gameObject.SetActive(true);

                int slotIndex = i;
                registerButtons[i].onClick.AddListener(() => OnClickRegisterCharacterSet2(slotIndex));
            }
        }
    }

    private void InitRegisterSlotsVisual()
    {
        for (int i = 0; i < 10; i++)
        {
            UpdateRegisterSlotVisual(i);
        }
    }

    /// <summary>
    /// 덱 패널의 등록 슬롯(i번째) UI 갱신
    /// </summary>
    private void UpdateRegisterSlotVisual(int i)
    {
        if (registerSlotImages == null || i < 0 || i >= registerSlotImages.Count) return;

        Image slotImg = registerSlotImages[i];
        TextMeshProUGUI lvlText = null;
        if (registerSlotLevelTexts != null && i < registerSlotLevelTexts.Count)
        {
            lvlText = registerSlotLevelTexts[i];
        }

        CharacterData cData = registeredCharactersSet2[i];
        if (slotImg == null) return;

        if (cData == null)
        {
            slotImg.gameObject.SetActive(false);
            if (lvlText != null) lvlText.gameObject.SetActive(false);
        }
        else
        {
            slotImg.gameObject.SetActive(true);
            slotImg.sprite = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : null;

            if (lvlText != null)
            {
                lvlText.gameObject.SetActive(true);
                lvlText.text = $"Lv.{cData.level}";
            }
        }
    }

    /// <summary>
    /// 세트2 등록 버튼 클릭 -> 캐릭터 등록/교체
    /// </summary>
    private void OnClickRegisterCharacterSet2(int slotIndex)
    {
        if (selectedCharacterForRegistration == null)
        {
            Debug.LogWarning($"[DeckPanelManager] 등록할 캐릭터가 선택되지 않음! (슬롯 {slotIndex})");
            return;
        }
        if (selectedInventorySlotIndex < 0
            || characterInventory.sharedSlotData20[selectedInventorySlotIndex] == null)
        {
            Debug.LogWarning("[DeckPanelManager] 인벤토리에서 '빈칸' 선택 or 인덱스 오류!");
            return;
        }

        // 덱 슬롯 현재 캐릭터
        CharacterData existingChar = registeredCharactersSet2[slotIndex];

        if (existingChar == null)
        {
            // 빈 슬롯에 새로 등록
            Debug.Log($"[DeckPanelManager] 빈 슬롯({slotIndex})에 등록 => {selectedCharacterForRegistration.characterName}");

            // 인벤토리->덱 이동
            characterInventory.sharedSlotData20[selectedInventorySlotIndex] = null;
            characterInventory.RemoveFromInventory(selectedCharacterForRegistration);
            characterInventory.MoveToDeck(selectedCharacterForRegistration);

            registeredCharactersSet2[slotIndex] = selectedCharacterForRegistration;
        }
        else
        {
            // 이미 덱에 있는 캐릭터 교체
            Debug.Log($"[DeckPanelManager] 교체 => 기존 {existingChar.characterName} / 새 {selectedCharacterForRegistration.characterName}");

            characterInventory.sharedSlotData20[selectedInventorySlotIndex] = existingChar;
            characterInventory.RemoveFromDeck(existingChar);
            characterInventory.AddToInventory(existingChar);

            characterInventory.RemoveFromInventory(selectedCharacterForRegistration);
            characterInventory.MoveToDeck(selectedCharacterForRegistration);

            registeredCharactersSet2[slotIndex] = selectedCharacterForRegistration;
        }

        // 등록 슬롯 시각 갱신
        UpdateRegisterSlotVisual(slotIndex);

        // 20칸 새로고침(0번부터 차곡차곡 채우기 + 클릭 불가 제외)
        RefreshDeckDisplay();

        // 선택 해제
        selectedCharacterForRegistration = null;
        selectedInventorySlotIndex = -1;

        // 즉시 저장
        characterInventory.SaveCharacters();

        // 업그레이드 패널 갱신
        UpgradePanelManager upm = FindFirstObjectByType<UpgradePanelManager>();
        if (upm != null)
        {
            upm.RefreshDisplay();
            upm.SetUpgradeRegisteredSlotsFromDeck();
        }
    }
}
