using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 업그레이드 패널:
/// 중복 포함 모든 캐릭터를 12칸에 표시.
/// 선택(토글)한 캐릭터 중 첫 번째를 '베이스 캐릭터'로 보고,
/// 나머지는 재료로 사용하여 인벤토리에서 제거 + 베이스 EXP 증가.
/// 업그레이드 버튼도 8개가 있음.
/// </summary>
public class UpgradePanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("중복 캐릭터도 전부 보여줄 12개 슬롯 (업그레이드 창)")]
    [SerializeField] private List<GameObject> upgradeSlots;

    // ================================================
    // [변경] 업그레이드 버튼 8개
    // ================================================
    [Header("캐릭터 업그레이드 버튼 (총 8개)")]
    [SerializeField] private List<Button> upgradeButtons; // 8개

    [Header("빈 슬롯용 스프라이트 (선택)")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("빈 슬롯용 텍스트 (선택)")]
    [SerializeField] private string emptySlotText = "";

    // 선택된(토글된) 캐릭터 목록
    private List<CharacterData> selectedCharacters = new List<CharacterData>();

    private void OnEnable()
    {
        RefreshUpgradeDisplay();
        SetupUpgradeButtons();
    }

    /// <summary>
    /// 업그레이드용 전체 캐릭터 목록 표시
    /// </summary>
    public void RefreshUpgradeDisplay()
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[UpgradePanelManager] characterInventory가 없음");
            return;
        }
        // 중복 포함 전체 목록
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
    /// 하나의 슬롯에 data를 세팅
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
            if (cardImg) cardImg.sprite = emptySlotSprite;
            if (cardText) cardText.text = emptySlotText;

            if (toggle)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = false;
            }
            if (button)
            {
                button.onClick.RemoveAllListeners();
            }
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
    /// 슬롯 버튼 클릭 시 (단순 로그)
    /// </summary>
    private void OnClickCharacterSlot(CharacterData data)
    {
        Debug.Log($"[UpgradePanelManager] 캐릭터 슬롯 클릭: {data.characterName}");
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
    //  8개 업그레이드 버튼 세팅
    // =================================
    private void SetupUpgradeButtons()
    {
        if (upgradeButtons == null || upgradeButtons.Count < 8)
        {
            Debug.LogWarning("[UpgradePanelManager] upgradeButtons 설정이 부족합니다.");
            return;
        }
        // 각 버튼에 i값을 넘기는 방식
        for (int i = 0; i < upgradeButtons.Count; i++)
        {
            int btnIndex = i;
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => OnClickUpgradeCharacter(btnIndex));
        }
    }

    /// <summary>
    /// 업그레이드 버튼 클릭 시 (0~7번)
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
        // 나머지는 '재료'로 사용 -> 인벤토리에서 제거
        int feedCount = selectedCharacters.Count - 1;

        if (feedCount > 0)
        {
            List<CharacterData> feedList = new List<CharacterData>(selectedCharacters);
            feedList.RemoveAt(0); // 첫 번째(베이스)는 제외
            characterInventory.ConsumeCharactersForUpgrade(feedList);
        }

        // 베이스 캐릭터의 currentExp를 '재료 수'만큼 증가
        baseChar.currentExp += feedCount;

        // 로그/이펙트
        Debug.Log($"[UpgradePanel] {index}번 업그레이드 버튼 -> 베이스=[{baseChar.characterName}], " +
                  $"재료={feedCount}장, NewExp={baseChar.currentExp}");

        // 다시 갱신
        RefreshUpgradeDisplay();
        selectedCharacters.Clear();
    }
}
