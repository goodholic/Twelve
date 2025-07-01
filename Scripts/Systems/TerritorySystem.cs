using UnityEngine;
using System;
using System.Collections; // IEnumerator를 위해 추가
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core; // ResourceType을 위해 추가
using GuildMaster.Battle; // Battle 네임스페이스 추가

namespace GuildMaster.Systems
{
    public class TerritorySystem : MonoBehaviour
    {
        private static TerritorySystem _instance;
        public static TerritorySystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TerritorySystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("TerritorySystem");
                        _instance = go.AddComponent<TerritorySystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 영토 타입
        public enum TerritoryType
        {
            Plains,      // 평원 - 자원 생산 보너스
            Forest,      // 숲 - 목재 생산 특화
            Mountain,    // 산악 - 석재, 광물 특화
            Coast,       // 해안 - 무역 보너스
            Desert,      // 사막 - 마나스톤 특화
            Fortress,    // 요새 - 방어 보너스
            Capital      // 수도 - 모든 보너스
        }
        
        // 영토 상태
        public enum TerritoryStatus
        {
            Neutral,     // 중립
            PlayerOwned, // 플레이어 소유
            AIOwned,     // AI 길드 소유
            Contested,   // 분쟁 중
            Protected    // 보호 중 (일시적)
        }
        
        // 영토 정보
        [System.Serializable]
        public class Territory
        {
            public int territoryId;
            public string territoryName;
            public string description;
            public TerritoryType type;
            public TerritoryStatus status;
            public Vector2 mapPosition; // 대륙 지도상 위치
            
            public string currentOwner; // 소유 길드 이름
            public float controlPoints; // 0-100, 점령도
            public int defenseLevel; // 방어 수준 1-5
            
            public TerritoryBonus baseBonus;
            public List<string> connectedTerritories; // 인접 영토 ID
            public List<TerritoryBuilding> buildings; // 영토 내 건물
            
            public DateTime lastBattleTime;
            public DateTime protectionEndTime;
            
            public Territory()
            {
                connectedTerritories = new List<string>();
                buildings = new List<TerritoryBuilding>();
            }
        }
        
        // 영토 보너스
        [System.Serializable]
        public class TerritoryBonus
        {
            public Dictionary<ResourceType, float> resourceProduction;
            public float goldMultiplier;
            public float expMultiplier;
            public float combatBonus;
            public float defenseBonus;
            public float tradeBonus;
            public string specialBonus; // 특수 효과
            
            public TerritoryBonus()
            {
                resourceProduction = new Dictionary<ResourceType, float>();
            }
        }
        
        // 영토 건물
        [System.Serializable]
        public class TerritoryBuilding
        {
            public string buildingName;
            public int level;
            public float efficiency;
            public bool isActive;
        }
        
        // 영토 전투
        [System.Serializable]
        public class TerritoryBattle
        {
            public Territory territory;
            public string attackerGuild;
            public string defenderGuild;
            public List<Battle.Squad> attackerSquads;
            public List<Battle.Squad> defenderSquads;
            public DateTime battleStartTime;
            public float battleDuration; // 분
            public bool isActive;
            public BattleResult result;
        }
        
        // 전투 결과
        [System.Serializable]
        public class BattleResult
        {
            public string winner;
            public float controlPointsGained;
            public Dictionary<string, int> casualties;
            public TerritoryReward rewards;
            
            public BattleResult()
            {
                casualties = new Dictionary<string, int>();
            }
        }
        
        // 영토 보상
        [System.Serializable]
        public class TerritoryReward
        {
            public int goldReward;
            public int reputationReward;
            public Dictionary<ResourceType, int> resourceRewards;
            public List<string> itemRewards;
            
            public TerritoryReward()
            {
                resourceRewards = new Dictionary<ResourceType, int>();
                itemRewards = new List<string>();
            }
        }
        
        // 대륙 정보
        [System.Serializable]
        public class Continent
        {
            public string continentName;
            public int totalTerritories;
            public Dictionary<string, int> guildTerritories; // 길드별 영토 수
            public string dominantGuild; // 지배 길드
            
            public Continent()
            {
                guildTerritories = new Dictionary<string, int>();
            }
        }
        
        // 영토 데이터
        private Dictionary<int, Territory> allTerritories;
        private List<TerritoryBattle> activeBattles;
        private Continent worldContinent;
        
        // 플레이어 영토
        private List<int> playerTerritories;
        private float totalTerritoryBonus;
        
        // 주간 정산
        private DateTime nextWeeklySettlement;
        private Dictionary<string, int> weeklyInfluence; // 길드별 주간 영향력
        
        // 이벤트
        public event Action<Territory> OnTerritoryOccupied;
        public event Action<Territory> OnTerritoryLost;
        public event Action<TerritoryBattle> OnBattleStarted;
        public event Action<BattleResult> OnBattleEnded;
        public event Action<TerritoryReward> OnWeeklyReward;
        public event Action<string> OnContinentDominance;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Initialize();
        }
        
        void Initialize()
        {
            allTerritories = new Dictionary<int, Territory>();
            activeBattles = new List<TerritoryBattle>();
            playerTerritories = new List<int>();
            weeklyInfluence = new Dictionary<string, int>();
            
            // 대륙 생성
            CreateWorldMap();
            
            // 주간 정산 시간 설정
            SetNextWeeklySettlement();
            
            // 영토 업데이트 시작
            StartCoroutine(TerritoryUpdater());
            StartCoroutine(BattleProcessor());
        }
        
        void CreateWorldMap()
        {
            worldContinent = new Continent
            {
                continentName = "아르카디아 대륙",
                totalTerritories = 30
            };
            
            // 중앙 지역 (수도)
            CreateTerritory(1, "왕도 엘리시움", TerritoryType.Capital, new Vector2(0, 0));
            
            // 북부 지역 (산악)
            CreateTerritory(2, "북부 산맥", TerritoryType.Mountain, new Vector2(0, 100));
            CreateTerritory(3, "드워프 광산", TerritoryType.Mountain, new Vector2(-50, 120));
            CreateTerritory(4, "설산 요새", TerritoryType.Fortress, new Vector2(50, 120));
            
            // 동부 지역 (숲)
            CreateTerritory(5, "엘프의 숲", TerritoryType.Forest, new Vector2(100, 0));
            CreateTerritory(6, "고대 수목원", TerritoryType.Forest, new Vector2(120, 50));
            CreateTerritory(7, "정령의 샘", TerritoryType.Forest, new Vector2(120, -50));
            
            // 서부 지역 (평원)
            CreateTerritory(8, "황금 평원", TerritoryType.Plains, new Vector2(-100, 0));
            CreateTerritory(9, "곡창 지대", TerritoryType.Plains, new Vector2(-120, 50));
            CreateTerritory(10, "목축 지대", TerritoryType.Plains, new Vector2(-120, -50));
            
            // 남부 지역 (해안)
            CreateTerritory(11, "무역항 포트로얄", TerritoryType.Coast, new Vector2(0, -100));
            CreateTerritory(12, "어촌 마을", TerritoryType.Coast, new Vector2(-50, -120));
            CreateTerritory(13, "해적 소굴", TerritoryType.Coast, new Vector2(50, -120));
            
            // 동남부 지역 (사막)
            CreateTerritory(14, "오아시스", TerritoryType.Desert, new Vector2(80, -80));
            CreateTerritory(15, "고대 유적", TerritoryType.Desert, new Vector2(100, -100));
            
            // 요새 지역
            CreateTerritory(16, "동부 관문", TerritoryType.Fortress, new Vector2(150, 0));
            CreateTerritory(17, "서부 관문", TerritoryType.Fortress, new Vector2(-150, 0));
            CreateTerritory(18, "남부 관문", TerritoryType.Fortress, new Vector2(0, -150));
            CreateTerritory(19, "북부 관문", TerritoryType.Fortress, new Vector2(0, 150));
            
            // 추가 영토들
            for (int i = 20; i <= 30; i++)
            {
                TerritoryType randomType = (TerritoryType)UnityEngine.Random.Range(0, 6);
                Vector2 randomPos = new Vector2(
                    UnityEngine.Random.Range(-200f, 200f),
                    UnityEngine.Random.Range(-200f, 200f)
                );
                CreateTerritory(i, $"영토 {i}", randomType, randomPos);
            }
            
            // 영토 연결 설정
            SetupTerritoryConnections();
            
            // AI 길드에게 일부 영토 할당
            AssignInitialTerritories();
        }
        
        void CreateTerritory(int id, string name, TerritoryType type, Vector2 position)
        {
            Territory territory = new Territory
            {
                territoryId = id,
                territoryName = name,
                description = GetTerritoryDescription(type),
                type = type,
                status = TerritoryStatus.Neutral,
                mapPosition = position,
                controlPoints = 0f,
                defenseLevel = 1
            };
            
            // 타입별 기본 보너스 설정
            territory.baseBonus = CreateTerritoryBonus(type);
            
            allTerritories[id] = territory;
        }
        
        string GetTerritoryDescription(TerritoryType type)
        {
            switch (type)
            {
                case TerritoryType.Plains:
                    return "넓은 평원 지대로 농업과 목축에 적합합니다.";
                case TerritoryType.Forest:
                    return "울창한 숲으로 뒤덮여 있어 목재 생산에 유리합니다.";
                case TerritoryType.Mountain:
                    return "험준한 산악 지대로 광물 자원이 풍부합니다.";
                case TerritoryType.Coast:
                    return "바다와 인접해 있어 무역과 어업이 발달했습니다.";
                case TerritoryType.Desert:
                    return "메마른 사막이지만 신비한 마나스톤이 발견됩니다.";
                case TerritoryType.Fortress:
                    return "전략적 요충지로 방어에 유리한 지형입니다.";
                case TerritoryType.Capital:
                    return "대륙의 중심지로 모든 면에서 발달했습니다.";
                default:
                    return "특별한 특징이 없는 일반적인 영토입니다.";
            }
        }
        
        TerritoryBonus CreateTerritoryBonus(TerritoryType type)
        {
            TerritoryBonus bonus = new TerritoryBonus();
            
            switch (type)
            {
                case TerritoryType.Plains:
                    bonus.goldMultiplier = 1.2f;
                    bonus.resourceProduction[ResourceType.Gold] = 100f;
                    bonus.expMultiplier = 1.1f;
                    break;
                    
                case TerritoryType.Forest:
                    bonus.resourceProduction[ResourceType.Wood] = 200f;
                    bonus.goldMultiplier = 1.0f;
                    bonus.specialBonus = "wood_production";
                    break;
                    
                case TerritoryType.Mountain:
                    bonus.resourceProduction[ResourceType.Stone] = 200f;
                    bonus.resourceProduction[ResourceType.Gold] = 50f;
                    bonus.defenseBonus = 0.2f;
                    bonus.specialBonus = "mining_bonus";
                    break;
                    
                case TerritoryType.Coast:
                    bonus.goldMultiplier = 1.5f;
                    bonus.tradeBonus = 0.3f;
                    bonus.specialBonus = "trade_routes";
                    break;
                    
                case TerritoryType.Desert:
                    bonus.resourceProduction[ResourceType.ManaStone] = 50f;
                    bonus.goldMultiplier = 0.8f;
                    bonus.specialBonus = "mana_crystal";
                    break;
                    
                case TerritoryType.Fortress:
                    bonus.defenseBonus = 0.5f;
                    bonus.combatBonus = 0.1f;
                    bonus.goldMultiplier = 0.9f;
                    bonus.specialBonus = "military_training";
                    break;
                    
                case TerritoryType.Capital:
                    bonus.goldMultiplier = 2.0f;
                    bonus.expMultiplier = 1.5f;
                    bonus.resourceProduction[ResourceType.Gold] = 200f;
                    bonus.resourceProduction[ResourceType.Wood] = 100f;
                    bonus.resourceProduction[ResourceType.Stone] = 100f;
                    bonus.resourceProduction[ResourceType.ManaStone] = 25f;
                    bonus.combatBonus = 0.2f;
                    bonus.defenseBonus = 0.3f;
                    bonus.tradeBonus = 0.5f;
                    bonus.specialBonus = "capital_prosperity";
                    break;
            }
            
            return bonus;
        }
        
        void SetupTerritoryConnections()
        {
            // 거리 기반으로 인접 영토 연결
            float connectionDistance = 100f;
            
            foreach (var territory1 in allTerritories.Values)
            {
                foreach (var territory2 in allTerritories.Values)
                {
                    if (territory1.territoryId != territory2.territoryId)
                    {
                        float distance = Vector2.Distance(territory1.mapPosition, territory2.mapPosition);
                        if (distance <= connectionDistance)
                        {
                            territory1.connectedTerritories.Add(territory2.territoryId.ToString());
                        }
                    }
                }
            }
        }
        
        void AssignInitialTerritories()
        {
            // AI 길드들에게 초기 영토 할당
            string[] aiGuildNames = {
                "철의 늑대단",
                "은빛 상인 연합", 
                "수정 마법사회",
                "성스러운 기사단",
                "그림자 단검단"
            };
            
            // 각 AI 길드에게 2-3개의 영토 할당
            int territoryIndex = 5; // 첫 4개는 중요 영토이므로 제외
            foreach (string guildName in aiGuildNames)
            {
                int territoriesForGuild = UnityEngine.Random.Range(2, 4);
                for (int i = 0; i < territoriesForGuild && territoryIndex <= 30; i++)
                {
                    if (allTerritories.ContainsKey(territoryIndex))
                    {
                        Territory territory = allTerritories[territoryIndex];
                        territory.status = TerritoryStatus.AIOwned;
                        territory.currentOwner = guildName;
                        territory.controlPoints = 100f;
                        territory.defenseLevel = UnityEngine.Random.Range(1, 4);
                        
                        UpdateContinentStatus();
                    }
                    territoryIndex++;
                }
            }
        }
        
        // 영토 점령 시도
        public bool AttemptOccupyTerritory(int territoryId, List<Battle.Squad> squads)
        {
            if (!allTerritories.ContainsKey(territoryId))
                return false;
            
            Territory territory = allTerritories[territoryId];
            
            // 이미 플레이어 소유인지 확인
            if (territory.status == TerritoryStatus.PlayerOwned)
                return false;
            
            // 보호 중인지 확인
            if (territory.status == TerritoryStatus.Protected && 
                DateTime.Now < territory.protectionEndTime)
            {
                Debug.Log("Territory is under protection");
                return false;
            }
            
            // 이미 전투 중인지 확인
            if (activeBattles.Any(b => b.territory.territoryId == territoryId && b.isActive))
            {
                Debug.Log("Territory is already in battle");
                return false;
            }
            
            // 전투 시작
            StartTerritoryBattle(territory, "Player Guild", territory.currentOwner ?? "Neutral", squads);
            
            return true;
        }
        
        void StartTerritoryBattle(Territory territory, string attacker, string defender, 
                                 List<Battle.Squad> attackerSquads)
        {
            TerritoryBattle battle = new TerritoryBattle
            {
                territory = territory,
                attackerGuild = attacker,
                defenderGuild = defender,
                attackerSquads = attackerSquads,
                battleStartTime = DateTime.Now,
                battleDuration = 10f, // 10분
                isActive = true
            };
            
            // 방어 부대 생성 (AI 또는 중립)
            if (defender != "Neutral")
            {
                battle.defenderSquads = GenerateDefenderSquads(territory);
            }
            
            territory.status = TerritoryStatus.Contested;
            activeBattles.Add(battle);
            
            OnBattleStarted?.Invoke(battle);
        }
        
        List<Battle.Squad> GenerateDefenderSquads(Territory territory)
        {
            List<Battle.Squad> defenderSquads = new List<Battle.Squad>();
            
            // 방어 레벨에 따라 부대 수 결정
            int squadCount = Mathf.Min(territory.defenseLevel, 4);
            
            // AI 길드 생성기를 사용하여 방어 부대 생성
            // 영토 소유자의 난이도 추정
            AIGuildGenerator.Difficulty difficulty = territory.defenseLevel switch
            {
                1 => AIGuildGenerator.Difficulty.Novice,
                2 => AIGuildGenerator.Difficulty.Bronze,
                3 => AIGuildGenerator.Difficulty.Silver,
                4 => AIGuildGenerator.Difficulty.Gold,
                5 => AIGuildGenerator.Difficulty.Platinum,
                _ => AIGuildGenerator.Difficulty.Novice
            };
            
            var aiSquads = AIGuildGenerator.GenerateAIGuild(difficulty, 15);
            
            for (int i = 0; i < squadCount && i < aiSquads.Count; i++)
            {
                if (aiSquads[i] != null)
                {
                    defenderSquads.Add(aiSquads[i]);
                }
            }
            
            return defenderSquads;
        }
        
        IEnumerator BattleProcessor()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                for (int i = activeBattles.Count - 1; i >= 0; i--)
                {
                    TerritoryBattle battle = activeBattles[i];
                    
                    if (!battle.isActive) continue;
                    
                    float elapsedTime = (float)(DateTime.Now - battle.battleStartTime).TotalMinutes;
                    
                    // 전투 시간 종료 또는 즉시 결과 계산
                    if (elapsedTime >= battle.battleDuration || ShouldCalculateBattleResult(battle))
                    {
                        BattleResult result = CalculateBattleResult(battle);
                        ApplyBattleResult(battle, result);
                        
                        battle.isActive = false;
                        activeBattles.RemoveAt(i);
                        
                        OnBattleEnded?.Invoke(result);
                    }
                }
            }
        }
        
        bool ShouldCalculateBattleResult(TerritoryBattle battle)
        {
            // 한쪽 부대가 전멸했는지 확인
            bool attackerDefeated = battle.attackerSquads.All(s => s.isDefeated);
            bool defenderDefeated = battle.defenderSquads == null || 
                                   battle.defenderSquads.Count == 0 ||
                                   battle.defenderSquads.All(s => s.isDefeated);
            
            return attackerDefeated || defenderDefeated;
        }
        
        BattleResult CalculateBattleResult(TerritoryBattle battle)
        {
            BattleResult result = new BattleResult();
            
            // 전투력 계산
            float attackerPower = 0f;
            float defenderPower = 0f;
            
            foreach (var squad in battle.attackerSquads)
            {
                attackerPower += squad.totalPower;
                result.casualties[battle.attackerGuild] = squad.GetAllUnits().Count - squad.GetAliveUnits().Count;
            }
            
            if (battle.defenderSquads != null)
            {
                foreach (var squad in battle.defenderSquads)
                {
                    defenderPower += squad.totalPower;
                    result.casualties[battle.defenderGuild] = squad.GetAllUnits().Count - squad.GetAliveUnits().Count;
                }
            }
            
            // 영토 보너스 적용
            if (battle.territory.status == TerritoryStatus.AIOwned)
            {
                defenderPower *= (1f + battle.territory.baseBonus.defenseBonus);
            }
            
            // 승자 결정
            if (attackerPower > defenderPower * 1.2f)
            {
                result.winner = battle.attackerGuild;
                result.controlPointsGained = Mathf.Min(100f, 50f + (attackerPower / defenderPower) * 25f);
            }
            else if (defenderPower > attackerPower * 1.2f)
            {
                result.winner = battle.defenderGuild;
                result.controlPointsGained = 0f;
            }
            else
            {
                // 무승부 - 공격자가 일부 점령도 획득
                result.winner = "Draw";
                result.controlPointsGained = 25f;
            }
            
            // 보상 계산
            if (result.winner == battle.attackerGuild)
            {
                result.rewards = CalculateTerritoryReward(battle.territory);
            }
            
            return result;
        }
        
        TerritoryReward CalculateTerritoryReward(Territory territory)
        {
            TerritoryReward reward = new TerritoryReward
            {
                goldReward = Mathf.RoundToInt(1000 * territory.baseBonus.goldMultiplier),
                reputationReward = 50
            };
            
            foreach (var resource in territory.baseBonus.resourceProduction)
            {
                reward.resourceRewards[resource.Key] = Mathf.RoundToInt(resource.Value);
            }
            
            // 특별 보상
            if (territory.type == TerritoryType.Capital)
            {
                reward.goldReward *= 2;
                reward.reputationReward *= 2;
                reward.itemRewards.Add("capital_treasure");
            }
            
            return reward;
        }
        
        void ApplyBattleResult(TerritoryBattle battle, BattleResult result)
        {
            Territory territory = battle.territory;
            
            if (result.winner == battle.attackerGuild)
            {
                // 공격자 승리
                if (battle.attackerGuild == "Player Guild")
                {
                    // 플레이어가 영토 획득
                    territory.controlPoints += result.controlPointsGained;
                    
                    if (territory.controlPoints >= 100f)
                    {
                        OccupyTerritory(territory, true);
                    }
                    
                    // 보상 지급
                    if (result.rewards != null)
                    {
                        ApplyTerritoryReward(result.rewards);
                    }
                }
                else
                {
                    // AI가 영토 획득
                    territory.currentOwner = battle.attackerGuild;
                    territory.status = TerritoryStatus.AIOwned;
                    territory.controlPoints = result.controlPointsGained;
                }
            }
            else if (result.winner == "Draw")
            {
                // 무승부 - 부분 점령
                if (battle.attackerGuild == "Player Guild")
                {
                    territory.controlPoints = Mathf.Min(territory.controlPoints + result.controlPointsGained, 99f);
                }
            }
            
            // 전투 후 보호 시간 설정
            territory.lastBattleTime = DateTime.Now;
            territory.protectionEndTime = DateTime.Now.AddHours(24); // 24시간 보호
            territory.status = TerritoryStatus.Protected;
            
            UpdateContinentStatus();
        }
        
        void OccupyTerritory(Territory territory, bool isPlayer)
        {
            string previousOwner = territory.currentOwner;
            
            if (isPlayer)
            {
                territory.status = TerritoryStatus.PlayerOwned;
                territory.currentOwner = "Player Guild";
                territory.controlPoints = 100f;
                
                if (!playerTerritories.Contains(territory.territoryId))
                {
                    playerTerritories.Add(territory.territoryId);
                }
                
                OnTerritoryOccupied?.Invoke(territory);
            }
            else
            {
                territory.status = TerritoryStatus.AIOwned;
                
                if (playerTerritories.Contains(territory.territoryId))
                {
                    playerTerritories.Remove(territory.territoryId);
                    OnTerritoryLost?.Invoke(territory);
                }
            }
            
            // 영토 보너스 재계산
            CalculateTotalTerritoryBonus();
            
            UpdateContinentStatus();
        }
        
        void CalculateTotalTerritoryBonus()
        {
            totalTerritoryBonus = 0f;
            
            foreach (int territoryId in playerTerritories)
            {
                if (allTerritories.ContainsKey(territoryId))
                {
                    Territory territory = allTerritories[territoryId];
                    
                    // 기본 보너스
                    totalTerritoryBonus += territory.baseBonus.goldMultiplier - 1f;
                    
                    // 연결 보너스 (인접한 플레이어 영토)
                    int connectedPlayerTerritories = 0;
                    foreach (string connectedId in territory.connectedTerritories)
                    {
                        if (int.TryParse(connectedId, out int id) && playerTerritories.Contains(id))
                        {
                            connectedPlayerTerritories++;
                        }
                    }
                    
                    if (connectedPlayerTerritories > 0)
                    {
                        totalTerritoryBonus += connectedPlayerTerritories * 0.05f; // 연결당 5% 보너스
                    }
                }
            }
        }
        
        void ApplyTerritoryReward(TerritoryReward reward)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            // 골드
            if (reward.goldReward > 0 && gameManager.ResourceManager != null)
            {
                gameManager.ResourceManager.AddGold(reward.goldReward);
            }
            
            // 평판
            if (reward.reputationReward > 0 && gameManager.ResourceManager != null)
            {
                gameManager.ResourceManager.AddReputation(reward.reputationReward);
            }
            
            // 자원
            if (gameManager.ResourceManager != null)
            {
                foreach (var resource in reward.resourceRewards)
                {
                    switch (resource.Key)
                    {
                        case ResourceType.Wood:
                            gameManager.ResourceManager.AddWood(resource.Value);
                            break;
                        case ResourceType.Stone:
                            gameManager.ResourceManager.AddStone(resource.Value);
                            break;
                        case ResourceType.ManaStone:
                            gameManager.ResourceManager.AddManaStone(resource.Value);
                            break;
                    }
                }
            }
            
            // 아이템
            foreach (string itemId in reward.itemRewards)
            {
                Debug.Log($"Territory reward item: {itemId}");
            }
        }
        
        IEnumerator TerritoryUpdater()
        {
            while (true)
            {
                yield return new WaitForSeconds(60f); // 1분마다 업데이트
                
                // 플레이어 영토 자원 생산
                foreach (int territoryId in playerTerritories)
                {
                    if (allTerritories.ContainsKey(territoryId))
                    {
                        Territory territory = allTerritories[territoryId];
                        ProduceTerritoryResources(territory);
                    }
                }
                
                // AI 활동 시뮬레이션
                SimulateAITerritoryActions();
                
                // 주간 정산 체크
                if (DateTime.Now >= nextWeeklySettlement)
                {
                    PerformWeeklySettlement();
                }
            }
        }
        
        void ProduceTerritoryResources(Territory territory)
        {
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager == null) return;
            
            foreach (var production in territory.baseBonus.resourceProduction)
            {
                int amount = Mathf.RoundToInt(production.Value / 60f); // 분당 생산량
                
                switch (production.Key)
                {
                    case ResourceType.Gold:
                        resourceManager.AddGold(amount);
                        break;
                    case ResourceType.Wood:
                        resourceManager.AddWood(amount);
                        break;
                    case ResourceType.Stone:
                        resourceManager.AddStone(amount);
                        break;
                    case ResourceType.ManaStone:
                        resourceManager.AddManaStone(amount);
                        break;
                }
            }
        }
        
        void SimulateAITerritoryActions()
        {
            // AI 길드들의 영토 공격 시뮬레이션
            if (UnityEngine.Random.value < 0.1f) // 10% 확률
            {
                // 랜덤한 AI 길드가 랜덤한 영토 공격
                var neutralTerritories = allTerritories.Values.Where(t => 
                    t.status == TerritoryStatus.Neutral && 
                    t.protectionEndTime < DateTime.Now).ToList();
                
                if (neutralTerritories.Count > 0)
                {
                    Territory targetTerritory = neutralTerritories[UnityEngine.Random.Range(0, neutralTerritories.Count)];
                    string attackerGuild = GetRandomAIGuild();
                    
                    // 간단한 점령 시뮬레이션
                    targetTerritory.status = TerritoryStatus.AIOwned;
                    targetTerritory.currentOwner = attackerGuild;
                    targetTerritory.controlPoints = 100f;
                    targetTerritory.defenseLevel = UnityEngine.Random.Range(1, 4);
                    
                    UpdateContinentStatus();
                }
            }
        }
        
        string GetRandomAIGuild()
        {
            string[] aiGuilds = {
                "철의 늑대단",
                "은빛 상인 연합",
                "수정 마법사회",
                "성스러운 기사단",
                "그림자 단검단"
            };
            
            return aiGuilds[UnityEngine.Random.Range(0, aiGuilds.Length)];
        }
        
        void UpdateContinentStatus()
        {
            worldContinent.guildTerritories.Clear();
            
            foreach (var territory in allTerritories.Values)
            {
                if (!string.IsNullOrEmpty(territory.currentOwner))
                {
                    if (!worldContinent.guildTerritories.ContainsKey(territory.currentOwner))
                    {
                        worldContinent.guildTerritories[territory.currentOwner] = 0;
                    }
                    worldContinent.guildTerritories[territory.currentOwner]++;
                }
            }
            
            // 지배 길드 결정 (전체 영토의 50% 이상)
            foreach (var guild in worldContinent.guildTerritories)
            {
                if (guild.Value >= worldContinent.totalTerritories * 0.5f)
                {
                    if (worldContinent.dominantGuild != guild.Key)
                    {
                        worldContinent.dominantGuild = guild.Key;
                        OnContinentDominance?.Invoke(guild.Key);
                    }
                    break;
                }
            }
        }
        
        void SetNextWeeklySettlement()
        {
            DateTime now = DateTime.Now;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0 && now.Hour >= 5)
            {
                daysUntilMonday = 7;
            }
            nextWeeklySettlement = now.Date.AddDays(daysUntilMonday).AddHours(5);
        }
        
        void PerformWeeklySettlement()
        {
            Debug.Log("Performing weekly territory settlement...");
            
            // 플레이어 보유 영토에 따른 주간 보상
            TerritoryReward weeklyReward = new TerritoryReward();
            
            foreach (int territoryId in playerTerritories)
            {
                if (allTerritories.ContainsKey(territoryId))
                {
                    Territory territory = allTerritories[territoryId];
                    
                    weeklyReward.goldReward += Mathf.RoundToInt(1000 * territory.baseBonus.goldMultiplier);
                    weeklyReward.reputationReward += 20;
                    
                    // 특별 보상
                    if (territory.type == TerritoryType.Capital)
                    {
                        weeklyReward.goldReward += 5000;
                        weeklyReward.itemRewards.Add("weekly_capital_chest");
                    }
                }
            }
            
            // 연결 보너스
            int connectedTerritories = CountConnectedTerritories();
            weeklyReward.goldReward += connectedTerritories * 500;
            
            // 보상 지급
            if (weeklyReward.goldReward > 0)
            {
                ApplyTerritoryReward(weeklyReward);
                OnWeeklyReward?.Invoke(weeklyReward);
            }
            
            // 다음 정산 시간 설정
            SetNextWeeklySettlement();
        }
        
        int CountConnectedTerritories()
        {
            HashSet<int> visited = new HashSet<int>();
            int largestGroup = 0;
            
            foreach (int territoryId in playerTerritories)
            {
                if (!visited.Contains(territoryId))
                {
                    int groupSize = CountConnectedGroup(territoryId, visited);
                    largestGroup = Mathf.Max(largestGroup, groupSize);
                }
            }
            
            return largestGroup;
        }
        
        int CountConnectedGroup(int startId, HashSet<int> visited)
        {
            if (visited.Contains(startId) || !playerTerritories.Contains(startId))
                return 0;
            
            visited.Add(startId);
            int count = 1;
            
            if (allTerritories.ContainsKey(startId))
            {
                foreach (string connectedId in allTerritories[startId].connectedTerritories)
                {
                    if (int.TryParse(connectedId, out int id))
                    {
                        count += CountConnectedGroup(id, visited);
                    }
                }
            }
            
            return count;
        }
        
        // 영토 업그레이드
        public bool UpgradeTerritoryDefense(int territoryId)
        {
            if (!allTerritories.ContainsKey(territoryId) || !playerTerritories.Contains(territoryId))
                return false;
            
            Territory territory = allTerritories[territoryId];
            
            if (territory.defenseLevel >= 5)
                return false;
            
            // 업그레이드 비용
            int cost = territory.defenseLevel * 2000;
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            
            if (resourceManager != null && resourceManager.GetGold() >= cost)
            {
                resourceManager.AddGold(-cost);
                territory.defenseLevel++;
                Debug.Log($"Territory {territory.territoryName} defense upgraded to level {territory.defenseLevel}");
                return true;
            }
            
            return false;
        }
        
        // 조회 메서드들
        public List<Territory> GetAllTerritories()
        {
            return allTerritories.Values.ToList();
        }
        
        public List<Territory> GetPlayerTerritories()
        {
            return playerTerritories.Select(id => allTerritories[id]).ToList();
        }
        
        public Territory GetTerritory(int territoryId)
        {
            return allTerritories.ContainsKey(territoryId) ? allTerritories[territoryId] : null;
        }
        
        public float GetTotalTerritoryBonus()
        {
            return totalTerritoryBonus;
        }
        
        public Continent GetContinentStatus()
        {
            return worldContinent;
        }
        
        public List<TerritoryBattle> GetActiveBattles()
        {
            return new List<TerritoryBattle>(activeBattles);
        }
        
        public bool CanAttackTerritory(int territoryId)
        {
            if (!allTerritories.ContainsKey(territoryId))
                return false;
            
            Territory territory = allTerritories[territoryId];
            
            // 플레이어 소유 영토는 공격 불가
            if (territory.status == TerritoryStatus.PlayerOwned)
                return false;
            
            // 보호 중인 영토는 공격 불가
            if (territory.protectionEndTime > DateTime.Now)
                return false;
            
            // 이미 전투 중인 영토는 공격 불가
            if (activeBattles.Any(b => b.territory.territoryId == territoryId && b.isActive))
                return false;
            
            // 인접한 플레이어 영토가 있어야 공격 가능
            bool hasAdjacentPlayerTerritory = false;
            foreach (string connectedId in territory.connectedTerritories)
            {
                if (int.TryParse(connectedId, out int id) && playerTerritories.Contains(id))
                {
                    hasAdjacentPlayerTerritory = true;
                    break;
                }
            }
            
            return hasAdjacentPlayerTerritory || playerTerritories.Count == 0; // 첫 영토는 어디든 공격 가능
        }
    }
}