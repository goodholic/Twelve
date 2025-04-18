using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 덱 패널: 
///  - 인벤토리 캐릭터(중복제외)를 12칸(4×3)에 표시
///  - 등록 기능: 버튼 총 16개(기존 8개 + 추가 8개)
///    각각 등록 슬롯 이미지도 16개
/// </summary>
public class DeckPanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager (인벤토리)")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("덱 패널용 12슬롯 (Button 등) - 인벤토리 표시")]
    [SerializeField] private List<GameObject> deckSlots;

    [Header("빈 슬롯용 스프라이트 (선택)")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("빈 슬롯 시 표시할 텍스트 (선택)")]
    [SerializeField] private string emptySlotText = "";

    // ================================================
    // 1) 기존 8개 등록 버튼 + 이미지 슬롯
    // ================================================
    [Header("[세트1] 등록 관련 UI (총 8개)")]
    [Tooltip("등록 버튼(8개) - 첫 세트")]
    [SerializeField] private List<Button> registerButtons; // 기존 8개

    [Tooltip("등록 슬롯에 표시할 이미지(8개) - 첫 세트")]
    [SerializeField] private List<Image> registerSlotImages; // 기존 8개

    // ================================================
    // 2) 추가된 '또 다른 8개' 버튼 + 이미지 슬롯
    // ================================================
    [Header("[세트2] 등록 관련 UI (추가 8개)")]
    [Tooltip("등록 버튼(8개) - 두 번째 세트")]
    [SerializeField] private List<Button> registerButtons2; // 새로 추가된 8개

    [Tooltip("등록 슬롯에 표시할 이미지(8개) - 두 번째 세트")]
    [SerializeField] private List<Image> registerSlotImages2; // 새로 추가된 8개

    // ==========================
    // 선택된 캐릭터(등록 대상)
    // ==========================
    private CharacterData selectedCharacterForRegistration = null;

    // ==========================
    // 실제 등록 완료 캐릭터 저장 (세트1, 세트2)
    // ==========================
    // "업그레이드 패널"에서도 동일하게 보고 싶다면 public으로 공개하거나 getter를 제공해도 됨.
    public CharacterData[] registeredCharactersSet1 = new CharacterData[8];
    public CharacterData[] registeredCharactersSet2 = new CharacterData[8];

    private void OnEnable()
    {
        // 패널 켜질 때마다 새로고침
        RefreshDeckDisplay();

        // 버튼들 초기화(세트1, 세트2 모두)
        SetupRegisterButtons();
        SetupRegisterButtons2();

        // 등록 슬롯(두 세트) 시각 갱신
        InitRegisterSlotsVisual();
        InitRegisterSlotsVisual2();
    }

    /// <summary>
    /// 인벤토리 '유니크 목록' (덱 제외)을 12칸에 표시
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

        List<CharacterData> uniqueCharacters = GetUniqueInventoryList();

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
    /// 인벤토리(ownedCharacters)만 유니크 추려서 반환
    /// (deckCharacters는 제외)
    /// </summary>
    private List<CharacterData> GetUniqueInventoryList()
    {
        // "ownedCharacters" 내 중복제거
        List<CharacterData> list = new List<CharacterData>();
        Dictionary<string, bool> nameCheck = new Dictionary<string, bool>();

        // "GetOwnedCharacters()" = 인벤토리 전용 (덱 제외)
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
            if (cardImg)  cardImg.sprite = emptySlotSprite;
            if (cardText) cardText.text  = emptySlotText;
            if (button)   button.onClick.RemoveAllListeners();
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
                cardText.text = $"{data.characterName} (Lv={data.level}, Exp={data.currentExp})";
            }

            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClickDeckCharacter(data));
            }
        }
    }

    /// <summary>
    /// 덱 캐릭터 슬롯 클릭 시 => 등록 후보 지정
    /// </summary>
    private void OnClickDeckCharacter(CharacterData data)
    {
        selectedCharacterForRegistration = data;
        Debug.Log($"[DeckPanelManager] 슬롯 클릭 => 등록 후보: {data.characterName} (Lv={data.level})");
    }

    // ==========================================
    //   세트1: 8개 등록 버튼
    // ==========================================
    private void SetupRegisterButtons()
    {
        if (registerButtons == null || registerButtons.Count < 8)
        {
            Debug.LogWarning("[DeckPanelManager] registerButtons(세트1) 설정이 부족합니다.");
            return;
        }

        for (int i = 0; i < registerButtons.Count; i++)
        {
            if (registerButtons[i] != null)
            {
                registerButtons[i].gameObject.SetActive(true);
                registerButtons[i].onClick.RemoveAllListeners();

                int slotIndex = i;
                registerButtons[i].onClick.AddListener(() => OnClickRegisterCharacterSet1(slotIndex));
            }
        }
    }

    private void InitRegisterSlotsVisual()
    {
        for (int i = 0; i < 8; i++)
        {
            UpdateRegisterSlotImageSet1(i);
        }
    }

    /// <summary>
    /// 세트1: i번 버튼 클릭
    /// </summary>
    private void OnClickRegisterCharacterSet1(int slotIndex)
    {
        if (selectedCharacterForRegistration == null)
        {
            Debug.LogWarning($"[DeckPanelManager] (세트1) 등록할 캐릭터가 선택되지 않음! (슬롯 {slotIndex})");
            return;
        }

        // 1) 이미 등록된 캐릭터(이름)이 있는지 검사
        string newName = selectedCharacterForRegistration.characterName;
        if (IsAlreadyRegistered(newName))
        {
            Debug.LogWarning($"[DeckPanelManager] 이미 등록된 캐릭터({newName})입니다!");
            return;
        }

        // 2) 인벤토리 -> 덱 이동 (owned → deck)
        characterInventory.MoveToDeck(selectedCharacterForRegistration);

        Debug.Log($"[DeckPanelManager] (세트1) [{selectedCharacterForRegistration.characterName}] 등록 완료 -> 인벤토리에서 덱으로 이동");

        // 3) 해당 슬롯에 등록
        registeredCharactersSet1[slotIndex] = selectedCharacterForRegistration;
        UpdateRegisterSlotImageSet1(slotIndex);

        // 4) 인벤토리 표시 재갱신
        RefreshDeckDisplay();

        // 5) 선택 해제
        selectedCharacterForRegistration = null;

        // 6) 필요시 즉시 저장
        characterInventory.SaveCharacters();
    }

    /// <summary>
    /// 세트1: 등록 슬롯 i번 이미지 갱신
    /// </summary>
    private void UpdateRegisterSlotImageSet1(int i)
    {
        if (registerSlotImages == null || i < 0 || i >= registerSlotImages.Count) return;

        Image slotImg = registerSlotImages[i];
        CharacterData cData = registeredCharactersSet1[i];

        if (slotImg == null) return;

        if (cData == null)
        {
            slotImg.gameObject.SetActive(false);
        }
        else
        {
            slotImg.sprite = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : null;
            slotImg.gameObject.SetActive(true);
        }
    }

    // ==========================================
    //   세트2: 추가된 8개 등록 버튼
    // ==========================================
    private void SetupRegisterButtons2()
    {
        if (registerButtons2 == null || registerButtons2.Count < 8)
        {
            Debug.LogWarning("[DeckPanelManager] registerButtons2(세트2) 설정이 부족합니다.");
            return;
        }

        for (int i = 0; i < registerButtons2.Count; i++)
        {
            if (registerButtons2[i] != null)
            {
                registerButtons2[i].gameObject.SetActive(true);
                registerButtons2[i].onClick.RemoveAllListeners();

                int slotIndex = i;
                registerButtons2[i].onClick.AddListener(() => OnClickRegisterCharacterSet2(slotIndex));
            }
        }
    }

    private void InitRegisterSlotsVisual2()
    {
        for (int i = 0; i < 8; i++)
        {
            UpdateRegisterSlotImageSet2(i);
        }
    }

    /// <summary>
    /// 세트2: i번 버튼 클릭
    /// </summary>
    private void OnClickRegisterCharacterSet2(int slotIndex)
    {
        if (selectedCharacterForRegistration == null)
        {
            Debug.LogWarning($"[DeckPanelManager] (세트2) 등록할 캐릭터가 선택되지 않음! (슬롯 {slotIndex})");
            return;
        }

        // 1) 이미 등록된 이름인지 검사
        string newName = selectedCharacterForRegistration.characterName;
        if (IsAlreadyRegistered(newName))
        {
            Debug.LogWarning($"[DeckPanelManager] 이미 등록된 캐릭터({newName})입니다!");
            return;
        }

        // 2) 인벤토리 -> 덱 이동
        characterInventory.MoveToDeck(selectedCharacterForRegistration);
        Debug.Log($"[DeckPanelManager] (세트2) [{selectedCharacterForRegistration.characterName}] 등록 완료 -> 인벤토리에서 덱으로 이동");

        // 3) 해당 슬롯에 등록
        registeredCharactersSet2[slotIndex] = selectedCharacterForRegistration;
        UpdateRegisterSlotImageSet2(slotIndex);

        // 4) 인벤토리 표시 갱신
        RefreshDeckDisplay();

        // 5) 선택 해제
        selectedCharacterForRegistration = null;

        // 6) 즉시 저장
        characterInventory.SaveCharacters();
    }

    /// <summary>
    /// 세트2: 등록 슬롯 i번 이미지 갱신
    /// </summary>
    private void UpdateRegisterSlotImageSet2(int i)
    {
        if (registerSlotImages2 == null || i < 0 || i >= registerSlotImages2.Count) return;

        Image slotImg = registerSlotImages2[i];
        CharacterData cData = registeredCharactersSet2[i];

        if (slotImg == null) return;

        if (cData == null)
        {
            slotImg.gameObject.SetActive(false);
        }
        else
        {
            slotImg.sprite = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : null;
            slotImg.gameObject.SetActive(true);
        }
    }

    // =============================================
    //  "같은 캐릭터 이름이 이미 등록되었는지" 검사
    // =============================================
    private bool IsAlreadyRegistered(string charName)
    {
        // 세트1 검사
        for (int i = 0; i < registeredCharactersSet1.Length; i++)
        {
            var c = registeredCharactersSet1[i];
            if (c != null && c.characterName == charName)
            {
                return true;
            }
        }

        // 세트2 검사
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

