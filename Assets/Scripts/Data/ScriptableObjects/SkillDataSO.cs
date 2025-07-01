using UnityEngine;
using System;
using System.Collections.Generic;
using GuildMaster.Battle; // Unit, JobClass를 위해 추가
using System.Linq;

namespace GuildMaster.Data
{
    /// <summary>
    /// 스킬 데이터 ScriptableObject
    /// CSV 데이터를 기반으로 생성되는 스킬 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "GuildMaster/Data/Skill", order = 2)]
    public class SkillDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string skillId;
        public string skillName;
        [TextArea(2, 4)]
        public string description;
        
        [Header("스킬 타입")]
        public SkillType skillType;
        public TargetType targetType;
        
        [Header("데미지 및 효과")]
        public float damageMultiplier = 1.0f;
        public float healAmount = 0f;
        public BuffType buffType;
        public float buffAmount = 0f;
        public float buffDuration = 0f;
        
        [Header("비용 및 쿨다운")]
        public int manaCost = 0;
        public float cooldown = 0f;
        
        [Header("범위")]
        public float range = 1f;
        public float areaOfEffect = 0f;
        
        [Header("요구사항")]
        public int requiredLevel = 1;
        public JobClass jobClass = JobClass.Warrior;
        
        [Header("비주얼")]
        public GameObject effectPrefab;
        public Sprite skillIcon;
        public string animationTrigger;
        public AudioClip castSound;
        public AudioClip hitSound;
        
        [Header("추가 속성")]
        public int damage;
        public int healing;
        public bool areaEffect;
        public DebuffType debuffType;
        public float duration;
        public string animationName;
        public string soundEffectName;
        public string particleEffectName;
        public string requirements;
        public float criticalMultiplier = 1.5f;
        public float statusChance = 1.0f;
        public int maxTargets = 1;
        public bool piercing = false;
        public float castTime = 0f;
        public bool canCrit = true;
        public bool ignoreDefense = false;
        public float lifesteal = 0f;
        
        public enum SkillType
        {
            Physical,
            Magical,
            Healing,
            Buff,
            Debuff,
            Utility,
            Defensive,
            Summon
        }
        
        public enum TargetType
        {
            Self,
            Single,
            AOE,
            Line,
            AllAllies,
            AllEnemies,
            Area,
            Enemy
        }
        
        public enum BuffType
        {
            None,
            Defense,
            HolyShield,
            Fortress,
            Rage,
            Berserk,
            Immunity,
            Accuracy,
            Blessing,
            DivineProtection,
            Sanctuary,
            Morale,
            Courage,
            Heroic,
            MagicBoost,
            Victory,
            Destiny,
            Speed,
            Regeneration,
            ManaRestore,
            Focus,
            Resistance,
            Alert,
            Counter,
            Survival,
            ElementalShield,
            ManaAbsorb,
            Chivalry,
            LastStand,
            Berserker,
            Unbreakable,
            MagicShield,
            BeastForm,
            EarthBlessing,
            ShadowWalk,
            BattleSong,
            Clone,
            Stealth
        }
        
        public enum DebuffType
        {
            None,
            Taunt,
            Stun,
            Knockdown,
            Poison,
            Freeze,
            Paralyze,
            Burn,
            Slow,
            Blind,
            Fear,
            Sleep,
            Debuff,
            Silence,
            Root,
            Entangle,
            DeadlyPoison,
            Confusion,
            Charm,
            TimeStop,
            Knockback,
            Net,
            Weakness,
            Pacify,
            Despair,
            ElementalDamage,
            Void,
            Mark,
            Multiple
        }

        /// <summary>
        /// CSV 데이터로부터 스킬 생성
        /// </summary>
        public void InitializeFromCSV(string csvLine)
        {
            string[] values = csvLine.Split(',');
            
            if (values.Length >= 19)
            {
                skillId = values[0];
                skillName = values[1];
                description = values[2];
                
                // SkillType 파싱
                if (Enum.TryParse<SkillType>(values[3], out SkillType parsedSkillType))
                    skillType = parsedSkillType;
                
                // TargetType 파싱
                if (Enum.TryParse<TargetType>(values[4], out TargetType parsedTargetType))
                    targetType = parsedTargetType;
                
                // 수치값들 파싱
                if (float.TryParse(values[5], out float parsedDamageMultiplier))
                    damageMultiplier = parsedDamageMultiplier;
                
                if (float.TryParse(values[6], out float parsedHealAmount))
                    healAmount = parsedHealAmount;
                
                if (int.TryParse(values[7], out int parsedManaCost))
                    manaCost = parsedManaCost;
                
                if (float.TryParse(values[8], out float parsedCooldown))
                    cooldown = parsedCooldown;
                
                if (float.TryParse(values[9], out float parsedRange))
                    range = parsedRange;
                
                if (float.TryParse(values[10], out float parsedAOE))
                    areaOfEffect = parsedAOE;
                
                if (int.TryParse(values[11], out int parsedRequiredLevel))
                    requiredLevel = parsedRequiredLevel;
                
                // JobClass 파싱 (문자열 매핑 필요)
                jobClass = ParseJobClassFromString(values[12]);
                
                // BuffType 파싱
                if (Enum.TryParse<BuffType>(values[13], out BuffType parsedBuffType))
                    buffType = parsedBuffType;
                
                if (float.TryParse(values[14], out float parsedBuffAmount))
                    buffAmount = parsedBuffAmount;
                
                if (float.TryParse(values[15], out float parsedBuffDuration))
                    buffDuration = parsedBuffDuration;
                
                // DebuffType 파싱
                if (Enum.TryParse<DebuffType>(values[16], out DebuffType parsedDebuffType))
                    debuffType = parsedDebuffType;
                
                if (float.TryParse(values[17], out float parsedDuration))
                    duration = parsedDuration;
                
                // 기타 정보
                requirements = values.Length > 18 ? values[18] : "";
            }
        }
        
        JobClass ParseJobClassFromString(string jobClassStr)
        {
            return jobClassStr.ToLower() switch
            {
                "warrior" => JobClass.Warrior,
                "mage" => JobClass.Mage,
                "archer" => JobClass.Archer,
                "priest" => JobClass.Priest,
                "rogue" => JobClass.Rogue,
                "paladin" => JobClass.Paladin,
                "berserker" => JobClass.Warrior,
                "necromancer" => JobClass.Mage,
                _ => JobClass.Warrior
            };
        }
        
        void ExtractLevelRequirement()
        {
            if (string.IsNullOrEmpty(requirements)) return;
            
            // "Level X" 형태에서 레벨 추출
            if (requirements.StartsWith("Level "))
            {
                string levelStr = requirements.Substring(6);
                if (int.TryParse(levelStr, out int level))
                {
                    requiredLevel = level;
                }
            }
        }
        
        public bool CanUseOnTarget(Unit caster, Unit target)
        {
            if (caster == null) return false;
            
            switch (targetType)
            {
                case TargetType.Self:
                    return target == caster;
                case TargetType.Single:
                case TargetType.Enemy:
                    return target != null && target != caster;
                case TargetType.AllAllies:
                case TargetType.AllEnemies:
                case TargetType.AOE:
                case TargetType.Area:
                case TargetType.Line:
                    return true;
                default:
                    return false;
            }
        }
        
        public bool CanUseSkill(Unit caster)
        {
            if (jobClass != JobClass.Warrior && caster.jobClass != jobClass)
                return false;
                
            if (caster.level < requiredLevel)
                return false;
                
            if (caster.currentMP < manaCost)
                return false;
                
            return true;
        }
        
        public int CalculateSkillDamage(Unit caster, Unit target)
        {
            if (caster == null) return 0;
            
            float baseDamage = damage > 0 ? damage : 0;
            
            // 스킬 타입에 따른 데미지 계산
            switch (skillType)
            {
                case SkillType.Physical:
                    baseDamage += caster.attackPower * damageMultiplier;
                    break;
                case SkillType.Magical:
                    baseDamage += caster.attackPower * 0.8f * damageMultiplier; // magicPower 대신 attackPower 사용
                    break;
            }
            
            // 방어력 적용 (타겟이 있는 경우)
            if (target != null && !ignoreDefense)
            {
                float defense = target.defense;
                baseDamage = Mathf.Max(1, baseDamage - defense * 0.5f);
            }
            
            // 크리티컬 적용
            if (canCrit && caster != null)
            {
                if (UnityEngine.Random.value < 0.1f) // criticalRate 대신 기본값 사용
                {
                    baseDamage *= 1.5f; // criticalDamage 대신 기본값 사용
                }
            }
            
            return Mathf.RoundToInt(baseDamage);
        }
        
        public int CalculateSkillHealing(Unit caster)
        {
            if (caster == null) return 0;
            
            float baseHeal = healing > 0 ? healing : healAmount;
            
            // 공격력 기반 힐링 (magicPower 대신)
            baseHeal += caster.attackPower * 0.3f;
            
            return Mathf.RoundToInt(baseHeal);
        }
        
        public float CalculateDamage(float attackerPower, float targetDefense)
        {
            float damage = attackerPower * damageMultiplier;
            if (!ignoreDefense)
            {
                damage = Mathf.Max(1, damage - targetDefense * 0.5f);
            }
            return damage;
        }
        
        public float CalculateHeal(float healerPower)
        {
            return (healing > 0 ? healing : healAmount) + healerPower * 0.3f;
        }
        
        public string GetSkillInfo()
        {
            string info = $"[{skillName}]\n";
            info += $"{description}\n\n";
            info += $"타입: {GetSkillTypeName()}\n";
            info += $"대상: {GetTargetTypeName()}\n";
            info += $"마나 소모: {manaCost}\n";
            info += $"쿨다운: {cooldown}초\n";
            info += $"사거리: {range}\n";
            
            if (requiredLevel > 1)
                info += $"요구 레벨: {requiredLevel}\n";
            
            if (!string.IsNullOrEmpty(requirements))
                info += $"요구사항: {requirements}\n";
            
            return info;
        }
        
        string GetJobClassName()
        {
            return jobClass switch
            {
                JobClass.Warrior => "전사",
                JobClass.Mage => "마법사",
                JobClass.Archer => "궁수",
                JobClass.Priest => "성직자",
                JobClass.Rogue => "도적",
                JobClass.Paladin => "기사",
                _ => "공통"
            };
        }
        
        string GetSkillTypeName()
        {
            return skillType switch
            {
                SkillType.Physical => "물리",
                SkillType.Magical => "마법",
                SkillType.Healing => "치유",
                SkillType.Buff => "버프",
                SkillType.Debuff => "디버프",
                SkillType.Utility => "유틸리티",
                SkillType.Defensive => "방어",
                SkillType.Summon => "소환",
                _ => "기타"
            };
        }
        
        string GetTargetTypeName()
        {
            return targetType switch
            {
                TargetType.Self => "자신",
                TargetType.Single => "단일 대상",
                TargetType.Enemy => "적",
                TargetType.AOE => "범위",
                TargetType.Line => "직선",
                TargetType.AllAllies => "모든 아군",
                TargetType.AllEnemies => "모든 적",
                TargetType.Area => "지역",
                _ => "기타"
            };
        }
        
        public void ApplySkillEffect(Unit caster, Unit target)
        {
            if (caster == null || target == null) return;
            
            switch (skillType)
            {
                case SkillType.Physical:
                    float physicalDamage = CalculatePhysicalDamage(caster);
                    target.currentHP = Mathf.Max(0, target.currentHP - (int)physicalDamage);
                    break;
                    
                case SkillType.Magical:
                    float magicalDamage = CalculateMagicalDamage(caster);
                    target.currentHP = Mathf.Max(0, target.currentHP - (int)magicalDamage);
                    break;
                    
                case SkillType.Healing:
                    float healAmount = CalculateHealAmount(caster);
                    target.currentHP = Mathf.Min(target.maxHP, target.currentHP + (int)healAmount);
                    break;
                    
                case SkillType.Buff:
                    ApplyBuff(target);
                    break;
                    
                case SkillType.Debuff:
                    ApplyDebuff(target);
                    break;
            }
        }
        
        float CalculatePhysicalDamage(Unit caster)
        {
            float baseDamage = damageMultiplier * 100f;
            baseDamage += caster.attackPower * 0.5f;
            
            if (UnityEngine.Random.value < 0.1f) // criticalRate 대신 기본값 사용
            {
                baseDamage *= 1.5f; // criticalDamage 대신 기본값 사용
            }
            
            return baseDamage;
        }
        
        float CalculateMagicalDamage(Unit caster)
        {
            float baseDamage = damageMultiplier * 100f;
            baseDamage += caster.attackPower * 0.3f; // magicPower 대신 attackPower 사용
            
            return baseDamage;
        }
        
        float CalculateHealAmount(Unit caster)
        {
            float baseHeal = healAmount;
            baseHeal += caster.attackPower * 0.2f; // magicPower 대신 attackPower 사용
            return baseHeal;
        }
        
        void ApplyBuff(Unit target)
        {
            Debug.Log($"Applying buff to {target.unitName}");
        }
        
        void ApplyDebuff(Unit target)
        {
            Debug.Log($"Applying debuff to {target.unitName}");
        }
        
        public static List<SkillDataSO> GetSkillsForJobClass(JobClass jobClass)
        {
            var allSkills = Resources.LoadAll<SkillDataSO>("Skills");
            return allSkills.Where(skill => skill.jobClass == jobClass || skill.jobClass == JobClass.Warrior).ToList();
        }
    }
}