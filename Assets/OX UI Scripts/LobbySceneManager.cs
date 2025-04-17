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
        InitializeStages();
        gold = 0;
        diamond = 0;
        PlayerPrefs.DeleteKey("GameData");
        PlayerPrefs.DeleteKey("Gold");
        PlayerPrefs.DeleteKey("Diamond");
        PlayerPrefs.Save();

        currentStageIndex = 0;
        UpdateStageUI();
        UpdateGoldAndDiamondUI();
    }

    private void Awake()
    {
        SetupPanels();
        if (explainText) explainText.text = "";
    }

    private void Start()
    {
        InitializeStages();
        LoadGame();
        UpdateStageUI();
        UpdateGoldAndDiamondUI();
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
                stages[i].isUnlocked     = data.stageInfos[i].isUnlocked;
                stages[i].winCount       = data.stageInfos[i].winCount;
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
        if (goldText)    goldText.text    = gold.ToString();
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
            stageIndexText.text = $"Stage {currentStageIndex + 1}";

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

        Debug.Log($"Stage {currentStageIndex + 1} 입장 -> GameScene 이동");
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

        // 예시 로직: 5회 플레이 완료 후 3회 이상 승리 시 다음 스테이지 해금
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

        if (friendGameObject)   friendGameObject.SetActive(false);
        if (rankingGameObject)  rankingGameObject.SetActive(false);
    }

    private void CloseAllPanels()
    {
        foreach (var p in allPanels)
        {
            if (p) p.SetActive(false);
        }
    }

    public void OnClickOpenCharacterPanel()
    {
        CloseAllPanels();
        if (characterPanel)
        {
            var cpm = characterPanel.GetComponent<CharacterPanelManager>();
            if (cpm)
            {
                cpm.OpenCharacterPanel();
            }
            else
            {
                characterPanel.SetActive(true);
            }
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

    public void OnClickDeckButton()
    {
        if (deckObject) deckObject.SetActive(true);
        if (upgradeObject) upgradeObject.SetActive(false);
    }
    public void OnClickUpgradeButton()
    {
        if (deckObject) deckObject.SetActive(false);
        if (upgradeObject) upgradeObject.SetActive(true);
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
        if (friendGameObject)   friendGameObject.SetActive(true);
        if (rankingGameObject)  rankingGameObject.SetActive(false);
    }
    public void OnClickProfileRanking()
    {
        if (friendGameObject)   friendGameObject.SetActive(false);
        if (rankingGameObject)  rankingGameObject.SetActive(true);
    }

    [Header("튜토리얼/스토리 text")]
    [SerializeField] private TextMeshProUGUI explainText;

    private readonly string tutorialStr =
@"1. 5×5 칸 중 원하는 곳을 클릭해 캐릭터를 소환하되, 이미 다른 캐릭터가 있으면 체력을 0으로 만들 수 있을 때만 교체 가능.
2. 체력이 남아있으면 배치 불가, 둘 다 죽으면 빈 칸이 되고, 내 캐릭터만 살아남으면 배치.
3. X/O 두 팀 외 다양한 캐릭터 사용 가능. 공격 패턴(대각,직선,특수)은 자유 설정.";

    private readonly string storyStr =
@"1. X/O 두 세력간 전쟁, 5×5 전장에서 병사 배치로 승부.
2. 병사는 고유 범위/공격력으로 적을 쓰러뜨리거나 자신까지 희생하기도.
3. 한 줄 완성해 세력 확장. 수많은 전투와 희생은 플레이어의 전략을 시험.";

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
}
