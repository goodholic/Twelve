using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace pjy.Managers
{
    /// <summary>
    /// 인게임 강화 시스템 매니저
    /// 전투 중 종족별 강화 버튼으로 즉시 버프 제공
    /// </summary>
    public class InGameEnhanceManager : MonoBehaviour
    {
        [Header("강화 설정")]
        [SerializeField] private int enhanceCost = 100;
        [SerializeField] private float enhanceBonus = 0.05f; // 5%
        [SerializeField] private float enhanceCostMultiplier = 1.5f; // 강화할수록 비용 증가
        [SerializeField] private int maxEnhanceLevel = 10; // 최대 강화 횟수
        
        [Header("종족별 강화 버튼")]
        [SerializeField] private Button humanEnhanceButton;
        [SerializeField] private Button orcEnhanceButton;
        [SerializeField] private Button elfEnhanceButton;
        
        [Header("UI 요소")]
        [SerializeField] private TextMeshProUGUI humanCostText;
        [SerializeField] private TextMeshProUGUI orcCostText;
        [SerializeField] private TextMeshProUGUI elfCostText;
        
        [SerializeField] private TextMeshProUGUI humanLevelText;
        [SerializeField] private TextMeshProUGUI orcLevelText;
        [SerializeField] private TextMeshProUGUI elfLevelText;
        
        [Header("효과")]
        [SerializeField] private GameObject enhanceEffectPrefab;
        [SerializeField] private AudioClip enhanceSound;
        
        // 종족별 강화 레벨
        private Dictionary<CharacterRace, int> enhanceLevels = new Dictionary<CharacterRace, int>();
        private Dictionary<CharacterRace, int> enhanceCosts = new Dictionary<CharacterRace, int>();
        
        // 현재 적용된 강화 보너스
        private Dictionary<CharacterRace, float> attackBonuses = new Dictionary<CharacterRace, float>();
        private Dictionary<CharacterRace, float> attackSpeedBonuses = new Dictionary<CharacterRace, float>();
        
        // 매니저 참조
        private GameManager gameManager;
        private PlacementManager placementManager;
        private AudioSource audioSource;
        
        // 싱글톤 인스턴스
        private static InGameEnhanceManager instance;
        public static InGameEnhanceManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<InGameEnhanceManager>();
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            // 초기화
            InitializeEnhanceData();
            
            // 오디오 소스 가져오기
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        private void Start()
        {
            // 매니저 참조
            gameManager = GameManager.Instance;
            placementManager = PlacementManager.Instance;
            
            // 버튼 이벤트 설정
            if (humanEnhanceButton != null)
                humanEnhanceButton.onClick.AddListener(() => EnhanceRace(CharacterRace.Human));
                
            if (orcEnhanceButton != null)
                orcEnhanceButton.onClick.AddListener(() => EnhanceRace(CharacterRace.Orc));
                
            if (elfEnhanceButton != null)
                elfEnhanceButton.onClick.AddListener(() => EnhanceRace(CharacterRace.Elf));
                
            // 초기 UI 업데이트
            UpdateAllUI();
        }
        
        /// <summary>
        /// 강화 데이터 초기화
        /// </summary>
        private void InitializeEnhanceData()
        {
            enhanceLevels[CharacterRace.Human] = 0;
            enhanceLevels[CharacterRace.Orc] = 0;
            enhanceLevels[CharacterRace.Elf] = 0;
            
            enhanceCosts[CharacterRace.Human] = enhanceCost;
            enhanceCosts[CharacterRace.Orc] = enhanceCost;
            enhanceCosts[CharacterRace.Elf] = enhanceCost;
            
            attackBonuses[CharacterRace.Human] = 0f;
            attackBonuses[CharacterRace.Orc] = 0f;
            attackBonuses[CharacterRace.Elf] = 0f;
            
            attackSpeedBonuses[CharacterRace.Human] = 0f;
            attackSpeedBonuses[CharacterRace.Orc] = 0f;
            attackSpeedBonuses[CharacterRace.Elf] = 0f;
        }
        
        /// <summary>
        /// 종족 강화 실행
        /// </summary>
        public void EnhanceRace(CharacterRace race)
        {
            // 최대 레벨 체크
            if (enhanceLevels[race] >= maxEnhanceLevel)
            {
                ShowMessage($"{race} 종족은 최대 강화 레벨에 도달했습니다!");
                return;
            }
            
            // 미네랄 체크
            int currentMineral = GetCurrentMineral();
            int cost = enhanceCosts[race];
            
            if (currentMineral < cost)
            {
                ShowMessage("미네랄이 부족합니다!");
                return;
            }
            
            // 미네랄 차감
            SpendMineral(cost);
            
            // 강화 레벨 증가
            enhanceLevels[race]++;
            
            // 강화 보너스 적용
            attackBonuses[race] += enhanceBonus * 100f; // 5% -> 5
            attackSpeedBonuses[race] += enhanceBonus * 100f;
            
            // 비용 증가
            enhanceCosts[race] = Mathf.RoundToInt(enhanceCosts[race] * enhanceCostMultiplier);
            
            // 해당 종족의 모든 캐릭터에게 강화 적용
            ApplyEnhanceToRace(race);
            
            // 효과 표시
            ShowEnhanceEffect(race);
            
            // UI 업데이트
            UpdateRaceUI(race);
            
            Debug.Log($"[InGameEnhanceManager] {race} 종족 강화! 레벨: {enhanceLevels[race]}, 공격력/공격속도 +{attackBonuses[race]}%");
        }
        
        /// <summary>
        /// 해당 종족의 모든 캐릭터에게 강화 적용
        /// </summary>
        private void ApplyEnhanceToRace(CharacterRace race)
        {
            if (placementManager == null) return;
            
            // 플레이어 캐릭터 중 해당 종족 찾기
            List<Character> raceCharacters = new List<Character>();
            
            foreach (var character in FindObjectsOfType<Character>())
            {
                if (character != null && character.characterData != null && character.characterData.race == race)
                {
                    // 플레이어 캐릭터인지 확인 (AI 제외)
                    if (!character.CompareTag("Enemy") && !character.CompareTag("Opponent"))
                    {
                        raceCharacters.Add(character);
                    }
                }
            }
            
            // 강화 적용
            foreach (var character in raceCharacters)
            {
                ApplyEnhanceToCharacter(character, race);
            }
            
            Debug.Log($"[InGameEnhanceManager] {race} 종족 {raceCharacters.Count}명에게 강화 적용됨");
        }
        
        /// <summary>
        /// 개별 캐릭터에게 강화 적용
        /// </summary>
        private void ApplyEnhanceToCharacter(Character character, CharacterRace race)
        {
            if (character == null || character.characterData == null) return;
            
            // 기본 스탯 가져오기
            float baseAttack = character.characterData.attackPower;
            float baseAttackSpeed = character.characterData.attackSpeed;
            
            // 강화 보너스 적용
            float attackMultiplier = 1f + (attackBonuses[race] / 100f);
            float attackSpeedMultiplier = 1f - (attackSpeedBonuses[race] / 100f); // 공격속도는 낮을수록 빠름
            
            character.attackPower = baseAttack * attackMultiplier;
            character.attackSpeed = baseAttackSpeed * attackSpeedMultiplier;
            
            // 강화 이펙트 표시
            ShowCharacterEnhanceEffect(character);
        }
        
        /// <summary>
        /// 캐릭터별 강화 이펙트
        /// </summary>
        private void ShowCharacterEnhanceEffect(Character character)
        {
            if (enhanceEffectPrefab != null && character != null)
            {
                GameObject effect = Instantiate(enhanceEffectPrefab, character.transform.position, Quaternion.identity);
                effect.transform.SetParent(character.transform);
                effect.transform.localPosition = Vector3.zero;
                
                // 종족별 색상 설정
                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = GetRaceColor(character.characterData.race);
                }
                
                Destroy(effect, 2f);
            }
        }
        
        /// <summary>
        /// 종족별 강화 효과 표시
        /// </summary>
        private void ShowEnhanceEffect(CharacterRace race)
        {
            // 사운드 재생
            if (enhanceSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(enhanceSound);
            }
            
            // 버튼 플래시 효과
            Button targetButton = GetRaceButton(race);
            if (targetButton != null)
            {
                StartCoroutine(ButtonFlashEffect(targetButton, GetRaceColor(race)));
            }
            
            // 메시지 표시
            ShowMessage($"{race} 종족 강화 레벨 {enhanceLevels[race]}!");
        }
        
        /// <summary>
        /// 버튼 플래시 효과
        /// </summary>
        private IEnumerator ButtonFlashEffect(Button button, Color flashColor)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color originalColor = buttonImage.color;
                
                for (int i = 0; i < 3; i++)
                {
                    buttonImage.color = flashColor;
                    yield return new WaitForSeconds(0.1f);
                    buttonImage.color = originalColor;
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        
        /// <summary>
        /// UI 업데이트 메서드들
        /// </summary>
        private void UpdateAllUI()
        {
            UpdateRaceUI(CharacterRace.Human);
            UpdateRaceUI(CharacterRace.Orc);
            UpdateRaceUI(CharacterRace.Elf);
        }
        
        private void UpdateRaceUI(CharacterRace race)
        {
            int level = enhanceLevels[race];
            int cost = enhanceCosts[race];
            bool isMaxLevel = level >= maxEnhanceLevel;
            int currentMineral = GetCurrentMineral();
            
            // 비용 텍스트 업데이트
            TextMeshProUGUI costText = GetCostText(race);
            if (costText != null)
            {
                if (isMaxLevel)
                    costText.text = "MAX";
                else
                    costText.text = $"{cost}";
                    
                // 미네랄 부족 시 빨간색
                costText.color = (currentMineral >= cost && !isMaxLevel) ? Color.white : Color.red;
            }
            
            // 레벨 텍스트 업데이트
            TextMeshProUGUI levelText = GetLevelText(race);
            if (levelText != null)
            {
                levelText.text = $"Lv.{level}";
                if (level > 0)
                {
                    levelText.text += $"\n+{attackBonuses[race]:F0}%";
                }
            }
            
            // 버튼 활성화 상태
            Button button = GetRaceButton(race);
            if (button != null)
            {
                button.interactable = !isMaxLevel && currentMineral >= cost;
            }
        }
        
        /// <summary>
        /// 유틸리티 메서드들
        /// </summary>
        private Button GetRaceButton(CharacterRace race)
        {
            switch (race)
            {
                case CharacterRace.Human: return humanEnhanceButton;
                case CharacterRace.Orc: return orcEnhanceButton;
                case CharacterRace.Elf: return elfEnhanceButton;
                default: return null;
            }
        }
        
        private TextMeshProUGUI GetCostText(CharacterRace race)
        {
            switch (race)
            {
                case CharacterRace.Human: return humanCostText;
                case CharacterRace.Orc: return orcCostText;
                case CharacterRace.Elf: return elfCostText;
                default: return null;
            }
        }
        
        private TextMeshProUGUI GetLevelText(CharacterRace race)
        {
            switch (race)
            {
                case CharacterRace.Human: return humanLevelText;
                case CharacterRace.Orc: return orcLevelText;
                case CharacterRace.Elf: return elfLevelText;
                default: return null;
            }
        }
        
        private Color GetRaceColor(CharacterRace race)
        {
            switch (race)
            {
                case CharacterRace.Human: return new Color(0.3f, 0.5f, 1f);
                case CharacterRace.Orc: return new Color(1f, 0.3f, 0.3f);
                case CharacterRace.Elf: return new Color(0.3f, 1f, 0.3f);
                default: return Color.white;
            }
        }
        
        /// <summary>
        /// 미네랄 관련 메서드
        /// </summary>
        private int GetCurrentMineral()
        {
            // GameManager에서 미네랄 가져오기
            if (gameManager != null)
            {
                return gameManager.GetPlayerMineral();
            }
            
            // 임시로 PlayerPrefs에서 가져오기
            return PlayerPrefs.GetInt("PlayerMineral", 100);
        }
        
        private void SpendMineral(int amount)
        {
            // GameManager를 통해 미네랄 사용
            if (gameManager != null)
            {
                gameManager.SpendMineral(amount);
            }
            else
            {
                // 임시로 PlayerPrefs 사용
                int current = PlayerPrefs.GetInt("PlayerMineral", 100);
                PlayerPrefs.SetInt("PlayerMineral", current - amount);
            }
            
            // UI 업데이트
            UpdateAllUI();
        }
        
        /// <summary>
        /// 메시지 표시
        /// </summary>
        private void ShowMessage(string message)
        {
            StartCoroutine(ShowTemporaryMessage(message, 2f));
        }
        
        private IEnumerator ShowTemporaryMessage(string message, float duration)
        {
            GameObject messageObj = new GameObject("EnhanceMessage");
            messageObj.transform.SetParent(transform, false);
            
            TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
            messageText.text = message;
            messageText.fontSize = 32;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.color = Color.yellow;
            
            RectTransform rect = messageObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.8f);
            rect.anchorMax = new Vector2(0.5f, 0.8f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(600, 60);
            
            // 애니메이션
            float elapsed = 0f;
            Vector2 startPos = rect.anchoredPosition;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 위로 이동 + 페이드 아웃
                rect.anchoredPosition = startPos + Vector2.up * (50f * t);
                messageText.color = new Color(1, 1, 0, 1f - t);
                
                yield return null;
            }
            
            Destroy(messageObj);
        }
        
        /// <summary>
        /// 새로운 캐릭터가 소환될 때 자동으로 강화 적용
        /// </summary>
        public void OnCharacterSpawned(Character character)
        {
            if (character == null || character.characterData == null) return;
            
            CharacterRace race = character.characterData.race;
            
            // 이미 강화가 있다면 적용
            if (enhanceLevels[race] > 0)
            {
                ApplyEnhanceToCharacter(character, race);
            }
        }
        
        /// <summary>
        /// 게임 시작 시 초기화
        /// </summary>
        public void ResetEnhancements()
        {
            InitializeEnhanceData();
            UpdateAllUI();
        }
    }
}