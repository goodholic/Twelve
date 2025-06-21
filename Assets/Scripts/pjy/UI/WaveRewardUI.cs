using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using pjy.Data;
using pjy.Managers;

namespace pjy.UI
{
    /// <summary>
    /// 5웨이브 보상 UI - 랜덤 2성 캐릭터 3개 중 1개 선택
    /// </summary>
    public class WaveRewardUI : MonoBehaviour
    {
        private static WaveRewardUI instance;
        public static WaveRewardUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<WaveRewardUI>();
                }
                return instance;
            }
        }

        [Header("UI 패널")]
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private GameObject backgroundOverlay;
        [SerializeField] private Button closeButton;

        [Header("보상 선택")]
        [SerializeField] private Transform rewardContainer;
        [SerializeField] private GameObject rewardItemPrefab;
        [SerializeField] private Button[] rewardButtons = new Button[3];
        [SerializeField] private Image[] rewardImages = new Image[3];
        [SerializeField] private TextMeshProUGUI[] rewardNames = new TextMeshProUGUI[3];

        [Header("타이틀 및 설명")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI waveInfoText;

        [Header("애니메이션")]
        [SerializeField] private float showAnimationDuration = 0.5f;
        [SerializeField] private float hideAnimationDuration = 0.3f;
        [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("효과음")]
        [SerializeField] private AudioClip rewardAppearSound;
        [SerializeField] private AudioClip rewardSelectSound;
        [SerializeField] private AudioClip rewardGetSound;

        // 현재 보상 데이터
        private CharacterData[] currentRewardOptions = new CharacterData[3];
        private int currentWave = 0;
        private bool isSelectingReward = false;

        // 캐릭터 데이터베이스 참조
        private CharacterCSVDatabase characterDatabase;

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
            InitializeUI();
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // 캐릭터 데이터베이스 참조
            characterDatabase = FindFirstObjectByType<CharacterCSVDatabase>();
            
            // 패널 숨기기
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(false);
            }
            
            if (backgroundOverlay != null)
            {
                backgroundOverlay.SetActive(false);
            }
            
            // 버튼 이벤트 연결
            SetupButtons();
            
            // 텍스트 초기화
            if (titleText != null)
            {
                titleText.text = "웨이브 보상";
            }
            
            if (descriptionText != null)
            {
                descriptionText.text = "3개의 캐릭터 중 1개를 선택하세요";
            }
        }

        /// <summary>
        /// 버튼 이벤트 설정
        /// </summary>
        private void SetupButtons()
        {
            // 보상 선택 버튼들
            for (int i = 0; i < rewardButtons.Length; i++)
            {
                if (rewardButtons[i] != null)
                {
                    int index = i; // 클로저 문제 해결
                    rewardButtons[i].onClick.AddListener(() => SelectReward(index));
                }
            }
            
            // 닫기 버튼
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseRewardPanel);
            }
        }

        /// <summary>
        /// 웨이브 보상 표시
        /// </summary>
        public void ShowWaveReward(int waveNumber)
        {
            if (isSelectingReward)
            {
                Debug.LogWarning("[WaveRewardUI] 이미 보상 선택 중입니다!");
                return;
            }
            
            currentWave = waveNumber;
            isSelectingReward = true;
            
            // 보상 캐릭터 생성
            GenerateRewardOptions();
            
            // UI 표시
            StartCoroutine(ShowRewardAnimation());
            
            // 게임 일시정지
            Time.timeScale = 0f;
            
            Debug.Log($"[WaveRewardUI] {waveNumber}웨이브 보상 표시");
        }

        /// <summary>
        /// 보상 옵션 생성 (랜덤 2성 캐릭터 3개)
        /// </summary>
        private void GenerateRewardOptions()
        {
            if (characterDatabase == null)
            {
                Debug.LogError("[WaveRewardUI] CharacterCSVDatabase를 찾을 수 없습니다!");
                return;
            }
            
            // 2성 캐릭터 리스트 가져오기
            var star2Characters = characterDatabase.GetCharacterDataList()
                .Where(c => c.starLevel == 2)
                .ToList();
            
            if (star2Characters.Count < 3)
            {
                Debug.LogWarning("[WaveRewardUI] 2성 캐릭터가 3개 미만입니다!");
                // 부족한 경우 1성으로 대체
                var star1Characters = characterDatabase.GetCharacterDataList()
                    .Where(c => c.starLevel == 1)
                    .ToList();
                star2Characters.AddRange(star1Characters);
            }
            
            // 랜덤하게 3개 선택 (중복 없이)
            var shuffled = star2Characters.OrderBy(x => Random.value).ToList();
            
            for (int i = 0; i < 3 && i < shuffled.Count; i++)
            {
                currentRewardOptions[i] = shuffled[i];
            }
            
            // UI 업데이트
            UpdateRewardUI();
        }

        /// <summary>
        /// 보상 UI 업데이트
        /// </summary>
        private void UpdateRewardUI()
        {
            // 웨이브 정보 업데이트
            if (waveInfoText != null)
            {
                waveInfoText.text = $"웨이브 {currentWave} 완료 보상";
            }
            
            // 각 보상 옵션 UI 설정
            for (int i = 0; i < 3; i++)
            {
                if (currentRewardOptions[i] != null)
                {
                    SetupRewardOption(i, currentRewardOptions[i]);
                }
            }
        }

        /// <summary>
        /// 개별 보상 옵션 UI 설정
        /// </summary>
        private void SetupRewardOption(int index, CharacterData character)
        {
            // 캐릭터 이미지
            if (rewardImages[index] != null && character.characterSprite != null)
            {
                rewardImages[index].sprite = character.characterSprite;
                rewardImages[index].color = Color.white;
            }
            
            // 캐릭터 이름
            if (rewardNames[index] != null)
            {
                rewardNames[index].text = character.characterName;
                
                // 등급에 따른 색상 설정
                switch (character.starLevel)
                {
                    case 1:
                        rewardNames[index].color = Color.gray;
                        break;
                    case 2:
                        rewardNames[index].color = Color.green;
                        break;
                    case 3:
                        rewardNames[index].color = Color.blue;
                        break;
                    default:
                        rewardNames[index].color = Color.white;
                        break;
                }
            }
            
            // 버튼 활성화
            if (rewardButtons[index] != null)
            {
                rewardButtons[index].interactable = true;
            }
        }

        /// <summary>
        /// 보상 선택
        /// </summary>
        private void SelectReward(int index)
        {
            if (!isSelectingReward || currentRewardOptions[index] == null)
            {
                return;
            }
            
            CharacterData selectedCharacter = currentRewardOptions[index];
            
            // 효과음 재생
            PlaySound(rewardSelectSound);
            
            // 캐릭터를 인벤토리에 추가
            AddCharacterToInventory(selectedCharacter);
            
            // 선택 효과 표시
            StartCoroutine(SelectionEffect(index, selectedCharacter));
            
            Debug.Log($"[WaveRewardUI] {selectedCharacter.characterName} 선택!");
        }

        /// <summary>
        /// 선택 효과 코루틴
        /// </summary>
        private System.Collections.IEnumerator SelectionEffect(int selectedIndex, CharacterData character)
        {
            // 선택되지 않은 옵션들 비활성화
            for (int i = 0; i < rewardButtons.Length; i++)
            {
                if (i != selectedIndex && rewardButtons[i] != null)
                {
                    rewardButtons[i].interactable = false;
                    
                    // 페이드 아웃 효과
                    if (rewardImages[i] != null)
                    {
                        rewardImages[i].color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }
                }
            }
            
            // 선택된 아이템 강조 효과
            if (rewardButtons[selectedIndex] != null)
            {
                Transform selectedTransform = rewardButtons[selectedIndex].transform;
                Vector3 originalScale = selectedTransform.localScale;
                
                // 펄스 효과
                float elapsed = 0f;
                float duration = 0.5f;
                
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float scale = 1f + Mathf.Sin(elapsed * 10f) * 0.1f;
                    selectedTransform.localScale = originalScale * scale;
                    yield return null;
                }
                
                selectedTransform.localScale = originalScale;
            }
            
            // 획득 효과음
            PlaySound(rewardGetSound);
            
            // 잠시 대기 후 패널 닫기
            yield return new WaitForSecondsRealtime(1f);
            CloseRewardPanel();
        }

        /// <summary>
        /// 캐릭터를 인벤토리에 추가
        /// </summary>
        private void AddCharacterToInventory(CharacterData character)
        {
            if (CharacterInventoryManager.Instance != null)
            {
                CharacterInventoryManager.Instance.AddCharacter(character);
                Debug.Log($"[WaveRewardUI] {character.characterName}이(가) 인벤토리에 추가되었습니다!");
            }
            else
            {
                Debug.LogWarning("[WaveRewardUI] CharacterInventoryManager.Instance가 null입니다!");
            }
        }

        /// <summary>
        /// 보상 패널 닫기
        /// </summary>
        private void CloseRewardPanel()
        {
            if (!isSelectingReward) return;
            
            StartCoroutine(HideRewardAnimation());
        }

        /// <summary>
        /// 보상 표시 애니메이션
        /// </summary>
        private System.Collections.IEnumerator ShowRewardAnimation()
        {
            // 배경 오버레이 표시
            if (backgroundOverlay != null)
            {
                backgroundOverlay.SetActive(true);
            }
            
            // 패널 활성화
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(true);
                
                // 스케일 애니메이션
                Transform panelTransform = rewardPanel.transform;
                Vector3 originalScale = panelTransform.localScale;
                panelTransform.localScale = Vector3.zero;
                
                float elapsed = 0f;
                while (elapsed < showAnimationDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / showAnimationDuration;
                    float scale = showCurve.Evaluate(t);
                    panelTransform.localScale = originalScale * scale;
                    yield return null;
                }
                
                panelTransform.localScale = originalScale;
            }
            
            // 효과음 재생
            PlaySound(rewardAppearSound);
        }

        /// <summary>
        /// 보상 숨기기 애니메이션
        /// </summary>
        private System.Collections.IEnumerator HideRewardAnimation()
        {
            if (rewardPanel != null)
            {
                Transform panelTransform = rewardPanel.transform;
                Vector3 originalScale = panelTransform.localScale;
                
                float elapsed = 0f;
                while (elapsed < hideAnimationDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / hideAnimationDuration;
                    float scale = Mathf.Lerp(1f, 0f, t);
                    panelTransform.localScale = originalScale * scale;
                    yield return null;
                }
                
                rewardPanel.SetActive(false);
            }
            
            // 배경 오버레이 숨기기
            if (backgroundOverlay != null)
            {
                backgroundOverlay.SetActive(false);
            }
            
            // 게임 재개
            Time.timeScale = 1f;
            isSelectingReward = false;
            
            // 상태 초기화
            ResetRewardState();
        }

        /// <summary>
        /// 보상 상태 초기화
        /// </summary>
        private void ResetRewardState()
        {
            currentWave = 0;
            
            // 보상 옵션 초기화
            for (int i = 0; i < currentRewardOptions.Length; i++)
            {
                currentRewardOptions[i] = null;
            }
            
            // 버튼 상태 초기화
            for (int i = 0; i < rewardButtons.Length; i++)
            {
                if (rewardButtons[i] != null)
                {
                    rewardButtons[i].interactable = true;
                }
                
                if (rewardImages[i] != null)
                {
                    rewardImages[i].color = Color.white;
                }
            }
        }

        /// <summary>
        /// 효과음 재생
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(clip);
            }
        }

        /// <summary>
        /// 현재 보상 선택 중인지 확인
        /// </summary>
        public bool IsSelectingReward()
        {
            return isSelectingReward;
        }

        /// <summary>
        /// 강제로 보상 패널 닫기 (디버그용)
        /// </summary>
        [ContextMenu("Force Close Reward Panel")]
        public void ForceCloseRewardPanel()
        {
            if (isSelectingReward)
            {
                Time.timeScale = 1f;
                isSelectingReward = false;
                
                if (rewardPanel != null)
                {
                    rewardPanel.SetActive(false);
                }
                
                if (backgroundOverlay != null)
                {
                    backgroundOverlay.SetActive(false);
                }
                
                ResetRewardState();
            }
        }

        /// <summary>
        /// 테스트용 보상 표시
        /// </summary>
        [ContextMenu("Test Show Wave Reward")]
        public void TestShowWaveReward()
        {
            ShowWaveReward(5);
        }
    }
}