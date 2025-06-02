// Assets\OX UI Scripts\CharacterSelectUI.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// 캐릭터 선택 UI. 1~9 캐릭터 (또는 덱)에서 4장(Hand) + Next 1장 + Reserve 4장 로직을 다룸.
/// OnClickUse4CardsAtRandom()에서 '지역1'에 placable/placeTile이 꽉 찼을 경우 walkable에 배치하도록 수정함.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("Placement Manager 참조")]
    public PlacementManager placementManager; // 캐릭터 배치(소환) 담당

    [System.Serializable]
    public class SelectButton
    {
        public Button button;
        public Image iconImage;
        public TextMeshProUGUI costText;
        public int characterIndex;  // 0~8
        public bool isEmpty;
    }

    [Header("소환 버튼(4개)")]
    public SelectButton[] selectButtons = new SelectButton[4];

    [Header("Next Unit (다음 카드)")]
    public Image nextUnitImage;
    public TextMeshProUGUI nextUnitCost;

    // =========================================
    // 1~9 캐릭터(인덱스 0..8) 저장
    // =========================================
    private CharacterData[] deckFromLobby = new CharacterData[9];

    // "handIndices"(4개), "nextIndex"(1개), "reserveIndices"(4개)
    private int[] handIndices = new int[4];
    private int nextIndex = -1;
    private List<int> reserveIndices = new List<int>(4);

    private List<int> allIndices = new List<int>();

    // =========================================
    // [추가] 클릭으로 카드 하나가 선택 중이면,
    //        그 카드가 배치될 때까지 다른 클릭을 무시하기 위한 플래그
    // =========================================
    private bool hasPendingCard = false;   // 이미 카드 하나가 선택된 상태인지?
    private int pendingCardIndex = -1;     // 현재 선택된(아직 배치 안 된) 카드 인덱스

    // =======================================================================
    // (1) 인벤토리 표시 => CharacterInventoryManager의 sharedSlotData200 활용
    // =====================================================================

    [Header("CharacterInventoryManager (인벤토리)")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    // =========================================
    // [추가] 주사위 버튼 추가
    // =========================================
    [Header("자동 합성 버튼 (주사위)")]
    [SerializeField] private Button diceButton;

    private void Start()
    {
        // 1) 1~9 캐릭터
        if (GameManager.Instance != null &&
            GameManager.Instance.currentRegisteredCharacters != null &&
            GameManager.Instance.currentRegisteredCharacters.Length >= 9)
        {
            for (int i = 0; i < 9; i++)
            {
                deckFromLobby[i] = GameManager.Instance.currentRegisteredCharacters[i];
            }
            Debug.Log("[CharacterSelectUI] GameManager.currentRegisteredCharacters에서 9개 캐릭터 로드 완료");
        }
        else
        {
            Debug.LogError("[CharacterSelectUI] 1~9 캐릭터를 가져오지 못했습니다!");
            return;
        }

        // ▼▼ [수정추가] CharacterInventoryManager 자동 찾기 ▼▼
        if (characterInventory == null)
        {
            characterInventory = FindFirstObjectByType<CharacterInventoryManager>();
            if (characterInventory != null)
            {
                Debug.Log("[CharacterSelectUI] CharacterInventoryManager를 자동으로 찾았습니다.");
            }
            else
            {
                Debug.LogWarning("[CharacterSelectUI] CharacterInventoryManager를 찾을 수 없습니다. Inspector에서 수동으로 연결하거나 씬에 추가해주세요.");
            }
        }

        // ▼▼ [수정추가] PlacementManager 자동 찾기 ▼▼
        if (placementManager == null)
        {
            placementManager = PlacementManager.Instance;
            if (placementManager == null)
            {
                placementManager = FindFirstObjectByType<PlacementManager>();
            }
            if (placementManager != null)
            {
                Debug.Log("[CharacterSelectUI] PlacementManager를 자동으로 찾았습니다.");
            }
            else
            {
                Debug.LogWarning("[CharacterSelectUI] PlacementManager를 찾을 수 없습니다. Inspector에서 수동으로 연결하거나 씬에 추가해주세요.");
            }
        }
        // ▲▲ [수정추가 끝] ▲▲

        // 2) 인덱스(0..8) 셔플
        allIndices.Clear();
        for (int i = 0; i < 9; i++)
        {
            allIndices.Add(i);
        }
        ShuffleList(allIndices);

        // 3) 처음 4개 -> hand
        for (int i = 0; i < 4; i++)
        {
            handIndices[i] = allIndices[i];
        }

        // 4) 5번째 -> nextIndex
        nextIndex = allIndices[4];

        // 5) 나머지 4개 -> reserve
        reserveIndices.Clear();
        for (int i = 5; i < 9; i++)
        {
            reserveIndices.Add(allIndices[i]);
        }

        // UI 갱신
        UpdateHandButtons();
        UpdateNextUnitUI();

        // =========================================
        // [추가] 주사위 버튼 리스너 등록
        // =========================================
        if (diceButton != null)
        {
            diceButton.onClick.RemoveAllListeners();
            diceButton.onClick.AddListener(OnClickAutoMerge);
            Debug.Log("[CharacterSelectUI] 주사위 버튼 리스너 등록 완료");
        }
        else
        {
            Debug.LogWarning("[CharacterSelectUI] 주사위 버튼이 연결되지 않았습니다!");
        }
    }

    /// <summary>
    /// 주사위 버튼 클릭 시 호출되는 자동 합성 메서드
    /// </summary>
    private void OnClickAutoMerge()
    {
        Debug.Log("[CharacterSelectUI] 주사위 버튼 클릭 - 자동 합성 시작");

        // 모든 캐릭터 가져오기
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        // 지역1 캐릭터만 필터링 (areaIndex == 1)
        List<Character> area1Characters = new List<Character>();
        foreach (var character in allCharacters)
        {
            if (character != null && character.areaIndex == 1 && !character.isHero)
            {
                area1Characters.Add(character);
            }
        }

        // 1성 캐릭터만 필터링
        List<Character> oneStarCharacters = new List<Character>();
        foreach (var character in area1Characters)
        {
            if (character.star == CharacterStar.OneStar)
            {
                oneStarCharacters.Add(character);
            }
        }

        Debug.Log($"[CharacterSelectUI] 지역1 1성 캐릭터 수: {oneStarCharacters.Count}");

        // 3개 미만이면 합성 불가
        if (oneStarCharacters.Count < 3)
        {
            Debug.LogWarning("[CharacterSelectUI] 1성 캐릭터가 3개 미만이므로 합성할 수 없습니다.");
            return;
        }

        // 무작위로 3개 선택
        ShuffleList(oneStarCharacters);
        List<Character> selectedCharacters = oneStarCharacters.Take(3).ToList();

        // 기획서: 가장 뒤쪽 캐릭터 위치에 생성
        // 뒤쪽 = Y좌표가 가장 낮은 곳 (UI 좌표계에서)
        Character backMostCharacter = null;
        float lowestY = float.MaxValue;
        Tile targetTile = null;

        foreach (var character in selectedCharacters)
        {
            // UI 좌표계에서 Y값 확인
            RectTransform rect = character.GetComponent<RectTransform>();
            float y = rect != null ? rect.anchoredPosition.y : character.transform.position.y;
            
            if (y < lowestY)
            {
                lowestY = y;
                backMostCharacter = character;
                targetTile = character.currentTile;
            }
        }

        if (backMostCharacter == null || targetTile == null)
        {
            Debug.LogError("[CharacterSelectUI] 합성할 캐릭터나 타일을 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"[CharacterSelectUI] 합성 대상 3개: {selectedCharacters[0].characterName}, {selectedCharacters[1].characterName}, {selectedCharacters[2].characterName}");
        Debug.Log($"[CharacterSelectUI] 가장 뒤쪽 캐릭터: {backMostCharacter.characterName} (Y: {lowestY})");

        // 합성 실행
        ExecuteAutoMerge(selectedCharacters, targetTile);
    }

    /// <summary>
    /// 실제 합성을 실행하는 메서드
    /// </summary>
    private void ExecuteAutoMerge(List<Character> charactersToMerge, Tile targetTile)
    {
        // 첫 번째 캐릭터를 기준으로 정보 저장
        Character firstChar = charactersToMerge[0];
        int areaIndex = firstChar.areaIndex;
        Vector3 position = targetTile.transform.position;
        Transform parent = firstChar.transform.parent;

        // StarMergeDatabase에서 2성 캐릭터 데이터 가져오기
        var coreData = CoreDataManager.Instance;
        StarMergeDatabaseObject targetDB = (areaIndex == 2 && coreData.starMergeDatabaseRegion2 != null) 
            ? coreData.starMergeDatabaseRegion2 : coreData.starMergeDatabase;

        if (targetDB == null)
        {
            Debug.LogWarning("[CharacterSelectUI] StarMergeDatabase가 null입니다.");
            // 기본 합성 처리
            BasicMerge(charactersToMerge, targetTile);
            return;
        }

        // 랜덤 종족 선택 (3개 중 하나)
        RaceType selectedRace = (RaceType)charactersToMerge[Random.Range(0, 3)].race;
        CharacterData newCharData = targetDB.GetRandom2Star(selectedRace);

        if (newCharData == null || newCharData.spawnPrefab == null)
        {
            Debug.LogWarning($"[CharacterSelectUI] 2성 프리팹을 찾을 수 없습니다. 기본 합성으로 처리");
            BasicMerge(charactersToMerge, targetTile);
            return;
        }

        // 새로운 2성 캐릭터 생성
        GameObject newCharObj = Instantiate(newCharData.spawnPrefab, parent);
        if (newCharObj == null)
        {
            Debug.LogError("[CharacterSelectUI] 새 프리팹 생성 실패");
            return;
        }

        // 위치 설정
        RectTransform newCharRect = newCharObj.GetComponent<RectTransform>();
        if (newCharRect != null && parent != null)
        {
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                Vector2 localPos = parentRect.InverseTransformPoint(position);
                newCharRect.anchoredPosition = localPos;
                newCharRect.localRotation = Quaternion.identity;
            }
        }
        else
        {
            newCharObj.transform.position = position;
            newCharObj.transform.localRotation = Quaternion.identity;
        }

        // Character 컴포넌트 설정
        Character newCharacter = newCharObj.GetComponent<Character>();
        if (newCharacter != null)
        {
            // 기본 정보 설정
            newCharacter.currentTile = targetTile;
            newCharacter.areaIndex = areaIndex;
            newCharacter.isHero = false;
            newCharacter.isCharAttack = false;
            newCharacter.currentWaypointIndex = -1;
            newCharacter.maxWaypointIndex = 6;

            // 새로운 데이터 적용
            newCharacter.characterName = newCharData.characterName;
            newCharacter.race = newCharData.race;
            newCharacter.star = CharacterStar.TwoStar;

            // 2성 스탯 적용
            float statMultiplier = 1.3f;
            newCharacter.attackPower = newCharData.attackPower * statMultiplier;
            newCharacter.attackSpeed = newCharData.attackSpeed * 1.1f;
            newCharacter.attackRange = newCharData.attackRange * 1.1f;
            newCharacter.currentHP = newCharData.maxHP * statMultiplier;
            newCharacter.moveSpeed = newCharData.moveSpeed;

            // 별 비주얼 적용
            newCharacter.ApplyStarVisual();

            // 패널 설정
            if (coreData.bulletPanel != null)
            {
                newCharacter.SetBulletPanel(coreData.bulletPanel);
            }

            // 앞뒤 이미지 적용
            if (newCharData.frontSprite != null || newCharData.backSprite != null)
            {
                ApplyFrontBackImages(newCharacter, newCharData);
            }
        }

        // 기존 3개 캐릭터 제거
        foreach (var character in charactersToMerge)
        {
            if (character != null && character.gameObject != null)
            {
                Destroy(character.gameObject);
            }
        }

        Debug.Log($"[CharacterSelectUI] 자동 합성 성공! 새로운 2성 캐릭터 '{newCharData.characterName}' 생성");
    }

    /// <summary>
    /// 기본 합성 처리 (StarMergeDatabase 없을 때)
    /// </summary>
    private void BasicMerge(List<Character> charactersToMerge, Tile targetTile)
    {
        // 첫 번째 캐릭터를 2성으로 업그레이드
        Character targetChar = charactersToMerge[0];
        targetChar.star = CharacterStar.TwoStar;
        
        // 스탯 업그레이드
        targetChar.attackPower *= 1.3f;
        targetChar.attackSpeed *= 1.1f;
        targetChar.attackRange *= 1.1f;
        targetChar.currentHP *= 1.2f;
        
        targetChar.ApplyStarVisual();

        // 위치 이동
        if (targetChar.transform.parent != null)
        {
            RectTransform targetRect = targetChar.GetComponent<RectTransform>();
            RectTransform parentRect = targetChar.transform.parent.GetComponent<RectTransform>();
            if (targetRect != null && parentRect != null)
            {
                Vector2 localPos = parentRect.InverseTransformPoint(targetTile.transform.position);
                targetRect.anchoredPosition = localPos;
            }
        }
        else
        {
            targetChar.transform.position = targetTile.transform.position;
        }
        targetChar.currentTile = targetTile;

        // 나머지 2개 제거
        for (int i = 1; i < charactersToMerge.Count; i++)
        {
            if (charactersToMerge[i] != null && charactersToMerge[i].gameObject != null)
            {
                Destroy(charactersToMerge[i].gameObject);
            }
        }

        Debug.Log($"[CharacterSelectUI] 기본 합성 완료! {targetChar.characterName}이(가) 2성으로 업그레이드됨");
    }

    /// <summary>
    /// 캐릭터에 앞뒤 이미지 적용
    /// </summary>
    private void ApplyFrontBackImages(Character character, CharacterData data)
    {
        // 기존 이미지 컴포넌트 찾기 또는 생성
        Transform frontImageObj = character.transform.Find("FrontImage");
        Transform backImageObj = character.transform.Find("BackImage");
        
        // FrontImage 생성 또는 업데이트
        if (data.frontSprite != null)
        {
            if (frontImageObj == null)
            {
                GameObject frontGO = new GameObject("FrontImage");
                frontGO.transform.SetParent(character.transform, false);
                frontImageObj = frontGO.transform;
                
                Image frontImg = frontGO.AddComponent<Image>();
                RectTransform frontRect = frontGO.GetComponent<RectTransform>();
                frontRect.anchorMin = new Vector2(0.5f, 0.5f);
                frontRect.anchorMax = new Vector2(0.5f, 0.5f);
                frontRect.pivot = new Vector2(0.5f, 0.5f);
                frontRect.sizeDelta = new Vector2(100, 100);
                frontRect.anchoredPosition = Vector2.zero;
            }
            
            Image frontImage = frontImageObj.GetComponent<Image>();
            if (frontImage != null)
            {
                frontImage.sprite = data.frontSprite;
                frontImage.preserveAspect = true;
            }
            
            frontImageObj.gameObject.SetActive(false);
        }
        
        // BackImage 생성 또는 업데이트
        if (data.backSprite != null)
        {
            if (backImageObj == null)
            {
                GameObject backGO = new GameObject("BackImage");
                backGO.transform.SetParent(character.transform, false);
                backImageObj = backGO.transform;
                
                Image backImg = backGO.AddComponent<Image>();
                RectTransform backRect = backGO.GetComponent<RectTransform>();
                backRect.anchorMin = new Vector2(0.5f, 0.5f);
                backRect.anchorMax = new Vector2(0.5f, 0.5f);
                backRect.pivot = new Vector2(0.5f, 0.5f);
                backRect.sizeDelta = new Vector2(100, 100);
                backRect.anchoredPosition = Vector2.zero;
            }
            
            Image backImage = backImageObj.GetComponent<Image>();
            if (backImage != null)
            {
                backImage.sprite = data.backSprite;
                backImage.preserveAspect = true;
            }
            
            backImageObj.gameObject.SetActive(true);
        }
        
        Debug.Log($"[CharacterSelectUI] {character.characterName}에 앞뒤 이미지 적용 완료");
    }

    private void UpdateHandButtons()
    {
        for (int i = 0; i < selectButtons.Length; i++)
        {
            selectButtons[i].characterIndex = handIndices[i];
            selectButtons[i].isEmpty = false;
            UpdateOneButtonUI(selectButtons[i]);
        }
    }

    private void UpdateNextUnitUI()
    {
        if (nextIndex < 0 || nextIndex >= deckFromLobby.Length)
        {
            if (nextUnitImage) nextUnitImage.sprite = null;
            if (nextUnitCost) nextUnitCost.text = "-";
            return;
        }

        CharacterData data = deckFromLobby[nextIndex];
        if (data == null)
        {
            if (nextUnitImage) nextUnitImage.sprite = null;
            if (nextUnitCost) nextUnitCost.text = "-";
            return;
        }

        if (nextUnitImage && data.buttonIcon != null)
        {
            nextUnitImage.sprite = data.buttonIcon.sprite;
        }
        if (nextUnitCost)
        {
            nextUnitCost.text = data.cost.ToString();
        }
    }

    private void UpdateOneButtonUI(SelectButton sb)
    {
        if (sb.button == null) return;
        sb.button.onClick.RemoveAllListeners();

        if (sb.characterIndex < 0 || sb.characterIndex >= deckFromLobby.Length)
        {
            sb.isEmpty = true;
            if (sb.iconImage) sb.iconImage.sprite = null;
            if (sb.costText) sb.costText.text = "-";
            return;
        }

        CharacterData data = deckFromLobby[sb.characterIndex];
        if (data != null)
        {
            sb.isEmpty = false;
            if (sb.iconImage && data.buttonIcon != null)
            {
                sb.iconImage.sprite = data.buttonIcon.sprite;
            }
            if (sb.costText)
            {
                sb.costText.text = data.cost.ToString();
            }

            int copyIndex = sb.characterIndex;
            // ===========================
            // 클릭 → 배치 대기 상태로 설정
            // ===========================
            sb.button.onClick.AddListener(() =>
            {
                OnClickCardButton(copyIndex);
            });
        }
        else
        {
            sb.isEmpty = true;
            if (sb.iconImage) sb.iconImage.sprite = null;
            if (sb.costText) sb.costText.text = "-";
        }

        // 드래그 소환용 컴포넌트
        DraggableSummonButtonUI dragComp = sb.button.GetComponent<DraggableSummonButtonUI>();
        if (dragComp == null)
        {
            dragComp = sb.button.gameObject.AddComponent<DraggableSummonButtonUI>();
        }

        dragComp.SetSummonData(sb.characterIndex);
        dragComp.parentSelectUI = this;
    }

    /// <summary>
    /// [신규] 카드 버튼 클릭 시 로직
    /// "이미 선택중인 카드가 있으면 무시 / 없으면 선택 대기"
    /// </summary>
    private void OnClickCardButton(int clickedIndex)
    {
        // ▼▼ [수정] 이미 선택된 카드와 같은 카드를 클릭하면 유지, 다른 카드면 변경 ▼▼
        if (hasPendingCard && pendingCardIndex != clickedIndex)
        {
            Debug.Log($"[CharacterSelectUI] 기존 선택({pendingCardIndex}) -> 새로운 선택({clickedIndex})으로 변경");
        }

        hasPendingCard = true;
        pendingCardIndex = clickedIndex;

        // PlacementManager에 "이 캐릭터 인덱스 선택" 알림
        if (placementManager != null)
        {
            placementManager.OnClickSelectUnit(clickedIndex);
            
            // ▼▼ [추가] 자동 배치 호출 (placed/placable 꽉 차면 walkable로) ▼▼
            placementManager.OnClickAutoPlace();
        }
        Debug.Log($"[CharacterSelectUI] 카드({clickedIndex}) 클릭 -> 자동 배치 시도");
    }

    /// <summary>
    /// (드래그 소환) 드롭 성공 시 OnDragUseCard(usedIndex) 호출
    /// </summary>
    public void OnDragUseCard(int usedIndex)
    {
        for (int i = 0; i < selectButtons.Length; i++)
        {
            if (selectButtons[i].characterIndex == usedIndex)
            {
                OnUseCard(selectButtons[i]);
                break;
            }
        }
    }

    /// <summary>
    /// [신규] "클릭으로 선택된 카드"가 실제로 배치 성공했을 때(PlacementManager) 호출되는 메서드.
    /// </summary>
    public void MarkCardAsUsed(int usedIndex)
    {
        if (!hasPendingCard || pendingCardIndex != usedIndex)
        {
            Debug.LogWarning($"[CharacterSelectUI] MarkCardAsUsed({usedIndex})가 호출됐지만 대기중인 카드와 불일치 -> 무시");
            return;
        }

        // 실제로 OnUseCard() 실행
        for (int i = 0; i < selectButtons.Length; i++)
        {
            if (selectButtons[i].characterIndex == usedIndex)
            {
                OnUseCard(selectButtons[i]);
                break;
            }
        }

        // ▼▼ [수정] 카드 사용 후에도 선택 상태 유지 (연속 배치 가능) ▼▼
        // hasPendingCard = false;
        // pendingCardIndex = -1;
        Debug.Log($"[CharacterSelectUI] 카드({usedIndex}) 사용됨. 선택 상태는 유지됩니다.");
    }

    /// <summary>
    /// 카드 사용(이미 소환됨) → 다음 카드 교체 로직
    /// </summary>
    private void OnUseCard(SelectButton usedButton)
    {
        int usedIndex = usedButton.characterIndex;

        // 1) 사용된 버튼에 nextIndex 교체
        usedButton.characterIndex = nextIndex;
        usedButton.isEmpty = false;
        UpdateOneButtonUI(usedButton);

        // 2) reserve(4) + used(1) = 5
        List<int> five = new List<int>(reserveIndices);
        five.Add(usedIndex);

        // 3) 셔플
        ShuffleList(five);
        nextIndex = five[0];
        reserveIndices.Clear();
        for (int i = 1; i < 5; i++)
        {
            reserveIndices.Add(five[i]);
        }

        // 다음 카드 UI 갱신
        UpdateNextUnitUI();
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }

    public void RefreshInventoryUI()
    {
        if (characterInventory == null)
        {
            // 자동으로 CharacterInventoryManager 찾기 시도
            characterInventory = FindFirstObjectByType<CharacterInventoryManager>();
            
            if (characterInventory == null)
            {
                Debug.LogWarning("[CharacterSelectUI] CharacterInventoryManager를 찾을 수 없습니다!");
                return;
            }
            else
            {
                Debug.Log("[CharacterSelectUI] CharacterInventoryManager를 자동으로 찾았습니다.");
            }
        }

        // 현재는 구현 생략 (DeckPanelManager 등에서 구현)
        // 필요 시 DeckPanelManager 같은 곳에서 sharedSlotData200을 갱신하여 사용
    }

    /// <summary>
    /// (요청) placable/placeTile이 전부 찼을 경우 => walkable로 소환
    /// 4장의 카드(Hand)에 대해 지역1의 placable/placeTile을 찾아 랜덤으로 소환.
    /// 만약 placable/placeTile이 전혀 없으면, walkable로 소환하도록 수정.
    /// </summary>
    public void OnClickUse4CardsAtRandom()
    {
        // 지역1의 모든 Tile 스캔
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        List<Tile> validTiles = new List<Tile>();

        // placable / placeTile 중 isRegion2 == false (지역1)
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

        // [MODIFIED for #2] => 만약 placable/placeTile이 하나도 없으면 walkable로 대체
        if (validTiles.Count == 0)
        {
            Debug.LogWarning("[CharacterSelectUI] 지역1에 배치할 수 있는 placable/placeTile이 없습니다! => walkable로 대체");
            List<Tile> walkableTiles = new List<Tile>();
            foreach (Tile t in allTiles)
            {
                if (t != null && !t.isRegion2 && t.IsWalkable())
                {
                    walkableTiles.Add(t);
                }
            }
            if (walkableTiles.Count == 0)
            {
                Debug.LogWarning("[CharacterSelectUI] 지역1에 배치할 수 있는 walkable도 없습니다! -> 소환 포기");
                return;
            }
            validTiles = walkableTiles;
        }

        Debug.Log($"[CharacterSelectUI] OnClickUse4CardsAtRandom() -> 지역1의 배치가능 타일 {validTiles.Count}개 중 랜덤으로 4장 소환 시도.");

        for (int i = 0; i < 4; i++)
        {
            SelectButton sb = selectButtons[i];
            if (sb == null || sb.isEmpty)
            {
                Debug.Log($"[CharacterSelectUI] {i}번째 selectButton이 비어있음 -> 스킵");
                continue;
            }

            // 타일 무작위 선택
            Tile randomTile = validTiles[Random.Range(0, validTiles.Count)];

            if (placementManager == null)
            {
                Debug.LogWarning("[CharacterSelectUI] placementManager가 null -> 소환 불가");
                return;
            }

            // 소환 시도
            bool success = placementManager.SummonCharacterOnTile(sb.characterIndex, randomTile);
            if (success)
            {
                // 성공하면 카드 사용 처리
                OnUseCard(sb);
            }
            else
            {
                Debug.Log($"[CharacterSelectUI] 카드({sb.characterIndex}) 소환 실패 -> 남은 카드들은 중단");
                break;
            }
        }
    }

    /// <summary>
    /// 테스트 용: 특정 캐릭터를 덱에 직접 추가
    /// </summary>
    public void AddCharacterToDeck(int index, CharacterData data)
    {
        if (index < 0 || index >= 9) return;
        deckFromLobby[index] = data;
    }
}