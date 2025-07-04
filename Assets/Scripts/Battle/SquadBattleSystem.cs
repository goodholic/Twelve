using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Core;
using GuildMaster.Systems;
using GuildMaster.Data;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 부대 기반 전투 시스템
    /// </summary>
    public class SquadBattleSystem : MonoBehaviour
    {
        private static SquadBattleSystem instance;
        public static SquadBattleSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SquadBattleSystem>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SquadBattleSystem");
                        instance = go.AddComponent<SquadBattleSystem>();
                    }
                }
                return instance;
            }
        }
        
        // 상수 정의 (README 기준: 각 부대당 9명씩, 총 18명 vs 18명)
        public const int SQUADS_PER_GUILD = 2;
        public const int UNITS_PER_SQUAD = 9;    // README: 각 부대당 9명
        public const int SQUAD_WIDTH = 6;
        public const int SQUAD_HEIGHT = 3;
        public const int TOTAL_UNITS_PER_SIDE = 18;  // 2부대 x 9명 = 18명
        
        // 이벤트
        public event Action<SquadFormation, SquadFormation> OnBattleStart;
        public event Action<BattleResult> OnBattleEnd;
        public event Action<int, bool> OnSquadTurnStart; // squadIndex, isPlayer
        public event Action<Unit, float> OnUnitDamaged;
        public event Action<Unit> OnUnitDefeated;
        
        [Header("전투 설정")]
        public float turnDuration = 5f;
        public float battleSpeed = 1f;
        
        private SquadFormation playerFormation;
        private SquadFormation enemyFormation;
        private bool isBattleActive = false;
        private int currentTurn = 0;
        private int currentSquadIndex = 0;
        private bool isPlayerTurn = true;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
        
        /// <summary>
        /// 전투 시작
        /// </summary>
        public void StartBattle(SquadFormation playerFormation, SquadFormation enemyFormation)
        {
            this.playerFormation = playerFormation;
            this.enemyFormation = enemyFormation;
            
            isBattleActive = true;
            currentTurn = 0;
            currentSquadIndex = 0;
            isPlayerTurn = true;
            
            // 전투 시작 이벤트 발생
            OnBattleStart?.Invoke(playerFormation, enemyFormation);
            
            // 전투 루프 시작
            StartCoroutine(BattleLoop());
        }
        
        /// <summary>
        /// 전투 루프
        /// </summary>
        private IEnumerator BattleLoop()
        {
            while (isBattleActive)
            {
                // 현재 턴의 부대 선택
                SquadFormation currentFormation = isPlayerTurn ? playerFormation : enemyFormation;
                Squad currentSquad = currentFormation.GetSquad(currentSquadIndex);
                
                if (currentSquad != null && !currentSquad.IsDefeated)
                {
                    // 부대 턴 시작 이벤트
                    OnSquadTurnStart?.Invoke(currentSquadIndex, isPlayerTurn);
                    
                    // 부대 행동 실행
                    yield return StartCoroutine(ExecuteSquadTurn(currentSquad));
                }
                
                // 다음 턴으로 이동
                NextTurn();
                
                // 승부 판정
                if (CheckBattleEnd())
                {
                    EndBattle();
                    break;
                }
                
                yield return new WaitForSeconds(turnDuration / battleSpeed);
            }
        }
        
        /// <summary>
        /// 부대 턴 실행
        /// </summary>
        private IEnumerator ExecuteSquadTurn(Squad squad)
        {
            var aliveUnits = squad.GetAliveUnits();
            
            foreach (var unit in aliveUnits)
            {
                if (unit.IsAlive)
                {
                    // 유닛 행동 (간단한 구현)
                    yield return StartCoroutine(ExecuteUnitAction(unit));
                }
            }
        }
        
        /// <summary>
        /// 유닛 행동 실행
        /// </summary>
        private IEnumerator ExecuteUnitAction(Unit unit)
        {
            // 간단한 공격 로직
            SquadFormation targetFormation = unit.IsPlayerUnit ? enemyFormation : playerFormation;
            Unit target = FindRandomTarget(targetFormation);
            
            if (target != null)
            {
                float damage = unit.Attack;
                target.TakeDamage(damage);
                
                // 데미지 이벤트 발생
                OnUnitDamaged?.Invoke(target, damage);
                
                // 유닛 사망 체크
                if (!target.IsAlive)
                {
                    OnUnitDefeated?.Invoke(target);
                }
            }
            
            yield return new WaitForSeconds(0.5f / battleSpeed);
        }
        
        /// <summary>
        /// 랜덤 타겟 찾기
        /// </summary>
        private Unit FindRandomTarget(SquadFormation formation)
        {
            var allAliveUnits = new System.Collections.Generic.List<Unit>();
            
            foreach (var squad in formation.Squads)
            {
                allAliveUnits.AddRange(squad.GetAliveUnits());
            }
            
            if (allAliveUnits.Count > 0)
            {
                return allAliveUnits[UnityEngine.Random.Range(0, allAliveUnits.Count)];
            }
            
            return null;
        }
        
        /// <summary>
        /// 다음 턴으로 이동
        /// </summary>
        private void NextTurn()
        {
            currentSquadIndex++;
            
            if (currentSquadIndex >= SQUADS_PER_GUILD)
            {
                currentSquadIndex = 0;
                isPlayerTurn = !isPlayerTurn;
                
                if (isPlayerTurn)
                {
                    currentTurn++;
                }
            }
        }
        
        /// <summary>
        /// 전투 종료 체크
        /// </summary>
        private bool CheckBattleEnd()
        {
            return playerFormation.IsDefeated() || enemyFormation.IsDefeated();
        }
        
        /// <summary>
        /// 전투 종료
        /// </summary>
        private void EndBattle()
        {
            isBattleActive = false;
            
            BattleResult result = new BattleResult
            {
                IsVictory = !playerFormation.IsDefeated(),
                TotalTurns = currentTurn,
                RemainingUnits = playerFormation.GetTotalAliveUnits(),
                EnemiesDefeated = enemyFormation.Squads.Count * UNITS_PER_SQUAD - enemyFormation.GetTotalAliveUnits(),
                BattleDuration = Time.time
            };
            
            OnBattleEnd?.Invoke(result);
        }
        
        /// <summary>
        /// 전투 속도 설정
        /// </summary>
        public void SetBattleSpeed(float speed)
        {
            battleSpeed = Mathf.Clamp(speed, 0.1f, 10f);
        }
    }
    
    /// <summary>
    /// 부대 진형 (6x3 그리드)
    /// </summary>
    public class SquadFormation
    {
        public int squadId;
        public string squadName;
        public bool isPlayerSquad;
        public Unit[,] units; // 6x3 그리드
        public SquadRole squadRole;
        
        // Squad 리스트 관리
        public List<Squad> Squads { get; set; } = new List<Squad>();
        
        public SquadFormation()
        {
            units = new Unit[3, 6]; // 3x6 그리드
        }
        
        public void PrepareBattle()
        {
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    unit.ResetBattleState();
                }
            }
            
            foreach (var squad in Squads)
            {
                foreach (var unit in squad.GetAliveUnits())
                {
                    unit.ResetBattleState();
                }
            }
        }
        
        public List<Unit> GetAliveUnits()
        {
            var aliveUnits = new List<Unit>();
            foreach (var unit in units)
            {
                if (unit != null && unit.IsAlive)
                {
                    aliveUnits.Add(unit);
                }
            }
            return aliveUnits;
        }
        
        public List<Unit> GetDamagedUnits()
        {
            return GetAliveUnits().Where(u => u.currentHP < u.maxHP).ToList();
        }
        
        public bool HasAliveUnits()
        {
            return GetAliveUnits().Count > 0;
        }
        
        public bool ContainsUnit(Unit unit)
        {
            foreach (var u in units)
            {
                if (u == unit) return true;
            }
            return false;
        }
        
        public bool IsDefeated()
        {
            return !HasAliveUnits() && Squads.All(s => s.IsDefeated);
        }
        
        public int GetTotalAliveUnits()
        {
            int total = GetAliveUnits().Count;
            foreach (var squad in Squads)
            {
                total += squad.GetAliveUnits().Count;
            }
            return total;
        }
        
        public Squad GetSquad(int index)
        {
            if (index >= 0 && index < Squads.Count)
            {
                return Squads[index];
            }
            return null;
        }
        
        public void AddSquad(Squad squad)
        {
            if (squad != null)
            {
                Squads.Add(squad);
            }
        }
    }
    
    // SquadRole은 Squad.cs에 이미 정의되어 있음
    
    /// <summary>
    /// 전투 결과 타입
    /// </summary>
    public enum BattleResultType
    {
        Victory,
        Defeat,
        Draw,
        Timeout
    }
    
    /// <summary>
    /// 전투 결과
    /// </summary>
    [System.Serializable]
    public class BattleResult
    {
        public bool IsVictory { get; set; }
        public int TotalTurns { get; set; }
        public int RemainingUnits { get; set; }
        public int EnemiesDefeated { get; set; }
        public float BattleDuration { get; set; }
        public BattleResultType ResultType { get; set; }
        public BattleStatistics Statistics { get; set; }
        public BattleRewards Rewards { get; set; }
        public Unit MvpUnit { get; set; }
    }
    
    /// <summary>
    /// 전투 통계
    /// </summary>
    [System.Serializable]
    public class BattleStatistics
    {
        public float StartTime;
        public float EndTime;
        public int TotalTurns;
        public BattleResultType ResultType;
        
        public float PlayerDamageDealt;
        public float EnemyDamageDealt;
        public int PlayerUnitsLost;
        public int EnemyUnitsLost;
        public int SkillsUsed;
        public int CriticalHits;
        
        public float GetBattleDuration() => EndTime - StartTime;
    }
    
    /// <summary>
    /// 전투 보상
    /// </summary>
    [System.Serializable]
    public class BattleRewards
    {
        public int gold;
        public float exp;
        public int reputation;
        public List<string> items = new List<string>();
    }
}