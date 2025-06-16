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

    // ============================
    // 종족 규칙 체크 관련
    // ============================
    [Header("종족 규칙 경고 표시")]
    [Tooltip("종족 규칙(휴먼3, 오크3, 엘프3 + 1)에 맞지 않으면 잠시 표시될 텍스트")]
    [SerializeField] private TextMeshProUGUI raceRuleWarningText;
    
    [Header("경고 표시 시간")]
    [SerializeField] private float warningDisplayTime = 3f;

    private CharacterData[] prevSharedSlotData200 = new CharacterData[200];

    [HideInInspector]
    public bool isUpgradeMode = false;

    private void Start()
    {
        if (characterInventory == null)
        {
            characterInventory = FindFirstObjectByType<CharacterInventoryManager>();
        }

        LoadDeckSlotsFromInventoryManager();
        SetupRegisterButtons();
        RefreshInventoryUI();
        InitRegisterSlotsVisual();

        MigrateDeckCharactersToInventory();
        
        // 데이터베이스 확인
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

    // 덱에 있는 캐릭터들을 인벤토리로 마이그레이션
    private void MigrateDeckCharactersToInventory()
    {
        if (characterInventory == null) return;
        
        var deckChars = characterInventory.GetDeckCharacters();
        if (deckChars.Count == 0) return;
        
        Debug.Log($"[DeckPanelManager] 덱에 {deckChars.Count}개 캐릭터 발견. 인벤토리로 이동 시작.");
        
        // 1) 기존 덱 캐릭터를 모두 인벤토리로 이동
        int movedCount = 0;
        for (int i = deckChars.Count - 1; i >= 0; i--)
        {
            var character = deckChars[i];
            if (character != null)
            {
                try {
                    characterInventory.RemoveFromDeck(character);
                    Debug.Log($"[DeckPanelManager] 덱에서 제거: {character.characterName}");
                } catch (System.Exception e) {
                    Debug.LogWarning($"[DeckPanelManager] 덱에서 제거 실패: {character.characterName}, 오류: {e.Message}");
                }
                
                try {
                    characterInventory.AddToInventory(character);
                    Debug.Log($"[DeckPanelManager] 인벤토리로 추가: {character.characterName}");
                    movedCount++;
                } catch (System.Exception e) {
                    Debug.LogWarning($"[DeckPanelManager] 인벤토리 추가 실패: {character.characterName}, 오류: {e.Message}");
                }
            }
        }
        
        Debug.Log($"[DeckPanelManager] 마이그레이션 완료: {movedCount}개 캐릭터를 인벤토리로 이동");
        
        // 2) registeredCharactersSet2 배열의 유효한 캐릭터들을 다시 덱으로 이동
        movedCount = 0;
        for (int i = 0; i < 10; i++)
        {
            if (registeredCharactersSet2[i] != null)
            {
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

        // 먼저 기존 캐릭터가 있으면 인벤토리로 되돌리기
        if (registeredCharactersSet2[slotIndex] != null)
        {
            characterInventory.RemoveFromDeck(registeredCharactersSet2[slotIndex]);
            characterInventory.AddToInventory(registeredCharactersSet2[slotIndex]);
            registeredCharactersSet2[slotIndex] = null;
        }

        // 새 캐릭터 등록
        registeredCharactersSet2[slotIndex] = selectedCharacterForRegistration;
        Debug.Log($"[DeckPanelManager] 등록 완료: 슬롯[{slotIndex}] = {selectedCharacterForRegistration.characterName} (종족:{raceToCheck})");

        // 종족 규칙 검사
        if (!CheckRaceRule())
        {
            StartCoroutine(ShowRaceRuleWarning(
                "종족 규칙에 맞지 않습니다!\n" + 
                "(Human 3, Orc 3, Elf 3 + 아무거나 1)\n" +
                "또는 Undead 같은 특수 종족은 덱에 넣을 수 없습니다."
            ));
        }

        // 인벤토리에서 덱으로 이동
        characterInventory.RemoveFromInventory(selectedCharacterForRegistration);
        characterInventory.MoveToDeck(selectedCharacterForRegistration);
        characterInventory.SaveCharacters();

        // UI 갱신
        UpdateRegisterSlotVisual(slotIndex);
        RefreshInventoryUI();

        // 업그레이드 패널 갱신
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
            if (t != null && !t.isRegion2)  // isRegion2 프로퍼티 사용
            {
                if ((t.IsPlacable() || t.IsPlaceTile()))  // IsPlacable 메서드 사용
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
            Debug.LogError("[DeckPanelManager] 인벤토리에 캐릭터가 없습니다.");
            return;
        }
        Debug.Log($"[DeckPanelManager] 인벤토리에서 {availableChars.Count}개 캐릭터 발견");

        // 2) 종족별로 캐릭터 분류
        List<CharacterData> humanList = new List<CharacterData>();
        List<CharacterData> orcList = new List<CharacterData>();
        List<CharacterData> elfList = new List<CharacterData>();
        List<CharacterData> etcList = new List<CharacterData>();

        foreach (var c in availableChars)
        {
            if (c == null) continue;
            
            // 데이터베이스에서 원본 종족 정보 확인
            CharacterData originalData = FindCharacterInDatabase(c.characterName);
            CharacterRace raceToCheck = (originalData != null) ? originalData.race : c.race;
            
            if (raceToCheck == CharacterRace.Human)
                humanList.Add(c);
            else if (raceToCheck == CharacterRace.Orc)
                orcList.Add(c);
            else if (raceToCheck == CharacterRace.Elf)
                elfList.Add(c);
            else
                etcList.Add(c);
        }

        Debug.Log($"[DeckPanelManager] 종족별 분류 - Human: {humanList.Count}, Orc: {orcList.Count}, Elf: {elfList.Count}, 기타: {etcList.Count}");

        // 3) 현재 덱 상태 확인 및 기존 캐릭터 보존
        Debug.Log("[DeckPanelManager] 현재 덱 상태 확인:");
        List<CharacterData> existingDeckChars = new List<CharacterData>();
        for (int i = 0; i < 10; i++)
        {
            if (registeredCharactersSet2[i] != null)
            {
                existingDeckChars.Add(registeredCharactersSet2[i]);
                Debug.Log($"[DeckPanelManager] 기존 덱 슬롯[{i}]: {registeredCharactersSet2[i].characterName}");
            }
        }

        // 4) 빈 슬롯만 채우기 위한 새 캐릭터 선택
        List<CharacterData> selectedChars = new List<CharacterData>();
        
        // 현재 덱에 있는 종족 수 계산
        int currentHumanCount = 0, currentOrcCount = 0, currentElfCount = 0, currentEtcCount = 0;
        foreach (var existing in existingDeckChars)
        {
            CharacterData originalData = FindCharacterInDatabase(existing.characterName);
            CharacterRace raceToCheck = (originalData != null) ? originalData.race : existing.race;
            
            if (raceToCheck == CharacterRace.Human) currentHumanCount++;
            else if (raceToCheck == CharacterRace.Orc) currentOrcCount++;
            else if (raceToCheck == CharacterRace.Elf) currentElfCount++;
            else currentEtcCount++;
        }

        Debug.Log($"[DeckPanelManager] 현재 덱의 종족 구성 - Human: {currentHumanCount}, Orc: {currentOrcCount}, Elf: {currentElfCount}, 기타: {currentEtcCount}");

        // 필요한 추가 캐릭터 계산
        int needHuman = Mathf.Max(0, 3 - currentHumanCount);
        int needOrc = Mathf.Max(0, 3 - currentOrcCount);
        int needElf = Mathf.Max(0, 3 - currentElfCount);
        int needEtc = Mathf.Max(0, 1 - currentEtcCount);
        
        // 총 필요한 캐릭터 수
        int totalNeeded = needHuman + needOrc + needElf + needEtc;
        int emptySlots = 10 - existingDeckChars.Count;
        
        Debug.Log($"[DeckPanelManager] 필요한 캐릭터 - Human: {needHuman}, Orc: {needOrc}, Elf: {needElf}, 기타: {needEtc}");
        Debug.Log($"[DeckPanelManager] 빈 슬롯: {emptySlots}, 필요한 캐릭터: {totalNeeded}");

        // 필요한 만큼만 선택 (빈 슬롯을 초과하지 않도록)
        if (totalNeeded > emptySlots)
        {
            Debug.LogWarning($"[DeckPanelManager] 필요한 캐릭터({totalNeeded})가 빈 슬롯({emptySlots})보다 많습니다. 일부만 채웁니다.");
        }

        // 휴먼 선택
        for (int i = 0; i < needHuman && i < humanList.Count && selectedChars.Count < emptySlots; i++)
        {
            selectedChars.Add(humanList[i]);
            Debug.Log($"[DeckPanelManager] Human 선택: {humanList[i].characterName}");
        }

        // 오크 선택
        for (int i = 0; i < needOrc && i < orcList.Count && selectedChars.Count < emptySlots; i++)
        {
            selectedChars.Add(orcList[i]);
            Debug.Log($"[DeckPanelManager] Orc 선택: {orcList[i].characterName}");
        }

        // 엘프 선택
        for (int i = 0; i < needElf && i < elfList.Count && selectedChars.Count < emptySlots; i++)
        {
            selectedChars.Add(elfList[i]);
            Debug.Log($"[DeckPanelManager] Elf 선택: {elfList[i].characterName}");
        }

        // 기타 종족 선택 (10번째 슬롯용)
        if (needEtc > 0 && etcList.Count > 0 && selectedChars.Count < emptySlots)
        {
            selectedChars.Add(etcList[0]);
            Debug.Log($"[DeckPanelManager] 기타 종족 선택: {etcList[0].characterName}");
        }
        else if (needEtc > 0 && selectedChars.Count < emptySlots)
        {
            // 기타 종족이 없으면 아무 종족이나 추가
            if (humanList.Count > needHuman)
                selectedChars.Add(humanList[needHuman]);
            else if (orcList.Count > needOrc)
                selectedChars.Add(orcList[needOrc]);
            else if (elfList.Count > needElf)
                selectedChars.Add(elfList[needElf]);
        }

        Debug.Log($"[DeckPanelManager] 1단계: 총 {selectedChars.Count}개 캐릭터 선택됨");

        // registeredCharactersSet2 배열 업데이트 (빈 슬롯만 채우기)
        Debug.Log("[DeckPanelManager] 2단계: 빈 슬롯에 캐릭터 배치");
        int selectedIndex = 0;
        for (int slotIndex = 0; slotIndex < 10; slotIndex++)
        {
            if (registeredCharactersSet2[slotIndex] == null && selectedIndex < selectedChars.Count)
            {
                CharacterData selectedChar = selectedChars[selectedIndex++];
                registeredCharactersSet2[slotIndex] = selectedChar;
                
                CharacterData originalData = FindCharacterInDatabase(selectedChar.characterName);
                CharacterRace raceToCheck = (originalData != null) ? originalData.race : selectedChar.race;
                
                if (slotIndex < 9)
                {
                    Debug.Log($"[DeckPanelManager] 슬롯[{slotIndex}] <- {selectedChar.characterName} (종족:{raceToCheck}, Lv.{selectedChar.level}) 추가");
                }
                else
                {
                    Debug.Log($"[DeckPanelManager] 슬롯[{slotIndex}] <- {selectedChar.characterName} (종족:{raceToCheck}, Lv.{selectedChar.level}) 추가 (10번째 슬롯)");
                }
            }
            else if (registeredCharactersSet2[slotIndex] == null)
            {
                Debug.LogWarning($"[DeckPanelManager] 슬롯[{slotIndex}]에 넣을 캐릭터가 없습니다. 빈 슬롯으로 남겨둡니다.");
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
        
        Debug.LogWarning($"[DeckPanelManager] 데이터베이스에서 '{characterName}' 캐릭터를 찾을 수 없습니다.");
        return null;
    }

    private bool CheckRaceRule()
    {
        int humanCnt = 0, orcCnt = 0, elfCnt = 0, etcCnt = 0;
        int allowedCnt = 0;

        for (int i = 0; i < 10; i++)
        {
            if (registeredCharactersSet2[i] == null) continue;
            
            // 데이터베이스에서 원본 종족 정보 확인
            CharacterData originalData = FindCharacterInDatabase(registeredCharactersSet2[i].characterName);
            CharacterRace raceToCheck = (originalData != null) ? originalData.race : registeredCharactersSet2[i].race;
            
            if (IsAllowedRace(raceToCheck))
            {
                allowedCnt++;
                
                if (raceToCheck == CharacterRace.Human)
                    humanCnt++;
                else if (raceToCheck == CharacterRace.Orc)
                    orcCnt++;
                else if (raceToCheck == CharacterRace.Elf)
                    elfCnt++;
            }
            else
            {
                etcCnt++;
            }
        }

        int total = humanCnt + orcCnt + elfCnt + etcCnt;
        
        if (total == 0)
            return true;

        // 규칙: 휴먼3, 오크3, 엘프3 + 아무거나1 or 특수 종족은 불가
        bool isValidByMainRule = (humanCnt <= 3 && orcCnt <= 3 && elfCnt <= 3 && total <= 10);
        bool hasNoForbidden = (allowedCnt == total);

        return isValidByMainRule && hasNoForbidden;
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