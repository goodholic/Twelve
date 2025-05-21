using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;

/// <summary>
/// 등록된 캐릭터(10칸)에 대해 '업그레이드' 기능을 담당하는 패널 매니저.
/// 기존엔 업그레이드 재료(feedCharacter)를 직접 선택했지만,
/// 이제는 "클릭만 하면 → 같은 종족 + 같은 성급 캐릭터를 인벤토리에서 자동 소모 + 골드 차감 + 경험치+1%". 
/// 100% 차면 레벨업(최대 30).
/// (업그레이드 후 캐릭터의 경험치와 스탯을 3초간 표시)
/// </summary>
public class UpgradePanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("DeckPanelManager (등록상태 복사)")]
    [SerializeField] private DeckPanelManager deckPanelManager;

    [Header("ShopManager (골드 소모)")]
    [SerializeField] private ShopManager shopManager;

    [Header("업그레이드 비용 (고정 10골드)")]
    [SerializeField] private int upgradeCost = 10;

    // ==========================
    //  (A) 등록 상태(10칸)
    // ==========================
    [Header("빈 슬롯용 스프라이트(업그레이드 10칸)")]
    [SerializeField] private Sprite emptyUpgradeSlotSprite;

    [Header("업그레이드 등록 상태 이미지(10칸) - set2")]
    [SerializeField] private List<Image> upgradeRegisteredSlotImages;

    [Header("업그레이드 슬롯(10칸) 레벨 텍스트")]
    [SerializeField] private List<TextMeshProUGUI> upgradeRegisteredSlotLevelTexts;

    // ▼▼ [수정 전] private CharacterData[] registeredSet2_Up = new CharacterData[10];
    // ===========================================
    // 기본 코드. 여기서는 바로 초기화했지만, 아래에서 덱과 동일 배열을 참조하도록 변경할 예정.
    // ===========================================
    [Header("▼▼ [중요] 덱 참조 ▼▼")]
    // (기존에 있던 필드 유지하되, 실질적으로는 덱 배열을 참조하게 변경)
    private CharacterData[] registeredSet2_Up = new CharacterData[10];

    // ==========================
    // (B) 결과/스탯 표시
    // ==========================
    [Header("업그레이드 결과 패널 (3초간 보여주기)")]
    [SerializeField] private GameObject upgradeResultPanel;

    [Header("업그레이드 결과 텍스트 (경험치/스탯 등 표시)")]
    [SerializeField] private TextMeshProUGUI upgradeResultText;

    [Header("스탯 증가 표시용 패널 (옵션)")]
    [SerializeField] private GameObject statBarsPanel;
    [SerializeField] private RectTransform attackPowerBar;
    [SerializeField] private RectTransform attackSpeedBar;
    [SerializeField] private RectTransform hpBar;
    [SerializeField] private RectTransform moveSpeedBar;
    [SerializeField] private RectTransform attackRangeBar;

    // ===========================================
    // (C) 업그레이드 버튼 (10개)
    // ===========================================
    [Header("캐릭터 업그레이드 버튼 (총 10개)")]
    [SerializeField] private List<Button> upgradeButtons;

    private void Awake()
    {
        if (shopManager == null)
        {
            shopManager = FindFirstObjectByType<ShopManager>();
        }

        if (upgradeResultPanel != null)
        {
            upgradeResultPanel.SetActive(false);
        }

        if (statBarsPanel != null)
        {
            statBarsPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 덱 패널에서 등록 상태 복사
        SetUpgradeRegisteredSlotsFromDeck();
        // 업그레이드 버튼 초기화
        SetupUpgradeButtons();
    }

    /// <summary>
    /// 외부에서 "업그레이드 패널" 새로고침
    /// </summary>
    public void RefreshDisplay()
    {
        SetUpgradeRegisteredSlotsFromDeck();
    }

    /// <summary>
    /// 덱 패널에 등록된 10칸 정보를 가져와서 upgradeRegisteredSlotImages 등에 반영
    /// </summary>
    public void SetUpgradeRegisteredSlotsFromDeck()
    {
        if (deckPanelManager == null)
        {
            Debug.LogWarning("[UpgradePanelManager] deckPanelManager가 null");
            return;
        }

        // -------------------------------------------------------------------
        // ▼▼ [수정] 덱 배열(registeredCharactersSet2)을 그대로 참조하도록 변경 ▼▼
        // -------------------------------------------------------------------
        registeredSet2_Up = deckPanelManager.registeredCharactersSet2;  // 덱 배열 "그대로" 참조
        // ▲▲ [수정끝] ▲▲

        if (registeredSet2_Up == null || registeredSet2_Up.Length < 10)
        {
            Debug.LogWarning("[UpgradePanelManager] registeredSet2_Up가 올바르지 않음");
            return;
        }

        // 슬롯 시각 갱신
        for (int i = 0; i < 10; i++)
        {
            UpdateUpgradeRegisteredImage(i);
        }
    }

    private void UpdateUpgradeRegisteredImage(int i)
    {
        if (upgradeRegisteredSlotImages == null || i < 0 || i >= upgradeRegisteredSlotImages.Count)
            return;

        Image slotImg = upgradeRegisteredSlotImages[i];
        TextMeshProUGUI lvlText = (upgradeRegisteredSlotLevelTexts != null && i < upgradeRegisteredSlotLevelTexts.Count)
            ? upgradeRegisteredSlotLevelTexts[i]
            : null;

        CharacterData cData = registeredSet2_Up[i];
        if (cData == null)
        {
            if (slotImg)
            {
                slotImg.sprite = emptyUpgradeSlotSprite;
            }
            if (lvlText != null) lvlText.text = "";
        }
        else
        {
            // 캐릭터 있음
            if (slotImg && cData.buttonIcon != null)
            {
                slotImg.sprite = cData.buttonIcon.sprite;
            }
            if (lvlText != null)
            {
                lvlText.text = $"Lv.{cData.level}";
            }
        }
    }

    /// <summary>
    /// 업그레이드 버튼(10개)에 리스너 연결
    /// </summary>
    public void SetupUpgradeButtons()
    {
        if (upgradeButtons == null || upgradeButtons.Count < 10)
        {
            Debug.LogWarning("[UpgradePanelManager] upgradeButtons가 10개 미만!");
            return;
        }

        for (int i = 0; i < upgradeButtons.Count; i++)
        {
            Button btn = upgradeButtons[i];
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                int copyIndex = i;
                btn.onClick.AddListener(() => OnClickUpgradeButton(copyIndex));
            }
        }
    }

    /// <summary>
    /// (1) 클릭된 캐릭터 = registeredSet2_Up[index]
    /// (2) 인벤토리에서 "같은 종족 + 같은 성급" 캐릭터 1명 찾아 제거
    /// (3) 10골드 소모
    /// (4) 경험치+1%, 100% 되면 레벨업(최대30)
    /// (5) 3초간 스탯/경험치 표시
    /// </summary>
    private void OnClickUpgradeButton(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 10)
        {
            Debug.LogWarning($"[UpgradePanel] 잘못된 업그레이드 슬롯 인덱스: {slotIndex}");
            return;
        }

        // 캐릭터 데이터 확인
        CharacterData targetChar = registeredSet2_Up[slotIndex];
        if (targetChar == null)
        {
            Debug.LogWarning($"[UpgradePanel] {slotIndex}번 슬롯에 캐릭터가 없어 업그레이드 불가");
            return;
        }

        // 골드 소모 (10골드)
        if (shopManager == null)
        {
            Debug.LogWarning("[UpgradePanel] shopManager가 없어 업그레이드 불가");
            return;
        }
        // 골드 차감
        if (!shopManager.TrySpendGold(upgradeCost))
        {
            // 골드 부족
            return;
        }

        // 인벤토리에서 "같은 종족 + 같은 성급" 캐릭터 찾기
        if (characterInventory == null)
        {
            Debug.LogWarning("[UpgradePanel] characterInventory가 null이라 업그레이드 불가");
            return;
        }

        List<CharacterData> ownedList = characterInventory.GetOwnedCharacters();
        CharacterData match = null;
        for (int i = 0; i < ownedList.Count; i++)
        {
            CharacterData cd = ownedList[i];
            if (cd != null && cd.race == targetChar.race && cd.level == targetChar.level)
            {
                // 동일 종족 + 동일 레벨 => 여기서 '성급'은 level 아닌 star일 수도 있지만,
                // 만약 star 정보를 사용하려면 cd.initialStar == targetChar.initialStar 로 체크
                if (cd.initialStar == targetChar.initialStar)
                {
                    match = cd;
                    break;
                }
            }
        }

        if (match == null)
        {
            Debug.LogWarning($"[UpgradePanel] 같은 종족+성급 캐릭터가 인벤토리에 없어 업그레이드 불가 => 골드 {upgradeCost}는 이미 소모됨...");
            return;
        }

        // 매치된 캐릭터 제거
        characterInventory.RemoveFromInventory(match);

        // 1% 경험치 증가
        targetChar.currentExp += 1;

        // 100% 누적되면 레벨업 (최대 30레벨)
        if (targetChar.currentExp >= 100)
        {
            targetChar.level++;
            if (targetChar.level > 30)
            {
                targetChar.level = 30;
            }
            else
            {
                // 레벨업하면 currentExp는 0으로
                targetChar.currentExp = 0;
            }
        }

        // 스탯/경험치 표시 (3초)
        string resultMsg = MakeUpgradeResultMessage(targetChar);
        ShowUpgradeResult(targetChar, resultMsg);

        // 슬롯 UI 갱신
        UpdateUpgradeRegisteredImage(slotIndex);

        // 저장
        characterInventory.SaveCharacters();
    }

    /// <summary>
    /// 업그레이드 결과 메시지(캐릭터 레벨, 현재Exp 등)
    /// </summary>
    private string MakeUpgradeResultMessage(CharacterData targetChar)
    {
        return $"{targetChar.characterName} 업그레이드!\n" +
               $"Lv.{targetChar.level} (Exp {targetChar.currentExp}%)\n\n" +
               $"공격력: {targetChar.attackPower}\n" +
               $"사거리: {targetChar.attackRange}\n" +
               $"공격속도: {targetChar.attackSpeed}\n" +
               $"체력: {targetChar.maxHP}";
    }

    /// <summary>
    /// 3초간 업그레이드 결과 패널을 띄우고 스탯 바 표시
    /// </summary>
    private void ShowUpgradeResult(CharacterData character, string resultText)
    {
        if (upgradeResultPanel == null || upgradeResultText == null)
        {
            Debug.LogWarning("[UpgradePanelManager] 업그레이드 결과 패널이 null이어서 표시 불가.");
            return;
        }

        upgradeResultText.text = resultText;
        upgradeResultPanel.SetActive(true);

        // 스탯 바 업데이트
        UpdateStatBars(character);

        // 3초 후 자동 숨김
        StartCoroutine(HideUpgradeResultAfterDelay(3.0f));
    }

    /// <summary>
    /// 캐릭터 스탯 바를 업데이트
    /// </summary>
    private void UpdateStatBars(CharacterData character)
    {
        if (statBarsPanel == null) return;

        statBarsPanel.SetActive(true);

        if (attackPowerBar != null)
            attackPowerBar.sizeDelta = new Vector2(character.attackPower * 10f, attackPowerBar.sizeDelta.y);

        if (attackSpeedBar != null)
            attackSpeedBar.sizeDelta = new Vector2(character.attackSpeed * 50f, attackSpeedBar.sizeDelta.y);

        if (hpBar != null)
            hpBar.sizeDelta = new Vector2(character.maxHP / 2f, hpBar.sizeDelta.y);

        if (moveSpeedBar != null)
            moveSpeedBar.sizeDelta = new Vector2(character.moveSpeed * 30f, moveSpeedBar.sizeDelta.y);

        if (attackRangeBar != null)
            attackRangeBar.sizeDelta = new Vector2(character.attackRange * 30f, attackRangeBar.sizeDelta.y);
    }

    private IEnumerator HideUpgradeResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (upgradeResultPanel != null)
            upgradeResultPanel.SetActive(false);

        if (statBarsPanel != null)
            statBarsPanel.SetActive(false);
    }
}
