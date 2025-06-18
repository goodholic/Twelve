using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 종족별 강화 시스템 매니저
/// 인게임 중 종족별로 공격력/공격속도 5% 증가
/// </summary>
public class RaceEnhancementManager : MonoBehaviour
{
    private static RaceEnhancementManager instance;
    public static RaceEnhancementManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<RaceEnhancementManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("RaceEnhancementManager");
                    instance = go.AddComponent<RaceEnhancementManager>();
                }
            }
            return instance;
        }
    }
    
    [Header("종족 선택 버튼")]
    [SerializeField] private Button humanSelectButton;
    [SerializeField] private Button orcSelectButton;
    [SerializeField] private Button elfSelectButton;
    
    [Header("강화 실행 버튼")]
    [SerializeField] private Button enhanceButton;
    
    [Header("종족별 강화 레벨 텍스트")]
    [SerializeField] private TextMeshProUGUI humanLevelText;
    [SerializeField] private TextMeshProUGUI orcLevelText;
    [SerializeField] private TextMeshProUGUI elfLevelText;
    
    [Header("강화 비용 텍스트")]
    [SerializeField] private TextMeshProUGUI enhanceCostText;
    
    [Header("선택 표시 아웃라인")]
    [SerializeField] private Outline humanOutline;
    [SerializeField] private Outline orcOutline;
    [SerializeField] private Outline elfOutline;
    [SerializeField] private Color selectedOutlineColor = new Color(1f, 0.8f, 0f, 1f); // 금색
    [SerializeField] private float outlineWidth = 3f;
    
    [Header("강화 비용")]
    [SerializeField] private float costMultiplier = 1.5f;
    
    [Header("강화 수치")]
    [SerializeField] private float enhancePercentage = 0.05f; // 5% 증가
    
    // 현재 선택된 종족
    private CharacterRace selectedRace = CharacterRace.Human;
    
    // 종족별 강화 레벨
    private Dictionary<CharacterRace, int> raceEnhancementLevels = new Dictionary<CharacterRace, int>()
    {
        { CharacterRace.Human, 0 },
        { CharacterRace.Orc, 0 },
        { CharacterRace.Elf, 0 }
    };
    
    // 종족별 강화 비용
    private Dictionary<CharacterRace, int> raceEnhancementCosts = new Dictionary<CharacterRace, int>()
    {
        { CharacterRace.Human, 50 },
        { CharacterRace.Orc, 50 },
        { CharacterRace.Elf, 50 }
    };
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    
    private void Start()
    {
        // 종족 선택 버튼 리스너 등록
        if (humanSelectButton != null)
        {
            humanSelectButton.onClick.RemoveAllListeners();
            humanSelectButton.onClick.AddListener(() => OnSelectRace(CharacterRace.Human));
        }
        
        if (orcSelectButton != null)
        {
            orcSelectButton.onClick.RemoveAllListeners();
            orcSelectButton.onClick.AddListener(() => OnSelectRace(CharacterRace.Orc));
        }
        
        if (elfSelectButton != null)
        {
            elfSelectButton.onClick.RemoveAllListeners();
            elfSelectButton.onClick.AddListener(() => OnSelectRace(CharacterRace.Elf));
        }
        
        // 강화 버튼 리스너 등록
        if (enhanceButton != null)
        {
            enhanceButton.onClick.RemoveAllListeners();
            enhanceButton.onClick.AddListener(OnEnhanceSelectedRace);
        }
        
        // 아웃라인 컴포넌트가 없으면 추가
        EnsureOutlineComponents();
        
        // 초기 선택 상태 설정 (Human)
        OnSelectRace(CharacterRace.Human);
        
        UpdateUI();
    }
    
    /// <summary>
    /// 아웃라인 컴포넌트 확인 및 추가
    /// </summary>
    private void EnsureOutlineComponents()
    {
        if (humanSelectButton != null && humanOutline == null)
        {
            humanOutline = humanSelectButton.GetComponent<Outline>();
            if (humanOutline == null)
            {
                humanOutline = humanSelectButton.gameObject.AddComponent<Outline>();
            }
        }
        
        if (orcSelectButton != null && orcOutline == null)
        {
            orcOutline = orcSelectButton.GetComponent<Outline>();
            if (orcOutline == null)
            {
                orcOutline = orcSelectButton.gameObject.AddComponent<Outline>();
            }
        }
        
        if (elfSelectButton != null && elfOutline == null)
        {
            elfOutline = elfSelectButton.GetComponent<Outline>();
            if (elfOutline == null)
            {
                elfOutline = elfSelectButton.gameObject.AddComponent<Outline>();
            }
        }
        
        // 모든 아웃라인 초기 설정
        if (humanOutline != null)
        {
            humanOutline.effectColor = selectedOutlineColor;
            humanOutline.effectDistance = new Vector2(outlineWidth, outlineWidth);
            humanOutline.enabled = false;
        }
        
        if (orcOutline != null)
        {
            orcOutline.effectColor = selectedOutlineColor;
            orcOutline.effectDistance = new Vector2(outlineWidth, outlineWidth);
            orcOutline.enabled = false;
        }
        
        if (elfOutline != null)
        {
            elfOutline.effectColor = selectedOutlineColor;
            elfOutline.effectDistance = new Vector2(outlineWidth, outlineWidth);
            elfOutline.enabled = false;
        }
    }
    
    /// <summary>
    /// 종족 선택 시 호출
    /// </summary>
    private void OnSelectRace(CharacterRace race)
    {
        selectedRace = race;
        
        // 모든 아웃라인 비활성화
        if (humanOutline != null) humanOutline.enabled = false;
        if (orcOutline != null) orcOutline.enabled = false;
        if (elfOutline != null) elfOutline.enabled = false;
        
        // 선택된 종족의 아웃라인 활성화
        switch (race)
        {
            case CharacterRace.Human:
                if (humanOutline != null) humanOutline.enabled = true;
                break;
            case CharacterRace.Orc:
                if (orcOutline != null) orcOutline.enabled = true;
                break;
            case CharacterRace.Elf:
                if (elfOutline != null) elfOutline.enabled = true;
                break;
        }
        
        // 강화 비용 표시 업데이트
        UpdateEnhanceCostText();
        
        Debug.Log($"[RaceEnhancementManager] {race} 종족 선택됨");
    }
    
    /// <summary>
    /// 선택된 종족 강화 실행
    /// </summary>
    private void OnEnhanceSelectedRace()
    {
        OnEnhanceRace(selectedRace);
    }
    
    /// <summary>
    /// 종족 강화 실행
    /// </summary>
    private void OnEnhanceRace(CharacterRace race)
    {
        int cost = raceEnhancementCosts[race];
        
        // 미네랄 체크
        MineralBar mineralBar = CoreDataManager.Instance.region1MineralBar;
        if (mineralBar == null)
        {
            Debug.LogWarning("[RaceEnhancementManager] 미네랄바를 찾을 수 없습니다.");
            return;
        }
        
        if (!mineralBar.TrySpend(cost))
        {
            Debug.Log($"[RaceEnhancementManager] {race} 강화 실패 - 미네랄 부족 (필요: {cost})");
            return;
        }
        
        // 강화 레벨 증가
        raceEnhancementLevels[race]++;
        
        // 비용 증가
        raceEnhancementCosts[race] = Mathf.RoundToInt(cost * costMultiplier);
        
        // 현재 필드에 있는 모든 해당 종족 캐릭터 강화
        ApplyEnhancementToExistingCharacters(race);
        
        // UI 업데이트
        UpdateUI();
        
        Debug.Log($"[RaceEnhancementManager] {race} 종족 강화 완료! 레벨: {raceEnhancementLevels[race]}");
    }
    
    /// <summary>
    /// 이미 필드에 있는 캐릭터들에게 강화 적용
    /// </summary>
    private void ApplyEnhancementToExistingCharacters(CharacterRace race)
    {
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var character in allCharacters)
        {
            if (character != null && character.race == race && character.areaIndex == 1)
            {
                // 5% 스탯 증가
                character.attackPower *= (1f + enhancePercentage);
                character.attackSpeed *= (1f + enhancePercentage);
                
                Debug.Log($"[RaceEnhancementManager] {character.characterName} 강화됨 - 공격력: {character.attackPower:F1}, 공격속도: {character.attackSpeed:F2}");
            }
        }
    }
    
    /// <summary>
    /// 새로 소환되는 캐릭터에 강화 적용
    /// </summary>
    public void ApplyEnhancementToNewCharacter(Character character)
    {
        if (character == null || character.areaIndex != 1) return;
        
        int enhanceLevel = raceEnhancementLevels[character.race];
        if (enhanceLevel <= 0) return;
        
        float totalMultiplier = 1f + (enhancePercentage * enhanceLevel);
        character.attackPower *= totalMultiplier;
        character.attackSpeed *= totalMultiplier;
        
        Debug.Log($"[RaceEnhancementManager] 신규 캐릭터 {character.characterName} 강화 적용 (레벨 {enhanceLevel}) - 공격력: {character.attackPower:F1}, 공격속도: {character.attackSpeed:F2}");
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        // 휴먼
        if (humanLevelText != null)
        {
            humanLevelText.text = $"Lv.{raceEnhancementLevels[CharacterRace.Human]}";
        }
        
        // 오크
        if (orcLevelText != null)
        {
            orcLevelText.text = $"Lv.{raceEnhancementLevels[CharacterRace.Orc]}";
        }
        
        // 엘프
        if (elfLevelText != null)
        {
            elfLevelText.text = $"Lv.{raceEnhancementLevels[CharacterRace.Elf]}";
        }
        
        // 강화 비용 텍스트 업데이트
        UpdateEnhanceCostText();
        
        // 버튼 활성화 여부 체크
        UpdateButtonStates();
    }
    
    /// <summary>
    /// 강화 비용 텍스트 업데이트
    /// </summary>
    private void UpdateEnhanceCostText()
    {
        if (enhanceCostText != null)
        {
            int cost = raceEnhancementCosts[selectedRace];
            enhanceCostText.text = $"{selectedRace} 강화 비용: {cost}";
        }
    }
    
    /// <summary>
    /// 버튼 활성화 상태 업데이트
    /// </summary>
    private void UpdateButtonStates()
    {
        MineralBar mineralBar = CoreDataManager.Instance.region1MineralBar;
        if (mineralBar == null) return;
        
        int currentMinerals = mineralBar.GetCurrentMinerals();
        int selectedRaceCost = raceEnhancementCosts[selectedRace];
        
        // 강화 버튼 활성화 여부
        if (enhanceButton != null)
        {
            enhanceButton.interactable = (currentMinerals >= selectedRaceCost);
        }
    }
    
    /// <summary>
    /// 종족의 현재 강화 레벨 반환
    /// </summary>
    public int GetRaceEnhancementLevel(CharacterRace race)
    {
        return raceEnhancementLevels.ContainsKey(race) ? raceEnhancementLevels[race] : 0;
    }
    
    /// <summary>
    /// 종족의 총 강화 배수 반환 (1 + 0.05 * 레벨)
    /// </summary>
    public float GetRaceEnhancementMultiplier(CharacterRace race)
    {
        int level = GetRaceEnhancementLevel(race);
        return 1f + (enhancePercentage * level);
    }
    
    private void Update()
    {
        // 주기적으로 버튼 상태 업데이트
        if (Time.frameCount % 30 == 0) // 0.5초마다
        {
            UpdateButtonStates();
        }
    }
}