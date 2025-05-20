using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DeckPanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager (인벤토리)")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    // ============================================
    //  (A) 200칸 인벤토리: Image 슬롯 + Button 슬롯
    // ============================================
    [Header("인벤토리 UI (200칸짜리)")]
    [Tooltip("인벤토리 200칸에 대응하는 Image 컴포넌트들 (순서 중요!)")]
    [SerializeField] private Image[] inventorySlotImages;
    
    [Header("인벤토리 슬롯 빈칸 스프라이트")]
    [SerializeField] private Sprite emptyInventorySlotSprite;

    [Header("인벤토리 슬롯 버튼")]
    [Tooltip("인벤토리 200칸에 대응하는 Button 컴포넌트들 (순서 동일하게!)")]
    [SerializeField] private Button[] inventorySlotButtons;

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

    // =============================
    // (추가됨) 실시간 null판별을 위해 이전 프레임의 sharedSlotData200 상태를 보관
    // =============================
    private CharacterData[] prevSharedSlotData200;

    // -----------------------------------------------------------------
    // (기존) 빈 슬롯 시 표시할 텍스트 필드 + 로직은 제거 (Text가 없어졌으므로 사용 안 함)
    // -----------------------------------------------------------------

    private void OnEnable()
    {
        // 패널이 켜질 때, 업그레이드 모드를 무조건 해제
        isUpgradeMode = false;

        // 인벤토리(200칸) 즉시 갱신
        RefreshInventoryUI();

        // 덱 등록 버튼 초기화
        SetupRegisterButtons();

        // (추가됨) 저장된 덱 정보를 registeredCharactersSet2에 로드
        LoadDeckSlotsFromInventoryManager();  // (추가됨)

        // 덱 슬롯(10칸) 시각 갱신
        InitRegisterSlotsVisual();

        // 인벤토리 UI 슬롯 이미지 기본 초기화
        if (inventorySlotImages != null)
        {
            for (int i = 0; i < inventorySlotImages.Length; i++)
            {
                if (inventorySlotImages[i] != null)
                {
                    inventorySlotImages[i].sprite = emptyInventorySlotSprite;
                }
            }
        }

        // prevSharedSlotData200 배열 크기 초기화
        if (characterInventory != null && characterInventory.sharedSlotData200 != null)
        {
            prevSharedSlotData200 = new CharacterData[characterInventory.sharedSlotData200.Length];
        }
        else
        {
            prevSharedSlotData200 = new CharacterData[200]; // 기본 크기
            Debug.LogWarning("[DeckPanelManager] characterInventory 또는 sharedSlotData200이 null입니다. 기본 크기(200)로 초기화합니다.");
        }
    }

    // -----------------------------------------------------
    // (추가됨) 인벤토리 매니저에 저장된 '덱 캐릭터' 10칸을
    //          registeredCharactersSet2 에 로딩하는 메서드
    // -----------------------------------------------------
    private void LoadDeckSlotsFromInventoryManager()
    {
        if (characterInventory == null) return;

        // deckCharacters (현재 덱 목록) 가져오기
        var deckList = characterInventory.GetDeckCharacters(); 
        // ↑ 이 목록은 "SaveCharacters() / LoadCharacters()" 통해 저장/불러오기

        // 10칸에 맞춰 등록
        for (int i = 0; i < 10; i++)
        {
            if (i < deckList.Count)
                registeredCharactersSet2[i] = deckList[i];
            else
                registeredCharactersSet2[i] = null;
        }
    }

    // -----------------------------------------------------
    // (추가됨) Update()에서 sharedSlotData200 변화를 실시간 감지
    // -----------------------------------------------------
    private void Update()
    {
        if (characterInventory == null || prevSharedSlotData200 == null) return;

        // prevSharedSlotData200이 null이면 초기화
        if (prevSharedSlotData200 == null)
        {
            if (characterInventory != null && characterInventory.sharedSlotData200 != null)
            {
                prevSharedSlotData200 = new CharacterData[characterInventory.sharedSlotData200.Length];
            }
            else
            {
                prevSharedSlotData200 = new CharacterData[200]; // 기본 크기
                Debug.LogWarning("[DeckPanelManager] prevSharedSlotData200이 null입니다. 기본 크기(200)로 초기화합니다.");
            }
            return; // 다음 프레임에 비교 수행
        }
        
        // 실제 UI 슬롯 개수에 맞춰 비교
        int slotCount = prevSharedSlotData200.Length;
        
        // 매 프레임 sharedSlotData200과 prevSharedSlotData200 비교
        for (int i = 0; i < slotCount; i++)
        {
            if (characterInventory != null && 
                characterInventory.sharedSlotData200 != null && 
                i < characterInventory.sharedSlotData200.Length && 
                i < prevSharedSlotData200.Length && 
                characterInventory.sharedSlotData200[i] != prevSharedSlotData200[i])
            {
                // null 또는 빈 데이터 정리
                characterInventory.CondenseAndReorderSharedSlots();
                characterInventory.SyncOwnedFromSharedSlots();

                // 그리고 UI 다시 갱신
                RefreshInventoryUI();

                // prevSharedSlotData200 최신화
                for (int j = 0; j < slotCount; j++)
                {
                    if (characterInventory.sharedSlotData200 != null && 
                        j < characterInventory.sharedSlotData200.Length && 
                        j < prevSharedSlotData200.Length)
                    {
                        prevSharedSlotData200[j] = characterInventory.sharedSlotData200[j];
                    }
                }
                break;
            }
        }
    }

    // ==========================================================================
    // (1) 인벤토리 표시 => CharacterInventoryManager의 sharedSlotData200 활용
    // ==========================================================================
    public void RefreshInventoryUI()
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[DeckPanelManager] characterInventory가 연결되지 않음!");
            return;
        }
        
        // 실제 UI 슬롯 개수 확인
        int uiSlotCount = 0;
        if (inventorySlotImages != null)
        {
            uiSlotCount = inventorySlotImages.Length;
        }
        
        if (uiSlotCount < 200)
        {
            Debug.LogWarning($"[DeckPanelManager] inventorySlotImages가 충분하지 않음! (현재: {uiSlotCount}개, 필요: 200개)");
            // 계속 진행하지만 경고 표시
        }
        
        if (inventorySlotButtons == null || inventorySlotButtons.Length < uiSlotCount)
        {
            Debug.LogWarning($"[DeckPanelManager] inventorySlotButtons가 충분하지 않음! (현재: {(inventorySlotButtons != null ? inventorySlotButtons.Length : 0)}개, 필요: {uiSlotCount}개)");
            return;
        }

        // (A) ownedCharacters를 sharedSlotData200에 복사
        var ownedList = characterInventory.GetOwnedCharacters();
        
        if (characterInventory.sharedSlotData200 == null)
        {
            Debug.LogError("[DeckPanelManager] characterInventory.sharedSlotData200이 null입니다!");
            return;
        }
        
        int dataSlotCount = characterInventory.sharedSlotData200.Length;
        
        for (int i = 0; i < dataSlotCount; i++)
        {
            if (i < ownedList.Count && i < characterInventory.sharedSlotData200.Length)
                characterInventory.sharedSlotData200[i] = ownedList[i];
            else if (i < characterInventory.sharedSlotData200.Length)
                characterInventory.sharedSlotData200[i] = null;
        }

        // (B) UI 갱신 (실제 UI 슬롯 개수만큼만)
        for (int i = 0; i < uiSlotCount; i++)
        {
            Image slotImg = inventorySlotImages[i];
            Button slotBtn = inventorySlotButtons[i];

            CharacterData cData = null;
            if (i < dataSlotCount && i < characterInventory.sharedSlotData200.Length)
            {
                cData = characterInventory.sharedSlotData200[i];
            }

            if (cData != null)
            {
                // 아이콘
                slotImg.sprite = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : emptyInventorySlotSprite;

                // 버튼
                slotBtn.onClick.RemoveAllListeners();
                slotBtn.interactable = true;

                int copyIndex = i; 
                CharacterData copyData = cData;
                slotBtn.onClick.AddListener(() => OnClickInventoryCharacter(copyData, copyIndex));
            }
            else
            {
                slotImg.sprite = emptyInventorySlotSprite;
                slotBtn.onClick.RemoveAllListeners();
                slotBtn.interactable = false;
            }
        }
    }

    /// <summary>
    /// 인벤토리 슬롯(200개) 클릭 시: 업그레이드모드면 재료 선택, 아니면 덱 등록용
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
    // (2) 덱(10칸) 관련: 등록버튼/슬롯
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
            slotImg.sprite = (cData.buttonIcon != null) ? cData.buttonIcon.sprite : emptyDeckSlotSprite;
            if (lvlText != null) lvlText.text = $"Lv.{cData.level}";
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

        // 덱에 이미 있나 확인
        CharacterData existingChar = registeredCharactersSet2[slotIndex];
        if (existingChar == null)
        {
            // 빈 슬롯 -> 새로 등록
            Debug.Log($"[DeckPanelManager] 덱슬롯 {slotIndex}에 '{selectedCharacterForRegistration.characterName}' 등록");

            // 인벤토리 목록에서 제거 -> 덱 목록에 추가
            characterInventory.RemoveFromInventory(selectedCharacterForRegistration);
            characterInventory.MoveToDeck(selectedCharacterForRegistration);

            // registeredCharactersSet2[slotIndex]에 반영
            registeredCharactersSet2[slotIndex] = characterInventory.GetDeckCharacters()
                .Find(c => c == registeredCharactersSet2[slotIndex]) 
                ?? selectedCharacterForRegistration;
        }
        else
        {
            // 이미 있던 캐릭터 교체
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

        // 인벤토리 UI 갱신
        RefreshInventoryUI();

        // 저장 (인벤토리+덱 변경 반영)
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