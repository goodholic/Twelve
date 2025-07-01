using UnityEngine;
using System;
using System.Collections.Generic;
using GuildMaster.Battle; // Unit, JobClass를 위해 추가

namespace GuildMaster.Data
{
    /// <summary>
    /// 스킬 데이터 ScriptableObject
    /// CSV 데이터를 기반으로 생성되는 스킬 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "GuildMaster/Data/SkillData", order = 8)]
    public class SkillDataSO : ScriptableObject
    {
        [Header("Skill Identity")]
        public string skillId;
        public string skillName;
        public JobClass jobClass;
        
        [Header("Skill Properties")]
        public SkillType skillType;
        public TargetType targetType;
        public int damage;
        public int healing;
        public int manaCost;
        public float cooldown;
        public int range;
        public bool areaEffect;
        
        [Header("Status Effects")]
        public BuffType buffType;
        public DebuffType debuffType;
        public float duration;
        
        [Header("Skill Description")]
        [TextArea(2, 4)]
        public string description;
        
        [Header("Visual & Audio")]
        public string animationName;
        public string soundEffectName;
        public string particleEffectName;
        
        [Header("Requirements")]
        public string requirements;
        public int requiredLevel = 1;
        
        [Header("Advanced Properties")]
        public float criticalMultiplier = 1.5f;
        public float statusChance = 1.0f;
        public int maxTargets = 1;
        public bool piercing = false;
        public float castTime = 0f;
        
        [Header("Legacy Properties")]
        public float damageMultiplier = 1f;
        public float healAmount = 0f;
        public float buffAmount = 0f;
        public int buffDuration = 0;
        public int areaOfEffect = 0;
        public Sprite skillIcon;
        public GameObject effectPrefab;
        public string animationTrigger;
        public AudioClip castSound;
        public AudioClip hitSound;
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
                
                if (Enum.TryParse<JobClass>(values[2], true, out JobClass parsedJobClass))
                    jobClass = parsedJobClass;
                
                if (Enum.TryParse<SkillType>(values[3], true, out SkillType parsedSkillType))
                    skillType = parsedSkillType;
                
                if (Enum.TryParse<TargetType>(values[4], true, out TargetType parsedTargetType))
                    targetType = parsedTargetType;
                
                if (int.TryParse(values[5], out int parsedDamage))
                    damage = parsedDamage;
                
                if (int.TryParse(values[6], out int parsedHealing))
                    healing = parsedHealing;
                
                if (int.TryParse(values[7], out int parsedManaCost))
                    manaCost = parsedManaCost;
                
                if (float.TryParse(values[8], out float parsedCooldown))
                    cooldown = parsedCooldown;
                
                if (int.TryParse(values[9], out int parsedRange))
                    range = parsedRange;
                
                if (bool.TryParse(values[10], out bool parsedAreaEffect))
                    areaEffect = parsedAreaEffect;
                
                if (Enum.TryParse<BuffType>(values[11], true, out BuffType parsedBuffType))
                    buffType = parsedBuffType;
                
                if (Enum.TryParse<DebuffType>(values[12], true, out DebuffType parsedDebuffType))
                    debuffType = parsedDebuffType;
                
                if (float.TryParse(values[13], out float parsedDuration))
                    duration = parsedDuration;
                
                description = values[14];
                animationName = values[15];
                soundEffectName = values[16];
                particleEffectName = values[17];
                requirements = values[18];
                
                // Extract level requirement
                ExtractLevelRequirement();
                
                // Legacy compatibility
                damageMultiplier = damage > 0 ? damage / 100f : 1f;
                healAmount = healing;
                buffDuration = Mathf.RoundToInt(duration);
                areaOfEffect = areaEffect ? range : 0;
                animationTrigger = animationName;
            }
        }
        
        void ExtractLevelRequirement()
        {
            if (string.IsNullOrEmpty(requirements)) return;
            
            // "Level X" 형태에서 숫자 추출
            if (requirements.StartsWith("Level "))
            {
                string levelStr = requirements.Substring(6);
                if (int.TryParse(levelStr, out int level))
                {
                    requiredLevel = level;
                }
            }
        }
        
        /// <summary>
        /// 스킬이 대상에게 사용 가능한지 확인
        /// </summary>
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
        
        /// <summary>
        /// 스킬 사용 조건 확인
        /// </summary>
        public bool CanUseSkill(Unit caster)
        {
            if (caster == null) return false;
            
            // 레벨 확인
            if (caster.level < requiredLevel) return false;
            
            // 마나 확인
            if (caster.currentMP < manaCost) return false;
            
            // 쿨다운 확인 (실제 구현에서는 쿨다운 매니저 필요)
            
            // 직업 확인
            if (jobClass != JobClass.All && caster.jobClass != jobClass) return false;
            
            return true;
        }
        
        /// <summary>
        /// 스킬 데미지 계산 (새 시스템)
        /// </summary>
        public int CalculateSkillDamage(Unit caster, Unit target)
        {
            if (damage <= 0) return 0;
            
            float finalDamage = damage;
            
            // 공격력 보정
            if (skillType == SkillType.Physical)
            {
                finalDamage += caster.attack * 0.5f;
            }
            else if (skillType == SkillType.Magical)
            {
                finalDamage += caster.magicPower * 0.5f;
            }
            
            // 방어력 계산
            if (target != null)
            {
                if (skillType == SkillType.Physical)
                {
                    finalDamage -= target.defense * 0.3f;
                }
                else if (skillType == SkillType.Magical)
                {
                    finalDamage -= target.magicResistance * 0.3f;
                }
            }
            
            // 크리티컬 계산
            if (caster != null && UnityEngine.Random.value < caster.critRate)
            {
                finalDamage *= criticalMultiplier;
            }
            
            return Mathf.Max(1, Mathf.RoundToInt(finalDamage));
        }
        
        /// <summary>
        /// 스킬 치유량 계산
        /// </summary>
        public int CalculateSkillHealing(Unit caster)
        {
            if (healing <= 0) return 0;
            
            float finalHealing = healing;
            
            // 치유력 보정
            if (caster != null)
            {
                finalHealing += caster.magicPower * 0.3f;
            }
            
            return Mathf.RoundToInt(finalHealing);
        }
        
        // Legacy methods for backward compatibility
        public float CalculateDamage(float attackerPower, float targetDefense)
        {
            float damage = attackerPower * damageMultiplier;
            
            if (!ignoreDefense)
            {
                damage = damage * (100f / (100f + targetDefense));
            }
            
            return damage;
        }
        
        public float CalculateHeal(float healerPower)
        {
            return healAmount + (healerPower * 0.5f);
        }
        
        /// <summary>
        /// 스킬 정보를 문자열로 반환
        /// </summary>
        public string GetSkillInfo()
        {
            string info = $"<b>{skillName}</b>\n";
            info += $"직업: {GetJobClassName()}\n";
            info += $"유형: {GetSkillTypeName()}\n";
            info += $"대상: {GetTargetTypeName()}\n";
            
            if (damage > 0)
                info += $"데미지: {damage}\n";
            
            if (healing > 0)
                info += $"치유: {healing}\n";
            
            if (manaCost > 0)
                info += $"마나 소모: {manaCost}\n";
            
            if (cooldown > 0)
                info += $"쿨다운: {cooldown}초\n";
            
            if (range > 0)
                info += $"사거리: {range}\n";
            
            info += $"\n{description}";
            
            return info;
        }
        
        string GetJobClassName()
        {
            return jobClass switch
            {
                JobClass.All => "공통",
                JobClass.Knight => "기사",
                JobClass.Warrior => "전사",
                JobClass.Archer => "궁수",
                JobClass.Mage => "마법사",
                JobClass.Priest => "사제",
                JobClass.Ranger => "레인저",
                JobClass.Rogue => "도적",
                JobClass.Bard => "음유시인",
                _ => jobClass.ToString()
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
                _ => skillType.ToString()
            };
        }
        
        string GetTargetTypeName()
        {
            return targetType switch
            {
                TargetType.Self => "자신",
                TargetType.Single => "단일 대상",
                TargetType.AOE => "광역",
                TargetType.Line => "직선",
                TargetType.AllAllies => "아군 전체",
                TargetType.AllEnemies => "적군 전체",
                TargetType.Area => "지역",
                TargetType.Enemy => "적",
                _ => targetType.ToString()
            };
        }
    }
}