using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class StageInfo
{
    public int stageIndex;
    public bool isUnlocked;
    public int totalAttempts;
    public int winCount;
    public bool[] attemptResults;
}

public class LobbySceneManager : MonoBehaviour
{
    [Header("스테이지 개수")]
    [SerializeField] private int totalStages = 100;

    // ======================================================
    // (변경) 스테이지별 이미지를 "게임 오브젝트"로 사용
    // ======================================================
    [Header("스테이지별 오브젝트(인덱스 순)")]
    [Tooltip("각 스테이지마다 켜고 끌 GameObject를 연결 (씬에 존재하거나 프리팹이면, 미리 배치 후 SetActive로 제어)")]
    [SerializeField] private GameObject[] stageObjects;

    // 기존 "stageThumbnail"와 "stageLockIcon"은 잠금 아이콘/UI 표시용으로 유지
    [Header("스테이지 락 아이콘/썸네일 (UI 이미지)")]
    [SerializeField] private Image stageLockIcon;
    [SerializeField] private Image stageThumbnail;  
    // ↑ 여기서는 굳이 Sprite를 지정하지 않고, 단순히 "잠겼을 때/잠금해제일 때 UI 표시" 용도로만 씁니다.

    // 인벤토리/덱 매니저
    [Header("CharacterInventoryManager(인벤토리)")]
    [SerializeField] private CharacterInventoryManager characterInventoryManager;

    [Header("DeckPanelManager(덱 패널)")]
    [SerializeField] private DeckPanelManager deckPanelManager;

    // 스테이지 정보
    private List<StageInfo> stages = new List<StageInfo>();
    private int currentStageIndex = 0;

    // (기존) 재화 관련
    private int gold = 0;
    private int diamond = 0;

    [Header("골드/다이아 표시 UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI diamondText;

    [Header("스테이지 이동 버튼/UI")]
    [SerializeField] private Button stageLeftButton;
    [SerializeField] private Button stageRightButton;
    [SerializeField] private TextMeshProUGUI stageIndexText;

    [Header("5회 플레이 체크 아이콘(승리=checkSprite)")]
    [SerializeField] private Image[] stageAttemptIcons;
    [SerializeField] private Sprite checkSprite;
    [SerializeField] private Sprite emptySprite;

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
    public GameObject itemPanel;
    public GameObject itemInventoryPanel;

    [Header("튜토리얼/스토리 text")]
    [SerializeField] private TextMeshProUGUI explainText;

    private List<GameObject> allPanels = new List<GameObject>();
    private readonly string tutorialStr =
@"1. 5×5 칸 중 원하는 곳에 캐릭터를 소환(클릭 or 드래그)
2. 이미 캐릭터가 있으면, 교체 or 합성 가능
3. 각 캐릭터는 사거리, 공격패턴이 달라 전략적으로 배치";
    private readonly string storyStr =
@"1. X/O 두 세력의 전쟁, 5×5 전장에서 각자 병사 배치
2. 다양한 캐릭터, 공격 범위/버프/광역 등 역할을 활용
3. 병사를 효율적으로 배치해 전쟁에서 승리하라!";

    private void Awake()
    {
        SetupPanels();
        if (explainText)
            explainText.text = "";
    }

    private void Start()
    {
        // 스테이지 리스트 초기화
        InitializeStages();

        // (재화 UI 등) - 로컬 저장/로드 삭제
        UpdateStageUI();
        UpdateGoldAndDiamondUI();

        // 덱/업그레이드 UI 초기화
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

    /// <summary>
    /// 스테이지 리스트 기본 세팅 (totalStages만큼)
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
        currentStageIndex = 0;
    }

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

    /// <summary>
    /// 골드/다이아 UI만 갱신 (로컬 저장은 제거)
    /// </summary>
    private void UpdateGoldAndDiamondUI()
    {
        if (goldText)    goldText.text    = gold.ToString();
        if (diamondText) diamondText.text = diamond.ToString();
    }

    /// <summary>
    /// 현재 스테이지 UI 반영
    /// </summary>
    private void UpdateStageUI()
    {
        if (stageIndexText)
            stageIndexText.text = $"{currentStageIndex + 1}";

        StageInfo info = stages[currentStageIndex];
        bool locked = !info.isUnlocked;

        // 락 아이콘 표시
        if (stageLockIcon)   stageLockIcon.gameObject.SetActive(locked);

        // 기존 Image(썸네일)은 그냥 "잠금/해제"만 처리
        if (stageThumbnail)  
        {
            stageThumbnail.gameObject.SetActive(!locked);
        }

        // =========================================================
        //  스테이지별 오브젝트를 활성/비활성 (locked면 전부 off)
        // =========================================================
        if (stageObjects != null && stageObjects.Length > 0)
        {
            // 먼저 전부 끔
            for (int i = 0; i < stageObjects.Length; i++)
            {
                if (stageObjects[i] != null)
                {
                    stageObjects[i].SetActive(false);
                }
            }

            // 잠금 해제 상태이고 배열 범위 안이면 현재 스테이지 인덱스 오브젝트 켜기
            if (!locked && currentStageIndex < stageObjects.Length && stageObjects[currentStageIndex] != null)
            {
                stageObjects[currentStageIndex].SetActive(true);
            }
        }

        // 5회 중 승리했던 기록 표시
        for (int i = 0; i < 5; i++)
        {
            if (stageAttemptIcons != null && i < stageAttemptIcons.Length && stageAttemptIcons[i] != null)
            {
                bool isWin = info.attemptResults[i];
                stageAttemptIcons[i].sprite = isWin ? checkSprite : emptySprite;
            }
        }
    }

    // --------------------------------
    // 스테이지 화살표 버튼
    // --------------------------------
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

    /// <summary>
    /// 스테이지 입장 -> GameScene으로 이동
    /// </summary>
    public void OnClickEnterStage()
    {
        StageInfo info = stages[currentStageIndex];
        if (!info.isUnlocked)
        {
            Debug.Log($"[LobbySceneManager] 스테이지 {currentStageIndex + 1}는 잠겨있습니다.");
            return;
        }

        // 최소 9개 등록 여부 체크
        DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        if (dpm != null)
        {
            CharacterData[] deckSet = dpm.registeredCharactersSet2;
            int count = 0;
            foreach (var c in deckSet)
            {
                if (c != null) count++;
            }
            if (count < 9)
            {
                Debug.LogWarning($"[LobbySceneManager] 덱에 등록된 캐릭터가 {count}개. 최소 9개 필요!");
                return;
            }

            // 9개+Hero(10번째) -> GameManager에 반영
            if (GameManager.Instance != null &&
                GameManager.Instance.currentRegisteredCharacters != null &&
                GameManager.Instance.currentRegisteredCharacters.Length >= 10)
            {
                for (int i = 0; i < deckSet.Length && i < 10; i++)
                {
                    GameManager.Instance.currentRegisteredCharacters[i] = deckSet[i];
                }
                Debug.Log("[LobbySceneManager] 덱(10개) -> GameManager.currentRegisteredCharacters에 반영");
            }
        }
        else
        {
            Debug.LogWarning("[LobbySceneManager] DeckPanelManager를 찾지 못함 -> 진행 불가");
            return;
        }

        // 로비에서 아이템 인벤토리 켜둠 (옵션)
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }

        Debug.Log($"[LobbySceneManager] 스테이지 {currentStageIndex + 1} 입장 -> GameScene 로드");
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// 스테이지 플레이 결과를 기록(진행도)
    /// 여기서 로컬 저장은 제거.
    /// </summary>
    public void RecordStageAttempt(int attemptIndex, bool isWin)
    {
        if (attemptIndex < 0 || attemptIndex >= 5) return;

        StageInfo info = stages[currentStageIndex];
        info.attemptResults[attemptIndex] = isWin;

        int wCount = 0;
        foreach (bool w in info.attemptResults)
            if (w) wCount++;
        info.winCount = wCount;

        // 마지막 시도 후 3회 이상 승리이면 다음 스테이지 언락
        if (attemptIndex == 4 && info.winCount >= 3)
        {
            int next = currentStageIndex + 1;
            if (next < totalStages) stages[next].isUnlocked = true;
        }

        UpdateStageUI();
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

    // 덱/업그레이드 탭
    public void OnClickDeckButton()
    {
        if (deckObject) deckObject.SetActive(true);
        if (upgradeObject) upgradeObject.SetActive(false);

        DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        if (dpm != null)
        {
            dpm.isUpgradeMode = false;
        }
    }

    public void OnClickUpgradeButton()
    {
        if (deckObject) deckObject.SetActive(false);
        if (upgradeObject) upgradeObject.SetActive(true);

        DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
        if (dpm != null)
        {
            dpm.isUpgradeMode = true;
        }
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
        if (friendGameObject)  friendGameObject.SetActive(true);
        if (rankingGameObject) rankingGameObject.SetActive(false);
    }

    public void OnClickProfileRanking()
    {
        if (friendGameObject)  friendGameObject.SetActive(false);
        if (rankingGameObject) rankingGameObject.SetActive(true);
    }

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
    /// 외부에서 패널 새로고침
    /// </summary>
    public void RefreshPanel()
    {
        if (deckPanelManager != null)
        {
            deckPanelManager.RefreshInventoryUI();
        }
    }
}
