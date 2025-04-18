using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 업그레이드 패널:
///  - 중복 포함 모든 캐릭터(인벤토리+덱)를 12칸에 표시(아래 upgradeSlots)
///  - 추가로, 덱에서 등록된 상태(세트1/세트2)도 똑같이 표시(새로 16칸).
///  - 선택(토글)한 캐릭터 중 첫 번째를 '베이스 캐릭터'로 보고,
///    나머지는 재료로 사용하여 인벤토리/덱에서 제거 + 베이스 EXP 증가.
/// </summary>
public class UpgradePanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("DeckPanelManager (등록상태를 가져오기 위해)")]
    [SerializeField] private DeckPanelManager deckPanelManager;

    [Header("업그레이드용 12슬롯 (인벤토리+덱 전체 표시)")]
    [SerializeField] private List<GameObject> upgradeSlots;  // 중복 포함 전체 캐릭터 12칸

    // =====================================
    // 덱의 등록 상태(세트1, 세트2)도 여기서 표시하기 위한 UI (각 8칸)
    // =====================================
    [Header("[업그레이드] 등록 상태 표시용 (세트1)")]
    [SerializeField] private List<Image> upgradeRegisteredSlotImages1; // 8개 (덱 패널 세트1과 동일)

    [Header("[업그레이드] 등록 상태 표시용 (세트2)")]
    [SerializeField] private List<Image> upgradeRegisteredSlotImages2; // 8개 (덱 패널 세트2와 동일)

    // 업그레이드 버튼 8개
    [Header("캐릭터 업그레이드 버튼 (총 8개)")]
    [SerializeField] private List<Button> upgradeButtons; // 8개

    [Header("빈 슬롯용 스프라이트 (선택)")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("빈 슬롯용 텍스트 (선택)")]
    [SerializeField] private string emptySlotText = "";

    // 선택된(토글된) 캐릭터 목록
    private List<CharacterData> selectedCharacters = new List<CharacterData>();

    // ====================================
    // "덱 등록 상태"를 업그레이드 패널이 
    // 그대로 들고있기 위한 배열(복사본)
    // ====================================
    private CharacterData[] registeredSet1_Up = new CharacterData[8];
    private CharacterData[] registeredSet2_Up = new CharacterData[8];

    private void OnEnable()
    {
        RefreshUpgradeDisplay();

        // "덱 등록 상태"도 복사해서 표시
        SetUpgradeRegisteredSlotsFromDeck();

        SetupUpgradeButtons();
    }

    /// <summary>
    /// 업그레이드용 전체 캐릭터 목록(인벤토리+덱 포함) 12칸에 표시
    /// </summary>
    public void RefreshUpgradeDisplay()
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[UpgradePanelManager] characterInventory가 없음");
            return;
        }
        // 중복 포함 전체 목록(소유 + 덱)
        List<CharacterData> allCharacters = characterInventory.GetAllCharactersWithDuplicates();

        int slotCount = upgradeSlots.Count;
        int charCount = allCharacters.Count;
        int displayCount = Mathf.Min(slotCount, charCount);

        // 먼저 모든 슬롯을 빈칸으로
        for (int i = 0; i < slotCount; i++)
        {
            SetSlotInfo(upgradeSlots[i], null);
        }

        // 실제 캐릭터 정보를 표시
        for (int i = 0; i < displayCount; i++)
        {
            CharacterData data = allCharacters[i];
            SetSlotInfo(upgradeSlots[i], data);
        }

        // 선택 초기화
        selectedCharacters.Clear();
    }

    /// <summary>
    /// 덱 패널(DeckPanelManager)의 등록 상태(2세트×8)를 가져와,
    /// 업그레이드 패널 쪽 UI에 그대로 표시
    /// </summary>
    private void SetUpgradeRegisteredSlotsFromDeck()
    {
        if (deckPanelManager == null)
        {
            Debug.LogWarning("[UpgradePanelManager] deckPanelManager가 null -> 등록 상태를 복사 못함");
            return;
        }

        // 1) DeckPanelManager의 세트1, 세트2 배열 받아옴
        var deckSet1 = deckPanelManager.registeredCharactersSet1; // 8칸
        var deckSet2 = deckPanelManager.registeredCharactersSet2; // 8칸

        // 2) 업그레이드 패널이 가진(복사용) 배열에도 저장
        for (int i = 0; i < 8; i++)
        {
            registeredSet1_Up[i] = deckSet1[i];
            registeredSet2_Up[i] = deckSet2[i];
        }

        // 3) 각 배열 내용을 실제 UI Image에 반영
        for (int i = 0; i < 8; i++)
        {
            UpdateUpgradeRegisteredImage1(i);
            UpdateUpgradeRegisteredImage2(i);
        }

        Debug.Log("[UpgradePanelManager] 덱 패널 등록 상태 -> 업그레이드 패널로 동기화 완료");
    }

    private void UpdateUpgradeRegisteredImage1(int i)
    {
        if (upgradeRegisteredSlotImages1 == null ||
            i < 0 || i >= upgradeRegisteredSlotImages1.Count)
            return;

        Image slotImg = upgradeRegisteredSlotImages1[i];
        CharacterData cData = registeredSet1_Up[i];
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

    private void UpdateUpgradeRegisteredImage2(int i)
    {
        if (upgradeRegisteredSlotImages2 == null ||
            i < 0 || i >= upgradeRegisteredSlotImages2.Count)
            return;

        Image slotImg = upgradeRegisteredSlotImages2[i];
        CharacterData cData = registeredSet2_Up[i];
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

    /// <summary>
    /// 하나의 슬롯에 data를 세팅 (12칸용)
    /// </summary>
    private void SetSlotInfo(GameObject slotObj, CharacterData data)
    {
        if (slotObj == null)
        {
            Debug.LogWarning("[UpgradePanelManager] 슬롯 오브젝트가 null입니다.");
            return;
        }

        Image cardImg = slotObj.transform.Find("CardImage")?.GetComponent<Image>();
        TextMeshProUGUI cardText = slotObj.transform.Find("CardText")?.GetComponent<TextMeshProUGUI>();
        Toggle toggle = slotObj.transform.Find("SelectToggle")?.GetComponent<Toggle>();
        Button button = slotObj.GetComponent<Button>();

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
                // 레벨 및 경험치 표시
                cardText.text = $"{data.characterName} (Lv={data.level}, Exp={data.currentExp})";
            }

            if (toggle)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = false; // 초기상태는 Off
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn) SelectCharacter(data);
                    else DeselectCharacter(data);
                });
            }

            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClickCharacterSlot(data));
            }
        }
    }

    /// <summary>
    /// 슬롯(Button) 클릭 시(토글과 별개) → 단순 로그
    /// </summary>
    private void OnClickCharacterSlot(CharacterData data)
    {
        Debug.Log($"[UpgradePanelManager] 캐릭터 슬롯 클릭: {data.characterName} (Lv={data.level})");
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

    // =================================
    //  업그레이드 버튼 8개
    // =================================
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
            upgradeButtons[i].onClick.AddListener(() => OnClickUpgradeCharacter(btnIndex));
        }
    }

    /// <summary>
    /// 업그레이드 버튼 클릭 시
    /// </summary>
    private void OnClickUpgradeCharacter(int index)
    {
        if (selectedCharacters.Count == 0)
        {
            Debug.LogWarning($"[UpgradePanel] 선택된 캐릭터가 없습니다 (버튼 {index}번).");
            return;
        }

        // 첫 번째를 '베이스 캐릭터'로 가정
        CharacterData baseChar = selectedCharacters[0];
        // 나머지는 '재료' -> 인벤토리(또는 덱)에서 제거
        int feedCount = selectedCharacters.Count - 1;

        if (feedCount > 0)
        {
            List<CharacterData> feedList = new List<CharacterData>(selectedCharacters);
            feedList.RemoveAt(0); // 첫 번째(베이스)는 제외
            characterInventory.ConsumeCharactersForUpgrade(feedList);
        }

        // 베이스 캐릭터 currentExp += feedCount
        baseChar.currentExp += feedCount;

        // 레벨업 체크
        baseChar.CheckLevelUp();

        Debug.Log($"[UpgradePanel] {index}번 업그레이드 버튼 => 베이스={baseChar.characterName}, 재료={feedCount}장 " +
                  $"=> 새 Exp={baseChar.currentExp}, Lv={baseChar.level}");

        // 다시 갱신
        RefreshUpgradeDisplay();
        SetUpgradeRegisteredSlotsFromDeck(); // 덱 패널 등록 상태도 다시 가져오기

        selectedCharacters.Clear();
    }
}
