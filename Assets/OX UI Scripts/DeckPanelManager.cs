// =========================================
// DeckPanelManager.cs (수정본)
// =========================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DeckPanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager (인벤토리)")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    // ============================================
    //  (A) 20칸 인벤토리: Image 슬롯 + Button 슬롯
    // ============================================
    [Header("20칸 인벤토리 슬롯 (Image)")]
    [Tooltip("인벤토리 20칸에 대응하는 Image 컴포넌트들 (순서 중요!)")]
    [SerializeField] private List<Image> inventorySlotImages20;

    [Header("인벤토리 슬롯 20칸의 빈칸용 스프라이트")]
    [SerializeField] private Sprite emptyInventorySlotSprite;

    [Header("인벤토리 슬롯(버튼) 20칸")]
    [Tooltip("인벤토리 20칸에 대응하는 Button 컴포넌트들 (순서 동일하게!)")]
    [SerializeField] private List<Button> inventorySlotButtons20;

    // ============================================
    //  (B) 덱(10칸) 관련
    // ============================================
    [Header("덱(10칸)에서 사용될 빈칸 스프라이트")]
    [SerializeField] private Sprite emptyDeckSlotSprite;

    [Header("등록 버튼(10개) + 이미지(10개) - 세트2")]
    [SerializeField] private List<Button> registerButtons;   
    [SerializeField] private List<Image> registerSlotImages; 

    [Header("덱(10칸)의 레벨 표시용 텍스트(옵션)")]
    [SerializeField] private List<TextMeshProUGUI> registerSlotLevelTexts;

    // ============================================
    // 덱(10칸)에 실제 등록된 캐릭터들
    // ============================================
    public CharacterData[] registeredCharactersSet2 = new CharacterData[10];

    // ============================================
    // 인벤토리 슬롯 클릭 시 등록에 사용하기 위한 임시값
    // ============================================
    private CharacterData selectedCharacterForRegistration = null;
    private int selectedInventorySlotIndex = -1;

    // ============================================
    // 업그레이드 모드 여부(클릭 시 재료 선택용)
    // ============================================
    [Header("업그레이드 모드인지 여부 (true면 클릭 시 재료 선택)")]
    public bool isUpgradeMode = false;

    // -----------------------------------------------------------------
    // (추가됨) 실시간 null판별을 위해 이전 프레임의 sharedSlotData20 상태를 보관
    // -----------------------------------------------------------------
    private CharacterData[] prevSharedSlotData20 = new CharacterData[20]; // 추가된 멤버

    // ----------------------------------------------------------------------------------
    // (기존) 빈 슬롯 시 표시할 텍스트 필드 + 로직은 제거 (Text가 없어졌으므로 사용 안 함)
    // ----------------------------------------------------------------------------------

    private void OnEnable()
    {
        // 패널이 켜질 때, 업그레이드 모드를 무조건 해제
        isUpgradeMode = false;

        // 인벤토리(20칸) 즉시 갱신
        RefreshInventoryUI();

        // 덱 등록 버튼 초기화
        SetupRegisterButtons();

        // 덱 슬롯(10칸) 시각 갱신
        InitRegisterSlotsVisual();

        // -------------------------------------------------------------------
        // (추가됨) 패널 켜질 때 현재 sharedSlotData20 상태를 prev 배열에 복사
        // -------------------------------------------------------------------
        if (characterInventory != null)
        {
            for (int i = 0; i < 20; i++)
            {
                prevSharedSlotData20[i] = characterInventory.sharedSlotData20[i];
            }
        }
    }

    // -------------------------------------------------------------------
    // (추가됨) Update()에서 sharedSlotData20 변화를 실시간으로 감지하여 UI 갱신
    // -------------------------------------------------------------------
    private void Update()
    {
        if (characterInventory == null) return;

        // 매 프레임 sharedSlotData20과 prevSharedSlotData20 비교
        for (int i = 0; i < 20; i++)
        {
            if (characterInventory.sharedSlotData20[i] != prevSharedSlotData20[i])
            {
                // ---------------------------------------------------------------
                // (추가됨) null이 된 칸이 있다면 즉시 '비정상' 데이터 정리 후 재정렬
                // ---------------------------------------------------------------
                characterInventory.CondenseAndReorderSharedSlots(); // <-- null/빈칸 제거
                characterInventory.SyncOwnedFromSharedSlots();      // <-- 인벤토리 리스트와 동기화

                // 그리고 UI 다시 갱신
                RefreshInventoryUI();

                // 갱신 직후, prevSharedSlotData20도 최신화
                for (int j = 0; j < 20; j++)
                {
                    prevSharedSlotData20[j] = characterInventory.sharedSlotData20[j];
                }
                break;
            }
        }
    }

    // ==========================================================================
    // (1) 20칸 인벤토리 표시
    // => CharacterInventoryManager의 sharedSlotData20을 이용
    // ==========================================================================
    public void RefreshInventoryUI()
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[DeckPanelManager] characterInventory가 연결되지 않음!");
            return;
        }
        if (inventorySlotImages20 == null || inventorySlotImages20.Count < 20)
        {
            Debug.LogWarning("[DeckPanelManager] inventorySlotImages20이 20개 세팅되지 않음!");
            return;
        }
        if (inventorySlotButtons20 == null || inventorySlotButtons20.Count < 20)
        {
            Debug.LogWarning("[DeckPanelManager] inventorySlotButtons20이 20개 세팅되지 않음!");
            return;
        }

        // (A) ownedCharacters를 sharedSlotData20에 복사
        // CharacterInventoryManager에는 SyncOwnedToSharedSlots가 없으므로 여기서 직접 구현
        var ownedList = characterInventory.GetOwnedCharacters();
        for (int i = 0; i < 20; i++)
        {
            if (i < ownedList.Count)
                characterInventory.sharedSlotData20[i] = ownedList[i];
            else
                characterInventory.sharedSlotData20[i] = null;
        }

        // (B) 20칸 UI 갱신
        for (int i = 0; i < 20; i++)
        {
            Image slotImg = inventorySlotImages20[i];
            Button slotBtn = inventorySlotButtons20[i];

            CharacterData cData = characterInventory.sharedSlotData20[i];
            if (cData != null)
            {
                // 실제 캐릭터
                // 1) 아이콘
                if (cData.buttonIcon != null)
                    slotImg.sprite = cData.buttonIcon.sprite;
                else
                    slotImg.sprite = emptyInventorySlotSprite;

                // 2) 버튼 클릭 리스너
                slotBtn.onClick.RemoveAllListeners();
                slotBtn.interactable = true;

                int copyIndex = i; 
                CharacterData copyData = cData;
                slotBtn.onClick.AddListener(() => OnClickInventoryCharacter(copyData, copyIndex));
            }
            else
            {
                // ============ null이면 => 빈칸 처리 ============ 
                slotImg.sprite = emptyInventorySlotSprite;

                // 클릭 불가
                slotBtn.onClick.RemoveAllListeners();
                slotBtn.interactable = false;
            }
        }
    }

    /// <summary>
    /// 인벤토리 슬롯(20개 중 하나) 클릭 시 로직
    /// (업그레이드 모드면 재료 선택, 아니면 덱 등록용)
    /// </summary>
    private void OnClickInventoryCharacter(CharacterData data, int inventorySlotIndex)
    {
        if (isUpgradeMode)
        {
            // 업그레이드 패널에 재료로 전달
            UpgradePanelManager upm = FindFirstObjectByType<UpgradePanelManager>();
            if (upm != null)
            {
                upm.SetFeedFromInventory(inventorySlotIndex, data);
                Debug.Log($"[DeckPanelManager] 업그레이드 재료 선택됨: {data.characterName}, index={inventorySlotIndex}");
            }
        }
        else
        {
            // 덱 등록 모드
            selectedCharacterForRegistration = data;
            selectedInventorySlotIndex = inventorySlotIndex;
            Debug.Log($"[DeckPanelManager] 인벤토리 슬롯({inventorySlotIndex}) 클릭 => {data.characterName}");
        }
    }

    // =====================================================================
    // (2) 덱(10칸) 관련 - 버튼/슬롯
    // =====================================================================
    public void SetupRegisterButtons()
    {
        if (registerButtons == null || registerButtons.Count < 10)
        {
            Debug.LogWarning("[DeckPanelManager] registerButtons(10개)가 세팅 안 됨!");
            return;
        }

        // 각 버튼에 클릭 리스너 연결
        for (int i = 0; i < registerButtons.Count; i++)
        {
            Button btn = registerButtons[i];
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                int copyIndex = i;
                btn.onClick.AddListener(() => OnClickRegisterCharacterSet2(copyIndex));
            }
        }
    }

    public void InitRegisterSlotsVisual()
    {
        // 10칸 슬롯 이미지/텍스트 초기화
        for (int i = 0; i < 10; i++)
        {
            UpdateRegisterSlotVisual(i);
        }
    }

    private void UpdateRegisterSlotVisual(int i)
    {
        if (registerSlotImages == null || i < 0 || i >= registerSlotImages.Count)
            return;

        Image slotImg = registerSlotImages[i];
        TextMeshProUGUI lvlText = (registerSlotLevelTexts != null && i < registerSlotLevelTexts.Count)
                                  ? registerSlotLevelTexts[i]
                                  : null;

        CharacterData cData = registeredCharactersSet2[i];
        if (cData == null)
        {
            // 빈칸
            slotImg.sprite = emptyDeckSlotSprite;
            if (lvlText != null) lvlText.text = "";
        }
        else
        {
            // 캐릭터가 있으면
            if (cData.buttonIcon != null)
                slotImg.sprite = cData.buttonIcon.sprite;
            else
                slotImg.sprite = emptyDeckSlotSprite;

            if (lvlText != null)
            {
                lvlText.text = $"Lv.{cData.level}";
            }
        }
    }

    private void OnClickRegisterCharacterSet2(int slotIndex)
    {
        if (selectedCharacterForRegistration == null)
        {
            Debug.LogWarning($"[DeckPanelManager] 등록할 캐릭터가 선택되지 않았습니다. (덱슬롯 {slotIndex})");
            return;
        }
        if (selectedInventorySlotIndex < 0)
        {
            Debug.LogWarning("[DeckPanelManager] 인벤토리에서 선택된 슬롯 인덱스가 유효하지 않음!");
            return;
        }

        // 기존에 등록된 캐릭터가 있는지 확인
        CharacterData existingChar = registeredCharactersSet2[slotIndex];
        if (existingChar == null)
        {
            // 빈 슬롯 -> 새로 등록
            Debug.Log($"[DeckPanelManager] 덱슬롯 {slotIndex}에 '{selectedCharacterForRegistration.characterName}' 등록");

            // 인벤토리 목록에서 제거 -> 덱 목록에 추가
            characterInventory.RemoveFromInventory(selectedCharacterForRegistration);
            characterInventory.MoveToDeck(selectedCharacterForRegistration);
            selectedCharacterForRegistration = null;

            // 새로 등록된 캐릭터를 registeredCharactersSet2[slotIndex]에 할당
            registeredCharactersSet2[slotIndex] = characterInventory.GetDeckCharacters()
                .Find(c => c == registeredCharactersSet2[slotIndex]) 
                ?? selectedCharacterForRegistration;
        }
        else
        {
            // 이미 덱에 있던 캐릭터 교체
            Debug.Log($"[DeckPanelManager] 교체: 기존={existingChar.characterName} / 새={selectedCharacterForRegistration.characterName}");
            characterInventory.RemoveFromDeck(existingChar);
            characterInventory.AddToInventory(existingChar);

            characterInventory.RemoveFromInventory(selectedCharacterForRegistration);
            characterInventory.MoveToDeck(selectedCharacterForRegistration);

            registeredCharactersSet2[slotIndex] = selectedCharacterForRegistration;
        }

        // 선택 해제
        selectedCharacterForRegistration = null;
        selectedInventorySlotIndex = -1;

        // 덱 슬롯 UI 갱신
        UpdateRegisterSlotVisual(slotIndex);

        // 인벤토리 다시 표시(20칸)
        RefreshInventoryUI();

        // 저장
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
