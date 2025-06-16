using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using Unity.VisualScripting;

public class DeckPanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager (인벤토리)")]
    [SerializeField] private CharacterInventoryManager characterInventory;
    
    // ================================
    // [추가] 캐릭터 데이터베이스 참조 추가
    // ================================
    [Header("캐릭터 데이터베이스")]
    [Tooltip("캐릭터 데이터베이스(모든 캐릭터 정보가 있는 ScriptableObject)")]
    [SerializeField] private CharacterDatabase characterDB;
    
    [Tooltip("캐릭터 데이터베이스 객체(선택사항)")]
    [SerializeField] private CharacterDatabaseObject characterDBObject;

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

    [Header("잘못된 종족 등록 시 경고 텍스트")]
    [Tooltip("첫 9칸에 휴먼/오크/엘프 외 종족이거나 이미 3명을 넘을 때 보여줄 경고 Text")]
    [SerializeField] private TextMeshProUGUI raceRuleWarningText;

    [Tooltip("경고 텍스트를 몇 초간 표시할지")]
    [SerializeField] private float warningDisplayTime = 3f;

    private void OnEnable()
    {
        // 패널이 켜질 때, 업그레이드 모드를 무조건 해제
        isUpgradeMode = false;
        
        // [추가] 데이터베이스 체크
        CheckDatabases();

        // 인벤토리(200칸) 즉시 갱신
        RefreshInventoryUI();

        // 덱 등록 버튼 초기화
        SetupRegisterButtons();

        // (추가됨) 저장된 덱 정보를 registeredCharactersSet2에 로드
        LoadDeckSlotsFromInventoryManager();  // 기존 코드 유지

        // ================================
        // === [수정] 자동 등록 코드 제거 (기존 OnEnable에서 호출되던 부분) ===
        //     => 이제는 "버튼"으로만 작동하게 변경
        // ================================
        // AutoFillDeckByRuleRule();   // (제거)

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
    
    // [추가] 시작 시 데이터베이스 초기화 확인
    private void Start()
    {
        // 시작 시 데이터베이스가 설정되어 있는지 확인
        CheckDatabases();
        
        // 데이터베이스 정보 출력
        if (characterDB != null)
        {
            Debug.Log($"[DeckPanelManager] Start: 캐릭터 데이터베이스 설정됨. 등록된 캐릭터 수: {(characterDB.currentRegisteredCharacters != null ? characterDB.currentRegisteredCharacters.Length : 0)}");
        }
        else if (characterDBObject != null)
        {
            Debug.Log($"[DeckPanelManager] Start: 캐릭터 데이터베이스 객체 설정됨. 등록된 캐릭터 수: {(characterDBObject.characters != null ? characterDBObject.characters.Length : 0)}");
        }
        else
        {
            Debug.LogWarning("[DeckPanelManager] Start: 캐릭터 데이터베이스가 설정되지 않았습니다!");
        }
    }
    
    // [추가] 데이터베이스 체크 및 초기화
    private void CheckDatabases()
    {
        if (characterDB == null && characterDBObject == null)
        {
            Debug.LogWarning("[DeckPanelManager] 데이터베이스가 설정되지 않았습니다. 캐릭터DB를 찾아보겠습니다.");
            
            // 기존 캐릭터 데이터베이스 찾기 시도
            characterDB = FindFirstObjectByType<CharacterDatabase>();
            
            if (characterDB != null)
            {
                Debug.Log($"[DeckPanelManager] 캐릭터 데이터베이스를 찾았습니다: {characterDB.name}");
            }
            else
            {
                // 다른 방법으로 찾기 시도
                var dbObjects = FindObjectsByType<CharacterDatabaseObject>(FindObjectsSortMode.None);
                if (dbObjects != null && dbObjects.Length > 0)
                {
                    characterDBObject = dbObjects[0];
                    Debug.Log($"[DeckPanelManager] 캐릭터 데이터베이스 객체를 찾았습니다: {characterDBObject.name}");
                }
                else
                {
                    Debug.LogError("[DeckPanelManager] 캐릭터 데이터베이스를 찾을 수 없습니다. 캐릭터 DB 정보에 접근할 수 없습니다.");
                }
            }
        }
        else
        {
            Debug.Log($"[DeckPanelManager] 데이터베이스 참조 확인: DB={characterDB != null}, DBObject={characterDBObject != null}");
        }
    }

    // -----------------------------------------------------
    // (추가) 휴먼3, 오크3, 엘프3 + (10번째 아무 종족) 자동 등록 함수
    // -----------------------------------------------------
    private void AutoFillDeckByRaceRule()
    {
        if (characterInventory == null)
        {
            Debug.LogError("[DeckPanelManager] characterInventory가 null이라 자동 등록 불가");
            return;
        }

        // 명확한 단계 표시를 위한 로그
        Debug.Log("[DeckPanelManager] ====== 자동 덱 구성 시작 (빈 슬롯만 채우기) ======");
        
        // 데이터베이스 확인
        CheckDatabases();

        // 1) 인벤토리에 있는 캐릭터들(Owned) 전부 가져오기
        List<CharacterData> availableChars = characterInventory.GetOwnedCharacters();
        if (availableChars == null || availableChars.Count == 0)
        {
            Debug.LogError("[DeckPanelManager] 인벤토리에 캐릭터가 없습니다. (자동등록 불가)");
            return;
        }
        
        // 2) 현재 덱에 있는 캐릭터 확인 및 종족별 카운트
        int humanCount = 0;
        int orcCount = 0;
        int elfCount = 0;
        HashSet<CharacterData> usedChars = new HashSet<CharacterData>();
        
        // 빈 슬롯 목록 저장
        List<int> emptySlots = new List<int>();
        
        Debug.Log("[DeckPanelManager] 1단계: 현재 덱 상태 확인");
        for (int i = 0; i < 10; i++)
        {
            if (registeredCharactersSet2[i] != null)
            {
                usedChars.Add(registeredCharactersSet2[i]);
                
                // 종족 정보 확인 (DB 기준)
                CharacterData originalData = FindCharacterInDatabase(registeredCharactersSet2[i].characterName);
                CharacterRace raceToCheck = (originalData != null) ? originalData.race : registeredCharactersSet2[i].race;
                
                // 첫 9개 슬롯에서만 종족 카운트 추적
                if (i < 9)
                {
                    switch (raceToCheck)
                    {
                        case CharacterRace.Human:
                            humanCount++;
                            break;
                        case CharacterRace.Orc:
                            orcCount++;
                            break;
                        case CharacterRace.Elf:
                            elfCount++;
                            break;
                    }
                }
                
                Debug.Log($"[DeckPanelManager] 덱 슬롯[{i}]에 '{registeredCharactersSet2[i].characterName}' (종족:{raceToCheck}, Lv.{registeredCharactersSet2[i].level}) 이미 등록됨");
            }
            else
            {
                emptySlots.Add(i);
                Debug.Log($"[DeckPanelManager] 덱 슬롯[{i}] 비어있음 - 채울 예정");
            }
        }
        
        Debug.Log($"[DeckPanelManager] 현재 종족별 카운트: 휴먼={humanCount}/3, 오크={orcCount}/3, 엘프={elfCount}/3, 빈 슬롯={emptySlots.Count}개");
        
        // 빈 슬롯이 없으면 종료
        if (emptySlots.Count == 0)
        {
            Debug.Log("[DeckPanelManager] 모든 슬롯이 이미 채워져 있습니다. 자동 등록 종료.");
            return;
        }
        
        // 3) 사용 가능한 캐릭터(덱에 없는)를 종족별로 분류
        List<CharacterData> availableHumans = new List<CharacterData>();
        List<CharacterData> availableOrcs = new List<CharacterData>();
        List<CharacterData> availableElves = new List<CharacterData>();
        List<CharacterData> availableOthers = new List<CharacterData>();
        
        foreach (var character in availableChars)
        {
            // 이미 덱에 있는 캐릭터는 제외
            if (usedChars.Contains(character)) continue;
            
            // DB에서 종족 정보 확인
            CharacterData originalData = FindCharacterInDatabase(character.characterName);
            CharacterRace raceToCheck = (originalData != null) ? originalData.race : character.race;
            
            switch (raceToCheck)
            {
                case CharacterRace.Human:
                    availableHumans.Add(character);
                    break;
                case CharacterRace.Orc:
                    availableOrcs.Add(character);
                    break;
                case CharacterRace.Elf:
                    availableElves.Add(character);
                    break;
                default:
                    availableOthers.Add(character);
                    break;
            }
        }
        
        // 레벨 내림차순으로 정렬
        availableHumans.Sort((a, b) => b.level.CompareTo(a.level));
        availableOrcs.Sort((a, b) => b.level.CompareTo(a.level));
        availableElves.Sort((a, b) => b.level.CompareTo(a.level));
        availableOthers.Sort((a, b) => b.level.CompareTo(a.level));
        
        Debug.Log($"[DeckPanelManager] 2단계: 사용 가능한 캐릭터 - 휴먼:{availableHumans.Count}, 오크:{availableOrcs.Count}, 엘프:{availableElves.Count}, 기타:{availableOthers.Count}");
        
        // 4) 빈 슬롯을 채우기 (첫 9칸은 종족 규칙에 맞게)
        List<CharacterData> selectedChars = new List<CharacterData>();
        
        foreach (int slotIndex in emptySlots)
        {
            // 첫 9칸은 휴먼/오크/엘프만, 종족별 최대 3명까지
            if (slotIndex < 9)
            {
                // 부족한 휴먼 채우기
                if (humanCount < 3 && availableHumans.Count > 0)
                {
                    CharacterData selectedChar = availableHumans[0];
                    availableHumans.RemoveAt(0);
                    registeredCharactersSet2[slotIndex] = selectedChar;
                    selectedChars.Add(selectedChar);
                    humanCount++;
                    Debug.Log($"[DeckPanelManager] 덱 슬롯[{slotIndex}]에 휴먼 '{selectedChar.characterName}' (Lv.{selectedChar.level}) 추가");
                    continue;
                }
                
                // 부족한 오크 채우기
                if (orcCount < 3 && availableOrcs.Count > 0)
                {
                    CharacterData selectedChar = availableOrcs[0];
                    availableOrcs.RemoveAt(0);
                    registeredCharactersSet2[slotIndex] = selectedChar;
                    selectedChars.Add(selectedChar);
                    orcCount++;
                    Debug.Log($"[DeckPanelManager] 덱 슬롯[{slotIndex}]에 오크 '{selectedChar.characterName}' (Lv.{selectedChar.level}) 추가");
                    continue;
                }
                
                // 부족한 엘프 채우기
                if (elfCount < 3 && availableElves.Count > 0)
                {
                    CharacterData selectedChar = availableElves[0];
                    availableElves.RemoveAt(0);
                    registeredCharactersSet2[slotIndex] = selectedChar;
                    selectedChars.Add(selectedChar);
                    elfCount++;
                    Debug.Log($"[DeckPanelManager] 덱 슬롯[{slotIndex}]에 엘프 '{selectedChar.characterName}' (Lv.{selectedChar.level}) 추가");
                    continue;
                }
                
                // 3종족 모두 다 찼거나 없는 경우 기타 종족으로 채우기
                if (availableOthers.Count > 0)
                {
                    CharacterData selectedChar = availableOthers[0];
                    availableOthers.RemoveAt(0);
                    registeredCharactersSet2[slotIndex] = selectedChar;
                    selectedChars.Add(selectedChar);
                    Debug.Log($"[DeckPanelManager] 덱 슬롯[{slotIndex}]에 기타종족 '{selectedChar.characterName}' (Lv.{selectedChar.level}) 추가");
                    continue;
                }
            }
            else
            {
                // 10번째 슬롯(index=9)은 아무 종족이나 배치
                // 가장 레벨 높은 캐릭터 순으로 시도
                List<CharacterData> allRemaining = new List<CharacterData>();
                allRemaining.AddRange(availableHumans);
                allRemaining.AddRange(availableOrcs);
                allRemaining.AddRange(availableElves);
                allRemaining.AddRange(availableOthers);
                allRemaining.Sort((a, b) => b.level.CompareTo(a.level));
                
                if (allRemaining.Count > 0)
                {
                    CharacterData selectedChar = allRemaining[0];
                    registeredCharactersSet2[slotIndex] = selectedChar;
                    selectedChars.Add(selectedChar);
                    Debug.Log($"[DeckPanelManager] 덱 슬롯[{slotIndex}]에 '{selectedChar.characterName}' (Lv.{selectedChar.level}) 추가 (10번째 슬롯)");
                }
                else
                {
                    Debug.LogWarning($"[DeckPanelManager] 슬롯[{slotIndex}]에 넣을 캐릭터가 없습니다. 빈 슬롯으로 남겨둡니다.");
                }
            }
        }
        
        // 5) 새로 선택된 캐릭터만 덱으로 이동
        Debug.Log($"[DeckPanelManager] 3단계: 새로 선택된 {selectedChars.Count}개 캐릭터를 덱으로 이동");
        foreach (var character in selectedChars)
        {
            if (character != null)
            {
                try {
                    characterInventory.RemoveFromInventory(character);
                    Debug.Log($"[DeckPanelManager] 인벤토리에서 제거: {character.characterName}");
                    
                    characterInventory.MoveToDeck(character);
                    Debug.Log($"[DeckPanelManager] 덱으로 이동: {character.characterName}");
                } catch (System.Exception e) {
                    Debug.LogWarning($"[DeckPanelManager] 덱으로 이동 중 오류: {character.characterName}, 오류: {e.Message}");
                }
            }
        }
        
        // 6) 변경 사항 저장
        characterInventory.SaveCharacters();
        Debug.Log("[DeckPanelManager] 캐릭터 상태 저장 완료");
        
        // 7) 덱 최종 상태 확인
        Debug.Log("[DeckPanelManager] 4단계: 최종 덱 상태");
        int validCount = 0;
        for (int i = 0; i < 10; i++) {
            if (registeredCharactersSet2[i] != null) {
                validCount++;
                CharacterData originalData = FindCharacterInDatabase(registeredCharactersSet2[i].characterName);
                CharacterRace raceToCheck = (originalData != null) ? originalData.race : registeredCharactersSet2[i].race;
                Debug.Log($"[DeckPanelManager] 최종 등록: 슬롯[{i}] = {registeredCharactersSet2[i].characterName} (종족:{raceToCheck}, Lv.{registeredCharactersSet2[i].level})");
            } else {
                Debug.Log($"[DeckPanelManager] 최종 등록: 슬롯[{i}] = null (비어있음)");
            }
        }
        
        Debug.Log($"[DeckPanelManager] ====== 자동 덱 구성 완료 - 덱에 총 {validCount}개 캐릭터 등록됨 ======");
    }
    
    // [추가] 캐릭터 데이터베이스에서 이름으로 캐릭터 찾기
    private CharacterData FindCharacterInDatabase(string characterName)
    {
        // 1. CharacterDatabase에서 찾기
        if (characterDB != null && characterDB.currentRegisteredCharacters != null)
        {
            foreach (var character in characterDB.currentRegisteredCharacters)
            {
                if (character != null && character.characterName == characterName)
                {
                    return character;
                }
            }
        }
        
        // 2. CharacterDatabaseObject에서 찾기
        if (characterDBObject != null && characterDBObject.characters != null)
        {
            foreach (var character in characterDBObject.characters)
            {
                if (character != null && character.characterName == characterName)
                {
                    return character;
                }
            }
        }
        
        // 못 찾은 경우
        return null;
    }
    
    // 등록된 캐릭터들을 CharacterInventoryManager의 덱으로 이동
    private void MoveRegisteredCharactersToDeck()
    {
        if (characterInventory == null) {
            Debug.LogError("[DeckPanelManager] characterInventory가 null이라 덱 이동 불가");
            return;
        }
        
        // 1) 현재 덱에 있는 캐릭터들을 모두 인벤토리로 되돌림
        var currentDeckChars = characterInventory.GetDeckCharacters();
        Debug.Log($"[DeckPanelManager] 현재 덱에 있는 캐릭터 수: {currentDeckChars.Count}");
        
        foreach (var character in currentDeckChars)
        {
            if (character != null) {
                Debug.Log($"[DeckPanelManager] 덱에서 인벤토리로 이동: {character.characterName}");
                characterInventory.RemoveFromDeck(character);
                characterInventory.AddToInventory(character);
            }
        }
        
        // 2) registeredCharactersSet2에 있는 캐릭터들을 덱으로 이동
        int movedCount = 0;
        for (int i = 0; i < 10; i++)
        {
            if (registeredCharactersSet2[i] != null)
            {
                Debug.Log($"[DeckPanelManager] 인벤토리에서 덱으로 이동 시도: 슬롯[{i}] {registeredCharactersSet2[i].characterName}");
                
                // 인벤토리에서 제거 및 덱으로 이동
                try {
                    characterInventory.RemoveFromInventory(registeredCharactersSet2[i]);
                    Debug.Log($"[DeckPanelManager] 인벤토리에서 제거: {registeredCharactersSet2[i].characterName}");
                } catch (System.Exception e) {
                    Debug.LogWarning($"[DeckPanelManager] 인벤토리에서 제거 실패: {registeredCharactersSet2[i].characterName}, 오류: {e.Message}");
                }
                
                try {
                    characterInventory.MoveToDeck(registeredCharactersSet2[i]);
                    Debug.Log($"[DeckPanelManager] 덱으로 이동: {registeredCharactersSet2[i].characterName}");
                    movedCount++;
                } catch (System.Exception e) {
                    Debug.LogWarning($"[DeckPanelManager] 덱으로 이동 실패: {registeredCharactersSet2[i].characterName}, 오류: {e.Message}");
                }
            }
        }
        
        Debug.Log($"[DeckPanelManager] 덱으로 이동 완료: 총 {movedCount}개 캐릭터 이동됨");
        
        // 3) 변경 사항 저장
        characterInventory.SaveCharacters();
        Debug.Log("[DeckPanelManager] 캐릭터 상태 저장 완료");
        
        // 4) 덱에 있는 최종 캐릭터 확인
        var finalDeckChars = characterInventory.GetDeckCharacters();
        Debug.Log($"[DeckPanelManager] 최종 덱 내용 ({finalDeckChars.Count}개):");
        for (int i = 0; i < finalDeckChars.Count; i++) {
            Debug.Log($"  - {i+1}. {finalDeckChars[i].characterName} (Lv.{finalDeckChars[i].level})");
        }
    }

    // =====================================================================
    // (1) 인벤토리 표시 => CharacterInventoryManager의 sharedSlotData200 활용
    // =====================================================================
    public void RefreshInventoryUI()
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[DeckPanelManager] characterInventory가 연결되지 않음!");
            return;
        }
        
        int uiSlotCount = (inventorySlotImages != null) ? inventorySlotImages.Length : 0;
        if (uiSlotCount < 200)
        {
            Debug.LogWarning($"[DeckPanelManager] inventorySlotImages가 충분하지 않음! (현재: {uiSlotCount}개, 필요: 200개)");
        }
        if (inventorySlotButtons == null || inventorySlotButtons.Length < uiSlotCount)
        {
            Debug.LogWarning($"[DeckPanelManager] inventorySlotButtons가 충분하지 않음! (현재: {(inventorySlotButtons != null ? inventorySlotButtons.Length : 0)}개, 필요: {uiSlotCount}개)");
            return;
        }

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
                slotImg.sprite = (cData.buttonIcon != null) ? cData.buttonIcon : emptyInventorySlotSprite;
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

    private void OnClickInventoryCharacter(CharacterData data, int inventorySlotIndex)
    {
        if (isUpgradeMode)
        {
            // 업그레이드 패널에 재료로 전달
            UpgradePanelManager upm = FindFirstObjectByType<UpgradePanelManager>();
            if (upm != null)
            {
                // SetFeedFromInventory 메서드가 없어서 컴파일 오류 발생
                // 주석 처리하고 로그만 남김
                // upm.SetFeedFromInventory(inventorySlotIndex, data);
                Debug.Log($"[DeckPanelManager] 업그레이드 재료 선택됨: {data.characterName}, index={inventorySlotIndex}");
                Debug.LogWarning("[DeckPanelManager] UpgradePanelManager에 SetFeedFromInventory 메서드가 없습니다. 구현이 필요합니다.");
            }
        }
        else
        {
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
            slotImg.sprite = emptyDeckSlotSprite;
            if (lvlText != null) lvlText.text = "";
        }
        else
        {
            slotImg.sprite = (cData.buttonIcon != null) ? cData.buttonIcon : emptyDeckSlotSprite;
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

        // [수정] 데이터베이스에서 종족 정보 확인
        CharacterData originalData = FindCharacterInDatabase(selectedCharacterForRegistration.characterName);
        CharacterRace raceToCheck = (originalData != null) ? originalData.race : selectedCharacterForRegistration.race;
        
        // 종족 정보 로그 출력
        Debug.Log($"[DeckPanelManager] 등록 시도 캐릭터: {selectedCharacterForRegistration.characterName}, 종족: {raceToCheck}");
        
        if (slotIndex < 9)
        {
            if (!IsAllowedRace(raceToCheck))
            {
                StartCoroutine(ShowRaceRuleWarning("첫 9칸에는 휴먼, 오크, 엘프만 가능합니다!"));
                return;
            }

            int sameRaceCount = 0;
            for (int i = 0; i < 9; i++)
            {
                var c = registeredCharactersSet2[i];
                if (c != null)
                {
                    // [수정] 등록된 캐릭터도 데이터베이스 종족 정보 확인
                    CharacterData regOriginalData = FindCharacterInDatabase(c.characterName);
                    CharacterRace regRace = (regOriginalData != null) ? regOriginalData.race : c.race;
                    
                    if (regRace == raceToCheck)
                    {
                        sameRaceCount++;
                    }
                }
            }
            if (sameRaceCount >= 3)
            {
                StartCoroutine(ShowRaceRuleWarning($"이미 해당 종족({raceToCheck})은 3명을 초과했습니다!"));
                return;
            }
        }

        CharacterData existingChar = registeredCharactersSet2[slotIndex];
        if (existingChar == null)
        {
            characterInventory.RemoveFromInventory(selectedCharacterForRegistration);
            characterInventory.MoveToDeck(selectedCharacterForRegistration);
            registeredCharactersSet2[slotIndex] = selectedCharacterForRegistration;
        }
        else
        {
            characterInventory.RemoveFromDeck(existingChar);
            characterInventory.AddToInventory(existingChar);

            characterInventory.RemoveFromInventory(selectedCharacterForRegistration);
            characterInventory.MoveToDeck(selectedCharacterForRegistration);

            registeredCharactersSet2[slotIndex] = selectedCharacterForRegistration;
        }

        selectedCharacterForRegistration = null;
        selectedInventorySlotIndex = -1;

        UpdateRegisterSlotVisual(slotIndex);
        RefreshInventoryUI();
        characterInventory.SaveCharacters();

        UpgradePanelManager upm = FindFirstObjectByType<UpgradePanelManager>();
        if (upm != null)
        {
            upm.RefreshDisplay();
            upm.SetUpgradeRegisteredSlotsFromDeck();
        }
    }

    private bool IsAllowedRace(CharacterRace race)
    {
        return (race == CharacterRace.Human
             || race == CharacterRace.Orc
             || race == CharacterRace.Elf);
    }

    private IEnumerator ShowRaceRuleWarning(string message)
    {
        if (raceRuleWarningText != null)
        {
            raceRuleWarningText.text = message;
            raceRuleWarningText.gameObject.SetActive(true);

            yield return new WaitForSeconds(warningDisplayTime);

            raceRuleWarningText.gameObject.SetActive(false);
        }
    }

    // =====================================================================
    // (3) LoadDeckSlotsFromInventoryManager() / Deck 저장/불러오기
    // =====================================================================
    private void LoadDeckSlotsFromInventoryManager()
    {
        if (characterInventory == null) return;
        var deckList = characterInventory.GetDeckCharacters(); 
        for (int i = 0; i < 10; i++)
        {
            if (i < deckList.Count)
                registeredCharactersSet2[i] = deckList[i];
            else
                registeredCharactersSet2[i] = null;
        }
    }

    // =====================================================================
    // (4) 버튼으로 인벤토리->덱 등록 (일괄처리)
    // =====================================================================
    public void OnClickUse4CardsAtRandom()
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[DeckPanelManager] characterInventory가 null이라 작업 불가");
            return;
        }

        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        List<Tile> validTiles = new List<Tile>();
        foreach (Tile t in allTiles)
        {
            if (t != null && !t.isRegion2)
            {
                if ((t.IsPlacable() || t.IsPlaceTile()))
                {
                    validTiles.Add(t);
                }
            }
        }

        if (validTiles.Count == 0)
        {
            Debug.LogWarning("[DeckPanelManager] 지역1에 배치할 수 있는 placable/placeTile이 없습니다!");
            return;
        }

        Debug.Log($"[DeckPanelManager] OnClickUse4CardsAtRandom() -> 지역1의 배치가능 타일 {validTiles.Count}개 중 랜덤으로 4장 소환 시도.");
    }

    // ----------------------------
    // [수정추가] "버튼" 누르면 실행
    // ----------------------------
    public void OnClickAutoFillDeckByRaceRule()
    {
        Debug.Log("[DeckPanelManager] '종족 규칙대로 자동등록' 버튼 클릭됨");
        
        // 1. 자동 덱 구성 실행
        AutoFillDeckByRaceRule();
        
        // 2. UI 갱신 (명확하게 분리)
        Debug.Log("[DeckPanelManager] UI 갱신 시작");
        
        // 덱 슬롯 갱신
        InitRegisterSlotsVisual();
        
        // 인벤토리 UI 갱신
        RefreshInventoryUI();
        
        // ▼▼ [수정추가] 업그레이드 패널도 자동 갱신 ▼▼
        UpgradePanelManager upm = FindFirstObjectByType<UpgradePanelManager>();
        if (upm != null)
        {
            upm.SetUpgradeRegisteredSlotsFromDeck();
            upm.RefreshDisplay();
        }
        // ▲▲ [수정끝] ▲▲
        
        Debug.Log("[DeckPanelManager] UI 갱신 완료");
        Debug.Log("[DeckPanelManager] '종족 규칙대로 자동등록' 버튼 처리 완료");
    }

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
        
        int slotCount = prevSharedSlotData200.Length;
        for (int i = 0; i < slotCount; i++)
        {
            if (characterInventory != null && 
                characterInventory.sharedSlotData200 != null && 
                i < characterInventory.sharedSlotData200.Length && 
                i < prevSharedSlotData200.Length && 
                characterInventory.sharedSlotData200[i] != prevSharedSlotData200[i])
            {
                characterInventory.CondenseAndReorderSharedSlots();
                characterInventory.SyncOwnedFromSharedSlots();

                RefreshInventoryUI();

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
}




