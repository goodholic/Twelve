using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 덱 패널: 중복 제거(유니크)된 캐릭터 목록을 표시.
/// 12칸(4×3)에 순서대로 배치.
/// + 등록 기능(8개 등록 버튼/슬롯)이 추가됨.
/// </summary>
public class DeckPanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager (인벤토리)")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("덱 패널용 12슬롯 (Button 등)")]
    [SerializeField] private List<GameObject> deckSlots;

    [Header("빈 슬롯용 스프라이트 (선택)")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("빈 슬롯 시 표시할 텍스트 (선택)")]
    [SerializeField] private string emptySlotText = "";

    // ================================================
    // [변경] 등록 관련 UI(8개 버튼 + 8개 이미지 슬롯)
    // ================================================
    [Header("등록 관련 UI (총 8개)")]
    [Tooltip("등록 버튼(8개). i번째 버튼이 클릭되면, 선택된 캐릭터를 i번째 등록 슬롯에 등록.")]
    [SerializeField] private List<Button> registerButtons; // 8개

    [Tooltip("등록 슬롯에 표시할 이미지(8개). 등록 성공 시 해당 슬롯에 캐릭터 아이콘 표시")]
    [SerializeField] private List<Image> registerSlotImages; // 8개

    // 선택된 캐릭터(슬롯 클릭 시 저장)
    private CharacterData selectedCharacterForRegistration = null;

    // 실제 등록이 완료된 캐릭터 목록(슬롯별)
    private CharacterData[] registeredCharacters = new CharacterData[8];

    private void OnEnable()
    {
        RefreshDeckDisplay();
        SetupRegisterButtons();
        InitRegisterSlotsVisual();
    }

    /// <summary>
    /// 유니크 캐릭터 목록을 12칸에 표시
    /// </summary>
    public void RefreshDeckDisplay()
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[DeckPanelManager] characterInventory가 연결되지 않음!");
            return;
        }
        if (deckSlots == null || deckSlots.Count == 0)
        {
            Debug.LogWarning("[DeckPanelManager] deckSlots가 세팅되지 않음!");
            return;
        }

        List<CharacterData> uniqueCharacters = characterInventory.GetUniqueCharacterList();

        int slotCount = deckSlots.Count;
        int charCount = uniqueCharacters.Count;
        int displayCount = Mathf.Min(slotCount, charCount);

        // 모든 슬롯을 '빈 슬롯'으로 초기화
        for (int i = 0; i < slotCount; i++)
        {
            SetSlotInfo(deckSlots[i], null);
        }

        // 실제 캐릭터 수만큼 슬롯 채우기
        for (int i = 0; i < displayCount; i++)
        {
            CharacterData cData = uniqueCharacters[i];
            SetSlotInfo(deckSlots[i], cData);
        }
    }

    /// <summary>
    /// 특정 슬롯에 CharacterData를 배치(이미지/텍스트)
    /// data == null이면 "빈 슬롯"
    /// </summary>
    private void SetSlotInfo(GameObject slotObj, CharacterData data)
    {
        if (slotObj == null)
        {
            Debug.LogWarning("[DeckPanelManager] 슬롯 오브젝트가 null입니다.");
            return;
        }

        Image cardImg = slotObj.transform.Find("CardImage")?.GetComponent<Image>();
        TextMeshProUGUI cardText = slotObj.transform.Find("CardText")?.GetComponent<TextMeshProUGUI>();
        Button button = slotObj.GetComponent<Button>();

        if (data == null)
        {
            // 빈 슬롯
            if (cardImg) cardImg.sprite = emptySlotSprite;
            if (cardText) cardText.text = emptySlotText;

            if (button) button.onClick.RemoveAllListeners();
        }
        else
        {
            // 실제 캐릭터
            if (cardImg)
            {
                cardImg.sprite = (data.buttonIcon != null) ? data.buttonIcon.sprite : null;
            }
            if (cardText)
            {
                cardText.text = $"{data.characterName} (Cost={data.cost}, Exp={data.currentExp})";
            }

            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClickDeckCharacter(data));
            }
        }
    }

    /// <summary>
    /// 덱 캐릭터 슬롯 클릭 시 상세 표시 및 "등록 후보" 지정
    /// </summary>
    private void OnClickDeckCharacter(CharacterData data)
    {
        selectedCharacterForRegistration = data;
        Debug.Log($"[DeckPanelManager] 덱 캐릭터 클릭: {data.characterName}");
    }

    // ==========================================
    //  8개 등록 버튼
    // ==========================================
    private void SetupRegisterButtons()
    {
        if (registerButtons == null || registerButtons.Count < 8)
        {
            Debug.LogWarning("[DeckPanelManager] registerButtons 설정이 부족합니다.");
            return;
        }
        // 각 버튼에 i값을 넘기는 방식
        for (int i = 0; i < registerButtons.Count; i++)
        {
            int slotIndex = i;
            registerButtons[i].onClick.RemoveAllListeners();
            registerButtons[i].onClick.AddListener(() => OnClickRegisterCharacter(slotIndex));
        }
    }

    /// <summary>
    /// 각 등록 슬롯 이미지를 초기화
    /// </summary>
    private void InitRegisterSlotsVisual()
    {
        // 이미 등록된 캐릭터가 있을 수도 있으니 다시 표시
        for (int i = 0; i < 8; i++)
        {
            UpdateRegisterSlotImage(i);
        }
    }

    /// <summary>
    /// 등록 버튼 클릭 (i번 슬롯에 등록)
    /// </summary>
    private void OnClickRegisterCharacter(int slotIndex)
    {
        if (selectedCharacterForRegistration == null)
        {
            Debug.LogWarning($"[DeckPanelManager] 등록할 캐릭터가 선택되지 않음! (슬롯 {slotIndex})");
            return;
        }

        // 1) 인벤토리에서 제거
        characterInventory.ConsumeCharactersForUpgrade(new List<CharacterData> { selectedCharacterForRegistration });
        Debug.Log($"[DeckPanelManager] [{selectedCharacterForRegistration.characterName}] 등록 완료 -> 인벤토리에서 제거됨");

        // 2) 해당 슬롯에 등록
        registeredCharacters[slotIndex] = selectedCharacterForRegistration;

        // 3) 등록 슬롯 이미지를 갱신
        UpdateRegisterSlotImage(slotIndex);

        // 4) 덱 목록 갱신
        RefreshDeckDisplay();

        // 5) 선택 해제
        selectedCharacterForRegistration = null;
    }

    /// <summary>
    /// i번 등록 슬롯의 Image UI 갱신
    /// </summary>
    private void UpdateRegisterSlotImage(int i)
    {
        if (registerSlotImages == null || i < 0 || i >= registerSlotImages.Count) return;

        Image slotImg = registerSlotImages[i];
        CharacterData cData = registeredCharacters[i];

        if (cData == null)
        {
            // 빈 등록 슬롯
            slotImg.gameObject.SetActive(false);
        }
        else
        {
            // 등록된 캐릭터 아이콘 표시
            slotImg.sprite = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : null;
            slotImg.gameObject.SetActive(true);
        }
    }
}
