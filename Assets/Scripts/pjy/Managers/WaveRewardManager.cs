using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace pjy.Managers
{
    /// <summary>
    /// 5웨이브 보상 시스템 매니저
    /// 5, 10, 15웨이브 클리어 시 랜덤 2성 캐릭터 3개 중 1개 선택
    /// </summary>
    public class WaveRewardManager : MonoBehaviour
    {
        [Header("보상 설정")]
        [SerializeField] private int[] rewardWaves = {5, 10, 15};
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private float selectionTimeLimit = 30f;
        
        [Header("보상 슬롯")]
        [SerializeField] private Transform[] rewardSlots = new Transform[3];
        [SerializeField] private Button[] selectButtons = new Button[3];
        
        [Header("UI 요소")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private GameObject timerPanel;
        
        [Header("캐릭터 정보 표시")]
        [SerializeField] private TextMeshProUGUI[] characterNameTexts = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] characterStatTexts = new TextMeshProUGUI[3];
        [SerializeField] private Image[] characterImages = new Image[3];
        [SerializeField] private Image[] raceIcons = new Image[3];
        
        [Header("효과")]
        [SerializeField] private GameObject selectionEffectPrefab;
        [SerializeField] private AudioClip rewardSound;
        [SerializeField] private AudioClip selectSound;
        
        [Header("캐릭터 데이터베이스")]
        [SerializeField] private CharacterDatabaseObject characterDatabase;
        
        // 보상 후보 캐릭터들
        private List<CharacterData> rewardCandidates = new List<CharacterData>();
        private CharacterData[] currentRewards = new CharacterData[3];
        private bool isSelecting = false;
        private float selectionTimer = 0f;
        
        // 매니저 참조
        private GameManager gameManager;
        private WaveSpawner waveSpawner;
        private CharacterInventoryManager inventoryManager;
        private AudioSource audioSource;
        
        // 싱글톤 인스턴스
        private static WaveRewardManager instance;
        public static WaveRewardManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<WaveRewardManager>();
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
            waveSpawner = FindObjectOfType<WaveSpawner>();
            inventoryManager = FindObjectOfType<CharacterInventoryManager>();
            
            // 보상 패널 숨기기
            if (rewardPanel != null)
                rewardPanel.SetActive(false);
                
            // 버튼 이벤트 설정
            for (int i = 0; i < selectButtons.Length; i++)
            {
                int index = i; // 클로저를 위한 복사
                if (selectButtons[i] != null)
                {
                    selectButtons[i].onClick.AddListener(() => SelectReward(index));
                }
            }
            
            // 2성 캐릭터 목록 준비
            PrepareRewardCandidates();
            
            // WaveSpawner의 웨이브 완료 이벤트 구독
            if (waveSpawner != null)
            {
                // WaveSpawner에 OnWaveCompleted 이벤트가 있다고 가정
                // 없다면 WaveSpawner를 수정하거나 다른 방식으로 체크
            }
        }
        
        private void Update()
        {
            // 선택 시간 제한
            if (isSelecting && selectionTimer > 0)
            {
                selectionTimer -= Time.deltaTime;
                UpdateTimerUI();
                
                if (selectionTimer <= 0)
                {
                    // 시간 초과 - 랜덤 선택
                    SelectReward(Random.Range(0, 3));
                }
            }
        }
        
        /// <summary>
        /// 2성 캐릭터 목록 준비
        /// </summary>
        private void PrepareRewardCandidates()
        {
            rewardCandidates.Clear();
            
            if (characterDatabase != null && characterDatabase.characters != null)
            {
                foreach (var character in characterDatabase.characters)
                {
                    if (character != null && character.star == CharacterStar.TwoStar)
                    {
                        rewardCandidates.Add(character);
                    }
                }
            }
            
            Debug.Log($"[WaveRewardManager] 2성 캐릭터 {rewardCandidates.Count}개 준비됨");
        }
        
        /// <summary>
        /// 웨이브 완료 체크 (WaveSpawner에서 호출)
        /// </summary>
        public void OnWaveCompleted(int waveNumber)
        {
            if (IsRewardWave(waveNumber))
            {
                ShowReward(waveNumber);
            }
        }
        
        /// <summary>
        /// 보상 웨이브인지 확인
        /// </summary>
        private bool IsRewardWave(int waveNumber)
        {
            return System.Array.Exists(rewardWaves, w => w == waveNumber);
        }
        
        /// <summary>
        /// 보상 표시
        /// </summary>
        public void ShowReward(int waveNumber)
        {
            if (rewardCandidates.Count < 3)
            {
                Debug.LogWarning("[WaveRewardManager] 2성 캐릭터가 3개 미만입니다!");
                return;
            }
            
            // 게임 일시정지
            if (gameManager != null)
            {
                Time.timeScale = 0f;
            }
            
            // 랜덤 3개 선택
            List<CharacterData> shuffled = new List<CharacterData>(rewardCandidates);
            ShuffleList(shuffled);
            
            for (int i = 0; i < 3; i++)
            {
                currentRewards[i] = shuffled[i];
            }
            
            // UI 설정
            UpdateRewardUI(waveNumber);
            
            // 패널 표시
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(true);
            }
            
            // 사운드 재생
            if (rewardSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(rewardSound);
            }
            
            // 선택 시작
            isSelecting = true;
            selectionTimer = selectionTimeLimit;
            
            // 효과
            ShowRewardEffect();
        }
        
        /// <summary>
        /// 보상 선택
        /// </summary>
        private void SelectReward(int index)
        {
            if (!isSelecting || index < 0 || index >= 3) return;
            
            CharacterData selectedCharacter = currentRewards[index];
            if (selectedCharacter == null) return;
            
            // 선택 사운드
            if (selectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(selectSound);
            }
            
            // 인벤토리에 추가
            if (inventoryManager != null)
            {
                // 새로운 캐릭터 인스턴스 생성
                CharacterData newCharacter = ScriptableObject.Instantiate(selectedCharacter);
                inventoryManager.AddToInventory(newCharacter);
                
                Debug.Log($"[WaveRewardManager] {newCharacter.characterName}을(를) 획득했습니다!");
            }
            
            // 선택 효과
            ShowSelectionEffect(index);
            
            // 메시지 표시
            ShowRewardMessage(selectedCharacter.characterName);
            
            // 패널 닫기
            StartCoroutine(CloseRewardPanel());
        }
        
        /// <summary>
        /// 보상 패널 닫기
        /// </summary>
        private IEnumerator CloseRewardPanel()
        {
            yield return new WaitForSecondsRealtime(1f);
            
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(false);
            }
            
            // 게임 재개
            Time.timeScale = 1f;
            
            isSelecting = false;
            selectionTimer = 0f;
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateRewardUI(int waveNumber)
        {
            // 제목
            if (titleText != null)
            {
                titleText.text = $"Wave {waveNumber} 클리어 보상!";
            }
            
            // 각 슬롯 업데이트
            for (int i = 0; i < 3; i++)
            {
                if (currentRewards[i] != null)
                {
                    // 캐릭터 이름
                    if (characterNameTexts[i] != null)
                    {
                        characterNameTexts[i].text = currentRewards[i].characterName;
                    }
                    
                    // 캐릭터 스탯
                    if (characterStatTexts[i] != null)
                    {
                        characterStatTexts[i].text = $"공격력: {currentRewards[i].attackPower}\n" +
                                                   $"체력: {currentRewards[i].maxHP}\n" +
                                                   $"공격속도: {currentRewards[i].attackSpeed:F1}";
                    }
                    
                    // 캐릭터 이미지
                    if (characterImages[i] != null && currentRewards[i].characterSprite != null)
                    {
                        characterImages[i].sprite = currentRewards[i].characterSprite;
                        characterImages[i].color = Color.white;
                    }
                    
                    // 종족 아이콘
                    if (raceIcons[i] != null)
                    {
                        raceIcons[i].color = GetRaceColor(currentRewards[i].race);
                    }
                }
            }
        }
        
        /// <summary>
        /// 타이머 UI 업데이트
        /// </summary>
        private void UpdateTimerUI()
        {
            if (timerText != null)
            {
                timerText.text = $"선택 시간: {selectionTimer:F0}초";
                
                // 시간이 적을 때 빨간색
                if (selectionTimer <= 5f)
                {
                    timerText.color = Color.red;
                }
                else
                {
                    timerText.color = Color.white;
                }
            }
        }
        
        /// <summary>
        /// 리스트 셔플
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
        
        /// <summary>
        /// 종족별 색상
        /// </summary>
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
        /// 보상 효과 표시
        /// </summary>
        private void ShowRewardEffect()
        {
            // 각 슬롯에 반짝임 효과
            for (int i = 0; i < rewardSlots.Length; i++)
            {
                if (rewardSlots[i] != null)
                {
                    StartCoroutine(SlotGlowEffect(rewardSlots[i]));
                }
            }
        }
        
        /// <summary>
        /// 슬롯 반짝임 효과
        /// </summary>
        private IEnumerator SlotGlowEffect(Transform slot)
        {
            Image slotImage = slot.GetComponent<Image>();
            if (slotImage == null) yield break;
            
            Color originalColor = slotImage.color;
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float glow = Mathf.Sin(elapsed * 3f) * 0.3f + 0.7f;
                slotImage.color = new Color(glow, glow, glow, originalColor.a);
                yield return null;
            }
            
            slotImage.color = originalColor;
        }
        
        /// <summary>
        /// 선택 효과 표시
        /// </summary>
        private void ShowSelectionEffect(int index)
        {
            if (index < 0 || index >= rewardSlots.Length) return;
            
            if (selectionEffectPrefab != null && rewardSlots[index] != null)
            {
                GameObject effect = Instantiate(selectionEffectPrefab, rewardSlots[index].position, Quaternion.identity);
                effect.transform.SetParent(rewardPanel.transform, true);
                Destroy(effect, 2f);
            }
            
            // 선택된 슬롯 확대
            StartCoroutine(SelectedSlotAnimation(rewardSlots[index]));
        }
        
        /// <summary>
        /// 선택된 슬롯 애니메이션
        /// </summary>
        private IEnumerator SelectedSlotAnimation(Transform slot)
        {
            Vector3 originalScale = slot.localScale;
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
                slot.localScale = originalScale * scale;
                yield return null;
            }
            
            slot.localScale = originalScale;
        }
        
        /// <summary>
        /// 보상 메시지 표시
        /// </summary>
        private void ShowRewardMessage(string characterName)
        {
            GameObject messageObj = new GameObject("RewardMessage");
            messageObj.transform.SetParent(rewardPanel.transform, false);
            
            TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
            messageText.text = $"{characterName} 획득!";
            messageText.fontSize = 48;
            messageText.color = Color.yellow;
            messageText.alignment = TextAlignmentOptions.Center;
            
            RectTransform rect = messageObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(500, 100);
            
            // 애니메이션
            StartCoroutine(MessageAnimation(rect, messageText));
        }
        
        private IEnumerator MessageAnimation(RectTransform rect, TextMeshProUGUI text)
        {
            float duration = 1f;
            float elapsed = 0f;
            Vector2 startPos = rect.anchoredPosition;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                // 위로 이동 + 페이드 아웃
                rect.anchoredPosition = startPos + Vector2.up * (100f * t);
                text.color = new Color(1, 1, 0, 1f - t);
                
                yield return null;
            }
            
            Destroy(rect.gameObject);
        }
    }
}