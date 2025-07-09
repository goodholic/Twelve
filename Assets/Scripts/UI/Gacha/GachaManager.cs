using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using GuildMaster.Data;

/// <summary>
/// 가챠 시스템 매니저 - 단일/10연차 캐릭터 뽑기 시스템
/// </summary>
public class GachaManager : MonoBehaviour
{
    private static GachaManager instance;
    public static GachaManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GachaManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GachaManager");
                    instance = go.AddComponent<GachaManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("가챠 설정")]
    [SerializeField] private int singleDrawCost = 100;     // 단일 뽑기 비용 (골드)
    [SerializeField] private int tenDrawCost = 900;        // 10연차 비용 (10% 할인)
    [SerializeField] private int guaranteedStarLevel = 2;   // 10연차 보장 등급

    [Header("확률 설정")]
    [SerializeField] private float star1Probability = 70f;  // 1성 확률 (70%)
    [SerializeField] private float star2Probability = 25f;  // 2성 확률 (25%)
    [SerializeField] private float star3Probability = 5f;   // 3성 확률 (5%)

    [Header("UI 참조")]
    [SerializeField] private GameObject gachaUI;
    [SerializeField] private Button singleDrawButton;
    [SerializeField] private Button tenDrawButton;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI singleCostText;
    [SerializeField] private TextMeshProUGUI tenCostText;

    [Header("결과 UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Transform resultContainer;
    [SerializeField] private GameObject characterResultPrefab;
    [SerializeField] private Button closeResultButton;

    [Header("애니메이션")]
    [SerializeField] private float drawAnimationDuration = 2f;
    [SerializeField] private AudioClip drawSound;
    [SerializeField] private AudioClip rareDrawSound;

    // 캐릭터 데이터베이스 참조 - CharacterCSVDatabase 대신 GameObject로 참조
    [Header("데이터베이스")]
    [SerializeField] private GameObject characterDatabaseObject;
    private Component characterDatabase;
    
    // 뽑기 기록
    private List<GachaResult> drawHistory = new List<GachaResult>();
    private List<GachaResult> lastDrawResults = new List<GachaResult>();
    
    // 골드 시스템 연동
    private int currentGold = 1000; // 기본 골드

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeGachaSystem();
    }

    /// <summary>
    /// 가챠 시스템 초기화
    /// </summary>
    private void InitializeGachaSystem()
    {
        // 캐릭터 데이터베이스 로드
        LoadCharacterDatabase();
        
        // UI 설정
        SetupUI();
        
        // 초기 골드 UI 업데이트
        UpdateGoldUI();
        
        Debug.Log("[GachaManager] 가챠 시스템 초기화 완료");
    }

    /// <summary>
    /// 캐릭터 데이터베이스 로드
    /// </summary>
    private void LoadCharacterDatabase()
    {
        // CharacterCSVDatabase 컴포넌트를 동적으로 찾기
        if (characterDatabaseObject != null)
        {
            characterDatabase = characterDatabaseObject.GetComponent("CharacterCSVDatabase");
        }
        else
        {
            // Scene에서 CharacterCSVDatabase 찾기
            GameObject dbObject = GameObject.Find("CharacterCSVDatabase");
            if (dbObject != null)
            {
                characterDatabase = dbObject.GetComponent("CharacterCSVDatabase");
                characterDatabaseObject = dbObject;
            }
        }
        
        if (characterDatabase == null)
        {
            Debug.LogError("[GachaManager] CharacterCSVDatabase를 찾을 수 없습니다!");
            return;
        }
        
        Debug.Log("[GachaManager] CharacterCSVDatabase 로드 완료");
    }

    /// <summary>
    /// UI 설정
    /// </summary>
    private void SetupUI()
    {
        // 버튼 이벤트 연결
        if (singleDrawButton != null)
        {
            singleDrawButton.onClick.AddListener(() => StartDrawing(1));
        }
        
        if (tenDrawButton != null)
        {
            tenDrawButton.onClick.AddListener(() => StartDrawing(10));
        }
        
        if (closeResultButton != null)
        {
            closeResultButton.onClick.AddListener(CloseResultPanel);
        }
        
        // 비용 텍스트 설정
        if (singleCostText != null)
        {
            singleCostText.text = $"{singleDrawCost} 골드";
        }
        
        if (tenCostText != null)
        {
            tenCostText.text = $"{tenDrawCost} 골드";
        }
        
        // 결과 패널 숨기기
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 뽑기 시작
    /// </summary>
    public void StartDrawing(int drawCount)
    {
        int totalCost = drawCount == 1 ? singleDrawCost : tenDrawCost;
        
        // 골드 체크
        if (currentGold < totalCost)
        {
            Debug.LogWarning("[GachaManager] 골드가 부족합니다!");
            ShowInsufficientGoldMessage();
            return;
        }
        
        // 골드 차감
        currentGold -= totalCost;
        UpdateGoldUI();
        
        // 뽑기 수행
        StartCoroutine(PerformDrawAnimation(drawCount));
    }

    /// <summary>
    /// 뽑기 애니메이션 수행
    /// </summary>
    private IEnumerator PerformDrawAnimation(int drawCount)
    {
        // UI 비활성화
        SetDrawButtonsInteractable(false);
        
        // 뽑기 사운드 재생
        if (drawSound != null)
        {
            AudioSource.PlayClipAtPoint(drawSound, transform.position);
        }
        
        // 애니메이션 대기
        yield return new WaitForSeconds(drawAnimationDuration);
        
        // 뽑기 실행
        List<GachaResult> results = PerformDraw(drawCount);
        lastDrawResults = results;
        
        // 희귀 캐릭터 뽑기 시 특별 사운드
        if (results.Any(r => r.character != null && r.character.star >= 3))
        {
            if (rareDrawSound != null)
            {
                AudioSource.PlayClipAtPoint(rareDrawSound, transform.position);
            }
        }
        
        // 결과 표시
        ShowDrawResults(results);
        
        // 인벤토리에 추가
        AddCharactersToInventory(results);
        
        // UI 재활성화
        SetDrawButtonsInteractable(true);
    }

    /// <summary>
    /// 실제 뽑기 수행
    /// </summary>
    private List<GachaResult> PerformDraw(int count)
    {
        List<GachaResult> results = new List<GachaResult>();
        bool guaranteedUsed = false;
        
        for (int i = 0; i < count; i++)
        {
            int starLevel;
            
            // 10연차이고 아직 보장을 사용하지 않았으며, 마지막 뽑기인 경우
            if (count == 10 && !guaranteedUsed && i == count - 1)
            {
                // 지금까지 최소 등급 이상이 없었다면 보장
                if (!results.Any(r => r.character != null && r.character.star >= guaranteedStarLevel))
                {
                    starLevel = guaranteedStarLevel;
                    guaranteedUsed = true;
                }
                else
                {
                    starLevel = RollStarLevel();
                }
            }
            else
            {
                starLevel = RollStarLevel();
                
                // 보장 등급 이상이 나왔다면 보장 사용한 것으로 처리
                if (starLevel >= guaranteedStarLevel)
                {
                    guaranteedUsed = true;
                }
            }
            
            // 캐릭터 획득
            CharacterData character = GetRandomCharacterByStarLevel(starLevel);
            
            if (character != null)
            {
                results.Add(new GachaResult
                {
                    character = character,
                    isNew = !IsCharacterOwned(character.characterName)
                });
            }
        }
        
        return results;
    }

    /// <summary>
    /// 별 등급 뽑기
    /// </summary>
    private int RollStarLevel()
    {
        float roll = Random.Range(0f, 100f);
        
        if (roll < star3Probability)
        {
            return 3;
        }
        else if (roll < star3Probability + star2Probability)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }

    /// <summary>
    /// 특정 등급의 랜덤 캐릭터 가져오기
    /// </summary>
    private CharacterData GetRandomCharacterByStarLevel(int starLevel)
    {
        if (characterDatabase == null) return null;
        
        // Reflection을 사용해 GetCharactersByStar 메서드 호출
        var method = characterDatabase.GetType().GetMethod("GetCharactersByStar");
        if (method != null)
        {
            var characters = method.Invoke(characterDatabase, new object[] { starLevel }) as List<CharacterData>;
            if (characters != null && characters.Count > 0)
            {
                return characters[Random.Range(0, characters.Count)];
            }
        }
        
        Debug.LogWarning($"[GachaManager] {starLevel}성 캐릭터를 가져올 수 없습니다!");
        return null;
    }

    /// <summary>
    /// 캐릭터 보유 여부 확인
    /// </summary>
    private bool IsCharacterOwned(string characterName)
    {
        if (CharacterInventoryManager.Instance != null)
        {
            return CharacterInventoryManager.Instance.GetOwnedCharacters()
                .Any(c => c.characterName == characterName);
        }
        return false;
    }

    /// <summary>
    /// 뽑기 결과 표시
    /// </summary>
    private void ShowDrawResults(List<GachaResult> results)
    {
        if (resultPanel == null || resultContainer == null) return;
        
        // 기존 결과 아이템 삭제
        foreach (Transform child in resultContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 새 결과 아이템 생성
        foreach (var result in results)
        {
            if (characterResultPrefab != null && result.character != null)
            {
                GameObject resultItem = Instantiate(characterResultPrefab, resultContainer);
                
                // 결과 아이템 설정 (컴포넌트가 있다고 가정)
                // 실제 프로젝트에 맞게 수정 필요
                ConfigureResultItem(resultItem, result);
            }
        }
        
        // 결과 패널 표시
        resultPanel.SetActive(true);
    }

    /// <summary>
    /// 결과 아이템 설정
    /// </summary>
    private void ConfigureResultItem(GameObject item, GachaResult result)
    {
        // 이름 텍스트
        TextMeshProUGUI nameText = item.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = result.character.characterName;
        }
        
        // NEW 표시
        Transform newIndicator = item.transform.Find("NewIndicator");
        if (newIndicator != null)
        {
            newIndicator.gameObject.SetActive(result.isNew);
        }
        
        // 별 등급 표시
        Transform starContainer = item.transform.Find("StarContainer");
        if (starContainer != null)
        {
            for (int i = 0; i < starContainer.childCount; i++)
            {
                starContainer.GetChild(i).gameObject.SetActive(i < result.character.star);
            }
        }
    }

    /// <summary>
    /// 인벤토리에 캐릭터 추가
    /// </summary>
    private void AddCharactersToInventory(List<GachaResult> results)
    {
        if (CharacterInventoryManager.Instance == null) return;
        
        foreach (var result in results)
        {
            if (result.character != null)
            {
                CharacterInventoryManager.Instance.AddCharacter(result.character);
                
                // 뽑기 기록에 추가
                drawHistory.Add(result);
            }
        }
        
        // 인벤토리 저장
        CharacterInventoryManager.Instance.SaveCharacters();
    }

    /// <summary>
    /// 결과 패널 닫기
    /// </summary>
    private void CloseResultPanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 골드 UI 업데이트
    /// </summary>
    private void UpdateGoldUI()
    {
        if (goldText != null)
        {
            goldText.text = currentGold.ToString();
        }
    }

    /// <summary>
    /// 뽑기 버튼 활성화/비활성화
    /// </summary>
    private void SetDrawButtonsInteractable(bool interactable)
    {
        if (singleDrawButton != null)
        {
            singleDrawButton.interactable = interactable;
        }
        
        if (tenDrawButton != null)
        {
            tenDrawButton.interactable = interactable;
        }
    }

    /// <summary>
    /// 골드 부족 메시지 표시
    /// </summary>
    private void ShowInsufficientGoldMessage()
    {
        // 실제 프로젝트에 맞게 구현
        Debug.Log("[GachaManager] 골드가 부족합니다!");
    }

    /// <summary>
    /// 골드 추가 (외부에서 호출용)
    /// </summary>
    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateGoldUI();
    }

    /// <summary>
    /// 현재 골드 가져오기
    /// </summary>
    public int GetCurrentGold()
    {
        return currentGold;
    }

    /// <summary>
    /// 마지막 뽑기 결과 가져오기
    /// </summary>
    public List<GachaResult> GetLastDrawResults()
    {
        return new List<GachaResult>(lastDrawResults);
    }

    /// <summary>
    /// 뽑기 기록 가져오기
    /// </summary>
    public List<GachaResult> GetDrawHistory()
    {
        return new List<GachaResult>(drawHistory);
    }
}

/// <summary>
/// 가챠 결과 데이터
/// </summary>
[System.Serializable]
public class GachaResult
{
    public CharacterData character;
    public bool isNew;
}