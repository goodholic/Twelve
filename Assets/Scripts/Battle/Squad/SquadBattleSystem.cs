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
    /// 2부대 vs 2부대 정예 전투 시스템
    /// 각 부대는 6x3 그리드에 18명의 모험가 배치 (총 36명 vs 36명)
    /// </summary>
    public class SquadBattleSystem : MonoBehaviour
    {
        private static SquadBattleSystem _instance;
        public static SquadBattleSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SquadBattleSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SquadBattleSystem");
                        _instance = go.AddComponent<SquadBattleSystem>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("전투 설정")]
        [SerializeField] private float baseActionDelay = 1f;
        [SerializeField] private int maxTurns = 100;
        [SerializeField] private float squadTurnDelay = 2f;
        
        [Header("부대 구성")]
        public const int SQUADS_PER_GUILD = 2;  // 2부대로 변경
        public const int UNITS_PER_SQUAD = 18;
        public const int SQUAD_WIDTH = 6;      // 6x3 그리드로 변경
        public const int SQUAD_HEIGHT = 3;
        
        // 플레이어 부대와 적 부대
        private List<SquadFormation> playerSquads;
        private List<SquadFormation> enemySquads;
        
        // 전투 상태
        private bool isInBattle = false;
        private int currentTurn = 0;
        private int currentSquadIndex = 0;
        private bool isPlayerTurn = true;
        private Coroutine battleCoroutine;
        
        // 전투 통계
        private BattleStatistics battleStats;
        
        // 이벤트
        public event Action<SquadFormation, SquadFormation> OnBattleStart;
        public event Action<BattleResult> OnBattleEnd;
        public event Action<int, bool> OnSquadTurnStart; // squadIndex, isPlayer
        public event Action<int, bool> OnSquadTurnEnd;
        public event Action<Unit, Unit, float> OnUnitAttack;
        public event Action<Unit, float> OnUnitDamaged;
        public event Action<Unit> OnUnitDefeated;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        /// <summary>
        /// 정예 전투 시작 (2부대 vs 2부대)
        /// </summary>
        public void StartSquadBattle(List<Squad> playerSquadList, List<Squad> enemySquadList)
        {
            if (isInBattle)
            {
                Debug.LogWarning("이미 전투 중입니다!");
                return;
            }
            
            // 부대를 SquadFormation으로 변환
            playerSquads = ConvertToSquadFormations(playerSquadList, true);
            enemySquads = ConvertToSquadFormations(enemySquadList, false);
            
            // 전투 초기화
            InitializeBattle();
            
            // 전투 시작
            battleCoroutine = StartCoroutine(SquadBattleLoop());
        }
        
        /// <summary>
        /// Squad를 SquadFormation으로 변환 (6x3 그리드 배치)
        /// </summary>
        List<SquadFormation> ConvertToSquadFormations(List<Squad> squads, bool isPlayer)
        {
            var formations = new List<SquadFormation>();
            
            for (int i = 0; i < SQUADS_PER_GUILD && i < squads.Count; i++)
            {
                var formation = new SquadFormation
                {
                    squadId = i,
                    squadName = squads[i].Name,
                    isPlayerSquad = isPlayer,
                    units = new Unit[SQUAD_WIDTH, SQUAD_HEIGHT],
                    squadRole = (SquadRole)i // 0: 선봉, 1: 주력, 2: 지원, 3: 후방
                };
                
                // 유닛을 6x3 그리드에 배치
                var unitList = squads[i].GetAllUnits();
                int unitIndex = 0;
                
                // 기본 배치: 탱커는 앞줄, 딜러는 중간, 힐러/서포터는 뒷줄
                for (int y = 0; y < SQUAD_HEIGHT && unitIndex < unitList.Count; y++)
                {
                    for (int x = 0; x < SQUAD_WIDTH && unitIndex < unitList.Count; x++)
                    {
                        if (unitIndex < unitList.Count)
                        {
                            var unit = unitList[unitIndex];
                            formation.units[x, y] = unit;
                            unit.squadPosition = new Vector2Int(x, y);
                            unitIndex++;
                        }
                    }
                }
                
                formations.Add(formation);
            }
            
            return formations;
        }
        
        void InitializeBattle()
        {
            isInBattle = true;
            currentTurn = 0;
            currentSquadIndex = 0;
            isPlayerTurn = true;
            
            // 전투 통계 초기화
            battleStats = new BattleStatistics();
            battleStats.StartTime = Time.time;
            
            // 모든 유닛 전투 준비
            foreach (var squad in playerSquads)
            {
                squad.PrepareBattle();
            }
            
            foreach (var squad in enemySquads)
            {
                squad.PrepareBattle();
            }
            
            // 전투 시작 이벤트
            if (playerSquads.Count > 0 && enemySquads.Count > 0)
            {
                OnBattleStart?.Invoke(playerSquads[0], enemySquads[0]);
            }
            
            // 전투 시작 효과
            ParticleEffectsSystem.Instance?.PlayEffect("battle_start", Vector3.zero);
            SoundSystem.Instance?.OnBattleStart(false);
        }
        
        /// <summary>
        /// 부대 전투 메인 루프
        /// </summary>
        IEnumerator SquadBattleLoop()
        {
            while (isInBattle && currentTurn < maxTurns)
            {
                // 모든 부대가 한 번씩 행동
                for (int squadRound = 0; squadRound < SQUADS_PER_GUILD * 2; squadRound++)
                {
                    // 플레이어와 적이 번갈아가며 행동
                    isPlayerTurn = (squadRound % 2 == 0);
                    currentSquadIndex = squadRound / 2;
                    
                    var actingSquad = isPlayerTurn ? 
                        GetSquad(playerSquads, currentSquadIndex) : 
                        GetSquad(enemySquads, currentSquadIndex);
                    
                    if (actingSquad != null && actingSquad.HasAliveUnits())
                    {
                        // 부대 턴 시작
                        OnSquadTurnStart?.Invoke(currentSquadIndex, isPlayerTurn);
                        
                        // 부대 행동
                        yield return ProcessSquadTurn(actingSquad);
                        
                        // 부대 턴 종료
                        OnSquadTurnEnd?.Invoke(currentSquadIndex, isPlayerTurn);
                        
                        // 부대 간 딜레이
                        yield return new WaitForSeconds(squadTurnDelay / (int)GameSpeedSystem.Instance.GetCurrentSpeed());
                    }
                    
                    // 승리 조건 체크
                    if (CheckBattleEnd())
                    {
                        yield break;
                    }
                }
                
                currentTurn++;
            }
            
            // 시간 초과로 전투 종료
            if (currentTurn >= maxTurns)
            {
                EndBattle(BattleResultType.Draw);
            }
        }
        
        /// <summary>
        /// 부대 턴 처리
        /// </summary>
        IEnumerator ProcessSquadTurn(SquadFormation squad)
        {
            // 부대 내 모든 살아있는 유닛이 행동
            var aliveUnits = squad.GetAliveUnits();
            
            // 속도 순으로 정렬
            aliveUnits = aliveUnits.OrderByDescending(u => u.speed).ToList();
            
            foreach (var unit in aliveUnits)
            {
                if (!unit.IsAlive) continue;
                
                // 유닛 행동
                yield return ProcessUnitAction(unit, squad);
                
                // 행동 간 딜레이
                yield return new WaitForSeconds(baseActionDelay / (int)GameSpeedSystem.Instance.GetCurrentSpeed());
            }
        }
        
        /// <summary>
        /// 유닛 개별 행동 처리
        /// </summary>
        IEnumerator ProcessUnitAction(Unit unit, SquadFormation squad)
        {
            // 스킬 사용 가능 체크
            var availableSkills = unit.GetAvailableSkills();
            Skill skillToUse = null;
            
            // AI 스킬 선택 (우선순위: 궁극기 > 특수 스킬 > 기본 공격)
            if (availableSkills.Count > 0)
            {
                skillToUse = SelectBestSkill(unit, availableSkills, squad);
            }
            
            if (skillToUse != null)
            {
                // 타겟 선택
                var targets = GetTargets(unit, skillToUse, squad);
                
                if (targets.Count > 0)
                {
                    // 스킬 사용
                    yield return UseSkill(unit, skillToUse, targets);
                }
            }
            else
            {
                // 기본 공격
                var target = GetBasicAttackTarget(unit, squad);
                if (target != null)
                {
                    yield return PerformBasicAttack(unit, target);
                }
            }
        }
        
        /// <summary>
        /// 최적의 스킬 선택
        /// </summary>
        Skill SelectBestSkill(Unit caster, List<Skill> skills, SquadFormation squad)
        {
            // 간단한 AI: MP가 충분하면 가장 강력한 스킬 사용
            foreach (var skill in skills.OrderByDescending(s => s.GetManaCost()))
            {
                if (caster.currentMP >= skill.GetManaCost())
                {
                    // 스킬 타입에 따라 사용 여부 결정
                    var skillData = skill.GetSkillData();
                    if (skillData == null) continue;
                    
                    // 스킬 타입별 특수 처리
                    switch (skillData.skillType)
                    {
                        case GuildMaster.Data.SkillType.Heal:
                            // 아군에게 치유가 필요한 경우만
                            if (squad.GetDamagedUnits().Count > 0)
                                return skill;
                            break;
                            
                        case GuildMaster.Data.SkillType.Attack:
                        case GuildMaster.Data.SkillType.Debuff:
                            // 적이 있을 때만
                            if (GetEnemySquads(squad).Any(s => s.HasAliveUnits()))
                                return skill;
                            break;
                            
                        case GuildMaster.Data.SkillType.Buff:
                            // 버프가 필요한 아군이 있을 때
                            return skill;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 스킬 타겟 선택
        /// </summary>
        List<Unit> GetTargets(Unit caster, Skill skill, SquadFormation casterSquad)
        {
            var targets = new List<Unit>();
            var skillData = skill.GetSkillData();
            if (skillData == null) return targets;
            
            switch (skillData.targetType)
            {
                case TargetType.Self:
                    targets.Add(caster);
                    break;
                    
                case TargetType.Ally:
                    // 가장 체력이 낮은 아군
                    var damaged = casterSquad.GetDamagedUnits();
                    if (damaged.Count > 0)
                        targets.Add(damaged.OrderBy(u => u.currentHP / u.maxHP).First());
                    break;
                    
                case TargetType.Enemy:
                    // 가장 가까운 적
                    var enemyTarget = GetNearestEnemy(caster, casterSquad);
                    if (enemyTarget != null)
                        targets.Add(enemyTarget);
                    break;
                    
                case TargetType.AllAllies:
                    targets.AddRange(casterSquad.GetAliveUnits());
                    break;
                    
                case TargetType.AllEnemies:
                    var enemySquads = GetEnemySquads(casterSquad);
                    foreach (var enemySquad in enemySquads)
                    {
                        targets.AddRange(enemySquad.GetAliveUnits());
                    }
                    break;
                    
                case TargetType.Area:
                    // 범위 공격: 중심 타겟 주변의 적들
                    var centerTarget = GetNearestEnemy(caster, casterSquad);
                    if (centerTarget != null)
                    {
                        targets.Add(centerTarget);
                        targets.AddRange(GetNearbyUnits(centerTarget, Mathf.RoundToInt(skillData.areaOfEffect)));
                    }
                    break;
            }
            
            return targets;
        }
        
        /// <summary>
        /// 스킬 사용
        /// </summary>
        IEnumerator UseSkill(Unit caster, Skill skill, List<Unit> targets)
        {
            // 스킬 시작 효과
            var skillData = skill.GetSkillData();
            if (skillData != null)
            {
                ParticleEffectsSystem.Instance?.PlayEffectOnTarget("skill_cast", caster.transform);
                SoundSystem.Instance?.PlaySound($"skill_{skillData.skillType.ToString().ToLower()}");
            }
            
            // MP 소모
            caster.currentMP -= skill.GetManaCost();
            
            // 각 타겟에 효과 적용
            foreach (var target in targets)
            {
                yield return ApplySkillEffect(caster, skill, target);
            }
            
            // 쿨다운 설정
            skill.StartCooldown();
        }
        
        /// <summary>
        /// 스킬 효과 적용
        /// </summary>
        IEnumerator ApplySkillEffect(Unit caster, Skill skill, Unit target)
        {
            var skillData = skill.GetSkillData();
            if (skillData == null) yield break;
            
            switch (skillData.skillType)
            {
                case GuildMaster.Data.SkillType.Attack:
                    float damage = caster.attack * skillData.damageMultiplier;
                    ApplyDamage(target, damage, caster);
                    break;
                    
                case GuildMaster.Data.SkillType.Heal:
                    float healAmount = skillData.healAmount + (caster.magicPower * 0.5f);
                    target.Heal(healAmount);
                    ParticleEffectsSystem.Instance?.PlayEffectOnTarget("skill_heal", target.transform);
                    break;
                    
                case GuildMaster.Data.SkillType.Buff:
                    // 버프 적용
                    ApplyBuff(target, skillData.buffType, skillData.buffAmount, Mathf.RoundToInt(skillData.buffDuration));
                    break;
                    
                case GuildMaster.Data.SkillType.Debuff:
                    // 디버프 적용
                    ApplyDebuff(target, skillData.buffType, skillData.buffAmount, Mathf.RoundToInt(skillData.buffDuration));
                    break;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        /// <summary>
        /// 기본 공격
        /// </summary>
        IEnumerator PerformBasicAttack(Unit attacker, Unit target)
        {
            // 공격 애니메이션 (추후 구현)
            float damage = CalculateDamage(attacker, target);
            
            // 공격 이벤트
            OnUnitAttack?.Invoke(attacker, target, damage);
            
            // 효과음
            SoundSystem.Instance?.PlaySound("sword_swing");
            
            // 데미지 적용
            ApplyDamage(target, damage, attacker);
            
            // 데미지 텍스트
            ParticleEffectsSystem.Instance?.ShowDamageText(
                target.transform.position + Vector3.up, 
                (int)damage, 
                attacker.GetCriticalHit() ? ParticleEffectsSystem.DamageType.Critical : ParticleEffectsSystem.DamageType.Normal
            );
            
            yield return new WaitForSeconds(0.3f);
        }
        
        /// <summary>
        /// 데미지 계산
        /// </summary>
        float CalculateDamage(Unit attacker, Unit defender)
        {
            float baseDamage = attacker.attack;
            
            // 크리티컬 체크
            bool isCritical = UnityEngine.Random.value < attacker.criticalRate;
            if (isCritical)
            {
                baseDamage *= attacker.criticalDamage;
            }
            
            // 방어력 적용
            float defense = defender.defense;
            float finalDamage = baseDamage * (100f / (100f + defense));
            
            // 최소 데미지 보장
            finalDamage = Mathf.Max(1, finalDamage);
            
            // 명중률 체크
            if (UnityEngine.Random.value > attacker.accuracy - defender.evasion)
            {
                // 회피
                ParticleEffectsSystem.Instance?.PlayEffectOnTarget("dodge_effect", defender.transform);
                return 0;
            }
            
            return finalDamage;
        }
        
        /// <summary>
        /// 데미지 적용
        /// </summary>
        void ApplyDamage(Unit target, float damage, Unit attacker)
        {
            target.TakeDamage(damage);
            
            // 데미지 이벤트
            OnUnitDamaged?.Invoke(target, damage);
            
            // 피격 효과
            ParticleEffectsSystem.Instance?.PlayEffectOnTarget("hit_physical", target.transform);
            
            // 전투 통계 업데이트
            if (attacker != null)
            {
                var attackerSquad = GetUnitSquad(attacker);
                if (attackerSquad != null)
                {
                    if (attackerSquad.isPlayerSquad)
                        battleStats.PlayerDamageDealt += damage;
                    else
                        battleStats.EnemyDamageDealt += damage;
                }
            }
            
            // 사망 체크
            if (!target.IsAlive)
            {
                OnUnitDefeated?.Invoke(target);
                HandleUnitDefeat(target);
            }
        }
        
        /// <summary>
        /// 유닛 사망 처리
        /// </summary>
        void HandleUnitDefeat(Unit defeatedUnit)
        {
            var squad = GetUnitSquad(defeatedUnit);
            if (squad != null)
            {
                if (squad.isPlayerSquad)
                    battleStats.PlayerUnitsLost++;
                else
                    battleStats.EnemyUnitsLost++;
            }
            
            // 사망 효과
            ParticleEffectsSystem.Instance?.PlayEffectOnTarget("unit_defeat", defeatedUnit.transform);
        }
        
        /// <summary>
        /// 버프 적용
        /// </summary>
        void ApplyBuff(Unit target, BuffType buffType, float amount, int duration)
        {
            // TODO: 버프 시스템 구현
            ParticleEffectsSystem.Instance?.PlayEffectOnTarget("skill_buff", target.transform);
        }
        
        /// <summary>
        /// 디버프 적용
        /// </summary>
        void ApplyDebuff(Unit target, BuffType buffType, float amount, int duration)
        {
            // TODO: 디버프 시스템 구현
            ParticleEffectsSystem.Instance?.PlayEffectOnTarget("skill_debuff", target.transform);
        }
        
        /// <summary>
        /// 가장 가까운 적 찾기
        /// </summary>
        Unit GetNearestEnemy(Unit unit, SquadFormation unitSquad)
        {
            Unit nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            
            var enemySquads = GetEnemySquads(unitSquad);
            
            foreach (var enemySquad in enemySquads)
            {
                foreach (var enemy in enemySquad.GetAliveUnits())
                {
                    float distance = GetUnitDistance(unit, enemy, unitSquad, enemySquad);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemy;
                    }
                }
            }
            
            return nearestEnemy;
        }
        
        /// <summary>
        /// 기본 공격 타겟 선택
        /// </summary>
        Unit GetBasicAttackTarget(Unit attacker, SquadFormation attackerSquad)
        {
            // 우선순위: 전방 적 > 가장 가까운 적 > 체력이 낮은 적
            var enemySquads = GetEnemySquads(attackerSquad);
            
            // 선봉 부대 우선 공격
            var frontSquad = enemySquads.FirstOrDefault(s => s.squadRole == SquadRole.Vanguard && s.HasAliveUnits());
            if (frontSquad != null)
            {
                var frontUnits = frontSquad.GetAliveUnits();
                if (frontUnits.Count > 0)
                {
                    // 같은 열의 적 우선
                    var sameColumn = frontUnits.FirstOrDefault(u => u.squadPosition.x == attacker.squadPosition.x);
                    if (sameColumn != null) return sameColumn;
                    
                    // 가장 가까운 적
                    return frontUnits.OrderBy(u => Mathf.Abs(u.squadPosition.x - attacker.squadPosition.x)).First();
                }
            }
            
            // 가장 가까운 적
            return GetNearestEnemy(attacker, attackerSquad);
        }
        
        /// <summary>
        /// 유닛 간 거리 계산
        /// </summary>
        float GetUnitDistance(Unit unit1, Unit unit2, SquadFormation squad1, SquadFormation squad2)
        {
            // 부대 간 거리 + 부대 내 위치 차이
            float squadDistance = Mathf.Abs(squad1.squadId - squad2.squadId) * 10f;
            float positionDistance = Vector2Int.Distance(unit1.squadPosition, unit2.squadPosition);
            
            return squadDistance + positionDistance;
        }
        
        /// <summary>
        /// 주변 유닛 찾기
        /// </summary>
        List<Unit> GetNearbyUnits(Unit centerUnit, int range)
        {
            var nearbyUnits = new List<Unit>();
            var centerSquad = GetUnitSquad(centerUnit);
            if (centerSquad == null) return nearbyUnits;
            
            // 같은 부대 내에서만 범위 체크
            foreach (var unit in centerSquad.GetAliveUnits())
            {
                if (unit != centerUnit)
                {
                    int distance = Mathf.Abs(unit.squadPosition.x - centerUnit.squadPosition.x) + 
                                  Mathf.Abs(unit.squadPosition.y - centerUnit.squadPosition.y);
                    
                    if (distance <= range)
                    {
                        nearbyUnits.Add(unit);
                    }
                }
            }
            
            return nearbyUnits;
        }
        
        /// <summary>
        /// 유닛이 속한 부대 찾기
        /// </summary>
        SquadFormation GetUnitSquad(Unit unit)
        {
            foreach (var squad in playerSquads)
            {
                if (squad.ContainsUnit(unit))
                    return squad;
            }
            
            foreach (var squad in enemySquads)
            {
                if (squad.ContainsUnit(unit))
                    return squad;
            }
            
            return null;
        }
        
        /// <summary>
        /// 적 부대 목록 가져오기
        /// </summary>
        List<SquadFormation> GetEnemySquads(SquadFormation squad)
        {
            return squad.isPlayerSquad ? enemySquads : playerSquads;
        }
        
        /// <summary>
        /// 부대 가져오기
        /// </summary>
        SquadFormation GetSquad(List<SquadFormation> squads, int index)
        {
            if (index >= 0 && index < squads.Count)
                return squads[index];
            return null;
        }
        
        /// <summary>
        /// 전투 종료 체크
        /// </summary>
        bool CheckBattleEnd()
        {
            bool playerHasAlive = playerSquads.Any(s => s.HasAliveUnits());
            bool enemyHasAlive = enemySquads.Any(s => s.HasAliveUnits());
            
            if (!playerHasAlive && !enemyHasAlive)
            {
                EndBattle(BattleResultType.Draw);
                return true;
            }
            else if (!enemyHasAlive)
            {
                EndBattle(BattleResultType.Victory);
                return true;
            }
            else if (!playerHasAlive)
            {
                EndBattle(BattleResultType.Defeat);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 전투 종료
        /// </summary>
        void EndBattle(BattleResultType resultType)
        {
            if (!isInBattle) return;
            
            isInBattle = false;
            
            if (battleCoroutine != null)
            {
                StopCoroutine(battleCoroutine);
                battleCoroutine = null;
            }
            
            // 전투 통계 완료
            battleStats.EndTime = Time.time;
            battleStats.TotalTurns = currentTurn;
            battleStats.ResultType = resultType;
            
            // 보상 계산
            var rewards = CalculateRewards(resultType);
            
            // 전투 결과
            var result = new BattleResult
            {
                resultType = resultType,
                statistics = battleStats,
                rewards = rewards,
                mvpUnit = GetMVPUnit()
            };
            
            // 전투 종료 이벤트
            OnBattleEnd?.Invoke(result);
            
            // 사운드
            SoundSystem.Instance?.OnBattleEnd(resultType == BattleResultType.Victory);
            
            // 전투 결과 저장
            SaveBattleResult(result);
        }
        
        /// <summary>
        /// 보상 계산
        /// </summary>
        BattleRewards CalculateRewards(BattleResultType resultType)
        {
            var rewards = new BattleRewards();
            
            if (resultType == BattleResultType.Victory)
            {
                // 기본 보상
                rewards.gold = 1000 + (currentTurn * 10);
                rewards.exp = 500 + (battleStats.EnemyUnitsLost * 50);
                rewards.reputation = 10;
                
                // 완벽한 승리 보너스
                if (battleStats.PlayerUnitsLost == 0)
                {
                    rewards.gold *= 2;
                    rewards.reputation *= 2;
                }
                
                // 빠른 승리 보너스
                if (currentTurn < 10)
                {
                    rewards.exp *= 1.5f;
                }
            }
            else if (resultType == BattleResultType.Defeat)
            {
                // 패배 시에도 약간의 보상
                rewards.exp = 100 + (battleStats.EnemyUnitsLost * 10);
            }
            
            return rewards;
        }
        
        /// <summary>
        /// MVP 유닛 선정
        /// </summary>
        Unit GetMVPUnit()
        {
            // TODO: 데미지, 킬, 힐링 등을 종합하여 MVP 선정
            return playerSquads.SelectMany(s => s.GetAliveUnits()).FirstOrDefault();
        }
        
        /// <summary>
        /// 전투 결과 저장
        /// </summary>
        void SaveBattleResult(BattleResult result)
        {
            // 전투 기록 저장
            PlayerPrefs.SetInt("TotalBattles", PlayerPrefs.GetInt("TotalBattles", 0) + 1);
            
            if (result.resultType == BattleResultType.Victory)
            {
                PlayerPrefs.SetInt("TotalVictories", PlayerPrefs.GetInt("TotalVictories", 0) + 1);
                
                // 연승 기록
                int winStreak = PlayerPrefs.GetInt("CurrentWinStreak", 0) + 1;
                PlayerPrefs.SetInt("CurrentWinStreak", winStreak);
                
                int bestWinStreak = PlayerPrefs.GetInt("BestWinStreak", 0);
                if (winStreak > bestWinStreak)
                {
                    PlayerPrefs.SetInt("BestWinStreak", winStreak);
                }
            }
            else
            {
                PlayerPrefs.SetInt("CurrentWinStreak", 0);
            }
            
            PlayerPrefs.Save();
            
            // 업적 체크
            AchievementSystem.Instance?.UpdateProgress("combat_first_win", 1);
            AchievementSystem.Instance?.UpdateProgress("combat_win_streak_10", PlayerPrefs.GetInt("CurrentWinStreak", 0), false);
            if (result.statistics.PlayerUnitsLost == 0 && result.resultType == BattleResultType.Victory)
            {
                AchievementSystem.Instance?.UpdateProgress("combat_perfect_victory", 1);
            }
        }
        
        /// <summary>
        /// 전투 중단
        /// </summary>
        public void StopBattle()
        {
            if (battleCoroutine != null)
            {
                StopCoroutine(battleCoroutine);
                battleCoroutine = null;
            }
            
            isInBattle = false;
        }
        
        public bool IsInBattle() => isInBattle;
        public int GetCurrentTurn() => currentTurn;
        public float GetBattleProgress() => (float)currentTurn / maxTurns;
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
        
        public void PrepareBattle()
        {
            foreach (var unit in units)
            {
                if (unit != null)
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
        
        // 호환성을 위한 속성
        public int UnitsLost => PlayerUnitsLost + EnemyUnitsLost;
        
        public float GetBattleDuration() => EndTime - StartTime;
    }
    
    /// <summary>
    /// 전투 결과
    /// </summary>
    [System.Serializable]
    public class BattleResult
    {
        public BattleResultType resultType;
        public BattleStatistics statistics;
        public BattleRewards rewards;
        [System.NonSerialized]
        public Unit mvpUnit;
        
        // 호환성을 위한 속성들
        public bool isVictory => resultType == BattleResultType.Victory;
        public int unitsLost => statistics?.UnitsLost ?? 0;
        public int totalTurns => statistics?.TotalTurns ?? 0;
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