using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace pjy.Managers
{
    /// <summary>
    /// 길드 시스템 관리
    /// - 길드 생성/가입/탈퇴
    /// - 길드전 매칭
    /// - 길드 보상
    /// </summary>
    public class GuildManager : MonoBehaviour
    {
        private static GuildManager instance;
        public static GuildManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<GuildManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GuildManager");
                        instance = go.AddComponent<GuildManager>();
                    }
                }
                return instance;
            }
        }

        [Header("길드 설정")]
        [SerializeField] private int maxGuildMembers = 30;
        [SerializeField] private int guildCreationCost = 1000;
        [SerializeField] private float guildBonusAttack = 0.05f; // 5% 공격력 보너스
        [SerializeField] private float guildBonusHealth = 0.05f; // 5% 체력 보너스

        [Header("길드 데이터")]
        [SerializeField] private List<Guild> allGuilds = new List<Guild>();
        [SerializeField] private Guild playerGuild;

        [Header("길드 UI")]
        [SerializeField] private GameObject guildPanel;
        [SerializeField] private GameObject guildCreationPanel;
        [SerializeField] private GameObject guildListPanel;
        [SerializeField] private GameObject guildInfoPanel;
        [SerializeField] private TextMeshProUGUI guildNameText;
        [SerializeField] private TextMeshProUGUI guildLevelText;
        [SerializeField] private TextMeshProUGUI guildMembersText;
        [SerializeField] private Transform guildMemberListParent;
        [SerializeField] private GameObject guildMemberItemPrefab;

        [Header("길드전 설정")]
        [SerializeField] private int guildWarDuration = 3600; // 1시간
        [SerializeField] private int guildWarRewardGold = 5000;
        [SerializeField] private int guildWarRewardDiamond = 100;

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
            LoadGuildData();
            InitializeUI();
        }

        /// <summary>
        /// 길드 생성
        /// </summary>
        public bool CreateGuild(string guildName, string guildDescription)
        {
            if (playerGuild != null)
            {
                Debug.LogWarning("[GuildManager] 이미 길드에 가입되어 있습니다.");
                return false;
            }

            if (string.IsNullOrEmpty(guildName) || guildName.Length < 2 || guildName.Length > 20)
            {
                Debug.LogWarning("[GuildManager] 길드 이름은 2~20자여야 합니다.");
                return false;
            }

            // 중복 이름 체크
            if (allGuilds.Any(g => g.guildName == guildName))
            {
                Debug.LogWarning("[GuildManager] 이미 존재하는 길드 이름입니다.");
                return false;
            }

            // 골드 체크
            if (!SpendGold(guildCreationCost))
            {
                Debug.LogWarning($"[GuildManager] 길드 생성에 {guildCreationCost} 골드가 필요합니다.");
                return false;
            }

            // 길드 생성
            Guild newGuild = new Guild
            {
                guildId = System.Guid.NewGuid().ToString(),
                guildName = guildName,
                guildDescription = guildDescription,
                guildLevel = 1,
                guildExp = 0,
                createdDate = System.DateTime.Now,
                masterPlayerId = GetPlayerID(),
                memberIds = new List<string> { GetPlayerID() }
            };

            allGuilds.Add(newGuild);
            playerGuild = newGuild;

            SaveGuildData();
            UpdateGuildUI();

            Debug.Log($"[GuildManager] 길드 '{guildName}' 생성 완료!");
            return true;
        }

        /// <summary>
        /// 길드 가입
        /// </summary>
        public bool JoinGuild(string guildId)
        {
            if (playerGuild != null)
            {
                Debug.LogWarning("[GuildManager] 이미 길드에 가입되어 있습니다.");
                return false;
            }

            Guild targetGuild = allGuilds.FirstOrDefault(g => g.guildId == guildId);
            if (targetGuild == null)
            {
                Debug.LogWarning("[GuildManager] 존재하지 않는 길드입니다.");
                return false;
            }

            if (targetGuild.memberIds.Count >= maxGuildMembers)
            {
                Debug.LogWarning("[GuildManager] 길드 인원이 가득 찼습니다.");
                return false;
            }

            // 가입 신청 (실제로는 길드장 승인 필요)
            targetGuild.memberIds.Add(GetPlayerID());
            playerGuild = targetGuild;

            SaveGuildData();
            UpdateGuildUI();

            // 길드 버프 적용
            ApplyGuildBuffs();

            Debug.Log($"[GuildManager] 길드 '{targetGuild.guildName}' 가입 완료!");
            return true;
        }

        /// <summary>
        /// 길드 탈퇴
        /// </summary>
        public bool LeaveGuild()
        {
            if (playerGuild == null)
            {
                Debug.LogWarning("[GuildManager] 가입한 길드가 없습니다.");
                return false;
            }

            string playerId = GetPlayerID();
            
            // 길드장인 경우
            if (playerGuild.masterPlayerId == playerId)
            {
                if (playerGuild.memberIds.Count > 1)
                {
                    // 다음 멤버에게 길드장 위임
                    playerGuild.masterPlayerId = playerGuild.memberIds.FirstOrDefault(id => id != playerId);
                }
                else
                {
                    // 길드 해체
                    allGuilds.Remove(playerGuild);
                }
            }

            playerGuild.memberIds.Remove(playerId);
            playerGuild = null;

            // 길드 버프 제거
            RemoveGuildBuffs();

            SaveGuildData();
            UpdateGuildUI();

            Debug.Log("[GuildManager] 길드 탈퇴 완료!");
            return true;
        }

        /// <summary>
        /// 길드전 시작
        /// </summary>
        public void StartGuildWar(string targetGuildId)
        {
            if (playerGuild == null)
            {
                Debug.LogWarning("[GuildManager] 길드에 가입되어 있지 않습니다.");
                return;
            }

            Guild targetGuild = allGuilds.FirstOrDefault(g => g.guildId == targetGuildId);
            if (targetGuild == null)
            {
                Debug.LogWarning("[GuildManager] 대상 길드를 찾을 수 없습니다.");
                return;
            }

            // 길드전 매칭
            GuildWar newWar = new GuildWar
            {
                warId = System.Guid.NewGuid().ToString(),
                attackerGuildId = playerGuild.guildId,
                defenderGuildId = targetGuildId,
                startTime = System.DateTime.Now,
                endTime = System.DateTime.Now.AddSeconds(guildWarDuration),
                attackerScore = 0,
                defenderScore = 0
            };

            playerGuild.currentWar = newWar;
            targetGuild.currentWar = newWar;

            SaveGuildData();

            Debug.Log($"[GuildManager] 길드전 시작! {playerGuild.guildName} vs {targetGuild.guildName}");
        }

        /// <summary>
        /// 길드전 점수 업데이트
        /// </summary>
        public void UpdateGuildWarScore(int score)
        {
            if (playerGuild?.currentWar == null)
            {
                return;
            }

            if (playerGuild.guildId == playerGuild.currentWar.attackerGuildId)
            {
                playerGuild.currentWar.attackerScore += score;
            }
            else
            {
                playerGuild.currentWar.defenderScore += score;
            }

            SaveGuildData();
        }

        /// <summary>
        /// 길드전 종료 및 보상
        /// </summary>
        public void EndGuildWar()
        {
            if (playerGuild?.currentWar == null)
            {
                return;
            }

            GuildWar war = playerGuild.currentWar;
            bool isWinner = false;

            if (playerGuild.guildId == war.attackerGuildId)
            {
                isWinner = war.attackerScore > war.defenderScore;
            }
            else
            {
                isWinner = war.defenderScore > war.attackerScore;
            }

            if (isWinner)
            {
                // 승리 보상
                AddGold(guildWarRewardGold);
                AddDiamond(guildWarRewardDiamond);
                playerGuild.guildExp += 100;
                
                Debug.Log($"[GuildManager] 길드전 승리! 보상: {guildWarRewardGold} 골드, {guildWarRewardDiamond} 다이아몬드");
            }
            else
            {
                // 패배 보상 (절반)
                AddGold(guildWarRewardGold / 2);
                playerGuild.guildExp += 50;
                
                Debug.Log($"[GuildManager] 길드전 패배. 보상: {guildWarRewardGold / 2} 골드");
            }

            // 길드 레벨업 체크
            CheckGuildLevelUp();

            playerGuild.currentWar = null;
            SaveGuildData();
        }

        /// <summary>
        /// 길드 레벨업 체크
        /// </summary>
        private void CheckGuildLevelUp()
        {
            if (playerGuild == null) return;

            int requiredExp = playerGuild.guildLevel * 100;
            while (playerGuild.guildExp >= requiredExp)
            {
                playerGuild.guildExp -= requiredExp;
                playerGuild.guildLevel++;
                
                // 레벨업 보상
                guildBonusAttack += 0.01f; // 레벨당 1% 추가
                guildBonusHealth += 0.01f;
                
                Debug.Log($"[GuildManager] 길드 레벨업! 현재 레벨: {playerGuild.guildLevel}");
                
                requiredExp = playerGuild.guildLevel * 100;
            }

            UpdateGuildUI();
        }

        /// <summary>
        /// 길드 버프 적용
        /// </summary>
        private void ApplyGuildBuffs()
        {
            if (playerGuild == null) return;

            // 모든 캐릭터에게 길드 버프 적용
            Character[] allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
            foreach (var character in allCharacters)
            {
                if (character.areaIndex == 1) // 플레이어 캐릭터만
                {
                    character.ApplyGuildBuff(guildBonusAttack, guildBonusHealth);
                }
            }
        }

        /// <summary>
        /// 길드 버프 제거
        /// </summary>
        private void RemoveGuildBuffs()
        {
            Character[] allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
            foreach (var character in allCharacters)
            {
                if (character.areaIndex == 1)
                {
                    character.RemoveGuildBuff();
                }
            }
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            if (guildPanel != null)
            {
                guildPanel.SetActive(false);
            }

            UpdateGuildUI();
        }

        /// <summary>
        /// 길드 UI 업데이트
        /// </summary>
        private void UpdateGuildUI()
        {
            if (playerGuild != null)
            {
                // 길드 정보 표시
                if (guildNameText != null)
                    guildNameText.text = playerGuild.guildName;
                    
                if (guildLevelText != null)
                    guildLevelText.text = $"Lv.{playerGuild.guildLevel}";
                    
                if (guildMembersText != null)
                    guildMembersText.text = $"{playerGuild.memberIds.Count}/{maxGuildMembers}";

                // 멤버 리스트 업데이트
                UpdateMemberList();

                // UI 패널 전환
                if (guildCreationPanel != null)
                    guildCreationPanel.SetActive(false);
                if (guildListPanel != null)
                    guildListPanel.SetActive(false);
                if (guildInfoPanel != null)
                    guildInfoPanel.SetActive(true);
            }
            else
            {
                // 길드 없음 - 생성/가입 UI 표시
                if (guildCreationPanel != null)
                    guildCreationPanel.SetActive(true);
                if (guildListPanel != null)
                    guildListPanel.SetActive(true);
                if (guildInfoPanel != null)
                    guildInfoPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 멤버 리스트 UI 업데이트
        /// </summary>
        private void UpdateMemberList()
        {
            if (guildMemberListParent == null || guildMemberItemPrefab == null) return;

            // 기존 아이템 제거
            foreach (Transform child in guildMemberListParent)
            {
                Destroy(child.gameObject);
            }

            // 멤버 아이템 생성
            foreach (string memberId in playerGuild.memberIds)
            {
                GameObject memberItem = Instantiate(guildMemberItemPrefab, guildMemberListParent);
                TextMeshProUGUI memberText = memberItem.GetComponentInChildren<TextMeshProUGUI>();
                if (memberText != null)
                {
                    memberText.text = memberId; // 실제로는 플레이어 이름으로 변환
                    if (memberId == playerGuild.masterPlayerId)
                    {
                        memberText.text += " (길드장)";
                    }
                }
            }
        }

        /// <summary>
        /// 길드 목록 가져오기
        /// </summary>
        public List<Guild> GetGuildList()
        {
            return allGuilds.OrderByDescending(g => g.guildLevel).ThenByDescending(g => g.memberIds.Count).ToList();
        }

        /// <summary>
        /// 길드 검색
        /// </summary>
        public List<Guild> SearchGuilds(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return GetGuildList();

            return allGuilds.Where(g => 
                g.guildName.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ||
                g.guildDescription.Contains(keyword, System.StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        /// <summary>
        /// 길드 데이터 저장
        /// </summary>
        private void SaveGuildData()
        {
            // 실제로는 서버나 로컬 저장소에 저장
            PlayerPrefs.SetString("GuildData", JsonUtility.ToJson(new GuildSaveData
            {
                allGuilds = allGuilds,
                playerGuildId = playerGuild?.guildId
            }));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 길드 데이터 로드
        /// </summary>
        private void LoadGuildData()
        {
            string savedData = PlayerPrefs.GetString("GuildData", "");
            if (!string.IsNullOrEmpty(savedData))
            {
                GuildSaveData loadedData = JsonUtility.FromJson<GuildSaveData>(savedData);
                allGuilds = loadedData.allGuilds ?? new List<Guild>();
                
                if (!string.IsNullOrEmpty(loadedData.playerGuildId))
                {
                    playerGuild = allGuilds.FirstOrDefault(g => g.guildId == loadedData.playerGuildId);
                }
            }
        }

        /// <summary>
        /// 플레이어 ID 가져오기
        /// </summary>
        private string GetPlayerID()
        {
            // 실제로는 로그인된 플레이어 ID 반환
            return PlayerPrefs.GetString("PlayerID", "Player_" + System.Guid.NewGuid().ToString().Substring(0, 8));
        }

        /// <summary>
        /// 골드 사용
        /// </summary>
        private bool SpendGold(int amount)
        {
            if (ShopManager.Instance != null)
            {
                return ShopManager.Instance.SpendGold(amount);
            }
            return false;
        }

        /// <summary>
        /// 골드 추가
        /// </summary>
        private void AddGold(int amount)
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddGold(amount);
            }
        }

        /// <summary>
        /// 다이아몬드 추가
        /// </summary>
        private void AddDiamond(int amount)
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddDiamond(amount);
            }
        }

        /// <summary>
        /// 길드 패널 토글
        /// </summary>
        public void ToggleGuildPanel()
        {
            if (guildPanel != null)
            {
                guildPanel.SetActive(!guildPanel.activeSelf);
                if (guildPanel.activeSelf)
                {
                    UpdateGuildUI();
                }
            }
        }
    }

    /// <summary>
    /// 길드 데이터 구조
    /// </summary>
    [System.Serializable]
    public class Guild
    {
        public string guildId;
        public string guildName;
        public string guildDescription;
        public int guildLevel;
        public int guildExp;
        public System.DateTime createdDate;
        public string masterPlayerId;
        public List<string> memberIds;
        public GuildWar currentWar;
    }

    /// <summary>
    /// 길드전 데이터
    /// </summary>
    [System.Serializable]
    public class GuildWar
    {
        public string warId;
        public string attackerGuildId;
        public string defenderGuildId;
        public System.DateTime startTime;
        public System.DateTime endTime;
        public int attackerScore;
        public int defenderScore;
    }

    /// <summary>
    /// 길드 저장 데이터
    /// </summary>
    [System.Serializable]
    public class GuildSaveData
    {
        public List<Guild> allGuilds;
        public string playerGuildId;
    }
}