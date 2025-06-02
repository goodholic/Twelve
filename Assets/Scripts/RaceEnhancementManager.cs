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
    
    [Header("종족별 강화 버튼")]
    [SerializeField] private Button humanEnhanceButton;
    [SerializeField] private Button orcEnhanceButton;
    [SerializeField] private Button elfEnhanceButton;
    
    [Header("종족별 강화 레벨 텍스트")]
    [SerializeField] private TextMeshProUGUI humanLevelText;
    [SerializeField] private TextMeshProUGUI orcLevelText;
    [SerializeField] private TextMeshProUGUI elfLevelText;
    
    [Header("강화 비용")]
    [SerializeField] private float costMultiplier = 1.5f;
    
    [Header("강화 수치")]
    [SerializeField] private float enhancePercentage = 0.05f; // 5% 증가
    
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
        // 버튼 리스너 등록
        if (humanEnhanceButton != null)
        {
            humanEnhanceButton.onClick.RemoveAllListeners();
            humanEnhanceButton.onClick.AddListener(() => OnEnhanceRace(CharacterRace.Human));
        }
        
        if (orcEnhanceButton != null)
        {
            orcEnhanceButton.onClick.RemoveAllListeners();
            orcEnhanceButton.onClick.AddListener(() => OnEnhanceRace(CharacterRace.Orc));
        }
        
        if (elfEnhanceButton != null)
        {
            elfEnhanceButton.onClick.RemoveAllListeners();
            elfEnhanceButton.onClick.AddListener(() => OnEnhanceRace(CharacterRace.Elf));
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// 종족 강화 버튼 클릭 시 호출
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
            humanLevelText.text = $"Lv.{raceEnhancementLevels[CharacterRace.Human]} (비용: {raceEnhancementCosts[CharacterRace.Human]})";
        }
        
        // 오크
        if (orcLevelText != null)
        {
            orcLevelText.text = $"Lv.{raceEnhancementLevels[CharacterRace.Orc]} (비용: {raceEnhancementCosts[CharacterRace.Orc]})";
        }
        
        // 엘프
        if (elfLevelText != null)
        {
            elfLevelText.text = $"Lv.{raceEnhancementLevels[CharacterRace.Elf]} (비용: {raceEnhancementCosts[CharacterRace.Elf]})";
        }
        
        // 버튼 활성화 여부 체크
        UpdateButtonStates();
    }
    
    /// <summary>
    /// 버튼 활성화 상태 업데이트
    /// </summary>
    private void UpdateButtonStates()
    {
        MineralBar mineralBar = CoreDataManager.Instance.region1MineralBar;
        if (mineralBar == null) return;
        
        int currentMinerals = mineralBar.GetCurrentMinerals();
        
        if (humanEnhanceButton != null)
        {
            humanEnhanceButton.interactable = (currentMinerals >= raceEnhancementCosts[CharacterRace.Human]);
        }
        
        if (orcEnhanceButton != null)
        {
            orcEnhanceButton.interactable = (currentMinerals >= raceEnhancementCosts[CharacterRace.Orc]);
        }
        
        if (elfEnhanceButton != null)
        {
            elfEnhanceButton.interactable = (currentMinerals >= raceEnhancementCosts[CharacterRace.Elf]);
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