// ================================================
//  Assets\OX UI Scripts\DeckPanelManager.cs
//    - 12슬롯 제거, 20슬롯 통합
//    - registeredCharactersSet2만 남김 (set1 제거)
//    - sharedSlotData20에 덱 데이터 동기화
// ================================================

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
    [Header("등록 버튼(8개) + 이미지 슬롯(8개) - (세트2만)")]
    [SerializeField] private List<Button> registerButtons;      // set2 버튼
    [SerializeField] private List<Image> registerSlotImages;    // set2 이미지

    // ============================================
    // 실제 등록 완료 캐릭터 (세트2만)
    // ============================================
    public CharacterData[] registeredCharactersSet2 = new CharacterData[8];

    // ============================================
    // 선택된 캐릭터(등록 대상)
    // ============================================
    private CharacterData selectedCharacterForRegistration = null;

    private void OnEnable()
    {
        // 패널 켜질 때마다 인벤토리(20칸) 새로고침
        RefreshDeckDisplay();

        // 등록 버튼(8개) 초기화
        SetupRegisterButtons();

        // 등록 슬롯(8개) 시각 갱신
        InitRegisterSlotsVisual();
    }

    // ============================================
    //  (20칸) 인벤토리 '유니크 목록' 표시
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

        // 인벤토리(덱 제외)만 중복제거
        List<CharacterData> uniqueCharacters = GetUniqueInventoryList();

        int slotCount = deckSlots.Count;    // 20
        int charCount = uniqueCharacters.Count;
        int displayCount = Mathf.Min(slotCount, charCount);

        // =============== [중요] sharedSlotData20 업데이트 ================
        //   0~displayCount-1 까지는 실제 CharacterData,
        //   나머지는 null
        // ===============================================================
        for (int i = 0; i < slotCount; i++)
        {
            if (i < displayCount)
                characterInventory.sharedSlotData20[i] = uniqueCharacters[i];
            else
                characterInventory.sharedSlotData20[i] = null;
        }

        // 모든 슬롯 '빈 슬롯'으로 초기화 후, 실제 데이터 채우기
        for (int i = 0; i < slotCount; i++)
        {
            SetSlotInfo(deckSlots[i], characterInventory.sharedSlotData20[i]);
        }
    }

    /// <summary>
    /// 인벤토리(덱 제외)만 중복제거하여 반환
    /// </summary>
    private List<CharacterData> GetUniqueInventoryList()
    {
        List<CharacterData> list = new List<CharacterData>();
        Dictionary<string, bool> nameCheck = new Dictionary<string, bool>();

        foreach (var c in characterInventory.GetOwnedCharacters())
        {
            if (c == null) continue;
            if (!nameCheck.ContainsKey(c.characterName))
            {
                nameCheck[c.characterName] = true;
                list.Add(c);
            }
        }
        return list;
    }

    /// <summary>
    /// (20칸) 슬롯에 CharacterData 배치
    /// </summary>
    private void SetSlotInfo(GameObject slotObj, CharacterData data)
    {
        if (slotObj == null) return;

        Image cardImg = slotObj.transform.Find("CardImage")?.GetComponent<Image>();
        TextMeshProUGUI cardText = slotObj.transform.Find("CardText")?.GetComponent<TextMeshProUGUI>();
        Button btn = slotObj.GetComponent<Button>();

        if (data == null)
        {
            // 빈 슬롯
            if (cardImg)  cardImg.sprite = emptySlotSprite;
            if (cardText) cardText.text  = emptySlotText;
            if (btn)      btn.onClick.RemoveAllListeners();
        }
        else
        {
            // 실제 캐릭터
            if (cardImg)  cardImg.sprite = (data.buttonIcon != null) ? data.buttonIcon.sprite : null;
            if (cardText) cardText.text  = $"{data.characterName} (Lv={data.level}, Exp={data.currentExp})";

            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnClickInventoryCharacter(data));
            }
        }
    }

    /// <summary>
    /// 20칸 슬롯 클릭 -> 등록 후보 지정
    /// </summary>
    private void OnClickInventoryCharacter(CharacterData data)
    {
        selectedCharacterForRegistration = data;
        Debug.Log($"[DeckPanelManager] 20칸 인벤토리 슬롯 클릭 => {data.characterName}");
    }

    // ============================================
    //  등록 버튼(8개) - 세트2만 남김
    // ============================================
    private void SetupRegisterButtons()
    {
        if (registerButtons == null || registerButtons.Count < 8)
        {
            Debug.LogWarning("[DeckPanelManager] registerButtons(세트2) 설정이 부족합니다.");
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
        for (int i = 0; i < 8; i++)
        {
            UpdateRegisterSlotImage(i);
        }
    }

    private void OnClickRegisterCharacterSet2(int slotIndex)
    {
        if (selectedCharacterForRegistration == null)
        {
            Debug.LogWarning($"[DeckPanelManager] 등록할 캐릭터가 선택되지 않음! (슬롯 {slotIndex})");
            return;
        }

        // 이미 등록된 캐릭터인지 확인
        string newName = selectedCharacterForRegistration.characterName;
        if (IsAlreadyRegistered(newName))
        {
            Debug.LogWarning($"[DeckPanelManager] 이미 등록된 캐릭터({newName})입니다!");
            return;
        }

        // 인벤토리 -> 덱 이동
        characterInventory.MoveToDeck(selectedCharacterForRegistration);
        Debug.Log($"[DeckPanelManager] [{selectedCharacterForRegistration.characterName}] 등록 완료 (세트2)");

        // 해당 슬롯에 등록
        registeredCharactersSet2[slotIndex] = selectedCharacterForRegistration;
        UpdateRegisterSlotImage(slotIndex);

        // 인벤토리 표시 재갱신 (20칸) => sharedSlotData20도 업데이트됨
        RefreshDeckDisplay();

        // 선택 해제
        selectedCharacterForRegistration = null;

        // 즉시 저장
        characterInventory.SaveCharacters();

        // 업그레이드 패널 갱신
        UpgradePanelManager upm = FindFirstObjectByType<UpgradePanelManager>();
        if (upm != null)
        {
            upm.RefreshDisplay(); // 20칸 업그레이드 패널 표시 갱신
            upm.SetUpgradeRegisteredSlotsFromDeck(); 
        }
    }

    private void UpdateRegisterSlotImage(int i)
    {
        if (registerSlotImages == null || i < 0 || i >= registerSlotImages.Count) return;

        Image slotImg = registerSlotImages[i];
        CharacterData cData = registeredCharactersSet2[i];

        if (slotImg == null) return;

        if (cData == null)
        {
            // 등록 안 된 상태
            slotImg.gameObject.SetActive(false);
        }
        else
        {
            slotImg.gameObject.SetActive(true);
            slotImg.sprite = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : null;
        }
    }

    /// <summary>
    /// 이미 등록된(세트2) 캐릭터인지 검사
    /// </summary>
    private bool IsAlreadyRegistered(string charName)
    {
        for (int i = 0; i < registeredCharactersSet2.Length; i++)
        {
            var c = registeredCharactersSet2[i];
            if (c != null && c.characterName == charName)
            {
                return true;
            }
        }
        return false;
    }
}
