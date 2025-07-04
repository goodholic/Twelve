using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;

namespace GuildMaster.Battle
{
    /// <summary>
    /// README 기준 길드 전투 핵심 시스템
    /// - 2:2 부대 전투 (각 부대당 9명씩)
    /// - 총 18명 vs 18명 정예 전투
    /// - 18명의 캐릭터 수집 시스템
    /// </summary>
    public class GuildBattleCore : MonoBehaviour
    {
        public static GuildBattleCore Instance { get; private set; }
        
        // README 기준 상수
        public const int SQUADS_PER_GUILD = 2;      // 2부대
        public const int UNITS_PER_SQUAD = 9;       // 각 부대당 9명
        public const int TOTAL_COLLECTIBLE_CHARACTERS = 18;  // 수집 가능한 캐릭터 총 18명
        public const int SQUAD_GRID_WIDTH = 6;      // 6x3 그리드
        public const int SQUAD_GRID_HEIGHT = 3;
        
        [Header("길드 전투 설정")]
        [SerializeField] private float battleSpeed = 1.0f;
        [SerializeField] private bool autoPlay = true;
        [SerializeField] private float turnInterval = 2.0f;
        
        // 길드 데이터
        [Header("길드 구성")]
        [SerializeField] private List<GuildMaster.Battle.Unit> collectedCharacters = new List<GuildMaster.Battle.Unit>(); // 수집한 18명의 캐릭터
        [SerializeField] private GuildSquad squad1 = new GuildSquad();             // 1부대 (9명)
        [SerializeField] private GuildSquad squad2 = new GuildSquad();             // 2부대 (9명)
        
        // 전투 상태
        private bool isInBattle = false;
        private GuildFormation playerFormation;
        private GuildFormation enemyFormation;
        
        // 이벤트
        public event Action<GuildBattleResult> OnBattleComplete;
        public event Action<Unit> OnCharacterCollected;
        public event Action<GuildSquad, GuildSquad> OnBattleStart;
        
        // 현재 편성된 부대들
        private List<Squad> currentSquads = new List<Squad>();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGuildSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 길드 시스템 초기화
        /// </summary>
        private void InitializeGuildSystem()
        {
            // 기본 캐릭터 생성 (시작 시 몇 명)
            GenerateStartingCharacters();
            
            // 부대 초기화
            squad1.Initialize("정예부대", 0);
            squad2.Initialize("선봉부대", 1);
            
            Debug.Log($"길드 시스템 초기화 완료 - 수집 가능 캐릭터: {TOTAL_COLLECTIBLE_CHARACTERS}명");
        }
        
        /// <summary>
        /// 시작 캐릭터들 생성
        /// </summary>
        private void GenerateStartingCharacters()
        {
            // 각 직업별 기본 캐릭터 1명씩 생성 (총 7명)
            JobClass[] startingJobs = { 
                JobClass.Warrior, JobClass.Knight, JobClass.Mage, 
                JobClass.Priest, JobClass.Assassin, JobClass.Ranger, JobClass.Sage 
            };
            
            foreach (var job in startingJobs)
            {
                var character = CreateCharacter($"Basic {job}", 1, job, Rarity.Common);
                CollectCharacter(character);
            }
        }
        
        /// <summary>
        /// 새 캐릭터 생성
        /// </summary>
        public Unit CreateCharacter(string name, int level, JobClass jobClass, Rarity rarity)
        {
            GameObject characterGO = new GameObject(name);
            characterGO.transform.SetParent(transform);
            
            Unit character = characterGO.AddComponent<Unit>();
            character.unitName = name;
            character.level = level;
            character.jobClass = jobClass;
            character.rarity = rarity;
            character.isPlayerUnit = true;
            character.unitId = System.Guid.NewGuid().ToString();
            
            return character;
        }
        
        /// <summary>
        /// 캐릭터 수집 (최대 18명)
        /// </summary>
        public bool CollectCharacter(Unit character)
        {
            if (collectedCharacters.Count >= TOTAL_COLLECTIBLE_CHARACTERS)
            {
                Debug.LogWarning($"이미 최대 캐릭터 수({TOTAL_COLLECTIBLE_CHARACTERS}명)에 도달했습니다!");
                return false;
            }
            
            collectedCharacters.Add(character);
            OnCharacterCollected?.Invoke(character);
            
            Debug.Log($"캐릭터 수집: {character.unitName} ({collectedCharacters.Count}/{TOTAL_COLLECTIBLE_CHARACTERS})");
            return true;
        }
        
        /// <summary>
        /// 부대에 캐릭터 배치
        /// </summary>
        public bool AssignCharacterToSquad(Unit character, int squadIndex, Vector2Int gridPos)
        {
            if (squadIndex < 0 || squadIndex >= SQUADS_PER_GUILD) return false;
            if (!collectedCharacters.Contains(character)) return false;
            
            GuildSquad targetSquad = squadIndex == 0 ? squad1 : squad2;
            return targetSquad.AssignUnit(character, gridPos);
        }
        
        /// <summary>
        /// 전투 시작 (2:2 부대 전투)
        /// </summary>
        public void StartGuildBattle(GuildFormation enemyGuildFormation)
        {
            if (isInBattle)
            {
                Debug.LogWarning("이미 전투 중입니다!");
                return;
            }
            
            // 플레이어 길드 편성
            playerFormation = new GuildFormation();
            playerFormation.SetSquad(0, squad1);
            playerFormation.SetSquad(1, squad2);
            playerFormation.guildName = "플레이어 길드";
            
            this.enemyFormation = enemyGuildFormation;
            isInBattle = true;
            
            OnBattleStart?.Invoke(squad1, squad2);
            
            if (autoPlay)
            {
                StartCoroutine(AutoBattleCoroutine());
            }
        }
        
        /// <summary>
        /// 자동 전투 코루틴
        /// </summary>
        private System.Collections.IEnumerator AutoBattleCoroutine()
        {
            Debug.Log("길드 자동 전투 시작!");
            
            int turn = 0;
            while (isInBattle && !IsBattleFinished())
            {
                turn++;
                
                // 플레이어 길드 턴
                yield return ExecuteGuildTurn(playerFormation, enemyFormation);
                
                if (IsBattleFinished()) break;
                
                // 적 길드 턴  
                yield return ExecuteGuildTurn(enemyFormation, playerFormation);
                
                yield return new WaitForSeconds(turnInterval / battleSpeed);
            }
            
            // 전투 결과 처리
            FinishBattle();
        }
        
        /// <summary>
        /// 길드 턴 실행
        /// </summary>
        private System.Collections.IEnumerator ExecuteGuildTurn(GuildFormation attackingGuild, GuildFormation defendingGuild)
        {
            for (int squadIndex = 0; squadIndex < SQUADS_PER_GUILD; squadIndex++)
            {
                var squad = attackingGuild.GetSquad(squadIndex);
                if (squad != null && !squad.IsDefeated())
                {
                    yield return ExecuteSquadAttack(squad, defendingGuild);
                }
            }
        }
        
        /// <summary>
        /// 부대 공격 실행
        /// </summary>
        private System.Collections.IEnumerator ExecuteSquadAttack(GuildSquad attackingSquad, GuildFormation defendingFormation)
        {
            var attackers = attackingSquad.GetAliveUnits();
            
            foreach (var attacker in attackers)
            {
                var target = FindBestTarget(attacker, defendingFormation);
                if (target != null)
                {
                    float damage = attacker.GetAttackDamage();
                    target.TakeDamage(damage);
                }
                
                yield return new WaitForSeconds(0.1f / battleSpeed);
            }
        }
        
        /// <summary>
        /// 최적 타겟 찾기
        /// </summary>
        private Unit FindBestTarget(Unit attacker, GuildFormation defendingFormation)
        {
            var allTargets = new List<Unit>();
            
            for (int i = 0; i < SQUADS_PER_GUILD; i++)
            {
                var squad = defendingFormation.GetSquad(i);
                if (squad != null)
                {
                    allTargets.AddRange(squad.GetAliveUnits());
                }
            }
            
            return allTargets.Count > 0 ? allTargets[UnityEngine.Random.Range(0, allTargets.Count)] : null;
        }
        
        /// <summary>
        /// 전투 종료 체크
        /// </summary>
        private bool IsBattleFinished()
        {
            return playerFormation.IsDefeated() || enemyFormation.IsDefeated();
        }
        
        /// <summary>
        /// 전투 완료 처리
        /// </summary>
        private void FinishBattle()
        {
            isInBattle = false;
            
            bool playerWin = !playerFormation.IsDefeated();
            var result = new GuildBattleResult
            {
                isVictory = playerWin,
                survivingUnits = playerFormation.GetAliveUnitCount(),
                totalTurns = 0,
                rewards = CalculateBattleRewards(playerWin)
            };
            
            OnBattleComplete?.Invoke(result);
            Debug.Log($"길드 전투 완료! 결과: {(playerWin ? "승리" : "패배")}");
        }
        
        /// <summary>
        /// 전투 보상 계산
        /// </summary>
        private BattleRewards CalculateBattleRewards(bool victory)
        {
            return new BattleRewards
            {
                gold = victory ? 1000 : 100,
                exp = victory ? 500 : 50,
                reputation = victory ? 10 : -5
            };
        }
        
        // 접근자 메서드들
        public List<Unit> GetCollectedCharacters() => collectedCharacters;
        public GuildSquad GetSquad(int index) => index == 0 ? squad1 : squad2;
        public bool IsInBattle() => isInBattle;
        public int GetCollectedCharacterCount() => collectedCharacters.Count;
        public bool CanCollectMoreCharacters() => collectedCharacters.Count < TOTAL_COLLECTIBLE_CHARACTERS;
        
        /// <summary>
        /// GameManager에서 호출하는 초기화 메서드
        /// </summary>
        public void Initialize()
        {
            if (Instance == null)
            {
                InitializeGuildSystem();
            }
        }
        
        /// <summary>
        /// GameManager에서 호출하는 업데이트 메서드
        /// </summary>
        public void UpdateBattleSystems(float deltaTime)
        {
            // 전투 관련 시스템 업데이트
            if (isInBattle)
            {
                // 전투 진행 상태 업데이트
            }
        }
        
        /// <summary>
        /// 전투 데이터 저장
        /// </summary>
        public void SaveBattleData()
        {
            PlayerPrefs.SetInt("CollectedCharacterCount", collectedCharacters.Count);
            PlayerPrefs.SetString("Squad1Name", squad1.squadName);
            PlayerPrefs.SetString("Squad2Name", squad2.squadName);
            Debug.Log("전투 데이터 저장 완료");
        }
        
        /// <summary>
        /// 전투 데이터 로드
        /// </summary>
        public void LoadBattleData()
        {
            int characterCount = PlayerPrefs.GetInt("CollectedCharacterCount", 0);
            squad1.squadName = PlayerPrefs.GetString("Squad1Name", "정예부대");
            squad2.squadName = PlayerPrefs.GetString("Squad2Name", "선봉부대");
            Debug.Log("전투 데이터 로드 완료");
        }
        
        /// <summary>
        /// 전투 데이터 리셋
        /// </summary>
        public void ResetBattleData()
        {
            collectedCharacters.Clear();
            squad1.assignedUnits.Clear();
            squad2.assignedUnits.Clear();
            isInBattle = false;
            Debug.Log("전투 데이터 리셋 완료");
        }
    }
    
    /// <summary>
    /// 길드 부대 (9명으로 구성)
    /// </summary>
    [System.Serializable]
    public class GuildSquad
    {
        public string squadName;
        public int squadIndex;
        public Unit[,] formation = new Unit[GuildBattleCore.SQUAD_GRID_WIDTH, GuildBattleCore.SQUAD_GRID_HEIGHT];
        public Dictionary<Vector2Int, Unit> assignedUnits = new Dictionary<Vector2Int, Unit>();
        
        public void Initialize(string name, int index)
        {
            squadName = name;
            squadIndex = index;
            formation = new Unit[GuildBattleCore.SQUAD_GRID_WIDTH, GuildBattleCore.SQUAD_GRID_HEIGHT];
            assignedUnits.Clear();
        }
        
        public bool AssignUnit(Unit unit, Vector2Int gridPos)
        {
            if (assignedUnits.Count >= GuildBattleCore.UNITS_PER_SQUAD)
            {
                Debug.LogWarning($"부대가 이미 가득참! (최대 {GuildBattleCore.UNITS_PER_SQUAD}명)");
                return false;
            }
            
            if (gridPos.x >= 0 && gridPos.x < GuildBattleCore.SQUAD_GRID_WIDTH && 
                gridPos.y >= 0 && gridPos.y < GuildBattleCore.SQUAD_GRID_HEIGHT)
            {
                if (formation[gridPos.x, gridPos.y] == null)
                {
                    formation[gridPos.x, gridPos.y] = unit;
                    assignedUnits[gridPos] = unit;
                    unit.SetPosition(squadIndex, gridPos.x, gridPos.y);
                    return true;
                }
            }
            
            return false;
        }
        
        public List<Unit> GetAliveUnits()
        {
            return assignedUnits.Values.Where(u => u != null && u.IsAlive).ToList();
        }
        
        public bool IsDefeated()
        {
            return GetAliveUnits().Count == 0;
        }
        
        public int GetUnitCount()
        {
            return assignedUnits.Count;
        }
    }
    
    /// <summary>
    /// 길드 편성 (2부대)
    /// </summary>
    [System.Serializable]
    public class GuildFormation
    {
        public string guildName;
        public GuildSquad[] squads = new GuildSquad[GuildBattleCore.SQUADS_PER_GUILD];
        
        public void SetSquad(int index, GuildSquad squad)
        {
            if (index >= 0 && index < squads.Length)
            {
                squads[index] = squad;
            }
        }
        
        public GuildSquad GetSquad(int index)
        {
            return (index >= 0 && index < squads.Length) ? squads[index] : null;
        }
        
        public bool IsDefeated()
        {
            return squads.All(squad => squad == null || squad.IsDefeated());
        }
        
        public int GetAliveUnitCount()
        {
            return squads.Where(s => s != null).Sum(s => s.GetAliveUnits().Count);
        }
    }
    
    /// <summary>
    /// 길드 전투 결과
    /// </summary>
    [System.Serializable]
    public class GuildBattleResult
    {
        public bool isVictory;
        public int survivingUnits;
        public int totalTurns;
        public BattleRewards rewards;
        public List<Unit> mvpUnits = new List<Unit>();
    }
} 