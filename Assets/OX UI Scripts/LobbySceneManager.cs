using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 각 스테이지(챕터) 관리, 캐릭터/인벤토리 UI 열기/닫기, 
/// 게임 데이터(스테이지 클리어 정보, 재화 등)를 저장/로드하는 매니저.
/// </summary>
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
    [SerializeField] private Sprite[] stageSprites;

    // ============================================
    //  (추가) CharacterInventoryManager 참조
    // ============================================
    [Header("CharacterInventoryManager(인벤토리)")]
    [Tooltip("게임 데이터 리셋 시 인벤토리까지 초기화하려면 연결 필요")]
    [SerializeField] private CharacterInventoryManager characterInventoryManager;

    // ============================================
    //  DeckPanelManager 참조 추가
    // ============================================
    [Header("DeckPanelManager(덱 패널)")]
    [SerializeField] private DeckPanelManager deckPanelManager;

    // 스테이지 정보
    private List<StageInfo> stages = new List<StageInfo>();
    private int currentStageIndex = 0;

    // 재화
    private int gold = 0;
    private int diamond = 0;

    private void Awake()
    {
        SetupPanels();
        if (explainText) 
            explainText.text = "";
    }

    private void Start()
    {
        // 스테이지 리스트 초기화 + 로드
        InitializeStages();
        LoadGame();
        UpdateStageUI();
        UpdateGoldAndDiamondUI();

        // 덱/업그레이드 UI도 초기화
        DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        if (dpm != null)
        {
            dpm.RefreshInventoryUI();
        }
        UpgradePanelManager upm = FindFirstObjectByType<UpgradePanelManager>();
        if (upm != null)
        {
            upm.RefreshDisplay();
            upm.SetUpgradeRegisteredSlotsFromDeck();
        }

        // 아이템 인벤토리 패널 상시 활성
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

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

    /// <summary>
    /// 스테이지/재화/인벤토리 등을 모두 초기화
    /// </summary>
    private void ResetGameData()
    {
        InitializeStages();
        gold = 0;
        diamond = 0;

        PlayerPrefs.DeleteKey("GameData");
        PlayerPrefs.DeleteKey("Gold");
        PlayerPrefs.DeleteKey("Diamond");

        if (characterInventoryManager != null)
        {
            characterInventoryManager.ClearAllData();
        }

        PlayerPrefs.Save();

        currentStageIndex = 0;
        UpdateStageUI();
        UpdateGoldAndDiamondUI();

        // 덱도 다시 초기화
        DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        if (dpm != null)
        {
            dpm.RefreshInventoryUI();
            dpm.SetupRegisterButtons();
            dpm.InitRegisterSlotsVisual();
            Debug.Log("[LobbySceneManager] Reset 후 DeckPanelManager 리셋 완료");
        }
    }

    /// <summary>
    /// 스테이지 리스트 기본 세팅
    /// </summary>
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
        // 스테이지 정보
        SaveData data = new SaveData();
        data.stageInfos = new StageInfoData[stages.Count];
        for (int i = 0; i < stages.Count; i++)
        {
            data.stageInfos[i] = new StageInfoData
            {
                isUnlocked     = stages[i].isUnlocked,
                winCount       = stages[i].winCount,
                attemptResults = stages[i].attemptResults
            };
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("GameData", json);

        // 재화
        PlayerPrefs.SetInt("Gold", gold);
        PlayerPrefs.SetInt("Diamond", diamond);

        PlayerPrefs.Save();
        Debug.Log("[LobbySceneManager] SaveGame() 완료");
    }

    private void LoadGame()
    {
        gold    = PlayerPrefs.GetInt("Gold", 0);
        diamond = PlayerPrefs.GetInt("Diamond", 0);

        string json = PlayerPrefs.GetString("GameData", "");
        if (!string.IsNullOrEmpty(json))
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            int count = Mathf.Min(data.stageInfos.Length, stages.Count);
            for (int i = 0; i < count; i++)
            {
                stages[i].isUnlocked     = data.stageInfos[i].isUnlocked;
                stages[i].winCount       = data.stageInfos[i].winCount;
                stages[i].attemptResults = data.stageInfos[i].attemptResults;
            }
        }
        else
        {
            Debug.Log("[LobbySceneManager] 저장된 GameData 없음 -> 기본값 사용");
        }
    }

    [Header("골드/다이아 표시 UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI diamondText;

    private void UpdateGoldAndDiamondUI()
    {
        if (goldText)    goldText.text    = gold.ToString();
        if (diamondText) diamondText.text = diamond.ToString();
    }

    [Header("스테이지 이동 버튼/UI")]
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

        if (stageLockIcon)   stageLockIcon.gameObject.SetActive(locked);
        if (stageThumbnail)  stageThumbnail.gameObject.SetActive(!locked);

        if (!locked && stageThumbnail != null)
        {
            if (stageSprites != null && currentStageIndex < stageSprites.Length)
                stageThumbnail.sprite = stageSprites[currentStageIndex];
            else
                stageThumbnail.sprite = null;
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
        if (currentStageIndex < 0) 
            currentStageIndex = 0;
        UpdateStageUI();
    }

    public void OnClickStageRight()
    {
        currentStageIndex++;
        if (currentStageIndex >= totalStages) 
            currentStageIndex = totalStages - 1;
        UpdateStageUI();
    }

    /// <summary>
    /// 현재 스테이지 진입 (게임 씬으로 이동)
    /// </summary>
    public void OnClickEnterStage()
    {
        StageInfo info = stages[currentStageIndex];
        if (!info.isUnlocked)
        {
            Debug.Log($"[LobbySceneManager] Stage {currentStageIndex + 1}는 잠겨있습니다.");
            return;
        }

        // 덱에서 10칸을 가져와 GameManager & CharacterDatabase 반영
        DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        if (dpm != null)
        {
            CharacterData[] deckSet = dpm.registeredCharactersSet2;
            int count = 0;
            foreach (var c in deckSet)
            {
                if (c != null) 
                    count++;
            }
            if (count < 9)
            {
                Debug.LogWarning($"[LobbySceneManager] 덱에 등록된 캐릭터가 {count}개. 최소 9개 필요합니다.");
                return;
            }

            // 9개만 추출(0..8), 나머지 1개는 Hero(9번)
            if (GameManager.Instance != null && 
                GameManager.Instance.currentRegisteredCharacters != null &&
                GameManager.Instance.currentRegisteredCharacters.Length >= 10)
            {
                // 덱 정보를 GameManager.currentRegisteredCharacters에 전달
                for (int i = 0; i < deckSet.Length && i < 10; i++)
                {
                    GameManager.Instance.currentRegisteredCharacters[i] = deckSet[i];
                }
                Debug.Log("[LobbySceneManager] 덱(10개)을 GameManager.currentRegisteredCharacters에 반영");
            }
            else
            {
                Debug.LogWarning("[LobbySceneManager] GameManager.currentRegisteredCharacters가 준비되지 않음");
            }

            // CharacterInventoryManager.characterDatabaseObject에도 반영 (0..9)
            if (characterInventoryManager != null &&
                characterInventoryManager.characterDatabaseObject != null &&
                characterInventoryManager.characterDatabaseObject.characters != null &&
                characterInventoryManager.characterDatabaseObject.characters.Length >= 10)
            {
                for (int i = 0; i < 10; i++)
                {
                    characterInventoryManager.characterDatabaseObject.characters[i] = deckSet[i];
                }
                Debug.Log("[LobbySceneManager] 로비 덱(10개)을 CharacterDatabase(0~9)에 반영");
            }
        }
        else
        {
            Debug.LogWarning("[LobbySceneManager] DeckPanelManager를 찾지 못해 진행 불가");
            return;
        }

        Debug.Log($"[LobbySceneManager] Stage {currentStageIndex + 1} 입장 -> GameScene 로드");
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }

        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// 스테이지 플레이 결과(시도) 기록
    /// </summary>
    public void RecordStageAttempt(int attemptIndex, bool isWin)
    {
        if (attemptIndex < 0 || attemptIndex >= 5) 
            return;

        StageInfo info = stages[currentStageIndex];
        info.attemptResults[attemptIndex] = isWin;

        int wCount = 0;
        foreach (bool w in info.attemptResults)
            if (w) wCount++;
        info.winCount = wCount;

        // 예시) 마지막 시도 후 승리>=3이면 다음 스테이지 언락
        if (attemptIndex == 4 && info.winCount >= 3)
        {
            int next = currentStageIndex + 1;
            if (next < totalStages)
                stages[next].isUnlocked = true;
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

    [Header("아이템 패널(웨이브 보상용) + 아이템 인벤토리 패널")]
    public GameObject itemPanel;             // 웨이브 클리어 시 보상
    public GameObject itemInventoryPanel;    // 언제든 볼 수 있는 인벤토리

    private List<GameObject> allPanels = new List<GameObject>();

    private void SetupPanels()
    {
        allPanels.Clear();
        if (profilePanel)   { profilePanel.SetActive(false);   allPanels.Add(profilePanel); }
        if (optionPanel)    { optionPanel.SetActive(false);    allPanels.Add(optionPanel); }
        if (shopPanel)      { shopPanel.SetActive(false);      allPanels.Add(shopPanel); }
        if (drawPanel)      { drawPanel.SetActive(false);      allPanels.Add(drawPanel); }
        if (clanPanel)      { clanPanel.SetActive(false);      allPanels.Add(clanPanel); }
        if (characterPanel) { characterPanel.SetActive(false); allPanels.Add(characterPanel); }

        if (deckObject)    deckObject.SetActive(false);
        if (upgradeObject) upgradeObject.SetActive(false);

        if (friendGameObject)  friendGameObject.SetActive(false);
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

    // ====================
    //  캐릭터/프로필 패널
    // ====================

    public void OnClickOpenCharacterPanel()
    {
        CloseAllPanels();
        if (characterPanel) characterPanel.SetActive(true);

        if (deckObject) deckObject.SetActive(true);
        if (upgradeObject) upgradeObject.SetActive(false);

        DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        if (dpm != null)
        {
            dpm.isUpgradeMode = false;
            Debug.Log("[LobbySceneManager] 캐릭터 패널 -> 업그레이드 모드 해제");
        }
    }

    public void OnClickCloseCharacterPanel()
    {
        if (characterPanel) characterPanel.SetActive(false);
    }

    public void OnClickOpenProfilePanel()
    {
        CloseAllPanels();
        if (profilePanel) 
            profilePanel.SetActive(true);
    }
    public void OnClickCloseProfilePanel()
    {
        if (profilePanel) 
            profilePanel.SetActive(false);
    }

    public void OnClickOpenOptionPanel()
    {
        CloseAllPanels();
        if (optionPanel) 
            optionPanel.SetActive(true);
    }
    public void OnClickCloseOptionPanel()
    {
        if (optionPanel) 
            optionPanel.SetActive(false);
    }

    public void OnClickOpenShopPanel()
    {
        CloseAllPanels();
        if (shopPanel) 
            shopPanel.SetActive(true);
    }
    public void OnClickCloseShopPanel()
    {
        if (shopPanel) 
            shopPanel.SetActive(false);
    }

    public void OnClickOpenDrawPanel()
    {
        CloseAllPanels();
        if (drawPanel) 
            drawPanel.SetActive(true);
    }
    public void OnClickCloseDrawPanel()
    {
        if (drawPanel) 
            drawPanel.SetActive(false);
    }

    public void OnClickOpenClanPanel()
    {
        CloseAllPanels();
        if (clanPanel) 
            clanPanel.SetActive(true);
    }
    public void OnClickCloseClanPanel()
    {
        if (clanPanel) 
            clanPanel.SetActive(false);
    }

    // ======================================
    // 덱 / 업그레이드 버튼 토글
    // ======================================
    public void OnClickDeckButton()
    {
        if (deckObject) 
            deckObject.SetActive(true);
        if (upgradeObject) 
            upgradeObject.SetActive(false);

        DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        if (dpm != null)
        {
            dpm.isUpgradeMode = false;
            Debug.Log("[LobbySceneManager] 덱 열림 -> 업그레이드 모드 해제");
        }
    }

    public void OnClickUpgradeButton()
    {
        if (deckObject) 
            deckObject.SetActive(false);
        if (upgradeObject) 
            upgradeObject.SetActive(true);

        DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        if (dpm != null)
        {
            dpm.isUpgradeMode = true;
            Debug.Log("[LobbySceneManager] 업그레이드 열림 -> 업그레이드 모드 활성");
        }
    }

    public void OnClickCloseDeck()
    {
        if (deckObject) 
            deckObject.SetActive(false);
    }

    public void OnClickCloseUpgrade()
    {
        if (upgradeObject) 
            upgradeObject.SetActive(false);
    }

    public void OnClickProfileFriend()
    {
        if (friendGameObject)  
            friendGameObject.SetActive(true);
        if (rankingGameObject) 
            rankingGameObject.SetActive(false);
    }

    public void OnClickProfileRanking()
    {
        if (friendGameObject)  
            friendGameObject.SetActive(false);
        if (rankingGameObject) 
            rankingGameObject.SetActive(true);
    }

    [Header("튜토리얼/스토리 text")]
    [SerializeField] private TextMeshProUGUI explainText;

    private readonly string tutorialStr =
@"1. 5×5 칸 중 원하는 곳에 캐릭터를 소환(클릭 or 드래그)
2. 이미 캐릭터가 있으면, 체력 상황에 따라 교체 or 합성 가능
3. 각 캐릭터는 사거리, 공격패턴이 달라 전략적으로 배치";

    private readonly string storyStr =
@"1. X/O 두 세력의 전쟁, 5×5 전장에서 각자 병사 배치
2. 다양한 캐릭터, 공격 범위/버프/광역 등 역할을 활용
3. 병사를 효율적으로 배치해 전쟁에서 승리하라!";

    public void OnClickTutorialButton()
    {
        if (explainText) 
            explainText.text = tutorialStr;
    }

    public void OnClickStoryButton()
    {
        if (explainText) 
            explainText.text = storyStr;
    }

    public void OnClickClearExplainText()
    {
        if (explainText) 
            explainText.text = "";
    }

    /// <summary>
    /// 외부에서 필요 시 패널 새로고침
    /// </summary>
    public void RefreshPanel()
    {
        deckPanelManager.RefreshInventoryUI();
    }
}
