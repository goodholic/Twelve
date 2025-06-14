using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 종족 선택 및 리사이클 시스템 관리
/// 게임 기획서: 종족 3개 버튼 중 하나를 선택하면 아웃라인이 빛나고,
/// 리사이클 버튼을 누르면 해당 종족의 모든 캐릭터가 제거됨
/// </summary>
public class RaceRecycleManager : MonoBehaviour
{
    [Header("종족 버튼")]
    [SerializeField] private Button humanButton;
    [SerializeField] private Button orcButton;
    [SerializeField] private Button elfButton;
    
    [Header("종족 버튼 아웃라인")]
    [SerializeField] private Outline humanOutline;
    [SerializeField] private Outline orcOutline;
    [SerializeField] private Outline elfOutline;
    
    [Header("리사이클 버튼")]
    [SerializeField] private Button recycleButton;
    [SerializeField] private TextMeshProUGUI recycleButtonText;
    
    [Header("아웃라인 설정")]
    [SerializeField] private Color selectedOutlineColor = Color.yellow;
    [SerializeField] private float selectedOutlineWidth = 5f;
    
    [Header("종족별 캐릭터 수 텍스트")]
    public TextMeshProUGUI humanCountText;
    public TextMeshProUGUI orcCountText;
    public TextMeshProUGUI elfCountText;
    
    // 현재 선택된 종족
    private CharacterRace? selectedRace = null;
    
    // 종족별 버튼과 아웃라인 매핑
    private Dictionary<CharacterRace, Button> raceButtons;
    private Dictionary<CharacterRace, Outline> raceOutlines;
    
    private void Start()
    {
        // 매핑 초기화
        raceButtons = new Dictionary<CharacterRace, Button>
        {
            { CharacterRace.Human, humanButton },
            { CharacterRace.Orc, orcButton },
            { CharacterRace.Elf, elfButton }
        };
        
        raceOutlines = new Dictionary<CharacterRace, Outline>
        {
            { CharacterRace.Human, humanOutline },
            { CharacterRace.Orc, orcOutline },
            { CharacterRace.Elf, elfOutline }
        };
        
        // 버튼 이벤트 연결
        if (humanButton != null)
            humanButton.onClick.AddListener(() => OnRaceButtonClicked(CharacterRace.Human));
        if (orcButton != null)
            orcButton.onClick.AddListener(() => OnRaceButtonClicked(CharacterRace.Orc));
        if (elfButton != null)
            elfButton.onClick.AddListener(() => OnRaceButtonClicked(CharacterRace.Elf));
        
        if (recycleButton != null)
        {
            recycleButton.onClick.AddListener(OnRecycleButtonClicked);
            recycleButton.interactable = false; // 초기에는 비활성화
        }
        
        // 아웃라인 초기화
        InitializeOutlines();
        
        // 종족별 카운트 업데이트
        UpdateRaceCountTexts();
    }
    
    /// <summary>
    /// 아웃라인 초기 설정
    /// </summary>
    private void InitializeOutlines()
    {
        foreach (var outline in raceOutlines.Values)
        {
            if (outline != null)
            {
                outline.effectColor = selectedOutlineColor;
                outline.effectDistance = new Vector2(selectedOutlineWidth, selectedOutlineWidth);
                outline.enabled = false; // 초기에는 모두 비활성화
            }
        }
    }
    
    /// <summary>
    /// 종족 버튼 클릭 시 호출
    /// </summary>
    private void OnRaceButtonClicked(CharacterRace race)
    {
        Debug.Log($"[RaceRecycleManager] {race} 종족 버튼 클릭됨");
        
        // 이전 선택 해제
        if (selectedRace.HasValue && raceOutlines.ContainsKey(selectedRace.Value))
        {
            if (raceOutlines[selectedRace.Value] != null)
                raceOutlines[selectedRace.Value].enabled = false;
        }
        
        // 새로운 선택
        selectedRace = race;
        
        // 아웃라인 활성화
        if (raceOutlines.ContainsKey(race) && raceOutlines[race] != null)
        {
            raceOutlines[race].enabled = true;
        }
        
        // 리사이클 버튼 활성화 및 텍스트 업데이트
        if (recycleButton != null)
        {
            recycleButton.interactable = true;
            
            if (recycleButtonText != null)
            {
                recycleButtonText.text = $"{GetRaceKoreanName(race)} 제거";
            }
        }
    }
    
    /// <summary>
    /// 리사이클 버튼 클릭 시 호출
    /// </summary>
    private void OnRecycleButtonClicked()
    {
        if (!selectedRace.HasValue)
        {
            Debug.LogWarning("[RaceRecycleManager] 선택된 종족이 없습니다!");
            return;
        }
        
        Debug.Log($"[RaceRecycleManager] {selectedRace.Value} 종족 리사이클 시작");
        
        // 해당 종족의 모든 캐릭터 찾기
        Character[] allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        List<Character> charactersToRemove = new List<Character>();
        
        // GameManager의 currentRegisteredCharacters에서도 해당 종족 제거
        var gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.currentRegisteredCharacters != null)
        {
            for (int i = 0; i < gameManager.currentRegisteredCharacters.Length; i++)
            {
                var charData = gameManager.currentRegisteredCharacters[i];
                if (charData != null && charData.race == selectedRace.Value)
                {
                    gameManager.currentRegisteredCharacters[i] = null;
                    Debug.Log($"[RaceRecycleManager] 등록된 캐릭터에서 {charData.characterName} 제거");
                }
            }
        }
        
        // 씬에 있는 캐릭터들 중 해당 종족 찾기
        foreach (var character in allCharacters)
        {
            if (character != null && !character.isHero) // 히어로는 제외
            {
                // Character의 종족 확인
                CharacterData charData = FindCharacterData(character);
                if (charData != null && charData.race == selectedRace.Value)
                {
                    charactersToRemove.Add(character);
                }
            }
        }
        
        // 찾은 캐릭터들 제거
        int removedCount = 0;
        foreach (var character in charactersToRemove)
        {
            if (character != null)
            {
                // 타일에서 제거
                if (character.currentTile != null)
                {
                    TileManager.Instance.ClearCharacterTileReference(character);
                    TileManager.Instance.OnCharacterRemovedFromTile(character.currentTile);
                }
                
                // 게임 오브젝트 파괴
                Destroy(character.gameObject);
                removedCount++;
            }
        }
        
        Debug.Log($"[RaceRecycleManager] {selectedRace.Value} 종족 캐릭터 {removedCount}개 제거 완료");
        
        // UI 업데이트
        UpdateRaceCountTexts();
        
        // 선택 초기화
        ResetSelection();
    }
    
    /// <summary>
    /// Character 컴포넌트로부터 CharacterData 찾기
    /// </summary>
    private CharacterData FindCharacterData(Character character)
    {
        if (character == null || string.IsNullOrEmpty(character.characterName))
            return null;
        
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
            return null;
        
        // 지역1 캐릭터 확인
        if (character.areaIndex == 1 && coreData.characterDatabase != null)
        {
            foreach (var data in coreData.characterDatabase.currentRegisteredCharacters)
            {
                if (data != null && data.characterName == character.characterName)
                    return data;
            }
        }
        // 지역2 캐릭터 확인
        else if (character.areaIndex == 2)
        {
            if (coreData.enemyDatabase != null && coreData.enemyDatabase.characters != null)
            {
                foreach (var data in coreData.enemyDatabase.characters)
                {
                    if (data != null && data.characterName == character.characterName)
                        return data;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 선택 상태 초기화
    /// </summary>
    private void ResetSelection()
    {
        // 아웃라인 비활성화
        if (selectedRace.HasValue && raceOutlines.ContainsKey(selectedRace.Value))
        {
            if (raceOutlines[selectedRace.Value] != null)
                raceOutlines[selectedRace.Value].enabled = false;
        }
        
        selectedRace = null;
        
        // 리사이클 버튼 비활성화
        if (recycleButton != null)
        {
            recycleButton.interactable = false;
            
            if (recycleButtonText != null)
            {
                recycleButtonText.text = "종족 선택";
            }
        }
    }
    
    /// <summary>
    /// 종족별 캐릭터 수 업데이트
    /// </summary>
    private void UpdateRaceCountTexts()
    {
        int humanCount = 0;
        int orcCount = 0;
        int elfCount = 0;
        
        // 등록된 캐릭터에서 카운트
        var gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.currentRegisteredCharacters != null)
        {
            foreach (var charData in gameManager.currentRegisteredCharacters)
            {
                if (charData != null)
                {
                    switch (charData.race)
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
            }
        }
        
        // 텍스트 업데이트
        if (humanCountText != null)
            humanCountText.text = $"휴먼: {humanCount}";
        if (orcCountText != null)
            orcCountText.text = $"오크: {orcCount}";
        if (elfCountText != null)
            elfCountText.text = $"엘프: {elfCount}";
    }
    
    /// <summary>
    /// 종족 한글 이름 반환
    /// </summary>
    private string GetRaceKoreanName(CharacterRace race)
    {
        switch (race)
        {
            case CharacterRace.Human:
                return "휴먼";
            case CharacterRace.Orc:
                return "오크";
            case CharacterRace.Elf:
                return "엘프";
            default:
                return "기타";
        }
    }
}