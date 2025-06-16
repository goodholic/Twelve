using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

/// <summary>
/// 덱 패널 매니저 - 인벤토리(200칸)와 덱(10칸) 관리
/// 캐릭터 등록/해제, UI 표시, 종족 제한 검증 기능 포함
/// </summary>
public class DeckPanelManager : MonoBehaviour
{
    [Header("=== 인벤토리 매니저 참조 ===")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("=== 데이터베이스 참조 ===")]
    [SerializeField] private CharacterDatabase characterDB;
    [SerializeField] private CharacterDatabaseObject characterDBObject;

    [Header("=== UI 슬롯 이미지 (200칸) ===")]
    [SerializeField] private Image[] inventorySlotImages = new Image[200];
    
    [Header("=== UI 슬롯 버튼 (200칸) ===")]
    [SerializeField] private Button[] inventorySlotButtons = new Button[200];

    [Header("=== 덱 등록 슬롯 이미지 (10칸) ===")]
    [SerializeField] private Image[] registerSlotImages = new Image[10];
    
    [Header("=== 덱 등록 슬롯 버튼 (10칸) ===")]
    [SerializeField] private Button[] registerSlotButtons = new Button[10];

    [Header("=== 레벨 텍스트 (10칸) ===")]
    [SerializeField] private TextMeshProUGUI[] registerSlotLevelTexts = new TextMeshProUGUI[10];

    [Header("=== 빈 슬롯 스프라이트 ===")]
    [SerializeField] private Sprite emptyInventorySlotSprite;
    [SerializeField] private Sprite emptyRegisterSlotSprite;

    [Header("=== 종족별 등록 카운트 텍스트 ===")]
    [SerializeField] private TextMeshProUGUI humanCountText;
    [SerializeField] private TextMeshProUGUI orcCountText;
    [SerializeField] private TextMeshProUGUI elfCountText;

    [Header("=== 자동 등록 버튼 ===")]
    [SerializeField] private Button autoRegisterButton;

    [Header("=== 종족 제한 경고 ===")]
    [SerializeField] private TextMeshProUGUI raceRuleWarningText;
    [SerializeField] private float warningDisplayTime = 3f;

    // 등록된 캐릭터 배열 (10칸)
    public CharacterData[] registeredCharactersSet2 = new CharacterData[10];
    
    // 종족별 카운트
    private Dictionary<CharacterRace, int> raceCountDict = new Dictionary<CharacterRace, int>();

    // 업그레이드 모드 여부
    public bool isUpgradeMode = false;

    // 이전 프레임 인벤토리 상태 (null 체크용)
    private CharacterData[] prevSharedSlotData200;

    private void OnEnable()
    {
        Debug.Log("[DeckPanelManager] 패널 활성화됨");
        
        // 업그레이드 모드 해제
        isUpgradeMode = false;
        
        // 데이터베이스 체크
        CheckDatabases();

        // 인벤토리 즉시 갱신
        RefreshInventoryUI();

        // 덱 등록 버튼 초기화
        SetupRegisterButtons();

        // 저장된 덱 정보 로드
        LoadDeckSlotsFromInventoryManager();

        // 덱 슬롯 시각 갱신
        InitRegisterSlotsVisual();

        // 종족별 카운트 업데이트
        UpdateRaceCountUI();

        // 이전 상태 배열 초기화
        if (characterInventory != null && characterInventory.sharedSlotData200 != null)
        {
            prevSharedSlotData200 = new CharacterData[characterInventory.sharedSlotData200.Length];
        }
    }

    private void Start()
    {
        CheckDatabases();
        
        // 자동 등록 버튼 설정
        if (autoRegisterButton != null)
        {
            autoRegisterButton.onClick.RemoveAllListeners();
            autoRegisterButton.onClick.AddListener(AutoFillDeckByRuleRule);
        }
    }

    private void Update()
    {
        // 실시간 인벤토리 변화 감지
        if (characterInventory != null && characterInventory.sharedSlotData200 != null)
        {
            for (int i = 0; i < characterInventory.sharedSlotData200.Length && i < 200; i++)
            {
                CharacterData currentData = characterInventory.sharedSlotData200[i];
                CharacterData prevData = (prevSharedSlotData200 != null && i < prevSharedSlotData200.Length) 
                    ? prevSharedSlotData200[i] : null;

                if (currentData != prevData)
                {
                    UpdateInventorySlotUI(i, currentData);
                    
                    if (prevSharedSlotData200 != null && i < prevSharedSlotData200.Length)
                    {
                        prevSharedSlotData200[i] = currentData;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 데이터베이스 확인 및 설정
    /// </summary>
    private void CheckDatabases()
    {
        if (characterDB == null && characterDBObject == null)
        {
            // CoreDataManager에서 가져오기
            var coreData = CoreDataManager.Instance;
            if (coreData != null)
            {
                characterDBObject = coreData.characterDatabase;
                if (characterDBObject != null)
                {
                    Debug.Log("[DeckPanelManager] CoreDataManager에서 데이터베이스 가져옴");
                }
            }
        }
    }

    /// <summary>
    /// 인벤토리 UI 전체 갱신
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (characterInventory == null) return;

        Debug.Log("[DeckPanelManager] 인벤토리 UI 갱신 시작");

        for (int i = 0; i < inventorySlotImages.Length && i < 200; i++)
        {
            CharacterData data = null;
            if (characterInventory.sharedSlotData200 != null && i < characterInventory.sharedSlotData200.Length)
            {
                data = characterInventory.sharedSlotData200[i];
            }
            
            UpdateInventorySlotUI(i, data);
        }
    }

    /// <summary>
    /// 개별 인벤토리 슬롯 UI 업데이트
    /// </summary>
    private void UpdateInventorySlotUI(int index, CharacterData data)
    {
        if (index >= inventorySlotImages.Length || inventorySlotImages[index] == null) return;

        Image slotImage = inventorySlotImages[index];
        Button slotButton = (index < inventorySlotButtons.Length) ? inventorySlotButtons[index] : null;

        if (data != null && data.buttonIcon != null)
        {
            slotImage.sprite = data.buttonIcon;
            slotImage.color = Color.white;
            
            // 버튼 활성화 및 이벤트 설정
            if (slotButton != null)
            {
                slotButton.interactable = true;
                slotButton.onClick.RemoveAllListeners();
                int capturedIndex = index;
                slotButton.onClick.AddListener(() => OnInventorySlotClicked(capturedIndex));
            }
        }
        else
        {
            slotImage.sprite = emptyInventorySlotSprite;
            slotImage.color = new Color(1, 1, 1, 0.3f);
            
            if (slotButton != null)
            {
                slotButton.interactable = false;
            }
        }
    }

    /// <summary>
    /// 인벤토리 슬롯 클릭 처리
    /// </summary>
    private void OnInventorySlotClicked(int index)
    {
        if (characterInventory == null || characterInventory.sharedSlotData200 == null) return;
        if (index >= characterInventory.sharedSlotData200.Length) return;

        CharacterData clickedData = characterInventory.sharedSlotData200[index];
        if (clickedData == null) return;

        Debug.Log($"[DeckPanelManager] 인벤토리 슬롯 {index} 클릭: {clickedData.characterName}");

        // 빈 덱 슬롯 찾기
        int emptySlotIndex = FindEmptyDeckSlot();
        if (emptySlotIndex != -1)
        {
            // 종족 제한 체크
            if (!CheckRaceRestriction(clickedData, emptySlotIndex))
            {
                ShowWarning($"{clickedData.race} 종족은 이미 3명이 등록되어 있습니다!");
                return;
            }

            // 덱에 등록
            RegisterCharacterToDeck(clickedData, emptySlotIndex);
            
            // 인벤토리에서 제거
            characterInventory.sharedSlotData200[index] = null;
            UpdateInventorySlotUI(index, null);
        }
        else
        {
            ShowWarning("덱이 가득 찼습니다!");
        }
    }

    /// <summary>
    /// 빈 덱 슬롯 찾기
    /// </summary>
    private int FindEmptyDeckSlot()
    {
        for (int i = 0; i < 10; i++)
        {
            if (registeredCharactersSet2[i] == null)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 종족 제한 체크 (첫 9칸은 각 종족 3명씩만)
    /// </summary>
    private bool CheckRaceRestriction(CharacterData data, int slotIndex)
    {
        // 10번째 슬롯(인덱스 9)은 자유
        if (slotIndex == 9) return true;

        // 종족별 카운트 계산
        UpdateRaceCount();

        // 해당 종족이 이미 3명이면 거부
        if (raceCountDict.ContainsKey(data.race) && raceCountDict[data.race] >= 3)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 캐릭터를 덱에 등록
    /// </summary>
    private void RegisterCharacterToDeck(CharacterData data, int slotIndex)
    {
        registeredCharactersSet2[slotIndex] = data;
        
        // UI 업데이트
        UpdateDeckSlotUI(slotIndex);
        
        // 종족 카운트 업데이트
        UpdateRaceCountUI();
        
        // 데이터베이스에 저장
        SaveToDatabase();
        
        Debug.Log($"[DeckPanelManager] {data.characterName}을(를) 슬롯 {slotIndex}에 등록");
    }

    /// <summary>
    /// 덱 슬롯 UI 업데이트
    /// </summary>
    private void UpdateDeckSlotUI(int index)
    {
        if (index >= registerSlotImages.Length) return;

        CharacterData data = registeredCharactersSet2[index];
        Image slotImage = registerSlotImages[index];
        TextMeshProUGUI levelText = (index < registerSlotLevelTexts.Length) ? registerSlotLevelTexts[index] : null;

        if (data != null)
        {
            slotImage.sprite = data.buttonIcon;
            slotImage.color = Color.white;
            
            if (levelText != null)
            {
                levelText.text = $"Lv.{data.level}";
                levelText.gameObject.SetActive(true);
            }
        }
        else
        {
            slotImage.sprite = emptyRegisterSlotSprite;
            slotImage.color = new Color(1, 1, 1, 0.3f);
            
            if (levelText != null)
            {
                levelText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 덱 등록 버튼 설정
    /// </summary>
    public void SetupRegisterButtons()
    {
        for (int i = 0; i < registerSlotButtons.Length && i < 10; i++)
        {
            if (registerSlotButtons[i] != null)
            {
                int capturedIndex = i;
                registerSlotButtons[i].onClick.RemoveAllListeners();
                registerSlotButtons[i].onClick.AddListener(() => OnDeckSlotClicked(capturedIndex));
            }
        }
    }

    /// <summary>
    /// 덱 슬롯 클릭 처리 (해제)
    /// </summary>
    private void OnDeckSlotClicked(int index)
    {
        if (index >= 10 || registeredCharactersSet2[index] == null) return;

        CharacterData data = registeredCharactersSet2[index];
        Debug.Log($"[DeckPanelManager] 덱 슬롯 {index} 클릭: {data.characterName} 해제");

        // 인벤토리에 빈 슬롯 찾기
        int emptyInventorySlot = FindEmptyInventorySlot();
        if (emptyInventorySlot != -1)
        {
            // 인벤토리로 이동
            characterInventory.sharedSlotData200[emptyInventorySlot] = data;
            UpdateInventorySlotUI(emptyInventorySlot, data);
            
            // 덱에서 제거
            registeredCharactersSet2[index] = null;
            UpdateDeckSlotUI(index);
            
            // 종족 카운트 업데이트
            UpdateRaceCountUI();
            
            // 데이터베이스에 저장
            SaveToDatabase();
        }
        else
        {
            ShowWarning("인벤토리가 가득 찼습니다!");
        }
    }

    /// <summary>
    /// 빈 인벤토리 슬롯 찾기
    /// </summary>
    private int FindEmptyInventorySlot()
    {
        if (characterInventory == null || characterInventory.sharedSlotData200 == null) return -1;

        for (int i = 0; i < characterInventory.sharedSlotData200.Length && i < 200; i++)
        {
            if (characterInventory.sharedSlotData200[i] == null)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 자동 등록 기능 (종족별 3명씩)
    /// </summary>
    private void AutoFillDeckByRuleRule()
    {
        Debug.Log("[DeckPanelManager] 자동 등록 시작");

        // 현재 덱 초기화
        for (int i = 0; i < 9; i++)
        {
            if (registeredCharactersSet2[i] != null)
            {
                // 인벤토리로 반환
                int emptySlot = FindEmptyInventorySlot();
                if (emptySlot != -1)
                {
                    characterInventory.sharedSlotData200[emptySlot] = registeredCharactersSet2[i];
                }
                registeredCharactersSet2[i] = null;
            }
        }

        // 종족별로 캐릭터 분류
        Dictionary<CharacterRace, List<CharacterData>> raceGroups = new Dictionary<CharacterRace, List<CharacterData>>();
        raceGroups[CharacterRace.Human] = new List<CharacterData>();
        raceGroups[CharacterRace.Orc] = new List<CharacterData>();
        raceGroups[CharacterRace.Elf] = new List<CharacterData>();

        // 인벤토리에서 종족별로 수집
        for (int i = 0; i < characterInventory.sharedSlotData200.Length && i < 200; i++)
        {
            CharacterData data = characterInventory.sharedSlotData200[i];
            if (data != null)
            {
                if (data.race == CharacterRace.Human || data.race == CharacterRace.Orc || data.race == CharacterRace.Elf)
                {
                    raceGroups[data.race].Add(data);
                }
            }
        }

        // 각 종족별로 3명씩 등록
        int slotIndex = 0;
        foreach (var race in new[] { CharacterRace.Human, CharacterRace.Orc, CharacterRace.Elf })
        {
            var characters = raceGroups[race].OrderByDescending(c => c.level).Take(3).ToList();
            
            foreach (var character in characters)
            {
                if (slotIndex < 9)
                {
                    // 덱에 등록
                    registeredCharactersSet2[slotIndex] = character;
                    
                    // 인벤토리에서 제거
                    for (int i = 0; i < characterInventory.sharedSlotData200.Length; i++)
                    {
                        if (characterInventory.sharedSlotData200[i] == character)
                        {
                            characterInventory.sharedSlotData200[i] = null;
                            break;
                        }
                    }
                    
                    slotIndex++;
                }
            }
        }

        // UI 갱신
        RefreshInventoryUI();
        InitRegisterSlotsVisual();
        UpdateRaceCountUI();
        SaveToDatabase();

        Debug.Log("[DeckPanelManager] 자동 등록 완료");
    }

    /// <summary>
    /// 종족별 카운트 계산
    /// </summary>
    private void UpdateRaceCount()
    {
        raceCountDict.Clear();
        raceCountDict[CharacterRace.Human] = 0;
        raceCountDict[CharacterRace.Orc] = 0;
        raceCountDict[CharacterRace.Elf] = 0;

        // 첫 9칸만 카운트
        for (int i = 0; i < 9; i++)
        {
            if (registeredCharactersSet2[i] != null)
            {
                CharacterRace race = registeredCharactersSet2[i].race;
                if (raceCountDict.ContainsKey(race))
                {
                    raceCountDict[race]++;
                }
            }
        }
    }

    /// <summary>
    /// 종족별 카운트 UI 업데이트
    /// </summary>
    private void UpdateRaceCountUI()
    {
        UpdateRaceCount();

        if (humanCountText != null)
            humanCountText.text = $"휴먼: {raceCountDict[CharacterRace.Human]}/3";
        
        if (orcCountText != null)
            orcCountText.text = $"오크: {raceCountDict[CharacterRace.Orc]}/3";
        
        if (elfCountText != null)
            elfCountText.text = $"엘프: {raceCountDict[CharacterRace.Elf]}/3";
    }

    /// <summary>
    /// 덱 슬롯 시각화 초기화
    /// </summary>
    public void InitRegisterSlotsVisual()
    {
        for (int i = 0; i < 10; i++)
        {
            UpdateDeckSlotUI(i);
        }
    }

    /// <summary>
    /// 경고 메시지 표시
    /// </summary>
    private void ShowWarning(string message)
    {
        if (raceRuleWarningText != null)
        {
            raceRuleWarningText.text = message;
            raceRuleWarningText.gameObject.SetActive(true);
            
            CancelInvoke(nameof(HideWarning));
            Invoke(nameof(HideWarning), warningDisplayTime);
        }
        
        Debug.LogWarning($"[DeckPanelManager] {message}");
    }

    private void HideWarning()
    {
        if (raceRuleWarningText != null)
        {
            raceRuleWarningText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 인벤토리 매니저에서 덱 정보 로드
    /// </summary>
    private void LoadDeckSlotsFromInventoryManager()
    {
        if (characterInventory == null) return;

        CharacterData[] savedDeck = characterInventory.GetRegisteredCharacters();
        if (savedDeck != null)
        {
            for (int i = 0; i < savedDeck.Length && i < 10; i++)
            {
                registeredCharactersSet2[i] = savedDeck[i];
            }
            
            Debug.Log("[DeckPanelManager] 저장된 덱 정보 로드 완료");
        }
    }

    /// <summary>
    /// 데이터베이스에 저장
    /// </summary>
    private void SaveToDatabase()
    {
        // CharacterInventoryManager에 저장
        if (characterInventory != null)
        {
            characterInventory.SaveRegisteredCharacters(registeredCharactersSet2);
        }

        // CharacterDatabase에 저장
        if (characterDB != null)
        {
            characterDB.currentRegisteredCharacters = new CharacterData[10];
            for (int i = 0; i < 10; i++)
            {
                characterDB.currentRegisteredCharacters[i] = registeredCharactersSet2[i];
            }
        }

        // CharacterDatabaseObject에 저장
        if (characterDBObject != null)
        {
            characterDBObject.currentRegisteredCharacters = new CharacterData[10];
            for (int i = 0; i < 10; i++)
            {
                characterDBObject.currentRegisteredCharacters[i] = registeredCharactersSet2[i];
            }
        }

        // GameManager에도 전달
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentRegisteredCharacters = new CharacterData[10];
            for (int i = 0; i < 10; i++)
            {
                GameManager.Instance.currentRegisteredCharacters[i] = registeredCharactersSet2[i];
            }
        }

        Debug.Log("[DeckPanelManager] 덱 정보 저장 완료");
    }

    /// <summary>
    /// 외부에서 등록된 캐릭터 가져오기
    /// </summary>
    public CharacterData[] GetRegisteredCharacters()
    {
        return registeredCharactersSet2;
    }

    /// <summary>
    /// 특정 슬롯의 캐릭터 가져오기
    /// </summary>
    public CharacterData GetCharacterAtSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 10)
        {
            return registeredCharactersSet2[slotIndex];
        }
        return null;
    }

    private void OnDisable()
    {
        // 패널이 비활성화될 때 경고 숨기기
        HideWarning();
    }
}