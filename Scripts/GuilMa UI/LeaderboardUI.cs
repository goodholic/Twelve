using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.Systems;
using GuildMaster.Core;

namespace GuildMaster.UI
{
    /// <summary>
    /// 리더보드 UI
    /// 각종 랭킹을 표시하고 관리
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private Transform leaderboardContent;
        [SerializeField] private GameObject leaderboardEntryPrefab;
        
        [Header("Tab Buttons")]
        [SerializeField] private Button guildPowerTab;
        [SerializeField] private Button guildWealthTab;
        [SerializeField] private Button guildReputationTab;
        [SerializeField] private Button battleWinsTab;
        [SerializeField] private Button dungeonProgressTab;
        [SerializeField] private Button overallTab;
        
        [Header("Header Info")]
        [SerializeField] private TextMeshProUGUI leaderboardTitle;
        [SerializeField] private TextMeshProUGUI lastUpdateText;
        [SerializeField] private TextMeshProUGUI myRankText;
        
        [Header("Player Info")]
        [SerializeField] private GameObject playerRankPanel;
        [SerializeField] private TextMeshProUGUI playerRankText;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerScoreText;
        [SerializeField] private Image playerRankChangeIcon;
        [SerializeField] private TextMeshProUGUI playerRankChangeText;
        
        [Header("Settings")]
        [SerializeField] private int entriesToShow = 50;
        [SerializeField] private bool showPlayerAlways = true;
        [SerializeField] private float refreshInterval = 30f;
        
        [Header("Visual Settings")]
        [SerializeField] private Color playerEntryColor = new Color(1f, 0.9f, 0.3f, 0.3f);
        [SerializeField] private Color rankUpColor = Color.green;
        [SerializeField] private Color rankDownColor = Color.red;
        [SerializeField] private Sprite rankUpIcon;
        [SerializeField] private Sprite rankDownIcon;
        
        private LeaderboardSystem leaderboardSystem;
        private LeaderboardSystem.LeaderboardType currentType = LeaderboardSystem.LeaderboardType.GuildPower;
        private List<GameObject> entryObjects = new List<GameObject>();
        private string playerId;
        private float lastRefreshTime;
        
        [System.Serializable]
        public class LeaderboardEntryUI
        {
            public GameObject gameObject;
            public TextMeshProUGUI rankText;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI scoreText;
            public Image rankChangeIcon;
            public TextMeshProUGUI rankChangeText;
            public Image background;
            public GameObject crownIcon; // Top 3 표시
        }
        
        void Start()
        {
            leaderboardSystem = LeaderboardSystem.Instance;
            playerId = GameManager.Instance?.GuildManager?.GetGuildId() ?? "player";
            
            SetupTabButtons();
            SubscribeToEvents();
            
            // 초기 탭 선택
            ShowLeaderboard(LeaderboardSystem.LeaderboardType.GuildPower);
        }
        
        void SetupTabButtons()
        {
            if (guildPowerTab != null)
                guildPowerTab.onClick.AddListener(() => ShowLeaderboard(LeaderboardSystem.LeaderboardType.GuildPower));
            
            if (guildWealthTab != null)
                guildWealthTab.onClick.AddListener(() => ShowLeaderboard(LeaderboardSystem.LeaderboardType.GuildWealth));
            
            if (guildReputationTab != null)
                guildReputationTab.onClick.AddListener(() => ShowLeaderboard(LeaderboardSystem.LeaderboardType.GuildReputation));
            
            if (battleWinsTab != null)
                battleWinsTab.onClick.AddListener(() => ShowLeaderboard(LeaderboardSystem.LeaderboardType.BattleWins));
            
            if (dungeonProgressTab != null)
                dungeonProgressTab.onClick.AddListener(() => ShowLeaderboard(LeaderboardSystem.LeaderboardType.DungeonProgress));
            
            if (overallTab != null)
                overallTab.onClick.AddListener(() => ShowOverallRanking());
        }
        
        void SubscribeToEvents()
        {
            if (leaderboardSystem != null)
            {
                leaderboardSystem.OnLeaderboardUpdated += OnLeaderboardUpdated;
                leaderboardSystem.OnRankingChanged += OnRankingChanged;
            }
        }
        
        void Update()
        {
            // 자동 새로고침
            if (Time.time - lastRefreshTime > refreshInterval && leaderboardPanel.activeSelf)
            {
                RefreshCurrentLeaderboard();
            }
        }
        
        public void ShowLeaderboard(LeaderboardSystem.LeaderboardType type)
        {
            currentType = type;
            
            // UI 활성화
            if (leaderboardPanel != null && !leaderboardPanel.activeSelf)
            {
                leaderboardPanel.SetActive(true);
            }
            
            // 제목 업데이트
            UpdateTitle(type);
            
            // 탭 하이라이트
            UpdateTabHighlight(type);
            
            // 리더보드 표시
            DisplayLeaderboard(type);
            
            // 플레이어 정보 표시
            UpdatePlayerInfo(type);
            
            lastRefreshTime = Time.time;
        }
        
        void UpdateTitle(LeaderboardSystem.LeaderboardType type)
        {
            if (leaderboardTitle != null)
            {
                string title = type switch
                {
                    LeaderboardSystem.LeaderboardType.GuildPower => "길드 전투력 랭킹",
                    LeaderboardSystem.LeaderboardType.GuildWealth => "길드 재산 랭킹",
                    LeaderboardSystem.LeaderboardType.GuildReputation => "길드 명성 랭킹",
                    LeaderboardSystem.LeaderboardType.GuildLevel => "길드 레벨 랭킹",
                    LeaderboardSystem.LeaderboardType.BattleWins => "전투 승리 랭킹",
                    LeaderboardSystem.LeaderboardType.DungeonProgress => "던전 진행도 랭킹",
                    LeaderboardSystem.LeaderboardType.AchievementPoints => "업적 점수 랭킹",
                    LeaderboardSystem.LeaderboardType.WeeklyChallenge => "주간 챌린지 랭킹",
                    LeaderboardSystem.LeaderboardType.MonthlyChallenge => "월간 챌린지 랭킹",
                    LeaderboardSystem.LeaderboardType.Season => "시즌 랭킹",
                    _ => type.ToString()
                };
                
                leaderboardTitle.text = title;
            }
            
            if (lastUpdateText != null)
            {
                lastUpdateText.text = $"마지막 업데이트: {DateTime.Now:HH:mm:ss}";
            }
        }
        
        void UpdateTabHighlight(LeaderboardSystem.LeaderboardType type)
        {
            // 모든 탭 비활성화 색상
            Color normalColor = new Color(0.8f, 0.8f, 0.8f);
            Color activeColor = Color.white;
            
            if (guildPowerTab != null)
                guildPowerTab.image.color = type == LeaderboardSystem.LeaderboardType.GuildPower ? activeColor : normalColor;
            
            if (guildWealthTab != null)
                guildWealthTab.image.color = type == LeaderboardSystem.LeaderboardType.GuildWealth ? activeColor : normalColor;
            
            if (guildReputationTab != null)
                guildReputationTab.image.color = type == LeaderboardSystem.LeaderboardType.GuildReputation ? activeColor : normalColor;
            
            if (battleWinsTab != null)
                battleWinsTab.image.color = type == LeaderboardSystem.LeaderboardType.BattleWins ? activeColor : normalColor;
            
            if (dungeonProgressTab != null)
                dungeonProgressTab.image.color = type == LeaderboardSystem.LeaderboardType.DungeonProgress ? activeColor : normalColor;
        }
        
        void DisplayLeaderboard(LeaderboardSystem.LeaderboardType type)
        {
            // 기존 엔트리 제거
            ClearLeaderboardEntries();
            
            // 리더보드 데이터 가져오기
            var entries = leaderboardSystem.GetLeaderboard(type, entriesToShow);
            
            // 플레이어가 리스트에 없으면 추가
            bool playerInList = entries.Exists(e => e.id == playerId);
            if (!playerInList && showPlayerAlways)
            {
                var playerRank = leaderboardSystem.GetPlayerRank(type, playerId);
                if (playerRank > 0)
                {
                    // 플레이어 주변 엔트리 가져오기
                    var nearbyEntries = leaderboardSystem.GetNearbyEntries(type, playerId, 2);
                    
                    // 구분선 추가
                    if (entries.Count > 0 && nearbyEntries.Count > 0)
                    {
                        CreateSeparator();
                    }
                    
                    // 플레이어 포함 엔트리 추가
                    foreach (var entry in nearbyEntries)
                    {
                        CreateLeaderboardEntry(entry, entry.id == playerId);
                    }
                }
            }
            else
            {
                // 일반 엔트리 표시
                foreach (var entry in entries)
                {
                    CreateLeaderboardEntry(entry, entry.id == playerId);
                }
            }
        }
        
        void CreateLeaderboardEntry(LeaderboardSystem.LeaderboardEntry data, bool isPlayer)
        {
            if (leaderboardEntryPrefab == null || leaderboardContent == null)
                return;
            
            var entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContent);
            var entryUI = new LeaderboardEntryUI
            {
                gameObject = entryObj,
                rankText = entryObj.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>(),
                nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>(),
                scoreText = entryObj.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>(),
                rankChangeIcon = entryObj.transform.Find("RankChangeIcon")?.GetComponent<Image>(),
                rankChangeText = entryObj.transform.Find("RankChangeText")?.GetComponent<TextMeshProUGUI>(),
                background = entryObj.GetComponent<Image>(),
                crownIcon = entryObj.transform.Find("CrownIcon")?.gameObject
            };
            
            // 랭크 표시
            if (entryUI.rankText != null)
            {
                entryUI.rankText.text = data.rank.ToString();
                
                // Top 3 특별 표시
                if (data.rank <= 3)
                {
                    entryUI.rankText.color = data.rank switch
                    {
                        1 => new Color(1f, 0.84f, 0f), // 금색
                        2 => new Color(0.75f, 0.75f, 0.75f), // 은색
                        3 => new Color(0.8f, 0.5f, 0.2f), // 동색
                        _ => Color.white
                    };
                    
                    if (entryUI.crownIcon != null)
                    {
                        entryUI.crownIcon.SetActive(true);
                    }
                }
            }
            
            // 이름 표시
            if (entryUI.nameText != null)
            {
                entryUI.nameText.text = data.name;
                if (isPlayer)
                {
                    entryUI.nameText.text += " (나)";
                    entryUI.nameText.fontStyle = FontStyles.Bold;
                }
            }
            
            // 점수 표시
            if (entryUI.scoreText != null)
            {
                entryUI.scoreText.text = FormatScore(currentType, data.score);
            }
            
            // 랭크 변화 표시
            int rankChange = data.GetRankChange();
            if (rankChange != 0 && entryUI.rankChangeIcon != null && entryUI.rankChangeText != null)
            {
                entryUI.rankChangeIcon.gameObject.SetActive(true);
                entryUI.rankChangeText.gameObject.SetActive(true);
                
                if (rankChange > 0)
                {
                    entryUI.rankChangeIcon.sprite = rankUpIcon;
                    entryUI.rankChangeIcon.color = rankUpColor;
                    entryUI.rankChangeText.text = $"+{rankChange}";
                    entryUI.rankChangeText.color = rankUpColor;
                }
                else
                {
                    entryUI.rankChangeIcon.sprite = rankDownIcon;
                    entryUI.rankChangeIcon.color = rankDownColor;
                    entryUI.rankChangeText.text = rankChange.ToString();
                    entryUI.rankChangeText.color = rankDownColor;
                }
            }
            else
            {
                if (entryUI.rankChangeIcon != null)
                    entryUI.rankChangeIcon.gameObject.SetActive(false);
                if (entryUI.rankChangeText != null)
                    entryUI.rankChangeText.gameObject.SetActive(false);
            }
            
            // 플레이어 엔트리 하이라이트
            if (isPlayer && entryUI.background != null)
            {
                entryUI.background.color = playerEntryColor;
            }
            
            entryObjects.Add(entryObj);
        }
        
        void CreateSeparator()
        {
            // 구분선 생성 (점선 등)
            var separator = new GameObject("Separator");
            separator.transform.SetParent(leaderboardContent);
            
            var rect = separator.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 20);
            
            var text = separator.AddComponent<TextMeshProUGUI>();
            text.text = "• • •";
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.5f, 0.5f, 0.5f);
            
            entryObjects.Add(separator);
        }
        
        void UpdatePlayerInfo(LeaderboardSystem.LeaderboardType type)
        {
            var playerRank = leaderboardSystem.GetPlayerRank(type, playerId);
            
            if (myRankText != null)
            {
                if (playerRank > 0)
                {
                    myRankText.text = $"내 순위: {playerRank}위";
                }
                else
                {
                    myRankText.text = "순위 없음";
                }
            }
            
            // 플레이어 정보 패널 업데이트
            if (playerRankPanel != null)
            {
                playerRankPanel.SetActive(playerRank > 0);
                
                if (playerRank > 0)
                {
                    var entries = leaderboardSystem.GetLeaderboard(type, int.MaxValue);
                    var playerEntry = entries.Find(e => e.id == playerId);
                    
                    if (playerEntry != null)
                    {
                        if (playerRankText != null)
                            playerRankText.text = playerRank.ToString();
                        
                        if (playerNameText != null)
                            playerNameText.text = playerEntry.name;
                        
                        if (playerScoreText != null)
                            playerScoreText.text = FormatScore(type, playerEntry.score);
                        
                        // 랭크 변화 표시
                        int rankChange = playerEntry.GetRankChange();
                        if (rankChange != 0)
                        {
                            if (playerRankChangeIcon != null)
                            {
                                playerRankChangeIcon.gameObject.SetActive(true);
                                playerRankChangeIcon.sprite = rankChange > 0 ? rankUpIcon : rankDownIcon;
                                playerRankChangeIcon.color = rankChange > 0 ? rankUpColor : rankDownColor;
                            }
                            
                            if (playerRankChangeText != null)
                            {
                                playerRankChangeText.gameObject.SetActive(true);
                                playerRankChangeText.text = rankChange > 0 ? $"+{rankChange}" : rankChange.ToString();
                                playerRankChangeText.color = rankChange > 0 ? rankUpColor : rankDownColor;
                            }
                        }
                        else
                        {
                            if (playerRankChangeIcon != null)
                                playerRankChangeIcon.gameObject.SetActive(false);
                            if (playerRankChangeText != null)
                                playerRankChangeText.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
        
        void ShowOverallRanking()
        {
            // 종합 랭킹 표시
            ClearLeaderboardEntries();
            
            if (leaderboardTitle != null)
            {
                leaderboardTitle.text = "종합 랭킹";
            }
            
            var overallRanking = leaderboardSystem.GetOverallRanking(entriesToShow);
            
            int rank = 1;
            foreach (var playerData in overallRanking)
            {
                // 종합 랭킹용 엔트리 생성
                var entry = new LeaderboardSystem.LeaderboardEntry
                {
                    id = playerData.playerId,
                    name = playerData.playerName,
                    score = playerData.totalPoints,
                    rank = rank++
                };
                
                CreateLeaderboardEntry(entry, entry.id == playerId);
            }
        }
        
        string FormatScore(LeaderboardSystem.LeaderboardType type, int score)
        {
            return type switch
            {
                LeaderboardSystem.LeaderboardType.GuildPower => $"{score:N0} 전투력",
                LeaderboardSystem.LeaderboardType.GuildWealth => $"{score:N0} 골드",
                LeaderboardSystem.LeaderboardType.GuildReputation => $"{score:N0} 명성",
                LeaderboardSystem.LeaderboardType.GuildLevel => $"Lv.{score}",
                LeaderboardSystem.LeaderboardType.BattleWins => $"{score} 승",
                LeaderboardSystem.LeaderboardType.DungeonProgress => $"{score}층",
                LeaderboardSystem.LeaderboardType.AchievementPoints => $"{score:N0}점",
                _ => score.ToString("N0")
            };
        }
        
        void ClearLeaderboardEntries()
        {
            foreach (var obj in entryObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            entryObjects.Clear();
        }
        
        public void RefreshCurrentLeaderboard()
        {
            ShowLeaderboard(currentType);
        }
        
        public void CloseLeaderboard()
        {
            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(false);
            }
        }
        
        // 이벤트 핸들러
        void OnLeaderboardUpdated(LeaderboardSystem.LeaderboardType type)
        {
            if (type == currentType && leaderboardPanel.activeSelf)
            {
                RefreshCurrentLeaderboard();
            }
        }
        
        void OnRankingChanged(string id, int oldRank, int newRank)
        {
            if (id == playerId)
            {
                // 플레이어 랭킹 변경 알림
                string message = newRank < oldRank ? 
                    $"랭킹 상승! {oldRank}위 → {newRank}위" : 
                    $"랭킹 하락... {oldRank}위 → {newRank}위";
                
                NotificationManager.Instance?.ShowToast(message);
            }
        }
        
        void OnDestroy()
        {
            if (leaderboardSystem != null)
            {
                leaderboardSystem.OnLeaderboardUpdated -= OnLeaderboardUpdated;
                leaderboardSystem.OnRankingChanged -= OnRankingChanged;
            }
        }
    }
}