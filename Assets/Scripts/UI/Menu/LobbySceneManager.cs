using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using GuildMaster.Data;

[System.Serializable]
public class StageInfo
{
    public int stageIndex;
    public bool isUnlocked;
    public int totalAttempts;
    public int winCount;
    public bool[] attemptResults;
}

[System.Serializable]
public class SaveData
{
    public StageInfoData[] stageInfos;
}

[System.Serializable]
public class StageInfoData
{
    public bool isUnlocked;
    public int winCount;
    public bool[] attemptResults;
}

public class LobbySceneManager : MonoBehaviour
{
    [Header("게임 데이터 초기화 옵션")]
    [SerializeField] private bool resetGameData = false;

    [Header("스테이지 개수")]
    [SerializeField] private int totalStages = 100;

    [Header("스테이지별 이미지(인덱스 순)")]
    [SerializeField] private GameObject[] stageSprites;

    // ============================================
    //  (추가) CharacterInventoryManager 참조
    // ============================================
    [Header("CharacterInventoryManager(인벤토리)")]
    [Tooltip("게임 데이터 리셋 시 인벤토리까지 초기화하려면 연결 필요")]
    [SerializeField] private CharacterInventoryManager characterInventoryManager;

    // DeckPanelManager 타입이 제거되어 주석 처리
    // ============================================
    //  DeckPanelManager 참조
    // ============================================
    // [Header("DeckPanelManager(덱 패널)")]
    // [SerializeField] private DeckPanelManager deckPanelManager;

    // ==========================
    //  (새로 추가) Saved DB
    // ==========================
    // (★ 필요없다면 완전히 제거 가능)
    // [Header("새로 추가: Saved Character DB (ScriptableObject)")]
    // [Tooltip("로비씬에서 강제 로드했던 DB. (자동 갱신 로직 제거됨)")]
    // public CharacterDatabaseObject savedCharacterDatabase;

    private List<StageInfo> stages = new List<StageInfo>();
    private int currentStageIndex = 0;

    private int gold = 0;
    private int diamond = 0;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (resetGameData)
        {
            ResetGameData();
            resetGameData = false;
            Debug.Log("게임 데이터 리셋됨.(OnValidate)");
        }
    }
#endif

    private void ResetGameData()
    {
        // 1) 스테이지 정보 초기화
        InitializeStages();
        // 2) 골드/다이아 0
        gold = 0;
        diamond = 0;

        // 3) StageInfo 저장 키값 제거
        PlayerPrefs.DeleteKey("GameData");
        PlayerPrefs.DeleteKey("Gold");
        PlayerPrefs.DeleteKey("Diamond");

        // 4) 인벤토리 매니저 초기화(있다면)
        if (characterInventoryManager != null)
        {
            characterInventoryManager.ClearAllData();
        }

        PlayerPrefs.Save();

        // 5) 현재 스테이지 인덱스 0으로
        currentStageIndex = 0;

        // 6) UI 새로고침
        UpdateStageUI();
        UpdateGoldAndDiamondUI();

        // + 덱 패널도 갱신 - DeckPanelManager 타입이 제거되어 주석 처리
        // DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        // if (dpm != null)
        // {
        //     dpm.RefreshInventoryUI();
        //     dpm.SetupRegisterButtons();
        //     dpm.InitRegisterSlotsVisual();
        //     Debug.Log("[LobbySceneManager] ResetGameData() 이후 DeckPanelManager도 재초기화 완료");
        // }
    }

    private void Awake()
    {
        SetupPanels();
        if (explainText) explainText.text = "";
    }

    // =========================================================================
    // (★ 수정) OnEnable()에서 DB를 다시 로드하거나 덮어쓰는 코드 제거
    // =========================================================================
    private void OnEnable()
    {
        // 기존에는 여기서 ScriptableObject를 강제로 재로드하던 로직이 있었으나, 제거.
    }

    private void Start()
    {
        // 1) 스테이지 정보 로드
        InitializeStages();
        LoadGame();

        // (★ 수정) savedCharacterDatabase를 강제로 DB에 덮어쓰는 로직 제거

        UpdateStageUI();
        UpdateGoldAndDiamondUI();

        // 덱/업그레이드 패널들 UI 갱신 - DeckPanelManager 타입이 제거되어 주석 처리
        // DeckPanelManager deckPanel = FindFirstObjectByType<DeckPanelManager>();
        // if (deckPanel != null)
        // {
        //     deckPanel.RefreshInventoryUI();
        // }
        UpgradePanelManager upm = FindFirstObjectByType<UpgradePanelManager>();
        if (upm != null)
        {
            upm.RefreshDisplay();
            upm.SetUpgradeRegisteredSlotsFromDeck();
        }

        // 아이템 인벤토리 패널은 상시 활성
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void InitializeStages()
    {
        stages.Clear();
        for (int i = 0; i < totalStages; i++)
        {
            StageInfo info = new StageInfo
            {
                stageIndex = i,
                isUnlocked = (i == 0),
                totalAttempts = 5,
                winCount = 0,
                attemptResults = new bool[5]
            };
            stages.Add(info);
        }
    }

    private void SaveGame()
    {
        // 스테이지+시도기록
        SaveData data = new SaveData();
        data.stageInfos = new StageInfoData[stages.Count];
        for (int i = 0; i < stages.Count; i++)
        {
            data.stageInfos[i] = new StageInfoData
            {
                isUnlocked = stages[i].isUnlocked,
                winCount = stages[i].winCount,
                attemptResults = stages[i].attemptResults
            };
        }
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("GameData", json);

        // 골드/다이아
        PlayerPrefs.SetInt("Gold", gold);
        PlayerPrefs.SetInt("Diamond", diamond);
        PlayerPrefs.Save();
        Debug.Log("SaveGame() 완료");
    }

    private void LoadGame()
    {
        gold = PlayerPrefs.GetInt("Gold", 0);
        diamond = PlayerPrefs.GetInt("Diamond", 0);

        string json = PlayerPrefs.GetString("GameData", "");
        if (!string.IsNullOrEmpty(json))
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            int count = Mathf.Min(data.stageInfos.Length, stages.Count);
            for (int i = 0; i < count; i++)
            {
                stages[i].isUnlocked = data.stageInfos[i].isUnlocked;
                stages[i].winCount = data.stageInfos[i].winCount;
                stages[i].attemptResults = data.stageInfos[i].attemptResults;
            }
        }
        else
        {
            Debug.Log("저장된 GameData 없음 -> 기본값");
        }
    }

    [Header("골드/다이아 표시 UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI diamondText;

    private void UpdateGoldAndDiamondUI()
    {
        if (goldText) goldText.text = gold.ToString();
        if (diamondText) diamondText.text = diamond.ToString();
    }

    [Header("스테이지 이동 버튼")]
    [SerializeField] private Button stageLeftButton;
    [SerializeField] private Button stageRightButton;
    [SerializeField] private TextMeshProUGUI stageIndexText;

    [Header("스테이지 락 아이콘/썸네일")]
    [SerializeField] private Image stageLockIcon;
    [SerializeField] private Image stageThumbnail;

    [Header("5회 플레이 체크 아이콘(승리=checkSprite)")]
    [SerializeField] private Image[] stageAttemptIcons;
    [SerializeField] private Sprite checkSprite;
    [SerializeField] private Sprite emptySprite;

    private void UpdateStageUI()
    {
        if (stageIndexText)
            stageIndexText.text = $"{currentStageIndex + 1}";

        StageInfo info = stages[currentStageIndex];
        bool locked = !info.isUnlocked;

        // 잠금 아이콘은 잠금 상태에 따라 표시
        if (stageLockIcon) stageLockIcon.gameObject.SetActive(locked);
        
        // 썸네일 이미지는 항상 표시 (기본 상태)
        if (stageThumbnail) stageThumbnail.gameObject.SetActive(!locked);

        // 모든 스테이지 프리팹 비활성화
        if (stageSprites != null)
        {
            for (int i = 0; i < stageSprites.Length; i++)
            {
                if (stageSprites[i] != null)
                {
                    stageSprites[i].SetActive(false);
                }
            }
        }

        // 스테이지가 잠금 해제 상태일 때만 해당 스테이지 게임 오브젝트 활성화
        if (!locked && stageSprites != null)
        {
            // 현재 스테이지에 맞는 프리팹 활성화
            if (currentStageIndex < stageSprites.Length && stageSprites[currentStageIndex] != null)
            {
                stageSprites[currentStageIndex].SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[LobbySceneManager] 스테이지 {currentStageIndex+1}의 프리팹이 없거나 인덱스가 범위를 벗어났습니다.");
            }
        }

        for (int i = 0; i < 5; i++)
        {
            if (stageAttemptIcons != null && i < stageAttemptIcons.Length && stageAttemptIcons[i] != null)
            {
                bool isWin = info.attemptResults[i];
                stageAttemptIcons[i].sprite = isWin ? checkSprite : emptySprite;
            }
        }
    }

    public void OnClickStageLeft()
    {
        currentStageIndex--;
        if (currentStageIndex < 0) currentStageIndex = 0;
        UpdateStageUI();
    }

    public void OnClickStageRight()
    {
        currentStageIndex++;
        if (currentStageIndex >= totalStages) currentStageIndex = totalStages - 1;
        UpdateStageUI();
    }

    public void OnClickEnterStage()
    {
        StageInfo info = stages[currentStageIndex];
        if (!info.isUnlocked)
        {
            Debug.Log($"Stage {currentStageIndex + 1}는 잠겨있음");
            return;
        }

        // DeckPanelManager 타입이 제거되어 주석 처리
        // DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        // if (dpm != null)
        // {
        //     // 덱(10칸) 확인 (9개 이상 등록돼야 게임씬 진행 가능)
        //     CharacterData[] deckSet = dpm.registeredCharactersSet2;
        //     int count = 0;
        //     foreach (var c in deckSet)
        //     {
        //         if (c != null) count++;
        //     }
        //     if (count < 9)
        //     {
        //         Debug.LogWarning($"[LobbySceneManager] 덱에 등록된 캐릭터 {count}개이므로 게임시작 불가(최소 9개 필요)");
        //         return;
        //     }
        //
        //     // (게임씬에서 필요하다던 캐릭터 10칸 세팅)
        //     if (GuildMaster.Core.GameManager.Instance != null &&
        //         GuildMaster.Core.GameManager.Instance.currentRegisteredCharacters != null &&
        //         GuildMaster.Core.GameManager.Instance.currentRegisteredCharacters.Length >= 10)
        //     {
        //         for (int i = 0; i < deckSet.Length && i < 10; i++)
        //         {
        //             if (deckSet[i] != null)
        //             {
        //                 // CharacterData를 Unit으로 변환
        //                 var unit = new GuildMaster.Battle.Unit(
        //                     deckSet[i].name,
        //                     deckSet[i].level,
        //                     deckSet[i].jobClass,
        //                     ConvertCharacterRarityToRarity(deckSet[i].rarity)
        //                 );
        //                 unit.unitId = deckSet[i].id;
        //                 unit.maxHP = deckSet[i].baseHP;
        //                 unit.maxMP = deckSet[i].baseMP;
        //                 unit.attackPower = deckSet[i].baseAttack;
        //                 unit.defense = deckSet[i].baseDefense;
        //                 unit.magicPower = deckSet[i].baseMagicPower;
        //                 unit.speed = deckSet[i].baseSpeed;
        //                 unit.criticalRate = deckSet[i].critRate;
        //                 unit.accuracy = deckSet[i].accuracy;
        //                 unit.currentHP = unit.maxHP;
        //                 unit.currentMP = unit.maxMP;
        //                 
        //                 GuildMaster.Core.GameManager.Instance.currentRegisteredCharacters[i] = unit;
        //             }
        //         }
        //         Debug.Log("[LobbySceneManager] 덱(10칸)을 GameManager.currentRegisteredCharacters에 반영");
        //     }
        //     else
        //     {
        //         Debug.LogWarning("[LobbySceneManager] GameManager.currentRegisteredCharacters가 유효하지 않음");
        //         return;
        //     }
        //
        //     // ▼▼ [수정] DB에 덱을 덮어쓰는 로직을 막기 위해 주석처리 ▼▼
        //     /*
        //     if (characterInventoryManager != null &&
        //         characterInventoryManager.characterDatabaseObject != null &&
        //         characterInventoryManager.characterDatabaseObject.characters != null &&
        //         characterInventoryManager.characterDatabaseObject.characters.Length >= 10)
        //     {
        //         for (int i = 0; i < 10; i++)
        //         {
        //             characterInventoryManager.characterDatabaseObject.characters[i] = deckSet[i];
        //         }
        //         Debug.Log("[LobbySceneManager] 로비씬 덱(10개)을 ScriptableObject DB(0~9)에도 덮어씀");
        //     }
        //     */
        //     // ▲▲ [수정 끝] ▲▲
        // }
        // else
        // {
        //     Debug.LogWarning("[LobbySceneManager] DeckPanelManager 미발견 => 게임 시작 불가");
        //     return;
        // }
        
        // 덱 검증 없이 진행
        Debug.LogWarning("[LobbySceneManager] DeckPanelManager가 제거되어 덱 검증 없이 게임 시작");

        Debug.Log($"Stage {currentStageIndex + 1} 입장 -> GameScene 이동");

        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }

        SceneManager.LoadScene("GameScene");
    }

    public void RecordStageAttempt(int attemptIndex, bool isWin)
    {
        if (attemptIndex < 0 || attemptIndex >= 5) return;

        StageInfo info = stages[currentStageIndex];
        info.attemptResults[attemptIndex] = isWin;

        int wCount = 0;
        foreach (bool w in info.attemptResults) if (w) wCount++;
        info.winCount = wCount;

        if (attemptIndex == 4 && info.winCount >= 3)
        {
            int next = currentStageIndex + 1;
            if (next < totalStages) stages[next].isUnlocked = true;
        }

        UpdateStageUI();
        SaveGame();
    }

    [Header("패널들")]
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject drawPanel;
    [SerializeField] private GameObject clanPanel;
    [SerializeField] private GameObject characterPanel;

    [Header("덱/업그레이드 관련 오브젝트")]
    [SerializeField] private GameObject deckObject;
    [SerializeField] private GameObject upgradeObject;

    [Header("프로필 패널 안: 친구/랭킹 오브젝트")]
    [SerializeField] private GameObject friendGameObject;
    [SerializeField] private GameObject rankingGameObject;

    // 아이템 패널(보상용) + 아이템 인벤토리 패널
    [Header("아이템 패널(보상용) + 아이템 인벤토리 패널")]
    public GameObject itemPanel;
    public GameObject itemInventoryPanel;

    private List<GameObject> allPanels = new List<GameObject>();

    private void SetupPanels()
    {
        allPanels.Clear();
        if (profilePanel) { profilePanel.SetActive(false); allPanels.Add(profilePanel); }
        if (optionPanel) { optionPanel.SetActive(false); allPanels.Add(optionPanel); }
        if (shopPanel) { shopPanel.SetActive(false); allPanels.Add(shopPanel); }
        if (drawPanel) { drawPanel.SetActive(false); allPanels.Add(drawPanel); }
        if (clanPanel) { clanPanel.SetActive(false); allPanels.Add(clanPanel); }
        if (characterPanel) { characterPanel.SetActive(false); allPanels.Add(characterPanel); }

        if (deckObject) deckObject.SetActive(false);
        if (upgradeObject) upgradeObject.SetActive(false);

        if (friendGameObject) friendGameObject.SetActive(false);
        if (rankingGameObject) rankingGameObject.SetActive(false);

        if (itemPanel)
        {
            itemPanel.SetActive(false);
            allPanels.Add(itemPanel);
        }
        if (itemInventoryPanel)
        {
            itemInventoryPanel.SetActive(false);
            allPanels.Add(itemInventoryPanel);
        }
    }

    private void CloseAllPanels()
    {
        foreach (var p in allPanels)
        {
            if (p) p.SetActive(false);
        }
    }

    // 캐릭터/프로필 등
    public void OnClickOpenCharacterPanel()
    {
        CloseAllPanels();
        if (characterPanel) characterPanel.SetActive(true);

        if (deckObject)
        {
            deckObject.SetActive(true);
            Button[] deckButtons = deckObject.GetComponentsInChildren<Button>(true);
            foreach (var btn in deckButtons)
            {
                btn.gameObject.SetActive(true);
            }
        }
        if (upgradeObject) upgradeObject.SetActive(false);

        // DeckPanelManager 타입이 제거되어 주석 처리
        // DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        // if (dpm != null)
        // {
        //     dpm.isUpgradeMode = false;
        //     Debug.Log("[LobbySceneManager] 캐릭터 패널 열림 -> DeckPanelManager.isUpgradeMode = false");
        // }
    }

    public void OnClickCloseCharacterPanel()
    {
        if (characterPanel) characterPanel.SetActive(false);
    }

    public void OnClickOpenProfilePanel()
    {
        CloseAllPanels();
        if (profilePanel) profilePanel.SetActive(true);
    }
    public void OnClickCloseProfilePanel()
    {
        if (profilePanel) profilePanel.SetActive(false);
    }

    public void OnClickOpenOptionPanel()
    {
        CloseAllPanels();
        if (optionPanel) optionPanel.SetActive(true);
    }
    public void OnClickCloseOptionPanel()
    {
        if (optionPanel) optionPanel.SetActive(false);
    }

    public void OnClickOpenShopPanel()
    {
        CloseAllPanels();
        if (shopPanel) shopPanel.SetActive(true);
    }
    public void OnClickCloseShopPanel()
    {
        if (shopPanel) shopPanel.SetActive(false);
    }

    public void OnClickOpenDrawPanel()
    {
        CloseAllPanels();
        if (drawPanel) drawPanel.SetActive(true);
    }
    public void OnClickCloseDrawPanel()
    {
        if (drawPanel) drawPanel.SetActive(false);
    }

    public void OnClickOpenClanPanel()
    {
        CloseAllPanels();
        if (clanPanel) clanPanel.SetActive(true);
    }
    public void OnClickCloseClanPanel()
    {
        if (clanPanel) clanPanel.SetActive(false);
    }

    // 덱/업그레이드 패널
    public void OnClickDeckButton()
    {
        if (deckObject) deckObject.SetActive(true);
        if (upgradeObject) upgradeObject.SetActive(false);

        // DeckPanelManager 타입이 제거되어 주석 처리
        // DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        // if (dpm != null)
        // {
        //     dpm.isUpgradeMode = false;
        //     Debug.Log("[LobbySceneManager] 덱 패널 열림 -> DeckPanelManager.isUpgradeMode = false");
        // }
    }

    public void OnClickUpgradeButton()
    {
        if (deckObject) deckObject.SetActive(false);
        if (upgradeObject) upgradeObject.SetActive(true);

        // DeckPanelManager 타입이 제거되어 주석 처리
        // DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        // if (dpm != null)
        // {
        //     dpm.isUpgradeMode = true;
        //     Debug.Log("[LobbySceneManager] 업그레이드 패널 열림 -> DeckPanelManager.isUpgradeMode = true");
        // }
    }

    public void OnClickCloseDeck()
    {
        if (deckObject) deckObject.SetActive(false);
    }
    public void OnClickCloseUpgrade()
    {
        if (upgradeObject) upgradeObject.SetActive(false);
    }

    public void OnClickProfileFriend()
    {
        if (friendGameObject) friendGameObject.SetActive(true);
        if (rankingGameObject) rankingGameObject.SetActive(false);
    }
    public void OnClickProfileRanking()
    {
        if (friendGameObject) friendGameObject.SetActive(false);
        if (rankingGameObject) rankingGameObject.SetActive(true);
    }

    [Header("튜토리얼/스토리 text")]
    [SerializeField] private TextMeshProUGUI explainText;

    private readonly string tutorialStr =
@"1. 5×5 칸 중 원하는 곳을 클릭해 캐릭터를 소환하되...
(이하 생략)";
    private readonly string storyStr =
@"1. X/O 두 세력간 전쟁...
(이하 생략)";

    public void OnClickTutorialButton()
    {
        if (explainText) explainText.text = tutorialStr;
    }

    public void OnClickStoryButton()
    {
        if (explainText) explainText.text = storyStr;
    }

    public void OnClickClearExplainText()
    {
        if (explainText) explainText.text = "";
    }

    public void RefreshPanel()
    {
        // DeckPanelManager 타입이 제거되어 주석 처리
        // deckPanelManager.RefreshInventoryUI();
    }
    
    /// <summary>
    /// 화폐 표시 업데이트 (리사이클 등 외부에서 호출용)
    /// </summary>
    public void UpdateCurrencyDisplay()
    {
        // PlayerPrefs에서 최신 값 읽기
        gold = PlayerPrefs.GetInt("Gold", 0);
        diamond = PlayerPrefs.GetInt("Diamond", 0);
        
        // UI 업데이트
        UpdateGoldAndDiamondUI();
    }
    
    private GuildMaster.Data.Rarity ConvertCharacterRarityToRarity(CharacterRarity characterRarity)
    {
        switch (characterRarity)
        {
            case CharacterRarity.Common:
                return GuildMaster.Data.Rarity.Common;
            case CharacterRarity.Uncommon:
                return GuildMaster.Data.Rarity.Uncommon;
            case CharacterRarity.Rare:
                return GuildMaster.Data.Rarity.Rare;
            case CharacterRarity.Epic:
                return GuildMaster.Data.Rarity.Epic;
            case CharacterRarity.Legendary:
                return GuildMaster.Data.Rarity.Legendary;
            default:
                return GuildMaster.Data.Rarity.Common;
        }
    }
}
