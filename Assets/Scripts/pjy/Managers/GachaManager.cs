using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using pjy.Data;

namespace pjy.Managers
{
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

        // 캐릭터 데이터베이스 참조
        private CharacterCSVDatabase characterDatabase;
        
        // 뽑기 기록
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
            characterDatabase = FindFirstObjectByType<CharacterCSVDatabase>();
            
            if (characterDatabase == null)
            {
                Debug.LogError("[GachaManager] CharacterCSVDatabase를 찾을 수 없습니다!");
                return;
            }
            
            // 데이터베이스 검증
            if (characterDatabase.GetCharacterDataList() == null || characterDatabase.GetCharacterDataList().Count == 0)
            {
                Debug.LogWarning("[GachaManager] 캐릭터 데이터가 없습니다. CSV 데이터를 확인하세요.");
            }
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
                // TODO: 부족 알림 UI 표시
                return;
            }
            
            // 골드 차감
            SpendGold(totalCost);
            
            // 뽑기 실행
            StartCoroutine(DrawAnimation(drawCount));
        }

        /// <summary>
        /// 뽑기 애니메이션 코루틴
        /// </summary>
        private System.Collections.IEnumerator DrawAnimation(int drawCount)
        {
            // 버튼 비활성화
            SetButtonsInteractable(false);
            
            // 뽑기 애니메이션
            yield return new WaitForSeconds(drawAnimationDuration);
            
            // 결과 생성
            List<GachaResult> results = PerformDraw(drawCount);
            lastDrawResults = results;
            
            // 결과 표시
            ShowDrawResults(results);
            
            // 캐릭터 인벤토리에 추가
            AddCharactersToInventory(results);
            
            // 버튼 다시 활성화
            SetButtonsInteractable(true);
        }

        /// <summary>
        /// 실제 뽑기 실행
        /// </summary>
        private List<GachaResult> PerformDraw(int drawCount)
        {
            List<GachaResult> results = new List<GachaResult>();
            bool guaranteedRare = drawCount == 10; // 10연차는 최소 1개 2성 이상 보장
            
            for (int i = 0; i < drawCount; i++)
            {
                int starLevel;
                
                // 10연차 마지막에 보장 등급 체크
                if (guaranteedRare && i == drawCount - 1)
                {
                    // 이미 2성 이상이 나왔는지 체크
                    bool hasRareAlready = results.Any(r => r.character.starLevel >= guaranteedStarLevel);
                    
                    if (!hasRareAlready)
                    {
                        starLevel = guaranteedStarLevel; // 2성 보장
                    }
                    else
                    {
                        starLevel = RollStarLevel(); // 일반 확률
                    }
                }
                else
                {
                    starLevel = RollStarLevel(); // 일반 확률
                }
                
                // 해당 등급의 캐릭터 랜덤 선택
                CharacterData character = GetRandomCharacterByStarLevel(starLevel);
                
                if (character != null)
                {
                    results.Add(new GachaResult
                    {
                        character = character,
                        isNew = !IsCharacterOwned(character.characterID)
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
            
            var characters = characterDatabase.GetCharacterDataList()
                .Where(c => c.starLevel == starLevel)
                .ToList();
            
            if (characters.Count == 0)
            {
                Debug.LogWarning($"[GachaManager] {starLevel}성 캐릭터가 없습니다!");
                return null;
            }
            
            return characters[Random.Range(0, characters.Count)];
        }

        /// <summary>
        /// 캐릭터 보유 여부 확인
        /// </summary>
        private bool IsCharacterOwned(int characterID)
        {
            if (CharacterInventoryManager.Instance != null)
            {
                return CharacterInventoryManager.Instance.IsCharacterOwned(characterID);
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
                CreateResultItem(result);
            }
            
            // 결과 패널 표시
            resultPanel.SetActive(true);
            
            // 효과음 재생
            PlayDrawSound(results);
        }

        /// <summary>
        /// 결과 아이템 UI 생성
        /// </summary>
        private void CreateResultItem(GachaResult result)
        {
            if (characterResultPrefab == null) return;
            
            GameObject item = Instantiate(characterResultPrefab, resultContainer);
            
            // 캐릭터 정보 설정
            Image characterImage = item.GetComponentInChildren<Image>();
            if (characterImage != null && result.character.characterSprite != null)
            {
                characterImage.sprite = result.character.characterSprite;
            }
            
            // 이름 텍스트
            TextMeshProUGUI nameText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = result.character.characterName;
                
                // 새 캐릭터면 텍스트 색상 변경
                if (result.isNew)
                {
                    nameText.color = Color.yellow;
                }
            }
            
            // 별 등급 표시
            SetStarDisplay(item, result.character.starLevel);
        }

        /// <summary>
        /// 별 등급 표시 설정
        /// </summary>
        private void SetStarDisplay(GameObject item, int starLevel)
        {
            Transform starContainer = item.transform.Find("StarContainer");
            if (starContainer == null) return;
            
            // 모든 별 비활성화
            for (int i = 0; i < starContainer.childCount; i++)
            {
                starContainer.GetChild(i).gameObject.SetActive(false);
            }
            
            // 해당 등급만큼 별 활성화
            for (int i = 0; i < starLevel && i < starContainer.childCount; i++)
            {
                starContainer.GetChild(i).gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 뽑기 효과음 재생
        /// </summary>
        private void PlayDrawSound(List<GachaResult> results)
        {
            bool hasRare = results.Any(r => r.character.starLevel >= 3);
            AudioClip soundToPlay = hasRare ? rareDrawSound : drawSound;
            
            if (soundToPlay != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(soundToPlay);
            }
        }

        /// <summary>
        /// 캐릭터들을 인벤토리에 추가
        /// </summary>
        private void AddCharactersToInventory(List<GachaResult> results)
        {
            if (CharacterInventoryManager.Instance == null) return;
            
            foreach (var result in results)
            {
                if (result.isNew)
                {
                    // 새 캐릭터 추가
                    CharacterInventoryManager.Instance.AddCharacter(result.character);
                }
                else
                {
                    // 중복 캐릭터는 조각으로 변환
                    CharacterInventoryManager.Instance.AddCharacterFragments(result.character.characterID, 1);
                }
            }
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
        /// 버튼 활성화/비활성화
        /// </summary>
        private void SetButtonsInteractable(bool interactable)
        {
            if (singleDrawButton != null)
            {
                singleDrawButton.interactable = interactable && currentGold >= singleDrawCost;
            }
            
            if (tenDrawButton != null)
            {
                tenDrawButton.interactable = interactable && currentGold >= tenDrawCost;
            }
        }

        /// <summary>
        /// 골드 사용
        /// </summary>
        private void SpendGold(int amount)
        {
            currentGold = Mathf.Max(0, currentGold - amount);
            UpdateGoldUI();
            SetButtonsInteractable(true); // 골드 변경 후 버튼 상태 업데이트
        }

        /// <summary>
        /// 골드 추가
        /// </summary>
        public void AddGold(int amount)
        {
            currentGold += amount;
            UpdateGoldUI();
            SetButtonsInteractable(true);
        }

        /// <summary>
        /// 골드 UI 업데이트
        /// </summary>
        private void UpdateGoldUI()
        {
            if (goldText != null)
            {
                goldText.text = $"골드: {currentGold:N0}";
            }
        }

        /// <summary>
        /// 가챠 UI 열기/닫기
        /// </summary>
        public void ToggleGachaUI()
        {
            if (gachaUI != null)
            {
                gachaUI.SetActive(!gachaUI.activeSelf);
                
                if (gachaUI.activeSelf)
                {
                    UpdateGoldUI();
                    SetButtonsInteractable(true);
                }
            }
        }

        /// <summary>
        /// 현재 골드 반환
        /// </summary>
        public int GetCurrentGold()
        {
            return currentGold;
        }

        /// <summary>
        /// 마지막 뽑기 결과 반환
        /// </summary>
        public List<GachaResult> GetLastDrawResults()
        {
            return new List<GachaResult>(lastDrawResults);
        }

        #region Debug Methods
        
        [ContextMenu("Debug: Add 1000 Gold")]
        private void DebugAddGold()
        {
            AddGold(1000);
        }
        
        [ContextMenu("Debug: Single Draw")]
        private void DebugSingleDraw()
        {
            if (currentGold >= singleDrawCost)
            {
                StartDrawing(1);
            }
        }
        
        [ContextMenu("Debug: Ten Draw")]
        private void DebugTenDraw()
        {
            if (currentGold >= tenDrawCost)
            {
                StartDrawing(10);
            }
        }
        
        #endregion
    }

    /// <summary>
    /// 가챠 결과 데이터 구조
    /// </summary>
    [System.Serializable]
    public class GachaResult
    {
        public CharacterData character;
        public bool isNew;
        public System.DateTime drawTime;

        public GachaResult()
        {
            drawTime = System.DateTime.Now;
        }
    }
}