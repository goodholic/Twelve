using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Systems;
using GuildMaster.Battle;

namespace GuildMaster.UI
{
    /// <summary>
    /// 길드 대항전 UI
    /// 대전 신청, 랭킹, 토너먼트 관리
    /// </summary>
    public class GuildBattleUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject guildBattlePanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        
        [Header("Battle Selection")]
        [SerializeField] private GameObject battleSelectionPanel;
        [SerializeField] private Button dailyBattleButton;
        [SerializeField] private Button rankedBattleButton;
        [SerializeField] private Button tournamentButton;
        [SerializeField] private TextMeshProUGUI dailyBattlesText;
        [SerializeField] private TextMeshProUGUI cooldownText;
        
        [Header("Difficulty Selection")]
        [SerializeField] private GameObject difficultyPanel;
        [SerializeField] private List<DifficultyButton> difficultyButtons;
        [SerializeField] private Button confirmBattleButton;
        [SerializeField] private Button cancelSelectionButton;
        
        [Header("Opponent Preview")]
        [SerializeField] private GameObject opponentPreviewPanel;
        [SerializeField] private TextMeshProUGUI opponentNameText;
        [SerializeField] private TextMeshProUGUI opponentLevelText;
        [SerializeField] private TextMeshProUGUI opponentRatingText;
        [SerializeField] private TextMeshProUGUI opponentPowerText;
        [SerializeField] private Image opponentBadgeImage;
        [SerializeField] private Transform squadPreviewContainer;
        [SerializeField] private GameObject squadPreviewPrefab;
        
        [Header("Ranking Display")]
        [SerializeField] private GameObject rankingPanel;
        [SerializeField] private Transform rankingContainer;
        [SerializeField] private GameObject rankingEntryPrefab;
        [SerializeField] private TextMeshProUGUI playerRankText;
        [SerializeField] private TextMeshProUGUI playerRatingText;
        [SerializeField] private Button refreshRankingButton;
        
        [Header("Tournament")]
        [SerializeField] private GameObject tournamentPanel;
        [SerializeField] private TextMeshProUGUI tournamentStatusText;
        [SerializeField] private Transform bracketContainer;
        [SerializeField] private GameObject matchDisplayPrefab;
        [SerializeField] private Button joinTournamentButton;
        
        [Header("Battle History")]
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private Transform historyContainer;
        [SerializeField] private GameObject historyEntryPrefab;
        [SerializeField] private Button historyTabButton;
        
        [Header("Rewards Display")]
        [SerializeField] private GameObject rewardsPopup;
        [SerializeField] private TextMeshProUGUI rewardsTitleText;
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private GameObject rewardItemPrefab;
        [SerializeField] private Button claimRewardsButton;
        
        [Header("Animation")]
        [SerializeField] private float panelAnimDuration = 0.3f;
        [SerializeField] private AnimationCurve panelAnimCurve;
        
        // 시스템 참조
        private AIGuildBattleSystem battleSystem;
        
        // UI 상태
        private AIGuildGenerator.Difficulty selectedDifficulty;
        private AIGuildBattleSystem.BattleType selectedBattleType;
        private List<GameObject> rankingEntries = new List<GameObject>();
        private List<GameObject> historyEntries = new List<GameObject>();
        private Coroutine updateCoroutine;
        
        [System.Serializable]
        public class DifficultyButton
        {
            public Button button;
            public AIGuildGenerator.Difficulty difficulty;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI levelRangeText;
            public TextMeshProUGUI rewardText;
            public Image backgroundImage;
            public Color normalColor;
            public Color selectedColor;
        }
        
        void Start()
        {
            battleSystem = AIGuildBattleSystem.Instance;
            
            if (battleSystem == null)
            {
                Debug.LogError("AIGuildBattleSystem not found!");
                enabled = false;
                return;
            }
            
            SetupUI();
            SubscribeToEvents();
            
            // 초기 UI 업데이트
            UpdateBattleButtons();
            RefreshRanking();
            
            updateCoroutine = StartCoroutine(UpdateCoroutine());
        }
        
        void SetupUI()
        {
            // 메인 버튼
            if (openButton != null)
                openButton.onClick.AddListener(() => ShowPanel(true));
            if (closeButton != null)
                closeButton.onClick.AddListener(() => ShowPanel(false));
            
            // 전투 선택 버튼
            if (dailyBattleButton != null)
                dailyBattleButton.onClick.AddListener(() => SelectBattleType(AIGuildBattleSystem.BattleType.Daily));
            if (rankedBattleButton != null)
                rankedBattleButton.onClick.AddListener(() => SelectBattleType(AIGuildBattleSystem.BattleType.Ranked));
            if (tournamentButton != null)
                tournamentButton.onClick.AddListener(ShowTournamentPanel);
            
            // 난이도 버튼
            foreach (var diffButton in difficultyButtons)
            {
                var difficulty = diffButton.difficulty;
                diffButton.button.onClick.AddListener(() => SelectDifficulty(difficulty));
                
                // 난이도 정보 표시
                UpdateDifficultyButton(diffButton);
            }
            
            // 기타 버튼
            if (confirmBattleButton != null)
                confirmBattleButton.onClick.AddListener(StartBattle);
            if (cancelSelectionButton != null)
                cancelSelectionButton.onClick.AddListener(CancelSelection);
            if (refreshRankingButton != null)
                refreshRankingButton.onClick.AddListener(RefreshRanking);
            if (historyTabButton != null)
                historyTabButton.onClick.AddListener(ShowBattleHistory);
            if (joinTournamentButton != null)
                joinTournamentButton.onClick.AddListener(JoinTournament);
            if (claimRewardsButton != null)
                claimRewardsButton.onClick.AddListener(ClaimRewards);
        }
        
        void SubscribeToEvents()
        {
            if (battleSystem != null)
            {
                battleSystem.OnBattleCompleted += OnBattleCompleted;
                battleSystem.OnTournamentCompleted += OnTournamentCompleted;
                battleSystem.OnRankingUpdated += OnRankingUpdated;
                battleSystem.OnDailyBattlesReset += OnDailyBattlesReset;
            }
        }
        
        void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            
            // 이벤트 구독 해제
            if (battleSystem != null)
            {
                battleSystem.OnBattleCompleted -= OnBattleCompleted;
                battleSystem.OnTournamentCompleted -= OnTournamentCompleted;
                battleSystem.OnRankingUpdated -= OnRankingUpdated;
                battleSystem.OnDailyBattlesReset -= OnDailyBattlesReset;
            }
        }
        
        IEnumerator UpdateCoroutine()
        {
            while (true)
            {
                UpdateBattleButtons();
                UpdateTournamentStatus();
                
                yield return new WaitForSeconds(1f);
            }
        }
        
        void ShowPanel(bool show)
        {
            if (guildBattlePanel != null)
            {
                if (show)
                {
                    guildBattlePanel.SetActive(true);
                    StartCoroutine(AnimatePanel(true));
                }
                else
                {
                    StartCoroutine(AnimatePanel(false));
                }
            }
        }
        
        IEnumerator AnimatePanel(bool show)
        {
            if (guildBattlePanel == null) yield break;
            
            var canvasGroup = guildBattlePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = guildBattlePanel.AddComponent<CanvasGroup>();
            }
            
            float elapsed = 0f;
            float startAlpha = show ? 0f : 1f;
            float endAlpha = show ? 1f : 0f;
            
            Vector3 startScale = show ? Vector3.one * 0.8f : Vector3.one;
            Vector3 endScale = show ? Vector3.one : Vector3.one * 0.8f;
            
            while (elapsed < panelAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / panelAnimDuration;
                
                if (panelAnimCurve != null)
                    t = panelAnimCurve.Evaluate(t);
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                guildBattlePanel.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                
                yield return null;
            }
            
            if (!show)
            {
                guildBattlePanel.SetActive(false);
            }
        }
        
        void UpdateBattleButtons()
        {
            int dailyRemaining = battleSystem.GetDailyBattlesRemaining();
            float cooldown = battleSystem.GetBattleCooldownRemaining();
            
            // 일일 전투 정보
            if (dailyBattlesText != null)
            {
                dailyBattlesText.text = $"일일 전투: {dailyRemaining}회 남음";
            }
            
            // 쿨다운 표시
            if (cooldownText != null)
            {
                if (cooldown > 0)
                {
                    int minutes = Mathf.FloorToInt(cooldown / 60f);
                    int seconds = Mathf.FloorToInt(cooldown % 60f);
                    cooldownText.text = $"다음 전투까지: {minutes:00}:{seconds:00}";
                    cooldownText.gameObject.SetActive(true);
                }
                else
                {
                    cooldownText.gameObject.SetActive(false);
                }
            }
            
            // 버튼 활성화 상태
            bool canBattle = dailyRemaining > 0 && cooldown <= 0;
            
            if (dailyBattleButton != null)
                dailyBattleButton.interactable = canBattle;
            if (rankedBattleButton != null)
                rankedBattleButton.interactable = canBattle;
        }
        
        void SelectBattleType(AIGuildBattleSystem.BattleType battleType)
        {
            selectedBattleType = battleType;
            
            if (battleType == AIGuildBattleSystem.BattleType.Ranked)
            {
                // 랭크전은 자동 매칭
                battleSystem.RequestRankedBattle();
                ShowPanel(false);
            }
            else
            {
                // 일반전은 난이도 선택
                ShowDifficultySelection();
            }
        }
        
        void ShowDifficultySelection()
        {
            if (battleSelectionPanel != null)
                battleSelectionPanel.SetActive(false);
            if (difficultyPanel != null)
                difficultyPanel.SetActive(true);
            
            // 기본 난이도 선택
            var recommended = AIGuildGenerator.GetRecommendedDifficulty(GetPlayerLevel());
            SelectDifficulty(recommended);
        }
        
        void UpdateDifficultyButton(DifficultyButton diffButton)
        {
            if (diffButton.nameText != null)
            {
                diffButton.nameText.text = GetDifficultyName(diffButton.difficulty);
            }
            
            if (diffButton.levelRangeText != null)
            {
                var (min, max) = GetLevelRange(diffButton.difficulty);
                diffButton.levelRangeText.text = $"Lv.{min}-{max}";
            }
            
            if (diffButton.rewardText != null)
            {
                AIGuildGenerator.GetBattleRewards(diffButton.difficulty, out int gold, out _, out _);
                diffButton.rewardText.text = $"골드: {gold}";
            }
        }
        
        string GetDifficultyName(AIGuildGenerator.Difficulty difficulty)
        {
            return difficulty switch
            {
                AIGuildGenerator.Difficulty.Novice => "초보자",
                AIGuildGenerator.Difficulty.Bronze => "브론즈",
                AIGuildGenerator.Difficulty.Silver => "실버",
                AIGuildGenerator.Difficulty.Gold => "골드",
                AIGuildGenerator.Difficulty.Platinum => "플래티넘",
                AIGuildGenerator.Difficulty.Diamond => "다이아몬드",
                AIGuildGenerator.Difficulty.Legendary => "전설",
                _ => "알 수 없음"
            };
        }
        
        (int min, int max) GetLevelRange(AIGuildGenerator.Difficulty difficulty)
        {
            return difficulty switch
            {
                AIGuildGenerator.Difficulty.Novice => (1, 5),
                AIGuildGenerator.Difficulty.Bronze => (5, 10),
                AIGuildGenerator.Difficulty.Silver => (10, 15),
                AIGuildGenerator.Difficulty.Gold => (15, 20),
                AIGuildGenerator.Difficulty.Platinum => (20, 25),
                AIGuildGenerator.Difficulty.Diamond => (25, 30),
                AIGuildGenerator.Difficulty.Legendary => (30, 50),
                _ => (1, 50)
            };
        }
        
        void SelectDifficulty(AIGuildGenerator.Difficulty difficulty)
        {
            selectedDifficulty = difficulty;
            
            // 버튼 하이라이트
            foreach (var diffButton in difficultyButtons)
            {
                bool isSelected = diffButton.difficulty == difficulty;
                
                if (diffButton.backgroundImage != null)
                {
                    diffButton.backgroundImage.color = isSelected ? 
                        diffButton.selectedColor : diffButton.normalColor;
                }
            }
            
            // 상대 미리보기 표시 (추후 구현)
            ShowOpponentPreview(difficulty);
        }
        
        void ShowOpponentPreview(AIGuildGenerator.Difficulty difficulty)
        {
            if (opponentPreviewPanel != null)
            {
                opponentPreviewPanel.SetActive(true);
                
                // 예상 상대 정보 표시
                if (opponentNameText != null)
                    opponentNameText.text = "???";
                
                var (min, max) = GetLevelRange(difficulty);
                if (opponentLevelText != null)
                    opponentLevelText.text = $"Lv.{min}-{max}";
                
                if (opponentRatingText != null)
                {
                    int baseRating = 1000 + (min * 50);
                    opponentRatingText.text = $"레이팅: {baseRating}~{baseRating + 500}";
                }
                
                // 예상 보상 표시
                AIGuildGenerator.GetBattleRewards(difficulty, out int gold, out int exp, out int rep);
                if (opponentPowerText != null)
                {
                    opponentPowerText.text = $"보상: 골드 {gold}, 경험치 {exp}, 명성 {rep}";
                }
            }
        }
        
        void StartBattle()
        {
            if (selectedBattleType == AIGuildBattleSystem.BattleType.Daily)
            {
                battleSystem.RequestDailyBattle(selectedDifficulty);
            }
            
            ShowPanel(false);
        }
        
        void CancelSelection()
        {
            if (difficultyPanel != null)
                difficultyPanel.SetActive(false);
            if (battleSelectionPanel != null)
                battleSelectionPanel.SetActive(true);
        }
        
        void RefreshRanking()
        {
            var rankings = battleSystem.GetTopRankings(20);
            UpdateRankingDisplay(rankings);
        }
        
        void UpdateRankingDisplay(List<AIGuildBattleSystem.GuildRanking> rankings)
        {
            // 기존 엔트리 제거
            foreach (var entry in rankingEntries)
            {
                Destroy(entry);
            }
            rankingEntries.Clear();
            
            // 플레이어 정보 업데이트
            int playerRank = battleSystem.GetPlayerRank();
            if (playerRankText != null)
            {
                playerRankText.text = playerRank > 0 ? $"#{playerRank}" : "순위 없음";
            }
            
            var playerRanking = rankings.FirstOrDefault(r => r.guildId == "player");
            if (playerRanking != null && playerRatingText != null)
            {
                playerRatingText.text = $"레이팅: {playerRanking.rating}";
            }
            
            // 랭킹 엔트리 생성
            foreach (var ranking in rankings)
            {
                if (rankingContainer != null && rankingEntryPrefab != null)
                {
                    var entry = Instantiate(rankingEntryPrefab, rankingContainer);
                    SetupRankingEntry(entry, ranking);
                    rankingEntries.Add(entry);
                }
            }
        }
        
        void SetupRankingEntry(GameObject entry, AIGuildBattleSystem.GuildRanking ranking)
        {
            var rankText = entry.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var ratingText = entry.transform.Find("RatingText")?.GetComponent<TextMeshProUGUI>();
            var recordText = entry.transform.Find("RecordText")?.GetComponent<TextMeshProUGUI>();
            
            if (rankText != null)
                rankText.text = $"#{ranking.rank}";
            
            if (nameText != null)
            {
                nameText.text = ranking.guildName;
                if (ranking.guildId == "player")
                {
                    nameText.color = Color.yellow;
                }
            }
            
            if (ratingText != null)
                ratingText.text = ranking.rating.ToString();
            
            if (recordText != null)
                recordText.text = $"{ranking.wins}승 {ranking.losses}패";
        }
        
        void ShowTournamentPanel()
        {
            if (tournamentPanel != null)
            {
                tournamentPanel.SetActive(true);
                UpdateTournamentDisplay();
            }
        }
        
        void UpdateTournamentStatus()
        {
            if (!battleSystem.IsInTournament()) return;
            
            var tournament = battleSystem.GetCurrentTournament();
            if (tournament == null) return;
            
            if (tournamentStatusText != null)
            {
                string status = tournament.currentPhase switch
                {
                    AIGuildBattleSystem.TournamentPhase.RoundOf8 => "8강 진행중",
                    AIGuildBattleSystem.TournamentPhase.SemiFinal => "4강 진행중",
                    AIGuildBattleSystem.TournamentPhase.Final => "결승 진행중",
                    _ => "대기중"
                };
                
                tournamentStatusText.text = $"토너먼트: {status}";
            }
        }
        
        void UpdateTournamentDisplay()
        {
            var tournament = battleSystem.GetCurrentTournament();
            
            if (tournament == null)
            {
                if (joinTournamentButton != null)
                {
                    joinTournamentButton.gameObject.SetActive(true);
                    joinTournamentButton.interactable = !battleSystem.IsInTournament();
                }
                return;
            }
            
            // 대진표 표시
            DisplayTournamentBracket(tournament);
        }
        
        void DisplayTournamentBracket(AIGuildBattleSystem.TournamentData tournament)
        {
            // 기존 매치 표시 제거
            foreach (Transform child in bracketContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 현재 페이즈의 매치 표시
            var currentMatches = tournament.matches
                .Where(m => m.phase == tournament.currentPhase)
                .ToList();
            
            foreach (var match in currentMatches)
            {
                if (matchDisplayPrefab != null)
                {
                    var matchDisplay = Instantiate(matchDisplayPrefab, bracketContainer);
                    SetupMatchDisplay(matchDisplay, match);
                }
            }
        }
        
        void SetupMatchDisplay(GameObject display, AIGuildBattleSystem.TournamentMatch match)
        {
            var guild1Text = display.transform.Find("Guild1Text")?.GetComponent<TextMeshProUGUI>();
            var guild2Text = display.transform.Find("Guild2Text")?.GetComponent<TextMeshProUGUI>();
            var statusText = display.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            
            if (guild1Text != null)
                guild1Text.text = GetGuildDisplayName(match.guild1Id);
            
            if (guild2Text != null)
                guild2Text.text = GetGuildDisplayName(match.guild2Id);
            
            if (statusText != null)
            {
                if (match.isCompleted)
                {
                    string winner = GetGuildDisplayName(match.winnerId);
                    statusText.text = $"승자: {winner}";
                }
                else
                {
                    statusText.text = "대기중";
                }
            }
        }
        
        string GetGuildDisplayName(string guildId)
        {
            if (guildId == "player")
                return "플레이어 길드";
            
            // AI 길드 이름은 시스템에서 조회
            return "AI 길드";
        }
        
        void JoinTournament()
        {
            if (!battleSystem.IsInTournament())
            {
                battleSystem.StartTournament("주간 토너먼트");
                ShowTournamentPanel();
            }
        }
        
        void ShowBattleHistory()
        {
            if (historyPanel != null)
            {
                historyPanel.SetActive(true);
                
                var history = battleSystem.GetBattleHistory(20);
                UpdateHistoryDisplay(history);
            }
        }
        
        void UpdateHistoryDisplay(List<AIGuildBattleSystem.GuildBattleRecord> records)
        {
            // 기존 엔트리 제거
            foreach (var entry in historyEntries)
            {
                Destroy(entry);
            }
            historyEntries.Clear();
            
            // 히스토리 엔트리 생성
            foreach (var record in records)
            {
                if (historyContainer != null && historyEntryPrefab != null)
                {
                    var entry = Instantiate(historyEntryPrefab, historyContainer);
                    SetupHistoryEntry(entry, record);
                    historyEntries.Add(entry);
                }
            }
        }
        
        void SetupHistoryEntry(GameObject entry, AIGuildBattleSystem.GuildBattleRecord record)
        {
            var resultText = entry.transform.Find("ResultText")?.GetComponent<TextMeshProUGUI>();
            var timeText = entry.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
            var statsText = entry.transform.Find("StatsText")?.GetComponent<TextMeshProUGUI>();
            var rewardText = entry.transform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();
            
            if (resultText != null)
            {
                resultText.text = record.victory ? "승리" : "패배";
                resultText.color = record.victory ? Color.green : Color.red;
            }
            
            if (timeText != null)
            {
                timeText.text = record.battleTime.ToString("MM/dd HH:mm");
            }
            
            if (statsText != null)
            {
                statsText.text = $"처치: {record.enemyUnitsKilled} / 손실: {record.unitsLost}";
            }
            
            if (rewardText != null && record.rewards != null)
            {
                rewardText.text = $"골드: {record.rewards.gold}";
            }
        }
        
        void OnBattleCompleted(AIGuildBattleSystem.GuildBattleResult result)
        {
            // 전투 결과 표시
            ShowBattleResult(result);
            
            // UI 업데이트
            UpdateBattleButtons();
            RefreshRanking();
        }
        
        void ShowBattleResult(AIGuildBattleSystem.GuildBattleResult result)
        {
            if (rewardsPopup != null)
            {
                rewardsPopup.SetActive(true);
                
                if (rewardsTitleText != null)
                {
                    rewardsTitleText.text = result.victory ? "승리!" : "패배";
                    rewardsTitleText.color = result.victory ? Color.yellow : Color.gray;
                }
                
                // 보상 표시
                DisplayRewards(result.rewards);
                
                // 레이팅 변화 표시
                if (result.battleType == AIGuildBattleSystem.BattleType.Ranked && result.ratingChange != 0)
                {
                    CreateRewardItem($"레이팅: {(result.ratingChange > 0 ? "+" : "")}{result.ratingChange}");
                }
            }
        }
        
        void DisplayRewards(AIGuildBattleSystem.BattleRewards rewards)
        {
            // 기존 보상 아이템 제거
            foreach (Transform child in rewardsContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (rewards == null) return;
            
            // 골드
            if (rewards.gold > 0)
                CreateRewardItem($"골드: {rewards.gold}");
            
            // 경험치
            if (rewards.experience > 0)
                CreateRewardItem($"경험치: {rewards.experience}");
            
            // 명성
            if (rewards.reputation > 0)
                CreateRewardItem($"명성: {rewards.reputation}");
            
            // 자원
            if (rewards.resources != null)
            {
                foreach (var resource in rewards.resources)
                {
                    if (resource.Value > 0 && resource.Key != Core.ResourceType.Gold)
                    {
                        CreateRewardItem($"{resource.Key}: {resource.Value}");
                    }
                }
            }
        }
        
        void CreateRewardItem(string text)
        {
            if (rewardItemPrefab != null && rewardsContainer != null)
            {
                var item = Instantiate(rewardItemPrefab, rewardsContainer);
                var textComp = item.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                {
                    textComp.text = text;
                }
            }
        }
        
        void ClaimRewards()
        {
            if (rewardsPopup != null)
            {
                rewardsPopup.SetActive(false);
            }
        }
        
        void OnTournamentCompleted(AIGuildBattleSystem.TournamentResult result)
        {
            // 토너먼트 결과 표시
            ShowTournamentResult(result);
        }
        
        void ShowTournamentResult(AIGuildBattleSystem.TournamentResult result)
        {
            if (rewardsPopup != null)
            {
                rewardsPopup.SetActive(true);
                
                if (rewardsTitleText != null)
                {
                    rewardsTitleText.text = $"토너먼트 {result.finalRank}위!";
                }
                
                // 토너먼트 보상 표시
                if (result.rewards?.rankRewards?.ContainsKey(result.finalRank) == true)
                {
                    DisplayRewards(result.rewards.rankRewards[result.finalRank]);
                }
            }
        }
        
        void OnRankingUpdated(List<AIGuildBattleSystem.GuildRanking> rankings)
        {
            if (rankingPanel != null && rankingPanel.activeInHierarchy)
            {
                UpdateRankingDisplay(rankings);
            }
        }
        
        void OnDailyBattlesReset(int maxBattles)
        {
            // 일일 전투 리셋 알림
            Debug.Log($"일일 전투가 리셋되었습니다! ({maxBattles}회)");
            UpdateBattleButtons();
        }
        
        int GetPlayerLevel()
        {
            // GameManager나 GuildManager에서 플레이어 레벨 조회
            return Core.GameManager.Instance?.GuildManager?.GetGuildLevel() ?? 1;
        }
    }
}